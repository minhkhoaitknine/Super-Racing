Shader "SuperRacing/Town Atlas Detail"
{
    Properties
    {
        _BaseMap("Original architecture atlas", 2D) = "white" {}
        _BaseColor("Tint", Color) = (1,1,1,1)
        [Normal] _BumpMap("Atlas relief", 2D) = "bump" {}
        _BumpScale("Relief strength", Range(0,1)) = .15
        _GrassMap("Ground detail", 2D) = "white" {}
        [Normal] _GrassNormal("Ground normal", 2D) = "bump" {}
        _GrassScale("Ground tile metres", Float) = 3
        _Smoothness("Smoothness", Range(0,1)) = .1
        _Cutoff("Cutoff", Float) = .5
        [HideInInspector] _Cull("Cull", Float) = 2
        [HideInInspector] _ZWrite("ZWrite", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_GrassMap); SAMPLER(sampler_GrassMap);
            TEXTURE2D(_GrassNormal); SAMPLER(sampler_GrassNormal);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _GrassScale;
                half _BumpScale, _Smoothness, _Cutoff;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 tangentOS:TANGENT; float2 uv:TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS:SV_POSITION;
                float3 positionWS:TEXCOORD0;
                half3 normalWS:TEXCOORD1;
                half4 tangentWS:TEXCOORD2;
                float2 uv:TEXCOORD3;
                half fog:TEXCOORD4;
                float3 positionOS:TEXCOORD5;
            };
            Varyings Vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.positionOS = input.positionOS.xyz;
                o.normalWS = n.normalWS;
                o.tangentWS = half4(n.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                half3 atlas = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb;
                half3 n = normalize(i.normalWS);
                // Smooth pixel mask keeps paving, kerbs and facades on their original atlas.
                half grassMask = saturate((min(atlas.r, atlas.g) - atlas.b * 1.7h - .025h) * 12.0h)
                    * saturate((atlas.g - atlas.r * .9h) * 16.0h)
                    * saturate((n.y - .7h) * 5.0h);
                // Town's source FBX is centimetres, Z-up. Object-space UVs stay
                // attached to the map when the track-selection preview rotates/scales.
                float2 uv = i.positionOS.xy * .01 / max(_GrassScale, .1);
                half3 grass = SAMPLE_TEXTURE2D(_GrassMap, sampler_GrassMap, uv).rgb;
                half3 detail = UnpackNormalScale(SAMPLE_TEXTURE2D(_GrassNormal, sampler_GrassNormal, uv), .35h);
                half3 atlasNormal = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv), _BumpScale);
                half3 tangent = normalize(i.tangentWS.xyz);
                half3 bitangent = cross(n, tangent) * i.tangentWS.w;
                half3 mapped = normalize(tangent * atlasNormal.x + bitangent * atlasNormal.y + n * atlasNormal.z);
                half3 groundNormal = TransformObjectToWorldNormal(detail);
                SurfaceData surface = (SurfaceData)0;
                surface.albedo = lerp(atlas, grass * .95h, grassMask * .9h) * _BaseColor.rgb;
                surface.smoothness = _Smoothness;
                surface.occlusion = 1;
                surface.alpha = 1;
                InputData data = (InputData)0;
                data.positionWS = i.positionWS;
                data.positionCS = i.positionCS;
                data.normalWS = normalize(lerp(mapped, groundNormal, grassMask));
                data.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                data.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                data.bakedGI = SampleSH(data.normalWS);
                data.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                data.shadowMask = 1;
                half4 color = UniversalFragmentPBR(data, surface);
                color.rgb = MixFog(color.rgb, i.fog);
                return color;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
