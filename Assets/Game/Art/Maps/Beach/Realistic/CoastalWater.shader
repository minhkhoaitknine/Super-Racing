Shader "SuperRacing/Coastal Water"
{
    Properties
    {
        _BaseColor ("Deep water", Color) = (0.025,0.19,0.23,1)
        _ShallowColor ("Wave tint", Color) = (0.07,0.35,0.34,1)
        _Smoothness ("Smoothness", Range(0,1)) = 0.88
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ShallowColor;
                half _Smoothness;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float fog:TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs p=GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS=p.positionCS;
                output.positionWS=p.positionWS;
                output.fog=ComputeFogFactor(p.positionCS.z);
                return output;
            }
            float Hash(float2 p)
            {
                float3 p3=frac(float3(p.xyx)*0.1031);
                p3+=dot(p3,p3.yzx+33.33);
                return frac((p3.x+p3.y)*p3.z);
            }
            float Noise(float2 p)
            {
                float2 cell=floor(p), f=frac(p);
                f=f*f*(3-2*f);
                return lerp(lerp(Hash(cell),Hash(cell+float2(1,0)),f.x),
                            lerp(Hash(cell+float2(0,1)),Hash(cell+1),f.x),f.y);
            }
            float Ripples(float2 p,float t)
            {
                return Noise(p*0.8+float2(t*0.07,t*0.03))
                    +0.3*Noise(p*2.3+float2(-t*0.09,t*0.11));
            }
            half4 Frag(Varyings input):SV_Target
            {
                float2 p=input.positionWS.xz;
                float t=_Time.y;
                float a=dot(p,float2(1.3,0.7))+t*0.8;
                float b=dot(p,float2(-2.1,1.8))-t*1.1;
                float height=Ripples(p,t);
                float2 slope=float2(Ripples(p+float2(0.08,0),t)-height,
                                    Ripples(p+float2(0,0.08),t)-height)*2.2;
                slope+=float2(1.3,0.7)*cos(a)*0.009;
                half3 normal=normalize(float3(-slope.x,1,-slope.y));
                half3 view=GetWorldSpaceNormalizeViewDir(input.positionWS);
                Light sun=GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half fresnel=0.02+0.98*pow(1-saturate(dot(normal,view)),5);
                half3 reflected=reflect(-view,normal);
                half3 sky=lerp(half3(0.48,0.62,0.68),half3(0.17,0.34,0.48),saturate(reflected.y));
                half3 base=lerp(_BaseColor.rgb,_ShallowColor.rgb,0.45+0.08*sin(a)*sin(b));
                half lighting=0.6+0.4*saturate(dot(normal,sun.direction))*sun.shadowAttenuation;
                half3 halfVector=SafeNormalize(view+sun.direction);
                half sparkle=pow(saturate(dot(normal,halfVector)),lerp(64,256,_Smoothness));
                half4 color=half4(lerp(base*lighting,sky,fresnel)+sun.color*sparkle*sun.shadowAttenuation*0.45,1);
                color.rgb=MixFog(color.rgb,input.fog);
                return color;
            }
            ENDHLSL
        }
    }
}
