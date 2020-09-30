using HarmonyLib;
using Oc;
using SR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UniGLTF;
using UniRx;
using UnityEngine;
using VRM;
using Oc.Item;

namespace Player2VRM
{
    [HarmonyPatch(typeof(OcPlHeadPrefabSetting))]
    [HarmonyPatch("Start")]
    static class OcPlHeadPrefabSettingVRM
    {
        static void Postfix(OcPlHeadPrefabSetting __instance)
        {
            if (!Settings.ReadBool("UseMulti", false))
            {
                OcPl pl = __instance.GetComponentInParentRecursive<OcPl>();
                var slave = pl as OcPlSlave;
                if (slave && !slave.FindNameInParentRecursive("UI"))
                {
                    var selfId = OcNetMng.Inst.NetPlId_Master;
                    if (SingletonMonoBehaviour<OcPlMng>.Inst.getPlSlave(selfId - 1) != slave) return;
                }
            }

            foreach (var mr in __instance.GetComponentsInChildren<MeshRenderer>())
            {
                mr.enabled = false;
            }
        }
    }


    [HarmonyPatch(typeof(OcPl))]
    [HarmonyPatch(nameof(OcPl.lateMove))]
    static class EquipAdjustPos_OcPlEquipCtrl_lateMove
    {
        // OcPlEquipCtrlに紐づくOcPlEquipのリスト
        static readonly Dictionary<OcPlEquipCtrl, HashSet<OcPlEquip>> plEquipCtrlCorrespondedplEquips = new Dictionary<OcPlEquipCtrl, HashSet<OcPlEquip>>();

        // OcEquipSlot別の、VRMモデルのどのボーンを親にするかの設定
        static readonly IReadOnlyDictionary<OcEquipSlot, HumanBodyBones> epuipBaseBones = new Dictionary<OcEquipSlot, HumanBodyBones>
        {
            {OcEquipSlot.EqHead, HumanBodyBones.Head},
            {OcEquipSlot.Accessory, HumanBodyBones.Hips},
            //{OcEquipSlot.FlightUnit, HumanBodyBones.Hips}, // グライダー中は姿勢が固定なのでモデル追従にする意味がない

            // 頭装備とアクセサリ以外は不具合があるため、VRMモデル追従の設定ができないようにしておく
            //{OcEquipSlot.WpSub, HumanBodyBones.RightHand}, // 追従設定すると盾を装備した場合に背中に背負わなくなる
            //{OcEquipSlot.WpDual, HumanBodyBones.Spine},  // 追従設定すると攻撃モーション中も背中のまま移動しない（向きだけ変わる）
            //{OcEquipSlot.WpTwoHand, HumanBodyBones.Spine}, // 追従設定すると攻撃モーション中も背中のまま移動しない（向きだけ変わる）。弓を構えたとき、弓の位置は変わるけど、矢の位置は変わらない。
            //{OcEquipSlot.Ammo, HumanBodyBones.Hips}, 
            //{OcEquipSlot.EqBody, HumanBodyBones.Hips},
            //{OcEquipSlot.WpMain, HumanBodyBones.Spine},
        };

        // OcEquipSlot別の設定ファイルのKey名
        static readonly IReadOnlyDictionary<OcEquipSlot, string> equipSlot2Key = new Dictionary<OcEquipSlot, string>
        {
            { OcEquipSlot.EqHead, "EquipHead" },
            { OcEquipSlot.Accessory, "EquipAccessory" },
            { OcEquipSlot.FlightUnit, "EquipFlightUnit" },
            { OcEquipSlot.WpSub, "EquipSub" },
            { OcEquipSlot.WpDual, "EquipDual" },
            { OcEquipSlot.WpTwoHand, "EquipTwoHand" },
            // 以下は用途不明or不具合があるので読み込まないようにしておく
            //{ OcEquipSlot.Ammo, "EquipAmmo" }, // これの位置を変えると何に影響があるのか不明
            //{ OcEquipSlot.EqBody, "EquipBody" }, // これの位置を変えると何に影響があるのか不明
            //{ OcEquipSlot.WpMain, "EquipMain" }, // これの位置を変えると、ピッケル・斧の所持位置と、壁などの設置場所（！！）が変わる。
        };

