Shader "UI/RadialGradient"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Center Color", Color) = (0.18, 0.60, 0.30, 1)
        _EdgeColor ("Edge Color", Color) = (0.05, 0.22, 0.11, 1)
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radius", Range(0, 1.5)) = 0.55
        _Softness ("Softness", Range(0.001, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref 1
            Comp Always
            Pass Keep
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _EdgeColor;
            float4 _Center;
            float _Radius;
            float _Softness;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPosition = v.vertex;
                o.texcoord = v.texcoord;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.texcoord);
                float2 uv = i.texcoord.xy;
                float2 d = uv - _Center.xy;
                float dist = length(d);
                float t = smoothstep(_Radius, _Radius + _Softness, dist);
                fixed4 col = lerp(_Color, _EdgeColor, t);
                col *= tex * i.color;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
