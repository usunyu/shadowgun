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

/***************************************************************
 * Class Name :	Blackboard
 * Function   : Central memory for GOAPController and other subsystems. 
 * 
 * Created by : Marek Rabas
 *
 **************************************************************/

using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BlackBoard
{
	[System.Serializable]
	public class BaseSettings
	{
//		public float MaxSprintSpeed = 5.5f;
		public float m_MaxRunSpeed = 4.0f;
		public float m_MaxWalkSpeed = 2.0f;
		public float MaxCoverSpeed = 1;

		public float MaxRunSpeed
		{
			get { return MathUtils.CipherValue(m_MaxRunSpeed, Cipher1); }
			set { m_MaxRunSpeed = MathUtils.CipherValue(value, Cipher1); }
		}

		public float MaxWalkSpeed
		{
			get { return MathUtils.CipherValue(m_MaxWalkSpeed, Cipher1); }
			set { m_MaxWalkSpeed = MathUtils.CipherValue(value, Cipher1); }
		}

		public float MaxHealth = 100;

		public float SpeedSmooth = 2.0f;
		public float RollDistance = 3.0f;

		// true for apply input force also - it will cause slowdown of acceleration (based on input)
		public bool UseMoveSpeedModifier = false;

		//used to xor values that we want to hide; it's changed in Reset()
		uint m_Cipher1 = 0;

		public uint Cipher1
		{
			get { return m_Cipher1; }
			set { m_Cipher1 = value; }
		}
	}

	///////////////// GOAP Settings /////////////////////////////
	[System.Serializable]
	public class GoapSettings
	{
		public float DodgeRelevancy = 0.9f;
		public float MoveRelevancy = 0.5f;
		public float GoToRelevancy = 0.5f;
		public float KillTargetRelevancy = 0.85f;
		public float PlayAnimRelevancy = 0.95f;
		public float UseWorlObjectRelevancy = 0.9f;
		public float CoverRelevancy = 0.8f;
		public float ReloadRelevancy = 0.8f;
		public float TeleportRelevancy = 0.9f;
		public float WeaponChangeRelevancy = 0.8f;
		public float MeleeRelevancy = 0.8f;

		public float DodgeDelay = 5.0f;
		public float MoveDelay = 0.1f;
		public float GoToDelay = 0.5f;
		public float UseWorlObjectDelay = 5.0f;
		public float CoverDelay = 1.0f;
		public float ReloadDelay = 1.0f;
		public float TeleportDelay = 4;
		public float WeaponChangeDelay = 0.1f;
		public float PlayAnimDelay = 0.1f;
		public float MeleeDelay = 0.8f;
	}

	// Damage settings
	[System.Serializable]
	public class DamageSettings
	{
		public bool Invulnerable = false;

		public float FallHeightForDamage = 4.0f;
		public float FallHeightForKill = 10.0f;
	}

	[System.Serializable]
	public class CoverSettings
	{
		public float RightMaxUp = 40.0f;
		public float RightMaxDown = 40.0f;
		public float RightMaxRight = 70.0f;
		public float RightMaxLeft = 20.0f;

		public float LeftMaxUp = 40.0f;
		public float LeftMaxDown = 40.0f;
		public float LeftMaxRight = 20.0f;
		public float LeftMaxLeft = 70.0f;

		public float CenterMaxUp = 40.0f;
		public float CenterMaxDown = 20.0f;
		public float CenterMaxRight = 70.0f;
		public float CenterMaxLeft = 70.0f;

		// Keep this distance between two agents hiding at the same cover
		public float MultiCoverSafeDist = 0.3f;
	}

	[System.Serializable]
	public class StepsSettings
	{
		public float RunMinDelay = 0.35f;
		public float RunMaxDelay = 0.38f;
		public float WalkMinDelay = 0.64f;
		public float WalkMaxDelay = 0.73f;
		public float AnimMinDelay = 0.64f;
		public float AnimMaxDelay = 0.73f;
	}

	public class DesiredData
	{
		public Quaternion Rotation;
		public Vector3 MoveDirection;
		public Vector3 FireDirection;
		public Vector3 FireTargetPlace;
		public bool WalkOnly;
		public bool Sprint;
//        public float MoveSpeedModifier;

		float m_MoveSpeedModifier;

		public float MoveSpeedModifier
		{
			get { return m_MoveSpeedModifier; }
			set { m_MoveSpeedModifier = value; }
		}

		public CoverAIInfo CoverNear = new CoverAIInfo();

		public Cover CoverSelected;
		public E_CoverDirection CoverPosition;

		public InteractionObject InteractionObject;

		public string Animation;

		public E_WeaponID Weapon;
		public bool WeaponTriggerOn;
		public bool WeaponTriggerUp;
		public bool WeaponTriggerUpDisabled;

		public E_ItemID Gadget;

		public Vector3 TeleportDestination = new Vector3();
		public Quaternion TeleportRotation = new Quaternion();

		public Agent MeleeTarget;
		public bool MeleeTriggerOn;

		public E_Direction RollDirection;

		public void Reset()
		{
			MoveDirection = Vector3.zero;
			FireDirection = Vector3.zero;
			WalkOnly = false;
			Sprint = false;
			MoveSpeedModifier = 1;
			CoverSelected = null;
			CoverPosition = E_CoverDirection.Unknown;

			CoverNear.Reset();

			InteractionObject = null;
			Animation = null;
			Weapon = E_WeaponID.None;
			Gadget = E_ItemID.None;
			WeaponTriggerOn = false;
			WeaponTriggerUp = false;
			WeaponTriggerUpDisabled = true;

			MeleeTarget = null;
			MeleeTriggerOn = false;
			RollDirection = E_Direction.None;
		}
	}

	public class ProxySpawnData
	{
		public bool IsValid = false;
		public bool Death = false;
		public E_WeaponID CurrentWeapon = E_WeaponID.None;

		public Cover Cover = null;
		public E_CoverDirection CoverPosition = E_CoverDirection.Unknown;
		public E_CoverPose CoverPose;
		public bool CoverFiring = false;

		public void Reset()
		{
			IsValid = false;
			Death = false;
			CurrentWeapon = E_WeaponID.None;

			Cover = null;
			CoverPosition = E_CoverDirection.Unknown;
			CoverFiring = false;
		}
	}

	//////////////// AGENT ACTIONS ///////////////////////
	List<AgentAction> m_ActiveActions = new List<AgentAction>();

	[System.NonSerialized] public AgentHuman Owner;
	[System.NonSerialized] public GameObject GameObject; // { get { return myGameObject; } private set { myGameObject = value; } }

	[System.NonSerialized] public bool IsPlayer = false;

	/////////////// Runtime data ////////////////////////////

	public GoapSettings GoapSetup = new GoapSettings();
	public BaseSettings BaseSetup = new BaseSettings();
	public DamageSettings DamageSetup = new DamageSettings();
	public CoverSettings CoverSetup = new CoverSettings();
	public StepsSettings StepsSetup = new StepsSettings();

	///////////////// STATS /////////////////////////////
	[System.NonSerialized] public DesiredData Desires = new DesiredData();

	[System.NonSerialized] public bool KeepMotion = false;
									   //beny: added due to 'UseItem while Move' feature; this tells whether the new AnimState should reset Motion or not
	[System.NonSerialized] public E_MotionType MotionType = E_MotionType.None;
	[System.NonSerialized] public E_MoveType MoveType = E_MoveType.None;
	[System.NonSerialized] public Vector3 MoveDir;
	[System.NonSerialized] public Vector3 FireDir;
	[System.NonSerialized] public Vector3 Velocity;

	[System.NonSerialized] public E_CoverPose CoverPose = E_CoverPose.Stand;
	[System.NonSerialized] public E_CoverDirection CoverPosition = E_CoverDirection.Middle;
	[System.NonSerialized] public Cover Cover = null;
	[System.NonSerialized] public float CoverTime;
	[System.NonSerialized] public bool CoverFire;

	[System.NonSerialized] public float m_Speed = 0;

	public float Speed
	{
		get { return MathUtils.CipherValue(m_Speed, BaseSetup.Cipher1); }
		set { m_Speed = MathUtils.CipherValue(value, BaseSetup.Cipher1); }
	}

	[System.NonSerialized] public float Health = 100;
	[System.NonSerialized] public float SpawnProtectionRestTime = 0;

	public class DamageData
	{
		public uLink.NetworkPlayer Attacker;
		public float Damage;
	}

	[System.NonSerialized]
	//public Stack<DamageData > AttackersDamageData = new Stack<DamageData>();
	public List<DamageData> AttackersDamageData = new List<DamageData>();

	[System.NonSerialized] public InteractionObject InteractionObject;

	[System.NonSerialized] public Vector3 DirToTarget;
	[System.NonSerialized] public bool DontUpdate = true;
	[System.NonSerialized] public bool ReactOnHits = true;
	[System.NonSerialized] public bool BusyAction = false;
	[System.NonSerialized] public bool Invulnerable = false;
	[System.NonSerialized] public float AbsorbedDamage;
	[System.NonSerialized] public bool InKnockDown;

	[System.NonSerialized] public float LastInjuryTime;

	[System.NonSerialized] public float IdleTimer = 0;
	[System.NonSerialized] public bool DontDeathAnimMove = false;
	[System.NonSerialized] public bool GrenadesExplodeOnHit = true;

	[System.NonSerialized] public bool IsDetected = false; //is the agent currently detected by a Detector gadget?

	[System.NonSerialized] public bool AimAnimationsEnabled = false;

	public ProxySpawnData ProxyDataForSpawn = new ProxySpawnData();

	public bool Stop { get; set; }

	public delegate void AgentActionHandler(AgentAction a);

	public AgentActionHandler ActionHandler;

	public float RealMaxHealth
	{
		get { return BaseSetup.MaxHealth*Owner.HealthModifier; }
	}

	public float RealMaxSprintSpeed
	{
		get { return RealMaxRunSpeed*Owner.SprintSpeedModifier; }
	}

	public float RealMaxRunSpeed
	{
		get { return BaseSetup.MaxRunSpeed*Owner.RunSpeedModifier; }
	}

	public float RealMaxWalkSpeed
	{
		get { return BaseSetup.MaxWalkSpeed*Owner.RunSpeedModifier; }
	}

	public void Reset()
	{
		Desires.Reset();
		ProxyDataForSpawn.Reset();

		for (int i = 0; i < m_ActiveActions.Count; i++)
			ActionDone(m_ActiveActions[i]);

		m_ActiveActions.Clear();

		Stop = false;
		MotionType = E_MotionType.None;
		MoveType = E_MoveType.None;

//		Speed = 0;	//ciphered below

		Health = RealMaxHealth;

		IdleTimer = 0;
		CoverTime = 0;
		Cover = null;
		CoverPosition = E_CoverDirection.Unknown;
		CoverFire = false;

		MoveDir = Vector3.zero;

		FireDir = Owner.Transform.forward;

		Desires.Rotation = Owner.Transform.rotation;
		Desires.FireDirection = Owner.Transform.forward;

		InteractionObject = null;

		Invulnerable = false;
		AbsorbedDamage = 0;
		ReactOnHits = true;
		BusyAction = false;
		DontUpdate = false;
		InKnockDown = false;
		LastInjuryTime = 0;

		KeepMotion = false;
		IsDetected = false;

		AttackersDamageData.Clear();

		//mangle values
//		Debug.Log ("SPEED 3: Reset, m_MaxRunSpeed=" + BaseSetup.m_MaxRunSpeed + ", MaxRunSpeed=" + BaseSetup.MaxRunSpeed + ", Speed=" + Speed + ", CIPHER=0x" + string.Format("{0:X}", BaseSetup.Cipher1) );

		float run_spd = BaseSetup.MaxRunSpeed;
		float walk_spd = BaseSetup.MaxWalkSpeed;
		BaseSetup.Cipher1 = (uint)(new System.Random(Time.frameCount).Next());
		BaseSetup.MaxRunSpeed = run_spd; //re-assign to re-cipher with new Cipher value
		BaseSetup.MaxWalkSpeed = walk_spd;
		Speed = 0;

//		Debug.Log ("SPEED 4: Reset, m_MaxRunSpeed=" + BaseSetup.m_MaxRunSpeed + ", MaxRunSpeed=" + BaseSetup.MaxRunSpeed + ", Speed=" + Speed + ", CIPHER=0x" + string.Format("{0:X}", BaseSetup.Cipher1) );
	}

	//////////////// ACTIONS /////////////////////////

	public void ActionAdd(AgentAction action)
	{
		IdleTimer = 0;

		m_ActiveActions.Add(action);

		ActionHandler(action);
	}

	public void Update()
	{
		Owner.WorldState.SetWSProperty(E_PropKey.TargetNode, Owner.Position);

		if (SpawnProtectionRestTime > 0.0f)
		{
			SpawnProtectionRestTime -= Time.deltaTime;
			SpawnProtectionRestTime = Mathf.Max(SpawnProtectionRestTime, 0.0f);
		}
	}

	public void PostUpdate()
	{
		for (int i = 0; i < m_ActiveActions.Count; i++)
		{
			if (m_ActiveActions[i].IsActive())
				continue;

			ActionDone(m_ActiveActions[i]);
			m_ActiveActions.RemoveAt(i);
			return;
		}
	}

	void ActionDone(AgentAction action)
	{
		AgentActionFactory.Return(action);
	}

	public void RemoveNetworkPlayerFromDamageData(uLink.NetworkPlayer player)
	{
		if (null != AttackersDamageData)
		{
			for (int i = AttackersDamageData.Count - 1; i >= 0; i--)
			{
				if (player == AttackersDamageData[i].Attacker)
				{
					AttackersDamageData.RemoveAt(i);
				}
			}
		}
	}
}
