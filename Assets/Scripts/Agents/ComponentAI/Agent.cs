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

//using System;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class Agent : uLink.MonoBehaviour
{
	public Transform Transform { get; private set; }
	public GameObject GameObject { get; private set; }
	public AudioSource Audio { get; private set; }
	public Animation Animation { get; private set; }
	public uLink.NetworkView NetworkView { get; private set; }

	public Vector3 Position
	{
		get { return Transform.position; }
	}

	public Vector3 Forward
	{
		get { return Transform.forward; }
	}

	public Vector3 Right
	{
		get { return Transform.right; }
	}

	public abstract bool IsAlive { get; }
	public abstract bool IsVisible { get; }

	public abstract bool IsInvulnerable { get; }
	public abstract bool IsInCover { get; }
	public abstract bool IsEnteringToCover { get; }

	public abstract Vector3 ChestPosition { get; }

	public virtual float HealthPercent
	{
		get { return 1; }
	} //implementovane pouze pro nektere agenty. pokud bude potreba pro vsechny, zmenit na abstract.

	public abstract bool IsFriend(AgentHuman target);

	public abstract void KnockDown(AgentHuman humanAttacker, E_MeleeType meleeType, Vector3 direction);

	protected void Initialize()
	{
		Transform = transform;
		GameObject = gameObject;
		Audio = GetComponent<AudioSource>();
		Animation = GetComponent<Animation>();
		NetworkView = networkView;
	}
}
