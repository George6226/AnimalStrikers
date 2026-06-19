Shader "Custom/RingShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _EmissionColor ("Emission Color", Color) = (1,1,1,1)
        _EmissionPower ("Emission Power", Range(0,10)) = 1
        _RingThickness ("Ring Thickness", Range(0,1)) = 0.1
        _RingRadius ("Ring Radius", Range(0,1)) = 0.4
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _Color;
            float4 _EmissionColor;
            float _EmissionPower;
            float _RingThickness;
            float _RingRadius;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 center = float2(0.5, 0.5);
                float dist = distance(i.uv, center);
                
                // リングの半径を考慮した計算
                float ring = 1 - abs(_RingRadius - dist) / _RingThickness;
                ring = saturate(ring);
                
                // 発光色を計算
                float4 col = _Color * ring;
                col.rgb += _EmissionColor.rgb * _EmissionPower * ring;
                col.a *= ring;
                
                return col;
            }
            ENDCG
        }
    }
} 