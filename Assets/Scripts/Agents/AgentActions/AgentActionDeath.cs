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

public class AgentActionDeath : AgentAction
{
	public AgentHuman Attacker;
	public Vector3 Pos;
	public Vector3 Impulse;
	public int Damage;
	public E_BodyPart BodyPart;

	public E_WeaponID ByWeapon;
	public E_ItemID ByGadget;

	public AgentActionDeath() : base(AgentActionFactory.E_Type.Death)
	{
	}
}
