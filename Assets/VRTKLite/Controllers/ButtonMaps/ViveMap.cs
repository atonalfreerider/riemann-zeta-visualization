using System;
using UnityEngine;

namespace VRTKLite.Controllers.ButtonMaps
{
    public class ViveMap : ControllerButtonMap
    {
        public override Vector3 GetElementPosition(ControllerElements element)
        {
            switch (element)
            {
                case ControllerElements.AttachPoint:
                    return Vector3.zero;
                case ControllerElements.Trigger:
                    return new Vector3(0, -.035f, -.05f);
                case ControllerElements.GripLeft:
                    return new Vector3(-.02f, -.015f, .1f);
                case ControllerElements.GripRight:
                    return new Vector3(.02f, -.015f, .1f);
                case ControllerElements.Touchpad:
                    return new Vector3(0, .005f, -.05f);
                case ControllerElements.ButtonOne:
                    return new Vector3(0, .005f, -.05f);
                case ControllerElements.ButtonTwo:
                    return new Vector3(0, .005f, -.02f);
                case ControllerElements.Body:
                    return Vector3.zero;
                case ControllerElements.StartMenu:
                    return Vector3.zero;
                case ControllerElements.SystemMenu:
                    return new Vector3(0, .005f, -.1f);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(element),
                        element,
                        null);
            }
        }
    }
}