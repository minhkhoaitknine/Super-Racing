Shader "Hidden/SuperRacing/CarPaintRecolor"
{
    Properties { _MainTex ("Source", 2D) = "white" {} _PaintColor ("Paint", Color) = (1,1,1,1) }
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
            float4 _PaintColor;
            float4 frag(v2f_img input) : SV_Target
            {
                float4 source = tex2D(_MainTex, input.uv);
                float high = max(source.r, max(source.g, source.b));
                float low = min(source.r, min(source.g, source.b));
                // Factory paint is saturated; neutral glass, trim and shading stay intact.
                float saturation = (high - low) / max(high, 0.0001);
                float paintMask = smoothstep(0.15, 0.55, saturation);
                return float4(lerp(source.rgb, _PaintColor.rgb * high, paintMask), source.a);
            }
            ENDCG
        }
    }
}
