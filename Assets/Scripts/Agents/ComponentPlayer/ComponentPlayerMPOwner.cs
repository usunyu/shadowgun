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

public class ComponentPlayerMPOwner : ComponentPlayerLocal
{
	public delegate void OnActivatedDelegate(ComponentPlayerMPOwner localPlayer);
	public delegate void OnDeactivatedDelegate(ComponentPlayerMPOwner localPlayer);

	public static OnActivatedDelegate OnActivated;
	public static OnDeactivatedDelegate OnDeactivated;

	public float FiringMaximalAngle = 60.0f;

	protected override void Awake()
	{
		base.Awake();

		Controls.Start();
		Owner.BlackBoard.AimAnimationsEnabled = true;
		Owner.BlackBoard.ActionHandler += HandleActions;
	}

	protected void OnDisable()
	{
		//Deactivate();
	}

	protected override void OnDestroy()
	{
		Controls.Destroy();

		base.OnDestroy();
	}

	// Use this for initialization
	protected override void Start()
	{
		base.Start();

		Owner.SensorsComponent.AddSensor(E_SensorType.CoverPlayer, true);
		Owner.SensorsComponent.AddSensor(E_SensorType.EyePlayer, true);

		Owner.AddGOAPAction(E_GOAPAction.Goto);
		Owner.AddGOAPAction(E_GOAPAction.Move);
		Owner.AddGOAPAction(E_GOAPAction.CoverEnter);
		Owner.AddGOAPAction(E_GOAPAction.CoverMove);
		Owner.AddGOAPAction(E_GOAPAction.CoverLeave);
		Owner.AddGOAPAction(E_GOAPAction.CoverFire);
		Owner.AddGOAPAction(E_GOAPAction.CoverJumpOverPlayer);
		Owner.AddGOAPAction(E_GOAPAction.CoverLeaveRightLeft);
		Owner.AddGOAPAction(E_GOAPAction.Use);
		Owner.AddGOAPAction(E_GOAPAction.WeaponReload);
//		Owner.AddGOAPAction(E_GOAPAction.WeaponChange);
		Owner.AddGOAPAction(E_GOAPAction.Roll);
		Owner.AddGOAPAction(E_GOAPAction.UseGadget);
		Owner.AddGOAPAction(E_GOAPAction.Melee);

		Owner.AddGOAPGoal(E_GOAPGoals.Move);
		Owner.AddGOAPGoal(E_GOAPGoals.CoverEnter);
		Owner.AddGOAPGoal(E_GOAPGoals.CoverLeave);
		Owner.AddGOAPGoal(E_GOAPGoals.CoverFire);
		Owner.AddGOAPGoal(E_GOAPGoals.CoverMove);
		Owner.AddGOAPGoal(E_GOAPGoals.UseWorldObject);
		Owner.AddGOAPGoal(E_GOAPGoals.WeaponReload);
//		Owner.AddGOAPGoal(E_GOAPGoals.WeaponChange);
		Owner.AddGOAPGoal(E_GOAPGoals.Roll);
		Owner.AddGOAPGoal(E_GOAPGoals.UseGadget);
		Owner.AddGOAPGoal(E_GOAPGoals.Melee);

		Owner.InitializeGOAP();

		//register controls delegates
		Controls.FireDownDelegate = ActionBeginFire;
		Controls.FireUpDelegate = ActionEndFire;
		Controls.UseDelegate = ActionUse;
		Controls.UseObjectDelegate = ActionUseObject;
		Controls.ReloadDelegate = ActionReload;
		Controls.RollDelegate = ActionRoll;
		Controls.SprintDownDelegate = ActionSprintBegin;
		Controls.SprintUpDelegate = ActionSprintEnd;
		Controls.ChangeWeaponDelegate = ActionChangeWeapon;
		Controls.UseGadgetDelegate = ActionUseGadget;
		Controls.SendCommandDelegate = ActionCommand;
	}

