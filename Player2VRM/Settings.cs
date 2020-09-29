using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using UnityEngine;
using Oc;

namespace Player2VRM
{
    static class Settings
    {
        private static readonly string SettingsPath = Environment.CurrentDirectory + @"\Player2VRM\settings.txt";
        private static readonly string AvatorsPath = Environment.CurrentDirectory + @"\Player2VRM\avators.txt";

        public static bool isUseVRM(OcPl __instance)
        {
            //if slave and not has avator setting then return false
            bool is_slave = false;
            var slave = __instance as OcPlSlave;
            if (slave && !slave.FindNameInParentRecursive("UI"))
            {
                var selfId = OcNetMng.Inst.NetPlId_Master;
                if (SingletonMonoBehaviour<OcPlMng>.Inst.getPlSlave(selfId - 1) != slave) is_slave = true;
            }

            string playername = getPlayerName(__instance);
            string path = FindAvatorSettngs(playername);
            if (path == SettingsPath)
            {
                return !is_slave;
            }
            else
            {
                return true;
            }
        }

        public static string getPlayerName(OcPl __instance)
        {
            //acquire playername
            string playername = null;
            OcPlMng plmng = OcPlMng.Inst;
            OcNetMng ntmng = OcNetMng.Inst;
            if (plmng != null)
            {
                if (__instance.CharaMakeData != null)
                {
                    playername = __instance.CharaMakeData.Name;
                }
                if (playername == null || playername.Length == 0)
                {
                    int netid = plmng.getNetPlId(__instance);
                    if (netid != -1)
                    {
                        playername = ntmng.getPlName(netid);

                    }
                }
            }
            return playername;
        }

        public static string FindAvatorSettngs(string key)
        {
            if (key == null)
            {
                return SettingsPath;
            }

            try
            {
                var lines = File.ReadAllLines(AvatorsPath);
                foreach (var line in lines)
                {
                    try
                    {
                        if (line.Length > 1 && line.Substring(0, 2) == "//") continue;

                        var args = line.Split('=');
                        if (args.Length != 2) continue;
                        if (args[0] == key)
                        {
                            return Environment.CurrentDirectory + @"\Player2VRM\" + args[1];
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return SettingsPath;
        }

        public static bool HasAvatarSettings(OcPl pl)
        {
            string playername = getPlayerName(pl);
            string path = FindAvatorSettngs(playername);
            return path != SettingsPath;
        }

        public static string ReadSettings(string playername, string key)
        {
            try
            {
                string _SettingsPath = FindAvatorSettngs(playername);
                if (_SettingsPath == null)
                {
                    _SettingsPath = SettingsPath;
                }

                var lines = File.ReadAllLines(_SettingsPath);
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

        public static int ReadIntForPlayer(string playername, string key, int defaultValue = 0)
        {
            var str = ReadSettings(playername, key);
            var res = defaultValue;
            int.TryParse(str, out res);
            return res;
        }

        public static float ReadFloat(string playername, string key, float defaultValue = 0.0f)
        {
            var str = ReadSettings(playername, key);
            var res = defaultValue;
            float.TryParse(str, out res);
            return res;
        }

        public static bool ReadBool(string playername, string key, bool defaultValue = false)
        {
            var str = ReadSettings(playername, key);
            var res = defaultValue;
            bool.TryParse(str, out res);
            return res;
        }

        public static Vector3 ReadVector3(string playername, string key, Vector3 defaultValue = default)
        {
            var str = ReadSettings(playername, key);
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
            catch (FormatException)
            {
                return defaultValue;
            }
        }

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
            catch (FormatException)
            {
                return defaultValue;
            }
        }
    }
}
