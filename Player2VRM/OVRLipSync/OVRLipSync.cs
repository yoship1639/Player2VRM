/************************************************************************************
Filename    :   OVRLipSync.cs
Content     :   Interface to Oculus Lip-Sync engine
Created     :   August 4th, 2015
Copyright   :   Copyright 2015 Oculus VR, Inc. All Rights reserved.

Licensed under the Oculus VR Rift SDK License Version 3.1 (the "License"); 
you may not use the Oculus VR Rift SDK except in compliance with the License, 
which is provided at the time of installation or download, or which 
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

http://www.oculusvr.com/licenses/LICENSE-3.1 

Unless required by applicable law or agreed to in writing, the Oculus VR SDK 
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
************************************************************************************/
using UnityEngine;
using System;
using System.Runtime.InteropServices;

//-------------------------------------------------------------------------------------
// ***** OVRPhoneme
//
/// <summary>
/// OVRLipSync interfaces into the Oculus lip-sync engine. This component should be added
/// into the scene once. 
///
/// </summary>
public class OVRLipSync : MonoBehaviour
{
    // Error codes that may return from Lip-Sync engine
    public enum Result
    {
        Success = 0,
        Unknown = -2200,	//< An unknown error has occurred
        CannotCreateContext = -2201, 	//< Unable to create a context
        InvalidParam = -2202,	//< An invalid parameter, e.g. NULL pointer or out of range
        BadSampleRate = -2203,	//< An unsupported sample rate was declared
        MissingDLL = -2204,	//< The DLL or shared library could not be found
        BadVersion = -2205,	//< Mismatched versions between header and libs
        UndefinedFunction = -2206	//< An undefined function 
    };

    // Various visemes
    public enum Viseme
    {
        sil,
        PP,
        FF,
        TH,
        DD,
        kk,
        CH,
        SS,
        nn,
        RR,
        aa,
        E,
        ih,
        oh,
        ou
    };

    public static OVRLipSync Instance { get; private set; }

    public static readonly int VisemeCount = Enum.GetNames(typeof(Viseme)).Length;

    /// Flags
    public enum Flags
    {
        None = 0x0000,
        DelayCompensateAudio = 0x0001

    };

    // Enum for sending lip-sync engine specific signals
    public enum Signals
    {
        VisemeOn,
        VisemeOff,
        VisemeAmount,
        VisemeSmoothing
    };

    public static readonly int SignalCount = Enum.GetNames(typeof(Signals)).Length;

    // Enum for provider context to create
    public enum ContextProviders
    {
        Main,
        Other
    };

    /// NOTE: Opaque typedef for lip-sync context is an unsigned int (uint)

    /// Current phoneme frame results
    [System.Serializable]
    public class Frame
    {
        public void CopyInput(Frame input)
        {
            frameNumber = input.frameNumber;
            frameDelay = input.frameDelay;
            input.Visemes.CopyTo(Visemes, 0);
        }

        public int frameNumber; 	// count from start of recognition
        public int frameDelay;  	// in ms
        public float[] Visemes = new float[VisemeCount];		// Array of floats for viseme frame. Size of Viseme Count, above
    };

    // * * * * * * * * * * * * *
    // Import functions
    public const string strOVRLS = "../../Managed/OVRLipSync";
    [DllImport(strOVRLS)]
    private static extern int ovrLipSyncDll_Initialize(int samplerate, int buffersize);
    [DllImport(strOVRLS)]
    private static extern void ovrLipSyncDll_Shutdown();
    [DllImport(strOVRLS)]
    private static extern IntPtr ovrLipSyncDll_GetVersion(ref int Major,
                                                          ref int Minor,
                                                          ref int Patch);
    [DllImport(strOVRLS)]
    private static extern int ovrLipSyncDll_CreateContext(ref uint context,
                                                           ContextProviders provider);
    [DllImport(strOVRLS)]
    private static extern int ovrLipSyncDll_DestroyContext(uint context);


    [DllImport(strOVRLS)]
    private static extern int ovrLipSyncDll_ResetContext(uint context);
    [DllImport(strOVRLS)]
    private static extern int ovrLipSyncDll_SendSignal(uint context,
                                                       Signals signal,
                                                       int arg1, int arg2);
    [DllImport(strOVRLS)]
    private static extern int ovrLipSyncDll_ProcessFrame(uint context,
                                                         float[] audioBuffer, Flags flags,
                                                         ref int frameNumber, ref int frameDelay,
                                                         float[] visemes, int visemeCount);
    [DllImport(strOVRLS)]
    private static extern int ovrLipSyncDll_ProcessFrameInterleaved(uint context,
                                                         float[] audioBuffer, Flags flags,
                                                         ref int frameNumber, ref int frameDelay,
                                                         float[] visemes, int visemeCount);

    // * * * * * * * * * * * * *
    // Public members

    // * * * * * * * * * * * * *
    // Static members
    private static Result sInitialized = Result.Unknown;

    // interface through this static member.
    public static OVRLipSync sInstance = null;


