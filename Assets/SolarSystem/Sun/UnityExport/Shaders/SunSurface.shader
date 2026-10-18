Shader "SolarSystem/SunSurface"
{
    // Unlit emissive Sun with limb darkening (port of the Blender Sun + Sun_LimbDarkening materials).
    // Works in Built-in and URP. Not HDRP.
    Properties
    {
        _MainTex ("Sun Emission (sRGB)", 2D) = "white" {}
        _Intensity ("Emission Intensity", Float) = 1.35
        _LimbColor ("Limb Color", Color) = (0.55, 0.12, 0.01, 1)
        _LimbEmission ("Limb Emission", Float) = 0.9
        _LimbStrength ("Limb Strength", Range(0, 1)) = 0.85
        _LimbPower ("Limb Power", Float) = 2
        _LimbEdgeFade ("Limb Edge Fade", Range(0.001, 0.5)) = 0.07
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Back
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Intensity;
            float4 _LimbColor;
            float _LimbEmission;
            float _LimbStrength;
            float _LimbPower;
            float _LimbEdgeFade;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 nrm : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.nrm = UnityObjectToWorldNormal(v.normal);
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = _WorldSpaceCameraPos - wp;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.nrm);
                float3 v = normalize(i.viewDir);
                float facing = saturate(dot(n, v));

                float3 col = tex2D(_MainTex, i.uv).rgb * _Intensity;

                // limb darkening: reddish, darker rim that fades out at the very edge
                float limb = saturate(pow(1.0 - facing, _LimbPower) * _LimbStrength);
                limb *= saturate(facing / _LimbEdgeFade);
                col = lerp(col, _LimbColor.rgb * _LimbEmission, limb);

                return float4(col, 1.0);
            }
            ENDCG
        }
    }
    FallBack "Unlit/Texture"
}
