using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Meta.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Drives the whole visualization:
///   • climbs the critical line s = 1/2 + it and draws ζ(s) as a 3D curve (x = Re ζ, z = Im ζ, y = t·yScale)
///   • drops a glowing marker every time t passes a non-trivial zero (LMFDB table)
///   • feeds the zeros into Riemann's explicit formula to draw the prime staircase ψ(x)
///   • sums cos(γ·log x) over the zeros to show the "prime chorus"
/// The <see cref="TourDirector"/> drives this through the public API below.
/// </summary>
[RequireComponent(typeof(LimitCurve))]
public class Main : MonoBehaviour
{
    readonly List<Vector3> zetaCurvePoints = new();
    Unity.Collections.NativeArray<Vector3> trailBuffer;
    readonly List<GameObject> zetaPoints = new();
    int passedZeroCount;

    public const int NumPrimeCurvePoints = 10000;
    public const int PrimeCurveXLength = 100;
    const float PrimeFilterHeight = 1.5f;
    const double AnimStep = .03d;
    const int MaxStepsPerFrame = 24;

    [Header("Settings")] public bool ShowZetaCurveComponents = false;
    [Tooltip("How fast t advances, in units of t per second.")]
    public double Speed = 1.8d;

    /// <summary>Extra factor on Speed (the storyboard uses it to compress a line's animation into fewer seconds).</summary>
    public double SpeedMultiplier = 1d;

    /// <summary>Real part of s. The visualization lives on the critical line (½); the tour moves it to show what breaks.</summary>
    public double Sigma = .5d;

    /// <summary>How many zeros the cosine chorus sums (-1 = all found so far). The tour builds it up one wave at a time.</summary>
    public int CosineZeroLimit = -1;

    /// <summary>Only the first N zero markers get a numeric label (-1 = all).</summary>
    public int ZeroLabelLimit = -1;

    public bool OnCriticalLine => Math.Abs(Sigma - .5d) < 1e-9;

    /// <summary>When > 0 the cosine chorus is drawn as −(Σ cos)·CosineScale/N so its height stays bounded; ≤ 0 draws the raw sum.</summary>
    public float CosineScale = -1f;

    /// <summary>Renderers that make up a named element, for attention pulses.</summary>
    public IEnumerable<Renderer> RenderersOf(string element)
    {
        switch (element)
        {
            case "prime": yield return primeCurve; foreach (LineRenderer d in daggerLines) yield return d; break;
            case "cosine": yield return cosineWaveCurve; break;
            case "trail": yield return zetaCurve; break;
            case "zeros": foreach (GameObject z in zetaPoints) yield return z.GetComponent<Renderer>(); break;
            case "spiral": foreach (Renderer r in limitCurve.ChainRenderers) yield return r; break;
            case "tail": yield return limitCurve.TailCurve; break;
        }
    }

    /// <summary>Current chorus curve (for difference lines in the tour).</summary>
    public void GetCosinePositions(Vector3[] into) => cosineWaveCurve.GetPositions(into);

    /// <summary>Heights γ of the zeros passed so far.</summary>
    public IReadOnlyList<double> PassedZeros => zetaCurvesByZero.Keys.ToList();

    LineRenderer zetaCurve;
    LineRenderer primeCurve;
    LineRenderer cosineWaveCurve;

    readonly List<LineRenderer> daggerLines = new();
    readonly List<TextBox> primeLabels = new();

    public double[] zetaZeroValues;

    GameObject zetaPointContainer;
    GameObject daggerContainer;
    GameObject equationContainer;

    double currentIter = 0;
    double pendingAdvance = 0;
    bool isPlaying = false;
    bool heavyUpdateDirty = true;

    LimitCurve limitCurve;
    public static Main Instance;

    readonly Dictionary<double, List<double>> zetaCurvesByZero = new();
    GameObject zetaCurveContainer;

    // ---- state exposed to the tour -------------------------------------------------------
    public double CurrentT => currentIter;
    public bool IsPlaying => isPlaying;
    public LimitCurve Limit => limitCurve;
    public Vector3 ZetaTip => zetaCurvePoints.Count > 0 ? zetaCurvePoints[^1] : Vector3.zero;
    public Complex CurrentZeta { get; private set; }
    public IReadOnlyList<GameObject> ZeroMarkers => zetaPoints;
    public int ZeroCount => zetaPoints.Count;
    public float[] PsiValues { get; } = new float[NumPrimeCurvePoints];
    public static double PrimeCurveX(int i) => (i + 1) * (1d / NumPrimeCurvePoints * PrimeCurveXLength);
    public bool DaggersVisible = true;

