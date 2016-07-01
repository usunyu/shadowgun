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
 * Class Name : Action
 * Function   : A base class for all Actions in GOAP system
 *				
 * Created by : Marek Rabas
 *
 **************************************************************/

using System;
using UnityEngine;

public abstract class GOAPAction : System.Object
{
	// what states we have to solve to use this action
	public WorldState WorldPreconditions; // { get { return WorldPreconditions; } private set { WorldPreconditions = value; } }

	// action is solvning these states
	public WorldState WorldEffects; // { get { return WorldEffects; } private set { WorldEffects = value; } }

	// cost of action, needed for heuristic
	public float Cost; // { get { return Cost; } private set { Cost = value; } }

	// if we could interup this action
	public bool Interruptible = true; // { get { return Interruptible; } protected set { Interruptible = value; } }

	// precende is for scoring in map, higher precedence means higher priority for action !! 0-100
	public int Precedence = 0; // { get { return Precedence; } protected set { Precedence = value; } } 

	// type of action
	public E_GOAPAction Type; // { get { return Type; } private set { Type = value; } }
	public AgentHuman Owner; // { get { return Owner; } private set { Owner = value; } }

	protected GOAPAction(E_GOAPAction type, AgentHuman owner)
	{
		WorldPreconditions = new WorldState();
		WorldEffects = new WorldState();

		Type = type;
		Owner = owner;
	}

	public abstract void InitAction();

	public virtual void Update()
	{
	}

	/**
 * Solve a plans WS variable. Basically just need to go through each of the effects of the action and set the current state to be equal to the goal state for each of the valid effect properties
 * @param the current state of the agent
 * @param the goal state aiming for
 */

	public virtual void SolvePlanWSVariable(WorldState currentState, WorldState goalState)
	{
		WorldStateProp effect;
		WorldStateProp goal;
		for (int i = 0; i < (int)E_PropKey.Count; i++)
		{
			//Get the effect 
			effect = WorldEffects.GetWSProperty((E_PropKey)i);

			//If the effect has been set
			if (effect != null)
			{
				//Get the goal property for this effect key
				goal = goalState.GetWSProperty(effect.PropKey);

				//If the goal has this WS Prop set
				if (goal != null) //Set the current state to be the goal WS
					currentState.SetWSProperty(goal);
			}
		}
	}

	/**
	 *  Checks the current state and tests if the current state is different from the action's effects
	 *  If the current state is the same as the effect state then the validation fails, otherwise it passes
	 *  @param the current state
	 *  @param the goal state
	 */

	public bool ValidateWSEffects(WorldState current, WorldState goal)
	{
		if (WorldEffects.GetNumUnsatisfiedWorldStateProps(current) == 0)
			return true;

		return false;
	}

	// Validates the context preconditions
	public virtual bool ValidateContextPreconditions(WorldState current, bool planning)
	{
		return true;
	}

	/**
	*	Checks the current state and returns true if the action's preconditions and met by the current state
	* @param the current state
	* @param the goal state
	*/

	public bool ValidateWSPreconditions(WorldState current, WorldState goal)
	{
		if (WorldPreconditions.GetNumUnsatisfiedWorldStateProps(current) == 0)
			return true;

		return false;
	}

	/**
 * Sets the plans WS preconditions. Takes in the goal and sets its WS to be the preconditions
 * @param the agents ai
 * @param the goal state
 */

	public virtual void SetPlanWSPreconditions(WorldState goalState)
	{
		WorldStateProp precond;

		for (E_PropKey i = 0; i < E_PropKey.Count; i++)
						//Go through the action's preconditions and set the goal state's properties to be equal to the precondition properties
		{
			precond = WorldPreconditions.GetWSProperty(i);

			//If the precondition isn't invalid
			if (precond != null)
				goalState.SetWSProperty(precond);
		}
	}

	/**
 * Applies the actions world state effects to the current world state
 * @param the current world state
 * @param the goal state
 */

	public void ApplyWSEffects(WorldState currentState, WorldState goalState)
	{
		WorldStateProp effect;
		for (E_PropKey i = 0; i < E_PropKey.Count; i++)
		{
			effect = WorldEffects.GetWSProperty(i);

			//If effect is valid
			if (effect != null)
				currentState.SetWSProperty(effect);
		}
	}

	/**
	 * Validates the action. Checks if the action is valid,it is invalid if the action status has failed
	 * @param the ai module
	 */

	public virtual bool ValidateAction()
	{
		return true;
	}

	public virtual bool IsActionComplete()
	{
		return false;
	}

	public virtual void Activate()
	{
		if (Owner.debugGOAP)
			DebugLogActivate();
	}

	public virtual void Deactivate()
	{
		if (Owner.debugGOAP)
			Debug.Log(Time.timeSinceLevelLoad + " " + this.ToString() + " - Deactivated");
	}

	protected virtual void DebugLogActivate()
	{
		Debug.Log(Time.timeSinceLevelLoad + " " + this.ToString() + " - Activated");
	}
}
