using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// "The zeros inside E8's stack" (sections JA–JF) as a model to walk around: the factor funnel of E8's wheel for n ≤ 600 with
///   coils   the first zeros of ζ as helices up the axis: at height log n the coil of γ has turned γ·log n
///   phases  where all of ζ's zeros point at the picked column's height: they lean to −1 at a prime power (Landau)
///   comb    ζ's zeros (and its pole) summed at every height, drawn out to one side: a tooth of log p at each prime power,
///           scaled so a tooth of log p reaches the top of p's column; nothing at the composites
///   rings   each tooth spun round the axis: a ring through the top of that column — the height, not the hour
///   fans    the eight L-functions of Q(ζ30) turned back by χ(h) and added: rays from the axis to the prime columns on spoke h
///   lamps   the sum at every column, lit as its top piece: one piece of log p on each prime power, none elsewhere
/// The number of ζ's zeros can be lowered (10 500, 1 000, 100, 30) to watch the teeth blur. The sums run on worker threads.
/// </summary>
public class ZerosModel : LabModel
{
    public const int Count = 600;
    const float S = 2.4f, Hs = 6f;
    static readonly int[] ZeroCounts = { 10500, 1000, 100, 30 };
    static readonly Color[] CoilColors = { new(1f, .85f, .1f), new(.35f, .85f, 1f), new(1f, .45f, .8f) };

    FunnelView funnel;
    Transform coilRoot, combRoot, fanRoot, phaseRoot;
    SegmentMesh rings, lamps, phaseBars;
    LineRenderer comb, profile, phaseMean;
    readonly LineRenderer[] fans = new LineRenderer[8];
    PointMesh fanTips;
    TextBox phaseLabel;
    readonly List<TextBox> hourLabels = new();

    bool showCoils = true, showComb = true, showRings, showFans, showLamps, showPhases = true;
    int spoke = -1, zeroLevel;
    Task<CombData> combJob;
    Task<FanData> fanJob;
    CombData combData;
    FanData fanData;
    Vector3 side;

    sealed class CombData
    {
        public int Zeros;
        public double Sigma;
        public double[] Z, W;
        public double[] Lamp = new double[Count + 1];
    }

    sealed class FanData
    {
        public double[] Z;
        public double[][] W = new double[8][];
        public readonly Dictionary<int, double> Tip = new();
    }

    public override string Id => "zeros";
    public override string Title => "The zeros in the stack";
    public override string Short => "Zeros";
    public override VRIcons.Id Icon => VRIcons.Id.Zeros;
    public FunnelView Funnel => funnel;
    /// <summary>Both sums are in (the test waits for this).</summary>
    public bool Ready => combData != null && fanData != null && combData.Zeros == ZeroCounts[zeroLevel];
    public int ZeroCount => ZeroCounts[zeroLevel];