    /// <summary>Fired when t passes a zero: (index, γ, world position of the marker).</summary>
    public event Action<int, double, Vector3> ZeroPassed;

    /// <summary>Fired when the animation is reset to t = 0.</summary>
    public event Action AnimationReset;

    void Awake()
    {
        Instance = this;
        limitCurve = GetComponent<LimitCurve>();

        // pull the actual zeta zeros for more precise rendering
        // source: https://www.lmfdb.org/zeros/zeta/
        TextAsset zetaZerosFile = Resources.Load<TextAsset>("zetazeros");
        string[] lines = zetaZerosFile.text.Split('\n');

        zetaZeroValues = lines
            .Where(line => !string.IsNullOrEmpty(line))
            .Select(lineNumAndVal => Convert.ToDouble(lineNumAndVal.Split(' ')[1], CultureInfo.InvariantCulture))
            .ToArray();
    }

    void Start()
    {
        zetaCurve = Factory.NewLine("zetaCurve", .05f, Factory.Palette.Zeta, Factory.SoftGlowIntensity);
        zetaCurve.transform.SetParent(transform, false);

        primeCurve = Factory.NewLine("primeCurve", .05f, Factory.Palette.Prime, Factory.SoftGlowIntensity);
        primeCurve.transform.SetParent(transform, false);
        primeCurve.positionCount = NumPrimeCurvePoints;

        cosineWaveCurve = Factory.NewLine("cosineWaveCurve", .05f, Factory.Palette.Cosine, Factory.SoftGlowIntensity);
        cosineWaveCurve.transform.SetParent(transform, false);
        cosineWaveCurve.positionCount = NumPrimeCurvePoints;

        zetaPointContainer = new("Zeta Point Container");
        zetaPointContainer.transform.SetParent(transform, false);
        daggerContainer = new("Dagger Container");
        daggerContainer.transform.SetParent(transform, false);
        zetaCurveContainer = new("Zeta Curve Container");
        zetaCurveContainer.transform.SetParent(transform, false);
        equationContainer = new("Equations");
        equationContainer.transform.SetParent(transform, false);

        RenderedEquations.DrawPrimeEquation().transform.SetParent(equationContainer.transform, true);
        RenderedEquations.DrawZetaEquation().transform.SetParent(equationContainer.transform, true);

        DrawRiemann(0);
    }

    // ---- public control API ---------------------------------------------------------------

    public void Play()
    {
        isPlaying = true;
    }

    public void Pause()
    {
        isPlaying = false;
    }

    public void Toggle()
    {
        isPlaying = !isPlaying;
    }

    /// <summary>Fast-forward (or restart and forward) to height t without animating.</summary>
    public void JumpTo(double t)
    {
        if (t < currentIter)
        {
            ResetAnimation();
        }

        while (currentIter + AnimStep <= t)
        {
            currentIter += AnimStep;
            RiemannZeta(currentIter);
        }

        DrawRiemann(t);
    }

    public void ResetAnimation()
    {
        isPlaying = false;
        pendingAdvance = 0;

        foreach (GameObject zetaPoint in zetaPoints)
        {
            Destroy(zetaPoint);
        }

        zetaPoints.Clear();
        zetaCurvePoints.Clear();
        passedZeroCount = 0;
        foreach (LineRenderer lineRenderer in daggerLines)
        {
            Destroy(lineRenderer.gameObject);
        }

        daggerLines.Clear();
        foreach (TextBox primeLabel in primeLabels)
        {
            Destroy(primeLabel.gameObject);
        }

        primeLabels.Clear();
        zetaCurvesByZero.Clear();
        foreach (Transform child in zetaCurveContainer.transform)
        {
            Destroy(child.gameObject);
        }

        limitCurve.Reset();
        if (RenderedEquations.zetaZeroCounter != null) RenderedEquations.zetaZeroCounter.Text = "";
        heavyUpdateDirty = true;
        AnimationReset?.Invoke();

        DrawRiemann(0);
    }

    public void SetPrimeCurveVisible(bool visible)
    {
        primeCurve.enabled = visible;
        DaggersVisible = visible;
        daggerContainer.SetActive(visible);
    }

