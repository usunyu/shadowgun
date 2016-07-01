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

class GOAPActionCoverJumpOverPlayer : GOAPAction
{
	AgentActionCoverLeave Action;
	Vector3 FinalPos;

	public GOAPActionCoverJumpOverPlayer(AgentHuman owner) : base(E_GOAPAction.CoverJumpOverPlayer, owner)
	{
	}

	public override void InitAction()
	{
		WorldEffects.SetWSProperty(E_PropKey.CoverState, E_CoverState.None);
		Cost = 1;
		Precedence = 10;
		Interruptible = false;
	}

	// Validates the context preconditions
	public override bool ValidateContextPreconditions(WorldState current, bool planning)
	{
		if (planning == false)
			return true;

		if (Owner.BlackBoard.Cover == null)
		{
			// Debug.Log(Time.timeSinceLevelLoad + "no cover ");
			return false;
		}

		if (Owner.BlackBoard.CoverPosition != E_CoverDirection.Middle)
		{
			// Debug.Log(Time.timeSinceLevelLoad + "no moddle ");
			return false;
		}

		if (Owner.BlackBoard.Cover.CoverFlags.Get(Cover.E_CoverFlags.UpCrouch) == false)
		{
			// Debug.Log(Time.timeSinceLevelLoad + "no up crouch cover ");
			return false;
		}

		if (Vector3.Dot(Owner.BlackBoard.Desires.MoveDirection, Owner.BlackBoard.Cover.Forward) < 0.4f)
		{
			// Debug.Log(Time.timeSinceLevelLoad + "bad dot " + Vector3.Dot(Owner.BlackBoard.Desires.MoveDirection, Owner.BlackBoard.Cover.Forward));
			return false;
		}

		if (Owner.BlackBoard.Cover.CanJumpOver == false && Owner.BlackBoard.Cover.CanJumpUp == false)
		{
			// Debug.Log(Time.timeSinceLevelLoad + "no opposite cover ");
			return false;
		}

		return true;
	}

	public override void Update()
	{
	}

	public override void Activate()
	{
		base.Activate();

		Action = AgentActionFactory.Create(AgentActionFactory.E_Type.CoverLeave) as AgentActionCoverLeave;

		if (Owner.BlackBoard.Cover.CanJumpUp)
			Action.TypeOfLeave = AgentActionCoverLeave.E_Type.JumpUp;
		else
			Action.TypeOfLeave = AgentActionCoverLeave.E_Type.Jump;

		Action.FinalViewDirection = Owner.BlackBoard.Cover.Forward;
		Action.Cover = Owner.BlackBoard.Cover;

		// Debug.Log(Action.TypeOfLeave);

		Owner.BlackBoard.ActionAdd(Action);
	}

	public override void Deactivate()
	{
		base.Deactivate();
	}

	public override bool IsActionComplete()
	{
		return Action != null && !Action.IsActive();
	}

	public override bool ValidateAction()
	{
		if (Action != null && Action.IsFailed() == true)
		{
			//UnityEngine.Debug.Log(this.ToString() + " not valid anymore !" + FinalPos.ToString());
			return false;
		}

		return true;
	}
}
