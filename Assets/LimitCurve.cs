using System;
using System.Collections.Generic;
using System.Linq;
using Meta.Numerics;
using UnityEngine;

/// <summary>
/// Draws ζ(1/2 + it) as a chain of arrows (the "spiral") at height t, using the
/// Euler–Maclaurin decomposition in <see cref="ZetaMath"/> so the chain lands exactly on ζ.
///
///   viridis segments : the first N-1 terms n^-s of the Dirichlet series
///   orange segment   : the integral tail N^(1-s)/(s-1) plus Bernoulli corrections
/// </summary>
public class LimitCurve : MonoBehaviour
{
    [HideInInspector] public LineRenderer MovingLimitCurve;
    [HideInInspector] public LineRenderer TailCurve;

    readonly List<LimitCurveSpiral> limitCurveSpirals = new();

    [Header("Settings")]
    [Tooltip("Minimum number of spiral arrows. More are added automatically as t grows (N ≈ t/2 + 4).")]
    public int NumLimitCurvePoints = 16;
    public bool ShowSpirals = false;
    [Tooltip("Draw the Euler–Maclaurin tail that closes the spiral onto the exact zeta value.")]
    public bool ShowTail = true;
    public int PreRenderedZeroCurveCount = 0;
    public bool ShowInflectionCurves = false;
    [Tooltip("Fixed number of arrows (0 = automatic, grows with t). The tour uses this to build the chain one arrow at a time.")]
    public int TermOverride = 0;
    public bool ShowArrowheads = true;
    public bool ShowLengthLabels = false;
    const int MaxArrowheads = 12;
    const int MaxLengthLabels = 10;
    readonly List<LineRenderer> arrowheads = new();
    readonly List<TextBox> lengthLabels = new();
    GameObject arrowheadContainer;
    GameObject lengthLabelContainer;

    /// <summary>The decomposition drawn on the last frame (spiral joints, tail, total).</summary>
    public ZetaMath.SpiralDecomposition LastDecomposition { get; private set; }
    public double LastT { get; private set; }

    GameObject limitCurveSpiralContainer;
    readonly List<GameObject> preRendered = new();
    bool initialized;

    void Start()
    {
        EnsureInitialized();

        for (int ii = 0; ii < PreRenderedZeroCurveCount; ii++)
        {
            LineRenderer curve = Factory.NewLine("preRenderedLimitCurve", .05f, Factory.Viridis.ViridisColor(1));
            curve.endColor = Factory.Viridis.ViridisColor(0);
            curve.transform.SetParent(transform, false);

            LineRenderer tail = Factory.NewLine("preRenderedTail", .05f, Factory.Palette.Tail, Factory.SoftGlowIntensity);
            tail.transform.SetParent(curve.transform, false);
            tail.positionCount = 3;

            LineRenderer inflectionCurve = null;
            if (ShowInflectionCurves)
            {
                inflectionCurve = Factory.NewLine("inflectionCurve", .05f, Color.gray);
                inflectionCurve.endColor = Color.white;
                inflectionCurve.transform.SetParent(curve.transform, false);
            }

            RenderLimitCurve(Main.Instance.zetaZeroValues[ii], curve, inflectionCurve, tail);
            preRendered.Add(curve.gameObject);
        }
    }

    void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        MovingLimitCurve = Factory.NewLine("limitCurve", .05f, ChainColor(0f), Factory.SoftGlowIntensity);
        MovingLimitCurve.endColor = ChainColor(1f);
        MovingLimitCurve.transform.SetParent(transform, false);
        MovingLimitCurve.positionCount = NumLimitCurvePoints;

        TailCurve = Factory.NewLine("tailCurve", .07f, Factory.Palette.Tail, Factory.GlowIntensity);
        TailCurve.transform.SetParent(transform, false);
        TailCurve.positionCount = 3;
        TailCurve.enabled = ShowTail;

        arrowheadContainer = new GameObject("Arrowheads");
        arrowheadContainer.transform.SetParent(transform, false);
        lengthLabelContainer = new GameObject("LengthLabels");
        lengthLabelContainer.transform.SetParent(transform, false);
        lengthLabelContainer.SetActive(false);

        limitCurveSpiralContainer = new("LimitCurveSpiralContainer");
        limitCurveSpiralContainer.transform.SetParent(transform, false);

