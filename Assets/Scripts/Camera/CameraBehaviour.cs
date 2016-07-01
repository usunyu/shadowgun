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

public class CameraBehaviour : MonoBehaviour
{
	enum E_State
	{
		ThirdPerson,
		Cover,
		Knockdown,
		Death
	}

	public Transform CameraOrigin = null;

	public AgentHuman Owner { get; private set; }
	AgentAction WaitForEndOfAction;

	public CameraState State { get; private set; }

	Dictionary<E_State, CameraState> States = new Dictionary<E_State, CameraState>();

	// Use this for initialization
	void Awake()
	{
		Owner = GetComponent<AgentHuman>();

//		CameraOrigin = transform;
		if (!CameraOrigin) //is it filled in editor props by user?
		{
			CameraOrigin = transform.FindChildByName("CameraTargetDir");
			if (!CameraOrigin)
				CameraOrigin = transform;
		}

		//
		States.Add(E_State.ThirdPerson, new CameraState3RD(Owner));
		States.Add(E_State.Cover, new CameraStateCover(Owner));
		States.Add(E_State.Knockdown, new CameraStateKnockdown(Owner));
		States.Add(E_State.Death, new CameraStateDeath(Owner));

		State = States[E_State.ThirdPerson];

		Owner.BlackBoard.ActionHandler += HandleAction;
	}

	public Transform GetDesiredCameraTransform()
	{
		if (State != null)
			return State.GetDesiredCameraTransform();

		return null;
	}

	public void Activate()
	{
		State = States[E_State.ThirdPerson];
		State.Activate(transform);
		WaitForEndOfAction = null;
	}

	public void HandleAction(AgentAction a)
	{
		if (a is AgentActionCoverEnter)
		{
			SwitchState(E_State.Cover);
		}
		else if (a is AgentActionCoverLeave)
		{
			WaitForEndOfAction = a;
		}
		else if (a is AgentActionDeath)
		{
			SwitchState(E_State.Death);
		}
		/*else if( a is AgentActionKnockdown ) // enable this to use special knockdown camera
		{
			SwitchState( E_State.Knockdown );
			
			WaitForEndOfAction = a;
		}*/
	}

	void SwitchState(E_State InState)
	{
		if (null != State)
		{
			State.Deactivate();
		}

		State = States[InState];

		State.Activate(Owner.Transform);
	}

	void Update()
	{
		if (WaitForEndOfAction != null && WaitForEndOfAction.IsActive() == false)
		{
			SwitchState(E_State.ThirdPerson);

			WaitForEndOfAction = null;
		}
	}
}
