Shader "MADFINGER/Environment/Lightprobes with Gloss Per-Vertex Additive" {
Properties {
	_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
	_SpecDir ("Specular Direction", Vector) = (1, 1, 0, 0)
	_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
	_Shininess ("Shininess", Range (0.01, 1)) = 0.078125
	_SHLightingScale("LightProbe influence scale",float) = 1
}

SubShader {
	Tags { "RenderType"="Opaque" "LightMode"="ForwardBase"}
	LOD 100
	
	
	
	CGINCLUDE
	#pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
	#include "UnityCG.cginc"
	sampler2D _MainTex;
	float4 _MainTex_ST;
	
	
	float3 _SpecDir;
	float3 _SpecColor;
	float _Shininess;
	float _SHLightingScale;
	
	struct v2f {
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
		float3 refl : TEXCOORD1;
		fixed3 spec : TEXCOORD3;
		fixed3 SHLighting: TEXCOORD4;
	};

	
	v2f vert (appdata_full v)
	{
		v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		o.uv = v.texcoord;
		float3 worldNormal = mul((float3x3)_Object2World, v.normal);
		float3 worldV = normalize(-WorldSpaceViewDir(v.vertex));
		o.refl = reflect(worldV, worldNormal);
		
		float3 worldLightDir = normalize(_SpecDir);
		o.spec = _SpecColor * pow(saturate(dot(worldLightDir, o.refl)), _Shininess * 128) * 2;
		
		o.SHLighting	= ShadeSH9(float4(worldNormal,1)) * _SHLightingScale;
		
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

