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

public class AnimStateInjury : AnimState
{
	float MoveTime;
	float CurrentMoveTime;
	bool PositionOK = false;
	Vector3 Impulse;

	AgentActionInjury Action = null;

	float EndOfStateTime;
	float PlayAnimTime;

	public AnimStateInjury(Animation anims, AgentHuman owner)
					: base(anims, owner)
	{
	}

	public override void OnActivate(AgentAction action)
	{
		base.OnActivate(action);

		Owner.BlackBoard.MotionType = E_MotionType.None;
		Owner.BlackBoard.MoveDir = Vector3.zero;
		Owner.BlackBoard.Speed = 0;
	}

	public override void OnDeactivate()
	{
		Owner.BlackBoard.MotionType = E_MotionType.None;
		Owner.BlackBoard.MoveDir = Vector3.zero;
		Owner.BlackBoard.Speed = 0;

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
		if (PositionOK == false)
		{
			CurrentMoveTime += Time.deltaTime;
			if (CurrentMoveTime >= MoveTime)
			{
				CurrentMoveTime = MoveTime;
				PositionOK = true;
			}

			float progress = Mathf.Max(0, Mathf.Min(1.0f, CurrentMoveTime/MoveTime));

			Vector3 impuls = Vector3.Lerp(Impulse, Vector3.zero, progress);

			if (MoveEx(impuls*Time.deltaTime) == false)
			{
				//Debug.Log("move false");
				PositionOK = true;
			}
		}

		if (EndOfStateTime <= Time.timeSinceLevelLoad)
			Release();
	}

	public override bool HandleNewAction(AgentAction action)
	{
		if (action is AgentActionInjury)
		{
			if (Action != null)
				Action.SetSuccess();

			SetFinished(false); // just for sure

			Initialize(action);

			return true;
		}
		return false;
	}

	protected override void Initialize(AgentAction action)
	{
		base.Initialize(action);

		Action = action as AgentActionInjury;

		if (PlayAnimTime < Time.timeSinceLevelLoad)
		{
			// play owner anims
			string animName = Owner.AnimSet.GetInjuryAnim();

			PlayAnimTime = Time.timeSinceLevelLoad + Animation[animName].length*0.35f;
			CrossFade(animName, 0.25f, PlayMode.StopSameLayer);

			EndOfStateTime = Animation[animName].length + Time.timeSinceLevelLoad;
		}
		else
			EndOfStateTime = 0.2f + Time.timeSinceLevelLoad;

		Owner.BlackBoard.MotionType = E_MotionType.None;

		MoveTime = Random.Range(0.05f, 0.09f);
		CurrentMoveTime = 0;

		Impulse = Action.Impulse;

		PositionOK = Impulse == Vector3.zero;

		Owner.BlackBoard.MotionType = E_MotionType.Injury;
	}
}
