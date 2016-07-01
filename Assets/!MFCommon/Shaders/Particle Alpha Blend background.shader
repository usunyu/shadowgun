Shader "MADFINGER/Particles/Alpha Blended - background" {
Properties {
	_MainTex ("Particle Texture", 2D) = "white" {}
	_Params("x - enable force depth, y - forced depth",Vector) = (0,0,0,0)
}


SubShader 
{
	Tags { "Queue" = "Transparent" }
	Blend SrcAlpha OneMinusSrcAlpha
	Cull Off Lighting Off Fog { Mode Off }
	ZWrite Off
	
			
	CGINCLUDE
	#include "UnityCG.cginc"

	sampler2D	_MainTex;
	float4		_Params;
	
	struct v2f 
	{
		float4 pos	: SV_POSITION;
		float2 uv	: TEXCOORD0;
		fixed4 color: COLOR;
	};

	
	v2f vert (appdata_full v)
	{
		v2f o;

		o.pos	= mul(UNITY_MATRIX_MVP, v.vertex);		
		o.uv	= v.texcoord;

		o.pos.z = _Params.x > 0 ? _Params.y : o.pos.z;
		o.color = v.color;
		
		return o;
	}
	ENDCG


	Pass {
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma fragmentoption ARB_precision_hint_fastest		

		fixed4 frag (v2f i) : COLOR		
		{
			return tex2D(_MainTex,i.uv) * i.color;
		}
		ENDCG 
	}	
}
}