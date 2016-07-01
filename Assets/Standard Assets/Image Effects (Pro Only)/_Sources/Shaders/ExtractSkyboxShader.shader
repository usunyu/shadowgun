
Shader "Hidden/ExtractSkyboxShader" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}
		_Skybox ("Sky (RGB)", 2D) = "" {}
	}
	
	// Shader code pasted into all further CGPROGRAM blocks
	CGINCLUDE
	
	#include "UnityCG.cginc"
	
	struct v2f {
		float4 pos : POSITION;
		float2 uv : TEXCOORD0;
	};
	
	sampler2D _MainTex;
	sampler2D _Skybox;
		
	v2f vert( appdata_img v ) {
		v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		o.uv =  v.texcoord.xy;
		return o;
	} 
	
	half4 frag(v2f i) : COLOR {
		float4 sky = (tex2D (_Skybox, i.uv.xy));
		float4 color = (tex2D (_MainTex, i.uv.xy));
		
		if(length(sky.rgb-color.rgb)>0.2)
			return 0;
		else
			return sky;
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