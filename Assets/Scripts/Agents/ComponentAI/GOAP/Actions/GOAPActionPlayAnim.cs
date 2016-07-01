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

class GOAPActionPlayAnim : GOAPAction
{
	AgentActionPlayAnim Action;

	public GOAPActionPlayAnim(AgentHuman owner) : base(E_GOAPAction.PlayAnim, owner)
	{
	}

	public override void InitAction()
	{
		WorldEffects.SetWSProperty(E_PropKey.PlayAnim, false);
		Cost = 1;
		Interruptible = true;
	}

	public override void Activate()
	{
		base.Activate();

		Action = AgentActionFactory.Create(AgentActionFactory.E_Type.PlayAnim) as AgentActionPlayAnim;
		Action.AnimName = Owner.BlackBoard.Desires.Animation;
		Owner.BlackBoard.ActionAdd(Action);
	}

	public override void Deactivate()
	{
		Owner.WorldState.SetWSProperty(E_PropKey.PlayAnim, false);

		AgentActionIdle a = AgentActionFactory.Create(AgentActionFactory.E_Type.Idle) as AgentActionIdle;
		Owner.BlackBoard.ActionAdd(a);

		base.Deactivate();
	}

	public override bool IsActionComplete()
	{
		if (Action.IsActive() == false)
			return true;

		return false;
	}
}
