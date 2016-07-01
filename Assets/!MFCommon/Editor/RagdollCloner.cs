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

using System;
using System.Reflection;

//
public class RagdollCloner : EditorWindow 
{
	
	GameObject	BaseRagdoll = null;
	
//	bool		DBG_copied = false;
	
	
	// Add menu item to the Window menu
	[MenuItem ("Window/Ragdoll Cloner")]
	static void Init ()
	{
		Init(true);
	}
	
	static void Init (bool setMaxSize)
	{
		// Get existing open window or if none, make a new one:
		RagdollCloner wnd = EditorWindow.GetWindow<RagdollCloner> (false, "Ragdoll Cloner");
		
		//restrict size
//		UnityEngine.Object[] activeGOs = Selection.GetFiltered(typeof(AgentHuman), SelectionMode.Editable | SelectionMode.TopLevel);

		wnd.minSize = new Vector2(400,  95);
		
		if (setMaxSize)
		{
			wnd.maxSize = new Vector2(400,  95.1f);
		
//		if (wnd.docked == false)										//wnd.docked is unfortunately inaccessible
			wnd.maxSize = new Vector2(4000, 4000);						//setting back to defaults (after formatting the window size) to allow window resizing which is good for docked windows (otherwise will resize the parent)
		}
	}
	
	// Implement your own editor GUI here.
	void OnGUI ()
	{
		GUIEditorUtils.LookLikeControls();
		
		//base ragdoll field
		GUILayout.Space(20);
		EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Base Ragdoll:", GUILayout.Width(95), GUILayout.ExpandWidth(false));
		
			BaseRagdoll = (GameObject)EditorGUILayout.ObjectField(BaseRagdoll, typeof(GameObject), true, GUILayout.Width(220), GUILayout.ExpandWidth(false));
		
		EditorGUILayout.EndHorizontal();		
		GUILayout.Space(10);
		
		//clone button
		EditorGUILayout.BeginHorizontal("box");
			GUILayout.Label("", GUILayout.Width(90), GUILayout.ExpandWidth(false));

			UnityEngine.Object[]	activeGOs	= Selection.GetFiltered(typeof(AgentHuman), SelectionMode.Editable | SelectionMode.TopLevel);
			string caption = ("Clone To Selection: " + activeGOs.Length + (activeGOs.Length == 1 ? " object" : " objects") );
		
			if ( GUILayout.Button(caption, GUILayout.Width(200)) )			//beny: possible to use also GUILayout.ExpandWidth(false), but absolute width is better for this
			{
				CloneToSelection();
			}
		
			GUILayout.Label("", GUILayout.Width(100), GUILayout.ExpandWidth(false));
			GUILayout.Space(20);
		EditorGUILayout.EndHorizontal();			
		
		GUI.changed = false;


//		if (GUI.changed)
		{
			//check the base ragdoll
			if ( BaseRagdoll )
			{
				AgentHuman agent = BaseRagdoll.GetComponent<AgentHuman>();
				
				if (!agent || !agent.RagdollRoot)			//do not allow other objects to be picked in
				{
					BaseRagdoll = null;
					this.Repaint();
				}
			}
		}
	}
	
	//clone the Ragdoll setup from BaseRagdoll to every (suitable) selected object
	void CloneToSelection()
	{
//		DBG_copied = false;
		
		Debug.Log ("Ragdoll Cloner: base=" + (BaseRagdoll ? BaseRagdoll.name : "null") );
		
		if (!BaseRagdoll)
		{
			Debug.Log ("Ragdoll Cloner: select a valid ragdoll agent first." );
			return;
		}
		
		//
		AgentHuman				baseAgent	= BaseRagdoll.GetComponent<AgentHuman>();
		UnityEngine.Object[]	activeGOs	= Selection.GetFiltered(typeof(AgentHuman), SelectionMode.Editable | SelectionMode.TopLevel);
		
		foreach (UnityEngine.Object obj in activeGOs)
		{
			AgentHuman	agent = (obj as AgentHuman);
			
			if (agent == baseAgent)
			{
				Debug.Log ("Ragdoll Cloner: SKIPPING target=" + agent.name + " - the same object as Base Ragdoll." );
				continue;
			}
			
			if ( !agent.RagdollRoot )
			{
				Debug.Log ("Ragdoll Cloner: SKIPPING target=" + agent.name + " - AgentHuman.RagdollRoot is NOT SET." );
				continue;				//do not proceed with any selected non-ragdoll agents
			}

			Debug.Log ("Ragdoll Cloner: target=" + agent.name );
			
			//
			CloneRagdoll(baseAgent.RagdollRoot, agent.RagdollRoot);
			
			//save the prefab modifications
//			UnityEngine.Object	prefab = PrefabUtility.GetPrefabObject(obj);
//			PropertyModification[] mod = PrefabUtility.GetPropertyModifications(prefab);		//GetPrefabObject() ?
//			PropertyModification[] mod = PrefabUtility.GetPropertyModifications(obj);		//GetPrefabObject() ?
//			PrefabUtility.SetPropertyModifications(prefab, mod);
//			PrefabUtility.SetPropertyModifications(obj, mod);
//			PrefabUtility.SetPropertyModifications(BaseRagdoll, mod);
		}
		
		//save modifications
		AssetDatabase.SaveAssets();
	}
	
