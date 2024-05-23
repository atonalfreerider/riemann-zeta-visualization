using UnityEngine;
using UnityEngine.InputSystem;

namespace VRTKLite.Controllers
{
    /// <summary>
    /// <para>This class is used for when there is no VR headset and we are controlling the camera through the mouse,
    /// keyboard, and screen.</para>
    ///
    /// <para>Some desktop-based controls (namely the keyboard and some mouse movements) can be used even with a headset
    /// on, and those controls are implemented in <see cref="UniversalInput" />.</para>
    /// </summary>
    public class MouseFreelook : MonoBehaviour
    {
        // From:
        // http://answers.unity3d.com/questions/29741/mouse-look-script.html

        const float Sensitivity = 1f;

        const float MinimumXRotation = -360f;
        const float MaximumXRotation = 360f;

        const float MinimumYRotation = -60f;
        const float MaximumYRotation = 60f;

        Vector2 rotation = Vector2.zero;

        void Update()
        {
            // Rotate the camera on right click
            if (Mouse.current.rightButton.isPressed)
            {
                // Read the mouse input axis
                rotation += new Vector2(
                    Mouse.current.delta.x.ReadValue(),
                    Mouse.current.delta.y.ReadValue()) * Sensitivity;
                rotation.x = ClampAngle(
                    rotation.x,
                    MinimumXRotation,
                    MaximumXRotation);
                rotation.y = ClampAngle(
                    rotation.y,
                    MinimumYRotation,
                    MaximumYRotation);
                Quaternion xQuaternion =
                    Quaternion.AngleAxis(rotation.x, Vector3.up);
                Quaternion yQuaternion =
                    Quaternion.AngleAxis(rotation.y, -Vector3.right);
                transform.localRotation = xQuaternion * yQuaternion;
            }
        }

        static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f)
            {
                angle += 360f;
            }

            if (angle > 360f)
            {
                angle -= 360f;
            }

            return Mathf.Clamp(angle, min, max);
        }
    }
}