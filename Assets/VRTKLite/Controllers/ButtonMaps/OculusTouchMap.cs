using System;
using UnityEngine;

namespace VRTKLite.Controllers.ButtonMaps
{
    public class OculusTouchMap : ControllerButtonMap
    {
        readonly bool isRight;

        string Prefix => !isRight
            ? "lctrl"
            : "rctrl";

        public OculusTouchMap(GameObject controller, bool isRight)
        {
            controllerModel = controller;
            this.isRight = isRight;
        }

        protected override string ElementPath(ControllerElements element)
        {
            switch (element)
            {
                case ControllerElements.AttachPoint:
                    return null;
                case ControllerElements.Trigger:
                    return $"{Prefix}:b_trigger";
                case ControllerElements.GripLeft:
                    return $"{Prefix}:b_hold";
                case ControllerElements.GripRight:
                    return $"{Prefix}:b_hold";
                case ControllerElements.Touchpad:
                    return $"{Prefix}:b_stick";
                case ControllerElements.ButtonOne:
                    return $"{Prefix}:b_button01";
                case ControllerElements.ButtonTwo:
                    return $"{Prefix}:b_button02";
                case ControllerElements.Body:
                    return null;
                case ControllerElements.StartMenu:
                    return null;
                case ControllerElements.SystemMenu:
                    return $"{Prefix}:b_button03";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(element),
                        element,
                        null);
            }
        }
    }
}