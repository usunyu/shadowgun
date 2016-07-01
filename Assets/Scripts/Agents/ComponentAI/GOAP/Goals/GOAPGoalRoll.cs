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
class GOAPGoalRoll : GOAPGoal
{
	float WorldStateTime;

	public GOAPGoalRoll(AgentHuman owner) : base(E_GOAPGoals.Roll, owner)
	{
	}

	public override void InitGoal()
	{
	}

	public override float GetMaxRelevancy()
	{
		return Owner.BlackBoard.GoapSetup.DodgeRelevancy;
	}

	public override void CalculateGoalRelevancy()
	{
		GoalRelevancy = 0;

		if (Owner.WorldState.GetWSProperty(E_PropKey.InDodge).GetBool() == false)
			return;

		if (Owner.IsInCover)
			return;

		GoalRelevancy = Owner.BlackBoard.GoapSetup.DodgeRelevancy;
	}

	public override void SetDisableTime()
	{
		NextEvaluationTime = Owner.BlackBoard.GoapSetup.DodgeDelay + Time.timeSinceLevelLoad;
	}

	public override void SetWSSatisfactionForPlanning(WorldState worldState)
	{
		worldState.SetWSProperty(E_PropKey.InDodge, false);
	}

	public override bool IsWSSatisfiedForPlanning(WorldState worldState)
	{
		return worldState.GetWSProperty(E_PropKey.InDodge).GetBool() == false;
	}

	public override bool IsSatisfied()
	{
		return IsPlanFinished();
	}
}
