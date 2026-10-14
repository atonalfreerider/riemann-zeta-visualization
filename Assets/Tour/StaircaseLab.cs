using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>
/// The staircase on its own, without the zeta trail, chain or cosine chorus.
///
/// Terms: Riemann's explicit formula built one piece at a time on x = 1 … 30,
///   ψ(x) = x − ln 2π − ½ ln(1 − x⁻²) − Σ_γ 4√x (cos(γ log x) + 2γ sin(γ log x)) / (1 + 4γ²).
///   Each zero's wave is drawn by itself in a strip above the graph, then poured into the staircase while the previous
///   shape stays faint, so what each term does is visible; the exact ψ (log p at every prime power) is the target.
///
/// Wobble: the same staircase from the first K zeros on the line (white) against the same zeros moved to Re(s) = β (red),
///   with orange bars for the difference at every whole x and a readout of the largest gap.
/// </summary>
public class StaircaseLab : MonoBehaviour
{
    // ---- terms view geometry (world units = the graph's own x and y)
    public const float TermsX0 = 1f, TermsX1 = 30f;
    const int TermsSamples = 1500;
    public const float StripY = 34f;
    const float StripScale = 2f;

    // ---- wobble view geometry
    public const float WobbleX0 = 2f, WobbleX1 = 60f;
    const int WobbleSamples = 1200;
    const float WobbleClampLow = -12f, WobbleClampHigh = 85f, WobbleLabelY = 74f;

    Main main;
    GameObject termsRoot, wobbleRoot;
    Coroutine routine;

    // terms
    double[] tx, tSqrt, tLog, sum;
    Vector3[] buffer;
    LineRenderer sumLine, previousLine, termLine, exactLine;
    TextBox stripLabel;
    int wavesAdded;
    bool constantAdded, trivialAdded;

    // wobble
    float[] wx, wLog;
    float[] cosU, sinU; // per zero × sample
    double[] gammas;
    double[] original, wobbled;
    Vector3[] wBuffer;
    LineRenderer originalLine, wobbleLine;
    readonly List<LineRenderer> deltaBars = new();
    TextBox wobbleReadout;
    float beta = .5f, shownBeta = -1f;

    public float Beta => beta;

    public static StaircaseLab Create(Main main)
    {
        StaircaseLab lab = new GameObject("StaircaseLab").AddComponent<StaircaseLab>();
        lab.main = main;
        return lab;
    }

    public void Hide()
    {
        Stop();
        if (termsRoot != null) termsRoot.SetActive(false);
        if (wobbleRoot != null) wobbleRoot.SetActive(false);
        Board(null);
    }

    static void Board(string text)
    {
        CaptionDisplay captions = TourDirector.Instance != null ? TourDirector.Instance.Captions : null;
        if (captions == null) return;
        if (string.IsNullOrEmpty(text)) captions.HideBoard();
        else captions.ShowBoard(text);
    }

    LineRenderer Line(string name, Transform parent, float width, Color color, float intensity, int count)
    {
        LineRenderer line = Factory.NewLine(name, width, color, intensity);
        line.transform.SetParent(parent, false);
        line.positionCount = count;
        line.numCornerVertices = 2;
        return line;
    }

    TextBox Label(Transform parent, string text, float size, Color color, Vector3 position, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        TextBox label = TextBox.Create(text, size);
        label.transform.SetParent(parent, false);
        label.transform.position = position;
        label.Alignment = alignment;
        label.Color = color;
        label.TextField.rectTransform.sizeDelta = new Vector2(400f, label.TextField.rectTransform.sizeDelta.y);
        label.TextField.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }

    static bool IsPrime(int n)
    {
        if (n < 2) return false;
        for (int d = 2; d * d <= n; d++) if (n % d == 0) return false;
        return true;
    }

    /// <summary>log p if n is a power of the prime p, else 0 (the height of ψ's step at n).</summary>
    static double Mangoldt(int n)
    {
        for (int p = 2; p <= n; p++)
        {
            if (n % p != 0) continue;
            int m = n;
            while (m % p == 0) m /= p;
            return m == 1 && IsPrime(p) ? System.Math.Log(p) : 0d;
        }

        return 0d;
    }

