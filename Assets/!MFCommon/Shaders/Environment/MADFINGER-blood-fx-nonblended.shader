Shader "MADFINGER/FX/Blood FX nonblended" {
Properties {
	//
	// Params.x = normalized health
	// Params.y = hearbeat freq scale
	//
	
	_Params("Params",Vector) = (0,1,0,0)
	_Color("Color",Color) = (0,0,0,0)
}

SubShader {

	Tags { "Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Transparent" }
	
	Cull Off 
	Lighting Off 
	ZWrite Off 
	ZTest Always
	Fog { Color (0,0,0,0) }

	CGINCLUDE
	#include "UnityCG.cginc"
	float4		_Params;
	float4		_Color;
	
	
	struct v2f {
		float4 pos : SV_POSITION;
	};


	float Impulse(float k,float x)
	{
		float h = k * x;
		
		return h * exp(1 - h);
	}

	float HeartBeatWave(float time,float T)
	{
		return Impulse(20,fmod(time,T));
	}
	
	
	v2f vert (appdata_full v)	
	{
		v2f 	o;
		float2	mdir = normalize(-v.vertex.xy);
		
		const float heartBeatDuration	= 0.5;
		
		float	health			= _Params.x;		
		float	heartBeatTime	= fmod(_Time.y * _Params.y,1.25);
		float	heartBeat 		= heartBeatTime < heartBeatDuration ? HeartBeatWave(heartBeatTime,heartBeatDuration) : 0;
		float	heartBeatMod	= health > 0 ? lerp(0.1f,0,health) : 0;
		float	heartBeatColMod	= health > 0 ? lerp(1,0,health) : 0;
		
	
		o.pos	= float4(v.vertex.xy * 2,0,1);
		o.pos.xy -= mdir * (health + heartBeat * heartBeatMod) * v.color.a;
							
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
			return _Color;
		}
		ENDCG 
	}	
}
}
