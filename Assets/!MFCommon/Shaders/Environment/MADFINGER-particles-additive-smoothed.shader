
Shader "MADFINGER/Particles/Additive + fadeout" {

Properties {
	_MainTex ("Main tex", 2D) = "white" {}
	_Color("Tint color", Color) = (0,1,0,0)
}

	
SubShader {
	
	
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }

	Cull Off	
	Blend One One
	Lighting Off ZWrite Off Fog { Color (0,0,0,0) }
	
	LOD 100
	
	CGINCLUDE	
	#include "UnityCG.cginc"
	
	sampler2D	_MainTex;	
	float4 		_Color;
	
	struct v2f {
		float4	pos		: SV_POSITION;
		fixed4	color	: COLOR;
		float2	uv		: TEXCOORD0;
	};

	v2f vert (appdata_full v)
	{
		v2f 	o;
		float3	wrldNormal	= normalize(mul((float3x3)_Object2World, v.normal));
		float3	wrldPos		= mul(_Object2World,v.vertex);
		float3	toViewer	= normalize(_WorldSpaceCameraPos  - wrldPos);
		float	fadeout		= saturate(4 * abs(dot(wrldNormal,toViewer)));
		
				
		o.pos	= mul(UNITY_MATRIX_MVP, v.vertex); 
//		o.color	= fixed4(_Color.xyz,fadeout);
		o.color	= fixed4(_Color.xyz,1);
		o.uv	= v.texcoord;
						
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
			fixed4 	tex = tex2D(_MainTex,i.uv);
			
			tex.rgb *= i.color.rgb;		
			
			return  tex * (i.color.a * i.color.a);
		}
		ENDCG 
	}	


}


}

