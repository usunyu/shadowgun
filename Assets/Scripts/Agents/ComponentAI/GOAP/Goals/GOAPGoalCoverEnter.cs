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

/******************************************************************
 * GOAL STATS
 * 
 * For initialize : 
 *                  E_PropKey.E_IDLING              true
 * For planing : 
 *                  E_PropKey.E_IDLING              false
 * Set : 
 *                  
 * Finished:
 *                  When action is done
 * 
 * ***************************************************************/

class GOAPGoalCoverEnter : GOAPGoal
{
	public GOAPGoalCoverEnter(AgentHuman owner) : base(E_GOAPGoals.CoverEnter, owner)
	{
	}

	public override float GetMaxRelevancy()
	{
		return Owner.BlackBoard.GoapSetup.CoverRelevancy;
	}

	bool Hack_DisableGoalForOneMoreTick = false;

	public override void CalculateGoalRelevancy()
	{
		GoalRelevancy = 0;

		if (Owner.BlackBoard.MotionType != E_MotionType.None)
			return;

		// Do not enter the cover once player is either aiming or shooting
		//if (Owner.BlackBoard.Desires.WeaponTriggerOn || Owner.BlackBoard.Desires.WeaponTriggerUp)
		if (Owner.BlackBoard.Desires.WeaponTriggerOn)
		{
			Hack_DisableGoalForOneMoreTick = true;
			return;
		}
		else
		{
			//FIXME: This nasty hack is required in a special situation when we need to postpone entering into for one extra tick.
			// This time it solves the problem when player gets close to a cover while aiming (with OSG or Shitstorm weapon)
			// and releases the fire button to shoot. Without the hack the player would enter into the cover and thus
			// there would be no shot fired. (becasue the fire is processed later in the same tick)
			if (Hack_DisableGoalForOneMoreTick)
			{
				Hack_DisableGoalForOneMoreTick = false;
				return;
			}
		}

		WorldStateProp prop = Owner.WorldState.GetWSProperty(E_PropKey.CoverState);

		if (prop == null || prop.GetCoverState() != E_CoverState.None || Owner.BlackBoard.Desires.CoverNear.Cover == null)
			return;

		// no cover during reloading - BUG #283 - Missing animation of reloading when player is sprinting against cover
		WeaponBase Weapon = Owner.WeaponComponent.GetCurrentWeapon();

		if (null != Weapon)
		{
			if (Weapon.IsBusy())
			{
				return;
			}
		}

		GoalRelevancy = Owner.BlackBoard.GoapSetup.CoverRelevancy;
		Owner.BlackBoard.Desires.CoverSelected = Owner.BlackBoard.Desires.CoverNear.Cover;
	}

	public override void SetDisableTime()
	{
		NextEvaluationTime = Owner.BlackBoard.GoapSetup.CoverDelay + Time.timeSinceLevelLoad;
	}

	public override void SetWSSatisfactionForPlanning(WorldState worldState)
	{
		worldState.SetWSProperty(E_PropKey.CoverState, E_CoverState.Middle);
		worldState.SetWSProperty(Owner.WorldState.GetWSProperty(E_PropKey.TargetNode));
	}

	public override bool IsWSSatisfiedForPlanning(WorldState worldState)
	{
		WorldStateProp prop = worldState.GetWSProperty(E_PropKey.CoverState);

		if (prop.GetCoverState() != E_CoverState.None)
			return true;

		return false;
	}

	public override bool IsSatisfied()
	{
		return IsPlanFinished();
	}

	public override bool Activate(GOAPPlan plan)
	{
		// FIXME: Is that logic correct? The following line locks all the available positions in the cover but few moments later
		// the GOAPActionCoverEnter::Activate locks the BlackBoard.Desires.CoverPosition...
		// Why is this lock required? To disallow two defferent AIs to plan to go for specific cover?

		// disabled for testing
		//Owner.BlackBoard.Desires.CoverSelected.OccupyPosition(E_CoverDirection.Unknown, Owner);

		Cover c = Owner.BlackBoard.Desires.CoverSelected;
		Vector3 posOnCover = c.GetNearestPointOnCover(Owner.Position);

		if (c.IsRightAllowed && Vector3.Magnitude(posOnCover - c.RightEdge) < 0.3f)
		{
			Owner.BlackBoard.Desires.CoverPosition = E_CoverDirection.Right;
		}
		else if (c.IsLeftAllowed && Vector3.Magnitude(posOnCover - c.LeftEdge) < 0.3f)
		{
			Owner.BlackBoard.Desires.CoverPosition = E_CoverDirection.Left;
		}
		else
			Owner.BlackBoard.Desires.CoverPosition = E_CoverDirection.Unknown;

		return base.Activate(plan);
	}

	public override void Deactivate()
	{
		base.Deactivate();

		if (Owner.WorldState.GetWSProperty(E_PropKey.CoverState).GetCoverState() == E_CoverState.None) // failed
			Owner.CoverStop();
	}
}
