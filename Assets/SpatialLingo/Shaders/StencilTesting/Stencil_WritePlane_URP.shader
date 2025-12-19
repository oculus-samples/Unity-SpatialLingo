// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "Stencil/Write_NoDepth_DoubleSided_BG_Radius"
{
    Properties
    {
        [IntRange]_StencilRef ("Stencil Ref", Range(0,255)) = 1

        // Center and radius (object space)
        _CenterOS       ("Center (Object Space)", Vector) = (0,0,0,0)
        _InnerRadiusOS  ("Inner Radius (Object Space)", Float) = 0.40
        _Feather        ("Feather (soft edge)", Range(0,1)) = 0.0

        // Axis (measurement plane): 0=XY, 1=XZ (default ground plane), 2=YZ
        [KeywordEnum(XY, XZ, YZ)] _PLANEAXIS ("Mask Plane Axis", Float) = 1
    }
    SubShader
    {
        // Before the sky and geometry
        Tags { "RenderType"="Opaque" "Queue"="Background+10" }

        // Only writes to the stencil buffer, not to the color/depth buffer; double-sided; always passes the depth test.
        ColorMask 0
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            // Important: Place the Stencil operation inside the Pass
            // so that clipped fragments are not written to the stencil buffer.
            Stencil
            {
                Ref [_StencilRef]
                Comp Always
                Pass Replace
                Fail Replace
                ZFail Replace
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _PLANEAXIS_XY _PLANEAXIS_XZ _PLANEAXIS_YZ

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _CenterOS;
            float  _InnerRadiusOS;
            float  _Feather;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionHCS : SV_Position; float3 posOS : TEXCOORD0; };

            Varyings vert (Attributes IN)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                o.posOS = IN.positionOS.xyz;
                return o;
            }

            float2 SelectPlane2D(float3 p)
            {
                #if defined(_PLANEAXIS_XY)
                    return p.xy;
                #elif defined(_PLANEAXIS_YZ)
                    return p.yz;
                #else // _PLANEAXIS_XZ (default)
                    return p.xz;
                #endif
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 p2 = SelectPlane2D(i.posOS);
                float2 c2 = SelectPlane2D(_CenterOS.xyz);

                float dist   = length(p2 - c2);
                float radius = _InnerRadiusOS;

                // Soft edge: a gradient from (R - Feather) to R; Feather=0 is equivalent to a hard edge.
                float feather = max(_Feather, 1e-6);
                // Mask: 1 inside the circle, 0 outside the circle.
                float mask = smoothstep(radius, radius - feather, dist);

                // Write the template only within the circle; clip (do not write) anything outside the circle.
                clip(mask - 1e-4);

                // Do not output color.
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
