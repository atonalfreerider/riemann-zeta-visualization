using UnityEngine;


public class Graph : MonoBehaviour
{
    const int axisHeight = 200;
    public const float yScale = .3f;
    
    void Start()
    {
        LineRenderer xAxis = Factory.NewLine("xAxis", .05f, Color.gray);
        xAxis.transform.SetParent(transform, false);
        xAxis.positionCount = 2;
        xAxis.SetPositions(new[] { Vector3.zero, new Vector3(axisHeight, 0, 0) });

        float tickLength = .3f;

        for (int i = 0; i < axisHeight; i++)
        {
            LineRenderer xTick = Factory.NewLine("xTick", .03f, Color.gray);
            xTick.transform.SetParent(xAxis.transform, false);
            xTick.positionCount = 2;
            xTick.SetPositions(new[]
            {
                new Vector3(i, -tickLength * .5f, 0),
                new Vector3(i, tickLength * .5f, 0)
            });
        }

        LineRenderer yAxis = Factory.NewLine("yAxis", .05f, Color.gray);
        yAxis.transform.SetParent(transform, false);
        yAxis.positionCount = 2;
        yAxis.SetPositions(new[] { Vector3.zero, new Vector3(0, axisHeight, 0) });

        for (int i = 0; i < axisHeight; i++)
        {
            LineRenderer yTick = Factory.NewLine("yTick", .03f, Color.gray);
            yTick.transform.SetParent(yAxis.transform, false);
            yTick.positionCount = 2;
            yTick.SetPositions(new[]
            {
                new Vector3(-tickLength * .5f, i, 0),
                new Vector3(tickLength * .5f, i, 0)
            });
        }

        tickLength = .1f;
        for (int i = 0; i < axisHeight / yScale; i++)
        {
            LineRenderer yScaleTick = Factory.NewLine("yScaleTick", .03f, Color.gray);
            yScaleTick.transform.SetParent(yAxis.transform, false);
            yScaleTick.positionCount = 2;
            yScaleTick.SetPositions(new[]
            {
                new Vector3(-tickLength * .5f, i * yScale, 0),
                new Vector3(tickLength * .5f, i * yScale, 0)
            });
        }
    }
}