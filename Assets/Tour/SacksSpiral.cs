using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The primes as a picture: a 1..30 grid that morphs into Robert Sacks's spiral
/// (integer n at radius √n, angle 2π√n, one turn per perfect square), which can then be bent
/// onto a saddle z = (x² − y²)/k. Routes from the origin show where primes cluster
/// (Euler's k²+k+41 curve) and where they never appear (the squares ray, the pronic ray).
/// </summary>
public class SacksSpiral : MonoBehaviour
{
    public const int GridCount = 30;
    const int Count = 1300;
    const float R = 1.45f;           // spiral radius scale
    const float SaddleK = 1f / 48f;  // saddle curvature

    readonly Vector3 origin = new(-34f, 18.5f, 0f);
    readonly Vector3 gridOrigin = new(-40f, 26f, 0f);
    const float Cell = 3.0f;

    readonly List<GameObject> dots = new();
    readonly List<TextBox> labels = new();
    readonly List<TextBox> factorLabels = new();
    readonly List<LineRenderer> routes = new();
    readonly List<TextBox> routeLabels = new();
    GameObject routesRoot;
    TextBox caption;

    GameObject latticeRoot;
    readonly List<(int p, int m, GameObject cell, LineRenderer link)> latticeCells = new();
    readonly List<TextBox> latticeRowLabels = new();
    readonly Vector3 latticeOrigin = new(-40f, 7.6f, 0f);
    const float LatticeCell = 1.5f, LatticeRow = 2.6f;

    float morph;   // 0 = grid, 1 = spiral
    float bend;    // 0 = flat, 1 = saddle
    float reveal;  // fraction of the extra numbers (31..Count) shown

    public Vector3 Center => origin;
    public float Extent => R * Mathf.Sqrt(Count);

    public static SacksSpiral Create()
    {
        GameObject go = new("SacksSpiral");
        SacksSpiral s = go.AddComponent<SacksSpiral>();
        s.Build();
        go.SetActive(false);
        return s;
    }

    static bool IsPrime(int n)
    {
        if (n < 2) return false;
        for (int d = 2; d * d <= n; d++) if (n % d == 0) return false;
        return true;
    }

    static bool IsSquare(int n)
    {
        int r = Mathf.RoundToInt(Mathf.Sqrt(n));
        return r * r == n;
    }

    static string Factorization(int n)
    {
        List<string> f = new();
        int m = n;
        for (int d = 2; d <= m; d++) while (m % d == 0) { f.Add(d.ToString()); m /= d; }
        return string.Join(" × ", f);
    }

    Vector3 GridPos(int n)
    {
        int col = (n - 1) % 5, row = (n - 1) / 5;
        return gridOrigin + new Vector3(col * Cell, -row * Cell, 0);
    }

    /// <summary>Sacks position for a real-valued index (so routes can be drawn as smooth curves).</summary>
    Vector3 SpiralPos(float n, float bendAmount)
    {
        float r = R * Mathf.Sqrt(n);
        float a = 2f * Mathf.PI * Mathf.Sqrt(n);
        float x = r * Mathf.Cos(a), y = r * Mathf.Sin(a);
        float z = bendAmount * SaddleK * (x * x - y * y);
        return origin + new Vector3(x, y, z);
    }

    Vector3 Pos(int n)
    {
        Vector3 s = SpiralPos(n, bend);
        return n <= GridCount ? Vector3.Lerp(GridPos(n), s, morph) : s;
    }