	protected override void Activate()
	{
		base.Activate();

		Controls.SwitchToCombatMode();

		Client.Instance.PlaySoundPlayerSpawn();

		GameCamera.ChangeMode(GameCamera.E_State.Player);

		if (OnActivated != null)
		{
			OnActivated(this);
		}

		/*	GuiHUD.Instance.ShowCombatText(Client.E_MessageType.Kill, "100Exp/50$");
		GuiHUD.Instance.ShowCombatText(Client.E_MessageType.HeadShot, "+50Exp/25$");
		GuiHUD.Instance.ShowCombatText(Client.E_MessageType.Rank, "100 Exp/5000$");
		GuiHUD.Instance.ShowCombatText(Client.E_MessageType.ExclusiveKill, string.Format(TextDatabase.instance[00502077], 1));
			
		GuiHUD.Instance.ShowCombatText(Client.E_MessageType.KillAssist, "50 Exp");
		GuiHUD.Instance.ShowCombatText(Client.E_MessageType.Turret, "50 Exp");
		GuiHUD.Instance.ShowCombatText(Client.E_MessageType.ZoneDefended, "50 Exp");
		GuiHUD.Instance.ShowCombatText(Client.E_MessageType.ZoneAttacked, "50 Exp");
		GuiHUD.Instance.ShowCombatText(Client.E_MessageType.ZoneNeutral, "50 Exp");
		GuiHUD.Instance.ShowCombatText(Client.E_MessageType.ZoneControl, "50 Exp");
		
		GuiHUD.Instance.ShowCombatText(Client.E_MessageType.Win, "50 Exp");
		GuiHUD.Instance.ShowCombatText(Client.E_MessageType.Lost, "50 Exp");
		
		GuiHUD.Instance.ShowCombatText(Client.E_MessageType.Unlock, "50 Exp");
		
		GuiHUD.Instance.ShowCombatText(Client.E_MessageType.Spider, "50 Exp");*/

		StopAllCoroutines();

#if UNITY_STANDALONE
		StartCoroutine( ClientTimeCheck() );
#endif
	}

	protected override void Deactivate()
	{
		StopAllCoroutines();
		base.Deactivate();

		if (GameCamera.Instance)
		{
			GameCamera.ChangeMode(GameCamera.E_State.Spectator_Free);
		}

		if (OnDeactivated != null)
		{
			OnDeactivated(this);
		}
	}

