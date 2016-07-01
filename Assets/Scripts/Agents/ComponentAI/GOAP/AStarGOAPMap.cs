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
using System.Collections.Generic;

class PrecedenceComparer : IComparer<GOAPAction>
{
	public int Compare(GOAPAction x, GOAPAction y)
	{
		return x.Precedence - y.Precedence;
	}
}

class AStarGOAPMap : AStarMap
{
	AgentHuman Ai;

	List<GOAPAction>[] m_EffectsTable = new List<GOAPAction>[(int)E_PropKey.Count];
	List<GOAPAction> m_Neighbours = new List<GOAPAction>();

	public void Initialise(AgentHuman ai)
	{
		Ai = ai;
		m_Neighbours.Clear();
	}

	public void BuildActionsEffectsTable()
	{
		//Go through every effect and add all the actions that have this effect to the effectsTable
		for (E_GOAPAction i = 0; i < E_GOAPAction.Count; i++)
		{
			GOAPAction action = Ai.GetAction(i);

			if (action == null)
				continue;

			//go through all world effects
			for (uint j = 0; j < (int)E_PropKey.Count; j++)
			{
				if (m_EffectsTable[j] == null)
					m_EffectsTable[j] = new List<GOAPAction>();

				if (action.WorldEffects.IsWSPropertySet((E_PropKey)j)) // if set
				{
					m_EffectsTable[j].Add(action); // store it
				}
			}
		}
	}

	public override int GetNumAStarNeighbours(AStarNode aStarNode)
	{
		if (aStarNode == null) //If the node is invalid then there's no neighbours
			return 0;

		//New search about to occur, clear the previous neighbour records
		m_Neighbours.Clear();

		/**
		 * Go through each world state property and test if it is in the goal and current state
		 * If it isn't then we look through the actionEffects table and see if the actions for this effect can solve the goal state
		 */
		AStarGOAPNode node = (AStarGOAPNode)aStarNode;

		WorldStateProp currProp;
		WorldStateProp goalProp;
		GOAPAction action;

		for (E_PropKey i = 0; i < E_PropKey.Count; i++)
		{
			// First test if the effect isn't in both the goal and the current state

			if (!(node.CurrentState.IsWSPropertySet(i) && node.GoalState.IsWSPropertySet(i)))
				continue; //If not skip this effect

			/**
			 * Now test if the two world state properties are the same
			 * If not we continue
			 */
			currProp = node.CurrentState.GetWSProperty(i);
			goalProp = node.GoalState.GetWSProperty(i);

			if (currProp != null && goalProp != null)
			{
				if (!(currProp == goalProp))
				{
					for (int j = 0; j < m_EffectsTable[(int)i].Count; j++)
					{
						action = m_EffectsTable[(int)i][j];

						//Are the context preconditions valid?
						if (!action.ValidateContextPreconditions(node.CurrentState, true))
						{
							continue;
						}

//                        UnityEngine.Debug.Log(action.ToString() + " adding to sousedi");
						m_Neighbours.Add(action);
					}
				}
			}
		}
		//Sort the returned neighbour actions by precedence
		PrecedenceComparer c = new PrecedenceComparer();
		m_Neighbours.Sort(c);

		return m_Neighbours.Count;
	}

	/**
	 * Returns the neighbours action converted to a node
	* @return a neighbour
	*/

	public override short GetAStarNeighbour(AStarNode AStarNode, short neighbourCount)
	{
		return ((short)m_Neighbours[neighbourCount].Type);
	}

	/**
	 * Creates an a-star node
	* @param the id of the new node
	* @return a new a-star node
	*/

	public override AStarNode CreateANode(short id)
	{
		AStarGOAPNode newNode = new AStarGOAPNode();
		newNode.NodeID = id;

		return newNode;
	}

	/**
	* Returns the A Star Flags
	* @return the unchecked flag
	*/

	public override AStarNode.E_AStarFlags GetAStarFlags(short node)
	{
		return AStarNode.E_AStarFlags.Unchecked;
	}

	/**
	 * compares the two nodes for precedence, returns true if node one has higher precedence than node two
	 */

	public override bool CompareNodes(AStarNode node1, AStarNode node2)
	{
		GOAPAction action1 = GetAction(node1.NodeID);
		GOAPAction action2 = GetAction(node2.NodeID);

		return (action1.Precedence > action2.Precedence);
	}

	/**
	 * gets the AI action
	 * @return the AI action for this action type
	 */

	public GOAPAction GetAction(short nodeID)
	{
		return Ai.GetAction((E_GOAPAction)nodeID);
	}

	public override void Cleanup()
	{
	}
}
