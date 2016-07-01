

Shader "Hidden/EdgeBlurApply" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
	_EdgeTex ("_EdgeTex", 2D) = "white" {}
}

SubShader {
	Pass {
		ZTest Always Cull Off ZWrite Off
		Fog { Mode off }

CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag
#pragma fragmentoption ARB_precision_hint_fastest 

#include "UnityCG.cginc"

uniform sampler2D _MainTex;
uniform sampler2D _EdgeTex;

uniform float4 _MainTex_TexelSize;

float filterRadius;

struct v2f {
	float4 pos : POSITION;
	float2 uv : TEXCOORD0;
};

v2f vert( appdata_img v )
{
	v2f o;
	o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
	float2 uv = v.texcoord.xy;
	o.uv.xy = uv;
	
	return o;
}

half4 frag (v2f i) : COLOR
{
	const float2 poisson[8] = {
			float2( 0.0, 0.0),
			float2( 0.527837,-0.085868),
			float2(-0.040088, 0.536087),
			float2(-0.670445,-0.179949), 
			float2(-0.419418,-0.616039),
			float2( 0.440453,-0.639399),
			float2(-0.757088, 0.349334),
			float2( 0.574619, 0.685879),
	};	
	
	half4 color = tex2D(_MainTex,  i.uv.xy);
	half edges = color.a;
	
	for(int j = 0; j < 8; j++) { 
		float2 coordHigh = i.uv.xy + (_MainTex_TexelSize.xy * poisson[j] * filterRadius);
		color += tex2D(_MainTex, coordHigh);
	}		
	
	color /= 9.0;
	
	color = lerp(color, tex2D(_MainTex, i.uv.xy),  edges);	
	return color;
}
ENDCG
	}
}

Fallback off

}