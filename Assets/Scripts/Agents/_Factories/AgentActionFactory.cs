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
using System.Collections.Generic;
using UnityEngine;

public static class AgentActionFactory
{
	public enum E_Type
	{
		Idle,
		Move,
		Sprint,
		Goto,
		Attack,
		Melee,
		Injury,
		Roll,
		WeaponShow,
		Rotate,
		Use,
		PlayAnim,
		PlayIdleAnim,
		Death,
		Knockdown,
		Teleport,
		CoverEnter,
		CoverLeave,
		CoverMove,
		CoverFire,
		CoverFireCancel,
		Reload,
		WeaponChange,
		UseItem,
		ConstructGadget,
		TeamCommand,
		Count
	}

	static Queue<AgentAction>[] m_UnusedActions = new Queue<AgentAction>[(int)E_Type.Count];

	// DEBUG !!!!!!!
//	static private List<AgentAction> m_ActionsInAction = new List<AgentAction>();

	static AgentActionFactory()
	{
		for (E_Type i = 0; i < E_Type.Count; i++)
		{
			m_UnusedActions[(int)i] = new Queue<AgentAction>();
			//maybe we could precreate few of them ?
		}
	}

	public static AgentAction Create(E_Type type)
	{
		int index = (int)type;

		AgentAction a;
		if (m_UnusedActions[index].Count > 0)
		{
			a = m_UnusedActions[index].Dequeue();
		}
		else
		{
			switch (type)
			{
			case E_Type.Idle:
				a = new AgentActionIdle();
				break;
			case E_Type.Move:
				a = new AgentActionMove();
				break;
			case E_Type.Sprint:
				a = new AgentActionSprint();
				break;
			case E_Type.Goto:
				a = new AgentActionGoTo();
				break;
			case E_Type.Attack:
				a = new AgentActionAttack();
				break;
			case E_Type.Melee:
				a = new AgentActionMelee();
				break;
			case E_Type.Injury:
				a = new AgentActionInjury();
				break;
			case E_Type.Roll:
				a = new AgentActionRoll();
				break;
			case E_Type.WeaponChange:
				a = new AgentActionWeaponChange();
				break;
			case E_Type.Rotate:
				a = new AgentActionRotate();
				break;
			case E_Type.Use:
				a = new AgentActionUse();
				break;
			case E_Type.PlayAnim:
				a = new AgentActionPlayAnim();
				break;
			case E_Type.PlayIdleAnim:
				a = new AgentActionPlayIdleAnim();
				break;
			case E_Type.Death:
				a = new AgentActionDeath();
				break;
			case E_Type.Knockdown:
				a = new AgentActionKnockdown();
				break;
			case E_Type.Teleport:
				a = new AgentActionTeleport();
				break;
			case E_Type.CoverEnter:
				a = new AgentActionCoverEnter();
				break;
			case E_Type.CoverMove:
				a = new AgentActionCoverMove();
				break;
			case E_Type.CoverFire:
				a = new AgentActionCoverFire();
				break;
			case E_Type.CoverFireCancel:
				a = new AgentActionCoverFireCancel();
				break;
			case E_Type.CoverLeave:
				a = new AgentActionCoverLeave();
				break;
			case E_Type.Reload:
				a = new AgentActionReload();
				break;
			case E_Type.UseItem:
				a = new AgentActionUseItem();
				break;
			case E_Type.ConstructGadget:
				a = new AgentActionConstructGadget();
				break;
			case E_Type.TeamCommand:
				a = new AgentActionTeamCommand();
				break;
			default:
				Debug.LogError("no AgentAction to create");
				return null;
			}
		}
		a.Reset();
		a.SetActive();

		// DEBUG !!!!!!
		//	m_ActionsInAction.Add(a);
		return a;
	}

	public static void Return(AgentAction action)
	{
		action.SetUnused();

		m_UnusedActions[(int)action.Type].Enqueue(action);
		//DEBUG SHIT
//		m_ActionsInAction.Remove(action);
	}

	public static void Clear()
	{
		for (E_Type i = 0; i < E_Type.Count; i++)
			m_UnusedActions[(int)i].Clear();
	}

	/*  static public void Report()
    {
        Debug.Log("Action Factory m_ActionsInAction " + m_ActionsInAction.Count);
        for (int i = 0; i < m_ActionsInAction.Count; i++)
            Debug.Log(m_ActionsInAction[i].ToString());
    }*/
}
