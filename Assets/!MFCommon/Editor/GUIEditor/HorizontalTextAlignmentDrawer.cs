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


[CustomPropertyDrawer(typeof(GUIBase_TextArea.HorizontalTextAlignment))]
public class HorizontalTextAlignmentDrawer : MFGPropertyDrawer
{
	static Texture2D[] icons;
	
	public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
	{
		return base.GetPropertyHeight(prop, label) + 4;
	}
	
	public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
	{
		if (icons == null)
		{
			icons = new Texture2D[4];
			icons[0] = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/!MFCommon/Editor Resources/text-align-left-icon.png",    typeof(Texture2D));
			icons[1] = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/!MFCommon/Editor Resources/text-align-center-icon.png",  typeof(Texture2D));
			icons[2] = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/!MFCommon/Editor Resources/text-align-right-icon.png",   typeof(Texture2D));
			icons[3] = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/!MFCommon/Editor Resources/text-align-justify-icon.png", typeof(Texture2D));
		}
		
		int id = GUIUtility.GetControlID(FocusType.Keyboard);
		
		EditorGUI.BeginProperty(pos, label, prop);
		
		pos = EditorGUI.PrefixLabel(pos, id, label);
		pos.width -= 4;
		
		EditorGUI.BeginChangeCheck();
			pos.y += 1;
			int value = Toolbar(pos, id, prop.hasMultipleDifferentValues ? -1 : prop.intValue, icons);
		if (EditorGUI.EndChangeCheck() == true)
		{
			prop.intValue = value;
		}
		
		EditorGUI.EndProperty();
	}
}
