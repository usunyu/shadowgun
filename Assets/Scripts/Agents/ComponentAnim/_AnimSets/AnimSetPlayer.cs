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

[System.Serializable]
public class AnimSetPlayer : AnimSet
{
	public class WeaponAnims
	{
		public string FireIdle;
		public string FireRun;
		public string FireCoverCrouchCenter;
		public string FireCoverCrouchLeft;
		public string FireCoverCrouchRight;
		public string FireCoverStandLeft;
		public string FireCoverStandRight;

		public string ReloadIdle;
		public string ReloadRun;
		public string ReloadCoverStand;
		public string ReloadCoverStandLeft;
		public string ReloadCoverStandRight;
		public string ReloadCoverCrouch;
		public string SwitchIdle;
		public string SwitchCoverCrouch;
		public string SwitchCoverStand;
	}

	static Dictionary<E_WeaponType, WeaponAnims> WeaponAnimations = new Dictionary<E_WeaponType, WeaponAnims>();

	BlackBoard BlackBoard;
	Animation Animation;
	WorldState WorldState;
	ComponentWeapons Weapons;

	static Randomizer InjuryFront = new Randomizer();
	static Randomizer DeathFront = new Randomizer();

	static string SprintAnim = "SprintF";
	static string[] RunAnims = {"RunF", "RunB", "RunL", "RunR"};
	static string[] MeleeAnims = {"MeleeKick", "MeleeAttack1", "MeleeAttack2", "MeleeAtack3"};
//  static string[] WalkAnims  = { "WalkF", "WalkB", "WalkL", "WalkR" };

