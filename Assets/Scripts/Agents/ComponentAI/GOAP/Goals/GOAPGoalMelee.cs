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

//E_PropKey.E_AT_TARGET_POS
class GOAPGoalMelee : GOAPGoal
{
	float WorldStateTime;

	public GOAPGoalMelee(AgentHuman owner) : base(E_GOAPGoals.Melee, owner)
	{
	}

	public override void InitGoal()
	{
	}

	public override float GetMaxRelevancy()
	{
		return Owner.BlackBoard.GoapSetup.MeleeRelevancy;
	}

	public override void CalculateGoalRelevancy()
	{
		GoalRelevancy = 0;

		if (Owner.IsInCover || Owner.BlackBoard.Desires.MeleeTarget == null)
			return;

		if (Owner.BlackBoard.Desires.MeleeTriggerOn == false)
		{
			//Debug.Log(Time.timeSinceLevelLoad + "goal trigger false");
			return; // musi mit zmacknuty trigger
		}

		GoalRelevancy = Owner.BlackBoard.GoapSetup.MeleeRelevancy;
	}

	public override void SetDisableTime()
	{
		NextEvaluationTime = Owner.BlackBoard.GoapSetup.MeleeDelay + Time.timeSinceLevelLoad;
	}

	public override void SetWSSatisfactionForPlanning(WorldState worldState)
	{
		worldState.SetWSProperty(E_PropKey.KillTarget, true);
	}

	public override bool IsWSSatisfiedForPlanning(WorldState worldState)
	{
		return worldState.GetWSProperty(E_PropKey.KillTarget).GetBool() == true;
	}

	public override bool IsSatisfied()
	{
		return IsPlanFinished();
	}
}
