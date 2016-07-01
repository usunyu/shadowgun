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
using System.Collections.Generic;

public abstract class CameraState
{
	protected AgentHuman Owner;

	// Use this for initialization
	public CameraState(AgentHuman owner)
	{
		Owner = owner;
	}

	public abstract Transform GetDesiredCameraTransform();

	public virtual void Activate(Transform t)
	{
		//  Debug.Log(Time.timeSinceLevelLoad + ToString() + " activate at " + t.position + " " + Camera.main.transform.forward);

		//WeaponBase	weapon = Owner.WeaponComponent.GetCurrentWeapon();

		if (Owner.NetworkView.isOwner)
			GameCamera.Instance.Reset(0, 30);

		//GameCamera.Instance.Activate(t.position + Vector3.up, t.position + t.forward);
	}

	public virtual void Deactivate()
	{
		//  Debug.Log(ToString() + " Deactivate");
	}
}
