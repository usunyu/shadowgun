// Visualise Spherical Harmonics
// sampled with vertex normal
Shader "Hidden/SH" {
Properties {
	_HandleSize ("HandleSize", Float) = 0.5
}
SubShader {
    Pass {
    
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

float _HandleSize;

struct v2f {
    float4 pos : SV_POSITION;
    float3 color : COLOR0;
};

v2f vert (appdata_base v)
{
    v2f o;
    half4 pos = v.vertex;
    pos.xyz *= _HandleSize;
    o.pos = mul (UNITY_MATRIX_MVP, pos);
    o.color = ShadeSH9(half4(v.normal, 1));
    return o;
}

half4 frag (v2f i) : COLOR
{
    return half4 (i.color, 1);
}
ENDCG

    }
    
// Same as the first pass, just with disabled z testing
    Pass {
    ZTest Always
    
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

float _HandleSize;

struct v2f {
    float4 pos : SV_POSITION;
    float3 color : COLOR0;
};

v2f vert (appdata_base v)
{
    v2f o;
    half4 pos = v.vertex;
    pos.xyz *= _HandleSize;
    o.pos = mul (UNITY_MATRIX_MVP, pos);
    o.color = ShadeSH9(half4(v.normal, 1));
    return o;
}

half4 frag (v2f i) : COLOR
{
    return half4 (i.color, 1);
}
ENDCG

    }
}

SubShader {
	Pass {
		Color(1,0,1,1)
	}
	Pass {
		ZTest Always
		Color(1,0,1,1)
	}
}

}