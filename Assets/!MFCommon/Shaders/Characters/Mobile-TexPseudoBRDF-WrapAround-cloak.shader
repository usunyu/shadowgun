Shader "MADFINGER/Characters/BRDFLit + Cloak FX" {
Properties {
	_MainTex ("Base (RGB) Gloss (A)", 2D) = "grey" {}
	_BumpMap ("Normalmap", 2D) = "bump" {}
	_BRDFTex ("NdotL NdotH (RGB)", 2D) = "white" {}
	_NoiseTex ("Noise tex", 2D) = "white" {}
	_LightProbesLightingAmount("Light probes lighting amount", Range(0,1)) = 0.9
	_EffectAmount("Effect amount",range(0,1)) = 0		
	_FXColor("FXColor", Color) = (0,0.97,0.89,1)
	_RefrTint("Refraction color",Color) = (1,1,1,1)	
	_SpecularStrength("Specular strength weights", Vector) = (0,0,0,1)
	_Params("x - scroll speed, y - fluctuation speed, z - refraction bumps",Vector) = (0.2,5,0.02,0)
}	
SubShader { 


	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	
	Blend SrcAlpha OneMinusSrcAlpha

	LOD 400
	
//	GrabPass { "_GrabTex" }

	
CGPROGRAM
#pragma surface surf MyPseudoBRDF vertex:vert exclude_path:prepass nolightmap noforwardadd noambient approxview keepalpha
#pragma multi_compile UNITY_SHADER_DETAIL_LOW UNITY_SHADER_DETAIL_MEDIUM UNITY_SHADER_DETAIL_HIGH UNITY_SHADER_DETAIL_VERY_HIGH


#include "../globals.cginc"
#include "../gpu_noise_lib.cginc"


struct MySurfaceOutput {
	fixed3 Albedo;
	fixed3 Normal;
	fixed3 Emission;
	fixed Specular;
	fixed Gloss;
	fixed Alpha;
	fixed Border;
	fixed3 Refr;
};


sampler2D	_BRDFTex;
sampler2D	_MainTex;
sampler2D	_BumpMap;
sampler2D	_NoiseTex;
sampler2D	_GrabTex;

float4		_NoiseTex_ST;
fixed4		_FXColor;
float 		_LightProbesLightingAmount;
float4		_SpecularStrength;
float		_EffectAmount;
float4		_Params;
float4		_RefrTint;

struct Input 
{
	float4	UV;
//	float4	GrabProjUV;
	fixed4	SHLightingColorAndThreshold;
};


 void vert (inout appdata_full v,out Input o) 
 {
	float3	wrldNormal	= mul((float3x3)_Object2World,v.normal);
	float3	SHLighting	= ShadeSH9(float4(wrldNormal,1));
	float	time		= _Time.y;
 	
	o.SHLightingColorAndThreshold.xyz	= saturate(SHLighting + (1 - _LightProbesLightingAmount).xxx);
	o.SHLightingColorAndThreshold.w		= _EffectAmount;
	
	
	float2	noiseUV		= TRANSFORM_TEX(v.texcoord,_NoiseTex);
	float2	noiseUVOffs	= time * _Params.x;
	
	o.UV.xy	= v.texcoord;
	o.UV.zw	= noiseUV + frac(noiseUVOffs);

	float noise 			= SimplexPerlin2D((noiseUV + noiseUVOffs) * _Params.y);
	float fluctuationAmount	= sin(_EffectAmount * PI);
	
	o.SHLightingColorAndThreshold.w = saturate(o.SHLightingColorAndThreshold.w + noise * fluctuationAmount);
	o.SHLightingColorAndThreshold.w = o.SHLightingColorAndThreshold.w * 2 - 1;
	
//	o.GrabProjUV = ComputeGrabScreenPos(mul(UNITY_MATRIX_MVP, v.vertex));
 }


inline fixed4 LightingMyPseudoBRDF (MySurfaceOutput s, fixed3 lightDir, fixed3 viewDir, fixed atten)
{
	fixed3 halfDir = normalize(lightDir + viewDir);
	
	fixed nl = dot (s.Normal, lightDir);
	fixed nh = dot (s.Normal, halfDir);
	fixed4 l = tex2D(_BRDFTex, fixed2(nl * 0.5 + 0.5, nh));

	fixed4 c;
	
/*	
#if defined(UNITY_SHADER_DETAIL_HIGH) || defined(UNITY_SHADER_DETAIL_VERY_HIGH)	
	c.rgb	= lerp(s.Albedo * (l.rgb + s.Gloss * l.a) * 2,s.Refr,s.Border);
	c.a		= 1;
#else
	c.rgb	= s.Albedo * (l.rgb + s.Gloss * l.a) * 2;
	c.a		= max(1 - s.Border,0.1);
#endif
*/
	c.rgb	= s.Albedo * (l.rgb + s.Gloss * l.a) * 2;
	c.a		= 1 - s.Border;
		
	return c;
}


void surf (Input IN, inout MySurfaceOutput o) {

	fixed4 noise 		= tex2D(_NoiseTex, IN.UV.zw);
	fixed threshold		= IN.SHLightingColorAndThreshold.w;
	fixed killDiff		= noise.z + threshold;

	
	fixed4	tex 	= tex2D(_MainTex, IN.UV.xy);
	fixed	border	= saturate(killDiff * 4);
	
	border *= border;
	border *= border;

	
				
	o.Albedo	= tex.rgb * IN.SHLightingColorAndThreshold.xyz;
	o.Gloss		= dot(_SpecularStrength,tex);
	o.Normal	= tex2D(_BumpMap, IN.UV.xy).rgb * 2.0 - 1.0;	
	o.Border	= border;
	o.Emission	= _FXColor * border * 4;
	
/*	
#if defined(UNITY_SHADER_DETAIL_HIGH) || defined(UNITY_SHADER_DETAIL_VERY_HIGH)
	float4	screenUV	= IN.GrabProjUV;
			
	screenUV.xy += noise.xy * _Params.z * IN.GrabProjUV.w;
		
	o.Refr		= tex2Dproj(_GrabTex,UNITY_PROJ_COORD(screenUV)) * _RefrTint;
	o.Emission	*= (1 - border);
#endif
*/

}
ENDCG

	}

FallBack "Diffuse"
}
