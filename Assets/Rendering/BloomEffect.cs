using UnityEngine;

/// <summary>
/// Self-contained HDR bloom for the Built-in Render Pipeline (no package dependency).
/// Attached to the main camera at runtime by <see cref="ZetaBootstrap"/>.
/// Anything rendered brighter than <see cref="Threshold"/> (see Zeta/GlowLine's _Intensity)
/// blooms; dim grid lines and text stay crisp.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class BloomEffect : MonoBehaviour
{
    [Range(0f, 4f)] public float Threshold = 1.0f;
    [Range(0f, 1f)] public float SoftKnee = 0.6f;
    [Range(0f, 4f)] public float Intensity = 1.1f;
    [Range(1, 8)] public int Iterations = 6;
    [Range(0.5f, 2f)] public float Diffusion = 1.0f;

    Shader shader;
    Material material;
    readonly RenderTexture[] pyramid = new RenderTexture[16];

    void OnEnable()
    {
        shader = Shader.Find("Zeta/Bloom");
        if (shader == null)
        {
            Debug.LogWarning("BloomEffect: shader 'Zeta/Bloom' not found; bloom disabled.");
            enabled = false;
            return;
        }

        material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        GetComponent<Camera>().allowHDR = true;
    }

    void OnDisable()
    {
        if (material != null)
        {
            DestroyImmediate(material);
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (material == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        float knee = Threshold * SoftKnee;
        material.SetVector("_Filter", new Vector4(Threshold, Threshold - knee, knee * 2f, 0.25f / Mathf.Max(knee, 1e-5f)));
        material.SetFloat("_Intensity", Intensity);
        material.SetFloat("_SampleScale", Diffusion);

        int width = source.width / 2;
        int height = source.height / 2;
        RenderTextureFormat format = RenderTextureFormat.DefaultHDR;

        RenderTexture current = RenderTexture.GetTemporary(width, height, 0, format);
        pyramid[0] = current;
        Graphics.Blit(source, current, material, 0);

        int levels = 1;
        for (int i = 1; i < Iterations; i++)
        {
            width /= 2;
            height /= 2;
            if (width < 2 || height < 2) break;

            RenderTexture next = RenderTexture.GetTemporary(width, height, 0, format);
            Graphics.Blit(current, next, material, 1);
            pyramid[i] = next;
            current = next;
            levels++;
        }

        for (int i = levels - 2; i >= 0; i--)
        {
            Graphics.Blit(pyramid[i + 1], pyramid[i], material, 2);
            RenderTexture.ReleaseTemporary(pyramid[i + 1]);
            pyramid[i + 1] = null;
        }

        material.SetTexture("_BloomTex", pyramid[0]);
        Graphics.Blit(source, destination, material, 3);
        RenderTexture.ReleaseTemporary(pyramid[0]);
        pyramid[0] = null;
    }
}