        // 装備位置変更設定をキャッシュするか（毎フレームパースするのは無駄）。この設定自体はキャッシュしない。
        static bool CachingEnabled => !Settings.ReadBool("DynamicEquipAdjustment", false);

        // 装備位置変更設定のキャッシュ（VRMモデルに合わせるかどうか、オフセット値）
        static readonly Dictionary<OcEquipSlot, bool> equipPositionIsAdujstedToVrmModel = new Dictionary<OcEquipSlot, bool>();
        static readonly Dictionary<OcEquipSlot, Vector3> equipPositionOffsets = new Dictionary<OcEquipSlot, Vector3>();
        // 装備の本来の親Transform
        static readonly Dictionary<OcPlEquipCtrl, Transform> originalParentTransform = new Dictionary<OcPlEquipCtrl, Transform>();

        // VRMモデルのアニメータのキャッシュ（OcPl別にキャッシュ）
        static readonly Dictionary<OcPl, Animator> plRelatedModelAnimator = new Dictionary<OcPl, Animator>();

        // 装備品一覧の取得
        internal static HashSet<OcPlEquip> GetPlEquips(OcPlEquipCtrl plEquipCtrl)
        {
            if (plEquipCtrlCorrespondedplEquips.TryGetValue(plEquipCtrl, out var plEquips))
            {
                return plEquips;
            }
            else
            {
                // OcPlEquipCtrlがDestoryされるタイミングがわからないので、新規追加のタイミングでDictionary中のOcPlEquipCtrlの存在チェックを実施する
                foreach (var destroyedPlEquipCtrl in plEquipCtrlCorrespondedplEquips.Keys.Where(key => key == null).ToArray())
                {
                    plEquipCtrlCorrespondedplEquips.Remove(destroyedPlEquipCtrl);
                }
                foreach (var destroyedPlEquipCtrl in originalParentTransform.Keys.Where(key => key == null).ToArray())
                {
                    originalParentTransform.Remove(destroyedPlEquipCtrl);
                }

                var newPlEquips = new HashSet<OcPlEquip>();
                plEquipCtrlCorrespondedplEquips.Add(plEquipCtrl, newPlEquips);
                return newPlEquips;
            }
        }

        static Vector3 GetOffset(OcEquipSlot equipSlot, string playername = null)
        {
            Vector3 offset;
            if (equipPositionOffsets.TryGetValue(equipSlot, out offset) && CachingEnabled)
            {
                return offset;
            }

            offset = equipSlot2Key.TryGetValue(equipSlot, out var key)
                ? Settings.ReadVector3(playername, $"{key}Offset", Vector3.zero)
                : Vector3.zero;
            if (CachingEnabled) equipPositionOffsets.Add(equipSlot, offset);
            return offset;

        }
        static bool IsAdujstedToVrmModel(OcEquipSlot equipSlot)
        {
            bool result;
            if (equipPositionIsAdujstedToVrmModel.TryGetValue(equipSlot, out result) && CachingEnabled)
            {
                return result;
            }

            result = equipSlot2Key.TryGetValue(equipSlot, out var key)
                ? Settings.ReadBool($"{key}FollowsModel", false)
                : false;
            if (CachingEnabled) equipPositionIsAdujstedToVrmModel.Add(equipSlot, result);
            return result;
        }

        static Animator GetPlRelatedModelAnimator(OcPl pl)
        {
            if (plRelatedModelAnimator.TryGetValue(pl, out var anim) == false || anim == null) // Dictionaryにキャッシュされて無いorデストロイ済み
            {
                anim = pl
                    .Animator.gameObject
                    .GetComponent<CloneHumanoid>()
                    .GetInstancedVRMModel()
                    .GetComponent<Animator>();
                plRelatedModelAnimator[pl] = anim; // インデクサでのアクセスならkeyの存在有無にかかわらず追加・更新できる
            }
            return anim;
        }