    /// <summary>Whole numbers along the x axis: primes in yellow, prime powers in orange.</summary>
    void NumberLine(Transform parent, float x0, float x1, float labelSize)
    {
        LineRenderer axis = Line("x axis", parent, .04f, new Color(.45f, .45f, .52f), Factory.NoGlow, 2);
        axis.SetPositions(new[] { new Vector3(0, 0, 0), new Vector3(x1 + 1f, 0, 0) });
        LineRenderer yAxis = Line("y axis", parent, .04f, new Color(.45f, .45f, .52f), Factory.NoGlow, 2);
        yAxis.SetPositions(new[] { new Vector3(0, 0, 0), new Vector3(0, x1, 0) });
        for (int n = Mathf.CeilToInt(x0); n <= (int)x1; n++)
        {
            bool prime = IsPrime(n);
            bool power = !prime && Mangoldt(n) > 0;
            if (!prime && !power) continue;
            Color c = prime ? Factory.Palette.Zero : new Color(1f, .55f, .2f);
            LineRenderer tick = Line("tick " + n, parent, prime ? .06f : .045f, c, Factory.NoGlow, 2);
            tick.SetPositions(new[] { new Vector3(n, 0, 0), new Vector3(n, prime ? -.6f : -.4f, 0) });
            float below = labelSize * .2f;
            Label(parent, n.ToString(CultureInfo.InvariantCulture), prime ? labelSize : labelSize * .8f, c, new Vector3(n, -.6f - below, 0));
        }
    }

    // =============================================================================================== terms

    void EnsureTerms()
    {
        if (termsRoot != null) return;
        termsRoot = new GameObject("StaircaseTerms");
        termsRoot.transform.SetParent(transform, false);
        tx = new double[TermsSamples];
        tSqrt = new double[TermsSamples];
        tLog = new double[TermsSamples];
        sum = new double[TermsSamples];
        buffer = new Vector3[TermsSamples];
        for (int i = 0; i < TermsSamples; i++)
        {
            tx[i] = TermsX0 + (TermsX1 - TermsX0) * i / (TermsSamples - 1d);
            tSqrt[i] = System.Math.Sqrt(tx[i]);
            tLog[i] = System.Math.Log(tx[i]);
        }

        NumberLine(termsRoot.transform, TermsX0, TermsX1, 7f);

        // the exact ψ(x): a step of log p at every prime power, the target the terms are aiming for
        List<Vector3> steps = new() { new Vector3(TermsX0, 0, .05f) };
        double psi = 0;
        for (int n = 2; n <= (int)TermsX1; n++)
        {
            double jump = Mangoldt(n);
            if (jump <= 0) continue;
            steps.Add(new Vector3(n, (float)psi, .05f));
            psi += jump;
            steps.Add(new Vector3(n, (float)psi, .05f));
        }

        steps.Add(new Vector3(TermsX1, (float)psi, .05f));
        exactLine = Line("exact staircase", termsRoot.transform, .05f, new Color(.5f, .5f, .58f, .7f), Factory.NoGlow, steps.Count);
        exactLine.SetPositions(steps.ToArray());
        Label(termsRoot.transform, "exact ψ(x): a step of log p at every prime power", 6f, new Color(.6f, .6f, .68f), new Vector3(TermsX1 - 7f, (float)psi + 2.2f, 0));

        previousLine = Line("previous", termsRoot.transform, .05f, new Color(1f, 1f, 1f, .3f), Factory.NoGlow, TermsSamples);
        sumLine = Line("staircase", termsRoot.transform, .09f, Color.white, Factory.SoftGlowIntensity, TermsSamples);

        LineRenderer strip = Line("strip axis", termsRoot.transform, .03f, new Color(.4f, .4f, .48f), Factory.NoGlow, 2);
        strip.SetPositions(new[] { new Vector3(TermsX0, StripY, 0), new Vector3(TermsX1, StripY, 0) });
        termLine = Line("term", termsRoot.transform, .07f, Factory.Palette.Cosine, Factory.SoftGlowIntensity, TermsSamples);
        stripLabel = Label(termsRoot.transform, "", 6.5f, new Color(.8f, .8f, .88f), new Vector3(TermsX0, StripY + 3.6f, 0), TextAlignmentOptions.Left);
        termsRoot.SetActive(false);
    }

