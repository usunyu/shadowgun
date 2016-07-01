Shader "MADFINGER/Particles/Alpha Blended" {
Properties {
	_MainTex ("Particle Texture", 2D) = "white" {}
}

Category {
	Tags { "Queue" = "Transparent" }
	Blend SrcAlpha OneMinusSrcAlpha
	Cull Off Lighting Off Fog { Mode off }
	ZWrite Off
	ColorMask RGBA
	BindChannels {
		Bind "Color", color
		Bind "Vertex", vertex
		Bind "TexCoord", texcoord
	}
	
	
	SubShader {
		Pass {
			SetTexture [_MainTex] {
				combine texture * primary
			}
		}
	}
}
}
