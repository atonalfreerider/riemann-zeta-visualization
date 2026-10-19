using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thin lines drawn as one mesh of flat quads in a local plane (x, y; z only offsets depth), for things that face the
/// viewer such as a marble fingerprint: container rings, the string, the exponent's hypercube shadow. Colours per line,
/// through the glow-line shader (vertex colour × a soft HDR glow). Fill it each frame, then Draw.
/// </summary>
public class LineBatch
{
    readonly List<Vector3> verts = new();
    readonly List<Color> colors = new();
    readonly List<int> tris = new();
    Mesh mesh;
    MaterialPropertyBlock props;
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int IntensityId = Shader.PropertyToID("_Intensity");

    public float Intensity = Factory.SoftGlowIntensity;
    /// <summary>Every line's alpha is multiplied by this (a whole batch fading).</summary>
    public float Alpha = 1f;

    public void Clear()
    {
        verts.Clear();
        colors.Clear();
        tris.Clear();
    }

    public void Add(Vector3 a, Vector3 b, float width, Color c)
    {
        c.a *= Alpha;
        if (c.a <= .003f) return;
        Vector2 d = new(b.x - a.x, b.y - a.y);
        Vector3 n = d.sqrMagnitude < 1e-10f ? Vector3.right : new Vector3(-d.y, d.x, 0f).normalized;
        n *= width * .5f;
        int i = verts.Count;
        verts.Add(a - n); verts.Add(a + n); verts.Add(b + n); verts.Add(b - n);
        colors.Add(c); colors.Add(c); colors.Add(c); colors.Add(c);
        tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
        tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
    }

    /// <summary>A polyline through the points (closed: back to the first).</summary>
    public void Path(IList<Vector3> pts, float width, Color c, bool closed = false)
    {
        for (int i = 0; i + 1 < pts.Count; i++) Add(pts[i], pts[i + 1], width, c);
        if (closed && pts.Count > 2) Add(pts[^1], pts[0], width, c);
    }

    /// <summary>A circle in the x, y plane.</summary>
    public void Circle(Vector3 centre, float r, float width, Color c, int segments = 48)
    {
        Vector3 prev = centre + new Vector3(r, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            Vector3 p = centre + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
            Add(prev, p, width, c);
            prev = p;
        }
    }

    public void Draw(Matrix4x4 toWorld, int layer = 0)
    {
        if (verts.Count == 0) return;
        if (mesh == null)
        {
            mesh = new Mesh { name = "line batch" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.MarkDynamic();
        }

        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetColors(colors);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        props ??= new MaterialPropertyBlock();
        props.SetColor(ColorId, Color.white);
        props.SetFloat(IntensityId, Intensity);
        Graphics.DrawMesh(mesh, toWorld, Factory.mainMat, layer, null, 0, props, false, false);
    }
}
