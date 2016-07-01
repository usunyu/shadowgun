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
using System.Collections.Generic;
using System.IO;

public class ShaderUtils : EditorWindow
{
	public enum E_ShaderDetails
	{
		E_SHADER_DETAILS_LOW,
		E_SHADER_DETAILS_MEDIUM,
		E_SHADER_DETAILS_HIGH
	};
	
	public E_ShaderDetails	m_ShaderDetails;

    // Add menu item to the Window menu
    [MenuItem("Window/ShaderUtils")]
    static void Init()
    {
        EditorWindow.GetWindow<ShaderUtils>(false, "Shader utils");
    }
	
	void RebuildShaders()
	{
		string[] filePaths = Directory.GetFiles(@"Assets/!MFCommon/Shaders", "*.shader",SearchOption.AllDirectories);
		
		foreach (string curr in filePaths)
		{
			AssetDatabase.ImportAsset(curr,ImportAssetOptions.ForceUpdate);				
		}		
	}
	
	void ChangeShaderDetails(E_ShaderDetails level)
	{
		GraphicsDetailsUtl.DisableShaderKeyword("UNITY_SHADER_DETAIL_LOW");
		GraphicsDetailsUtl.DisableShaderKeyword("UNITY_SHADER_DETAIL_MEDIUM");
		GraphicsDetailsUtl.DisableShaderKeyword("UNITY_SHADER_DETAIL_HIGH");

		switch (level)
		{
			case E_ShaderDetails.E_SHADER_DETAILS_LOW:
			{
				GraphicsDetailsUtl.EnableShaderKeyword("UNITY_SHADER_DETAIL_LOW");
			}
			break;
			
			case E_ShaderDetails.E_SHADER_DETAILS_MEDIUM:
			{
				GraphicsDetailsUtl.EnableShaderKeyword("UNITY_SHADER_DETAIL_MEDIUM");
			}
			break;
			
			case E_ShaderDetails.E_SHADER_DETAILS_HIGH:
			{
				GraphicsDetailsUtl.EnableShaderKeyword("UNITY_SHADER_DETAIL_HIGH");
			}
			break;				
		}
	}
	
    void OnGUI()
    {
		E_ShaderDetails shaderDetails = (E_ShaderDetails)EditorGUILayout.EnumPopup(m_ShaderDetails);
		
		if (shaderDetails != m_ShaderDetails)
		{
			ChangeShaderDetails(shaderDetails);
		}
		
		m_ShaderDetails = shaderDetails;		
		
		if (GUILayout.Button("Rebuild shaders", GUILayout.Width(200)) == true)
		{
			RebuildShaders();		
		}
	}
}