    protected override void OnBuild()
    {
        funnel = new FunnelView(Root, O, Count, S, Hs);
        funnel.SetDim(.35f);
        funnel.Stepped += lit =>
        {
            Lab.SetStrip(funnel.Selected, lit);
            UpdateStatus();
        };
        side = new Vector3(Mathf.Cos(28f * Mathf.Deg2Rad), Mathf.Sin(28f * Mathf.Deg2Rad), 0f);
        coilRoot = Group("coils");
        combRoot = Group("comb");
        fanRoot = Group("fans");
        phaseRoot = Group("phases");
        rings = new SegmentMesh(Root, "rings", Factory.SoftGlowIntensity);
        lamps = new SegmentMesh(Root, "lamps", Factory.GlowIntensity);
        phaseBars = new SegmentMesh(phaseRoot, "phase bars", Factory.SoftGlowIntensity);
        fanTips = new PointMesh(fanRoot, "fan tips", Factory.GlowIntensity);
        BuildCoils();
        foreach (int h in ZeroSums.Gold)
        {
            Vector3 u = HourDir(h);
            hourLabels.Add(LabDraw.Label(fanRoot, h.ToString(), O + u * (funnel.Rim * 1.1f) + funnel.Up(Mathf.Log(Count)), 34f, HourColor(h)));
        }

        Controls.Add(LabControl.Toggle("Coils", VRIcons.Id.Zeros, () => showCoils, () => Set(ref showCoils), "C"));
        Controls.Add(LabControl.Toggle("Comb", VRIcons.Id.Comb, () => showComb, () => Set(ref showComb), "Z"));
        Controls.Add(LabControl.Toggle("Rings", VRIcons.Id.Torus, () => showRings, () => Set(ref showRings), "R"));
        Controls.Add(LabControl.Toggle("E8 fans", VRIcons.Id.E8, () => showFans, () => Set(ref showFans), "F"));
        Controls.Add(new LabControl
        {
            Label = "Spoke", Icon = VRIcons.Id.Dial, Clicked = CycleSpoke, On = () => spoke >= 0,
            DynamicLabel = () => spoke < 0 ? "Spokes: all" : $"Spoke {ZeroSums.Gold[spoke]}", Keys = "S"
        });
        Controls.Add(LabControl.Toggle("Lamps", VRIcons.Id.Stairs, () => showLamps, () => Set(ref showLamps), "L"));
        Controls.Add(new LabControl
        {
            Label = "Zeros", Icon = VRIcons.Id.Waves, Clicked = CycleZeros, DynamicLabel = () => $"Zeros: {PathsModel.Group(ZeroCount)}", Keys = "T"
        });
        Controls.Add(LabControl.Toggle("Phases", VRIcons.Id.Eye, () => showPhases, () => Set(ref showPhases), "P"));
        Controls.Add(LabControl.Button("n − 1", VRIcons.Id.PrevLine, () => Go(funnel.Selected - 1), "←"));
        Controls.Add(LabControl.Button("n + 1", VRIcons.Id.NextLine, () => Go(funnel.Selected + 1), "→"));
        Controls.Add(LabControl.Button("Replay", VRIcons.Id.Replay, () => funnel.Replay(), "Space"));
        Controls.Add(LabControl.Button("Reset view", VRIcons.Id.Reset, () => Lab.ResetView(false), "Home"));
        ApplyVisibility();
    }

    Transform Group(string name)
    {
        GameObject g = new(name);
        g.transform.SetParent(Root, false);
        return g.transform;
    }

    void Set(ref bool flag)
    {
        flag = !flag;
        ApplyVisibility();
        UpdateStatus();
    }

    public void SetLayers(bool coils, bool comb, bool ringsOn, bool fansOn, bool lampsOn, bool phases)
    {
        showCoils = coils;
        showComb = comb;
        showRings = ringsOn;
        showFans = fansOn;
        showLamps = lampsOn;
        showPhases = phases;
        ApplyVisibility();
        UpdateStatus();
    }

    void ApplyVisibility()
    {
        coilRoot.gameObject.SetActive(showCoils);
        combRoot.gameObject.SetActive(showComb);
        rings.Renderer.gameObject.SetActive(showRings);
        fanRoot.gameObject.SetActive(showFans);
        lamps.Renderer.gameObject.SetActive(showLamps);
        phaseRoot.gameObject.SetActive(showPhases && funnel.Selected >= 2);
        for (int i = 0; i < 8; i++)
            if (fans[i] != null)
                fans[i].gameObject.SetActive(spoke < 0 || spoke == i);
        for (int i = 0; i < hourLabels.Count; i++) hourLabels[i].gameObject.SetActive(spoke < 0 || spoke == i);
        RebuildFanTips();
        funnel.SetDim(showLamps || showFans ? .25f : .35f);
    }

    void CycleSpoke()
    {
        spoke = spoke >= 7 ? -1 : spoke + 1;
        if (!showFans) showFans = true;
        ApplyVisibility();
        UpdateStatus();
    }

    public void SetSpoke(int index)
    {
        spoke = Mathf.Clamp(index, -1, 7);
        ApplyVisibility();
        UpdateStatus();
    }

