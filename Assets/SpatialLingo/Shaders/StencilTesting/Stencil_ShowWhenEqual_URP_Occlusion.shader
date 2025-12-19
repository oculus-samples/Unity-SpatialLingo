Shader "Stencil/URP_ShowWhenEqual_Skydome_Auto_Occlude"
{
    Properties
    {
        [IntRange]_StencilRef ("Stencil Ref", Range(0,255)) = 3
        [NoScaleOffset]_BaseMap ("Sky (Equirect)", 2D) = "gray" {}
        _Exposure ("Exposure", Range(0,8)) = 1

        _AutoRotateYawDegPerSec ("Auto Rotate Yaw (deg/s)", Float) = 3
        _YawOffsetDeg ("Yaw Offset (deg)", Float) = 0

        _EnvironmentDepthBias ("Environment Depth Bias", Float) = 0.0
    }

    SubShader
    {
        //Tags { "RenderType"="Transparent" "Queue"="Transparent+10" }
        Tags { "RenderType"="Opaque" "Queue"="AlphaTest"  }

        // Sky: Depth not specified, always passing through, inner surface.
        ZWrite Off
        ZTest  Always
        Cull   Front

        // Display only in the template=Ref area
        Stencil
        {
            Ref  [_StencilRef]
            Comp Equal
            Pass Keep
            Fail Keep
            ZFail Keep
        }

        Pass
        {
            Name "SkydomeForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            
            // (If there are compilation target limitations, you can enable a higher target.)：#pragma target 4.5
            #pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/SpatialLingo/Shaders/MetaOcclusion/EnvironmentOcclusionURP.hlsl"

            TEXTURE2D(_BaseMap); 
            SAMPLER(sampler_BaseMap);

            float4 _BaseMap_ST;
            float  _Exposure;
            float  _AutoRotateYawDegPerSec;
            float  _YawOffsetDeg;
            float  _EnvironmentDepthBias;

            struct Attributes 
            {
                 float4 positionOS : POSITION; 
                 UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_Position;

                // World position variable must have this name to work with EnvironmentDepth Macros
                float3 posWorld    : TEXCOORD1;
                float3 dirWS       : TEXCOORD0; // Direction only

                // META_DEPTH_VERTEX_OUTPUT(1)
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output)
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                float3 camPosWS = GetCameraPositionWS();
                OUT.dirWS = normalize(worldPos - camPosWS);
                
                // Occlusion macro
                META_DEPTH_INITIALIZE_VERTEX_OUTPUT(OUT, IN.positionOS.xyz);

                return OUT;
            }

            

            float3 RotateYaw(float3 v, float yawDeg)
            {
                float yaw = radians(yawDeg);
                float cy = cos(yaw), sy = sin(yaw);
                float3x3 Ry = float3x3(
                    cy, 0, -sy,
                    0 , 1,  0,
                    sy, 0,  cy
                );
                return mul(Ry, v);
            }

            float2 DirToEquirectUV(float3 d)
            {
                d = normalize(d);
                return float2(atan2(d.x, d.z) / (2.0 * PI) + 0.5,
                              asin (d.y)       /  PI        + 0.5);
            }

            // The directional derivative is approximated as the UV derivative, and the U derivative is wrapped to avoid large jumps across the seam.
            void DirDerivToUVDeriv(float3 d, float3 ddxDir, float3 ddyDir, out float2 ddxUV, out float2 ddyUV)
            {
                float invLen = rsqrt(max(dot(d,d), 1e-8));
                float3 nd = d * invLen;

                float x = nd.x, y = nd.y, z = nd.z;
                float denomXZ = max(x*x + z*z, 1e-6);
                float inv2Pi  = 1.0 / (2.0 * PI);
                float invPi   = 1.0 / PI;
                float invSqrt = rsqrt(max(1.0 - y*y, 1e-6));

                float3 dx = ddxDir * invLen;
                float3 dy = ddyDir * invLen;

                float du_dx = ( (z*dx.x - x*dx.z) / denomXZ ) * inv2Pi;
                float dv_dx = ( dx.y * invSqrt ) * invPi;

                float du_dy = ( (z*dy.x - x*dy.z) / denomXZ ) * inv2Pi;
                float dv_dy = ( dy.y * invSqrt ) * invPi;

                ddxUV = float2(du_dx, dv_dx);
                ddyUV = float2(du_dy, dv_dy);

            // Wrap the derivative of U to avoid large jumps across the seam
                if (abs(ddxUV.x) > 0.5) ddxUV.x -= sign(ddxUV.x);
                if (abs(ddyUV.x) > 0.5) ddyUV.x -= sign(ddyUV.x);
            }

            half4 frag (Varyings IN) : SV_Target
            {

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // Rotate and calculate derivatives at the fragment stage (ddx/ddy can only be used in FS).
                float totalYaw = _AutoRotateYawDegPerSec * _Time.y + _YawOffsetDeg;

                float3 d = RotateYaw(IN.dirWS, totalYaw);
                // Find the screen derivative with respect to the rotated direction
                float3 d_dx = ddx(d);
                float3 d_dy = ddy(d);

                float2 uv = DirToEquirectUV(d);
                uv.x = frac(uv.x); // Forced U-Surround

                float2 ddxUV, ddyUV;
                DirDerivToUVDeriv(d, d_dx, d_dy, ddxUV, ddyUV);

                // Use gradient sampling after wrapping to avoid the two sides of the seam falling into different MIPs.
                half3 col = SAMPLE_TEXTURE2D_GRAD(_BaseMap, sampler_BaseMap, uv, ddxUV, ddyUV).rgb;

                col *= _Exposure;

                half4 depthColor = (col, 1);

                //Occlusuion Macro
                META_DEPTH_OCCLUDE_OUTPUT_PREMULTIPLY(IN, depthColor, _EnvironmentDepthBias);

                half4 finalOutColor = depthColor * half4(col, 1);

                return half4(finalOutColor);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
