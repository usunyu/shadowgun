Shader "Hidden/DepthOfField" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}
		_LowRez ("_LowRez", 2D) = "" {}
	}

	CGINCLUDE

		
	#include "UnityCG.cginc"
	
	struct v2f {
		float4 pos : POSITION;
		float2 uv : TEXCOORD0;
		float2 uv1 : TEXCOORD1;
	};
		
	sampler2D _MainTex;
	sampler2D _LowRez;
	
	sampler2D _CameraDepthTexture;

	float focalDistance01;	

	float4 _MainTex_TexelSize;
	float4 _MainTex_ST;
	float4 _LowRez_TexelSize;
	float4 _LowRez_ST;
		
	v2f vert( appdata_img v ) {
		v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		o.uv.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
		o.uv1.xy = TRANSFORM_TEX(v.texcoord, _LowRez);

		#ifdef SHADER_API_D3D9
		if (_MainTex_TexelSize.y < 0)
			 o.uv1.y = 1.0- o.uv1.y;
		#endif

		return o;
	}
	
	half4 frag(v2f i) : COLOR {
		
		const half2 poisson[8] = {
			half2( 0.0, 0.0),
			half2( 0.527837,-0.085868),
			half2(-0.040088, 0.536087),
			half2(-0.670445,-0.179949),
			half2(-0.419418,-0.616039), 
			half2( 0.440453,-0.639399),
			half2(-0.757088, 0.349334),
			half2( 0.574619, 0.685879),
		}; 
 
		// CALCULATE THE SMALL BLUR

		half4 smallBlurValue = tex2D(_MainTex, i.uv.xy);
		smallBlurValue.a = 1.0;
		for(int j = 0; j < 8; j++) { 
			half2 coords = i.uv.xy + (_MainTex_TexelSize.xy * poisson[j] * 1.75);
			half4 tapHighRez = tex2D(_MainTex, coords);
			smallBlurValue += half4(tapHighRez.rgb,1.0) * tapHighRez.a;
		}

		smallBlurValue /= smallBlurValue.a;



		// DOF COLOR based on small and big blur

		half coc2CocBlurred = 0.0;

		half4 finalColor = half4(0.0,0.0,0.0,1.0);

		half4 tapHigh = tex2D(_MainTex, i.uv.xy); 

		half4 tapLow = tex2D(_LowRez, i.uv1.xy);

		half tapBlur = tapHigh.a;
			
		half4 cocedColor = lerp(smallBlurValue, tapLow, saturate(tapBlur*1.4));
		
		half4 tempColor = tex2D(_MainTex,i.uv.xy);
		finalColor.rgb = lerp(tempColor.rgb, cocedColor.rgb, tapBlur);

			return finalColor; 

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