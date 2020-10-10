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
        public static readonly string Player2VRMDir = Environment.CurrentDirectory + @"\Player2VRM";
        public static readonly string PluginsDir = Environment.CurrentDirectory + @"\BepInEx\plugins";
        public static readonly string ManagedDir = Environment.CurrentDirectory + @"\Craftopia_Data\Managed";
        public static readonly string SettingsPath = Player2VRMDir + @"\settings.txt";
        public static readonly string AvatarsPath = Player2VRMDir + @"\avatars.txt";

        private static Dictionary<string, string> dic_common_settings = new Dictionary<string, string>();
        private static Dictionary<string, Dictionary<string, string>> dic_players_settings = new Dictionary<string, Dictionary<string, string>>();

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

            bool use_multi = ReadBool("UseMulti", false);

            if (!use_multi && is_slave)
            {
                // マルチなしでホスト以外なら不使用
                return false;
            }

            if (!use_multi && !is_slave)
            {
                // マルチなしホストなら基本設定で評価
                return true;
            }

            // マルチありでの評価
            string playername = getPlayerName(__instance);

            // プレイヤー名は取れない場合がある
            if (playername == null)
            {
                return false;
            }


            // 設定が取れなかった場合はnullが返るのでfalseになる
            return ReadBool(playername, "Enabled", false);

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
                if (playername == null || playername.Length == 0 || playername == "no name")
                {
                    int netid = plmng.getNetPlId(__instance);
                    if (netid != -1)
                    {
                        playername = ntmng.getPlName(netid);

                    }
                }
            }

            if (playername == null || playername == "no name")
            {
                UnityEngine.Debug.LogWarning("プレイヤー名の取得に失敗しました");
                return null;
            }

            return playername;
        }

        public static string GetAvatarSettingsFileName(string key)
        {
            try
            {
                var lines = File.ReadAllLines(AvatarsPath);
                foreach (var line in lines)
                {
                    try
                    {
                        if (line.Length > 1 && line.Substring(0, 2) == "//") continue;

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

        public static string FindAvatarSettngs(string key)
        {
            if (key == null)
            {
                return SettingsPath;
            }

            try
            {
                var lines = File.ReadAllLines(AvatarsPath);
                foreach (var line in lines)
                {
                    try
                    {
                        if (line.Length > 1 && line.Substring(0, 2) == "//") continue;

                        var args = line.Split('=');
                        if (args.Length != 2) continue;

                        if (args[0] == key)
                        {
                            UnityEngine.Debug.LogWarning("プレイヤー別設定ファイルが見つかりました path=" + Environment.CurrentDirectory + @"\Player2VRM\" + args[1]);
                            return Environment.CurrentDirectory + @"\Player2VRM\" + args[1];
                        }
                    }
                    catch { }
                }
            }
            catch { }

            UnityEngine.Debug.LogWarning("プレイヤー別設定ファイルがありません プレイヤー名=" + key);

            return SettingsPath;
        }

        public static bool HasAvatarSettings(OcPl pl)
        {
            string playername = getPlayerName(pl);
            return dic_players_settings.ContainsKey(playername);
        }

        public static string ReadSettings(string playername, string key, bool useCache = true)
        {
            if (useCache && dic_players_settings.ContainsKey(playername))
            {
                if (dic_players_settings[playername].ContainsKey(key))
                {
                    return dic_players_settings[playername][key];
                }
                else
                {
                    return null;
                }
            }

            string retval = null;
            try
            {
                string _SettingsPath = FindAvatarSettngs(playername);
                if (_SettingsPath == null)
                {
                    return null;
                }
                dic_players_settings.Add(playername, new Dictionary<string, string>());
                var lines = File.ReadAllLines(_SettingsPath);
                foreach (var line in lines)
                {
                    try
                    {
                        if (line.Length > 1 && line.Substring(0, 2) == "//") continue;

                        var args = line.Split('=');
                        if (args.Length != 2) continue;
                        dic_players_settings[playername][args[0]] = args[1];

                        if (args[0] == key)
                        {
                            retval = args[1];
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return retval;
        }

        public static int ReadInt(string playername, string key, int defaultValue = 0, bool useCache = true)
        {
            var str = ReadSettings(playername, key);
            var res = defaultValue;
            if (int.TryParse(str, out res)) return res;
            return defaultValue;
        }

        public static float ReadFloat(string playername, string key, float defaultValue = 0.0f, bool useCache = true)
        {
            var str = ReadSettings(playername, key);
            var res = defaultValue;
            if (float.TryParse(str, out res)) return res;
            return defaultValue;
        }

        public static bool ReadBool(string playername, string key, bool defaultValue = false, bool useCache = true)
        {
            var str = ReadSettings(playername, key);
            var res = defaultValue;
            if (bool.TryParse(str, out res)) return res;
            return defaultValue;
        }

        public static Vector3 ReadVector3(string playername, string key, Vector3 defaultValue = default, bool useCache = true)
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

        public static string ReadSettings(string key, bool useCache = true)
        {
            if (useCache && dic_common_settings.ContainsKey(key))
            {
                return dic_common_settings[key];
            }

            string retval = null;

            try
            {
                var lines = File.ReadAllLines(SettingsPath);
                foreach (var line in lines)
                {
                    try
                    {
                        if (line.Length > 1 && line.Substring(0, 2) == "//") continue;

                        var args = line.Split('=');
                        if (args.Length != 2) continue;
                        dic_common_settings[args[0]] = args[1];
                        if (args[0] == key)
                        {
                            retval = args[1];
                        }

                    }
                    catch { }
                }
            }
            catch { }

            return retval;
        }

        public static int ReadInt(string key, int defaultValue = 0, bool useCache = true)
        {
            var str = ReadSettings(key);
            var res = defaultValue;
            if (int.TryParse(str, out res)) return res;
            return defaultValue;
        }

        public static float ReadFloat(string key, float defaultValue = 0.0f, bool useCache = true)
        {
            var str = ReadSettings(key);
            var res = defaultValue;
            if (float.TryParse(str, out res)) return res;
            return defaultValue;
        }

        public static bool ReadBool(string key, bool defaultValue = false, bool useCache = true)
        {
            var str = ReadSettings(key);
            var res = defaultValue;
            if (bool.TryParse(str, out res)) return res;
            return defaultValue;
        }

        public static Vector3 ReadVector3(string key, Vector3 defaultValue = default, bool useCache = true)
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
