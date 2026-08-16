Shader "CatHome/Window Sky"
{
    Properties
    {
        [MainTexture] _BaseMap("Optional Sky Texture", 2D) = "white" {}
        [HideInInspector] _MainTex("Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Sky Color", Color) = (0.22, 0.62, 1, 1)
        [HideInInspector] _Color("Color", Color) = (0.22, 0.62, 1, 1)
        _HorizonColor("Horizon Color", Color) = (0.72, 0.91, 1, 1)
        _StarStrength("Star Strength", Range(0, 1)) = 0
        _MoonStrength("Moon Strength", Range(0, 1)) = 0
        [HideInInspector] _Cull("Cull", Float) = 0
        [HideInInspector] _ZWrite("Z Write", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "WindowSkyUnlit"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull [_Cull]
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _Color;
                half4 _HorizonColor;
                half _StarStrength;
                half _MoonStrength;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half Hash21(float2 value)
            {
                value = frac(value * float2(123.34, 456.21));
                value += dot(value, value + 45.32);
                return frac(value.x * value.y);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half horizonBlend = 1.0h - smoothstep(0.12h, 0.78h, input.uv.y);
                half3 sky = lerp(_BaseColor.rgb, _HorizonColor.rgb, horizonBlend);
                sky *= SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;

                float2 starGrid = input.uv * float2(11.0, 7.0);
                float2 starCell = floor(starGrid);
                float2 starPoint = frac(starGrid) - 0.5;
                half sparseCell = step(0.91h, Hash21(starCell));
                half starDot = 1.0h - step(0.075h, length(starPoint));
                half upperSky = smoothstep(0.30h, 0.58h, input.uv.y);
                half stars = sparseCell * starDot * upperSky * _StarStrength;

                sky = lerp(sky, half3(0.78h, 0.86h, 1.0h), stars);

                // Small procedural crescent: two soft circles, with the second one cutting
                // into the first. No texture lookup or extra object is required.
                float2 moonUv = input.uv - float2(0.79, 0.72);
                moonUv.x *= 1.35;
                half moonDisc = 1.0h - smoothstep(0.068h, 0.075h, length(moonUv));
                half moonCut = 1.0h - smoothstep(
                    0.061h,
                    0.069h,
                    length(moonUv - float2(-0.028, 0.018))
                );
                half moon = moonDisc * (1.0h - moonCut) * _MoonStrength;
                sky = lerp(sky, half3(1.0h, 0.96h, 0.78h), moon);
                return half4(sky, 1.0h);
            }
            ENDHLSL
        }
    }
}
