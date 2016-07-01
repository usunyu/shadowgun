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
// StrictCharacter - Owner part

using System;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("uLink Utilities/Strict Character/Owner")]
[RequireComponent(typeof (SmoothTransform))]
public class StrictCharacterOwner : StrictCharacter
{
	const int MAX_KNOCKDOWN_SPEED = 10;

	SmoothTransform SmoothTransformKnockdown;

	Vector3 previousPosition;

	struct Move : IComparable<Move>
	{
		public double timestamp;
		public float deltaTime;
		public Vector3 vel;

		public static bool operator ==(Move lhs, Move rhs)
		{
			return lhs.timestamp == rhs.timestamp;
		}

		public static bool operator !=(Move lhs, Move rhs)
		{
			return lhs.timestamp != rhs.timestamp;
		}

		public static bool operator >=(Move lhs, Move rhs)
		{
			return lhs.timestamp >= rhs.timestamp;
		}

		public static bool operator <=(Move lhs, Move rhs)
		{
			return lhs.timestamp <= rhs.timestamp;
		}

		public static bool operator >(Move lhs, Move rhs)
		{
			return lhs.timestamp > rhs.timestamp;
		}

		public static bool operator <(Move lhs, Move rhs)
		{
			return lhs.timestamp < rhs.timestamp;
		}

		public override bool Equals(object other)
		{
			if (other == null || !(other is Move))
				return false;

			return this == (Move)other;
		}

		public override int GetHashCode()
		{
			return timestamp.GetHashCode();
		}

		public int CompareTo(Move other)
		{
			if (this > other)
				return 1;

			if (this < other)
				return -1;

			return 0;
		}
	}

	//List<Move> ownerMoves = new List<Move>();

	void Start()
	{
		SmoothTransformKnockdown = GetComponent<SmoothTransform>();
	}

	protected override void Deactivate()
	{
		if (null != SmoothTransformKnockdown)
		{
			SmoothTransformKnockdown.Reset();
		}

		//ownerMoves.Clear();

		previousPosition = default(Vector3);

		base.Deactivate();
	}

	// We have a window of interpolationBackTime where we basically play 
	// By having interpolationBackTime the average ping, you will usually use interpolation.
	// And only if no more data arrives we will use extra polation
	void Update()
	{
		MFDebugUtils.Assert(networkView);
		MFDebugUtils.Assert(networkView.isMine);

		if (Owner.IsInKnockdown)
		{
			if (null != SmoothTransformKnockdown)
			{
				SmoothTransformKnockdown.UpdateCustom();

				SetTransform(SmoothTransformKnockdown.Position, SmoothTransformKnockdown.Rotation, SmoothTransformKnockdown.Velocity, true);
				//return;
			}
		}

		if ((m_Transform.position - previousPosition).magnitude < 0.0001f) // fuck of errors in floats
			velocity = Vector3.zero;
		else
		{
			Vector3 moveDir = m_Transform.position - previousPosition;
			velocity = moveDir/Time.deltaTime;

			if (Owner.IsInKnockdown)
			{
				moveDir.y = 0;

				float speed2 = velocity.sqrMagnitude;

				if (speed2 > MAX_KNOCKDOWN_SPEED*MAX_KNOCKDOWN_SPEED) //clip to maximal velocity
				{
					velocity = velocity.normalized*MAX_KNOCKDOWN_SPEED;
					m_Transform.position = previousPosition + velocity*Time.deltaTime;
				}
			}

			previousPosition = m_Transform.position;
		}
	}

