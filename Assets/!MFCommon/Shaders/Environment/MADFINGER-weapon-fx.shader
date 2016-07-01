// - Unlit
// - Per-vertex gloss

Shader "MADFINGER/Environment/weapon FX" {
Properties {
	_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
	_NoiseTex ("Noise tex", 2D) = "white" {}
	_Shininess ("Shininess", Range (0.01, 1)) = 0.078125
	_SHLightingScale("LightProbe influence scale",float) = 1
	_FakeProbeTopColor("Fake light probe top color", Color) = (1,0,0,1)
	_FakeProbeBotColor("Fake light probe bottom color", Color) = (0,1,0,1)
	_FXColor("FXColor", Color) = (0,0.97,0.89,1)
	_TimeOffs("Time offs",float) = 0
	_Duration("Duration",float) = 2
	_Invert("Invert",float) = 0	
}

SubShader {

	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	
	Blend SrcAlpha OneMinusSrcAlpha
	
	LOD 100



	CGINCLUDE
	#pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON UNITY_IPHONE
	#include "UnityCG.cginc"
	sampler2D _MainTex;
	sampler2D _NoiseTex;
	float4 _MainTex_ST;


	float3	_SpecDir;
	float3	_SpecColor;
	float	_Shininess;
	float	_SHLightingScale;
	float4	_FakeProbeTopColor;
	float4	_FakeProbeBotColor;
	fixed4	_FXColor;
	float 	_TimeOffs;
	float 	_Duration;
	float 	_LightProbesLightingAmount;
	float	_Invert;
	float	_GlobalTime;


	struct v2f {
		float4 pos 			: SV_POSITION;
		float2 uv 			: TEXCOORD0;
		fixed3 spec 		: TEXCOORD1;
		fixed3 SHLighting	: TEXCOORD2;
		fixed4 Threshold	: TEXCOORD3;
	};


	v2f vert (appdata_full v)
	{
		v2f o;
		//o.pos = mul(UNITY_MATRIX_MVP, v.vertex);

		float4 out_pos = mul(UNITY_MATRIX_MVP, v.vertex);
		
		
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);

		o.uv = v.texcoord;
		float3 worldNormal = normalize(mul((float3x3)_Object2World, v.normal));
		float3 worldV = normalize(-WorldSpaceViewDir(v.vertex));
		float3 refl = reflect(worldV, worldNormal);
		float3 shl = ShadeSH9(float4(worldNormal,1));

		float3 worldLightDir	= _WorldSpaceLightPos0;
		float  t				= saturate((_TimeOffs + _GlobalTime) / _Duration); 	
		
#if !defined(UNITY_IPHONE) // this breaks performance on IOS6 horribly

		// lame attempt to perform depth clamping
		o.pos.z = max(o.pos.z,0);
#endif

		o.spec = normalize(shl) * pow(saturate(dot(worldLightDir, refl)), _Shininess * 128) * 2;

		o.SHLighting	= lerp(shl * _SHLightingScale,lerp(_FakeProbeBotColor.xyz,_FakeProbeTopColor.xyz,worldNormal.y * 0.5 + 0.5),step(_SHLightingScale,0.001f));
//		o.SHLighting	= _SHLightingScale > 0 ? shl * _SHLightingScale : lerp(_FakeProbeBotColor.xyz,_FakeProbeTopColor.xyz,worldNormal.y * 0.5 + 0.5);
		o.Threshold		= _Invert > 0 ? 1 - t : t;

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
			fixed4	c			= tex2D (_MainTex, i.uv);
			fixed	noise 		= tex2D(_NoiseTex, i.uv * 2);
			fixed	threshold	= i.Threshold.x;
			fixed	killDiff	= noise - threshold;
			fixed	border	= 1 - saturate(killDiff * 4);
	
			border *= border;
			border *= border;


			c.rgb *= i.SHLighting;
			c.rgb += i.spec.rgb * c.a;
			c.rgb += _FXColor.xyz * border;
			
			c.a	= noise > threshold;
			
			return c;
		}
		ENDCG
	}
}
}

