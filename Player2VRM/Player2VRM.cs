using HarmonyLib;
using Oc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UniGLTF;
using UnityEngine;
using VRM;

namespace Player2VRM
{
    [HarmonyPatch(typeof(OcPlHeadPrefabSetting))]
    [HarmonyPatch("Start")]
    static class OcPlHeadPrefabSettingVRM
    {
        static void Postfix(OcPl __instance)
        {
            foreach (var mr in __instance.GetComponentsInChildren<MeshRenderer>())
            {
                mr.enabled = false;
            }
        }
    }

    [HarmonyPatch(typeof(OcPlEquip))]
    [HarmonyPatch("setDraw")]
    static class OcPlEquipVRM
    {
        static bool Prefix(OcPlEquip __instance, ref bool isDraw)
        {
            var str = Settings.ReadSettings("DrawEquipHead");
            var flag = true;
            if (__instance.EquipSlot == OcEquipSlot.EqHead && bool.TryParse(str, out flag))
            {
                if (!flag)
                {
                    isDraw = false;
                    return true;
                }
            }

            str = Settings.ReadSettings("DrawEquipAccessory");
            flag = true;
            if (__instance.EquipSlot == OcEquipSlot.Accessory && bool.TryParse(str, out flag))
            {
                if (!flag)
                {
                    isDraw = false;
                    return true;
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(OcPlCharacterBuilder))]
    [HarmonyPatch("ChangeHair")]
    static class OcPlCharacterBuilderVRM
    {
        static void Postfix(OcPlCharacterBuilder __instance, GameObject prefab, int? layer = null)
        {
            var go = __instance.GetRefField<OcPlCharacterBuilder, GameObject>("hair");
            foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
            {
                mr.enabled = false;
            }
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.enabled = false;
            }
        }
    }

    [HarmonyPatch(typeof(Shader))]
    [HarmonyPatch(nameof(Shader.Find))]
    static class ShaderPatch
    {
        static bool Prefix(ref Shader __result, string name)
        {
            if (VRMShaders.Shaders.TryGetValue(name, out var shader))
            {
                __result = shader;
                return false;
            }

            return true;
        }
    }

    public static class VRMShaders
    {
        public static Dictionary<string, Shader> Shaders { get; } = new Dictionary<string, Shader>();

        public static void Initialize()
        {
            var bundlePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"Player2VRM.shaders");
            if (File.Exists(bundlePath))
            {
                var assetBundle = AssetBundle.LoadFromFile(bundlePath);
                var assets = assetBundle.LoadAllAssets<Shader>();
                foreach (var asset in assets)
                {
                    UnityEngine.Debug.Log("Add Shader: " + asset.name);
                    Shaders.Add(asset.name, asset);
                }
            }
        }
    }

    class CloneHumanoid : MonoBehaviour
    {
        HumanPoseHandler orgPose, vrmPose;
        HumanPose hp = new HumanPose();
        GameObject instancedModel;

        public void Setup(GameObject vrmModel, Animator orgAnim)
        {
            var instance = instancedModel ?? Instantiate(vrmModel);
            foreach (var sm in instance.GetComponentsInChildren<Renderer>())
                sm.enabled = true;
            instance.transform.SetParent(orgAnim.transform, false);
            PoseHandlerCreate(orgAnim, instance.GetComponent<Animator>());
            instancedModel = instance;
        }

        void PoseHandlerCreate(Animator org, Animator vrm)
        {
            OnDestroy();
            orgPose = new HumanPoseHandler(org.avatar, org.transform);
            vrmPose = new HumanPoseHandler(vrm.avatar, vrm.transform);
        }

        void OnDestroy()
        {
            if (orgPose != null)
                orgPose.Dispose();
            if (vrmPose != null)
                vrmPose.Dispose();
        }

        void LateUpdate()
        {
            orgPose.GetHumanPose(ref hp);
            vrmPose.SetHumanPose(ref hp);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }

    [HarmonyPatch(typeof(OcPl))]
    [HarmonyPatch("charaChangeSteup")]
    static class OcPlVRM
    {
        static GameObject vrmModel;

        static void Postfix(OcPl __instance)
        {
            if (vrmModel == null)
            {
                //カスタムモデル名の取得(設定ファイルにないためLogの出力が不自然にならないよう調整)
                var ModelStr = Settings.ReadSettings("ModelName");
                var path = Environment.CurrentDirectory + @"\Player2VRM\player.vrm";
                if (ModelStr != null)
                    path = Environment.CurrentDirectory + @"\Player2VRM\" + ModelStr + ".vrm";

                try
                {
                    vrmModel = ImportVRM(path);
                }
                catch
                {
                    if(ModelStr != null)
                        UnityEngine.Debug.LogWarning("VRMファイルの読み込みに失敗しました。settings.txt内のModelNameを確認してください。");
                    else
                        UnityEngine.Debug.LogWarning("VRMファイルの読み込みに失敗しました。Player2VRMフォルダにplayer.vrmを配置してください。");
                    return;
                }

                var receiveShadows = Settings.ReadBool("ReceiveShadows");
                if (!receiveShadows)
                {
                    foreach (var smr in vrmModel.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        smr.receiveShadows = false;
                    }
                }

                // プレイヤースケール調整
                {
                    var scaleStr = Settings.ReadSettings("PlayerScale");
                    var scale = 1.0f;
                    if (scaleStr != null && float.TryParse(scaleStr, out scale))
                    {
                        __instance.transform.localScale *= scale;
                        vrmModel.transform.localScale /= scale;
                    }
                }
            }

            foreach (var smr in __instance.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                foreach (var mat in smr.materials)
                {
                    mat.SetFloat("_EnableTextureTransparent", 1.0f);
                }
                smr.enabled = false;
                Transform trans = smr.transform;
                while (vrmModel != null && trans != null)
                {
                    if (trans.name.Contains(vrmModel.name))
                    {
                        smr.enabled = true;
                        break;
                    }
                    trans = trans.parent;
                }
            }

            __instance.Animator.gameObject.GetOrAddComponent<CloneHumanoid>().Setup(vrmModel, __instance.Animator);
        }

        private static GameObject ImportVRM(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var context = new VRMImporterContext();
            context.ParseGlb(bytes);

            try
            {
                context.Load();
            }
            catch { }

            // モデルスケール調整
            var scaleStr = Settings.ReadSettings("ModelScale");
            var scale = 1.0f;
            if (scaleStr != null && float.TryParse(scaleStr, out scale))
            {
                context.Root.transform.localScale *= scale;
            }

            return context.Root;
        }
    }
}
