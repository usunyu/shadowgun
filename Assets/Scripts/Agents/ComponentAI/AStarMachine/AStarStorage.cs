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
 * Class Name : AStarStorage
 * Function   : A class that stores AStar open and closed lists.
 *				Also performs functions such as returning cheapest nodes and finding nodes in the list
 * Created by : Marek Rabas
 *
 **************************************************************/

using System;
using System.Collections.Generic;

class AStarStorage : System.Object
{
	AStarNode headOfOpenList = null;
	AStarNode headOfClosedList = null;
	ushort openSize = 0;

	/**
	* Adds the specified node to the open list. 
	* The list is sorted so the node must be inserted in order
	* @param the node to add
	*/

	public void AddToOpenList(AStarNode node, AStarMap map)
	{
		AStarNode currNode = headOfOpenList;
		AStarNode PreviousNode = null;
		AStarNode NextNode = null;

		if (map.GetAStarFlags(node.NodeID) == AStarNode.E_AStarFlags.Open)
			return;

		if (currNode == null)
		{
			headOfOpenList = node;
			openSize = 1;
			map.SetAStarFlags(node.NodeID, AStarNode.E_AStarFlags.Open);
			node.Flag = AStarNode.E_AStarFlags.Open;
		}
		else
		{
			bool inserted = false;
			while (!inserted)
			{
				NextNode = currNode.Next;
				PreviousNode = currNode.Previous;

				//New node has a higher value than what is already in the open list
				if (currNode.F > node.F)
				{
					//If no Previous node then we have the start node
					if (PreviousNode == null)
					{
						currNode.Previous = node;
						node.Next = currNode;
						node.Previous = null;
						headOfOpenList = node;
						node.Flag = AStarNode.E_AStarFlags.Open;
						map.SetAStarFlags(node.NodeID, AStarNode.E_AStarFlags.Open);
						inserted = true;
						openSize++;
					}
					else
					{
						PreviousNode.Next = node;
						node.Previous = PreviousNode;
						node.Next = currNode;
						currNode.Previous = node;
						map.SetAStarFlags(node.NodeID, AStarNode.E_AStarFlags.Open);
						node.Flag = AStarNode.E_AStarFlags.Open;
						inserted = true;
						openSize++;
					}
				}
				else if (currNode.F == node.F)
				{
					//Check if the current node has a higher precendence than existing
					if (map.CompareNodes(currNode, node))
					{
						//Can insert node in before the current node
						if (PreviousNode != null)
						{
							PreviousNode.Next = node;
							currNode.Previous = node;
							node.Previous = PreviousNode;
							node.Next = currNode;
							map.SetAStarFlags(node.NodeID, AStarNode.E_AStarFlags.Open);
							node.Flag = AStarNode.E_AStarFlags.Open;
							inserted = true;
							openSize++;
						}
						else if (PreviousNode == null)
						{
							node.Previous = null;
							node.Next = currNode;
							currNode.Previous = node;
							map.SetAStarFlags(node.NodeID, AStarNode.E_AStarFlags.Open);
							node.Flag = AStarNode.E_AStarFlags.Open;
							inserted = true;
							openSize++;
							headOfOpenList = node;
						}
					}
					else
					{
						if (NextNode == null)
						{
							currNode.Next = node;
							node.Previous = currNode;
							node.Next = null;
							map.SetAStarFlags(node.NodeID, AStarNode.E_AStarFlags.Open);
							node.Flag = AStarNode.E_AStarFlags.Open;
							inserted = true;
							openSize++;
						}
						else
							currNode = NextNode;
					}
				}
				else
				{
					if (NextNode == null)
					{
						currNode.Next = node;
						node.Previous = currNode;
						node.Next = null;
						map.SetAStarFlags(node.NodeID, AStarNode.E_AStarFlags.Open);
						node.Flag = AStarNode.E_AStarFlags.Open;
						inserted = true;
						openSize++;
					}
					else
						currNode = NextNode;
				}
			}
		}
	}

	/**
	* Adds the specified node to the open list. 
	* Insert new node to head of closed list
	* @param the node to add
	*/

	public void AddToClosedList(AStarNode node, AStarMap map)
	{
		AStarNode currNode = headOfClosedList;

		if (currNode != null)
		{
			node.Next = currNode;
			node.Previous = null;
			currNode.Previous = node;
			headOfClosedList = node;
			map.SetAStarFlags(node.NodeID, AStarNode.E_AStarFlags.Closed);
			node.Flag = AStarNode.E_AStarFlags.Closed;
		}
		else
		{
			headOfClosedList = node;
			node.Next = null;
			node.Previous = null;
			map.SetAStarFlags(node.NodeID, AStarNode.E_AStarFlags.Closed);
			node.Flag = AStarNode.E_AStarFlags.Closed;
		}
	}

