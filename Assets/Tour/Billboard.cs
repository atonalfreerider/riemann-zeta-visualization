using UnityEngine;

/// <summary>Keeps a world-space label facing the camera (runs after CameraRig's LateUpdate).</summary>
[DefaultExecutionOrder(1000)]
public class Billboard : MonoBehaviour
{
    static Transform camTransform;
    static int camFrame = -1;

    void LateUpdate()
    {
        if (camFrame != Time.frameCount)
        {
            camFrame = Time.frameCount;
            Camera cam = Camera.main;
            camTransform = cam != null ? cam.transform : null;
        }

        if (camTransform != null) transform.rotation = camTransform.rotation;
    }
}
