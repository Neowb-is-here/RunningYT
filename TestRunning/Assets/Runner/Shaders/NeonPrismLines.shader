Shader "HyperCasual/Neon Prism Lines"
{
    Properties
    {
        [HDR] _BaseTint ("Glass Tint", Color) = (0.04, 0.02, 0.09, 0.18)
        [HDR] _LineColorA ("Line Color A", Color) = (0.0, 1.0, 1.0, 1.0)
        [HDR] _LineColorB ("Line Color B", Color) = (1.0, 0.0, 0.85, 1.0)
        [HDR] _LineColorC ("Line Color C", Color) = (1.0, 0.85, 0.0, 1.0)
        [HDR] _LineColorD ("Line Color D", Color) = (0.25, 0.25, 1.0, 1.0)
        _EmissionIntensity ("Glow Brightness", Range(0.0, 20.0)) = 4.0
        _GlassAlpha ("Glass Alpha", Range(0.0, 1.0)) = 0.22
        _PatternScale ("Pattern Scale", Float) = 4.0
        _LineWidth ("Line Width", Range(0.001, 0.1)) = 0.02
        _LineSoftness ("Line Softness", Range(0.001, 0.2)) = 0.035
        _ScrollSpeed ("Scroll Speed", Float) = 0.2
        _RimIntensity ("Rim Intensity", Float) = 1.5
        _RimPower ("Rim Power", Range(0.5, 8.0)) = 2.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseTint;
                half4 _LineColorA;
                half4 _LineColorB;
                half4 _LineColorC;
                half4 _LineColorD;
                half _EmissionIntensity;
                half _GlassAlpha;
                half _PatternScale;
                half _LineWidth;
                half _LineSoftness;
                half _ScrollSpeed;
                half _RimIntensity;
                half _RimPower;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = positionInputs.positionCS;
                output.positionOS = input.positionOS.xyz;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            half LaserLine(float2 position, float2 direction, half offset)
            {
                direction = normalize(direction);
                half distanceToLine = abs(frac(dot(position, direction) + offset) - 0.5);
                return 1.0 - smoothstep(_LineWidth, _LineWidth + _LineSoftness, distanceToLine);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float time = _Time.y * _ScrollSpeed;
                float3 localPosition = input.positionOS;

                float2 xy = localPosition.xy * _PatternScale + float2(time * 0.37, time * 0.11);
                float2 yz = localPosition.yz * _PatternScale + float2(time * -0.21, time * 0.29);
                float2 xz = localPosition.xz * _PatternScale + float2(time * 0.17, time * -0.31);

                half lineA = LaserLine(xy, float2(1.0, 0.33), 0.07);
                half lineB = LaserLine(xy, float2(-0.42, 1.0), 0.23);
                half lineC = LaserLine(yz, float2(0.68, 1.0), 0.41);
                half lineD = LaserLine(xz, float2(1.0, -0.58), 0.61);
                half lineE = LaserLine(xz + yz * 0.35, float2(0.28, 1.0), 0.79);

                half pulse = 0.78 + 0.22 * sin(_Time.y * 2.4);
                half lineMask = saturate(lineA + lineB + lineC + lineD + lineE);

                half3 lineColor =
                    lineA * _LineColorA.rgb +
                    lineB * _LineColorB.rgb +
                    lineC * _LineColorC.rgb +
                    lineD * _LineColorD.rgb +
                    lineE * lerp(_LineColorA.rgb, _LineColorB.rgb, 0.5);

                half3 normalWS = normalize(input.normalWS);
                half3 viewDirectionWS = normalize(GetWorldSpaceViewDir(input.positionWS));
                half rim = pow(1.0 - saturate(dot(normalWS, viewDirectionWS)), _RimPower) * _RimIntensity;

                half3 color = _BaseTint.rgb * _GlassAlpha;
                color += lineColor * _EmissionIntensity * pulse;
                color += rim * (_LineColorA.rgb + _LineColorB.rgb) * 0.5;

                half alpha = saturate(_BaseTint.a * _GlassAlpha + lineMask * 0.9 + rim * 0.2);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
