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

public class AnimStateCoverMove : AnimState
{
	AgentActionCoverMove Action;
	float MaxSpeed;

	public AnimStateCoverMove(Animation anims, AgentHuman owner)
					: base(anims, owner)
	{
	}

	public override void OnActivate(AgentAction action)
	{
		Owner.BlackBoard.ReactOnHits = false;
		// Time.timeScale = 0.1f;
		base.OnActivate(action);
	}

	public override void OnDeactivate()
	{
		Owner.BlackBoard.ReactOnHits = true;
		if (Action != null)
		{
			Action.SetSuccess();
			Action = null;
		}
		Owner.BlackBoard.Speed = 0;
		base.OnDeactivate();
		// Time.timeScale = 1;
	}

	public override void Reset()
	{
		Action.SetSuccess();
		Action = null;

		base.Reset();
	}

	protected override void Initialize(AgentAction action)
	{
		base.Initialize(action);

		Action = action as AgentActionCoverMove;

		if (Owner.BlackBoard.BaseSetup.UseMoveSpeedModifier)
		{
			MaxSpeed = Owner.BlackBoard.BaseSetup.MaxCoverSpeed*Action.Speed;
		}
		else
		{
			// don't care about input power
			MaxSpeed = Owner.BlackBoard.BaseSetup.MaxCoverSpeed;
		}

		Owner.BlackBoard.MotionType = GetMotionType();
		Owner.BlackBoard.MoveType = GetMoveType();
	}

	public override void Update()
	{
		//if (Owner.debugAnims) Debug.Log(Time.timeSinceLevelLoad + " " + "Speed " + Owner.BlackBoard.Speed + " Max Speed " + Owner.BlackBoard.MaxWalkSpeed);
		if (Action.IsActive() == false)
		{
			Release();
			return;
		}

		// Smooth the speed based on the current target direction
		float curSmooth = Owner.BlackBoard.BaseSetup.SpeedSmooth*Time.deltaTime;

		Owner.BlackBoard.Speed = Mathf.Lerp(Owner.BlackBoard.Speed, MaxSpeed, curSmooth);
		Owner.BlackBoard.MoveDir = Owner.BlackBoard.Desires.MoveDirection;

		UpdateMoveAnim();
	}

	public override bool HandleNewAction(AgentAction action)
	{
		if (action is AgentActionCoverMove)
		{
			if (Action != null)
				Action.SetSuccess();

			SetFinished(false); // just for sure, if we already finish in same tick

			Initialize(action);

			return true;
		}

		if (action is AgentActionInjury)
		{
			PlayInjuryAnimation(action as AgentActionInjury);
			return true;
		}

		if (action is AgentActionIdle)
		{
			action.SetSuccess();

			SetFinished(true);
		}

		if (action is AgentActionKnockdown)
		{
			action.SetSuccess();

			return true;
		}

		return false;
	}

	void PlayMoveAnim(E_MotionType motion)
	{
		Owner.BlackBoard.MotionType = motion;
		Owner.BlackBoard.MoveType = GetMoveType();

		string anim = Owner.AnimSet.GetMoveAnim();

		CrossFade(anim, 0.2f, PlayMode.StopSameLayer);
		Owner.SetDominantAnimName(anim);
	}

	void UpdateMoveAnim()
	{
		Owner.BlackBoard.MotionType = GetMotionType();
		Owner.BlackBoard.MoveType = GetMoveType();

		string anim = Owner.AnimSet.GetMoveAnim();

		Animation[anim].speed = 1.8f;

		if (Animation.IsPlaying(anim) == false)
		{
			CrossFade(anim, 0.2f, PlayMode.StopSameLayer);
		}

		Owner.SetDominantAnimName(anim);
	}

	E_MoveType GetMoveType()
	{
		Vector2 bodyRight = new Vector2(Transform.right.x, Transform.right.z);
		Vector2 moveDir = new Vector2(Owner.BlackBoard.Desires.MoveDirection.x, Owner.BlackBoard.Desires.MoveDirection.z);

		float b = Vector2.Angle(bodyRight, moveDir);

		// Debug.Log("forward " + a + " right " + b);
		if (b < 45)
			return E_MoveType.StrafeRight;
		else
			return E_MoveType.StrafeLeft;
	}

	E_MotionType GetMotionType()
	{
		if (Owner.BlackBoard.Speed > Owner.BlackBoard.RealMaxWalkSpeed*1.5f)
			return E_MotionType.Run;

		return E_MotionType.Walk;
	}
}
