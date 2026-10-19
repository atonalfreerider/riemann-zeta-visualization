using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Many marbles (spheres) drawn every frame as GPU-instanced batches of the Zeta/Marble shader, one colour per instance.
/// Fill it each frame (or once, for a still set) with Add, then call Draw. Used for the marble fingerprints and for the
/// E8 lattice spheres.
/// </summary>
public class MarbleBatch
{
    static Material material;
    static readonly Dictionary<int, Mesh> spheres = new();
    readonly List<Matrix4x4> matrices = new();
    readonly List<Vector4> colors = new();
    readonly Matrix4x4[] chunkM = new Matrix4x4[1023];
    readonly Vector4[] chunkC = new Vector4[1023];
    readonly MaterialPropertyBlock block = new();
    readonly Mesh mesh;
    public int Layer;

    /// <param name="detail">0: an icosahedron (20 faces, for E8's thousands of points); 1: 80 faces; 2: 320 (marbles).</param>
    public MarbleBatch(int detail = 2)
    {
        mesh = Sphere(detail);
    }

    public static Material Material
    {
        get
        {
            if (material == null)
            {
                Shader s = Shader.Find("Zeta/Marble");
                material = new Material(s != null ? s : Shader.Find("Unlit/Color")) { enableInstancing = true };
            }

            return material;
        }
    }

    public int Count => matrices.Count;

    public void Clear()
    {
        matrices.Clear();
        colors.Clear();
    }

    /// <summary>One marble of diameter d at p (world space, or in the space of the matrix given to Draw).</summary>
    public void Add(Vector3 p, float d, Color c, float glow = 1f)
    {
        if (d <= 1e-5f) return;
        matrices.Add(Matrix4x4.TRS(p, Quaternion.identity, Vector3.one * d));
        colors.Add(new Vector4(c.r * glow, c.g * glow, c.b * glow, 1f));
    }

    /// <summary>Draw every marble, through the matrix to world space (identity: they are already in world space).</summary>
    public void Draw(Matrix4x4 toWorld)
    {
        Material m = Material;
        bool identity = toWorld == Matrix4x4.identity;
        for (int start = 0; start < matrices.Count; start += 1023)
        {
            int n = Mathf.Min(1023, matrices.Count - start);
            for (int i = 0; i < n; i++)
            {
                chunkM[i] = identity ? matrices[start + i] : toWorld * matrices[start + i];
                chunkC[i] = colors[start + i];
            }

            block.Clear();
            block.SetVectorArray("_Color", chunkC);
            Graphics.DrawMeshInstanced(mesh, 0, m, chunkM, n, block, UnityEngine.Rendering.ShadowCastingMode.Off, false, Layer);
        }
    }

    /// <summary>A unit-diameter icosphere with the given number of subdivisions (shared per detail level).</summary>
    public static Mesh Sphere(int detail)
    {
        if (spheres.TryGetValue(detail, out Mesh cached) && cached != null) return cached;
        float t = (1f + Mathf.Sqrt(5f)) / 2f;
        List<Vector3> v = new()
        {
            new(-1, t, 0), new(1, t, 0), new(-1, -t, 0), new(1, -t, 0), new(0, -1, t), new(0, 1, t),
            new(0, -1, -t), new(0, 1, -t), new(t, 0, -1), new(t, 0, 1), new(-t, 0, -1), new(-t, 0, 1),
        };
        List<int> f = new()
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
        };
        for (int i = 0; i < v.Count; i++) v[i] = v[i].normalized * .5f;
        for (int s = 0; s < detail; s++)
        {
            Dictionary<long, int> mid = new();
            int Mid(int a, int b)
            {
                long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                if (mid.TryGetValue(key, out int m)) return m;
                v.Add(((v[a] + v[b]) * .5f).normalized * .5f);
                mid[key] = v.Count - 1;
                return v.Count - 1;
            }

            List<int> nf = new();
            for (int i = 0; i < f.Count; i += 3)
            {
                int a = f[i], b = f[i + 1], c = f[i + 2];
                int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                nf.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }

            f = nf;
        }

        Mesh mesh = new() { name = $"marble {detail}" };
        mesh.SetVertices(v);
        List<Vector3> normals = new();
        foreach (Vector3 p in v) normals.Add(p.normalized);
        mesh.SetNormals(normals);
        mesh.SetTriangles(f, 0);
        mesh.RecalculateBounds();
        spheres[detail] = mesh;
        return mesh;
    }
}
