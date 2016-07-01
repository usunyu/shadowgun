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

class GOAPActionGoTo : GOAPAction
{
	AgentActionGoTo Action;
	Vector3 Position;

	public GOAPActionGoTo(AgentHuman owner) : base(E_GOAPAction.Goto, owner)
	{
	}

	public override void InitAction()
	{
		WorldEffects.SetWSProperty(E_PropKey.AtTargetPos, true);
		Cost = 5;
		Precedence = 20;
	}

	public override void SolvePlanWSVariable(WorldState currentState, WorldState goalState)
	{
		base.SolvePlanWSVariable(currentState, goalState);

		WorldStateProp prop = goalState.GetWSProperty(E_PropKey.TargetNode);
		if (prop != null)
		{
			currentState.SetWSProperty(prop);
		}
	}

	public override void SetPlanWSPreconditions(WorldState goalState)
	{
		base.SetPlanWSPreconditions(goalState);
		if (Owner.WorldState.GetWSProperty(E_PropKey.CoverState).GetCoverState() != E_CoverState.None)
			goalState.SetWSProperty(E_PropKey.CoverState, E_CoverState.None);
	}

	// Validates the context preconditions
	public override bool ValidateContextPreconditions(WorldState current, bool planning)
	{
		if (planning == false)
			return true;

		WorldStateProp prop = current.GetWSProperty(E_PropKey.TargetNode);

		if (prop == null) // nowhere to go !!
		{
			return false;
		}

		Position = prop.GetVector();

		return true;
	}

	public override void Activate()
	{
		base.Activate();

		Action = AgentActionFactory.Create(AgentActionFactory.E_Type.Goto) as AgentActionGoTo;
		Action.MoveType = E_MoveType.Forward;
		Action.Motion = E_MotionType.Run;
		Action.FinalPosition = Position;

		Action.UseNavMeshAgentRotation = true;

		Owner.BlackBoard.ActionAdd(Action);
	}

	public override void Deactivate()
	{
		Owner.WorldState.SetWSProperty(E_PropKey.AtTargetPos, true);

		if (Action != null && Action.IsActive())
		{
			AgentActionIdle a = AgentActionFactory.Create(AgentActionFactory.E_Type.Idle) as AgentActionIdle;
			Owner.BlackBoard.ActionAdd(a);
		}

		base.Deactivate();
	}

	public override bool IsActionComplete()
	{
		if (Action != null && Action.IsSuccess())
			return true;

		return false;
	}

	public override bool ValidateAction()
	{
		if (Action != null && Action.IsFailed())
		{
			//Debug.Log("FAILED action failed");
			return false;
		}

		return true;
	}
}
