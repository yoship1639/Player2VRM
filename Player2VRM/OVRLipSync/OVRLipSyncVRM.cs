using HarmonyLib;
using Oc;
using SRF;
using System;
using System.IO;
using System.Linq;
using UniRx;
using UnityEngine;
using VRM;
using Debug = UnityEngine.Debug;

namespace Player2VRM.LipSync
{
    [DefaultExecutionOrder(int.MaxValue - 99)]
    public class OVRLipSyncVRM : MonoBehaviour
    {
        public static bool IsUseLipSync { get; private set; }
        public static OVRLipSyncVRM Instance { get; private set; }

        public IObservable<OVRLipSync.Frame> OnBlend => subjectOnBlend;

        OVRLipSyncContextBase lipsyncContext = null;
        Action<OVRLipSync.Frame, VRMBlendShapeProxy> onBlendShape;
        readonly Subject<OVRLipSync.Frame> subjectOnBlend = new Subject<OVRLipSync.Frame>();
        OVRLipSyncMicInput micInput;
        readonly int smoothAmount = 100;

        public static OVRLipSyncVRM Setup(Action<OVRLipSync.Frame, VRMBlendShapeProxy> customBlendFunc = null)
        {
            IsUseLipSync = Settings.ReadBool("UseLipSync", false);
            var micDeviceIndex = Settings.ReadInt("LipSyncMicIndex", -1);
            var micGain = Settings.ReadFloat("LipSyncMicGain", 3);
            if (!IsUseLipSync)
                return null;

            var dllPath = $"{Environment.CurrentDirectory}/Craftopia_Data/Managed/OVRLipSync.dll";
            if (File.Exists(dllPath) == false)
            {
                Debug.LogError($"OVRLipSync.dll が正しい位置にインストールされていません\n{dllPath} に配置してください");
                IsUseLipSync = false;
                return null;
            }

            if (micDeviceIndex < 0 || micDeviceIndex >= Microphone.devices.Length)
            {
                micDeviceIndex = 0;
                Debug.Log($"MicIndexes\n{string.Join("\n", Microphone.devices.Select((n, i) => $" {i}:{n}"))}");
            }
            Debug.Log($"UseLipSync Setup(MicIndex:{micDeviceIndex} MicGain:{micGain})");

            var engine = new GameObject();
            DontDestroyOnLoad(engine);
            engine.AddComponent<OVRLipSync>();
            engine.AddComponent<AudioSource>();
            var micInput = engine.AddComponent<OVRLipSyncMicInput>();
            micInput.micIndex = micDeviceIndex;
            micInput.enabled = false;
            engine.AddComponent<OVRLipSyncContext>().gain = micGain;
            Instance = engine.AddComponent<OVRLipSyncVRM>();
            Instance.micInput = micInput;
            Instance.onBlendShape = customBlendFunc ?? OnDefaultBlend;
            return Instance;
        }

        void Start()
        {
            lipsyncContext = GetComponent<OVRLipSyncContextBase>();
            if (lipsyncContext == null)
                Debug.Log("LipSyncContextMorphTarget.Start WARNING: No phoneme context component set to object");
            lipsyncContext.Smoothing = smoothAmount;
        }

        void LateUpdate()
        {
            if (lipsyncContext != null)
            {
                var frame = lipsyncContext.GetCurrentPhonemeFrame();
                if (frame != null)
                    subjectOnBlend.OnNext(frame);
            }
        }

        static readonly BlendShapePreset[] visemeToBlendTargets = new BlendShapePreset[]
        {
            BlendShapePreset.Neutral,
            BlendShapePreset.A,
            BlendShapePreset.E,
            BlendShapePreset.I,
            BlendShapePreset.O,
            BlendShapePreset.U,
        };

        public void BlendFunc(OVRLipSync.Frame frame, VRMBlendShapeProxy vrmBlendShapeProxy)
        {
            onBlendShape(frame, vrmBlendShapeProxy);
            vrmBlendShapeProxy.Apply();
        }

        static void OnDefaultBlend(OVRLipSync.Frame frame, VRMBlendShapeProxy vrmBlendShapeProxy)
        {
            var visemes = new float[visemeToBlendTargets.Length];
            Array.Copy(frame.Visemes, (int)OVRLipSync.Viseme.aa, visemes, 1, visemes.Length - 1);
            visemes[0] = frame.Visemes[(int)OVRLipSync.Viseme.sil];
            var sum = visemes.Sum();

            if (sum <= float.Epsilon)
            {
                visemes[0] = 1;
            }
            else
            {
                for (int i = 0; i < visemes.Length; ++i)
                    visemes[i] /= sum;
            }
            for (int i = 0; i < visemes.Length; ++i)
                vrmBlendShapeProxy.AccumulateValue(BlendShapeKey.CreateFromPreset(visemeToBlendTargets[i]), visemes[i]);
        }

        void OnEnable()
        {
            if (micInput == null)
                return;
            micInput.enabled = true;
            micInput.gameObject.GetComponentOrAdd<OVRLipSyncVRM_AudioSource>();
        }

        void OnDisable()
        {
            if (micInput == null)
                return;
            micInput.enabled = false;
            var src = micInput.gameObject.GetComponent<OVRLipSyncVRM_AudioSource>();
            if (src != null)
                Destroy(src);
        }
    }

    public class OVRLipSyncVRM_AudioSource : MonoBehaviour
    {
        OVRLipSyncContext context;

        void Awake()
        {
            context = GetComponent<OVRLipSyncContext>();
        }

        public void OnAudioFilterRead(float[] data, int channels)
        {
            context.OnAudioFilter(data, channels);
        }
    }

    [HarmonyPatch(typeof(OcGameSceneTransformManager), "Update")]
    static class OcGameSceneTransformManager_IsLoadig
    {
        static void Postfix(ref bool ____loading)
        {
            if (OVRLipSyncVRM.IsUseLipSync && OVRLipSyncVRM.Instance.enabled != !____loading)
                OVRLipSyncVRM.Instance.enabled = !____loading;
        }
    }
}
