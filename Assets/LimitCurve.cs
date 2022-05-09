using System;
using System.Collections.Generic;
using System.Linq;
using Meta.Numerics;
using UnityEngine;

public class LimitCurve : MonoBehaviour
{
    [HideInInspector]
    public LineRenderer MovingLimitCurve;
    readonly List<LimitCurveSpiral> limitCurveSpirals = new();

    [Header("Settings")]
    public int NumLimitCurvePoints = 16;
    public bool ShowSpirals = false;
    public int PreRenderedZeroCurveCount = 0;
    public bool ShowInflectionCurves = false;
    public bool ShowFlattenedHoles = false;

    void Start()
    {
        MovingLimitCurve = Factory.NewLine("limitCurve", .05f, Factory.Viridis.ViridisColor(1));
        MovingLimitCurve.endColor = Factory.Viridis.ViridisColor(0);
        MovingLimitCurve.transform.SetParent(transform, false);
        MovingLimitCurve.positionCount = NumLimitCurvePoints;

        GameObject limitCurveSpiralContainer = new("LimitCurveSpiralContainer");
        limitCurveSpiralContainer.transform.SetParent(transform, false);

        if (ShowSpirals)
        {
            for (int i = 1; i < NumLimitCurvePoints; i++)
            {
                LimitCurveSpiral limitCurveSpiral = new GameObject(i.ToString()).AddComponent<LimitCurveSpiral>();
                limitCurveSpiral.transform.SetParent(limitCurveSpiralContainer.transform, false);
                limitCurveSpiral.Init(i.ToString(), Factory.Viridis.ViridisColor(1f - (float)i / NumLimitCurvePoints));
                limitCurveSpirals.Add(limitCurveSpiral);
            }
        }

        for (int ii = 0; ii < PreRenderedZeroCurveCount; ii++)
        {
            LineRenderer curve = Factory.NewLine("limitCurve", .05f, Factory.Viridis.ViridisColor(1));
            curve.endColor = Factory.Viridis.ViridisColor(0);
            curve.transform.SetParent(transform, false);
            curve.positionCount = NumLimitCurvePoints;

            LineRenderer inflectionCurve = null;
            if (ShowInflectionCurves)
            {
                inflectionCurve = Factory.NewLine("inflectionCurve", .05f, Color.gray);
                inflectionCurve.endColor = Color.white;
                inflectionCurve.transform.SetParent(transform, false);
            }

            RenderLimitCurve(Main.Instance.zetaZeroValues[ii], curve, inflectionCurve);
        }
    }
    
    public void RenderLimitCurve(double iter, LineRenderer limitCurve, LineRenderer inflectionCurve)
    {
        float yIntercept = (float)iter * Graph.yScale;
        // graph the limit that results in the Riemann Zeta funtion output
        DoubleVector2[] limitCurvePoints = new DoubleVector2[NumLimitCurvePoints];
        DoubleVector2 previousPoint = new(
            0d,
            0d);

        limitCurvePoints[0] = previousPoint;
        List<DoubleVector2> inflectionPoints = new();
        int prevSign = 0;
        int newSign = 0;
        Complex previousRotation = Complex.One;
        
        List<double> segLengths = new(); 

        for (int kk = 1; kk < NumLimitCurvePoints; kk++)
        {
            // each segment gets shorter, and bends as the complex exponent rotates it in a circle 
            // eventually this process reaches a limit on the Zeta curve 
            // see: https://youtu.be/sD0NjbwqlYw?t=418 
            double segLength = Math.Pow(1d / kk, .5d);
            Complex rotation = ComplexMath.Pow(1d / kk, iter * Complex.I);
            double xTerm = previousPoint.X + segLength * rotation.Re;
            double zTerm = previousPoint.Z + segLength * rotation.Im;

            if (ShowInflectionCurves)
            {
                // detect when the rotation flips directions and draw a curve through these points
                Vector3 cross = Vector3.Cross(
                    new Vector3((float)previousRotation.Re, 0f, (float)previousRotation.Im),
                    new Vector3((float)rotation.Re, 0f, (float)rotation.Im));
                newSign = Math.Sign(cross.y);

                if (kk > 2 && prevSign != newSign)
                {
                    inflectionPoints.Add(previousPoint);
                }
            }

            if (ShowFlattenedHoles)
            {
                if (kk > 2 && Math.Sign(zTerm) != Math.Sign(previousPoint.Z))
                {
                    // curve has crossed the x axis -> make a hole  
                    double xIntercept = (xTerm - previousPoint.X) / (zTerm - previousPoint.Z) * -zTerm + xTerm;

                    double dFromJoint =
                        Math.Sqrt(Math.Pow(xIntercept - previousPoint.X, 2d) + Math.Pow(0d - previousPoint.Z, 2d));
                    double flattened = segLengths.Sum() + dFromJoint;

                    GameObject point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    point.transform.SetParent(transform, false);
                    point.transform.localScale = Vector3.one * .03f;
                    point.GetComponent<MeshRenderer>().material = Factory.mainMat;
                    point.GetComponent<MeshRenderer>().material.color = Color.red;
                    point.transform.position = new Vector3(
                        (float)flattened,
                        yIntercept,
                        0);
                }

                segLengths.Add(segLength);
            }

            previousPoint = new( 
                xTerm, 
                zTerm); 
            limitCurvePoints[kk] = previousPoint; 
            
            DoubleVector2 newPoint = new(
                xTerm,
                zTerm);
            
            limitCurvePoints[kk] = newPoint;

            if (ShowSpirals)
            {
                limitCurveSpirals[kk - 1].AddPoint(previousPoint.Convert(yIntercept));
            }

            previousPoint = newPoint;
            previousRotation = rotation;
            prevSign = newSign;
        }

        Vector3[] limitCurvePointsVector3 = limitCurvePoints.Select(x => x.Convert(yIntercept)).ToArray();
        limitCurve.SetPositions(limitCurvePointsVector3);

        if (ShowInflectionCurves && inflectionCurve != null)
        {
            Vector3[] inflectionCurvePointsVector3 = inflectionPoints.Select(x => x.Convert(yIntercept + .01f)).ToArray();
            inflectionCurve.positionCount = inflectionPoints.Count;
            inflectionCurve.SetPositions(inflectionCurvePointsVector3);
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