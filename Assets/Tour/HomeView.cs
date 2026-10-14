using UnityEngine;

/// <summary>
/// What the app shows before (and after) the tour: the Sacks spiral bent on its saddle, slowly orbiting, with the zeta
/// trail growing out of the saddle's centre from t = 0. Every zero the trail passes sends its cosine wave out along the
/// spiral at the zero's height; the wave sinks onto the saddle and joins the master wave. When the trail reaches the top
/// the picture holds, then grows again.
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

    /// <summary>Clear the stage and start the growth (no camera changes).</summary>
    public void PrepareScene()
    {
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
        director.Captions.Clear();
        SacksSpiral sacks = director.Annotations.Sacks;
        sacks.StopFullMasterWave();
        sacks.ShowFull();
        sacks.ShowRoutes(false);
        sacks.ShowHypothesis(false);
        sacks.StartGrowth();
    }

    public void Enter()
    {
        Active = true;
        PrepareScene();
        if (VRMode.Enabled) return; // the VR layer places the model itself
        director.Rig.SetCue("home_orbit", 0.2f);
        director.Rig.Snap();
    }

    public void Exit()
    {
        if (!Active && !director.Annotations.Sacks.Growing) return;
        Active = false;
        director.Annotations.Sacks.StopGrowth();
        if (!director.Running) director.Rig.Release();
    }
}
