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

public class SelectionTools : EditorWindow {
	
	bool 	m_HideObjects 		= false;
	bool 	m_ShowObjects 		= false;	
	bool 	m_HSApplyOnChilds 	= true;

	bool 	m_ActivateObjects 	= false;
	bool 	m_DeactivateObjects = false;	
	bool 	m_ADApplyOnChilds 	= true;
	
	bool 	m_SetLayer          = false;
	int 	m_SelectedLayer 	= 0;

    bool    m_SetTag            	= false;
    string  m_SelectedTag       	= "Untagged";

    //bool    m_ChangeFont    		= false;
    //GUIBase_Font m_NewFont  		= null;

	
	// Add menu item to the Window menu
	[MenuItem ("Window/Selection Tools")]
	static void Init () {
		// Get existing open window or if none, make a new one:
		EditorWindow.GetWindow<SelectionTools> (false, "Selection Tools");
		//EditorWindow.GetWindowWithRect<SelectionTools> (new Rect(200,200,200,85), false, "Selection Tools");
	}
	
	// Implement your own editor GUI here.
	void OnGUI () {
		
		// Define content of window...
		EditorGUILayout.BeginHorizontal ();
			m_HideObjects 		= GUILayout.Button("Hide", GUILayout.Width(75));
			m_ShowObjects 		= GUILayout.Button("Show", GUILayout.Width(75));
			m_HSApplyOnChilds 	= GUILayout.Toggle(m_HSApplyOnChilds, "Apply On Childs");
		EditorGUILayout.EndHorizontal ();
		
		EditorGUILayout.BeginHorizontal ();
			m_DeactivateObjects = GUILayout.Button("Deactivate", GUILayout.Width(75));
			m_ActivateObjects 	= GUILayout.Button("Activate"  , GUILayout.Width(75));
			m_ADApplyOnChilds 	= GUILayout.Toggle(m_ADApplyOnChilds, "Apply On Childs");
		EditorGUILayout.EndHorizontal ();
		
		EditorGUILayout.BeginHorizontal ();
			m_SetLayer 			= GUILayout.Button("Set Layer", GUILayout.Width(75));
			m_SelectedLayer 	= EditorGUILayout.LayerField(m_SelectedLayer);
		EditorGUILayout.EndHorizontal ();
		
        EditorGUILayout.BeginHorizontal ();
            m_SetTag            = GUILayout.Button("Set Tag", GUILayout.Width(75));
            m_SelectedTag       = EditorGUILayout.TagField(m_SelectedTag);
        EditorGUILayout.EndHorizontal ();
/*
        EditorGUILayout.BeginHorizontal ();
            m_ChangeFont        = GUILayout.Button("Fix missing Font", GUILayout.Width(75));
            m_NewFont       	= (GUIBase_Font) EditorGUILayout.ObjectField(m_NewFont, typeof(GUIBase_Font), true);
        EditorGUILayout.EndHorizontal ();
*/
		GizmoCover.ShowNotSelectedCovers = GUILayout.Toggle(GizmoCover.ShowNotSelectedCovers, "Show all covers");

		// process controlls...
		if(m_HideObjects || m_ShowObjects)
		{
			bool val = !m_HideObjects && m_ShowObjects;
			ChangeVisibility(val, m_HSApplyOnChilds);
            EditorApplication.RepaintHierarchyWindow();
		}
		
		if(m_DeactivateObjects || m_ActivateObjects)
		{
			bool val = !m_DeactivateObjects && m_ActivateObjects;
			ChangeActivity(val,  m_ADApplyOnChilds);
            EditorApplication.RepaintHierarchyWindow();
		}
		
		if(m_SetLayer)		
		{
			SetLayer(m_SelectedLayer);
            EditorApplication.RepaintHierarchyWindow();
		}

        if(m_SetTag)
        {
            SetTag(m_SelectedTag);
            EditorApplication.RepaintHierarchyWindow();
        }

    	/*if(m_ChangeFont == true && m_NewFont != null)
		{
			// update all labels...
			GUIBase_Label[] labels = Object.FindObjectsOfType(typeof(GUIBase_Label)) as GUIBase_Label[];
			foreach (GUIBase_Label label in labels)
			{
				if(label.m_Font == null)
				{
					label.m_Font = m_NewFont;
					EditorUtility.SetDirty(label);
				}
			}
		}*/


		/*
		EditorGUILayout.BeginHorizontal ();		
		if(GUILayout.Button("Hide")) {
			foreach (Transform transform in Selection.transforms) 
			{
				GameObject go = transform.gameObject;
				Undo.RecordObject(go, go.name + " Hide" );
				go.active = false;
			}
		}
		
		if(GUILayout.Button("Show")) {
			foreach (Transform transform in Selection.transforms) 
			{
				GameObject go = transform.gameObject;
				Undo.RecordObject(go, go.name + " Show" );
				go.active = true;
			}
		}
		
		m_ApplyOnChilds = GUILayout.Toggle(m_ApplyOnChilds, "Apply On Childs");
		EditorGUILayout.EndHorizontal ();
		
		
		EditorGUILayout.BeginHorizontal ();		
		if(GUILayout.Button("Hide")) {
			foreach (Transform transform in Selection.transforms) 
			{
				GameObject go = transform.gameObject;
				Undo.RecordObject(go, go.name + " Hide" );
				go.active = false;
			}
		}
		
		if(GUILayout.Button("Show")) {
			foreach (Transform transform in Selection.transforms) 
			{
				GameObject go = transform.gameObject;
				Undo.RecordObject(go, go.name + " Show" );
				go.active = true;
			}
		}
		
		m_ApplyOnChilds = GUILayout.Toggle(m_ApplyOnChilds, "Apply On Childs");
		
		EditorGUILayout.EndHorizontal ();
		
		EditorGUILayout.BeginHorizontal ();
		m_SetLayer 		= GUILayout.Button("Set Layer!");
		m_SelectedLayer = EditorGUILayout.LayerField(m_SelectedLayer);
		{
		}

		EditorGUILayout.EndHorizontal ();
		*/
	}
	
