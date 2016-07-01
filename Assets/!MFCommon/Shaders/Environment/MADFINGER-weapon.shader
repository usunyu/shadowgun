// - Unlit
// - Per-vertex gloss

Shader "MADFINGER/Environment/weapon" {
Properties {
	_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
	_Shininess ("Shininess", Range (0.01, 1)) = 0.078125
	_SHLightingScale("LightProbe influence scale",float) = 1
	_FakeProbeTopColor("Fake light probe top color", Color) = (1,0,0,1)
	_FakeProbeBotColor("Fake light probe bottom color", Color) = (0,1,0,1)
}

SubShader {
	Tags { "RenderType"="Opaque" "LightMode"="ForwardBase"}
	LOD 100



	CGINCLUDE
	#pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON UNITY_IPHONE
	#include "UnityCG.cginc"
	sampler2D _MainTex;
	float4 _MainTex_ST;


	float3 _SpecDir;
	float3 _SpecColor;
	float _Shininess;
	float _SHLightingScale;
	float4	_FakeProbeTopColor;
	float4	_FakeProbeBotColor;


	struct v2f {
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
		fixed3 spec : TEXCOORD1;
		fixed3 SHLighting: TEXCOORD2;
	};


	v2f vert (appdata_full v)
	{
		v2f o;
		//o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);

		o.uv = v.texcoord;
		float3 worldNormal = normalize(mul((float3x3)_Object2World, v.normal));
		float3 worldV = normalize(-WorldSpaceViewDir(v.vertex));
		float3 refl = reflect(worldV, worldNormal);
		float3 shl = ShadeSH9(float4(worldNormal,1));

		float3 worldLightDir = _WorldSpaceLightPos0;

#if !defined(UNITY_IPHONE) // this breaks performance on IOS6 horribly

		// lame attempt to perform depth clamping
		o.pos.z = max(o.pos.z,0);

#endif

		o.spec = normalize(shl) * pow(saturate(dot(worldLightDir, refl)), _Shininess * 128) * 2;

		o.SHLighting	= lerp(shl * _SHLightingScale,lerp(_FakeProbeBotColor.xyz,_FakeProbeTopColor.xyz,worldNormal.y * 0.5 + 0.5),step(_SHLightingScale,0.001f));
//		o.SHLighting	= _SHLightingScale > 0 ? shl * _SHLightingScale : lerp(_FakeProbeBotColor.xyz,_FakeProbeTopColor.xyz,worldNormal.y * 0.5 + 0.5);

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
			fixed4 c	= tex2D (_MainTex, i.uv);

			c.rgb *= i.SHLighting;
			c.rgb += i.spec.rgb * c.a;

			return c;
		}
		ENDCG
	}
}
}

