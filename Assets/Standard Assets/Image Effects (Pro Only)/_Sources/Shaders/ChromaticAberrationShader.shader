Shader "Hidden/ChromaticAberrationShader" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}
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
	
	float4 _MainTex_TexelSize;
	float chromaticAberrationIntensity;

		
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
		
		float2 uvG = uv - _MainTex_TexelSize.xy * chromaticAberrationIntensity * coords * coordDot;
		half4 color = tex2D (_MainTex, uv);
		#if SHADER_API_D3D9
		// Work around Cg's code generation bug for D3D9 pixel shaders :(
		color.g = color.g * 0.0001 + tex2D (_MainTex, uvG).g;
		#else
		color.g = tex2D (_MainTex, uvG).g;
		#endif
		return color;
	}

	ENDCG 
	
Subshader {
 Pass {
	  ZTest Always Cull Off ZWrite Off
	  Fog { Mode off }      

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      ENDCG
  }
}

Fallback off
	
} // shader