	protected override void Update()
	{
		if (Owner.BlackBoard.Stop)
		{
			Controls.Update();
			return;
		}

		base.Update();

		Controls.Update();

		if (Owner.IsAlive == false)
			return;

		if (Camera.main == null)
			return;

		if (Controls.Use)
		{
			CreateOrderUse();
		}

		if (Controls.Move.Enabled && Controls.Move.Direction != Vector3.zero)
		{
			//Debug.DrawLine(Agent.Position + Vector3.up, Agent.Position + Vector3.up + Controls.MoveJoystick.Direction * Controls.MoveJoystick.Force * 4);

			bool canMove = true;

			if (Owner.IsEnteringToCover || IsLeavingCover || IsRolling || Owner.IsInKnockdown)
			{
				canMove = false;
			}
			else if (Owner.IsInCover)
			{
				float dotRight = Vector3.Dot(Owner.BlackBoard.Cover.Right, Controls.Move.Direction);
				float dotForward = Vector3.Dot(Owner.BlackBoard.Cover.Forward, Controls.Move.Direction);

				//AgentActionCoverMove.E_Direction DesiredDirection;

				canMove = false;
				if (dotForward > 0.75f)
				{
					canMove = true;
				}
				else if (dotForward < -0.75f) //move 
					canMove = true;
				else
				{
					if (dotRight > 0)
					{
						Vector3 edgePos = Owner.BlackBoard.Cover.RightEdge;
						edgePos.y = Owner.Position.y;

						if ((Owner.Position - edgePos).magnitude > Mathf.Epsilon)
							canMove = Owner.RestrictedCoverMove(AgentActionCoverMove.E_Direction.Right, 0.1f, true);
					}
					else if (dotRight < 0)
					{
						Vector3 edgePos = Owner.BlackBoard.Cover.LeftEdge;
						edgePos.y = Owner.Position.y;

						if ((Owner.Position - edgePos).magnitude > Mathf.Epsilon)
							canMove = Owner.RestrictedCoverMove(AgentActionCoverMove.E_Direction.Left, 0.1f, true);
					}
				}
			}

			if (canMove)
			{
				Owner.BlackBoard.Desires.MoveDirection = Controls.Move.Direction;

				Owner.BlackBoard.Desires.MoveSpeedModifier = Controls.Move.Force;
				if (Owner.BlackBoard.Desires.MoveSpeedModifier > 1)
					Owner.BlackBoard.Desires.MoveSpeedModifier = 1;

				Owner.WorldState.SetWSProperty(E_PropKey.AtTargetPos, false);
			}
			else
			{
				Owner.BlackBoard.Desires.MoveDirection = Vector3.zero;
				Owner.BlackBoard.Desires.MoveSpeedModifier = 0;
				Owner.WorldState.SetWSProperty(E_PropKey.AtTargetPos, true);
			}
		}
		else
		{
			Owner.BlackBoard.Desires.MoveDirection = Vector3.zero;
			Owner.BlackBoard.Desires.MoveSpeedModifier = 0;
			Owner.WorldState.SetWSProperty(E_PropKey.AtTargetPos, true);
		}

		if (Owner.BlackBoard.MotionType != E_MotionType.Roll)
		{
			Vector3 add = new Vector3(Controls.View.PitchAdd, Controls.View.YawAdd);

			add *= GameCamera.Instance.GetFovRatioExp();

			Owner.BlackBoard.Desires.Rotation.eulerAngles += add;
			ClipRotation();
		}

		//remove sprint of gadget is empty
		if (Owner.IsInCover || Owner.IsEnteringToCover || Owner.IsLeavingToCover ||
			(Owner.GadgetsComponent.IsPerkAvailableForUse(E_PerkID.Sprint) == false &&
			 Owner.GadgetsComponent.IsPerkAvailableForUse(E_PerkID.SprintII) == false &&
			 Owner.GadgetsComponent.IsPerkAvailableForUse(E_PerkID.SprintIII) == false)
						)
			Owner.BlackBoard.Desires.Sprint = false;

		// Debug.Log(Controls.View.YawAdd + " " + Controls.View.PitchAdd);

		//UpdateIdealFireDir();
		UpdateWeaponFireDir();

		// reset controls
		Controls.Move.ZeroInput();
		Controls.View.ZeroInput();
		Controls.Use = false;
	}

	void UpdateWeaponFireDir()
	{
		Owner.BlackBoard.Desires.FireDirection = CameraDirection;
		Owner.BlackBoard.Desires.FireTargetPlace = Owner.WeaponComponent.GetCurrentWeapon().ShotPos + CameraDirection*1000;
	}

	void LateUpdate()
	{
		if (BloodFXManager.Instance)
			BloodFXManager.Instance.SetHealthNormalized(Owner.BlackBoard.Health/Owner.BlackBoard.RealMaxHealth);

		if (Owner.IsAlive == false)
			return;
	}

	public void CreateOrderUse()
	{
//        Owner.BlackBoard.Desires.InteractionObject = Mission.Instance.GameZone.GetNearestInteractionObject(Owner.Position, 2);

		if (Owner.BlackBoard.Desires.InteractionObject)
			Owner.WorldState.SetWSProperty(E_PropKey.UseWorldObject, true);
	}

	public override void StopMove(bool stop)
	{
		base.StopMove(stop);

		if (stop)
			Controls.DisableInput();
		else
			Controls.EnableInput();
	}

	void ActionUseGadget(E_ItemID id)
	{
		if (Owner.BlackBoard.Stop)
			return;

		if (Owner.CharacterController.isGrounded == false)
						//do not allow to use gadgets while falling (added especially for SentryGun and Mine, maybe we could allow some other gadgets)
			return;

		if (Owner.GadgetsComponent.IsGadgetAvailableForUse(id) == false)
			return;

		Item g = Owner.GadgetsComponent.GetGadget(id);

		if (Owner.CanUseGadget(g) == false)
			return;

		if (g.Settings.ItemBehaviour == E_ItemBehaviour.Booster)
		{
			Owner.NetworkView.RPC("AskForBoost", uLink.RPCMode.Server, id);
			return;
		}

		Owner.WorldState.SetWSProperty(E_PropKey.UseGadget, true);
		Owner.BlackBoard.Desires.Gadget = id;
	}

