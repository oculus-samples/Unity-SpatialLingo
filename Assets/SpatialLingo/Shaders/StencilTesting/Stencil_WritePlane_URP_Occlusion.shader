Shader "Stencil/Write_NoDepth_Occlusion_DoubleSided_BG_Radius"
{
    Properties
    {
        [IntRange]_StencilRef ("Stencil Ref", Range(0,255)) = 1

        // Center and radius (object space)
        _CenterOS       ("Center (Object Space)", Vector) = (0,0,0,0)
        _InnerRadiusOS  ("Inner Radius (Object Space)", Float) = 0.40
        _Feather        ("Feather (soft edge)", Range(0,1)) = 0.0

        _EnvironmentDepthBias ("Environment Depth Bias", Float) = 0.0

        // Axis (distance measurement plane): 0=XY, 1=XZ (default ground), 2=YZ
        [KeywordEnum(XY, XZ, YZ)] _PLANEAXIS ("Mask Plane Axis", Float) = 1
    }
    SubShader
    {
        // Prior to the sky and geometry
        Tags { "RenderType"="Opaque" "Queue"="Background+10" }

        // Only specify the template, not the color/depth; double-sided; always passes the depth test.
        ColorMask 0
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            //Important: Place the Stencil inside the Pass,
            // This way, clipped fragments won't be written to the stencil.
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
            #pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION

            #include "Assets/SpatialLingo/Shaders/MetaOcclusion/EnvironmentOcclusionURP.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _CenterOS;
            float  _InnerRadiusOS;
            float  _Feather;
            float  _EnvironmentDepthBias;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   
            { 
                float4 positionHCS : SV_Position; 
                float3 posOS : TEXCOORD0; 
                float3 posWorld : TEXCOORD1;   // World position variable must have this name to work with EnvironmentDepth Macros
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert (Attributes IN)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                o.posOS = IN.positionOS.xyz;

                //Occlusion macro
                META_DEPTH_INITIALIZE_VERTEX_OUTPUT(o, IN.positionOS.xyz);

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
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float2 p2 = SelectPlane2D(i.posOS);
                float2 c2 = SelectPlane2D(_CenterOS.xyz);

                float dist   = length(p2 - c2);
                float radius = _InnerRadiusOS;

                // Soft edges: Gradient from (R - Feather) to R; Feather=0 is equivalent to a hard edge.
                float feather = max(_Feather, 1e-6);
                // mask：The value inside the circle is 1, and the value outside the circle is 0.
                float mask = smoothstep(radius, radius - feather, dist);

                float4 depthColor = (1,1,1,1);

                //Occlusuion Macro
                META_DEPTH_OCCLUDE_OUTPUT_PREMULTIPLY(i, depthColor, _EnvironmentDepthBias);

                //mult mask * depthColor alpha to make new alpha
                mask *= (1-depthColor.a); 

                // Write the template only inside the circle; leave the template outside the circle as a clip (don't write it).
                clip(mask - 1e-4);

                // No color output
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
