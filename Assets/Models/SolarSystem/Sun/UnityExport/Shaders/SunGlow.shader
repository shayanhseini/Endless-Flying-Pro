Shader "SolarSystem/SunGlow"
{
    // Corona / glow shell (port of the Blender Sun_Glow material).
    // Put it on a sphere that is 1.25x the Sun's radius (that is what Sun.fbx contains).
    // Works in Built-in and URP. Not HDRP.
    Properties
    {
        _GlowColor ("Glow Color", Color) = (1, 0.5, 0.1, 1)
        _Intensity ("Emission Intensity", Float) = 1.6
        _Alpha ("Max Alpha", Range(0, 1)) = 0.55
        _Falloff ("Falloff Power", Float) = 3.8
        _SunRatio ("Sun Radius / Glow Radius", Range(0.5, 0.99)) = 0.8
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _GlowColor;
            float _Intensity;
            float _Alpha;
            float _Falloff;
            float _SunRatio;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 nrm : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.nrm = UnityObjectToWorldNormal(v.normal);
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = _WorldSpaceCameraPos - wp;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.nrm);
                float3 v = normalize(i.viewDir);
                float facing = saturate(abs(dot(n, v)));
                float s = sqrt(saturate(1.0 - facing * facing)); // 0 at the centre, 1 at the silhouette

                // hide the part that overlaps the Sun's disc, then fade out towards the rim
                float mask = smoothstep(_SunRatio - 0.002, _SunRatio + 0.018, s);
                float fall = pow(saturate(1.0 - (s - _SunRatio) / (1.0 - _SunRatio)), _Falloff);
                float a = saturate(mask * fall * _Alpha);

                return float4(_GlowColor.rgb * _Intensity, a);
            }
            ENDCG
        }
    }
}
