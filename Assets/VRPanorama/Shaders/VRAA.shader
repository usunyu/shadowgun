// Shader created with Shader Forge v1.17 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.17;sub:START;pass:START;ps:flbk:,iptp:0,cusa:False,bamd:0,lico:1,lgpr:1,limd:0,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:False,hqlp:False,rprd:False,enco:False,rmgx:True,rpth:0,hqsc:True,nrmq:1,nrsp:0,vomd:0,spxs:False,tesm:0,culm:0,bsrc:0,bdst:1,dpts:6,wrdp:False,dith:0,rfrpo:False,rfrpn:Refraction,coma:15,ufog:False,aust:False,igpj:True,qofs:0,qpre:1,rntp:1,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,ofsf:0,ofsu:0,f2p0:False;n:type:ShaderForge.SFN_Final,id:3138,x:33077,y:32560,varname:node_3138,prsc:2|emission-3185-OUT;n:type:ShaderForge.SFN_Tex2d,id:5689,x:32631,y:32945,varname:node_5689,prsc:2,tex:01e1db82f5eac1742adb1f3339f3ac3d,ntxv:0,isnm:False|UVIN-1058-OUT,TEX-1890-TEX;n:type:ShaderForge.SFN_Tex2dAsset,id:1890,x:31799,y:32677,ptovrint:False,ptlb:MainTex,ptin:_MainTex,varname:_MainTex,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,tex:01e1db82f5eac1742adb1f3339f3ac3d,ntxv:0,isnm:False;n:type:ShaderForge.SFN_TexCoord,id:2004,x:31541,y:32828,varname:node_2004,prsc:2,uv:0;n:type:ShaderForge.SFN_Add,id:3541,x:32491,y:32658,varname:node_3541,prsc:2|A-5689-RGB,B-7555-RGB,C-408-RGB,D-6926-RGB,E-3182-RGB;n:type:ShaderForge.SFN_Divide,id:4493,x:32022,y:32964,varname:node_4493,prsc:2|A-5122-OUT,B-2799-OUT;n:type:ShaderForge.SFN_ValueProperty,id:2799,x:31608,y:33091,ptovrint:False,ptlb:U,ptin:_U,varname:_U,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:215;n:type:ShaderForge.SFN_Vector1,id:5122,x:31837,y:32964,varname:node_5122,prsc:2,v1:1;n:type:ShaderForge.SFN_Add,id:1058,x:32441,y:32945,varname:node_1058,prsc:2|A-2004-UVOUT,B-3408-OUT;n:type:ShaderForge.SFN_Append,id:3408,x:32267,y:32964,varname:node_3408,prsc:2|A-4493-OUT,B-585-OUT;n:type:ShaderForge.SFN_Vector1,id:585,x:31837,y:33130,varname:node_585,prsc:2,v1:0;n:type:ShaderForge.SFN_Divide,id:3185,x:32806,y:32658,varname:node_3185,prsc:2|A-3541-OUT,B-7787-OUT;n:type:ShaderForge.SFN_Vector1,id:7787,x:32647,y:32773,varname:node_7787,prsc:2,v1:5;n:type:ShaderForge.SFN_Tex2d,id:7555,x:32636,y:33248,varname:node_5684,prsc:2,tex:01e1db82f5eac1742adb1f3339f3ac3d,ntxv:0,isnm:False|UVIN-4957-OUT,TEX-1890-TEX;n:type:ShaderForge.SFN_Append,id:3329,x:32264,y:33267,varname:node_3329,prsc:2|A-4493-OUT,B-4905-OUT;n:type:ShaderForge.SFN_Vector1,id:4905,x:31885,y:33436,varname:node_4905,prsc:2,v1:0;n:type:ShaderForge.SFN_Tex2d,id:408,x:32661,y:33561,varname:_node_5933_copy,prsc:2,tex:01e1db82f5eac1742adb1f3339f3ac3d,ntxv:0,isnm:False|UVIN-2313-OUT,TEX-1890-TEX;n:type:ShaderForge.SFN_Divide,id:7364,x:32051,y:33580,varname:node_7364,prsc:2|A-4810-OUT,B-5192-OUT;n:type:ShaderForge.SFN_ValueProperty,id:5192,x:31594,y:33606,ptovrint:False,ptlb:V,ptin:_V,varname:_V,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:512;n:type:ShaderForge.SFN_Vector1,id:4810,x:31881,y:33580,varname:node_4810,prsc:2,v1:1;n:type:ShaderForge.SFN_Add,id:2313,x:32434,y:33561,varname:node_2313,prsc:2|A-2004-UVOUT,B-7228-OUT;n:type:ShaderForge.SFN_Append,id:7228,x:32260,y:33580,varname:node_7228,prsc:2|A-231-OUT,B-7364-OUT;n:type:ShaderForge.SFN_Vector1,id:231,x:31881,y:33749,varname:node_231,prsc:2,v1:0;n:type:ShaderForge.SFN_Subtract,id:4957,x:32447,y:33248,varname:node_4957,prsc:2|A-2004-UVOUT,B-3329-OUT;n:type:ShaderForge.SFN_Tex2d,id:6926,x:32656,y:33872,varname:_node_5253,prsc:2,tex:01e1db82f5eac1742adb1f3339f3ac3d,ntxv:0,isnm:False|UVIN-9660-OUT,TEX-1890-TEX;n:type:ShaderForge.SFN_Append,id:2384,x:32255,y:33891,varname:node_2384,prsc:2|A-231-OUT,B-7364-OUT;n:type:ShaderForge.SFN_Subtract,id:9660,x:32448,y:33872,varname:node_9660,prsc:2|A-2004-UVOUT,B-2384-OUT;n:type:ShaderForge.SFN_Tex2d,id:3182,x:32214,y:32469,varname:_node_3182,prsc:2,tex:01e1db82f5eac1742adb1f3339f3ac3d,ntxv:0,isnm:False|UVIN-2004-UVOUT,TEX-1890-TEX;proporder:1890-2799-5192;pass:END;sub:END;*/

