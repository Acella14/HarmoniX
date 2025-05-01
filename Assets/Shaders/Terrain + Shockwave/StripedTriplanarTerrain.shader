Shader "Custom/StripedTriplanarTerrain"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)

        [NoScaleOffset]_Albedo0("Albedo 0", 2D) = "white" {}
        [NoScaleOffset]_Albedo1("Albedo 1", 2D) = "white" {}
        [NoScaleOffset]_Albedo2("Albedo 2", 2D) = "white" {}

        [NoScaleOffset]_Normal0("Normal 0", 2D) = "bump" {}
        [NoScaleOffset]_Normal1("Normal 1", 2D) = "bump" {}
        [NoScaleOffset]_Normal2("Normal 2", 2D) = "bump" {}

        _TriplanarScale("Triplanar Scale", Float) = 4.0
        _StripeDensity("Stripe Frequency", Float) = 0.2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            ZWrite On
            Cull Back
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos   : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 viewDirWS  : TEXCOORD2;
            };

            TEXTURE2D(_Albedo0); SAMPLER(sampler_Albedo0);
            TEXTURE2D(_Albedo1); SAMPLER(sampler_Albedo1);
            TEXTURE2D(_Albedo2); SAMPLER(sampler_Albedo2);

            TEXTURE2D(_Normal0); SAMPLER(sampler_Normal0);
            TEXTURE2D(_Normal1); SAMPLER(sampler_Normal1);
            TEXTURE2D(_Normal2); SAMPLER(sampler_Normal2);

            float4 _BaseColor;
            float _TriplanarScale;
            float _StripeDensity;


            Varyings vert(Attributes input)
            {
                Varyings o;
                float3 worldPos = mul(unity_ObjectToWorld, input.positionOS).xyz;
                o.positionCS = mul(UNITY_MATRIX_VP, float4(worldPos, 1));
                o.worldPos = worldPos;
                o.normalWS = normalize(mul((float3x3)unity_ObjectToWorld, input.normalOS));
                o.viewDirWS = normalize(_WorldSpaceCameraPos - o.worldPos);
                return o;
            }

            float3 TriplanarSample(Texture2D tex, SamplerState smp, float3 worldPos, float3 normalVec, float scale)
            {
                float3 blend = abs(normalVec);
                blend = pow(blend, 4.0);
                blend /= dot(blend, 1.0);

                float2 uvX = worldPos.yz / scale;
                float2 uvY = worldPos.xz / scale;
                float2 uvZ = worldPos.xy / scale;

                float3 colX = tex.Sample(smp, uvX);
                float3 colY = tex.Sample(smp, uvY);
                float3 colZ = tex.Sample(smp, uvZ);

                return colX * blend.x + colY * blend.y + colZ * blend.z;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);

                // Stripe logic
                float stripePos = input.worldPos.y * _StripeDensity;
                float stripeIndex = fmod(stripePos, 3.0);

                float3 col0 = TriplanarSample(_Albedo0, sampler_Albedo0, input.worldPos, normalWS, _TriplanarScale);
                float3 col1 = TriplanarSample(_Albedo1, sampler_Albedo1, input.worldPos, normalWS, _TriplanarScale);
                float3 col2 = TriplanarSample(_Albedo2, sampler_Albedo2, input.worldPos, normalWS, _TriplanarScale);

                float w0 = 1.0 - saturate(abs(stripeIndex - 0.0));
                float w1 = 1.0 - saturate(abs(stripeIndex - 1.0));
                float w2 = 1.0 - saturate(abs(stripeIndex - 2.0));
                float total = w0 + w1 + w2 + 1e-5;

                float3 albedo = (col0 * w0 + col1 * w1 + col2 * w2) / total;

                float3 n0 = UnpackNormal(float4(TriplanarSample(_Normal0, sampler_Normal0, input.worldPos, normalWS, _TriplanarScale), 1));
                float3 n1 = UnpackNormal(float4(TriplanarSample(_Normal1, sampler_Normal1, input.worldPos, normalWS, _TriplanarScale), 1));
                float3 n2 = UnpackNormal(float4(TriplanarSample(_Normal2, sampler_Normal2, input.worldPos, normalWS, _TriplanarScale), 1));
                float3 normal = normalize((n0 * w0 + n1 * w1 + n2 * w2) / total);

                // Lighting
                InputData inputData = (InputData)0;
                inputData.positionWS = input.worldPos;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.shadowCoord = TransformWorldToShadowCoord(input.worldPos);

                SurfaceData surface;
                surface.albedo = albedo * _BaseColor.rgb;
                surface.normalTS = normal;
                surface.metallic = 0;
                surface.specular = 0;
                surface.smoothness = 0.5;
                surface.occlusion = 1;
                surface.emission = 0;
                surface.alpha = 1;

                return UniversalFragmentBlinnPhong(inputData, surface);
            }


            ENDHLSL
        }
    }
    FallBack "Diffuse"
}
