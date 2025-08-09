using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using HoLLy.ManagedInjector;

namespace patchershit
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var patcherPath = Path.GetFullPath("_patcher.dll");
                if (!File.Exists(patcherPath))
                    throw new FileNotFoundException("_patcher.dll not found!", patcherPath);

                var osuProc = Process.GetProcessesByName("osu!").FirstOrDefault();
                if (osuProc == null)
                    throw new Exception("please run osu! first because im too lazy to make a launcher");

                using (var proc = new InjectableProcess((uint)osuProc.Id))
                    proc.Inject(patcherPath, "_patcher.Main", "Initialize");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                Console.Read();
            }
        }
    }
}