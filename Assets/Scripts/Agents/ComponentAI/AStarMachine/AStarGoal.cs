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

////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////		Abstract Goal Node
///////////////////////////////////////////////////////////////////////////////////////////////////

abstract class AStarGoal : System.Object
{
	public abstract void SetDestNode(AStarNode destNode);
	public abstract float GetHeuristicDistance(AgentHuman ai, AStarNode pAStarNode, bool firstRun);
	public abstract float GetActualCost(AStarNode nodeOne, AStarNode nodeTwo);
	public abstract bool IsAStarFinished(AStarNode currNode);
	public abstract bool IsAStarNodePassable(int node);
	public abstract void Cleanup();
}
