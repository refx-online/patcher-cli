using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using HoLLy.ManagedInjector;
using Newtonsoft.Json.Linq;

namespace patchershit
{
    internal class Program
    {
        private static readonly string ConfigPath = Path.GetFullPath("conf.db");
        private const string Domain = "remeliah.cyou";
        private const string PatcherUrl = "https://updater.remeliah.cyou/patcher";
        
        public static void Main(string[] args)
        {
            try
            {
                var osuPath = GetOsuPath();
                
                var patcherPath = Path.GetFullPath("_patcher.dll");
                var harmonyPath = Path.GetFullPath("0Harmony.dll"); // just for consistency

                var json = new WebClient().DownloadString(PatcherUrl);
                var data = JObject.Parse(json);
                
                if (!File.Exists(harmonyPath) || !HashMatches(harmonyPath, (string)((JObject)data["0Harmony.dll"])["hash_md5"]))
                    harmonyPath = DownloadPatcher(data, "0Harmony.dll", "0Harmony.dll");

                if (!File.Exists(patcherPath) || !HashMatches(patcherPath, (string)((JObject)data["_patcher.dll"])["hash_md5"]))
                    patcherPath = DownloadPatcher(data, "_patcher.dll", "_patcher.dll");
                
                var osuProc = Process.Start(new ProcessStartInfo
                {
                    FileName = osuPath,
                    Arguments = $"-devserver {Domain}",
                    UseShellExecute = false
                });
                
                if (osuProc == null)
                    throw new Exception("failed to start osu!");
                
                osuProc.WaitForInputIdle();
                Thread.Sleep(2000);
                
                using (var proc = new InjectableProcess((uint)osuProc.Id))
                    proc.Inject(patcherPath, "_patcher.Main", "Initialize");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                Console.Read();
            }
        }
        
        /// <summary>
        /// retrieves the stored osu!.exe path from <c>ConfigPath</c> / prompts the user to enter it
        /// </summary>
        /// <returns>
        /// absolute file path to <c>osu!.exe</c>
        /// </returns>
        /// <exception cref="FileNotFoundException">
        /// throws when stored path doesnt point to an existing file
        /// </exception>
        private static string GetOsuPath()
        {
            if (File.Exists(ConfigPath))
            {
                var savedPath = File.ReadAllText(ConfigPath).Trim();
                if (File.Exists(savedPath))
                    return savedPath;

                Console.WriteLine("saved osu! path not found, re-entering...");
            }

            Console.Write("enter full path to osu!.exe (ex: D:\\osu!\\osu!.exe): ");
            var path = Console.ReadLine()?.Trim('"');

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                throw new FileNotFoundException("osu!.exe path invalid.", path);

            File.WriteAllText(ConfigPath, path);
            
            return path;
        }
        
        /// <summary>
        /// grabs the dll link from the provided data and downloads it
        /// </summary>
        /// <param name="data">json object containing the download url</param>
        /// <param name="k">key to look up the url</param>
        /// <param name="name">file name to save as</param>
        /// <returns>path to the downloaded file</returns>
        /// <exception cref="Exception">throws if the key is missing / empty</exception>
        private static string DownloadPatcher(JObject data, string k, string name)
        {
            var obj = data[k] as JObject;
            if (obj == null)
                throw new Exception($"'{k}' not found");

            var url = (string)obj["url"];
            if (string.IsNullOrEmpty(url))
                throw new Exception($"no 'url' field for '{k}'");

            var filePath = Path.GetFullPath(name);
            Console.WriteLine($"downloading {name} from {url}...");

            new WebClient().DownloadFile(url, filePath);

            return filePath;
        }
        
        /// <summary>
        /// checks if hash matches
        /// </summary>
        /// <param name="filePath">path of the file</param>
        /// <param name="expectedHash">expected hash</param>
        /// <returns></returns>
        private static bool HashMatches(string filePath, string expectedHash)
        {
            if (string.IsNullOrEmpty(expectedHash) || !File.Exists(filePath))
                return false;

            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = md5.ComputeHash(stream);
                string actual = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                
                return actual == expectedHash.ToLowerInvariant();
            }
        }
    }
}