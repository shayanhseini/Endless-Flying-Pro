Shader "Universal Render Pipeline/Custom/PlanetAtmosphere"
{
    Properties
    {
        [HDR] _AtmosphereColor ("Atmosphere Color", Color) = (0.3, 0.65, 1.0, 1.0)
        [HDR] _SunColor ("Sunlit Rim Tint", Color) = (1.0, 0.85, 0.6, 1.0)
        _Intensity ("Atmosphere Intensity", Range(0.1, 5.0)) = 1.5
        _Falloff ("Limb Falloff (Power)", Range(0.5, 8.0)) = 3.5
        _SunWrap ("Sunlight Wrap", Range(0.0, 1.0)) = 0.4
        _InnerAlpha ("Inner Transparency", Range(0.0, 1.0)) = 0.0
        _OuterAlpha ("Outer Peak Alpha", Range(0.1, 1.0)) = 0.85
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
            "IgnoreProjector"="True" 
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

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
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _AtmosphereColor;
                float4 _SunColor;
                float _Intensity;
                float _Falloff;
                float _SunWrap;
                float _InnerAlpha;
                float _OuterAlpha;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);

                // View Fresnel (limb brightness)
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float limb = pow(1.0 - NdotV, _Falloff);

                // Sun illumination
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float NdotL = dot(normalWS, lightDir);
                float sunFactor = saturate((NdotL + _SunWrap) / (1.0 + _SunWrap));

                // Color blending: sunset warm tint on sunlit edge + atmosphere ambient
                float3 finalColor = lerp(_AtmosphereColor.rgb, _SunColor.rgb * _AtmosphereColor.rgb, sunFactor * 0.5);
                finalColor *= _Intensity * (0.2 + 0.8 * sunFactor);

                float alpha = lerp(_InnerAlpha, _OuterAlpha, limb) * limb;
                alpha = saturate(alpha * (0.15 + 0.85 * sunFactor));

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
