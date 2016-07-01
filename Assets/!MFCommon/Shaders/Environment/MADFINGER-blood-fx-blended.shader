Shader "MADFINGER/FX/Blood FX blended" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
	
	//
	// Params.x = normalized health
	// Params.y = hearbeat freq scale
	//
	
	_Params("Params",Vector) = (0,1,0,0)
	_ColorBooster("Color booster",Color) = (0.5,0,0,0)
}

SubShader {

	Tags { "Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Transparent" }
	
	Blend DstColor SrcColor
	Cull Off 
	Lighting Off 
	ZWrite Off 
	ZTest Always
	Fog { Color (0,0,0,0) }

	CGINCLUDE
	#include "UnityCG.cginc"
	sampler2D	_MainTex;
	float4		_Params;
	float4		_ColorBooster;
	
	
	struct v2f {
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
		fixed4 color: COLOR;
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
		o.uv	= v.texcoord.xy;
		o.color = fixed4(_ColorBooster.xyz * heartBeatColMod * v.color.a * heartBeat,v.color.a);

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
			return tex2D(_MainTex, i.uv.xy);// * i.color;
		}
		ENDCG 
	}	
}
}
