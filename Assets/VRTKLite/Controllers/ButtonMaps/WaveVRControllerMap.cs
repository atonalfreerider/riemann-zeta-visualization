using UnityEngine;

namespace VRTKLite.Controllers.ButtonMaps
{
    public class WaveVRControllerMap : ControllerButtonMap
    {
        public WaveVRControllerMap(GameObject controller)
        {
            controllerModel = controller;
        }
        
        protected override string ElementPath(ControllerElements element)
        {
            switch (element)
            {
                case ControllerElements.Trigger:
                    return "Trigger";
                case ControllerElements.Touchpad:
                case ControllerElements.ButtonOne:
                    return "TouchPad";
                case ControllerElements.ButtonTwo:
                    return "AppButton";
                case ControllerElements.SystemMenu:
                    return "HomeButton";
                case ControllerElements.Body:
                    return "Body";
                case ControllerElements.StartMenu:
                    return null;
                case ControllerElements.AttachPoint:
                    break;
                case ControllerElements.GripLeft:
                    return "Grip";
                case ControllerElements.GripRight:
                    return "Grip";
            }

            return "";
        }
    }
}