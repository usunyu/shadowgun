Shader "MADFINGER/Characters/BRDFLit (Supports Backlight) - custom glossingess mask" {
Properties {
	_MainTex ("Base (RGB) Gloss (A)", 2D) = "grey" {}
	_BumpMap ("Normalmap", 2D) = "bump" {}
	_BRDFTex ("NdotL NdotH (RGB)", 2D) = "white" {}
	
	_FakeProbeTopColor("Fake light probe top color", Color) = (1,0,0,1)
	_FakeProbeBotColor("Fake light probe bottom color", Color) = (0,1,0,1)
	
	_LightProbesLightingAmount("Light probes lighting amount", Range(0,1)) = 0.9
	_SpecularStrength("Specular strength weights", Vector) = (0,0,0,1)
	_Params("x = open holes, y = FPV projection",Vector) = (0,0,0,0)
}	



SubShader {

	Tags { "RenderType"="Opaque" "LightMode"="ForwardBase" }
	LOD 400
	
	
	
	CGINCLUDE
//	#pragma UNITY_IPHONE
	#include "UnityCG.cginc"
	#include "../config.cginc"
	#include "../globals.cginc"
	
	sampler2D		_MainTex;
	sampler2D 		_BumpMap;
	sampler2D		_BRDFTex;
	float4			_SpecularStrength;
	float			_LightProbesLightingAmount;
	float4			_Params;
	float4			_ProjParams;
	float4 			_FakeProbeTopColor;
	float4 			_FakeProbeBotColor;
	
	struct v2f 
	{
		float4 pos	: SV_POSITION;
		float2 uv 	: TEXCOORD0;
		fixed3 ldir	: TEXCOORD1;
		fixed3 hdir : TEXCOORD2;	
		fixed4 color: COLOR;
	};


	half3 GetDominantDirLightFromSH()
 	{ 		
 		half3 res = unity_SHAr.xyz * 0.3 + unity_SHAg.xyz * 0.59 + unity_SHAb.xyz * 0.11;
 		
 		normalize(res);
 	
 		return res;
 	}

	
	v2f vert (appdata_full v)
	{
		float4x4	projTM		= UNITY_MATRIX_P;
		float3 		worldNormal = mul((float3x3)_Object2World, v.normal);
		float3		viewPos		= mul(UNITY_MATRIX_MV,v.vertex);		
		float4		projParams	= _Params.y > 0 ? _ProjParams : float4(1,1,1,0);
		v2f		o;
		
		viewPos = v.color.a < _Params.x ? float3(0,0,0) : viewPos;
		
		projTM[0][0] *= projParams.x;
		projTM[1][1] *= projParams.y;
						
		o.pos	= mul(projTM,float4(viewPos,1));		
		o.pos.z *= projParams.z;
		o.pos.z += projParams.w * o.pos.w;

//#if !defined(UNITY_IPHONE) // this breaks performance on IOS6 horribly
		
		// lame attempt to perform depth clamping
//		o.pos.z = max(o.pos.z,0);

//#endif
		

		o.uv	= v.texcoord;
			
		TANGENT_SPACE_ROTATION;
		
		#if 0
		half3	ldir = mul((float3x3)_World2Object,GetDominantDirLightFromSH());
		o.ldir = normalize(mul(rotation, ldir));
		#else
		o.ldir = mul(rotation,ObjSpaceLightDir(v.vertex));
		#endif
		
		float3 vdir = normalize(mul(rotation,ObjSpaceViewDir(v.vertex)));
		
		o.hdir = normalize(vdir + o.ldir);
		
		float3 SHLighting	= ShadeSH9(float4(worldNormal,1));
 
//		o.color = saturate(SHLighting + (1 - _LightProbesLightingAmount).xxx).xyzz;

		o.color =  lerp(saturate(SHLighting + (1 - _LightProbesLightingAmount).xxx).xyzz,lerp(_FakeProbeBotColor.xyz,_FakeProbeTopColor.xyz,worldNormal.y * 0.5 + 0.5).xyzz,step(_LightProbesLightingAmount,0.001f));
//		o.color = _LightProbesLightingAmount > 0 ? saturate(SHLighting + (1 - _LightProbesLightingAmount).xxx).xyzz : lerp(_FakeProbeBotColor.xyz,_FakeProbeTopColor.xyz,worldNormal.y * 0.5 + 0.5).xyzz;						
						
#if defined(UNITY_SHADER_ENABLE_VOLUME_FOG)
		o.color.a	= o.pos.w  * UNITY_SHADER_VOLUME_FOG_DIST_SCALE;
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
			fixed4	c		= tex2D (_MainTex, i.uv.xy);
			fixed3	normal	= tex2D(_BumpMap, i.uv.xy).rgb * 2.0 - 1.0;
			fixed	gloss	= dot(_SpecularStrength,c);
			
			c.xyz *= i.color.xyz;

			fixed	nl		= dot (normal, i.ldir);
			fixed	nh		= dot (normal, i.hdir);
			fixed4	l		= tex2D(_BRDFTex, fixed2(nl * 0.5 + 0.5, nh));

			c.rgb *= (l.rgb + gloss * l.a) * 2;

#if defined(UNITY_SHADER_ENABLE_VOLUME_FOG)
			c.a = i.color.a;
#endif
						
			return c;
		}
		ENDCG 
	}	
}
}
