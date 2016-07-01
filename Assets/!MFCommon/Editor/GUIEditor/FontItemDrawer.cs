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


[CustomPropertyDrawer(typeof(MFFontManager.FontItem))]
public class FontItemDrawer : MFGPropertyDrawer
{
	public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
	{
		SerializedProperty font = prop.FindPropertyRelative("m_Font");
		//SerializedProperty path = prop.FindPropertyRelative("m_FontAssetPath");

		EditorGUI.BeginProperty(pos, label, prop);
		{
			EditorGUI.BeginChangeCheck();
			{
				EditorGUI.PropertyField( pos, font, label );
			}
			if (EditorGUI.EndChangeCheck() == true)
			{
				UpdateAssetPath(prop);
			}
		}
		EditorGUI.EndProperty();
	}

	private void UpdateAssetPath(SerializedProperty prop)
	{
		SerializedProperty font = prop.FindPropertyRelative("m_Font");
		SerializedProperty path = prop.FindPropertyRelative("m_FontAssetPath");

		if (font.objectReferenceValue == null)
		{
			path.stringValue = string.Empty;
			return;
		}

		if (EditorUtility.IsPersistent(font.objectReferenceValue) == false)
		{
			Debug.LogError("Object has to be a prefab !!!");
		}

		string asset_path = AssetDatabase.GetAssetPath(font.objectReferenceValue);
		asset_path = System.IO.Path.ChangeExtension(asset_path, null);

		// check if asset is in Resources subfolder and prepare path for resource loading..
		int idx = asset_path.IndexOf("Resources/");
		if(idx == -1)
		{
			Debug.LogError("Asset has to be in 'Resources' subfolder !!!\n" + asset_path);
		}
		else
		{
			asset_path = asset_path.Substring(idx + "Resources/".Length);
		}

		Debug.Log("asset_path = " + asset_path);
		path.stringValue = asset_path;
	}
}

