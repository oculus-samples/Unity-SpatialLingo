// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "Unlit/PassthroughHighlightShader"
{
    Properties
    {
        _HighlightPosition("Highlight Position", Vector) = (0,0,0,0)
        _HighlightRadius("Highlight Radius", Float) = 0.25
        _FadeRadius("Fade Radius", Float) = 0.5
        _HighlightOpacity("Highlight Opacity", Range(0, 1)) = 0.5
        _DimOpacity("Dim Opacity", Range(0, 1)) = 0.25
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            float4 _HighlightPosition;
            float _HighlightRadius;
            float _FadeRadius;
            float _HighlightOpacity;
            float _DimOpacity; // Declare the new variable

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float dist = distance(i.worldPos, _HighlightPosition.xyz);

                // This mask is 1.0 in the highlight center and 0.0 in the dimmed area
                float highlightMask = 1.0 - saturate((dist - _HighlightRadius) / (_FadeRadius - _HighlightRadius));

                // Blend the overlay color from black (for dimming) to white (for highlighting)
                fixed3 finalColor = lerp(fixed3(0, 0, 0), fixed3(1, 1, 1), highlightMask);
                
                // Blend the opacity from the dim level to the highlight level
                float finalAlpha = lerp(_DimOpacity, _HighlightOpacity, highlightMask);

                return fixed4(finalColor, finalAlpha);
            }
            ENDCG
        }
    }
}