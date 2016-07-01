Shader "MADFINGER/PostFX/ColorCorrectionSimple" { 

	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}		
		_Params("x : flip image",Vector) = (0,0,0,0)
	}
	
	// Shader code pasted into all further CGPROGRAM blocks
	CGINCLUDE

	#include "UnityCG.cginc"
	
	struct v2f {
		float4 pos : POSITION;
		float2 uv : TEXCOORD0;
	};
		
	sampler2D	_MainTex;
	fixed4		_ColorBias;		
	float4		_Params;
		
	v2f vert( appdata_img v ) 
	{
		v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		o.uv =  v.texcoord.xy;
		
		if (_Params.x > 0)
		{
			o.uv.y = 1 - o.uv.y;
		}
		
		return o;
	}
	
	fixed4 frag(v2f i) : COLOR 	
	{
		return tex2D(_MainTex, i.uv) + _ColorBias;
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