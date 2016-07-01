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

public enum E_GOAPAction
{
	invalid = -1,
	Move, //30
	Goto, //70
	Roll,
	WeaponReload,
//	WeaponChange,
	Use, //0
	UseGadget,
	PlayAnim, //0
	Knockdown, //100
	CoverEnter, //
	CoverMove, //40 
	CoverFire,
	CoverLeave,
	CoverJumpOverPlayer,
	CoverLeaveRightLeft,
	Melee,
	Count
}

class GOAPActionFactory : System.Object
{
	public static GOAPAction Create(E_GOAPAction type, AgentHuman owner)
	{
		GOAPAction a;
		switch (type)
		{
		case E_GOAPAction.Move:
			a = new GOAPActionMove(owner);
			break;
		case E_GOAPAction.Goto:
			a = new GOAPActionGoTo(owner);
			break;
		case E_GOAPAction.Roll:
			a = new GOAPActionRoll(owner);
			break;
/*			case E_GOAPAction.WeaponChange:
                a = new GOAPActionWeaponChange(owner);
                break;
*/
		case E_GOAPAction.WeaponReload:
			a = new GOAPActionWeaponReload(owner);
			break;
		case E_GOAPAction.Use:
			a = new GOAPActionUse(owner);
			break;
		case E_GOAPAction.UseGadget:
			a = new GOAPActionUseGadget(owner);
			break;
		case E_GOAPAction.PlayAnim:
			a = new GOAPActionPlayAnim(owner);
			break;
		case E_GOAPAction.CoverEnter:
			a = new GOAPActionCoverEnter(owner);
			break;
		case E_GOAPAction.CoverMove:
			a = new GOAPActionCoverMove(owner);
			break;
		case E_GOAPAction.CoverFire:
			a = new GOAPActionCoverFire(owner);
			break;
		case E_GOAPAction.CoverLeave:
			a = new GOAPActionCoverLeave(owner);
			break;
		case E_GOAPAction.CoverJumpOverPlayer:
			a = new GOAPActionCoverJumpOverPlayer(owner);
			break;
		case E_GOAPAction.CoverLeaveRightLeft:
			a = new GOAPActionCoverLeaveRightLeft(owner);
			break;
		case E_GOAPAction.Melee:
			a = new GOAPActionMelee(owner);
			break;
		default:
			Debug.LogError("GOAPActionFactory -  unknow state " + type);
			return null;
		}

		a.InitAction();
		return a;
	}
}