	static AnimSetPlayer()
	{
		WeaponAnimations.Add(E_WeaponType.Rifle,
							 new WeaponAnims()
							 {
								 FireIdle = "IdleFireSMG",
								 FireRun = "RunFireSMG",
								 FireCoverCrouchCenter = "CoverCrouchFireCenterSMG",
								 FireCoverCrouchLeft = "CoverCrouchFireLeftSMG",
								 FireCoverCrouchRight = "CoverCrouchFireRightSMG",
								 FireCoverStandLeft = "CoverStandFireLeftSMG",
								 FireCoverStandRight = "CoverStandFireRightSMG",
								 ReloadIdle = "IdleReloadSMG",
								 ReloadRun = "IdleReloadSMG",
								 ReloadCoverStand = "IdleReloadSMG",
								 ReloadCoverStandLeft = "IdleReloadSMG",
								 ReloadCoverStandRight = "IdleReloadSMG",
								 ReloadCoverCrouch = "IdleReloadSMG",
								 SwitchIdle = "IdleChangeWeapon",
								 SwitchCoverCrouch = "CoverCrouchChangeWeapon",
								 SwitchCoverStand = "CoverStandChangeWeapon"
							 });

		WeaponAnimations.Add(E_WeaponType.Launcher,
							 new WeaponAnims()
							 {
								 FireIdle = "IdleFireRocket",
								 FireRun = "RunFireRocket",
								 FireCoverCrouchCenter = "CoverCrouchFireCenterRocket",
								 FireCoverCrouchLeft = "CoverCrouchFireLeftRocket",
								 FireCoverCrouchRight = "CoverCrouchFireRightRocket",
								 FireCoverStandLeft = "CoverStandFireLeftRocket",
								 FireCoverStandRight = "CoverStandFireRightRocket",
								 ReloadIdle = "IdleReloadRocket",
								 ReloadRun = "IdleReloadRocket",
								 ReloadCoverStand = "IdleReloadRocket",
								 ReloadCoverStandLeft = "IdleReloadRocket",
								 ReloadCoverStandRight = "IdleReloadRocket",
								 ReloadCoverCrouch = "CoverCrouchReloadRocket",
								 SwitchIdle = "IdleChangeWeapon",
								 SwitchCoverCrouch = "CoverCrouchChangeWeapon",
								 SwitchCoverStand = "CoverStandChangeWeapon"
							 });

		WeaponAnimations.Add(E_WeaponType.Shotgun,
							 new WeaponAnims()
							 {
								 FireIdle = "IdleFireShotgun",
								 FireRun = "RunFireShotgun",
								 FireCoverCrouchCenter = "CoverCrouchFireCenterShotgun",
								 FireCoverCrouchLeft = "CoverCrouchFireLeftShotgun",
								 FireCoverCrouchRight = "CoverCrouchFireRightShotgun",
								 FireCoverStandLeft = "CoverStandFireLeftShotgun",
								 FireCoverStandRight = "CoverStandFireRightShotgun",
								 ReloadIdle = "IdleReloadShotgun",
								 ReloadRun = "IdleReloadShotgun",
								 ReloadCoverStand = "IdleReloadShotgun",
								 ReloadCoverStandLeft = "IdleReloadShotgun",
								 ReloadCoverStandRight = "IdleReloadShotgun",
								 ReloadCoverCrouch = "IdleReloadShotgun",
								 SwitchIdle = "IdleChangeWeapon",
								 SwitchCoverCrouch = "CoverCrouchChangeWeapon",
								 SwitchCoverStand = "CoverStandChangeWeapon"
							 });

		WeaponAnimations.Add(E_WeaponType.Plasma,
							 new WeaponAnims()
							 {
								 FireIdle = "IdleFirePlasma",
								 FireRun = "RunFirePlasma",
								 FireCoverCrouchCenter = "CoverCrouchFireCenterPlasma",
								 FireCoverCrouchLeft = "CoverCrouchFireLeftPlasma",
								 FireCoverCrouchRight = "CoverCrouchFireRightPlasma",
								 FireCoverStandLeft = "CoverStandFireLeftPlasma",
								 FireCoverStandRight = "CoverStandFireRightPlasma",
								 ReloadIdle = "ReloadPlasma",
								 ReloadRun = "ReloadPlasma",
								 ReloadCoverStand = "ReloadPlasma",
								 ReloadCoverStandLeft = "ReloadPlasma",
								 ReloadCoverStandRight = "ReloadPlasma",
								 ReloadCoverCrouch = "ReloadPlasma",
								 SwitchIdle = "IdleChangeWeapon",
								 SwitchCoverCrouch = "CoverCrouchChangeWeapon",
								 SwitchCoverStand = "CoverStandChangeWeapon"
							 });

		WeaponAnimations.Add(E_WeaponType.Sniper,
							 new WeaponAnims()
							 {
								 FireIdle = "IdleFireSniper",
								 FireRun = "RunFireSniper",
								 FireCoverCrouchCenter = "CoverCrouchFireCenterSniper",
								 FireCoverCrouchLeft = "CoverCrouchFireLeftSniper",
								 FireCoverCrouchRight = "CoverCrouchFireRightSniper",
								 FireCoverStandLeft = "CoverStandFireLeftSniper",
								 FireCoverStandRight = "CoverStandFireRightSniper",
								 ReloadIdle = "IdleReloadSniper",
								 ReloadRun = "IdleReloadSniper",
								 ReloadCoverStand = "IdleReloadSniper",
								 ReloadCoverStandLeft = "IdleReloadSniper",
								 ReloadCoverStandRight = "IdleReloadSniper",
								 ReloadCoverCrouch = "IdleReloadSniper",
								 SwitchIdle = "IdleChangeWeapon",
								 SwitchCoverCrouch = "CoverCrouchChangeWeapon",
								 SwitchCoverStand = "CoverStandChangeWeapon"
							 });

		InjuryFront.Add("InjuryIdleFront1");
		InjuryFront.Add("InjuryIdleFront2");
		InjuryFront.Add("InjuryIdleFront3");

		DeathFront.Add("DeathIdleFront1");
//		DeathFront.Add("DeathIdleFront2");
//		DeathFront.Add("DeathIdleFront3");
	}

