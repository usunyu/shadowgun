Shader "MADFINGER/FX/Water" {
Properties {
	_EnvTex ("Cube env tex", CUBE) = "black" {}	
	_Params("Fresnel bias - x, Fresnel pow - y, Transparency bias - z",vector) = (0,10,0.2,0)
	_DeepColor("Deep color",Color) = (0,1,0,1)
	_ShallowColor("Shallow color",Color) = (1,0,0,1)

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
	
	samplerCUBE _EnvTex;
	float4 		_Params;
	float4 		_DeepColor;
	float4		_ShallowColor;
	float4		_SpecColor;
	float4		_SpecOffset;
	float4		_SpecParams;
	
	struct v2f {
		float4 pos : SV_POSITION;
		float3 refl: TEXCOORD0;
		fixed4 color: COLOR;
		fixed4 color2: COLOR1;
	};

#if 0
	float _Fresnel(float facing, float fresnelBias, float fresnelPow)
	{
		return  max(fresnelBias + (1 - fresnelBias) * pow(facing, fresnelPow), 0);
	}
#else
	
	float _Fresnel(float facing,float bias,float power)
	{
  		return saturate(bias + (1 - bias) * pow(facing, power));
	}
#endif

	float PlaneRayISec(float4 plane,float3 rayOrigin,float3 rayDir)
	{
		float k = -plane.w - dot(rayOrigin,plane.xyz);
		float d = dot(rayDir,plane.xyz);
		
		return k / d;
	}
	
	
	v2f vert (appdata_full v)
	{
		v2f 	o;
		
		float3	worldNormal = mul((float3x3)_Object2World, normalize(v.normal));
		float3	viewDir		= normalize(WorldSpaceViewDir(v.vertex));
		float3	viewRefl	= reflect(-viewDir, worldNormal);
		float	facing		= 1 - saturate(dot(viewDir,worldNormal));
		float	fresnel		= _Fresnel(facing,_Params.x,_Params.y);
		float3	color		= lerp(_DeepColor,_ShallowColor,facing);


#if 0
		float4	waterPlane0		= float4(0,1,0,0);
		float4	waterPlane1		= float4(0,1,0,1);
		
		float3	viewDirLocal	= mul((float3x3)_World2Object,viewDir);
		float3	viewPosLocal	= mul(_World2Object,float4(_WorldSpaceCameraPos,1));				
		
		float 	isect0			= PlaneRayISec(waterPlane0,viewPosLocal,viewDirLocal);
		float 	isect1			= PlaneRayISec(waterPlane1,viewPosLocal,viewDirLocal);

		float3	isecPt0 = viewPosLocal + viewDirLocal * isect0;
		float3	isecPt1 = viewPosLocal + viewDirLocal * isect1;
#endif
				
		
		#if 0
		float3 	worldLightDir	= _WorldSpaceLightPos0;	
		float	specLevel		= pow(saturate(dot(worldLightDir,viewRefl)), _SpecParams.y) * _SpecParams.x;
		float3	specColor 		= _SpecColor * specLevel;
		#endif

		
		
		o.pos	= mul(UNITY_MATRIX_MVP,v.vertex);
		o.refl	= viewRefl;
		o.color	= fixed4(color.xyz,fresnel);
		o.color2 = facing + _Params.z;

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
		#if 0
			fixed4	res = i.color2;			
			res.xyz += lerp(i.color.xyz,texCUBE(_EnvTex,i.refl).xyz,i.color.a);
			
			return res;
		#else
			fixed4 res = i.color2;
			
			res.xyz = lerp(i.color.xyz,texCUBE(_EnvTex,i.refl).xyz,i.color.a);
		
			return res;			
		#endif

			
		}
		ENDCG 
	}	
}
}
