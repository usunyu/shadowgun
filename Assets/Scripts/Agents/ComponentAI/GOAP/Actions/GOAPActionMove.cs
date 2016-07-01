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

class GOAPActionMove : GOAPAction
{
	AgentActionMove ActionMove;
	Vector3 FinalPos;

	public GOAPActionMove(AgentHuman owner) : base(E_GOAPAction.Move, owner)
	{
	}

	public override void InitAction()
	{
		WorldEffects.SetWSProperty(E_PropKey.AtTargetPos, true);
		Cost = 5;
		Precedence = 30;
	}

	// Validates the context preconditions
	public override bool ValidateContextPreconditions(WorldState current, bool planning)
	{
		if (Owner.IsInCover)
			return false;

		return Owner.BlackBoard.Desires.MoveDirection != Vector3.zero;
	}

	public override void Update()
	{
		if (Owner.BlackBoard.MotionType != E_MotionType.Sprint && Owner.WorldState.GetWSProperty(E_PropKey.WeaponLoaded).GetBool() == false &&
			Owner.WeaponComponent.GetCurrentWeapon().IsBusy() == false && Owner.WeaponComponent.GetCurrentWeapon().WeaponAmmo > 0)
		{
			AgentAction a = AgentActionFactory.Create(AgentActionFactory.E_Type.Reload) as AgentActionReload;
			Owner.BlackBoard.ActionAdd(a);
		}
	}

	public override void Activate()
	{
		base.Activate();

		ActionMove = AgentActionFactory.Create(AgentActionFactory.E_Type.Move) as AgentActionMove;
		Owner.BlackBoard.ActionAdd(ActionMove);
	}

	public override void Deactivate()
	{
		base.Deactivate();

		Owner.WorldState.SetWSProperty(E_PropKey.AtTargetPos, true);
		AgentActionIdle a = AgentActionFactory.Create(AgentActionFactory.E_Type.Idle) as AgentActionIdle;
		Owner.BlackBoard.ActionAdd(a);

		ActionMove = null;
	}

	public override bool IsActionComplete()
	{
		if (ActionMove != null && ActionMove.IsActive() == false)
			return true;

		if (Owner.BlackBoard.Desires.MoveDirection == Vector3.zero || Owner.WorldState.GetWSProperty(E_PropKey.AtTargetPos).GetBool() == true)
			return true;

		return false;
	}

	public override bool ValidateAction()
	{
		if (ActionMove != null && ActionMove.IsFailed() == true)
			return false;

		return true;
	}
}
