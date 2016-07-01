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
using System.Collections.Generic;

public class AnimFSM
{
	protected Dictionary<AgentActionFactory.E_Type, AnimState> AnimStates = new Dictionary<AgentActionFactory.E_Type, AnimState>(0);
	public AnimState CurrentAnimState { get; private set; }
	protected AnimState NextAnimState;
	protected AnimState DefaultAnimState;

	protected Animation AnimEngine;
	protected AgentHuman Owner;

	public AnimFSM(Animation anims, AgentHuman owner)
	{
		AnimEngine = anims;
		Owner = owner;
	}

	public void Initialize()
	{
		AnimStates.Add(AgentActionFactory.E_Type.Idle, new AnimStateIdle(AnimEngine, Owner));
		AnimStates.Add(AgentActionFactory.E_Type.Goto, new AnimStateGoToWithoutNavmesh(AnimEngine, Owner));
		AnimStates.Add(AgentActionFactory.E_Type.Move, new AnimStateMove(AnimEngine, Owner));
		AnimStates.Add(AgentActionFactory.E_Type.CoverEnter, new AnimStateCoverEnter(AnimEngine, Owner));
		AnimStates.Add(AgentActionFactory.E_Type.CoverMove, new AnimStateCoverMove(AnimEngine, Owner));
		AnimStates.Add(AgentActionFactory.E_Type.CoverLeave, new AnimStateCoverLeave(AnimEngine, Owner));
		AnimStates.Add(AgentActionFactory.E_Type.CoverFire, new AnimStateCoverFire(AnimEngine, Owner));
		AnimStates.Add(AgentActionFactory.E_Type.Use, new AnimStateUse(AnimEngine, Owner));
		AnimStates.Add(AgentActionFactory.E_Type.Teleport, new AnimStateTeleport(AnimEngine, Owner));
		AnimStates.Add(AgentActionFactory.E_Type.Injury, new AnimStateInjury(AnimEngine, Owner));
		AnimStates.Add(AgentActionFactory.E_Type.Death, new AnimStateDeath(AnimEngine, Owner));
		AnimStates.Add(AgentActionFactory.E_Type.PlayAnim, new AnimStatePlayAnim(AnimEngine, Owner));
		AnimStates.Add(AgentActionFactory.E_Type.Roll, new AnimStateRoll(AnimEngine, Owner));
		AnimStates.Add(AgentActionFactory.E_Type.UseItem, new AnimStateUseItem(AnimEngine, Owner));
		AnimStates.Add(AgentActionFactory.E_Type.Melee, new AnimStateMelee(AnimEngine, Owner));
		AnimStates.Add(AgentActionFactory.E_Type.Knockdown, new AnimStateKnockdown(AnimEngine, Owner));

		DefaultAnimState = AnimStates[AgentActionFactory.E_Type.Idle];
	}

	public void Activate()
	{
		CurrentAnimState = DefaultAnimState;
		CurrentAnimState.OnActivate(null);
		NextAnimState = null;
	}

	// Update is called once per frame
	public void UpdateAnimStates()
	{
		if (CurrentAnimState.IsFinished())
		{
			CurrentAnimState.OnDeactivate();
			CurrentAnimState = DefaultAnimState;
			CurrentAnimState.OnActivate(null);
		}

		CurrentAnimState.Update();
	}

	public void Reset()
	{
		/*   foreach (KeyValuePair<AgentActionFactory.E_Type, AnimState> pair in AnimStates)
        {
            AnimState state = pair.Value;
            state.OnDeactivate();
            state.SetFinished(true);
        }
      * */

		if (CurrentAnimState != null && CurrentAnimState.IsFinished() == false)
		{
			CurrentAnimState.SetFinished(true);
			CurrentAnimState.Reset();
		}
	}

	public bool DoAction(AgentAction action)
	{
		if (CurrentAnimState.HandleNewAction(action) == true)
		{
			//Debug.Log("AC - Do Action " + action.ToString());
			NextAnimState = null;
			return true;
		}
		else
		{
			if (AnimStates.ContainsKey(action.Type))
			{
				NextAnimState = AnimStates[action.Type];
				SwitchToNewStage(action);
				return true;
			}
		}
		return false;
	}

	protected void SwitchToNewStage(AgentAction action)
	{
		if (NextAnimState != null)
		{
			CurrentAnimState.Release();

			CurrentAnimState.OnDeactivate();
			CurrentAnimState = NextAnimState;

			CurrentAnimState.OnActivate(action);

			NextAnimState = null;
		}
	}
}
