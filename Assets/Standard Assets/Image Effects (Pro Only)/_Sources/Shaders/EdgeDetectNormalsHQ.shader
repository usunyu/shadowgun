

Shader "Hidden/EdgeDetectNormalsHQ" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
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
sampler2D _CameraDepthNormalsTexture;
uniform float4 _MainTex_TexelSize;

uniform float4 sensitivity; 

float edgesOnly;
float4 edgesOnlyBgColor;

struct v2f {
	float4 pos : POSITION;
	float4 uv1 : TEXCOORD0;
	float4 uv2 : TEXCOORD1;
	float4 uv3 : TEXCOORD2;
};

v2f vert( appdata_img v )
{
	v2f o;
	o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
	float2 uv = v.texcoord.xy;
	o.uv1.xy = uv;
	
	// On D3D when AA is used, the main texture & scene depth texture
	// will come out in different vertical orientations.
	// So flip sampling of depth texture when that is the case (main texture
	// texel size will have negative Y).
	#if SHADER_API_D3D9
	if (_MainTex_TexelSize.y < 0)
		uv.y = 1-uv.y;
	#endif
	
	o.uv1.xy = uv;
	
	o.uv1.zw = uv + float2(-_MainTex_TexelSize.x, -_MainTex_TexelSize.y) * sensitivity.z;
	o.uv2.xy = uv + float2(+_MainTex_TexelSize.x, -_MainTex_TexelSize.y) * sensitivity.z;
	o.uv2.zw = uv + float2(+_MainTex_TexelSize.x, +_MainTex_TexelSize.y) * sensitivity.z;
	o.uv3.xy = uv + float2(-_MainTex_TexelSize.x, +_MainTex_TexelSize.y) * sensitivity.z;	
	o.uv3.zw = float2(0,0);

	return o;
}

inline half CheckSame (half2 centerNormal, float centerDepth, half4 sample)
{
	// difference in normals
	// do not bother decoding normals - there's no need here
	half2 diff = abs(centerNormal - sample.xy) * sensitivity.y;
	half isSameNormal = (diff.x + diff.y) * sensitivity.y < 0.1;
	// difference in depth
	float sampleDepth = DecodeFloatRG (sample.zw);
	float zdiff = abs(centerDepth-sampleDepth);
	// scale the required threshold by the distance
	half isSameDepth = zdiff * sensitivity.x < 0.09 * centerDepth;

	// return:
	// 1 - if normals and depth are similar enough
	// 0 - otherwise
	
	return isSameNormal * isSameDepth;
}

half4 frag (v2f i) : COLOR
{
	half4 original;
	
	// use edgesOnly 
	original = lerp(tex2D(_MainTex, i.uv1.xy),edgesOnlyBgColor, edgesOnly);

	half4 center = tex2D (_CameraDepthNormalsTexture, i.uv1.xy);
	half4 sample1 = tex2D (_CameraDepthNormalsTexture, i.uv1.zw);
	half4 sample2 = tex2D (_CameraDepthNormalsTexture, i.uv2.xy);
	half4 sample3 = tex2D (_CameraDepthNormalsTexture, i.uv2.zw);
	half4 sample4 = tex2D (_CameraDepthNormalsTexture, i.uv3.xy);



	// encoded normal
	half2 centerNormal = center.xy;
	// decoded depth
	float centerDepth = DecodeFloatRG (center.zw);
	
	original.a = 1.0;
	original.a *= CheckSame(centerNormal, centerDepth, sample1);
	original.a *= CheckSame(centerNormal, centerDepth, sample2);
	original.a *= CheckSame(centerNormal, centerDepth, sample3);
	original.a *= CheckSame(centerNormal, centerDepth, sample4);
			
	return original;
}
ENDCG
	}
}

Fallback off

}