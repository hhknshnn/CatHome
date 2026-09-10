Shader "CatHome/MiniGameSky"
{
    Properties { _Top("Sky",Color)=(.5,.75,.82,1) _Bottom("Horizon",Color)=(.94,.94,.86,1) }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
            float4 _Top;float4 _Bottom;
            CBUFFER_END
            Varyings Vert(Attributes i){Varyings o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;return o;}
            half4 Frag(Varyings i):SV_Target{return lerp(_Bottom,_Top,smoothstep(.15,1,i.uv.y));}
            ENDHLSL
        }
    }
}
