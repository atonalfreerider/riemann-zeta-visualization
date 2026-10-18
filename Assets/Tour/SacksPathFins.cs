using System;
using System.Collections.Generic;
using Meta.Numerics;
using UnityEngine;

/// <summary>
/// The twist on the twist on the live path of partial sums (section QA; the long-form segment xorbit). Every link n gets its
/// square: a perpendicular of the same length, its fin, standing at the joint where the link starts and turned about the link
/// by α·log n while the link itself turns by t·log n. Parameters (exp:tw), beside the path's own (SacksPathWorld.cs):
///   pw.fins    0 … 1: the fins grow from nothing to their full length n^(−½)
///   pw.alpha   α as a multiple of t: fin n is turned by −α·log n (gold where it points up out of the plane, cyan down,
///              white where it lies flat)
///   pw.faces   0 … 1: the squares' faces
///   pw.finsum  0 … 1: the fins slide head to tail from their links into their running sum from the path's origin, the
///              first fins first
///   pw.road    0 … 1: each summed fin loses its share of the difference chain, (i/2)·n^(−½ − i(t − α)): at α = t that is
///              a fixed half step straight up the plane, the road; what is left is half the chain at t + α (at α = t, the
///              octave) and the lift
///   pw.lift    0 … 1: the summed fins' height out of the plane (Im S(α) at the end of the sum); 0 lays the sum flat, as
///              seen from straight above
/// </summary>
public partial class SacksSpiral
{
    partial class PwScene
    {
        public Transform FinRoot;
        public LiveMesh Faces, Fins;
        public string FinSig = "";
        public Vector3[] FinHeads = new Vector3[0];
        public int FinCount;
    }

    /// <summary>A mesh rebuilt from segments and quads whenever its picture changes: one object and one draw call.</summary>
    class LiveMesh
    {
        readonly Mesh mesh;
        public readonly MeshRenderer Renderer;
        readonly List<Vector3> v = new();
        readonly List<Color> c = new();
        readonly List<int> tr = new();

        public LiveMesh(Transform parent, string name, float glow, int order)
        {
            mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.MarkDynamic();
            GameObject go = new(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            Renderer = go.AddComponent<MeshRenderer>();
            Renderer.sharedMaterial = Factory.mainMat;
            Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Renderer.receiveShadows = false;
            Renderer.sortingOrder = order;
            Factory.SetColor(Renderer, Color.white);
            Factory.SetIntensity(Renderer, glow);
        }

        public void Begin()
        {
            v.Clear();
            c.Clear();
            tr.Clear();
        }

        /// <summary>A segment from a to b, w wide: two crossed quads, so it shows from every side.</summary>
        public void Seg(Vector3 a, Vector3 b, Color col, float w)
        {
            Vector3 d = b - a;
            if (d.sqrMagnitude < 1e-10f || col.a <= .002f) return;
            d.Normalize();
            Vector3 n1 = Vector3.Cross(d, Mathf.Abs(d.z) > .9f ? Vector3.right : Vector3.forward).normalized * (w * .5f);
            Vector3 n2 = Vector3.Cross(d, n1).normalized * (w * .5f);
            Quad(a - n1, a + n1, b + n1, b - n1, col);
            Quad(a - n2, a + n2, b + n2, b - n2, col);
        }

        /// <summary>A flat quad (the material draws both faces).</summary>
        public void Quad(Vector3 a, Vector3 b, Vector3 d, Vector3 e, Color col)
        {
            if (col.a <= .002f) return;
            int i = v.Count;
            v.Add(a);
            v.Add(b);
            v.Add(d);
            v.Add(e);
            for (int k = 0; k < 4; k++) c.Add(col);
            tr.Add(i);
            tr.Add(i + 1);
            tr.Add(i + 2);
            tr.Add(i);
            tr.Add(i + 2);
            tr.Add(i + 3);
        }

        public void End()
        {
            mesh.Clear();
            mesh.SetVertices(v);
            mesh.SetColors(c);
            mesh.SetTriangles(tr, 0);
            mesh.RecalculateBounds();
        }
    }

    static readonly Color FinSumFrom = new(.62f, .5f, 1f);

