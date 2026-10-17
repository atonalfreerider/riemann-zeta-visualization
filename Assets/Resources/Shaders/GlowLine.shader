// Unlit, vertex-colour tinted line/surface shader with an HDR intensity multiplier.
// LineRenderer vertex colours are clamped to [0,1]; _Intensity pushes the final colour
// above 1.0 so the Bloom effect (Assets/Rendering/BloomEffect.cs) picks it up.
//
// VR: written for single-pass instanced stereo (Meta Quest / OpenXR multiview), and _Color / _Intensity are
// per-instance properties so MeshRenderers that share a mesh and this material but carry different
// MaterialPropertyBlock values (the 1300 Sacks-spiral dots) collapse into a few GPU-instanced draw calls.
Shader "Zeta/GlowLine"
{
    Properties
    {
        [HDR] _Color ("Tint", Color) = (1, 1, 1, 1)
        _Intensity ("HDR Intensity", Float) = 1.0
        _Mute ("Mute (grey, faint; up to 2: nearly out)", Float) = 0.0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", Float) = 4
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        // colour blends as usual; alpha accumulates as source-over so passthrough sees a correct alpha channel
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        ZWrite Off
        ZTest [_ZTest]
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float, _Intensity)
                UNITY_DEFINE_INSTANCED_PROP(float, _Mute)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float4 c = i.color * UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                c.rgb *= UNITY_ACCESS_INSTANCED_PROP(Props, _Intensity);
                // muted: a dim blue-grey of the same lightness, and fainter, so a highlighted part stands out against it
                // past 1 (up to 2) it fades further, nearly out, for close shots where even grey context is clutter
                float m = UNITY_ACCESS_INSTANCED_PROP(Props, _Mute);
                float m1 = saturate(m), m2 = saturate(m - 1.0);
                float l = dot(saturate(c.rgb), float3(0.3, 0.59, 0.11));
                c.rgb = lerp(c.rgb, l * float3(0.30, 0.32, 0.40), m1);
                c.a *= lerp(1.0, 0.22, m1) * lerp(1.0, 0.18, m2);
                return c;
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
