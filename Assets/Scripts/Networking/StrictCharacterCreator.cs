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
// StrictCharacter - Server/Creator part

// AngelOf : UnreliableRPC causing problems on PROXY objects - reading past the buffer size
//           even that command was send to OWNER object, not PROXY object
//           TODO : Optimize

#define USE_RPC_INSTEAD_UNRELIABLERPC

using UnityEngine;

[AddComponentMenu("uLink Utilities/Strict Character/Creator")]
public class StrictCharacterCreator : StrictCharacter
{
	public float sqrMaxServerError = 20.0f;
	public float sqrMaxServerSpeed = 1000.0f;

	public bool applyTransformations = true;

	double serverLastTimestamp = 0;

	protected override void Deactivate()
	{
		serverLastTimestamp = 0;

		base.Deactivate();
	}

	// server -> proxies
	// @see StrictCharacterOwner.uLink_OnSerializeNetworkViewOwner()
	void uLink_OnSerializeNetworkViewOwner(uLink.BitStream stream, uLink.NetworkMessageInfo info)
	{
		MFDebugUtils.Assert(stream.isWriting);

		if (Owner.IsInKnockdown)
		{
			stream.Write(m_Transform.position);
			stream.Write(m_Transform.rotation);
			stream.Write(velocity);
		}
	}

	// server -> proxies
	// @see StrictCharacterProxy.uLink_OnSerializeNetworkView()
	void uLink_OnSerializeNetworkView(uLink.BitStream stream, uLink.NetworkMessageInfo info)
	{
		MFDebugUtils.Assert(stream.isWriting);

		stream.WriteVector3(m_Transform.position);
		stream.WriteVector3(velocity);
		//stream.Write( m_Transform.rotation ); // kdyz se posila jenom "rotation", tak nefunguje spravne proxy, kdyz se ignoruje server update
		stream.WriteVector3(Owner.BlackBoard.FireDir);

		Quaternion bodyRotation = m_Transform.rotation;
		byte quantBodyYaw = (byte)NetUtils.QuantizeAngle(bodyRotation.eulerAngles.y, 8);

		stream.WriteByte(quantBodyYaw);
	}

	// RPC call from Owner to server to check desired position and directions
	// @see StrictCharacterOwner.GoodOwnerPos() and StrictCharacterOwner.AdjustOwnerPos()
	[uSuite.RPC]
	void ServerUpdate(Vector3 ownerPos,
					  Vector3 vel,
					  byte quantBodyYaw,
					  byte quantAimPitch,
					  byte quantAimYaw,
					  Vector3 FireTargetPlace,
					  uLink.NetworkMessageInfo info)
	{
		if (!Owner.IsAlive || Owner.IsInKnockdown)
		{
			// ignore any data from client when the character is dead
			//ignore any data when he is in knockdown, because move is handled by server now... 
			return;
		}

		if (info.timestamp <= serverLastTimestamp)
			return;

		float bodyYaw = NetUtils.DequantizeAngle(quantBodyYaw, 8);
		Quaternion bodyRotation = Quaternion.Euler(0, bodyYaw, 0);

		float aimPitch = NetUtils.DequantizeAngle(quantAimPitch, 8);
		float aimYaw = NetUtils.DequantizeAngle(quantAimYaw, 8);
		Quaternion aimRotation = Quaternion.Euler(aimPitch, aimYaw, 0);
		Vector3 fireDir = aimRotation*Vector3.forward;
		Owner.BlackBoard.Desires.Rotation = aimRotation;

#if !DEADZONE_CLIENT
		ServerAnticheat.ReportMove(Owner.NetworkView.owner, ownerPos, vel, info);
#endif

		//TODO remove/reimplement this is a very naive code. The server accepts the position from the client but limits the speed at the same time.
		//The only benefit I can see that the character will not move too fast once a no more messages will come from its client.
		if (vel.sqrMagnitude > sqrMaxServerSpeed)
		{
			vel.x = vel.y = vel.z = Mathf.Sqrt(sqrMaxServerSpeed)/3.0f;
		}

		//float deltaTime = (float)( info.timestamp - serverLastTimestamp );
		//Vector3 deltaPos = vel * deltaTime;

		if (applyTransformations)
		{
			//m_Transform.rotation = bodyRotation;
			m_Transform.localRotation = bodyRotation;

			// character.Move( deltaPos ); // HACK for now
		}

		//m_Transform.position = ownerPos; 
		m_Transform.localPosition = ownerPos; // TODO Hack, this means the server is no longer authoritative.

		rotation = bodyRotation;
		velocity = vel;

		Owner.BlackBoard.Desires.FireDirection = Owner.BlackBoard.FireDir = fireDir;

		Owner.BlackBoard.Desires.FireTargetPlace = FireTargetPlace;

		serverLastTimestamp = info.timestamp;

		/*Vector3 serverPos = m_Transform.position;
		Vector3 diff = serverPos - ownerPos;*/

		/*
		if( Vector3.SqrMagnitude( diff ) > sqrMaxServerError )
		{
#if USE_RPC_INSTEAD_UNRELIABLERPC
			networkView.RPC( "AdjustOwnerPos", uLink.RPCMode.Owner, serverPos );
#else 
			networkView.UnreliableRPC( "AdjustOwnerPos", uLink.RPCMode.Owner, serverPos );
#endif //USE_RPC_INSTEAD_UNRELIABLERPC
			
		}
		else
		{
#if USE_RPC_INSTEAD_UNRELIABLERPC
			networkView.RPC( "GoodOwnerPos", uLink.RPCMode.Owner );
#else
			networkView.UnreliableRPC( "GoodOwnerPos", uLink.RPCMode.Owner );
#endif // USE_RPC_INSTEAD_UNRELIABLERPC
		}
		
		*/
		if (velocity.sqrMagnitude > Mathf.Epsilon)
		{
			Owner.cbServerUserInput();
		}
	}

	public void SetSynchronizeOwner(bool sync)
	{
	}
}
