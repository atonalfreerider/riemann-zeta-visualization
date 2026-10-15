using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// The Experiments section of the side menu: directed explainer modules for the thought experiments on the saddle
/// (docs/experiments/README.md), scripted in Assets/Resources/Narration/experiments.json with the same line format as
/// the tour. A module runs through the tour machinery (captions, camera cues, actions) as a single chapter; the
/// stage is restored when it ends. Actions start with "exp:" and are listed in ApplyExperimentAction.
/// The lines are narrated like the tour (Tools/generate_narration.py --script experiments); their shots are mostly
/// "orbit:" specs (CameraRig.OrbitCue), and a module opens already framed on its first shot.
/// </summary>
public partial class TourDirector
{
    TourScriptData experiments, mainScript;
    Coroutine sweepRoutine;

    public bool PlayingExperiment { get; private set; }
    public TourChapter[] ExperimentChapters => experiments?.chapters ?? System.Array.Empty<TourChapter>();

    /// <summary>Play one experiment module on its own, from a clean stage, then return to the home view.</summary>
    public void PlayExperiment(string id)
    {
        if (Running)
        {
            jumping = true;
            StopTour();
            jumping = false;
        }

        mainScript ??= script;
        script = experiments;
        PlayingExperiment = true;
        GoToChapter(id, false);
    }

    public void PlayExperiment(int index)
    {
        if (index < 0 || index >= ExperimentChapters.Length) return;
        PlayExperiment(ExperimentChapters[index].id);
    }

    /// <summary>Headless storyboard driver: run the experiment modules as the whole tour (ZETA_SCRIPT=experiments).</summary>
    public void UseExperimentsScript()
    {
        mainScript ??= script;
        script = experiments;
        PlayingExperiment = true;
    }

    /// <summary>Back to the main script and a clean saddle; called when the tour stops or ends.</summary>
    void EndExperiment()
    {
        if (!PlayingExperiment) return;
        PlayingExperiment = false;
        if (mainScript != null) script = mainScript;
        if (sweepRoutine != null)
        {
            StopCoroutine(sweepRoutine);
            sweepRoutine = null;
        }

        Annotations.Sacks.ExpRestoreStage();
    }

    void ExpStage()
    {
        Main.CancelAnimations();
        Main.Pause();
        Annotations.CancelAnimations();
        Annotations.HideAll();
        Main.Limit.SetSpiralVisible(false);
        Main.Limit.SetTailVisible(false);
        SacksSpiral sacks = Annotations.Sacks;
        sacks.ShowFull();
        sacks.ExpCleanStage();
        sacks.ShowRoutes(false);
        sacks.ExpSetBend(1f);
        sacks.ExpNumberField(SacksSpiral.Count);
    }

    static float[] Heights(string list)
    {
        List<float> h = new();
        foreach (string s in list.Split(',', System.StringSplitOptions.RemoveEmptyEntries))
        {
            if (float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float v)) h.Add(v);
        }

