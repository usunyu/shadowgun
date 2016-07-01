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


[CustomPropertyDrawer(typeof(LocalizedTextIdAttribute))]
public class LocalizedTextIdDrawer : MFGPropertyDrawer
{
	static Texture2D m_IconValid;
	static Texture2D m_IconInvalid;
	
	public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
	{
		return base.GetPropertyHeight(prop, label) * 3 + 3;
	}
	
	public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
	{
		if (m_IconValid == null)
		{
			m_IconValid = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/!MFCommon/Editor Resources/accept.png", typeof(Texture2D));
		}
		if (m_IconInvalid == null)
		{
			m_IconInvalid = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/!MFCommon/Editor Resources/cross.png", typeof(Texture2D));
		}
		
		float height = base.GetPropertyHeight(prop, label);
		
		pos.height = height;
		
		EditorGUI.BeginProperty(pos, label, prop);
		
		// text id
		pos.width -= height;
		EditorGUI.BeginChangeCheck();
			int value = EditorGUI.IntField(pos, label, prop.intValue);
		if (EditorGUI.EndChangeCheck() == true)
		{
			prop.intValue = value;
		}
		if (prop.hasMultipleDifferentValues == false && prop.intValue != 0)
		{
			GUIStyle style = new GUIStyle();
			style.fixedHeight = height;
			style.fixedWidth  = height;
    		style.normal.background = TextDatabase.Contains(prop.intValue) ? m_IconValid : m_IconInvalid;
    		EditorGUI.LabelField(new Rect(pos.x + pos.width - height, pos.y, height, height), GUIContent.none, style);
		}
		pos.width += height;
		
		pos.width -= 4;
		
		// text preview
		pos.y += height;
		EditorGUI.BeginDisabledGroup(true);
			pos = EditorGUI.PrefixLabel(pos, -1, new GUIContent("Text Dyn"));
			if (prop.hasMultipleDifferentValues == false && prop.intValue != 0 && TextDatabase.Contains(prop.intValue))
			{
				GUI.TextField(pos, TextDatabase.instance[prop.intValue], EditorStyles.textField);
			}
		EditorGUI.EndDisabledGroup();
		
		// language selector
		pos.x     -= 16;
		pos.y     += height;
		pos.width += 16;
		pos.width *= 0.5f;
		EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = false;
			int idx = System.Array.IndexOf(GuiOptions.convertLanguageToSysLanguage, TextDatabase.GetLanguage());
			GuiOptions.E_Language language = idx != -1 ? (GuiOptions.E_Language)idx : GuiOptions.E_Language.English;
			language = (GuiOptions.E_Language)EditorGUI.EnumPopup(pos, language, EditorStyles.toolbarPopup);
			EditorGUI.showMixedValue = prop.hasMultipleDifferentValues;
		if (EditorGUI.EndChangeCheck() == true)
		{
			TextDatabase.SetLanguage(GuiOptions.convertLanguageToSysLanguage[(int)language]);
		}
		
		// reload button
		pos.x += pos.width;
		EditorGUI.BeginChangeCheck();
			bool reload = GUI.Toggle(pos, false, "Reload", EditorStyles.toolbarButton);
		if (EditorGUI.EndChangeCheck() == true && reload == true)
		{
			TextDatabase.instance.Reload();
		}
		
		EditorGUI.EndProperty();
	}
}
