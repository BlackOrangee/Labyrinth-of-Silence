Shader "Custom/URP_Outline"
{
    Properties
    {
        _OutlineColor   ("Outline Color",            Color)       = (0.85, 1, 1, 1)
        _OutlineWidth   ("Outline Width (px)",       Float)       = 4.0
        _PulseSpeed     ("Pulse Speed",              Float)       = 2.0
        _PulseMinAlpha  ("Pulse Min Alpha",          Range(0,1))  = 0.2
        _FlickerAmount  ("Flicker Amount (0=off)",   Range(0,10)) = 0.0
        _PhaseOffset    ("Phase Offset",             Float)       = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Transparent+1"
        }

        Pass
        {
            Name "Outline"
            Cull   Front
            ZWrite Off
            ZTest  LEqual
            Blend  SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _PulseSpeed;
                float  _PulseMinAlpha;
                float  _FlickerAmount;
                float  _PhaseOffset;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                // ── Крок 1: переводимо нормаль у world space ──────────────
                // TransformObjectToWorldNormal — безпечна функція URP,
                // враховує масштаб об'єкта правильно
                float3 worldNormal = TransformObjectToWorldNormal(IN.normalOS);

                // ── Крок 2: переводимо нормаль у view space ───────────────
                // (view space = система координат камери)
                float3 viewNormal = TransformWorldToViewDir(worldNormal, true);

                // ── Крок 3: отримуємо clip-позицію вершини ────────────────
                float4 clipPos = TransformObjectToHClip(IN.positionOS.xyz);

                // ── Крок 4: зсуваємо вершину вздовж нормалі в clip space ──
                // Ділимо на _ScreenParams.xy щоб товщина була в пікселях,
                // а не у відносних одиницях — тому контур не залежить від
                // розміру об'єкта та відстані до камери
                float2 screenOffset = normalize(viewNormal.xy)
                                      * (_OutlineWidth / _ScreenParams.xy)
                                      * clipPos.w * 2.0;

                clipPos.xy += screenOffset;
                OUT.positionHCS = clipPos;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float t = _Time.y + _PhaseOffset;

                // ── Плавна пульсація ──────────────────────────────────────
                float pulse = sin(t * _PulseSpeed) * 0.5 + 0.5;
                float alpha = lerp(_PulseMinAlpha, 1.0, pulse);

                // ── Органічне мерехтіння (без step — без смикання!) ───────
                if (_FlickerAmount > 0.0)
                {
                    float f1 = sin(t * 7.3  + 1.1) * 0.5 + 0.5;
                    float f2 = sin(t * 13.7 + 2.4) * 0.5 + 0.5;
                    float f3 = sin(t * 3.1  + 0.7) * 0.5 + 0.5;
                    float flicker = f1 * 0.5 + f2 * 0.3 + f3 * 0.2;
                    alpha *= lerp(1.0, flicker, _FlickerAmount / 10.0);
                }

                return half4(_OutlineColor.rgb, _OutlineColor.a * saturate(alpha));
            }
            ENDHLSL
        }
    }

    // Якщо шейдер не підтримується — замість рожевого показуємо прозорий
    FallBack "Hidden/InternalErrorShader"
}
