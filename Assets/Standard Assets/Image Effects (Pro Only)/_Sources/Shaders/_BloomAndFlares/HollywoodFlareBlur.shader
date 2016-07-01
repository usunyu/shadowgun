Shader "Hidden/HollywoodFlareBlurShader" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}
		_NonBlurredTex ("Base (RGB)", 2D) = "" {}
	}
	
	CGINCLUDE

	#include "UnityCG.cginc"
	
	struct v2f {
		float4 pos : POSITION;
		float2 uv : TEXCOORD0;
	};
	
	float4 offsets;
	float4 tintColor;
	
	// ok, _NonBlurredTex and _MainTex are switched ...

	sampler2D _MainTex;
	sampler2D _NonBlurredTex;
		
	v2f vert (appdata_img v) {
		v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		o.uv =  v.texcoord.xy;
		return o;
	}
		
	half4 frag (v2f i) : COLOR {
		half4 color = tex2D (_MainTex, i.uv);
		half4 colorNb = tex2D (_NonBlurredTex, i.uv);
				
		return (color) * tintColor * 0.5 + colorNb * normalize(tintColor) * 0.5; // - saturate(colorNb - color); 
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