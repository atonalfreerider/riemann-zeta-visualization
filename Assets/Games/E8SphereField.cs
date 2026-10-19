using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// E8's lattice as a row of spheres, one per shell: shell n's 240 σ₃(n) points (E8Shells) on a ball of radius ∝ √(2n),
/// coloured by n's kind (gold if n is prime, deep red if a prime's square, teal if pronic, grey otherwise, white for 1).
/// As many shells as fit a point budget (hundreds of thousands on the desktop, tens of thousands in VR), drawn as
/// instanced marbles. A game picks shell n by its centre and radius (Centre, Radius).
/// </summary>
public class E8SphereField : MonoBehaviour
{
    public int Shells { get; private set; }
    public long Points { get; private set; }
    MarbleBatch dots;                                    // made in Awake: no render state in field initializers
    readonly List<Vector3> centres = new();
    readonly List<float> radii = new();
    readonly List<(int start, int count)> ranges = new();
    readonly List<Vector3> local = new();
    readonly List<float> sizes = new();
    readonly List<int> shellOf = new();
    int lit = -1;
    float dim = 0f;
    bool dirty = true;

    public Vector3 Centre(int n) => n >= 1 && n <= centres.Count ? transform.TransformPoint(centres[n - 1]) : transform.position;
    public float Radius(int n) => n >= 1 && n <= radii.Count ? radii[n - 1] * transform.lossyScale.x : 0f;

    /// <summary>Shells 1, 2, … in a row along +x while their points fit in budget (at most maxShells).</summary>
    public static E8SphereField Create(Transform parent, Vector3 localPos, float unit, long budget, int maxShells = 12)
    {
        GameObject go = new("E8 spheres");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        E8SphereField f = go.AddComponent<E8SphereField>();
        f.Build(unit, budget, maxShells);
        return f;
    }

    void Awake() => dots ??= new MarbleBatch(0);

    void Build(float unit, long budget, int maxShells)
    {
        dots ??= new MarbleBatch(0);
        float x = 0f;
        long total = 0;
        for (int n = 1; n <= maxShells; n++)
        {
            long count = E8Shells.Count(n);
            if (n > 1 && total + count > budget) break;
            float r = unit * Mathf.Sqrt(2f * n);
            x += (n == 1 ? 0f : unit * .9f) + r;
            Vector3 c = new(x, 0f, 0f);
            x += r;
            List<Vector3> pts = E8Shells.Sphere(n, r);
            // a marble about half the mean spacing of the shell's points
            float spacing = r * Mathf.Sqrt(4f * Mathf.PI / pts.Count);
            // in VR the whole row is a metre or two across: the points touch, so each shell reads as a ball
            float d = VRMode.Enabled ? Mathf.Clamp(spacing * 1.05f, unit * .05f, unit * .3f) : Mathf.Clamp(spacing * .7f, unit * .025f, unit * .18f);
            ranges.Add((local.Count, pts.Count));
            foreach (Vector3 p in pts)
            {
                local.Add(c + p);
                sizes.Add(d);
                shellOf.Add(n);
            }

            centres.Add(c);
            radii.Add(r);
            total += count;
            Shells = n;
        }

        Points = total;
        dirty = true;
    }

    /// <summary>Light one shell (the others dim), or none (−1).</summary>
    public void Highlight(int n, float dimOthers = .75f)
    {
        if (n == lit && Mathf.Approximately(dim, dimOthers)) return;
        lit = n;
        dim = dimOthers;
        dirty = true;
    }

    void Refill()
    {
        // the field stands still: its points go to the batch in world space once, so drawing copies no matrices
        dots.Clear();
        Matrix4x4 m = transform.localToWorldMatrix;
        float s = transform.lossyScale.x;
        for (int i = 0; i < local.Count; i++)
        {
            int n = shellOf[i];
            Color c = ColourOf(n);
            float glow = 1.05f;
            if (lit > 0)
            {
                if (n == lit) glow = 1.35f;
                else c = Color.Lerp(c, new Color(.2f, .21f, .26f), dim);
            }

            dots.Add(m.MultiplyPoint3x4(local[i]), sizes[i] * s, c, glow);
        }

        dirty = false;
    }

    void LateUpdate()
    {
        if (dirty || transform.hasChanged)
        {
            transform.hasChanged = false;
            Refill();
        }

        dots.Draw(Matrix4x4.identity);
    }

    /// <summary>Recolour: n → its colour (for a veiled game, silver until found).</summary>
    public System.Func<int, Color> ColourOf = n => NumberKind.ColorOf(n);

    public void Refresh() => dirty = true;
}
