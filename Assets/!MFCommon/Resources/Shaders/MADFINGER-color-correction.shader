Shader "MADFINGER/PostFX/ColorCorrection" { 

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
		
	sampler2D _MainTex;
	float4x4	_ColorMatrix;
		
	v2f vert( appdata_img v ) 
	{
		v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		o.uv =  v.texcoord.xy;
		return o;
	}
	
	fixed4 frag(v2f i) : COLOR 	
	{
		fixed4	tex = tex2D(_MainTex, i.uv);
		fixed4	dst = mul(tex,_ColorMatrix);
	
		return fixed4(dst.xyz,tex.a);
	}

	ENDCG
	
Subshader {
 Pass {
	  ZTest Always Cull Off ZWrite Off
	  Fog { Mode off }      

      CGPROGRAM
	  #pragma debug
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma vertex vert
      #pragma fragment frag
      ENDCG
  }
}

Fallback off
	

}