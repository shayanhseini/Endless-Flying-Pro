Shader "Custom/Twinkling Star Skybox"
{
    Properties
    {
        [NoScaleOffset] _Tex ("Cubemap", Cube) = "grey" {}
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Exposure ("Exposure", Range(0, 8)) = 1
        _Rotation ("Rotation", Range(0, 360)) = 0
        _RotationSpeed ("Rotation Speed", Range(-2, 2)) = 0.25

        [Header(Stars)]
        _StarThreshold ("White Star Threshold", Range(0, 4)) = 0.72
        _StarSoftness ("Threshold Softness", Range(0.001, 1)) = 0.18
        _StarIntensity ("Star Intensity", Range(0, 12)) = 3.5
        _GlowIntensity ("Glow Intensity", Range(0, 8)) = 1.6
        _GlowSize ("Glow Size", Range(0.0001, 0.01)) = 0.0012
        _TwinkleAmount ("Twinkle Amount", Range(0, 1)) = 0.28
        _TwinkleSpeed ("Twinkle Speed", Range(0, 10)) = 2.2
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            samplerCUBE _Tex;
            half4 _Tex_HDR;
            half4 _Tint;
            half _Exposure;
            half _Rotation;
            half _RotationSpeed;
            half _StarThreshold;
            half _StarSoftness;
            half _StarIntensity;
            half _GlowIntensity;
            half _GlowSize;
            half _TwinkleAmount;
            half _TwinkleSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            float3 RotateAroundY(float3 direction, float degrees)
            {
                float angle = radians(degrees);
                float sine;
                float cosine;
                sincos(angle, sine, cosine);
                return float3(
                    cosine * direction.x - sine * direction.z,
                    direction.y,
                    sine * direction.x + cosine * direction.z);
            }

            v2f vert(appdata input)
            {
                v2f output;
                output.position = UnityObjectToClipPos(input.vertex);
                float animatedRotation = _Rotation + _Time.y * _RotationSpeed;
                output.direction = RotateAroundY(input.vertex.xyz, animatedRotation);
                return output;
            }

            half3 SampleSky(float3 direction)
            {
                return DecodeHDR(texCUBE(_Tex, direction), _Tex_HDR) * _Tint.rgb;
            }

            half StarMask(half3 color)
            {
                half brightest = max(color.r, max(color.g, color.b));
                half whiteness = min(color.r, min(color.g, color.b)) /
                    max(brightest, 0.0001h);
                half brightnessMask = smoothstep(
                    _StarThreshold,
                    _StarThreshold + _StarSoftness,
                    brightest);
                half whiteMask = smoothstep(0.58h, 0.9h, whiteness);
                return brightnessMask * whiteMask;
            }

            float DirectionHash(float3 direction)
            {
                float3 cell = floor((direction * 0.5 + 0.5) * 420.0);
                return frac(sin(dot(cell, float3(12.9898, 78.233, 37.719))) * 43758.5453);
            }

            half4 frag(v2f input) : SV_Target
            {
                float3 direction = normalize(input.direction);
                half3 baseColor = SampleSky(direction);
                half centerStar = StarMask(baseColor);

                float3 helper = abs(direction.y) < 0.95
                    ? float3(0, 1, 0)
                    : float3(1, 0, 0);
                float3 tangent = normalize(cross(helper, direction));
                float3 bitangent = normalize(cross(direction, tangent));

                half nearbyStar = 0;
                nearbyStar = max(nearbyStar, StarMask(SampleSky(normalize(direction + tangent * _GlowSize))));
                nearbyStar = max(nearbyStar, StarMask(SampleSky(normalize(direction - tangent * _GlowSize))));
                nearbyStar = max(nearbyStar, StarMask(SampleSky(normalize(direction + bitangent * _GlowSize))));
                nearbyStar = max(nearbyStar, StarMask(SampleSky(normalize(direction - bitangent * _GlowSize))));

                half glow = saturate(nearbyStar - centerStar * 0.35h);
                float randomPhase = DirectionHash(direction) * 6.2831853;
                half pulse = 1.0h + sin(_Time.y * _TwinkleSpeed + randomPhase) *
                    _TwinkleAmount;

                half3 starColor = lerp(half3(0.72h, 0.82h, 1.0h), half3(1, 1, 1), centerStar);
                half3 emission = starColor *
                    (centerStar * _StarIntensity * pulse + glow * _GlowIntensity);

                return half4((baseColor + emission) * _Exposure, 1);
            }
            ENDCG
        }
    }

    Fallback Off
}
