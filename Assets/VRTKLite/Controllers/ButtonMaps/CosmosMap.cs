using System;
using UnityEngine;

namespace VRTKLite.Controllers.ButtonMaps
{
    public class CosmosMap : ControllerButtonMap
    {
        public override Vector3 GetElementPosition(ControllerElements element)
        {
            switch (element)
            {
                case ControllerElements.AttachPoint:
                    return Vector3.zero;
                case ControllerElements.Trigger:
                    return new Vector3(0, -.056f, -.056f);
                case ControllerElements.GripLeft:
                    return new Vector3(0, -.025f, -.1f);
                case ControllerElements.GripRight:
                    return new Vector3(0, -.025f, -.1f);
                // right hand only
                case ControllerElements.Touchpad:
                    return new Vector3(-.006f, .002f, -.05f);
                case ControllerElements.ButtonOne:
                    return new Vector3(-.013f, .006f, -.06f);
                case ControllerElements.ButtonTwo:
                    return new Vector3(-.017f, .003f, -.05f);

                case ControllerElements.Body:
                    return Vector3.zero;
                case ControllerElements.StartMenu:
                    return Vector3.zero;
                case ControllerElements.SystemMenu:
                    return new Vector3(.006f, .0006f, -.06f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(element), element, null);
            }
        }
    }
}