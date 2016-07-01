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


public static class EditorGuiUtils
{
	private static   string[]       m_BoolValues   = { "False", "True" };
	private static   Stack< int >   m_IndentLevels = new Stack< int >();

	public static void StoreIndentLevel()
	{
		m_IndentLevels.Push( EditorGUI.indentLevel );
	}

	public static void StoreAndResetIndentLevel()
	{
		m_IndentLevels.Push( EditorGUI.indentLevel );
		EditorGUI.indentLevel = 0;
	}

	public static void RestoreIndentLevel()
	{
		EditorGUI.indentLevel = m_IndentLevels.Pop();
	}

	public static bool BoolPopup( bool inValue, GUIStyle inStyle, GUILayoutOption inOptions )
	{
		bool newInitValue = EditorGUILayout.IntPopup((inValue == false) ? 0 : 1, m_BoolValues, new int[] {0,1}) == 0 ? false : true ;
	//  bool newInitValue = EditorGUILayout.IntPopup((inValue == false) ? 0 : 1, m_BoolValues, new int[] {0,1},  inStyle, inOptions) == 0 ? false : true ;
		return newInitValue;
	}

	public static int PopupWithNullItem(string[] inStrings, int inIndex)
	{	return PopupWithNullItem(inStrings, inIndex, null, GUIStyle.none);	}
	public static int PopupWithNullItem(string[] inStrings, int inIndex, GUIStyle inStyle)
	{	return PopupWithNullItem(inStrings, inIndex, null, inStyle);	}
	public static int PopupWithNullItem(string[] inStrings, int inIndex, string inNullString, GUIStyle inStyle)
	{
		bool buiWasDisabled = false;
		string [] allStrings;

		if(inStrings == null || inStrings.Length == 0)
		{
			buiWasDisabled = GUI.enabled != false;
			GUI.enabled = false;
			allStrings = new string[1];
			allStrings[0] = "[Select]";
		}
		else
		{
			/// Add selection text (null string) as first item...
			allStrings = new string[inStrings.Length + 1];
			allStrings[0] = string.IsNullOrEmpty(inNullString) == false ? inNullString : "[Select]";
			System.Array.Copy(inStrings, 0, allStrings, 1, inStrings.Length);
		}


		/// We added one item into strings so we must increase index
		/// and for sure we will clamp it.
		int index = Mathf.Clamp(inIndex + 1, 0, allStrings.Length);

		if(inStyle != GUIStyle.none)
		{	index = EditorGUILayout.Popup(index, allStrings, inStyle);	}
		else
		{	index = EditorGUILayout.Popup(index, allStrings);			}

		if(buiWasDisabled == true)
			GUI.enabled = true;

		index 	  = index   - 1;
		return index;
	}
};


public static class EditorGUILayoutExtension
{
	// this doesn't work ...
    public static bool BoolPopup(this EditorGUILayout inEditorGUILayout, bool inValue)
    {	return EditorGuiUtils.BoolPopup(inValue, null, null);    }
	public static bool BoolPopup(this EditorGUILayout inEditorGUILayout, bool inValue, GUIStyle inStyle)
    {	return EditorGuiUtils.BoolPopup(inValue, inStyle, null);    }
	public static bool BoolPopup(this EditorGUILayout inEditorGUILayout, bool inValue, GUILayoutOption inOptions)
    {	return EditorGuiUtils.BoolPopup(inValue, null, inOptions);    }
	public static bool BoolPopup(this EditorGUILayout inEditorGUILayout, bool inValue, GUIStyle inStyle, GUILayoutOption inOptions)
    {	return EditorGuiUtils.BoolPopup(inValue, inStyle, inOptions);   }
}
