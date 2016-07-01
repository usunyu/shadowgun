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
 * Class Name : GoalManager
 * Function   : Manages the goals for an agent.Calculates their relevancy
 *				and decides when to build/rebuild plans.Also steps through them.
 *				Also responsibile for  building plans. Run AStar and create plan from nodes
 *				
 * Created by : Marek R.
 **************************************************************/

using System.Collections.Generic;
using UnityEngine;

class GOAPManager : System.Object
{
	Dictionary<E_GOAPGoals, GOAPGoal> Goals = new Dictionary<E_GOAPGoals, GOAPGoal>();

	public GOAPGoal CurrentGoal = null; // { get { return CurrentGoal; } private set { CurrentGoal = value; } }
	AgentHuman Owner;

	AStarEngine AStar;
	AStarStorage Storage;
	AStarGOAPMap Map;
	AStarGOAPGoal Goal;

	public GOAPManager(AgentHuman ai)
	{
		Owner = ai;
		Map = new AStarGOAPMap(); //Initialise the AStar Planner
		//Map.Initialise(Owner);
		//Map.BuildActionsEffectsTable();//Build the action effects table 

		Storage = new AStarStorage();

		Goal = new AStarGOAPGoal();

		AStar = new AStarEngine();

		//AStar.Setup(Goal,Storage,Map);
	}

	public void Initialize()
	{
		Map.Initialise(Owner);
		Map.BuildActionsEffectsTable(); //Build the action effects table 
		AStar.Setup(Goal, Storage, Map);
	}

	/**
 * Reset the goal manager after a run
 */

	public void Reset()
	{
		if (CurrentGoal != null)
		{
			CurrentGoal.Deactivate();
			CurrentGoal = null;
		}

		foreach (KeyValuePair<E_GOAPGoals, GOAPGoal> pair in Goals)
			pair.Value.Reset();
	}

	public void Clean()
	{
		AStar.Cleanup();
		AStar = null;

		Map = null;

		Storage = null;

		Goal = null;
	}

	/**
	* Adds the goal to the list of goals
	* @param the new goal
	*/

	public GOAPGoal AddGoal(E_GOAPGoals type)
	{
		if (Goals.ContainsKey(type) == false)
		{
			GOAPGoal goal = GOAPGoalFactory.Create(type, Owner);
			Goals.Add(type, goal);
			return goal;
		}
		return null;
	}

	public GOAPGoal GetGoal(E_GOAPGoals type)
	{
		if (Goals.ContainsKey(type))
			return Goals[type];

		return null;
	}

	/**
 * Updates the current goal
 */

	public void UpdateCurrentGoal()
	{
		if (CurrentGoal != null)
		{
			if (CurrentGoal.UpdateGoal())
			{
				if (CurrentGoal.ReplanRequired())
				{
					if (Owner.debugGOAP)
						Debug.Log(Time.timeSinceLevelLoad + " " + CurrentGoal.ToString() + " - REPLAN required !!");
					ReplanCurrentGoal();
				}

				if (CurrentGoal.IsPlanFinished())
				{
// goal is finished, so clear it and make new one

					if (Owner.debugGOAP)
						Debug.Log(Time.timeSinceLevelLoad + " " + CurrentGoal.ToString() + " - FINISHED");
					CurrentGoal.Deactivate();
					CurrentGoal = null;
				}
			}
			else // something bad happened, clear it
			{
				CurrentGoal.Deactivate();
				CurrentGoal = null;
			}
		}
	}

	public void ManageGoals()
	{
		//First check if the current plan is invalid.
		//Then check if the current plan has validated the WS conditions
		//If this is so, then we need to find a new relevant goal and set that to the current goal
		if (CurrentGoal != null)
		{
			if (CurrentGoal.ReplanRequired())
			{
				if (Owner.debugGOAP)
					Debug.Log(Time.timeSinceLevelLoad + " " + CurrentGoal.ToString() + " - REPLAN required !!", Owner);

				if (ReplanCurrentGoal() == false)
				{
					if (Owner.debugGOAP)
						Debug.Log(Time.timeSinceLevelLoad + " " + CurrentGoal.ToString() + " - REPLAN failed, find new goal", Owner);

					FindNewGoal();
				}
			}
			else if (!CurrentGoal.IsPlanValid())
			{
				//Current goal's plan is invalid, replan and update goals flags set

				if (Owner.debugGOAP)
					Debug.Log(Time.timeSinceLevelLoad + " " + CurrentGoal.ToString() + " - INVALID, find new goal", Owner);

				FindNewGoal();
			}
			else if (CurrentGoal.IsSatisfied())
			{
//	Current goal's goal WS has been satisfied, replan and update goals flags set
				if (Owner.debugGOAP)
					Debug.Log(Time.timeSinceLevelLoad + " " + CurrentGoal.ToString() + " - DONE, find new goal", Owner);

				FindNewGoal();
			}
			else if (CurrentGoal.IsPlanInterruptible())
			{
				FindMostImportantGoal();
			}
		}
		else
		{
			FindNewGoal();
		}
	}

	bool ReplanCurrentGoal()
	{
		if (CurrentGoal == null)
			return false;

		CurrentGoal.ReplanReset();

		GOAPPlan plan = BuildPlan(CurrentGoal);

		if (plan == null)
		{
			//if (Owner.debugGOAP) Debug.Log(Time.timeSinceLevelLoad + " " + CurrentGoal.ToString() + " - REPLAN failed", Owner);
			return false;
		}

		CurrentGoal.Activate(plan);

		return true;
	}

