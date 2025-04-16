Shader "Custom/TessellationShockwave"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _TriplanarScale("Triplanar Scale", Float) = 4.0
        _TessellationEdgeLength("Edge Length", Range(5, 100)) = 50
        [Toggle] _DebugTessellation("Debug Tessellation", Float) = 0
        _TessellationFactor("Tessellation Factor", Range(1, 64)) = 4
        [Toggle] _UseUniformTess("Use Uniform Tessellation", Float) = 0

        [NoScaleOffset]_Layer0_Base("Layer 0 Albedo", 2D) = "white" {}
        [NoScaleOffset]_Layer0_Normal("Layer 0 Normal", 2D) = "bump" {}
        [NoScaleOffset]_Layer0_Roughness("Layer 0 Roughness", 2D) = "white" {}
        [NoScaleOffset]_Layer0_AO("Layer 0 AO", 2D) = "white" {}

        [NoScaleOffset]_Layer1_Base("Layer 1 Albedo", 2D) = "white" {}
        [NoScaleOffset]_Layer1_Normal("Layer 1 Normal", 2D) = "bump" {}
        [NoScaleOffset]_Layer1_Roughness("Layer 1 Roughness", 2D) = "white" {}
        [NoScaleOffset]_Layer1_AO("Layer 1 AO", 2D) = "white" {}

        [NoScaleOffset]_Layer2_Base("Layer 2 Albedo", 2D) = "white" {}
        [NoScaleOffset]_Layer2_Normal("Layer 2 Normal", 2D) = "bump" {}
        [NoScaleOffset]_Layer2_Roughness("Layer 2 Roughness", 2D) = "white" {}
        [NoScaleOffset]_Layer2_AO("Layer 2 AO", 2D) = "white" {}

        [NoScaleOffset]_Layer3_Base("Layer 3 Albedo", 2D) = "white" {}
        [NoScaleOffset]_Layer3_Normal("Layer 3 Normal", 2D) = "bump" {}
        [NoScaleOffset]_Layer3_Roughness("Layer 3 Roughness", 2D) = "white" {}
        [NoScaleOffset]_Layer3_AO("Layer 3 AO", 2D) = "white" {}
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

            TEXTURE2D(_Layer1_Base); SAMPLER(sampler_Layer1_Base);
            TEXTURE2D(_Layer1_Normal); SAMPLER(sampler_Layer1_Normal);
            TEXTURE2D(_Layer1_Roughness); SAMPLER(sampler_Layer1_Roughness);
            TEXTURE2D(_Layer1_AO); SAMPLER(sampler_Layer1_AO);

            TEXTURE2D(_Layer2_Base); SAMPLER(sampler_Layer2_Base);
            TEXTURE2D(_Layer2_Normal); SAMPLER(sampler_Layer2_Normal);
            TEXTURE2D(_Layer2_Roughness); SAMPLER(sampler_Layer2_Roughness);
            TEXTURE2D(_Layer2_AO); SAMPLER(sampler_Layer2_AO);

            TEXTURE2D(_Layer3_Base); SAMPLER(sampler_Layer3_Base);
            TEXTURE2D(_Layer3_Normal); SAMPLER(sampler_Layer3_Normal);
            TEXTURE2D(_Layer3_Roughness); SAMPLER(sampler_Layer3_Roughness);
            TEXTURE2D(_Layer3_AO); SAMPLER(sampler_Layer3_AO);

            float4 _BaseColor;
            float _TriplanarScale;
            float _TessellationEdgeLength;
            float _TessellationFactor;
            float _UseUniformTess;

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

                float3 colorX = 0, colorY = 0, colorZ = 0;
                float totalWeight = 0.0;

                float scales[3];
                scales[0] = scale * 0.5;
                scales[1] = scale;
                scales[2] = scale * 2.0;

                [unroll]
                for (int i = 0; i < 3; i++)
                {
                    float s = scales[i];

                    float2 uvXY = worldPos.xy * s;
                    float2 uvYZ = worldPos.yz * s;
                    float2 uvXZ = worldPos.xz * s;

                    colorX += tex.Sample(smp, uvYZ); // X axis projection
                    colorY += tex.Sample(smp, uvXZ); // Y axis projection
                    colorZ += tex.Sample(smp, uvXY); // Z axis projection

                    totalWeight += 1.0;
                }

                colorX /= totalWeight;
                colorY /= totalWeight;
                colorZ /= totalWeight;

                return colorX * blends.x + colorY * blends.y + colorZ * blends.z;
            }



            half4 MyFragmentProgram(Varyings input) : SV_Target
            {
                InputData inputData = (InputData)0;
                inputData.positionWS = input.worldPos;
                inputData.normalWS = normalize(input.normalWS);
                inputData.viewDirectionWS = normalize(input.viewDirWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.worldPos);


                float3 worldPos = input.worldPos;
                float3 normalWS = normalize(input.normalWS);
                float scale = _TriplanarScale;

                // Slope factor
                float slope = 1.0 - saturate(dot(normalWS, float3(0, 1, 0)));
                float blendVal = slope * 3.0;

                // Sample each layer
                float3 albedo0 = TriplanarSample(_Layer0_Base, sampler_Layer0_Base, worldPos, normalWS, scale);
                float3 albedo1 = TriplanarSample(_Layer1_Base, sampler_Layer1_Base, worldPos, normalWS, scale);
                float3 albedo2 = TriplanarSample(_Layer2_Base, sampler_Layer2_Base, worldPos, normalWS, scale);
                float3 albedo3 = TriplanarSample(_Layer3_Base, sampler_Layer3_Base, worldPos, normalWS, scale);

                float3 normal0 = UnpackNormal(float4(TriplanarSample(_Layer0_Normal, sampler_Layer0_Normal, worldPos, normalWS, scale), 1.0));
                float3 normal1 = UnpackNormal(float4(TriplanarSample(_Layer1_Normal, sampler_Layer1_Normal, worldPos, normalWS, scale), 1.0));
                float3 normal2 = UnpackNormal(float4(TriplanarSample(_Layer2_Normal, sampler_Layer2_Normal, worldPos, normalWS, scale), 1.0));
                float3 normal3 = UnpackNormal(float4(TriplanarSample(_Layer3_Normal, sampler_Layer3_Normal, worldPos, normalWS, scale), 1.0));

                float r0 = TriplanarSample(_Layer0_Roughness, sampler_Layer0_Roughness, worldPos, normalWS, scale).r;
                float r1 = TriplanarSample(_Layer1_Roughness, sampler_Layer1_Roughness, worldPos, normalWS, scale).r;
                float r2 = TriplanarSample(_Layer2_Roughness, sampler_Layer2_Roughness, worldPos, normalWS, scale).r;
                float r3 = TriplanarSample(_Layer3_Roughness, sampler_Layer3_Roughness, worldPos, normalWS, scale).r;

                float ao0 = TriplanarSample(_Layer0_AO, sampler_Layer0_AO, worldPos, normalWS, scale).r;
                float ao1 = TriplanarSample(_Layer1_AO, sampler_Layer1_AO, worldPos, normalWS, scale).r;
                float ao2 = TriplanarSample(_Layer2_AO, sampler_Layer2_AO, worldPos, normalWS, scale).r;
                float ao3 = TriplanarSample(_Layer3_AO, sampler_Layer3_AO, worldPos, normalWS, scale).r;

                // Blend between layers based on slope
                float3 albedo;
                float3 normalTS;
                float roughness;
                float ao;

                if (blendVal < 1.0)
                {
                    float t = blendVal;
                    albedo = lerp(albedo0, albedo1, t);
                    normalTS = normalize(lerp(normal0, normal1, t));
                    roughness = lerp(r0, r1, t);
                    ao = lerp(ao0, ao1, t);
                }
                else if (blendVal < 2.0)
                {
                    float t = blendVal - 1.0;
                    albedo = lerp(albedo1, albedo2, t);
                    normalTS = normalize(lerp(normal1, normal2, t));
                    roughness = lerp(r1, r2, t);
                    ao = lerp(ao1, ao2, t);
                }
                else
                {
                    float t = blendVal - 2.0;
                    albedo = lerp(albedo2, albedo3, t);
                    normalTS = normalize(lerp(normal2, normal3, t));
                    roughness = lerp(r2, r3, t);
                    ao = lerp(ao2, ao3, t);
                }


                SurfaceData surfaceData;
                surfaceData.albedo = albedo * _BaseColor.rgb;
                surfaceData.metallic = 0.0;
                surfaceData.specular = 0.0;
                surfaceData.smoothness = 1.0 - roughness;
                surfaceData.normalTS = normalTS;
                surfaceData.emission = input.emission;
                surfaceData.occlusion = ao;
                surfaceData.alpha = 1.0;
                surfaceData.clearCoatMask = 0;
                surfaceData.clearCoatSmoothness = 0;

                half4 color = UniversalFragmentBlinnPhong(inputData, surfaceData);
                float shadow = MainLightRealtimeShadow(inputData.shadowCoord);
                color.rgb *= shadow;

                #if defined(_DEBUG_TESSELLATION)
                float edgeHighlight = Wireframe(input.baryCoords);
                color.rgb = lerp(color.rgb, float3(1, 0, 0), saturate(edgeHighlight * 4.0));
                #endif

                return color;
            }

            ENDHLSL
        }
    }
    FallBack "Diffuse"
}