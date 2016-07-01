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

class GOAPActionRoll : GOAPAction
{
	AgentActionRoll Action;
	E_StrafeDirection StrafeDirection;

	public GOAPActionRoll(AgentHuman owner) : base(E_GOAPAction.Roll, owner)
	{
	}

	public override void InitAction()
	{
		WorldEffects.SetWSProperty(E_PropKey.InDodge, false);
		Cost = 5;
		Precedence = 70;
	}

	// Validates the context preconditions
	public override bool ValidateContextPreconditions(WorldState current, bool planning)
	{
		return true;
	}

	public override void Activate()
	{
		base.Activate();

		Owner.BlackBoard.Desires.MeleeTriggerOn = false;
		Owner.BlackBoard.Desires.WeaponTriggerOn = false;
		Owner.BlackBoard.Desires.WeaponTriggerUp = false;
		Owner.BlackBoard.Desires.WeaponTriggerUpDisabled = true;
		Owner.WorldState.SetWSProperty(E_PropKey.InDodge, false);

		Action = AgentActionFactory.Create(AgentActionFactory.E_Type.Roll) as AgentActionRoll;

		Action.Direction = Owner.BlackBoard.Desires.RollDirection;

		Owner.BlackBoard.ActionAdd(Action);
	}

	public override void Deactivate()
	{
		Owner.WorldState.SetWSProperty(E_PropKey.InDodge, false);
		base.Deactivate();
	}

	public override bool IsActionComplete()
	{
		if (Action != null && Action.IsActive() == false)
			return true;

		return false;
	}

	public override bool ValidateAction()
	{
		if (Action != null && Action.IsFailed() == true)
			return false;

		return Owner.IsAlive;
	}
}
