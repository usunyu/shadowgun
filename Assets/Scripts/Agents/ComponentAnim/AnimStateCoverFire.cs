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
using System.Collections;

public class AnimStateCoverFire : AnimState
{
	enum E_State
	{
		Start,
		Fire,
		End,
	}

	AgentActionCoverFire Action;
	AgentActionCoverFireCancel ActionCancel;
	float EndOfStateTime;
	E_State State;

	string AnimNameLeft;
	string AnimNameRight;
	string AnimNameUp;
	string AnimNameDown;
	string AnimNameBase;

	float BlendUp;
	float BlendDown;
	float BlendRight;
	float BlendLeft;

	public AnimStateCoverFire(Animation anims, AgentHuman owner)
					: base(anims, owner)
	{
	}

	public override void OnActivate(AgentAction action)
	{
		base.OnActivate(action);
		Owner.BlackBoard.BusyAction = true;

		Owner.BlackBoard.CoverFire = true;

		//Time.timeScale = 0.1f;
	}

	public override void OnDeactivate()
	{
		//Time.timeScale = 1;
		Owner.BlackBoard.BusyAction = false;

		Animation.Stop(AnimNameUp);
		Animation.Stop(AnimNameDown);
		Animation.Stop(AnimNameLeft);
		Animation.Stop(AnimNameRight);

		if (Owner.IsAlive)
			Owner.BlackBoard.CoverFire = false;

		//Debug.Log("deactivate " + Owner.BlackBoard.CoverFire);
		if (ActionCancel != null)
			ActionCancel.SetSuccess();

		ActionCancel = null;
		Action.SetSuccess();
		Action = null;
		base.OnDeactivate();
	}

	public override void Reset()
	{
		if (ActionCancel != null)
			ActionCancel.SetSuccess();

		if (Owner.IsOwner)
		{
			//WeaponBase	weapon = Owner.WeaponComponent.GetCurrentWeapon();

			GameCamera.Instance.Reset(0, 30);
		}

		Owner.BlackBoard.CoverFire = false;
		ActionCancel = null;

		Action.SetSuccess();
		Action = null;

		base.Reset();
	}

	public override void Update()
	{
		switch (State)
		{
		case E_State.Start:
			UpdateAim();
			break;
		case E_State.Fire:
			UpdateFire();
			break;
		case E_State.End:
			UpdateEnd();
			break;
		}

		UpdateBlendedAnims();
	}

	public override bool HandleNewAction(AgentAction action)
	{
		if (action is AgentActionCoverFire)
		{
			Debug.LogError(ToString() + ": Second action is not allowed");
			action.SetFailed();
			return true;
		}

		if (action is AgentActionCoverFireCancel)
		{
			ActionCancel = action as AgentActionCoverFireCancel;
			StartEnd();
			return true;
		}

		if (action is AgentActionInjury)
		{
			PlayInjuryAnimation(action as AgentActionInjury);
			return true;
		}

		if (action is AgentActionAttack)
		{
			string s = Owner.AnimSet.GetWeaponAnim(E_WeaponAction.Fire);

			Animation[s].blendMode = AnimationBlendMode.Additive;
			Animation[s].layer = 2;

			if (Animation.IsPlaying(s))
				Animation[s].time = 0;
			else
				Blend(s, 0.05f);

			action.SetSuccess();
			return true;
		}

		return false;
	}

	protected override void Initialize(AgentAction action)
	{
		base.Initialize(action);

		Action = action as AgentActionCoverFire;

		AnimNameBase = Owner.AnimSet.GetCoverAnim(AnimSet.E_CoverAnim.AimStart, Action.CoverPose, Action.CoverDirection);
		Animation[AnimNameBase].speed = 1.2f;

		EndOfStateTime = Animation[AnimNameBase].length*0.9f + Time.timeSinceLevelLoad;

		Owner.WeaponComponent.GetCurrentWeapon().SetBusy(Animation[AnimNameBase].length);

		Owner.BlackBoard.MotionType = E_MotionType.None;

		State = E_State.Start;

		AnimNameUp = Owner.AnimSet.GetAimAnim(E_AimDirection.Up, Action.CoverPose, Action.CoverDirection);
		AnimNameDown = Owner.AnimSet.GetAimAnim(E_AimDirection.Down, Action.CoverPose, Action.CoverDirection);
		AnimNameRight = Owner.AnimSet.GetAimAnim(E_AimDirection.Right, Action.CoverPose, Action.CoverDirection);
		AnimNameLeft = Owner.AnimSet.GetAimAnim(E_AimDirection.Left, Action.CoverPose, Action.CoverDirection);

		Animation[AnimNameUp].layer = 1;
		Animation[AnimNameDown].layer = 1;
		Animation[AnimNameRight].layer = 1;
		Animation[AnimNameLeft].layer = 1;

		//AnimName = Owner.AnimSet.GetCoverAnim(AnimSet.E_CoverAnim.AimLoop, Owner.BlackBoard.CoverPose, Owner.BlackBoard.CoverDirection);
		Animation[AnimNameBase].wrapMode = WrapMode.ClampForever;

		CrossFade(AnimNameBase, 0.15f, PlayMode.StopSameLayer);
		Owner.SetDominantAnimName(AnimNameBase);

		float time = Animation[AnimNameBase].length*0.9f;

		if (uLink.Network.isClient && Owner.NetworkView.isMine)
		{
			float newFOV = GameCamera.Instance.DefaultFOV;
			newFOV *= Owner.WeaponComponent.GetCurrentWeapon().CoverFireFovModificator;
			GameCamera.Instance.SetFov(newFOV, 60);
		}

		UpdateBlendValues();

		Animation[AnimNameUp].weight = 0;
		Animation[AnimNameDown].weight = 0;
		Animation[AnimNameRight].weight = 0;
		Animation[AnimNameLeft].weight = 0;

		Animation.Blend(AnimNameUp, BlendUp, time);
		Animation.Blend(AnimNameDown, BlendDown, time);
		Animation.Blend(AnimNameRight, BlendRight, time);
		Animation.Blend(AnimNameLeft, BlendLeft, time);

		Owner.WeaponComponent.DisableCurrentWeapon(time);
	}

