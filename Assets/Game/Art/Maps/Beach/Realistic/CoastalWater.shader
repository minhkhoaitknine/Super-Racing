Shader "SuperRacing/Coastal Water"
{
    Properties
    {
        _BaseColor ("Deep water", Color) = (0.035,0.22,0.25,1)
        _ShallowColor ("Wave tint", Color) = (0.09,0.42,0.40,1)
        _Smoothness ("Smoothness", Range(0,1)) = 0.88
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
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
            half4 Frag(Varyings input):SV_Target
            {
                float2 p=input.positionWS.xz;
                float t=_Time.y;
                float a=dot(p,float2(1.3,0.7))+t*0.8;
                float b=dot(p,float2(-2.1,1.8))-t*1.1;
                float c=dot(p,float2(4.2,3.1))+t*1.5;
                float2 slope=float2(1.3,0.7)*cos(a)*0.04+float2(-2.1,1.8)*cos(b)*0.018+float2(4.2,3.1)*cos(c)*0.008;
                half3 normal=normalize(float3(-slope.x,1,-slope.y));
                InputData data=(InputData)0;
                data.positionWS=input.positionWS;
                data.normalWS=normal;
                data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(input.positionWS);
                data.shadowCoord=TransformWorldToShadowCoord(input.positionWS);
                data.bakedGI=SampleSH(normal);
                data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(input.positionCS);
                data.shadowMask=half4(1,1,1,1);
                SurfaceData surface=(SurfaceData)0;
                surface.albedo=lerp(_BaseColor.rgb,_ShallowColor.rgb,0.45+0.12*sin(a)*sin(b));
                surface.alpha=1;
                surface.smoothness=_Smoothness;
                surface.occlusion=1;
                surface.normalTS=half3(0,0,1);
                half4 color=UniversalFragmentPBR(data,surface);
                color.rgb=MixFog(color.rgb,input.fog);
                return color;
            }
            ENDHLSL
        }
    }
}