    public void SetCosineVisible(bool visible)
    {
        if (visible && !cosineWaveCurve.enabled) heavyUpdateDirty = true;
        cosineWaveCurve.enabled = visible;
        if (visible) DrawCosineWave(currentIter);
    }

    public void SetZetaCurveVisible(bool visible)
    {
        zetaCurve.enabled = visible;
    }

    Coroutine trailStyleRoutine;

    /// <summary>Animate the trail's width and glow (thin and faint → full and bright, or back).</summary>
    public void SetTrailStyle(float width, float intensity, float seconds)
    {
        if (trailStyleRoutine != null) StopCoroutine(trailStyleRoutine);
        trailStyleRoutine = StartCoroutine(TrailStyleRoutine(width, intensity, seconds));
    }

    IEnumerator TrailStyleRoutine(float width, float intensity, float seconds)
    {
        float w0 = zetaCurve.startWidth;
        float i0 = Factory.GetIntensity(zetaCurve);
        if (i0 <= 0f) i0 = Factory.NoGlow;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, seconds <= 0f ? 1f : t / seconds);
            float w = Mathf.Lerp(w0, width, k);
            zetaCurve.startWidth = w;
            zetaCurve.endWidth = w;
            Factory.SetIntensity(zetaCurve, Mathf.Lerp(i0, intensity, k));
            yield return null;
        }

        zetaCurve.startWidth = width;
        zetaCurve.endWidth = width;
        Factory.SetIntensity(zetaCurve, intensity);
    }

    Coroutine cosineBlendRoutine;
    Coroutine cosineBlendMarker;

    /// <summary>Morph the chorus from its current shape to the sum over the first <paramref name="limit"/> collisions.</summary>
    public void SetCosineZeroLimitAnimated(int limit, float seconds)
    {
        if (cosineBlendMarker != null) StopCoroutine(cosineBlendMarker);
        cosineBlendMarker = StartCoroutine(CosineBlendRoutine(limit, seconds));
        cosineBlendRoutine = cosineBlendMarker;
    }

    IEnumerator CosineBlendRoutine(int limit, float seconds)
    {
        Vector3[] from = new Vector3[NumPrimeCurvePoints];
        cosineWaveCurve.GetPositions(from);
        CosineZeroLimit = limit;
        cosineBlendRoutine = null;
        Vector3[] to = ComputeCosineWave(currentIter);
        cosineBlendRoutine = cosineBlendMarker;
        Vector3[] blend = new Vector3[NumPrimeCurvePoints];
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / seconds);
            for (int i = 0; i < blend.Length; i++) blend[i] = Vector3.Lerp(from[i], to[i], k);
            cosineWaveCurve.SetPositions(blend);
            yield return null;
        }

        cosineWaveCurve.SetPositions(to);
        cosineBlendRoutine = null;
        cosineBlendMarker = null;
    }

    public void SetEquationsVisible(bool visible)
    {
        equationContainer.SetActive(visible);
    }

    public void SetZeroMarkersVisible(bool visible)
    {
        zetaPointContainer.SetActive(visible);
    }

    public void SetGridVisible(bool visible)
    {
        GetComponent<Graph>()?.SetVisible(visible);
    }

    /// <summary>Change the real part of s and redraw the trail from t = 0 (markers only drop on the critical line).</summary>
    public void SetSigma(double sigma)
    {
        if (Math.Abs(sigma - Sigma) < 1e-12) return;
        Sigma = sigma;
        double t = currentIter;
        ResetAnimation();
        JumpTo(t);
    }

    public void SetCosineZeroLimit(int limit)
    {
        CosineZeroLimit = limit;
        heavyUpdateDirty = true;
        DrawCosineWave(currentIter);
    }

    public Vector3 ZeroMarkerPosition(int index)
    {
        return new Vector3(0, (float)zetaZeroValues[index] * Graph.yScale, 0);
    }

    // ---- animation -----------------------------------------------------------------------

    void Update()
    {
        HandleKeys();

        if (!isPlaying) return;

        // accumulate so slow speeds (less than one AnimStep per frame) still advance
        pendingAdvance += Speed * SpeedMultiplier * Time.deltaTime;
        int steps = 0;
        while (pendingAdvance >= AnimStep && steps < MaxStepsPerFrame)
        {
            pendingAdvance -= AnimStep;
            currentIter += AnimStep;
            RiemannZeta(currentIter);
            steps++;
        }

        if (steps == MaxStepsPerFrame) pendingAdvance = 0; // drop backlog rather than stall

        DrawRiemann(currentIter);
    }

    void HandleKeys()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            Toggle();
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            ResetAnimation();
        }

        if (keyboard.rKey.wasPressedThisFrame)
        {
            limitCurve.ShowSpirals = !limitCurve.ShowSpirals;
            limitCurve.GenerateSpirals();
        }
    }

    void DrawRiemann(double iter)
    {
        currentIter = iter;

        limitCurve.RenderLimitCurve(iter, limitCurve.MovingLimitCurve, null);
        if (zetaCurvePoints.Count == 0)
        {
            RiemannZeta(iter);
        }

        UpdateZetaCurves(iter);
        if (heavyUpdateDirty)
        {
            // the prime staircase and the cosine chorus only change when a new zero joins the sum
            heavyUpdateDirty = false;
            UpdatePrimeNumberTheorem();
            DrawCosineWave(iter);
        }
    }

    void RiemannZeta(double iter)
    {
        // compute the actual Riemann Zeta function output (analytic continuation, Meta.Numerics)
        Complex input = new(Sigma, iter);
        Complex output = ZetaMath.Exact(input);
        CurrentZeta = output;

        Vector3 point = new(
            (float)output.Re,
            (float)iter * Graph.yScale,
            (float)output.Im);

        zetaCurvePoints.Add(point);
        if (!trailBuffer.IsCreated || trailBuffer.Length < zetaCurvePoints.Count)
        {
            int capacity = Mathf.Max(4096, trailBuffer.IsCreated ? trailBuffer.Length * 2 : 0);
            Unity.Collections.NativeArray<Vector3> grown = new(capacity, Unity.Collections.Allocator.Persistent);
            if (trailBuffer.IsCreated)
            {
                Unity.Collections.NativeArray<Vector3>.Copy(trailBuffer, grown, trailBuffer.Length);
                trailBuffer.Dispose();
            }

            trailBuffer = grown;
        }

        trailBuffer[zetaCurvePoints.Count - 1] = point;

        zetaCurve.positionCount = zetaCurvePoints.Count;
        zetaCurve.SetPositions(trailBuffer.GetSubArray(0, zetaCurvePoints.Count));
    }

    void OnDestroy()
    {
        if (trailBuffer.IsCreated) trailBuffer.Dispose();
    }

    void UpdateZetaCurves(double iter)
    {
        if (!OnCriticalLine) return; // off the line the tip never hits the centre, so no zeros to collect
        while (passedZeroCount < zetaZeroValues.Length && zetaZeroValues[passedZeroCount] <= iter) passedZeroCount++;
        if (passedZeroCount == zetaPoints.Count && passedZeroCount == zetaCurvesByZero.Count) return; // nothing new this frame
        List<double> zetaZeros = new(passedZeroCount);
        for (int i = 0; i < passedZeroCount; i++) zetaZeros.Add(zetaZeroValues[i]);
        foreach (double zetaZero in zetaZeros)
        {
            if (zetaCurvesByZero.ContainsKey(zetaZero)) continue;

            List<double> points = new();
            zetaCurvesByZero.Add(zetaZero, points);
            heavyUpdateDirty = true;

            Vector3[] primeCurvePoints = new Vector3[NumPrimeCurvePoints];

            const double curveResolution = 1d / NumPrimeCurvePoints * PrimeCurveXLength;
            double x = curveResolution;
            for (int i = 0; i < NumPrimeCurvePoints; i++)
            {
                double periodicSum = ZetaMath.ExplicitFormulaTerm(x, zetaZero);
                points.Add(periodicSum);

                if (ShowZetaCurveComponents)
                {
                    primeCurvePoints[i] = new Vector3((float)x, (float)periodicSum * 100, 0f);
                }

                x += curveResolution;
            }

            if (ShowZetaCurveComponents)
            {
                LineRenderer componentCurve =
                    Factory.NewLine(zetaZero.ToString(CultureInfo.InvariantCulture), .01f, Color.gray);
                componentCurve.transform.SetParent(zetaCurveContainer.transform, false);
                componentCurve.positionCount = primeCurvePoints.Length;
                componentCurve.SetPositions(primeCurvePoints);
            }
        }

        DrawZetaZeroPoints(zetaZeros);
    }

    /// <summary>
    /// https://medium.com/cantors-paradise/the-riemann-hypothesis-explained-fa01c1f75d3f
    /// https://en.wikipedia.org/wiki/Explicit_formulae_for_L-functions
    /// https://github.com/danielhutama/Riemann-Explicit-Formula-for-Primes
    /// </summary>
    void UpdatePrimeNumberTheorem()
    {
        Vector3[] primeCurvePoints = new Vector3[NumPrimeCurvePoints];
        List<double>[] components = zetaCurvesByZero.Values.ToArray();

        const double curveResolution = 1d / NumPrimeCurvePoints * PrimeCurveXLength;
        double x = curveResolution;
        for (int i = 0; i < NumPrimeCurvePoints; i++)
        {
            double periodicSum = 0d;
            for (int c = 0; c < components.Length; c++)
            {
                periodicSum += components[c][i];
            }

            double psi = ZetaMath.Psi(x, periodicSum);
            PsiValues[i] = (float)psi;

            Vector3 point = new((float)x, (float)psi, 0f);
            if (psi < 0)
            {
                point = Vector3.zero;
            }

            primeCurvePoints[i] = point;
            x += curveResolution;
        }

        lastPrimePoints = primeCurvePoints;
        if (primeBlendRoutine == null) primeCurve.SetPositions(primeCurvePoints);
        DrawDaggers(primeCurvePoints);
    }

    Vector3[] lastPrimePoints;
    Coroutine primeBlendRoutine;

    /// <summary>Animate the staircase into existence: start from the straight line y = x, then subtract the waves.</summary>
    public void BuildStaircase(float seconds)
    {
        if (primeBlendRoutine != null) StopCoroutine(primeBlendRoutine);
        primeBlendRoutine = StartCoroutine(BuildStaircaseRoutine(seconds));
    }

    IEnumerator BuildStaircaseRoutine(float seconds)
    {
        heavyUpdateDirty = true;
        UpdatePrimeNumberTheorem();
        Vector3[] to = lastPrimePoints;
        Vector3[] line = new Vector3[NumPrimeCurvePoints];
        Vector3[] blend = new Vector3[NumPrimeCurvePoints];
        for (int i = 0; i < line.Length; i++)
        {
            float x = (float)PrimeCurveX(i);
            line[i] = to[i] == Vector3.zero ? Vector3.zero : new Vector3(x, x, 0);
        }

        daggerContainer.SetActive(false);
        primeCurve.enabled = true;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / seconds);
            for (int i = 0; i < blend.Length; i++) blend[i] = Vector3.Lerp(line[i], to[i], k);
            primeCurve.SetPositions(blend);
            yield return null;
        }

        primeCurve.SetPositions(to);
        daggerContainer.SetActive(DaggersVisible);
        primeBlendRoutine = null;
    }

    void DrawDaggers(Vector3[] primeCurvePoints)
    {
        foreach (LineRenderer lineRenderer in daggerLines)
        {
            Destroy(lineRenderer.gameObject);
        }

        daggerLines.Clear();

        foreach (TextBox primeLabel in primeLabels)
        {
            Destroy(primeLabel.gameObject);
        }

        primeLabels.Clear();

        // Collect steep runs, then keep exactly one (the tallest) per integer so Gibbs ringing
        // around small primes cannot produce several red lines for the same step.
        Dictionary<int, (float height, Vector3 top)> best = new();
        bool verticalRun = false;
        Vector3 previousPoint = Vector3.zero;
        List<Vector3> verticalRunPoints = new();
        const float primeFindingThreshold = 1f / NumPrimeCurvePoints * .5f;
        foreach (Vector3 point in primeCurvePoints)
        {
            if (point.x < 1.6f)
            {
                previousPoint = point;
                continue;
            }

            if (point.y - previousPoint.y > primeFindingThreshold)
            {
                verticalRun = true;
                verticalRunPoints.Add(point);
            }

            if (verticalRun && point.y - previousPoint.y < primeFindingThreshold)
            {
                verticalRun = false;
                float height = verticalRunPoints[^1].y - verticalRunPoints[0].y;
                Vector3 halfwayPoint = verticalRunPoints[Mathf.FloorToInt(verticalRunPoints.Count * .5f)];
                int estPrime = Mathf.RoundToInt(halfwayPoint.x);
                bool small = estPrime <= 3;
                bool nearInteger = Math.Abs(halfwayPoint.x - estPrime) < (small ? .3f : .5f);
                float needed = small ? .35f : PrimeFilterHeight;
                if (nearInteger && height > needed)
                {
                    if (!best.TryGetValue(estPrime, out (float height, Vector3 top) current) || height > current.height)
                    {
                        best[estPrime] = (height, halfwayPoint);
                    }
                }

                verticalRunPoints.Clear();
            }

            previousPoint = point;
        }

        foreach (KeyValuePair<int, (float height, Vector3 top)> entry in best)
        {
            int estPrime = entry.Key;
            Vector3 halfwayPoint = entry.Value.top;
            LineRenderer daggerLine = Factory.NewLine("dagger", .08f, Factory.Palette.Dagger, Factory.GlowIntensity);
            daggerLine.transform.SetParent(daggerContainer.transform, false);
            daggerLine.positionCount = 2;
            daggerLine.SetPositions(new[] { new Vector3(estPrime, halfwayPoint.y, 0), new Vector3(estPrime, 0, 0) });
            daggerLines.Add(daggerLine);

            TextBox primeLabel = TextBox.Create(estPrime.ToString(CultureInfo.InvariantCulture), 10f);
            primeLabel.transform.SetParent(daggerContainer.transform, false);
            primeLabel.Alignment = TextAlignmentOptions.Center;
            primeLabel.transform.localPosition = new Vector3(estPrime, -.5f, 0);
            primeLabel.Color = Factory.Palette.Dagger;
            primeLabels.Add(primeLabel);
        }
    }

    void DrawZetaZeroPoints(List<double> zetaZeros)
    {
        int count = 0;
        foreach (double zetaZero in zetaZeros)
        {
            // render sphere points
            count++;
            if (zetaPoints.Count < count)
            {
                GameObject point = Factory.NewSphere($"zero {count}", .3f, Factory.Palette.Zero);
                point.transform.SetParent(zetaPointContainer.transform, false);
                point.transform.Translate(Vector3.up * ((float)zetaZero * Graph.yScale));
                zetaPoints.Add(point);

                if (ZeroLabelLimit < 0 || count <= ZeroLabelLimit)
                {
                    TextBox label = TextBox.Create(zetaZero.ToString("F2", CultureInfo.InvariantCulture), 6f);
                    label.Alignment = TextAlignmentOptions.Right;
                    label.transform.SetParent(point.transform, false);
                    label.transform.Translate(Vector3.left * .5f);
                    label.Color = Factory.Palette.Zero;
                    label.gameObject.AddComponent<Billboard>();
                }

                ZeroPassed?.Invoke(count - 1, zetaZero, point.transform.position);
            }
        }

        if (count > 0)
        {
            RenderedEquations.zetaZeroCounter.Text = count.ToString();
        }
    }

    void DrawCosineWave(double iter)
    {
        if (!cosineWaveCurve.enabled) return;
        if (cosineBlendRoutine != null) return; // a blend is in progress; it will set the final shape
        cosineWaveCurve.SetPositions(ComputeCosineWave(iter));
    }

    Vector3[] ComputeCosineWave(double iter)
    {
        List<double> zetaZeros = zetaZeroValues.TakeWhile(t => !(t > iter)).ToList();
        if (CosineZeroLimit >= 0 && zetaZeros.Count > CosineZeroLimit) zetaZeros = zetaZeros.Take(CosineZeroLimit).ToList();
        Vector3[] cosineWavePoints = new Vector3[NumPrimeCurvePoints];
        const double curveResolution = 1d / NumPrimeCurvePoints * PrimeCurveXLength;
        float x = (float)curveResolution;

        float scale = CosineScale > 0f && zetaZeros.Count > 0 ? CosineScale / zetaZeros.Count : 1f;
        for (int i = 0; i < NumPrimeCurvePoints; i++)
        {
            float y = 0;
            float logX = Mathf.Log(x);
            foreach (double zetaZero in zetaZeros)
            {
                y -= Mathf.Cos((float)zetaZero * logX);
            }

            // x = 1 makes every cosine 1 and produces a meaningless spike; start the curve at 1.5
            cosineWavePoints[i] = x < 1.5f ? new Vector3(1.5f, 0, 0) : new Vector3(x, y * scale, 0);
            x += (float)curveResolution;
        }

        return cosineWavePoints;
    }
}
