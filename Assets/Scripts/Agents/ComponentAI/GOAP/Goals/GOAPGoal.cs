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

/***************************************************************
 * Class Name : Goal
 * Function   : A base class for all Goals in GOAP system
 *				
 * Created by : Marek Rabas
 **************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class GOAPGoal : System.Object
{
	public AgentHuman Owner { get; private set; }
	public float GoalRelevancy { get; protected set; }
	public E_GOAPGoals GoalType { get; private set; }

	public bool Active { get; private set; }

	GOAPPlan Plan;
	protected float NextEvaluationTime { get; set; }

	static int id;
	public int UID;

	public abstract void SetWSSatisfactionForPlanning(WorldState worldState);
	public abstract bool IsWSSatisfiedForPlanning(WorldState worldState);

	public virtual void ChangeCurrentWSForPlanning(WorldState worldState)
	{
	}

	public abstract float GetMaxRelevancy();
	public abstract void CalculateGoalRelevancy(); // how important is this goal !!!

	public void ClearGoalRelevancy()
	{
		GoalRelevancy = 0;
	}

	public virtual void SetDisableTime()
	{
		NextEvaluationTime = Time.timeSinceLevelLoad + UnityEngine.Random.Range(0.1f, 0.2f);
	}

	public virtual bool ReplanRequired()
	{
		return false;
	} // if goal need to be replanned !!!!
	public abstract bool IsSatisfied();

	public bool IsDisabled()
	{
		return NextEvaluationTime >= Time.timeSinceLevelLoad;
	}

	protected GOAPGoal(E_GOAPGoals type, AgentHuman owner)
	{
		GoalType = type;
		Owner = owner;
	}

	public virtual void InitGoal()
	{
	}

	/**
    * Updates the goal. This involves getting the plan, checking if the current step(i.e. action is complete), if so advance the plan
    */

	public bool UpdateGoal()
	{
		//Check if plan exists, if not quit
		if (Plan == null)
			return false;

		Plan.Update();

		//Check if the plan step is complete, if so advance if not do nothing :)
		if (Plan.IsPlanStepComplete())
			return Plan.AdvancePlan();

		return true;
	}

	public virtual bool Activate(GOAPPlan plan)
	{
		UID = ++id;

		Active = true;
		Plan = plan;

		return Plan.Activate(Owner, this);
	}

	public virtual void ReplanReset()
	{
		Active = false;
		if (Plan != null)
			Plan.Deactivate();

		Plan = null;

		if (Owner.debugGOAP)
			Debug.Log(Time.timeSinceLevelLoad + " " + this.ToString() + " - replan Reset");
	}

	public virtual void Reset()
	{
		Active = false;
		if (Plan != null)
			Plan.Deactivate();

		Plan = null;
		ClearGoalRelevancy();

		NextEvaluationTime = 0;

		if (Owner.debugGOAP)
			Debug.Log(Time.timeSinceLevelLoad + " " + this.ToString() + " - Reset");
	}

	/* 
     * do some cleaning shit here
     */

	public virtual void Deactivate()
	{
		Active = false;
		if (Plan != null)
			Plan.Deactivate();

		Plan = null;
		ClearGoalRelevancy();
		SetDisableTime();

		if (Owner.debugGOAP)
			Debug.Log(Time.timeSinceLevelLoad + " " + this.ToString() + " - Deactivated");
		if (Owner.debugGOAP)
			Debug.Log(Owner.WorldState.ToString());
	}

	/**
    * Checks whether the plan is interruptible or not
    * @return true if the plan is interruptible, false otherwise
    */

	public virtual bool IsPlanInterruptible()
	{
		return Plan == null ? true : Plan.IsPlanStepInterruptible();
	}

	/**
    * Checks whether the plan is valid
    * @return true if the plan is valid, false otherwise
    */

	public virtual bool IsPlanValid()
	{
		return Plan == null ? false : Plan.IsPlanValid();
	}

	public bool IsPlanFinished()
	{
		return Plan == null ? true : Plan.IsDone();
	}

	//If a plan fails to be built, clear the relevancy and try again
	public virtual void HandlePlanBuildFailure()
	{
		ClearGoalRelevancy();
	}

	public override string ToString()
	{
		return base.ToString() + "(Releavancy: " + GoalRelevancy + (Active ? "Active " : " Deactive ") +
			   (IsDisabled() ? " Disabled " : " Enabled ") + ")";
	}
}
