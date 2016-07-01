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
using System;

class GOAPActionCoverEnter : GOAPAction
{
	AgentActionCoverEnter Action;

	public GOAPActionCoverEnter(AgentHuman owner) : base(E_GOAPAction.CoverEnter, owner)
	{
	}

	public override void InitAction()
	{
		WorldEffects.SetWSProperty(E_PropKey.CoverState, E_CoverState.Middle);
		WorldPreconditions.SetWSProperty(E_PropKey.AtTargetPos, true);

		Interruptible = false;

		Cost = 1;
		Precedence = 60;
	}

	// Validates the context preconditions
	public override bool ValidateContextPreconditions(WorldState current, bool planning)
	{
		if (planning == false)
			return true;

		return true;
	}

	public override void Activate()
	{
		base.Activate();

		if (Owner.BlackBoard.Desires.CoverSelected)
		{
			Action = Owner.CoverStart(Owner.BlackBoard.Desires.CoverSelected, Owner.BlackBoard.Desires.CoverPosition);
		}
	}

	public override bool ValidateAction()
	{
		return Owner.BlackBoard.Desires.CoverSelected != null;
	}

	public override void Deactivate()
	{
		base.Deactivate();
		if (Action != null && Action.IsSuccess())
		{
		}
		else
		{
			Owner.WorldState.SetWSProperty(E_PropKey.CoverState, E_CoverState.None);
			Owner.BlackBoard.Cover = null;
		}

		Owner.BlackBoard.Desires.CoverSelected = null;
	}

	public override bool IsActionComplete()
	{
		if (Action.IsActive() == false)
			return true;

		return false;
	}
}
