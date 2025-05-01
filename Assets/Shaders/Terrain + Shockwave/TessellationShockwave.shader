Shader "Custom/TessellationShockwave"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _TriplanarScale0("Triplanar Scale - Layer 0", Float) = 4.0
        _TriplanarScale0Var("Triplanar Scale - Layer 0 Variant", Float) = 4.0
        _TriplanarScale1("Triplanar Scale - Layer 1", Float) = 4.0
        _TriplanarScale2("Triplanar Scale - Layer 2", Float) = 4.0
        _HeightScale("Height Strength", Range(0,1)) = 0.1
        _TessellationEdgeLength("Edge Length", Range(5, 100)) = 50
        [Toggle] _DebugTessellation("Debug Tessellation", Float) = 0
        _TessellationFactor("Tessellation Factor", Range(1, 64)) = 4
        [Toggle] _UseUniformTess("Use Uniform Tessellation", Float) = 0
        [NoScaleOffset]_NoiseTex("Noise Texture", 2D) = "white" {}
        _NoiseScale("Noise Scale", Float) = 0.2
        _NoiseProjectionCenter("Noise Projection Center", Vector) = (0, 0, 0, 0)
        _NoiseProjectionSize("Noise Projection Size", Float) = 1000
        _NoiseUVOffset("Noise UV Offset", Vector) = (0, 0, 0, 0)
        _BlendAmount("Variant Blend Threshold", Range(0, 1)) = 0.5

        [NoScaleOffset]_Layer0_Base("Layer 0 Albedo", 2D) = "white" {}
        [NoScaleOffset]_Layer0_Normal("Layer 0 Normal", 2D) = "bump" {}
        [NoScaleOffset]_Layer0_Roughness("Layer 0 Roughness", 2D) = "white" {}
        [NoScaleOffset]_Layer0_AO("Layer 0 AO", 2D) = "white" {}
        [NoScaleOffset]_Layer0_Height("Layer 0 Height", 2D) = "gray" {}

        [NoScaleOffset]_Layer0_Base2("Layer 0 Variant Albedo", 2D) = "white" {}
        [NoScaleOffset]_Layer0_Normal2("Layer 0 Variant Normal", 2D) = "bump" {}
        [NoScaleOffset]_Layer0_Height2("Layer 0 Variant Height", 2D) = "gray" {}



        [NoScaleOffset]_Layer1_Base("Layer 1 Albedo", 2D) = "white" {}
        [NoScaleOffset]_Layer1_Normal("Layer 1 Normal", 2D) = "bump" {}


        [NoScaleOffset]_Layer2_Base("Layer 2 Albedo", 2D) = "white" {}
        [NoScaleOffset]_Layer2_Normal("Layer 2 Normal", 2D) = "bump" {}

        [NoScaleOffset]_RoadMask("Road Mask", 2D) = "black" {}
        _RoadMaskScale("Road Mask Scale", Float) = 1.0

        [NoScaleOffset]_Road_Albedo("Road Albedo", 2D) = "white" {}
        [NoScaleOffset]_Road_Normal("Road Normal", 2D) = "bump" {}
        [NoScaleOffset]_Road_Roughness("Road Roughness", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            Cull Back
            Blend One Zero

            HLSLPROGRAM

            #pragma target 4.6

            #pragma vertex MyTessellationVertexProgram
            #pragma hull MyHullProgram
            #pragma domain MyDomainProgram
            #pragma fragment MyFragmentProgram

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS

            #pragma shader_feature_local _DEBUG_TESSELLATION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "TessellationShockwave.cginc"

            TEXTURE2D(_Layer0_Base); SAMPLER(sampler_Layer0_Base);
            TEXTURE2D(_Layer0_Normal); SAMPLER(sampler_Layer0_Normal);
            TEXTURE2D(_Layer0_Roughness); SAMPLER(sampler_Layer0_Roughness);
            TEXTURE2D(_Layer0_AO); SAMPLER(sampler_Layer0_AO);
            TEXTURE2D(_Layer0_Height); SAMPLER(sampler_Layer0_Height);

            TEXTURE2D(_Layer0_Base2); SAMPLER(sampler_Layer0_Base2);
            TEXTURE2D(_Layer0_Normal2); SAMPLER(sampler_Layer0_Normal2);
            TEXTURE2D(_Layer0_Height2); SAMPLER(sampler_Layer0_Height2);


            TEXTURE2D(_Layer1_Base); SAMPLER(sampler_Layer1_Base);
            TEXTURE2D(_Layer1_Normal); SAMPLER(sampler_Layer1_Normal);


            TEXTURE2D(_Layer2_Base); SAMPLER(sampler_Layer2_Base);
            TEXTURE2D(_Layer2_Normal); SAMPLER(sampler_Layer2_Normal);

            TEXTURE2D(_RoadMask); SAMPLER(sampler_RoadMask);
            TEXTURE2D(_Road_Albedo); SAMPLER(sampler_Road_Albedo);
            TEXTURE2D(_Road_Normal); SAMPLER(sampler_Road_Normal);
            TEXTURE2D(_Road_Roughness); SAMPLER(sampler_Road_Roughness);


            float4 _BaseColor;
            float _TriplanarScale0;
            float _TriplanarScale0Var;
            float _TriplanarScale1;
            float _TriplanarScale2;
            float _HeightScale;
            float _TessellationEdgeLength;
            float _TessellationFactor;
            float _UseUniformTess;

            float _NoiseScale;
            float4 _NoiseProjectionCenter;
            float _NoiseProjectionSize;
            float _BlendAmount;
            float4 _NoiseUVOffset;
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            float _RoadMaskScale;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct TessellationControlPoint
            {
                float4 vertex : INTERNALTESSPOS;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct TessellationFactors
            {
                float edge[3] : SV_TessFactor;
                float inside : SV_InsideTessFactor;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float3 emission : TEXCOORD3;
                float3 baryCoords : TEXCOORD4;
            };

            float TessellationEdgeFactor(float3 p0, float3 p1)
            {
                if (_UseUniformTess > 0.5)
                    return _TessellationFactor;
                float edgeLength = distance(p0, p1);
                float3 center = (p0 + p1) * 0.5;
                float viewDistance = distance(center, _WorldSpaceCameraPos);
                return edgeLength * _ScreenParams.y / (_TessellationEdgeLength * viewDistance);
            }

            float Wireframe(float3 bary)
            {
                float minDist = min(min(bary.x, bary.y), bary.z);
                float lineWidth = 0.05;
                return smoothstep(0.0, lineWidth, minDist);
            }

            TessellationControlPoint MyTessellationVertexProgram(Attributes v)
            {
                TessellationControlPoint o;
                o.vertex = v.positionOS;
                o.normal = v.normalOS;
                o.uv = v.uv;
                return o;
            }

            [domain("tri")]
            [outputcontrolpoints(3)]
            [outputtopology("triangle_cw")]
            [partitioning("fractional_odd")]
            [patchconstantfunc("MyPatchConstantFunction")]
            TessellationControlPoint MyHullProgram(InputPatch<TessellationControlPoint, 3> patch, uint id : SV_OutputControlPointID)
            {
                return patch[id];
            }

            TessellationFactors MyPatchConstantFunction(InputPatch<TessellationControlPoint, 3> patch)
            {
                float3 p0 = mul(unity_ObjectToWorld, patch[0].vertex).xyz;
                float3 p1 = mul(unity_ObjectToWorld, patch[1].vertex).xyz;
                float3 p2 = mul(unity_ObjectToWorld, patch[2].vertex).xyz;

                TessellationFactors f;
                f.edge[0] = TessellationEdgeFactor(p1, p2);
                f.edge[1] = TessellationEdgeFactor(p2, p0);
                f.edge[2] = TessellationEdgeFactor(p0, p1);
                f.inside = (f.edge[0] + f.edge[1] + f.edge[2]) / 3.0;
                return f;
            }

            [domain("tri")]
            Varyings MyDomainProgram(TessellationFactors factors, OutputPatch<TessellationControlPoint, 3> patch, float3 bary : SV_DomainLocation)
            {
                Varyings o;
                float3 posOS = patch[0].vertex.xyz * bary.x + patch[1].vertex.xyz * bary.y + patch[2].vertex.xyz * bary.z;
                float3 displacement;
                float3 emission;
                ApplyShockwaves_float(posOS, displacement, emission);
                float3 displaced = posOS + displacement;

                float4 posWS = mul(unity_ObjectToWorld, float4(displaced, 1.0));
                o.positionCS = mul(UNITY_MATRIX_VP, posWS);
                o.worldPos = posWS.xyz;

                float3 normalOS = normalize(
                    patch[0].normal * bary.x +
                    patch[1].normal * bary.y +
                    patch[2].normal * bary.z);

                o.normalWS = normalize(mul((float3x3)unity_ObjectToWorld, normalOS));
                o.viewDirWS = normalize(_WorldSpaceCameraPos - o.worldPos);
                o.emission = emission;
                o.baryCoords = bary;
                return o;
            }

            
            float3 TriplanarSample(Texture2D tex, SamplerState smp, float3 worldPos, float3 normalVec, float scale)
            {
                float3 blends = abs(normalVec);
                blends = pow(blends, 2.0);
                blends /= (blends.x + blends.y + blends.z + 1e-5);

                float invScale = 1.0 / scale;
                float2 uvX = worldPos.yz * invScale;
                float2 uvY = worldPos.xz * invScale;
                float2 uvZ = worldPos.xy * invScale;

                float3 x = tex.Sample(smp, uvX);
                float3 y = tex.Sample(smp, uvY);
                float3 z = tex.Sample(smp, uvZ);

                return x * blends.x + y * blends.y + z * blends.z;
            }


            half4 MyFragmentProgram(Varyings input) : SV_Target
            {
                InputData inputData = (InputData)0;
                inputData.positionWS      = input.worldPos;
                inputData.normalWS        = normalize(input.normalWS);
                inputData.viewDirectionWS = normalize(input.viewDirWS);
                inputData.shadowCoord     = TransformWorldToShadowCoord(input.worldPos);

                float3 worldPos   = input.worldPos;
                float3 normalWS   = normalize(input.normalWS);
                float  slope      = 1.0 - saturate(dot(normalWS, float3(0,1,0)));
                float  blendVal   = slope * 2.0;

                float2 halfSize = float2(_NoiseProjectionSize * 0.5, _NoiseProjectionSize * 0.5);
                float2 localXZ = worldPos.xz - (_NoiseProjectionCenter.xz - halfSize);
                float2 noiseUV = ((localXZ / _NoiseProjectionSize) * _NoiseScale) + _NoiseUVOffset.xy;

                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
                float blendStart = _BlendAmount - 0.1;
                float blendEnd   = _BlendAmount + 0.1;
                float variantBlend = smoothstep(blendStart, blendEnd, noise);

                float h0 = lerp(
                    TriplanarSample(_Layer0_Height, sampler_Layer0_Height, worldPos, normalWS, _TriplanarScale0).r,
                    TriplanarSample(_Layer0_Height2, sampler_Layer0_Height2, worldPos, normalWS, _TriplanarScale0Var).r,
                    variantBlend);

                float height;
                float h1 = 0.5;
                float h2 = 0.5;

                if (blendVal < 1.0)
                    height = lerp(h0, h1, blendVal);
                else
                    height = lerp(h1, h2, blendVal - 1.0);

                float remappedHeight = (height - 0.5) * 2.0;
                float3 parallaxPos = worldPos + inputData.viewDirectionWS * (remappedHeight * _HeightScale);

                float3 albedo0 = lerp(
                    TriplanarSample(_Layer0_Base, sampler_Layer0_Base, parallaxPos, normalWS, _TriplanarScale0),
                    TriplanarSample(_Layer0_Base2, sampler_Layer0_Base2, parallaxPos, normalWS, _TriplanarScale0Var),
                    variantBlend);
                float3 albedo1 = TriplanarSample(_Layer1_Base, sampler_Layer1_Base, parallaxPos, normalWS, _TriplanarScale1);
                float3 albedo2 = TriplanarSample(_Layer2_Base, sampler_Layer2_Base, parallaxPos, normalWS, _TriplanarScale2);

                float3 normal0 = UnpackNormal(float4(lerp(
                    TriplanarSample(_Layer0_Normal, sampler_Layer0_Normal, parallaxPos, normalWS, _TriplanarScale0),
                    TriplanarSample(_Layer0_Normal2, sampler_Layer0_Normal2, parallaxPos, normalWS, _TriplanarScale0Var),
                    variantBlend), 1.0));
                float3 normal1 = UnpackNormal(float4(TriplanarSample(_Layer1_Normal, sampler_Layer1_Normal, parallaxPos, normalWS, _TriplanarScale1), 1.0));
                float3 normal2 = UnpackNormal(float4(TriplanarSample(_Layer2_Normal, sampler_Layer2_Normal, parallaxPos, normalWS, _TriplanarScale2), 1.0));

                float r0 = TriplanarSample(_Layer0_Roughness, sampler_Layer0_Roughness, parallaxPos, normalWS, _TriplanarScale0).r;
                float r1 = 0.5;
                float r2 = 0.5;

                float ao0 = TriplanarSample(_Layer0_AO, sampler_Layer0_AO, parallaxPos, normalWS, _TriplanarScale0).r;
                float ao1 = 1.0;
                float ao2 = 1.0;

                float3 albedo;
                float3 normalTS;
                float roughness;
                float ao;

                if (blendVal < 1.0)
                {
                    float t = blendVal;
                    albedo    = lerp(albedo0, albedo1, t);
                    normalTS  = normalize(lerp(normal0, normal1, t));
                    roughness = lerp(r0, r1, t);
                    ao        = lerp(ao0, ao1, t);
                }
                else
                {
                    float t = blendVal - 1.0;
                    albedo    = lerp(albedo1, albedo2, t);
                    normalTS  = normalize(lerp(normal1, normal2, t));
                    roughness = lerp(r1, r2, t);
                    ao        = lerp(ao1, ao2, t);
                }

                // Road mask sampling
                float2 roadUV = ((localXZ / _NoiseProjectionSize) * _RoadMaskScale);
                roadUV.xy = 1.0 - roadUV.xy;

                roadUV = saturate(roadUV);
                float roadMask = SAMPLE_TEXTURE2D(_RoadMask, sampler_RoadMask, roadUV).r;
                roadMask = smoothstep(0.3, 0.7, roadMask); // Optional smoothing

                // Sample road textures
                float3 roadAlbedo = TriplanarSample(_Road_Albedo, sampler_Road_Albedo, parallaxPos, normalWS, _TriplanarScale0);
                float3 roadNormal = UnpackNormal(float4(TriplanarSample(_Road_Normal, sampler_Road_Normal, parallaxPos, normalWS, _TriplanarScale0), 1.0));
                float roadRoughness = TriplanarSample(_Road_Roughness, sampler_Road_Roughness, parallaxPos, normalWS, _TriplanarScale0).r;

                // Blend in road
                albedo    = lerp(albedo, roadAlbedo, roadMask);
                normalTS  = normalize(lerp(normalTS, roadNormal, roadMask));
                roughness = lerp(roughness, roadRoughness, roadMask);

                SurfaceData surfaceData;
                surfaceData.albedo            = albedo * _BaseColor.rgb;
                surfaceData.metallic          = 0.0;
                surfaceData.specular          = 0.0;
                surfaceData.smoothness        = 1.0 - roughness;
                surfaceData.normalTS          = normalTS;
                surfaceData.emission          = input.emission;
                surfaceData.occlusion         = ao;
                surfaceData.alpha             = 1.0;
                surfaceData.clearCoatMask     = 0;
                surfaceData.clearCoatSmoothness = 0;

                half4 color = UniversalFragmentBlinnPhong(inputData, surfaceData);
                color.rgb *= MainLightRealtimeShadow(inputData.shadowCoord);

                #if defined(_DEBUG_TESSELLATION)
                    float edgeHighlight = Wireframe(input.baryCoords);
                    color.rgb = lerp(color.rgb, float3(1,0,0), saturate(edgeHighlight * 4.0));
                #endif

                return color;
            }



            ENDHLSL
        }
    }
    FallBack "Diffuse"
}