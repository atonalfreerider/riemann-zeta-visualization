// Unlit, vertex-colour tinted line/surface shader with an HDR intensity multiplier.
// LineRenderer vertex colours are clamped to [0,1]; _Intensity pushes the final colour
// above 1.0 so the Bloom effect (Assets/Rendering/BloomEffect.cs) picks it up.
Shader "Zeta/GlowLine"
{
    Properties
    {
        [HDR] _Color ("Tint", Color) = (1, 1, 1, 1)
        _Intensity ("HDR Intensity", Float) = 1.0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Intensity;

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
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 c = i.color * _Color;
                c.rgb *= _Intensity;
                return c;
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