    void CycleZeros() => SetZeroLevel((zeroLevel + 1) % ZeroCounts.Length);

    public void SetZeroLevel(int level)
    {
        zeroLevel = Mathf.Clamp(level, 0, ZeroCounts.Length - 1);
        combJob = null;
        StartJobs();
        UpdateStatus();
    }

    public override LabView Home => new()
    {
        Focus = O + funnel.Up(Mathf.Log(Count) * .45f), Distance = 140f, Yaw = -62f, Pitch = 22f, Fov = 50f
    };

    public override void OnEnter()
    {
        StartJobs();
        if (funnel.Selected < 2) Go(7);
        else
        {
            Lab.SetStrip(funnel.Selected, funnel.StepsShown);
            UpdateStatus();
        }
    }

    void StartJobs()
    {
        int count = ZeroCounts[zeroLevel];
        if (combJob == null && (combData == null || combData.Zeros != count)) combJob = Task.Run(() => ComputeComb(count));
        if (fanJob == null && fanData == null) fanJob = Task.Run(ComputeFans);
    }

    public override void Tick(float dt)
    {
        funnel.Tick(dt);
        if (combJob != null && combJob.IsCompleted)
        {
            if (combJob.Status == TaskStatus.RanToCompletion && combJob.Result.Zeros == ZeroCounts[zeroLevel])
            {
                combData = combJob.Result;
                BuildComb();
                UpdateStatus();
            }
            else if (combJob.IsFaulted) Debug.LogException(combJob.Exception);

            combJob = null;
            if (combData == null || combData.Zeros != ZeroCounts[zeroLevel]) StartJobs();
        }

        if (fanJob != null && fanJob.IsCompleted)
        {
            if (fanJob.Status == TaskStatus.RanToCompletion)
            {
                fanData = fanJob.Result;
                BuildFans();
                UpdateStatus();
            }
            else if (fanJob.IsFaulted) Debug.LogException(fanJob.Exception);

            fanJob = null;
        }
    }

    // ---- the sums (worker threads) ------------------------------------------------------------------------

    static List<double> PrimePowerHeights(Func<int, bool> keep)
    {
        List<double> z = new();
        for (int m = 2; m <= Count; m++)
            if (keep(m) && ZeroSums.PrimePower(m, out _, out _))
                z.Add(Math.Log(m));
        return z;
    }

    static CombData ComputeComb(int count)
    {
        ZeroSums.Sum sum = ZeroSums.ZetaFirst(count);
        double coarse = Math.Max(.0025d, .25d / sum.Sigma);
        List<double> zs = ZeroSums.Samples(.42d, Math.Log(Count) + .02d, coarse, PrimePowerHeights(_ => true), sum.Sigma);
        CombData d = new() { Zeros = count, Sigma = sum.Sigma, Z = zs.ToArray(), W = new double[zs.Count] };
        for (int i = 0; i < zs.Count; i++) d.W[i] = sum.At(zs[i]).Re;
        for (int n = 2; n <= Count; n++) d.Lamp[n] = sum.At(Math.Log(n)).Re;
        return d;
    }

    static FanData ComputeFans()
    {
        ZeroSums.Sum[] sums = ZeroSums.LSums;
        List<double> zs = ZeroSums.Samples(1.0d, Math.Log(Count) + .02d, .004d, PrimePowerHeights(m => m % 2 != 0 && m % 3 != 0 && m % 5 != 0), ZeroSums.SigmaL);
        FanData d = new() { Z = zs.ToArray() };
        for (int h = 0; h < 8; h++) d.W[h] = new double[zs.Count];
        Cpx[] w = new Cpx[8];
        for (int i = 0; i < zs.Count; i++)
        {
            for (int k = 0; k < 8; k++) w[k] = sums[k].At(zs[i]);
            for (int h = 0; h < 8; h++) d.W[h][i] = ZeroSums.SpokeFrom(ZeroSums.Gold[h], zs[i], w);
        }

        for (int h = 0; h < 8; h++)
        {
            int hour = ZeroSums.Gold[h];
            for (int m = hour; m <= Count; m += 30)
                if (ZeroSums.PrimePower(m, out _, out _))
                    d.Tip[m] = ZeroSums.Spoke(hour, Math.Log(m));
        }

        return d;
    }

