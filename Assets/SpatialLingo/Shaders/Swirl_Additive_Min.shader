// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "CYMY/Portal/OutsideEdgeGlow_URP"
{
    Properties
    {
        [IntRange]_StencilRef("Stencil Ref", Range(0,255)) = 1

        _InnerRadius ("Inner Radius (UV)", Range(0.0,1.5)) = 0.45
        _Thickness   ("Edge Thickness",     Range(0.0,1.0)) = 0.08
        _Softness    ("Edge Softness",      Range(0.0,1.0)) = 0.35

        _InnerColor  ("Inner Color", Color) = (0.72,0.27,1.0,1)  // Purple
        _OuterColor  ("Outer Color", Color) = (0.1, 0.2, 1.0,1)  // Blue
        _Intensity   ("Intensity", Range(0,5)) = 1.0

        [NoScaleOffset]_NoiseTex ("Optional Noise", 2D) = "gray" {}
        _NoiseAmp    ("Noise Amp", Range(0,1)) = 0.2
        _NoiseScale  ("Noise Scale", Range(0.1,10)) = 2.0
        _NoiseSpeed  ("Noise Speed", Range(-5,5)) = 0.5

        _EnvironmentDepthBias ("Environment Depth Bias", Float) = 0.0
    }

    SubShader
    {
        Tags{ "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back
        
        // Key point: Unlike the inner circle, only draw where the template is "different," 
        // so that the outer ring that crosses the boundary only retains the outer half of the circle.
        Stencil
        {
            Ref [_StencilRef]
            Comp NotEqual
            Pass Keep
            Fail Keep
            ZFail Keep
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/SpatialLingo/Shaders/MetaOcclusion/EnvironmentOcclusionURP.hlsl"


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0; // The grid center should be within UV(0.5,0.5).
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float2 uv       : TEXCOORD0;
                // World position variable must have this name to work with EnvironmentDepth Macros
                float3 posWorld : TEXCOORD1;
              
                // META_DEPTH_VERTEX_OUTPUT(1)
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                int   _StencilRef;
                float _InnerRadius;
                float _Thickness;
                float _Softness;
                float4 _InnerColor;
                float4 _OuterColor;
                float _Intensity;
                float _NoiseAmp;
                float _NoiseScale;
                float _NoiseSpeed;
                float _EnvironmentDepthBias;
            CBUFFER_END

            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            v2f vert(appdata IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output)
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.pos = TransformObjectToHClip(IN.vertex.xyz);
                OUT.uv  = IN.uv;

                // Occlusion macro
                META_DEPTH_INITIALIZE_VERTEX_OUTPUT(OUT, IN.vertex.xyz);

                return OUT;
            }

            // Smooth step transition from 0 to 1
            float smooth01(float x0, float x1, float x)
            {
                return saturate( (x - x0) / max(1e-5, (x1 - x0)) );
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                
                // Radius of the circle centered at UV(0.5,0.5)
                float2 p = i.uv - 0.5;
                float  r = length(p);

                // Outer ring range: Starting from InnerRadius, extending outwards by Thickness.
                float edgeStart = _InnerRadius;
                float edgeEnd   = _InnerRadius + _Thickness;

                // Base ring mask (1 on the ring, 0 elsewhere)
                float ring = smoothstep(edgeStart, edgeStart + _Softness, r) *
                             (1.0 - smoothstep(edgeEnd,   edgeEnd   + _Softness, r));

                // Slight "radial jagged" noise (optional, turn it off by setting _NoiseAmp to 0)
                float t = _Time.y * _NoiseSpeed;
                float2 nUV = float2( r * _NoiseScale, atan2(p.y, p.x) * 0.15915 ); // Angle/π ≈ 0.318 or /2π ≈ 0.15915
                float  n = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, nUV + t).r;
                float  spikes = lerp(1.0, 1.0 + (n - 0.5) * 2.0 * _NoiseAmp, ring);

                // Color gradient from the inside out
                float edgeLerp = smooth01(edgeStart, edgeEnd, r);
                float4 col = lerp(_InnerColor, _OuterColor, edgeLerp);

                // Alpha: Fades outwards from the inner edge
                float alpha = ring * spikes;
                col.rgb *= _Intensity;
                col.a   = alpha * col.a;

                float4 depthColor = (col);

                // Occlusuion Macro
                META_DEPTH_OCCLUDE_OUTPUT_PREMULTIPLY(i, depthColor, _EnvironmentDepthBias);

                float4 finalOutColor = depthColor * col;

                return finalOutColor;
            }
            ENDHLSL
        }
    }
}
