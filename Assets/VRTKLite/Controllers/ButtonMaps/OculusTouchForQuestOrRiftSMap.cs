using System;
using UnityEngine;

namespace VRTKLite.Controllers.ButtonMaps
{
    public class OculusTouchForQuestOrRiftSMap : ControllerButtonMap
    {
        public override Vector3 GetElementPosition(ControllerElements element)
        {
            switch (element)
            {
                case ControllerElements.AttachPoint:
                    return Vector3.zero;
                case ControllerElements.Trigger:
                    return new Vector3(0, -.01f, .02f);
                case ControllerElements.GripLeft:
                    return new Vector3(0, -.02f, -.03f);
                case ControllerElements.GripRight:
                    return new Vector3(0, -.02f, -.03f);
                case ControllerElements.Touchpad:
                    // this is for right touch (left is -0.0185f
                    return new Vector3(.0185f, 0, .0085f);
                case ControllerElements.ButtonOne:
                    return new Vector3(0, 0, -.01f);
                case ControllerElements.ButtonTwo:
                    return new Vector3(0, 0, .01f);
                case ControllerElements.Body:
                    return Vector3.zero;
                case ControllerElements.StartMenu:
                    return Vector3.zero;
                case ControllerElements.SystemMenu:
                    return new Vector3(0, 0, -.0265f);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(element),
                        element,
                        null);
            }
        }
    }
}