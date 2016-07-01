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


[CustomPropertyDrawer(typeof(Vector2))]
public class Vector2Drawer : MFGPropertyDrawer
{
	public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
	{
		EditorGUI.BeginProperty(pos, label, prop);
		
		pos = EditorGUI.PrefixLabel(pos, GUIUtility.GetControlID(FocusType.Keyboard), label);
		
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
		
		pos.width = (pos.width - 4) * 0.5f;
		
		Axis(pos, prop.FindPropertyRelative("x"), "X");
		pos.x += pos.width;
		Axis(pos, prop.FindPropertyRelative("y"), "Y");
		
        EditorGUI.indentLevel = indent;
		
		EditorGUI.EndProperty();
	}
}
