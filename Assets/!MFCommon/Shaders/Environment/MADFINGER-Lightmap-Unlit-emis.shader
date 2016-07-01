// Upgrade NOTE: commented out 'float4 unity_LightmapST', a built-in variable
// Upgrade NOTE: commented out 'sampler2D unity_Lightmap', a built-in variable
// Upgrade NOTE: replaced tex2D unity_Lightmap with UNITY_SAMPLE_TEX2D

Shader "MADFINGER/Environment/Lightmap + emissivity" {
Properties {
	_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
	_Emissivity("Emissivity scaler",range(0,4)) = 1
//	_EmisColor("Emisive color",Color) = (1,1,1)
}

SubShader {
	Tags { "RenderType"="Opaque" "LightMode"="ForwardBase"}
	LOD 100
	
			
	CGINCLUDE
	#include "UnityCG.cginc"
	sampler2D _MainTex;
	
	#ifndef LIGHTMAP_OFF
	// float4 unity_LightmapST;
	// sampler2D unity_Lightmap;
	#endif

	float3	_EmisColor;
	float	_Emissivity;
	
	struct v2f {
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
		#ifndef LIGHTMAP_OFF
		float2 lmap : TEXCOORD1;
		#endif
	};

	
	v2f vert (appdata_full v)
	{
		v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		
		o.uv = v.texcoord;
		
		#ifndef LIGHTMAP_OFF
		o.lmap = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
		#endif
		return o;
	}
	ENDCG


	Pass {
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma fragmentoption ARB_precision_hint_fastest		
		fixed4 frag (v2f i) : COLOR
		{
			fixed4 c = tex2D (_MainTex, i.uv);
			
			#ifndef LIGHTMAP_OFF
			c.rgb *= (DecodeLightmap (UNITY_SAMPLE_TEX2D(unity_Lightmap, i.lmap)) + c.aaa * _Emissivity);
			#endif
			
						
			return c;
		}
		ENDCG 
	}	
}
}