    void Build()
    {
        for (int n = 1; n <= Count; n++)
        {
            bool prime = IsPrime(n), square = IsSquare(n);
            Color c = prime ? Factory.Palette.Zero : square ? new Color(1f, .55f, .2f) : new Color(.3f, .3f, .38f);
            GameObject dot = Factory.NewSphere("n " + n, prime ? .42f : .22f, c, prime ? Factory.GlowIntensity : Factory.NoGlow);
            dot.transform.SetParent(transform, false);
            dot.SetActive(n <= GridCount);
            dots.Add(dot);

            if (n <= GridCount)
            {
                TextBox label = TextBoxLabel(n.ToString(), prime ? 4.6f : 3.4f, new Color(.45f, .45f, .5f));
                labels.Add(label);
                TextBox fac = TextBoxLabel(n > 1 && !prime ? Factorization(n) : "", 1.5f, new Color(.55f, .55f, .65f));
                fac.gameObject.SetActive(false);
                factorLabels.Add(fac);
            }
        }

        routesRoot = new GameObject("Routes");
        routesRoot.transform.SetParent(transform, false);
        AddRoute("squares", new Color(1f, .55f, .2f), k => k * k, 1f, 36f, "k²  — the squares: never a prime");
        AddRoute("pronic", new Color(.55f, .6f, 1f), k => k * (k + 1), 1f, 35f, "k(k+1)  — never a prime either");
        AddRoute("euler", new Color(.35f, 1f, .55f), k => k * k + k + 41, 13f, 35f, "k² + k + 41  — Euler's route: 40 primes in a row");
        routesRoot.SetActive(false);

        caption = TextBoxLabel("", 2.4f, new Color(.85f, .85f, .95f));
        caption.gameObject.SetActive(false);
        BuildLattice();
        Layout();
    }

    /// <summary>Rows p × m (m ≥ p) for p = 2, 3, 5: every composite ≤ 30 appears exactly where it is first made.</summary>
    void BuildLattice()
    {
        latticeRoot = new GameObject("Lattice");
        latticeRoot.transform.SetParent(transform, false);
        int[] primes = { 2, 3, 5 };
        for (int r = 0; r < primes.Length; r++)
        {
            int p = primes[r];
            float y = latticeOrigin.y - r * LatticeRow;
            TextBox rowLabel = TextBoxLabel($"{p} ×", 3.2f, Factory.Palette.Zero);
            rowLabel.transform.SetParent(latticeRoot.transform, false);
            rowLabel.transform.position = new Vector3(latticeOrigin.x - 2.0f, y, 0);
            latticeRowLabels.Add(rowLabel);
            for (int m = p; p * m <= GridCount; m++)
            {
                int n = p * m;
                bool square = m == p;
                GameObject cell = new GameObject($"{p}x{m}");
                cell.transform.SetParent(latticeRoot.transform, false);
                cell.transform.position = new Vector3(latticeOrigin.x + (m - 2) * LatticeCell, y, 0);
                TextBox t = TextBox.Create($"{n}", square ? 3.4f : 2.8f);
                t.transform.SetParent(cell.transform, false);
                t.Alignment = TextAlignmentOptions.Center;
                t.Color = square ? new Color(1f, .55f, .2f) : new Color(.6f, .6f, .7f);
                t.gameObject.AddComponent<Billboard>();
                TextBox sub = TextBox.Create($"{p}·{m}", 1.4f);
                sub.transform.SetParent(cell.transform, false);
                sub.Alignment = TextAlignmentOptions.Center;
                sub.transform.localPosition = Vector3.down * 1.05f;
                sub.Color = new Color(.45f, .45f, .55f);
                sub.gameObject.AddComponent<Billboard>();
                LineRenderer link = Factory.NewLine("link", .035f, square ? new Color(1f, .55f, .2f) : new Color(.5f, .5f, .6f), Factory.SoftGlowIntensity);
                link.transform.SetParent(cell.transform, false);
                link.positionCount = 2;
                link.enabled = false;
                cell.SetActive(false);
                latticeCells.Add((p, m, cell, link));
            }
        }

        latticeRoot.SetActive(false);
    }

    /// <summary>Animate the lattice: each product pops in and strikes its grid cell, dimming it (orange for squares).</summary>
    public IEnumerator BuildLatticeRows(float seconds)
    {
        latticeRoot.SetActive(true);
        foreach (TextBox l in latticeRowLabels) l.gameObject.SetActive(false);
        float per = seconds / (latticeCells.Count + 3);
        int lastP = -1;
        foreach ((int p, int m, GameObject cell, LineRenderer link) in latticeCells)
        {
            if (p != lastP)
            {
                lastP = p;
                latticeRowLabels[System.Array.IndexOf(new[] { 2, 3, 5 }, p)].gameObject.SetActive(true);
                yield return new WaitForSeconds(per);
            }

            int n = p * m;
            cell.SetActive(true);
            StartCoroutine(Shrink(cell.transform, 1.8f, 1f, .35f));
            Vector3 target = GridPos(n);
            link.enabled = true;
            float t = 0f;
            while (t < per * .8f)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0, 1, t / (per * .8f));
                link.SetPositions(new[] { cell.transform.position + Vector3.up * .6f, Vector3.Lerp(cell.transform.position + Vector3.up * .6f, target, k) });
                yield return null;
            }