	void SendMoveUpdateToServer()
	{
		// TODO: optimize by not sending rpc if no input and rotation. also add idleTime so server's timestamp is still in sync

		/*Move move;
		move.timestamp = uLink.Network.time;
		move.deltaTime = ( ownerMoves.Count > 0 ) ? (float)( move.timestamp - ownerMoves[ownerMoves.Count - 1].timestamp ) : 0.0f;
		move.vel = velocity;
		
		ownerMoves.Add( move );
		*/

		// The Desires.Rotation contains in fact rotation of the camera not the rotation of the agent's body
		//Quaternion bodyRotation = Owner.BlackBoard.Desires.Rotation;
		Quaternion bodyRotation = Owner.Transform.rotation;
		byte quantBodyYaw = (byte)NetUtils.QuantizeAngle(bodyRotation.eulerAngles.y, 8);

		// originaly we passed the Owner.BlackBoard.FireDir but the desired rotation should contain the same information
		byte quantAimPitch = (byte)NetUtils.QuantizeAngle(Owner.BlackBoard.Desires.Rotation.eulerAngles.x, 8);
		byte quantAimYaw = (byte)NetUtils.QuantizeAngle(Owner.BlackBoard.Desires.Rotation.eulerAngles.y, 8);

		networkView.UnreliableRPC("ServerUpdate",
								  uLink.NetworkPlayer.server,
								  m_Transform.position,
								  velocity,
								  quantBodyYaw,
								  quantAimPitch,
								  quantAimYaw,
								  Owner.BlackBoard.Desires.FireTargetPlace);
	}

	// Owner is sending informations about its state to the server
	void LateUpdate()
	{
		MFDebugUtils.Assert(networkView.isMine);

		if (Owner.IsAlive)
		{
			SendMoveUpdateToServer();
		}
	}

	/*// RPC call from server in situation when transformation data from owner ARE VALID
	// @see StrictCharacterCreator.ServerUpdate()
	[uSuite.RPC]
	void GoodOwnerPos( uLink.NetworkMessageInfo info )
	{
		Move goodMove;
		goodMove.timestamp = info.timestamp;
		goodMove.deltaTime = 0;
		goodMove.vel = Vector3.zero;

		int index = ownerMoves.BinarySearch( goodMove );
		if (index < 0) index = ~index;

		ownerMoves.RemoveRange(0, index);
	}
	
	// RPC call from server in situation when transformation data from owner ARE NOT VALID
	// @see StrictCharacterCreator.ServerUpdate()
	[uSuite.RPC]
	void AdjustOwnerPos( Vector3 pos, uLink.NetworkMessageInfo info )
	{
		GoodOwnerPos( info );
		
		m_Transform.position = pos;

		foreach( Move move in ownerMoves )
		{
			Owner.CharacterController.Move( move.vel * move.deltaTime );
		}
	}
	*/

	// @see StrictCharacterCreator.uLink_OnSerializeNetworkView()
	void uLink_OnSerializeNetworkViewOwner(uLink.BitStream stream, uLink.NetworkMessageInfo info)
	{
		Vector3 pos = stream.Read<Vector3>();
		Quaternion rot = stream.Read<Quaternion>();
		Vector3 vel = stream.Read<Vector3>();

		if (null != SmoothTransformKnockdown)
		{
			double timestamp = SmoothTransform.GetTime(info);
			SmoothTransformKnockdown.AddState(timestamp, pos, vel, rot);
		}
		else
		{
			SetTransform(pos, rot, vel);
		}
	}

	// @see StrictCharacterCreator.uLink_OnSerializeNetworkView()
	void uLink_OnSerializeNetworkView(uLink.BitStream stream, uLink.NetworkMessageInfo info)
	{
		Debug.LogWarning("StrictCharacterOwner.uLink_OnSerializeNetworkView() : unexpected method call");

		SerializeNetworkView(stream);
	}

	// @see StrictCharacterCreator.uLink_OnSerializeNetworkView()
	void SerializeNetworkView(uLink.BitStream stream)
	{
	}

	void SetTransform(Vector3 Pos, Quaternion Rot, Vector3 Vel, bool testCollisions = false)
	{
		if (true == testCollisions && null != character)
		{
			character.Move(Pos - m_Transform.position);
		}
		else
		{
			m_Transform.position = Pos;
		}

		m_Transform.rotation = Rot;

		Owner.BlackBoard.Desires.Rotation = m_Transform.rotation;
		Owner.BlackBoard.Desires.FireDirection = m_Transform.rotation*Vector3.forward;
	}
}