        static void AdjustEquipPos(OcPlEquip plEquip, string playername = null)
        {
            if (IsAdujstedToVrmModel(plEquip.EquipSlot) && epuipBaseBones.TryGetValue(plEquip.EquipSlot, out var bone))
            {
                var modelHeadTrans = GetPlRelatedModelAnimator(plEquip.OwnerPl).GetBoneTransform(bone);
                plEquip.transform.SetParent(modelHeadTrans, false);
                plEquip.SetLocalPosition(GetOffset(plEquip.EquipSlot, playername));
                return;
            }
            else
            {
                plEquip.TransSelf.SetParent(originalParentTransform[plEquip.OwnerPl.EquipCtrl], true);
                plEquip.TransSelf.localPosition += GetOffset(plEquip.EquipSlot, playername);
            }
        }

        // 矢筒は他の装備品と管理方法が違うので別途対応（やってることはほぼ同じ）
        static readonly Dictionary<OcPlCommon, Transform> quiverTransforms = new Dictionary<OcPlCommon, Transform>();
        static Vector3? quiverOffset = null;
        // static bool? isQuiverAdujstedToVrmModel = null;

        static Vector3 GetQuiverOffset(string playername = null)
        {
            if (quiverOffset.HasValue && CachingEnabled) return quiverOffset.Value;
            quiverOffset = Settings.ReadVector3(playername, "EquipArrowOffset", Vector3.zero);
            return quiverOffset.Value;
        }

        static bool IsQuiverAdujstedToVrmModel()
        {
            // ゲーム開始時にモデル追従の設定になっていると不具合が起きるので強制的に追従しない設定を反映させる。
            return false;
            //if (isQuiverAdujstedToVrmModel.HasValue && CachingEnabled) return isQuiverAdujstedToVrmModel.Value;
            //isQuiverAdujstedToVrmModel = Settings.ReadBool("EquipArrowFollowsModel", false);
            //return isQuiverAdujstedToVrmModel.Value;
        }

        static void AdjustQuiverPos(OcPl pl, OcPlCommon plCommon)
        {
            string playername = Settings.getPlayerName(pl);

            Transform quiver;
            if (quiverTransforms.TryGetValue(plCommon, out quiver) == false || quiver == null)
            {
                quiver = plCommon.AccessoryCtrl.transform.Find("OcQuiver");
                if (quiver == null) return;
                quiverTransforms.Add(plCommon, quiver);
            }

            if (IsQuiverAdujstedToVrmModel())
            {
                var modelHeadTrans = GetPlRelatedModelAnimator(pl).GetBoneTransform(HumanBodyBones.Spine);
                quiver.SetParent(modelHeadTrans, false);
                quiver.SetLocalPosition(GetQuiverOffset(playername));
                return;
            }
            else
            {
                quiver.SetParent(plCommon.AccessoryCtrl, true);
                quiver.localPosition += GetQuiverOffset(playername);
            }

        }

        static void Postfix(OcPl __instance)
        {
            if (!Settings.isUseVRM(__instance))
            {
                return;
            }

            string playername = Settings.getPlayerName(__instance);

            if (!Settings.ReadBool(playername, "UseEquipAdjustment", false)) return;

            var plEquipCtrl = __instance.EquipCtrl;
            var plCommon = __instance.PlCommon;
            var plEquips = GetPlEquips(plEquipCtrl);
            plEquips.RemoveWhere(plEquip => plEquip == null); // Destroyされていたら null チェックが True になる

            if (originalParentTransform.ContainsKey(plEquipCtrl) == false && plEquips.Any())
            {
                originalParentTransform.Add(plEquipCtrl, IEnumerableExtensions.First(plEquips).transform.parent);
            }

            foreach (var plEquip in plEquips)
            {
                AdjustEquipPos(plEquip, playername);
            }

            AdjustQuiverPos(__instance, __instance.PlCommon);
        }
    }


    [HarmonyPatch(typeof(OcPlEquipCtrl))]
    [HarmonyPatch(nameof(OcPlEquipCtrl.setEquip))]
    static class EquipAdjustPos_OcPlEquipCtrl_setEquip
    {
        static bool Prefix(OcPlEquipCtrl __instance, OcItem item, OcEquipSlot equipSlot, out OcEquipSlot __state)
        {
            __state = equipSlot;
            return true;
        }

