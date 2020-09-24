#define UNUSE_FOCUS
/************************************************************************************
Filename    :   OVRLipSyncMicInput.cs
Content     :   Interface to microphone input
Created     :   May 12, 2015
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
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]

public class OVRLipSyncMicInput : MonoBehaviour
{
    public enum MicActivation
    {
        HoldToSpeak,
        PushToSpeak,
        ConstantSpeak
    }

    // PUBLIC MEMBERS
    public AudioSource audioSource = null;
    public bool GuiSelectDevice = true;

    public int micIndex = 0;

    public string inputDefaultDeviceName = string.Empty;

    private float sensitivity = 100;
    public float Sensitivity
    {
        get { return sensitivity; }
        set { sensitivity = Mathf.Clamp(value, 0, 100); }
    }

    private float sourceVolume = 100;
    public float SourceVolume
    {
        get { return sourceVolume; }
        set { sourceVolume = Mathf.Clamp(value, 0, 100); }
    }

    private int micFrequency = 16000;
    public float MicFrequency
    {
        get { return micFrequency; }
        set { micFrequency = (int)Mathf.Clamp((float)value, 0, 96000); }
    }


    public MicActivation micControl;

    public string selectedDevice;

    public float loudness; // Use this to chenge visual values. Range is 0 - 100

    // PRIVATE MEMBERS
    private bool micSelected = false;
    private int minFreq, maxFreq;

    //----------------------------------------------------
    // MONOBEHAVIOUR OVERRIDE FUNCTIONS
    //----------------------------------------------------

    /// <summary>
    /// Awake this instance.
    /// </summary>
    void Awake()
    {
        // First thing to do, cache the unity audio source (can be managed by the
        // user if audio source can change)
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (!audioSource) return; // this should never happen
    }

    /// <summary>
    /// Start this instance.
    /// </summary>
    void Start()
    {
        audioSource.loop = true;    // Set the AudioClip to loop
        audioSource.mute = false;
        micControl = MicActivation.ConstantSpeak;

        var devices = new List<string>();
        for (int i = 0; i < Microphone.devices.Length; i++)
        {
            if (Microphone.devices[i] == inputDefaultDeviceName)
                micIndex = i;
        }

        if (Microphone.devices.Length > 0)
        {
            selectedDevice = Microphone.devices[micIndex];
            micSelected = true;
            GetMicCaps();
        }
        StartMicrophone();
    }

    private void OnEnable()
    {
        StartMicrophone();
    }

    void OnDisable()
    {
        StopMicrophone();
    }

    /// <summary>
    /// Gets the mic caps.
    /// </summary>
    public void GetMicCaps()
    {
        if (micSelected == false) return;

        //Gets the frequency of the device
        Microphone.GetDeviceCaps(selectedDevice, out minFreq, out maxFreq);

        if (minFreq == 0 && maxFreq == 0)
        {
            Debug.LogWarning("GetMicCaps warning:: min and max frequencies are 0");
            minFreq = 44100;
            maxFreq = 44100;
        }

        if (micFrequency > maxFreq)
            micFrequency = maxFreq;
    }

    /// <summary>
    /// Starts the microphone.
    /// </summary>
    public void StartMicrophone()
    {
        if (micSelected == false) return;

        Debug.Log($"MicStart:{selectedDevice}");

        //Starts recording
        try
        {
            audioSource.clip = Microphone.Start(selectedDevice, true, 1, micFrequency);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            return;
        }

        // Wait until the recording has started
        while (!(Microphone.GetPosition(selectedDevice) > 0)) { }

        // Play the audio source
        audioSource.Play();
    }

    /// <summary>
    /// Stops the microphone.
    /// </summary>
    public void StopMicrophone()
    {
        if (micSelected == false) return;

        // Overriden with a clip to play? Don't stop the audio source
        if ((audioSource != null) && (audioSource.clip != null) && (audioSource.clip.name == "Microphone"))
            audioSource.Stop();

        Microphone.End(selectedDevice);
    }
}