// Opaque, glossy marbles for the counting games: a fixed key light in view space gives each sphere a lit side, a soft
// rim and a small specular glint, so a marble reads as a marble without any scene lights. _Color is per instance, so
// thousands of marbles (or E8 lattice points) in a few colours draw as GPU-instanced batches.
//
// VR: single-pass instanced stereo (Meta Quest / OpenXR multiview), like Zeta/GlowLine.
Shader "Zeta/Marble"
{
    Properties
    {
        [HDR] _Color ("Colour", Color) = (1, 1, 1, 1)
        _Glow ("Glow (above 1 blooms)", Float) = 1.0
    }
    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" "IgnoreProjector" = "True" }
        ZWrite On
        // both faces: the icosphere's winding never matters, and depth keeps only the near side
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
                UNITY_DEFINE_INSTANCED_PROP(float, _Glow)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 nv : TEXCOORD0;      // the normal in view space
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
                o.nv = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal));
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float4 c = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                float g = UNITY_ACCESS_INSTANCED_PROP(Props, _Glow);
                float3 n = normalize(i.nv);
                // view space: x right, y up, z toward the viewer
                float3 l = normalize(float3(-0.45, 0.6, 0.66));
                float diff = saturate(dot(n, l));
                float rim = pow(1.0 - saturate(n.z), 2.0);
                float3 h = normalize(l + float3(0, 0, 1));
                float spec = pow(saturate(dot(n, h)), 48.0);
                float3 rgb = c.rgb * (0.34 + 0.72 * diff) + c.rgb * rim * 0.35 + spec * 0.55;
                return float4(rgb * g, 1.0);
            }
            ENDCG
        }
    }
    Fallback "Unlit/Color"
}