    // ---- drawing --------------------------------------------------------------------------------------------

    static Vector3 HourDir(int h)
    {
        float a = (90f - 12f * h) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
    }

    public static Color HourColor(int h)
    {
        int i = Array.IndexOf(ZeroSums.Gold, ((h % 30) + 30) % 30);
        return i < 0 ? new Color(.7f, .72f, .85f) : Color.HSVToRGB((i / 8f + .02f) % 1f, .62f, 1f);
    }

    /// <summary>Radius of a tooth: s·e^(z/2)·W/z, so a tooth of log p reaches the top of p's column (p^j: 1/j of the way).</summary>
    static float Reach(double z, double w) => (float)(S * Math.Exp(z / 2d) * Math.Clamp(w / z, -1.6d, 1.6d));

    void BuildCoils()
    {
        float zMax = Mathf.Log(Count);
        for (int k = 0; k < 3; k++)
        {
            float r = S * (3.4f - .9f * k), gm = (float)ZeroStackData.Zeta[k];
            int steps = Mathf.CeilToInt(gm * zMax / (2f * Mathf.PI) * 48f);
            Vector3[] pts = new Vector3[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                float z = zMax * i / steps, a = gm * z;
                pts[i] = O + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f) + funnel.Up(z);
            }

            LabDraw.Line(coilRoot, $"coil {k + 1}", .3f - .05f * k, CoilColors[k], Factory.GlowIntensity, pts);
        }
    }

    void BuildComb()
    {
        CombData d = combData;
        Vector3[] pts = new Vector3[d.Z.Length];
        for (int i = 0; i < pts.Length; i++) pts[i] = O + side * Reach(d.Z[i], d.W[i]) + funnel.Up((float)d.Z[i]);
        if (comb == null) comb = LabDraw.Line(combRoot, "comb", .16f, Color.white, Factory.GlowIntensity, pts);
        else LabDraw.SetLine(comb, pts);
        if (profile == null)
        {
            List<Vector3> pr = new();
            for (double z = .42d; z <= Math.Log(Count) + .02d; z += .02d) pr.Add(O + side * (float)(S * Math.Exp(z / 2d)) + funnel.Up((float)z));
            profile = LabDraw.Line(combRoot, "funnel profile", .08f, new Color(.5f, .55f, .75f) * .7f, Factory.NoGlow, pr);
        }

        // rings: each prime power's tooth spun round the axis; lamps: the sum at every column, lit as its top piece
        List<(Vector3, Vector3, Color, float)> ringSegs = new(), lampSegs = new();
        for (int m = 2; m <= Count; m++)
        {
            double z = Math.Log(m), w = d.Lamp[m];
            bool pp = ZeroSums.PrimePower(m, out long p, out int j);
            Color col = LabMath.PrimeColor(p);
            if (pp)
            {
                float rr = Reach(z, w);
                Vector3 c = O + funnel.Up((float)z);
                for (int i = 0; i < 72; i++)
                {
                    float a0 = i / 72f * Mathf.PI * 2f, a1 = (i + 1) / 72f * Mathf.PI * 2f;
                    ringSegs.Add((c + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * rr, c + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * rr,
                        col * (j == 1 ? .7f : .45f), j == 1 ? .07f : .05f));
                }
            }

            if (Math.Abs(w) < .02d) continue;
            Vector3 top = funnel.Top(m) + Vector3.back * .05f;
            lampSegs.Add((top - funnel.Up((float)Math.Clamp(w, -z, z)), top, pp ? col : Color.white, .55f));
        }

        rings.Set(ringSegs);
        lamps.Set(lampSegs);
    }

    void BuildFans()
    {
        FanData d = fanData;
        for (int h = 0; h < 8; h++)
        {
            int hour = ZeroSums.Gold[h];
            Vector3 u = HourDir(hour);
            Vector3[] pts = new Vector3[d.Z.Length];
            for (int i = 0; i < pts.Length; i++) pts[i] = O + u * Reach(d.Z[i], d.W[h][i]) + funnel.Up((float)d.Z[i]);
            if (fans[h] == null) fans[h] = LabDraw.Line(fanRoot, $"fan {hour}", .15f, HourColor(hour), Factory.GlowIntensity, pts);
            else LabDraw.SetLine(fans[h], pts);
        }

        ApplyVisibility();
    }

    void RebuildFanTips()
    {
        if (fanData == null || fanTips == null) return;
        List<Vector3> pts = new();
        List<Color> cols = new();
        List<float> sizes = new();
        foreach (var kv in fanData.Tip)
        {
            int hour = kv.Key % 30, h = Array.IndexOf(ZeroSums.Gold, hour);
            if (spoke >= 0 && spoke != h) continue;
            double z = Math.Log(kv.Key);
            ZeroSums.PrimePower(kv.Key, out _, out int j);
            pts.Add(O + HourDir(hour) * Reach(z, kv.Value) + funnel.Up((float)z));
            cols.Add(HourColor(hour));
            sizes.Add(j == 1 ? .75f : .5f);
        }

        fanTips.Set(pts, cols, sizes);
    }

    /// <summary>Where all of ζ's zeros point at the height of n: 48 bars round the axis, and their mean as an arrow (scaled up).</summary>
    void BuildPhases(int n)
    {
        double[] zs = ZeroStackData.Zeta;
        int count = ZeroCount;
        const int bins = 48;
        int[] hist = new int[bins];
        double z = Math.Log(n);
        for (int i = 0; i < count; i++)
        {
            double ph = zs[i] * z % (2d * Math.PI);
            hist[(int)(ph / (2d * Math.PI) * bins) % bins]++;
        }

        var (mr, mi) = ZeroSums.MeanPhase(n, count);
        Vector3 c = O + funnel.Up((float)z);
        float r0 = S * 5f, unit = S * 3.4f, mean = count / (float)bins;
        bool lit = ZeroSums.Mangoldt(n) > 0d;
        Color col = lit ? new Color(1f, .68f, .25f) : new Color(.75f, .78f, .9f);
        List<(Vector3, Vector3, Color, float)> segs = new();
        for (int i = 0; i < bins; i++)
        {
            float a = (i + .5f) / bins * Mathf.PI * 2f;
            Vector3 dir = new(Mathf.Cos(a), Mathf.Sin(a), 0f);
            segs.Add((c + dir * r0, c + dir * (r0 + unit * hist[i] / mean), col, .32f));
        }

        for (int i = 0; i < 96; i += 2)
        {
            float a0 = i / 96f * Mathf.PI * 2f, a1 = (i + 1) / 96f * Mathf.PI * 2f;
            segs.Add((c + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * (r0 + unit), c + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * (r0 + unit), Color.white * .5f, .06f));
        }

        phaseBars.Set(segs);
        float gain = r0 * .9f / .12f;
        Vector3 tip = c + new Vector3((float)mr, (float)mi, 0f) * gain;
        if (phaseMean == null) phaseMean = LabDraw.Line(phaseRoot, "mean", .3f, Color.white, Factory.GlowIntensity, new[] { c, tip });
        else LabDraw.SetLine(phaseMean, new[] { c, tip });
        string text = $"mean of {n}<sup>iγ</sup>:  {Signed(mr)}";
        if (phaseLabel == null) phaseLabel = LabDraw.Label(phaseRoot, text, c + Vector3.back * 1.6f + Vector3.left * (r0 + unit * 1.6f), 30f, col);
        else
        {
            phaseLabel.Text = text;
            phaseLabel.Color = col;
            phaseLabel.transform.position = c + Vector3.back * 1.6f + Vector3.left * (r0 + unit * 1.6f);
        }
    }

    static string Signed(double v) => (v >= 0d ? "+" : "−") + Math.Abs(v).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);

    // ---- picking, keys, the board ---------------------------------------------------------------------------

    void Go(int n)
    {
        n = Mathf.Clamp(n, 2, Count);
        funnel.Select(n, true);
        BuildPhases(n);
        ApplyVisibility();
        UpdateStatus();
    }

    public void Pick(int n) => Go(n);

    /// <summary>The comb's value at column n (≈ log p at a prime power, ≈ 0 elsewhere), or NaN before the sum is in.</summary>
    public double LampAt(int n) => combData != null && n >= 2 && n <= Count ? combData.Lamp[n] : double.NaN;

    /// <summary>The spoke-h fan's value at column n (n ≡ h mod 30, a prime power), or NaN.</summary>
    public double FanAt(int n) => fanData != null && fanData.Tip.TryGetValue(n, out double v) ? v : double.NaN;

    public override int PickCount => funnel.PickCount;

    public override Vector3 PickPoint(int i) => funnel.Top(funnel.PickNumber(i));

    public override string PickLabel(int i)
    {
        int n = funnel.PickNumber(i);
        return LabMath.IsPrime(n) ? $"{n}:  prime" : $"{n} = {LabMath.FactorString(n)}";
    }

    public override void OnPick(int i) => Go(funnel.PickNumber(i));

    public override bool OnKey(Keyboard k)
    {
        bool used = true;
        if (k.leftArrowKey.wasPressedThisFrame) Go(funnel.Selected - 1);
        else if (k.rightArrowKey.wasPressedThisFrame) Go(funnel.Selected + 1);
        else if (k.downArrowKey.wasPressedThisFrame) Go(funnel.Selected - 30);
        else if (k.upArrowKey.wasPressedThisFrame) Go(funnel.Selected + 30);
        else if (k.spaceKey.wasPressedThisFrame) funnel.Replay();
        else if (k.cKey.wasPressedThisFrame) Set(ref showCoils);
        else if (k.zKey.wasPressedThisFrame) Set(ref showComb);
        else if (k.rKey.wasPressedThisFrame) Set(ref showRings);
        else if (k.fKey.wasPressedThisFrame) Set(ref showFans);
        else if (k.sKey.wasPressedThisFrame) CycleSpoke();
        else if (k.lKey.wasPressedThisFrame) Set(ref showLamps);
        else if (k.tKey.wasPressedThisFrame) CycleZeros();
        else if (k.pKey.wasPressedThisFrame) Set(ref showPhases);
        else used = false;
        return used;
    }

    void UpdateStatus()
    {
        int n = funnel.Selected;
        if (n < 2) return;
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        string head = LabMath.IsPrime(n) ? $"{n}:  prime" : $"{n} = {LabMath.FactorString(n)}";
        int count = ZeroCount;
        double lam = ZeroSums.Mangoldt(n);
        string sum = combData != null && combData.Zeros == count
            ? $"ζ's {PathsModel.Group(count)} zeros at log {n}:  {combData.Lamp[n].ToString("0.000", ci)}" + (lam > 0d ? $"  = log p ({lam.ToString("0.000", ci)})" : "  (no prime power: 0)")
            : $"summing ζ's {PathsModel.Group(count)} zeros …";
        var (mr, _) = ZeroSums.MeanPhase(n, count);
        string phases = $"coils at log {n}:  mean {Signed(mr)}, Landau {Signed(ZeroSums.LandauMean(n, count))}";
        int hour = n % 30, h = Array.IndexOf(ZeroSums.Gold, hour);
        string fan;
        if (h < 0) fan = $"hour {hour}: no gold spoke";
        else if (fanData == null) fan = $"spoke {hour}: …";
        else fan = fanData.Tip.TryGetValue(n, out double v) ? $"spoke {hour}'s fan: {v.ToString("0.000", ci)}" : $"spoke {hour}: no tooth";
        Lab.SetStatus(head + "\n" + PathsModel.Small(sum) + "\n" + PathsModel.Small(phases + "    " + fan));
    }
}
