
Shader "MADFINGER/Environment/Blinking emissive" {


Properties {
	_MainTex ("Base texture", 2D) = "white" {}
	_IntensityScaleBias ("Intensity scale X / bias Y", Vector) = (1,0.1,0,0)
	_SwitchOnOffDuration("Switch ON (X) / OFF (Y) duration", Vector) = (1,3,0,0)
	_BlinkingRate("Blinking rate",Float) = 10	
	_RndGridSize("Randomization grid size",Float) = 5
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
	float2 _SwitchOnOffDuration;
	float _BlinkingRate;	
	float _RndGridSize;
	float4 _MainTex_ST;

	
	struct v2f {
		float4 pos : SV_POSITION;
		float2 uv: TEXCOORD0;
		fixed4 color : TEXCOORD1;
	};

	float FakeNoise(float time,float seed)
	{
		return abs(cos(17 * sin(time * 5) +  10 * sin(seed + time * 3 + 7.993f)));
	}
	
	float TriangleWave(float time,float T)
	{
		float f = fmod(time,T);
			
		return min(T - f,f);
	}
	
	v2f vert (appdata_full v)
	{
		v2f 		o;
		float		time 			= _Time.y;
		float		seed			= dot(v.color.xyzw,v.color.xyzw) * 40;
		float		rnd			= FakeNoise(time * _BlinkingRate,seed) > 0.5f;
		float2	swOnOff	= _SwitchOnOffDuration * (0.8 + 0.4f * frac(seed));
		
		rnd *= fmod(_Time.y + seed,swOnOff.x + swOnOff.y) < swOnOff.x;
		
		o.uv	= TRANSFORM_TEX(v.texcoord.xy,_MainTex);
		o.pos	= mul(UNITY_MATRIX_MVP, v.vertex);
		o.color	= v.color * rnd * _IntensityScaleBias.x + _IntensityScaleBias.y;
			
		
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

