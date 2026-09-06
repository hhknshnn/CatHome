Shader "Hidden/CatHome/BackdropCopy"
{
    Properties { _MainTex("World",2D)="white"{} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _BlurDirection;
            fixed4 frag(v2f_img i):SV_Target
            {
                // Bilinear paired taps assume adjacent texels, not widely spaced copies.
                float2 d=_BlurDirection.xy*.4;
                half3 color=tex2D(_MainTex,i.uv).rgb*.227027;
                color+=(tex2D(_MainTex,i.uv+d*1.384615).rgb+tex2D(_MainTex,i.uv-d*1.384615).rgb)*.316216;
                color+=(tex2D(_MainTex,i.uv+d*3.230769).rgb+tex2D(_MainTex,i.uv-d*3.230769).rgb)*.070270;
                return half4(color,1);
            }
            ENDCG
        }
    }
}