        // 装備変更のタイミングで装備品リストを更新（追加）
        static void Postfix(OcPlEquipCtrl __instance, OcEquipSlot __state)
        {
            EquipAdjustPos_OcPlEquipCtrl_lateMove.GetPlEquips(__instance).Add(__instance.getEquip(__state));
        }

    }


    [HarmonyPatch(typeof(OcPlEquip))]
    [HarmonyPatch("setDraw")]
    static class OcPlEquipVRM
    {
        static bool Prefix(OcPlEquip __instance, ref bool isDraw)
        {
            OcPl pl = __instance.GetComponentInParentRecursive<OcPl>();
            if (!Settings.isUseVRM(pl)) return true;
            string playername = Settings.getPlayerName(pl);

            if (__instance.EquipSlot == OcEquipSlot.EqHead && !Settings.ReadBool(playername, "DrawEquipHead", true))
            {
                isDraw = false;
                return true;
            }

            if (__instance.EquipSlot == OcEquipSlot.Accessory && !Settings.ReadBool(playername, "DrawEquipAccessory", true))
            {
                isDraw = false;
                return true;
            }

            if (__instance.EquipSlot == OcEquipSlot.WpSub && !Settings.ReadBool(playername, "DrawEquipShield", true))
            {
                isDraw = false;
                return true;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(OcAccessoryCtrl))]
    [HarmonyPatch("setAf_DrawFlag")]
    static class OcAccessoryCtrlVRM
    {
        static bool Prefix(OcAccessoryCtrl __instance, OcAccessoryCtrl.AccType type)
        {
            if (type == OcAccessoryCtrl.AccType.Quiver)
            {
                return Settings.ReadBool("DrawEquipArrow");
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

            OcPl pl = go.GetComponentInParentRecursive<OcPl>();
            if (!Settings.isUseVRM(pl)) return;


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

    [HarmonyPatch(typeof(ShaderStore))]
    [HarmonyPatch("GetShader")]
    static class ShaderToRealToon
    {
        static bool Prefix(ShaderStore __instance, ref Shader __result, glTFMaterial material)
        {
            if (Settings.ReadBool("UseRealToonShader", false))
            {
                __result = Shader.Find("RealToon/Version 5/Default/Default");
                return false;
            }

            return true;
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
                    var max = baseColorFactor2.Max();
                    var rate = Mathf.Min(0.688f / max, 1.0f);
                    __result.SetColor("_MainColor", new Color(baseColorFactor2[0] * rate, baseColorFactor2[1] * rate, baseColorFactor2[2] * rate));
                }
                else if (x.pbrMetallicRoughness.baseColorFactor != null && x.pbrMetallicRoughness.baseColorFactor.Length == 4)
                {
                    float[] baseColorFactor2 = x.pbrMetallicRoughness.baseColorFactor;
                    var facotrs = new float[] { baseColorFactor2[0], baseColorFactor2[1], baseColorFactor2[2] };
                    var max = facotrs.Max();
                    var rate = Mathf.Min(0.688f / max, 1.0f);
                    __result.SetColor("_MainColor", new Color(baseColorFactor2[0] * rate, baseColorFactor2[1] * rate, baseColorFactor2[2] * rate, baseColorFactor2[3]));
                }
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

    [DefaultExecutionOrder(int.MaxValue - 100)]
    internal class CloneHumanoid : MonoBehaviour
    {
        HumanPoseHandler orgPose, vrmPose;
        HumanPose hp = new HumanPose();
        GameObject instancedModel;
        internal GameObject GetInstancedVRMModel() => instancedModel;
        VRMBlendShapeProxy blendProxy;
        Facial.FaceCtrl facialFace;

        public void Setup(GameObject vrmModel, Animator orgAnim, bool isMaster)
        {
            var instance = instancedModel ?? Instantiate(vrmModel);
            var useRealToon = Settings.ReadBool("UseRealToonShader", false);
            foreach (var sm in instance.GetComponentsInChildren<Renderer>())
            {
                sm.enabled = true;
                if (useRealToon)
                {
                    foreach (var mat in sm.materials)
                    {
                        mat.SetFloat("_EnableTextureTransparent", 1.0f);
                    }
                }
            }

            instance.transform.SetParent(orgAnim.transform, false);
            PoseHandlerCreate(orgAnim, instance.GetComponent<Animator>());
            if (instancedModel == null)
            {
                blendProxy = instance.GetComponent<VRMBlendShapeProxy>();
                if (isMaster && LipSync.OVRLipSyncVRM.IsUseLipSync)
                    AttachLipSync(instance);

                instance.GetOrAddComponent<Facial.EyeCtrl>();
                var useFacial = Settings.ReadBool("UseFacial", true);
                if (isMaster && useFacial)
                    facialFace = instance.GetOrAddComponent<Facial.FaceCtrl>();
                instancedModel = instance;
            }
        }

        void AttachLipSync(GameObject vrmModel)
        {
            var ovrInstance = LipSync.OVRLipSyncVRM.Instance;
            ovrInstance.OnBlend.Subscribe(v => ovrInstance.BlendFunc(v, blendProxy)).AddTo(vrmModel);
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
            instancedModel.transform.localPosition = Vector3.zero;
            instancedModel.transform.localRotation = Quaternion.identity;
            if (blendProxy)
                blendProxy.Apply();
        }
    }

    [HarmonyPatch(typeof(OcPl))]
    [HarmonyPatch("charaChangeSteup")]
    static class OcPlVRM
    {
        static Dictionary<string, GameObject> dic_vrmModel = new Dictionary<string, GameObject>();

        static void Postfix(OcPl __instance)
        {

            if (!Settings.isUseVRM(__instance)) return;

            string playername = Settings.getPlayerName(__instance);

            if (Settings.ReadBool("DisableStool", false)) SROptions.Current.DisableStool = true;

            GameObject _vrmModel = null;
            if (playername != null)
            {
                if (dic_vrmModel.ContainsKey(playername))
                {
                    _vrmModel = dic_vrmModel[playername];
                }
            }
            if (_vrmModel == null)
            {
                //カスタムモデル名の取得(設定ファイルにないためLogの出力が不自然にならないよう調整)
                var ModelStr = Settings.ReadSettings(playername, "ModelName");


                var path = Environment.CurrentDirectory + @"\Player2VRM\player.vrm";
                if (ModelStr != null)
                    path = Environment.CurrentDirectory + @"\Player2VRM\" + ModelStr + ".vrm";



                try
                {
                    _vrmModel = ImportVRM(path);
                }
                catch
                {
                    string _settings_path = Settings.FindAvatarSettngs(playername);

                    if (ModelStr != null)
                        UnityEngine.Debug.LogWarning("VRMファイルの読み込みに失敗しました。" + _settings_path + "内のModelNameを確認してください。");
                    else
                        UnityEngine.Debug.LogWarning("VRMファイルの読み込みに失敗しました。Player2VRMフォルダにplayer.vrmを配置してください。");
                    return;
                }

                var receiveShadows = Settings.ReadBool(playername, "ReceiveShadows");
                if (!receiveShadows)
                {
                    foreach (var smr in _vrmModel.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        smr.receiveShadows = false;
                    }
                }

                // プレイヤースケール調整
                {
                    var scale = Settings.ReadFloat(playername, "PlayerScale", 1.0f);
                    __instance.transform.localScale *= scale;
                    _vrmModel.transform.localScale /= scale;
                }
            }

            foreach (var smr in __instance.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (Settings.ReadBool(playername, "UseRealToonShader", false))
                {
                    foreach (var mat in smr.materials)
                    {
                        mat.SetFloat("_EnableTextureTransparent", 1.0f);
                    }
                }
                smr.enabled = false;
                Transform trans = smr.transform;
                while (_vrmModel != null && trans != null)
                {
                    if (trans.name.Contains(_vrmModel.name))
                    {
                        smr.enabled = true;
                        break;
                    }
                    trans = trans.parent;
                }
            }

            __instance.Animator.gameObject.GetOrAddComponent<CloneHumanoid>().Setup(_vrmModel, __instance.Animator, __instance is OcPlMaster);
            if (playername != null)
            {
                dic_vrmModel[playername] = _vrmModel;
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
            context.Root.transform.localScale *= Settings.ReadFloat("ModelScale", 1.0f);

            return context.Root;
        }
    }
}
