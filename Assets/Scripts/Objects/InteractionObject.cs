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
using System.Collections;
using System.Collections.Generic;

public abstract class InteractionObject : MonoBehaviour
{
	public GameObject Icon;

	public Transform EntryTransform;
	public AnimationClip UserAnimationClip;

	public static int UseLayer = 16; //defined in tag manager

	protected Animation Animation;
	protected Transform Transform;
	bool _InteractionObjectUsable;

	public bool InteractionObjectUsable
	{
		get { return _InteractionObjectUsable; }
		protected set
		{
			_InteractionObjectUsable = value;
			if (_InteractionObjectUsable == false)
				Icon.SetActive(false);
		}
	}

	public bool DisableDuringFight { get; protected set; }

	public Vector3 Position
	{
		get { return EntryTransform ? EntryTransform.position : Transform.position; }
	}

	public bool IsActive
	{
		get { return InteractionObjectUsable && IsEnabled; }
	}

	public bool IsEnabled { get; protected set; }

	public virtual bool IsInteractionFinished
	{
		get { return EndOfInteraction < Time.timeSinceLevelLoad; }
		protected set { }
	}

	public virtual Transform GetEntryTransform()
	{
		return EntryTransform;
	}

	public string GetUserAnimation()
	{
		return UserAnimationClip.name;
	}

	public virtual float UseTime
	{
		get { return UserAnimationClip.length; }
	}

	float EndOfInteraction;

	public void Initialize()
	{
		//Debug.Log(gameObject.name + " initialize");
		InteractionObjectUsable = true;
		DisableDuringFight = true;
		Transform = transform;

		if (Icon.GetComponent<Collider>() == null)
		{
			SphereCollider sc = Icon.AddComponent<SphereCollider>();
			sc.gameObject.layer = UseLayer;
			sc.radius *= 1.5f; //icony se scluji se vzdalenosti
		}
		// AXTODO
		//Icon.SetActive(IsActive);		  
	}

	public virtual void OnDestroy()
	{
		UserAnimationClip = null;
	}

	public virtual void Enable()
	{
		//Debug.Log(gameObject.name + " enable " + OnGameEvents.Count);
		IsEnabled = true;

		Icon.SetActive(IsActive);

		//Debug.Log(gameObject.name + " " + IsActive);
	}

	public virtual void Disable()
	{
		//Debug.Log(gameObject.name + " disable");

		Icon.SetActive(false);
		IsEnabled = false;
	}

	public virtual void DoInteraction()
	{
		EndOfInteraction = UserAnimationClip.length + Time.timeSinceLevelLoad - 0.3f;
	}

	public virtual void Reset()
	{
		//Debug.Log(gameObject.name + " reset");
		InteractionObjectUsable = true;
	}

	void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		//Gizmos.DrawSphere(transform.position, 0.4f);

		Gizmos.DrawIcon(transform.position, "InteractionUse.tif");
	}
}