	void ActionCommand(E_CommandID id)
	{
		//TODO: do something clever here...
		AgentActionTeamCommand a = AgentActionFactory.Create(AgentActionFactory.E_Type.TeamCommand) as AgentActionTeamCommand;
		a.Command = id;

		Owner.BlackBoard.ActionAdd(a);
	}

	// former UpdateIdealFireDir()
	public Vector3 GetRealFireDir()
	{
		//Owner.BlackBoard.Desires.FireDirection = CameraDirection;
		Vector3 Result = CameraDirection;

		if (Owner.WeaponComponent.GetCurrentWeapon() == null)
			return Result;

		Vector3 startPos = Owner.WeaponComponent.GetCurrentWeapon().ShotPos;
		//startPos = Owner.ChestPosition;
		RaycastHit[] hits;

		//hits = Physics.RaycastAll(CameraPosition, Owner.BlackBoard.Desires.FireDirection, 200);
		hits = Physics.RaycastAll(CameraPosition, CameraDirection, 200);

		//sort by distance
		if (hits.Length > 1)
			System.Array.Sort(hits, CollisionUtils.CompareHits);

		Vector3 final = startPos + Result*30;
		foreach (RaycastHit hit in hits)
		{
			if (hit.transform == Owner.Transform)
				continue;

			if (hit.collider.isTrigger)
				continue;

			if (hit.transform.IsChildOf(Owner.Transform))
				continue;

			final = hit.point;
			break;
		}

		/* if (Owner.WeaponComponent.CurrentWeapon == E_WeaponID.GrenadeLauncher)
        {
            // jak ja to nesnasim.. no nakonec jsme vymyslel tohle :

            Vector3 dir = final - Owner.WeaponComponent.GetCurrentWeapon().ShotPos;
            float distance = Mathf.Max(1, dir.magnitude - 1f);
            dir.Normalize();

            float projectileSpeed = Owner.WeaponComponent.GetCurrentWeapon().Speed; // get speed
            float pitch = 0.5f * Mathf.Asin((distance * -Physics.gravity.y) / (projectileSpeed * projectileSpeed)); // vrh sikmy shit

            if (float.IsNaN(pitch))
                pitch = Mathf.Deg2Rad * 30 ; // moc daleko, tak aspon tech 45 stupnu

            pitch += Mathf.Atan2(dir.y, Mathf.Sqrt(dir.x * dir.x + dir.z * dir.z)); // pridame nebo uberem uhel ktery je mezi panaci

            pitch *= Mathf.Rad2Deg;

            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg; // spocitame uhel

            Vector3 temp = new Vector3(-pitch, yaw, 0); // a timto shitem ziskame zpet vector..
            Owner.BlackBoard.Desires.FireDirection = Quaternion.Euler(temp) * Vector3.forward;
            Owner.BlackBoard.Desires.FireDirection.Normalize();
        }
        else*/
		{
			//Owner.BlackBoard.Desires.FireDirection = Owner.BlackBoard.Desires.Rotation * Vector3.forward;approximate

			Vector3 Direction = (final - startPos).normalized;
			//Vector3 ShootPos = Owner.WeaponComponent.GetCurrentWeapon().ShotPos;
			//Vector3 TestDirection = (final - ShootPos).normalized;

			DebugDraw.Line(Color.white, startPos, startPos + CameraDirection*2);

			if (Vector3.Angle(Direction, CameraDirection) < FiringMaximalAngle)
			{
				DebugDraw.Line(Color.green, startPos, startPos + Direction*2);
				Result = Direction;
			}
			else
			{
				DebugDraw.Line(Color.red, startPos, startPos + Direction*2);
			}

			Owner.BlackBoard.Desires.FireTargetPlace = final;

			DebugDraw.Sphere(Color.red, 0.1f, final);

			return Result;
		}
	}