    /// <summary>The squares and fins on the path's links as drawn now (called every frame; redrawn when anything moved).</summary>
    void PwFins(PwScene s, double t, float shown)
    {
        float grow = Tw("pw.fins", 0f), af = Tw("pw.alpha", 0f), faces = Tw("pw.faces", 1f), sum = Tw("pw.finsum", 0f);
        float road = Tw("pw.road", 0f), lift = Tw("pw.lift", 1f);
        // as wide as the path, which follows the zoom; wider than the path's cap when the camera pulls far back (the road)
        float w = .3f * s.Path.widthMultiplier;
        Camera cam = Camera.main;
        if (cam != null) w = Mathf.Max(w, Mathf.Min(Vector3.Distance(cam.transform.position, PathFocus) * .0028f, 2.5f));
        bool on = grow > .001f && Tw("pw.gold", 0f) <= 0f;
        string sig = on ? $"{t:F5}|{shown:F3}|{grow:F4}|{af:F6}|{faces:F3}|{sum:F4}|{road:F4}|{lift:F4}|{w:F4}|{PathHeight(t):F3}" : "off";
        if (sig == s.FinSig) return;
        s.FinSig = sig;
        if (s.FinRoot == null)
        {
            // their own group, so the path can be muted without them (exp:mute:PW path)
            s.FinRoot = ExpGroup("PW fins").transform;
            s.Faces = new LiveMesh(s.FinRoot, "squares", Factory.NoGlow, -10);
            s.Fins = new LiveMesh(s.FinRoot, "fins", Factory.SoftGlowIntensity, -9);
        }

        s.Faces.Begin();
        s.Fins.Begin();
        s.FinCount = 0;
        if (!on)
        {
            s.Faces.End();
            s.Fins.End();
            return;
        }

        int nMax = Mathf.Max(16, (int)(t / Math.PI) + 12);
        int whole = Mathf.Clamp(Mathf.FloorToInt(shown), 0, nMax);
        if (s.FinHeads.Length < whole + 1) s.FinHeads = new Vector3[whole + 64];
        double alpha = af * t;
        Vector3 up = Vector3.back;
        Vector3 origin = PwP(Complex.Zero, t), sumAt = origin;
        float logN = Mathf.Log(Mathf.Max(2, whole));
        double re = 0, im = 0;
        s.FinHeads[0] = origin;
        for (int n = 1; n <= whole; n++)
        {
            double ln = Math.Log(n), r = 1 / Math.Sqrt(n), th = -t * ln, ph = -alpha * ln;
            Vector3 a = PwP(new Complex(re, im), t);
            re += r * Math.Cos(th);
            im += r * Math.Sin(th);
            Vector3 b = PwP(new Complex(re, im), t);
            // the fin: the link's perpendicular in the plane, turned about the link by φ = −α·log n
            Vector3 side = new((float)-Math.Sin(th), (float)Math.Cos(th), 0f);
            float rs = (float)(r * PwScale);
            Vector3 f = rs * ((float)Math.Cos(ph) * side + (float)Math.Sin(ph) * up);
            // summed, it gives up its share of the difference chain and (with pw.lift) its height
            double dph = -(t - alpha) * ln;
            Vector3 F = f - road * .5f * rs * new Vector3((float)-Math.Sin(dph), (float)Math.Cos(dph), 0f);
            F += up * (Vector3.Dot(F, up) * (lift - 1f));
            float u = n == 1 ? 0f : Mathf.Log(n) / logN;
            float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((sum - .45f * u) / .55f));
            Vector3 vec = Vector3.Lerp(f, F, e) * grow, at = Vector3.Lerp(a, sumAt, e);
            sumAt += F * grow;
            float tilt = (float)Math.Sin(ph);
            Color tc = TiltColour(tilt), col = Color.Lerp(tc, Color.Lerp(FinSumFrom, Color.white, u), e);
            float stay = 1f - e;
            if (stay > .002f)
            {
                // the first squares are big: their faces stay faint, so they never cover the chain
                float face = .24f * Mathf.Clamp01(.1f + .1f * n);
                s.Faces.Quad(a, b, b + f * grow, a + f * grow, new Color(tc.r, tc.g, tc.b, face * faces * stay));
                s.Fins.Seg(a + f * grow, b + f * grow, new Color(tc.r, tc.g, tc.b, .75f * stay), w * .4f);
                s.Fins.Seg(b, b + f * grow, new Color(tc.r, tc.g, tc.b, .45f * stay), w * .3f);
            }

            s.Fins.Seg(at, at + vec, col, Mathf.Lerp(w * .75f, w * .9f, e));
            s.FinHeads[n] = at + vec;
        }

        s.FinCount = whole;
        s.Faces.End();
        s.Fins.End();
    }

    /// <summary>For callouts: the head of fin n as drawn now (on its link, or in the fins' running sum).</summary>
    public Vector3 FinVertex(int n)
    {
        if (pw == null || n < 0 || n > pw.FinCount || n >= pw.FinHeads.Length) return PathVertex(n);
        return pw.FinHeads[n] + Vector3.back * .1f;
    }
}
