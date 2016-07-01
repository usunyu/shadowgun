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

////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////	A STAR NODE
///////////////////////////////////////////////////////////////////////////////////////////

class AStarNode : System.Object
{
	public enum E_AStarFlags
	{
		Unchecked = 0,
		Open = 1,
		Closed = 2,
		NotPassable = 3,
	}

	public AStarNode()
	{
		NodeID = -1;
		G = 0;
		H = 0;
		F = float.MaxValue;

		Flag = E_AStarFlags.Unchecked;
	}

	public short NodeID;
	public float G;
	public float H;
	public float F;
	public AStarNode Next;
	public AStarNode Previous;
	public AStarNode Parent;
	public E_AStarFlags Flag;
};
