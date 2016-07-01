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

public class AnimTester : EditorWindow
{
	public class AnimTrack
	{
		internal GameObject 	m_GameObject;
		internal AnimationClip	m_Animation;
		internal float 			m_Speed = 1.0f;
		internal float 			m_Delay = 0.0f;
		
		public AnimTrack(GameObject inGameObject) {
			m_GameObject = inGameObject;
			
			if(m_GameObject.GetComponent<Animation>() != null)
				m_Animation  = m_GameObject.GetComponent<Animation>().clip;
		}
	};
	
	
	
//	GameObject[]	m_EditableObjects;
	AnimTrack[]		m_EditableObjects;
	bool 			m_IsPlaying = false;
	float 			m_CurrentTime = 0;
	float 			m_AnimTime    = 0;
	float 			m_LastUpdateTime = 0;
	float 			m_MaxSliderTime  = 0;	

	//AnimationCurve 	testCurve = AnimationCurve.Linear(0,0,10,10);	
//	AnimationClip   testAnim;
	
	// Add menu item to the Window menu
	[MenuItem ("Window/Anim Tester")]
	static void Init()
	{
		Init(true);
	}
	
	static void Init (bool setMaxSize)
	{
		// Get existing open window or if none, make a new one:
		AnimTester wnd = EditorWindow.GetWindow<AnimTester> (false, "Anim Tester");
		
		//restrict size
		Object[] activeGOs = Selection.GetFiltered(typeof(Animation), SelectionMode.Editable | SelectionMode.TopLevel);

		wnd.minSize = new Vector2(500,  75 + activeGOs.Length*20);
		
		if (setMaxSize)
		{
			wnd.maxSize = new Vector2(500,  GetMaxHeight(activeGOs.Length));	//75.1, because when the values are exactly the same, the window doesn't have a border which is ugly
		
//		if (wnd.docked == false)										//wnd.docked is unfortunately inaccessible
			wnd.maxSize = new Vector2(4000, 4000);						//setting back to defaults (after formatting the window size) to allow window resizing which is good for docked windows (otherwise will resize the parent)
		}
	}
	
	// Implement your own editor GUI here.
	void OnGUI ()
	{	
		GUIEditorUtils.LookLikeControls();
		
		if (m_EditableObjects == null)
		{
			GUILayout.Space(30);
			EditorGUILayout.BeginHorizontal("box");			
				GUILayout.Label("Select object(s) that you want to see animated first...");
				GUILayout.FlexibleSpace();
				if ( GUILayout.Button("Refresh", GUILayout.Width(78)) )
				{
					RefreshContent();
				}
			EditorGUILayout.EndHorizontal();
			return;
		}
		
		//update the slider time...
		m_MaxSliderTime = 0;
		
		EditorGUILayout.BeginHorizontal();
			bool OldGUI = GUI.enabled;
			GUI.enabled = false;
	        GUILayout.Label("Target",  	  GUILayout.Width(150),  GUILayout.ExpandWidth(false));
	        GUILayout.Label("Animation",  GUILayout.Width(200),  GUILayout.ExpandWidth(false));
	        GUILayout.Label("Length",	  GUILayout.Width(75),  GUILayout.ExpandWidth(false));
	        GUILayout.Label("Speed",      GUILayout.Width(75),  GUILayout.ExpandWidth(false));
			
			//GUILayout.Space(2*kPlsuMinusWidth);
			//GUILayout.Space(kPlsuMinusWidth);
			//GUILayout.Space(kPlsuMinusWidth);
	
			GUI.enabled = OldGUI;				
		EditorGUILayout.EndHorizontal();		
		
		//display tracks per object and update m_MaxSliderTime
		foreach (AnimTrack track in m_EditableObjects)
		{
			if (track != null && track.m_GameObject != null)
			{
				EditorGUILayout.BeginHorizontal("", GUILayout.MaxHeight(11));
					
					GUILayout.Label(track.m_GameObject.name, GUILayout.Width(150), GUILayout.ExpandWidth(false));
					
					track.m_Animation = (AnimationClip)EditorGUILayout.ObjectField(track.m_Animation, typeof(AnimationClip), false, GUILayout.Width(200), GUILayout.ExpandWidth(false));
					
					string animLength = (track.m_Animation != null) ? track.m_Animation.length.ToString("F3") : " -- ";
					GUILayout.Label(animLength, GUILayout.Width(75), GUILayout.ExpandWidth(false));
					
					track.m_Speed = EditorGUILayout.FloatField(track.m_Speed, GUILayout.Width(75),  GUILayout.ExpandWidth(false));
					
					float trackSpeed  = (track.m_Animation == null || track.m_Speed <= 0.001) ? 0.0f : track.m_Animation.length / track.m_Speed;
					m_MaxSliderTime   = Mathf.Max(m_MaxSliderTime, trackSpeed);

					track.m_Delay = EditorGUILayout.FloatField(track.m_Delay, GUILayout.Width(75),  GUILayout.ExpandWidth(false));
					m_MaxSliderTime   = Mathf.Max(m_MaxSliderTime, m_MaxSliderTime+track.m_Delay);
				
				EditorGUILayout.EndHorizontal();
			}
		}
	
		GUILayout.Space(10);
		
		EditorGUILayout.BeginHorizontal("box");
			if ( GUILayout.Button("Play", GUILayout.Width(78)) )			//beny: possible to use also GUILayout.ExpandWidth(false), but absolute width is better for this
			{
				m_AnimTime = 0;
				m_LastUpdateTime = Time.realtimeSinceStartup;
				m_IsPlaying = true;
			}
			
			if ( GUILayout.Button("Stop", GUILayout.Width(78)) )
			{
				m_AnimTime = 0;
				m_LastUpdateTime = 0;
				m_IsPlaying = false;
			}
	
			if ( GUILayout.Button("Pause", GUILayout.Width(78)) )
			{
				m_IsPlaying = !m_IsPlaying;
				m_LastUpdateTime = Time.realtimeSinceStartup;
			}
		
			GUILayout.Space(20);
			GUILayout.Label("Time: " + m_AnimTime.ToString("F3"), GUILayout.Width(95));	//do not allow the string to resize the label (and format it to .3 digits)
//			GUILayout.Space(10);
		
			if ( GUILayout.Button("Refresh", GUILayout.Width(78)) )
			{
				RefreshContent();
			}
		
		EditorGUILayout.EndHorizontal();			
		
		GUI.changed = false;

		bool oldEnabled = GUI.enabled;
		GUI.enabled = (m_MaxSliderTime > 0);
		m_AnimTime = GUILayout.HorizontalSlider(m_AnimTime, 0, m_MaxSliderTime, GUILayout.Width(450));
		GUI.enabled = oldEnabled;

		if(GUI.changed)
		{
			UpdateAnimationsByTime(m_AnimTime);
		}
		
		//EditorGUILayout.CurveField("Test", testCurve);
	}
	
