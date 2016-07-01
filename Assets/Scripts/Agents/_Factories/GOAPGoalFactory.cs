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
using UnityEngine;

public enum E_GOAPGoals
{
	E_INVALID = -1,
	Move,
	Roll,
	CoverEnter,
	CoverLeave,
	CoverFire,
	CoverMove,
	CoverJumpOver,
	WeaponReload,
//	WeaponChange,
	UseWorldObject,
	UseGadget,
	PlayAnim,
	IdleAnim,
	Teleport,
	Melee,
	Count,
}

class GOAPGoalFactory : System.Object
{
	public static GOAPGoal Create(E_GOAPGoals type, AgentHuman owner)
	{
		GOAPGoal g;
		switch (type)
		{
		case E_GOAPGoals.Move:
			g = new GOAPGoalMove(owner);
			break;
		case E_GOAPGoals.Roll:
			g = new GOAPGoalRoll(owner);
			break;
		case E_GOAPGoals.CoverEnter:
			g = new GOAPGoalCoverEnter(owner);
			break;
		case E_GOAPGoals.CoverLeave:
			g = new GOAPGoalCoverLeave(owner);
			break;
		case E_GOAPGoals.CoverFire:
			g = new GOAPGoalCoverFire(owner);
			break;
		case E_GOAPGoals.CoverMove:
			g = new GOAPGoalCoverMove(owner);
			break;
		case E_GOAPGoals.CoverJumpOver:
			g = new GOAPGoalCoverJumpOver(owner);
			break;
		case E_GOAPGoals.WeaponReload:
			g = new GOAPGoalWeaponReload(owner);
			break;
/*			case E_GOAPGoals.WeaponChange:
                g = new GOAPGoalWeaponChange(owner);
                break;
*/
		case E_GOAPGoals.UseWorldObject:
			g = new GOAPGoalUseWorldObject(owner);
			break;
		case E_GOAPGoals.UseGadget:
			g = new GOAPGoalUseGadget(owner);
			break;
		case E_GOAPGoals.PlayAnim:
			g = new GOAPGoalPlayAnim(owner);
			break;
		case E_GOAPGoals.Melee:
			g = new GOAPGoalMelee(owner);
			break;
		default:
			Debug.LogError("GOAPGoalFactory Unknow goal " + type + " for " + owner.name);
			return null;
		}

		g.InitGoal();
		return g;
	}
}
