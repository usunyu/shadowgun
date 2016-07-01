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
 * Class Name : AStarEngine
 * Function   : Generic AStar engine using abstract classes for searching.. \used for creating GOAP plan or navigation path
 *				
 * Created by : Marek Rabas
 *
 **************************************************************/

using System;
using UnityEngine;

class AStarEngine : System.Object
{
	AStarGoal Goal;
	AStarMap Map;
	AStarStorage Storage;
	public AStarNode CurrentNode; // { get { return CurrentNode; } private set { CurrentNode = value; } }

	public short Start;
	public short End;

	/**
	 * Initialise the A Star machine to run a search
	 * @param the Goal we are searching towards
	 * @param the Storage which will store the open and closed lists
	 * @param the map which will get the neighbours, heuristic/action costs for the search
	 */

	public void Setup(AStarGoal _goal, AStarStorage _storage, AStarMap _aStarMap)
	{
		Goal = _goal;
		Storage = _storage;

		Map = _aStarMap;
		Storage.ResetStorage(Map);
	}

/**
 * Runs a generic AStar search
 * The search can look through action nodes or pathfinding nodes and find a path from the start to the Goal
 * @param the ai module,needed to generate neighbours
 */

	public void RunAStar(AgentHuman ai)
	{
		float g, h, f; //The g,h and f values needed for the formula f = g + h
		int numberOfNeighbours = 0;
		short neighbour;
		AStarNode neighbourNode;

		CurrentNode = Map.CreateANode(End);
		//Add the first node to the open list
		Storage.AddToOpenList(CurrentNode, Map);

		h = Goal.GetHeuristicDistance(ai, CurrentNode, true);
		CurrentNode.G = 0.0f;
		CurrentNode.H = h;
		CurrentNode.F = h;

		//if (ai.debugGOAP) Debug.Log(Time.timeSinceLevelLoad + " RunAStar " + CurrentNode.NodeID);
		/**
		 * We come to the main body of A*, we continue until a solution is found
		 */
		while (true)
		{
			//Get the node with the lowest f from the front of the Storage
			CurrentNode = Storage.RemoveCheapestOpenNode(Map);

			//Add the node to the closed list
			if (CurrentNode != null)
				Storage.AddToClosedList(CurrentNode, Map);
			else
				break;

			//Check whether we've reached our Goal
			//The abstract Goal handles the different Goal conditions for nav and planning
			if (Goal.IsAStarFinished(CurrentNode))
				break; //We've finished, break out

			/**
			 * Next job is to get the neighbours of the current node
			 * Iterate over them calculate their f,g,h and add to the open list if required
			 */

			numberOfNeighbours = Map.GetNumAStarNeighbours(CurrentNode);
/*
            if (CurrentNode.NodeID != -1)
            {
                Debug.Log("current plan is");
                int ii = 0;
                AStarNode a = CurrentNode;
                while ((Map as AStarGOAPMap).GetAction(a.NodeID) != null)
                {
                    Debug.Log(ii + ". " + (Map as AStarGOAPMap).GetAction(a.NodeID).ToString());
                    a = a.Parent;
                    ii++;
                }
                Debug.Log("numberOfNeighbours" + numberOfNeighbours);
            }
            else
                Debug.Log("GOAL - numberOfNeighbours" + numberOfNeighbours);
		*/

			for (short i = 0; i < numberOfNeighbours; i++)
			{
				neighbour = Map.GetAStarNeighbour(CurrentNode, i);
				if (neighbour == -1) //If neighbour is invalid quit
					break;

				AStarNode.E_AStarFlags flags = Map.GetAStarFlags(neighbour);
				//check whether the node is flagged as open
				if (flags == AStarNode.E_AStarFlags.Open)
				{
					neighbourNode = Storage.FindInOpenList(neighbour);
				}
				else if (flags == AStarNode.E_AStarFlags.Closed)
				{
					/**
					 * Get node from the closed list
					 * If the new f for this node is better than previously then remove the node from the closed list
					 * If it is greater than previously then skip this neighbour
					 */
					neighbourNode = Storage.FindInClosedList(neighbour);
				}
				else if (Goal.IsAStarNodePassable(neighbour))
					neighbourNode = Map.CreateANode(neighbour);
				else
					continue;

				//If out best path so far is through the neighbour
				//then theres no need to re-assess
				if (neighbourNode == null || CurrentNode.Parent == neighbourNode)
					continue;

				//Debug.Log("Testing " + (Map as AStarGOAPMap).GetAction(neighbourNode.NodeID).ToString());

				/**
				 * Get the new g,h and f for this neighbour node
				 */
				g = CurrentNode.G + (Goal.GetActualCost(CurrentNode, neighbourNode));
				h = Goal.GetHeuristicDistance(ai, neighbourNode, false);

				f = g + h;

				/**
				 * Now need to check if the new f is more expensive to get to this
				 * neighbor node from the current node than from its previous parent.
				 */
				if (f >= neighbourNode.F)
				{
					//Debug.Log("FAILED");
					continue;
				}

				neighbourNode.F = f;
				neighbourNode.G = g;
				neighbourNode.H = h;

				if (flags == AStarNode.E_AStarFlags.Closed)
					Storage.RemoveFromClosedList(neighbourNode.NodeID, Map);

				// Finally add the neighbour to the open list
				Storage.AddToOpenList(neighbourNode, Map);

				neighbourNode.Parent = CurrentNode;

				/*Debug.Log("ADDED - current plan is");
                int ii = 0;
                AStarNode a = neighbourNode;
                while ((Map as AStarGOAPMap).GetAction(a.NodeID) != null)
                {
                    Debug.Log(ii + ". " + (Map as AStarGOAPMap).GetAction(a.NodeID).ToString());
                    a = a.Parent;
                    ii++;
                }*/
			}
		}
	}

	/**
	 * Cleanup
	 */

	public void Cleanup()
	{
		Goal = null;
		Map = null;
		Storage = null;

		CurrentNode = null;
		Start = 0;
		End = 0;
	}
}