	void ActionBeginFire()
	{
		if (Owner.BlackBoard.Stop)
			return;

		if (Owner.CanMelee())
		{
			//Debug.Log(">>NO Fire");
			Owner.BlackBoard.Desires.MeleeTriggerOn = true;
			Owner.BlackBoard.Desires.WeaponTriggerUpDisabled = true;
		}
		else if (Owner.WeaponComponent.GetCurrentWeapon().IsOutOfAmmo)
		{
			//Debug.Log(">>NO Fire");
			Owner.BlackBoard.Desires.WeaponTriggerUpDisabled = true;
		}
		else if (Owner.CanFire2())
		{
			//Debug.Log("CanFire");
			Owner.BlackBoard.Desires.WeaponTriggerOn = true;
			Owner.BlackBoard.Desires.WeaponTriggerUpDisabled = false;
		}
		else
		{
			//Debug.Log(">>NO Fire");
			Owner.BlackBoard.Desires.WeaponTriggerUpDisabled = true;
		}

		Owner.BlackBoard.Desires.WeaponTriggerUp = false;
		Controls.Fire = true;
	}

	void ActionEndFire()
	{
		// Debug.Log("end of fire");
		Controls.Fire = false;

		// @see Ticket #447 - Shadowgun MP - Client can not shoot to other one if he got charged weapon and melee icon togather on screen
		//if(!Owner.CanMelee() && !Owner.BlackBoard.Desires.WeaponTriggerUpDisabled)
		if (!Owner.BlackBoard.Desires.WeaponTriggerUpDisabled)
		{
			Owner.BlackBoard.Desires.WeaponTriggerUp = true;
			//Debug.Log("!!! Fire up");
		}
		//else
		//Debug.Log(">>IGNORED Fire up");
		Owner.BlackBoard.Desires.WeaponTriggerOn = false;
		Owner.BlackBoard.Desires.MeleeTriggerOn = false;
	}

	void ActionUse()
	{
		if (Owner.BlackBoard.Stop)
			return;

		Controls.Use = true;
		CreateOrderUse();
	}

	void ActionUseObject(InteractionObject obj)
	{
		if (Owner.BlackBoard.Stop)
			return;

		Controls.Use = true;

		Owner.BlackBoard.Desires.InteractionObject = obj;

		if (Owner.BlackBoard.Desires.InteractionObject)
			Owner.WorldState.SetWSProperty(E_PropKey.UseWorldObject, true);
	}

	void ActionReload()
	{
		if (Owner.BlackBoard.Stop)
			return;

		if ((Owner.WeaponComponent.GetCurrentWeapon().IsFullyLoaded == false) && (Owner.WeaponComponent.GetCurrentWeapon().WeaponAmmo > 0))
			Owner.WorldState.SetWSProperty(E_PropKey.WeaponLoaded, false);
	}

	void ActionChangeWeapon(E_WeaponID weaponType)
	{
		if (CanChangeWeapon() == false)
			return;

		Owner.BlackBoard.Desires.Weapon = weaponType;

		//workaround to avoid sliding during weapon change (when this goes through GOAP, the GOAPMove action is terminated and it goes through Idle to the GOAPMove again)
		//Owner.WorldState.SetWSProperty(E_PropKey.WeaponChange, true);
		AgentActionWeaponChange Action = AgentActionFactory.Create(AgentActionFactory.E_Type.WeaponChange) as AgentActionWeaponChange;
		Action.NewWeapon = Owner.BlackBoard.Desires.Weapon;
		Owner.BlackBoard.ActionAdd(Action);
	}

