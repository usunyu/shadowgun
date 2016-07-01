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

// MFG 2012
// StrictCharacter - Proxy part

using UnityEngine;

[AddComponentMenu("uLink Utilities/Strict Character/Proxy")]
[RequireComponent(typeof (SmoothTransform))]
public class StrictCharacterProxy : StrictCharacter
{
	SmoothTransform SmoothTransform;

	//private Vector3 FireDir = new Vector3();

	void Start()
	{
		SmoothTransform = GetComponent<SmoothTransform>();
	}

	protected override void Deactivate()
	{
		if (null != SmoothTransform)
		{
			SmoothTransform.Reset();
		}

		base.Deactivate();
	}

	// @see StrictCharacterCreator.uLink_OnSerializeNetworkView()
	void uLink_OnSerializeNetworkView(uLink.BitStream stream, uLink.NetworkMessageInfo info)
	{
		MFDebugUtils.Assert(stream.isReading);

		Vector3 pos = stream.ReadVector3();
		Vector3 vel = stream.ReadVector3();

		Vector3 FireDir = stream.ReadVector3();

		byte quantBodyYaw = stream.ReadByte();

		float bodyYaw = NetUtils.DequantizeAngle(quantBodyYaw, 8);

		Quaternion bodyRotation = Quaternion.Euler(0, bodyYaw, 0);

		Owner.BlackBoard.FireDir = Owner.BlackBoard.Desires.FireDirection = FireDir;

		// on proxies, approximate fire place 
		Owner.BlackBoard.Desires.FireTargetPlace = pos + FireDir*10;

		if (Owner.IsAlive)
		{
			if (null != SmoothTransform)
			{
				double timestamp = SmoothTransform.GetTime(info);
				SmoothTransform.AddState(timestamp, pos, vel, bodyRotation);
			}
			else
			{
				SetTransform(pos, bodyRotation, vel);
			}
		}
	}

	void Update()
	{
		MFDebugUtils.Assert(!networkView.isMine);

		if (null != SmoothTransform)
		{
			if (SmoothTransform.UpdateCustom())
			{
				SetTransform(SmoothTransform.Position, SmoothTransform.Rotation, SmoothTransform.Velocity);
			}
		}
	}

	void SetTransform(Vector3 Pos, Quaternion Rot, Vector3 Vel)
	{
		m_Transform.position = Pos;

		rotation = Rot;

		velocity = Vel;
	}
}
