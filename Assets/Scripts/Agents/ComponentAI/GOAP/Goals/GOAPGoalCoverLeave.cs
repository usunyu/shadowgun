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

class GOAPGoalCoverLeave : GOAPGoal
{
	//   Vector3 AdvancePos;
	public GOAPGoalCoverLeave(AgentHuman owner) : base(E_GOAPGoals.CoverLeave, owner)
	{
	}

	public override float GetMaxRelevancy()
	{
		return Owner.BlackBoard.GoapSetup.CoverRelevancy;
	}

	public override void CalculateGoalRelevancy()
	{
		GoalRelevancy = 0;

		if (Owner.BlackBoard.Cover == null)
			return;

		if (Owner.BlackBoard.Desires.MoveDirection == Vector3.zero)
			return;

		float dot = Vector3.Dot(Owner.BlackBoard.Cover.Forward, Owner.BlackBoard.Desires.MoveDirection);

		if (dot > 0.75f)
		{
			// Debug.Log(Time.timeSinceLevelLoad + " " + Owner.BlackBoard.Desires.MoveDirection + " " + dot);

			if (Owner.BlackBoard.CoverPosition == E_CoverDirection.Middle)
			{
				if (Owner.BlackBoard.Cover.CanJumpOver == false && Owner.BlackBoard.Cover.CanJumpUp == false)
					return;
			}
		}
		else if (dot > -0.75f)
			return;

		// Debug.Log(Time.timeSinceLevelLoad + " " + Owner.BlackBoard.Desires.MoveDirection + " " + dot);

		GoalRelevancy = Owner.BlackBoard.GoapSetup.CoverRelevancy;

		//     AdvancePos = Owner.BlackBoard.Cover.Position - Owner.BlackBoard.Cover.Forward;
	}

	public override void SetDisableTime()
	{
		NextEvaluationTime = Owner.BlackBoard.GoapSetup.CoverDelay + Time.timeSinceLevelLoad;
	}

	public override void SetWSSatisfactionForPlanning(WorldState worldState)
	{
		worldState.SetWSProperty(E_PropKey.CoverState, E_CoverState.None);
		//worldState.SetWSProperty(E_PropKey.TargetNode, AdvancePos);
	}

	public override bool IsWSSatisfiedForPlanning(WorldState worldState)
	{
		WorldStateProp prop = worldState.GetWSProperty(E_PropKey.CoverState);

		if (prop.GetCoverState() == E_CoverState.None)
			return true;

		return false;
	}

	public override bool Activate(GOAPPlan plan)
	{
		//Owner.WorldState.SetWSProperty(E_PropKey.TargetNode, AdvancePos);
		return base.Activate(plan);
	}

	public override bool IsSatisfied()
	{
		return IsPlanFinished();
	}
}
