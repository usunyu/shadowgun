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

public class AnimStateCoverLeave : AnimState
{
	AgentActionCoverLeave Action;

	Quaternion FinalRotation = new Quaternion();
	Quaternion StartRotation = new Quaternion();

	Vector3 StartPosition;
	Vector3 FinalPosition;
	Vector3 StartPositionHelper;
	Vector3 FinalPositionHelper;
	float CurrentMoveTime;
	float MoveTime;

	float EndOfStateTime;

	bool PositionOK = false;
	bool RotationOK = false;

	public AnimStateCoverLeave(Animation anims, AgentHuman owner)
					: base(anims, owner)
	{
	}

	public override void OnActivate(AgentAction action)
	{
		base.OnActivate(action);

		Owner.BlackBoard.MotionType = E_MotionType.AnimationDrive;
		Owner.BlackBoard.ReactOnHits = false;
		Owner.BlackBoard.BusyAction = true;
		Owner.BlackBoard.Desires.WeaponTriggerOn = false;
		Owner.BlackBoard.Desires.WeaponTriggerUp = false;
		Owner.BlackBoard.Desires.WeaponTriggerUpDisabled = true;
		Owner.BlackBoard.Desires.MeleeTriggerOn = false;
		//      Time.timeScale = .1f;
	}

	public override void OnDeactivate()
	{
		//  Time.timeScale = 1;
		Owner.BlackBoard.ReactOnHits = true;
		Owner.BlackBoard.BusyAction = false;

		Owner.CoverStop();

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
		/*if (Owner.IsPlayer)
        {
         //   Debug.Log(AnimNameBase + " " + Animation[AnimNameBase].weight);

            string s = "Anims:";
            foreach (AnimationState state in Owner.animation)
            {
                if (Owner.animation.IsPlaying(state.clip.name) == false)
                    continue;

                s += " " + state.clip.name + " " + state.weight.ToString();

            }

            Debug.Log(s);
        }*/

//        Debug.Log(Owner.Transform.forward);
		if (PositionOK == false)
		{
			//Debug.DrawLine(StartPosition, FinalPosition, Color.blue, 2);
			CurrentMoveTime += Time.deltaTime;
			if (CurrentMoveTime >= MoveTime)
			{
				CurrentMoveTime = MoveTime;
				PositionOK = true;

				Action.SetSuccess();
				Owner.BlackBoard.BusyAction = false;

				Owner.CoverStop();

				Owner.BlackBoard.Desires.CoverNear.Cover = null;

				//Debug.Log(Time.realtimeSinceStartup + " move ok");

				Release();
			}

			if (CurrentMoveTime > 0)
			{
				float progress = CurrentMoveTime/MoveTime;

				//Debug.Log("progress " + progress + " " + (v - Transform.position).magnitude + " pos " + Transform.position.x + ", " + Transform.position.z);

				if (Action.TypeOfLeave == AgentActionCoverLeave.E_Type.Right || Action.TypeOfLeave == AgentActionCoverLeave.E_Type.Left)
				{
					Vector3 position = Mathfx.InterpolateCatmullRom(StartPositionHelper, StartPosition, FinalPosition, FinalPositionHelper, progress);
					//Owner.CharacterController.Move( position - Transform.position );
					Transform.position = position; // we decided not to use collision test during leaving of cover from left or right side
				}
				else if (Action.TypeOfLeave == AgentActionCoverLeave.E_Type.JumpUp)
				{
					Transform.position = Vector3.Lerp(StartPosition, FinalPosition, progress);
				}
				else
				{
					Vector3 position = Mathfx.Hermite(StartPosition, FinalPosition, progress);
					if (Action.TypeOfLeave == AgentActionCoverLeave.E_Type.Jump)
					{
						Transform.position = position; // jump over without collisions
					}
					else // E_Type.Back
					{
						Owner.CharacterController.Move(position - Transform.position);
					}
				}

				if (RotationOK == false)
				{
					Owner.Transform.rotation = Quaternion.Lerp(StartRotation, FinalRotation, progress);

					//Debug.Log ("AnimStateCoverLeave, RotationOK == false, Owner.Transform.rotation=" + Owner.Transform.rotation + ", progress=" + progress + ", time=" + Time.timeSinceLevelLoad);
				}
			}
		}

		if (EndOfStateTime <= Time.timeSinceLevelLoad)
		{
			// Owner.Transform.rotation = Owner.BlackBoard.Desires.Rotation;
			//Debug.Log(Time.realtimeSinceStartup + " release");
			Release();
		}
	}

