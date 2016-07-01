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
using System.Collections;
using UnityEngine;

class AStarGOAPGoal : AStarGoal
{
	GOAPGoal Goal;
	AStarGOAPMap Map;
	//AgentHuman Owner;

	public void Initialise(AgentHuman ai, AStarGOAPMap map, GOAPGoal goal)
	{
		// Owner = ai;
		Map = map;
		Goal = goal;
	}

	/********************************************
	*			GETTERS/SETTERS
	********************************************/

	public override float GetHeuristicDistance(AgentHuman ai, AStarNode aStarNode, bool firstRun)
	{
		//If we are on the first run then we know that we have no node so we need to setup the first node
		AStarGOAPNode node = (AStarGOAPNode)aStarNode;
		if (firstRun)
		{
			//Copy the WS satisfaction conditions to the nodes goal state
			node.GoalState.Reset();
			Goal.SetWSSatisfactionForPlanning(node.GoalState);
			Goal.ChangeCurrentWSForPlanning(node.CurrentState);
		}
		else
		{
			//Now we know that the node being checked is an action. 
			//We need to get the action and apply it
			GOAPAction action = Map.GetAction(aStarNode.NodeID);
			//if(action.ValidateWSPreconditions(node.CurrentState,node.GoalState))
			{
				//if(action.ValidateWSEffects(ai,&node.CurrentState,&node.GoalState))
				{
					//action.AddInvalidPreconditionsToGoal(ai,&node.CurrentState,&node.GoalState);
					//action.ApplyWSEffects(ai,&node.CurrentState,&node.GoalState);
					action.SolvePlanWSVariable(node.CurrentState, node.GoalState);
					action.SetPlanWSPreconditions(node.GoalState);
				}
			}
		}

		//  if (Owner.debugGOAP) Debug.Log("GetHeuristicDistance1 : goal" + node.GoalState.ToString());
		//  if (Owner.debugGOAP) Debug.Log("GetHeuristicDistance1 : current" + node.CurrentState.ToString());

		MergeStates(ai, node.CurrentState, node.GoalState);

		//       if (Owner.debugGOAP) Debug.Log("GetHeuristicDistance2 : goal" + node.GoalState.ToString());
//        if (Owner.debugGOAP) Debug.Log("GetHeuristicDistance2 : current" + node.CurrentState.ToString());

		return node.GoalState.GetNumWorldStateDifferences(node.CurrentState);
	}

	public override float GetActualCost(AStarNode nodeOne, AStarNode nodeTwo)
	{
		/**
		* When getting the actual cost we must first copy over the states from one node to another
		* Then we can get the actual costs for the action and return them
		*/
		AStarGOAPNode pNodeOne = (AStarGOAPNode)nodeOne;
		AStarGOAPNode pNodeTwo = (AStarGOAPNode)nodeTwo;

		pNodeTwo.CurrentState.CopyWorldState(pNodeOne.CurrentState);
		pNodeTwo.GoalState.CopyWorldState(pNodeOne.GoalState);

		//Now try and get the action for the second node. If it exists return the cost

		GOAPAction action = Map.GetAction(nodeTwo.NodeID);

		if (action != null)
			return action.Cost;

		//Something went wrong, return a high value
		return float.MaxValue;
	}

	public override bool IsAStarFinished(AStarNode currNode)
	{
		/**
		* A Star is finished if we can go from the AI agents current state 
		* and by applying the actions in reverse arrive at the goal
		*/
		//A Star can be finished if the node being input is NULL
		if (currNode == null)
			return true;

		AStarGOAPNode node = (AStarGOAPNode)(currNode);

		if (IsPlanValid(node))
			return true;

		return false;
	}

	public override bool IsAStarNodePassable(int node)
	{
		return true;
	}

	public bool IsPlanValid(AStarGOAPNode currentNode)
	{
		/*if (Owner.debugGOAP)
        {
            if (Map.GetAction(currentNode.NodeID) != null)
                Debug.Log("IsPlanValid : GOAL");
            else
                Debug.Log("IsPlanValid node id :" + currentNode.NodeID);        

			int ii = 0;
			AStarNode a = currentNode;
			while (Map.GetAction(a.NodeID) != null)
			{
				Debug.Log(ii + ". " + Map.GetAction(a.NodeID).ToString());
				a = a.Parent;
				ii++;
			}
		}*/

		//if (Owner.debugGOAP) Debug.Log("WS diference :" + currentNode.GoalState.GetNumWorldStateDifferences(currentNode.CurrentState));

		// if (Owner.debugGOAP) Debug.Log("WS : goal " + currentNode.GoalState.ToString());
		//if (Owner.debugGOAP) Debug.Log("WS : current " + currentNode.CurrentState.ToString());

		if (currentNode.GoalState.GetNumWorldStateDifferences(currentNode.CurrentState) == 0)
		{
			WorldState tempState = new WorldState();
			//tempState.CopyWorldState(Ai.WorldState);
			tempState.CopyWorldState(currentNode.GoalState);

			// if (Owner.debugGOAP) Debug.Log(tempState.ToString());

			AStarGOAPNode currNode = currentNode;

			GOAPAction parentNode;

			// int i = 0;
			while (currNode.NodeID != -1)
			{
				parentNode = Map.GetAction(currNode.NodeID);

				/*if (Owner.debugGOAP) Debug.Log("Validating" + i + ". "+ parentNode.ToString());
                i++;

				//Get out if the current world state is validated by the current node
                if (!parentNode.ValidateWSEffects(tempState, null))
                {
                    if (Owner.debugGOAP)
                    {
                        Debug.Log("FAILED: ValidateWSEffects return false");
                        Debug.Log(parentNode.ToString() + " " + parentNode.WorldEffects.ToString());
                        Debug.Log(tempState.ToString());

                    }
                    return false;
                }*/

				/**
				 * Now that the action's preconditions have been reversed,
				 * the preconditions of the action must be satisfied
				 */
				if (!parentNode.ValidateWSPreconditions(tempState, null))
				{
					//         if (Owner.debugGOAP) Debug.Log("FAILED: ValidateWSPreconditions return false");
					return false;
				}

				/**
				 * The context preconditions must also be satisfied
				 */
				if (!parentNode.ValidateContextPreconditions(tempState, true))
				{
					//            if (Owner.debugGOAP) Debug.Log("FAILED: ValidateContextPreconditions return false");
					return false;
				}

				/**
				 * Apply the preconditions to the temporate world state
				 */
				parentNode.ApplyWSEffects(tempState, null);

				//Get the next node in the list of nodes
				currNode = (AStarGOAPNode)currNode.Parent;
			}

			//Have applied all the actions in reverse,is the temp state now equal to the agents current state???
			if (Goal.IsWSSatisfiedForPlanning(tempState))
			{
//                if (Owner.debugGOAP) Debug.Log("TRUE :");
				return true;
			}
		}

//        Debug.Log("FAILED");
		return false;
	}

	public void MergeStates(AgentHuman ai, WorldState currentState, WorldState goalState)
	{
		for (E_PropKey iProp = 0; iProp < E_PropKey.Count; ++iProp)
		{
			// Continue if property already exists in current 
			// world state.

			if ((!goalState.IsWSPropertySet(iProp)) || // if not set in goal
				(currentState.IsWSPropertySet(iProp))) //if already set
			{
				continue; // then continue
			}

			// and set real property from Agent to current
			WorldStateProp prop = ai.WorldState.GetWSProperty((E_PropKey)iProp);
			currentState.SetWSProperty(prop);
		}
	}

	public override void SetDestNode(AStarNode destNode)
	{
	}

	public override void Cleanup()
	{
	}
}
