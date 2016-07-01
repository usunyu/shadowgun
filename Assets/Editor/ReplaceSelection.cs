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

/* Added by ReJ:
	. window instead of wizard
	. UseOriginalName, UseOriginalTag, UseOriginalLayer, UseOriginalStaticField
	. shortcut
 */

/* This wizard will replace a selection with an object or prefab.
 * Scene objects will be cloned (destroying their prefab links).
 * Original coding by 'yesfish', nabbed from Unity Forums
 * 'keep parent' added by Dave A (also removed 'rotation' option, using localRotation
 */
using UnityEngine;
using UnityEditor;
using System.Collections;

public class ReplaceSelection : ScriptableWizard
{
	static GameObject replacement = null;
	static bool inheritName = true;
	static bool inheritTag = false;
	static bool inheritLayer = false;
	static bool inheritStatic = false;
	static bool keep = false;

	public GameObject ReplacementObject = null;
	public bool UseOriginalName = true;
	public bool UseOriginalTag = false;
	public bool UseOriginalLayer = false;
	public bool UseOriginalStaticField = false;
	public bool KeepOriginals = false;
	
	/*
	[MenuItem("MADFINGER/Replace Selection... %#r")]
	static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard(
			"Replace Selection", typeof(ReplaceSelection), "Replace");
	}
	*/

	[MenuItem("MADFINGER/Replace Selection... %#r")]
	static void CreateWindow()
	{
		EditorWindow window = EditorWindow.GetWindow(typeof(ReplaceSelection));
		window.titleContent = new GUIContent("Replace");
		window.minSize = new Vector2 (300, 360);
		window.Show();
	}

	public ReplaceSelection()
	{
		ReplacementObject = replacement;
		KeepOriginals = keep;
		
		UseOriginalName = inheritName;
		UseOriginalTag = inheritTag;
		UseOriginalLayer = inheritLayer;
		UseOriginalStaticField = inheritStatic;
	}

	void OnWizardUpdate()
	{
		replacement = ReplacementObject;
		keep = KeepOriginals;
		
		inheritName = UseOriginalName;
		inheritTag = UseOriginalTag;
		inheritLayer = UseOriginalLayer;
		inheritStatic = UseOriginalStaticField;
	}

	void OnGUI()
	{
		//ScriptableWizard.OnGUI ();
		ReplacementObject = EditorGUILayout.ObjectField ("Replace With", ReplacementObject, typeof(GameObject), true) as GameObject;
		GUILayout.Label ("Use from Original:");
		EditorGUI.indentLevel = 2;
		UseOriginalName = EditorGUILayout.Toggle ("name", UseOriginalName);
		UseOriginalTag = EditorGUILayout.Toggle ("tag", UseOriginalTag);
		UseOriginalLayer = EditorGUILayout.Toggle ("layer", UseOriginalLayer);
		UseOriginalStaticField = EditorGUILayout.Toggle ("static field", UseOriginalStaticField);
		EditorGUI.indentLevel = 0;
		KeepOriginals = EditorGUILayout.Toggle ("Keep Originals", KeepOriginals);
		GUILayout.BeginHorizontal ();
		GUILayout.FlexibleSpace ();
		if (GUILayout.Button ("Replace Selection", GUILayout.MinWidth(120)))
		{
			OnWizardUpdate ();
			OnWizardCreate ();
		}
		GUILayout.EndHorizontal ();
	}

	void OnWizardCreate()
	{
		if (replacement == null)
			return;

		GUIEditorUtils.RegisterSceneUndo("Replace Selection");

		Transform[] transforms = Selection.GetTransforms(
			SelectionMode.TopLevel | SelectionMode.OnlyUserModifiable);

		foreach (Transform t in transforms)
		{
			GameObject g;
			PrefabType pref = PrefabUtility.GetPrefabType(replacement);

			if (pref == PrefabType.Prefab || pref == PrefabType.ModelPrefab)
			{
				g = (GameObject)PrefabUtility.InstantiatePrefab(replacement);
			}
			else
			{
				g = (GameObject)Editor.Instantiate(replacement);
			}
			g.transform.parent = t.parent;
			g.name = (inheritName)? t.gameObject.name : replacement.name;
			if (inheritTag)
				g.tag = t.gameObject.tag;
			if (inheritLayer)
				g.layer = t.gameObject.layer;
			if (inheritStatic)
				g.isStatic = t.gameObject.isStatic;
			g.transform.localPosition = t.localPosition;
			g.transform.localScale = t.localScale;
			g.transform.localRotation = t.localRotation;
		}

		if (!keep)
		{
			foreach (GameObject g in Selection.gameObjects)
			{
				GameObject.DestroyImmediate(g);
			}
		}
	}
}