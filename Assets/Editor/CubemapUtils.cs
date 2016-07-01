//
// By using or accessing the source codes or any other information of the Game SHADOWGUN: DeadZone ("Game"),
// you ("You" or "Licensee") agree to be bound by all the terms and conditions of SHADOWGUN: DeadZone Public
// License Agreement (the "PLA") starting the day you access the "Game" under the Terms of the "PLA".
//
// You can review the most current version of the "PLA" at any time at: http://madfingergames.com/pla/deadzone
//
// If you don't agree to all the terms and conditions of the "PLA", you shouldn't, and aren't permitted
// to use or access the source codes or any other information of the "Game" supplied by MADFINGER Games, a.s.
//

using UnityEngine;
using UnityEditor;
using System.Collections;

public class CubemapUtils : ScriptableWizard
{
	public Cubemap 		m_Cubemap;
	private int			m_DstTexSize = 1024;
	
	public	Vector3		m_DbgLightDir0 		= new Vector3(0.5f,-0.5f,0.8f);
	public	Vector3		m_DbgLightDir1 		= new Vector3(-0.5f,0.5f,-0.4f);
	public	Color		m_DbgLightColor0	= Color.white;
	public	Color		m_DbgLightColor1	= Color.cyan;
	public	float		m_DbgLightSpecPow0	= 20;
	public	float		m_DbgLightSpecPow1	= 20;
		
	void CubemapToEnvMap()
	{
        Texture2D	resultTex = new Texture2D(m_DstTexSize,m_DstTexSize,TextureFormat.RGB24,false);
		float		invTexRes = 1.0f / (resultTex.height - 1);
		//bool		topHemisphere = true;

        for (int y = 0; y < resultTex.height; y++)
        {
            for (int x = 0; x < resultTex.width; x++)
            {
				Vector3 dir;
				
				float xf = (float)x * invTexRes;
				float yf = (float)y * invTexRes;
				
				xf = xf * 2 - 1;
				yf = yf * 2 - 1;
				
				float p = 0.5f - 0.5f * (xf * xf + yf * yf);
								
				dir.x = xf;
				dir.y = p;
				dir.z = yf;
				
				dir.Normalize();
				
				
//				float theta	= 2 * Mathf.Acos(Mathf.Sqrt(1 - xf));
//				float phi	= topHemisphere ? (Mathf.PI * yf) : (2 * Mathf.PI * yf);
							
//				dir.x = Mathf.Sin(theta) * Mathf.Cos(phi);
//				dir.y = Mathf.Sin(theta) * Mathf.Sin(phi);
//				dir.z = Mathf.Cos(theta);
								
				CubemapFace	face;
				float		u, v;
				
				DirToUV(dir,out face,out u,out v);
				
				Color col = m_Cubemap.GetPixel(face,(int)(u * m_Cubemap.width),(int)(v * m_Cubemap.height));
												
                resultTex.SetPixel(x,y,col);
            }
        }
		
        resultTex.Apply();

        SaveTex(resultTex, "Assets/GeneratedTextures/envmap.png");		
	}
	
	Color EvalSpecLighting(Vector3 dir)
	{
		float l		= Vector3.Dot(dir,-m_DbgLightDir0);
		
		l = Mathf.Max(l,0);
		l = Mathf.Pow(l,m_DbgLightSpecPow0);
		
		Color col = m_DbgLightColor0 * l * 2;
		
		l = Vector3.Dot(dir,-m_DbgLightDir1);
		
		l = Mathf.Max(l,0);
		l = Mathf.Pow(l,m_DbgLightSpecPow1);
		
		col += m_DbgLightColor1 * l * 2;
		
		return col;
	}
	
	void TestGenerateSpecCubemap()
	{
		float dv 	= 1.0f / m_Cubemap.height;
		float du 	= 1.0f / m_Cubemap.width;
		
		float uoffs	= 0.5f / m_Cubemap.width;
		float voffs = 0.5f / m_Cubemap.height;
			
		m_DbgLightDir0.Normalize();
		m_DbgLightDir1.Normalize();
		
		foreach (CubemapFace face in CubemapFace.GetValues(typeof(CubemapFace)))
		{
			for (int y = 0; y < m_Cubemap.height; y++)
			{
				float v = y * dv + voffs;
				
				for (int x = 0; x < m_Cubemap.width; x++)
				{
					float 	u 	= x * du + uoffs;
					Vector3	dir = UVToDir(u,v,face);
						
					dir.Normalize();
					
					m_Cubemap.SetPixel(face,x,y,EvalSpecLighting(dir));
				}
			}
		}
		
		m_Cubemap.Apply();
	}