	/**
	* Find the specified node in the open list
	* @param the node to check
	*/

	public AStarNode FindInOpenList(short node)
	{
		AStarNode currNode = headOfOpenList;

		while (currNode != null)
		{
			if (currNode.NodeID == node)
				return currNode;

			currNode = currNode.Next;
		}

		return null;
	}

	/**
	* Find the specified node in the closed list
	* @param the node to check
	*/

	public AStarNode FindInClosedList(short node)
	{
		AStarNode currNode = headOfClosedList;

		while (currNode != null)
		{
			if (currNode.NodeID == node)
				return currNode;

			currNode = currNode.Next;
		}

		return null;
	}

/**
 * removes the specified node from the closed list
 * @param the node to remove from the closed list
 */

	public void RemoveFromClosedList(short nodeId, AStarMap map)
	{
		AStarNode removeNode = FindInClosedList(nodeId);

		if (removeNode != null)
		{
			AStarNode next = removeNode.Next;
			AStarNode previous = removeNode.Previous;
			if (next != null)
				next.Previous = previous;

			if (previous != null)
				previous.Next = next;

			if (headOfClosedList == removeNode)
				headOfClosedList = next;

			removeNode.Next = null;
			removeNode.Previous = null;
			removeNode.Flag = AStarNode.E_AStarFlags.Unchecked;
			map.SetAStarFlags(removeNode.NodeID, AStarNode.E_AStarFlags.Unchecked);
		}
	}

	/**
	* removes the specified node from the open list
	* @param the node to remove from the open list
	*/

	public void RemoveFromOpenList(short nodeId, AStarMap map)
	{
		AStarNode removeNode = FindInOpenList(nodeId);

		if (removeNode != null)
		{
			AStarNode next = removeNode.Next;
			AStarNode previous = removeNode.Previous;
			if (next != null)
				next.Previous = previous;

			if (previous != null)
				previous.Next = next;

			if (headOfOpenList == removeNode)
				headOfOpenList = next;

			removeNode.Next = null;
			removeNode.Previous = null;
			removeNode.Flag = AStarNode.E_AStarFlags.Unchecked;
			map.SetAStarFlags(removeNode.NodeID, AStarNode.E_AStarFlags.Unchecked);
			openSize--;
		}
	}

	/**
	* Removes the first node from the open list
	* @return the node at the head of the list
	*/

	public AStarNode RemoveCheapestOpenNode(AStarMap map)
	{
		if (openSize == 0)
			return null;

		AStarNode head = headOfOpenList;
		if (openSize == 1)
		{
			headOfOpenList.Next = null;
			headOfOpenList.Previous = null;
			headOfOpenList = null;
			head.Flag = AStarNode.E_AStarFlags.Unchecked;
			map.SetAStarFlags(head.NodeID, AStarNode.E_AStarFlags.Unchecked);
			openSize--;
		}
		else if (openSize > 1)
		{
			AStarNode front = headOfOpenList;
			AStarNode Next = headOfOpenList.Next;

			front.Next = null;
			front.Previous = null;

			front.Flag = AStarNode.E_AStarFlags.Unchecked;
			map.SetAStarFlags(front.NodeID, AStarNode.E_AStarFlags.Unchecked);

			headOfOpenList = Next;
			headOfOpenList.Previous = null;
			head = front;

			openSize--;
		}

		return head;
	}

	public bool CheckStorage()
	{
		if (headOfOpenList != null && headOfOpenList.Next == null && openSize > 1)
			return true;
		return false;
	}

	public void Cleanup()
	{
		headOfOpenList = null;
		headOfClosedList = null;
	}

	/*
	*  Go through the open and closed lists and clear the Next and previous pointers 
	*/

	public void ResetStorage(AStarMap map)
	{
		AStarNode currNode = headOfOpenList;
		AStarNode nextNode = headOfOpenList;
		while (nextNode != null)
		{
			map.SetAStarFlags(nextNode.NodeID, AStarNode.E_AStarFlags.Unchecked);
			currNode = nextNode;
			nextNode = currNode.Next;
			currNode.Flag = AStarNode.E_AStarFlags.Unchecked;
			currNode.Parent = null;
			currNode.Previous = null;
			currNode.Next = null;
			currNode = null;
		}

		currNode = headOfClosedList;
		nextNode = headOfClosedList;

		while (nextNode != null)
		{
			map.SetAStarFlags(nextNode.NodeID, AStarNode.E_AStarFlags.Unchecked);
			currNode = nextNode;
			nextNode = currNode.Next;
			currNode.Parent = null;
			map.SetAStarFlags(currNode.NodeID, AStarNode.E_AStarFlags.Unchecked);
			currNode.Previous = null;
			currNode.Next = null;
			currNode = null;
		}

		headOfOpenList = null;
		headOfClosedList = null;

		openSize = 0;
	}
}