	void Awake()
	{
		BlackBoard = GetComponent<AgentHuman>().BlackBoard;
		Weapons = GetComponent<ComponentWeapons>();
		Animation = GetComponent<Animation>();

		//Animation["AimU"].layer = 2;
		//Animation["AimD"].layer = 2;

		//moved to NtworkInstantiatePool.cs yo optimalize instantiate (spawn/respawn)
		/*Animation["AimU"].AddMixingTransform(transform.Find("pelvis/stomach"));
        Animation["AimD"].AddMixingTransform(transform.Find("pelvis/stomach"));

        Animation["IdleChangeWeapon"].AddMixingTransform(transform.Find("pelvis/stomach"));*/
	}

	void Start()
	{
		WorldState = GetComponent<AgentHuman>().WorldState;
	}

	public override string GetBlockAnim(E_BlockState state)
	{
		return null;
	}

	public override string GetIdleAnim()
	{
		if (BlackBoard.Cover != null)
		{
			if (BlackBoard.CoverPosition == E_CoverDirection.Middle)
				return BlackBoard.CoverPose == E_CoverPose.Stand ? "CoverStandIdleCenter" : "CoverCrouchIdleCenter";
			else if (BlackBoard.CoverPosition == E_CoverDirection.Left)
				return BlackBoard.CoverPose == E_CoverPose.Stand ? "CoverStandIdleLeft" : "CoverCrouchIdleLeft";
			else if (BlackBoard.CoverPosition == E_CoverDirection.Right)
				return BlackBoard.CoverPose == E_CoverPose.Stand ? "CoverStandIdleRight" : "CoverCrouchIdleRight";
		}

		return "Idle";
	}

	public override string GetIdleActionAnim()
	{
		return "Idle";
	}

	public override string GetMoveAnim()
	{
		if (BlackBoard.Cover)
		{
			if (BlackBoard.MoveType == E_MoveType.StrafeLeft)
				return BlackBoard.CoverPose == E_CoverPose.Stand ? "CoverStandStrafeLeft" : "CoverCrouchStrafeLeft";
			else if (BlackBoard.MoveType == E_MoveType.StrafeRight)
				return BlackBoard.CoverPose == E_CoverPose.Stand ? "CoverStandStrafeRight" : "CoverCrouchStrafeRight";
		}

		switch (BlackBoard.MotionType)
		{
		case E_MotionType.Sprint:
			return SprintAnim;
		case E_MotionType.Run:
			return RunAnims[(int)BlackBoard.MoveType];
		case E_MotionType.Walk:
			return RunAnims[(int)BlackBoard.MoveType]; //return WalkAnims[(int)BlackBoard.MotionType];
		}

		Debug.LogError(" No move animation for player " + BlackBoard.MotionType + " " + BlackBoard.MoveType);

		return "Idle";
	}

	public override string GetRotateAnim(E_RotationType rotationType)
	{
		return rotationType == E_RotationType.Left ? "TurnL" : "TurnR";
	}

	public override string GetDodgeAnim(E_StrafeDirection dir)
	{
		return "Idle";
	}

