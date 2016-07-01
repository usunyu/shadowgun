//#define UNITY_SHADER_ENABLE_VOLUME_FOG
#define UNITY_SHADER_VOLUME_FOG_DIST_SCALE 0.075f

static const float PI = 3.1415926535897f;


float3 MFShadeVertexLights (float4 vertex, float3 normal)
{
	float3 viewpos = mul (UNITY_MATRIX_MV, vertex).xyz;
	float3 viewN = mul ((float3x3)UNITY_MATRIX_IT_MV, normal);
	float3 lightColor = 0;
	
	for (int i = 0; i < 4; i++) 
	{
		float3	toLight = unity_LightPosition[i].xyz - viewpos.xyz * unity_LightPosition[i].w;
		float	lengthSq = dot(toLight, toLight);
		float	atten = 1.0 / (1.0 + lengthSq * unity_LightAtten[i].z);
		float	diff = max (0, dot (viewN, normalize(toLight)));
		
		lightColor += unity_LightPosition[i].w > 0 ? unity_LightColor[i].rgb * (diff * atten) : 0;
//		lightColor += unity_LightPosition[i].w * float3(unity_LightColor[i].rgb * (diff * atten));
	}
	
	return lightColor;
}


float3 MFShadeVertexLightsExt(float4 vertex, float3 normal,out float3 strongestLightDir)
{
	float3 viewpos = mul (UNITY_MATRIX_MV, vertex).xyz;
	float3 viewN = mul ((float3x3)UNITY_MATRIX_IT_MV, normal);
	float3 lightColor = 0;
	
	for (int i = 0; i < 4; i++) 
	{
		float3	toLight		= unity_LightPosition[i].xyz - viewpos.xyz * unity_LightPosition[i].w;
		float	lengthSq	= dot(toLight, toLight);
		float	atten 		= (1 / (1.0 + lengthSq * unity_LightAtten[i].z)) * unity_LightPosition[i].w;
		float3	toLightNrm	= normalize(toLight);
		float	diff 		= max (0, dot (viewN, toLightNrm));
		
		strongestLightDir += toLightNrm * atten * step(1,dot(unity_LightColor[i].rgb > 0,float3(1,1,1)));
		
		lightColor += float3(unity_LightColor[i].rgb * (diff * atten));
	}
	
	
	return lightColor;
}



float4 MFSCurve4(float4 t)
{
	return t * t * (float(3.0).xxxx - 2.0f * t);
}

float4 MFSine4(float4 x)
{
	x = frac(x * (0.5 / PI) + 0.5) * 2 - 1;
	
	return 4.0f * (x - x * abs(x));
}	
float4 MFCos4(float4 x)
{	
	return MFSine4(x + 0.5 * PI);
}	


float4 MFRand4(float4 v)
{
    float4 x = v * 78.233;
    	
	x = frac(x * (0.5 / PI) + 0.5) * 2 - 1;
	          
    return frac((x - x * abs(x)) * 43758.5453 * 4.0f);
}
	
// Generates continous random value from interval <0,1>
float4 MFNoise4(float4 v)
{
	float4	t	= frac(v);
	float4	r0	= MFRand4(v - t);
	float4	r1	= MFRand4(v - t + 1);
		
	return lerp(r0,r1,MFSCurve4(t));
}

float MFFresnel(float facing,float bias,float power)
{
	return saturate(bias + (1 - bias) * pow(facing, power));
}

half3 MFMixNormals(half3 n1,half3 n2)
{
	half3 r = half3(n1.xy + n2.xy, n1.z*n2.z);
   			
	return normalize(r);		
}

