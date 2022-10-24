using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Meta.Numerics;
using Meta.Numerics.Functions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LimitCurve))]
public class Main : MonoBehaviour
{
    readonly List<Vector3> zetaCurvePoints = new();
    readonly List<GameObject> zetaPoints = new();

    const int NumPrimeCurvePoints = 10000;
    const int PrimeCurveXLength = 100;
    const float PrimeFilterHeight = 1.5f;
    const double AnimStep = .03d;

    [Header("Settings")]
    public bool ShowZetaCurveComponents = false;

    LineRenderer zetaCurve;
    LineRenderer primeCurve;

    readonly List<LineRenderer> daggerLines = new();
    readonly List<TextBox> primeLabels = new();

    public double[] zetaZeroValues;

    GameObject zetaPointContainer;
    GameObject daggerContainer;

    Coroutine animator;
    double currentIter = 0;
    bool isPlaying = false;

    LimitCurve limitCurve;
    public static Main Instance;

    readonly Dictionary<double, List<double>> zetaCurvesByZero = new();
    GameObject zetaCurveContainer;

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
            .Select(lineNumAndVal => Convert.ToDouble(lineNumAndVal.Split(' ')[1]))
            .ToArray();
    }

    void Start()
    {
        zetaCurve = Factory.NewLine("zetaCurve", .05f, new Color(.8f, .8f, .8f));
        zetaCurve.transform.SetParent(transform, false);

        primeCurve = Factory.NewLine("primeCurve", .05f, Color.white);
        primeCurve.transform.SetParent(transform, false);
        primeCurve.positionCount = NumPrimeCurvePoints;

        zetaPointContainer = new("Zeta Point Container");
        zetaPointContainer.transform.SetParent(transform, false);
        daggerContainer = new("Dagger Container");
        daggerContainer.transform.SetParent(transform, false);
        zetaCurveContainer = new("Zeta Curve Container");
        zetaCurveContainer.transform.SetParent(transform, false);

        RenderedEquations.DrawPrimeEquation();
        RenderedEquations.DrawZetaEquation();
        UpdatePrimeNumberTheorem();
    }

    IEnumerator AnimateRiemann(double iter)
    {
        DrawRiemann(iter);

        yield return null;
        iter += AnimStep;
        animator = StartCoroutine(AnimateRiemann(iter));
    }

    void DrawRiemann(double iter)
    {
        currentIter = iter;

        limitCurve.RenderLimitCurve(iter, limitCurve.MovingLimitCurve, null);
        RiemannZeta(iter);
        UpdateZetaCurves(iter);
        UpdatePrimeNumberTheorem();
    }

    void RiemannZeta(double iter)
    {
        // compute the actual Riemann Zeta function output
        Complex input = new(.5d, iter);
        Complex output = AdvancedComplexMath.RiemannZeta(input);

        Vector3 point = new(
            (float)output.Re,
            (float)iter * Graph.yScale,
            (float)output.Im);

        zetaCurvePoints.Add(point);

        zetaCurve.positionCount = zetaCurvePoints.Count;
        zetaCurve.SetPositions(zetaCurvePoints.ToArray());
    }

    void UpdateZetaCurves(double iter)
    {
        double[] zetaZeros = zetaZeroValues.TakeWhile(t => !(t > iter)).ToArray();
        foreach (double zetaZero in zetaZeros)
        {
            if (zetaCurvesByZero.ContainsKey(zetaZero)) continue;

            List<double> points = new();
            zetaCurvesByZero.Add(zetaZero, points);

            // pull out all the zeta zeros from the values less than the current iterator
            Vector3[] primeCurvePoints = new Vector3[NumPrimeCurvePoints];

            const double curveResolution = 1d / NumPrimeCurvePoints * PrimeCurveXLength;
            double x = curveResolution;
            for (int i = 0; i < NumPrimeCurvePoints; i++)
            {
                double periodicSum = PeriodicEstimate(x, zetaZero);
                points.Add(periodicSum);

                if (ShowZetaCurveComponents)
                {
                    Vector3 point = new(
                        (float)x,
                        (float)periodicSum * 100,
                        0f);

                    primeCurvePoints[i] = point;
                }

                x += curveResolution;
            }

            if (ShowZetaCurveComponents)
            {
                LineRenderer zetaCurve =
                    Factory.NewLine(zetaZero.ToString(CultureInfo.InvariantCulture), .01f, Color.gray);
                zetaCurve.transform.SetParent(zetaCurveContainer.transform, false);
                zetaCurve.positionCount = primeCurvePoints.Length;
                zetaCurve.SetPositions(primeCurvePoints);
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
        // pull out all the zeta zeros from the values less than the current iterator
        Vector3[] primeCurvePoints = new Vector3[NumPrimeCurvePoints];

        const double curveResolution = 1d / NumPrimeCurvePoints * PrimeCurveXLength;
        double x = curveResolution;
        for (int i = 0; i < NumPrimeCurvePoints; i++)
        {
            double periodicSum = zetaCurvesByZero.Values.Sum(listItem => listItem[i]);
            double psi = Psi(x, periodicSum);

            Vector3 point = new(
                (float)x,
                (float)psi,
                0f);

            if (psi < 0)
            {
                point = Vector3.zero;
            }

            primeCurvePoints[i] = point;
            x += curveResolution;
        }

        primeCurve.SetPositions(primeCurvePoints);
        DrawDaggers(primeCurvePoints);
    }

    static double Psi(double x, double periodicSum)
    {
        double estimateTerm = 4 * Math.Sqrt(x) * periodicSum;
        double trivialZerosTerm = 0d;
        // this term is always NaN?
        //double trivialZerosTerm = .5d * Math.Log(1d - Math.Pow(x, -2d));

        double shiftTerm = Math.Log(2 * Math.PI);
        return x - estimateTerm - trivialZerosTerm - shiftTerm;
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

        bool verticalRun = false;
        Vector3 previousPoint = Vector3.zero;
        List<Vector3> verticalRunPoints = new();
        const float primeFindingThreshold = 1f / NumPrimeCurvePoints * .5f;
        foreach (Vector3 point in primeCurvePoints)
        {
            if (point.x < 2f)
            {
                previousPoint = point;
                continue;
            }

            if (point.y - previousPoint.y > primeFindingThreshold)
            {
                if (!verticalRun)
                {
                    verticalRun = true;
                }

                verticalRunPoints.Add(point);
            }

            if (verticalRun && point.y - previousPoint.y < primeFindingThreshold)
            {
                verticalRun = false;

                if (verticalRunPoints[0].x < 3.5f ||
                    verticalRunPoints[^1].y - verticalRunPoints[0].y > PrimeFilterHeight)
                {
                    Vector3 halfwayPoint = verticalRunPoints[Mathf.FloorToInt(verticalRunPoints.Count * .5f)];
                    int estPrime = Convert.ToInt32(halfwayPoint.x);
                    if (verticalRunPoints[0].x < 2.5f && verticalRunPoints[0].x - 2 > .01f)
                    {
                        // hack: cleanup around 2
                        verticalRunPoints.Clear();
                        previousPoint = point;
                        continue;
                    }

                    if (verticalRunPoints[0].x is >= 2.5f and < 3.5f && Math.Abs(verticalRunPoints[0].x - 3) > .15f)
                    {
                        // hack: cleanup around 3
                        verticalRunPoints.Clear();
                        previousPoint = point;
                        continue;
                    }

                    LineRenderer daggerLine = Factory.NewLine("dagger", .08f, Color.red);
                    daggerLine.transform.SetParent(daggerContainer.transform, false);
                    daggerLine.positionCount = 2;
                    daggerLine.SetPositions(new[] { halfwayPoint, new Vector3(halfwayPoint.x, 0, 0) });
                    daggerLines.Add(daggerLine);

                    TextBox primeLabel = TextBox.Create(estPrime.ToString(CultureInfo.InvariantCulture), 10f);
                    primeLabel.transform.SetParent(daggerContainer.transform, false);
                    primeLabel.Alignment = TextAlignmentOptions.Center;
                    primeLabel.transform.localPosition = new Vector3(estPrime, -.5f, 0);
                    primeLabel.Color = Color.red;

                    primeLabels.Add(primeLabel);
                }

                verticalRunPoints.Clear();
            }

            previousPoint = point;
        }
    }

    static double PeriodicEstimate(double x, double zetaZero)
    {
        if (x <= double.Epsilon) return 0d;
        
        double term1 = Math.Cos(zetaZero * Math.Log(x));
        double term2 = 2 * zetaZero * Math.Sin(zetaZero * Math.Log(x));
        double term3 = 1 + 4 * zetaZero * zetaZero;
        return (term1 + term2) / term3;
    }

    void DrawZetaZeroPoints(IEnumerable<double> zetaZeros)
    {
        int count = 0;
        foreach (double zetaZero in zetaZeros)
        {
            // render sphere points
            count++;
            if (zetaPoints.Count < count)
            {
                GameObject point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                point.transform.SetParent(zetaPointContainer.transform, false);
                point.transform.localScale = Vector3.one * .3f;
                point.transform.Translate(Vector3.up * ((float)zetaZero * Graph.yScale));
                point.GetComponent<MeshRenderer>().material = Factory.mainMat;
                point.GetComponent<MeshRenderer>().material.color = Color.yellow;
                zetaPoints.Add(point);

                TextBox label = TextBox.Create(zetaZero.ToString(CultureInfo.InvariantCulture), 10f);
                label.Alignment = TextAlignmentOptions.Right;
                label.transform.SetParent(point.transform, false);
                label.transform.Translate(Vector3.left * .5f);
            }
        }

        if (count > 0)
        {
            RenderedEquations.zetaZeroCounter.Text = count.ToString();
        }
    }

    void Toggle()
    {
        isPlaying = !isPlaying;
        if (isPlaying)
        {
            animator = StartCoroutine(AnimateRiemann(currentIter));
        }
        else
        {
            StopCoroutine(animator);
            animator = null;
            UpdateZetaCurves(0);
        }
    }

    void Reset()
    {
        isPlaying = false;
        if (animator != null)
        {
            StopCoroutine(animator);
            animator = null;
        }

        foreach (GameObject zetaPoint in zetaPoints)
        {
            Destroy(zetaPoint);
        }
        zetaPoints.Clear();
        zetaCurvePoints.Clear();
        foreach (LineRenderer lineRenderer in daggerLines)
        {
            Destroy(lineRenderer.gameObject);
        }
        daggerLines.Clear();
        zetaCurvesByZero.Clear();
        
        limitCurve.Reset();
        
        DrawRiemann(0);
    }

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Toggle();
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Reset();
        }

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            limitCurve.ShowSpirals = !limitCurve.ShowSpirals;
            limitCurve.GenerateSpirals();
        }
    }
}