	public override string GetWeaponAnim(E_WeaponAction action)
	{
		E_WeaponType t = Weapons.GetCurrentWeapon().WeaponType;
		switch (action)
		{
		case E_WeaponAction.Fire:
			if (BlackBoard.Cover != null)
			{
				if (BlackBoard.CoverPose == E_CoverPose.Crouch)
				{
					switch (BlackBoard.CoverPosition)
					{
					case E_CoverDirection.Left:
						return WeaponAnimations[t].FireCoverCrouchLeft;
					case E_CoverDirection.Right:
						return WeaponAnimations[t].FireCoverCrouchRight;
					default:
						return WeaponAnimations[t].FireCoverCrouchCenter;
					}
				}
				else
				{
					switch (BlackBoard.CoverPosition)
					{
					case E_CoverDirection.Left:
						return WeaponAnimations[t].FireCoverStandLeft;
					case E_CoverDirection.Right:
						return WeaponAnimations[t].FireCoverStandRight;
					}
				}
			}

			if (BlackBoard.MotionType == E_MotionType.None)
				return WeaponAnimations[t].FireIdle;

			return WeaponAnimations[t].FireRun;
		case E_WeaponAction.Reload:
			if (BlackBoard.Cover != null)
			{
				if (BlackBoard.CoverPose == E_CoverPose.Crouch)
				{
					return WeaponAnimations[t].ReloadCoverCrouch;
				}
				else
				{
					switch (BlackBoard.CoverPosition)
					{
					case E_CoverDirection.Left:
						return WeaponAnimations[t].ReloadCoverStandLeft;
					case E_CoverDirection.Right:
						return WeaponAnimations[t].ReloadCoverStandRight;
					default:
						return WeaponAnimations[t].ReloadCoverStand;
					}
				}
			}

			if (BlackBoard.MotionType == E_MotionType.None)
				return WeaponAnimations[t].ReloadIdle;
			else
				return WeaponAnimations[t].ReloadRun;
		case E_WeaponAction.Switch:
			if (BlackBoard.Cover != null)
			{
				if (BlackBoard.CoverPose == E_CoverPose.Crouch)
					return WeaponAnimations[t].SwitchCoverCrouch;

				return WeaponAnimations[t].SwitchCoverStand;
			}

			return WeaponAnimations[t].SwitchIdle;
		}
		return "Idle";
	}

	public override float GetWeaponAnimTime(E_WeaponType type, E_WeaponAction action)
	{
		string animName;
		switch (action)
		{
		case E_WeaponAction.Switch:
			animName = WeaponAnimations[type].SwitchIdle;
			break;
		case E_WeaponAction.Fire:
			animName = BlackBoard.MotionType == E_MotionType.None ? WeaponAnimations[type].FireIdle : WeaponAnimations[type].FireRun;
			break;
		case E_WeaponAction.Reload:
			animName = BlackBoard.MotionType == E_MotionType.None ? WeaponAnimations[type].ReloadIdle : WeaponAnimations[type].ReloadRun;
			break;
		default:
			throw new System.NotImplementedException();
		}

		if (animName != null && Animation[animName] != null)
			return Animation[animName].length;

		return 0;
	}

	public override string GetInjuryAnim()
	{
		if (BlackBoard.Cover != null)
		{
			if (WorldState.GetWSProperty(E_PropKey.CoverState).GetCoverState() == E_CoverState.Middle)
			{
				return BlackBoard.CoverPose == E_CoverPose.Stand ? "InjuryCoverStandIdle" : "InjuryCoverCrouchIdle";
			}
			else
			{
				if (BlackBoard.CoverPosition == E_CoverDirection.Middle)
					return BlackBoard.CoverPose == E_CoverPose.Stand ? "InjuryCoverStandIdle" : "InjuryCoverCrouchAimCenter";
				else if (BlackBoard.CoverPosition == E_CoverDirection.Left)
					return BlackBoard.CoverPose == E_CoverPose.Stand ? "InjuryCoverStandAimLeft" : "InjuryCoverCrouchAimLeft";
				else if (BlackBoard.CoverPosition == E_CoverDirection.Right)
					return BlackBoard.CoverPose == E_CoverPose.Stand ? "InjuryCoverStandAimRight" : "InjuryCoverCrouchAimRight";
			}
		}

		return InjuryFront.Get();
	}

