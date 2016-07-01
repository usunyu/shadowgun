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
class GOAPGoalCoverMove : GOAPGoal
{
	public GOAPGoalCoverMove(AgentHuman owner) : base(E_GOAPGoals.CoverMove, owner)
	{
	}

	public override void InitGoal()
	{
	}

	public override float GetMaxRelevancy()
	{
		return Owner.BlackBoard.GoapSetup.GoToRelevancy;
	}

	public override void CalculateGoalRelevancy()
	{
		//AgentOrder order = Ai.BlackBoard.OrderGet();
		//if(order != null && order.Type == AgentOrder.E_OrderType.E_GOTO)

		GoalRelevancy = 0;

		if (Owner.BlackBoard.Cover == null)
			return;

		if (Owner.BlackBoard.MotionType != E_MotionType.None)
			return;

		WorldStateProp prop = Owner.WorldState.GetWSProperty(E_PropKey.AtTargetPos);
		if (prop != null && prop.GetBool() == true)
			return;

		E_CoverState coverState = Owner.WorldState.GetWSProperty(E_PropKey.CoverState).GetCoverState();
		if (coverState == E_CoverState.Middle && Vector3.Dot(Owner.BlackBoard.Desires.MoveDirection, Owner.BlackBoard.Cover.Forward) > 0.6f)
			return;

		if (Owner.BlackBoard.CoverPosition == E_CoverDirection.Left &&
			Vector3.Dot(Owner.BlackBoard.Desires.MoveDirection, Owner.BlackBoard.Cover.Left) > -0.4f)
			return;

		if (Owner.BlackBoard.CoverPosition == E_CoverDirection.Right &&
			Vector3.Dot(Owner.BlackBoard.Desires.MoveDirection, Owner.BlackBoard.Cover.Right) > -0.4f)
			return;

		GoalRelevancy = Owner.BlackBoard.GoapSetup.GoToRelevancy;
	}

	public override void SetDisableTime()
	{
		NextEvaluationTime = Owner.BlackBoard.GoapSetup.GoToDelay + Time.timeSinceLevelLoad;
	}

	public override void SetWSSatisfactionForPlanning(WorldState worldState)
	{
		worldState.SetWSProperty(E_PropKey.AtTargetPos, true);
	}

	public override bool IsWSSatisfiedForPlanning(WorldState worldState)
	{
		WorldStateProp prop = worldState.GetWSProperty(E_PropKey.AtTargetPos);

		if (prop != null && prop.GetBool() == true)
			return true;

		return false;
	}

	public override bool IsSatisfied()
	{
		WorldStateProp prop = Owner.WorldState.GetWSProperty(E_PropKey.AtTargetPos);

		if (prop != null && prop.GetBool() == true)
			return true;

		return false;
	}
}
