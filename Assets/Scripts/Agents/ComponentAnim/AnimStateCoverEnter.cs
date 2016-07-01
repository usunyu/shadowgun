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

public class AnimStateCoverEnter : AnimState
{
	AgentActionCoverEnter Action;

	Vector3 StartPosition;
	Vector3 FinalPosition;
	float CurrentMoveTime;
	float MoveTime;
	float EndOfStateTime;
	float BlockEndTime;

	bool PositionOK = false;

	public AnimStateCoverEnter(Animation anims, AgentHuman owner)
					: base(anims, owner)
	{
	}

	public override void OnActivate(AgentAction action)
	{
		base.OnActivate(action);

		Owner.BlackBoard.ReactOnHits = false;
		Owner.BlackBoard.BusyAction = true;
		Owner.BlackBoard.Desires.WeaponTriggerOn = false;
		Owner.BlackBoard.Desires.WeaponTriggerUp = false;
		Owner.BlackBoard.Desires.WeaponTriggerUpDisabled = true;
		Owner.BlackBoard.Desires.MeleeTriggerOn = false;
		//Time.timeScale = .1f;
	}

	public override void OnDeactivate()
	{
		// ALWAYS Success
		Owner.BlackBoard.ReactOnHits = true;
		Owner.BlackBoard.BusyAction = false;

		Owner.BlackBoard.Cover = Action.Cover;
		Owner.BlackBoard.CoverPose = Action.Cover.IsStandAllowed ? E_CoverPose.Stand : E_CoverPose.Crouch;

		if (Owner.BlackBoard.Desires.CoverPosition == E_CoverDirection.Unknown)
			Owner.BlackBoard.CoverPosition = E_CoverDirection.Middle; // player shit
		else
			Owner.BlackBoard.CoverPosition = Owner.BlackBoard.Desires.CoverPosition;

		if (Owner.BlackBoard.CoverPosition == E_CoverDirection.Left || Owner.BlackBoard.CoverPosition == E_CoverDirection.Right)
			Owner.WorldState.SetWSProperty(E_PropKey.CoverState, E_CoverState.Edge);
		else
			Owner.WorldState.SetWSProperty(E_PropKey.CoverState, E_CoverState.Middle);

		//      Time.timeScale = 1;
		Action.SetSuccess();
		Action = null;
		base.OnDeactivate();
	}

	public override void Reset()
	{
		// ALWAYS Success
		Owner.BlackBoard.ReactOnHits = true;
		Owner.BlackBoard.BusyAction = false;

		//      Time.timeScale = 1;
		Action.SetSuccess();
		Action = null;

		base.Reset();
	}

	public override void Update()
	{
		if (PositionOK == false)
		{
			CurrentMoveTime += Time.deltaTime;
			if (CurrentMoveTime >= MoveTime)
			{
				CurrentMoveTime = MoveTime;
				PositionOK = true;
			}

			float progress = CurrentMoveTime/MoveTime;
			Owner.Transform.position = Mathfx.Hermite(StartPosition, FinalPosition, progress);
		}

		if (EndOfStateTime <= Time.timeSinceLevelLoad)
			Release();
	}

	public override bool HandleNewAction(AgentAction action)
	{
		if (action is AgentActionCoverEnter)
		{
			if (Action != null)
			{
				Debug.LogError(ToString() + ": Second action is not allowed");
				action.SetFailed();
				return true;
			}

			Initialize(action);

			return true;
		}
		else if (action is AgentActionKnockdown)
		{
			action.SetSuccess();

			return true;
		}

		return false;
	}

	protected override void Initialize(AgentAction action)
	{
		base.Initialize(action);

		Action = action as AgentActionCoverEnter;

		CurrentMoveTime = 0;
		StartPosition = Transform.position;

		string AnimName = Owner.AnimSet.GetCoverAnim(AnimSet.E_CoverAnim.Enter,
													 Action.Cover.IsStandAllowed ? E_CoverPose.Stand : E_CoverPose.Crouch,
													 E_CoverDirection.Middle);

		CrossFade(AnimName, 0.15f, PlayMode.StopAll);
		Owner.SetDominantAnimName(AnimName);

		if (Owner.BlackBoard.Desires.CoverPosition == E_CoverDirection.Unknown)
			FinalPosition = Action.Cover.GetNearestPointOnCover(Owner.Position);
		else if (Owner.BlackBoard.Desires.CoverPosition == E_CoverDirection.Left)
			FinalPosition = Action.Cover.LeftEdge;
		else if (Owner.BlackBoard.Desires.CoverPosition == E_CoverDirection.Right)
			FinalPosition = Action.Cover.RightEdge;
		else if (Owner.BlackBoard.Desires.CoverPosition == E_CoverDirection.Middle)
			FinalPosition = Action.Cover.Transform.position;

		Animation[AnimName].speed = 1.2f;
		MoveTime = 0.3f;
		EndOfStateTime = 0.3f + Time.timeSinceLevelLoad;

		PositionOK = false;

		Owner.BlackBoard.MotionType = E_MotionType.None;
	}
}
