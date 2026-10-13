using UnityEngine;

public class Factory : MonoBehaviour
{
    public Shader mainShader;

    static Material sharedMaterial;

    /// <summary>Shared glow material (Zeta/GlowLine), created on first use so script execution order never matters.</summary>
    public static Material mainMat
    {
        get
        {
            if (sharedMaterial == null)
            {
                Shader glow = Shader.Find("Zeta/GlowLine");
                sharedMaterial = new Material(glow != null ? glow : Shader.Find("Sprites/Default"));
                if (glow != null) sharedMaterial.SetFloat(IntensityId, NoGlow);
            }

            return sharedMaterial;
        }
    }

    static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    /// <summary>HDR multiplier for "hero" elements so they bloom (threshold is ~1.0).</summary>
    public const float GlowIntensity = 1.55f;
    public const float SoftGlowIntensity = 1.12f;
    public const float NoGlow = 1f;

    void Awake()
    {
        Material _ = mainMat;
    }

    public static LineRenderer NewLine(string lineName, float W, Color color, float intensity = NoGlow)
    {
        LineRenderer newLine = new GameObject(lineName).AddComponent<LineRenderer>();
        newLine.material = mainMat;
        newLine.startColor = color;
        newLine.endColor = color;
        newLine.startWidth = W;
        newLine.endWidth = W;
        newLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        newLine.receiveShadows = false;
        SetIntensity(newLine, intensity);
        return newLine;
    }

    /// <summary>Per-renderer HDR intensity without instancing a new material.</summary>
    public static void SetIntensity(Renderer renderer, float intensity)
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        block.SetFloat(IntensityId, intensity);
        renderer.SetPropertyBlock(block);
    }

    public static void SetColor(Renderer renderer, Color color)
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        block.SetColor(ColorId, color);
        renderer.SetPropertyBlock(block);
    }

    /// <summary>A glowing sphere marker (used for zeta zeros and probes).</summary>
    public static GameObject NewSphere(string name, float scale, Color color, float intensity = GlowIntensity)
    {
        GameObject point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        point.name = name;
        Object.Destroy(point.GetComponent<Collider>());
        point.transform.localScale = Vector3.one * scale;
        MeshRenderer renderer = point.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = mainMat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        SetColor(renderer, color);
        SetIntensity(renderer, intensity);
        return point;
    }

    /// <summary>A flat quad in the XY plane (facing −Z, i.e. toward the default camera).</summary>
    public static MeshRenderer NewQuad(string name, Vector2 size, Color color, float intensity = NoGlow)
    {
        GameObject go = new GameObject(name);
        MeshFilter filter = go.AddComponent<MeshFilter>();
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        Mesh mesh = new Mesh { name = name };
        float hx = size.x * .5f, hy = size.y * .5f;
        mesh.vertices = new[]
        {
            new Vector3(-hx, -hy, 0), new Vector3(hx, -hy, 0), new Vector3(hx, hy, 0), new Vector3(-hx, hy, 0)
        };
        mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateNormals();
        filter.sharedMesh = mesh;
        renderer.sharedMaterial = mainMat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        SetColor(renderer, color);
        SetIntensity(renderer, intensity);
        return renderer;
    }

    public static class Viridis
    {
        // https://cran.r-project.org/web/packages/viridis/vignettes/intro-to-viridis.html
        static readonly Color32[] viridis = {
            new(253, 231, 37, 255),
            new(220, 227, 25, 255),
            new(184, 222, 41, 255),
            new(149, 216, 64, 255),
            new(115, 208, 85, 255),
            new(85, 198, 103, 255),
            new(60, 187, 117, 255),
            new(41, 175, 127, 255),
            new(32, 163, 135, 255),
            new(31, 150, 139, 255),
            new(35, 138, 141, 255),
            new(40, 125, 142, 255),
            new(45, 112, 142, 255),
            new(51, 99, 141, 255),
            new(57, 86, 140, 255),
            new(64, 71, 136, 255),
            new(69, 55, 129, 255),
            new(72, 38, 119, 255),
            new(72, 21, 103, 255),
            new(68, 1, 84, 255)
        };

        public static Color ViridisColor(float prct)
        {
            return viridis[Mathf.RoundToInt(Mathf.Clamp01(prct) * (viridis.Length - 1))];
        }
    }

    /// <summary>Palette shared by the visualization and the tour annotations.</summary>
    public static class Palette
    {
        public static readonly Color Zeta = new(.85f, .88f, .95f);
        public static readonly Color Tail = new(1f, .48f, .1f);
        public static readonly Color Zero = new(1f, .85f, .1f);
        public static readonly Color Prime = Color.white;
        public static readonly Color Cosine = new(.25f, .9f, .85f);
        public static readonly Color Dagger = new(1f, .2f, .2f);
        public static readonly Color Grid = new(.22f, .22f, .25f);
        public static readonly Color CriticalLine = new(1f, .85f, .1f);
        public static readonly Color ZeroFree = new(.1f, .55f, .9f);
        public static readonly Color Ghost = new(1f, .25f, .35f);
        public static readonly Color ImAxis = new(.6f, .4f, 1f);
        public static readonly Color ReAxis = new(.4f, .9f, .5f);
        public static readonly Color Caption = Color.white;
        public static readonly Color CaptionHighlight = new(1f, .84f, 0f);
    }
}
