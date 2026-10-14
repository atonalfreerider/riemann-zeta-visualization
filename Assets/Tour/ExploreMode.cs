using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Free exploration of the final picture: the Sacks spiral on the saddle, the zeta trail rising from its centre,
/// and the master wave with every zero in the table riding the spiral.
///
/// Desktop: WASD / QE move, right-drag look, scroll changes speed, X flat / saddle; leave from the side menu. Starts above the saddle looking down.
/// VR: hands over to the VR layer's "Explore" view (VRMenus), which stands the user at the saddle centre with the
///     saddle as the floor and the trail rising overhead.
/// </summary>
public class ExploreMode : MonoBehaviour
{
    public static ExploreMode Instance { get; private set; }
    public bool Active { get; private set; }

    TourDirector director;
    Camera cam;
    CameraControl manual;
    Vector3 savedCamPos;
    Quaternion savedCamRot;

    public static ExploreMode Create(TourDirector director)
    {
        GameObject go = new("ExploreMode");
        ExploreMode e = go.AddComponent<ExploreMode>();
        e.director = director;
        e.cam = Camera.main;
        e.manual = e.cam != null ? e.cam.GetComponent<CameraControl>() : null;
        Instance = e;
        return e;
    }

    /// <summary>Show the final picture with every zero, ready to walk around.</summary>
    public void PrepareScene()
    {
        Main main = director.Main;
        main.Pause();
        main.SetPrimeCurveVisible(false); main.SetCosineVisible(false); main.SetEquationsVisible(false);
        main.SetZetaCurveVisible(false); main.SetGridVisible(false); main.SetZeroMarkersVisible(false);
        main.Limit.SetSpiralVisible(false);
        if (director.Home != null) director.Home.Exit();
        director.Annotations.HideAll();
        SacksSpiral sacks = director.Annotations.Sacks;
        sacks.ShowFull();
        sacks.ShowZetaOnly();
        if (!sacks.FullWaveRunning && sacks.FullWaveZerosShown == 0)
        {
            sacks.StartFullMasterWave(main.zetaZeroValues.Length, 160000, 1300f, 0f);
        }
    }

    public void Enter()
    {
        if (Active) return;
        if (VRMode.Enabled)
        {
            if (VRMode.App != null) VRMode.App.ApplyView("explore");
            return;
        }

        Active = true;
        if (director.Running) director.StopTour();
        PrepareScene();
        director.Rig.Release();
        director.Rig.Portrait = false;
        director.Captions.Clear();
        savedCamPos = cam.transform.position;
        savedCamRot = cam.transform.rotation;
        SacksSpiral sacks = director.Annotations.Sacks;
        manual.enabled = true;
        manual.Speed = 14f;
        cam.transform.position = sacks.Center + Vector3.back * 14f;
        cam.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        cam.fieldOfView = 55f;
        sacks.FullWaveAmplitude = 2.4f;
        director.Captions.SetHint("");
    }

    public void Exit()
    {
        if (!Active) return;
        Active = false;
        cam.transform.position = savedCamPos;
        cam.transform.rotation = savedCamRot;
        manual.enabled = true;
        manual.Speed = 10f;
        director.Captions.SetHint("");
    }

    void Update()
    {
        if (!Active) return;
        SacksSpiral sacks = director.Annotations.Sacks;
        Keyboard k = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f) manual.Speed = Mathf.Clamp(manual.Speed * (scroll > 0 ? 1.2f : 1f / 1.2f), 1f, 120f);
        }

        // the wave is displaced along the saddle normal; keep it readable at any camera distance
        float d = Vector3.Distance(cam.transform.position, sacks.Center);
        sacks.FullWaveAmplitude = Mathf.Clamp(d * 0.17f, 1.5f, 14f);
        if (k != null && k.xKey.wasPressedThisFrame) sacks.SetFlat(!sacks.Flat);
    }
}
