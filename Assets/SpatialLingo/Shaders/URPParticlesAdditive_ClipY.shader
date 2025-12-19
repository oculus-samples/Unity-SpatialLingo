// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "URP/Particles/Additive_ClipY_Fixed"
{
    Properties
    {
        _BaseMap  ("Particle Texture", 2D) = "white" {}
        _BaseColor("Tint", Color) = (1,1,1,1)

        _PortalY  ("Clip Height (World Y)", Float) = 1.0
        _Feather  ("Feather (World Units)", Float) = 0.20
    }
    SubShader
    {
        Tags{
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }

        // Transparent additive blending (uses alpha channel)
        // SrcAlpha: particle alpha
      
        Blend SrcAlpha OneMinusSrcAlpha

        // Double-sided (Cull Off), no depth write
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float  _PortalY;
            float  _Feather;

            struct Attributes {
                float4 positionOS: POSITION;
                float2 uv        : TEXCOORD0;
                float4 color     : COLOR;
            };

            struct Varyings {
                float4 positionHCS: SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                float3 positionWS : TEXCOORD1;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionHCS = TransformWorldToHClip(posWS);
                o.uv    = TRANSFORM_TEX(v.uv, _BaseMap);
                o.color = v.color * _BaseColor;
                o.positionWS = posWS;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // Distance above/below the clipping height
                float d = _PortalY - i.positionWS.y;

                // Feather region for smooth fade (avoid hard cut)
                float feather = max(_Feather, 1e-5);
                float a = saturate(smoothstep(0.0, feather, d));

                // Discard pixels above the clip height
                if (a <= 0.0001) discard;

                // Sample texture and apply vertex color * tint
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * i.color;

                // Multiply by feather alpha for smooth fade
                c.rgb *= a;
                c.a    = a; // used for blending (SrcAlpha One)
                return c;
            }
            ENDHLSL
        }
    }
}
