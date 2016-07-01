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

public class AnimStateMove : AnimState
{
	AgentActionMove Action;

	// watched reload action
	float TimeToFinishWeaponAction;

	float MaxSpeed;

	string AnimNameBase;
	string AnimNameDown;
	string AnimNameUp;

	float BlendUp;
	float BlendDown;

	public AnimStateMove(Animation anims, AgentHuman owner)
					: base(anims, owner)
	{
	}

	public override void OnActivate(AgentAction action)
	{
//		Debug.Log ("AnimStateMove.OnActivate(), time=" + Time.timeSinceLevelLoad + ", BlackBoard.KeepMotion=" + Owner.BlackBoard.KeepMotion);

		// Time.timeScale = 0.1f;
		base.OnActivate(action);
	}

	public override void OnDeactivate()
	{
		//THROW_RUN, THROW_RUN_2
		if (Owner.BlackBoard.KeepMotion == false) //beny: due to 'UseItem while Move' feature
		{
			Owner.BlackBoard.MotionType = E_MotionType.None;
			Owner.BlackBoard.MoveDir = Vector3.zero;
			Owner.BlackBoard.Speed = 0;
		}

		Action.SetSuccess();
		Action = null;

		if (Owner.BlackBoard.AimAnimationsEnabled)
		{
			Animation[AnimNameUp].weight = 0;
			Animation[AnimNameDown].weight = 0;

			Animation.Stop(AnimNameUp);
			Animation.Stop(AnimNameDown);
		}

		TimeToFinishWeaponAction = 0;

		base.OnDeactivate();
		// Time.timeScale = 1;
	}

	public override void Reset()
	{
		if (Owner.BlackBoard.AimAnimationsEnabled)
		{
			Animation[AnimNameUp].weight = 0;
			Animation[AnimNameDown].weight = 0;

			Animation.Stop(AnimNameUp);
			Animation.Stop(AnimNameDown);
		}
		Action.SetSuccess();
		Action = null;

		TimeToFinishWeaponAction = 0;

		base.Reset();
	}

