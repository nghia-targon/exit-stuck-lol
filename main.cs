using System;
using System.IO;
using System.Net;
using System.Management;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;


namespace ExitStuckLOL
{
    // @ref https://stackoverflow.com/questions/7666408/how-to-request-administrator-permissions-when-the-program-starts
    class Program
    {
        static void Main(string[] args)
        {
            // @credit
            Console.WriteLine("---------------------------------");
            Console.WriteLine("| Author: facebook.com/nghiadev |");
            Console.WriteLine("---------------------------------");

            string RiotPort = null;
            string RiotPassword = null;
            foreach (var process in Process.GetProcessesByName("LeagueClientUX"))
            {
                try
                {
                    string cmd = GetCommandLine(process.Id);
                    Regex regex = new Regex("\"(.*?)\"");

                    var matches = regex.Matches(cmd);

                    foreach (Match match in matches)
                    {
                        string cl = match.Groups[1].Value;

                        if(cl.Contains("--app-port"))
                        {
                            RiotPort = cl.Replace("--app-port=", string.Empty);
                        }

                        if (cl.Contains("--remoting-auth-token"))
                        {
                            RiotPassword = cl.Replace("--remoting-auth-token=", string.Empty);
                        }
                    }
                } catch { }
            }

            if (RiotPort == null || RiotPassword ==  null)
            {
                Console.WriteLine("[ERROR] Cannot detect LeagueClientUX is running");
                Console.ReadLine();
                Environment.Exit(0);
            }

            try
            {
                // @ref https://stackoverflow.com/questions/9145667/how-to-post-json-to-a-server-using-c
                var httpWebRequest = (HttpWebRequest)WebRequest.Create($"https://127.0.0.1:{RiotPort}/lol-lobby/v2/lobby");
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                httpWebRequest.Headers.Add("Authorization", "Basic " + Base64Encode($"riot:{RiotPassword}"));
                httpWebRequest.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;


                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json = "{\"queueId\": 430}";

                    streamWriter.Write(json);
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                }
            }
            catch
            {
                Console.WriteLine("[ERROR] Cannot connect to Riot Client API");
                Console.ReadLine();
                Environment.Exit(0);
            }
        }

        // @ref https://stackoverflow.com/questions/2633628/can-i-get-command-line-arguments-of-other-processes-from-net-c
        static string GetCommandLine(int processId)
        {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + processId))
            using (ManagementObjectCollection objects = searcher.Get())
            {
                return objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
            }
        }

        // @ref https://stackoverflow.com/questions/11743160/how-do-i-encode-and-decode-a-base64-string
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }
    }
}
