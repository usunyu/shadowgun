Shader "VRPanorama/VRAA2" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
	_U ("U", Float ) = 215
    _V ("V", Float ) = 512
}

SubShader {
	Pass {
		ZTest Always Cull Off ZWrite Off
				
CGPROGRAM
#pragma vertex vert_img
#pragma fragment frag
#include "UnityCG.cginc"

uniform sampler2D _MainTex;
            uniform float _U;
            uniform float _V;

fixed4 frag (v2f_img i) : SV_Target
{
	float _TXWidth = (1.0f / _U);
	float _TXHeight = (1.0f / _V);
	
	
	fixed4 original = tex2D(_MainTex, i.uv);
	
	            float4 c1 = tex2D(_MainTex, i.uv);
				float4 c2 = tex2D(_MainTex, i.uv + float2(_TXWidth, 0.0f));
				float4 c3 = tex2D(_MainTex, i.uv + float2(0.0f, _TXHeight));
				float4 c4 = tex2D(_MainTex, i.uv + float2(-_TXWidth , 0.0f));
				float4 c5 = tex2D(_MainTex, i.uv + float2(0.0f , -_TXHeight));
				
				
				float4 d2 = tex2D(_MainTex, i.uv + float2(_TXWidth*0.71, _TXHeight*0.71));
				float4 d3 = tex2D(_MainTex, i.uv + float2(-_TXWidth*0.71, _TXHeight*0.71));
				float4 d4 = tex2D(_MainTex, i.uv + float2(-_TXWidth *0.71 , -_TXHeight*0.71));
				float4 d5 = tex2D(_MainTex, i.uv + float2(_TXWidth *0.71 , -_TXHeight *0.71));
                
                float4 _Col = (c1*2 + c2 + c3 + c4 + c5 + d2 + d3 + d4 + d5) / 10.0f;
	

	float4 output = pow(_Col, 1/2.1);
//	float4 output = _Col;
	output.a = original.a;
	return output;
}
ENDCG

	}
}

Fallback off

}
