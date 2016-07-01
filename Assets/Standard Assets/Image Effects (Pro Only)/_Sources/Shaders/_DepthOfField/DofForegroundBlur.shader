 Shader "Hidden/DofForegroundBlur" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}
		_SourceTex ("Source (RGB)", 2D) = "" {}
		_BlurredCoc ("COC (RGB)", 2D) = "" {}
	}

	CGINCLUDE
	
	#include "UnityCG.cginc"
	
	struct v2f {
		float4 pos : POSITION;
		float2 uv : TEXCOORD0;
	};
		
	sampler2D _MainTex;
	sampler2D _SourceTex;
	sampler2D _BlurredCoc;
	sampler2D _CameraDepthTexture;

	
	uniform float focalDistance01;
	uniform float focalFalloff;

	uniform float focalStart01;
	uniform float focalEnd01;
	
	uniform float foregroundBlurStrength;
	uniform float foregroundBlurThreshhold;

	uniform float4 _MainTex_TexelSize;
		
	v2f vert( appdata_img v ) {
		v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		o.uv =  MultiplyUV(UNITY_MATRIX_TEXTURE0, v.texcoord);
		return o;
	}
	
	half4 frag(v2f i) : COLOR 
	{
		// calculate additional dof factor for foreground awesomeness

		half additionalDof = 0.0;
		half refDepth = tex2D(_CameraDepthTexture, i.uv.xy);
		half blurredDepth = DecodeFloatRGBA(tex2D(_MainTex,i.uv.xy));		

		half blurredCoc = (tex2D(_BlurredCoc, i.uv.xy)).a;

		if(refDepth > (blurredDepth + foregroundBlurThreshhold))
			additionalDof = blurredCoc * foregroundBlurStrength;

		half4 returnColor = tex2D(_SourceTex,i.uv.xy);

		returnColor.a = max(additionalDof, returnColor.a);
		return returnColor;
	}
 
	ENDCG
	
Subshader {
 Pass {
	//Blend One One
	//ColorMask A
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