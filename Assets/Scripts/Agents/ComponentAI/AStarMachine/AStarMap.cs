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

abstract class AStarMap : System.Object
{
	public abstract int GetNumAStarNeighbours(AStarNode pAStarNode);
	public abstract short GetAStarNeighbour(AStarNode pAStarNode, short iNeighbor);
	public abstract AStarNode CreateANode(short id);
	public abstract AStarNode.E_AStarFlags GetAStarFlags(short NodeID);

	public virtual void SetAStarFlags(short NodeID, AStarNode.E_AStarFlags flag)
	{
	}

	public abstract bool CompareNodes(AStarNode node1, AStarNode node2);
	public abstract void Cleanup();
}
