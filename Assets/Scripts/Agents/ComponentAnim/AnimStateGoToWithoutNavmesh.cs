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

public class AnimStateGoToWithoutNavmesh : AnimState
{
	AgentActionGoTo Action;
	float MaxSpeed;
	string AnimName;

	Quaternion FinalRotation = new Quaternion();
	Quaternion StartRotation = new Quaternion();
	float RotationProgress;

	public AnimStateGoToWithoutNavmesh(Animation anims, AgentHuman owner)
					: base(anims, owner)
	{
	}

	public override void OnActivate(AgentAction action)
	{
		// Time.timeScale = 0.1f;
		base.OnActivate(action);

		AnimName = null;
		PlayAnim(Action.Motion);
	}

	public override void OnDeactivate()
	{
		Owner.BlackBoard.MotionType = E_MotionType.None;
		Owner.BlackBoard.MoveDir = Vector3.zero;
		Owner.BlackBoard.Speed = 0;

		Action.SetSuccess();
		Action = null;

		base.OnDeactivate();

		// Time.timeScale = 1;
	}

	public override void Reset()
	{
		Action.SetSuccess();
		Action = null;

		base.Reset();
	}

	public override void Update()
	{
		//Debug.DrawLine(OwnerTransform.position + new Vector3(0, 1, 0), Action.FinalPosition + new Vector3(0, 1, 0));

		float dist = (Action.FinalPosition - Transform.position).sqrMagnitude;
		Vector3 dir;

		//if (Owner.debugAnims) Debug.Log(Time.timeSinceLevelLoad + " " + "Speed " + Owner.BlackBoard.Speed + " Max Speed " + Owner.BlackBoard.MaxWalkSpeed);

		if (dist < 1.5f*1.5f)
			MaxSpeed = Owner.BlackBoard.RealMaxWalkSpeed;

		RotationProgress += Time.deltaTime*12;
		RotationProgress = Mathf.Min(RotationProgress, 1);
		Quaternion q = Quaternion.Slerp(StartRotation, FinalRotation, RotationProgress);
		Owner.Transform.rotation = q;

		/*if (Quaternion.Angle(q, FinalRotation) > 40.0f)
            return;*/

		// Smooth the speed based on the current target direction
		float curSmooth = Owner.BlackBoard.BaseSetup.SpeedSmooth*Time.deltaTime;

		Owner.BlackBoard.Speed = Mathf.Lerp(Owner.BlackBoard.Speed, MaxSpeed, curSmooth);

		dir = Action.FinalPosition - Transform.position;
		dir.y = 0;
		dir.Normalize();
		Owner.BlackBoard.MoveDir = dir;

		// MOVE
		if (Move(Owner.BlackBoard.MoveDir*Owner.BlackBoard.Speed*Time.deltaTime) == false)
		{
			Release();
		}
		else if ((Action.FinalPosition - Transform.position).sqrMagnitude < 0.3f*0.3f)
		{
			Release();
		}
		else
		{
			E_MotionType motion = GetMotionType();

			if (motion != Owner.BlackBoard.MotionType)
				PlayAnim(motion);
		}
	}

	public override bool HandleNewAction(AgentAction action)
	{
		if (action is AgentActionGoTo)
		{
			if (Action != null)
				Action.SetSuccess();

			SetFinished(false); // just for sure, if we already finish in same tick

			Initialize(action);

			return true;
		}

		if (action is AgentActionReload)
		{
			string s = Owner.AnimSet.GetWeaponAnim(E_WeaponAction.Reload);
			Animation[s].layer = 2;
			Animation[s].blendMode = AnimationBlendMode.Additive;
			Blend(s, 0.1f);

			action.SetSuccess();

			return true;
		}

		return false;
	}

	void PlayAnim(E_MotionType motion)
	{
		Owner.BlackBoard.MotionType = motion;
		Owner.BlackBoard.MoveType = Action.MoveType;

		AnimName = Owner.AnimSet.GetMoveAnim();

		CrossFade(AnimName, 0.2f, PlayMode.StopSameLayer);
	}

	E_MotionType GetMotionType()
	{
		if (Owner.BlackBoard.Speed > Owner.BlackBoard.RealMaxWalkSpeed*1.5f)
			return E_MotionType.Run;

		return E_MotionType.Walk;
	}

	protected override void Initialize(AgentAction action)
	{
		base.Initialize(action);

		Action = action as AgentActionGoTo;

		Vector3 dir;

		dir = Action.FinalPosition - Owner.Transform.position;
		dir.y = 0;
		dir.Normalize();

		StartRotation = Owner.Transform.rotation;

		if (dir != Vector3.zero)
		{
			if (Action.MoveType == E_MoveType.Forward)
				FinalRotation.SetLookRotation(dir);
			else if (Action.MoveType == E_MoveType.Backward)
				FinalRotation.SetLookRotation(-dir);
		}

		Owner.BlackBoard.MotionType = GetMotionType();
		Owner.BlackBoard.MoveType = Action.MoveType;

		if (Action.Motion == E_MotionType.Run)
			MaxSpeed = Owner.BlackBoard.RealMaxRunSpeed; //Owner.BlackBoard.BaseSetup.MaxRunSpeed;
		else
			MaxSpeed = Owner.BlackBoard.RealMaxWalkSpeed; //Owner.BlackBoard.BaseSetup.MaxWalkSpeed;

		RotationProgress = 0;
	}
}
