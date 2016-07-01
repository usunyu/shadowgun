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

public class AnimStateUseItem : AnimState
{
	AgentActionUseItem Action = null;

	float EndOfStateTime;
	float ThrowTime;
	string AnimName = null;
	Transform Hand;

	public AnimStateUseItem(Animation anims, AgentHuman owner)
					: base(anims, owner)
	{
	}

	public override void OnActivate(AgentAction action)
	{
//		Debug.Log ("AnimStateUseItem.OnActivate(), time=" + Time.timeSinceLevelLoad + ", BlackBoard.KeepMotion=" + Owner.BlackBoard.KeepMotion);

		base.OnActivate(action);

		Owner.BlackBoard.Desires.WeaponTriggerOn = false;
		Owner.BlackBoard.Desires.WeaponTriggerUp = false;
		Owner.BlackBoard.Desires.WeaponTriggerUpDisabled = true;
		Owner.BlackBoard.Desires.MeleeTriggerOn = false;
		Owner.BlackBoard.ReactOnHits = false;
		Owner.BlackBoard.BusyAction = true;

		//THROW_RUN
		if (Owner.BlackBoard.KeepMotion == false) //beny: due to 'UseItem while Move' feature
		{
			Owner.BlackBoard.MotionType = E_MotionType.None;
			Owner.BlackBoard.MoveDir = Vector3.zero;
			Owner.BlackBoard.Speed = 0;
		}
	}

	public override void OnDeactivate()
	{
		ThrowTime = 0;
		Owner.BlackBoard.ReactOnHits = true;
		Owner.BlackBoard.BusyAction = false;
		Owner.BlackBoard.KeepMotion = false;

		Action.SetSuccess();
		Action = null;
		base.OnDeactivate();
	}

	public override void Reset()
	{
		ThrowTime = 0;

		Owner.BlackBoard.ReactOnHits = true;
		Owner.BlackBoard.BusyAction = false;

		Animation.Stop(AnimName);

		Action.SetSuccess();
		Action = null;

		base.Reset();
	}

	public override void Update()
	{
		if (Action.Throw == false && ThrowTime <= Time.timeSinceLevelLoad)
			Action.Throw = true;

		//THROW_RUN
		DoMove();

		if (EndOfStateTime <= Time.timeSinceLevelLoad)
			Release();
	}

	E_MoveType GetMoveType()
	{
		Vector2 bodyForward = new Vector2(Transform.forward.x, Transform.forward.z);
		Vector2 bodyRight = new Vector2(Transform.right.x, Transform.right.z);

		Vector2 moveDir = new Vector2(Owner.BlackBoard.Desires.MoveDirection.x, Owner.BlackBoard.Desires.MoveDirection.z);

		float a = Vector2.Angle(bodyForward, moveDir);
		float b = Vector2.Angle(bodyRight, moveDir);

		//  Debug.Log("forward " + a + " right " + b + " " + Owner.BlackBoard.Desires.MoveDirection);

		if (a <= 45)
			return E_MoveType.Forward;
		else if (a > 135)
			return E_MoveType.Backward;
		else if (b < 90)
			return E_MoveType.StrafeRight;
		else
			return E_MoveType.StrafeLeft;
	}

	E_MotionType GetMotionType()
	{
//		if (Owner.BlackBoard.Speed > Owner.BlackBoard.BaseSetup.MaxSprintSpeed * 0.9f)
		if (Owner.CanSprint && Owner.BlackBoard.Speed > Owner.BlackBoard.RealMaxSprintSpeed*0.9f)
			return E_MotionType.Sprint;

		if (Owner.BlackBoard.Speed > Owner.BlackBoard.RealMaxWalkSpeed*1.5f)
			return E_MotionType.Run;

		return E_MotionType.Walk;
	}

	void PlayMoveAnim()
	{
		E_MotionType old = Owner.BlackBoard.MotionType;
		Owner.BlackBoard.MotionType = GetMotionType();
		Owner.BlackBoard.MoveType = GetMoveType();

		if (Owner.IsOwner)
		{
			float fov = GameCamera.Instance.DefaultFOV;
			if (old != E_MotionType.Sprint && Owner.BlackBoard.MotionType == E_MotionType.Sprint)
			{
				GameCamera.Instance.SetFov(fov*0.9f, 60);
			}
			else if (old == E_MotionType.Sprint && Owner.BlackBoard.MotionType != E_MotionType.Sprint)
			{
				GameCamera.Instance.SetFov(fov, 60);
			}
		}

		string AnimName = Owner.AnimSet.GetMoveAnim();
		bool playing = Animation.IsPlaying(AnimName);

		if (!playing /*|| (!playing && force)*/)
		{
			CrossFade(AnimName, 0.28f, PlayMode.StopSameLayer);
		}
	}