	void UpdateAim()
	{
		if (EndOfStateTime <= Time.timeSinceLevelLoad)
		{
			State = E_State.Fire;
			Owner.BlackBoard.BusyAction = false;
			Action.SetSuccess();
		}
	}

	void UpdateFire()
	{
		UpdateBlendValues();
		UpdateBlendedAnims();
	}

	void StartEnd()
	{
		string AnimName = Owner.AnimSet.GetCoverAnim(AnimSet.E_CoverAnim.AimEnd, Action.CoverPose, Action.CoverDirection);

		//Debug.Log("start end " + AnimName);

		State = E_State.End;

		Animation[AnimName].speed = 1.3f;
		EndOfStateTime = Animation[AnimName].length*0.7f + Time.timeSinceLevelLoad;
		Owner.WeaponComponent.GetCurrentWeapon().SetBusy(Animation[AnimName].length);
		Owner.BlackBoard.CoverFire = false;

		CrossFade(AnimName, 0.15f, PlayMode.StopAll);
		Owner.SetDominantAnimName(AnimName);

		if (uLink.Network.isClient && Owner.NetworkView.isMine)
		{
			float newFOV = GameCamera.Instance.DefaultFOV;
			if (Owner.IsInCover)
				newFOV *= Owner.WeaponComponent.GetCurrentWeapon().CoverFovModificator;
			GameCamera.Instance.SetFov(newFOV, 60);
		}

		BlendUp = 0;
		BlendDown = 0;
		BlendRight = 0;
		BlendLeft = 0;

		Owner.BlackBoard.BusyAction = true;
		Owner.WeaponComponent.DisableCurrentWeapon(Animation[AnimName].length);
	}

	void UpdateEnd()
	{
		UpdateBlendedAnims();

		if (EndOfStateTime <= Time.timeSinceLevelLoad)
		{
			Release();
		}
	}

	void UpdateBlendValues()
	{
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

		if (diff.y > 180)
			diff.y -= 360;
		else if (diff.y < -180)
			diff.y += 360;

		float blendUp = diff.x > 0 ? 0 : -diff.x;
		float blendDown = diff.x < 0 ? 0 : diff.x;
		float blendRight = diff.y < 0 ? 0 : diff.y;
		float blendLeft = diff.y > 0 ? 0 : -diff.y;

		//  Debug.Log(diff + " " + blendRight + " " + blendLeft + " " + blendUp + " " + blendDown);

		if (Owner.BlackBoard.CoverPosition == E_CoverDirection.Right)
		{
			BlendUp = Mathf.Min(1, blendUp/Owner.BlackBoard.CoverSetup.RightMaxUp);
			BlendDown = Mathf.Min(1, blendDown/Owner.BlackBoard.CoverSetup.RightMaxDown);
			BlendRight = Mathf.Min(1, blendRight/Owner.BlackBoard.CoverSetup.RightMaxRight);
			BlendLeft = Mathf.Min(1, blendLeft/Owner.BlackBoard.CoverSetup.RightMaxLeft);
		}
		else if (Owner.BlackBoard.CoverPosition == E_CoverDirection.Left)
		{
			BlendUp = Mathf.Min(1, blendUp/Owner.BlackBoard.CoverSetup.LeftMaxUp);
			BlendDown = Mathf.Min(1, blendDown/Owner.BlackBoard.CoverSetup.LeftMaxDown);
			BlendRight = Mathf.Min(1, blendRight/Owner.BlackBoard.CoverSetup.LeftMaxRight);
			BlendLeft = Mathf.Min(1, blendLeft/Owner.BlackBoard.CoverSetup.LeftMaxLeft);
		}
		else
		{
			BlendUp = Mathf.Min(1, blendUp/Owner.BlackBoard.CoverSetup.CenterMaxUp);
			BlendDown = Mathf.Min(1, blendDown/Owner.BlackBoard.CoverSetup.CenterMaxDown);
			BlendRight = Mathf.Min(1, blendRight/Owner.BlackBoard.CoverSetup.CenterMaxRight);
			BlendLeft = Mathf.Min(1, blendLeft/Owner.BlackBoard.CoverSetup.CenterMaxLeft);

			// up animation with priority
			if (BlendUp > 0.0f)
			{
				BlendLeft = Mathf.Min(1 - BlendUp, BlendLeft);
				BlendRight = Mathf.Min(1 - BlendUp, BlendRight);
			}
		}

//        Debug.Log(BlendRight + " " + BlendLeft);
	}

	void UpdateBlendedAnims()
	{
		float speed = 15;
		Animation[AnimNameUp].weight = Mathf.Lerp(Animation[AnimNameUp].weight, BlendUp, Time.deltaTime*speed);
		Animation[AnimNameDown].weight = Mathf.Lerp(Animation[AnimNameDown].weight, BlendDown, Time.deltaTime*speed);
		Animation[AnimNameRight].weight = Mathf.Lerp(Animation[AnimNameRight].weight, BlendRight, Time.deltaTime*speed);
		Animation[AnimNameLeft].weight = Mathf.Lerp(Animation[AnimNameLeft].weight, BlendLeft, Time.deltaTime*speed);

		//  Debug.Log("Blend: diff " + diff + " right " + blendRight + " left " + blendLeft + " up " + blendUp + " down " + blendDown);
	}
}
