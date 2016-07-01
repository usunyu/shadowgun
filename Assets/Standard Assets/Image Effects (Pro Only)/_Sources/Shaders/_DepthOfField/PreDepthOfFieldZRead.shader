 Shader "Hidden/PreDepthOfFieldZRead" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}
		_onePixelTex ("Pixel (RGB)", 2D) = "" {}
	}

	CGINCLUDE
	
	#include "UnityCG.cginc"
	
	struct v2f {
		float4 pos : POSITION;
		float2 uv : TEXCOORD0;
	};
		
	sampler2D _MainTex;
	sampler2D _CameraDepthTexture;
	sampler2D _onePixelTex;
	
	uniform float focalDistance01;
	uniform float focalFalloff;

	uniform float focalStart01;
	uniform float focalEnd01;
	uniform float focalSize;

	uniform float4 _MainTex_TexelSize;
		
	v2f vert( appdata_img v ) {
		v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		o.uv.xy = v.texcoord.xy;
		return o;
	} 
	
	half4 frag(v2f i) : COLOR 
	{
		float d = tex2D (_CameraDepthTexture, i.uv.xy);
		d = Linear01Depth(d);
		 
		half preDof; // = 0.0;

		// TESTING Z BUFFER "RAYCAST"
		//focalDistance01 = tex2D(_CameraDepthTexture, half2(0.5,0.5));
		//focalDistance01 = Linear01Depth(focalDistance01);
		half4 focalDist = tex2D(_onePixelTex, half2(0.5,0.5));
		focalDistance01 = DecodeFloatRGBA(focalDist);

		focalEnd01 = saturate(focalDistance01 + focalSize);
		focalStart01 = saturate(focalDistance01 - focalSize);

		if(d>focalDistance01) 
			preDof = (d-focalDistance01) / (focalEnd01 - focalDistance01);
		else
			preDof = (d-focalDistance01) / (focalStart01 - focalDistance01);
			
		preDof *= focalFalloff;	
		return saturate(preDof);
	}
 
	ENDCG
	
Subshader {
 Pass {
	  ZTest Always Cull Off ZWrite Off
	  ColorMask A
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