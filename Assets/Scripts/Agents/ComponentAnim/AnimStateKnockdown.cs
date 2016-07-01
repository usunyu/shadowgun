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

public class AnimStateKnockdown : AnimState
{
	enum E_State
	{
		Knockdown,
		Death,
	}

	AgentActionKnockdown Action;
	AgentActionDeath ActionDeath;

	Quaternion FinalRotation;
	Quaternion StartRotation;
	Vector3 StartPosition;
	Vector3 FinalPosition;
	float CurrentRotationTime;
	float RotationTime;
	float CurrentMoveTime;
	float MoveTime;
	float EndOfStateTime;

	bool RotationOk = false;
	bool PositionOK = false;

	E_State State;

	public AnimStateKnockdown(Animation anims, AgentHuman owner)
					: base(anims, owner)
	{
	}

	public override void OnActivate(AgentAction action)
	{
		//  Time.timeScale = 0.1f;
		base.OnActivate(action);

		Owner.BlackBoard.Desires.MeleeTriggerOn = false;
		Owner.BlackBoard.Desires.WeaponTriggerOn = false;
		Owner.BlackBoard.Desires.WeaponTriggerUp = false;

		Owner.BlackBoard.InKnockDown = true;
		Owner.BlackBoard.BusyAction = true;
		Owner.BlackBoard.MotionType = E_MotionType.Knockdown;
		Owner.BlackBoard.MoveDir = Vector3.zero;
		Owner.BlackBoard.Speed = 0;

		Owner.Stop(true);
	}

	public override void OnDeactivate()
	{
		//  Time.timeScale = 1.0f;
		if (ActionDeath != null)
			ActionDeath.SetSuccess();

		Owner.BlackBoard.InKnockDown = false;
		Owner.BlackBoard.BusyAction = false;
		ActionDeath = null;

		Action.SetSuccess();
		Action = null;

		Owner.BlackBoard.MotionType = E_MotionType.None;

		Owner.Stop(false);

		base.OnDeactivate();
	}

	public override void Reset()
	{
		if (ActionDeath != null)
			ActionDeath.SetSuccess();

		Owner.BlackBoard.InKnockDown = false;
		Owner.BlackBoard.BusyAction = false;

		ActionDeath = null;

		Action.SetSuccess();
		Action = null;

		base.Reset();
	}

	public override bool HandleNewAction(AgentAction action)
	{
		if (action as AgentActionKnockdown != null)
		{
			Debug.LogError("obsolete AgentActionKnockdown arrived");
			action.SetFailed();
			return true;
		}
		else if (action as AgentActionInjury != null)
		{
			action.SetSuccess();
			return true;
		}
		/*else if (action as AgentActionDeath != null)
         {
             ActionDeath = action as AgentActionDeath;
             //if (Owner.debugAnims == true) Debug.Log(Time.timeSinceLevelLoad + " " + this.ToString() + " Handle new action " + action.ToString());


             InitializeDeath();

             return true;
         }*/

		return false;
	}

	public override void Release()
	{
		SetFinished(true);
	}

	public override void Update()
	{
		if (State == E_State.Death)
			return;

		if (Owner.IsServer)
		{
			if (RotationOk == false)
			{
				CurrentRotationTime += Time.deltaTime;

				if (CurrentRotationTime >= RotationTime)
				{
					CurrentRotationTime = RotationTime;
					RotationOk = true;
				}

				float progress = CurrentRotationTime/RotationTime;
				Quaternion q = Quaternion.Lerp(StartRotation, FinalRotation, progress);
				Owner.Transform.rotation = q;
				Owner.BlackBoard.Desires.Rotation = q;
			}

			if (PositionOK == false)
			{
				CurrentMoveTime += Time.deltaTime;
				if (CurrentMoveTime >= MoveTime)
				{
					CurrentMoveTime = MoveTime;
					PositionOK = true;
				}

				if (CurrentMoveTime >= 0)
				{
					float progress = CurrentMoveTime/MoveTime;
					Vector3 finalPos = Mathfx.Sinerp(StartPosition, FinalPosition, progress);
					finalPos.y = Transform.position.y;
					if (Move(finalPos - Transform.position) == false)
						PositionOK = true;
				}
			}
			else
			{
				// just to be sure player is falling down 
				Move(Vector3.zero);
			}
		}

		if (State == E_State.Knockdown && EndOfStateTime <= Time.timeSinceLevelLoad)
			Release();
	}

	protected override void Initialize(AgentAction action)
	{
		base.Initialize(action);

		Action = action as AgentActionKnockdown;

		Owner.SoundPlay(Owner.KnockdownSound);

		string animName = Owner.AnimSet.GetKnockdownAnim(E_KnockdownState.Down);

		CrossFade(animName, 0.05f, PlayMode.StopSameLayer);
		Owner.SetDominantAnimName(animName);

		if (Owner.IsServer)
		{
			StartRotation = Transform.rotation;
			StartPosition = Transform.position;

			float angle = Vector3.Angle(Transform.forward, Action.Direction);

			FinalRotation.SetLookRotation(-Action.Direction);
			RotationTime = angle/250.0f;
			//FinalPosition = StartPosition + Action.Attacker.Forward * 2.0f;
			FinalPosition = BuildFinalPosition(StartPosition, Action.Direction);
			MoveTime = Animation[animName].length*0.3f;

			RotationOk = RotationTime == 0;
			PositionOK = MoveTime == 0;

			CurrentRotationTime = 0;
			CurrentMoveTime = -0.1f;
		}
		else
		{
			RotationOk = true;
			PositionOK = true;
		}

		EndOfStateTime = Time.timeSinceLevelLoad + Animation[animName].length*0.9f;

		State = E_State.Knockdown;
	}

	Vector3 BuildFinalPosition(Vector3 startPos, Vector3 direction)
	{
		Vector3 result = startPos + direction*2.0f;

		// todo : test collision against terrain
		return result;
	}

	void InitializeDeath()
	{
		string animName = Owner.AnimSet.GetKnockdownAnim(E_KnockdownState.Fatality);
		CrossFade(animName, 0.1f, PlayMode.StopSameLayer);

		EndOfStateTime = Time.timeSinceLevelLoad + Animation[animName].length*0.9f;
		ActionDeath.SetSuccess();
		State = E_State.Death;
		Owner.BlackBoard.MotionType = E_MotionType.Death;
	}
}
