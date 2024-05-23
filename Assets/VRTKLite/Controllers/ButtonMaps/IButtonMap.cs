using UnityEngine;

namespace VRTKLite.Controllers.ButtonMaps
{
    public interface IButtonMap
    {
        Vector3 GetElementPosition(ControllerElements element);
    }
}