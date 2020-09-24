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
using System;
using UnityEngine;


[RequireComponent(typeof(AudioSource))]

//-------------------------------------------------------------------------------------
// ***** OVRPhonemeContext
//
/// <summary>
/// OVRPhonemeContext interfaces into the Oculus phoneme recognizer. 
/// This component should be added into the scene once for each Audio Source. 
///
/// </summary>
public class OVRLipSyncContext : OVRLipSyncContextBase
{
    // * * * * * * * * * * * * *
    // Public members
    public float gain = 1.0f;
    public bool delayCompensate = false;

    /// <summary>
    /// Raises the audio filter read event.
    /// </summary>
    /// <param name="data">Data.</param>
    /// <param name="channels">Channels.</param>
    public void OnAudioFilter(float[] data, int channels)
    {
        // Do not spatialize if we are not initialized, or if there is no
        // audio source attached to game object
        if ((OVRLipSync.IsInitialized() != OVRLipSync.Result.Success) || audioSource == null)
            return;

        // increase the gain of the input to get a better signal input
        for (int i = 0; i < data.Length; ++i)
            data[i] = data[i] * gain;

        // Send data into Phoneme context for processing (if context is not 0)
        lock (this)
        {
            if (Context != 0)
            {
                var flags = OVRLipSync.Flags.None;

                // Set flags to feed into process
                if (delayCompensate == true)
                    flags |= OVRLipSync.Flags.DelayCompensateAudio;

                OVRLipSync.ProcessFrameInterleaved(Context, data, flags, Frame);
            }
        }
        Array.Clear(data, 0, data.Length);
    }
}
