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

//using System.Collections.Generic;

public class ComponentBody : MonoBehaviour
{
	protected AgentHuman Owner;

	AgentActionRotate ActionRotate;
	AgentActionMove ActionMove;
	AgentActionCoverMove ActionMoveCover;
	AgentActionSprint ActionMoveSprint;
	AgentActionUseItem ActionUseItem;

	Quaternion FinalRotation = new Quaternion();
	float RotSpeed = 15;
	float RotattionDiff;

	float RotationSpeed
	{
		get { return RotSpeed; }
		set { RotSpeed = value; }
	}

	// Use this for initialization
	void Start()
	{
		Owner = GetComponent<AgentHuman>();
		RotattionDiff = 0;

		Owner.BlackBoard.ActionHandler += HandleAction;
	}

	// Update is called once per frame
	void Update()
	{
		if (Owner.IsAlive == false)
		{
			RotattionDiff = 0;
			return;
		}

		//Profiler.BeginSample ("ComponentBody.Update() : Main part");

		if (Owner.IsInCover && Owner.IsLeavingToCover == false)
		{
			FinalRotation.eulerAngles = new Vector3(0, Owner.BlackBoard.Cover.Transform.rotation.eulerAngles.y, 0);
			if (Time.deltaTime > Mathf.Epsilon)
				Owner.Transform.rotation = Quaternion.Lerp(Owner.Transform.rotation, FinalRotation, RotationSpeed*Time.deltaTime*5);

			CheckEdges();
		}
		else if (Owner.IsInKnockdown)
		{
			RotattionDiff = 0;

			if (Owner.IsProxy)
			{
				// Server changes the player rotation during knockdown, we have to follow it, not ignore it
				FinalRotation.eulerAngles = new Vector3(0, Owner.BlackBoard.Desires.Rotation.eulerAngles.y, 0);
				if (Time.deltaTime > Mathf.Epsilon)
				{
					Owner.Transform.rotation = Quaternion.Lerp(Owner.Transform.rotation, FinalRotation, RotationSpeed*Time.deltaTime);
				}
			}
		}
		else
		{
			if (ActionRotate == null && Owner.IsServer == false)
			{
				float diff = Owner.Transform.rotation.eulerAngles.y - Owner.BlackBoard.Desires.Rotation.eulerAngles.y;

				if (diff < 0.001f && diff > -0.001f)
								//clamp small numbers otherwise RotationDiff is being incremented for a long time if Desire and Owner rotation is not EXACTLY the same!
					diff = 0;

				if (diff > 180)
					diff -= 360;
				else if (diff < -180)
					diff += 360;

				if (diff == 0)
					RotattionDiff = 0;
				else
					RotattionDiff += Mathf.Abs(diff);

				if (RotattionDiff > 10)
				{
					ActionRotate = AgentActionFactory.Create(AgentActionFactory.E_Type.Rotate) as AgentActionRotate;
					ActionRotate.Rotation = diff > 0 ? E_RotationType.Left : E_RotationType.Right;
					Owner.BlackBoard.ActionAdd(ActionRotate);

					RotattionDiff = 0;
				}
			}

			FinalRotation.eulerAngles = new Vector3(0, Owner.BlackBoard.Desires.Rotation.eulerAngles.y, 0);

			//if( !Owner.IsProxy )
			if (!Owner.IsServer) // rotation on server is driven by ServerUpdate()
			{
				if (Time.deltaTime > Mathf.Epsilon)
				{
					Owner.Transform.rotation = Quaternion.Lerp(Owner.Transform.rotation, FinalRotation, RotationSpeed*Time.deltaTime);
					//Owner.Transform.rotation = Owner.BlackBoard.Desires.Rotation;
				}
			}
		}

		//Profiler.EndSample();

		//Profiler.BeginSample ("ComponentBody.Update() : UpdateAiming");
		UpdateAiming();
		//Profiler.EndSample();

		if (ActionRotate != null && ActionRotate.IsActive() == false)
			ActionRotate = null;

		HandleMovement();
	}

	protected virtual void HandleMovement()
	{
		if (ActionMove != null)
		{
			if (ActionMove.IsActive())
			{
//				Debug.Log ("ComponentBody.HandleMovement() MOVE, BlackBoard.MoveDir=" + Owner.BlackBoard.MoveDir);

				//Profiler.BeginSample ("ComponentBody.Update() : HandleMovement - Move");

				if (Move(Owner.BlackBoard.MoveDir*Owner.BlackBoard.Speed*Time.deltaTime) == false)
					ActionMove.SetFailed();

				//Profiler.EndSample();
			}

			if (ActionMove.IsActive() == false)
				ActionMove = null;
		}
		else if (ActionMoveCover != null)
		{
			if (ActionMoveCover.IsActive())
			{
				if (CoverMove() == false)
					ActionMoveCover.SetFailed();

				CheckEdges();
			}

			if (ActionMoveCover.IsActive() == false)
				ActionMoveCover = null;
		}
		else if (ActionMoveSprint != null)
		{
			if (ActionMoveSprint.IsActive())
			{
				if (Move(Owner.BlackBoard.MoveDir*Owner.BlackBoard.Speed*Time.deltaTime) == false)
					ActionMoveSprint.SetFailed();
			}

			if (ActionMoveSprint.IsActive() == false)
				ActionMoveSprint = null;
		}
		else if (ActionUseItem != null) //THROW_RUN support move while throwing grenades
		{
			if (!Owner.IsInCover && (Owner.BlackBoard.MotionType == E_MotionType.Walk || Owner.BlackBoard.MotionType == E_MotionType.Run))
			{
//				Debug.Log ("ComponentBody.HandleMovement() USE ITEM, BlackBoard.MoveDir=" + Owner.BlackBoard.MoveDir);

				if (ActionUseItem.IsActive())
					Move(Owner.BlackBoard.MoveDir*Owner.BlackBoard.Speed*Time.deltaTime);

				if (ActionUseItem.IsActive() == false)
					ActionUseItem = null;
			}
		}
	}

