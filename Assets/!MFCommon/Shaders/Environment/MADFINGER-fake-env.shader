// Upgrade NOTE: commented out 'float4 unity_LightmapST', a built-in variable
// Upgrade NOTE: commented out 'sampler2D unity_Lightmap', a built-in variable
// Upgrade NOTE: replaced tex2D unity_Lightmap with UNITY_SAMPLE_TEX2D

Shader "MADFINGER/Environment/Fake env map (Supports Lightmap)" {

Properties 
{
	_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
	_EnvTex ("Spherical env tex", 2D) = "black" {}
	_Spread("Spread", Range (0.1,0.5)) = 0.5
}

SubShader {
	Tags { "RenderType"="Opaque" "LightMode"="ForwardBase"}
	LOD 100
	
	
	
	CGINCLUDE
	#include "UnityCG.cginc"
	
	sampler2D _MainTex;
	sampler2D _EnvTex;
	
	#ifndef LIGHTMAP_OFF
	// float4 unity_LightmapST;
	// sampler2D unity_Lightmap;
	#endif

	float3 _SpecColor;
	float _Shininess;
	float _Spread;
	
	struct v2f {
		float4 pos : SV_POSITION;
		float4 uv : TEXCOORD0;
		#ifndef LIGHTMAP_OFF
		float2 lmap : TEXCOORD1;
		#endif
//		float3 spec: TEXCOORD2;
	};

	
	v2f vert (appdata_full v)
	{
		v2f o;
		
		o.pos	= mul(UNITY_MATRIX_MVP, v.vertex);
		o.uv.xy	= v.texcoord;
		
		float3 viewNormal = mul((float3x3)UNITY_MATRIX_MV, v.normal);
		
		o.uv.zw = viewNormal.xy * _Spread + 0.5;
		
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
			fixed4 c 	= tex2D (_MainTex, i.uv.xy);
			
			#ifndef LIGHTMAP_OFF
			c.xyz *= DecodeLightmap (UNITY_SAMPLE_TEX2D(unity_Lightmap, i.lmap));
			#endif
			
			c.xyz += tex2D(_EnvTex,i.uv.zw) * c.a;
			
			return c;
		}
		ENDCG 
	}	
}
}


