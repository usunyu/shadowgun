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

class GOAPActionUse : GOAPAction
{
	AgentActionUse Action;

	public GOAPActionUse(AgentHuman owner) : base(E_GOAPAction.Use, owner)
	{
	}

	public override void InitAction()
	{
		WorldPreconditions.SetWSProperty(E_PropKey.AtTargetPos, true);
		//WorldPreconditions.SetWSProperty(E_PropKey.WeaponInHands, false);
		WorldEffects.SetWSProperty(E_PropKey.UseWorldObject, false);

		Interruptible = false;

		Cost = 2;
	}

	public override void Activate()
	{
		base.Activate();

		Action = AgentActionFactory.Create(AgentActionFactory.E_Type.Use) as AgentActionUse;
		Action.InterObj = Owner.BlackBoard.Desires.InteractionObject;

		Owner.BlackBoard.InteractionObject = Owner.BlackBoard.Desires.InteractionObject;
		Owner.BlackBoard.Desires.InteractionObject = null;

		Owner.BlackBoard.ActionAdd(Action);
		Owner.BlackBoard.Desires.WeaponTriggerOn = false;
		Owner.BlackBoard.Desires.WeaponTriggerUp = false;
		Owner.BlackBoard.Desires.WeaponTriggerUpDisabled = true;
		Owner.BlackBoard.Desires.MeleeTriggerOn = false;
	}

	public override void Deactivate()
	{
		Owner.BlackBoard.InteractionObject = null;

		Owner.WorldState.SetWSProperty(E_PropKey.UseWorldObject, false);
		Owner.WorldState.SetWSProperty(E_PropKey.AtTargetPos, true);

		base.Deactivate();
	}

	public override bool IsActionComplete()
	{
		if (Action.IsActive() == false)
			return true;

		return false;
	}
}
