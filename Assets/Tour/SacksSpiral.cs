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
            Color c = prime ? Factory.Palette.Zero : square ? SquareColor : new Color(.3f, .3f, .38f);
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
        AddRoute("squares", SquareColor, k => k * k, 1f, 36f, "n = k²   the squares: never prime");
        AddRoute("pronic", PronicColor, k => k * (k + 1), 1f, 35f, "n = k(k + 1)   neighbours multiplied: never prime");
        AddRoute("euler", EulerColor, k => k * k + k + 41, 13f, 35f, "n = k² + k + 41   Euler: 40 primes in a row");
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
                t.Color = square ? SquareColor : new Color(.6f, .6f, .7f);
                t.gameObject.AddComponent<Billboard>();
                TextBox sub = TextBox.Create($"{p}·{m}", 1.4f);
                sub.transform.SetParent(cell.transform, false);
                sub.Alignment = TextAlignmentOptions.Center;
                sub.transform.localPosition = Vector3.down * 1.05f;
                sub.Color = new Color(.45f, .45f, .55f);
                sub.gameObject.AddComponent<Billboard>();
                LineRenderer link = Factory.NewLine("link", .035f, square ? SquareColor : new Color(.5f, .5f, .6f), Factory.SoftGlowIntensity);
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
            labels[n - 1].Color = square ? SquareColor : new Color(.28f, .28f, .34f);
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

    static readonly Color[] SieveColors = { new(.35f, 1f, .55f), new(.55f, .6f, 1f), new(1f, .55f, .2f) };
    static Color PrimeColor(int p) => p == 2 ? SieveColors[0] : p == 3 ? SieveColors[1] : p == 5 ? SieveColors[2] : new Color(1f, .4f, .9f);

    /// <summary>Perfect squares: red everywhere on the spiral and the saddle.</summary>
    public static readonly Color SquareColor = new(1f, .28f, .22f);
    static readonly Color CompositeColor = new(.6f, .6f, .7f);
    static readonly Color CompositeLabel = new(.55f, .55f, .62f);
    static readonly Color UndecidedLabel = new(.4f, .4f, .46f);
    /// <summary>The dots of the number being copied (the farther one, m).</summary>
    static readonly Color RowColor = new(.3f, .85f, 1f);
    static Color MultiplierColor => Factory.Palette.Zero;
    static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

    // ---- the sieve on the spiral -------------------------------------------------------------------
    // Every composite n is made as p × m: p is its smallest prime (the multiplier, nearer the centre) and m = n / p is
    // the number that gets copied (farther out). A row of m dots is counted along the road from 1 to m, the prime p
    // copies that row up one level at a time until there are p rows, the copies slide out along the road at their
    // height, and only the final dot drops: it lands on p × m. Whatever the landing jumped over was never made by any
    // multiplication: an open circle marks the spot, then a new prime is drawn there and joins the multipliers.
    // Composites are made in increasing order, exactly like a sieve walking up the numbers.

    LineRenderer spiralPath;
    GameObject sieveRoot;
    readonly List<GameObject> sieveMarks = new();
    readonly List<GameObject> workDots = new();
    readonly HashSet<int> struck = new();
    readonly List<int> sievePrimes = new();
    readonly Queue<(int upTo, float seconds)> sieveQueue = new();
    int sieveFrontier = 2;
    Coroutine sieveWorker;

    public bool SieveBusy => sieveWorker != null && (sieveQueue.Count > 0 || sieveStepRunning);
    bool sieveStepRunning;

    /// <summary>The whole sieve in one go (VR view, older scripts): break the grid open, then make every composite up to 30.</summary>
    public void StartSieve(float seconds)
    {
        BeginSieve(Mathf.Clamp(seconds * .15f, 2.5f, 6f));
        QueueSieve(GridCount, seconds * .85f);
    }

    /// <summary>Break the grid open onto the spiral; all primes glow, then only 2 stays. Multiplications queue after it.</summary>
    public void BeginSieve(float seconds)
    {
        CancelSieve();
        gameObject.SetActive(true);
        sieveWorker = StartCoroutine(SieveWorker(seconds));
    }

    /// <summary>Make every composite up to <paramref name="upTo"/> across <paramref name="seconds"/> (runs after whatever is already queued).</summary>
    public void QueueSieve(int upTo, float seconds)
    {
        if (sieveWorker == null) BeginSieve(2.5f);
        sieveQueue.Enqueue((upTo, seconds));
    }

    void CancelSieve()
    {
        if (sieveWorker != null) StopCoroutine(sieveWorker);
        sieveWorker = null;
        sieveStepRunning = false;
        sieveQueue.Clear();
        ClearWorkDots();
        Board(null);
    }

    static void Board(string text)
    {
        CaptionDisplay captions = TourDirector.Instance != null ? TourDirector.Instance.Captions : null;
        if (captions == null) return;
        if (string.IsNullOrEmpty(text)) captions.HideBoard();
        else captions.ShowBoard(text);
    }

    string Tally() => "<size=60%><color=#9A9AA8>primes so far:</color>  <color=#" + Hex(MultiplierColor) + ">" + string.Join("  ", sievePrimes) + "</color></size>";

    static string BoardText(string head, string detail, string tally) =>
        head + (string.IsNullOrEmpty(detail) ? "" : "\n<size=58%>" + detail + "</size>") + (string.IsNullOrEmpty(tally) ? "" : "\n" + tally);

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

    void EnsureSieveRoot()
    {
        if (sieveRoot != null) return;
        sieveRoot = new GameObject("SieveMarks");
        sieveRoot.transform.SetParent(transform, false);
    }

    void ClearSieveMarks()
    {
        foreach (GameObject g in sieveMarks) if (g != null) Destroy(g);
        sieveMarks.Clear();
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
        foreach (GameObject d in workDots) if (d != null) Destroy(d);
        workDots.Clear();
    }

    /// <summary>Fade a number's dot in or out (its label follows).</summary>
    IEnumerator FadeNumber(int n, float from, float to, float seconds)
    {
        Renderer r = dots[n - 1].GetComponent<Renderer>();
        dots[n - 1].SetActive(true);
        Color baseColor = IsPrime(n) ? Factory.Palette.Zero : IsSquare(n) ? SquareColor : CompositeColor;
        Color labelColor = IsPrime(n) ? Factory.Palette.Zero : UndecidedLabel;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds));
            Factory.SetColor(r, new Color(baseColor.r, baseColor.g, baseColor.b, a));
            labels[n - 1].Color = Color.Lerp(UndecidedLabel, labelColor, a);
            yield return null;
        }

        Factory.SetColor(r, new Color(baseColor.r, baseColor.g, baseColor.b, to));
        labels[n - 1].Color = Color.Lerp(UndecidedLabel, labelColor, to);
        if (to <= 0f) dots[n - 1].SetActive(false);
    }

    /// <summary>Altitude: rows of dots stack up off the spiral (−Z, the spiral's 'up'), one unit per row.</summary>
    static Vector3 Alt(float row) => Vector3.back * (0.45f + row * 1.3f);

    Vector3 Road(float n) => SpiralPos(n, 0f);

    /// <summary>A spot just outside number n on the flat spiral (radially outward), for its small tags.</summary>
    Vector3 Outside(int n, float distance)
    {
        Vector3 p = Road(n);
        Vector3 radial = p - origin;
        radial.z = 0f;
        return p + radial.normalized * distance + Vector3.back * .3f;
    }

    static int SmallestPrimeFactor(int n)
    {
        for (int d = 2; d * d <= n; d++) if (n % d == 0) return d;
        return n;
    }

    /// <summary>A circle lying on the spiral's plane around a number, drawn in over <paramref name="seconds"/>.</summary>
    IEnumerator DrawRing(LineRenderer ring, Vector3 center, float radius, float seconds)
    {
        const int segs = 48;
        Vector3[] pts = new Vector3[segs + 1];
        for (int i = 0; i <= segs; i++)
        {
            float a = i / (float)segs * Mathf.PI * 2f + Mathf.PI * .5f;
            pts[i] = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius + Vector3.back * .06f;
        }

        float t = 0f;
        while (t < seconds && ring != null)
        {
            t += Time.deltaTime;
            int shown = Mathf.Clamp(Mathf.CeilToInt(Mathf.SmoothStep(0f, 1f, t / seconds) * segs) + 1, 2, segs + 1);
            ring.positionCount = shown;
            for (int i = 0; i < shown; i++) ring.SetPosition(i, pts[i]);
            yield return null;
        }

        if (ring == null) yield break;
        ring.positionCount = segs + 1;
        ring.SetPositions(pts);
    }

    LineRenderer NewRing(string name, Color c, float width)
    {
        LineRenderer ring = Factory.NewLine(name, width, c, Factory.GlowIntensity);
        ring.transform.SetParent(sieveRoot.transform, false);
        ring.numCornerVertices = 2;
        ring.positionCount = 0;
        sieveMarks.Add(ring.gameObject);
        return ring;
    }

    TextBox Tag(string text, float size, Color color, Vector3 position)
    {
        TextBox tag = TextBoxLabel(text, size, color);
        tag.transform.SetParent(sieveRoot.transform, false);
        tag.transform.position = position;
        sieveMarks.Add(tag.gameObject);
        return tag;
    }

    static IEnumerator FadeRing(LineRenderer ring, float to, float seconds)
    {
        if (ring == null) yield break;
        Color c0 = ring.startColor;
        float t = 0f;
        while (t < seconds && ring != null)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(c0.a, to, t / seconds);
            ring.startColor = ring.endColor = new Color(c0.r, c0.g, c0.b, a);
            yield return null;
        }
    }

    static IEnumerator ScaleLabel(TextBox label, float to, float seconds)
    {
        if (label == null) yield break;
        float from = label.transform.localScale.x, t = 0f;
        while (t < seconds && label != null)
        {
            t += Time.deltaTime;
            label.transform.localScale = Vector3.one * Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds));
            yield return null;
        }
    }

    float LabelScale => Mathf.Lerp(1f, .62f, morph);

    void SetDecided(int n, bool animate)
    {
        bool prime = IsPrime(n), square = IsSquare(n);
        GameObject d = dots[n - 1];
        d.SetActive(true);
        Renderer r = d.GetComponent<Renderer>();
        r.enabled = true;
        Factory.SetColor(r, prime ? Factory.Palette.Zero : square ? SquareColor : CompositeColor);
        Factory.SetIntensity(r, prime || square ? Factory.GlowIntensity : Factory.SoftGlowIntensity);
        labels[n - 1].Color = prime ? Factory.Palette.Zero : square ? SquareColor : CompositeLabel;
        float size = prime ? .42f : square ? .34f : .26f;
        if (animate) StartCoroutine(PulseScale(d.transform, size, prime ? 2.6f : 3.2f, .8f));
        else d.transform.localScale = Vector3.one * size;
    }

    IEnumerator SieveWorker(float introSeconds)
    {
        yield return SieveIntro(introSeconds);
        while (true)
        {
            if (sieveQueue.Count == 0)
            {
                yield return null;
                continue;
            }

            sieveStepRunning = true;
            (int upTo, float seconds) = sieveQueue.Dequeue();
            List<int> todo = new();
            for (int n = sieveFrontier + 1; n <= Mathf.Min(upTo, GridCount); n++)
            {
                if (!IsPrime(n)) todo.Add(n);
            }

            // the first multiplications of a request take a little longer, the rest speed up
            float[] weights = new float[todo.Count];
            float sum = 0f;
            for (int i = 0; i < todo.Count; i++) sum += weights[i] = Mathf.Lerp(1.35f, .8f, todo.Count > 1 ? i / (todo.Count - 1f) : 0f);
            for (int i = 0; i < todo.Count; i++)
            {
                // when more is queued behind this request, hurry so the picture keeps up with the narration
                float hurry = sieveQueue.Count > 0 ? .7f : 1f;
                yield return Multiply(todo[i], Mathf.Max(.9f, seconds * weights[i] / sum * hurry));
            }

            sieveStepRunning = false;
        }
    }

    IEnumerator SieveIntro(float seconds)
    {
        gameObject.SetActive(true);
        HideLattice();
        foreach (TextBox f in factorLabels) f.gameObject.SetActive(false);
        caption.gameObject.SetActive(false);
        EnsureSieveRoot();
        sieveRoot.SetActive(true);
        ClearSieveMarks();
        ClearWorkDots();
        EnsureSpiralPath();
        struck.Clear();
        sievePrimes.Clear();
        sieveFrontier = 2;

        // break the grid open into spiral positions (only the first 30 numbers)
        float flyTime = Mathf.Max(1.2f, seconds * .45f);
        float t = 0f;
        float m0 = morph, b0 = bend;
        while (t < flyTime)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / flyTime);
            morph = Mathf.Lerp(m0, 1f, k);
            bend = Mathf.Lerp(b0, 0f, k);
            Layout();
            yield return null;
        }

        morph = 1f;
        bend = 0f;
        Layout();
        spiralPath.enabled = true;

        // every prime glows for a moment (what we expect to find), then all but the 2 fade: the sieve has to earn them
        for (int n = 1; n <= GridCount; n++)
        {
            if (IsPrime(n)) { dots[n - 1].SetActive(true); Factory.SetColor(dots[n - 1].GetComponent<Renderer>(), Factory.Palette.Zero); }
            else dots[n - 1].SetActive(false);
        }

        float rest = Mathf.Max(1f, seconds - flyTime);
        yield return new WaitForSeconds(rest * .3f);
        for (int k = 3; k <= GridCount; k++) if (IsPrime(k)) StartCoroutine(FadeNumber(k, 1f, 0f, rest * .45f));
        for (int k = 4; k <= GridCount; k++) if (!IsPrime(k)) labels[k - 1].Color = UndecidedLabel;
        labels[0].Color = new Color(.3f, .3f, .36f); // 1 is skipped: neither prime nor a multiplier
        sievePrimes.Add(2);
        SetDecided(2, true);
        Board(BoardText("<color=#" + Hex(MultiplierColor) + ">2</color> is our first prime", "every number after it is either made by multiplying, or skipped", Tally()));
        yield return new WaitForSeconds(rest * .7f);
    }

    /// <summary>One multiplication p × m = n on the spiral, followed by any primes the landing skipped.</summary>
    IEnumerator Multiply(int n, float seconds)
    {
        int p = SmallestPrimeFactor(n), m = n / p;
        bool square = p == m;
        bool detailed = seconds >= 3.2f;
        string hp = Hex(MultiplierColor), hm = Hex(RowColor);
        string pm = $"<color=#{hp}>{p}</color> × <color=#{hm}>{m}</color>";
        int skipped = 0;
        for (int j = sieveFrontier + 1; j < n; j++) if (!struck.Contains(j)) skipped++;
        float tSkip = skipped > 0 ? seconds * (detailed ? .2f : .16f) : 0f;
        float body = seconds - tSkip;
        float tFocus = body * .14f, tCount = body * .2f, tCopy = body * .2f, tSlide = body * .28f, tLand = body * .18f;

        // 1. the two numbers in the multiplication: the prime p (nearer the centre) and the number m it copies
        string copies = p == 2 ? "one level up" : $"{p - 1} levels up";
        Board(BoardText(pm, detailed ? $"count <color=#{hm}>{m}</color> dots, from 1 up to {m};  the prime <color=#{hp}>{p}</color> copies that row {copies}" : null, Tally()));
        LineRenderer ringM = NewRing("ring m", RowColor, .07f);
        LineRenderer ringP = NewRing("ring p", MultiplierColor, .09f);
        StartCoroutine(DrawRing(ringM, Road(m), .78f, tFocus));
        StartCoroutine(DrawRing(ringP, Road(p), square ? 1.02f : .78f, tFocus));
        StartCoroutine(ScaleLabel(labels[m - 1], LabelScale * 1.7f, tFocus));
        StartCoroutine(ScaleLabel(labels[p - 1], LabelScale * 1.7f, tFocus));
        yield return new WaitForSeconds(tFocus);

        // 2. count m dots along the road, 1..m, just above the spiral
        List<List<GameObject>> rows = new();
        List<GameObject> baseRow = new();
        for (int i = 0; i < m; i++)
        {
            GameObject d = MakeDot(RowColor, .4f);
            d.transform.position = Road(i + 1f) + Alt(0);
            d.transform.localScale = Vector3.zero;
            baseRow.Add(d);
        }

        rows.Add(baseRow);
        float t = 0f;
        while (t < tCount)
        {
            t += Time.deltaTime;
            for (int i = 0; i < m; i++)
            {
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - i * tCount * .7f / m) / (tCount * .3f)));
                baseRow[i].transform.localScale = Vector3.one * (.4f * u);
            }

            yield return null;
        }

        // 3. the prime p copies the row up one level at a time: a beam rises from p through the stack
        LineRenderer beam = Factory.NewLine("multiplier beam", .07f, MultiplierColor, Factory.GlowIntensity);
        beam.transform.SetParent(sieveRoot.transform, false);
        beam.positionCount = 2;
        sieveMarks.Add(beam.gameObject);
        float perCopy = tCopy / Mathf.Max(1, p - 1);
        for (int k = 1; k < p; k++)
        {
            if (detailed) Board(BoardText(pm, $"<color=#{hp}>{p}</color> rows of <color=#{hm}>{m}</color>:  row {k + 1} of {p}", Tally()));
            List<GameObject> row = new();
            for (int i = 0; i < m; i++)
            {
                GameObject d = MakeDot(RowColor, .38f);
                d.transform.position = Road(i + 1f) + Alt(k - 1);
                row.Add(d);
            }

            rows.Add(row);
            StartCoroutine(PulseScale(dots[p - 1].transform, .42f, 2.2f, perCopy));
            t = 0f;
            while (t < perCopy)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / perCopy);
                for (int i = 0; i < m; i++) row[i].transform.position = Road(i + 1f) + Vector3.Lerp(Alt(k - 1), Alt(k), u);
                beam.SetPosition(0, Road(p) + Vector3.back * .05f);
                beam.SetPosition(1, Road(p) + Vector3.Lerp(Alt(k - 1), Alt(k), u));
                yield return null;
            }
        }

        // 4. the copies slide up the spiral at their own height, each to the next free stretch: k·m+1 .. k·m+m
        if (detailed) Board(BoardText(pm, "the copies slide up the spiral and drop:  only the final dot lands", Tally()));
        StartCoroutine(FadeRing(ringM, .15f, tSlide * .5f));
        t = 0f;
        while (t < tSlide)
        {
            t += Time.deltaTime;
            float u = Mathf.SmoothStep(0f, 1f, t / tSlide);
            for (int k = 1; k < rows.Count; k++)
            {
                for (int i = 0; i < m; i++) rows[k][i].transform.position = Road(Mathf.Lerp(i + 1f, k * m + i + 1f, u)) + Alt(k);
            }

            beam.SetPosition(1, Road(p) + Alt(p - 1) * (1f - u));
            yield return null;
        }

        Destroy(beam.gameObject);

        // 5. only the final dot drops: it lands on p × m; every other dot fades where it is
        GameObject final = rows[^1][^1];
        Vector3 from = final.transform.position, to = Road(n) + Vector3.back * .05f;
        t = 0f;
        while (t < tLand)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / tLand);
            float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u * 1.6f));
            foreach (List<GameObject> row in rows)
            foreach (GameObject d in row)
            {
                if (d != final) d.transform.localScale = Vector3.one * (.4f * fade);
            }

            final.transform.position = Vector3.Lerp(from, to, u * u);
            yield return null;
        }

        ClearWorkDots();
        struck.Add(n);
        SetDecided(n, true);
        StartCoroutine(ScaleLabel(labels[m - 1], LabelScale, .4f));
        StartCoroutine(ScaleLabel(labels[p - 1], LabelScale, .4f));
        StartCoroutine(FadeRing(ringP, 0f, .6f));
        StartCoroutine(FadeRing(ringM, 0f, .6f));
        Tag($"{p}×{m}", 2.1f, square ? SquareColor : new Color(.75f, .75f, .85f), Outside(n, 1.5f));
        Board(BoardText(pm + $" = {n}" + (square ? $"  <size=60%><color=#{Hex(SquareColor)}>a perfect square</color></size>" : ""), null, Tally()));

        // 6. the spots the landing jumped over: nothing made them. An open circle, then a new prime
        if (skipped > 0)
        {
            float per = tSkip / skipped;
            for (int j = sieveFrontier + 1; j < n; j++)
            {
                if (struck.Contains(j)) continue;
                LineRenderer open = NewRing("candidate " + j, Color.white, .08f);
                yield return DrawRing(open, Road(j), .62f, per * .35f);
                Board(BoardText(pm + $" = {n}", $"we skipped <color=#{hp}>{j}</color>:  no multiplication lands there", Tally()));
                yield return new WaitForSeconds(per * .2f);
                sievePrimes.Add(j);
                SetDecided(j, true);
                open.startColor = open.endColor = MultiplierColor;
                Tag($"{j} is prime", 2.1f, MultiplierColor, Outside(j, 2.0f));
                Board(BoardText(pm + $" = {n}", $"<color=#{hp}>{j}</color> is prime, and joins the multipliers", Tally()));
                yield return new WaitForSeconds(per * .45f);
                StartCoroutine(FadeRing(open, 0f, 1.2f)); // the circle has done its job; the prime's own dot stays
            }
        }

        sieveFrontier = n;
    }

    /// <summary>Colour every number up to 30 as the finished sieve leaves it (used when a later line cuts the sieve short).</summary>
    void CompleteSieveColors()
    {
        for (int n = 2; n <= GridCount; n++) SetDecided(n, false);
        labels[0].Color = new Color(.3f, .3f, .36f);
    }

    IEnumerator FadeSieve(float seconds)
    {
        if (sieveRoot == null) yield break;
        float t = 0f;
        List<LineRenderer> lines = new();
        foreach (GameObject g in sieveMarks) if (g != null && g.TryGetComponent(out LineRenderer l)) lines.Add(l);
        while (t < seconds)
        {
            t += Time.deltaTime;
            float a = 1f - t / seconds;
            foreach (LineRenderer l in lines)
            {
                if (l == null) continue;
                Color c = l.startColor;
                l.startColor = l.endColor = new Color(c.r, c.g, c.b, Mathf.Min(c.a, a));
            }

            yield return null;
        }

        sieveRoot.SetActive(false);
        if (spiralPath != null) spiralPath.enabled = false;
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
            labels[i].transform.position = Pos(i + 1) + Vector3.back * Mathf.Lerp(.4f, .75f, morph);
            labels[i].transform.localScale = Vector3.one * Mathf.Lerp(1f, .62f, morph);
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
        StopGrowth();
        if (hypothesisRoot != null) hypothesisRoot.SetActive(false);
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
        bool sieveStarted = sieveWorker != null || struck.Count > 0;
        CancelSieve();
        if (sieveStarted) CompleteSieveColors();
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
        if (!on) HighlightRoute("all");
    }

    public static readonly Color PronicColor = new(.4f, .55f, 1f);
    public static readonly Color EulerColor = new(.35f, 1f, .55f);
    static readonly string[] RouteNames = { "squares", "pronic", "euler" };
    Coroutine routeHighlight;

    /// <summary>
    /// Make one route stand out and dim the other two ("all" shows them evenly). The numbers on the highlighted route
    /// pulse: the squares (red), the products of neighbours k(k+1) (tinted blue), or Euler's primes k² + k + 41.
    /// </summary>
    public void HighlightRoute(string name)
    {
        if (routeHighlight != null) StopCoroutine(routeHighlight);
        int chosen = System.Array.IndexOf(RouteNames, name);
        for (int r = 0; r < routes.Count; r++)
        {
            bool on = chosen < 0 || r == chosen;
            Color c = routes[r].startColor;
            float w = chosen < 0 ? .14f : on ? .26f : .07f;
            routes[r].startWidth = routes[r].endWidth = w;
            routes[r].startColor = routes[r].endColor = new Color(c.r, c.g, c.b, on ? 1f : .25f);
            Factory.SetIntensity(routes[r], on && chosen >= 0 ? Factory.GlowIntensity * 1.2f : Factory.GlowIntensity);
            routeLabels[r].gameObject.SetActive(on);
        }

        // pronic numbers are composites (grey dots): tint them blue while their route is highlighted, grey otherwise
        for (int k = 1; k * (k + 1) <= Count; k++)
        {
            int n = k * (k + 1);
            Renderer rd = dots[n - 1].GetComponent<Renderer>();
            Factory.SetColor(rd, name == "pronic" ? PronicColor : CompositeColor);
        }

        if (chosen >= 0 && gameObject.activeInHierarchy) routeHighlight = StartCoroutine(PulseRouteNumbers(chosen));
    }

    IEnumerator PulseRouteNumbers(int route)
    {
        (System.Func<float, float> fn, float k0, float k1) = routeFns[route];
        int kStart = route == 2 ? 0 : Mathf.CeilToInt(k0);
        for (int k = kStart; k <= Mathf.FloorToInt(k1); k++)
        {
            int n = Mathf.RoundToInt(fn(k));
            if (n < 1 || n > Count) continue;
            GameObject d = dots[n - 1];
            d.SetActive(true);
            float size = IsPrime(n) ? .42f : n > GridCount ? .22f : IsSquare(n) ? .34f : .26f;
            StartCoroutine(PulseScale(d.transform, size, 2.6f, .9f));
            yield return new WaitForSeconds(.12f);
        }
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

    /// <summary>Unit normal of the current surface (saddle bent by <see cref="bend"/>) at radius r, angle a, pointing 'up' (toward −Z).</summary>
    Vector3 SurfaceNormal(float x, float y)
    {
        // z = bend·k·(x² − y²)  →  gradient (2bkx, −2bky); the up-normal (toward −Z) is (2bkx, −2bky, −1) normalised
        float k = bend * SaddleK;
        return new Vector3(2f * k * x, -2f * k * y, -1f).normalized;
    }

    Vector3 SurfaceNormalAt(float n)
    {
        float r = R * Mathf.Sqrt(n), a = 2f * Mathf.PI * Mathf.Sqrt(n);
        return SurfaceNormal(r * Mathf.Cos(a), r * Mathf.Sin(a));
    }

    /// <summary>The spiral road at a given altitude above the surface, displaced along the LOCAL surface normal by a wave.</summary>
    Vector3 OnSpiral(float n, float altitude, float displacement) => SpiralPos(n, bend) + SurfaceNormalAt(n) * (altitude + displacement + .15f);

    /// <summary>Saddle (default) or flat plane for the final view; the wave, ticks and trail follow the surface.</summary>
    public bool Flat { get; private set; }
    Coroutine flattenRoutine;

    public void SetFlat(bool flat, float seconds = 1.5f)
    {
        Flat = flat;
        if (flattenRoutine != null) StopCoroutine(flattenRoutine);
        flattenRoutine = StartCoroutine(FlattenRoutine(flat ? 0f : 1f, seconds));
    }

    IEnumerator FlattenRoutine(float to, float seconds)
    {
        float from = bend;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            bend = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds));
            Layout();
            RefreshWaveGeometry();
            yield return null;
        }

        bend = to;
        Layout();
        RefreshWaveGeometry();
        flattenRoutine = null;
    }

    /// <summary>Re-place everything that sits on the surface after the bend changed.</summary>
    void RefreshWaveGeometry()
    {
        if (waveStackRoot == null) return;
        foreach (Transform c in waveStackRoot.transform)
        {
            if (!c.name.StartsWith("ticks ")) continue;
            float nMax = float.Parse(c.name.Substring(6), System.Globalization.CultureInfo.InvariantCulture);
            c.gameObject.SetActive(false);
            Destroy(c.gameObject);
            c.name = "old";
            EnsurePrimeTicks(nMax);
            break;
        }

        if (fullSums != null && fullPublishedZeros > 0)
        {
            double[] local = new double[fullSamples];
            int published;
            lock (fullLock)
            {
                System.Array.Copy(fullSums, local, fullSamples);
                published = fullPublishedZeros;
            }

            UploadFullWave(local, published);
        }
        else if (masterWave != null && masterWave.enabled && masterWave.positionCount == WavePts)
        {
            SetMasterWave(lastMasterZeros);
        }
    }

    int lastMasterZeros;

    void EnsureWaveStack()
    {
        if (waveStackRoot != null) return;
        waveStackRoot = new GameObject("WaveStack");
        waveStackRoot.transform.SetParent(hypothesisRoot.transform, false);
        masterWave = Factory.NewLine("master wave", .10f, Factory.Palette.Cosine, Factory.GlowIntensity);
        masterWave.transform.SetParent(waveStackRoot.transform, false);
        masterWave.positionCount = WavePts;
        masterWave.numCornerVertices = 3;
        EnsurePrimeTicks(FieldNMax);
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
            LineRenderer spoke = Factory.NewLine("wire spoke", ridge || valley ? .06f : .025f, ridge ? SquareColor : valley ? new Color(.3f, .7f, 1f) : c, ridge || valley ? Factory.SoftGlowIntensity : Factory.NoGlow);
            spoke.transform.SetParent(root.transform, false);
            spoke.positionCount = 40;
            Vector3[] pts = new Vector3[40];
            for (int i = 0; i < 40; i++) { float r = rMax * i / 39f; pts[i] = Saddle(r * Mathf.Cos(a), r * Mathf.Sin(a)) + Vector3.back * .02f; }
            spoke.SetPositions(pts);
        }

        TextBox l1 = TextBoxLabel("ridge (x axis): √n ≈ whole → squares k² and pronics k(k+1), never prime", 2.0f, SquareColor);
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

    // ---- the full master wave: as many zeros as the table holds, computed on a background thread ----
    public bool FullWaveRunning { get; private set; }
    public int FullWaveZerosShown { get; private set; }
    public int FullWaveZerosTarget { get; private set; }
    public float FullWaveAmplitude = 2.4f;
    int fullSamples;
    float fullNMax;
    double[] fullLogN;
    double[] fullSums;          // latest published cumulative sums (sample-wise)
    int fullPublishedZeros;
    readonly object fullLock = new();
    System.Threading.Thread fullThread;
    volatile bool fullCancel;
    Unity.Collections.NativeArray<Vector3> fullPositions;
    TextBox fullLabel;
    Vector3[] fullBase, fullNormal;   // per-sample surface point and normal for the current bend
    // the full wave is drawn as a curtain mesh (base on the surface, top at the wave): no per-frame LineRenderer rebuild
    Mesh curtainMesh;
    MeshRenderer curtainRenderer;
    Vector3[] curtainVerts;
    Color[] curtainColors;
    float fullBaseBend = -1f;
    int fullUploadedZeros = -1;
    float fullUploadedAmplitude = -1f;

    /// <summary>
    /// Dial in up to <paramref name="zeros"/> collisions. The cumulative sums are computed on a background
    /// thread (parallel over samples, batches of zeros); the main thread shows whatever has been published,
    /// with the displayed count rising geometrically over <paramref name="seconds"/>.
    /// </summary>
    public void StartFullMasterWave(int zeros, int samples, float nMax, float seconds, bool revealWhenDone = false)
    {
        ShowHypothesis(true);
        EnsureWaveStack();
        ClearExploration();
        waveStackRoot.SetActive(true);
        EnsurePrimeTicks(nMax);
        HandGrowthToFullWave();

        double[] zs = Main.Instance.zetaZeroValues;
        zeros = Mathf.Clamp(zeros, 1, zs.Length);
        if (Application.platform == RuntimePlatform.Android) samples = Mathf.Min(samples, 60000); // mobile GPU / CPU budget

        // computed before with the same settings, or baked into Resources/MasterWave: show it straight away
        bool computing = fullThread != null && fullThread.IsAlive;
        bool haveIt = fullSums != null && fullPublishedZeros == zeros && fullSamples == samples && Mathf.Approximately(fullNMax, nMax);
        if (!computing && !haveIt) TryLoadBakedFullWave(zeros, samples, nMax);
        if (!computing && fullSums != null && fullPublishedZeros == zeros && fullSamples == samples && Mathf.Approximately(fullNMax, nMax))
        {
            masterWave.enabled = false;
            EnsureCurtain(samples);
            curtainRenderer.enabled = true;
            double[] cached = new double[fullSamples];
            lock (fullLock) System.Array.Copy(fullSums, cached, fullSamples);
            fullBaseBend = -1f;
            UploadFullWave(cached, fullPublishedZeros);
            FullWaveZerosTarget = zeros;
            FullWaveZerosShown = zeros;
            FullWaveRunning = false;
            if (fullLabel != null && !VRMode.Enabled) fullLabel.Text = $"{zeros:N0} collision waves on the spiral — spikes at every prime";
            return;
        }

        if (computing && fullPublishedZeros < zeros && FullWaveZerosTarget == zeros && fullSamples == samples && Mathf.Approximately(fullNMax, nMax))
        {
            // the same wave is still being computed (e.g. the home view started it): keep the work, just watch it again
            fullHideUntilDone = revealWhenDone;
            WatchFullWave(zeros, seconds);
            return;
        }

        StopFullMasterWave();
        fullHideUntilDone = revealWhenDone;
        fullSamples = samples;
        fullBaseBend = -1f;
        fullUploadedZeros = -1;
        fullNMax = nMax;
        fullLogN = new double[samples];
        for (int i = 0; i < samples; i++) fullLogN[i] = FullWaveLogN(i, samples, nMax);
        fullSums = new double[samples];
        fullPublishedZeros = 0;
        FullWaveZerosTarget = zeros;
        FullWaveZerosShown = 0;
        if (fullPositions.IsCreated) fullPositions.Dispose();
        fullPositions = new Unity.Collections.NativeArray<Vector3>(samples, Unity.Collections.Allocator.Persistent);
        masterWave.enabled = false; // the curtain mesh replaces the line in full mode
        EnsureCurtain(samples);
        if (fullLabel == null)
        {
            fullLabel = TextBoxLabel("", 2.4f, Factory.Palette.Cosine);
            fullLabel.transform.SetParent(waveStackRoot.transform, false);
        }

        fullLabel.gameObject.SetActive(true);
        fullLabel.transform.position = origin + new Vector3(0, -R * Mathf.Sqrt(nMax) - 4f, 0);

        fullCancel = false;
        FullWaveRunning = true;
        double[] logN = fullLogN;
        // leave the main thread its cores: at most half the processors, low priority
        var options = new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = System.Math.Max(1, System.Environment.ProcessorCount / 2) };
        fullThread = new System.Threading.Thread(() =>
        {
            double[] acc = new double[samples];
            int done = 0;
            const int batch = 40;
            while (done < zeros && !fullCancel)
            {
                int end = System.Math.Min(zeros, done + batch);
                int from = done;
                System.Threading.Tasks.Parallel.For(0, samples, options, i =>
                {
                    double l = logN[i], s = 0;
                    for (int z = from; z < end; z++) s -= System.Math.Cos(zs[z] * l);
                    acc[i] += s;
                });
                done = end;
                lock (fullLock)
                {
                    System.Array.Copy(acc, fullSums, samples);
                    fullPublishedZeros = done;
                }
            }
        }) { IsBackground = true, Name = "FullMasterWave", Priority = System.Threading.ThreadPriority.BelowNormal };
        fullThread.Start();
        WatchFullWave(zeros, seconds);
    }

    bool fullHideUntilDone;
    Coroutine fullDisplay;

    /// <summary>log n of sample i of the full master wave (log-spaced from 1.5 to nMax); shared with the editor bake.</summary>
    public static double FullWaveLogN(int i, int samples, float nMax) => System.Math.Log(FieldNMin * Mathf.Pow(nMax / FieldNMin, i / (samples - 1f)));

    /// <summary>Resources path of a baked full master wave (float32 sums, one per sample), see Assets/Editor/MasterWaveBake.cs.</summary>
    public static string BakedFullWaveName(int zeros, int samples, float nMax) => $"MasterWave/master_{zeros}_{samples}_{(int)nMax}";

    /// <summary>Load the sums from a baked file instead of computing 10,000 zeros × 160,000 samples (minutes on a laptop).</summary>
    bool TryLoadBakedFullWave(int zeros, int samples, float nMax)
    {
        TextAsset baked = Resources.Load<TextAsset>(BakedFullWaveName(zeros, samples, nMax));
        if (baked == null) return false;
        byte[] bytes = baked.bytes;
        Resources.UnloadAsset(baked);
        if (bytes.Length != samples * 4) return false;
        StopFullMasterWave();
        double[] sums = new double[samples];
        for (int i = 0; i < samples; i++) sums[i] = System.BitConverter.ToSingle(bytes, i * 4);
        fullSamples = samples;
        fullNMax = nMax;
        fullLogN = new double[samples];
        for (int i = 0; i < samples; i++) fullLogN[i] = FullWaveLogN(i, samples, nMax);
        lock (fullLock)
        {
            fullSums = sums;
            fullPublishedZeros = zeros;
        }

        FullWaveZerosTarget = zeros;
        fullBaseBend = -1f;
        fullUploadedZeros = -1;
        return true;
    }

    void WatchFullWave(int zeros, float seconds)
    {
        if (fullDisplay != null) StopCoroutine(fullDisplay);
        fullDisplay = StartCoroutine(FullWaveDisplay(zeros, seconds));
    }

    IEnumerator FullWaveDisplay(int zeros, float seconds)
    {
        float t = 0f, sinceUpload = 10f;
        double[] local = new double[fullSamples];
        int copied = -1;
        if (fullHideUntilDone) curtainRenderer.enabled = false;
        while (!fullCancel)
        {
            t += Time.deltaTime;
            sinceUpload += Time.deltaTime;
            float u = seconds <= 0f ? 1f : Mathf.Clamp01(t / seconds);
            int want = Mathf.Clamp(Mathf.RoundToInt(Mathf.Pow(zeros, u)), 1, zeros);
            int have;
            lock (fullLock) have = fullPublishedZeros;

            // a few uploads a second at most: each one moves 2 × samples vertices (hidden waves wait for the last batch)
            bool ready = !fullHideUntilDone || have >= zeros;
            if (have != copied && ready && (sinceUpload >= .2f || have >= zeros))
            {
                lock (fullLock)
                {
                    have = fullPublishedZeros;
                    System.Array.Copy(fullSums, local, fullSamples);
                }

                copied = have;
            }

            // the count label follows the smaller of the published and the wanted count
            int shown = Mathf.Min(want, Mathf.Max(0, copied));
            if (shown > 0 && shown != FullWaveZerosShown)
            {
                FullWaveZerosShown = shown;
                if (!VRMode.Enabled) fullLabel.Text = $"{copied:N0} collision waves on the spiral — spikes at every prime";
            }

            // upload only for new data, a clear change of amplitude, or a new surface (not every frame)
            bool amplitudeChanged = Mathf.Abs(fullUploadedAmplitude - FullWaveAmplitude) > .04f * Mathf.Max(.1f, FullWaveAmplitude);
            bool changed = copied != fullUploadedZeros || amplitudeChanged || !Mathf.Approximately(fullBaseBend, bend);
            if (copied > 0 && changed && (sinceUpload >= .2f || copied >= zeros))
            {
                UploadFullWave(local, copied);
                sinceUpload = 0f;
                if (fullHideUntilDone && copied >= zeros) curtainRenderer.enabled = true;
            }

            if (copied >= zeros && fullUploadedZeros >= zeros && u >= 1f) break;
            yield return null;
        }

        FullWaveRunning = false;
        fullDisplay = null;
        Debug.Log($"FullMasterWave: {zeros} zeros × {fullSamples} samples ready after {t:0.0} s");
    }

    void EnsureCurtain(int samples)
    {
        if (curtainMesh == null)
        {
            GameObject go = new("master wave curtain");
            go.transform.SetParent(waveStackRoot.transform, false);
            curtainMesh = new Mesh { name = "master wave curtain", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            curtainMesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = curtainMesh;
            curtainRenderer = go.AddComponent<MeshRenderer>();
            curtainRenderer.sharedMaterial = Factory.mainMat;
            curtainRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Factory.SetIntensity(curtainRenderer, Factory.GlowIntensity);
        }

        curtainRenderer.enabled = true;
        if (curtainVerts == null || curtainVerts.Length != samples * 2)
        {
            curtainVerts = new Vector3[samples * 2];
            curtainColors = new Color[samples * 2];
            Color top = Factory.Palette.Cosine, bottom = new(Factory.Palette.Cosine.r, Factory.Palette.Cosine.g, Factory.Palette.Cosine.b, .12f);
            for (int i = 0; i < samples; i++) { curtainColors[2 * i] = bottom; curtainColors[2 * i + 1] = top; }
            int[] tris = new int[(samples - 1) * 6];
            int q = 0;
            for (int i = 0; i < samples - 1; i++)
            {
                int a = 2 * i, b = a + 1, c = a + 2, d = a + 3;
                tris[q++] = a; tris[q++] = c; tris[q++] = b;
                tris[q++] = b; tris[q++] = c; tris[q++] = d;
            }

            curtainMesh.Clear();
            curtainMesh.vertices = curtainVerts;
            curtainMesh.colors = curtainColors;
            curtainMesh.triangles = tris;
        }
    }

    void UploadFullWave(double[] sums, int zeros)
    {
        if (fullBase == null || fullBase.Length != fullSamples || !Mathf.Approximately(fullBaseBend, bend))
        {
            fullBase = new Vector3[fullSamples];
            fullNormal = new Vector3[fullSamples];
            for (int i = 0; i < fullSamples; i++)
            {
                float n = (float)System.Math.Exp(fullLogN[i]);
                fullBase[i] = SpiralPos(n, bend);
                fullNormal[i] = SurfaceNormalAt(n);
            }

            fullBaseBend = bend;
        }

        double max = 1e-9;
        for (int i = 0; i < fullSamples; i++) if (System.Math.Abs(sums[i]) > max) max = System.Math.Abs(sums[i]);
        float scale = (float)(FullWaveAmplitude / max);
        for (int i = 0; i < fullSamples; i++)
        {
            curtainVerts[2 * i] = fullBase[i] + fullNormal[i] * .1f;
            curtainVerts[2 * i + 1] = fullBase[i] + fullNormal[i] * ((float)sums[i] * scale + .15f);
        }

        curtainMesh.vertices = curtainVerts;
        curtainMesh.RecalculateBounds();
        fullUploadedZeros = zeros;
        fullUploadedAmplitude = FullWaveAmplitude;
    }

    public void StopFullMasterWave()
    {
        fullCancel = true;
        if (fullThread != null && fullThread.IsAlive) fullThread.Join(200);
        fullThread = null;
        FullWaveRunning = false;
    }

    /// <summary>All prime and prime-power ticks in ONE mesh (two crossed quads each) instead of hundreds of LineRenderers.</summary>
    void EnsurePrimeTicks(float nMax)
    {
        Transform existing = waveStackRoot.transform.Find("ticks " + (int)nMax);
        if (existing != null) return;
        foreach (Transform c in waveStackRoot.transform) if (c.name.StartsWith("ticks ")) c.gameObject.SetActive(false);
        GameObject root = new("ticks " + (int)nMax);
        root.transform.SetParent(waveStackRoot.transform, false);
        List<Vector3> verts = new();
        List<Color> cols = new();
        List<int> tris = new();
        for (int n = 2; n <= (int)nMax; n++)
        {
            bool prime = IsPrime(n);
            bool primePower = !prime && IsPrimePower(n);
            if (!prime && !primePower) continue;
            Color c = prime ? Factory.Palette.Zero : new Color(1f, .55f, .2f);
            Vector3 a = OnSpiral(n, 0f, -0.3f), b = OnSpiral(n, 0f, prime ? 0.9f : 0.6f);
            Vector3 normal = SurfaceNormalAt(n);
            Vector3 s1 = Vector3.Cross(normal, Vector3.right).normalized * (prime ? .03f : .022f);
            Vector3 s2 = Vector3.Cross(normal, s1).normalized * (prime ? .03f : .022f);
            foreach (Vector3 side in new[] { s1, s2 })
            {
                int v = verts.Count;
                verts.Add(a - side); verts.Add(a + side); verts.Add(b - side); verts.Add(b + side);
                cols.Add(c); cols.Add(c); cols.Add(c); cols.Add(c);
                tris.Add(v); tris.Add(v + 2); tris.Add(v + 1); tris.Add(v + 1); tris.Add(v + 2); tris.Add(v + 3);
            }
        }

        Mesh mesh = new() { name = "prime ticks", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(verts);
        mesh.SetColors(cols);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        root.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = root.AddComponent<MeshRenderer>();
        mr.sharedMaterial = Factory.mainMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Factory.SetIntensity(mr, Factory.SoftGlowIntensity);
    }

    void LateUpdate()
    {
        // in VR the only text is the menus: no caption strip or count label on the model
        if (!VRMode.Enabled) return;
        if (caption != null && caption.gameObject.activeSelf) caption.gameObject.SetActive(false);
        if (fullLabel != null && fullLabel.gameObject.activeSelf) fullLabel.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        DisposeGrowth();
        StopFullMasterWave();
        if (fullPositions.IsCreated) fullPositions.Dispose();
    }

    public void HideWaveStack()
    {
        if (waveStackRoot != null) waveStackRoot.SetActive(false);
    }

    void SetMasterWave(int k)
    {
        lastMasterZeros = k;
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
        ClearExploration();
        waveStackRoot.SetActive(true);
        HideInterference();
        StopFullMasterWave();
        if (fullLabel != null) fullLabel.gameObject.SetActive(false);
        if (curtainRenderer != null) curtainRenderer.enabled = false;
        masterWave.enabled = true;
        masterWave.positionCount = WavePts;
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
            GameObject d = Factory.NewSphere("square point", .34f, SquareColor, Factory.GlowIntensity);
            d.transform.SetParent(squares.transform, false);
            d.transform.position = SpiralPos(k * k, 1f) + Vector3.back * .25f;
        }

        TextBox sl = TextBoxLabel("red: the squares, where the √n spiral completes each turn", 2.0f, SquareColor);
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
            labels[i].Color = IsPrime(n) ? Factory.Palette.Zero : IsSquare(n) ? SquareColor : new Color(.28f, .28f, .34f);
        }

        morph = 1f;
        reveal = 1f;
        bend = 1f; // the final chapter always lives on the saddle
        caption.gameObject.SetActive(false);
        Layout();
    }

    // ---- the zeta trail growing out of the saddle's centre (t = 0 upward), in two modes ----
    //   FullWave  (home view, start and end of the app): the master wave with every zero in the table is already on the
    //             spiral (computed once on a background thread, shown only when complete, then cached); the trail climbs
    //             ever faster toward a practical top, holds, and grows again.
    //   Waves     (the outro chapter): every zero the trail passes sends its wave out along the spiral at the zero's
    //             height; the wave sinks onto the saddle and joins the master wave.
    // Performance: the road's points, normals and log n are computed once per bend; a zero's cosine is computed once when
    // it is born; each frame only moves points along precomputed normals. The trail is split into fixed chunks so only
    // the newest chunk is ever re-uploaded, however tall the trail gets.

    public enum GrowthMode { FullWave, Waves }

    public bool Growing => growRoot != null && growRoot.activeInHierarchy;
    public GrowthMode Mode { get; private set; }
    public float GrowthT { get; private set; }
    public float GrowthTMax { get; private set; } = HomeTMax;
    public float GrowthHeight => GrowthT * ZetaT;
    /// <summary>Seconds the finished picture holds before the growth starts over (looping growth only).</summary>
    public float GrowthHold = 12f;
    /// <summary>Extra factor on the growth speed (the review test uses it to reach the top quickly).</summary>
    public float GrowthSpeedScale = 1f;
    public int GrowthZerosMerged => growMerged;
    /// <summary>Radius of the stretch of spiral to keep in frame: the wave's reach in each mode.</summary>
    public float GrowthFrameRadius => Mode == GrowthMode.FullWave ? R * Mathf.Sqrt(FullNMax) * .8f : R * Mathf.Sqrt(FieldNMax);

    /// <summary>The home view's practical stand-in for infinity: t = 1000 (about 650 zeros on the axis).</summary>
    public const float HomeTMax = 1000f;
    const float FullNMax = 1300f;
    const int FullSamples = 160000;
    const float GrowStep = 0.05f;
    const int TrailChunk = 1024;
    const float GrowWaveOut = 1.7f, GrowWaveHold = .4f, GrowWaveSink = 1.6f;

    GameObject growRoot;
    LineRenderer growMaster, growAxis;
    readonly List<LineRenderer> growTrailChunks = new();
    GameObject growTip;
    Unity.Collections.NativeArray<Vector3> growTrailPts;
    int growTrailCount, growNextZero, growMerged;
    float growHoldTimer, growSeconds;
    bool growLoop;
    readonly List<GameObject> growZeroDots = new();
    Vector3[] growBase, growNormal, growMasterPts;
    float[] growLogN;
    double[] growSum;
    float growBend = -1f;

    sealed class GrowWave
    {
        public LineRenderer Line;
        public Unity.Collections.NativeArray<Vector3> Pts;
        public float[] Cos;     // −cos(γ·log n) per sample, computed once
        public float Amp, Altitude, Age;
        public int Computed;    // samples placed so far while the wave runs out
        public Color Color;
    }

    readonly List<GrowWave> growWaves = new();
    readonly Stack<GrowWave> growPool = new();

    /// <summary>
    /// Start the trail at t = 0. <paramref name="seconds"/> &gt; 0 reaches <paramref name="tMax"/> at a steady pace in that
    /// time; 0 uses the home pace (slow at first, then faster and faster).
    /// </summary>
    public void StartGrowth(GrowthMode mode, float tMax, float seconds, bool loop)
    {
        gameObject.SetActive(true);
        Mode = mode;
        GrowthTMax = Mathf.Clamp(tMax, 1f, (float)Main.Instance.zetaZeroValues[^1]);
        growSeconds = seconds;
        growLoop = loop;
        EnsureGrowth();
        growRoot.SetActive(true);
        SetGrowthTrailVisible(true);
        ResetGrowth();
        growMaster.enabled = mode == GrowthMode.Waves;
        if (mode == GrowthMode.FullWave)
        {
            // the finished master wave, every zero in the table, waiting on the spiral before the trail starts
            StartFullMasterWave(Main.Instance.zetaZeroValues.Length, FullSamples, FullNMax, 0f, true);
            foreach (Transform c in hypothesisRoot.transform) c.gameObject.SetActive(c.name == "WaveStack");
            if (fullLabel != null) fullLabel.gameObject.SetActive(false);
        }
        else if (waveStackRoot != null)
        {
            waveStackRoot.SetActive(false);
        }
    }

    public void StopGrowth()
    {
        if (growRoot == null) return;
        bool wasGrowing = growRoot.activeSelf;
        ResetGrowth();
        growRoot.SetActive(false);
        SetGrowthTrailVisible(true);
        // the master wave the growth showed goes with it (it is kept, computed, for the next time)
        if (wasGrowing && hypothesisRoot != null) hypothesisRoot.SetActive(false);
    }

    /// <summary>Hide or show the growing trail, its axis, tip and zero markers (the master wave is unaffected).</summary>
    public void SetGrowthTrailVisible(bool on)
    {
        if (growRoot == null) return;
        foreach (LineRenderer chunk in growTrailChunks) chunk.enabled = on;
        growAxis.enabled = on;
        growTip.SetActive(on);
        foreach (GameObject d in growZeroDots) if (d != null) d.SetActive(on);
        growTrailHidden = !on;
    }

    bool growTrailHidden;

    /// <summary>The waves stop coming and the growing master line steps aside (the full master wave is taking over).</summary>
    void HandGrowthToFullWave()
    {
        if (growRoot == null || !growRoot.activeSelf) return;
        // only the master wave joins the growing trail: none of the static trail, rings or columns
        foreach (Transform c in hypothesisRoot.transform) c.gameObject.SetActive(c.name == "WaveStack");
        Mode = GrowthMode.FullWave;
        foreach (GrowWave w in growWaves) ReleaseWave(w);
        growWaves.Clear();
        growMaster.enabled = false;
    }

    void EnsureGrowth()
    {
        if (growRoot == null)
        {
            growRoot = new GameObject("Growth");
            growRoot.transform.SetParent(transform, false);
            growTip = Factory.NewSphere("trail tip", .5f, Color.white, Factory.GlowIntensity);
            growTip.transform.SetParent(growRoot.transform, false);
            growAxis = Factory.NewLine("growing axis", .03f, Factory.Palette.ImAxis, Factory.SoftGlowIntensity);
            growAxis.transform.SetParent(growRoot.transform, false);
            growAxis.positionCount = 2;
            growMaster = Factory.NewLine("growing master wave", .11f, Factory.Palette.Cosine, Factory.GlowIntensity);
            growMaster.transform.SetParent(growRoot.transform, false);
            growMaster.numCornerVertices = 2;
            growMaster.positionCount = WavePts;
            growMasterPts = new Vector3[WavePts];
            growSum = new double[WavePts];
            growLogN = new float[WavePts];
            for (int i = 0; i < WavePts; i++) growLogN[i] = Mathf.Log(WaveN(i));
        }

        int needed = Mathf.CeilToInt(GrowthTMax / GrowStep) + 4;
        if (!growTrailPts.IsCreated || growTrailPts.Length < needed)
        {
            if (growTrailPts.IsCreated) growTrailPts.Dispose();
            growTrailPts = new Unity.Collections.NativeArray<Vector3>(needed, Unity.Collections.Allocator.Persistent);
        }
    }

    LineRenderer TrailChunkLine(int index)
    {
        while (growTrailChunks.Count <= index)
        {
            LineRenderer line = Factory.NewLine("growing zeta trail", .08f, Factory.Palette.Zeta, Factory.SoftGlowIntensity);
            line.transform.SetParent(growRoot.transform, false);
            line.numCornerVertices = 2;
            line.positionCount = 0;
            line.enabled = !growTrailHidden;
            growTrailChunks.Add(line);
        }

        return growTrailChunks[index];
    }

    /// <summary>The road's points and normals on the current surface, recomputed only when the bend changes.</summary>
    void EnsureGrowGeometry()
    {
        if (growBase != null && Mathf.Approximately(growBend, bend)) return;
        growBase ??= new Vector3[WavePts];
        growNormal ??= new Vector3[WavePts];
        for (int i = 0; i < WavePts; i++)
        {
            float n = WaveN(i);
            growBase[i] = SpiralPos(n, bend);
            growNormal[i] = SurfaceNormalAt(n);
        }

        growBend = bend;
        UpdateGrowMaster();
    }

    void ResetGrowth()
    {
        GrowthT = 0f;
        growTrailCount = 0;
        growNextZero = 0;
        growMerged = 0;
        growHoldTimer = 0f;
        if (growSum != null) System.Array.Clear(growSum, 0, growSum.Length);
        foreach (GameObject d in growZeroDots) if (d != null) Destroy(d);
        growZeroDots.Clear();
        foreach (GrowWave w in growWaves) ReleaseWave(w);
        growWaves.Clear();
        if (growRoot == null) return;
        foreach (LineRenderer chunk in growTrailChunks) chunk.positionCount = 0;
        growTip.transform.position = TrailPoint(0f);
        growAxis.SetPosition(0, origin);
        growAxis.SetPosition(1, origin);
        growBase = null; // re-place the road for whatever bend is current
        EnsureGrowGeometry();
    }

    Vector3 TrailPoint(float t)
    {
        Meta.Numerics.Complex z = ZetaMath.Exact(new Meta.Numerics.Complex(.5, t));
        return origin + new Vector3((float)z.Re * ZetaXY, (float)z.Im * ZetaXY, 0f) + Vector3.back * (t * ZetaT);
    }

    void UpdateGrowMaster()
    {
        if (growMaster == null || growBase == null) return;
        float scale = WaveAmp / Mathf.Sqrt(Mathf.Max(1, growMerged));
        for (int i = 0; i < WavePts; i++) growMasterPts[i] = growBase[i] + growNormal[i] * ((float)growSum[i] * scale + .15f);
        growMaster.SetPositions(growMasterPts);
        // the empty road is only a faint guide; the master wave gains strength as the zeros pour in
        float strength = Mathf.Clamp01(growMerged / 8f);
        Color c = Factory.Palette.Cosine;
        growMaster.startColor = growMaster.endColor = new Color(c.r, c.g, c.b, Mathf.Lerp(.22f, 1f, strength));
        growMaster.startWidth = growMaster.endWidth = Mathf.Lerp(.05f, .1f, strength);
    }

    GrowWave TakeWave()
    {
        if (growPool.Count > 0)
        {
            GrowWave w = growPool.Pop();
            w.Line.gameObject.SetActive(true);
            return w;
        }

        LineRenderer line = Factory.NewLine("incoming wave", .08f, Color.white, Factory.GlowIntensity);
        line.transform.SetParent(growRoot.transform, false);
        line.numCornerVertices = 2;
        return new GrowWave
        {
            Line = line,
            Pts = new Unity.Collections.NativeArray<Vector3>(WavePts + 1, Unity.Collections.Allocator.Persistent),
            Cos = new float[WavePts]
        };
    }

    void ReleaseWave(GrowWave w)
    {
        w.Line.positionCount = 0;
        w.Line.gameObject.SetActive(false);
        growPool.Push(w);
    }

    void SpawnGrowZero(int k, float gamma)
    {
        Vector3 onAxis = origin + Vector3.back * (gamma * ZetaT);
        float size = k < 100 ? .38f : .3f;
        GameObject dot = Factory.NewSphere("zero " + (k + 1), size, Factory.Palette.Zero, Factory.GlowIntensity);
        dot.transform.SetParent(growRoot.transform, false);
        dot.transform.position = onAxis;
        growZeroDots.Add(dot);
        if (growTrailHidden) dot.SetActive(false);
        else StartCoroutine(PulseScale(dot.transform, size, 3f, .9f));
        if (Mode != GrowthMode.Waves) return;

        GrowWave w = TakeWave();
        w.Altitude = gamma * ZetaT;
        w.Amp = WaveAmp * 1.1f / Mathf.Pow(k + 1, .6f);
        w.Age = 0f;
        w.Computed = 0;
        w.Color = Annotations.CollisionColor(k);
        w.Line.startColor = w.Line.endColor = w.Color;
        for (int i = 0; i < WavePts; i++) w.Cos[i] = -Mathf.Cos(gamma * growLogN[i]);
        w.Pts[0] = onAxis + Vector3.back * .15f;
        w.Line.positionCount = 1;
        growWaves.Add(w);
    }

    float GrowthSpeed => (growSeconds > 0f ? GrowthTMax / growSeconds : 1.6f + 0.012f * GrowthT) * GrowthSpeedScale;

    void StepGrowth(float dt)
    {
        EnsureGrowGeometry();
        double[] zs = Main.Instance != null ? Main.Instance.zetaZeroValues : System.Array.Empty<double>();
        if (GrowthT < GrowthTMax)
        {
            GrowthT = Mathf.Min(GrowthTMax, GrowthT + GrowthSpeed * dt);
            int before = growTrailCount;
            while (growTrailCount < growTrailPts.Length && growTrailCount * GrowStep <= GrowthT)
            {
                growTrailPts[growTrailCount] = TrailPoint(growTrailCount * GrowStep);
                growTrailCount++;
            }

            if (growTrailCount != before)
            {
                // chunks share their end points so the trail has no gaps; only chunks that gained points are uploaded
                for (int c = Mathf.Max(0, (before - 1) / TrailChunk); c * TrailChunk < growTrailCount; c++)
                {
                    int start = c * TrailChunk;
                    int count = Mathf.Min(TrailChunk + 1, growTrailCount - start);
                    if (count < 2) continue;
                    LineRenderer chunk = TrailChunkLine(c);
                    chunk.positionCount = count;
                    chunk.SetPositions(growTrailPts.GetSubArray(start, count));
                }
            }

            growTip.transform.position = TrailPoint(GrowthT);
            growAxis.SetPosition(1, origin + Vector3.back * GrowthHeight);
            while (growNextZero < zs.Length && zs[growNextZero] <= GrowthT)
            {
                SpawnGrowZero(growNextZero, (float)zs[growNextZero]);
                growNextZero++;
            }
        }
        else if (growLoop && growWaves.Count == 0)
        {
            growHoldTimer += dt;
            if (growHoldTimer > GrowthHold) ResetGrowth();
        }

        for (int w = growWaves.Count - 1; w >= 0; w--)
        {
            GrowWave wave = growWaves[w];
            wave.Age += dt;
            if (wave.Age < GrowWaveOut)
            {
                // run out along the spiral at the zero's height: only the newly reached samples are placed
                int want = Mathf.Clamp(Mathf.CeilToInt(Mathf.SmoothStep(0f, 1f, wave.Age / GrowWaveOut) * WavePts), 1, WavePts);
                for (int i = wave.Computed; i < want; i++)
                {
                    wave.Pts[i + 1] = growBase[i] + growNormal[i] * (wave.Altitude + wave.Amp * wave.Cos[i] + .15f);
                }

                if (want != wave.Computed)
                {
                    wave.Computed = want;
                    wave.Line.positionCount = want + 1;
                    wave.Line.SetPositions(wave.Pts.GetSubArray(0, want + 1));
                }
            }
            else if (wave.Age < GrowWaveOut + GrowWaveHold)
            {
                if (wave.Computed < WavePts)
                {
                    for (int i = wave.Computed; i < WavePts; i++) wave.Pts[i + 1] = growBase[i] + growNormal[i] * (wave.Altitude + wave.Amp * wave.Cos[i] + .15f);
                    wave.Computed = WavePts;
                    wave.Line.positionCount = WavePts + 1;
                    wave.Line.SetPositions(wave.Pts);
                }
            }
            else if (wave.Age < GrowWaveOut + GrowWaveHold + GrowWaveSink)
            {
                // sink onto the saddle, fading into the master wave
                float u = Mathf.SmoothStep(0f, 1f, (wave.Age - GrowWaveOut - GrowWaveHold) / GrowWaveSink);
                float alt = wave.Altitude * (1f - u);
                wave.Pts[0] = origin + Vector3.back * (alt + .15f);
                wave.Line.positionCount = WavePts + 1;
                for (int i = 0; i < WavePts; i++) wave.Pts[i + 1] = growBase[i] + growNormal[i] * (alt + wave.Amp * wave.Cos[i] + .15f);
                wave.Line.SetPositions(wave.Pts);
                Color c = new(wave.Color.r, wave.Color.g, wave.Color.b, 1f - u * .75f);
                wave.Line.startColor = wave.Line.endColor = c;
            }
            else
            {
                for (int i = 0; i < WavePts; i++) growSum[i] += wave.Cos[i];
                growMerged++;
                UpdateGrowMaster();
                ReleaseWave(wave);
                growWaves.RemoveAt(w);
            }
        }
    }

    void Update()
    {
        if (growRoot != null && growRoot.activeInHierarchy) StepGrowth(Time.deltaTime);
    }

    void DisposeGrowth()
    {
        if (growTrailPts.IsCreated) growTrailPts.Dispose();
        foreach (GrowWave w in growWaves) if (w.Pts.IsCreated) w.Pts.Dispose();
        foreach (GrowWave w in growPool) if (w.Pts.IsCreated) w.Pts.Dispose();
        growWaves.Clear();
        growPool.Clear();
    }

    public IEnumerable<Renderer> Renderers()
    {
        foreach (GameObject d in dots) if (d.activeSelf) yield return d.GetComponent<Renderer>();
        foreach (LineRenderer r in routes) yield return r;
    }
}