	public override void Update()
	{
		if (TimeToFinishWeaponAction > 0 && TimeToFinishWeaponAction < Time.timeSinceLevelLoad)
		{
			TimeToFinishWeaponAction = 0;
		}

		if (Action.IsActive() == false)
		{
			Release();
			return;
		}

		if (Owner.BlackBoard.Desires.MoveDirection == Vector3.zero)
			return;

		if (Owner.IsOwner)
		{
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

		PlayMoveAnim(false);
		if (Owner.BlackBoard.AimAnimationsEnabled)
		{
			UpdateBlendValues();
			UpdateBlendedAnims();
		}
	}

	public override bool HandleNewAction(AgentAction action)
	{
		if (action is AgentActionMove)
		{
			if (Action != null)
				Action.SetSuccess();

			SetFinished(false); // just for sure, if we already finish in same tick

			Initialize(action);

			return true;
		}

		if (action is AgentActionIdle)
		{
			action.SetSuccess();

			SetFinished(true);

			return true;
		}
		else if (action is AgentActionAttack)
		{
			string s = Owner.AnimSet.GetWeaponAnim(E_WeaponAction.Fire);
			Animation[s].blendMode = AnimationBlendMode.Additive;

			if (Animation.IsPlaying(s))
				Animation[s].time = 0;
			else
				Animation.Blend(s, 1, 0.1f);

			action.SetSuccess();

			return true;
		}
		else if (action is AgentActionInjury)
		{
			PlayInjuryAnimation(action as AgentActionInjury);
			return true;
		}
		else if (action is AgentActionReload)
		{
			string s = Owner.AnimSet.GetWeaponAnim(E_WeaponAction.Reload);

			AnimationState State = Animation[s];
			if (null != State)
			{
				State.layer = 2;
				State.blendMode = AnimationBlendMode.Blend;

				// What the magic constant 0.3f is good for? Is it here to cause the weapon to be ready little bit sooner
				// before the animation ends? Why?
				TimeToFinishWeaponAction = Time.timeSinceLevelLoad + State.length - 0.3f;
				Blend(s, 0.25f);
			}
			else
			{
				// TODO: It use to happen (not very often) that the state is NULL. This for example happens when s
				// is set to "ReloadPlasma". The plasma weapon does not have any reload. The question is what causes the bug?
				// A cheated client? Or a latency on the network? Another question is what to do in such case?
				// To fail an action sounds like a correct solution.


				action.SetFailed();
				return true;
			}

			action.SetSuccess();
			return true;
		}

		return false;
	}

	void PlayMoveAnim(bool force)
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

		AnimNameBase = Owner.AnimSet.GetMoveAnim();
		bool playing = Animation.IsPlaying(AnimNameBase);

		//speed up the anim when the movement is faster and vice versa
		float mod = Owner.RunSpeedModifier;

		//clamp the max speed
		if (mod > 1)
			mod = 1 + ((mod - 1)*0.5f);

		if ((Owner.BlackBoard.MotionType == E_MotionType.Run) && (Mathf.Approximately(Animation[AnimNameBase].speed, mod) == false))
		{
			Animation[AnimNameBase].speed = mod;

			if (Owner.debugAnims)
				Debug.Log("Run speed changed: " + mod + ", anim=" + AnimNameBase + ", time=" + Time.timeSinceLevelLoad);
		}

		//play anim
		if (!playing || (!playing && force))
		{
			CrossFade(AnimNameBase, 0.28f, PlayMode.StopSameLayer);
		}

		Owner.SetDominantAnimName(AnimNameBase);
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

	protected override void Initialize(AgentAction action)
	{
		base.Initialize(action);

		Action = action as AgentActionMove;

		PlayMoveAnim(true);

		if (Owner.BlackBoard.AimAnimationsEnabled)
		{
			AnimNameUp = Owner.AnimSet.GetAimAnim(E_AimDirection.Up, E_CoverPose.None, E_CoverDirection.Unknown);
			AnimNameDown = Owner.AnimSet.GetAimAnim(E_AimDirection.Down, E_CoverPose.None, E_CoverDirection.Unknown);

			Animation[AnimNameUp].wrapMode = WrapMode.ClampForever;
			Animation[AnimNameDown].wrapMode = WrapMode.ClampForever;

			Animation[AnimNameUp].blendMode = AnimationBlendMode.Additive;
			Animation[AnimNameUp].layer = 1;

			Animation[AnimNameDown].blendMode = AnimationBlendMode.Additive;
			Animation[AnimNameDown].layer = 1;

			UpdateBlendValues();

			Animation[AnimNameUp].time = 0.0333f;
			Animation[AnimNameDown].time = 0.0333f;

			Animation[AnimNameUp].weight = BlendUp;
			Animation[AnimNameDown].weight = BlendDown;

			Animation.Blend(AnimNameUp, BlendUp, 0);
			Animation.Blend(AnimNameDown, BlendDown, 0);
		}
	}

	void UpdateBlendValues()
	{
		if (Owner.IsInCover || Owner.BlackBoard.MotionType == E_MotionType.Sprint || TimeToFinishWeaponAction > 0)
		{
			BlendUp = 0;
			BlendDown = 0;
			return;
		}

		Quaternion r = Owner.BlackBoard.Desires.Rotation;
		r.SetLookRotation(Owner.BlackBoard.FireDir);
		//Vector3 bestAngles = Owner.BlackBoard.Desires.Rotation.eulerAngles;
		Vector3 bestAngles = r.eulerAngles;
		Vector3 currentAngles = Owner.Transform.rotation.eulerAngles;

		Vector3 diff = (bestAngles - currentAngles);

		if (diff.x > 180)
			diff.x -= 360;
		else if (diff.x < -180)
			diff.x += 360;

		float blendUp = diff.x > 0 ? 0 : -diff.x;
		float blendDown = diff.x < 0 ? 0 : diff.x;

		//Debug.Log(diff + " " + blendUp + " " + blendDown);

		BlendUp = Mathf.Min(1, blendUp/70.0f); // *Animation[AnimNameBase].weight;
		BlendDown = Mathf.Min(1, blendDown/50.0f); // *Animation[AnimNameBase].weight;
	}

	void UpdateBlendedAnims()
	{
		float speed = 10;
		Animation[AnimNameUp].weight = Mathf.Lerp(Animation[AnimNameUp].weight, BlendUp, Time.deltaTime*speed);
		Animation[AnimNameDown].weight = Mathf.Lerp(Animation[AnimNameDown].weight, BlendDown, Time.deltaTime*speed);
	}
}
