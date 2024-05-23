using System;
using UnityEngine;
using VRTKLite.Controllers.ButtonMaps;

namespace VRTKLite.Controllers
{
    /// <summary>
    /// The elements of a generic controller. Not all elements will exist on all controller types.
    /// </summary>
    public enum ControllerElements
    {
        /// <summary>
        /// The default point on the controller to attach grabbed objects to.
        /// </summary>
        AttachPoint,
        /// <summary>
        /// The trigger button.
        /// </summary>
        Trigger,
        /// <summary>
        /// The left part of the grip button collection.
        /// </summary>
        GripLeft,
        /// <summary>
        /// The right part of the grip button collection.
        /// </summary>
        GripRight,
        /// <summary>
        /// The touch pad/stick.
        /// </summary>
        Touchpad,
        /// <summary>
        /// The first generic button.
        /// </summary>
        ButtonOne,
        /// <summary>
        /// The second generic button.
        /// </summary>
        ButtonTwo,
        /// <summary>
        /// The system menu button.
        /// </summary>
        SystemMenu,
        /// <summary>
        /// The encompassing mesh of the controller body.
        /// </summary>
        Body,
        /// <summary>
        /// The start menu button.
        /// </summary>
        StartMenu
    }


    public enum ControllerType
    {
        None = -1,
        Unknown = 0,
        ViveWand = 1,
        RiftTouch = 2,
        WindowsMotion = 3,
        WaveVRController = 4,
        ValveIndex = 5,
        OculusTouchForQuestOrRiftS = 6,
        Cosmos = 7
    }

    public static class ControllerTypeExtensions
    {
        public static ControllerType ControllerTypesFromString(
            string controller)
        {
            if (string.IsNullOrEmpty(controller))
            {
                return ControllerType.None;
            }
            
            if (controller.Contains("cosmos"))
            {
                return ControllerType.Cosmos;
            }

            if (controller.Contains("vive"))
            {
                return ControllerType.ViveWand;
            }

            if (controller.Contains("rifts") ||       // Corresponds to OpenVR SDK
                controller.Contains("quest")) // Corresponds to Oculus SDK
            {
                return ControllerType.OculusTouchForQuestOrRiftS;
            }

            if (controller.Contains("touch"))
            {
                return ControllerType.RiftTouch;
            }

            if (controller.Contains("windowsmr"))
            {
                return ControllerType.WindowsMotion;
            }

            if (controller.Contains("wvr") || controller.Contains("finch"))
            {
                return ControllerType.WaveVRController;
            }

            if (controller.Contains("knuckles") || controller.Contains("index"))
            {
                return ControllerType.ValveIndex;
            }

            Debug.LogWarning($"Unknown controller type: {controller}");
            return ControllerType.Unknown;
        }
        
        public static ControllerButtonMap GetControllerButtonMap(
            string controllerName,
            bool isRight, 
            GameObject controllerModel)
        {
            ControllerType controllerType = ControllerTypesFromString(controllerName);
            switch(controllerType)
            {
                case ControllerType.None:
                    return null;
                case ControllerType.Unknown:
                    return null;
                case ControllerType.ViveWand:
                    return new ViveMap();
                case ControllerType.Cosmos:
                    return new CosmosMap();
                case ControllerType.ValveIndex:
                    return new KnucklesMap();
                case ControllerType.RiftTouch:
                    return new OculusTouchMap(controllerModel, isRight);
                case ControllerType.OculusTouchForQuestOrRiftS:
                    return new OculusTouchForQuestOrRiftSMap();
                case ControllerType.WindowsMotion:
                    return null;
                case ControllerType.WaveVRController:
                    return new WaveVRControllerMap(controllerModel);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(controllerName), controllerName, null);
            }
        }
    }
}