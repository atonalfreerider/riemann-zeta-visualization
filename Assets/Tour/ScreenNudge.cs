using UnityEngine;

/// <summary>
/// Holds a billboard label at a fixed offset from the point it names, measured in the camera's picture plane (world units
/// along the camera's right and up), so the label sits beside its point from any viewpoint instead of on top of it.
/// </summary>
[DefaultExecutionOrder(1001)]
public class ScreenNudge : MonoBehaviour
{
    public Vector3 Anchor;
    public Vector2 Offset;

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        Transform t = cam.transform;
        transform.position = Anchor + t.right * Offset.x + t.up * Offset.y;
    }
}
