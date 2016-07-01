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


public abstract class MFGPropertyDrawer : PropertyDrawer
{
	public static int Toolbar(Rect rect, int id, int value, Texture[] icons)
	{
		int result = value;
		
		rect.width = Mathf.RoundToInt(rect.width / icons.Length);
		
		for (int idx = 0; idx < icons.Length; ++idx)
		{
			EditorGUI.BeginChangeCheck();
				GUI.Toggle(rect, value == idx, icons[idx], EditorStyles.toolbarButton);
			if (EditorGUI.EndChangeCheck() == true)
			{
				result = idx;
				
				GUIUtility.keyboardControl = id;
				HandleUtility.Repaint();
			}
			
			rect.x += rect.width;
		}
		
		return result;
	}

	public static float Axis(Rect pos, SerializedProperty prop, string label)
	{
		GUIEditorUtils.LookLikeControls(20);
		
		EditorGUI.BeginChangeCheck();
			float result = EditorGUI.FloatField(pos, label, prop.floatValue);
		if (EditorGUI.EndChangeCheck() == true)
		{
			prop.floatValue = result;
		}
		
		GUIEditorUtils.LookLikeInspector();
		
		return result;
	}
}
