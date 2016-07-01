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
using System.Collections.Generic;

public abstract class ComponentPlayerMP : ComponentPlayer
{
	StrictCharacter StrictCharacter { get; set; }
	float LastSpeed = 0;
	public Vector3 Velocity;

	AgentActionCoverFire coverFireAction;
	AgentActionCoverFireCancel coverFireCancelAction;

	protected override void Awake()
	{
		base.Awake();

		Owner.BlackBoard.AimAnimationsEnabled = true;
	}

	protected override void Start()
	{
		base.Start();

		StrictCharacter = GetComponent<StrictCharacter>();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
	}

	protected override void Activate()
	{
		LastSpeed = 0;
		base.Activate();
	}

	protected override void Deactivate()
	{
		base.Deactivate();
	}

	protected override void Update()
	{
		if (Owner.IsAlive == false)
			return;

		// camera je null v pripade spectator kamer - panaci pak slidovali po scene (bez rotace)
		// pokud nekdo vi proc tady byl tento test, nebo v pripade problemu vratit zpatky
		//if (Camera.main == null)
		//    return;

		base.Update();

		//Debug.Log(StrictCharacter.isStandingStill + " " + StrictCharacter.velocity.x + StrictCharacter.velocity.y + StrictCharacter.velocity.z);
		if (IsRolling || Owner.BlackBoard.BusyAction || Owner.IsEnteringToCover || IsLeavingCover)
		{
			Vector3 moveDirection = StrictCharacter.Velocity;
			Owner.BlackBoard.Desires.MoveDirection = moveDirection.normalized;
			Owner.BlackBoard.Desires.MoveSpeedModifier = 1;

			float newSpeed = moveDirection.magnitude;
//			if (newSpeed > Owner.BlackBoard.BaseSetup.MaxSprintSpeed)
//				newSpeed = Owner.BlackBoard.BaseSetup.MaxSprintSpeed;
			if (newSpeed > Owner.BlackBoard.RealMaxSprintSpeed)
				newSpeed = Owner.BlackBoard.RealMaxSprintSpeed;

			Owner.BlackBoard.Speed = Mathf.Lerp(LastSpeed, newSpeed, 5);
			LastSpeed = Owner.BlackBoard.Speed;
		}
		else if (!StrictCharacter.isStandingStill)
		{
			Vector3 moveDirection = StrictCharacter.Velocity;

			if (Owner.IsInCover)
			{
				float dotRight = Vector3.Dot(Owner.BlackBoard.Cover.Right, moveDirection);
				float dotForward = Vector3.Dot(Owner.BlackBoard.Cover.Forward, moveDirection);
				bool canMove = false;

				if (dotForward > 0.75f || dotForward < -0.75f)
				{
					// disabled - HandleCoverMove() takes care just about left and right directions - why this was here then?
					// in case of any problems just enable it again
					//canMove = true;
				}
				else
				{
					if (dotRight > 0.75f)
					{
						Vector3 edgePos = Owner.BlackBoard.Cover.RightEdge;
						edgePos.y = Owner.Position.y;

						if ((Owner.Position - edgePos).magnitude > 0.01f)
							canMove = true;
					}
					else if (dotRight < -0.75f)
					{
						Vector3 edgePos = Owner.BlackBoard.Cover.LeftEdge;
						edgePos.y = Owner.Position.y;

						if ((Owner.Position - edgePos).magnitude > 0.01f)
							canMove = true;
					}
				}

				if (canMove)
					HandleCoverMove();
			}
			else if (Owner.BlackBoard.MotionType == E_MotionType.None)
			{
				Owner.BlackBoard.ActionAdd(AgentActionFactory.Create(AgentActionFactory.E_Type.Move) as AgentActionMove);
			}
			else
			{
				//Debug.Log ("ComponentPlayerMP.Update() 4");
			}

			Owner.BlackBoard.Desires.MoveDirection = moveDirection.normalized;
			Owner.BlackBoard.Desires.MoveSpeedModifier = 1;

			float newSpeed = moveDirection.magnitude;
//			if(newSpeed > Owner.BlackBoard.BaseSetup.MaxSprintSpeed)
//				newSpeed = Owner.BlackBoard.BaseSetup.MaxSprintSpeed;
			if (newSpeed > Owner.BlackBoard.RealMaxSprintSpeed)
				newSpeed = Owner.BlackBoard.RealMaxSprintSpeed;

			Owner.BlackBoard.Speed = Mathf.Lerp(LastSpeed, newSpeed, 5);
			LastSpeed = Owner.BlackBoard.Speed;
			Owner.WorldState.SetWSProperty(E_PropKey.AtTargetPos, false);
		}
		else
		{
			Owner.BlackBoard.Desires.MoveDirection = Vector3.zero;
			Owner.BlackBoard.Desires.MoveSpeedModifier = 0;
			Owner.BlackBoard.Speed = 0;
			LastSpeed = 0;

			if (Owner.BlackBoard.MotionType != E_MotionType.None /*&& !IsLeavingCover*/&& !Owner.BlackBoard.CoverFire)
							//mira and mara and beny consensus, that it could be deleted
			{
				AgentActionIdle idle = AgentActionFactory.Create(AgentActionFactory.E_Type.Idle) as AgentActionIdle;
				Owner.BlackBoard.ActionAdd(idle);
				Owner.WorldState.SetWSProperty(E_PropKey.AtTargetPos, true);
			}
		}

		Owner.BlackBoard.Desires.Rotation = StrictCharacter.Rotation;

		ClipRotation();

		// Debug.Log(Controls.View.YawAdd + " " + Controls.View.PitchAdd);

		HandleFire();
	}