	void PlayIdleAnim()
	{
		string AnimName = Owner.AnimSet.GetIdleAnim();

		if (Animation.IsPlaying(AnimName) == false)
		{
//			Debug.Log ("AnimStateUseItem.PlayIdleAnim(), Desires.MoveDirection=" + Owner.BlackBoard.Desires.MoveDirection.ToString("F4") + ", velocity=" + Owner.BlackBoard.Velocity.ToString("F4") + ", time=" + Time.timeSinceLevelLoad);

			if (Owner.IsInCover)
				CrossFade(AnimName, 0.2f, PlayMode.StopSameLayer);
			else
				CrossFade(AnimName, 0.25f, PlayMode.StopSameLayer);
		}

		if (Owner.BlackBoard.Speed > 0)
		{
			Owner.BlackBoard.Speed = Mathf.Lerp(Owner.BlackBoard.Speed, 0, Time.deltaTime*10);
// 			Debug.Log ("AnimStateUseItem.PlayIdleAnim(), Speed=" + Owner.BlackBoard.Speed.ToString("F4") + ", time=" + Time.timeSinceLevelLoad);
		}
	}

	void DoMove()
	{
		if (Owner.BlackBoard.Desires.MoveDirection == Vector3.zero)
		{
			PlayIdleAnim();
			return;
		}

		if (Owner.IsOwner)
		{
			float MaxSpeed;

			//compute max speed
			if (Owner.BlackBoard.Desires.Sprint && Vector3.Dot(Owner.BlackBoard.Desires.MoveDirection, Owner.Forward) > 0.4f &&
				Owner.WeaponComponent.GetCurrentWeapon().IsBusy() == false)
			{
				MaxSpeed = Owner.BlackBoard.RealMaxSprintSpeed; //Owner.BlackBoard.BaseSetup.MaxSprintSpeed;
				Owner.BlackBoard.MoveDir = Owner.Forward; // forward only
			}
			else
			{
				if (Owner.BlackBoard.BaseSetup.UseMoveSpeedModifier)
					MaxSpeed = Mathf.Max(Owner.BlackBoard.RealMaxWalkSpeed, Owner.BlackBoard.RealMaxRunSpeed*Owner.BlackBoard.Desires.MoveSpeedModifier);
				else
					MaxSpeed = Owner.BlackBoard.RealMaxRunSpeed;

				Owner.BlackBoard.MoveDir = Owner.BlackBoard.Desires.MoveDirection;
			}

			// Smooth the speed based on the current target direction
			float curSmooth = Owner.BlackBoard.BaseSetup.SpeedSmooth*Time.deltaTime;

			//compute new speed 
			Owner.BlackBoard.Speed = Mathf.Lerp(Owner.BlackBoard.Speed, MaxSpeed, curSmooth);
		}
		else
			Owner.BlackBoard.MoveDir = Owner.BlackBoard.Desires.MoveDirection;

		//
		PlayMoveAnim();
	}

	public override bool HandleNewAction(AgentAction action)
	{
		if (action is AgentActionPlayAnim)
		{
			if (Action != null)
				action.SetFailed();
		}
		return false;
	}

	protected override void Initialize(AgentAction action)
	{
		base.Initialize(action);

		Action = action as AgentActionUseItem;

		AnimName = Owner.AnimSet.GetGadgetAnim(Owner.BlackBoard.Desires.Gadget, Action.CoverPose, Action.CoverDirection);

		if ((AnimName == null) || (Animation[AnimName] == null))
		{
			Action.SetFailed();
			Action = null;
			Release();
			return;
		}

		// play owner anims
		//THROW_RUN
//		if ( Owner.BlackBoard.MotionType != E_MotionType.Walk && Owner.BlackBoard.MotionType != E_MotionType.Run )		//allow movement while throwing
		if (Owner.BlackBoard.KeepMotion == false) //beny: due to 'UseItem while Move' feature
		{
			Animation[AnimName].layer = 3;
			CrossFade(AnimName, 0.2f, PlayMode.StopAll);
			Animation[AnimName].speed = 1.4f;
			//Animation.Play(animName, AnimationPlayMode.Stop);
			EndOfStateTime = Animation[AnimName].length*0.7f + Time.timeSinceLevelLoad;
		}
		else //movement throw
		{
			Animation[AnimName].layer = 3;
//			Animation[AnimName].blendMode = AnimationBlendMode.Additive;
			Blend(AnimName, 0.2f);
			Animation[AnimName].speed = 1.4f;

			EndOfStateTime = Animation[AnimName].length*0.8f/Animation[AnimName].speed + Time.timeSinceLevelLoad;

//			Debug.Log ("ThrowRun");
		}

		if (Owner.IsServer)
			ThrowTime = 0.5f/Animation[AnimName].speed + Time.timeSinceLevelLoad;

		ItemSettings s = ItemSettingsManager.Instance.Get(Owner.BlackBoard.Desires.Gadget);

		if (s && s.ItemBehaviour == E_ItemBehaviour.Place)
			Owner.SoundPlay(Owner.UseSound);
		else
			Owner.SoundPlay(Owner.ThrowSound);
	}

	public override void HandleAnimationEvent(E_AnimEvent animEvent)
	{
		/*Debug.Log("HandleAnimationEvent " + animEvent);
        Animation[AnimName].time = 0;
        EndOfStateTime = (Animation[AnimName].length * 0.9f) + Time.timeSinceLevelLoad;*/
	}
}
