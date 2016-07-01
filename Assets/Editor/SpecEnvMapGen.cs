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
using System.Collections.Generic;

public class SpecEnvMapGen : EditorWindow
{
	public List<Light>	m_Lights = new List<Light>();
	public Cubemap 		m_Cubemap;//= new Cubemap(32,TextureFormat.ARGB32,true);
	
	// Add menu item to the Window menu
	[MenuItem("MADFINGER/Bake specular env map... %#r")]
	static void Init() 
	{
		//
		// Get existing open window or if none, make a new one:
		//
		
		EditorWindow window = EditorWindow.GetWindow<SpecEnvMapGen>(false,"Bake specular env map");
		
		window.minSize = new Vector3(300,150);
		window.Show();
	}
	
	void Process()
	{
		float dv 	= 1.0f / m_Cubemap.height;
		float du 	= 1.0f / m_Cubemap.width;
		
		float uoffs	= 0.5f / m_Cubemap.width;
		float voffs = 0.5f / m_Cubemap.height;
			
		
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

	Color EvalSpecLighting(Vector3 dir)
	{
		Color res = Color.clear;
		
		foreach (Light curr in m_Lights)
		{
			if (curr.type == LightType.Directional && curr.enabled)
			{
				Vector3 lightDir = -curr.transform.forward;
				
				lightDir.Normalize();
				
				float l	= Vector3.Dot(dir,lightDir);
		
				l = Mathf.Max(l,0);
				l = Mathf.Pow(l,1 + (0/*curr.shadowSoftness*/ - 1) * 5);
		
				res += curr.color * l * curr.intensity;				
			}
		}
				
		return res;
	}
	
	void OnGUI()
	{
		EditorGUILayout.BeginVertical();
		
		EditorGUILayout.PrefixLabel("Target cubemap");

		m_Cubemap = EditorGUILayout.ObjectField(m_Cubemap, typeof(Cubemap), false) as Cubemap;
		
		EditorGUILayout.EndVertical();
		
		
        GUILayout.BeginVertical();
		
		InspectorUtils.VizualizeList(ref m_Lights, true);
		
		if (GUILayout.Button("Bake", GUILayout.MinWidth(120)))
		{
			if (m_Cubemap)
			{
				Process();
			}
		}
		
		GUILayout.EndVertical();
	}
	
};