	void	DirToUV(Vector3 dir,out CubemapFace f,out float u,out float v)
	{
		f = CubemapFace.PositiveX;
		u = 0;
		v = 0;
		
		float	absx, absy, absz, max, r_max;
		int		major_axis = 0;

		absx	= Mathf.Abs(dir[0]);
		absy	= Mathf.Abs(dir[1]);
		absz	= Mathf.Abs(dir[2]);

		max		= absx;

		if (absy > absx)
		{
			max			= absy;
			major_axis	= 1;
		}

		if (absz > max)
		{
			max			= absz;
			major_axis	= 2;
		}

		r_max = 1.0f / max;

		switch (major_axis)
		{
		case 0:
			{
				if (dir[0] >= 0)
				{
					//
					// pos-x
					//

					u = (-dir[2] * r_max + 1) * 0.5f;
					v = (-dir[1] * r_max + 1) * 0.5f;
					f = CubemapFace.PositiveX;
				}
				else
				{
					//
					// neg-x
					//

					u = ( dir[2] * r_max + 1) * 0.5f;
					v = (-dir[1] * r_max + 1) * 0.5f;
					f = CubemapFace.NegativeX;
				}
			}
			break;

		case 1:
			{
				if (dir[1] >= 0)
				{
					//
					// pos-y
					//

					u = (dir[0] * r_max + 1) * 0.5f;
					v = (dir[2] * r_max + 1) * 0.5f;
					f = CubemapFace.PositiveY;
				}
				else
				{
					//
					// neg-y
					//

					u = ( dir[0] * r_max + 1) * 0.5f;
					v = (-dir[2] * r_max + 1) * 0.5f;
					f = CubemapFace.NegativeY;
				}
			}
			break;

		case 2:
			{
				if (dir[2] >= 0)
				{
					u = ( dir[0] * r_max + 1) * 0.5f;
					v = (-dir[1] * r_max + 1) * 0.5f;
					f = CubemapFace.PositiveZ;
				}
				else
				{
					u = (-dir[0] * r_max + 1) * 0.5f;
					v = (-dir[1] * r_max + 1) * 0.5f;
					f = CubemapFace.NegativeZ;
				}
			}
			break;
		}
	}
	
	Vector3	UVToDir(float u,float v,CubemapFace face)
	{
		MFDebugUtils.Assert(u >= 0.0f && u <= 1.0f);
		MFDebugUtils.Assert(v >= 0.0f && v <= 1.0f);

		switch(face)
		{
		case CubemapFace.PositiveX:
			{
				return new Vector3(0.5f,-v + 0.5f,-u + 0.5f);
			}

		case CubemapFace.NegativeX:
			{
				return new Vector3(-0.5f,-v + 0.5f,u - 0.5f);
			}

		case CubemapFace.PositiveY:
			{
				return new Vector3(u - 0.5f,0.5f,v - 0.5f);
			}

		case CubemapFace.NegativeY:
			{
				return new Vector3(u - 0.5f,-0.5f,-v + 0.5f);
			}

		case CubemapFace.PositiveZ:
			{
				return new Vector3(u - 0.5f,-v + 0.5f,0.5f);
			}

		case CubemapFace.NegativeZ:
			{
				return new Vector3(-u + 0.5f,-v + 0.5f,-0.5f);
			}
		}

		MFDebugUtils.Assert(false);
		return Vector3.zero;
	}
	
	
	void SaveTex(Texture2D tex,string name)
	{
		byte [] pngData = tex.EncodeToPNG();
		
		System.IO.File.WriteAllBytes(name, pngData);		
	}

	
	void OnWizardCreate () 
	{
		//CubemapToEnvMap();
		
		TestGenerateSpecCubemap();
	}
	
	void OnWizardUpdate () 
	{
//		helpString	= "Select cubemap to convert to env map";
		isValid 	=  m_Cubemap != null;			
	}

	
	[MenuItem("MADFINGER/CubemapUtils... %#r")]
	static void CreateWizard()
	{
		//ScriptableWizard.DisplayWizard("CubemapUtils", typeof(CubemapUtils), "Cubemap -> env map");
		ScriptableWizard.DisplayWizard("CubemapUtils", typeof(CubemapUtils), "Generate fake-spec cubemap");
	}

}
