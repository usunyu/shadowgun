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

// (c)2011 MuchDifferent. All Rights Reserved.
// modified by MFG 2012 - code splitted into StrictCharacterCreator, StrictCharacterOwner and StrictCharacterProxy classes

using System;
using System.Collections.Generic;
using UnityEngine;
using uLink;

/// <summary>
/// A script example that can be use for players' objects in a 3d game without gravity. 
/// The object is floating in space just like a spaceship or a submarine does.
/// </summary>
/// <remarks>
/// When using this example script, it should be added as a component to the game object that a player controls.
/// The server should be authoritative when using this script (uLink.Network.isAuthoritativeServer = true).
/// The basic idea is that the server simulates all physics and checks if any player tries to cheat by 
/// sending movment orders as an RPC (The RPC name is ServerMove) with false coordinates to move faster than allowed in the game.
/// The server checks the incoming ServerMove RPC from the client and sends two kinds of RPCs back to the client.
/// If the client did move too fast (due to a cheating attempt or a bug or whatever) the server sends an RPC named
/// AdjustOwnerPos. If the position is good, the server sends an RPC named GoodOwnerPos. They are both sent as unreliable
/// RPCs from the server to the client to minimize server resources.
///
/// This script component also makes sure interpolation and extrapolation is used for the state synchronozation sent from
/// the server to clients. The state synchronization, arriving at the client, is stored in an internal array and the 
/// public properties interpolationBackTime and extrapolationLimit can be used to tune the correct behavior for every game. 
/// Please read the code for more details.
/// </remarks>
[RequireComponent(typeof (uLinkNetworkView))]
public class StrictCharacter : uLink.MonoBehaviour
{
	protected CharacterController character;

	protected Transform m_Transform; //cached transfrom, ne need to call getcomponent each time through tranform property

	protected Vector3 velocity;
	protected Quaternion rotation;

	protected AgentHuman Owner;

	public Vector3 Velocity
	{
		get { return velocity; }
	}

	public Quaternion Rotation
	{
		get { return rotation; }
	}

	public bool isStandingStill
	{
		get { return velocity == Vector3.zero; }
	}

	void Awake()
	{
		character = GetComponent<CharacterController>();

		Owner = GetComponent<AgentHuman>();
		m_Transform = transform;
	}

	void Activate()
	{
		enabled = true;
	}

	protected virtual void Deactivate()
	{
		enabled = false;

		velocity = default(Vector3);

		rotation = default(Quaternion);
	}

