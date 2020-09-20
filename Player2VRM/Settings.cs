using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Player2VRM
{
	static class Settings
	{
		private static readonly string SettingsPath = Environment.CurrentDirectory + @"\Player2VRM\settings.txt";

		public static string ReadSettings(string key)
		{
            try
            {
                var lines = File.ReadAllLines(SettingsPath);
                foreach (var line in lines)
                {
                    try
                    {
                        var args = line.Split('=');
                        if (args.Length != 2) continue;
                        if (args[0] == key)
                        {
                            return args[1];
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return null;
        }
	}
}
