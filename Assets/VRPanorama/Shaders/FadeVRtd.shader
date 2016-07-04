
Shader "VRPanorama/FadeVRBD" {
    Properties {
        _Main ("Main", 2D) = "white" {}
        _MainR ("MainR", 2D) = "white" {}
        _MainX ("MainX", 2D) = "white" {}
        _MainRX ("MainRX", 2D) = "white" {}
        _FadeTex ("FadeTex", 2D) = "white" {}
        [HideInInspector]_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
        _U ("U", Float ) = 512
    }
    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Overlay"
        }
        Pass {
            Name "ForwardBase"
            Tags {
                "LightMode"="ForwardBase"
            }
            Blend SrcAlpha OneMinusSrcAlpha
            ZTest Always
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #pragma multi_compile_fwdbase
            #pragma exclude_renderers xbox360 ps3 flash d3d11_9x 
            #pragma target 3.0
            // float4 unity_LightmapST;
            #ifdef DYNAMICLIGHTMAP_ON
                // float4 unity_DynamicLightmapST;
            #endif
            uniform sampler2D _Main; uniform sampler2D _MainR; uniform sampler2D _MainX; uniform sampler2D _MainRX; uniform float4 _Main_ST;
            uniform sampler2D _FadeTex; uniform float4 _FadeTex_ST;
            uniform float _U;
            
            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                #ifndef LIGHTMAP_OFF
                    float4 uvLM : TEXCOORD1;
                #else
                    float3 shLight : TEXCOORD1;
                #endif
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
                return o;
            }
            fixed4 frag(VertexOutput i) : COLOR {
/////// Vectors:
////// Lighting:
////// Emissive:
				float _TXWidth = (1.0f / _U);
				float _TXHeight = (1.0f / _U);


              
                float4 c1 = tex2D(_Main, i.uv0);
				float4 c2 = tex2D(_Main, i.uv0 + float2(_TXWidth, 0.0f));
				float4 c3 = tex2D(_Main, i.uv0 + float2(0.0f, _TXHeight));
				float4 c4 = tex2D(_Main, i.uv0 + float2(-_TXWidth , 0.0f));
				float4 c5 = tex2D(_Main, i.uv0 + float2(0.0f , -_TXHeight));
				
				float4 r1 = tex2D(_MainR, i.uv0);
				float4 r2 = tex2D(_MainR, i.uv0 + float2(_TXWidth, 0.0f));
				float4 r3 = tex2D(_MainR, i.uv0 + float2(0.0f, _TXHeight));
				float4 r4 = tex2D(_MainR, i.uv0 + float2(-_TXWidth , 0.0f));
				float4 r5 = tex2D(_MainR, i.uv0 + float2(0.0f , -_TXHeight));
				
				float4 cx1 = tex2D(_MainX, i.uv0);
				float4 cx2 = tex2D(_MainX, i.uv0 + float2(_TXWidth, 0.0f));
				float4 cx3 = tex2D(_MainX, i.uv0 + float2(0.0f, _TXHeight));
				float4 cx4 = tex2D(_MainX, i.uv0 + float2(-_TXWidth , 0.0f));
				float4 cx5 = tex2D(_MainX, i.uv0 + float2(0.0f , -_TXHeight));
				
				float4 rx1 = tex2D(_MainRX, i.uv0);
				float4 rx2 = tex2D(_MainRX, i.uv0 + float2(_TXWidth, 0.0f));
				float4 rx3 = tex2D(_MainRX, i.uv0 + float2(0.0f, _TXHeight));
				float4 rx4 = tex2D(_MainRX, i.uv0 + float2(-_TXWidth , 0.0f));
				float4 rx5 = tex2D(_MainRX, i.uv0 + float2(0.0f , -_TXHeight));
				
                
                float4 _Col = (c1 + c2 + c3 + c4 + c5) / 5.0f;
                float4 _Colr = (r1 + r2 + r3 + r4 + r5) / 5.0f;
                
                float4 _Colx = (cx1 + cx2 + cx3 + cx4 + cx5) / 5.0f;
                float4 _Colrx = (rx1 + rx2 + rx3 + rx4 + rx5) / 5.0f;
                
                float4 _FadeTex_var = tex2D(_FadeTex,TRANSFORM_TEX(i.uv0, _FadeTex));
                float3 emissive = lerp(_Col.rgb, _Colr.rgb, _FadeTex_var.g);
                float3 emissive2 = lerp(_Colx.rgb, _Colrx.rgb, _FadeTex_var.b);
                float3 emissivex = lerp(emissive.rgb, emissive2.rgb, _FadeTex_var.a);
                float3 finalColor = emissivex;
                
                return fixed4(finalColor,_FadeTex_var.r);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