Shader "VRPanorama/VRAA" {
    Properties {
        _MainTex ("MainTex", 2D) = "white" {}
        _U ("U", Float ) = 512
        _V ("V", Float ) = 512
    }
    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "RenderType"="Opaque"
        }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="Always"
            }
            ZTest Always
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #pragma multi_compile_fwdbase
            #pragma exclude_renderers gles3 metal xbox360 xboxone ps3 ps4 psp2 
            #pragma target 3.0
            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            uniform float _U;
            uniform float _V;
            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
/////// Vectors:
////// Lighting:
////// Emissive:
                float node_4493 = (1.0/_U);
                float2 node_1058 = (i.uv0+float2(node_4493,0.0));
                float4 node_5689 = tex2D(_MainTex,TRANSFORM_TEX(node_1058, _MainTex));
                float2 node_4957 = (i.uv0-float2(node_4493,0.0));
                float4 node_5684 = tex2D(_MainTex,TRANSFORM_TEX(node_4957, _MainTex));
                float node_231 = 0.0;
                float node_7364 = (1.0/_V);
                float2 node_2313 = (i.uv0+float2(node_231,node_7364));
                float4 _node_5933_copy = tex2D(_MainTex,TRANSFORM_TEX(node_2313, _MainTex));
                float2 node_9660 = (i.uv0-float2(node_231,node_7364));
                float4 _node_5253 = tex2D(_MainTex,TRANSFORM_TEX(node_9660, _MainTex));
                float4 _node_3182 = tex2D(_MainTex,TRANSFORM_TEX(i.uv0, _MainTex));
                float3 emissive = ((node_5689.rgb+node_5684.rgb+_node_5933_copy.rgb+_node_5253.rgb+_node_3182.rgb)/5.0);
                float3 finalColor = emissive;
                return fixed4(pow(finalColor,1/2.2),1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
