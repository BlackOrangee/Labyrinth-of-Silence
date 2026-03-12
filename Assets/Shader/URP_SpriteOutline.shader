Shader "Custom/URP_SpriteOutline"
{
    Properties
    {
        [HideInInspector] _MainTex ("Sprite Texture", 2D) = "white" {}
        
        _OutlineColor  ("Outline Color",           Color)      = (0.9, 1.0, 1.0, 1.0)
        _OutlineWidth  ("Outline Width (px 0-8)",  Range(0,8)) = 2.0
        _PulseSpeed    ("Pulse Speed",             Float)      = 2.0
        _PulseMinAlpha ("Pulse Min Alpha",         Range(0,1)) = 0.2
        _FlickerSpeed  ("Flicker Speed (0=off)",   Float)      = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Transparent"
            "PreviewType"    = "Plane"
        }

        Cull   Off
        ZWrite Off
        Blend  SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _PulseSpeed;
                float  _PulseMinAlpha;
                float  _FlickerSpeed;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR; 
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color       = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 mainColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;
                float2 texelSize = _MainTex_TexelSize.xy * _OutlineWidth;

                float neighborAlpha = 0;
                neighborAlpha += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( texelSize.x,  0)).a;
                neighborAlpha += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(-texelSize.x,  0)).a;
                neighborAlpha += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( 0,  texelSize.y)).a;
                neighborAlpha += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( 0, -texelSize.y)).a;
                neighborAlpha += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( texelSize.x,  texelSize.y)).a * 0.707;
                neighborAlpha += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(-texelSize.x,  texelSize.y)).a * 0.707;
                neighborAlpha += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( texelSize.x, -texelSize.y)).a * 0.707;
                neighborAlpha += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(-texelSize.x, -texelSize.y)).a * 0.707;

                float isOutline = step(0.01, neighborAlpha) * (1.0 - step(0.01, mainColor.a));

                float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                float alpha = lerp(_PulseMinAlpha, 1.0, pulse);

                if (_FlickerSpeed > 0.0)
                {
                    float flicker = step(0.05, frac(sin(_Time.y * _FlickerSpeed) * 43758.5));
                    alpha *= flicker;
                }
                half4 outlinePixel = half4(_OutlineColor.rgb, _OutlineColor.a * alpha);
                half4 result = lerp(mainColor, outlinePixel, isOutline);

                return result;
            }
            ENDHLSL
        }
    }
}
