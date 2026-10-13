using System.Collections.Generic;
using UnityEngine;


public class Graph : MonoBehaviour
{
    const int axisHeight = 200;
    public const float yScale = .3f;

    GameObject root;

    public void SetVisible(bool visible)
    {
        if (root != null) root.SetActive(visible);
    }

    /// <summary>
    /// VR: the axes stay as lines, but the ~1070 tick marks become one mesh (one draw call instead of a thousand).
    /// </summary>
    static void BuildTicksBatched(Transform parent, LineRenderer xAxis, Color grid)
    {
        LineRenderer yAxis = Factory.NewLine("yAxis", .05f, grid);
        yAxis.transform.SetParent(parent, false);
        yAxis.positionCount = 2;
        yAxis.SetPositions(new[] { Vector3.zero, new Vector3(0, axisHeight, 0) });

        List<(Vector3, Vector3)> ticks = new();
        const float tickLength = .3f;
        for (int i = 0; i < axisHeight; i++)
        {
            ticks.Add((new Vector3(i, -tickLength * .5f, 0), new Vector3(i, tickLength * .5f, 0)));
            ticks.Add((new Vector3(-tickLength * .5f, i, 0), new Vector3(tickLength * .5f, i, 0)));
        }

        const float fine = .1f;
        for (int i = 0; i < axisHeight / yScale; i++)
        {
            ticks.Add((new Vector3(-fine * .5f, i * yScale, 0), new Vector3(fine * .5f, i * yScale, 0)));
        }

        MeshRenderer batch = Factory.NewSegmentBatch("ticks", ticks, .03f, grid);
        batch.transform.SetParent(parent, false);
    }

    void Start()
    {
        root = new GameObject("Grid");
        root.transform.SetParent(transform, false);
        Color grid = Factory.Palette.Grid;
        LineRenderer xAxis = Factory.NewLine("xAxis", .05f, grid);
        xAxis.transform.SetParent(root.transform, false);
        xAxis.positionCount = 2;
        xAxis.SetPositions(new[] { Vector3.zero, new Vector3(axisHeight, 0, 0) });

        if (VRMode.Enabled)
        {
            BuildTicksBatched(root.transform, xAxis, grid);
            return;
        }

        float tickLength = .3f;

        for (int i = 0; i < axisHeight; i++)
        {
            LineRenderer xTick = Factory.NewLine("xTick", .03f, grid);
            xTick.transform.SetParent(xAxis.transform, false);
            xTick.positionCount = 2;
            xTick.SetPositions(new[]
            {
                new Vector3(i, -tickLength * .5f, 0),
                new Vector3(i, tickLength * .5f, 0)
            });
        }

        LineRenderer yAxis = Factory.NewLine("yAxis", .05f, grid);
        yAxis.transform.SetParent(root.transform, false);
        yAxis.positionCount = 2;
        yAxis.SetPositions(new[] { Vector3.zero, new Vector3(0, axisHeight, 0) });

        for (int i = 0; i < axisHeight; i++)
        {
            LineRenderer yTick = Factory.NewLine("yTick", .03f, grid);
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
            LineRenderer yScaleTick = Factory.NewLine("yScaleTick", .03f, grid);
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
