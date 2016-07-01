Shader "MADFINGER/Characters/Character shadow plane - sphere AO" {
Properties {
	_Sphere0("S0",	Vector) = (0,0,0,0)
	_Sphere1("S0",	Vector) = (0,0,0,0)
	_Sphere2("S0",	Vector) = (0,0,0,0)
	_Intensity("Intensity",float) = 0.9
}

SubShader {

	Tags { "Queue"="Transparent-15" "IgnoreProjector"="True" "RenderType"="Transparent" }

	Blend DstColor Zero
	Cull Off Lighting Off ZWrite Off Fog { Color (0,0,0,0) }
	ColorMask RGB


	CGINCLUDE
	#include "UnityCG.cginc"

//	#define USE_PERPIXEL_AO




	
	struct v2f {
		float4 pos : SV_POSITION;
		// fixed should be enough as we want all components equal
		// but fixed suffers from some weird bug on adreno
		fixed4 color: COLOR;

		#if defined(USE_PERPIXEL_AO)
		float3 worldPos	: TEXCOORD0;
		float3 normal	: TEXCOORD1;
		#endif
	};

	float4	_Sphere0;
	float4	_Sphere1;
	float4	_Sphere2;
	float	_Intensity;

	float SphereAO(float4 sphere,float3 pos,float3 normal)
	{
		float3	dir = sphere.xyz - pos;
		float	d	= length(dir);
		float	v;

		dir /= d;

		v = (sphere.w / d);

		return dot(normal,dir) * v * v;
	}

	v2f vert (appdata_full v)
	{
		v2f			o;
		float3		wrldPos 	= mul(_Object2World,v.vertex);
		float3		wrldNormal	= mul((float3x3)_Object2World,v.normal);
		float		ao;

		o.pos	= mul(UNITY_MATRIX_MVP, v.vertex);

#if defined(USE_PERPIXEL_AO)
		o.worldPos = wrldPos;
		o.normal = wrldNormal;

		ao = 1 - v.color.r;
		o.color = fixed4(ao,ao,ao,ao);
#else

#if 1
		// quite suprisinly this looks better (probably there is some error in AO calculation)
		ao = 1 - saturate(SphereAO(_Sphere0,wrldPos,wrldNormal) + SphereAO(_Sphere1,wrldPos,wrldNormal) + SphereAO(_Sphere2,wrldPos,wrldNormal));
#else
		ao = 1 - max(max(SphereAO(_Sphere0,wrldPos,wrldNormal),SphereAO(_Sphere1,wrldPos,wrldNormal)),SphereAO(_Sphere2,wrldPos,wrldNormal));
#endif

		ao = max(ao,1 - _Intensity) + (1 - v.color.r);
		o.color = fixed4(ao,ao,ao,ao);

#endif

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
#if defined(USE_PERPIXEL_AO)

		float3		wrldPos 	= i.worldPos;
		float3		wrldNormal	= i.normal;
		float 		ao;

		ao = 1 - saturate(SphereAO(_Sphere0,wrldPos,wrldNormal) + SphereAO(_Sphere1,wrldPos,wrldNormal) + SphereAO(_Sphere2,wrldPos,wrldNormal));
		ao = max(ao,1 - _Intensity);

		return ao;

#else
		return i.color;
#endif
		}
		ENDCG
	}
}
}