	public override bool HandleNewAction(AgentAction action)
	{
		if (action is AgentActionCoverLeave)
		{
			if (Action != null)
			{
				Debug.LogError(ToString() + ": Second action is not allowed");
				action.SetFailed();
				return true;
			}

			Initialize(action);

			return true;
		}

		return false;
	}

	protected override void Initialize(AgentAction action)
	{
		base.Initialize(action);

		Action = action as AgentActionCoverLeave;

		StartPosition = Transform.position;

		string AnimName;

		//Debug.Log(Time.realtimeSinceStartup + " init");
		RotationOK = true;

		switch (Action.TypeOfLeave)
		{
		case AgentActionCoverLeave.E_Type.Right:
		case AgentActionCoverLeave.E_Type.Left:
			float multiplierHelper = 1;
			if (Action.TypeOfLeave == AgentActionCoverLeave.E_Type.Right)
			{
				AnimName = Owner.AnimSet.GetCoverAnim(AnimSet.E_CoverAnim.LeaveRight, Owner.BlackBoard.CoverPose, E_CoverDirection.Right);
			}
			else
			{
				multiplierHelper = -1;
				AnimName = Owner.AnimSet.GetCoverAnim(AnimSet.E_CoverAnim.LeaveLeft, Owner.BlackBoard.CoverPose, E_CoverDirection.Left);
			}

			//FinalPosition = Owner.Position + Owner.Right * 1.5f * multiplierHelper + Owner.Forward * 1.3f;
			//FinalPosition = BuildSafeFinalPositionForLeavingCover( Owner.Position + Owner.Right * 1.5f * multiplierHelper + Owner.Forward * 1.1f );
			FinalPosition = BuildSafeFinalPositionForLeavingCover(Action.TypeOfLeave, Owner.Position, Owner.Right, Owner.Forward);
			StartPositionHelper = StartPosition - Owner.Right*multiplierHelper;

			FinalPositionHelper = FinalPosition + Owner.Forward;

			CurrentMoveTime = 0f;
			Animation[AnimName].speed = 1f;
			MoveTime = 0.3f;
			EndOfStateTime = 0.3f + Time.timeSinceLevelLoad;
			Owner.BlackBoard.Speed = 5;
			break;

		case AgentActionCoverLeave.E_Type.Jump:
			AnimName = Owner.AnimSet.GetCoverAnim(AnimSet.E_CoverAnim.JumpOver, Owner.BlackBoard.CoverPose, E_CoverDirection.Middle);
			//FinalPosition = Owner.Position + Owner.Forward * 2.0f;
			//FinalPosition = BuildSafeFinalPositionForLeavingCover( Owner.Position + Owner.Forward * 2.0f ); // prevents to fall through the scene in case we are jumping 'uphill'
			FinalPosition = BuildSafeFinalPositionForLeavingCover(Action.TypeOfLeave, Owner.Position, Owner.Right, Owner.Forward);
			CurrentMoveTime = -0.2f;
			Animation[AnimName].speed = 1.3f;
			MoveTime = 0.4f;
			EndOfStateTime = 0.9f + Time.timeSinceLevelLoad;
			Owner.BlackBoard.Speed = 5;
			break;

		case AgentActionCoverLeave.E_Type.JumpUp:
			AnimName = Owner.AnimSet.GetCoverAnim(AnimSet.E_CoverAnim.JumpUp, Owner.BlackBoard.CoverPose, E_CoverDirection.Middle);
			//FinalPosition = Owner.Position + Vector3.up * 1.4f + Owner.Forward * 0.65f;
			FinalPosition = BuildSafeFinalPositionForLeavingCover(Action.TypeOfLeave, Owner.Position, Owner.Right, Owner.Forward);
			CurrentMoveTime = 0.0f;
			Animation[AnimName].speed = 1.4f;
			MoveTime = Animation[AnimName].length*0.8f/Animation[AnimName].speed;
			EndOfStateTime = Animation[AnimName].length/Animation[AnimName].speed*0.8f + Time.timeSinceLevelLoad;
			Owner.BlackBoard.Speed = 5;
			break;

		case AgentActionCoverLeave.E_Type.Back:
			AnimName = Owner.AnimSet.GetCoverAnim(AnimSet.E_CoverAnim.Leave, Owner.BlackBoard.CoverPose, E_CoverDirection.Middle);
			//FinalPosition = Owner.Position - Owner.BlackBoard.Cover.Forward * 1.1f;
			//FinalPosition = BuildSafeFinalPositionForLeavingCover( Owner.Position - Owner.BlackBoard.Cover.Forward * 1.1f );
			FinalPosition = BuildSafeFinalPositionForLeavingCover(Action.TypeOfLeave, Owner.Position, Owner.Right, -Owner.BlackBoard.Cover.Forward);
			StartRotation = Owner.Transform.rotation;
			FinalRotation.eulerAngles = new Vector3(0, Owner.BlackBoard.Desires.Rotation.eulerAngles.y, 0);
			Animation[AnimName].speed = 1.2f;
			MoveTime = 0.1f;
			CurrentMoveTime = -0.1f;
			EndOfStateTime = 0.15f + Time.timeSinceLevelLoad;
//			RotationOK = false;	//ComponentBody will take care about the rotation... When this is enabled it causes a rotation glitch.
			break;
		default:
			AnimName = "";
			Debug.LogWarning("Unsupported type of cover leaving : Action.TypeOfLeave");
			break;
		}

//		Debug.Log ("AnimStateCoverLeave, AnimName=" + AnimName + ", time=" + Time.timeSinceLevelLoad);

		CrossFade(AnimName, 0.15f, PlayMode.StopSameLayer);
		Owner.SetDominantAnimName(AnimName);

		Owner.WeaponComponent.DisableCurrentWeapon(Animation[AnimName].length);
		PositionOK = false;

		Owner.BlackBoard.MotionType = E_MotionType.None;

		//change FOV to the weapon/default value
		if (uLink.Network.isClient && Owner.NetworkView.isMine)
		{
			float newFOV = GameCamera.Instance.DefaultFOV;
			GameCamera.Instance.SetFov(newFOV, 200);
		}
	}

