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
using System.Collections.Specialized;
using System;
using System.Text;


public enum E_LayerType
{
	None = -1,
	Hud = 1 << 8,
	Player = 1 << 9,
	Enemy = 1 << 10,
	CollisonMove = 1 << 13,
	CollisionDecal = 1 << 14,

	PhysicDefault = 1 << 28,
	PhysicMetal = 1 << 29,
	PhysicWood = 1 << 30,
	PhysicBody = 1 << 31,
}

static class MPSettings
{
	public const float Version = 1.0f;
}

public enum E_Team
{
	None,
	Good,
	Bad,
	Last
}

// should be used by every dictionary that uses 'E_Team' as key to speed things little bit up :o)
public class TeamComparer : IEqualityComparer<E_Team>
{
	public readonly static TeamComparer Instance = new TeamComparer();

	public bool Equals(E_Team A, E_Team B)
	{
		return A == B;
	}

	public int GetHashCode(E_Team X)
	{
		return (int)X;
	}
}

public enum E_DisconnectStatus
{
	Unknown,
	Requested,
	RoundEnd,
	TimeOut,
	Crash,
	LostFocus
}

public enum E_MPServerType
{
	Dedicated,
	Host,
}

public enum E_MPPlayerHostType
{
	Owner,
	Server,
	Proxy,
}

public enum E_MPGameType
{
	DeathMatch,
	ZoneControl,
	None
}

public enum E_MeleeType
{
	First,
	Second,
	Third,
	Fourth,
}

public enum E_WeaponAction
{
	Switch,
	Fire,
	Reload,
}

public enum E_WeaponState
{
	NotInHands,
	Ready,
	Attacking,
	Reloading,
	Empty,
	Busy,
}

public enum E_BlockState
{
	None = -1,
	Start = 0,
	Loop,
	End,
	HitBlocked,
	Failed
}

public enum E_KnockdownState
{
	None = -1,
	Down = 0,
	Loop,
	Up,
	Fatality,
}

public enum E_PlayerType
{
	Soldier1 = 0,
	Mutant1,
	Max,
}

public enum E_EnemyType
{
	None = -1,
	Mutant,
	Flyer,
	Spider,
	Heavy,
	MutantShield,
	Elite,
	Kamikaze,
	BigSpider,
	FinalBoss,

	LightTurret,
	DropShip,
	Turret,

	Max
}

public enum E_GameState
{
	MainMenu,
	IngameMenu,
	Game,
	SaleScreen,
	Tutorial,
	Shop,
}

public enum E_GameType
{
	Tutorial,
	SaleScreen,
	Multiplayer,
}

public enum E_RotationType
{
	None,
	Left,
	Right
}

public enum E_CoverDirection
{
	Unknown,
	Middle,
	Left,
	Right,
}

public enum E_CoverPose
{
	Stand,
	Crouch,
	None // TODO : this should be first
	// this value can be transfered via network, change it only when both server and client are build
}

public enum E_CoverState
{
	None,
	Middle,
	Edge,
	Aim,
}

public enum E_MotionType
{
	None,
	Walk,
	Run,
	Attack,
	Injury,
	Knockdown,
	Death,
	AnimationDrive,
	Roll,
	Sprint
}

public enum E_MoveType
{
	None = -1,
	Forward = 0,
	Backward = 1,
	StrafeLeft = 2,
	StrafeRight = 3,
}

//beny: identifies the body part (that was hit)
public enum E_BodyPart
{
	Body,
	Head,
	LeftArm,
	RightArm,
	LeftLeg,
	RightLeg,
	Hat,
}

public enum E_LookType
{
	None,
	TrackTarget,
}

public enum E_Direction
{
	Forward,
	Backward,
	Left,
	Right,
	None,
}

public enum E_StrafeDirection
{
	Left,
	Right,
}

public enum E_AimDirection
{
	Left,
	Right,
	Up,
	Down,
}

public enum E_CommandID
{
	Affirmative,
	Negative,
	Attack,
	Help,
	CoverMe,
	Back,
	OutOfAmmo,
	Medic,
	Max
}

public enum E_AddMoneyAction
{
	None,
	KillDM,
	KillZC,
	KillSpider,
	KillSentry,
	KillMine,
	ZoneControl,
	MedKit,
	AmmoKit,
	Rank,
	DM
}

// don't modify this enum withou changes in cloud-related parts of code (app engine)
public enum E_SlotMachinePrizeType : short
{
	None,
	Small,
	Medium,
	Big,
	Jackpot
}

public enum E_SlotMachineSymbol : short
{
	None = -1,
	First = 0,
	Small1 = First,
	Small2,
	Small3,
	Small4,
	Small5,
	Medium1,
	Medium2,
	Big1,
	Jackpot,
	Last = Jackpot,
}

public enum E_UserAcctKind
{
	Normal,
	Guest,
	Facebook,
	Any
}

public enum E_AppProvider
{
	Madfinger,
	Incross_Obsolete //Keeping it here because the given value is still reserved on the could.
	//In case of any new provider, add it to the end of the list but do not remove any existing items
}

class Types
{
}
