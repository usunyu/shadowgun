// Upgrade NOTE: commented out 'float4 unity_LightmapST', a built-in variable

Shader "MADFINGER/Environment/Cubemap specular + Custom Lightmap + fake bump" {

Properties {

	_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
	_CustomLightmap ("Custom lightmap", 2D) = "white" {}
	_SpecCubeTex("SpecCube",Cube) = "black" {}
	_SpecularStrength("Specular strength weights", Vector) = (0,0,0,2)
	_ScrollingSpeed("Scrolling speed", Vector) = (0,0,0,0)
	_Params("Bumpiness - x",Vector) = (2,0,0,0)

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
	sampler2D _CustomLightmap;
	samplerCUBE _SpecCubeTex;

	// float4 unity_LightmapST;

	float4	_ScrollingSpeed;
	float4	_SpecularStrength;
	float4	_Params;


	struct v2f {

		float4 pos : SV_POSITION;
		float4 uv : TEXCOORD0;

		#ifndef LIGHTMAP_OFF
		float2 lmap : TEXCOORD1;
		#endif
		
#ifndef UNITY_SHADER_DETAIL_LOW		
		float3 refl : TEXCOORD2;
#endif
	};

	v2f vert (appdata_full v)
	{
		v2f o;

		o.pos	= mul(UNITY_MATRIX_MVP, v.vertex);
		o.uv.xy	= v.texcoord + frac(_ScrollingSpeed * _Time.y);
		o.uv.zw = float2(2,1) * _Params.x;

		

#ifndef UNITY_SHADER_DETAIL_LOW
		float3 worldNormal = normalize(mul((float3x3)_Object2World, v.normal));		

		o.refl = reflect(-WorldSpaceViewDir(v.vertex), worldNormal);
#endif
		
		//#ifndef LIGHTMAP_OFF
		o.lmap = v.texcoord1.xy;// * unity_LightmapST.xy + unity_LightmapST.zw;
		//#endif

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
			fixed4	c = tex2D (_MainTex, i.uv.xy);
						
			#ifndef UNITY_SHADER_DETAIL_LOW
			
			fixed3	spec	= texCUBE(_SpecCubeTex,i.refl + (c.xyz * i.uv.z - i.uv.w)) * dot(_SpecularStrength,c);

			c.rgb = c.rgb + spec;
			#endif
			
			c.rgb *= tex2D(_CustomLightmap,i.lmap);

			return c;
		}

		ENDCG 

	}	

}

}

