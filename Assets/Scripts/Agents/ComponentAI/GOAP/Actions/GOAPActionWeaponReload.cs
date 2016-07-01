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

class GOAPActionWeaponReload : GOAPAction
{
	AgentActionReload Action;

	public GOAPActionWeaponReload(AgentHuman owner) : base(E_GOAPAction.WeaponReload, owner)
	{
	}

	public override void InitAction()
	{
		WorldEffects.SetWSProperty(E_PropKey.WeaponLoaded, true);

		Interruptible = false;

		Cost = 1;
		Precedence = 10;
	}

	// Validates the context preconditions
	public override bool ValidateContextPreconditions(WorldState current, bool planning)
	{
		if (planning == false)
			return true;

		if (Owner.WeaponComponent.GetCurrentWeapon().IsBusy())
			return false;

		if (Owner.WeaponComponent.GetCurrentWeapon().WeaponAmmo == 0)
			return false;

		return true;
	}

	public override void Activate()
	{
		base.Activate();

		Action = AgentActionFactory.Create(AgentActionFactory.E_Type.Reload) as AgentActionReload;
		Owner.BlackBoard.ActionAdd(Action);
	}

	public override void Deactivate()
	{
		base.Deactivate();
		if (Action.IsSuccess() && Owner.WeaponComponent.GetCurrentWeapon().ClipAmmo > 0)
		{
			Owner.WorldState.SetWSProperty(E_PropKey.WeaponLoaded, true);
		}
	}

	public override bool IsActionComplete()
	{
		if (Action.IsActive() == false)
			return true;

		return false;
	}
}