    Vector3[] SumPoints(double[] values, float z = 0f)
    {
        for (int i = 0; i < TermsSamples; i++) buffer[i] = new Vector3((float)tx[i], Mathf.Clamp((float)values[i], -3f, 40f), z);
        return buffer;
    }

    void DrawSum() => sumLine.SetPositions(SumPoints(sum));

    static string Formula(string highlight)
    {
        string H(string id, string text) => id == highlight ? $"<color=#FFD400>{text}</color>" : $"<color=#B4B4C0>{text}</color>";
        return H("x", "x") + " " + H("const", "− ln 2π") + " " + H("trivial", "− ½ ln(1 − x<sup>−2</sup>)") + " " + H("waves", "− Σ<sub>γ</sub> 4√x (cos(γ log x) + 2γ sin(γ log x)) / (1 + 4γ²)");
    }

    void BoardTerms(string highlight, string detail) => Board("ψ(x) = " + Formula(highlight) + (string.IsNullOrEmpty(detail) ? "" : "\n<size=62%>" + detail + "</size>"));

    /// <summary>Start from nothing: axes, the exact staircase as the target, and the first piece, the line y = x, drawn in.</summary>
    public void BeginTerms(float seconds)
    {
        EnsureTerms();
        if (wobbleRoot != null) wobbleRoot.SetActive(false);
        termsRoot.SetActive(true);
        Stop();
        wavesAdded = 0;
        constantAdded = trivialAdded = false;
        for (int i = 0; i < TermsSamples; i++) sum[i] = tx[i];
        termLine.positionCount = 0;
        previousLine.positionCount = 0;
        stripLabel.Text = "";
        BoardTerms("x", "the first piece: the straight line y = x");
        Run(DrawIn(seconds));
    }

    // steps run one after another, so one narration line can add several pieces in order
    readonly Queue<IEnumerator> queue = new();

    void Run(IEnumerator r)
    {
        queue.Enqueue(r);
        if (routine == null) routine = StartCoroutine(Runner());
    }

    IEnumerator Runner()
    {
        while (queue.Count > 0) yield return queue.Dequeue();
        routine = null;
    }