	void FindNewGoal()
	{
		if (CurrentGoal != null)
		{
			CurrentGoal.Deactivate();
			CurrentGoal = null;
		}

		while (CurrentGoal == null)
		{
			GOAPGoal newGoal = GetMostImportantGoal(0);

			if (newGoal == null)
				break;

			if (Owner.debugGOAP)
				Debug.Log("Find new goal " + newGoal.ToString() + "WorldState - " + Owner.WorldState.ToString());

			CreatePlan(newGoal);

			if (CurrentGoal == null)
				newGoal.SetDisableTime();
		}
	}

	void FindMostImportantGoal()
	{
		GOAPGoal newGoal = GetMostImportantGoal(CurrentGoal.GoalRelevancy);
		if (newGoal == null)
			return;

		if (newGoal == CurrentGoal)
		{
			//if (Owner.debugGOAP) Debug.Log(Time.timeSinceLevelLoad + " Current goal " + CurrentGoal.ToString() + ": " + "is most important still (" + newGoal.GoalRelevancy + ")");
			return;
		}

		if (Owner.debugGOAP && CurrentGoal != null)
		{
			Debug.Log(
					  Time.timeSinceLevelLoad + " More important goal (" + newGoal.ToString() + " >" + CurrentGoal.GoalRelevancy + ") " +
					  Owner.WorldState.ToString(),
					  Owner);
		}

		CreatePlan(newGoal);
	}

	void CreatePlan(GOAPGoal goal)
	{
		GOAPPlan plan = BuildPlan(goal);
		if (plan == null)
		{
			if (Owner.debugGOAP)
				Debug.Log(Time.timeSinceLevelLoad + " BUILD PLAN - " + goal.ToString() + " FAILED !!! " + Owner.WorldState.ToString(), Owner);
			goal.SetDisableTime();
			return;
		}

		if (CurrentGoal != null)
		{
			CurrentGoal.Deactivate();
			CurrentGoal = null;
		}

		if (Owner.debugGOAP)
		{
			Debug.Log(Time.timeSinceLevelLoad + " BUILD " + goal.ToString() + " - " + plan.ToString() + " " + Owner.WorldState.ToString(), Owner);
			foreach (KeyValuePair<E_GOAPGoals, GOAPGoal> pair in Goals)
			{
				if (pair.Value != goal && pair.Value.GoalRelevancy > 0)
					Debug.Log(pair.Value.ToString());
			}
		}

		CurrentGoal = goal;
		CurrentGoal.Activate(plan);
	}

	GOAPGoal GetMostImportantGoal(float minRelevancy)
	{
		GOAPGoal maxGoal = null;
		float highestRelevancy = minRelevancy;

		GOAPGoal goal;
		foreach (KeyValuePair<E_GOAPGoals, GOAPGoal> pair in Goals)
		{
			//First check for timing checks?!

			goal = pair.Value;
			if (goal.IsDisabled())
				continue; //we dont want to select these goals

			if (highestRelevancy >= goal.GetMaxRelevancy())
				continue; // nema cenu resit goal ktery ma mensi prioritu nez uz vybrany

			if (goal.Active == false)
			{
// recalculate goal relevancy !!!!
				goal.CalculateGoalRelevancy();
				//m_GoalSet[i].SetNewEvaluationTime();
				//set new timing check time?!
			}

			// check all goal relevancy
			if (goal.GoalRelevancy > highestRelevancy)
			{
				highestRelevancy = goal.GoalRelevancy;
				maxGoal = goal;
			}

			//if (Owner.debugGOAP) Debug.Log(i + "." + goal.ToString() + " relevancy:" + goal.GoalRelevancy); 
		}

		return maxGoal;
	}

	/**
	* Builds a new plan for this agent
	* @param the agent to build the plan for
	* @return true if the plan builds successfully, false otherwise
	*/

	public GOAPPlan BuildPlan(GOAPGoal goal)
	{
		if (goal == null)
			return null;

//        if (Owner.debugGOAP) Debug.Log(Time.timeSinceLevelLoad + " " + goal.ToString() + " - build plan");

		//initialize shit
		Map.Initialise(Owner);
		Goal.Initialise(Owner, Map, goal);

		Storage.ResetStorage(Map);

		AStar.End = -1;
		AStar.RunAStar(Owner);

		AStarNode currNode = AStar.CurrentNode;

		if (currNode == null || currNode.NodeID == -1)
		{
//            if (Owner.debugGOAP) Debug.Log("Failed , no actions");
			return null; //Building of plan failed
		}

		GOAPPlan plan = new GOAPPlan(); //create a new plan

		GOAPAction action;
		/**
		 * We need to push each new plan step onto the list of steps until we reach the last action
		 * Which is going to be the goal node and of no use
		 */

		//if (Owner.debugGOAP) Debug.Log(Time.timeSinceLevelLoad + " " + goal.ToString() + " current node id :" + currNode.NodeID);

		while (currNode.NodeID != -1)
		{
			action = Map.GetAction(currNode.NodeID);

			if (action == null) //If we tried to cast an node to an action that can't be done, quit out
			{
				Debug.LogError(Time.timeSinceLevelLoad + " " + goal.ToString() + ": canot find action (" + currNode.NodeID + ")");
				return null;
			}

			//if (Owner.debugGOAP) Debug.Log(" " + plan.NumberOfSteps + ". " +action.ToString());
			plan.PushBack(action);
			currNode = currNode.Parent;
		}

		//Finally tell the ai what its plan is
		if (plan.IsDone())
		{
			Debug.LogError(Time.timeSinceLevelLoad + " " + goal.ToString() + ": plan is already  done !!! (" + plan.CurrentStepIndex + "," +
						   plan.NumberOfSteps + ")");
			return null;
		}

		return plan;
	}
}
