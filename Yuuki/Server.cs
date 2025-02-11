﻿using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using RestSharp;
using System.Diagnostics;
using System.Net;
using YuukiPS_Launcher.Json;

namespace YuukiPS_Launcher.Yuuki
{
    public class Server
    {
        private static string API_GITHUB_DockerGS = "https://api.github.com/repos/akbaryahya/DockerGS/";
        private static string API_GITHUB_Grasscutter = "https://api.github.com/repos/Grasscutters/Grasscutter/";
        private static string API_DL_Grasscutter_Resources = "https://gitlab.com/yukiz/GrasscutterResources/";

        public static string Serverfolder = Path.Combine(Main.CurrentlyPath, "server");

        private static string JAVA_RQS = "17";
        private static string API_GITHUB_JAVA = "https://api.github.com/repos/adoptium/temurin" + JAVA_RQS + "-binaries/";
        private static string JAVA_LOCK = "17.0.4.1_1";
        private static string JAVA_FOLDER = Path.Combine(Serverfolder, "java");
        private static string GetJavaZip = Path.Combine(JAVA_FOLDER, "java.zip");

        public static string DLJava()
        {
            if (!Directory.Exists(JAVA_FOLDER))
            {
                Console.WriteLine("No Java Folder Found so Create a new folder");
                Directory.CreateDirectory(JAVA_FOLDER);
            }
            Console.WriteLine("Check Version JAVA");
            var Javabin = Path.Combine(JAVA_FOLDER, "bin");
            if (CheckJava(Javabin))
            {
                return "Already latest version";
            }

            var found_zip = false;

            // skip download for debug
            /*
            if (File.Exists(GetJavaZip))
            {
                found_zip = true;
            }
            */

            if (!found_zip)
            {
                Console.WriteLine("Get last Java");
                var GetJavaInfo = GetJava(JAVA_LOCK);
                if (string.IsNullOrEmpty(GetJavaInfo))
                {
                    return "Failed to get java info";
                }

                Console.WriteLine("Download Java");
                var DL = new Download(GetJavaInfo, GetJavaZip);
                if (DL.ShowDialog() != DialogResult.OK)
                {
                    return "Java download failed";
                }
                else
                {
                    found_zip = true;
                }
            }

            if (found_zip)
            {
                // Unzip zip
                try
                {
                    Console.WriteLine("Unzip java");
                    FastZip fastZip = new FastZip();
                    string fileFilter = null;
                    fastZip.ExtractZip(GetJavaZip, JAVA_FOLDER, fileFilter);
                }
                catch (Exception e)
                {
                    return "Unzip failed: " + e.ToString();
                }

                // Move folder version to root java folder
                var java_folder_version = Path.Combine(JAVA_FOLDER, "jdk-" + JAVA_LOCK.Replace("_", "+"));
                if (Directory.Exists(java_folder_version))
                {
                    Microsoft.VisualBasic.FileIO.FileSystem.MoveDirectory(java_folder_version, JAVA_FOLDER, true);
                    //Tool.ExecuteCMD($"Move {java_folder_version}\\*.* {JAVA_FOLDER}");
                    Console.WriteLine("done...");
                }
                else
                {
                    return "Failed to move java version folder, maybe because the folder doesn't exist or download failed or uznip failed";
                }

                // Once again make sure version is correct.
                if (!CheckJava(Javabin))
                {
                    return "Hmm still failed to get java";
                }
            }

            return "";
        }
        public static bool CheckJava(string p = "")
        {
            //Console.WriteLine("JAVA Folder: " + p);
            string version = "";
            try
            {
                ProcessStartInfo procStartInfo = new ProcessStartInfo(p + "\\java", "-version ");
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.RedirectStandardError = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;
                Process proc = new Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
                version = proc.StandardError.ReadLine();
                //Console.WriteLine("JAVA: " + version);
                if (version.Contains(JAVA_RQS))
                {
                    //string strOutput = version.Split(' ')[2].Replace("\"", ""); 
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
            return false;
        }

        public static string? GetJava(string ver_set = "17.0.4.1_1", string os = "x64_windows")
        {
            var client = new RestClient(API_GITHUB_JAVA);
            var request = new RestRequest("releases");
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                if (response.Content != null)
                {
                    try
                    {
                        var GetData = JsonConvert.DeserializeObject<List<Update>>(response.Content);
                        if (GetData != null)
                        {
                            // Get List Releases
                            foreach (var GetVersion in GetData)
                            {
                                // Get List Asset
                                var aseet = GetVersion.assets;
                                if (aseet != null)
                                {
                                    foreach (var file in aseet)
                                    {
                                        if (file.name == "OpenJDK17U-jdk_" + os + "_hotspot_" + ver_set + ".zip")
                                        {
                                            return file.browser_download_url;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error GetJava: ", ex);
                    }
                }
            }
            else
            {
                Console.WriteLine("Error GetUpdate2: " + response.StatusCode);
            }
            return "";
        }
    }
}
