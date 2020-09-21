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
            pl.Animator.CrossFade(OcAnimHash.Sprint, 0.1f, 0, normalizedTimeOffset);
            OcPlVRM.modelMaster.GetComponentInChildren<Animator>().CrossFade(OcAnimHash.Sprint, 0.1f, 0, normalizedTimeOffset);
            return false;
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
            __result = Shader.Find("RealToon/Version 5/Default/Default");
            return false;
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
                modelMaster.transform.SetParent(__instance.transform, false);
                modelMaster.GetComponentInChildren<Animator>().runtimeAnimatorController = __instance.GetComponentInChildren<Animator>().runtimeAnimatorController;
            }

            if (__instance is OcPlSlave)
            {
                if (modelSlave == null) modelSlave = GameObject.Instantiate<GameObject>(vrmModel);
                modelSlave.transform.SetParent(__instance.transform, false);
                modelSlave.GetComponentInChildren<Animator>().runtimeAnimatorController = __instance.GetComponentInChildren<Animator>().runtimeAnimatorController;
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
