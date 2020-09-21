using DG.Tweening;
using HarmonyLib;
using Oc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    [HarmonyPatch(typeof(OcActState))]
    [HarmonyPatch(nameof(OcActState.animPlay), typeof(string), typeof(int), typeof(float))]
    static class OcActStateVRM
    {
        static bool Prefix(OcActState __instance, string str, int layer = 0, float fadeTime = 0.15f)
        {
            if (OcPlVRM.modelMaster == null) return true;

            var owner = __instance.GetRefField<OcActState, OcCharacter>("_OwnerCharacter");
            if (owner is OcPlMaster)
            {
                if (owner.Animator && owner.Animator.enabled && owner.Animator.gameObject.activeInHierarchy)
                {
                    owner.Animator.CrossFadeInFixedTime(str, fadeTime, layer);
                    OcPlVRM.modelMaster.GetComponentInChildren<Animator>().CrossFadeInFixedTime(str, fadeTime, layer);
                }
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(OcActState))]
    [HarmonyPatch(nameof(OcActState.animPlay), typeof(int), typeof(int), typeof(float))]
    static class OcActStateVRM2
    {
        static bool Prefix(OcActState __instance, int id, int layer = 0, float fadeTime = 0.15f)
        {
            if (OcPlVRM.modelMaster == null) return true;

            var owner = __instance.GetRefField<OcActState, OcCharacter>("_OwnerCharacter");
            if (owner is OcPlMaster)
            {
                if (owner.Animator && owner.Animator.enabled && owner.Animator.gameObject.activeInHierarchy)
                {
                    owner.Animator.CrossFadeInFixedTime(id, fadeTime, layer);
                    OcPlVRM.modelMaster.GetComponentInChildren<Animator>().CrossFadeInFixedTime(id, fadeTime, layer);
                }
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(OcActState))]
    [HarmonyPatch(nameof(OcActState.animPlaySec), typeof(string), typeof(float), typeof(int), typeof(float))]
    static class OcActStateVRM3
    {
        static bool Prefix(OcActState __instance, string str, float startTime = 0f, int layer = 0, float fadeTime = 0.15f)
        {
            if (OcPlVRM.modelMaster == null) return true;

            var owner = __instance.GetRefField<OcActState, OcCharacter>("_OwnerCharacter");
            if (owner is OcPlMaster)
            {
                if (owner.Animator && owner.Animator.enabled && owner.Animator.gameObject.activeInHierarchy)
                {
                    owner.Animator.CrossFadeInFixedTime(str, fadeTime, layer, startTime);
                    OcPlVRM.modelMaster.GetComponentInChildren<Animator>().CrossFadeInFixedTime(str, fadeTime, layer, startTime);
                }
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(OcActState))]
    [HarmonyPatch(nameof(OcActState.animPlaySec), typeof(int), typeof(float), typeof(int), typeof(float))]
    static class OcActStateVRM4
    {
        static bool Prefix(OcActState __instance, int id, float startTime = 0f, int layer = 0, float fadeTime = 0.15f)
        {
            if (OcPlVRM.modelMaster == null) return true;

            var owner = __instance.GetRefField<OcActState, OcCharacter>("_OwnerCharacter");
            if (owner is OcPlMaster)
            {
                if (owner.Animator && owner.Animator.enabled && owner.Animator.gameObject.activeInHierarchy)
                {
                    owner.Animator.CrossFadeInFixedTime(id, fadeTime, layer, startTime);
                    OcPlVRM.modelMaster.GetComponentInChildren<Animator>().CrossFadeInFixedTime(id, fadeTime, layer, startTime);
                }
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(OcActState))]
    [HarmonyPatch(nameof(OcActState.animPlayBase), typeof(string), typeof(float))]
    static class OcActStateVRM5
    {
        static bool Prefix(OcActState __instance, string str, float fadeTime = 0.15f)
        {
            if (OcPlVRM.modelMaster == null) return true;

            var owner = __instance.GetRefField<OcActState, OcCharacter>("_OwnerCharacter");
            if (owner is OcPlMaster)
            {
                if (owner.Animator && owner.Animator.enabled && owner.Animator.gameObject.activeInHierarchy)
                {
                    owner.Animator.CrossFadeInFixedTime(str, fadeTime, 0);
                    OcPlVRM.modelMaster.GetComponentInChildren<Animator>().CrossFadeInFixedTime(str, fadeTime, 0);
                }
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(OcActState))]
    [HarmonyPatch(nameof(OcActState.animPlayBase), typeof(int), typeof(float))]
    static class OcActStateVRM6
    {
        static bool Prefix(OcActState __instance, int id, float fadeTime = 0.15f)
        {
            if (OcPlVRM.modelMaster == null) return true;

            var owner = __instance.GetRefField<OcActState, OcCharacter>("_OwnerCharacter");
            if (owner is OcPlMaster)
            {
                if (owner.Animator && owner.Animator.enabled && owner.Animator.gameObject.activeInHierarchy)
                {
                    owner.Animator.CrossFadeInFixedTime(id, fadeTime, 0);
                    OcPlVRM.modelMaster.GetComponentInChildren<Animator>().CrossFadeInFixedTime(id, fadeTime, 0);
                }
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(AsPl_Sprint))]
    [HarmonyPatch(nameof(AsPl_Sprint.enter))]
    static class AsPl_SprintVRM
    {
        static bool Prefix(AsPl_Sprint __instance)
        {
            if (OcPlVRM.modelMaster == null) return true;

            var pl = __instance.GetRefField<OcActState_Pl, OcPl>("_Pl");
            __instance.GetRefField<AsPl_Sprint, float>("_RunBlockCheckStartTimer") = 0f;
            __instance.GetRefField<AsPl_Sprint, float>("_SprintContinueTimer") = 0.2f;
            if (pl.isActPrev<OcPl.As>(OcPl.As.LandSprint))
            {
                __instance.animPlayBase(OcAnimHash.Sprint, 0.25f);
                return false;
            }
            if (pl.isActPrev<OcPl.As>(OcPl.As.RollF) || pl.isActPrev<OcPl.As>(OcPl.As.RollF_Sprint))
            {
                __instance.animPlayBase(OcAnimHash.Sprint, 0.25f);
                return false;
            }
            float normalizedTimeOffset = 0f;
            if (pl.isActPrev<OcPl.As>(OcPl.As.MovementStand))
            {
                normalizedTimeOffset = (pl.getAct<OcPl.As>(OcPl.As.MovementStand) as AsPl_MovementStand).RunEndMotRate;
            }
            //__instance.animPlaySec(OcAnimHash.Sprint, normalizedTimeOffset, 0, 0.1f);
            pl.Animator.CrossFade(OcAnimHash.Sprint, 0.1f, 0, normalizedTimeOffset);
            OcPlVRM.modelMaster.GetComponentInChildren<Animator>().CrossFade(OcAnimHash.Sprint, 0.1f, 0, normalizedTimeOffset);
            return false;
        }
    }

    [HarmonyPatch(typeof(AsPl_MovementStand))]
    [HarmonyPatch("animPlayMovemnt")]
    static class AsPl_MovementStandVRM
    {
        static bool Prefix(AsPl_MovementStand __instance)
        {
            if (OcPlVRM.modelMaster == null) return true;

            var pl = __instance.GetRefField<OcActState_Pl, OcPl>("_Pl");
            if (pl.isActPrev<OcPl.As>(OcPl.As.Sprint))
            {
                AsPl_Sprint asPl_Sprint = pl.getAct<OcPl.As>(OcPl.As.Sprint) as AsPl_Sprint;
                OcPlVRM.modelMaster.GetComponentInChildren<Animator>().CrossFade(OcAnimHash.MovementStand, 0.6f, 0, asPl_Sprint.SprintEndMotRate);
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(AsPl_MovementBase))]
    [HarmonyPatch("move")]
    static class AsPl_MovementBaseVRM
    {
        static void Postfix(AsPl_MovementBase __instance)
        {
            if (OcPlVRM.modelMaster == null) return;

            var accel = __instance.GetRefField<AsPl_MovementBase, Vector3>("_MoveAccel");
            var anim = OcPlVRM.modelMaster.GetComponentInChildren<Animator>();
            anim.SetFloat("MoveSpeed.x", accel.x);
            anim.SetFloat("MoveSpeed.z", accel.z);
        }
    }

    [HarmonyPatch(typeof(ShaderStore))]
    [HarmonyPatch("GetShader")]
    static class ShaderToRealToon
    {
        static bool Prefix(ShaderStore __instance, ref Shader __result, glTFMaterial material)
        {
            if (material == null)
            {
                __result = Shader.Find("Standard");
                return false;
            }
            if (material.extensions != null && material.extensions.KHR_materials_unlit != null)
            {
                __result = Shader.Find("RealToon/Version 5/Default/Default");
                return false;
            }
            __result = Shader.Find("Standard");
            return false;
        }
    }

    [HarmonyPatch(typeof(MaterialImporter))]
    [HarmonyPatch("CreateMaterial")]
    static class MaterialImporterVRM
    {
        static void Postfix(MaterialImporter __instance, ref Material __result, int i, glTFMaterial x, bool hasVertexColor)
        {
            __result.SetFloat("_DoubleSided", x.doubleSided ? 0 : 2);
            __result.SetFloat("_Cutout", x.alphaCutoff);

            if (x.pbrMetallicRoughness != null)
            {
                if (x.pbrMetallicRoughness.baseColorFactor != null && x.pbrMetallicRoughness.baseColorFactor.Length == 3)
                {
                    float[] baseColorFactor2 = x.pbrMetallicRoughness.baseColorFactor;
                    __result.SetColor("_MainColor", new Color(baseColorFactor2[0], baseColorFactor2[1], baseColorFactor2[2]).gamma);
                }
                else if (x.pbrMetallicRoughness.baseColorFactor != null && x.pbrMetallicRoughness.baseColorFactor.Length == 4)
                {
                    float[] baseColorFactor2 = x.pbrMetallicRoughness.baseColorFactor;
                    __result.SetColor("_MainColor", new Color(baseColorFactor2[0], baseColorFactor2[1], baseColorFactor2[2], baseColorFactor2[3]).gamma);
                }
                __result.SetFloat("_RefMetallic", x.pbrMetallicRoughness.metallicFactor);
            }
            

            if (x.normalTexture != null && x.normalTexture.index != -1)
            {
                __result.SetFloat("_N_F_NM", 1.0f);
                var func = __instance.GetRefField<MaterialImporter, Func<int, TextureItem>>("GetTextureFunc");
                TextureItem textureItem4 = func(x.normalTexture.index);
                if (textureItem4 != null)
                {
                    string text2 = "_NormalMap";
                    __result.SetTexture(text2, textureItem4.ConvertTexture(text2, 1f));
                    __result.SetFloat("_NormalMapIntensity", x.normalTexture.scale);
                }
                SetTextureOffsetAndScale(__result, x.normalTexture, "_NormalMap");
            }
        }

        private static void SetTextureOffsetAndScale(Material material, glTFTextureInfo textureInfo, string propertyName)
        {
            if (textureInfo.extensions != null && textureInfo.extensions.KHR_texture_transform != null)
            {
                glTF_KHR_texture_transform khr_texture_transform = textureInfo.extensions.KHR_texture_transform;
                Vector2 vector = new Vector2(0f, 0f);
                Vector2 vector2 = new Vector2(1f, 1f);
                if (khr_texture_transform.offset != null && khr_texture_transform.offset.Length == 2)
                {
                    vector = new Vector2(khr_texture_transform.offset[0], khr_texture_transform.offset[1]);
                }
                if (khr_texture_transform.scale != null && khr_texture_transform.scale.Length == 2)
                {
                    vector2 = new Vector2(khr_texture_transform.scale[0], khr_texture_transform.scale[1]);
                }
                vector.y = (vector.y + vector2.y - 1f) * -1f;
                material.SetTextureOffset(propertyName, vector);
                material.SetTextureScale(propertyName, vector2);
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

    public class AnimFitter : MonoBehaviour
    {
        private OcPl pl;
        public Animator masterAnim;

        void Start()
        {
            pl = GetComponent<OcPl>();
        }

        private AnimatorStateInfo prevInfo;
        void Update()
        {
            var info = pl.Animator.GetCurrentAnimatorStateInfo(0);
            var time = info.normalizedTime;

            masterAnim.speed = pl.Animator.speed;
            if (prevInfo.shortNameHash != info.shortNameHash && info.shortNameHash == OcAnimHash.MovementStand) masterAnim.ForceStateNormalizedTime(time);
            if (prevInfo.shortNameHash != info.shortNameHash && info.shortNameHash == OcAnimHash.Sprint) masterAnim.ForceStateNormalizedTime(time);

            prevInfo = info;
        }

        public static void ShowHierarchy(Transform root, int num = 0)
        {
            if (num == 0) UnityEngine.Debug.LogError("ShowHierarchy ------------------------------- Start");
            string str = "";
            for (var i = 0; i < num; i++) str += "  ";
            UnityEngine.Debug.LogWarning(str + root.name + $"【{root.gameObject.layer}】" + " (" + root.GetComponents<Component>().Join(c => c.GetType().Name) + ")" + root.gameObject.activeInHierarchy);
            for (var i = 0; i < root.childCount; i++)
            {
                ShowHierarchy(root.GetChild(i), num + 1);
            }
            if (num == 0) UnityEngine.Debug.LogError("ShowHierarchy ------------------------------- End");
        }
    }

    [HarmonyPatch(typeof(OcPl))]
    [HarmonyPatch("charaChangeSteup")]
    static class OcPlVRM
    {
        static GameObject vrmModel;
        public static GameObject modelMaster;
        public static GameObject modelSlave;

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

                foreach (var smr in vrmModel.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    smr.receiveShadows = false;
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

            if (__instance is OcPlMaster)
            {
                if (modelMaster == null) modelMaster = GameObject.Instantiate<GameObject>(vrmModel);
                var instAnim = __instance.GetComponentInChildren<Animator>();
                var masterAnim = modelMaster.GetComponentInChildren<Animator>();
                modelMaster.transform.SetParent(__instance.transform, false);
                masterAnim.runtimeAnimatorController = instAnim.runtimeAnimatorController;

                var fitter = __instance.GetComponent<AnimFitter>();
                if (fitter == null)
                {
                    fitter = __instance.gameObject.AddComponent<AnimFitter>();
                    fitter.masterAnim = masterAnim;
                }
            }

            if (__instance is OcPlSlave)
            {
                if (modelSlave == null) modelSlave = GameObject.Instantiate<GameObject>(vrmModel);
                var instAnim = __instance.GetComponentInChildren<Animator>();
                var masterAnim = modelSlave.GetComponentInChildren<Animator>();
                modelSlave.transform.SetParent(__instance.transform, false);
                masterAnim.runtimeAnimatorController = instAnim.runtimeAnimatorController;

                var fitter = __instance.GetComponent<AnimFitter>();
                if (fitter == null)
                {
                    fitter = __instance.gameObject.AddComponent<AnimFitter>();
                    fitter.masterAnim = masterAnim;
                }
            }
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
