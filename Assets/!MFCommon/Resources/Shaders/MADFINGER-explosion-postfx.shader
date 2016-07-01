Shader "MADFINGER/PostFX/ExplosionFX" { 

	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}		
		_UVOffsAndAspectScale("UVOffsAndAspectScale",Vector) = (0,0,0,0)
		
		_Wave0ParamSet0("Wave0ParamSet0",Vector) = (0,0,0,0)
		_Wave0ParamSet1("Wave0ParamSet1",Vector) = (0,0,0,0)
		
		_Wave1ParamSet0("Wave1ParamSet0",Vector) = (0,0,0,0)
		_Wave1ParamSet1("Wave1ParamSet1",Vector) = (0,0,0,0)
		
		_Wave2ParamSet0("Wave2ParamSet0",Vector) = (0,0,0,0)
		_Wave2ParamSet1("Wave2ParamSet1",Vector) = (0,0,0,0)
		
		_Wave3ParamSet0("Wave3ParamSet0",Vector) = (0,0,0,0)
		_Wave3ParamSet1("Wave3ParamSet1",Vector) = (0,0,0,0)
	}
		
	// Shader code pasted into all further CGPROGRAM blocks
	CGINCLUDE

	#include "UnityCG.cginc"
	
//	#define DBG_SHOW_WAVE
	#define ENABLE_WAVE_COLORIZATION
	
	struct v2f {
		float4 pos : POSITION;
		float2 uv : TEXCOORD0;
		
#if defined(ENABLE_WAVE_COLORIZATION)		
		fixed4 col : COLOR;
#endif
	};
		
	sampler2D	_MainTex;
	float4		_UVOffsAndAspectScale;
	float4		_Wave0ParamSet0;
	float4		_Wave0ParamSet1;
	float4		_Wave1ParamSet0;
	float4		_Wave1ParamSet1;
	float4		_Wave2ParamSet0;
	float4		_Wave2ParamSet1;
	float4		_Wave3ParamSet0;
	float4		_Wave3ParamSet1;
	
	//
	// paramsSet0.xy	- wave center (normalized coords)
	// paramsSet0.z		- wave amplitude
	// paramsSet0.w		- wave frequency
	//
	// paramsSet1.x		- wave distance attenuation
	// paramsSet1.y		- wave speed
	// paramsSet1.z		- wave start time
	// paramsSet1.w		- wave time att const
	//
	
	float Wave(float time)
	{
		float timeAtt = 1.f / (1 + time * time);
		//float timeAtt = pow(0.5f,time);
	
		return sin(time) * timeAtt;
	}

	float Impulse(float x,float k)
	{
		float h = k * x;
		
    	return h * exp(1 - h);
	}

	float2 EvalWave(float4 paramsSet0,float4 paramsSet1,float2 uv,out float4 color)	
	{
		float2	diff		= (uv - paramsSet0) * _UVOffsAndAspectScale.zw;
		float	dist		= length(diff);
		float	time		= max(_Time.y - paramsSet1.z - dist / paramsSet1.y,0);		
		float	w			= Wave(time * paramsSet0.w) * paramsSet0.z;
		float	distAtt		= saturate(dist * paramsSet1.x);
		
#if defined(ENABLE_WAVE_COLORIZATION)

		float	timec		= max(_Time.y - paramsSet1.z,0);		
		
		float	i			= Impulse(timec * 2.5,6.5) + 0.0001f;
		float	rbase		= 1.f / paramsSet1.x;
		
		rbase = rbase > 0.65f ? rbase * 2 : rbase;
		
		float	r			= rbase * i;
		
		float4	col0		= fixed4(1,1,1,1);
		float4	col1		= fixed4(0.5,0.3,0,1);
		float	datt		= 1 - saturate(dist / r);

		datt *= datt;		

		color = datt * paramsSet1.x * lerp(col1,col0,i) * 1.5 * i;	

#endif // ENABLE_WAVE_COLORIZATION

	
		return w * (diff / dist) * (1 - distAtt * distAtt);
	}
	

	v2f vert( appdata_img v ) 
	{
		v2f 	o;
		float2	uv = v.vertex.xy;
		float4	col0;
		float4	col1;
		float4	col2;
		float4	col3;

		float2 wave =	EvalWave(_Wave0ParamSet0,_Wave0ParamSet1,uv,col0) + 
						EvalWave(_Wave1ParamSet0,_Wave1ParamSet1,uv,col1) +
						EvalWave(_Wave2ParamSet0,_Wave2ParamSet1,uv,col2) +
						EvalWave(_Wave3ParamSet0,_Wave3ParamSet1,uv,col3);

		o.pos	= float4(v.vertex.xy * 2 - float2(1,1),0,1);
		o.uv.xy	= v.vertex.xy + _UVOffsAndAspectScale.xy + wave;
		
		#if defined(DBG_SHOW_WAVE)
		o.uv.xy	= wave * 10;
		#endif
		
		
#if defined(ENABLE_WAVE_COLORIZATION)		
		o.col = col0 + col1 + col2 + col3;
#endif

		return o;
	}
	
	fixed4 frag(v2f i) : COLOR 	
	{
	#if defined(DBG_SHOW_WAVE)
		return abs(fixed4(i.uv.xy,0,1));
	#else
	
	#if defined(ENABLE_WAVE_COLORIZATION)
		return tex2D(_MainTex,i.uv) + i.col;
	#else
		return tex2D(_MainTex,i.uv);
	#endif

	#endif
	}

	ENDCG
	
Subshader {
 Pass {
	  ZTest Always Cull Off ZWrite Off
	  Fog { Mode off }      

      CGPROGRAM
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma vertex vert
      #pragma fragment frag
      ENDCG
  }
}

Fallback off
	

}