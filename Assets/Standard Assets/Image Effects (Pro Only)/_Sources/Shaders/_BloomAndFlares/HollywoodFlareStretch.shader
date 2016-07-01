Shader "Hidden/HollywoodFlareStretchShader" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}
	}
	
	CGINCLUDE

	#include "UnityCG.cginc"
	
	struct v2f {
		float4 pos : POSITION;
		float2 uv : TEXCOORD0;
	};
	
	float4 offsets;
	float stretchWidth;
	
	sampler2D _MainTex;
		
	v2f vert (appdata_img v) {
		v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		o.uv =  v.texcoord.xy;
		return o;
	}
		
	half4 frag (v2f i) : COLOR {
		float4 color = tex2D (_MainTex, i.uv);

		float b = stretchWidth;

		color = max(color,tex2D (_MainTex, i.uv + b * 2.0 * offsets.xy));
		color = max(color,tex2D (_MainTex, i.uv - b * 2.0 * offsets.xy));		
		color = max(color,tex2D (_MainTex, i.uv + b * 4.0 * offsets.xy));
		color = max(color,tex2D (_MainTex, i.uv - b * 4.0 * offsets.xy));
		color = max(color,tex2D (_MainTex, i.uv + b * 8.0 * offsets.xy));
		color = max(color,tex2D (_MainTex, i.uv - b * 8.0 * offsets.xy));
		color = max(color,tex2D (_MainTex, i.uv + b * 14.0 * offsets.xy));
		color = max(color,tex2D (_MainTex, i.uv - b * 14.0 * offsets.xy));
		color = max(color,tex2D (_MainTex, i.uv + b * 20.0 * offsets.xy));
		color = max(color,tex2D (_MainTex, i.uv - b * 20.0 * offsets.xy));

								
		return color;
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