Shader "Custom/VideoVignette" {
    Properties {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _HoleSize ("Hole Size (Дірка по центру)", Range(0, 1.5)) = 0.5
        _Softness ("Softness (М'якість країв)", Range(0.01, 1)) = 0.3
    }
    SubShader {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane"}
        Blend SrcAlpha OneMinusSrcAlpha
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t { float4 vertex : POSITION; float2 texcoord : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 texcoord : TEXCOORD0; float4 color : COLOR; };

            sampler2D _MainTex;
            float _HoleSize;
            float _Softness;

            v2f vert (appdata_t v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.texcoord) * i.color;

                float dist = distance(i.texcoord, float2(0.5, 0.5));

                float alpha = smoothstep(_HoleSize - _Softness, _HoleSize + _Softness, dist);
                col.a *= alpha;
                return col;
            }
            ENDCG
        }
    }
}