	// prevents to fall through the scene in case we are jumping 'uphill'
	static Vector3 BuildSafeFinalPositionForLeavingCover(Vector3 fromPosition)
	{
		Vector3 result = fromPosition;

		RaycastHit hit;

		if (Physics.Raycast(result + Vector3.up*0.5f, Vector3.down, out hit, 1.0f, ObjectLayerMask.Default | ObjectLayerMask.PhysicsMetal))
		{
			result = hit.point + Vector3.up*0.1f;
		}

		return result;
	}

	// prevents to fall through the scene in case we are jumping 'uphill'
	public static Vector3 BuildSafeFinalPositionForLeavingCover(AgentActionCoverLeave.E_Type type,
																Vector3 position,
																Vector3 right,
																Vector3 forward)
	{
		switch (type)
		{
		case AgentActionCoverLeave.E_Type.Right:
		case AgentActionCoverLeave.E_Type.Left:
			float multiplierHelper = 1;
			if (type == AgentActionCoverLeave.E_Type.Left)
			{
				multiplierHelper = -1.0f;
			}
			return BuildSafeFinalPositionForLeavingCover(position + right*multiplierHelper*1.5f + forward*1.1f);
		case AgentActionCoverLeave.E_Type.Jump:
			return BuildSafeFinalPositionForLeavingCover(position + forward*2.0f);
		case AgentActionCoverLeave.E_Type.JumpUp:
			return position + Vector3.up*1.4f + forward*0.65f;
		case AgentActionCoverLeave.E_Type.Back:
			return BuildSafeFinalPositionForLeavingCover(position + forward*1.1f);
		}

		Debug.LogWarning("BuildSafeFinalPositionForLeavingCover : Unsupported type of cover leaving : Action.TypeOfLeave");

		return position;
	}
}
