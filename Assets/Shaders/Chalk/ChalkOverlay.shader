Shader "Chalk/Overlay"
{
    Properties
    {
        _MainTex ("Stroke Texture", 2D) = "white" {}
        _Opacity ("Opacity", Range(0,1)) = 0.24
        _GrainTex ("Grain Texture", 2D) = "gray" {}
        _GrainScale ("Grain Scale (xy) + Offset (zw)", Vector) = (5,5,0,0)
        _GrainStrength ("Grain Strength", Range(0,1)) = 0.6
        _ChalkTint ("Chalk Tint", Color) = (0.93,0.91,0.86,0.9)
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }

        Pass
        {
            Name "ChalkOverlayURP"
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 grainUV : TEXCOORD1;
                float4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _GrainTex_ST;
                float4 _GrainScale;
                float4 _ChalkTint;
                float _Opacity;
                float _GrainStrength;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_GrainTex);
            SAMPLER(sampler_GrainTex);

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                float2 grainUV = input.uv * _GrainScale.xy + _GrainScale.zw;
                output.grainUV = grainUV * _GrainTex_ST.xy + _GrainTex_ST.zw;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 baseSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                half baseLum = dot(baseSample.rgb, half3(0.2126h, 0.7152h, 0.0722h));
                half strokeMask = saturate(max(baseSample.a, baseLum * 0.95h));

                half grainA = SAMPLE_TEXTURE2D(_GrainTex, sampler_GrainTex, input.grainUV).r;
                half grainB = SAMPLE_TEXTURE2D(_GrainTex, sampler_GrainTex, input.grainUV * 1.93h + half2(7.13h, 3.71h)).r;
                half grainCombined = saturate(lerp(grainA, grainA * grainB, 0.6h));
                half erosion = saturate((grainCombined - 0.18h) / 0.82h);
                half grainMod = lerp(1.0h - saturate(_GrainStrength), 1.0h, erosion);

                half alpha = saturate(strokeMask * grainMod * _Opacity * _ChalkTint.a);
                half chalkBrightness = lerp(0.78h, 1.05h, baseLum);
                half3 rgb = _ChalkTint.rgb * chalkBrightness;
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }

        Pass
        {
            Name "ChalkOverlayBuiltIn"
            Lighting Off
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _GrainTex;
            float4 _GrainTex_ST;
            float4 _GrainScale;
            float4 _ChalkTint;
            float _Opacity;
            float _GrainStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 grainUV : TEXCOORD1;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                float2 grainUV = v.uv * _GrainScale.xy + _GrainScale.zw;
                o.grainUV = grainUV * _GrainTex_ST.xy + _GrainTex_ST.zw;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 baseSample = tex2D(_MainTex, i.uv) * i.color;
                fixed baseLum = dot(baseSample.rgb, fixed3(0.2126, 0.7152, 0.0722));
                fixed strokeMask = saturate(max(baseSample.a, baseLum * 0.95));

                fixed grainA = tex2D(_GrainTex, i.grainUV).r;
                fixed grainB = tex2D(_GrainTex, i.grainUV * 1.93 + fixed2(7.13, 3.71)).r;
                fixed grainCombined = saturate(lerp(grainA, grainA * grainB, 0.6));
                fixed erosion = saturate((grainCombined - 0.18) / 0.82);
                fixed grainMod = lerp(1.0 - saturate(_GrainStrength), 1.0, erosion);

                fixed alpha = saturate(strokeMask * grainMod * _Opacity * _ChalkTint.a);
                fixed chalkBrightness = lerp(0.78, 1.05, baseLum);
                fixed3 rgb = _ChalkTint.rgb * chalkBrightness;
                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
