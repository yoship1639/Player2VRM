/************************************************************************************
Filename    :   OVRLipSyncContext.cs
Content     :   Interface to Oculus Lip-Sync engine
Created     :   August 6th, 2015
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


[RequireComponent(typeof(AudioSource))]

//-------------------------------------------------------------------------------------
// ***** OVRLipSyncContextBase
//
/// <summary>
/// OVRLipSyncContextBase interfaces into the Oculus phoneme recognizer. 
/// This component should be added into the scene once for each Audio Source. 
///
/// </summary>
public class OVRLipSyncContextBase : MonoBehaviour
{
    // * * * * * * * * * * * * *
    // Public members
    public AudioSource audioSource = null;

    public OVRLipSync.ContextProviders provider = OVRLipSync.ContextProviders.Main;

    // * * * * * * * * * * * * *
    // Private members
    private OVRLipSync.Frame frame = new OVRLipSync.Frame();
    private uint context = 0;   // 0 is no context

    public int Smoothing
    {
        set
        {
            OVRLipSync.SendSignal(context, OVRLipSync.Signals.VisemeSmoothing, value, 0);
        }
    }

    public uint Context
    {
        get
        {
            return context;
        }
    }

    protected OVRLipSync.Frame Frame
    {
        get
        {
            return frame;
        }
    }

    /// <summary>
    /// Awake this instance.
    /// </summary>
    void Awake()
    {
        // Cache the audio source we are going to be using to pump data to the SR
        if (!audioSource)
        {
            audioSource = GetComponent<AudioSource>();
        }

        lock (this)
        {
            if (context == 0)
            {
                engine.ForceAwake();
                if (OVRLipSync.CreateContext(ref context, provider) != OVRLipSync.Result.Success)
                {
                    Debug.Log("OVRPhonemeContext.Start ERROR: Could not create Phoneme context.");
                    return;
                }
            }
        }
    }

    public OVRLipSync engine => OVRLipSync.Instance;

    /// <summary>
    /// Raises the destroy event.
    /// </summary>
    void OnDestroy()
    {
        // Create the context that we will feed into the audio buffer
        lock (this)
        {
            if (context != 0)
            {
                if (OVRLipSync.DestroyContext(context) != OVRLipSync.Result.Success)
                {
                    Debug.Log("OVRPhonemeContext.OnDestroy ERROR: Could not delete Phoneme context.");
                }
            }
        }
    }

    // * * * * * * * * * * * * *
    // Public Functions

    /// <summary>
    /// Gets the current phoneme frame (lock and copy current frame to caller frame)
    /// </summary>
    /// <returns>error code</returns>
    /// <param name="inFrame">In frame.</param>
    public OVRLipSync.Frame GetCurrentPhonemeFrame()
    {
        return frame;
    }

    public void SetVisemeBlend(int viseme, int amount)
    {
        OVRLipSync.SendSignal(context, OVRLipSync.Signals.VisemeAmount, viseme, amount);
    }

    /// <summary>
    /// Resets the context.
    /// </summary>
    /// <returns>error code</returns>
    public OVRLipSync.Result ResetContext()
    {
        return OVRLipSync.ResetContext(context);
    }
}