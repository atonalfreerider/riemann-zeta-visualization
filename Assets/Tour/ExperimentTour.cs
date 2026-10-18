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

    /// <summary>
    /// Play one experiment module on its own, from a clean stage, then return to the home view. With
    /// <paramref name="continueOn"/> the modules after it follow (the VR sweep uses that).
    /// </summary>
    public void PlayExperiment(string id, bool continueOn = false)
    {
        if (Running)
        {
            jumping = true;
            StopTour();
            jumping = false;
        }

        UseExperimentsScript();
        GoToChapter(id, continueOn);
    }

    /// <summary>
    /// Back to the main tour's script: stops the module that is playing (without the end-of-tour handling), so a chapter
    /// of the tour can be started next.
    /// </summary>
    public void LeaveExperiment()
    {
        if (!PlayingExperiment) return;
        if (Running)
        {
            jumping = true;
            StopTour(); // ends the experiment
            jumping = false;
        }
        else
        {
            EndExperiment();
        }
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
        SacksSpiral.LabelBoost = 1f;
        SacksSpiral.CalloutScale = 1f;
        SacksSpiral.Tall = false;
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
        // the corner readouts belong to the scene that wrote them
        SacksSpiral.Hud(0, "", Color.white);
        SacksSpiral.Hud(1, "", Color.white);
    }

    /// <summary>
    /// The home view as a teaser inside a module: the Sacks spiral on its saddle with the master wave of every zero, and
    /// ζ's trail growing out of the saddle's centre to t = tMax in the given time (exp:home:TMAX:SECONDS).
    /// </summary>
    void ExpHomeTeaser(float tMax, float seconds)
    {
        SacksSpiral sacks = Annotations.Sacks;
        Main.CancelAnimations();
        Main.Pause();
        Main.SetPrimeCurveVisible(false);
        Main.SetCosineVisible(false);
        Main.SetEquationsVisible(false);
        Main.SetZetaCurveVisible(false);
        Main.SetGridVisible(false);
        Main.SetZeroMarkersVisible(false);
        Main.Limit.SetSpiralVisible(false);
        Main.Limit.SetTailVisible(false);
        Annotations.CancelAnimations();
        Annotations.HideAll();
        sacks.ShowFull();
        sacks.ShowRoutes(false);
        sacks.FullWaveAmplitude = 5f;
        sacks.StartGrowth(SacksSpiral.GrowthMode.FullWave, tMax, seconds, false);
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
    /// Pinches (SacksPinch.cs): exp:fold:F[:comb] · exp:residue:M:S[:arms] · exp:pleat:L:S[:curves][:marks] · exp:residuelattice[:even]
    /// exp:pinchlive:fold|residue|pleat:PARAM:SECONDS (fold PARAM = the final angle multiple)
    /// The 144 modules: exp:turnstep:N1,N2 · exp:board[:diagonals][:verticals] · exp:boardfill:SECONDS · exp:diagsweep:D1,D2:SECONDS
    /// exp:torusroll:SECONDS · exp:classes · exp:charlive:M:3|2|5:SECONDS[:slide][:rise][:BENDTO] · exp:fingerlive:CODE:SECONDS
    /// (CODE empty, primes, sq, euler, 17, k2p1, k2k1, pronic, u4u1) · exp:fingerall · exp:clocklive:SECONDS · exp:clock[:labels]
    /// exp:clockpulse:red|orange · exp:quadglow:Q:B:C
    /// The zeros module: exp:zfunnel · exp:ztrees · exp:zpaths · exp:zspacing · exp:show:GROUP (all items of a hidden group at once)
    /// </summary>
    /// <summary>The moduli of the sieve machine's rings (exp:sievedial, exp:sieveboard).</summary>
    static readonly int[] SieveMods = { 3, 4, 5, 7, 8, 9, 11, 13, 16, 17, 19 };

    /// <summary>A height on the critical line: a number, or zK for the K-th zero of ζ.</summary>
    double Gamma(string s)
    {
        double[] zs = Main.zetaZeroValues;
        if (s.StartsWith("z") && int.TryParse(s.Substring(1), out int k)) return zs[Mathf.Clamp(k, 1, zs.Length) - 1];
        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : zs[0];
    }

    static Vector3 ParseVector(string s)
    {
        string[] v = s.Split(',');
        float At(int i) => v.Length > i && float.TryParse(v[i], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ? x : 0f;
        return new Vector3(At(0), At(1), At(2));
    }

    /// <summary>An argument written "WIDE^TALL": the side for the frame the segment plays in (SacksSpiral.Tall).</summary>
    static string Pick(string s)
    {
        int k = s.IndexOf('^');
        if (k < 0) return s;
        return SacksSpiral.Tall ? s.Substring(k + 1) : s.Substring(0, k);
    }

    void ApplyExperimentAction(string[] p)
    {
        SacksSpiral sacks = Annotations.Sacks;
        string verb = p.Length > 1 ? p[1].Trim().ToLowerInvariant() : "";
        string A(int i) => p.Length > i ? Pick(p[i].Trim()) : "";
        float F(int i, float d) => float.TryParse(A(i), NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : d;
        int I(int i, int d) => int.TryParse(A(i), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : d;
        float LastNumber(int from, float d)
        {
            float v = d;
            for (int i = from; i < p.Length; i++)
                if (float.TryParse(p[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float x)) v = x;
            return v;
        }

        // an optional position X,Y,Z (from the centre) in the first argument, or the scene's own place
        Vector3 Where(Vector3 own) => A(2).Contains(",") ? ParseVector(A(2)) : own;

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
                // "wide" frames the chain for a horizontal picture
                bool wide = Has("wide");
                sacks.ExpChainLive(Dial(A(2)), Dial(A(3)), seconds, panel, wide ? 17f : 9.5f, wide ? 10.5f : 15f, Has("rings"), Has("mirrors"), 6f, Has("quiet"), Has("flat"));
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
            case "fold":
                sacks.ExpFoldField(SacksSpiral.Count, F(2, 2f));
                if (Has("comb")) sacks.ExpFoldComb(-32, 32, SacksSpiral.Count, 20f, true);
                break;
            case "residue": sacks.ExpResiduePinch(I(2, 30), F(3, 1f), SacksSpiral.Count, Has("arms"), I(2, 30) <= 30 ? 22f : 0f, true); break;
            case "pleat": sacks.ExpPleat(I(2, 30), F(3, 1f), SacksSpiral.Count, .9f, 20f, Has("curves"), Has("marks")); break;
            case "residuelattice": sacks.ExpResidueLattice(c + new Vector3(6000f, 0f, 0f), 6f, SacksSpiral.Count, Has("even"), 9f); break;
            case "pinchlive": sacks.ExpPinchLive(A(2).ToLowerInvariant(), I(3, 30), Seconds(A(4), 8f), SacksSpiral.Count); break;
            // ---- the 144 modules (SacksGross.cs, SacksGrossLive.cs) ----
            case "turnstep":
            {
                List<long> ns = new();
                foreach (string t in A(2).Split(',')) if (long.TryParse(t.Trim(), out long v)) ns.Add(v);
                sacks.ExpTurnStepMarks(ns.ToArray(), F(3, 36f));
                break;
            }
            case "board": sacks.ExpTorusBoard(c + new Vector3(SacksSpiral.BoardOffset, 0f, 0f), 6f, 10000000, Has("diagonals"), Has("verticals"), 32f); break;
            case "boardfill": sacks.ExpBoardFillLive(c + new Vector3(SacksSpiral.BoardOffset, 0f, 0f), 6f, 10000000, Seconds(A(2), 8f), 32f); break;
            case "diagsweep":
            {
                List<int> ds = new();
                foreach (string t in A(2).Split(',')) if (int.TryParse(t.Trim(), out int v)) ds.Add(v);
                sacks.ExpDiagonalSweepLive(c + new Vector3(SacksSpiral.BoardOffset, 0f, 0f), 6f, ds.ToArray(), Seconds(A(3), 8f), 32f);
                break;
            }
            case "torusroll": sacks.ExpTorusRollLive(c + new Vector3(SacksSpiral.BoardOffset, 0f, 0f), 6f, 30f, Seconds(A(2), 8f)); break;
            case "classes": sacks.ExpTorusClassesOnSpiral(41, SacksSpiral.Count, F(2, 40f), new[] { 41, 17, 11, 5, 1, 3 }); break;
            case "charlive":
            {
                // exp:charlive:M:3|2|5:SECONDS[:slide][:rise][:BENDTO]
                float bendTo = 1f;
                for (int i = 5; i < p.Length; i++) if (float.TryParse(p[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float bt)) bendTo = bt;
                int m = I(2, 24);
                sacks.ExpCharacterLive(m, A(3), Seconds(A(4), 8f), Has("slide"), Has("rise"), bendTo, m <= 24 ? 40f : 0f);
                break;
            }
            case "fingerlive": sacks.ExpFingerprintLive(c + new Vector3(SacksSpiral.FingerOffset, 0f, 0f), 4.2f, A(2).ToLowerInvariant(), Seconds(A(3), 8f), 24f); break;
            case "fingerall": sacks.ExpFingerprintAll(c + new Vector3(SacksSpiral.FingerOffset, 0f, 0f), 26f); break;
            case "clocklive": sacks.ExpClockLive(4000000, Seconds(A(2), 8f), 34f); break;
            case "clock": sacks.ExpClock144(SacksSpiral.Count, 4000000, Has("labels") ? 34f : 0f); break;
            case "clockpulse": sacks.ExpClockPulse(A(2).ToLowerInvariant() != "orange"); break;
            case "quadglow": sacks.ExpQuadGlow(I(2, 1), I(3, 1), I(4, 41), new Color(.3f, 1f, .55f)); break;
            case "meet": sacks.ExpEulerLandauMeet(24f); break;
            // ---- mirrors for prime pairs, the sieve machine, twisted tori (SacksTwisted.cs) ----
            case "goldbach": sacks.ExpMirrorChords(A(2).ToLowerInvariant(), I(3, 10), I(4, 40), .3f, true, true); break;
            case "goldbachlive": sacks.ExpMirrorLive(A(2).ToLowerInvariant(), I(3, 10), I(4, 40), Seconds(A(5), 8f), .3f); break;
            case "goldbachsky": sacks.ExpMirrorSkyline(c + new Vector3(SacksSpiral.SkylineOffset, 0f, 0f), 11f, 44f, 34f, false); break;
            case "goldbachticks": sacks.ExpSkylineTicksPulse(); break;
            case "mobiusband": sacks.ExpMobiusBand(c + new Vector3(SacksSpiral.MobiusOffset, 0f, 0f), 30f, 12f, 0f); break;
            case "mobiuspoints": sacks.ExpMobiusPoints(c + new Vector3(SacksSpiral.MobiusOffset, 0f, 0f), 30f, 12f, A(2).ToLowerInvariant(), I(3, 10), I(4, 60)); break;
            case "mobiuslive": sacks.ExpMobiusLive(c + new Vector3(SacksSpiral.MobiusOffset, 0f, 0f), 30f, 12f, A(2).ToLowerInvariant(), I(3, 10), I(4, 60), Seconds(A(5), 8f), 0f); break;
            case "sievedial":
                // exp:sievedial:N:A1:SECONDS (SECONDS 0 holds the dial still at the first candidate)
                sacks.ExpLehmerLive(c + new Vector3(SacksSpiral.MachineOffset, 0f, 0f), I(2, 1501), I(3, 60), SieveMods, Seconds(A(4), 0f), 44f, false);
                break;
            case "dialring": sacks.ExpDialRingPulse(c + new Vector3(SacksSpiral.MachineOffset, 0f, 0f), I(2, 10)); break;
            case "noboard": SacksSpiral.ExpClearBoard(); break;
            // ---- the AA–AE modules (SacksIntersect.cs, SacksIntersectLive.cs) ----
            case "reveal":
                if (A(3).Contains(","))
                {
                    // explicit times, one per item: exp:reveal:GROUP:T1,T2,…[:own] (own: on a clock of its own, so several
                    // reveals can run at once; without it a new reveal stops the one before)
                    List<float> times = new();
                    foreach (string t in A(3).Split(','))
                        if (float.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out float v)) times.Add(v);
                    sacks.ExpRevealAt(A(2), times.ToArray(), Has("own"));
                }
                else sacks.ExpRevealLive(A(2), Seconds(A(3), 6f));
                break;
            case "strip": SacksSpiral.StripOff(); break;
            case "hide": sacks.ExpHideItems(A(2)); break;
            case "normbase": sacks.ExpNormBase(c + new Vector3(SacksSpiral.NormSurfaceOffset, 0f, 0f), 2.5f, 6f, .18f, 10f, 7f, 0f); break;
            case "normslice":
            {
                List<int> ids = new();
                foreach (string t in A(2).Split(',')) if (int.TryParse(t.Trim(), out int v)) ids.Add(v);
                sacks.ExpNormSlicesAdd(ids.ToArray());
                break;
            }
            case "levelset": sacks.ExpFactorLevelSets(c + new Vector3(SacksSpiral.NormSurfaceOffset + 70f * I(3, 0), -200f, 0f), 30f, I(2, 13), 0f); break;
            case "ringlattice":
            {
                // exp:ringlattice:N:XOFFSET — a prime's ring at 4 units per lattice step; a big number's ring at radius 20
                long n = I(2, 13);
                bool big = n > 100;
                sacks.ExpRingLattice(c + new Vector3(SacksSpiral.RingLatticeOffset + F(3, 0f), 0f, 0f), big ? 20f / Mathf.Sqrt(n) : 4f, n, 0f, big ? 1.3f : 0f);
                break;
            }
            case "eulerlattice": sacks.ExpEulerLattice(c + new Vector3(SacksSpiral.RingLatticeOffset, -300f, 0f), 1f, 40, 0f, true); break;
            case "residuetorus":
                // exp:residuetorus:COUNT:CELL[:OVERLAYS separated by ;]
                sacks.ExpResidueTorusMatrix(c + new Vector3(SacksSpiral.ResidueTorusOffset, 0f, 0f), F(3, .5f), 1000000, I(2, 12), A(4).Replace(';', ','), I(2, 12) > 6 ? 64f : 40f);
                break;
            case "residuelive": sacks.ExpResidueOverlayLive(A(2).ToLowerInvariant(), Seconds(A(3), 8f)); break;
            case "modular":
                // exp:modular:UNIT:U:V:CMAX:LABELSIZE:ARCMAX
                sacks.ExpModularPlane(c + new Vector3(SacksSpiral.ModularOffset, 0f, 0f), F(2, 12f), F(3, 3.6f), F(4, 7.2f), I(5, 49), true, F(6, 0f), I(7, 24));
                break;
            case "race":
            {
                // exp:race:mod4|mod12:WIDTH:HEIGHT:LABELSIZE[:M1,M2,…]
                List<double> ms = new();
                foreach (string t in A(6).Split(',')) if (double.TryParse(t.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double v)) ms.Add(v);
                sacks.ExpRaceChart(c + new Vector3(SacksSpiral.RaceOffset, 0f, 0f), F(3, 92f), F(4, 110f), F(5, 70f), A(2).ToLowerInvariant() == "mod4", true, ms.Count > 0 ? ms.ToArray() : null, false);
                break;
            }
            case "racelive":
            {
                // exp:racelive:SECONDS[:M1,M2,…]
                List<double> ms = new();
                foreach (string t in A(3).Split(',')) if (double.TryParse(t.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double v)) ms.Add(v);
                sacks.ExpRaceLive(Seconds(A(2), 8f), ms.ToArray());
                break;
            }
            // ---- the E8 drop module (SacksE8Drop.cs) ----
            // exp:e8wheel:LABELSIZE[:NMAX[:FROM[:TO]]] — the numbers 1 … NMAX on a spiral from radius FROM to TO
            // exp:e8wheel:LABELSIZE[:NMAX[:FROM[:TO[:X,Y,Z[:SCALE]]]]] — the numbers 1 … NMAX on a spiral from radius FROM to TO, at
            // X,Y,Z if given, the roots at SCALE (8)
            case "e8wheel":
            {
                float sc = F(7, 8f);
                sacks.ExpE8Wheel(c + (A(6).Length > 0 ? ParseVector(A(6)) : new Vector3(SacksSpiral.E8DropOffset, 0f, 0f)), sc, I(3, 600), F(4, 15f), F(5, 33f), .55f * sc / 8f, F(2, 66f),
                    new long[] { 49, 77, 91, 119, 121, 133 });
                break;
            }
            case "e8walk":
            {
                // exp:e8walk:SECONDS[:T1,T2,…] — the walk once round, the eight hours lit at T1 … T8 (seconds from the start)
                List<float> times = new();
                foreach (string t in A(3).Split(',')) if (float.TryParse(t.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float v)) times.Add(v);
                sacks.ExpE8WalkLive(Seconds(A(2), 8f), times.Count > 0 ? times.ToArray() : null);
                break;
            }
            // exp:e8tower:LABELSIZE[:attach[:GAP]] — attached, the shells stack under the wheel (floor 1 is the wheel)
            case "e8tower":
                sacks.ExpE8Tower(c + new Vector3(SacksSpiral.E8DropOffset + 300f, 0f, 0f), 7, 30f, Has("attach") ? F(4, 34f) : 34f, 1f, F(2, 80f),
                    new[] { "E8 tower A", "E8 tower B", "E8 tower C" }, new[] { 3, 5 }, Has("attach"));
                break;
            // exp:e8cubesat:N:DX,DY,DZ:VOXEL:GAP:DOT:LABEL — the cubes of n's divisors, placed from the wheel's centre
            case "e8cubesat":
                sacks.ExpDivisorCubes(sacks.E8WheelCenter + ParseVector(A(3)), I(2, 6), F(4, 2.2f), F(5, 3f), F(6, .5f), F(7, 75f));
                break;
            case "e8cubes":
                // exp:e8cubes:N:X:Z:VOXEL:GAP:DOT:LABEL — the cubes of n's divisors, based at (offset + 600 + X, 0, Z)
                sacks.ExpDivisorCubes(c + new Vector3(SacksSpiral.E8DropOffset + 600f + F(3, 0f), 0f, F(4, 0f)), I(2, 6), F(5, 2.2f), F(6, 3f), F(7, .5f), F(8, 75f));
                break;
            case "e8solve": sacks.ExpE8SolveBoard(I(2, 35)); break;
            // exp:e8hl:N1,N2[:spoke] — light numbers on the wheel's spiral · exp:e8unhl
            case "e8hl":
            {
                List<long> ns = new();
                foreach (string one in A(2).Split(',')) if (long.TryParse(one.Trim(), out long v)) ns.Add(v);
                sacks.ExpE8Light(ns.ToArray(), Has("spoke"));
                break;
            }
            case "e8unhl": sacks.ExpE8Unlight(); break;
            case "e8close": SacksSpiral.ExpE8CloseBoard(); break;
            // ---- the modules "E5 to E8, from mirrors" and "The paths back" (SacksEConstruction.cs) ----
            case "mirrorscene": sacks.ExpMirrorScene(c + new Vector3(SacksSpiral.MirrorOffset, 0f, 0f), 2.4f, .6f, F(2, 120f)); break;
            case "dihedral":
                // exp:dihedral:M:X:Y — two mirrors at 180°/M, centred at (DihedralOffset + X, Y)
                sacks.ExpDihedralScene(c + new Vector3(SacksSpiral.DihedralOffset + F(3, 0f), F(4, 0f), 0f), I(2, 3), 2.3f, .6f, 120f);
                break;
            case "dynkin": sacks.ExpDynkinE(c + new Vector3(SacksSpiral.DynkinOffset, 0f, 0f), 7f, 17f, .6f, 125f); break;
            case "coxpanels": sacks.ExpCoxeterPanels(c + new Vector3(SacksSpiral.CoxPanelsOffset, 0f, 0f), new[] { 5, 6, 7, 8 }, 2, 50f, 62f, 20f, .5f, 125f); break;
            case "coxwalk": sacks.ExpCoxeterWalkPanels(Seconds(A(2), 8f)); break;
            case "estack": sacks.ExpEStackModule(c + new Vector3(SacksSpiral.EStackOffset, 0f, 0f), 125f); break;
            case "fcolumns": sacks.ExpFactorColumnsLive(c + new Vector3(SacksSpiral.FactorFunnelOffset, 0f, 0f), 400, 2.6f, 5f, .55f, 140f, 20); break;
            case "stair":
            {
                // exp:stair:N[:a,b,…] — n's path back, split into reveal groups "stair N #k" of a, b, … items
                List<int> sp = new();
                foreach (string t in A(3).Split(',')) if (int.TryParse(t.Trim(), out int v)) sp.Add(v);
                sacks.ExpFactorStairLive(c + new Vector3(SacksSpiral.FactorFunnelOffset, 0f, 0f), I(2, 360), 2.6f, 5f, .55f, 140f, sp.Count > 0 ? sp.ToArray() : null);
                break;
            }
            case "descent": sacks.ExpDescentRows(c + new Vector3(SacksSpiral.FactorFunnelOffset + 600f, 0f, 0f), 105f); break;
            // the zeros module (xzeros): the funnel with its layers hidden, the eight trees, the paths, the spacings
            case "zfunnel": sacks.ExpZeroModuleFunnel(c + new Vector3(SacksSpiral.ZeroModuleOffset, 0f, 0f), 150f); break;
            case "ztrees":
                // "wide": all eight trees in one row, for a horizontal picture
                // (an X,Y,Z argument draws them there instead, e.g. nearer the funnel so the camera can glide over)
                sacks.ExpCharacterTrees(c + (A(3).Contains(",") ? ParseVector(A(3)) : A(2).Contains(",") ? ParseVector(A(2)) : new Vector3(SacksSpiral.ZeroModuleOffset + 600f, 0f, 0f)),
                    34f, 100, 15f, 3.4f, .55f, 150f, Vector3.up, new Vector3(.94f, -.342f, 0f), Has("wide") ? 1 : 2, 100f);
                sacks.ExpHideItems("JC trees");
                break;
            case "zpaths":
                sacks.ExpPathLampChart(c + new Vector3(SacksSpiral.ZeroModuleOffset + 1150f, 60f, 0f), new long[] { 360, 343, 397 }, 15.5f, 9f, 22f, .55f, 215f, true, 82f);
                sacks.ExpHideItems("JE path lamps");
                break;
            case "zspacing": sacks.ExpZeroSpacing(c + new Vector3(SacksSpiral.ZeroModuleOffset + 1800f, 0f, 0f), 200f, true); break;
            case "show": sacks.ExpShowItems(A(2)); break;
            // callouts and isolation (the long-form segments): exp:callnum:N:DX,DY[,FRAC[,RRGGBB]]:TEXT (a number on the
            // surface) · exp:callat:X,Y,Z:DX,DY[,…]:TEXT (a point, from the centre) · exp:callfun:N:DX,DY[,…]:TEXT (the top of
            // n's column in the zeros module's funnel) · exp:uncall · exp:focus:G1,G2 · exp:unfocus · exp:pulse:G1,G2[:SECONDS[:PEAK]]
            // · exp:drawon:G1,G2:SECONDS|line
            case "callnum":
            case "callat":
            case "callfun":
            case "callwave":
            case "callbridge":
            case "callfold":
            case "calle8":
            case "callchain":
            case "callzeta":
            case "callblock":
            case "callhand":
            case "calltooth":
            case "calllat":
            case "callrace":
            case "callpath":
            case "callpathzeta":
            case "callfin":
            case "callsq":
            case "callsqsq":
            case "callnw":
            case "callclock":
            case "callclockend":
            case "callclocklink":
            case "callbar":
            case "callwheel":
            case "callvertex":
            {
                string[] o = A(3).Split(',');
                float Of(int i, float d) => o.Length > i && float.TryParse(o[i], NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : d;
                Vector2 dir = new(Of(0, 1f), Of(1, 1f)); // 0,0: away from the middle of the picture
                Color col = o.Length > 3 && ColorUtility.TryParseHtmlString("#" + o[3].Trim(), out Color parsed) ? parsed : new Color(1f, .86f, .45f);
                string text = string.Join(":", p, 4, Mathf.Max(0, p.Length - 4));
                // a number, or N@KIND for the wave KIND (callwave)
                string[] nk = A(2).Split('@');
                double num = double.TryParse(nk[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double nv) ? nv : 1;
                Vector3 target = verb switch
                {
                    "callnum" => sacks.ExpNumberPosition(num, .5f),
                    "callfun" => sacks.ZeroFunnelTop((long)num),
                    "callwave" => sacks.NumberWaveTop(num, nk.Length > 1 ? nk[1] : ""),
                    "callbridge" => sacks.FunnelTop(sacks.BridgeFunnelOrigin, (long)num, 4f, 8f),
                    "callfold" => sacks.FoldPosition(num),
                    "calle8" => sacks.E8SpiralPosition((long)num),
                    "callchain" => sacks.BridgeLinkMid((int)num),
                    "callzeta" => sacks.ZetaCurvePoint(Gamma(nk[0])),
                    "callnw" => sacks.NwPoint(num, nk.Length > 1 ? nk[1] : "p"),
                    "callpath" => sacks.PathVertex((int)num),
                    "callfin" => sacks.FinVertex((int)num),
                    "callsq" => sacks.SqFinTip((int)num),
                    "callsqsq" => sacks.SqSquareMiddle((int)num),
                    "callhand" => sacks.FwHandTip((long)num),
                    "calltooth" => sacks.FwToothTip((long)num),
                    "callrace" => sacks.RacePoint(ParseVector(A(2)).x, ParseVector(A(2)).y),
                    "calllat" => sacks.LatticePoint(Mathf.RoundToInt(ParseVector(A(2)).x), Mathf.RoundToInt(ParseVector(A(2)).y), Mathf.RoundToInt(ParseVector(A(2)).z)),
                    "callblock" => sacks.TraceBlockAt((int)num),
                    "callpathzeta" => sacks.PathZeta(),
                    "callclock" => sacks.ClockPosition((int)num),
                    "callclockend" => sacks.ClockChainEnd(Gamma(nk[0])),
                    "callclocklink" => sacks.ClockLinkMid((int)num),
                    "callbar" => sacks.CoxeterBarTop((int)num),
                    "callwheel" => sacks.CoxeterWheelTrace((int)num),
                    "callvertex" => sacks.LiveChainVertex((int)num),
                    _ => c + ParseVector(A(2)),
                };
                sacks.ExpCallout(target, dir, text, col, Of(2, .12f));
                break;
            }
            case "uncall": sacks.ExpClearCallouts(); break;
            // stop and remove the live scene only (the groups and the callouts stay)
            case "clearlive": sacks.ExpStopLive(); break;
            // the long-form segments' scenes
            // exp:nwave:prime|divisor|term:TERMS:HEIGHT:XMAX[:DX] (term: Voronoi's wave number TERMS alone; DX shifts it along x)
            // · exp:nwavelive:TERMS:HEIGHT:XMAX:SECONDS[:HOLD] (Voronoi's waves added live, the first alone for HOLD seconds)
            // · exp:divisors:N[@KIND][:RRGGBB[:SECONDS]] (n's divisors marked on the wave)
            case "nwave": sacks.ExpNumberWave(A(2).ToLowerInvariant(), I(3, 4000), F(4, 6f), F(5, 150f), new Vector3(F(6, 0f), 0f, 0f)); break;
            case "nwavelive": sacks.ExpNumberWaveLive(I(2, 4000), F(3, 6f), F(4, 150f), Seconds(A(5), 12f), F(6, 3f)); break;
            case "numbers": sacks.ExpSpiralNumbers(I(2, 150)); break;
            // the number line that winds into the Sacks spiral (SacksNumberWorld.cs); its parameters are eased with exp:tw
            case "nwinit": sacks.ExpNumberWorld(); break;
            // discovery four, step by step (SacksClassWaves.cs): exp:lacounts:X,Y,Z · exp:lwaves:X,Y,Z · exp:lratio:SECONDS ·
            // exp:t5terrain:X,Y,Z[:CELLS] (lf.build, lf.h, lf.slice eased with exp:tw)
            case "lacounts": sacks.ExpClassCountTable(c + ParseVector(A(2)), 220f); break;
            // discovery five, step by step (SacksS3Count.cs): exp:hoursquares:SECONDS · exp:powerwalk:P:MAXPOW:SECONDS ·
            // exp:modp:P:X,Y,Z:RADIUS · exp:modptest:P:SECONDS · exp:s3anim:NAME:SECONDS (NAME identity|rotation|reflection|square)
            case "hoursquares": sacks.ExpHourSquares(Seconds(A(2), 6f), 220f); break;
            // E8's wheel onto a block of the gold path (no arrow turns) and back: exp:tboverlay:K:SECONDS · exp:wheelhome:SECONDS
            case "tboverlay": sacks.ExpTraceOverlay(I(2, 15), Seconds(A(3), 4f)); break;
            // the vortices on the number line (SacksVortexLine.cs): exp:vlinit takes the path as it is; then exp:tw on
            // vl.unwind, vl.marks, vl.fold, vl.merge, vl.curl, vl.vis
            case "vlinit": sacks.ExpVortexLine(); break;
            case "wheelhome": sacks.ExpWheelHome(Seconds(A(2), 3f)); break;
            // exp:e8links:MAXN:GAP — the divisor links beside E8's tower · exp:eyelimits:SECONDS — why the eyes thin out at 1.71
            case "e8links": sacks.ExpE8Links(I(2, 7), F(3, 16f), 220f); break;
            case "eyelimits": sacks.ExpEyeLimits(Seconds(A(2), 8f), 220f); break;
            case "powerwalk": sacks.ExpPowerWalk(I(2, 7), I(3, 4), Seconds(A(4), 6f), 220f); break;
            case "modp": sacks.ExpRootsModP(c + ParseVector(A(3)), I(2, 5), F(4, 20f), 220f); break;
            case "modptest": sacks.ExpRootsTest(I(2, 5), Seconds(A(3), 4f), 220f); break;
            case "modpshow": sacks.ExpRootsShow(I(2, 5)); break;
            case "s3anim": sacks.ExpS3Animate("S3 " + A(2).ToLowerInvariant(), Seconds(A(3), 3f)); break;
            case "lwaves": sacks.ExpClassWaves(c + ParseVector(A(2)), 220f); break;
            case "lratio": sacks.ExpClassRatioLive(Seconds(A(2), 10f), 220f); break;
            case "t5terrain": sacks.ExpClassTerrain(c + ParseVector(A(2)), 124f, I(3, 280), 220f); break;
            // the teaser: exp:home:TMAX:SECONDS
            case "home": ExpHomeTeaser(F(2, 300f), Seconds(A(3), 15f)); break;
            // the terms entering the master waves (SacksNumberTerms.cs): exp:nwrows:p|d:COUNT:GAP:AMP:LABEL[:FIRST] ·
            // exp:nwenter:K:SECONDS · exp:nwrowsclear · exp:nwangle:on|off[:LABEL] · exp:nwradius:N:LABEL:TEXT
            case "nwrows": sacks.ExpNwRows(A(2).ToLowerInvariant(), I(3, 3), F(4, 10f), F(5, 3.5f), F(6, 4f), I(7, 1)); break;
            case "nwenter": sacks.ExpNwEnter(I(2, 1), Seconds(A(3), 3f)); break;
            case "nwrowsclear": sacks.ExpNwRowsClear(); break;
            case "nwangle": sacks.ExpNwAngle(A(2).ToLowerInvariant() != "off", F(3, 4f)); break;
            case "nwquarters": sacks.ExpNwQuarters(A(2).ToLowerInvariant() != "off", F(3, 30f)); break;
            case "nwradius": sacks.ExpNwRadius(F(2, 100f), string.Join(":", p, 4, Mathf.Max(0, p.Length - 4)), F(3, 4f)); break;
            case "nwflare":
            {
                string[] nk = A(2).Split('@');
                foreach (string one in nk[0].Split(','))
                    if (double.TryParse(one, NumberStyles.Float, CultureInfo.InvariantCulture, out double fv)) sacks.ExpNwFlare(fv, nk.Length > 1 ? nk[1] : "p");
                break;
            }
            // the partial-sum path at a fixed scale (SacksPathWorld.cs); its parameters are eased with exp:tw
            case "pwinit": sacks.ExpPathWorld(); break;
            // the Sacks spiral with every number's square, its fin turned by the zeros (SacksSquareWorld.cs); parameters sq.*
            case "sqinit": sacks.ExpSquareWorld(); break;
            // the coil assembled from the path's arrows over the zeros module's funnel (SacksFunnelWorld.cs)
            case "fwinit": sacks.ExpFunnelWorld(); break;
            // chart transforms (ExperimentMorphs.cs): exp:twgroup:GROUP:DX,DY,DZ:SCALE:SECONDS · exp:racemorph:SECONDS ·
            // exp:familymorph:SECONDS · exp:stripsmorph:SX,SY,SZ:CX,CY,CZ:SECONDS
            case "twgroup": sacks.ExpGroupMove(A(2), ParseVector(A(3)), F(4, 1f), Seconds(A(5), 2f)); break;
            case "racemorph": sacks.ExpRaceMorph(Seconds(A(2), 4f)); break;
            case "familymorph": sacks.ExpFamilyMorph(Seconds(A(2), 4f)); break;
            case "stripsmorph": sacks.ExpStripsMorph(c + ParseVector(A(2)), c + ParseVector(A(3)), Seconds(A(4), 5f)); break;
            // E8's clock inside the gold path (SacksTraceBlocks.cs): exp:tbblock:K · exp:tblift:K:SECONDS · exp:tbscale ·
            // exp:tbbars:X,Y,Z · exp:tbbarsall[:measured] · exp:tbsweep:K0:K1:SECONDS · exp:tbsieve:N:SECONDS
            case "tbblock": sacks.ExpTraceBlock(I(2, 15)); break;
            case "tblift": sacks.ExpTraceLift(I(2, 15), Seconds(A(3), 3f)); break;
            case "tbscale": sacks.ExpTraceScale(F(2, 2.2f)); break;
            case "tbbars": sacks.ExpTraceBars(c + ParseVector(A(2)), F(3, 2f)); break;
            case "tbbarsall": sacks.ExpTraceBarsAll(Has("measured")); break;
            case "tbsweep": sacks.ExpTraceSweep(F(2, 1f), F(3, 30f), Seconds(A(4), 10f)); break;
            case "tbsieve": sacks.ExpTraceSieve(I(2, 7), Seconds(A(3), 5f)); break;
            case "fwhl":
                foreach (string one in A(2).Split(','))
                    if (long.TryParse(one.Trim(), out long hn)) sacks.ExpFunnelLight(hn, Has("logs"));
                break;
            case "fwunhl": sacks.ExpFunnelUnlight(long.TryParse(A(2), out long un) ? un : 0); break;
            case "fwangles": sacks.ExpFunnelAngles(long.TryParse(A(2), out long an) ? an : 6); break;
            case "fwchars": sacks.ExpFunnelChars(long.TryParse(A(2), out long cn) ? cn : 7); break;
            case "fwturn": sacks.ExpFunnelTurn(long.TryParse(A(2), out long tn) ? tn : 7, Seconds(A(3), 6f)); break;
            case "nwdivisors":
            {
                string[] nk = A(2).Split('@');
                long dn = long.TryParse(nk[0], out long dv2) ? dv2 : 12;
                string lane = nk.Length > 1 ? nk[1] : "d";
                Color dc = ColorUtility.TryParseHtmlString("#" + A(3).Trim(), out Color dcol2) ? dcol2 : new Color(1f, .86f, .45f);
                sacks.ExpMarkDivisorsAt(dn, d => sacks.NwPoint(d, lane), dc, F(4, 1.6f));
                break;
            }
            case "divisors":
            {
                string[] nk = A(2).Split('@');
                long dn = long.TryParse(nk[0], out long dv) ? dv : 12;
                Color dc = ColorUtility.TryParseHtmlString("#" + A(3).Trim(), out Color dcol) ? dcol : new Color(1f, .86f, .45f);
                sacks.ExpMarkDivisors(dn, nk.Length > 1 ? nk[1] : "", dc, F(4, 1.6f));
                break;
            }
            // the zeta curve: exp:zplane · exp:zcurve:T1 · exp:zcurvelive:T0:T1:SECONDS[:chain] · exp:zchain:T:SECONDS
            // (the last plain number among the extra arguments, if any, is the label size)
            case "zplane": sacks.ExpZetaPlane(F(2, 200f)); break;
            case "zcurve": sacks.ExpZetaCurve(F(2, 50f)); break;
            case "zcurvelive": sacks.ExpZetaCurveLive(F(2, 0f), F(3, 50f), Seconds(A(4), 12f), LastNumber(5, 200f), Has("chain")); break;
            case "zchain": sacks.ExpZetaChainBuild(Gamma(A(2)), Seconds(A(3), 8f), LastNumber(4, 90f)); break;
            // the bridge: exp:zbridge:GAMMA · exp:zbridgechain:GAMMA:N · exp:zbridgelive:GAMMA:FROM:TO:SECONDS (GAMMA a number or zK)
            case "zbridge": sacks.ExpZeroBridge(Gamma(A(2)), 170f); break;
            case "zbridgechain": sacks.ExpBridgeChain(Gamma(A(2)), I(3, 30)); break;
            // the bridge as a row of clocks: exp:zclocks:N (columns and faces) · exp:zclockhands:GAMMA[:RRGGBB[:chain]] (every
            // hand at once) · exp:zclockslive:GAMMA:FROM:TO:SECONDS[:tail]
            case "zclocks": sacks.ExpBridgeClocks(I(2, 8), 150f); break;
            case "zclockhands":
                sacks.ExpBridgeClockHands(Gamma(A(2)), ColorUtility.TryParseHtmlString("#" + A(3).Trim(), out Color hc) ? hc : new Color(1f, .85f, .3f), 150f, Has("chain"));
                break;
            case "zclockslive": sacks.ExpBridgeClocksLive(Gamma(A(2)), I(3, 1), I(4, 8), Seconds(A(5), 12f), 150f, Has("tail")); break;
            // a second zero's coil for comparison: exp:zbridgecoil:GAMMA:GROUP[:RRGGBB]
            case "zbridgecoil":
                sacks.ExpBridgeCoil(Gamma(A(2)), A(3).Length > 0 ? A(3) : "ZB coil 2",
                    ColorUtility.TryParseHtmlString("#" + A(4).Trim(), out Color bc) ? bc : new Color(.4f, .85f, 1f));
                break;
            case "zbridgelive": sacks.ExpBridgeLive(Gamma(A(2)), I(3, 1), I(4, 12), Seconds(A(5), 12f), 170f); break;
            // the triangle of x³ − x − 1's roots and how a prime acts on it
            // exp:s3roots[:X,Y,Z[:GAP]] — GAP 0 puts the four panels on top of each other (shown one at a time)
            case "s3roots": sacks.ExpS3Roots(c + Where(new Vector3(SacksSpiral.FamilyRaceOffset + 600f, 0f, 0f)), 18f, F(3, 92f), 260f); break;
            // LE's chart with a sweeping height: exp:mixsweep:LAM0:LAM1:SECONDS
            // exp:mixsweep:LAM0:LAM1:SECONDS[:X,Y,Z]
            case "mixsweep":
                sacks.ExpMixtureSweepLive(c + (A(5).Contains(",") ? ParseVector(A(5)) : new Vector3(SacksSpiral.ClassGroupOffset + 2400f, 0f, 0f)), F(2, -3f), F(3, 4f), Seconds(A(4), 10f), 220f);
                break;
            // exp:hidechild:GROUP:PREFIX · exp:showchild:GROUP:PREFIX — one part of a group (e.g. the arcs of one hour)
            // (the prefix is the rest of the action, colons and spaces kept: "TextBox: 1", "trace ")
            // exp:clearchild:PREFIX — remove the experiment groups whose names start with PREFIX
            case "clearchild": sacks.ExpRemoveGroups(A(2)); break;
            case "hidechild": sacks.ExpSetChildren(A(2), string.Join(":", p, 3, Mathf.Max(0, p.Length - 3)), false); break;
            case "showchild": sacks.ExpSetChildren(A(2), string.Join(":", p, 3, Mathf.Max(0, p.Length - 3)), true); break;
            case "focus": sacks.ExpFocus(A(2).Split(','), F(3, .16f)); break;
            // exp:mute:G1,G2[:SECONDS] · exp:unmute:G1,G2[:SECONDS] — grey groups out (and back), for isolating one part
            // exp:mute:G1,G2[:SECONDS[:DEPTH]] — grey the groups out; DEPTH up to 2 fades them nearly out
            case "mute": sacks.ExpMute(A(2).Split(','), true, F(3, .8f), F(4, 1f)); break;
            case "unmute": sacks.ExpMute(A(2).Split(','), false, F(3, .8f)); break;
            // exp:tw:KEY:TO:SECONDS[:linear|geo] — ease a scene parameter
            case "tw": sacks.TwTo(A(2), F(3, 0f), Seconds(A(4), 1f), Has("linear") ? 0 : Has("geo") ? 2 : 1); break;
            case "unfocus": sacks.ExpUnfocus(); break;
            case "pulse": sacks.ExpPulse(A(2).Split(','), F(3, 1.6f), F(4, 2.2f)); break;
            case "drawon": sacks.ExpDrawOn(A(2).Split(','), Seconds(A(3), 3f), Has("seq")); break;
            // the five discoveries (xtop5): the stills of sections IA, KB, LC, LE, LF, MA, MB and MC, where their renders put them
            // exp:t5charges[:X,Y,Z] — section IA's picture, at its own place or at X,Y,Z (from the centre)
            case "t5charges": sacks.ExpCoxeterTraceCharges(c + (A(2).Length > 0 ? ParseVector(A(2)) : new Vector3(SacksSpiral.LieOffset, 0f, 0f)), 170f); break;
            case "t5lattices": sacks.ExpClassLattices(c + new Vector3(SacksSpiral.ClassGroupOffset, 0f, 0f), 220f); break;
            // (each of these takes an optional X,Y,Z, from the centre, to draw it somewhere else: next to the chart before it)
            case "t5e8land": sacks.ExpE8Landscape(c + Where(new Vector3(SacksSpiral.LatticeZetaOffset + 600f, 0f, 0f)), 170f); break;
            case "t5families": sacks.ExpClassZeros(c + Where(new Vector3(SacksSpiral.ClassGroupOffset + 1200f, 0f, 0f)), 220f); break;
            case "t5mix": sacks.ExpClassMixtures(c + Where(new Vector3(SacksSpiral.ClassGroupOffset + 2400f, 0f, 0f)), 220f); break;
            case "t5plane": sacks.ExpClassLandscape(c + Where(new Vector3(SacksSpiral.ClassFiveOffset + 600f, -10f, 0f)), 124f, 0f, 280, 220f, false); break;
            case "t5race": sacks.ExpSpokeRace(c + Where(new Vector3(SacksSpiral.SpokeRaceOffset, 0f, 0f)), 220f); break;
            case "t5fold":
            {
                Vector3 fo = Where(new Vector3(SacksSpiral.SpokeRaceOffset + 600f, 0f, 0f));
                sacks.ExpSquareFold(c + fo + new Vector3(-70f, -8f, 0f), 60f, 220f);
                sacks.ExpFairRace(c + fo + new Vector3(82f, -12f, 0f), 130f, 3.6f, 220f);
                break;
            }
            // exp:t5family[:X,Y,Z[:FX,FY,FZ[:RIM[:RACEW]]]] — the fold of S3 at FX,FY,FZ (radius RIM) instead of beside the race,
            // and the race RACEW wide (120)
            case "t5family":
                sacks.ExpFamilyRace(c + Where(new Vector3(SacksSpiral.FamilyRaceOffset, 0f, 0f)), 220f,
                    A(3).Contains(",") ? c + ParseVector(A(3)) : null, F(4, 46f), F(5, 120f));
                break;
            case "t5lattice3": sacks.ExpClassLattices(c + Where(new Vector3(SacksSpiral.ClassGroupOffset, 0f, 0f)), 220f); break;
            case "say":
            {
                // split at the first bar only: a subtitle may hold bars of its own (|1 + 2^-1/2|)
                string[] parts = string.Join(":", p, 2, p.Length - 2).Split(new[] { '|' }, 2);
                SacksSpiral.ExpSay(parts[0], parts.Length > 1 ? parts[1] : null);
                break;
            }
            case "dialholes": sacks.ExpDialHolesPulse(); break;
            case "sieveboard": sacks.ExpSieveBoard(c + new Vector3(SacksSpiral.MachineOffset + 160f, 0f, 0f), I(2, 50399), I(3, 225), I(4, 320), SieveMods, 2.2f, 36f, I(5, 48)); break;
            case "knot": sacks.ExpTorusKnot(c + new Vector3(SacksSpiral.KnotOffset, 0f, 0f), 30f, 12f, 5, 7, 1000000, Has("euler"), 48f, false); break;
            case "knotwalk":
                // exp:knotwalk:N0:N1:SECONDS
                sacks.ExpKnotWalkLive(c + new Vector3(SacksSpiral.KnotOffset, 0f, 0f), 30f, 12f, 5, 7, I(3, 35), Seconds(A(4), 8f), 0f, I(2, 1));
                break;
            case "umbilic":
            {
                // exp:umbilic:TWIST[:C1,C2,…]
                List<long> cs = new();
                foreach (string t in A(3).Split(',')) if (long.TryParse(t.Trim(), out long v)) cs.Add(v);
                sacks.ExpUmbilic(c + new Vector3(SacksSpiral.UmbilicOffset, 0f, 0f), 4f, F(2, 1f), 40, cs.ToArray(), 0, 0f);
                break;
            }
            case "umbiliclive": sacks.ExpUmbilicLive(c + new Vector3(SacksSpiral.UmbilicOffset, 0f, 0f), 4f, F(2, 1f), 40, Seconds(A(3), 8f), 0f); break;
            case "umbiliccurve": sacks.ExpUmbilicCurve(c + new Vector3(SacksSpiral.UmbilicOffset, 0f, 0f), 4f, F(3, 1f), I(2, 1), 40, Has("flare")); break;
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
