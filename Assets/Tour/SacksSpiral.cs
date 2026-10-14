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

    static bool IsPrimePower(int n)
    {
        for (int p = 2; p <= n; p++)
        {
            if (!IsPrime(p) || n % p != 0) continue;
            int m = n;
            while (m % p == 0) m /= p;
            return m == 1;
        }

        return false;
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
        gameObject.SetActive(true);
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

    /// <summary>Altitude: rows of dots stack toward the camera (−Z), one unit per row.</summary>
    static Vector3 Alt(int row) => Vector3.back * (0.3f + row * 1.0f);

    /// <summary>
    /// One multiplication, len × rows, built from the spiral itself:
    ///   row 0: dots on road positions 1..len;
    ///   rows 1..rows−1: the same len dots stacked one altitude unit above the previous row;
    ///   then each stacked row uncoils along the road to the next free stretch (k·len+1 .. k·len+len)
    ///   and drops onto the spiral. The last dot lands on len·rows, which lights up (orange if a square).
    ///   Every prime the rows landed on along the way is revealed: it was passed over, never produced.
    /// </summary>
    IEnumerator MultiplyRoutine(int len, int rows, float seconds)
    {
        int n = len * rows;
        bool square = len == rows;
        Color cLen = PrimeColor(len);
        string h = ColorUtility.ToHtmlStringRGB(cLen);
        float tBase = seconds * .18f, tStack = seconds * .22f, tUncoil = seconds * .45f, tLand = seconds * .15f;
        float perRow = tStack / Mathf.Max(1, rows - 1);

        stageRoot.SetActive(true);
        stageLabel.Text = $"count <color=#{h}>{len}</color>";
        stageLabel.transform.position = origin + new Vector3(0, Extent30 + 2.0f, 0);

        // row 0 on the road
        List<List<GameObject>> rowDots = new();
        List<GameObject> baseRow = new();
        for (int i = 0; i < len; i++)
        {
            GameObject d = MakeDot(cLen, .3f);
            d.transform.position = SpiralPos(i + 1f, 0f) + Alt(0);
            d.transform.localScale = Vector3.zero;
            baseRow.Add(d);
        }

        rowDots.Add(baseRow);
        float t = 0f;
        while (t < tBase)
        {
            t += Time.deltaTime;
            for (int i = 0; i < len; i++)
            {
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - i * tBase * .5f / len) / (tBase * .4f)));
                baseRow[i].transform.localScale = Vector3.one * (.3f * u);
            }

            yield return null;
        }

        // stacked rows, one altitude unit each
        for (int k = 1; k < rows; k++)
        {
            stageLabel.Text = $"<color=#{h}>{len}</color> × {k + 1}";
            List<GameObject> row = new();
            for (int i = 0; i < len; i++)
            {
                GameObject d = MakeDot(new Color(1f, 1f, 1f), .28f);
                d.transform.position = SpiralPos(i + 1f, 0f) + Alt(k - 1);
                row.Add(d);
            }

            rowDots.Add(row);
            t = 0f;
            while (t < perRow)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / perRow);
                for (int i = 0; i < len; i++) row[i].transform.position = SpiralPos(i + 1f, 0f) + Vector3.Lerp(Alt(k - 1), Alt(k), u);
                yield return null;
            }
        }

        // uncoil: row k slides along the road to positions k·len+1 .. k·len+len, then drops
        float perUncoil = tUncoil / Mathf.Max(1, rows - 1);
        for (int k = 1; k < rows; k++)
        {
            List<GameObject> row = rowDots[k];
            t = 0f;
            while (t < perUncoil)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / perUncoil);
                for (int i = 0; i < len; i++)
                {
                    float road = Mathf.Lerp(i + 1f, k * len + i + 1f, u);
                    float drop = Mathf.Clamp01((u - .75f) / .25f);
                    row[i].transform.position = SpiralPos(road, 0f) + Vector3.Lerp(Alt(k), Alt(0), drop);
                }

                yield return null;
            }

            // the numbers this row landed on: primes among them were passed over, never produced
            for (int i = 0; i < len; i++)
            {
                int m = k * len + i + 1;
                if (m == n) continue;
                if (IsPrime(m) && !revealedPrimes.Contains(m))
                {
                    revealedPrimes.Add(m);
                    StartCoroutine(FadeNumber(m, 0f, 1f, .6f));
                    StartCoroutine(PulseScale(dots[m - 1].transform, .5f, 2.4f, .8f));
                    TextBox tag = TextBoxLabel($"{m}: passed over → prime", 1.2f, Factory.Palette.Zero);
                    tag.transform.SetParent(sieveRoot.transform, false);
                    tag.transform.position = Pos(m) + Vector3.back * .5f + Vector3.up * .95f;
                    sieveLabels.Add(tag);
                }
            }
        }

        // land: the last dot is the product
        stageLabel.Text = $"<color=#{h}>{len}</color> × {rows} = {n}" + (square ? "   a perfect square" : "");
        t = 0f;
        while (t < tLand)
        {
            t += Time.deltaTime;
            float u = Mathf.SmoothStep(0f, 1f, t / tLand);
            foreach (List<GameObject> row in rowDots) foreach (GameObject d in row) d.transform.localScale = Vector3.one * (.3f * (1f - u));
            yield return null;
        }

        struck.Add(n);
        labels[n - 1].Color = square ? new Color(1f, .55f, .2f) : new Color(.55f, .55f, .62f);
        this.dots[n - 1].SetActive(true);
        this.dots[n - 1].GetComponent<Renderer>().enabled = true;
        Factory.SetColor(this.dots[n - 1].GetComponent<Renderer>(), square ? new Color(1f, .55f, .2f) : new Color(.6f, .6f, .7f));
        Factory.SetIntensity(this.dots[n - 1].GetComponent<Renderer>(), square ? Factory.GlowIntensity : Factory.SoftGlowIntensity);
        StartCoroutine(PulseScale(this.dots[n - 1].transform, square ? .34f : .26f, 3.2f, .8f));
        TextBox hit = TextBoxLabel($"{len}×{rows}", 1.3f, square ? new Color(1f, .55f, .2f) : new Color(.75f, .75f, .85f));
        hit.transform.SetParent(sieveRoot.transform, false);
        hit.transform.position = Pos(n) + Vector3.back * .5f + Vector3.down * .85f;
        sieveLabels.Add(hit);
        ClearWorkDots();
        stageRoot.SetActive(false);
        yield return new WaitForSeconds(seconds * .05f);
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
        labels[0].Color = new Color(.3f, .3f, .36f); // 1 is skipped: neither prime nor a jumping-off point
        StartCoroutine(PulseScale(dots[1].transform, .5f, 2.4f, 1.2f));
        yield return new WaitForSeconds(1.4f);

        (int p, int q)[] shown = { (2, 2), (3, 2), (3, 3), (5, 2), (7, 2), (5, 3), (5, 5), (7, 3) };
        float per = (seconds - flyTime - 2.2f) / (shown.Length + 1f);
        foreach ((int len, int rows) in shown)
        {
            yield return MultiplyRoutine(len, rows, per);
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
        if (!gameObject.activeInHierarchy) return;
        for (int i = 0; i < dots.Count; i++)
        {
            int n = i + 1;
            if (IsSquare(n) && dots[i].activeSelf) StartCoroutine(PulseScale(dots[i].transform, n <= GridCount ? .32f : .22f, 3.2f, seconds));
        }
    }

    /// <summary>Briefly brighten every composite so the prime pattern reads as the negative space.</summary>
    public void PulseComposites(float seconds)
    {
        if (!gameObject.activeInHierarchy) return;
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
    GameObject zeroDropsRoot, interferenceRoot;
    readonly List<GameObject> zeroMarkers = new();
    const float ZetaTMax = 140f, ZetaT = 0.25f, ZetaXY = 1.4f;
    static double[] Zeros => System.Array.FindAll(Main.Instance.zetaZeroValues, g => g <= ZetaTMax);
    readonly List<LineRenderer> fieldRings = new();
    readonly List<float> fieldN = new();
    const int FieldRings = 120, FieldSegs = 96;
    const float FieldNMin = 1.5f, FieldNMax = 420f, FieldAmp = 1.6f;
    Coroutine fieldRoutine, dropRoutine;

    /// <summary>
    /// Each zero spirals down from its height on the axis to the saddle surface. The guess: treat the height γ
    /// as a number and let it land where γ would sit on the Sacks spiral (radius √γ, angle 2π√γ); the descent
    /// is a helix that widens as it falls, and the trajectory stays drawn.
    /// </summary>
    public void DropZeros(float seconds)
    {
        if (dropRoutine != null) StopCoroutine(dropRoutine);
        dropRoutine = StartCoroutine(DropZerosRoutine(seconds));
    }

    IEnumerator DropZerosRoutine(float seconds)
    {
        if (zeroDropsRoot == null) yield break;
        foreach (Transform c in zeroDropsRoot.transform) Destroy(c.gameObject);
        float per = seconds / Zeros.Length;
        for (int z = 0; z < Zeros.Length; z++)
        {
            float g = (float)Zeros[z];
            Vector3 top = origin + Vector3.back * (g * ZetaT);
            Vector3 land = SpiralPos(g, 1f) + Vector3.back * .25f;
            float a1 = 2f * Mathf.PI * Mathf.Sqrt(g);
            float rLand = R * Mathf.Sqrt(g);
            LineRenderer trail = Factory.NewLine("drop " + z, .05f, new Color(1f, .9f, .4f), Factory.SoftGlowIntensity);
            trail.transform.SetParent(zeroDropsRoot.transform, false);
            const int steps = 60;
            Vector3[] pts = new Vector3[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                float u = i / (float)steps;
                float r = rLand * u;
                float a = a1 - (1f - u) * 2f * Mathf.PI * 2f; // two extra turns on the way down
                float x = r * Mathf.Cos(a), y = r * Mathf.Sin(a);
                float surface = SaddleK * (x * x - y * y);
                float h = Mathf.Lerp(g * ZetaT, 0f, Mathf.SmoothStep(0f, 1f, u));
                pts[i] = origin + new Vector3(x, y, surface * u) + Vector3.back * (h + .25f);
            }

            GameObject runner = Factory.NewSphere("falling zero", .32f, Factory.Palette.Zero, Factory.GlowIntensity);
            runner.transform.SetParent(zeroDropsRoot.transform, false);
            float t = 0f;
            float travel = per * .85f;
            while (t < travel)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / travel);
                int shown = Mathf.Max(2, Mathf.CeilToInt(u * steps) + 1);
                trail.positionCount = shown;
                trail.SetPositions(pts[..shown]);
                runner.transform.position = pts[Mathf.Min(steps, shown - 1)];
                yield return null;
            }

            trail.positionCount = pts.Length;
            trail.SetPositions(pts);
            runner.transform.position = land;
            GameObject mark = Factory.NewSphere("landing " + z, .42f, Factory.Palette.Zero, Factory.GlowIntensity);
            mark.transform.SetParent(zeroDropsRoot.transform, false);
            mark.transform.position = land;
            StartCoroutine(PulseScale(mark.transform, .42f, 2.5f, .7f));
            TextBox l = TextBoxLabel($"{g:0.00}", 1.4f, Factory.Palette.Zero);
            l.transform.SetParent(zeroDropsRoot.transform, false);
            l.transform.position = land + Vector3.back * .5f + Vector3.up * .8f;
            Destroy(runner);
            yield return new WaitForSeconds(per * .15f);
        }
    }

    /// <summary>
    /// Interference on the saddle: concentric rings (n = (r/R)²) displaced along the normal by the stacked
    /// cosine waves −Σ cos(γ·log n) over the first k zeros, normalised. Ridges form on the circles r = √p
    /// through the primes as the waves pile up.
    /// </summary>
    public void StartInterference(float seconds)
    {
        if (interferenceRoot == null)
        {
            interferenceRoot = new GameObject("Interference");
            interferenceRoot.transform.SetParent(hypothesisRoot.transform, false);
            for (int i = 0; i < FieldRings; i++)
            {
                float n = FieldNMin * Mathf.Pow(FieldNMax / FieldNMin, i / (FieldRings - 1f));
                fieldN.Add(n);
                LineRenderer ring = Factory.NewLine("field ring", .035f, Factory.Palette.Cosine, Factory.SoftGlowIntensity);
                ring.transform.SetParent(interferenceRoot.transform, false);
                ring.loop = true;
                ring.positionCount = FieldSegs;
                fieldRings.Add(ring);
            }
        }

        interferenceRoot.SetActive(true);
        if (fieldRoutine != null) StopCoroutine(fieldRoutine);
        fieldRoutine = StartCoroutine(InterferenceRoutine(seconds));
    }

    public void HideInterference()
    {
        if (fieldRoutine != null) StopCoroutine(fieldRoutine);
        if (interferenceRoot != null) interferenceRoot.SetActive(false);
    }

    IEnumerator InterferenceRoutine(float seconds)
    {
        int maxZeros = Mathf.Min(60, Main.Instance != null ? Main.Instance.zetaZeroValues.Length : 0);
        float t = 0f;
        int last = -1;
        while (t < seconds)
        {
            t += Time.deltaTime;
            int k = Mathf.Clamp(Mathf.FloorToInt(1 + (maxZeros - 1) * Mathf.Pow(t / seconds, 1.6f)), 1, maxZeros);
            if (k != last)
            {
                last = k;
                SetInterference(k);
                caption.gameObject.SetActive(true);
                caption.Text = $"interference on the saddle: {k} collision wave{(k > 1 ? "s" : "")} stacked, ridges on the circles through the primes";
            }

            yield return null;
        }
    }

    readonly Vector3[] ringBuffer = new Vector3[FieldSegs];

    GameObject waveStackRoot;
    LineRenderer masterWave;
    Coroutine waveRoutine;
    const int WavePts = 2400;
    const float WaveAmp = 2.2f;

    float WaveN(int i) => FieldNMin * Mathf.Pow(FieldNMax / FieldNMin, i / (WavePts - 1f));

    /// <summary>The spiral road at a given altitude above the saddle, displaced along the normal by a wave.</summary>
    Vector3 OnSpiral(float n, float altitude, float displacement) => SpiralPos(n, 1f) + Vector3.back * (altitude + displacement + .15f);

    void EnsureWaveStack()
    {
        if (waveStackRoot != null) return;
        waveStackRoot = new GameObject("WaveStack");
        waveStackRoot.transform.SetParent(hypothesisRoot.transform, false);
        masterWave = Factory.NewLine("master wave", .10f, Factory.Palette.Cosine, Factory.GlowIntensity);
        masterWave.transform.SetParent(waveStackRoot.transform, false);
        masterWave.positionCount = WavePts;
        masterWave.numCornerVertices = 3;
        // prime ticks standing on the saddle along the spiral, so the spikes can be read against them
        for (int n = 2; n <= (int)FieldNMax; n++)
        {
            bool prime = IsPrime(n);
            bool primePower = !prime && IsPrimePower(n);
            if (!prime && !primePower) continue;
            Color c = prime ? Factory.Palette.Zero : new Color(1f, .55f, .2f);
            LineRenderer tick = Factory.NewLine(prime ? "prime tick" : "prime power tick", prime ? .05f : .04f, c, Factory.SoftGlowIntensity);
            tick.transform.SetParent(waveStackRoot.transform, false);
            tick.positionCount = 2;
            tick.SetPositions(new[] { OnSpiral(n, 0f, -0.3f), OnSpiral(n, 0f, prime ? 0.9f : 0.6f) });
        }
    }

    // ---- exploration layers (static views of the interference, used by ExplorationRenderTest) ----
    GameObject explorationRoot;

    void ClearExploration()
    {
        if (explorationRoot != null) Destroy(explorationRoot);
        explorationRoot = new GameObject("Exploration");
        explorationRoot.transform.SetParent(hypothesisRoot.transform, false);
    }

    Vector3[] WaveCurve(System.Func<float, double> value, float altitude, float amp, int pts, float nMax)
    {
        Vector3[] arr = new Vector3[pts];
        for (int i = 0; i < pts; i++)
        {
            float n = FieldNMin * Mathf.Pow(nMax / FieldNMin, i / (pts - 1f));
            arr[i] = OnSpiral(n, altitude, (float)value(n) * amp);
        }

        return arr;
    }

    /// <summary>Every wave kept at its own height γ·ZetaT above the saddle: the stack as a static object.</summary>
    public void ShowLayerStack(int count, float nMax, float amp)
    {
        ShowHypothesis(true);
        EnsureWaveStack();
        ClearExploration();
        double[] zs = Main.Instance.zetaZeroValues;
        for (int k = 0; k < Mathf.Min(count, zs.Length); k++)
        {
            double g = zs[k];
            LineRenderer line = Factory.NewLine("layer " + k, .06f, Annotations.CollisionColor(k), Factory.GlowIntensity);
            line.transform.SetParent(explorationRoot.transform, false);
            line.numCornerVertices = 3;
            Vector3[] pts = WaveCurve(n => -System.Math.Cos(g * System.Math.Log(n)), (float)g * ZetaT, amp, 1600, nMax);
            line.positionCount = pts.Length;
            line.SetPositions(pts);
        }
    }

    /// <summary>Cumulative sums S_k = −Σ_{j≤k} cos(γ_j·log n), each at altitude k·dz: how the spikes converge with k.</summary>
    public void ShowPartialSumSheet(int count, float dz, float nMax, float amp)
    {
        ShowHypothesis(true);
        EnsureWaveStack();
        ClearExploration();
        double[] zs = Main.Instance.zetaZeroValues;
        for (int k = 1; k <= Mathf.Min(count, zs.Length); k++)
        {
            int kk = k;
            float t = (k - 1f) / Mathf.Max(1, count - 1);
            Color c = Color.Lerp(new Color(.2f, .5f, 1f), new Color(1f, .9f, .3f), t);
            LineRenderer line = Factory.NewLine("partial " + k, .05f, c, Factory.SoftGlowIntensity);
            line.transform.SetParent(explorationRoot.transform, false);
            line.numCornerVertices = 2;
            float scale = amp / Mathf.Sqrt(kk);
            Vector3[] pts = WaveCurve(n =>
            {
                double logN = System.Math.Log(n), sum = 0;
                for (int j = 0; j < kk; j++) sum -= System.Math.Cos(zs[j] * logN);
                return sum;
            }, k * dz, scale, 1600, nMax);
            line.positionCount = pts.Length;
            line.SetPositions(pts);
        }
    }

    /// <summary>Same cumulative sums, but along a straight ray from the centre (a cross-section plane through the saddle).</summary>
    public void ShowCrossSection(int count, float dz, float nMax, float amp, Vector3 rayDir)
    {
        ShowHypothesis(true);
        EnsureWaveStack();
        ClearExploration();
        double[] zs = Main.Instance.zetaZeroValues;
        rayDir.Normalize();
        for (int k = 1; k <= Mathf.Min(count, zs.Length); k++)
        {
            int kk = k;
            float t = (k - 1f) / Mathf.Max(1, count - 1);
            Color c = Color.Lerp(new Color(.2f, .5f, 1f), new Color(1f, .9f, .3f), t);
            LineRenderer line = Factory.NewLine("section " + k, .05f, c, Factory.SoftGlowIntensity);
            line.transform.SetParent(explorationRoot.transform, false);
            const int pts = 1200;
            Vector3[] arr = new Vector3[pts];
            float scale = amp / Mathf.Sqrt(kk);
            for (int i = 0; i < pts; i++)
            {
                float n = FieldNMin * Mathf.Pow(nMax / FieldNMin, i / (pts - 1f));
                double logN = System.Math.Log(n), sum = 0;
                for (int j = 0; j < kk; j++) sum -= System.Math.Cos(zs[j] * logN);
                float r = R * Mathf.Sqrt(n);
                float x = r * rayDir.x, y = r * rayDir.y;
                float surface = SaddleK * (x * x - y * y);
                arr[i] = origin + new Vector3(x, y, surface) + Vector3.back * (k * dz + (float)sum * scale + .15f);
            }

            line.positionCount = pts;
            line.SetPositions(arr);
        }

        // prime ticks along the ray
        for (int n = 2; n <= (int)nMax; n++)
        {
            if (!IsPrime(n)) continue;
            float r = R * Mathf.Sqrt(n);
            float x = r * rayDir.x, y = r * rayDir.y;
            float surface = SaddleK * (x * x - y * y);
            LineRenderer tick = Factory.NewLine("ray prime", .05f, Factory.Palette.Zero, Factory.GlowIntensity);
            tick.transform.SetParent(explorationRoot.transform, false);
            tick.positionCount = 2;
            tick.SetPositions(new[] { origin + new Vector3(x, y, surface) + Vector3.back * .1f, origin + new Vector3(x, y, surface) + Vector3.back * (count * dz + 1.5f) });
        }
    }

    /// <summary>Static master wave with k zeros on the saddle (plus ticks), no animation.</summary>
    public void ShowMasterStatic(int k)
    {
        ShowHypothesis(true);
        EnsureWaveStack();
        ClearExploration();
        waveStackRoot.SetActive(true);
        SetMasterWave(k);
    }

    public void ShowRingFieldStatic(int k)
    {
        ShowHypothesis(true);
        if (interferenceRoot == null) StartInterference(0.01f);
        if (fieldRoutine != null) StopCoroutine(fieldRoutine);
        interferenceRoot.SetActive(true);
        SetInterference(k);
    }

    public void ShowTrail(bool on)
    {
        Transform t = hypothesisRoot != null ? hypothesisRoot.transform.Find("ZetaTrail") : null;
        if (t != null) t.gameObject.SetActive(on);
    }

    /// <summary>Each wave by itself at height γ·ZetaT, along a straight ray: the (log n, γ) fringe pattern of cos(γ·log n).</summary>
    public void ShowLayerSection(int count, float nMax, float amp, Vector3 rayDir)
    {
        ShowHypothesis(true);
        EnsureWaveStack();
        ClearExploration();
        double[] zs = Main.Instance.zetaZeroValues;
        rayDir.Normalize();
        for (int k = 0; k < Mathf.Min(count, zs.Length); k++)
        {
            double g = zs[k];
            LineRenderer line = Factory.NewLine("layer section " + k, .05f, Annotations.CollisionColor(k), Factory.SoftGlowIntensity);
            line.transform.SetParent(explorationRoot.transform, false);
            const int pts = 900;
            Vector3[] arr = new Vector3[pts];
            for (int i = 0; i < pts; i++)
            {
                float n = FieldNMin * Mathf.Pow(nMax / FieldNMin, i / (pts - 1f));
                float r = R * Mathf.Sqrt(n);
                float x = r * rayDir.x, y = r * rayDir.y;
                float surface = SaddleK * (x * x - y * y);
                arr[i] = origin + new Vector3(x, y, surface) + Vector3.back * ((float)g * ZetaT - amp * Mathf.Cos((float)g * Mathf.Log(n)) + .15f);
            }

            line.positionCount = pts;
            line.SetPositions(arr);
        }

        for (int n = 2; n <= (int)nMax; n++)
        {
            if (!IsPrime(n)) continue;
            float r = R * Mathf.Sqrt(n);
            float x = r * rayDir.x, y = r * rayDir.y;
            float surface = SaddleK * (x * x - y * y);
            LineRenderer tick = Factory.NewLine("ray prime", .06f, Factory.Palette.Zero, Factory.GlowIntensity);
            tick.transform.SetParent(explorationRoot.transform, false);
            tick.positionCount = 2;
            tick.SetPositions(new[] { origin + new Vector3(x, y, surface) + Vector3.back * .1f, origin + new Vector3(x, y, surface) + Vector3.back * ((float)zs[Mathf.Min(count, zs.Length) - 1] * ZetaT + 2f) });
        }
    }

    // ---- connections: saddle wireframe, chain-arrow field, explicit-formula wave, fringe sheet ----

    Vector3 Saddle(float x, float y) => origin + new Vector3(x, y, SaddleK * (x * x - y * y));

    /// <summary>A dim polar wireframe of the saddle z = (x² − y²)/k, so the surface itself is visible.</summary>
    public void ShowSaddleWire(float rMax, bool on)
    {
        Transform old = hypothesisRoot.transform.Find("SaddleWire");
        if (old != null) old.gameObject.SetActive(on);
        if (!on || old != null) return;
        GameObject root = new("SaddleWire");
        root.transform.SetParent(hypothesisRoot.transform, false);
        Color c = new(.35f, .4f, .55f, .55f);
        for (float r = 4f; r <= rMax; r += 4f)
        {
            LineRenderer ring = Factory.NewLine("wire ring", .03f, c);
            ring.transform.SetParent(root.transform, false);
            ring.loop = true;
            ring.positionCount = 96;
            Vector3[] pts = new Vector3[96];
            for (int i = 0; i < 96; i++) { float a = i / 96f * Mathf.PI * 2f; pts[i] = Saddle(r * Mathf.Cos(a), r * Mathf.Sin(a)) + Vector3.back * .02f; }
            ring.SetPositions(pts);
        }

        for (int s = 0; s < 24; s++)
        {
            float a = s / 24f * Mathf.PI * 2f;
            bool ridge = s % 12 == 0, valley = s % 12 == 6;
            LineRenderer spoke = Factory.NewLine("wire spoke", ridge || valley ? .06f : .025f, ridge ? new Color(1f, .55f, .2f) : valley ? new Color(.3f, .7f, 1f) : c, ridge || valley ? Factory.SoftGlowIntensity : Factory.NoGlow);
            spoke.transform.SetParent(root.transform, false);
            spoke.positionCount = 40;
            Vector3[] pts = new Vector3[40];
            for (int i = 0; i < 40; i++) { float r = rMax * i / 39f; pts[i] = Saddle(r * Mathf.Cos(a), r * Mathf.Sin(a)) + Vector3.back * .02f; }
            spoke.SetPositions(pts);
        }

        TextBox l1 = TextBoxLabel("ridge (x axis): √n ≈ whole → squares k² and pronics k(k+1), never prime", 2.0f, new Color(1f, .55f, .2f));
        l1.transform.SetParent(root.transform, false);
        l1.transform.position = origin + new Vector3(0, -Extent - 4.5f, 0);
        TextBox l2 = TextBoxLabel("valley (y axis): √n ≈ whole + ¼ or ¾", 2.0f, new Color(.3f, .7f, 1f));
        l2.transform.SetParent(root.transform, false);
        l2.transform.position = origin + new Vector3(0, -Extent - 7.5f, 0);
    }

    /// <summary>
    /// The chain's arrows placed on the spiral: at number n, for dial t, arrow n of the chain has length 1/√n and
    /// direction −t·log n. Its horizontal component IS cos(t·log n): the cosine wave and the chain are one object.
    /// </summary>
    public void ShowChainField(float t, int nMax)
    {
        ShowHypothesis(true);
        ClearExploration();
        for (int n = 1; n <= nMax; n++)
        {
            float ang = -t * Mathf.Log(n);
            float len = 1.2f * Mathf.Pow(n, -0.5f) * 2.2f + .25f;
            Vector3 p = SpiralPos(n, 1f) + Vector3.back * .3f;
            Vector3 d = new(Mathf.Cos(ang), Mathf.Sin(ang), 0);
            float cosv = Mathf.Cos(t * Mathf.Log(n));
            Color c = Color.Lerp(new Color(.3f, .5f, 1f), new Color(1f, .9f, .3f), (cosv + 1f) * .5f);
            LineRenderer arrow = Factory.NewLine("chain arrow " + n, .06f, c, Factory.GlowIntensity);
            arrow.transform.SetParent(explorationRoot.transform, false);
            arrow.positionCount = 2;
            arrow.SetPositions(new[] { p, p + d * len });
            Vector3 tip = p + d * len, side = new Vector3(-d.y, d.x, 0);
            LineRenderer head = Factory.NewLine("head", .05f, c, Factory.GlowIntensity);
            head.transform.SetParent(explorationRoot.transform, false);
            head.positionCount = 3;
            head.SetPositions(new[] { tip - d * .25f + side * .14f, tip, tip - d * .25f - side * .14f });
        }

        TextBox l = TextBoxLabel($"chain arrows at dial t = {t:0.00}: length 1/√n, turned by −t·log n.  Their x-components are the wave cos(t·log n)", 2.0f, Color.white);
        l.transform.SetParent(explorationRoot.transform, false);
        l.transform.position = origin + new Vector3(0, -R * Mathf.Sqrt(nMax) - 3f, 0);
    }

    /// <summary>
    /// The explicit-formula weights instead of plain cosines: Σ_k 4√n·(cos(γ·log n) + 2γ·sin(γ·log n))/(1+4γ²)
    /// = n − ψ(n) − ln 2π (the smooth part minus the staircase), riding the spiral: a sawtooth that drops by log p at each prime.
    /// </summary>
    public void ShowExplicitWave(int k, float nMax, float amp)
    {
        ShowHypothesis(true);
        EnsureWaveStack();
        ClearExploration();
        waveStackRoot.SetActive(true);
        double[] zs = Main.Instance.zetaZeroValues;
        LineRenderer line = Factory.NewLine("explicit wave", .09f, new Color(1f, .5f, .9f), Factory.GlowIntensity);
        line.transform.SetParent(explorationRoot.transform, false);
        line.numCornerVertices = 3;
        const int pts = 2400;
        Vector3[] arr = new Vector3[pts];
        for (int i = 0; i < pts; i++)
        {
            float n = FieldNMin * Mathf.Pow(nMax / FieldNMin, i / (pts - 1f));
            double sum = 0;
            for (int z = 0; z < k; z++) sum += ZetaMath.ExplicitFormulaTerm(n, zs[z]);
            float v = (float)(4.0 * System.Math.Sqrt(n) * sum); // = n − ψ(n) − ln2π − trivial term, up to the k-zero truncation
            arr[i] = OnSpiral(n, 0f, v * amp);
        }

        line.positionCount = pts;
        line.SetPositions(arr);
        masterWave.enabled = false;
        TextBox l = TextBoxLabel($"explicit-formula weights, {k} zeros: 4√n·Σ(cos + 2γ sin)/(1+4γ²) = n − ψ(n) − ln2π   (drops by log p at each prime)", 2.0f, new Color(1f, .5f, .9f));
        l.transform.SetParent(explorationRoot.transform, false);
        l.transform.position = origin + new Vector3(0, -R * Mathf.Sqrt(nMax) - 3f, 0);
    }

    public void RestoreMasterWave()
    {
        if (masterWave != null) masterWave.enabled = true;
    }

    /// <summary>
    /// The fringe sheet cos(t·log n): a vertical heat-map standing on the saddle along the +x ridge, n along the
    /// radius (position √n, so it matches the spiral), t up the axis. Yellow rows mark the zeros γ_k.
    /// Summing the yellow rows gives the master wave (drawn along the bottom edge); summing each column over n with
    /// weight 1/√n gives ζ(½ + it) (|ζ| drawn up the left edge, touching zero exactly at the yellow rows).
    /// </summary>
    public void ShowFringeSheet(float nMax, float tMax, int zeros)
    {
        ShowHypothesis(true);
        EnsureWaveStack();
        ClearExploration();
        waveStackRoot.SetActive(false);
        const int nRes = 420, tRes = 300;
        Mesh mesh = new() { name = "fringe sheet", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        Vector3[] verts = new Vector3[nRes * tRes];
        Color[] cols = new Color[nRes * tRes];
        Color neg = new(.1f, .3f, 1f), pos = new(1f, .85f, .25f), mid = new(.02f, .02f, .05f);
        for (int j = 0; j < tRes; j++)
        {
            float t = tMax * j / (tRes - 1f);
            for (int i = 0; i < nRes; i++)
            {
                float n = 1f + (nMax - 1f) * i / (nRes - 1f);
                float r = R * Mathf.Sqrt(n);
                float v = Mathf.Cos(t * Mathf.Log(n));
                verts[j * nRes + i] = Saddle(r, 0f) + Vector3.back * (t * ZetaT + .3f);
                cols[j * nRes + i] = v >= 0 ? Color.Lerp(mid, pos, v) : Color.Lerp(mid, neg, -v);
            }
        }

        int[] tris = new int[(nRes - 1) * (tRes - 1) * 6];
        int q = 0;
        for (int j = 0; j < tRes - 1; j++)
        for (int i = 0; i < nRes - 1; i++)
        {
            int a = j * nRes + i, b = a + 1, c2 = a + nRes, d = c2 + 1;
            tris[q++] = a; tris[q++] = d; tris[q++] = b; tris[q++] = a; tris[q++] = c2; tris[q++] = d;
            // both sides visible: the shader culls nothing (Cull Off), so one winding is enough
        }

        mesh.vertices = verts; mesh.colors = cols; mesh.triangles = tris; mesh.RecalculateBounds();
        GameObject go = new("FringeSheet");
        go.transform.SetParent(explorationRoot.transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = Factory.mainMat;
        Factory.SetIntensity(mr, 1.0f);

        double[] zs = Main.Instance.zetaZeroValues;
        for (int k = 0; k < zeros && k < zs.Length && zs[k] <= tMax; k++)
        {
            float g = (float)zs[k];
            LineRenderer row = Factory.NewLine("zero row", .07f, Factory.Palette.Zero, Factory.GlowIntensity);
            row.transform.SetParent(explorationRoot.transform, false);
            row.positionCount = 2;
            row.SetPositions(new[] { Saddle(R, 0f) + Vector3.back * (g * ZetaT + .35f), Saddle(R * Mathf.Sqrt(nMax), 0f) + Vector3.back * (g * ZetaT + .35f) });
        }

        // bottom edge: the master wave = sum of the yellow rows
        LineRenderer master = Factory.NewLine("sheet master", .08f, Factory.Palette.Cosine, Factory.GlowIntensity);
        master.transform.SetParent(explorationRoot.transform, false);
        master.positionCount = nRes;
        Vector3[] mpts = new Vector3[nRes];
        int used = 0;
        for (int k = 0; k < zeros && k < zs.Length && zs[k] <= tMax; k++) used++;
        for (int i = 0; i < nRes; i++)
        {
            float n = 1f + (nMax - 1f) * i / (nRes - 1f);
            double sum = 0;
            for (int k = 0; k < used; k++) sum -= System.Math.Cos(zs[k] * System.Math.Log(n));
            mpts[i] = Saddle(R * Mathf.Sqrt(n), 0f) + Vector3.back * .3f + Vector3.up * (float)(sum * 2.2 / System.Math.Sqrt(System.Math.Max(1, used)));
        }

        master.SetPositions(mpts);
        for (int n = 2; n <= (int)nMax; n++)
        {
            if (!IsPrime(n)) continue;
            LineRenderer tick = Factory.NewLine("sheet prime", .05f, Factory.Palette.Zero, Factory.SoftGlowIntensity);
            tick.transform.SetParent(explorationRoot.transform, false);
            tick.positionCount = 2;
            tick.SetPositions(new[] { Saddle(R * Mathf.Sqrt(n), 0f) + Vector3.back * .3f + Vector3.down * 1.0f, Saddle(R * Mathf.Sqrt(n), 0f) + Vector3.back * .3f + Vector3.down * 3.2f });
        }

        // left edge: |ζ(½ + it)| up the axis = the columns summed over n with weight 1/√n
        LineRenderer mag = Factory.NewLine("sheet |zeta|", .08f, Factory.Palette.Zeta, Factory.GlowIntensity);
        mag.transform.SetParent(explorationRoot.transform, false);
        mag.positionCount = tRes;
        Vector3[] zpts = new Vector3[tRes];
        for (int j = 0; j < tRes; j++)
        {
            float t = tMax * j / (tRes - 1f);
            double m = Meta.Numerics.ComplexMath.Abs(ZetaMath.Exact(new Meta.Numerics.Complex(.5, t)));
            zpts[j] = Saddle(R, 0f) + Vector3.back * (t * ZetaT + .3f) + Vector3.left * (float)(1.0 + m * 2.0);
        }

        mag.SetPositions(zpts);
        TextBox l = TextBoxLabel("the fringe sheet cos(t · log n)   —   rows: the waves (yellow rows = zeros, their sum is the cyan master wave below)\ncolumns: the chain arrows; summed over n with weight 1/√n they give ζ(½ + it)  (|ζ| in white on the left, touching zero at each yellow row)", 1.9f, Color.white);
        l.transform.SetParent(explorationRoot.transform, false);
        l.transform.position = origin + new Vector3(R * Mathf.Sqrt(nMax) * .5f, -5.5f, 0);
    }

    public void HideWaveStack()
    {
        if (waveStackRoot != null) waveStackRoot.SetActive(false);
    }

    void SetMasterWave(int k)
    {
        double[] zs = Main.Instance.zetaZeroValues;
        float scale = WaveAmp / Mathf.Sqrt(Mathf.Max(1, k));
        Vector3[] pts = new Vector3[WavePts];
        for (int i = 0; i < WavePts; i++)
        {
            float n = WaveN(i);
            double logN = System.Math.Log(n);
            double sum = 0;
            for (int z = 0; z < k; z++) sum -= System.Math.Cos(zs[z] * logN);
            pts[i] = OnSpiral(n, 0f, (float)sum * scale);
        }

        masterWave.SetPositions(pts);
    }

    /// <summary>
    /// Dial the zeros in one at a time. Zero k's wave is born at its own height on the trail (γ·ZetaT up the axis),
    /// runs out along the spiral at that altitude as cos(γ·log n), then sinks onto the saddle and merges into the
    /// master wave: the spiral road displaced by the stacked sum, which spikes at every prime.
    /// </summary>
    public void StartWaveStack(float seconds, int count)
    {
        ShowHypothesis(true);
        EnsureWaveStack();
        waveStackRoot.SetActive(true);
        HideInterference();
        if (waveRoutine != null) StopCoroutine(waveRoutine);
        waveRoutine = StartCoroutine(WaveStackRoutine(seconds, count));
    }

    IEnumerator WaveStackRoutine(float seconds, int count)
    {
        double[] zs = Main.Instance.zetaZeroValues;
        count = Mathf.Min(count, zs.Length);
        SetMasterWave(0);
        float per = seconds / count;
        for (int k = 0; k < count; k++)
        {
            float g = (float)zs[k];
            float altitude = g * ZetaT;
            Color c = Annotations.CollisionColor(k);
            if (k < zeroMarkers.Count) StartCoroutine(PulseScale(zeroMarkers[k].transform, .3f, 3f, per * .5f));
            LineRenderer wave = Factory.NewLine("wave " + (k + 1), .08f, c, Factory.GlowIntensity);
            wave.transform.SetParent(waveStackRoot.transform, false);
            wave.numCornerVertices = 3;
            Vector3[] pts = new Vector3[WavePts + 1];
            float amp = WaveAmp / Mathf.Pow(k + 1, 0.85f);
            caption.gameObject.SetActive(true);
            caption.Text = $"collision {k + 1}:  γ = {g:0.00}  →  cos(γ · log n) follows the spiral at height γ, then sinks onto the saddle";

            // phase 1: grow out from the zero on the axis, along the spiral, at the zero's height
            float tOut = per * .4f, tDown = per * .4f, tHold = per * .2f;
            float t = 0f;
            pts[0] = origin + Vector3.back * (altitude + .15f);
            while (t < tOut)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / tOut);
                int shown = Mathf.Max(2, Mathf.CeilToInt(u * WavePts) + 1);
                for (int i = 1; i < shown; i++)
                {
                    float n = WaveN(i - 1);
                    pts[i] = OnSpiral(n, altitude, -amp * Mathf.Cos(g * Mathf.Log(n)));
                }

                wave.positionCount = shown;
                wave.SetPositions(pts[..shown]);
                yield return null;
            }

            // phase 2: sink to the saddle, fading into the master wave
            t = 0f;
            while (t < tDown)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / tDown);
                float alt = Mathf.Lerp(altitude, 0f, u);
                pts[0] = origin + Vector3.back * (alt + .15f);
                for (int i = 1; i <= WavePts; i++)
                {
                    float n = WaveN(i - 1);
                    pts[i] = OnSpiral(n, alt, -amp * Mathf.Cos(g * Mathf.Log(n)));
                }

                wave.positionCount = pts.Length;
                wave.SetPositions(pts);
                Color faded = new(c.r, c.g, c.b, 1f - u * .8f);
                wave.startColor = faded;
                wave.endColor = faded;
                yield return null;
            }

            Destroy(wave.gameObject);
            SetMasterWave(k + 1);
            yield return new WaitForSeconds(tHold);
        }

        caption.Text = $"{count} collision waves stacked on the spiral: the master wave spikes at every prime";
    }

    void SetInterference(int k)
    {
        double[] zs = Main.Instance.zetaZeroValues;
        float scale = FieldAmp / Mathf.Sqrt(Mathf.Max(1, k));
        for (int i = 0; i < fieldRings.Count; i++)
        {
            float n = fieldN[i];
            double sum = 0;
            double logN = System.Math.Log(n);
            for (int z = 0; z < k; z++) sum -= System.Math.Cos(zs[z] * logN);
            float h = (float)sum * scale;
            float r = R * Mathf.Sqrt(n);
            for (int j = 0; j < FieldSegs; j++)
            {
                float a = j / (float)FieldSegs * Mathf.PI * 2f;
                float x = r * Mathf.Cos(a), y = r * Mathf.Sin(a);
                ringBuffer[j] = origin + new Vector3(x, y, SaddleK * (x * x - y * y)) + Vector3.back * (h + .15f);
            }

            fieldRings[i].SetPositions(ringBuffer);
            float glow = Mathf.Clamp01(Mathf.Abs(h) / FieldAmp);
            Color c = Color.Lerp(new Color(.15f, .35f, .4f), Factory.Palette.Cosine, glow);
            fieldRings[i].startColor = c;
            fieldRings[i].endColor = c;
        }
    }

    /// <summary>
    /// A visual hypothesis for the ending: the two chain rules drawn on the saddle.
    /// The square-root rule IS the spiral (radius √n ↔ arrow length 1/√n). For a dial value t, the numbers whose
    /// arrows point the same way are n = e^(2πk/t); their spiral positions trace a curve. Two curves are drawn,
    /// for the first two zeros (t = 14.13 and 21.02). Whether these curves mean anything is an open question.
    /// </summary>
    GameObject ringsRoot;

    /// <summary>Show only the zeta trail (and its zeros on the axis); rings, road, jump-off columns and squares hidden.</summary>
    public void ShowZetaOnly()
    {
        ShowHypothesis(true);
        foreach (Transform c in hypothesisRoot.transform)
        {
            c.gameObject.SetActive(c.name == "ZetaTrail" || c.name == "Interference" || c.name == "WaveStack");
        }
    }

    public void ShowHypothesis(bool on)
    {
        if (!on) { HideInterference(); if (waveRoutine != null) StopCoroutine(waveRoutine); if (hypothesisRoot != null) hypothesisRoot.SetActive(false); return; }
        if (hypothesisRoot == null)
        {
            hypothesisRoot = new GameObject("Hypothesis");
            hypothesisRoot.transform.SetParent(transform, false);
            // the square-root rule: the spiral road itself, from 1 outward
            ringsRoot = new GameObject("Rings");
            ringsRoot.transform.SetParent(hypothesisRoot.transform, false);
            // only the inner turns, thin and dim: a guide for the eye, not a wall of yellow
            LineRenderer road = Factory.NewLine("sqrt road", .04f, new Color(1f, .85f, .3f, .55f), Factory.NoGlow);
            road.transform.SetParent(ringsRoot.transform, false);
            const int roadPts = 900, roadMax = 400;
            road.positionCount = roadPts;
            Vector3[] pts = new Vector3[roadPts];
            for (int i = 0; i < roadPts; i++) pts[i] = SpiralPos(1f + (roadMax - 1f) * i / (roadPts - 1f), 1f) + Vector3.back * .2f;
            road.SetPositions(pts);
            TextBox roadLabel = TextBoxLabel("the square-root rule: radius √n  (the chain's arrow 1/√n, inverted)", 2.4f, new Color(1f, .85f, .3f));
            roadLabel.transform.SetParent(ringsRoot.transform, false);
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
                    ring.transform.SetParent(ringsRoot.transform, false);
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
                    dot.transform.SetParent(ringsRoot.transform, false);
                    dot.transform.position = SpiralPos(n, 1f) + Vector3.back * .3f;
                }

                TextBox l = TextBoxLabel($"the logarithm rule at t = {tz:0.00}:  one full turn of the chain per ring, radii × e^(π/t) = {ratio:0.000}", 2.2f, cs[z]);
                l.transform.SetParent(ringsRoot.transform, false);
                l.transform.position = origin + new Vector3(0, -Extent - 7.5f - z * 3f, 0);
            }
        }

        if (hypothesisRoot.transform.Find("JumpOff") == null) BuildHypothesisExtras();
        hypothesisRoot.SetActive(true);
    }

    /// <summary>
    /// The sieve's jumping-off points (each prime p as a glowing column, rows of dots stacked above it),
    /// the square points where the spiral completes a turn (the √n rule's own landmarks),
    /// and the zeta trail itself threaded through the saddle: ζ(½ + it) drawn with t along the saddle normal,
    /// so it passes above and below the surface and crosses the axis at every zero.
    /// </summary>
    void BuildHypothesisExtras()
    {
        GameObject jump = new("JumpOff");
        jump.transform.SetParent(hypothesisRoot.transform, false);
        foreach (int p in new[] { 2, 3, 5, 7, 11, 13 })
        {
            Color c = PrimeColor(p);
            Vector3 b = SpiralPos(p, 1f);
            LineRenderer col = Factory.NewLine("jump " + p, .08f, c, Factory.GlowIntensity);
            col.transform.SetParent(jump.transform, false);
            col.positionCount = 2;
            col.SetPositions(new[] { b, b + Vector3.back * 3.2f });
            for (int k = 1; k <= 3; k++)
            {
                GameObject d = Factory.NewSphere("jump dot", .22f, c, Factory.SoftGlowIntensity);
                d.transform.SetParent(jump.transform, false);
                d.transform.position = b + Vector3.back * (k * 1.0f);
            }

            TextBox l = TextBoxLabel($"{p} ×", 2.0f, c);
            l.transform.SetParent(jump.transform, false);
            l.transform.position = b + Vector3.back * 3.9f;
        }

        GameObject squares = new("SquarePoints");
        squares.transform.SetParent(hypothesisRoot.transform, false);
        for (int k = 1; k * k <= Count; k++)
        {
            GameObject d = Factory.NewSphere("square point", .34f, new Color(1f, .55f, .2f), Factory.GlowIntensity);
            d.transform.SetParent(squares.transform, false);
            d.transform.position = SpiralPos(k * k, 1f) + Vector3.back * .25f;
        }

        TextBox sl = TextBoxLabel("orange: the squares, where the √n spiral completes each turn", 2.0f, new Color(1f, .55f, .2f));
        sl.transform.SetParent(squares.transform, false);
        sl.transform.position = origin + new Vector3(0, -Extent - 13.5f, 0);

        // the zeta trail, rising from the origin along the saddle normal (t upward only)
        GameObject zeta = new("ZetaTrail");
        zeta.transform.SetParent(hypothesisRoot.transform, false);
        List<Vector3> up = new();
        for (float tt = 0f; tt <= ZetaTMax; tt += 0.1f)
        {
            Meta.Numerics.Complex zv = ZetaMath.Exact(new Meta.Numerics.Complex(.5, tt));
            up.Add(origin + new Vector3((float)zv.Re * ZetaXY, (float)zv.Im * ZetaXY, 0) + Vector3.back * (tt * ZetaT));
        }

        LineRenderer line = Factory.NewLine("zeta up", .06f, Factory.Palette.Zeta, Factory.SoftGlowIntensity);
        line.transform.SetParent(zeta.transform, false);
        line.positionCount = up.Count;
        line.SetPositions(up.ToArray());

        LineRenderer axis = Factory.NewLine("zeta axis", .03f, Factory.Palette.ImAxis, Factory.SoftGlowIntensity);
        axis.transform.SetParent(zeta.transform, false);
        axis.positionCount = 2;
        axis.SetPositions(new[] { origin, origin + Vector3.back * (ZetaTMax * ZetaT) });
        zeroDropsRoot = new GameObject("ZeroDrops");
        zeroDropsRoot.transform.SetParent(hypothesisRoot.transform, false);
        foreach (double g in Zeros)
        {
            GameObject z = Factory.NewSphere("zero", .3f, Factory.Palette.Zero, Factory.GlowIntensity);
            z.transform.SetParent(zeta.transform, false);
            z.transform.position = origin + Vector3.back * ((float)g * ZetaT);
            zeroMarkers.Add(z);
        }

        TextBox zl = TextBoxLabel("the zeta trail ζ(½ + it) rising from the origin, t along the saddle's normal: a zero wherever it returns to the axis", 2.0f, Factory.Palette.Zeta);
        zl.transform.SetParent(zeta.transform, false);
        zl.transform.position = origin + new Vector3(0, -Extent - 16.5f, 0);
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
        bend = 1f; // the final chapter always lives on the saddle
        caption.gameObject.SetActive(false);
        Layout();
    }

    public IEnumerable<Renderer> Renderers()
    {
        foreach (GameObject d in dots) if (d.activeSelf) yield return d.GetComponent<Renderer>();
        foreach (LineRenderer r in routes) yield return r;
    }
}
