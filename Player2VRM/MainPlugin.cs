using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
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
        public const string PluginVersion = "1.2.0.0";

        void Awake()
        {
            if (!Settings.ReadBool("Enabled", true)) return;

            if (!Settings.ReadBool("UseRealToonShader", false)) VRMShaders.Initialize();
            LipSync.OVRLipSyncVRM.Setup(null);

            var harmony = new Harmony("com.yoship1639.plugins.player2vrm.patch");
            harmony.PatchAll();
        }
    }
}
