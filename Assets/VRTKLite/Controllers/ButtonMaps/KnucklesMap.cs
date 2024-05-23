using System;
using UnityEngine;

namespace VRTKLite.Controllers.ButtonMaps
{
    public class KnucklesMap : ControllerButtonMap
    {
        public override Vector3 GetElementPosition(ControllerElements element)
        {
            switch (element)
            {
                case ControllerElements.AttachPoint:
                    return new Vector3(-0.0006f, -0.0042f, 0.0931f);
                case ControllerElements.Trigger:
                    return new Vector3(-0.0065f, -0.0347f, 0.0504f);
                case ControllerElements.GripLeft:
                    return new Vector3(0, -.025f, -.1f);
                case ControllerElements.GripRight:
                    return new Vector3(0, -.025f, -.1f);
                // right hand only
                case ControllerElements.Touchpad:
                    return new Vector3(0.0139f, .0015f, 0.0515f);
                case ControllerElements.ButtonOne:
                    return new Vector3(-0.01895f, 0.00888f, 0.04f);
                case ControllerElements.ButtonTwo:
                    return new Vector3(-0.02156f, 0.00403f, 0.048f);
                
                case ControllerElements.Body:
                    return new Vector3(-0.0006f, -0.0042f, 0.0931f);
                case ControllerElements.StartMenu:
                    return Vector3.zero;
                case ControllerElements.SystemMenu:
                    return new Vector3(-.01f, .01f, -.065f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(element), element, null);
            }
        }
    }
}