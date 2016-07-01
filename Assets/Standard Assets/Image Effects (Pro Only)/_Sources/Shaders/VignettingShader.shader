Shader "Hidden/VignettingShader" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}
		_VignetteTex ("Vignette (RGB)", 2D) = "" {}
	}
	
	// Shader code pasted into all further CGPROGRAM blocks
	CGINCLUDE
	
	#include "UnityCG.cginc"
	
	struct v2f {
		float4 pos : POSITION;
		float2 uv : TEXCOORD0;
	};
	
	// the color should come from a half rezolution buffer
	// and target a quarter resolution buffer
	
	sampler2D _MainTex;
	sampler2D _VignetteTex;
	
	float4 _MainTex_TexelSize;
	float vignetteIntensity;
	float blurVignette;
		
	v2f vert( appdata_img v ) {
		v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		o.uv = v.texcoord.xy;
		return o;
	} 
	
	half4 frag(v2f i) : COLOR {
		half2 coords = i.uv;
		half2 uv = i.uv;
		
		coords = (coords - 0.5) * 2.0;		
		half coordDot = dot (coords,coords);
		half4 color = tex2D (_MainTex, uv);	 
		 		 
		float mask = 1.0 - coordDot * vignetteIntensity *    0.1; 
		
		half4 colorBlur = tex2D (_VignetteTex, uv);
		color = lerp (color, colorBlur, saturate (blurVignette * coordDot));
		
		return color * mask;
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