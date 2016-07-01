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

class GOAPActionCoverMove : GOAPAction
{
	AgentActionCoverMove Action;
	Vector3 FinalPos;

	public GOAPActionCoverMove(AgentHuman owner) : base(E_GOAPAction.CoverMove, owner)
	{
	}

	public override void InitAction()
	{
		WorldEffects.SetWSProperty(E_PropKey.AtTargetPos, true);
		Cost = 5;
		Precedence = 40;
	}

	// Validates the context preconditions
	public override bool ValidateContextPreconditions(WorldState current, bool planning)
	{
		E_CoverState coverState = Owner.WorldState.GetWSProperty(E_PropKey.CoverState).GetCoverState();

		//not in cover - failed
		if (coverState == E_CoverState.None)
			return false;

		//no move direction - failed
		if (Owner.BlackBoard.Desires.MoveDirection == Vector3.zero)
			return false;

		if (coverState == E_CoverState.Middle && Vector3.Dot(Owner.BlackBoard.Desires.MoveDirection, Owner.BlackBoard.Cover.Forward) < 0.6f)
			return true;

		if (Owner.BlackBoard.CoverPosition == E_CoverDirection.Left &&
			Vector3.Dot(Owner.BlackBoard.Desires.MoveDirection, Owner.BlackBoard.Cover.Left) < 0.4f)
			return true;

		if (Owner.BlackBoard.CoverPosition == E_CoverDirection.Right &&
			Vector3.Dot(Owner.BlackBoard.Desires.MoveDirection, Owner.BlackBoard.Cover.Right) < 0.4f)
			return true;

		return false;
	}

	public override void Update()
	{
		AgentActionCoverMove.E_Direction direction;

		if (Vector3.Dot(Owner.BlackBoard.Desires.MoveDirection, Owner.Right) > 0)
			direction = AgentActionCoverMove.E_Direction.Right;
		else
			direction = AgentActionCoverMove.E_Direction.Left;

		if (Action.Speed != Owner.BlackBoard.Desires.MoveSpeedModifier || Action.Direction != direction)
		{
			Action = AgentActionFactory.Create(AgentActionFactory.E_Type.CoverMove) as AgentActionCoverMove;
			Action.Speed = Owner.BlackBoard.Desires.MoveSpeedModifier;
			Action.Direction = direction;
			Owner.BlackBoard.ActionAdd(Action);
		}
	}

	public override void Activate()
	{
		base.Activate();

		Action = AgentActionFactory.Create(AgentActionFactory.E_Type.CoverMove) as AgentActionCoverMove;

		Action.Speed = Owner.BlackBoard.Desires.MoveSpeedModifier;

		if (Vector3.Dot(Owner.BlackBoard.Desires.MoveDirection, Owner.Right) > 0)
			Action.Direction = AgentActionCoverMove.E_Direction.Right;
		else
			Action.Direction = AgentActionCoverMove.E_Direction.Left;

		Owner.BlackBoard.ActionAdd(Action);
	}

	public override void Deactivate()
	{
		base.Deactivate();

		Owner.WorldState.SetWSProperty(E_PropKey.AtTargetPos, true);
		AgentActionIdle a = AgentActionFactory.Create(AgentActionFactory.E_Type.Idle) as AgentActionIdle;
		Owner.BlackBoard.ActionAdd(a);
	}

	public override bool IsActionComplete()
	{
		if (Action != null && Action.IsActive() == false || Owner.BlackBoard.Desires.MoveDirection == Vector3.zero ||
			Owner.WorldState.GetWSProperty(E_PropKey.AtTargetPos).GetBool() == true)
		{
			return true;
		}

		if (E_CoverState.Middle == Owner.WorldState.GetWSProperty(E_PropKey.CoverState).GetCoverState())
		{
			if (Vector3.Dot(Owner.BlackBoard.Desires.MoveDirection, Owner.BlackBoard.Cover.Forward) >= 0.6f)
			{
				return true;
			}
		}

		return false;
	}

	public override bool ValidateAction()
	{
		if (Action != null && Action.IsFailed() == true)
		{
			//UnityEngine.Debug.Log(this.ToString() + " not valid anymore !" + FinalPos.ToString());
			return false;
		}

		return true;
	}
}
