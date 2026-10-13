using UnityEngine;

/// <summary>Keeps a world-space label facing the camera (runs after CameraRig's LateUpdate).</summary>
[DefaultExecutionOrder(1000)]
public class Billboard : MonoBehaviour
{
    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam != null) transform.rotation = cam.transform.rotation;
    }
}
