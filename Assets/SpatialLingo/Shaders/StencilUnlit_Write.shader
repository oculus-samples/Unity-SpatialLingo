// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "Stencil/Unlit_Write"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
	[IntRange] _StencilRef ("Stencil Reference Value", Range(0,255)) = 0
    }
    SubShader
    {
	Tags{ "RenderType"="Opaque" "Queue"="Geometry-1"}
	ColorMask 0
	ZWrite Off
        //stencil operation
		Stencil{
			Ref [_StencilRef]
			Comp Always
			Pass Replace
		}


        Pass
        {
            //don't draw color or depth
	    Blend Zero One
	    ZWrite Off
            
	    CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
