Shader "MADFINGER/FX/Blood FX - alpha blended" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
	_FadeInSpeed("Fade in speed",float) = 5
	_DrippingSpeed("Dripping speed",float) = 0.1
}

SubShader {

	Tags { "Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Transparent" }
	
	Blend SrcAlpha OneMinusSrcAlpha
//	Blend DstColor Zero
	Cull Off 
	Lighting Off 
	ZWrite Off 
	Fog { Color (0,0,0,0) }

	CGINCLUDE
	#include "UnityCG.cginc"
	sampler2D _MainTex;
	sampler2D _Normal;
	
	
	struct v2f {
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
		fixed4 col: COLOR;
	};

	float _FadeInSpeed;
	float _DrippingSpeed;

	
	v2f vert (appdata_full v)
	{
		float time 		= (_Time.y + v.vertex.z) ;
		float duration = v.texcoord1.x;
		float fadeout	= 1 - max(time - 0.25 * duration,0);
	
		v2f o;
		
		float	threshold = saturate(1 - time * _FadeInSpeed) ;
		
		o.pos	= float4(v.vertex.xy,0,1);
		o.uv	= v.texcoord.xy;
		o.col	= fixed4(1,1,1,fadeout);

		o.pos.y -= time * _DrippingSpeed * v.texcoord1.y;

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
			return tex2D (_MainTex, i.uv.xy) * i.col;
		}
		ENDCG 
	}	
}
}
