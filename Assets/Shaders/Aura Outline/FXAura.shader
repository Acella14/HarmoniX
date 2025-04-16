Shader "FX/Aura Outline"
{
    Properties
    {
        _Color2("Aura Inner Color", Color) = (0, 0, 1, 1)
        _ColorR("Aura Edge Color", Color) = (0, 1, 1, 1)
        _Outline("Outline Width", Range(0.0, 0.2)) = 0.03
        _OutlineZ("Outline Z Offset", Range(-0.1, 0)) = -0.03
        _NoiseTex("Noise Texture", 2D) = "gray" {}
        _Scale("Noise Scale", Range(0.0, 0.4)) = 0.01
        _SpeedX("Speed X", Range(-10, 10)) = 0
        _SpeedY("Speed Y", Range(-10, 10)) = 3
        _Opacity("Noise Opacity", Range(0.01, 10)) = 10
        _Brightness("Brightness", Range(0.5, 3)) = 2
        _Edge("Rim Edge", Range(0, 1)) = 0.1
        _RimPower("Rim Power", Range(0.01, 10)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Cull Front
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 viewDir : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float fogCoord : TEXCOORD3;
            };


            float _Outline;
            float _OutlineZ;
            float _RimPower;
            float _Scale;
            float _SpeedX;
            float _SpeedY;
            float _Opacity;
            float _Edge;
            float _Brightness;

            float4 _Color2;
            float4 _ColorR;
            sampler2D _NoiseTex;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
                float3 camDir = normalize(positionWS - GetCameraPositionWS());

                positionWS += normalWS * _Outline;
                positionWS += -camDir * _OutlineZ;

                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.normal = normalWS;
                OUT.viewDir = normalize(GetCameraPositionWS() - positionWS);
                OUT.worldPos = positionWS;
                OUT.fogCoord = ComputeFogFactor(OUT.positionHCS.z);

                return OUT;
            }


            half4 frag(Varyings IN) : SV_Target
            {
                // --- Sheet-like noise UV ---
                float2 noiseUV;
                noiseUV.x = IN.worldPos.x * _Scale; // horizontal repeat
                noiseUV.y = (IN.worldPos.y + _Time.y * _SpeedY) * _Scale; // vertical scroll

                float noise = tex2D(_NoiseTex, noiseUV).r;

                // --- Invert noise so black areas = visible, white = transparent
                float alphaMask = 1.0 - noise;

                // --- Final color with transparent cutout effect ---
                float4 color = _Color2;
                color.rgb *= _Brightness;
                color.a = alphaMask * _Opacity;

                return color;
            }


            ENDHLSL
        }
    }

    FallBack Off
}
