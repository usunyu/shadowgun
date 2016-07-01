Shader "MADFINGER/Diffuse/FakeAlphaCutOff" { 

Properties 
{
  _MainTex ("Base (RGB)", 2D) = "white" {}
}

SubShader 
{
 Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
 
 Blend SrcAlpha OneMinusSrcAlpha

 LOD 100
 
   
 CGINCLUDE
 #include "UnityCG.cginc"

 sampler2D _MainTex;
 float4 _MainTex_ST;
 
 struct v2f 
 {
  float4 pos : SV_POSITION;
  float2 uv : TEXCOORD0;
 };

 
 v2f vert (appdata_full v)
 {
  v2f o;

  o.pos = mul(UNITY_MATRIX_MVP, v.vertex);  
  o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
  
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
	return tex2D(_MainTex,i.uv);
  }
  ENDCG 
 } 
}
}