    void Stop()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        queue.Clear();
    }

    IEnumerator DrawIn(float seconds)
    {
        sumLine.positionCount = 0;
        Vector3[] pts = SumPoints(sum);
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            int count = Mathf.Clamp(Mathf.CeilToInt(Mathf.SmoothStep(0f, 1f, t / seconds) * TermsSamples), 2, TermsSamples);
            sumLine.positionCount = count;
            for (int i = 0; i < count; i++) sumLine.SetPosition(i, pts[i]);
            yield return null;
        }

        sumLine.positionCount = TermsSamples;
        DrawSum();
    }

    /// <summary>Show a term by itself in the strip, then pour it into the staircase (the old shape stays faint).</summary>
    IEnumerator Apply(double[] term, Color color, string stripText, float seconds, bool showInStrip)
    {
        double[] before = (double[])sum.Clone();
        previousLine.positionCount = TermsSamples;
        previousLine.SetPositions(SumPoints(before, .02f));
        termLine.positionCount = TermsSamples;
        termLine.startColor = termLine.endColor = color;
        stripLabel.Text = stripText;
        float tDraw = showInStrip ? seconds * .4f : 0f, tPour = seconds * (showInStrip ? .45f : .9f);
        float t = 0f;
        // 1. the term on its own, drawn left to right in the strip
        while (t < tDraw)
        {
            t += Time.deltaTime;
            int count = Mathf.Clamp(Mathf.CeilToInt(Mathf.SmoothStep(0f, 1f, t / tDraw) * TermsSamples), 2, TermsSamples);
            termLine.positionCount = count;
            for (int i = 0; i < count; i++) termLine.SetPosition(i, new Vector3((float)tx[i], StripY + (float)term[i] * StripScale, -.02f));
            yield return null;
        }

        termLine.positionCount = TermsSamples;
        // 2. it travels down onto the staircase and the staircase takes its shape
        t = 0f;
        while (t < tPour)
        {
            t += Time.deltaTime;
            float u = Mathf.SmoothStep(0f, 1f, t / tPour);
            for (int i = 0; i < TermsSamples; i++)
            {
                float stripY = StripY + (float)term[i] * StripScale;
                float onStair = (float)(before[i] + term[i]);
                termLine.SetPosition(i, new Vector3((float)tx[i], Mathf.Lerp(stripY, Mathf.Clamp(onStair, -3f, 40f), u), -.02f));
                sum[i] = before[i] + term[i] * u;
            }

            DrawSum();
            yield return null;
        }

        for (int i = 0; i < TermsSamples; i++) sum[i] = before[i] + term[i];
        DrawSum();
        // the term returns to the strip, faint, as a reminder of what was just added
        for (int i = 0; i < TermsSamples; i++) termLine.SetPosition(i, new Vector3((float)tx[i], StripY + (float)term[i] * StripScale, -.02f));
        termLine.startColor = termLine.endColor = new Color(color.r, color.g, color.b, .45f);
        yield return new WaitForSeconds(seconds - tDraw - tPour);
    }

    public void AddConstant(float seconds)
    {
        EnsureTerms();
        termsRoot.SetActive(true);
        double c = -System.Math.Log(2 * System.Math.PI);
        double[] term = new double[TermsSamples];
        for (int i = 0; i < TermsSamples; i++) term[i] = c;
        constantAdded = true;
        BoardTerms("const", $"subtract ln 2π ≈ {-c:0.00}: the whole line shifts down");
        Run(Apply(term, new Color(.8f, .6f, 1f), "− ln 2π   (a constant)", seconds, false));
    }

    public void AddTrivial(float seconds)
    {
        EnsureTerms();
        termsRoot.SetActive(true);
        double[] term = new double[TermsSamples];
        for (int i = 0; i < TermsSamples; i++) term[i] = tx[i] > 1.0001 ? -.5 * System.Math.Log(1 - 1 / (tx[i] * tx[i])) : 0;
        trivialAdded = true;
        BoardTerms("trivial", "the trivial zeros at −2, −4, −6 …: a nudge near the start that fades away");
        Run(Apply(term, new Color(.6f, .8f, 1f), "− ½ ln(1 − x<sup>−2</sup>)   (the trivial zeros, ×2 in this strip)", seconds, true));
    }

    /// <summary>Add the waves of zeros from … to (exclusive; −1 = 300): one at a time with time to see each, then faster and faster.</summary>
    public void AddWaves(int from, int to, float seconds)
    {
        EnsureTerms();
        termsRoot.SetActive(true);
        double[] zs = main.zetaZeroValues;
        if (to < 0) to = 300;
        to = Mathf.Min(to, zs.Length);
        from = Mathf.Max(from, wavesAdded);
        Run(WavesRoutine(from, to, seconds));
    }

    double[] WaveTerm(double gamma)
    {
        double[] term = new double[TermsSamples];
        for (int i = 0; i < TermsSamples; i++)
        {
            double u = gamma * tLog[i];
            term[i] = -4 * tSqrt[i] * (System.Math.Cos(u) + 2 * gamma * System.Math.Sin(u)) / (1 + 4 * gamma * gamma);
        }

        return term;
    }

    IEnumerator WavesRoutine(int from, int to, float seconds)
    {
        double[] zs = main.zetaZeroValues;
        int count = Mathf.Max(1, to - from);
        float per = seconds / count;
        if (per >= 1.4f)
        {
            // slowly: each wave shown alone, then poured in
            for (int k = from; k < to; k++)
            {
                BoardTerms("waves", $"wave {k + 1}:  γ = {zs[k]:0.00}   ({k + 1} wave{(k > 0 ? "s" : "")} added)");
                yield return Apply(WaveTerm(zs[k]), Annotations.CollisionColor(k), $"wave {k + 1}, γ = {zs[k]:0.00}   (×2 in this strip)", per, true);
                wavesAdded = k + 1;
            }

            yield break;
        }

        // quickly: the strip shows the newest wave, the staircase absorbs the rest a few at a time
        previousLine.positionCount = TermsSamples;
        previousLine.SetPositions(SumPoints(sum, .02f));
        float t = 0f;
        int k2 = from;
        while (k2 < to)
        {
            t += Time.deltaTime;
            int want = Mathf.Min(to, from + Mathf.CeilToInt(count * Mathf.Pow(Mathf.Clamp01(t / (seconds * .9f)), 1.6f)));
            double[] last = null;
            for (; k2 < want; k2++)
            {
                last = WaveTerm(zs[k2]);
                for (int i = 0; i < TermsSamples; i++) sum[i] += last[i];
            }

            wavesAdded = k2;
            if (last != null)
            {
                DrawSum();
                Color c = Annotations.CollisionColor(k2 - 1);
                termLine.positionCount = TermsSamples;
                termLine.startColor = termLine.endColor = new Color(c.r, c.g, c.b, .7f);
                for (int i = 0; i < TermsSamples; i++) termLine.SetPosition(i, new Vector3((float)tx[i], StripY + (float)last[i] * StripScale, -.02f));
                stripLabel.Text = $"wave {k2}, γ = {zs[k2 - 1]:0.00}   (×2 in this strip)";
                BoardTerms("waves", $"{k2} waves added: the steps sharpen on the primes, smaller steps on 4, 8, 9, 16, 25, 27");
            }

            yield return null;
        }
    }

    // =============================================================================================== wobble

    void EnsureWobble(int zeros)
    {
        if (wobbleRoot != null && gammas != null && gammas.Length == zeros) return;
        if (wobbleRoot != null) Destroy(wobbleRoot);
        deltaBars.Clear();
        wobbleRoot = new GameObject("StaircaseWobble");
        wobbleRoot.transform.SetParent(transform, false);
        wx = new float[WobbleSamples];
        wLog = new float[WobbleSamples];
        for (int i = 0; i < WobbleSamples; i++)
        {
            wx[i] = WobbleX0 + (WobbleX1 - WobbleX0) * i / (WobbleSamples - 1f);
            wLog[i] = Mathf.Log(wx[i]);
        }

        double[] zs = main.zetaZeroValues;
        gammas = new double[zeros];
        cosU = new float[zeros * WobbleSamples];
        sinU = new float[zeros * WobbleSamples];
        for (int k = 0; k < zeros; k++)
        {
            gammas[k] = zs[k];
            for (int i = 0; i < WobbleSamples; i++)
            {
                double u = zs[k] * wLog[i];
                cosU[k * WobbleSamples + i] = (float)System.Math.Cos(u);
                sinU[k * WobbleSamples + i] = (float)System.Math.Sin(u);
            }
        }

        original = new double[WobbleSamples];
        wobbled = new double[WobbleSamples];
        wBuffer = new Vector3[WobbleSamples];
        Evaluate(.5f, original);

        NumberLine(wobbleRoot.transform, WobbleX0, WobbleX1, 11f);
        originalLine = Line("staircase on the line", wobbleRoot.transform, .14f, Color.white, Factory.SoftGlowIntensity, WobbleSamples);
        originalLine.SetPositions(Points(original, 0f));
        wobbleLine = Line("wobbled staircase", wobbleRoot.transform, .14f, Factory.Palette.Ghost, Factory.SoftGlowIntensity, WobbleSamples);
        for (int x = Mathf.CeilToInt(WobbleX0); x <= (int)WobbleX1; x++)
        {
            LineRenderer bar = Line("difference at " + x, wobbleRoot.transform, .1f, new Color(1f, .6f, .2f, .85f), Factory.NoGlow, 2);
            deltaBars.Add(bar);
        }

        Label(wobbleRoot.transform, "white: every collision on the ½ line      red: the same collisions moved to Re(s) = β      orange: the difference", 13f, new Color(.8f, .8f, .88f), new Vector3(WobbleX0, WobbleLabelY, 0), TextAlignmentOptions.Left);
        wobbleReadout = Label(wobbleRoot.transform, "", 16f, new Color(1f, .6f, .2f), new Vector3(WobbleX0, WobbleLabelY - 7f, 0), TextAlignmentOptions.Left);
        shownBeta = -1f;
    }

    /// <summary>ψ from the first zeros, every one moved to Re(s) = b: x − ln 2π − Σ 2x^b (b cos u + γ sin u) / (b² + γ²).</summary>
    void Evaluate(float b, double[] into)
    {
        int zeros = gammas.Length;
        double ln2pi = System.Math.Log(2 * System.Math.PI);
        for (int i = 0; i < WobbleSamples; i++)
        {
            double xb = System.Math.Exp(b * wLog[i]);
            double s = 0;
            for (int k = 0; k < zeros; k++)
            {
                double g = gammas[k];
                s += (b * cosU[k * WobbleSamples + i] + g * sinU[k * WobbleSamples + i]) / (b * b + g * g);
            }

            into[i] = wx[i] - ln2pi - 2 * xb * s;
        }
    }

    Vector3[] Points(double[] values, float z)
    {
        for (int i = 0; i < WobbleSamples; i++) wBuffer[i] = new Vector3(wx[i], Mathf.Clamp((float)values[i], WobbleClampLow, WobbleClampHigh), z);
        return wBuffer;
    }

    /// <summary>Show the staircase from the first <paramref name="zeros"/> zeros on the line, and the same zeros at Re(s) = β.</summary>
    public void BeginWobble(int zeros, float startBeta)
    {
        EnsureWobble(Mathf.Clamp(zeros, 1, main.zetaZeroValues.Length));
        if (termsRoot != null) termsRoot.SetActive(false);
        wobbleRoot.SetActive(true);
        Stop();
        beta = startBeta;
        shownBeta = -1f;
        Board(null);
    }

    public void SweepBeta(float from, float to, float seconds, bool pingPong)
    {
        if (wobbleRoot == null) BeginWobble(40, from);
        Stop();
        Run(Sweep(from, to, seconds, pingPong));
    }

    IEnumerator Sweep(float from, float to, float seconds, bool pingPong)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            float u = t / seconds;
            float k = pingPong ? 0.5f - 0.5f * Mathf.Cos(u * Mathf.PI * 2f) : Mathf.SmoothStep(0, 1, u);
            beta = Mathf.Lerp(from, to, k);
            yield return null;
        }

        beta = pingPong ? from : to;
    }

    void Update()
    {
        if (wobbleRoot == null || !wobbleRoot.activeSelf || Mathf.Abs(beta - shownBeta) < 1e-4f) return;
        shownBeta = beta;
        using FrameProbe.Scope probe = FrameProbe.Measure("Staircase wobble");
        Evaluate(beta, wobbled);
        wobbleLine.SetPositions(Points(wobbled, -.05f));
        double worst = 0;
        int worstX = 0;
        for (int b = 0; b < deltaBars.Count; b++)
        {
            int x = Mathf.CeilToInt(WobbleX0) + b;
            int i = Mathf.Clamp(Mathf.RoundToInt((x - WobbleX0) / (WobbleX1 - WobbleX0) * (WobbleSamples - 1)), 0, WobbleSamples - 1);
            float a = Mathf.Clamp((float)original[i], WobbleClampLow, WobbleClampHigh);
            float c = Mathf.Clamp((float)wobbled[i], WobbleClampLow, WobbleClampHigh);
            deltaBars[b].SetPosition(0, new Vector3(x, a, -.08f));
            deltaBars[b].SetPosition(1, new Vector3(x, c, -.08f));
            double d = wobbled[i] - original[i];
            if (System.Math.Abs(d) > System.Math.Abs(worst)) { worst = d; worstX = x; }
        }

        wobbleReadout.Text = $"β = {beta:0.00}      largest difference up to {WobbleX1:0}: {worst:+0.0;−0.0} at x = {worstX}";
    }
}