	//
	void CloneRagdoll(Transform baseObj, Transform targetObj)
	{
//		if (DBG_copied)
//			return;
		
//		Debug.Log ("Ragdoll Cloner: CloneRagdoll(), baseObj=" + baseObj.name + ", targetObj=" + targetObj );
		
		//clone HitZoneEffects
		CloneHitZoneEffects(baseObj, targetObj);
		
		//clone Capsule Collider
		CloneCapsuleCollider(baseObj, targetObj);
		
		//clone Box Collider
		CloneBoxCollider(baseObj, targetObj);
		
		//clone Sphere Collider
		CloneSphereCollider(baseObj, targetObj);
		
		//clone Rigidbody
		CloneRigidbody(baseObj, targetObj);
		
		//clone CharacterJoint
		CloneCharacterJoint(baseObj, targetObj);
		
		//recurse to children
		foreach (Transform child in baseObj)
		{
			Transform targetChild = targetObj.FindChild( child.name );
			
			if (targetChild)
				CloneRagdoll(child, targetChild);
		}
	}

	//dump the properties of given object
	void DumpProps(Component component)
	{
		if (!component)
			return;
		
		Type type = component.GetType();
		
		Debug.Log ("Component name=" + component.GetFullName() + ", type=" + type);
		
		MemberInfo[] memberInfo = type.GetMembers(); 
		
		foreach (MemberInfo mi in memberInfo)
		{
//			Type mt = mi.GetType();
			Debug.Log ("  Member name=" + mi.Name + ", type=" + mi.MemberType);
		}
	}
	
	//clone HitZoneEffects
	void CloneHitZoneEffects(Transform baseObj, Transform targetObj)
	{
//		if (DBG_copied)
//			return;
		
		HitZoneEffects	b = baseObj.GetComponent<HitZoneEffects>();
		HitZoneEffects	t = targetObj.GetComponent<HitZoneEffects>();

		if ( b && !t )	//we have just base - create the component on target
		{
			t = targetObj.gameObject.AddComponent<HitZoneEffects>();
		}
		
		if ( b && t )
		{
//			Component c = EditorUtility.CloneComponent(b);
//			Object.Destroy(t);
			
//			EditorUtility.CopySerialized(b, t);		// !!! Calling this crashes the Editor (either immediately or after clicking on the target object in the Hierarchy tree) !!!
			
//			Debug.Log ("CloneHitZoneEffects:");
			
//			DumpProps(t);

//			DBG_copied = true;
			
			t.DamageModifier				= b.DamageModifier;
///			t.ForPlayer						= b.ForPlayer;						//DEAD TRIGGER-specific
			t.MustDieToDestroy				= b.MustDieToDestroy;
			t.DestroyCumulativePercentage	= b.DestroyCumulativePercentage;
			t.DestroyBashPercentage			= b.DestroyBashPercentage;
			t.DestroyParticle				= b.DestroyParticle;				//Instantiate() ???
			
			EditorUtility.SetDirty(t);
		}
	}

	//clone Capsule Collider
	void CloneCapsuleCollider(Transform baseObj, Transform targetObj)
	{
		CapsuleCollider	b = baseObj.GetComponent<CapsuleCollider>();
		CapsuleCollider	t = targetObj.GetComponent<CapsuleCollider>();
		
		if ( b && !t )	//we have just base - create the component on target
		{
			t = targetObj.gameObject.AddComponent<CapsuleCollider>();
		}
		
		if ( b && t )
		{
//			EditorUtility.CopySerialized(b, t);
//			Debug.Log ("CloneCapsuleCollider:");
			
//			DumpProps(t);
			
			t.isTrigger			= b.isTrigger;
			t.material			= b.material;
			t.center			= b.center;
			t.radius			= b.radius;
			t.height			= b.height;
			t.direction			= b.direction;
			
			EditorUtility.SetDirty(t);
		}
	}
	
