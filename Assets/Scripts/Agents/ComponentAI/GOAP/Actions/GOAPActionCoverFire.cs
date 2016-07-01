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

class GOAPActionCoverFire : GOAPAction
{
	AgentActionCoverFire Action;
	AgentActionCoverFireCancel ActionCancel;

	public GOAPActionCoverFire(AgentHuman owner) : base(E_GOAPAction.CoverFire, owner)
	{
	}

	public override void InitAction()
	{
		WorldEffects.SetWSProperty(E_PropKey.KillTarget, true);
		WorldPreconditions.SetWSProperty(E_PropKey.WeaponLoaded, true);
		Cost = 5;
		Precedence = 30;
	}

	// Validates the context preconditions
	public override bool ValidateContextPreconditions(WorldState current, bool planning)
	{
		if (Owner.BlackBoard.Desires.WeaponTriggerOn == false)
			return false;
		//xxxx
		if (Owner.WeaponComponent.GetCurrentWeapon().ClipAmmo == 0)
			return false;

		return Owner.WorldState.GetWSProperty(E_PropKey.CoverState).GetCoverState() != E_CoverState.None;
	}

	public override void Update()
	{
		WeaponBase weapon = Owner.WeaponComponent.GetCurrentWeapon();
		if ((Owner.WorldState.GetWSProperty(E_PropKey.WeaponLoaded).GetBool() == false ||
			 !Owner.BlackBoard.Desires.WeaponTriggerOn && (!Owner.BlackBoard.Desires.WeaponTriggerUp || !weapon.UseFireUp)) &&
			ActionCancel == null && weapon.IsBusy() == false)
		{
			//xxxx
			Owner.BlackBoard.Desires.WeaponTriggerOn = false;
			ActionCancel = AgentActionFactory.Create(AgentActionFactory.E_Type.CoverFireCancel) as AgentActionCoverFireCancel;
			Owner.BlackBoard.ActionAdd(ActionCancel);
		}
	}

	public override void Activate()
	{
		base.Activate();

		Action = AgentActionFactory.Create(AgentActionFactory.E_Type.CoverFire) as AgentActionCoverFire;
		Action.CoverDirection = Owner.BlackBoard.CoverPosition;
		Action.CoverPose = Owner.BlackBoard.CoverPose;

		ActionCancel = null;

		Owner.BlackBoard.ActionAdd(Action);
	}

	public override void Deactivate()
	{
		base.Deactivate();

		Owner.WorldState.SetWSProperty(E_PropKey.KillTarget, false);
	}

	public override bool IsActionComplete()
	{
		if (Action != null && Action.IsActive() == false && ActionCancel != null && ActionCancel.IsActive() == false)
		{
			return true;
		}

		return false;
	}

	public override bool ValidateAction()
	{
		if (Action != null && Action.IsFailed() == true)
		{
			return false;
		}

		return true;
	}
}