        return h.ToArray();
    }

    /// <summary>
    /// exp:stage · exp:clear · exp:backdrop:on|off · exp:routes:on|off · exp:bend:B · exp:sweep:FROM:TO:SECONDS:H1,H2[:families]
    /// exp:contours:H1,H2[:planes] · exp:family:squares|quarter|diagonal|all · exp:section:x|y|z:C · exp:trail:TMAX[:gram][:half]
    /// exp:curves:MAXD · exp:euler:KMAX[:plain] · exp:zetawave · exp:vortexprep · exp:vortexpanel:J · exp:tower:A:B[:derotate]
    /// exp:towerall[:derotate][:KMAX] · exp:chainlive:FROM:TO:SECONDS[:rings][:mirrors] · exp:towergrow:A:B:SECONDS
    /// (FROM / TO: a dial value, zN for zero N, or "here" for where the live chain is; SECONDS: a number or "line")
    /// Section modules: exp:surface:FOLDS:TWISTπ · exp:surfacewire:RMAX · exp:surfacefield:NMAX · exp:lattice:circle|divisor:TERMS[:exact][:series][:first]
    /// exp:latticelive:circle|divisor:TERMS:SECONDS · exp:twistlive:FROMπ:TOπ:SECONDS[:ribbon] · exp:idealfield:NMAX · exp:idealneedles:NMAX:zeta|l|both
    /// exp:idealwave:NMAX:zeta|l|lonly:SECONDS · exp:eulerroute:NMAX · exp:numberlabels:N1,N2,… · exp:gaussfield · exp:mirrorcomb:M
    /// exp:mirrorswing:M:SECONDS[:FROM:TO] · exp:eulershow:N · exp:fermatwalk:N:SECONDS · exp:fermat:N · exp:comb[:rims][:sections][:labels]
    /// exp:combslide:B:FROM:TO:SECONDS · exp:skyline:B · exp:meet
    /// </summary>
    void ApplyExperimentAction(string[] p)
    {
        SacksSpiral sacks = Annotations.Sacks;
        string verb = p.Length > 1 ? p[1].Trim().ToLowerInvariant() : "";
        string A(int i) => p.Length > i ? p[i].Trim() : "";
        float F(int i, float d) => float.TryParse(A(i), NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : d;
        int I(int i, int d) => int.TryParse(A(i), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : d;
        bool Has(string flag)
        {
            for (int i = 2; i < p.Length; i++) if (p[i].Trim().ToLowerInvariant() == flag) return true;
            return false;
        }

        double[] zs = Main.zetaZeroValues;
        Vector3 c = sacks.Center;
        Vector3 panel = c + new Vector3(SacksSpiral.ExpPanelOffset, 0f, 0f);
        Vector3 tower = c + new Vector3(SacksSpiral.ExpTowerOffset, 0f, 0f);
        Color fall = new(1f, .45f, .3f), rise = new(.35f, .65f, 1f), level = new(.85f, .85f, .95f); // below the centre (falls away from the trail) / above it
        float Seconds(string s, float d) => s.ToLowerInvariant() == "line" ? currentLineSeconds * .9f : float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : d;
        switch (verb)
        {
            case "stage": ExpStage(); break;
            case "clear": sacks.ClearExperiments(); break;
            case "backdrop": sacks.ShowBackdrop(A(2).ToLowerInvariant() != "off"); break;
            case "routes": sacks.ShowRoutes(A(2).ToLowerInvariant() != "off"); break;
            case "bend":
                sacks.ExpSetBend(F(2, 1f));
                sacks.ExpNumberField(SacksSpiral.Count);
                break;
            case "sweep":
                if (sweepRoutine != null) StopCoroutine(sweepRoutine);
                sweepRoutine = StartCoroutine(sacks.ExpBendSweep(F(2, 0f), F(3, 1f), F(4, 5f), Heights(A(5)), Has("families")));
                break;
            case "contours":
                foreach (float h in Heights(A(2)))
                {
                    Color col = h > 0 ? fall : h < 0 ? rise : level;
                    if (Has("planes")) sacks.ExpSectionPlane('z', h, sacks.Extent + 6f, col, 16f);
                    sacks.ExpSectionCurve('z', h, col, .07f);
                    sacks.ExpSectionNumbers('z', h, System.Math.Abs(h) < 1e-3f ? .6f : 1f, col, SacksSpiral.Count, false, 0f);
                }

                break;
            case "family": sacks.ExpFamilies(A(2).ToLowerInvariant(), 0f); break;
            case "section":
            {
                char axis = A(2).ToLowerInvariant().Length > 0 ? A(2).ToLowerInvariant()[0] : 'z';
                float cc = F(3, 0f);
                Color col = axis == 'y' ? fall : axis == 'x' ? rise : level;
                sacks.ExpSectionPlane(axis, cc, sacks.Extent + 6f, col);
                sacks.ExpSectionCurve(axis, cc, col);
                sacks.ExpSectionNumbers(axis, cc, 1.2f, col, SacksSpiral.Count, false, 0f);
                break;
            }
            case "trail": sacks.ExpTrail(F(2, 140f), Has("gram"), Has("half"), 0f); break;
            case "curves": sacks.ExpCancellationCurves(I(2, 12), SacksSpiral.Count, 39, false, 4f, false, false); break;
            case "euler":
            {
                bool plain = Has("plain");
                sacks.ExpCancellationCurves(plain ? -1 : 16, 12000, I(2, 110), !plain, 7f, !plain, !plain);
                break;
            }
            case "zetawave": sacks.ExpZetaWaveAlongEuler(110, zs.Length, 7f, 7f); break;
            case "vortexprep": SacksSpiral.ExpPrepareVortices(zs); break;
            case "vortexpanel":
            {
                int j = Mathf.Clamp(I(2, 100), 1, zs.Length);
                sacks.ClearExperiments();
                sacks.ExpChainVortexPanel(zs[j - 1], j, panel, 11f, 16f, 12, $"zero #{j},  t = {zs[j - 1]:0.000}", 1.4f);
                break;
            }
            case "tower":
            {
                int a = Mathf.Clamp(I(2, 1), 1, zs.Length), b = Mathf.Clamp(I(3, a + 39), a + 1, zs.Length);
                bool derotate = Has("derotate");
                double t0 = zs[a - 1];
                float scale = 44f / Mathf.Max(1f, (float)(zs[b - 1] - t0));
                sacks.ClearExperiments();
                sacks.ExpVortexTower(SacksSpiral.ExpVortices(zs), a, b, tower, 4f, t => (float)(t - t0) * scale, derotate, true, !derotate, !derotate, b - a > 100 ? 8 : 8, 2.2f);
                sacks.ExpTowerFocus = derotate ? new Vector3(-4f, 0f, 0f) : Vector3.zero;
                break;
            }
            case "snap": Rig.Snap(); break; // put last in a line that rebuilds the picture elsewhere: cut, don't fly
            case "chainlive":
            {
                double Dial(string s)
                {
                    s = s.ToLowerInvariant();
                    if (s == "here" && sacks.ExpLiveT > 0) return sacks.ExpLiveT;
                    if (s.StartsWith("z") && int.TryParse(s.Substring(1), out int zj)) return zs[Mathf.Clamp(zj, 1, zs.Length) - 1];
                    return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : zs[99];
                }

                float seconds = A(4).ToLowerInvariant() == "line" ? currentLineSeconds * .92f : F(4, 6f);
                sacks.ExpChainLive(Dial(A(2)), Dial(A(3)), seconds, panel, 9.5f, 15f, Has("rings"), Has("mirrors"), 6f);
                break;
            }
            case "towergrow":
            {
                int a = Mathf.Clamp(I(2, 1), 1, zs.Length), b = Mathf.Clamp(I(3, a + 39), a + 1, zs.Length);
                double t0 = zs[a - 1];
                float scale = 44f / Mathf.Max(1f, (float)(zs[b - 1] - t0));
                float seconds = A(4).ToLowerInvariant() == "line" ? currentLineSeconds * .9f : F(4, 10f);
                sacks.ClearExperiments();
                sacks.ExpTowerFocus = Vector3.zero;
                sacks.ExpTowerGrow(a, b, seconds, tower, 4f, t => (float)(t - t0) * scale, 9f);
                break;
            }
            case "towerall":
            {
                bool derotate = Has("derotate");
                int kMax = 12;
                for (int i = 2; i < p.Length; i++) if (int.TryParse(p[i].Trim(), out int k)) kMax = k;
                System.Func<double, float> byIndex = t =>
                {
                    int lo = 0, hi = zs.Length - 1;
                    while (hi - lo > 1) { int m = (lo + hi) / 2; if (zs[m] <= t) lo = m; else hi = m; }
                    double f = (t - zs[lo]) / System.Math.Max(1e-9, zs[hi] - zs[lo]);
                    return (float)((lo + f) * .0002); // a flat stack: looked at down the axis, every zero's plane is at nearly the same distance
                };
                sacks.ClearExperiments();
                sacks.ExpVortexTower(SacksSpiral.ExpVortices(zs), 1, zs.Length, tower, 4f, byIndex, derotate, false, false, false, kMax, 0f, 1, .007f, false, Factory.NoGlow);
                sacks.ExpTowerFocus = derotate ? new Vector3(-4f, 0f, 0f) : Vector3.zero;
                break;
            }
            // ---- section modules (SacksSections.cs, SacksLatticeWaves.cs, SacksSectionsLive.cs) ----
            case "surface": sacks.ExpSetSurface(I(2, 2), F(3, 0f) * Mathf.PI); break;
            case "surfacewire": sacks.ExpSurfaceWire(F(2, 30f), new Color(.32f, .36f, .5f), 6f); break;
            case "surfacefield": sacks.ExpSurfaceField(I(2, SacksSpiral.Count)); break;
            case "lattice":
                sacks.ExpLatticeWave(A(2).ToLowerInvariant() == "circle", I(3, 3000), 9f, 400f, 5f, Has("exact"), Has("series"), Has("first"));
                break;
            case "latticelive":
                sacks.ExpLatticeLive(A(2).ToLowerInvariant() == "circle", I(3, 3000), Seconds(A(4), 8f), 9f, 400f, 5f, 4.2f);
                break;
            case "latticechart":
            {
                // exp:latticechart:circle|divisor:TERMS[:exact][:series][:first][:top|bottom]
                Vector3 at = c + new Vector3(SacksSpiral.LatticeChartOffset, Has("top") ? 22f : Has("bottom") ? -22f : 0f, 0f);
                sacks.ExpLatticeChart(A(2).ToLowerInvariant() == "circle", I(3, 3000), at, 54f, 26f, 12f, Has("exact"), Has("series"), Has("first"));
                break;
            }
            case "latticechartlive":
            {
                Vector3 at = c + new Vector3(SacksSpiral.LatticeChartOffset, Has("top") ? 22f : Has("bottom") ? -22f : 0f, 0f);
                sacks.ExpLatticeChartLive(A(2).ToLowerInvariant() == "circle", I(3, 3000), Seconds(A(4), 8f), at, 54f, 26f, 12f);
                break;
            }
            case "twistlive":
                sacks.ExpTwistLive(F(2, 0f), F(3, 2f), Seconds(A(4), 8f), Has("ribbon"), 400, 16f);
                break;
            case "idealfield": sacks.ExpIdealField(I(2, 800)); break;
            case "idealneedles": sacks.ExpIdealNeedles(I(2, 800), A(3).ToLowerInvariant(), 6f); break;
            case "idealwave": sacks.ExpIdealWaveLive(I(2, 800), A(3).ToLowerInvariant(), Seconds(A(4), 8f), 6f, 17f); break;
            case "eulerroute": sacks.ExpEulerRoute(I(2, 800)); break;
            case "numberlabels":
            {
                List<long> ns = new();
                foreach (string t in A(2).Split(',')) if (long.TryParse(t.Trim(), out long v)) ns.Add(v);
                sacks.ExpNumberLabels(ns.ToArray(), F(3, 7f), Color.white);
                break;
            }
            case "gaussfield": sacks.ExpGaussianField(SacksSpiral.Count); break;
            case "mirrorcomb": sacks.ExpMirrorComb(I(2, 3), SacksSpiral.Count, 14f, false); break;
            case "mirrorswing":
                sacks.ExpMirrorSwingLive(I(2, 3), Seconds(A(3), 6f), SacksSpiral.Count, 14f, F(4, 0f), F(5, 1f));
                break;
            case "eulershow":
            {
                long n = I(2, 1105);
                Vector3 off = n == 1105 ? new Vector3(0f, 14f, -1f) : n == 221 ? new Vector3(-6f, -16f, -1f) : new Vector3(-22f, -9f, -1f);
                sacks.ExpEulerShowcase(n, n == 1105 ? 16f : 10f, n == 1105 ? new Vector3(0f, 17f, -1f) : off);
                break;
            }
            case "fermatwalk": sacks.ExpFermatWalkLive(I(2, 1501), Seconds(A(3), 8f), 15f); break;
            case "fermat": sacks.ExpFermatShowcase(I(2, 1501), 7.5f, new Vector3(0f, 10f, -1f)); break;
            case "comb":
                sacks.ExpComb(new[] { -9, -4, -2, -1, 0, 1, 2, 4, 7 }, new[] { -6, -2, -1, 0, 1, 2, 3, 5 }, -20f, 75f, Has("rims"), Has("sections"), true, Has("labels") ? 9.5f : 0f, SacksSpiral.Count);
                break;
            case "combslide": sacks.ExpCombSlideLive(I(2, 0), F(3, -9f), F(4, 7f), Seconds(A(5), 8f), 3000, 20f); break;
            case "skyline": sacks.ExpSkyline(I(2, 0), -30, 60, 3000, c + new Vector3(2600f, 0f, 0f), 2.1f, 42f, 18f); break;
            case "meet": sacks.ExpEulerLandauMeet(24f); break;
            case "flare":
                // exp:flare:N1,N2,…[:RADIUS] — white flares at the numbers the line is talking about
                foreach (string t in A(2).Split(',')) if (long.TryParse(t.Trim(), out long fn)) sacks.FlareNumber(fn, F(3, 2.4f), 2.4f, 3);
                break;
            default:
                Debug.LogWarning($"Experiments: unknown action exp:{verb}");
                break;
        }
    }
}