    // * * * * * * * * * * * * *
    // MonoBehaviour overrides

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }
    /// <summary>
    /// Awake this instance.
    /// </summary>
    public void ForceAwake()
    {
        // We can only have one instance of OVRLipSync in a scene (use this for local property query)
        if (sInstance == null)
        {
            sInstance = this;
        }
        else
        {
            Debug.LogWarning("OVRLipSync Awake: Only one instance of OVRPLipSync can exist in the scene.");
            return;
        }

        var samplerate = AudioSettings.outputSampleRate;
        AudioSettings.GetDSPBufferSize(out int bufsize, out int numbuf);
        sInitialized = Initialize(samplerate, bufsize);

        if (sInitialized != Result.Success)
            Debug.LogWarning("OvrLipSync Awake: Failed to init Speech Rec library");
    }

    /// <summary>
    /// Raises the destroy event.
    /// </summary>
    void OnDestroy()
    {
        if (sInstance != this)
        {
            Debug.LogWarning(
            "OVRLipSync OnDestroy: This is not the correct OVRLipSync instance.");
            return;
        }

        // Do not shut down at this time
        //		ovrLipSyncDll_Shutdown();
        //		sInitialized = (int)Result.Unknown;
    }


    // * * * * * * * * * * * * *
    // Public Functions

    public static Result Initialize(int sampleRate, int bufferSize)
    {
        sInitialized = (Result)ovrLipSyncDll_Initialize(sampleRate, bufferSize);
        return sInitialized;
    }

    public static void Shutdown()
    {
        ovrLipSyncDll_Shutdown();
        sInitialized = Result.Unknown;
    }

    /// <summary>
    /// Determines if is initialized.
    /// </summary>
    /// <returns><c>true</c> if is initialized; otherwise, <c>false</c>.</returns>
    public static Result IsInitialized()
    {
        return sInitialized;
    }

    /// <summary>
    /// Creates a lip-sync context.
    /// </summary>
    /// <returns>error code</returns>
    /// <param name="context">Context.</param>
    /// <param name="provider">Provider.</param>
    public static Result CreateContext(ref uint context, ContextProviders provider)
    {
        if (IsInitialized() != Result.Success)
            return Result.CannotCreateContext;

        return (Result)ovrLipSyncDll_CreateContext(ref context, provider);
    }

    /// <summary>
    /// Destroy a lip-sync context.
    /// </summary>
    /// <returns>The context.</returns>
    /// <param name="context">Context.</param>
    public static Result DestroyContext(uint context)
    {
        if (IsInitialized() != Result.Success)
            return Result.Unknown;

        return (Result)ovrLipSyncDll_DestroyContext(context);
    }

    /// <summary>
    /// Resets the context.
    /// </summary>
    /// <returns>error code</returns>
    /// <param name="context">Context.</param>
    public static Result ResetContext(uint context)
    {
        if (IsInitialized() != Result.Success)
            return Result.Unknown;

        return (Result)ovrLipSyncDll_ResetContext(context);
    }

    /// <summary>
    /// Sends a signal to the lip-sync engine.
    /// </summary>
    /// <returns>error code</returns>
    /// <param name="context">Context.</param>
    /// <param name="signal">Signal.</param>
    /// <param name="arg1">Arg1.</param>
    /// <param name="arg2">Arg2.</param>
    public static Result SendSignal(uint context, Signals signal, int arg1, int arg2)
    {
        if (IsInitialized() != Result.Success)
            return Result.Unknown;

        return (Result)ovrLipSyncDll_SendSignal(context, signal, arg1, arg2);
    }

    /// <summary>
    /// Processes the frame.
    /// </summary>
    /// <returns>error code</returns>
    /// <param name="context">Context.</param>
    /// <param name="monoBuffer">Mono buffer.</param>
    /// <param name="delayCompensate">If set to <c>true</c> delay compensate.</param>
    /// <param name="frame">Frame.</param>
    public static Result ProcessFrame(uint context, float[] audioBuffer, Flags flags, Frame frame)
    {
        if (IsInitialized() != Result.Success)
            return Result.Unknown;

        // We need to pass the array of Visemes directly into the C call (no pointers of structs allowed, sadly)
        return (Result)ovrLipSyncDll_ProcessFrame(context, audioBuffer, flags,
                                          ref frame.frameNumber, ref frame.frameDelay,
                                          frame.Visemes, frame.Visemes.Length);
    }

    /// <summary>
    /// Processes the frame interleaved.
    /// </summary>
    /// <returns>The frame interleaved.</returns>
    /// <param name="context">Context.</param>
    /// <param name="audioBuffer">Audio buffer.</param>
    /// <param name="delayCompensate">If set to <c>true</c> delay compensate.</param>
    /// <param name="frame">Frame.</param>
    public static Result ProcessFrameInterleaved(uint context, float[] audioBuffer, Flags flags, Frame frame)
    {
        if (IsInitialized() != Result.Success)
            return Result.Unknown;

        // We need to pass the array of Visemes directly into the C call (no pointers of structs allowed, sadly)
        return (Result)ovrLipSyncDll_ProcessFrameInterleaved(context, audioBuffer, flags,
                                          ref frame.frameNumber, ref frame.frameDelay,
                                          frame.Visemes, frame.Visemes.Length);
    }
}
