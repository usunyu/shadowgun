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

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CoverAIInfo
{
	public Cover Cover;
	public float LeftCoverValidity;
	public float RightCoverValidity;
	public float MiddleCoverValidity;

	public void Reset()
	{
		Cover = null;
		LeftCoverValidity = 0;
		RightCoverValidity = 0;
		MiddleCoverValidity = 0;
	}
}

[AddComponentMenu("Game/Cover")]
[Serializable]
public class Cover : MonoBehaviour
{
	[Serializable]
	public enum E_CoverFlags
	{
		LeftCrouch = 0,
		RightCrouch,
		LeftStand,
		RightStand,
		UpCrouch,
		Max,
	}

	[Serializable]
	public class CoverFlagsBitArray
	{
		[SerializeField] int Flags = 0;

		public void Set(E_CoverFlags index, bool on)
		{
			if (on)
				Flags |= 1 << (int)index;
			else
				Flags &= ~(1 << (int)index);
		}

		public bool Get(E_CoverFlags index)
		{
			return (Flags & (1 << (int)index)) != 0;
		}
	}

	public CoverFlagsBitArray CoverFlags = new CoverFlagsBitArray();

	public bool CanJumpUp = false;
	public bool CanJumpOver { get; private set; }

	public class AgentsList : List<AgentHuman>
	{
		public void AddUnique(AgentHuman Human)
		{
			if (!Contains(Human))
			{
				Add(Human);
			}
		}
	};

	public AgentsList AllAgents = new AgentsList();

	public AgentsList LeftAgents = new AgentsList();
	public AgentsList MiddleAgents = new AgentsList();
	public AgentsList RightAgents = new AgentsList();

	public Transform[] m_Parts; // additional objects forming this cover

	public Vector3 Position
	{
		get { return Transform.position; }
	}

	public Transform Transform { get; private set; }
	Vector3 _LeftEdge;
	Vector3 _RightEdge;

	float _EdgeLength;

	public Vector3 LeftEdge
	{
		get { return _LeftEdge; }
	}

	public Vector3 RightEdge
	{
		get { return _RightEdge; }
	}

	public Vector3 EyeLeftEdge
	{
		get { return _LeftEdge + Left*0.5f + Vector3.up*1.6f; }
	}

	public Vector3 EyeRightEdge
	{
		get { return _RightEdge + Right*0.5f + Vector3.up*1.6f; }
	}

	public Vector3 EyeMiddle
	{
		get { return Position + Vector3.up*1.6f; }
	}

	public Vector3 Forward;
	public Vector3 Right;
	public Vector3 Left;

	public Cover OppositeCover { get; private set; }

	public bool IsStandAllowed
	{
		get { return CoverFlags.Get(E_CoverFlags.LeftStand) || CoverFlags.Get(E_CoverFlags.RightStand); }
	}

	public bool IsMiddleAllowed
	{
		get { return CoverFlags.Get(E_CoverFlags.UpCrouch); }
	}

	public bool IsLeftAllowed
	{
		get { return CoverFlags.Get(E_CoverFlags.LeftStand) || CoverFlags.Get(E_CoverFlags.LeftCrouch); }
	}

	public bool IsRightAllowed
	{
		get { return CoverFlags.Get(E_CoverFlags.RightCrouch) || CoverFlags.Get(E_CoverFlags.RightStand); }
	}

	public Vector3 MiddleEntryPos
	{
		get { return Position - Forward*1.5f; }
	}

	public Vector3 GetEyePos(E_CoverDirection pos)
	{
		if (pos == E_CoverDirection.Middle)
			return EyeMiddle;
		else if (pos == E_CoverDirection.Left)
			return EyeLeftEdge;
		else
			return EyeRightEdge;
	}

	void Awake()
	{
		Transform = transform;

		Forward = Transform.forward;
		Left = -Transform.right;
		Right = Transform.right;

		RaycastHit hitInfo;
		if (Physics.Raycast(Transform.position + Vector3.up*0.2f, Vector3.down, out hitInfo))
			Transform.position = hitInfo.point;

		if (Physics.Raycast(Transform.position + Vector3.up*0.45f, Forward, out hitInfo))
			Transform.position += Forward*(hitInfo.distance - 0.4f);

		_LeftEdge = Transform.position + Left*Transform.lossyScale.x*0.5f;
		_RightEdge = Transform.position + Right*Transform.lossyScale.x*0.5f;

		_EdgeLength = (_LeftEdge - _RightEdge).magnitude;
	}

	// Use this for initialization
	void Start()
	{
		OppositeCover = GetOppositeCover();

		CanJumpOver = OppositeCover && OppositeCover.CoverFlags.Get(Cover.E_CoverFlags.UpCrouch) && CoverFlags.Get(Cover.E_CoverFlags.UpCrouch);
	}

	// Update is called once per frame
	//void Update () {
	//
	//}

	public float GetCoverDirAngle(Vector3 dir)
	{
		return Vector3.Dot(Forward, dir);
	}

	public float GetCoverDirAngleToPos(Vector3 pos)
	{
		Vector3 dir = pos - Transform.position;
		dir.Normalize();

		return Vector3.Angle(Forward, dir);
	}

	public float GetDistanceTo(Vector3 pos)
	{
		Vector3 p = Mathfx.NearestPointStrict(_LeftEdge, _RightEdge, pos);
		return (pos - p).magnitude;
	}

	public Vector3 GetNearestPointOnCover(Vector3 pos)
	{
		return Mathfx.NearestPointStrict(_LeftEdge, _RightEdge, pos);
	}

