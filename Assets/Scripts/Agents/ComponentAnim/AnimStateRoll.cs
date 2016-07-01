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

public class AnimStateRoll : AnimState
{
	AgentActionRoll Action;

	Vector3 StartPosition;
	Vector3 FinalPosition;
	float CurrentMoveTime;
	float MoveTime;
	bool PositionOK = false;

	float EndOfStateTime;

	public AnimStateRoll(Animation anims, AgentHuman owner)
					: base(anims, owner)
	{
	}

	public override void OnActivate(AgentAction action)
	{
		base.OnActivate(action);

		Owner.BlackBoard.MotionType = E_MotionType.Roll;

		Owner.BlackBoard.BusyAction = true;
		Owner.BlackBoard.ReactOnHits = false;

		//     Time.timeScale = .7f;
	}

	public override void OnDeactivate()
	{
		//      Time.timeScale = 1;
		Owner.BlackBoard.BusyAction = false;
		Owner.BlackBoard.ReactOnHits = true;

		Owner.BlackBoard.MotionType = E_MotionType.None;

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

			Vector3 move = finalPos - Transform.position;
			move.y = Owner.BlackBoard.Velocity.y;

			if (Move(move) == false)
				PositionOK = true;

			Owner.BlackBoard.Speed = (finalPos - Transform.position).magnitude;
		}

		if (EndOfStateTime <= Time.timeSinceLevelLoad)
			Release();
	}

	public override bool HandleNewAction(AgentAction action)
	{
		if (action is AgentActionRoll)
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

		Action = action as AgentActionRoll;

		StartPosition = Transform.position;

		switch (Action.Direction)
		{
		case E_Direction.Forward:
			FinalPosition = StartPosition + Owner.Forward*Owner.BlackBoard.BaseSetup.RollDistance;
			break;
		case E_Direction.Right:
			FinalPosition = StartPosition + Owner.Right*Owner.BlackBoard.BaseSetup.RollDistance;
			break;
		case E_Direction.Left:
			FinalPosition = StartPosition - Owner.Right*Owner.BlackBoard.BaseSetup.RollDistance;
			break;
		case E_Direction.Backward:
			FinalPosition = StartPosition - Owner.Forward*Owner.BlackBoard.BaseSetup.RollDistance;
			break;
		}

		//Debug.Log("ROL DIR " + Action.Direction);

		string AnimName = Owner.AnimSet.GetRollAnim(Action.Direction);

		CrossFade(AnimName, 0.1f, PlayMode.StopSameLayer);
		Owner.SetDominantAnimName(AnimName);

		CurrentMoveTime = 0;
		MoveTime = Animation[AnimName].length*0.95f;
		EndOfStateTime = Animation[AnimName].length*0.85f + Time.timeSinceLevelLoad;

		if (Owner.IsOwner)
			PositionOK = false;
		else
			PositionOK = true;
	}
}
