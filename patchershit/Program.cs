using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using HoLLy.ManagedInjector;

namespace patchershit
{
    internal class Program
    {
        private static readonly string ConfigPath = Path.GetFullPath("conf.db");
        private const string Domain = "remeliah.cyou";
        
        public static void Main(string[] args)
        {
            try
            {
                var osuPath = GetOsuPath();
                
                // TODO: might just get this from the updater server
                var patcherPath = Path.GetFullPath("_patcher.dll");
                if (!File.Exists(patcherPath))
                    throw new FileNotFoundException("_patcher.dll not found!", patcherPath);
                
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
    }
}