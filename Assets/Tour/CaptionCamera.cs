using UnityEngine;

/// <summary>
/// Draws the caption canvas (captions, board, titles, footnotes) on top of the main camera's picture from a camera
/// that never moves from the world origin. A screen-space canvas sits just in front of its camera; on the main camera
/// that put the captions half a unit from a camera that can be thousands of units out (the experiment panels and
/// charts), where float precision moved the whole layer by a pixel or two from frame to frame. From the origin the
/// layer is exact. The camera follows the main camera's target texture and viewport, renders only the UI layer, and
/// clears depth only, so it simply paints the captions over the finished frame (after the bloom).
/// </summary>
[DefaultExecutionOrder(2000)]
public class CaptionCamera : MonoBehaviour
{
    public const int Layer = 5; // Unity's built-in "UI" layer

    Camera main, cam;

    public static Camera Create(Camera main)
    {
        GameObject go = new("Caption Camera");
        go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        Camera c = go.AddComponent<Camera>();
        c.clearFlags = CameraClearFlags.Depth;
        c.cullingMask = 1 << Layer;
        c.depth = main.depth + 1f;
        c.nearClipPlane = .05f;
        c.farClipPlane = 10f;
        c.allowHDR = false;
        c.allowMSAA = main.allowMSAA;
        c.useOcclusionCulling = false;
        CaptionCamera follower = go.AddComponent<CaptionCamera>();
        follower.main = main;
        follower.cam = c;
        follower.Sync();
        return c;
    }

    void LateUpdate() => Sync();

    void Sync()
    {
        if (main == null)
        {
            Destroy(gameObject);
            return;
        }

        cam.targetTexture = main.targetTexture;
        cam.rect = main.rect;
        cam.enabled = main.enabled && main.gameObject.activeInHierarchy;
    }
}