	// Called whenever the selection has changed.
	void OnSelectionChange() 
	{
		//this reinits the window to reflect the current object selection. It works, but maybe it's better without it. :)
//		RefreshContent();
	}
	
	// Called whenever the scene hierarchy
	// has changed.
	void OnHierarchyChange()
	{
	}
	
	// Called whenever the project has changed.
	void OnProjectChange()
	{
	}
	
	// OnInspectorUpdate is called at 10 frames
	// per second to give the inspector a chance
	// to update.
	void OnInspectorUpdate ()
	{
	}
	
	// Called 100 times per second on all visible
	// windows.
	void Update()
	{
		if (m_IsPlaying == true)
		{
			m_CurrentTime = Time.realtimeSinceStartup;
			m_AnimTime    += m_CurrentTime - m_LastUpdateTime;
			
//			Debug.Log("Anim Time : " + m_AnimTime + " CurrentTime : " + Time.realtimeSinceStartup);
			
			UpdateAnimationsByTime(m_AnimTime);
				
			m_LastUpdateTime = m_CurrentTime;
			this.Repaint();
		}
	}
	
	// This function is called when the scriptable
	// object will be destroyed.
	void OnDestroy()
	{
	}
	
	//calculates the maximum height of the window based on number of the objects
	static float GetMaxHeight(int numObjects)
	{
		return 75.1f + numObjects*20;
	}
	
	//
	private void RefreshContent()
	{
		//we have to do this bullsh*t because the wnd.docked is inaccessible and we need to know whether we can set the max size or not (i.e. guess if it's docked or custom sized)
		bool setMaxSize = true;
		
		AnimTester wnd = EditorWindow.GetWindow<AnimTester> (false, "Anim Tester");
		if(m_EditableObjects == null || m_EditableObjects.Length <= 0)
		{	setMaxSize = false;			}
		else if ( wnd.position.height > GetMaxHeight(m_EditableObjects.Length) )
		{	setMaxSize = false;			}
		
		//reinit the window and its content
		Init(setMaxSize);
		InitObjects();
		this.Repaint();
	}
	
	//
	private void InitObjects()
	{
		m_EditableObjects = null;
		
		Object[] activeGOs = Selection.GetFiltered(typeof(Animation), SelectionMode.Editable | SelectionMode.TopLevel);
		
		if(activeGOs.Length > 0)
		{
			m_EditableObjects = new AnimTrack[activeGOs.Length];
			int index = 0;
			foreach(Object obj in activeGOs)
			{
				AnimTrack aTrack = new AnimTrack(((Animation)obj).gameObject);
				if(aTrack.m_Animation)
					m_MaxSliderTime = Mathf.Max(m_MaxSliderTime, aTrack.m_Animation.length);
				m_EditableObjects[index++] = aTrack;
				
			}
		}
	}
	
	// This function is called when the object
	// is loaded.
	void OnEnable() 
	{
		InitObjects();
	}
	
	// This function is called when the scriptable
	// object goes out of scope.
	void OnDisable()
	{
		
	}
	
	// Called when the window gets keyboard
	// focus.
	void OnFocus()
	{
		
	}
	
	// Called when the window loses keyboard
	// focus.
	void OnLostFocus()
	{
	}
	
	void UpdateAnimationsByTime(float inAnimTime)
	{
		foreach (AnimTrack track in m_EditableObjects)
		{
			if (track.m_Animation != null)
			{
				float time = inAnimTime * track.m_Speed - track.m_Delay;
				if(time >= 0)
				{
					track.m_Animation.SampleAnimation(track.m_GameObject, time);
				}
			}
		}
	}
}
