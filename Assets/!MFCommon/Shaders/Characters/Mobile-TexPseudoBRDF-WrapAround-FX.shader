Shader "MADFINGER/Characters/BRDFLit FX (Supports Backlight)" {
Properties {
	_MainTex ("Base (RGB) Gloss (A)", 2D) = "grey" {}
	_BumpMap ("Normalmap", 2D) = "bump" {}
	_BRDFTex ("NdotL NdotH (RGB)", 2D) = "white" {}
	_NoiseTex ("Noise tex", 2D) = "white" {}
	_LightProbesLightingAmount("Light probes lighting amount", Range(0,1)) = 0.9
	_FXColor("FXColor", Color) = (0,0.97,0.89,1)
	_TimeOffs("Time offs",float) = 0
	_Duration("Duration",float) = 2
	_Invert("Invert",float) = 0
}	
SubShader { 


	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	
	Blend SrcAlpha OneMinusSrcAlpha

	LOD 400
	
CGPROGRAM
#pragma surface surf MyPseudoBRDF vertex:vert exclude_path:prepass nolightmap noforwardadd noambient approxview keepalpha

struct MySurfaceOutput {
	fixed3 Albedo;
	fixed3 Normal;
	fixed3 Emission;
	fixed Specular;
	fixed Gloss;
	fixed Alpha;
};


sampler2D _BRDFTex;

sampler2D _MainTex;
sampler2D _BumpMap;
sampler2D _NoiseTex;
fixed4 _FXColor;
float _TimeOffs;
float _Duration;
float _LightProbesLightingAmount;
float _Invert;
float _GlobalTime;

struct Input {
	float2 uv_MainTex;
	float2 uv_BumpMap;
	fixed3 SHLightingColor;
	fixed	Threshold;
};


 void vert (inout appdata_full v,out Input o) 
 {
	float3 wrldNormal	= mul((float3x3)_Object2World,v.normal);
	float3 SHLighting	= ShadeSH9(float4(wrldNormal,1));
	float  t			= saturate((_TimeOffs + _GlobalTime) / _Duration); 	
 
	o.SHLightingColor = saturate(SHLighting + (1 - _LightProbesLightingAmount).xxx);
	o.Threshold = _Invert > 0 ? 1 - t : t;
	o.uv_MainTex = v.texcoord;
	o.uv_BumpMap = v.texcoord;
 }


inline fixed4 LightingMyPseudoBRDF (MySurfaceOutput s, fixed3 lightDir, fixed3 viewDir, fixed atten)
{
	fixed3 halfDir = normalize(lightDir + viewDir);
	
	fixed nl = dot (s.Normal, lightDir);
	fixed nh = dot (s.Normal, halfDir);
	fixed4 l = tex2D(_BRDFTex, fixed2(nl * 0.5 + 0.5, nh));

	fixed4 c;
	
	c.rgb = s.Albedo * (l.rgb + s.Gloss * l.a) * 2;
	c.a = s.Alpha;
	
	
	return c;
}



void surf (Input IN, inout MySurfaceOutput o) {

	// Jedna z tech vetvi bude fungovat - zalezi na formatu te noise textury. Uz si nepamatuju - bude treba zkusit.
#if 0
	fixed noise 		= tex2D(_NoiseTex, IN.uv_MainTex * 2).a;
#else
	fixed noise 		= tex2D(_NoiseTex, IN.uv_MainTex * 2).r;
#endif

	fixed threshold	= IN.Threshold;
	fixed killDiff		= noise - threshold;

	
	fixed4	tex 		= tex2D(_MainTex, IN.uv_MainTex);
	fixed		border	= 1 - saturate(killDiff * 4);
	
	border *= border;
	border *= border;
	
	o.Albedo	= tex.rgb * IN.SHLightingColor;
	o.Gloss		= tex.a;
	o.Alpha		= noise > threshold;
	o.Normal	= tex2D(_BumpMap, IN.uv_BumpMap).rgb * 2.0 - 1.0;
	o.Emission	= _FXColor.xyz * border;
}
ENDCG

	}

FallBack "Diffuse"
}
