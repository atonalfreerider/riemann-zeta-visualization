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

        MovingLimitCurve = Factory.NewLine("limitCurve", .05f, Factory.Viridis.ViridisColor(1), Factory.SoftGlowIntensity);
        MovingLimitCurve.endColor = Factory.Viridis.ViridisColor(0);
        MovingLimitCurve.transform.SetParent(transform, false);
        MovingLimitCurve.positionCount = NumLimitCurvePoints;

        TailCurve = Factory.NewLine("tailCurve", .07f, Factory.Palette.Tail, Factory.GlowIntensity);
        TailCurve.transform.SetParent(transform, false);
        TailCurve.positionCount = 3;
        TailCurve.enabled = ShowTail;

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
    }

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
        Complex s = new(.5d, t);
        int N = ZetaMath.TermCount(t, NumLimitCurvePoints);

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
