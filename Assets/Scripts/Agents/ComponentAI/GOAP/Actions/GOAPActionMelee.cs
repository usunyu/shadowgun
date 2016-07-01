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

class GOAPActionMelee : GOAPAction
{
	AgentActionMelee Action;

	public GOAPActionMelee(AgentHuman owner) : base(E_GOAPAction.Melee, owner)
	{
	}

	public override void InitAction()
	{
		WorldEffects.SetWSProperty(E_PropKey.KillTarget, true);
		Interruptible = true;

		Cost = 2;
	}

	// Validates the context preconditions
	public override bool ValidateContextPreconditions(WorldState current, bool planning)
	{
		if (Owner.BlackBoard.Desires.MeleeTriggerOn == false)
			return false;

		if (Owner.BlackBoard.Desires.MeleeTarget == null)
			return false;

		if (Owner.IsInCover)
			return false;

		return true;
	}

	public override void Activate()
	{
		base.Activate();

		Action = AgentActionFactory.Create(AgentActionFactory.E_Type.Melee) as AgentActionMelee;
		Action.Target = Owner.BlackBoard.Desires.MeleeTarget;

		SentryGun sentryGun = Action.Target as SentryGun;

		if (null != sentryGun)
		{
			// leg attack
			Action.MeleeType = E_MeleeType.First;
		}
		else
		{
			Action.MeleeType = RandomEnum<E_MeleeType>();
		}

		Owner.BlackBoard.ActionAdd(Action);
	}

	public override void Deactivate()
	{
		Owner.WorldState.SetWSProperty(E_PropKey.KillTarget, false);
		base.Deactivate();
	}

	public override bool IsActionComplete()
	{
		if (Action.IsActive() == false)
			return true;

		return false;
	}

	public override bool ValidateAction()
	{
		if (Action != null && Action.IsFailed() == true)
			return false;

		return Owner.IsAlive;
	}

	public T RandomEnum<T>()
	{
		T[] values = (T[])Enum.GetValues(typeof (T));
		return values[UnityEngine.Random.Range(0, values.Length)];
	}
}
