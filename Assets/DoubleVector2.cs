using UnityEngine;

public class DoubleVector2
{
    public readonly double X;
    public readonly double Z;
    public DoubleVector2(double x, double z)
    {
        X = x;
        Z = z;
    }

    public Vector3 Convert(float yIntercept)
    {
        return new(
            (float) X,
            yIntercept,
            (float) Z);
    }
}