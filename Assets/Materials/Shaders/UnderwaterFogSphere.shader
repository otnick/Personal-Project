Shader "Custom/UnderwaterFogSphere"
{
    Properties
    {
        _Color ("Fog Color", Color) = (0.05, 0.25, 0.3, 1)
        _Density ("Density", Range(0, 5)) = 1.2
        _EdgeFade ("Edge Fade", Range(0, 10)) = 2.0
        _NoiseTex ("Noise (optional)", 2D) = "white" {}
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.15
        _NoiseScale ("Noise Scale", Range(0.1, 10)) = 1.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        // Innen rendern:
        Cull Front
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;

            float4 _Color;
            float _Density;
            float _EdgeFade;
            float _NoiseStrength;
            float _NoiseScale;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Distanz von Kamera zu Fragment (innerhalb der Kugel)
                float dist = distance(_WorldSpaceCameraPos, i.worldPos);

                // Haupt-Fog: dist -> alpha
                float a = saturate(1.0 - exp(-dist * _Density));

                // Edge-Fade: macht Rand weicher (weniger harte Kugelkante)
                // Je größer die Kugel, desto stärker kann EdgeFade sein.
                float edge = saturate(dist / max(_EdgeFade, 1e-4));
                a *= edge;

                // Optional Noise (sehr subtil, VR-freundlich)
                float2 nuv = i.uv * _NoiseScale;
                float n = tex2D(_NoiseTex, nuv).r;
                a *= lerp(1.0, n, _NoiseStrength);

                fixed4 col = _Color;
                col.a = a;

                return col;
            }
            ENDHLSL
        }
    }
}
