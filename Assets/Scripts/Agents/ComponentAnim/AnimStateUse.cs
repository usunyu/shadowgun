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

public class AnimStateUse : AnimState
{
	enum E_State
	{
		E_PREPARING_FOR_USE,
		E_USING,
	}

	AgentActionUse Action = null;
	InteractionObject InterObj;

	Quaternion FinalRotation;
	Quaternion StartRotation;
	Vector3 StartPosition;
	Vector3 FinalPosition;
	float MoveTime;
	float CurrentMoveTime;

	bool PositionOK = false;

	E_State State;

	public AnimStateUse(Animation anims, AgentHuman owner)
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

		Owner.BlackBoard.MotionType = E_MotionType.None;
		Owner.BlackBoard.MoveDir = Vector3.zero;
		Owner.BlackBoard.Speed = 0;

		//Time.timeScale = .1f;
	}

	public override void OnDeactivate()
	{
		Owner.BlackBoard.ReactOnHits = true;
		Owner.BlackBoard.BusyAction = false;

		Action.SetSuccess();
		Action = null;
		base.OnDeactivate();
	}

	public override void Reset()
	{
		Action.SetSuccess();
		Action = null;

		base.Reset();
	}

	public override void Update()
	{
		//       //Debug.DrawLine(OwnerTransform.position + new Vector3(0, 1, 0), OwnerTransform.position + Action.Direction + new Vector3(0, 1, 0));

		//Debug.Log("Update");
		if (State == E_State.E_PREPARING_FOR_USE && PositionOK == false)
		{
			CurrentMoveTime += Time.deltaTime;
			if (CurrentMoveTime >= MoveTime)
			{
				CurrentMoveTime = MoveTime;
				PositionOK = true;
			}

			float progress = Mathf.Min(1.0f, CurrentMoveTime/MoveTime);

			Owner.BlackBoard.Desires.Rotation = Quaternion.Lerp(StartRotation, FinalRotation, progress);

			Vector3 finalPos = Mathfx.Sinerp(StartPosition, FinalPosition, progress);
			if (Move(finalPos - Transform.position) == false)
				PositionOK = true;
		}

		if (State == E_State.E_PREPARING_FOR_USE && PositionOK)
		{
			State = E_State.E_USING;
			PlayAnim();
		}

		if (State == E_State.E_USING && Action.InterObj.IsInteractionFinished)
			Release();
	}

	public override void Release()
	{
		Transform.parent = null;

		base.Release();
	}

	public override bool HandleNewAction(AgentAction action)
	{
		if (action is AgentActionUse)
		{
			if (Action != null)
				action.SetFailed();
		}
		return false;
	}

	void PlayAnim()
	{
		// play owner anims
		//Debug.Log(Time.timeSinceLevelLoad + "play anim");

		if (Animation.GetClip(Action.InterObj.UserAnimationClip.name) == null)
			Animation.AddClip(Action.InterObj.UserAnimationClip, Action.InterObj.UserAnimationClip.name);

		CrossFade(Action.InterObj.GetUserAnimation(), 0.3f, PlayMode.StopSameLayer);

		//play anim on interaction object
		Action.InterObj.DoInteraction();

		//Debug.Log(animName + " " + Mathf.Max(time, time2));
		Owner.BlackBoard.MotionType = E_MotionType.None;

		//if (Action.InterObj.GetEntryTransform())
		//Transform.parent = Action.InterObj.GetEntryTransform();
	}

	protected override void Initialize(AgentAction action)
	{
		base.Initialize(action);

		Action = action as AgentActionUse;

		//  Debug.Log(Time.timeSinceLevelLoad + " Action.InterObj.UseTime " + Action.InterObj.UseTime);

		if (Action.InterObj.GetEntryTransform())
		{
			StartPosition = Transform.position;
			StartRotation = Transform.rotation;

			FinalRotation.SetLookRotation(Action.InterObj.GetEntryTransform().forward);
			FinalPosition = Action.InterObj.GetEntryTransform().position;

			CurrentMoveTime = 0;
			MoveTime = 0.2f;

			PositionOK = false;
		}
		else
		{
			PositionOK = true;
		}

		State = E_State.E_PREPARING_FOR_USE;
	}
}