	//clone Box Collider
	void CloneBoxCollider(Transform baseObj, Transform targetObj)
	{
		BoxCollider	b = baseObj.GetComponent<BoxCollider>();
		BoxCollider	t = targetObj.GetComponent<BoxCollider>();
		
		if ( b && !t )	//we have just base - create the component on target
		{
			t = targetObj.gameObject.AddComponent<BoxCollider>();
		}
		
		if ( b && t )
		{
//			EditorUtility.CopySerialized(b, t);
//			Debug.Log ("CloneBoxCollider:");
			
//			DumpProps(t);
			
			t.isTrigger			= b.isTrigger;
			t.material			= b.material;
			t.center			= b.center;
			t.size				= b.size;
			
			EditorUtility.SetDirty(t);
		}
	}
	
	//clone Sphere Collider
	void CloneSphereCollider(Transform baseObj, Transform targetObj)
	{
		SphereCollider	b = baseObj.GetComponent<SphereCollider>();
		SphereCollider	t = targetObj.GetComponent<SphereCollider>();
		
		if ( b && !t )	//we have just base - create the component on target
		{
			t = targetObj.gameObject.AddComponent<SphereCollider>();
		}
		
		if ( b && t )
		{
//			EditorUtility.CopySerialized(b, t);
//			Debug.Log ("CloneSphereCollider:");
			
//			DumpProps(t);
			
			t.isTrigger			= b.isTrigger;
			t.material			= b.material;
			t.center			= b.center;
			t.radius			= b.radius;
			
			EditorUtility.SetDirty(t);
		}
	}
	
	//clone Rigidbody
	void CloneRigidbody(Transform baseObj, Transform targetObj)
	{
		Rigidbody	b = baseObj.GetComponent<Rigidbody>();
		Rigidbody	t = targetObj.GetComponent<Rigidbody>();
		
		if ( b && !t )	//we have just base - create the component on target
		{
			t = targetObj.gameObject.AddComponent<Rigidbody>();
		}
		
		if ( b && t )
		{
//			EditorUtility.CopySerialized(b, t);
//			Debug.Log ("CloneRigidbody:");
			
//			DumpProps(t);
			
			t.mass						= b.mass;
			t.drag						= b.drag;
			t.angularDrag				= b.angularDrag;
			t.useGravity				= b.useGravity;
			t.isKinematic				= b.isKinematic;
			t.interpolation				= b.interpolation;
			t.collisionDetectionMode	= b.collisionDetectionMode;
			t.constraints				= b.constraints;
			
			EditorUtility.SetDirty(t);
		}
	}
	
	//clone CharacterJoint
	void CloneCharacterJoint(Transform baseObj, Transform targetObj)
	{
		CharacterJoint	b = baseObj.GetComponent<CharacterJoint>();
		CharacterJoint	t = targetObj.GetComponent<CharacterJoint>();
		
		if ( b && !t )	//we have just base - create the component on target
		{
			t = targetObj.gameObject.AddComponent<CharacterJoint>();
		}
		
		if ( b && t )
		{
//			EditorUtility.CopySerialized(b, t);
//			Debug.Log ("CloneCharacterJoint:");
			
//			DumpProps(t);
			
			Rigidbody	connBody = null;
			Transform	parent = targetObj.parent;
			
			do 
			{
				connBody = parent.GetComponent<Rigidbody>();
				parent = parent.parent;
			} while ( connBody == null && parent.parent != null );
			
			t.connectedBody			= connBody;
			t.anchor				= b.anchor;
			t.axis					= b.axis;
			t.swingAxis				= b.swingAxis;
			t.lowTwistLimit			= b.lowTwistLimit;
			t.highTwistLimit		= b.highTwistLimit;
			t.swing1Limit			= b.swing1Limit;
			t.swing2Limit			= b.swing2Limit;
			t.breakForce			= b.breakForce;
			t.breakTorque			= b.breakTorque;
			
			EditorUtility.SetDirty(t);
		}
	}

	

	
	
	// Called whenever the selection has changed.
	void OnSelectionChange () 
	{
		this.Repaint();
	}
	
	// Called whenever the scene hierarchy
	// has changed.
	void OnHierarchyChange ()
	{
	}
	
	// Called whenever the project has changed.
	void OnProjectChange ()
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
	void Update ()
	{
	}
	
	// This function is called when the scriptable
	// object will be destroyed.
	void OnDestroy ()
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
		
//		RagdollCloner wnd = EditorWindow.GetWindow<RagdollCloner> (false, "Ragdoll Cloner");
		
		//reinit the window and its content
		Init(setMaxSize);
		this.Repaint();
	}
	
	// This function is called when the object
	// is loaded.
	void OnEnable () 
	{
	}
	
	// This function is called when the scriptable
	// object goes out of scope.
	void OnDisable ()
	{
	}
	
	// Called when the window gets keyboard
	// focus.
	void OnFocus ()
	{
	}
	
	// Called when the window loses keyboard
	// focus.
	void OnLostFocus ()
	{
	}
	
}
