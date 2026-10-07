Shader "CatHome/Cozy Pond"
{
    Properties { _BaseColor("Water", Color)=(.31,.60,.57,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END
            Varyings Vert(Attributes a)
            { Varyings o; o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;return o; }
            half4 Frag(Varyings i):SV_Target
            {
                float2 p=i.uv*24;
                float t=_Time.y*.36;
                float wave=sin(p.x*1.3+sin(p.y*.9+t))+cos(p.y*1.7+sin(p.x*.7-t));
                float glint=smoothstep(1.66,1.99,wave)*.11;
                float depth=.91+.10*sin(i.uv.y*3.14159);
                return half4(_BaseColor.rgb*depth+half3(.10,.16,.12)*(wave*.12)+glint,1);
            }
            ENDHLSL
        }
    }
}
