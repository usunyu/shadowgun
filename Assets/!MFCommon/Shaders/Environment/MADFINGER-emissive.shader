
Shader "MADFINGER/Environment/Emissive" {


Properties {
	_MainTex ("Base texture", 2D) = "white" {}
	_IntensityScaleBias ("Intensity scale X / bias Y", Vector) = (1,0.1,0,0)
}

	
SubShader {
	
	
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	
	Blend One One
	Cull Off Lighting Off ZWrite Off Fog { Color (0,0,0,0) }
	
	LOD 100
	
	CGINCLUDE	
	#include "UnityCG.cginc"
	sampler2D _MainTex;
	
	float2 _IntensityScaleBias;
	float4 _MainTex_ST;
	
	struct v2f {
		float4 pos : SV_POSITION;
		float2 uv: TEXCOORD0;
		fixed4 color : TEXCOORD1;
	};

	v2f vert (appdata_full v)
	{
		v2f 		o;
		
		o.uv	= TRANSFORM_TEX(v.texcoord.xy,_MainTex);
		o.pos	= mul(UNITY_MATRIX_MVP, v.vertex);
		o.color	= v.color * _IntensityScaleBias.x + _IntensityScaleBias.y;
			
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
			return tex2D (_MainTex, i.uv.xy) * i.color;
		}
		ENDCG 
	}	
}


}

