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


[CustomPropertyDrawer(typeof(GuiOverlaySideBar.ButtonInfo))]
public class GuiOverlaySideBarButtonInfoDrawer : MFGPropertyDrawer
{
	public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
	{
		float height = base.GetPropertyHeight(prop, label);
		if (prop.isExpanded == false)
			return height;
		if (prop.hasVisibleChildren == false)
			return height;
		
		foreach (SerializedProperty child in prop)
		{
			height += EditorGUI.GetPropertyHeight(child);
		}
		
		return height;
	}
	
	public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
	{
		UpdateLabel(prop, ref label);
		
		float height = base.GetPropertyHeight(prop, label);
		pos.height   = height;
		
		EditorGUI.BeginProperty(pos, label, prop);
		
		int indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel -= 1;
		
		prop.isExpanded = EditorGUI.Foldout(pos, prop.isExpanded, label);
		pos.y += height;
		
		if (prop.isExpanded == true && prop.hasVisibleChildren == true)
		{
			EditorGUI.indentLevel += 2;

			foreach (SerializedProperty child in prop)
			{
				EditorGUI.PropertyField(pos, child);
				pos.y += EditorGUI.GetPropertyHeight(child);
			}
		}
		
		EditorGUI.indentLevel = indent;
		
		EditorGUI.EndProperty();
	}
	
	void UpdateLabel(SerializedProperty prop, ref GUIContent label)
	{
		List<string> parts = new List<string>();
		
		SerializedProperty child = prop.Copy();
		SerializedProperty   end = child.GetEndProperty();
		bool       enterChildren = true;
		while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end))
		{
			if (child.propertyType == SerializedPropertyType.ObjectReference)
			{
				if (child.objectReferenceValue == null)
				{
					parts.Add("<UNASSIGNED>");
					break;
				}
				else
				{
					parts.Add(child.objectReferenceValue.name);
				}
			}
			else
			if (child.propertyType == SerializedPropertyType.Integer) { parts.Add(child.intValue.ToString()); }
			else
			if (child.propertyType == SerializedPropertyType.Enum)    { parts.Add(child.enumNames[child.enumValueIndex]); }
			else
			if (child.propertyType == SerializedPropertyType.String)  { if (string.IsNullOrEmpty(child.stringValue) == false ) parts.Add(child.stringValue); }
			
			enterChildren = false;
		}

		string info = string.Join(", ", parts.ToArray());
		
		// modify text for array elements only
		if (prop.depth > 0)
		{
			label.text = string.Format("{0} ({1})", label.text, info);
		}
		else
		{
			label.tooltip = info;
		}
	}
}