	public override string GetDeathAnim()
	{
/*		
		//beny: we're using just ragdolls, animation is played only on server (mostly just to make the death somehow visible even on the server)
		if (BlackBoard.Cover != null)
        {
            if (WorldState.GetWSProperty(E_PropKey.CoverState).GetCoverState() != E_CoverState.Aim)
            {
                return BlackBoard.CoverPose == E_CoverPose.Stand ? "DeathCoverStandIdle" : "DeathCoverCrouchIdle";
            }
            else
            {
                if (BlackBoard.CoverPosition == E_CoverDirection.Middle)
                    return BlackBoard.CoverPose == E_CoverPose.Stand ? "" : "DeathCoverCrouchAimCenter";
                else if (BlackBoard.CoverPosition == E_CoverDirection.Left)
                    return BlackBoard.CoverPose == E_CoverPose.Stand ? "DeathCoverStandAimLeft" : "DeathCoverCrouchAimLeft";
                else if (BlackBoard.CoverPosition == E_CoverDirection.Right)
                    return BlackBoard.CoverPose == E_CoverPose.Stand ? "DeathCoverStandAimRight" : "DeathCoverCrouchAimRight";
            }
        }

        if (BlackBoard.MotionType == E_MotionType.Run)
        {
            if (BlackBoard.MoveType == E_MoveType.StrafeLeft)
                return "DeathRunL";
            else if (BlackBoard.MoveType == E_MoveType.StrafeRight)
                return "DeathRunR";
            else if (BlackBoard.MoveType == E_MoveType.Forward)
                return "DeathRunF";
        }
*/
		return DeathFront.Get();
	}

	public override string GetKnockdownAnim(E_KnockdownState knockdownState)
	{
		return "InjuryMeleeKick";
	}

	public override string GetCoverAnim(E_CoverAnim type, E_CoverPose pose, E_CoverDirection direction)
	{
		switch (type)
		{
		case E_CoverAnim.Enter:
			return pose == E_CoverPose.Stand ? "CoverStandEnter" : "CoverCrouchEnter";
		case E_CoverAnim.Leave:
			return pose == E_CoverPose.Stand ? "CoverStandLeave" : "CoverCrouchLeave";
		case E_CoverAnim.AimStart:
			switch (direction)
			{
			case E_CoverDirection.Left:
				return pose == E_CoverPose.Stand ? "CoverStandAimLeftStart" : "CoverCrouchAimLeftStart";
			case E_CoverDirection.Right:
				return pose == E_CoverPose.Stand ? "CoverStandAimRightStart" : "CoverCrouchAimRightStart";
			default:
				return pose == E_CoverPose.Stand ? "" : "CoverCrouchAimCenterStart";
			}
		case E_CoverAnim.AimEnd:
			switch (direction)
			{
			case E_CoverDirection.Left:
				return pose == E_CoverPose.Stand ? "CoverStandAimLeftBack" : "CoverCrouchAimLeftBack";
			case E_CoverDirection.Right:
				return pose == E_CoverPose.Stand ? "CoverStandAimRightBack" : "CoverCrouchAimRightBack";
			default:
				return pose == E_CoverPose.Stand ? "" : "CoverCrouchAimCenterBack";
			}
		case E_CoverAnim.JumpOver:
			return "JumpOverToIdle";
		case E_CoverAnim.JumpUp:
			return "CoverCrouchJumpUp";
		case E_CoverAnim.LeaveLeft:
			return pose == E_CoverPose.Stand ? "CoverStandLeftLeave" : "CoverCrouchLeftLeave";
		case E_CoverAnim.LeaveRight:
			return pose == E_CoverPose.Stand ? "CoverStandRightLeave" : "CoverCrouchRightLeave";
		}

		throw new System.ArgumentOutOfRangeException();
	}

	public override string GetTeleportAnim(AnimSet.E_TeleportAnim type)
	{
		throw new System.NotImplementedException();
	}

	public override string GetInjuryCritAnim()
	{
		throw new System.NotImplementedException();
	}

