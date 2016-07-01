Shader "MADFINGER/Environment/Skybox - opaque - no fog" {

Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
}

SubShader {
	Tags {"Queue"="Geometry+10" "RenderType"="Opaque" }
	
	Lighting Off Fog { Mode Off }
	ZWrite Off
	
	LOD 100
	
		
	CGINCLUDE
	#pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
	#include "UnityCG.cginc"

	sampler2D _MainTex;
	
	struct v2f {
		float4 pos	: SV_POSITION;
		float2 uv 	: TEXCOORD0;
	};

	
	v2f vert (appdata_full v)
	{
		v2f o;

		o.pos	= mul(UNITY_MATRIX_MVP, v.vertex);
		o.uv 	= v.texcoord.xy;

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
			return  tex2D(_MainTex,i.uv.xy);
		}
		ENDCG 
	}	
}
}
