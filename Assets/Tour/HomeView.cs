using UnityEngine;

/// <summary>
/// What the app shows before (and after) the tour: the Sacks spiral bent on its saddle, slowly orbiting, with the master
/// wave of every zero in the table already riding the spiral and the zeta trail growing out of the saddle's centre from
/// t = 0, faster and faster, dropping a marker at every zero, up to t = 1000 (a practical stand-in for infinity). The
/// picture holds, then the trail grows again. The tour's last line ends on the same animation.
///
/// Desktop and phones: the camera orbits the saddle with the saddle's normal as screen-up, so the trail rises up the screen.
/// VR: the VR layer stands the user on the saddle (its "Stand on it" view) and calls <see cref="PrepareScene"/>, so the
/// trail grows from the floor up.
/// </summary>
public class HomeView : MonoBehaviour
{
    TourDirector director;

    public bool Active { get; private set; }

    public static HomeView Create(TourDirector director)
    {
        HomeView home = new GameObject("HomeView").AddComponent<HomeView>();
        home.director = director;
        return home;
    }

    /// <summary>Clear the stage and start the growth (no camera changes). From a tour line the captions are left alone.</summary>
    public void PrepareScene(bool fromTour = false)
    {
        SacksSpiral sacks = director.Annotations.Sacks;
        if (sacks.Growing && sacks.Mode == SacksSpiral.GrowthMode.FullWave && sacks.GrowthTMax >= SacksSpiral.HomeTMax)
        {
            return; // already showing it (the tour's last line started it): carry on rather than start over
        }

        Main main = director.Main;
        main.CancelAnimations();
        main.Pause();
        main.SetPrimeCurveVisible(false);
        main.SetCosineVisible(false);
        main.SetEquationsVisible(false);
        main.SetZetaCurveVisible(false);
        main.SetGridVisible(false);
        main.SetZeroMarkersVisible(false);
        main.Limit.SetSpiralVisible(false);
        main.Limit.SetTailVisible(false);
        director.Annotations.CancelAnimations();
        director.Annotations.HideAll();
        if (!fromTour) director.Captions.Clear();
        sacks.ShowFull();
        sacks.ShowRoutes(false);
        sacks.FullWaveAmplitude = 5f;
        sacks.StartGrowth(SacksSpiral.GrowthMode.FullWave, SacksSpiral.HomeTMax, 0f, true);
    }

    public void Enter()
    {
        SacksSpiral sacks = director.Annotations.Sacks;
        // coming from the tour's last line the animation is already running: carry on with the camera where it is
        bool continuing = sacks.Growing && sacks.Mode == SacksSpiral.GrowthMode.FullWave && sacks.GrowthTMax >= SacksSpiral.HomeTMax;
        Active = true;
        PrepareScene();
        if (VRMode.Enabled) return; // the VR layer places the model itself
        director.Rig.Release();
        director.Orbit.Begin(!continuing);
        if (!continuing)
        {
            // the launch card plays over the saddle whenever the graph view opens with its intro
            foreach (LaunchTitle old in FindObjectsByType<LaunchTitle>(FindObjectsSortMode.None)) Destroy(old.gameObject);
            LaunchTitle.Play();
        }
    }

    public void Exit()
    {
        if (!Active && !director.Annotations.Sacks.Growing) return;
        Active = false;
        if (director.Orbit != null) director.Orbit.End();
        director.Annotations.Sacks.StopGrowth();
        if (!director.Running) director.Rig.Release();
    }
}
