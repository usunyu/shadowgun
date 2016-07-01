Shader "MADFINGER/Environment/Simple diffuse" 
{

Properties 
{
	_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
	_Color("Color (multiplied by 2)",Color) = (1,1,1,1)
}

SubShader 
{
	Tags { "RenderType"="Opaque"}
	LOD 100
	
			
	CGINCLUDE
	#include "UnityCG.cginc"
	sampler2D	_MainTex;
	fixed4		_Color;
	
	struct v2f 
	{
		float4 pos	: SV_POSITION;
		float2 uv	: TEXCOORD0;
		fixed4 col	: TEXCOORD1;
	};

	
	v2f vert (appdata_full v)
	{
		v2f o;

		o.pos	= mul(UNITY_MATRIX_MVP, v.vertex);		
		o.uv	= v.texcoord;
		o.col	= _Color * 2;
		
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
			return tex2D(_MainTex,i.uv) * i.col;
		}
		ENDCG 
	}	
}
}