	// old/original example implementation
	/*
	void uLink_OnSerializeNetworkView(uLink.BitStream stream, uLink.NetworkMessageInfo info)
	{
		if (stream.isWriting)
		{
			stream.Write(transform.position);
			stream.Write(velocity);
			stream.Write(rotation);
			stream.Write(isFiring);
		}
		else
		{
            Vector3 pos = stream.Read<Vector3>();
            Vector3 vel = stream.Read<Vector3>();
            Quaternion rot = stream.Read<Quaternion>();
            isFiring = stream.Read<bool>();

			// Shift the buffer sideways, deleting state 20
			for (int i = proxyStates.Length - 1; i >= 1; i--)
			{
				proxyStates[i] = proxyStates[i - 1];
			}

			// Record current state in slot 0
            proxyStates[0].pos = pos;
            proxyStates[0].vel = vel;
            proxyStates[0].rot = rot;
            proxyStates[0].timestamp = info.timestamp;

			// Update used slot count, however never exceed the buffer size
			// Slots aren't actually freed so this just makes sure the buffer is
			// filled up and that uninitalized slots aren't used.
			proxyStateCount = Mathf.Min(proxyStateCount + 1, proxyStates.Length);

			// Check if states are in order
			if (proxyStates[0].timestamp < proxyStates[1].timestamp)
				Debug.LogError("Timestamp inconsistent: " + proxyStates[0].timestamp + " should be greater than " + proxyStates[1].timestamp);
		}
	}
	
	// We have a window of interpolationBackTime where we basically play 
	// By having interpolationBackTime the average ping, you will usually use interpolation.
	// And only if no more data arrives we will use extra polation
	void Update()
	{
		if (uLink.Network.isAuthoritativeServer && uLink.Network.isServerOrCellServer)
		{
			return;
		}

		if (networkView.isMine)
		{
            if ((transform.position - previousPosition).magnitude < 0.0001f) // fuck of errors in floats
                velocity = Vector3.zero;
            else
            {
                velocity = (transform.position - previousPosition) / Time.deltaTime;
                previousPosition = transform.position;
            }
			return;
		}

		// This is the target playback time of the rigid body
		double interpolationTime = uLink.Network.time - interpolationBackTime;

		// Use interpolation if the target playback time is present in the buffer
		if (proxyStates[0].timestamp > interpolationTime)
		{
			// Go through buffer and find correct state to play back
			for (int i=0;i<proxyStateCount;i++)
			{
				if (proxyStates[i].timestamp <= interpolationTime || i == proxyStateCount-1)
				{
					// The state one slot newer (<100ms) than the best playback state
					State rhs = proxyStates[Mathf.Max(i-1, 0)];
					// The best playback state (closest to 100 ms old (default time))
					State lhs = proxyStates[i];

					// Use the time between the two slots to determine if interpolation is necessary
					double length = rhs.timestamp - lhs.timestamp;
					float t = 0.0F;
					// As the time difference gets closer to 100 ms t gets closer to 1 in 
					// which case rhs is only used
					// Example:
					// Time is 10.000, so sampleTime is 9.900 
					// lhs.time is 9.910 rhs.time is 9.980 length is 0.070
					// t is 9.900 - 9.910 / 0.070 = 0.14. So it uses 14% of rhs, 86% of lhs
					if (length > 0.0001)
						t = (float)((interpolationTime - lhs.timestamp) / length);
					
					// if t=0 => lhs is used directly
					transform.position = (lhs.pos == rhs.pos ? lhs.pos : Vector3.Lerp(lhs.pos, rhs.pos, t));
					rotation = (lhs.rot == rhs.rot ? lhs.rot : Quaternion.Slerp(lhs.rot, rhs.rot, t));
					velocity = proxyStates[i].vel;

					return;
				}
			}
		}
		// Use extrapolation
		else
		{
			State latest = proxyStates[0];
			
			velocity = proxyStates[0].vel;

			float extrapolationLength = (float)(interpolationTime - latest.timestamp);
			// Don't extrapolation for more than 500 ms, you would need to do that carefully
			if (extrapolationLength < extrapolationLimit)
			{				
				transform.position = latest.pos + latest.vel * extrapolationLength;
				rotation = latest.rot;

				if (character.enabled)
					character.SimpleMove(latest.vel);
			}
		}
	}

	void LateUpdate()
	{
		if (!uLink.Network.isAuthoritativeServer || uLink.Network.isServerOrCellServer || !networkView.isMine)
		{
			return;
		}

		// TODO: optimize by not sending rpc if no input and rotation. also add idleTime so server's timestamp is still in sync

		Move move;
		move.timestamp = uLink.Network.time;
		move.deltaTime = (ownerMoves.Count > 0) ? (float)(move.timestamp - ownerMoves[ownerMoves.Count - 1].timestamp) : 0.0f;
		move.vel = velocity;

		ownerMoves.Add(move);

		Quaternion rotation = Owner.BlackBoard.Desires.Rotation;
		bool isFiring = Owner.BlackBoard.Desires.WeaponTriggerOn;

		networkView.UnreliableRPC("ServerUpdate", uLink.NetworkPlayer.server, transform.position, move.vel, rotation, isFiring, Owner.BlackBoard.FireDir);
	}

	[uSuite.RPC]
	void ServerUpdate(Vector3 ownerPos, Vector3 vel, Quaternion rot, bool isFiring, Vector3 fireDir, uLink.NetworkMessageInfo info)
	{
		if (info.timestamp <= serverLastTimestamp)
			return;

		if (vel.sqrMagnitude > sqrMaxServerSpeed)
		{
			vel.x = vel.y = vel.z = Mathf.Sqrt(sqrMaxServerSpeed) / 3.0f;
		}

		float deltaTime = (float)(info.timestamp - serverLastTimestamp);
		Vector3 deltaPos = vel * deltaTime;

		if (applyTransformations)
		{
			transform.rotation = rot;
			character.Move(deltaPos);
		}

		transform.position = ownerPos; // TODO Hack, this means the server is no longer authoritative.
		rotation = rot;
		velocity = vel;
		this.isFiring = isFiring;
        Owner.BlackBoard.Desires.FireDirection = Owner.BlackBoard.FireDir = fireDir;
		serverLastTimestamp = info.timestamp;

		Vector3 serverPos = transform.position;
		Vector3 diff = serverPos - ownerPos;

		if (Vector3.SqrMagnitude(diff) > sqrMaxServerError)
		{
			networkView.UnreliableRPC("AdjustOwnerPos", uLink.RPCMode.Owner, serverPos);
		}
		else
		{
			networkView.UnreliableRPC("GoodOwnerPos", uLink.RPCMode.Owner);
		}
	}

	[uSuite.RPC]
	void GoodOwnerPos(uLink.NetworkMessageInfo info)
	{
		Move goodMove;
		goodMove.timestamp = info.timestamp;
		goodMove.deltaTime = 0;
		goodMove.vel = Vector3.zero;

		int index = ownerMoves.BinarySearch(goodMove);
		if (index < 0) index = ~index;

		ownerMoves.RemoveRange(0, index);
	}

	[uSuite.RPC]
	void AdjustOwnerPos(Vector3 pos, uLink.NetworkMessageInfo info)
	{
		GoodOwnerPos(info);
		
		transform.position = pos;

		foreach (Move move in ownerMoves)
		{
			character.Move(move.vel * move.deltaTime);
		}
	}
	/**/
}