	void UpdateAiming()
	{
		Owner.BlackBoard.FireDir = Owner.BlackBoard.Desires.FireDirection;

		/*if (Owner.debugAnims) Debug.DrawRay(Owner.TransformEye.position, Owner.BlackBoard.Desires.FireDirection * 10, Color.white);


        Owner.BlackBoard.FireDir = Vector3.Slerp(Owner.BlackBoard.FireDir, Owner.BlackBoard.Desires.FireDirection, 20 * Time.deltaTime);

        //Owner.BlackBoard.FireDir = Owner.BlackBoard.Desires.FireDirection;
        if(Owner.debugAnims) Debug.DrawRay(Owner.TransformEye.position, Owner.BlackBoard.FireDir * 10, Color.yellow);*/
	}

	public void HandleAction(AgentAction a)
	{
		if (a is AgentActionMove)
			ActionMove = a as AgentActionMove;
		else if (a is AgentActionCoverMove)
			ActionMoveCover = a as AgentActionCoverMove;
		else if (a is AgentActionSprint)
			ActionMoveSprint = a as AgentActionSprint;
		else if (a is AgentActionUseItem) //THROW_RUN
			ActionUseItem = a as AgentActionUseItem;
	}

	protected virtual bool Move(Vector3 velocity)
	{
		if (Owner.CharacterController == null)
			return false;

//		Debug.Log ("ComponentBody.Move(), agent=" + Owner.name + ", velocity=" + velocity.ToString("F4"));

		//if (Owner.CharacterController.isGrounded == false)
		if (!Owner.IsServer || (false == Owner.CharacterController.isGrounded)) // server optimizations
		{
			velocity += Physics.gravity*Time.deltaTime;
		}

		if (velocity.sqrMagnitude > Mathf.Epsilon)
			Owner.CharacterController.Move(velocity);

		return true;
	}

	protected virtual bool CoverMove()
	{
		float distanceToEdge;

		Vector3 edgePos;

		if (ActionMoveCover.Direction == AgentActionCoverMove.E_Direction.Left)
			edgePos = Owner.BlackBoard.Cover.LeftEdge;
		else
			edgePos = Owner.BlackBoard.Cover.RightEdge;

		edgePos.y = Owner.Position.y; // check only 2d

		distanceToEdge = (edgePos - Owner.Position).magnitude;

		float moveDistance = Owner.BlackBoard.Speed*Time.deltaTime;

		//Debug.Log(distanceToEdge);
		if (distanceToEdge < moveDistance)
		{
			//Debug.Log(Time.timeSinceLevelLoad + " distance to edge " + distanceToEdge);
			Owner.Transform.position = edgePos;

			return false;
		}

		return Owner.RestrictedCoverMove(ActionMoveCover.Direction, moveDistance);
	}

	protected bool CheckEdges()
	{
		Cover cover = Owner.BlackBoard.Cover;
		Vector3 coverLeftEdge = cover.LeftEdge;
		Vector3 coverRightEdge = cover.RightEdge;

		coverLeftEdge.y = coverRightEdge.y = Owner.Position.y;

		if (Owner.Position == coverLeftEdge && cover.IsLeftAllowed)
		{
			Owner.BlackBoard.CoverPosition = E_CoverDirection.Left;
			Owner.WorldState.SetWSProperty(E_PropKey.CoverState, Owner.BlackBoard.CoverFire ? E_CoverState.Aim : E_CoverState.Edge);
			return true;
		}
		else if (Owner.Position == coverRightEdge && cover.IsRightAllowed)
		{
			Owner.BlackBoard.CoverPosition = E_CoverDirection.Right;
			Owner.WorldState.SetWSProperty(E_PropKey.CoverState, Owner.BlackBoard.CoverFire ? E_CoverState.Aim : E_CoverState.Edge);

			return true;
		}

		Owner.BlackBoard.CoverPosition = E_CoverDirection.Middle;
		Owner.WorldState.SetWSProperty(E_PropKey.CoverState, Owner.BlackBoard.CoverFire ? E_CoverState.Aim : E_CoverState.Middle);

		return false;
	}
}
