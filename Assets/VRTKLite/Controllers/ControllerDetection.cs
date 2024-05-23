using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace VRTKLite.Controllers
{
    public class ControllerDetection : MonoBehaviour
    {
        [Header("Controller Events Assignment")]
        public ControllerEvents RightControllerEvents, LeftControllerEvents;

        bool leftLoaded, rightLoaded;

        Material controllerMaterial;

        public bool RightModelActive => RightModel != null && RightModel.activeInHierarchy;
        public bool LeftModelActive => LeftModel != null && LeftModel.activeInHierarchy;

        //Model objects
        GameObject LeftModel, RightModel;

        //Toggle model events
        public event Action LeftModelOn;
        public event Action RightModelOn;

        public event Action LeftModelOff;
        public event Action RightModelOff;

        readonly HashSet<string> touchpadControllers = new()
        {
            "vive", "chirp", "mi6","mia", "aspen", "oculusgo"
        };

        void Awake()
        {
            InputDevices.deviceConnected += OnInputDeviceConnected;
            InputDevices.deviceDisconnected += OnInputDeviceDisconnected;
            LeftControllerEvents.gameObject.SetActive(true);
            RightControllerEvents.gameObject.SetActive(true);
        }

        void OnEnable()
        {
            List<InputDevice> leftDevices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left, leftDevices);

            foreach (InputDevice leftDevice in leftDevices)
            {
                LoadLeft(leftDevice);
            }

            List<InputDevice> rightDevices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right, rightDevices);

            foreach (InputDevice rightDevice in rightDevices)
            {
                LoadRight(rightDevice);
            }
        }

        void OnInputDeviceConnected(InputDevice inputDevice)
        {
            if ((inputDevice.characteristics & InputDeviceCharacteristics.Left) != 0)
            {
                LoadLeft(inputDevice);
            }
            else if ((inputDevice.characteristics & InputDeviceCharacteristics.Right) != 0)
            {
                LoadRight(inputDevice);
            }
        }
        
        void OnInputDeviceDisconnected(InputDevice inputDevice)
        {
            if ((inputDevice.characteristics & InputDeviceCharacteristics.Left) != 0)
            {
                LeftModel.SetActive(false);
                LeftModelOff?.Invoke();
            }
            else if ((inputDevice.characteristics & InputDeviceCharacteristics.Right) != 0)
            {
                RightModel.SetActive(false);
                RightModelOff?.Invoke();
            }
        }

        void LoadLeft(InputDevice inputDevice)
        {
            if (leftLoaded)
            {
                LeftModel.SetActive(true);
                LeftModelOn?.Invoke();
                return;
            }

            LeftModel = LoadControllerModel(inputDevice, LeftControllerEvents.transform, false);
            LeftControllerEvents.AssignInputDevice(inputDevice, false, LeftModel);
            
            if (IsTouchpadDevice(inputDevice.name.ToLowerInvariant()))
            {
                LeftControllerEvents.NoJoystick = true;
            }

            leftLoaded = true;
        }

        void LoadRight(InputDevice inputDevice)
        {
            if (rightLoaded)
            {
                RightModel.SetActive(true);
                RightModelOn?.Invoke();
                return;
            }

            RightModel = LoadControllerModel(inputDevice, RightControllerEvents.transform, true);
            RightControllerEvents.AssignInputDevice(inputDevice, true, RightModel);
            
            if (IsTouchpadDevice(inputDevice.name.ToLowerInvariant()))
            {
                RightControllerEvents.NoJoystick = true;
            }

            rightLoaded = true;
        }

        bool IsTouchpadDevice(string deviceName)
        {
            return touchpadControllers.Any(deviceName.Contains);
        }

        static GameObject LoadControllerModel(InputDevice inputDevice, Transform mount, bool isRight)
        {
            string deviceName = inputDevice.name.ToLowerInvariant();
            GameObject ctrGameObject = null;
            if (deviceName.Contains("quest") || 
                deviceName.Contains("rifts") ||
                deviceName.Contains("oculus touch controller openxr"))
            {
                string hand = isRight ? "Right" : "Left";
                Material ctrMat =
                    Resources.Load<Material>(
                        $"Meshes/OculusTouchForQuestAndRiftS/Materials/OculusTouchForQuestAndRiftS_Material");
                ctrGameObject = ControllerModel(
                    $"Meshes/OculusTouchForQuestAndRiftS/OculusTouchForQuestAndRiftS_{hand}",
                    ctrMat);

                ctrGameObject.transform.SetParent(mount.transform, false);

                if (deviceName.Contains("oculus touch controller openxr"))
                {
                    ctrGameObject.transform.Rotate(Vector3.right * 90f);
                }
            }
            else if (deviceName.Contains("touch"))
            {
                string hand = isRight ? "right" : "left";
                Material ctrMat =
                    Resources.Load<Material>($"Meshes/OculusTouchForRift/Materials/OculusTouchForRift_Material");
                ctrGameObject = TouchControllerModelAndChildren(
                    $"Meshes/OculusTouchForRift/{hand}_touch_controller_model_skel");
                ctrGameObject.transform.SetParent(mount.transform, false);
            }
            else if (deviceName.Contains("oculusgo"))
            {
                Material ctrMat = Resources.Load<Material>($"Meshes/OculusGoController/Materials/OculusGoController");
                ctrGameObject = ControllerModel(
                    $"Meshes/OculusGoController/OculusGoController",
                    ctrMat);
                ctrGameObject.transform.SetParent(mount.transform, false);
            }
            else if (deviceName.Contains("cosmos"))
            {
                string hand = isRight ? "right" : "left";
                Material ctrMat = Resources.Load<Material>($"Meshes/ViveCosmos/cosmos_{hand}_material");
                ctrGameObject = ControllerModel(
                    $"Meshes/ViveCosmos/vive_cosmos_controller_{hand}_v1",
                    ctrMat);
                ctrGameObject.transform.SetParent(mount.transform, false);
                ctrGameObject.transform.Rotate(Vector3.up, 180);
            }
            else if (deviceName.Contains("vive"))
            {
                Material ctrMat = Resources.Load<Material>($"Meshes/vr_controller_vive_1_5/ViveWandMat");
                ctrGameObject = ControllerModel(
                    $"Meshes/vr_controller_vive_1_5/vr_controller_vive_1_5",
                    ctrMat);
                ctrGameObject.transform.SetParent(mount.transform, false);
                ctrGameObject.transform.Rotate(Vector3.up, 180);
            }
            else if (deviceName.Contains("knuckles") || deviceName.Contains("index"))
            {
                string hand = isRight ? "right" : "left";
                Material ctrMat = Resources.Load<Material>($"Meshes/Knuckles/{hand}_material");
                ctrGameObject = ControllerModel(
                    $"Meshes/Knuckles/valve_controller_knu_1_0_{hand}",
                    ctrMat);
                ctrGameObject.transform.SetParent(mount.transform, false);
                ctrGameObject.transform.Rotate(Vector3.up, 180);
                ctrGameObject.transform.Translate(Vector3.back * 0.1f, Space.Self);
            }
            else if (deviceName.Contains("chirp"))
            {
                string hand = isRight ? "R" : "L";
                ctrGameObject = Instantiate(
                    Resources.Load(
                        $"Meshes/Wave/WVR_CONTROLLER_ASPEN_XA_XB/Resources/Controller/WVR_CONTROLLER_ASPEN_XA_XB_MC_{hand}") as GameObject);
                ctrGameObject.transform.SetParent(mount.transform, false);
            }
            else if (deviceName.Contains("mi6") || deviceName.Contains("aspen"))
            {
                string hand = isRight ? "R" : "L";
                ctrGameObject = Instantiate(
                    Resources.Load(
                        $"Meshes/Wave/WVR_CONTROLLER_ASPEN_MI6_1/Resources/Controller/WVR_CONTROLLER_ASPEN_MI6_1_MC_{hand}") as GameObject);
                ctrGameObject.transform.SetParent(mount.transform, false);
            }
            else if (deviceName.Contains("mia"))
            {
                string hand = isRight ? "R" : "L";
                ctrGameObject = Instantiate(
                    Resources.Load(
                        $"Meshes/Wave/Finch/Resources/Controller/Generic_MC_{hand}") as GameObject);
                ctrGameObject.transform.SetParent(mount.transform, false);
            }
            else if (deviceName.Contains("wmr") || deviceName.Contains("windows"))
            {
                string hand = isRight ? "Right" : "Left";
                // there is also a "C" model
                ctrGameObject = ControllerModelAndChildren(
                    $"Meshes/WMR/A/{hand}HandModel",
                    null);
                ctrGameObject.transform.SetParent(mount.transform, false);
            }

            return ctrGameObject;
        }

        static GameObject ControllerModel(string path, Material mat)
        {
            Mesh ctrMesh = Resources.Load<Mesh>(path);
            GameObject ctrGameObject = new(path);
            MeshFilter filter = ctrGameObject.AddComponent<MeshFilter>();
            filter.mesh = ctrMesh;
            MeshRenderer meshRend = ctrGameObject.AddComponent<MeshRenderer>();
            
            meshRend.material = mat;

            return ctrGameObject;
        }

        static GameObject ControllerModelAndChildren(string path, Material mat)
        {
            Mesh[] ctrMeshes = Resources.LoadAll<Mesh>(path);
            GameObject ctrGameObject = new(path);
            Material thisMat = mat;

            foreach (Mesh ctrMesh in ctrMeshes)
            {
                GameObject ctrGameObjectChild = new(ctrMesh.name);
                MeshFilter filter = ctrGameObjectChild.AddComponent<MeshFilter>();
                filter.mesh = ctrMesh;
                MeshRenderer meshRend = ctrGameObjectChild.AddComponent<MeshRenderer>();
                meshRend.material = thisMat;
                ctrGameObjectChild.transform.SetParent(ctrGameObject.transform, false);
            }

            return ctrGameObject;
        }

        static GameObject TouchControllerModelAndChildren(string path)
       {
            GameObject model = Instantiate(Resources.Load<GameObject>(path));
            return model;
       }
    }
}