	public bool CanFire(AgentHuman agent)
	{
		if (agent.BlackBoard.CoverPose == E_CoverPose.Stand)
		{
			if (agent.BlackBoard.CoverPosition == E_CoverDirection.Middle)
				return false;

			if (agent.BlackBoard.CoverPosition == E_CoverDirection.Left && CoverFlags.Get(Cover.E_CoverFlags.LeftStand) == false)
				return false;

			if (agent.BlackBoard.CoverPosition == E_CoverDirection.Right && CoverFlags.Get(Cover.E_CoverFlags.RightStand) == false)
				return false;
		}
		else
		{
			if (agent.BlackBoard.CoverPosition == E_CoverDirection.Middle && CoverFlags.Get(Cover.E_CoverFlags.UpCrouch) == false)
				return false;

			if (agent.BlackBoard.CoverPosition == E_CoverDirection.Left && CoverFlags.Get(Cover.E_CoverFlags.LeftCrouch) == false)
				return false;

			if (agent.BlackBoard.CoverPosition == E_CoverDirection.Right && CoverFlags.Get(Cover.E_CoverFlags.RightCrouch) == false)
				return false;
		}

		return true;
	}

	public void OccupyPosition(E_CoverDirection position, AgentHuman agent)
	{
		AllAgents.AddUnique(agent);

		switch (position)
		{
		case E_CoverDirection.Left:
			LeftAgents.AddUnique(agent);
			return;

		case E_CoverDirection.Right:
			RightAgents.AddUnique(agent);
			return;

		case E_CoverDirection.Middle:
			MiddleAgents.AddUnique(agent);
			return;

		case E_CoverDirection.Unknown:

			if (IsLeftAllowed)
				LeftAgents.AddUnique(agent);

			if (IsRightAllowed)
				RightAgents.AddUnique(agent);

			if (IsMiddleAllowed)
				MiddleAgents.AddUnique(agent);

			break;
		}
	}

	/// <summary>
	/// Unlocks the agent from all positions it occupies
	/// </summary> 
	public void FreePosition(AgentHuman agent)
	{
		RightAgents.Remove(agent);

		LeftAgents.Remove(agent);

		MiddleAgents.Remove(agent);

		AllAgents.Remove(agent);
	}

	bool IsPositionOccupied(E_CoverDirection position)
	{
		switch (position)
		{
		case E_CoverDirection.Left:
			return IsLeftAllowed == false || LeftAgents.Count > 0;
		case E_CoverDirection.Right:
			return IsRightAllowed == false || RightAgents.Count > 0;
		case E_CoverDirection.Middle:
			return IsMiddleAllowed == false || MiddleAgents.Count > 0;
		}

		return true;
	}

	// @return true if there is a place/enough space for the player
	public bool IsLockedForPlayer(Vector3 Position, float MinimalDistanceNeeded)
	{
		if (AllAgents.Count > 0)
		{
			float TestedPosition01 = GetPositionEdgeRelative(GetNearestPointOnCover(Position));

			foreach (AgentHuman Agent in AllAgents)
			{
				if (Agent != null)
				{
					float MinDistAllowed01 = GetDistanceEdgeRelative(Agent.CharacterRadius + MinimalDistanceNeeded);
					float AgentPosition01 = GetPositionEdgeRelative(Agent.Position);

					if (Mathf.Abs(AgentPosition01 - TestedPosition01) < MinDistAllowed01)
						return true;
				}
			}
		}

		return false;
	}

	// @return relative (0-1) position of given point
	//         - for position near the left  edge result will be near to 0.0f
	//         - for position near the right edge result will be near to 1.0f
	public float GetPositionEdgeRelative(Vector3 Position)
	{
		Vector3 EdgePos = Mathfx.NearestPointStrict(_LeftEdge, _RightEdge, Position);

		return Mathf.Clamp01((_LeftEdge - EdgePos).magnitude/_EdgeLength);
	}

	// @return converted distance (0-1) into relative value within cover size
	// Example : if edge is 2.0 meters long, converted value for 1.0 will be 0.5f
	public float GetDistanceEdgeRelative(float Distance)
	{
		return Mathf.Clamp01(Distance/_EdgeLength);
	}

	public float GetDistanceEdgeReal(float Distance01)
	{
		return Distance01*_EdgeLength;
	}

	Cover GetOppositeCover()
	{
		if (CoverFlags.Get(Cover.E_CoverFlags.UpCrouch) == false)
			return null;

		Transform p = Transform.parent;
		if (p == null)
			return null;

		Cover[] covers = p.GetComponentsInChildren<Cover>();

		foreach (Cover c in covers)
		{
			if (c == this)
				continue;

			if (c.IsPositionOccupied(E_CoverDirection.Middle))
				continue;

			//Debug.Log(Vector3.SqrMagnitude(c.Position - cover.Position), c);
			if (Vector3.SqrMagnitude(c.Position - Position) > 2.2f*2.2f)
				continue;

			if (Vector3.Dot(c.Forward, Forward) > -0.9f)
				continue;

			return c;
		}

		return null;
	}

	public bool IsPartOfCover(GameObject Obj)
	{
		Transform objTr = Obj.transform;

		// check corresponding game-object
		if (Transform.IsChildOf(objTr))
			return true;

		// check additional game-object
		if (m_Parts != null)
		{
			foreach (Transform t in m_Parts)
			{
				if (t.IsChildOf(objTr))
					return true;
			}
		}

		return false;
	}
}
