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
 * Class Name : GOAPPlan
 * Function   : Represents a GOAP plan
 *				
 * Created by : Marek Rabas
 *
 **************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

public class GOAPPlan : System.Object
{
	List<GOAPAction> m_Actions = new List<GOAPAction>();
	int CurrentStep;

	public int NumberOfSteps
	{
		get { return m_Actions.Count; }
	}

	public int CurrentStepIndex
	{
		get { return CurrentStep; }
	}

	public void PushBack(GOAPAction action)
	{
		m_Actions.Add(action);
	}

	public GOAPAction CurrentAction
	{
		get
		{
			if (IsDone())
				return null;
			return m_Actions[CurrentStep];
		}
	}

	public void Update()
	{
		if (IsDone())
			return;

		m_Actions[CurrentStep].Update();
	}

	public bool IsPlanStepComplete()
	{
		if (IsDone())
			return true;

		return m_Actions[CurrentStep].IsActionComplete();
	}

	public bool IsDone()
	{
		return CurrentStep < m_Actions.Count == false;
	}

	/**
	* Tests whether the current step is interruptible or not
	* @return true if the current step can be interrupted, false otherwise
	*/

	public bool IsPlanStepInterruptible()
	{
		if (IsDone())
			return false;

		return m_Actions[CurrentStep].Interruptible;
	}

	/**
	* Checks whether the plan is valid
	* @return true if the plan is valid, false otherwise
	*/

	public bool IsPlanValid()
	{
		if (IsDone())
			return false;

		return CurrentAction.ValidateAction();
	}

	/*
	* Activate the GOAP plan
	*/

	public bool Activate(AgentHuman ai, GOAPGoal goal)
	{
		/*if(ai.debugGOAP)
        {
            string s = this.ToString() + " - Activated for " + goal.ToString() + " do actions:"; 
            for (int i = 0 ; i < m_Actions.Count ; i++)
                s += " " + m_Actions[i].ToString();

            Debug.Log(Time.timeSinceLevelLoad + " " + s);
		}*/

		if (m_Actions.Count == 0)
			return false;

		//Get the first action
		CurrentStep = 0;

		//For the first action, first check if context preconditions are satisfied.
		GOAPAction a = CurrentAction;
		if (a != null)
		{
			if (a.ValidateContextPreconditions(ai.WorldState, false) == false)
			{
//Are the context preconditions validated????
				//if (Owner.debugGOAP) Debug.Log(Time.timeSinceLevelLoad + " " + this.ToString() + " - " + a.ToString() + " ValidateContextPreconditions failed !!");
				return false;
			}

			a.Activate();
//			if(a.IsActionComplete())
//				AdvancePlan();
		}

		return true;
	}

	public void Deactivate()
	{
//		System.Diagnostics.Debug.WriteLine(this.ToString() + " - Deactivated");
		if (CurrentAction != null)
			CurrentAction.Deactivate();

		m_Actions.Clear();
		CurrentStep = 0;
	}

	public bool AdvancePlan()
	{
		while (IsDone() == false)
		{
			//if (Owner.debugGOAP) Debug.Log(Time.timeSinceLevelLoad + " " + this.ToString() + " Advancing plan (old action - " + CurrentAction.ToString() + " )");

			CurrentAction.Deactivate(); //deactivate current action

			CurrentStep++; //advance

			if (IsDone()) // no more action
				return true;

			//Validate the context preconditions
			if (CurrentAction.ValidateContextPreconditions(null, false) == false)
			{
				//if (Owner.debugGOAP) Debug.Log(Time.timeSinceLevelLoad + " " + this.ToString() + " - " + CurrentAction.ToString() + " ValidateContextPreconditions failed !!");
				return false;
			}

			CurrentAction.Activate();

			//Action is immediately complete,advance plan
//			if (CurrentAction.IsActionComplete())
//				continue;

			//Action isn't complete so huraay
			return true;
		}
		return true; // no more actions
	}

	public override string ToString()
	{
		string s = "GOAPPlan : ";

		for (int i = 0; i < m_Actions.Count; i++)
		{
			s += (i + 1) + ". " + m_Actions[i].Type.ToString() + " ";
		}

		return s;
	}
}