	void ActionRoll()
	{
		Cover c = Mission.Instance.GameZone.GetCoverForPlayer(Owner, 3.0f);

		if (c != null)
		{
			Owner.BlackBoard.Desires.CoverNear.Cover = c;
			return;
		}

		if (Owner.IsInCover)
		{
			return;
		}

		// cannot do roll in the air
		if (Owner.CharacterController.isGrounded == false)
		{
			return;
		}

		E_Direction dir = Owner.GetMoveDirectionType(Owner.BlackBoard.Desires.MoveDirection);
		Vector3 direction;

		switch (dir)
		{
		case E_Direction.Forward:
			direction = Owner.Forward;
			break;
		case E_Direction.Right:
			direction = Owner.Right;
			break;
		case E_Direction.Left:
			direction = -Owner.Right;
			break;
		default:
			direction = -Owner.Forward;
			break;
		}

		//Debug.Log("ROL DIR TEST " + dir);

		/*
		 RaycastHit Hit;
		
		// cast in forward direction - KNEES
       /if( Physics.Raycast( Owner.Position + Vector3.up * 0.45f, direction, out Hit, 2 ) )
		{
			// collision found, roll is not possible
            return;
		}
		*/

		/*LayerMask mask = ~( ObjectLayerMask.Ragdoll | ObjectLayerMask.IgnoreRayCast );

		// cast in forward direction - HEAD
        if( Physics.SphereCast( Owner.HatTarget.position + Vector3.up * 2.0f, 0.35f, direction, out Hit, 2, mask ) )
		{
			// collision found, roll is not possible
            return;
		}
		
		// cast in forward direction - BODY
		if( Physics.SphereCast( Owner.Position + Vector3.up * 1.25f, 0.35f, direction, out Hit, 2, mask ) )
		{
			// collision found, roll is not possible
            return;
		}
		/**/

		LayerMask mask = ~(ObjectLayerMask.Ragdoll | ObjectLayerMask.IgnoreRayCast);

		if (!Owner.SweepTest(direction, 2.0f, mask, true))
		{
			return;
		}

		Owner.BlackBoard.Desires.RollDirection = dir;
		Owner.WorldState.SetWSProperty(E_PropKey.InDodge, true);
	}

	void ActionSprintBegin()
	{
		if (Owner.IsInCover || Owner.IsEnteringToCover || Owner.IsLeavingToCover ||
			(Owner.GadgetsComponent.IsPerkAvailableForUse(E_PerkID.Sprint) == false &&
			 Owner.GadgetsComponent.IsPerkAvailableForUse(E_PerkID.SprintII) == false &&
			 Owner.GadgetsComponent.IsPerkAvailableForUse(E_PerkID.SprintIII) == false)
						)
			return;

		Owner.BlackBoard.Desires.WeaponTriggerOn = false;
		Owner.BlackBoard.Desires.WeaponTriggerUp = false;
		Owner.BlackBoard.Desires.WeaponTriggerUpDisabled = true;
		Owner.BlackBoard.Desires.Sprint = true;
	}

	void ActionSprintEnd()
	{
		Owner.BlackBoard.Desires.Sprint = false;
	}

	public void HandleActions(AgentAction a)
	{
		/*if (a is AgentActionInjury)
        {
            LastInjuryTime = Time.timeSinceLevelLoad;
        }*/

		if (a is AgentActionDeath)
		{
			Client.Instance.StartSpawnMenu(6.1f);
			GuiFrontendIngame.DontShowPauseMenuUntil = Time.time + 6.1f;
		}
	}

	[uSuite.RPC]
	void RPCClientSynchronizeSpeeds(float maxRunSpeed, float maxWalkSpeed, float perkModifier)
	{
		//Debug.Log( "ClientSynchronizeSpeeds : " + maxRunSpeed + ", " + maxWalkSpeed + ", " + perkModifier );

		Owner.BlackBoard.BaseSetup.MaxRunSpeed = maxRunSpeed;
		Owner.BlackBoard.BaseSetup.MaxWalkSpeed = maxWalkSpeed;

		if (null != Owner.GadgetsComponent && perkModifier >= 0)
		{
			Owner.GadgetsComponent.Perk.CipheredModifier = perkModifier;
		}
	}

	IEnumerator ClientTimeCheck()
	{
		while (true)
		{
			//Debug.Log( ".sent " + uLink.Network.timeInMillis + " time " + Time.time );

			Owner.NetworkView.RPC("RPCTimeCheck", uLink.NetworkPlayer.server);

			yield return new WaitForSeconds(SPEED_TEST_DELAY);
		}
	}
}
