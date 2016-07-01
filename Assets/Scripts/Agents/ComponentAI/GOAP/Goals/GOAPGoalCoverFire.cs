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

class GOAPGoalCoverFire : GOAPGoal
{
	public GOAPGoalCoverFire(AgentHuman owner) : base(E_GOAPGoals.CoverFire, owner)
	{
	}

	public override float GetMaxRelevancy()
	{
		return Owner.BlackBoard.GoapSetup.KillTargetRelevancy;
	}

	public override void CalculateGoalRelevancy()
	{
		GoalRelevancy = 0;

		if (Owner.CanFire() == false)
		{
			//Debug.Log(Time.timeSinceLevelLoad + "goal can NOT fire");
			return;
		}

		if (Owner.BlackBoard.Desires.WeaponTriggerOn == false)
		{
			//xxxx
			//Debug.Log(Time.timeSinceLevelLoad + "goal trigger false");
			return; // musi mit zmacknuty trigger
		}

		WorldStateProp prop = Owner.WorldState.GetWSProperty(E_PropKey.CoverState);

		if (prop.GetCoverState() == E_CoverState.Middle)
		{
			if (Owner.BlackBoard.Cover.CoverFlags.Get(Cover.E_CoverFlags.UpCrouch) == false)
			{
				// Debug.Log(Time.timeSinceLevelLoad + "goal bad cover");
				return; //je uprostred ale nemuze strilet pres cover
			}
		}
		else if (prop.GetCoverState() != E_CoverState.Edge)
		{
			//Debug.Log(Time.timeSinceLevelLoad + "goal not possible");
			return; // neni na kraji
		}

		GoalRelevancy = Owner.BlackBoard.GoapSetup.KillTargetRelevancy;

		//Debug.Log(Time.timeSinceLevelLoad + "goal ok " + GoalRelevancy);
	}

	public override void SetDisableTime()
	{
		NextEvaluationTime = Time.timeSinceLevelLoad + 0.001f;
	}

	public override void SetWSSatisfactionForPlanning(WorldState worldState)
	{
		worldState.SetWSProperty(E_PropKey.KillTarget, true);
	}

	public override bool IsWSSatisfiedForPlanning(WorldState worldState)
	{
		WorldStateProp prop = worldState.GetWSProperty(E_PropKey.KillTarget);

		if (prop.GetBool() == true)
			return true;

		return false;
	}

	public override bool IsSatisfied()
	{
		return IsPlanFinished();
	}
}