	void HandleFire()
	{
		// without this condition action AgentActionCoverFireCancel is fired on proxy, which stops visual part of first cover fire
		// @see BUG #376 - Shadowgun MP - First shooting from cover is not visible for other clients
		if (Owner.IsProxy)
			return;

		//Owner.BlackBoard.Desires.WeaponTriggerOn = false;
		//xxxx
		// This is taken from GOAPGoalCoverFire.

		/*WorldStateProp prop = Owner.WorldState.GetWSProperty(E_PropKey.CoverState);

        if (Owner.IsInCover)
        {
            if (((prop.GetCoverState() == E_CoverState.Middle && Owner.BlackBoard.Cover.CoverFlags.Get(Cover.E_CoverFlags.UpCrouch)) ||
             prop.GetCoverState() == E_CoverState.Edge))
            {
                if (coverFireAction == null && Owner.BlackBoard.Desires.WeaponTriggerOn && Owner.CanFire())
                {
                    coverFireAction = AgentActionFactory.Create(AgentActionFactory.E_Type.CoverFire) as AgentActionCoverFire;
                    coverFireCancelAction = null;

                    Owner.BlackBoard.ActionAdd(coverFireAction);
                }
            }
            else if (prop.GetCoverState() == E_CoverState.Aim)
            {
                if (coverFireCancelAction == null && !Owner.BlackBoard.Desires.WeaponTriggerOn)
                {
                    coverFireCancelAction = AgentActionFactory.Create(AgentActionFactory.E_Type.CoverFireCancel) as AgentActionCoverFireCancel;
                    coverFireAction = null;

                    Owner.BlackBoard.ActionAdd(coverFireCancelAction);
                }
            }
        }*/
	}

	void HandleCoverMove()
	{
		AgentActionCoverMove action = AgentActionFactory.Create(AgentActionFactory.E_Type.CoverMove) as AgentActionCoverMove;

		action.Speed = Owner.BlackBoard.Desires.MoveSpeedModifier;

		if (Vector3.Dot(Owner.BlackBoard.Desires.MoveDirection, Owner.Right) > 0)
			action.Direction = AgentActionCoverMove.E_Direction.Right;
		else
			action.Direction = AgentActionCoverMove.E_Direction.Left;

		Owner.BlackBoard.ActionAdd(action);
	}
}
