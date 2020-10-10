using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Player2VRM
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("Craftopia.exe")]
    public class MainPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.yoship1639.plugins.player2vrm";
        public const string PluginName = "Player2VRM";
        public const string PluginVersion = "1.3.5.0";

        void Awake()
        {
            FileLogger.Init("log_" + DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".txt");

            // DLL確認
            var dlls = new string[]
            {
                "DepthFirstScheduler.dll",
                "MToon.dll",
                "UniJSON.dll",
                "MeshUtility.dll",
                "ShaderProperty.Runtime.dll",
                "UniHumanoid.dll",
                "UniUnlit.dll",
                "VRM.dll",
                "OVRLipSync.dll",
            };
            foreach (var dll in dlls)
            {
                var file = Path.Combine(Settings.ManagedDir, dll);
                if (!File.Exists(file))
                {
                    FileLogger.WriteLine(dll + "が見つかりませんでした。 Craftopia_Data/Managedフォルダに配置してください。");
                    return;
                }
                else
                {
                    FileLogger.WriteLine("OK " + dll);
                }
            }

            // shader確認
            if (!File.Exists(Path.Combine(Settings.PluginsDir, "Player2VRM.shaders")))
            {
                FileLogger.WriteLine("Player2VRM.shaders が見つかりませんでした。 BepInEx/pluginsフォルダに配置してください。");
            }
            else
            {
                FileLogger.WriteLine("OK Player2VRM.shaders");
            }

            FileLogger.Init("log_" + DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".txt");

            // settings.txtの確認
            if (!File.Exists(Settings.SettingsPath))
            {
                FileLogger.WriteLine("settings.txt が見つかりませんでした。Player2VRMフォルダ内に settings.txt を配置してください。");
            }
            else
            {
                FileLogger.WriteLine("OK settings.txt");
            }

            // avatars.txtの確認
            if (!File.Exists(Settings.AvatarsPath))
            {
                FileLogger.WriteLine("avatars.txt が見つかりませんでした。Player2VRMフォルダ内に avatars.txt を配置してください。");
            }
            else
            {
                FileLogger.WriteLine("OK avatars.txt");
            }

            if (!Settings.ReadBool("Enabled", true))
            {
                FileLogger.WriteLine("Player2VRMは現在無効になっています。");
                FileLogger.WriteLine("有効にする場合は settings.txt の Enabled を true にしてください。");
                return;
            }

            FileLogger.WriteLine("Player2VRM Enabled");

            if (!Settings.ReadBool("UseRealToonShader", false)) VRMShaders.Initialize();
            LipSync.OVRLipSyncVRM.Setup(null);

            var harmony = new Harmony("com.yoship1639.plugins.player2vrm.patch");
            harmony.PatchAll();
        }
    }
}
