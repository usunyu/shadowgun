Shader "MADFINGER/PostFX/ScreenSpaceLightFX" { 

	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}		
	}
		
	// Shader code pasted into all further CGPROGRAM blocks
	CGINCLUDE

	#include "UnityCG.cginc"
	
	#define MAX_GLOWS 4
	
	struct v2f {
		float4 pos	: POSITION;
		float2 uv	: TEXCOORD0;
		fixed4 col	: COLOR;
	};
		
	sampler2D	_MainTex;	
	float4 		_MainTex_TexelSize;	
	float4		_GlowsIntensityMask;
	float4		_GlobalColor;
	float4x4	_UnprojectTM;
	
	//
	// Glow params:
	//
	// Matrix row 0: x, y, z - position, w - radius
	// Matrix row 1: x, y, z - color, w - cos(cone angle)
	// Matrix row 2: x, y, z - cone dir
	
	float4x4	_Glow0Params;
	float4x4	_Glow1Params;
	float4x4	_Glow2Params;
	float4x4	_Glow3Params;
	
	
	v2f vert( appdata_img v ) 
	{
		v2f 	o;
		float2	uv		= v.vertex.xy;
		float2	ndcposXY= uv * 2 - 1;

		float4	wp = mul(_UnprojectTM,float4(ndcposXY,0,1));
		float	iw = 1.f / wp.w;
		
		wp *= iw;
		
		float3 dir = normalize(wp.xyz - _WorldSpaceCameraPos);
		
	#if SHADER_API_D3D9
	
		// On D3D when AA is used, the main texture & scene depth texture will come out in different vertical orientations.	
	
		if (_MainTex_TexelSize.y < 0)
		{
			uv.y = 1 - uv.y;
		}
	#endif

		o.pos	= float4(v.vertex.xy * 2 - float2(1,1),0,1);
		o.uv.xy	= uv;
		
//		float3	glow0Pos	= _Glow0Params[0].xyz;
		
		float3 toViewer0	= normalize(_WorldSpaceCameraPos - _Glow0Params[0].xyz);
		float3 toViewer1	= normalize(_WorldSpaceCameraPos - _Glow1Params[0].xyz);
		float3 toViewer2	= normalize(_WorldSpaceCameraPos - _Glow2Params[0].xyz);
		float3 toViewer3	= normalize(_WorldSpaceCameraPos - _Glow3Params[0].xyz);
		
		float4 l	= max(float4(	dot(-toViewer0,dir),
									dot(-toViewer1,dir),
									dot(-toViewer2,dir),
									dot(-toViewer3,dir)),0);
				
		l = pow(l,350);					
		
		l = l * _GlowsIntensityMask;
				
		o.col = l.x * _Glow0Params[1].xyzz + 
				l.y * _Glow1Params[1].xyzz +
				l.z * _Glow2Params[1].xyzz +
				l.w * _Glow3Params[1].xyzz;
				
		o.col += _GlobalColor;

		return o;
	}
	
	fixed4 frag(v2f i) : COLOR 	
	{
	//	return i.col;
		return tex2D(_MainTex,i.uv) + i.col;
	}

	ENDCG
	
Subshader {
 Pass {
	  ZTest Always Cull Off ZWrite Off
	  Fog { Mode off }      

      CGPROGRAM
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma vertex vert
      #pragma fragment frag
      ENDCG
  }
}

Fallback off
	

}