	private void ChangeVisibility(bool inValue, bool inRecursive)
	{
		foreach (Transform transform in Selection.transforms) 
		{
			// toggles the visibility of this gameobject and all it's children
			GameObject go = transform.gameObject;
			if(inRecursive == true)
			{
			    Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
				Undo.RecordObjects(renderers, go.name + (inValue ? " Show" : " Hide"));
			    foreach (Renderer r in renderers) {
			        r.enabled = inValue;
			    }
			}
			else
			{
			    Renderer renderer = go.GetComponent<Renderer>();
				if(renderer != null)
				{
					Undo.RecordObject(renderer, go.name + (inValue ? " Show" : " Hide"));
			        renderer.enabled = inValue;
				}
			}
		}
	}

	private void SetActiveRecursive(Transform transform, bool inValue)
	{
		transform.gameObject.SetActive(inValue);

		for(int i=0; i<transform.childCount; ++i)
		{
			SetActiveRecursive(transform.GetChild(i), inValue);
		}
	}

	private void ChangeActivity(bool inValue, bool inRecursive)
	{
		foreach (Transform transform in Selection.transforms)
		{
			//Undo.RecordObject(go, go.name + (val ? " Activate" : " Deactivate"));

			//I'm not really sure if anyone is going to use the recursive variant after the logic changed in the 4.0 version
			//but I have decided to provide it.
			if(inRecursive == true)
			{
				//go.SetActiveRecursively(inValue);
				SetActiveRecursive(transform, inValue);
			}
			else
			{
				//go.active = inValue;
				transform.gameObject.SetActive(inValue);
			}
		}
	}
	
	private void SetLayer(int inNewLayer)
	{
		foreach (GameObject go in Selection.gameObjects)
		{
			Undo.RecordObject(go, go.name + " Change Layer" );
			go.layer = inNewLayer;
		}
	}

    private void SetTag(string inNewTag)
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            Undo.RecordObject(go, go.name + " Change tag" );
            go.tag = inNewTag;
        }
    }

	
}