        if (ShowSpirals)
        {
            GenerateSpirals();
        }
    }

    public void GenerateSpirals()
    {
        EnsureInitialized();
        for (int i = 1; i < NumLimitCurvePoints; i++)
        {
            LimitCurveSpiral limitCurveSpiral = new GameObject(i.ToString()).AddComponent<LimitCurveSpiral>();
            limitCurveSpiral.transform.SetParent(limitCurveSpiralContainer.transform, false);
            limitCurveSpiral.Init(i.ToString(), Factory.Viridis.ViridisColor(1f - (float)i / NumLimitCurvePoints));
            limitCurveSpirals.Add(limitCurveSpiral);
        }
    }

    public void SetTailVisible(bool visible)
    {
        EnsureInitialized();
        ShowTail = visible;
        if (TailCurve != null) TailCurve.enabled = visible;
    }

    public void SetSpiralVisible(bool visible)
    {
        EnsureInitialized();
        if (MovingLimitCurve != null) MovingLimitCurve.enabled = visible;
        if (TailCurve != null) TailCurve.enabled = visible && ShowTail;
        arrowheadContainer.SetActive(visible && ShowArrowheads);
        lengthLabelContainer.SetActive(visible && ShowLengthLabels);
    }

    public void SetArrowheadsVisible(bool visible)
    {
        EnsureInitialized();
        ShowArrowheads = visible;
        arrowheadContainer.SetActive(visible);
    }

    public void SetLengthLabelsVisible(bool visible)
    {
        EnsureInitialized();
        ShowLengthLabels = visible;
        lengthLabelContainer.SetActive(visible);
    }

    /// <summary>The spiral's joints in world space from the last draw (index 0 = origin).</summary>
    public Vector3[] LastJoints { get; private set; } = System.Array.Empty<Vector3>();

    public void Reset()
    {
        EnsureInitialized();
        Destroy(limitCurveSpiralContainer);
        limitCurveSpirals.Clear();
        limitCurveSpiralContainer = new("LimitCurveSpiralContainer");
        limitCurveSpiralContainer.transform.SetParent(transform, false);
        if (ShowSpirals)
        {
            GenerateSpirals();
        }
    }

    public void RenderLimitCurve(double t, LineRenderer limitCurve, LineRenderer inflectionCurve)
    {
        EnsureInitialized();
        if (limitCurve == null) limitCurve = MovingLimitCurve;
        RenderLimitCurve(t, limitCurve, inflectionCurve, limitCurve == MovingLimitCurve ? TailCurve : null);
    }

    public void RenderLimitCurve(double t, LineRenderer limitCurve, LineRenderer inflectionCurve, LineRenderer tailCurve)
    {
        float yIntercept = (float)t * Graph.yScale;
        double sigma = Main.Instance != null ? Main.Instance.Sigma : .5d;
        Complex s = new(sigma, t);
        int N = TermOverride > 0 ? Math.Max(2, TermOverride + 1) : ZetaMath.TermCount(t, NumLimitCurvePoints);

        // Each segment n^-s gets shorter (length n^-1/2) and rotates by -t·ln n.
        // see: https://youtu.be/sD0NjbwqlYw?t=418
        ZetaMath.SpiralDecomposition d = ZetaMath.Decompose(s, N);

        Vector3[] joints = new Vector3[N];
        for (int k = 0; k < N; k++)
        {
            joints[k] = ToWorld(d.PartialSums[k], yIntercept);
        }

        limitCurve.positionCount = N;
        limitCurve.SetPositions(joints);

        if (tailCurve != null)
        {
            tailCurve.SetPositions(new[]
            {
                ToWorld(d.SpiralEnd, yIntercept),
                ToWorld(d.AfterTail, yIntercept),
                ToWorld(d.Total, yIntercept)
            });
        }

        bool isMoving = limitCurve == MovingLimitCurve;
        if (isMoving)
        {
            LastDecomposition = d;
            LastT = t;
            LastJoints = joints;
            UpdateArrowheads(joints);
            UpdateLengthLabels(joints, s);

            if (ShowSpirals)
            {
                int spiralCount = Math.Min(N - 1, limitCurveSpirals.Count);
                for (int kk = 1; kk <= spiralCount; kk++)
                {
                    limitCurveSpirals[kk - 1].AddPoint(joints[kk]);
                }
            }
        }

        if (ShowInflectionCurves && inflectionCurve != null)
        {
            // detect when the rotation flips directions and draw a curve through these points
            List<Vector3> inflectionPoints = new();
            int prevSign = 0;
            Complex previousRotation = Complex.One;
            for (int kk = 1; kk < N; kk++)
            {
                Complex rotation = ComplexMath.Pow(1d / kk, t * Complex.I);
                Vector3 cross = Vector3.Cross(
                    new Vector3((float)previousRotation.Re, 0f, (float)previousRotation.Im),
                    new Vector3((float)rotation.Re, 0f, (float)rotation.Im));
                int newSign = Math.Sign(cross.y);
                if (kk > 2 && prevSign != newSign)
                {
                    inflectionPoints.Add(joints[kk - 1] + Vector3.up * .01f);
                }

                previousRotation = rotation;
                prevSign = newSign;
            }

            inflectionCurve.positionCount = inflectionPoints.Count;
            inflectionCurve.SetPositions(inflectionPoints.ToArray());
        }
    }

    public static Vector3 ToWorld(Complex value, float y)
    {
        return new Vector3((float)value.Re, y, (float)value.Im);
    }

    /// <summary>Chain colour from the first arrow (blue) to the tip (yellow); stays bright on a black background.</summary>
    public static Color ChainColor(float fraction)
    {
        return Factory.Viridis.ViridisColor(1f - Mathf.Lerp(0.6f, 0f, Mathf.Clamp01(fraction)));
    }

    void UpdateArrowheads(Vector3[] joints)
    {
        if (!ShowArrowheads) return;
        int count = Math.Min(joints.Length - 1, MaxArrowheads);
        while (arrowheads.Count < count)
        {
            LineRenderer head = Factory.NewLine("arrowhead", .04f, Color.white, Factory.SoftGlowIntensity);
            head.transform.SetParent(arrowheadContainer.transform, false);
            head.positionCount = 3;
            arrowheads.Add(head);
        }

        for (int i = 0; i < arrowheads.Count; i++)
        {
            bool active = i < count;
            arrowheads[i].enabled = active;
            if (!active) continue;

            Vector3 from = joints[i];
            Vector3 to = joints[i + 1];
            Vector3 dir = to - from;
            float length = dir.magnitude;
            if (length < 1e-4f)
            {
                arrowheads[i].enabled = false;
                continue;
            }

            dir /= length;
            Vector3 side = Vector3.Cross(dir, Vector3.up).normalized;
            float size = Mathf.Clamp(length * .3f, .04f, .16f);
            Color c = ChainColor((float)i / Math.Max(1, joints.Length - 1));
            arrowheads[i].startColor = c;
            arrowheads[i].endColor = c;
            arrowheads[i].SetPositions(new[] { to - dir * size + side * size * .55f, to, to - dir * size - side * size * .55f });
        }
    }

    void UpdateLengthLabels(Vector3[] joints, Complex s)
    {
        if (!ShowLengthLabels) return;
        int count = Math.Min(joints.Length - 1, MaxLengthLabels);
        while (lengthLabels.Count < count)
        {
            TextBox label = TextBox.Create("", 1.6f);
            label.transform.SetParent(lengthLabelContainer.transform, false);
            label.Alignment = TMPro.TextAlignmentOptions.Center;
            label.Color = new Color(1f, 1f, 1f, .85f);
            label.gameObject.AddComponent<Billboard>();
            lengthLabels.Add(label);
        }

        for (int i = 0; i < lengthLabels.Count; i++)
        {
            bool active = i < count;
            lengthLabels[i].gameObject.SetActive(active);
            if (!active) continue;
            double length = Math.Pow(i + 1, -s.Re);
            lengthLabels[i].Text = i == 0 ? "1" : length.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            Vector3 mid = (joints[i] + joints[i + 1]) * .5f;
            lengthLabels[i].transform.position = mid + Vector3.up * .05f + Vector3.back * .4f;
        }
    }

    class LimitCurveSpiral : MonoBehaviour
    {
        readonly List<Vector3> spiralPositions = new();

        LineRenderer spiralCurve;

        public void Init(string curveName, Color color)
        {
            spiralCurve = Factory.NewLine(curveName, .01f, color);
            spiralCurve.transform.SetParent(transform, false);
        }

        public void AddPoint(Vector3 point)
        {
            spiralPositions.Add(point);
            spiralCurve.positionCount = spiralPositions.Count;
            spiralCurve.SetPositions(spiralPositions.ToArray());
        }
    }
}
