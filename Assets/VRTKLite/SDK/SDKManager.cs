using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRTKLite.SDK
{
    public class SDKManager : MonoBehaviour
    {
        /// <summary>
        /// Assigned In Unity Editor and contains all Camera Rigs for each VR ecosystem.
        /// </summary>
        [Tooltip("The list of SDK Setups to choose from.")]
        public GameObject[] Setups = Array.Empty<GameObject>();

        /// <summary>
        /// DeviceModel to be confirmed
        /// </summary>
        static readonly HashSet<string> OpenVR_HMDs = new()
        {
            "vivemv", "vivedvt", "vive_promv", "index"
        };

        /// <summary>
        /// DeviceModel to be confirmed
        /// </summary>
        static readonly HashSet<string> OVR_HMDs = new()
        {
            "oculusriftcv1", "oculusriftes07", "oculusrifts"
        };

        /// <summary>
        /// These are known to be the device models for WaveVR headsets
        /// </summary>
        static readonly HashSet<string> WaveVR_HMDs = new()
        {
            "HTC Vive Focus", "HTC Vive Focus Plus"
        };

        /// <summary>
        /// DeviceModel to be confirmed
        /// </summary>
        static readonly HashSet<string> WindowsMR_HMDs = new()
        {
            "acerah100", "lenovoexplorer", "hpwindowsmixedrealityheadset", "samsungwindowsmixedreality800ZAA"
        };

        public delegate void LoadedVRSetupChangeEventHandler(GameObject newSetup);

        GameObject currentSetup;

        LoadedVRSetupChangeEventHandler loadedVRSetupChanged;

        public event LoadedVRSetupChangeEventHandler LoadedVRSetupChanged
        {
            add
            {
                loadedVRSetupChanged += value;
                // The newly-subscribed listener needs to "catch up" to the
                // currently loaded setup, if it exists
                if (currentSetup != null)
                {
                    value.Invoke(currentSetup);
                }
            }
            remove => loadedVRSetupChanged -= value;
        }

        void Start()
        {
            // iterate through setups
            List<XRInputSubsystem> xrInputSubsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(xrInputSubsystems);

            foreach (XRInputSubsystem xrInputSubsystem in xrInputSubsystems)
            {
                xrInputSubsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
            }

            List<XRDisplaySubsystem> xrDisplaySubsystems = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(xrDisplaySubsystems);

            foreach (XRDisplaySubsystem xrDisplay in xrDisplaySubsystems)
            {
                if (xrDisplay.SubsystemDescriptor.id == "OpenXR Display")
                {
                    // special case...for some reason xrDisplay.running is false. this might change in a future release
                    LoadHeadsetFromName(xrDisplay.SubsystemDescriptor.id);
                    return;
                }

                if (xrDisplay.running &&
                    LoadHeadsetFromName(xrDisplay.SubsystemDescriptor.id))
                {
                    return;
                }
            }

            // fallback enable the 2D camera
            foreach (GameObject sdkSetup in Setups)
            {
                if (sdkSetup.name == "Simulator")
                {
                    EnableAssociatedSDKSetup(sdkSetup);
                    return;
                }
            }
        }

        bool LoadHeadsetFromName(string headsetName)
        {
            if (!IsValidXR(headsetName)) return false;

            // go through each setup and enable the one that matches the VR SDK from the name
            foreach (GameObject sdkSetup in Setups)
            {
                if (sdkSetup.name == "GenericXR")
                {
                    EnableAssociatedSDKSetup(sdkSetup);
                    return true;
                }
            }

            return false;
        }

        void EnableAssociatedSDKSetup(GameObject setup)
        {
            // If there was a previous setup, we should disable its game object.
            if (currentSetup != null)
            {
                currentSetup.gameObject.SetActive(false);
            }

            currentSetup = setup;

            // The game object associated with the SDK contains the camera rig and the controllers.
            // Enable it so those objects can be used.
            setup.gameObject.SetActive(true);

            loadedVRSetupChanged?.Invoke(currentSetup);
        }

        static bool IsValidXR(string headsetType)
        {
            Debug.Log(headsetType);
            switch (headsetType)
            {
                case "oculus display":
                case "OpenXR Display":
                // TODO confirm this
                case "WindowsMR Display":
                case "windowsmr display":
                case "wmr display":
                case "WVR Display Provider":
                    return true;
            }

            return false;
        }
    }
}