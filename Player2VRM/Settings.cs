using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using UnityEngine;

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

        public static int ReadInt(string key, int defaultValue = 0)
        {
            var str = ReadSettings(key);
            var res = defaultValue;
            int.TryParse(str, out res);
            return res;
        }

        public static float ReadFloat(string key, float defaultValue = 0.0f)
        {
            var str = ReadSettings(key);
            var res = defaultValue;
            float.TryParse(str, out res);
            return res;
        }

        public static bool ReadBool(string key, bool defaultValue = false)
        {
            var str = ReadSettings(key);
            var res = defaultValue;
            bool.TryParse(str, out res);
            return res;
        }

        public static Vector3 ReadVector3(string key, Vector3 defaultValue = default)
        {
            var str = ReadSettings(key);
            if (str == null) return defaultValue;
            var match = new Regex("\\((?<x>[^,]*?),(?<y>[^,]*?),(?<z>[^,]*?)\\)").Match(str);
            if (match.Success == false) return defaultValue;
            try
            {
                return new Vector3()
                {
                    x = float.Parse(match.Groups["x"].Value),
                    y = float.Parse(match.Groups["y"].Value),
                    z = float.Parse(match.Groups["z"].Value)
                };
            }
            catch(FormatException)
            {
                return defaultValue;
            }
        }
    }
}
