// Upgrade NOTE: commented out 'float4 unity_LightmapST', a built-in variable
// Upgrade NOTE: commented out 'sampler2D unity_Lightmap', a built-in variable
// Upgrade NOTE: replaced tex2D unity_Lightmap with UNITY_SAMPLE_TEX2D

Shader "MADFINGER/Environment/Cubemap specular + Lightmap" {

Properties {

	_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
	_SpecCubeTex("SpecCube",Cube) = "black" {}
	_SpecularStrength("Specular strength weights", Vector) = (0,0,0,2)
	_ScrollingSpeed("Scrolling speed", Vector) = (0,0,0,0)

}

SubShader {

	Tags { "RenderType"="Opaque" "LightMode"="ForwardBase"}

	LOD 100


	CGINCLUDE
	
	

	#include "UnityCG.cginc"
	#include "../config.cginc"
	#include "../globals.cginc"


	#pragma multi_compile UNITY_SHADER_DETAIL_LOW UNITY_SHADER_DETAIL_MEDIUM UNITY_SHADER_DETAIL_HIGH	


	sampler2D _MainTex;
	samplerCUBE _SpecCubeTex;

	#ifndef LIGHTMAP_OFF
	// float4 unity_LightmapST;
	// sampler2D unity_Lightmap;
	#endif

	float4	_ScrollingSpeed;
	float4	_SpecularStrength;


	struct v2f {

		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;

		#ifndef LIGHTMAP_OFF
		float2 lmap : TEXCOORD1;
		#endif
		
#ifndef UNITY_SHADER_DETAIL_LOW		
		float3 refl : TEXCOORD2;
#endif

#if defined(UNITY_SHADER_ENABLE_VOLUME_FOG)
		fixed4 col : COLOR;
#endif
	};

	v2f vert (appdata_full v)
	{
		v2f o;

		o.pos	= mul(UNITY_MATRIX_MVP, v.vertex);
		o.uv 	= v.texcoord + frac(_ScrollingSpeed * _Time.y);

#ifndef UNITY_SHADER_DETAIL_LOW
		float3 worldNormal = normalize(mul((float3x3)_Object2World, v.normal));		

		o.refl = reflect(-WorldSpaceViewDir(v.vertex), worldNormal);
#endif
		
#ifndef LIGHTMAP_OFF
		o.lmap = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
#endif
		
#if defined(UNITY_SHADER_ENABLE_VOLUME_FOG)		
		o.col = o.pos.w * UNITY_SHADER_VOLUME_FOG_DIST_SCALE;
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
			fixed4	c = tex2D (_MainTex, i.uv);
						
			#ifndef UNITY_SHADER_DETAIL_LOW
			c.rgb = c.rgb + texCUBE(_SpecCubeTex,i.refl) * dot(_SpecularStrength,c);
			#endif
			
			c.rgb *= DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap,i.lmap));
			
#if defined(UNITY_SHADER_ENABLE_VOLUME_FOG)
			c.a = i.col.a;
#endif			

			return c;
		}

		ENDCG 

	}	
}

}
