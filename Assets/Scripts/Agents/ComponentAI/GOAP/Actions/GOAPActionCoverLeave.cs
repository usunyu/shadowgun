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

class GOAPActionCoverLeave : GOAPAction
{
	AgentActionCoverLeave Action;
	Vector3 FinalPos;

	public GOAPActionCoverLeave(AgentHuman owner) : base(E_GOAPAction.CoverLeave, owner)
	{
	}

	public override void InitAction()
	{
		WorldEffects.SetWSProperty(E_PropKey.CoverState, E_CoverState.None);
		Cost = 1;
		Precedence = 60;
		Interruptible = false;
	}

	// Validates the context preconditions
	public override bool ValidateContextPreconditions(WorldState current, bool planning)
	{
		return true;
	}

	public override void SetPlanWSPreconditions(WorldState goalState)
	{
		base.SetPlanWSPreconditions(goalState);

		if (Owner.WeaponComponent.GetCurrentWeapon().WeaponAmmo > 0)
			goalState.SetWSProperty(E_PropKey.WeaponLoaded, true);
	}

	public override void Activate()
	{
		base.Activate();

		Action = AgentActionFactory.Create(AgentActionFactory.E_Type.CoverLeave) as AgentActionCoverLeave;
		Action.FinalViewDirection = Owner.BlackBoard.Cover.Forward;
		Action.Cover = Owner.BlackBoard.Cover;

		Owner.BlackBoard.ActionAdd(Action);
	}

	public override void Deactivate()
	{
		base.Deactivate();
	}

	public override bool IsActionComplete()
	{
		return !Action.IsActive();
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
