// Minimal HDR bloom for the Built-in Render Pipeline.
// Pass 0: prefilter (soft threshold)   Pass 1: downsample (13-tap)
// Pass 2: upsample (tent, additive)     Pass 3: composite onto the source frame
Shader "Zeta/Bloom"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        CGINCLUDE
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        float4 _MainTex_TexelSize;
        sampler2D _BloomTex;
        float4 _Filter;   // x: threshold, y: threshold - knee, z: 2*knee, w: 0.25/knee
        float _Intensity;
        float _SampleScale;

        struct v2f
        {
            float4 pos : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        v2f vert(appdata_img v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.uv = v.texcoord;
            return o;
        }

        half3 Prefilter(half3 c)
        {
            half brightness = max(c.r, max(c.g, c.b));
            half soft = brightness - _Filter.y;
            soft = clamp(soft, 0, _Filter.z);
            soft = soft * soft * _Filter.w;
            half contribution = max(soft, brightness - _Filter.x);
            contribution /= max(brightness, 1e-4);
            return c * contribution;
        }

        half3 Sample(float2 uv)
        {
            return tex2D(_MainTex, uv).rgb;
        }

        // 13-tap downsample (Jimenez / Call of Duty style), reduces flicker on thin lines.
        half3 DownsampleBox13(float2 uv)
        {
            float2 t = _MainTex_TexelSize.xy;
            half3 A = Sample(uv + t * float2(-1.0, -1.0));
            half3 B = Sample(uv + t * float2( 0.0, -1.0));
            half3 C = Sample(uv + t * float2( 1.0, -1.0));
            half3 D = Sample(uv + t * float2(-0.5, -0.5));
            half3 E = Sample(uv + t * float2( 0.5, -0.5));
            half3 F = Sample(uv + t * float2(-1.0,  0.0));
            half3 G = Sample(uv);
            half3 H = Sample(uv + t * float2( 1.0,  0.0));
            half3 I = Sample(uv + t * float2(-0.5,  0.5));
            half3 J = Sample(uv + t * float2( 0.5,  0.5));
            half3 K = Sample(uv + t * float2(-1.0,  1.0));
            half3 L = Sample(uv + t * float2( 0.0,  1.0));
            half3 M = Sample(uv + t * float2( 1.0,  1.0));

            half2 div = (1.0 / 4.0) * half2(0.5, 0.125);
            half3 o = (D + E + I + J) * div.x;
            o += (A + B + G + F) * div.y;
            o += (B + C + H + G) * div.y;
            o += (F + G + L + K) * div.y;
            o += (G + H + M + L) * div.y;
            return o;
        }

        half3 UpsampleTent(float2 uv)
        {
            float4 d = _MainTex_TexelSize.xyxy * float4(1.0, 1.0, -1.0, 0.0) * _SampleScale;
            half3 s = Sample(uv - d.xy);
            s += Sample(uv - d.wy) * 2.0;
            s += Sample(uv - d.zy);
            s += Sample(uv + d.zw) * 2.0;
            s += Sample(uv) * 4.0;
            s += Sample(uv + d.xw) * 2.0;
            s += Sample(uv + d.zy);
            s += Sample(uv + d.wy) * 2.0;
            s += Sample(uv + d.xy);
            return s * (1.0 / 16.0);
        }
        ENDCG

        // 0: prefilter
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(v2f i) : SV_Target
            {
                half3 c = Prefilter(DownsampleBox13(i.uv));
                return half4(c, 1);
            }
            ENDCG
        }

        // 1: downsample
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(v2f i) : SV_Target
            {
                return half4(DownsampleBox13(i.uv), 1);
            }
            ENDCG
        }

        // 2: upsample + accumulate (additive blend with the previous level)
        Pass
        {
            Blend One One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(v2f i) : SV_Target
            {
                return half4(UpsampleTent(i.uv), 1);
            }
            ENDCG
        }

        // 3: composite
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(v2f i) : SV_Target
            {
                half4 src = tex2D(_MainTex, i.uv);
                half3 bloom = tex2D(_BloomTex, i.uv).rgb * _Intensity;
                return half4(src.rgb + bloom, src.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
