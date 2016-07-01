Shader "Hidden/CopyDepthToRGBA" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}
	}
	
	CGINCLUDE
		
	#include "UnityCG.cginc"
	
	struct v2f {
		float4 pos : POSITION;
		float2 uv1 : TEXCOORD0;
	};
		
	sampler2D _MainTex;
	sampler2D _CameraDepthTexture;
	 
	uniform float4 _MainTex_TexelSize;
	uniform float4 _CameraDepthTexture_ST;
		
	v2f vert( appdata_img v ) {
		v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		o.uv1.xy = TRANSFORM_TEX(v.texcoord, _CameraDepthTexture);
		
		#ifdef SHADER_API_D3D9
		if (_MainTex_TexelSize.y < 0)
			 o.uv1.y = 1.0- o.uv1.y;
		#endif
		
		return o;
	}

	half4 frag(v2f i) : COLOR 
	{
		float d = tex2D(_CameraDepthTexture, i.uv1.xy);
		//d = Linear01Depth(d);

		if(d>0.9999)
			return half4(1,1,1,1);
		else
			return EncodeFloatRGBA(d); 
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
	
} // shader