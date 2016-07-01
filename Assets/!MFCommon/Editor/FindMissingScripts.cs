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


public class FindMissingScripts : EditorWindow
{
	private int m_Objects    = 0;
	private int m_Components = 0;
	private int m_Missing    = 0;
	
	[MenuItem("Window/FindMissingScripts")]
	public static void ShowWindow()
	{
		EditorWindow.GetWindow(typeof(FindMissingScripts));
	}
	
	public void OnGUI()
	{
		if (GUILayout.Button("Find missing scripts in selected prefabs"))
		{
			FindInSelected();
		}
	}
	
	private void FindInSelected()
	{
		m_Objects    = 0;
		m_Components = 0;
		m_Missing    = 0;
		
		GameObject[] objects = Selection.gameObjects;
		foreach (var obj in objects)
		{
			CheckObject(obj);
		}
		
		Debug.Log(string.Format("Searched {0} GameObjects, {1} components, found {2} missing", m_Objects, m_Components, m_Missing));
	}
	
	private void CheckObject(GameObject obj)
	{
		m_Objects++;
		
		CheckComponents(obj);
		
		Transform trans = obj.transform;
		foreach (Transform child in trans)
		{
			CheckObject(child.gameObject);
		}
	}
	
	private void CheckComponents(GameObject obj)
	{
		Component[] components = obj.GetComponents<Component>();
		for (int idx = 0; idx < components.Length; ++idx)
		{
			m_Components++;
			if (components[idx] == null || components[idx].GetType() == typeof(MonoBehaviour))
			{
				m_Missing++;
				Debug.Log(obj.name + " has an empty script attached in position: " + idx, obj);
			}
		}
	}
}