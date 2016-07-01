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

public class AnimStateMelee : AnimState
{
	AgentActionMelee Action;

	Vector3 StartPosition;
	Vector3 FinalPosition;
	float CurrentMoveTime;
	float MoveTime;
	float KnockTime;
	bool PositionOK = false;
	//bool Knocked = false;

	float EndOfStateTime;

	public AnimStateMelee(Animation anims, AgentHuman owner)
					: base(anims, owner)
	{
	}

	public override void OnActivate(AgentAction action)
	{
		base.OnActivate(action);

		Owner.BlackBoard.Desires.MeleeTriggerOn = false;
		Owner.BlackBoard.MotionType = E_MotionType.Attack;

		Owner.BlackBoard.BusyAction = true;
		Owner.BlackBoard.ReactOnHits = false;
		Owner.Stop(true);
		//Knocked = false;
	}

	public override void OnDeactivate()
	{
		//      Time.timeScale = 1;
		Owner.BlackBoard.BusyAction = false;
		Owner.BlackBoard.ReactOnHits = true;

		Owner.BlackBoard.MotionType = E_MotionType.None;
		Owner.Stop(false);
		Action.SetSuccess();
		Action = null;
		base.OnDeactivate();
	}

	public override void Reset()
	{
		Owner.BlackBoard.MotionType = E_MotionType.None;

		Owner.BlackBoard.BusyAction = false;
		Owner.BlackBoard.ReactOnHits = true;
		Action.SetSuccess();
		Action = null;

		base.Reset();
	}

	public override void Update()
	{
		if (Owner.IsOwner)
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
				Vector3 finalPos = Mathfx.Sinerp(StartPosition, FinalPosition, progress);
				//MoveTo(finalPos);
				if (Move(finalPos - Transform.position) == false)
					PositionOK = true;
			}
		}

		if (EndOfStateTime <= Time.timeSinceLevelLoad)
			Release();
	}

	public override bool HandleNewAction(AgentAction action)
	{
		if (action is AgentActionMelee)
		{
			if (Action != null)
				Action.SetSuccess();

			Initialize(action);
			return true;
		}

		return false;
	}

	protected override void Initialize(AgentAction action)
	{
		base.Initialize(action);

		Action = action as AgentActionMelee;

		Owner.SoundPlay(Owner.MeleeSound);

		string AnimName = Owner.AnimSet.GetMeleeAnim(Action.MeleeType);

		CrossFade(AnimName, 0.1f, PlayMode.StopSameLayer);

		StartPosition = Transform.position;
		FinalPosition = Action.Target.Transform.position;

		CurrentMoveTime = -0.15f;
		MoveTime = Animation[AnimName].length*0.3f;
		EndOfStateTime = Animation[AnimName].length*0.9f + Time.timeSinceLevelLoad;

		PositionOK = false;
	}
}
