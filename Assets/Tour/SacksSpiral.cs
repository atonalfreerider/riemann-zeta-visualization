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

    /// <summary>Fade a number's dot and label in or out.</summary>
    IEnumerator FadeNumber(int n, float from, float to, float seconds)
    {
        Renderer r = dots[n - 1].GetComponent<Renderer>();
        dots[n - 1].SetActive(true);
        Color baseColor = IsPrime(n) ? Factory.Palette.Zero : IsSquare(n) ? new Color(1f, .55f, .2f) : new Color(.6f, .6f, .7f);
        Color labelColor = IsPrime(n) ? Factory.Palette.Zero : new Color(.5f, .5f, .58f);
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds));
            Factory.SetColor(r, new Color(baseColor.r, baseColor.g, baseColor.b, a));
            labels[n - 1].Color = new Color(labelColor.r, labelColor.g, labelColor.b, Mathf.Lerp(.35f, 1f, a));
            yield return null;
        }

        Factory.SetColor(r, new Color(baseColor.r, baseColor.g, baseColor.b, to));
        labels[n - 1].Color = new Color(labelColor.r, labelColor.g, labelColor.b, Mathf.Lerp(.35f, 1f, to));
        if (to <= 0f) dots[n - 1].SetActive(false);
    }

    readonly HashSet<int> revealedPrimes = new();

    /// <summary>Reveal every prime below n that is still hidden (the numbers the counting skipped).</summary>
    IEnumerator RevealPrimesBelow(int n, float seconds)
    {
        for (int k = 2; k < n; k++)
        {
            if (!IsPrime(k) || revealedPrimes.Contains(k)) continue;
            revealedPrimes.Add(k);
            StartCoroutine(FadeNumber(k, 0f, 1f, seconds));
            StartCoroutine(PulseScale(dots[k - 1].transform, .5f, 2.4f, seconds));
            TextBox tag = TextBoxLabel($"{k} is prime", 1.3f, Factory.Palette.Zero);
            tag.transform.SetParent(sieveRoot.transform, false);
            tag.transform.position = Pos(k) + Vector3.back * .5f + Vector3.up * .95f;
            sieveLabels.Add(tag);
            yield return new WaitForSeconds(seconds * .4f);
        }
    }

    /// <summary>
    /// One multiplication, drawn on the spiral itself:
    ///   count p along the road (dots on 1..p), count q beside it,
    ///   bend the q count off to the side, fill the rectangle to p×q dots,
    ///   roll every dot along the road to p×q (skipping the numbers in between), light p×q.
    /// </summary>
    IEnumerator MultiplyRoutine(int p, int q, float seconds)
    {
        int n = p * q;
        bool square = p == q;
        Color cp = PrimeColor(p), cq = PrimeColor(q);
        const float cell = .62f;
        float tCount = seconds * .22f, tBend = seconds * .2f, tFill = seconds * .1f, tRoll = seconds * .33f, tLand = seconds * .15f;

        Vector3 anchor = SpiralPos(1f, 0f) + Vector3.back * .3f;
        Vector3 u = (SpiralPos(2f, 0f) - SpiralPos(1f, 0f)).normalized;
        Vector3 v = new(-u.y, u.x, 0);          // perpendicular, in the spiral plane
        Vector3 side = v * .5f;                   // the second count sits beside the first

        stageRoot.SetActive(true);
        LineRenderer ax = stageRoot.transform.GetChild(0).GetComponent<LineRenderer>();
        LineRenderer ay = stageRoot.transform.GetChild(1).GetComponent<LineRenderer>();
        ax.enabled = false;
        ay.enabled = false;
        string hp = ColorUtility.ToHtmlStringRGB(cp), hq = ColorUtility.ToHtmlStringRGB(cq);
        stageLabel.Text = $"count <color=#{hp}>{p}</color>, count <color=#{hq}>{q}</color>";
        stageLabel.transform.position = origin + new Vector3(0, Extent30 + 2.0f, 0);

        // phase A: two counts along the road
        List<GameObject> dots = new();
        for (int k = 0; k < p; k++) { GameObject d = MakeDot(cp, .3f); d.transform.position = SpiralPos(k + 1f, 0f) + Vector3.back * .3f; d.transform.localScale = Vector3.zero; dots.Add(d); }
        for (int k = 0; k < q; k++) { GameObject d = MakeDot(cq, .3f); d.transform.position = SpiralPos(k + 1f, 0f) + Vector3.back * .3f + side; d.transform.localScale = Vector3.zero; dots.Add(d); }
        float t = 0f;
        while (t < tCount)
        {
            t += Time.deltaTime;
            for (int k = 0; k < dots.Count; k++)
            {
                int idx = k < p ? k : k - p;
                float delay = idx * (tCount * .5f / Mathf.Max(p, q)) + (k < p ? 0f : tCount * .1f);
                float uu = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - delay) / (tCount * .35f)));
                dots[k].transform.localScale = Vector3.one * (.3f * uu);
            }

            yield return null;
        }

        // phase B: straighten the p count along u, bend the q count off to the side along v
        stageLabel.Text = $"<color=#{hp}>{p}</color> × <color=#{hq}>{q}</color>";
        Vector3[] from = new Vector3[dots.Count];
        Vector3[] to = new Vector3[dots.Count];
        for (int k = 0; k < p; k++) { from[k] = dots[k].transform.position; to[k] = anchor + u * (k * cell); }
        for (int k = 0; k < q; k++) { from[p + k] = dots[p + k].transform.position; to[p + k] = anchor + v * (k * cell); }
        ax.enabled = true; ay.enabled = true;
        ax.startColor = cp; ax.endColor = cp; ay.startColor = cq; ay.endColor = cq;
        t = 0f;
        while (t < tBend)
        {
            t += Time.deltaTime;
            float k2 = Mathf.SmoothStep(0f, 1f, t / tBend);
            for (int k = 0; k < dots.Count; k++)
            {
                if (k >= p)
                {
                    // the q count swings about the anchor from the road direction to the side direction
                    float r = k - p == 0 ? 0f : (k - p) * cell;
                    float a0 = Mathf.Atan2(u.y, u.x), a1 = Mathf.Atan2(v.y, v.x);
                    float ang = Mathf.Lerp(a0, a1, k2);
                    Vector3 swung = anchor + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0) * r;
                    dots[k].transform.position = Vector3.Lerp(from[k], swung, Mathf.Clamp01(k2 * 2.5f));
                }
                else dots[k].transform.position = Vector3.Lerp(from[k], to[k], k2);
            }

            ax.SetPositions(new[] { anchor - u * .3f, anchor + u * ((p - 1) * cell + .3f) });
            Vector3 vEnd = anchor + v * ((q - 1) * cell + .3f);
            ay.SetPositions(new[] { anchor - v * .3f, Vector3.Lerp(anchor + u * ((q - 1) * cell + .3f), vEnd, k2) });
            yield return null;
        }

        // phase C: fill the rectangle with the missing dots
        for (int j = 1; j < q; j++)
        for (int i = 1; i < p; i++)
        {
            GameObject d = MakeDot(Color.white, .26f);
            d.transform.position = anchor + u * (i * cell) + v * (j * cell);
            d.transform.localScale = Vector3.zero;
            dots.Add(d);
        }

        // the duplicated corner (dot 1 of both counts) merges: hide the q count's first dot
        dots[p].SetActive(false);
        t = 0f;
        while (t < tFill)
        {
            t += Time.deltaTime;
            float uu = Mathf.SmoothStep(0f, 1f, t / tFill);
            for (int k = p + q; k < dots.Count; k++) dots[k].transform.localScale = Vector3.one * (.26f * uu);
            yield return null;
        }

        stageLabel.Text = $"<color=#{hp}>{p}</color> × <color=#{hq}>{q}</color> = {n} dots";

        // phase D: every dot rolls along the road to n, passing the numbers in between
        List<GameObject> rolling = new(dots); rolling.RemoveAt(p);
        Vector3[] roadStart = new Vector3[rolling.Count];
        for (int k = 0; k < rolling.Count; k++) roadStart[k] = rolling[k].transform.position;
        ax.enabled = false; ay.enabled = false;
        t = 0f;
        while (t < tRoll)
        {
            t += Time.deltaTime;
            for (int k = 0; k < rolling.Count; k++)
            {
                float delay = k * (tRoll * .4f / rolling.Count);
                float uu = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - delay) / (tRoll * .6f)));
                float road = Mathf.Lerp(1f, n, uu);
                Vector3 onRoad = SpiralPos(road, 0f) + Vector3.back * .3f;
                rolling[k].transform.position = Vector3.Lerp(roadStart[k], onRoad, Mathf.Clamp01(uu * 4f));
            }

            yield return null;
        }

        // phase E: merge on n and light it
        stageLabel.Text = $"<color=#{hp}>{p}</color> × <color=#{hq}>{q}</color> = {n}";
        t = 0f;
        while (t < tLand)
        {
            t += Time.deltaTime;
            float uu = Mathf.SmoothStep(0f, 1f, t / tLand);
            for (int k = 0; k < rolling.Count; k++) rolling[k].transform.localScale = Vector3.one * (.3f * (1f - uu));
            yield return null;
        }

        struck.Add(n);
        labels[n - 1].Color = square ? new Color(1f, .55f, .2f) : new Color(.55f, .55f, .62f);
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

        // the numbers the dots skipped over: the primes below n show up now
        yield return RevealPrimesBelow(n, Mathf.Max(.6f, seconds * .12f));
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

        // all the primes glow for a moment, then fade away except the 2
        revealedPrimes.Clear();
        revealedPrimes.Add(2);
        yield return new WaitForSeconds(0.8f);
        for (int k = 3; k <= GridCount; k++) if (IsPrime(k)) StartCoroutine(FadeNumber(k, 1f, 0f, 1.2f));
        StartCoroutine(PulseScale(dots[1].transform, .5f, 2.4f, 1.2f));
        yield return new WaitForSeconds(1.4f);

        (int p, int q)[] shown = { (2, 2), (2, 3), (2, 5), (3, 3), (2, 7), (3, 5), (5, 5), (3, 7) };
        float per = (seconds - flyTime - 2.2f) / (shown.Length + 1f);
        foreach ((int p, int q) in shown)
        {
            yield return MultiplyRoutine(p, q, per);
        }

        // the rest of the composites ≤ 30 are struck quickly, in order, and the last primes appear
        for (int n = 4; n <= GridCount; n++)
        {
            if (IsPrime(n) || struck.Contains(n)) continue;
            struck.Add(n);
            labels[n - 1].Color = IsSquare(n) ? new Color(1f, .55f, .2f) : new Color(.55f, .55f, .62f);
            dots[n - 1].SetActive(true);
            dots[n - 1].GetComponent<Renderer>().enabled = true;
            Factory.SetColor(dots[n - 1].GetComponent<Renderer>(), IsSquare(n) ? new Color(1f, .55f, .2f) : new Color(.6f, .6f, .7f));
            StartCoroutine(PulseScale(dots[n - 1].transform, .26f, 2.2f, .5f));
            yield return new WaitForSeconds(per / 12f);
        }

        yield return RevealPrimesBelow(GridCount + 1, .5f);
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

    GameObject hypothesisRoot;

    /// <summary>
    /// A visual hypothesis for the ending: the two chain rules drawn on the saddle.
    /// The square-root rule IS the spiral (radius √n ↔ arrow length 1/√n). For a dial value t, the numbers whose
    /// arrows point the same way are n = e^(2πk/t); their spiral positions trace a curve. Two curves are drawn,
    /// for the first two zeros (t = 14.13 and 21.02). Whether these curves mean anything is an open question.
    /// </summary>
    public void ShowHypothesis(bool on)
    {
        if (!on) { if (hypothesisRoot != null) hypothesisRoot.SetActive(false); return; }
        if (hypothesisRoot == null)
        {
            hypothesisRoot = new GameObject("Hypothesis");
            hypothesisRoot.transform.SetParent(transform, false);
            // the square-root rule: the spiral road itself, from 1 outward
            // only the inner turns, thin and dim: a guide for the eye, not a wall of yellow
            LineRenderer road = Factory.NewLine("sqrt road", .04f, new Color(1f, .85f, .3f, .55f), Factory.NoGlow);
            road.transform.SetParent(hypothesisRoot.transform, false);
            const int roadPts = 900, roadMax = 400;
            road.positionCount = roadPts;
            Vector3[] pts = new Vector3[roadPts];
            for (int i = 0; i < roadPts; i++) pts[i] = SpiralPos(1f + (roadMax - 1f) * i / (roadPts - 1f), 1f) + Vector3.back * .2f;
            road.SetPositions(pts);
            TextBox roadLabel = TextBoxLabel("the square-root rule: radius √n  (the chain's arrow 1/√n, inverted)", 2.4f, new Color(1f, .85f, .3f));
            roadLabel.transform.SetParent(hypothesisRoot.transform, false);
            roadLabel.transform.position = origin + new Vector3(0, -Extent - 4.5f, 0);

            // The logarithm rule: arrow n turns by t·log n. Between numbers n and m the chain makes one full
            // turn when log(m/n) = 2π/t, i.e. when the spiral radius grows by e^(π/t). So each collision's t
            // picks out rings whose radii grow by a constant ratio; where a ring meets the spiral is one number.
            float[] ts = { 14.134725f, 21.022040f };
            Color[] cs = { new(.55f, .95f, 1f), new(1f, .45f, .95f) };
            for (int z = 0; z < ts.Length; z++)
            {
                float tz = ts[z];
                float ratio = Mathf.Exp(Mathf.PI / tz);
                for (float r = 1f; r <= Mathf.Sqrt(Count); r *= ratio)
                {
                    LineRenderer ring = Factory.NewLine("ring " + tz, z == 0 ? .07f : .05f, cs[z], Factory.SoftGlowIntensity);
                    ring.transform.SetParent(hypothesisRoot.transform, false);
                    ring.loop = true;
                    const int segs = 72;
                    ring.positionCount = segs;
                    Vector3[] rp = new Vector3[segs];
                    for (int i = 0; i < segs; i++)
                    {
                        float a = i / (float)segs * Mathf.PI * 2f;
                        float x = r * Mathf.Cos(a), y = r * Mathf.Sin(a);
                        rp[i] = origin + new Vector3(x, y, SaddleK * (x * x - y * y)) + Vector3.back * (.2f + z * .05f);
                    }

                    ring.SetPositions(rp);
                    float n = r * r;
                    GameObject dot = Factory.NewSphere("ring dot", .4f, cs[z], Factory.GlowIntensity);
                    dot.transform.SetParent(hypothesisRoot.transform, false);
                    dot.transform.position = SpiralPos(n, 1f) + Vector3.back * .3f;
                }

                TextBox l = TextBoxLabel($"the logarithm rule at t = {tz:0.00}:  one full turn of the chain per ring, radii × e^(π/t) = {ratio:0.000}", 2.2f, cs[z]);
                l.transform.SetParent(hypothesisRoot.transform, false);
                l.transform.position = origin + new Vector3(0, -Extent - 7.5f - z * 3f, 0);
            }
        }

        hypothesisRoot.SetActive(true);
    }

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