	public override string GetAimAnim(E_AimDirection direction, E_CoverPose pose, E_CoverDirection position)
	{
		if (BlackBoard.Cover != null)
		{
			switch (direction)
			{
			case E_AimDirection.Left:
				switch (position)
				{
				case E_CoverDirection.Left:
					return pose == E_CoverPose.Stand ? "CoverStandAimLeftMaxL" : "CoverCrouchAimLeftMaxL";
				case E_CoverDirection.Right:
					return pose == E_CoverPose.Stand ? "CoverStandAimRightMaxL" : "CoverCrouchAimRightMaxL";
				default:
					return pose == E_CoverPose.Stand ? "" : "CoverCrouchAimCenterMaxL";
				}
			case E_AimDirection.Right:
				switch (position)
				{
				case E_CoverDirection.Left:
					return pose == E_CoverPose.Stand ? "CoverStandAimLeftMaxR" : "CoverCrouchAimLeftMaxR";
				case E_CoverDirection.Right:
					return pose == E_CoverPose.Stand ? "CoverStandAimRightMaxR" : "CoverCrouchAimRightMaxR";
				default:
					return pose == E_CoverPose.Stand ? "" : "CoverCrouchAimCenterMaxR";
				}
			case E_AimDirection.Up:
				switch (position)
				{
				case E_CoverDirection.Left:
					return pose == E_CoverPose.Stand ? "CoverStandAimLeftMaxU" : "CoverCrouchAimLeftMaxU";
				case E_CoverDirection.Right:
					return pose == E_CoverPose.Stand ? "CoverStandAimRightMaxU" : "CoverCrouchAimRightMaxU";
				default:
					return pose == E_CoverPose.Stand ? "" : "CoverCrouchAimCenterMaxU";
				}
			case E_AimDirection.Down:
				switch (position)
				{
				case E_CoverDirection.Left:
					return pose == E_CoverPose.Stand ? "CoverStandAimLeftMaxD" : "CoverCrouchAimLeftMaxD";
				case E_CoverDirection.Right:
					return pose == E_CoverPose.Stand ? "CoverStandAimRightMaxD" : "CoverCrouchAimRightMaxD";
				default:
					return pose == E_CoverPose.Stand ? "" : "CoverCrouchAimCenterMaxD";
				}
			}
		}
		else
		{
			if (direction == E_AimDirection.Down)
				return "AimD";
			else if (direction == E_AimDirection.Up)
				return "AimU";
		}

		throw new System.ArgumentOutOfRangeException();
	}

	public override string GetRollAnim(E_Direction moveType)
	{
		switch (moveType)
		{
		case E_Direction.Forward:
			return "RollF";
		case E_Direction.Backward:
			return "RollB";
		case E_Direction.Left:
			return "RollL";
		case E_Direction.Right:
			return "RollR";
		}
		return "Idle";
	}

	public override string GetGadgetAnim(E_ItemID gadget, E_CoverPose coverPose, E_CoverDirection coverDirection)
	{
		ItemSettings setting = ItemSettingsManager.Instance.Get(gadget);

		switch (setting.ItemBehaviour)
		{
// FIX IT
		case E_ItemBehaviour.Throw:
			if (BlackBoard.Cover)
			{
				if (coverDirection == E_CoverDirection.Middle)
					return coverPose == E_CoverPose.Stand ? "" : "CoverCrouchThrowCenter";
				else if (coverDirection == E_CoverDirection.Left)
					return coverPose == E_CoverPose.Stand ? "CoverStandThrowLeft" : "CoverCrouchThrowLeft";
				else if (coverDirection == E_CoverDirection.Right)
					return coverPose == E_CoverPose.Stand ? "CoverStandThrowRight" : "CoverCrouchThrowRight";
			}
			else
			{
				return "RunThrow"; //now we're using the same half-body anim for both idle and move
/*					if ( BlackBoard.MotionType == E_MotionType.Walk || BlackBoard.MotionType == E_MotionType.Run )
						return "RunThrow";
					else
                    	return "IdleThrow";
*/
			}
			break;
		case E_ItemBehaviour.Place:
			return "Drop";
		}

		throw new System.ArgumentOutOfRangeException(" Gadget: " + gadget + " behaviour " + setting.ItemBehaviour);
	}

	public override string GetMeleeAnim(E_MeleeType type)
	{
		return MeleeAnims[(int)type];
	}
}
