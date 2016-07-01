Shader "FX/BlurConeTap" {
	Properties {
		_MainTex ("", any) = "" {}
	}
	SubShader {
		Pass {
			ZTest Always Cull Off ZWrite Off Fog { Mode Off }
			SetTexture [_MainTex] {constantColor (0,0,0,0.25) combine texture * constant alpha}
			SetTexture [_MainTex] {constantColor (0,0,0,0.25) combine texture * constant + previous}
			SetTexture [_MainTex] {constantColor (0,0,0,0.25) combine texture * constant + previous}
			SetTexture [_MainTex] {constantColor (0,0,0,0.25) combine texture * constant + previous}
		}
	}
	Fallback off
}
