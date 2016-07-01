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

class GOAPGoalCoverJumpOver : GOAPGoal
{
	Vector3 AdvancePos;

	public GOAPGoalCoverJumpOver(AgentHuman owner) : base(E_GOAPGoals.CoverJumpOver, owner)
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
		{
			Debug.Log(Time.timeSinceLevelLoad + " no cover ");
			return;
		}

		if (Owner.BlackBoard.CoverPosition != E_CoverDirection.Middle)
		{
			Debug.Log(Time.timeSinceLevelLoad + " no middle ");
			return;
		}

		if (Owner.BlackBoard.Cover.CoverFlags.Get(Cover.E_CoverFlags.UpCrouch) == false)
		{
			Debug.Log(Time.timeSinceLevelLoad + " no crouch ");
			return;
		}

		if (Vector3.Dot(Owner.BlackBoard.Cover.Forward, Owner.BlackBoard.Desires.MoveDirection) < 0.75f)
		{
			Debug.Log(Time.timeSinceLevelLoad + " bad dot ");
			return;
		}

		if (Owner.BlackBoard.Cover.CanJumpOver == false && Owner.BlackBoard.Cover.CanJumpUp == false)
		{
			Debug.Log(Time.timeSinceLevelLoad + "no opposite cover ");
			return;
		}

		AdvancePos = Owner.BlackBoard.Cover.OppositeCover.Position - Owner.BlackBoard.Cover.OppositeCover.Forward;

		GoalRelevancy = Owner.BlackBoard.GoapSetup.CoverRelevancy;

		Debug.Log("relevancy " + GoalRelevancy);
	}

	public override void SetDisableTime()
	{
		NextEvaluationTime = Owner.BlackBoard.GoapSetup.CoverDelay + Time.timeSinceLevelLoad;
	}

	public override void SetWSSatisfactionForPlanning(WorldState worldState)
	{
		worldState.SetWSProperty(E_PropKey.CoverState, E_CoverState.None);
		worldState.SetWSProperty(E_PropKey.TargetNode, AdvancePos);
	}

	public override bool IsWSSatisfiedForPlanning(WorldState worldState)
	{
		WorldStateProp prop = worldState.GetWSProperty(E_PropKey.CoverState);

		if (prop.GetCoverState() == E_CoverState.None)
			return true;

		return false;
	}

	public override bool IsSatisfied()
	{
		return IsPlanFinished();
	}

	public override bool Activate(GOAPPlan plan)
	{
		Owner.WorldState.SetWSProperty(E_PropKey.TargetNode, AdvancePos);
		return base.Activate(plan);
	}
}
