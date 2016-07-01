Shader "Hidden/SeparableWeightedBlur" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}
	}

	CGINCLUDE
	
	#include "UnityCG.cginc"	
		
	struct v2f {
		float4 pos : POSITION;

		float2 uv : TEXCOORD0;

		float4 uv01 : TEXCOORD1;
		float4 uv23 : TEXCOORD2;
		float4 uv45 : TEXCOORD3;
	};
	
	float4 offsets;
	
	sampler2D _MainTex;
		
	v2f vert (appdata_img v) {
		v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);

		o.uv.xy = v.texcoord.xy;

		o.uv01 =  v.texcoord.xyxy + offsets.xyxy * float4(1,1, -1,-1);
		o.uv23 =  v.texcoord.xyxy + offsets.xyxy * float4(1,1, -1,-1) * 2.0;
		o.uv45 =  v.texcoord.xyxy + offsets.xyxy * float4(1,1, -1,-1) * 3.0;

		return o;  
	}
	
	// WEIGHTS are in the alpha channel ...
		
	float4 frag (v2f i) : COLOR {
		half4 color = float4 (0,0,0,0);

		half4 cocWeightSetA4;
		half3 cocWeightSetB3;

		cocWeightSetA4.x =(tex2D(_MainTex, i.uv.xy).a) * 0.40;
		cocWeightSetA4.y =(tex2D(_MainTex, i.uv01.xy).a) * 0.15;
		cocWeightSetA4.z =(tex2D(_MainTex, i.uv01.zw).a) * 0.15;
		cocWeightSetA4.w =(tex2D(_MainTex, i.uv23.xy).a) * 0.10;
		cocWeightSetB3.x =(tex2D(_MainTex, i.uv23.zw).a) * 0.10; 
		cocWeightSetB3.y =(tex2D(_MainTex, i.uv45.xy).a) * 0.05;
		cocWeightSetB3.z =(tex2D(_MainTex, i.uv45.zw).a) * 0.05;
		
		half sum = dot(half4(1,1,1,1), cocWeightSetA4);
		sum += dot(half3(1,1,1), cocWeightSetB3);
				
		color += tex2D(_MainTex, i.uv.xy) * cocWeightSetA4.x;
		color += tex2D(_MainTex, i.uv01.xy) * cocWeightSetA4.y;
		color += tex2D(_MainTex, i.uv01.zw) * cocWeightSetA4.z; 
		color += tex2D(_MainTex, i.uv23.xy) * cocWeightSetA4.w; 
		color += tex2D(_MainTex, i.uv23.zw) * cocWeightSetB3.x; 
		color += tex2D(_MainTex, i.uv45.xy) * cocWeightSetB3.y;
		color += tex2D(_MainTex, i.uv45.zw) * cocWeightSetB3.z;
		
		return color/sum;
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
	
} // shader