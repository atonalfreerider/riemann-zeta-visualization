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
    /// exp:contours:H1,H2[:planes] · exp:family:ridge|valley|level|all · exp:section:x|y|z:C · exp:trail:TMAX[:gram][:half]
    /// exp:curves:MAXD · exp:euler:KMAX[:plain] · exp:zetawave · exp:vortexprep · exp:vortexpanel:J · exp:tower:A:B[:derotate]
    /// exp:towerall[:derotate][:KMAX] · exp:chainlive:FROM:TO:SECONDS[:rings][:mirrors] · exp:towergrow:A:B:SECONDS
    /// (FROM / TO: a dial value, zN for zero N, or "here" for where the live chain is; SECONDS: a number or "line")
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
        Color ridge = new(1f, .45f, .3f), valley = new(.35f, .65f, 1f), level = new(.85f, .85f, .95f);
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
                    Color col = h > 0 ? ridge : h < 0 ? valley : level;
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
                Color col = axis == 'y' ? ridge : axis == 'x' ? valley : level;
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
            default:
                Debug.LogWarning($"Experiments: unknown action exp:{verb}");
                break;
        }
    }
}
