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


[CustomPropertyDrawer(typeof(TextAnchor))]
public class TextAnchorDrawer : MFGPropertyDrawer
{
	static Texture2D[] iconsH;
	static Texture2D[] iconsV;
	
	public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
	{
		return base.GetPropertyHeight(prop, label) * 2 + 6;
	}
	
	public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
	{
		if (iconsH == null)
		{
			iconsH = new Texture2D[3];
			iconsH[0] = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/!MFCommon/Editor Resources/shape_aling_left.png",   typeof(Texture2D));
			iconsH[1] = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/!MFCommon/Editor Resources/shape_aling_center.png", typeof(Texture2D));
			iconsH[2] = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/!MFCommon/Editor Resources/shape_aling_right.png",  typeof(Texture2D));
		}
		if (iconsV == null)
		{
			iconsV = new Texture2D[3];
			iconsV[0] = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/!MFCommon/Editor Resources/shape_aling_top.png",    typeof(Texture2D));
			iconsV[1] = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/!MFCommon/Editor Resources/shape_aling_middle.png", typeof(Texture2D));
			iconsV[2] = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/!MFCommon/Editor Resources/shape_aling_bottom.png", typeof(Texture2D));
		}
		
		int       id = GUIUtility.GetControlID(FocusType.Keyboard);
		float height = base.GetPropertyHeight(prop, label) + 2;
		
		EditorGUI.BeginProperty(pos, label, prop);
		
		pos = EditorGUI.PrefixLabel(pos, id, label);
		pos.height = height;
		pos.width -= 4;
		
		int oldValueV = prop.intValue / 3;
		int oldValueH = prop.intValue % 3;
		EditorGUI.BeginChangeCheck();
			pos.y += 1;
			int newValueV = Toolbar(pos, id, prop.hasMultipleDifferentValues ? -1 : oldValueV, iconsV);
			pos.y += height;
			int newValueH = Toolbar(pos, id, prop.hasMultipleDifferentValues ? -1 : oldValueH, iconsH);
		if (EditorGUI.EndChangeCheck() == true)
		{
			newValueH = newValueH < 0 ? oldValueH : newValueH;
			newValueV = newValueV < 0 ? oldValueV : newValueV;
			prop.intValue = newValueV * 3 + newValueH;
		}
		
		//pos.y += height;
		//EditorGUI.EnumPopup(pos, (TextAnchor)prop.intValue);
		
		EditorGUI.EndProperty();
	}
}
