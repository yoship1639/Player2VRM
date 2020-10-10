using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Player2VRM
{
	static class FileLogger
	{
		private static readonly string LogDirPath = Settings.Player2VRMDir + @"\logs";

		private static string path;
		public static void Init(string filename)
		{
			path = Path.Combine(LogDirPath, filename);

			if (!Directory.Exists(LogDirPath))
			{
				Directory.CreateDirectory(LogDirPath);
			}
		}

		public static void WriteLine(string message)
		{
			if (string.IsNullOrEmpty(path)) return;
			File.AppendAllLines(path, new string[] { message });
		}
	}
}