            // strike the grid cell
            bool square = m == p;
            labels[n - 1].Color = square ? new Color(1f, .55f, .2f) : new Color(.28f, .28f, .34f);
            dots[n - 1].SetActive(square);
            if (square) StartCoroutine(Shrink(dots[n - 1].transform, 1.6f, .32f, .5f));
            StartCoroutine(FadeLink(link, .6f));
        }
    }

    static IEnumerator FadeLink(LineRenderer link, float seconds)
    {
        Color c0 = link.startColor;
        float t = 0f;
        while (t < seconds && link != null)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(1f, .15f, t / seconds);
            link.startColor = new Color(c0.r, c0.g, c0.b, a);
            link.endColor = new Color(c0.r, c0.g, c0.b, a);
            yield return null;
        }
    }

    public void HideLattice() => latticeRoot.SetActive(false);

    /// <summary>Pulse the perfect squares (orange) on the spiral / saddle.</summary>
    public void PulseSquares(float seconds)
    {
        for (int i = 0; i < dots.Count; i++)
        {
            int n = i + 1;
            if (IsSquare(n) && dots[i].activeSelf) StartCoroutine(PulseScale(dots[i].transform, n <= GridCount ? .32f : .22f, 3.2f, seconds));
        }
    }

    /// <summary>Briefly brighten every composite so the prime pattern reads as the negative space.</summary>
    public void PulseComposites(float seconds)
    {
        for (int i = 0; i < dots.Count; i++)
        {
            int n = i + 1;
            if (!IsPrime(n) && !IsSquare(n))
            {
                dots[i].SetActive(true);
                StartCoroutine(PulseScale(dots[i].transform, .22f, 2.2f, seconds));
            }
        }
    }

    static IEnumerator PulseScale(Transform t, float baseScale, float factor, float seconds)
    {
        float e = 0f;
        while (e < seconds && t != null)
        {
            e += Time.deltaTime;
            float k = 0.5f - 0.5f * Mathf.Cos(e / seconds * Mathf.PI * 2f);
            t.localScale = Vector3.one * (baseScale * (1f + (factor - 1f) * k));
            yield return null;
        }

        if (t != null) t.localScale = Vector3.one * baseScale;
    }

    TextBox TextBoxLabel(string text, float size, Color color)
    {
        TextBox label = TextBox.Create(text, size);
        label.transform.SetParent(transform, false);
        label.Alignment = TextAlignmentOptions.Center;
        label.Color = color;
        label.gameObject.AddComponent<Billboard>();
        return label;
    }

    void AddRoute(string name, Color color, System.Func<float, float> nOfK, float k0, float k1, string text)
    {
        LineRenderer line = Factory.NewLine("route " + name, .14f, color, Factory.GlowIntensity);
        line.transform.SetParent(routesRoot.transform, false);
        line.numCornerVertices = 4;
        line.positionCount = Mathf.FloorToInt(k1) - Mathf.CeilToInt(k0) + 1;
        routes.Add(line);
        TextBox label = TextBoxLabel(text, 2.2f, color);
        label.transform.SetParent(routesRoot.transform, false);
        routeLabels.Add(label);
        routeFns.Add((nOfK, k0, k1));
    }

    readonly List<(System.Func<float, float> fn, float k0, float k1)> routeFns = new();

    void Layout()
    {
        for (int i = 0; i < dots.Count; i++)
        {
            int n = i + 1;
            dots[i].transform.position = Pos(n);
            if (n > GridCount)
            {
                float f = (float)(n - GridCount) / (Count - GridCount);
                dots[i].SetActive(f <= reveal);
            }
        }

        for (int i = 0; i < labels.Count; i++)
        {
            labels[i].transform.position = Pos(i + 1) + Vector3.back * .4f;
            labels[i].transform.localScale = Vector3.one * Mathf.Lerp(1f, .35f, morph);
            factorLabels[i].transform.position = GridPos(i + 1) + Vector3.down * 1.0f;
        }

        for (int r = 0; r < routes.Count; r++)
        {
            (System.Func<float, float> fn, float k0, float k1) = routeFns[r];
            // sample at integer k only: the route is the polyline through the actual numbers n(k)
            Vector3[] pts = new Vector3[routes[r].positionCount];
            int kStart = Mathf.CeilToInt(k0);
            for (int i = 0; i < pts.Length; i++)
            {
                pts[i] = SpiralPos(Mathf.Max(1f, fn(kStart + i)), bend) + Vector3.back * .15f;
            }

            routes[r].SetPositions(pts);
            routeLabels[r].transform.position = pts[^1] + Vector3.back * .6f + Vector3.up * 1.6f;
        }

        caption.transform.position = origin + new Vector3(0, -Extent - 4.5f, 0);
    }

    // ---- public animation API -------------------------------------------------------------------

    public void ShowGrid()
    {
        gameObject.SetActive(true);
        HideLattice();
        morph = 0f; bend = 0f; reveal = 0f;
        for (int i = 0; i < labels.Count; i++)
        {
            labels[i].Color = new Color(.45f, .45f, .5f);
            factorLabels[i].gameObject.SetActive(false);
            dots[i].SetActive(false);
        }

        routesRoot.SetActive(false);
        caption.gameObject.SetActive(false);
        Layout();
    }

    /// <summary>Light the primes one by one; composites dim and show their factors.</summary>
    public IEnumerator BuildGrid(float seconds)
    {
        float per = seconds / GridCount;
        for (int i = 0; i < GridCount; i++)
        {
            int n = i + 1;
            if (IsPrime(n))
            {
                labels[i].Color = Factory.Palette.Zero;
                dots[i].SetActive(true);
                StartCoroutine(Shrink(dots[i].transform, 1.8f, .5f, .6f));
            }
            else
            {
                // composites stay neutral until the lattice shows how they are made
                labels[i].Color = new Color(.5f, .5f, .58f);
                dots[i].SetActive(false);
            }

            yield return new WaitForSeconds(per);
        }
    }

    static IEnumerator Shrink(Transform t, float from, float to, float seconds)
    {
        float e = 0f;
        while (e < seconds && t != null)
        {
            e += Time.deltaTime;
            t.localScale = Vector3.one * Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, e / seconds));
            yield return null;
        }
    }

    /// <summary>Grid → spiral: the 30 numbers fly to their spiral places while the rest of the spiral fills in.</summary>
    public IEnumerator MorphToSpiral(float seconds)
    {
        gameObject.SetActive(true);
        HideLattice();
        foreach (TextBox f in factorLabels) f.gameObject.SetActive(false);
        for (int i = 0; i < GridCount; i++) dots[i].SetActive(true);
        caption.gameObject.SetActive(true);
        caption.Text = "Sacks spiral: number n at distance √n, one full turn per square";
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / seconds);
            morph = k;
            reveal = Mathf.Clamp01((k - 0.35f) / 0.65f);
            Layout();
            yield return null;
        }

        morph = 1f; reveal = 1f;
        Layout();
    }

    public IEnumerator Bend(float to, float seconds)
    {
        float from = bend;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            bend = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds));
            Layout();
            yield return null;
        }

        bend = to;
        Layout();
    }

    public void ShowRoutes(bool on)
    {
        routesRoot.SetActive(on);
        if (on) Layout();
    }

    public void Hide() => gameObject.SetActive(false);

    public IEnumerable<Renderer> Renderers()
    {
        foreach (GameObject d in dots) if (d.activeSelf) yield return d.GetComponent<Renderer>();
        foreach (LineRenderer r in routes) yield return r;
    }
}
