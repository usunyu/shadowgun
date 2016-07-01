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

class GOAPActionUseGadget : GOAPAction
{
	AgentActionUseItem Action;

	public GOAPActionUseGadget(AgentHuman owner) : base(E_GOAPAction.UseGadget, owner)
	{
	}

	public override void InitAction()
	{
		WorldEffects.SetWSProperty(E_PropKey.UseGadget, false);
		Interruptible = false;

		Cost = 2;
	}

	public override void Activate()
	{
		base.Activate();

		Action = AgentActionFactory.Create(AgentActionFactory.E_Type.UseItem) as AgentActionUseItem;

		if (ShouldKeepMotion(Owner.BlackBoard.Desires.Gadget))
			Owner.BlackBoard.KeepMotion = true; //beny: do not reset Motion while throwing grenade

		Action.CoverDirection = Owner.BlackBoard.CoverPosition;
		Action.CoverPose = Owner.BlackBoard.CoverPose;

		Owner.BlackBoard.ActionAdd(Action);
		Owner.BlackBoard.BusyAction = true;
	}

	public override void Deactivate()
	{
		Owner.BlackBoard.BusyAction = false;
		Owner.BlackBoard.KeepMotion = false; //beny: do not reset Motion while throwing grenade

		Owner.WorldState.SetWSProperty(E_PropKey.UseGadget, false);
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

	bool ShouldKeepMotion(E_ItemID id)
	{
		return true;
/*		if ( id == E_ItemID.GrenadeEMP		|| 
			 id == E_ItemID.GrenadeEMPII 	|| 
			 id == E_ItemID.GrenadeFlash 	|| 
			 id == E_ItemID.GrenadeFrag 	|| 
			 id == E_ItemID.GrenadeFragII 	|| 
			 id == E_ItemID.SentryGun		|| 
			 id == E_ItemID.SentryGunII		|| 
			 id == E_ItemID.SentryGunRail	|| 
			 id == E_ItemID.SentryGunRockets|| 
			 id == E_ItemID.BoxAmmo			|| 
			 id == E_ItemID.BoxAmmoII		|| 
			 id == E_ItemID.BoxHealth		|| 
			 id == E_ItemID.BoxHealthII	
			)
		{
			return true;
		}
		
		return false;
*/
	}
}
