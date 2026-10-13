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

    // sieve axes drawn in spiral space: from each prime p a glowing polyline through p×q
    GameObject sieveRoot;
    readonly List<LineRenderer> sieveAxes = new();
    readonly List<TextBox> sieveLabels = new();

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

    static readonly int[] SievePrimes = { 2, 3, 5 };
    static readonly Color[] SieveColors = { new(.35f, 1f, .55f), new(.55f, .6f, 1f), new(1f, .55f, .2f) };
    static Color PrimeColor(int p) => p == 2 ? SieveColors[0] : p == 3 ? SieveColors[1] : p == 5 ? SieveColors[2] : new Color(1f, .4f, .9f);

    LineRenderer spiralPath;
    GameObject stageRoot;
    TextBox stageLabel;
    readonly List<GameObject> workDots = new();
    readonly HashSet<int> struck = new();
    Coroutine sieveCoroutine;

    /// <summary>Start the sieve as a tracked coroutine so a later phase can cancel it safely.</summary>
    public void StartSieve(float seconds)
    {
        CancelSieve();
        sieveCoroutine = StartCoroutine(SieveRoutine(seconds));
    }

    void CancelSieve()
    {
        if (sieveCoroutine != null) StopCoroutine(sieveCoroutine);
        sieveCoroutine = null;
        ClearWorkDots();
        if (stageRoot != null) stageRoot.SetActive(false);
    }

    /// <summary>The thin spiral path through continuous n (so the dots have a road to count along).</summary>
    void EnsureSpiralPath()
    {
        if (spiralPath != null) return;
        spiralPath = Factory.NewLine("spiral path", .05f, new Color(.5f, .5f, .62f, .7f));
        spiralPath.transform.SetParent(transform, false);
        const int segs = 360;
        spiralPath.positionCount = segs + 1;
        Vector3[] pts = new Vector3[segs + 1];
        for (int i = 0; i <= segs; i++)
        {
            float n = 1f + (GridCount + 1f - 1f) * i / segs;
            pts[i] = SpiralPos(n, 0f) + Vector3.forward * .05f;
        }

        spiralPath.SetPositions(pts);
        spiralPath.enabled = false;
    }

    GameObject MakeDot(Color c, float size)
    {
        GameObject d = Factory.NewSphere("work dot", size, c, Factory.GlowIntensity);
        d.transform.SetParent(transform, false);
        workDots.Add(d);
        return d;
    }

    void ClearWorkDots()
    {
        foreach (GameObject d in workDots) Destroy(d);
        workDots.Clear();
    }

    /// <summary>
    /// One multiplication, drawn: p dots leave the number p along a horizontal axis, q dots leave q along a
    /// vertical axis, the rectangle fills to p×q dots, then the dots count along the spiral road
    /// (dot k travels to position k) and converge on p×q, which lights up.
    /// </summary>
    IEnumerator MultiplyRoutine(int p, int q, float seconds)
    {
        int n = p * q;
        bool square = p == q;
        Color cp = PrimeColor(p), cq = PrimeColor(q);
        Vector3 stage = origin + new Vector3(-((p - 1) * .75f) * .5f, Extent30 + 2.2f, 0);
        const float cell = .75f;
        float tEmerge = seconds * .28f, tFill = seconds * .12f, tCount = seconds * .38f, tLand = seconds * .22f;

        // stage axes
        stageRoot.SetActive(true);
        LineRenderer ax = stageRoot.transform.GetChild(0).GetComponent<LineRenderer>();
        LineRenderer ay = stageRoot.transform.GetChild(1).GetComponent<LineRenderer>();
        ax.startColor = cp; ax.endColor = cp;
        ay.startColor = cq; ay.endColor = cq;
        ax.SetPositions(new[] { stage + new Vector3(-.45f, -.45f, 0), stage + new Vector3((p - 1) * cell + .45f, -.45f, 0) });
        ay.SetPositions(new[] { stage + new Vector3(-.45f, -.45f, 0), stage + new Vector3(-.45f, (q - 1) * cell + .45f, 0) });
        stageLabel.Text = $"<color=#{ColorUtility.ToHtmlStringRGB(cp)}>{p}</color> × <color=#{ColorUtility.ToHtmlStringRGB(cq)}>{q}</color>";
        stageLabel.transform.position = stage + new Vector3((p - 1) * cell * .5f, (q - 1) * cell + 1.5f, 0);

        // phase A: p dots emerge from p along x, q dots from q along y
        List<GameObject> dots = new();
        Vector3 fromP = Pos(p) + Vector3.back * .3f, fromQ = Pos(q) + Vector3.back * .3f;
        Vector3[] targets = new Vector3[n];
        for (int i = 0; i < p; i++) targets[i] = stage + new Vector3(i * cell, 0, 0);
        for (int j = 1; j < q; j++) targets[p + j - 1] = stage + new Vector3(0, j * cell, 0);
        int edgeCount = p + q - 1;
        for (int k = 0; k < edgeCount; k++)
        {
            bool fromX = k < p;
            GameObject d = MakeDot(fromX ? cp : cq, .3f);
            d.transform.position = fromX ? fromP : fromQ;
            dots.Add(d);
        }

        float t = 0f;
        while (t < tEmerge)
        {
            t += Time.deltaTime;
            for (int k = 0; k < edgeCount; k++)
            {
                bool fromX = k < p;
                float delay = (fromX ? k : k - p) * 0.12f;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - delay) / (tEmerge * .7f)));
                dots[k].transform.position = Vector3.Lerp(fromX ? fromP : fromQ, targets[k], u);
            }

            yield return null;
        }

        // phase B: fill the rectangle
        for (int j = 1; j < q; j++)
        for (int i = 1; i < p; i++)
        {
            GameObject d = MakeDot(new Color(1f, 1f, 1f), .26f);
            d.transform.position = stage + new Vector3(i * cell, j * cell, 0);
            d.transform.localScale = Vector3.zero;
            dots.Add(d);
        }

        t = 0f;
        while (t < tFill)
        {
            t += Time.deltaTime;
            float u = Mathf.SmoothStep(0f, 1f, t / tFill);
            for (int k = edgeCount; k < dots.Count; k++) dots[k].transform.localScale = Vector3.one * (.26f * u);
            yield return null;
        }

        stageLabel.Text = $"<color=#{ColorUtility.ToHtmlStringRGB(cp)}>{p}</color> × <color=#{ColorUtility.ToHtmlStringRGB(cq)}>{q}</color> = {n} dots";

        // phase C: count along the spiral: dot k travels the road from the centre to position k
        Vector3[] start = new Vector3[n];
        for (int k = 0; k < n; k++) start[k] = dots[k].transform.position;
        t = 0f;
        while (t < tCount)
        {
            t += Time.deltaTime;
            for (int k = 0; k < n; k++)
            {
                float delay = k * (tCount * .5f / n);
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - delay) / (tCount * .5f)));
                // fly to the start of the road, then roll along it to k+1
                float road = Mathf.Lerp(0.6f, k + 1f, u);
                Vector3 onRoad = SpiralPos(road, 0f) + Vector3.back * .3f;
                dots[k].transform.position = Vector3.Lerp(start[k], onRoad, Mathf.Clamp01(u * 3f));
            }

            yield return null;
        }

        stageLabel.Text = $"<color=#{ColorUtility.ToHtmlStringRGB(cp)}>{p}</color> × <color=#{ColorUtility.ToHtmlStringRGB(cq)}>{q}</color> = {n}";

        // phase D: every dot rolls on to n and they merge there
        t = 0f;
        while (t < tLand)
        {
            t += Time.deltaTime;
            float u = Mathf.SmoothStep(0f, 1f, t / tLand);
            for (int k = 0; k < n; k++)
            {
                float road = Mathf.Lerp(k + 1f, n, u);
                dots[k].transform.position = SpiralPos(road, 0f) + Vector3.back * .3f;
            }

            yield return null;
        }

        // light the number
        struck.Add(n);
        labels[n - 1].Color = square ? new Color(1f, .55f, .2f) : new Color(.55f, .55f, .62f);
        dots[0].SetActive(false);
        this.dots[n - 1].SetActive(true);
        this.dots[n - 1].GetComponent<Renderer>().enabled = true;
        Factory.SetColor(this.dots[n - 1].GetComponent<Renderer>(), square ? new Color(1f, .55f, .2f) : new Color(.6f, .6f, .7f));
        Factory.SetIntensity(this.dots[n - 1].GetComponent<Renderer>(), square ? Factory.GlowIntensity : Factory.SoftGlowIntensity);
        StartCoroutine(PulseScale(this.dots[n - 1].transform, square ? .34f : .26f, 3.2f, .8f));
        TextBox hit = TextBoxLabel($"{p}×{q}", 1.3f, square ? new Color(1f, .55f, .2f) : new Color(.75f, .75f, .85f));
        hit.transform.SetParent(sieveRoot.transform, false);
        hit.transform.position = Pos(n) + Vector3.back * .5f + Vector3.down * .85f;
        sieveLabels.Add(hit);
        ClearWorkDots();
        stageRoot.SetActive(false);
    }

    float Extent30 => R * Mathf.Sqrt(GridCount);

    /// <summary>
    /// The sieve in spiral space: the 30 numbers fly to their spiral places, then from 2 an axis runs through
    /// 2×2, 2×3, 2×5, 2×7 …, from 3 through 3×3, 3×5, 3×7, and from 5 through 5×5, striking each product.
    /// </summary>
    public IEnumerator SieveRoutine(float seconds)
    {
        gameObject.SetActive(true);
        HideLattice();
        foreach (TextBox f in factorLabels) f.gameObject.SetActive(false);
        caption.gameObject.SetActive(true);
        caption.Text = "each number n sits at distance √n from the centre, one full turn per square";

        // phase A: break the grid open into spiral positions (only the first 30 numbers)
        float flyTime = Mathf.Min(2.5f, seconds * .25f);
        float t = 0f;
        float m0 = morph;
        while (t < flyTime)
        {
            t += Time.deltaTime;
            morph = Mathf.Lerp(m0, 1f, Mathf.SmoothStep(0f, 1f, t / flyTime));
            Layout();
            yield return null;
        }

        morph = 1f;
        Layout();

        // phase B: the multiplications, drawn one by one as dot rectangles that count along the spiral road
        if (sieveRoot == null)
        {
            sieveRoot = new GameObject("SieveAxes");
            sieveRoot.transform.SetParent(transform, false);
            stageRoot = new GameObject("MultiplyStage");
            stageRoot.transform.SetParent(transform, false);
            LineRenderer ax = Factory.NewLine("stage x", .05f, Color.white, Factory.SoftGlowIntensity);
            ax.transform.SetParent(stageRoot.transform, false);
            ax.positionCount = 2;
            LineRenderer ay = Factory.NewLine("stage y", .05f, Color.white, Factory.SoftGlowIntensity);
            ay.transform.SetParent(stageRoot.transform, false);
            ay.positionCount = 2;
            stageLabel = TextBoxLabel("", 2.6f, Color.white);
            stageLabel.transform.SetParent(stageRoot.transform, false);
        }

        EnsureSpiralPath();
        spiralPath.enabled = true;
        sieveRoot.SetActive(true);
        stageRoot.SetActive(false);
        foreach (LineRenderer a in sieveAxes) Destroy(a.gameObject);
        foreach (TextBox l in sieveLabels) Destroy(l.gameObject);
        sieveAxes.Clear();
        sieveLabels.Clear();

        (int p, int q)[] shown = { (2, 2), (2, 3), (2, 5), (2, 7), (3, 3), (3, 5), (3, 7), (5, 5) };
        float per = (seconds - flyTime) / (shown.Length + 1f);
        foreach ((int p, int q) in shown)
        {
            yield return MultiplyRoutine(p, q, per);
        }

        // the rest of the composites ≤ 30 are struck quickly, in order
        for (int n = 4; n <= GridCount; n++)
        {
            if (IsPrime(n) || struck.Contains(n)) continue;
            struck.Add(n);
            labels[n - 1].Color = IsSquare(n) ? new Color(1f, .55f, .2f) : new Color(.55f, .55f, .62f);
            dots[n - 1].SetActive(true);
            dots[n - 1].GetComponent<Renderer>().enabled = true;
            Factory.SetColor(dots[n - 1].GetComponent<Renderer>(), new Color(.6f, .6f, .7f));
            StartCoroutine(PulseScale(dots[n - 1].transform, .26f, 2.2f, .5f));
            yield return new WaitForSeconds(per / 12f);
        }
    }

    IEnumerator FadeSieve(float seconds)
    {
        if (sieveRoot == null) yield break;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            float a = 1f - t / seconds;
            foreach (LineRenderer axis in sieveAxes)
            {
                Color c = axis.startColor;
                axis.startColor = new Color(c.r, c.g, c.b, a);
                axis.endColor = new Color(c.r, c.g, c.b, a);
            }

            yield return null;
        }

        sieveRoot.SetActive(false);
        if (spiralPath != null) spiralPath.enabled = false;
        if (stageRoot != null) stageRoot.SetActive(false);
    }

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
        CancelSieve();
        if (sieveRoot != null) sieveRoot.SetActive(false);
        if (spiralPath != null) spiralPath.enabled = false;
        struck.Clear();
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
        CancelSieve();
        HideLattice();
        foreach (TextBox f in factorLabels) f.gameObject.SetActive(false);
        for (int i = 0; i < GridCount; i++) dots[i].SetActive(true);
        caption.gameObject.SetActive(true);
        caption.Text = "Sacks spiral: number n at distance √n, one full turn per square";
        StartCoroutine(FadeSieve(seconds * .6f));
        float m0 = morph;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / seconds);
            morph = Mathf.Lerp(m0, 1f, k);
            reveal = Mathf.Clamp01((k - 0.2f) / 0.8f);
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

    /// <summary>Show the complete spiral immediately (used when a chapter starts without the hook having built it).</summary>
    public void ShowFull()
    {
        gameObject.SetActive(true);
        HideLattice();
        CancelSieve();
        if (sieveRoot != null) sieveRoot.SetActive(false);
        if (spiralPath != null) spiralPath.enabled = false;
        foreach (TextBox f in factorLabels) f.gameObject.SetActive(false);
        for (int i = 0; i < GridCount; i++)
        {
            int n = i + 1;
            dots[i].SetActive(IsPrime(n) || IsSquare(n));
            labels[i].Color = IsPrime(n) ? Factory.Palette.Zero : IsSquare(n) ? new Color(1f, .55f, .2f) : new Color(.28f, .28f, .34f);
        }

        morph = 1f;
        reveal = 1f;
        caption.gameObject.SetActive(false);
        Layout();
    }

    public IEnumerable<Renderer> Renderers()
    {
        foreach (GameObject d in dots) if (d.activeSelf) yield return d.GetComponent<Renderer>();
        foreach (LineRenderer r in routes) yield return r;
    }
}
