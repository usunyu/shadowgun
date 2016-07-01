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
 * Class Name : Goal
 * Function   : A base class for all AgentActions
 *						Agent action is for notify to other systems, components,etc.. stored in blackboards	
 *				
 * Created by : Marek Rabas
 **************************************************************/

using UnityEngine;

public class AgentAction : System.Object
{
	public enum E_State
	{
		E_ACTIVE,
		E_SUCCESS,
		E_FAILED,
		E_UNUSED,
	}

	public AgentActionFactory.E_Type Type;

	public E_State Status = AgentAction.E_State.E_ACTIVE;

	public bool IsActive()
	{
		return Status == E_State.E_ACTIVE;
	}

	public bool IsFailed()
	{
		return Status == E_State.E_FAILED;
	}

	public bool IsSuccess()
	{
		return Status == E_State.E_SUCCESS;
	}

	public bool IsUnused()
	{
		return Status == E_State.E_UNUSED;
	}

	public void SetSuccess()
	{
		Status = E_State.E_SUCCESS; /*Debug.Log(this.ToString() + " set to " + Status.ToString());*/
	}

	//public void                     SetSuccess(float delay) { Status = E_State.E_SUCCESS; }
	public void SetFailed()
	{
		Status = E_State.E_FAILED; /*Debug.Log(this.ToString() + " set to " + Status.ToString());*/
	}

	public void SetUnused()
	{
		Status = E_State.E_UNUSED;
	}

	public void SetActive()
	{
		Status = E_State.E_ACTIVE;
	}

	public AgentAction(AgentActionFactory.E_Type type)
	{
		Type = type;
	}

	public virtual void Reset()
	{
	}

	public override string ToString()
	{
		return Type + " " + Status;
	}
}
