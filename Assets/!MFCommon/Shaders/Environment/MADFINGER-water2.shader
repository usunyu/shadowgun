Shader "MADFINGER/FX/Water2" {
Properties {
	_EnvTex ("Cube env tex", CUBE) = "black" {}	
	_Normals("Normals",2D) = "bump" {}
	_Params("Fresnel bias - x, Fresnel pow - y, Transparency bias - z",vector) = (0,10,0.2,0)
	_Params2("Bumps tiling - x, Bumps scroll speed",vector) = (0.25,0.1,0,0)
	_ShallowColor("Shallow color",Color) = (1,0,0,1)
	_DeepColor("Deep color",Color) = (0,1,0,1)

//	_SpecOffset ("Specular Offset from Camera", Vector) = (1, 10, 2, 0)
//	_SpecColor("Specular color",Color) = (1,1,1,1)
//	_SpecParams("Specular level - x, Specular power - y, Specular range - z", Vector) = (1,10,20,0)
	
}

SubShader {

	Tags {"Queue"="Transparent-10" "IgnoreProjector"="True" "RenderType"="Transparent"}

	Blend SrcAlpha OneMinusSrcAlpha 
	Lighting Off Fog { Mode Off }
	ZWrite Off
	
	LOD 100
	
			
	CGINCLUDE

	#include "UnityCG.cginc"
	#include "../config.cginc"
	#include "../globals.cginc"

	#pragma multi_compile UNITY_SHADER_DETAIL_LOW UNITY_SHADER_DETAIL_MEDIUM UNITY_SHADER_DETAIL_HIGH UNITY_SHADER_DETAIL_VERY_HIGH	
	
	samplerCUBE _EnvTex;
	sampler		_Normals;
	float4 		_Params;
	float4		_Params2;
	float4 		_DeepColor;
	float4		_ShallowColor;
	float4		_SpecColor;
	float4		_SpecOffset;
	float4		_SpecParams;
	
	struct v2f {
		float4 pos 		: SV_POSITION;
		float3 refl		: TEXCOORD0;
		
#if defined(UNITY_SHADER_DETAIL_HIGH) || defined(UNITY_SHADER_DETAIL_VERY_HIGH)
		float4 uv		: TEXCOORD1;
		float3 normal	: TEXCOORD2;
		float3 viewDir	: TEXCOORD3;
#endif
		fixed4 color	: COLOR;
		fixed4 color2	: COLOR1;
	};


	float PlaneRayISec(float4 plane,float3 rayOrigin,float3 rayDir)
	{
		float k = -plane.w - dot(rayOrigin,plane.xyz);
		float d = dot(rayDir,plane.xyz);
		
		return k / d;
	}
	
	
	v2f vert (appdata_full v)
	{
		v2f 	o;
		
		float3	worldNormal = normalize(mul((float3x3)_Object2World,v.normal));
		float3	viewDir		= normalize(WorldSpaceViewDir(v.vertex));
		float3	viewRefl	= reflect(-viewDir, worldNormal);
		float	facing		= 1 - saturate(dot(viewDir,worldNormal));
		float	fresnel		= MFFresnel(facing,_Params.x,_Params.y);
		float3	color		= lerp(_DeepColor,_ShallowColor,facing);

		
		o.pos	= mul(UNITY_MATRIX_MVP,v.vertex);

		o.refl	= viewRefl;
		o.refl.x = -o.refl.x;

		o.color	= fixed4(color.xyz,fresnel);
		o.color2 = fresnel + _Params.z;

#if defined(UNITY_SHADER_DETAIL_HIGH) || defined(UNITY_SHADER_DETAIL_VERY_HIGH)
		o.normal 	= worldNormal;
		o.viewDir	= WorldSpaceViewDir(v.vertex);
		
		float3 worldPos = mul(_Object2World,v.vertex);
		
		o.uv.xy = worldPos.xz * _Params2.x + frac(_Time.yy * _Params2.y * normalize(float2(-0.3,0.7)));
		o.uv.zw = worldPos.xz * _Params2.x + frac(_Time.yy * _Params2.y * normalize(float2(0.5,0.2)) * 0.5);
#endif

		return o;
	}
	ENDCG


	Pass {
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma fragmentoption ARB_precision_hint_fastest		
		
		half3 MixNormals(half3 n1,half3 n2)
		{
   			half3 r = half3(n1.xy + n2.xy, n1.z*n2.z);
   			
    		return normalize(r);		
		}
		
		fixed4 frag (v2f i) : COLOR								
		{	
			fixed4 	res;
		
#if defined(UNITY_SHADER_DETAIL_HIGH) || defined(UNITY_SHADER_DETAIL_VERY_HIGH)
//			half3	texnrm	= normalize((tex2D(_Normals,i.uv.xy).xyz + tex2D(_Normals,i.uv.zw).xyz) * 2 - 2);
			half3	texnrm	= MixNormals(tex2D(_Normals,i.uv.xy).xyz * 2 - 1,tex2D(_Normals,i.uv.zw).xyz * 2 - 1);
			half3	norm	= MixNormals(texnrm.xzy,i.normal);
			
			half3	viewDir	= normalize(i.viewDir);
			half3	refl	= reflect(-viewDir,norm);
			half	facing	= 1 - saturate(dot(viewDir,norm));	
			half	fresnel	= MFFresnel(facing,_Params.x,_Params.y);
			fixed3	color	= lerp(_DeepColor,_ShallowColor,facing);
				
			res.a 	= fresnel + _Params.z;
#else
			half3	refl	= i.refl;
			fixed	fresnel	= i.color.a;
			fixed3	color	= i.color;
			
			res	= i.color2;
#endif			
			res.xyz = lerp(color.xyz,texCUBE(_EnvTex,refl).xyz,fresnel);
			
		
			return res;			
		}
		ENDCG 
	}	
}
}
