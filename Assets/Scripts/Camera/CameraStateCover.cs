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

public class CameraStateCover : CameraState
{
	Transform DefaultPos;
	Transform DefaultLookat;

	GameObject Offset;
	Transform OffsetTransform;

	// Use this for initialization
	public CameraStateCover(AgentHuman owner) : base(owner)
	{
		DefaultPos = Owner.transform.Find("CameraTargetPos");
		DefaultLookat = Owner.transform.Find("CameraTargetDir");

		Offset = new GameObject("CameraOffsetCover");
		OffsetTransform = Offset.transform;
		OffsetTransform.parent = Owner.transform;
		OffsetTransform.position = DefaultPos.position;
		OffsetTransform.LookAt(DefaultLookat.position);
	}

	public override Transform GetDesiredCameraTransform()
	{
		OffsetTransform.position = DefaultPos.position;
		OffsetTransform.LookAt(DefaultLookat.position);

		// on Proxies, we haven't information about desired rotation in cover - this is needed for using spectator cameras watching another player
		if (Owner.NetworkView.isOwner)
		{
			OffsetTransform.RotateAround(DefaultLookat.position,
										 DefaultLookat.right,
										 Owner.BlackBoard.Desires.Rotation.eulerAngles.x - Owner.Transform.rotation.eulerAngles.x);
			OffsetTransform.RotateAround(DefaultLookat.position,
										 DefaultLookat.up,
										 Owner.BlackBoard.Desires.Rotation.eulerAngles.y - Owner.Transform.rotation.eulerAngles.y);
		}

//        Debug.Log(Owner.BlackBoard.Desires.Rotation.eulerAngles.x);

		return OffsetTransform;
	}

	/// 
	public override void Activate(Transform t)
	{
		//BENY: we do not call Activate() since we do not want to reset the GameCamera - which is currently the ONLY thing base.Activate() is doing...
		///		base.Activate(t);

//		Debug.Log ("CameraStateCover.Activate");

		if (Owner.NetworkView.isOwner)
		{
			float fov = GameCamera.Instance.DefaultFOV;
			if (Owner.IsInCover)
				fov *= Owner.WeaponComponent.GetCurrentWeapon().CoverFovModificator;
			GameCamera.Instance.SetFov(fov, 220);
		}
		//      OffsetTransform.position = t.TransformDirection(Vector3.zero);
		//GameCamera.Instance.Activate(t.position + Vector3.up, t.position + t.forward);
	}

	public override void Deactivate()
	{
		base.Deactivate();

//		Debug.Log ("CameraStateCover.Deactivate");
	}
}
