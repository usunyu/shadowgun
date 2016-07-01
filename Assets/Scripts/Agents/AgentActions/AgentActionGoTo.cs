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

using UnityEngine;

public class AgentActionGoTo : AgentAction
{
	public Vector3 FinalPosition;
	public E_MoveType MoveType = E_MoveType.Forward;
	public E_MotionType Motion = E_MotionType.Run;
	public Transform LookTarget = null;
	public float MinDistance = 0.3f;
	public bool DontChangeParameters = false;
	public bool UseNavMeshAgentRotation = false;

	public AgentActionGoTo() : base(AgentActionFactory.E_Type.Goto)
	{
	}

	public override void Reset()
	{
		MoveType = E_MoveType.Forward;
		Motion = E_MotionType.Run;
		LookTarget = null;
		MinDistance = 0.3f;
		DontChangeParameters = false;
		UseNavMeshAgentRotation = false;
	}
}
