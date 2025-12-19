// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "Unlit/Grow_Pop_GrassRock_MinCore_Occluded"
{
    Properties
    {
        // --- Base (no color tint, no tiling/offset) ---
        [NoScaleOffset]_BaseMap("Base (RGBA)", 2D) = "white" {}

        // --- Growth (single global 0..1) ---
        _Growth("Growth 0-1", Range(0,1)) = 0

        // --- Pop timing only (axis=+Y, depth is a fixed constant) ---
        _PopStart("Pop Start", Range(0,1)) = 0.25
        _PopEnd("Pop End", Range(0,1)) = 0.65

        // --- AO & Normal (no strengths/scales exposed) ---
        [NoScaleOffset]_AOMap("AO (R)", 2D) = "white" {}
        [NoScaleOffset]_NormalMap("Normal", 2D) = "bump" {}

        // --- Emissive (color + intensity only) ---
        _EmissiveColor("Emissive Color", Color) = (0,0,0,1)
        _EmissiveIntensity("Emissive Intensity", Range(0,10)) = 0

        // --- World Floor Plane Clip (always on, hard clip) ---
        _FloorPlane("Floor Plane (World: a,b,c,d)", Vector) = (0,1,0,0) // e.g. y=0

        _EnvironmentDepthBias ("Environment Depth Bias", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 100

        Cull Off
        ZWrite On
        Blend One Zero
        //Blend One OneMinusSrcAlpha, One OneMinusSrcAlpha

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION
            
            //Change path to local
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"

            #include "Assets/SpatialLingo/Shaders/MetaOcclusion/EnvironmentOcclusionURP.hlsl"
            //#include "Assets/SpatialLingo/Shaders/MetaOcclusion/CalculateEnvironmentDepthOcclusion.hlsl"

            // --- Textures & samplers ---
            TEXTURE2D(_BaseMap);   SAMPLER(sampler_BaseMap);
            TEXTURE2D(_AOMap);     SAMPLER(sampler_AOMap);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);

            // --- Material params (only what we keep) ---
            CBUFFER_START(UnityPerMaterial)
                float  _Growth;
                float  _PopStart, _PopEnd;
                float4 _EmissiveColor;
                float  _EmissiveIntensity;
                float4 _FloorPlane; // (a,b,c,d)
            CBUFFER_END

            // --- Fixed internal constants (not exposed) ---
            static const float  kAlphaCutoff = 0.5;  // fixed alpha test threshold
            static const float3 kPopAxisWS   = float3(0,1,0); // fixed pop axis: +Y
            static const float  kPopDepth    = 0.15; // fixed pop depth in object units
            static const float  kFakeShadeStrength = 0.25; // fixed fake shading strength

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;

                // World position variable must have this name to work with EnvironmentDepth Macros
                float3 posWorld      : TEXCOORD1;

                float3 nWS        : TEXCOORD2;
                float3 tWS        : TEXCOORD3;
                float3 bWS        : TEXCOORD4;

                // META_DEPTH_VERTEX_OUTPUT(1)
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float _EnvironmentDepthBias;

            // Map _Growth in [PopStart, PopEnd] to [0..1]
            float GrowthWindow(float g, float startV, float endV)
            {
                float denom = max(endV - startV, 1e-5);
                return saturate( (g - startV) / denom );
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                // Object position to be popped upward along world +Y
                float3 posOS = IN.positionOS;

                // Global growth → pop weight
                float w = GrowthWindow(_Growth, _PopStart, _PopEnd);

                // Pop from underground (-depth) to surface (0)
                float pop = lerp(-kPopDepth, 0.0, w);

                // Convert world pop axis to object space direction
                float3 popAxisOS = TransformWorldToObjectDir(kPopAxisWS);
                popAxisOS = normalize(popAxisOS);

                posOS += popAxisOS * pop;

                // Build TBN in world space
                float3 nWS = TransformObjectToWorldNormal(IN.normalOS);
                float3 tWS = TransformObjectToWorldDir(IN.tangentOS.xyz);
                float3 bWS = normalize(cross(nWS, tWS) * IN.tangentOS.w);

                OUT.positionCS = TransformObjectToHClip(posOS);
                OUT.uv   = IN.uv;
                OUT.posWorld = TransformObjectToWorld(posOS);
                OUT.nWS = nWS; OUT.tWS = tWS; OUT.bWS = bWS;

                META_DEPTH_INITIALIZE_VERTEX_OUTPUT(OUT, IN.positionOS.xyz);

                return OUT;
            }

            // Simple AO multiply
            float3 ApplyAO(float3 col, float ao)
            {
                return col * ao; // full strength (no exposed slider)
            }

            // Light-weight fake shading from normal vs view (constant strength)
            float3 ApplyFakeShading(float3 col, float3 nWS, float3 posWorld)
            {
                if (kFakeShadeStrength <= 0.001) return col;
                float3 V = normalize(GetWorldSpaceViewDir(posWorld));
                float ndv = saturate(abs(dot(nWS, V))); // two-sided
                float shade = lerp(1.0, 0.5 + 0.5 * ndv, kFakeShadeStrength);
                return col * shade;
            }

            float4 frag (Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // --- Floor plane hard clip (always on) ---
                // Keep fragments above the plane: dot(plane, posWorld) >= 0
                float planeDist = dot(_FloorPlane, float4(IN.posWorld, 1.0));
                if (planeDist < 0) clip(-1);

                // --- Base & alpha ---
                float4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                float3 col = baseTex.rgb;
                float  a   = baseTex.a;

                // --- Global growth alpha gating (no local mask/softness) ---
                float alphaForClip = a * saturate(_Growth);
                clip(alphaForClip - kAlphaCutoff);

                // --- AO ---
                float ao = SAMPLE_TEXTURE2D(_AOMap, sampler_AOMap, IN.uv).r;
                col = ApplyAO(col, ao);

                // --- Normal (for fake shading only) ---
                float3 nTS  = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv));
                float3 nWS  = normalize( mul( float3x3(IN.tWS, IN.bWS, IN.nWS), nTS) );
                col = ApplyFakeShading(col, nWS, IN.posWorld);

                // --- Emissive (color * intensity, no texture) ---
                float3 emissive = _EmissiveColor.rgb * _EmissiveIntensity;
                float4 finalColor = (col, a);

                META_DEPTH_OCCLUDE_OUTPUT_PREMULTIPLY(IN, finalColor, _EnvironmentDepthBias);

                float4 finalOutColor = finalColor * float4(col + emissive, 1);

                return float4(finalOutColor);
            }
            ENDHLSL
        }
    }



}
