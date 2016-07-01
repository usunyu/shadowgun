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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface AgentCallbackterface
{
	void RecieveHit(AgentHuman attacker, E_WeaponID weapon);
	void RecieveKnockDown(AgentHuman attacker, E_WeaponID weapon);

	void GOAPGoalActivate(E_GOAPGoals goal);
	void GOAPGoalDeactivated(E_GOAPGoals goal);
}

[System.Serializable]
public class AgentHuman : Agent, IHitZoneOwner
{
	public static float AgentUpdateTime = 0.1f;

	[System.Serializable]
	public class SoundInfo
	{
		public AudioClip[] Steps = new AudioClip[0];
		public AudioClip[] StepsMetal = new AudioClip[0];
		public AudioClip[] StepsWater = new AudioClip[0];
		public AudioClip[] CoverIn = new AudioClip[0];
		public AudioClip[] CoverJump = new AudioClip[0];
		public AudioClip[] Injuries = new AudioClip[0];
		public AudioClip[] Deaths = new AudioClip[0];
		public AudioClip[] Teleport = new AudioClip[0];
		public AudioClip[] Throw = new AudioClip[0];
		public AudioClip[] Use = new AudioClip[0];
		public AudioClip[] Heal = new AudioClip[0];
		public AudioClip[] Melee = new AudioClip[0];
		public AudioClip[] Knockdown = new AudioClip[0];

		public AudioClip[] CmdAffirmative = new AudioClip[0];
		public AudioClip[] CmdNegative = new AudioClip[0];
		public AudioClip[] CmdHelp = new AudioClip[0];
		public AudioClip[] CmdAttack = new AudioClip[0];
		public AudioClip[] CmdCoverMe = new AudioClip[0];
		public AudioClip[] CmdBack = new AudioClip[0];
		public AudioClip[] CmdOutOfAmmo = new AudioClip[0];
		public AudioClip[] CmdMedic = new AudioClip[0];
	}

	//properties to set by designer

	public E_Team Team;

	public BlackBoard BlackBoard = new BlackBoard(); // { get { return BlackBoard; } private set { BlackBoard = value; } }
	public SoundInfo SoundSetup = new SoundInfo();

	public Material FadeoutMaterial;
	public Material DiffuseMaterial;

	public Transform TransformEye;
	public Transform TransformTarget;

	//ragdoll (see AnimStateDeathRagdoll)
	public Transform RagdollRoot;
	public Rigidbody RigidBodyForce;
	public float RigidBodyPushingForce = 2;
	Rigidbody[] RigidBodies;

	GameObject CollSphere; // blocks player when Ragdoll character is used
	public Collider[] Colliders; // "standard" colliders (on prefabs with ragdoll)
	public Collider[] ExplosionHitTargets;

	public Transform HatTarget;
	GameObject Hat;

	public GameObject CommandIcon;
	public Material CommandIconMaterial;

	// capa
	string m_DominantAnimName;
	float m_DominantAnimTimer;
	bool m_DominantAnimSampled = false;

	public bool debugGOAP = false;
	public bool debugAnims = false;
	public bool debugGame = true;
	//public bool debugMemory = false;
	//public bool debugAI = false;

	// fall damage support
	float m_FallingTime = 0;
	float m_FallingStart = 0;

	bool m_bRestoreShadow = false;

	//agent overrides
	public override bool IsAlive
	{
		get { return IsAliveInternal && GameObject.activeSelf; }
	}

	bool IsAliveInternal = true;

	public override bool IsVisible
	{
		get { return Renderer == null ? false : Renderer.isVisible; }
	}

	public override bool IsInvulnerable
	{
		get { return (BlackBoard.DamageSetup.Invulnerable || BlackBoard.Invulnerable || IsSpawnedRecently); }
	}

	public override bool IsInCover
	{
		get { return BlackBoard.Cover != null; }
	}

	public override bool IsEnteringToCover
	{
		get { return GoalManager != null && GoalManager.CurrentGoal != null && GoalManager.CurrentGoal.GoalType == E_GOAPGoals.CoverEnter; }
	} // FIX ME !!! on proxy/server is not Goap !!!

	public bool IsSpawnedRecently
	{
		get { return BlackBoard.SpawnProtectionRestTime > 0; }
	}

	public bool IsBusy
	{
		get { return BlackBoard.BusyAction; }
	}

	public bool IsInKnockdown
	{
		get { return BlackBoard.InKnockDown; }
	}

	public bool IsLeavingToCover
	{
		get { return GoalManager.CurrentGoal != null && GoalManager.CurrentGoal.GoalType == E_GOAPGoals.CoverLeave; }
	} // FIX ME !!! on proxy/server is not Goap !!!

	public bool IsFullyHealed
	{
		get { return BlackBoard.Health.Equals(BlackBoard.RealMaxHealth); }
	}

	public float HealthModifier
	{
		get
		{
			if (GadgetsComponent.Perk.id == E_PerkID.ExtendedHealth || GadgetsComponent.Perk.id == E_PerkID.ExtendedHealthII)
				return GadgetsComponent.Perk.CipheredModifier; //GadgetsComponent.Perk.Modifier;

			return 1;
		}
	}

	public float RunSpeedModifier
	{
		get
		{
			float mod = 1;

			if (GadgetsComponent.Perk.id == E_PerkID.FasterMove || GadgetsComponent.Perk.id == E_PerkID.FasterMoveII)
				mod *= GadgetsComponent.Perk.CipheredModifier; //GadgetsComponent.Perk.Modifier;

			mod *= GadgetsComponent.GetActiveBoostModifier(E_ItemBoosterBehaviour.Speed);

			return mod;
		}
	}

	public bool CanSprint
	{
		get
		{
			return GadgetsComponent.Perk.id == E_PerkID.Sprint || GadgetsComponent.Perk.id == E_PerkID.SprintII ||
				   GadgetsComponent.Perk.id == E_PerkID.SprintIII;
		}
	}

	public float SprintSpeedModifier
	{
		get
		{
			if (CanSprint)
				return GadgetsComponent.Perk.CipheredModifier; //GadgetsComponent.Perk.Modifier;

			return 1;
		}
	}

	public override Vector3 ChestPosition
	{
		get { return Transform.position + transform.up*1.5f; }
	}

	// agent human public shits
	public WorldState WorldState { get; private set; }

	[SerializeField] [HideInInspector] AnimSet m_AnimSet;

	public AnimSet AnimSet
	{
		get { return m_AnimSet; }
	}

	[SerializeField] [HideInInspector] CharacterController m_CharacterController;

	public CharacterController CharacterController
	{
		get { return m_CharacterController; }
	}

// The m_CollisionController is not reference by the current code (only assigned) but it is serialized at the same time.
// I really do not want to break any serialization, thus I decided to suppress the warning in this case.
#pragma warning disable 414
	[SerializeField] [HideInInspector] CapsuleCollider m_CollisionController;

	public CapsuleCollider CollisionController
	{
		get { return CollisionController; }
	}
#pragma warning restore 414

	[SerializeField] [HideInInspector] ComponentWeapons m_WeaponComponent;

	public ComponentWeapons WeaponComponent
	{
		get { return m_WeaponComponent; }
	}

	[SerializeField] [HideInInspector] ComponentGadgets m_GadgetsComponent;

	public ComponentGadgets GadgetsComponent
	{
		get { return m_GadgetsComponent; }
	}

	[SerializeField] [HideInInspector] ComponentSensors m_SensorsComponent;

	public ComponentSensors SensorsComponent
	{
		get { return m_SensorsComponent; }
	}

	[SerializeField] [HideInInspector] AnimComponent m_AnimComponent;

	public AnimComponent AnimComponent
	{
		get { return m_AnimComponent; }
	}

	[SerializeField] [HideInInspector] ComponentPlayer m_PlayerComponent;

	public ComponentPlayer PlayerComponent
	{
		get { return m_PlayerComponent; }
	}

	public SkinnedMeshRenderer Renderer { get; private set; }
	public SkinnedMeshRenderer[] LodRenderers { get; private set; }
	//beny: we're using LODs, so changes meant for Renderer should be applied to all LODs => use LodRenderers to do it

	public Vector3 EyePosition
	{
		get { return TransformEye.position; }
	}

	public GOAPAction GetAction(E_GOAPAction type)
	{
		return (GOAPAction)Actions[type];
	}

	public int GetNumberOfActions()
	{
		return Actions.Count;
	}

	GOAPManager GoalManager;
	Hashtable Actions = new Hashtable();

	//private float TimeToUpdateAgent;
	float StepTime;
	public float LastHitTime { get; private set; }

	public float CharacterRadius
	{
		get { return CharacterController ? CharacterController.radius : 0; }
	}

	public AudioClip StepSound
	{
		get
		{
			if (SoundSetup.Steps.Length == 0)
				return null;
			return SoundSetup.Steps[Random.Range(0, SoundSetup.Steps.Length)];
		}
	}

	public AudioClip StepMetalSound
	{
		get
		{
			if (SoundSetup.StepsMetal.Length == 0)
				return null;
			return SoundSetup.StepsMetal[Random.Range(0, SoundSetup.StepsMetal.Length)];
		}
	}

	public AudioClip StepWaterSound
	{
		get
		{
			if (SoundSetup.StepsWater.Length == 0)
				return null;
			return SoundSetup.StepsWater[Random.Range(0, SoundSetup.StepsWater.Length)];
		}
	}

	public AudioClip CoverInSound
	{
		get
		{
			if (SoundSetup.CoverIn.Length == 0)
				return null;
			return SoundSetup.CoverIn[Random.Range(0, SoundSetup.CoverIn.Length)];
		}
	}

	public AudioClip CoverJumpSound
	{
		get
		{
			if (SoundSetup.CoverJump.Length == 0)
				return null;
			return SoundSetup.CoverJump[Random.Range(0, SoundSetup.CoverJump.Length)];
		}
	}

	public AudioClip InjuriesSound
	{
		get
		{
			if (SoundSetup.Injuries.Length == 0)
				return null;
			return SoundSetup.Injuries[Random.Range(0, SoundSetup.Injuries.Length)];
		}
	}

	public AudioClip DeathSound
	{
		get
		{
			if (SoundSetup.Deaths.Length == 0)
				return null;
			return SoundSetup.Deaths[Random.Range(0, SoundSetup.Deaths.Length)];
		}
	}

	public AudioClip TeleportSound
	{
		get
		{
			if (SoundSetup.Teleport.Length == 0)
				return null;
			return SoundSetup.Teleport[Random.Range(0, SoundSetup.Teleport.Length)];
		}
	}

	public AudioClip ThrowSound
	{
		get
		{
			if (SoundSetup.Throw.Length == 0)
				return null;
			return SoundSetup.Throw[Random.Range(0, SoundSetup.Throw.Length)];
		}
	}

	public AudioClip UseSound
	{
		get
		{
			if (SoundSetup.Use.Length == 0)
				return null;
			return SoundSetup.Use[Random.Range(0, SoundSetup.Use.Length)];
		}
	}

	public AudioClip HealSound
	{
		get
		{
			if (SoundSetup.Heal.Length == 0)
				return null;
			return SoundSetup.Heal[Random.Range(0, SoundSetup.Heal.Length)];
		}
	}

	public AudioClip MeleeSound
	{
		get
		{
			if (SoundSetup.Melee.Length == 0)
				return null;
			return SoundSetup.Melee[Random.Range(0, SoundSetup.Melee.Length)];
		}
	}

	public AudioClip KnockdownSound
	{
		get
		{
			if (SoundSetup.Knockdown.Length == 0)
				return null;
			return SoundSetup.Knockdown[Random.Range(0, SoundSetup.Knockdown.Length)];
		}
	}

	public AudioClip CmdAffirmativeSound
	{
		get
		{
			if (SoundSetup.CmdAffirmative.Length == 0)
				return null;
			return SoundSetup.CmdAffirmative[Random.Range(0, SoundSetup.CmdAffirmative.Length)];
		}
	}

	public AudioClip CmdNegativeSound
	{
		get
		{
			if (SoundSetup.CmdNegative.Length == 0)
				return null;
			return SoundSetup.CmdNegative[Random.Range(0, SoundSetup.CmdNegative.Length)];
		}
	}

	public AudioClip CmdAttackSound
	{
		get
		{
			if (SoundSetup.CmdAttack.Length == 0)
				return null;
			return SoundSetup.CmdAttack[Random.Range(0, SoundSetup.CmdAttack.Length)];
		}
	}

	public AudioClip CmdHelpSound
	{
		get
		{
			if (SoundSetup.CmdHelp.Length == 0)
				return null;
			return SoundSetup.CmdHelp[Random.Range(0, SoundSetup.CmdHelp.Length)];
		}
	}

	public AudioClip CmdCoverMeSound
	{
		get
		{
			if (SoundSetup.CmdCoverMe.Length == 0)
				return null;
			return SoundSetup.CmdCoverMe[Random.Range(0, SoundSetup.CmdCoverMe.Length)];
		}
	}

	public AudioClip CmdBackSound
	{
		get
		{
			if (SoundSetup.CmdBack.Length == 0)
				return null;
			return SoundSetup.CmdBack[Random.Range(0, SoundSetup.CmdBack.Length)];
		}
	}

	public AudioClip CmdOutOfAmmoSound
	{
		get
		{
			if (SoundSetup.CmdOutOfAmmo.Length == 0)
				return null;
			return SoundSetup.CmdOutOfAmmo[Random.Range(0, SoundSetup.CmdOutOfAmmo.Length)];
		}
	}

	public AudioClip CmdMedicSound
	{
		get
		{
			if (SoundSetup.CmdMedic.Length == 0)
				return null;
			return SoundSetup.CmdMedic[Random.Range(0, SoundSetup.CmdMedic.Length)];
		}
	}

	public bool IsOwner
	{
		get { return uLink.Network.isClient && NetworkView.isMine; }
	}

	public bool IsServer
	{
		get { return uLink.Network.isServer; }
	}

	public bool IsProxy
	{
		get { return uLink.Network.isClient && NetworkView.isMine == false; }
	}

	float RespawnedTime;

	static Dictionary<E_CommandID, Vector2> IconUV = new Dictionary<E_CommandID, Vector2>();

	static AgentHuman()
	{
		IconUV.Add(E_CommandID.Affirmative, new Vector2(0.53f, 0.55f));
		IconUV.Add(E_CommandID.Attack, new Vector2(0.588f, 0.388f));
		IconUV.Add(E_CommandID.Back, new Vector2(0.535f, 0.388f));
		IconUV.Add(E_CommandID.CoverMe, new Vector2(0.59f, 0.44f));
		IconUV.Add(E_CommandID.Help, new Vector2(0.588f, 0.497f));
		IconUV.Add(E_CommandID.Medic, new Vector2(0.534f, 0.442f));
		IconUV.Add(E_CommandID.Negative, new Vector2(0.58f, 0.55f));
		IconUV.Add(E_CommandID.OutOfAmmo, new Vector2(0.535f, 0.498f));
		//IconUV.Add(E_CommandID.OutOfAmmo, new Vector2(544/1024f, 494/1024f));
	}

	// this method is called from player cache to pre - initialize prefab
	public void PrefabPreAwake()
	{
		Animation anim = GetComponent<Animation>();
		Transform trans = transform;

		if (null != anim)
		{
			anim["AimU"].AddMixingTransform(trans.Find("pelvis/stomach"));
			anim["AimD"].AddMixingTransform(trans.Find("pelvis/stomach"));
			anim["IdleChangeWeapon"].AddMixingTransform(trans.Find("pelvis/stomach"));
		}

		m_CollisionController = trans.GetComponent<CapsuleCollider>();
		m_CharacterController = trans.GetComponent<CharacterController>();
		m_SensorsComponent = GetComponent<ComponentSensors>();
		m_WeaponComponent = GetComponent<ComponentWeapons>();
		m_GadgetsComponent = GetComponent<ComponentGadgets>();
		m_AnimComponent = GetComponent<AnimComponent>();
		m_PlayerComponent = GetComponent<ComponentPlayer>();
		m_AnimSet = GetComponent<AnimSet>();
	}

	//only once throught whole level
	void Awake()
	{
		base.Initialize();

		BlackBoard.Owner = this;
		BlackBoard.GameObject = GameObject;

		WorldState = new WorldState();
		GoalManager = new GOAPManager(this);

		ResetAgent();

		BlackBoard.DontUpdate = true;

		//TimeToUpdateAgent = 0;

		GameObject.layer = 31;

		//setup colliders
		Transform t = Transform.FindChildByName("_Sphere");
		if (t)
			CollSphere = t.gameObject;

		// enumerate "standard" colliders
		//Collider []       enumerated = GameObject.GetComponentsInChildren< Collider >();
		List<Collider> filtered = new List<Collider>();

		/*foreach ( Collider c in enumerated )
		{
			if ( c.gameObject.layer == ObjectLayer.Enemy )
				filtered.Add( c );
		}*/

		Colliders = filtered.ToArray();

		//
		if (RagdollRoot)
		{
			RigidBodies = RagdollRoot.GetComponentsInChildren<Rigidbody>();

			EnumerateColliders();
		}

		if (uLink.Network.isClient && DeviceInfo.PerformanceGrade != DeviceInfo.Performance.Low)
		{
			GetComponent<CharacterShadow>().enabled = true;
		}

//		BlackBoard.BaseSetup.MaxWalkSpeed = 20;
//		BlackBoard.BaseSetup.MaxRunSpeed = 20;
	}

	void OnDestroy()
	{
		SoundSetup = null;
		BlackBoard.Reset();
		BlackBoard = null;
	}

	void Start()
	{
//        Debug.Log(GameObject.name + "A start");
		BlackBoard.ActionHandler += HandleAction;
		Renderer = GameObject.GetComponentInChildren(typeof (SkinnedMeshRenderer)) as SkinnedMeshRenderer;
		LodRenderers = GameObject.GetComponentsInChildren<SkinnedMeshRenderer>();

		if (uLink.Network.isClient)
		{
			TeleportFadeIn(Client.Instance.GameState.SpawnTimeProtection);
		}

		//
		SetLightProbeAnchor();

		if (CommandIcon != null)
		{
			CommandIcon.SetActive(false);
			CommandIconMaterial = CommandIcon.GetComponent<Renderer>().material;
		}
	}

	public void ActivateProxy()
	{
		//disable Ragdoll
		EnableRagdoll(false);

		if (IsProxy == false)
			return;

		Player.Register(NetworkView.owner, PlayerComponent);

		networkView.RPC("SendSynchroState", uLink.RPCMode.Server);
	}

	[uSuite.RPC]
	void SendSynchroState(uLink.NetworkMessageInfo info)
	{
		if (IsServer == false)
			return;

		int coverIndex = -1;
		if (IsInCover)
		{
			coverIndex = Mission.Instance.GameZone.GetCoverIndex(BlackBoard.Cover);
		}

		networkView.RPC("RcvSynchroState",
						info.sender,
						coverIndex,
						BlackBoard.CoverPose,
						BlackBoard.CoverFire,
						IsAlive,
						WeaponComponent.CurrentWeapon);
	}

	[uSuite.RPC]
	void RcvSynchroState(int coverIndex, E_CoverPose coverPose, bool coverFiring, bool alive, E_WeaponID currentWeapon)
	{
		/*if(coverIndex != -1)
		{
			Cover cover = Mission.Instance.GameZone.GetCover(coverIndex);

	        if (cover == null)
	        {
	            Debug.LogWarning("Received CoverEnter RPC but no cover was found at the specified position. This could indicate a position sync problem.");
	        }
			else 
			{
				CoverStart( cover, E_CoverDirection.Middle );
				
				BlackBoard.CoverPose = coverPose;
			}
		}*/

		BlackBoard.ProxyDataForSpawn.IsValid = true;
		BlackBoard.ProxyDataForSpawn.CoverFiring = coverFiring;
		BlackBoard.ProxyDataForSpawn.CoverPose = coverPose;
		BlackBoard.ProxyDataForSpawn.Cover = Mission.Instance.GameZone.GetCover(coverIndex);
		BlackBoard.ProxyDataForSpawn.Death = alive == false;
		BlackBoard.ProxyDataForSpawn.CurrentWeapon = currentWeapon; // weaponcomponent is taking care about it
		BlackBoard.ProxyDataForSpawn.CoverPosition = E_CoverDirection.Middle;

		//if(Game.Instance.GameLog) Debug.Log("Proxy synchro received :" + currentWeapon + " cover: "  +  coverIndex + " " + coverPose + " " + (coverFiring ? " fire " :" idle "));

		GameObject.SetActive(true);
		GameObject.SendMessage("Activate", SendMessageOptions.RequireReceiver);

		if (BlackBoard.ProxyDataForSpawn.Death)
			SpawnProxyDie();

		if (BlackBoard.ProxyDataForSpawn.Cover)
		{
			BlackBoard.CoverPose = coverPose;
			CoverStart(BlackBoard.ProxyDataForSpawn.Cover, E_CoverDirection.Middle);
		}
	}

	public void SpawnProxyDie()
	{
		BlackBoard.Health = 0;
		IsAliveInternal = false;

		StartCoroutine(Fadeout());

		BlackBoard.Desires.WeaponTriggerOn = false;
		BlackBoard.Desires.WeaponTriggerUp = false;
		BlackBoard.Desires.MeleeTriggerOn = false;
		BlackBoard.Desires.WeaponTriggerUpDisabled = true;

		AgentActionDeath a = AgentActionFactory.Create(AgentActionFactory.E_Type.Death) as AgentActionDeath;
		//AX :: a.Attacker = inAttacker;
		a.Attacker = null;
		a.Impulse = Vector3.zero;
		a.Pos = Position;
		a.Damage = 0;

		BlackBoard.ActionAdd(a);
	}

	void Activate()
	{
		if (IsProxy == false)
			Player.Register(NetworkView.owner, PlayerComponent);

		IsAliveInternal = true;

		BlackBoard.Desires.Rotation = Transform.rotation;
		BlackBoard.Desires.FireDirection = Transform.forward;

		StepTime = 0;
		GameObject.SetActive(true);

		GameObject.layer = 31;

		LastHitTime = 0;

		AddHat();

		//disable Ragdoll
		EnableRagdoll(false);

		BlackBoard.DontUpdate = false;

		RespawnedTime = Time.timeSinceLevelLoad;

		if (CommandIcon != null)
		{
			CommandIcon.SetActive(false);
			CommandIconMaterial = CommandIcon.GetComponent<Renderer>().material;
		}
	}

	void Deactivate()
	{
		EnableRagdoll(false);

		//Debug.Log("Human deactivate");
//        RemoveHat();
		DestroyHat();
		Player.UnRegister(NetworkView.owner, PlayerComponent);

		Reset();
	}

	void LateUpdate()
	{
		if (IsAlive == false)
			return;

		//anim pose outdated
		m_DominantAnimSampled = false;

		//handle model visibility when it intersects player's (owner's) camera
		if (IsProxy && GameCamera.Instance != null)
		{
			Vector3 dir = GameCamera.Instance.CameraPosition - Position;
			float r = CharacterRadius;

			dir.y = 0; //2D test is enough
			r = r*r;

			if (dir.sqrMagnitude < r)
			{
				if (IsShown())
					ShowModel(false);
			}
			else
			{
				if (!IsShown())
					ShowModel(true);
			}
		}

		//footsteps
		if (null != GameCamera.Instance && uLink.Network.isClient && 15*15 > Vector3.SqrMagnitude(Position - GameCamera.Instance.CameraPosition))
		{
			if ((BlackBoard.MotionType == E_MotionType.Run || BlackBoard.MotionType == E_MotionType.Walk ||
				 BlackBoard.MotionType == E_MotionType.Sprint || BlackBoard.MotionType == E_MotionType.AnimationDrive) &&
				StepTime < Time.timeSinceLevelLoad)
			{
				RaycastHit hit;
				LayerMask mask = (ObjectLayerMask.Default | ObjectLayerMask.PhysicsDefault | ObjectLayerMask.PhysicsMetal | ObjectLayerMask.PhysicsWater);
				if (Physics.Raycast(Position + Vector3.up*0.5f, -Vector3.up, out hit, 1.0f, mask) == true)
				{
					AudioClip clip;
					switch (hit.transform.gameObject.layer)
					{
					case 27: //ObjectLayer.PhysicsWater
						clip = StepWaterSound;
						break;
					case 28: //ObjectLayer.PhysicsDefault
						clip = StepSound;
						break;
					case 29: //ObjectLayer.PhysicsMetal
						clip = StepMetalSound;
						break;
					default:
						clip = StepSound;
						break;
					}
					if (BlackBoard.MotionType == E_MotionType.Sprint)
					{
						SoundPlay(clip);
						StepTime = Time.timeSinceLevelLoad + Random.Range(BlackBoard.StepsSetup.RunMinDelay*0.8f, BlackBoard.StepsSetup.RunMaxDelay*0.8f);
					}
					else if (BlackBoard.MotionType == E_MotionType.Run)
					{
						SoundPlay(clip);
						StepTime = Time.timeSinceLevelLoad + Random.Range(BlackBoard.StepsSetup.RunMinDelay, BlackBoard.StepsSetup.RunMaxDelay);
					}
					else if (BlackBoard.MotionType == E_MotionType.Walk)
					{
						SoundPlay(clip);
						StepTime = Time.timeSinceLevelLoad + Random.Range(BlackBoard.StepsSetup.WalkMinDelay, BlackBoard.StepsSetup.WalkMaxDelay);
					}
					else if (BlackBoard.MotionType == E_MotionType.AnimationDrive)
					{
						SoundPlay(clip);
						StepTime = Time.timeSinceLevelLoad + Random.Range(BlackBoard.StepsSetup.AnimMinDelay, BlackBoard.StepsSetup.AnimMaxDelay);
					}
				}
			}
		}

		BlackBoard.IdleTimer += Time.deltaTime;

		if (BlackBoard.Cover != null)
			BlackBoard.CoverTime += Time.deltaTime;
		else
			BlackBoard.CoverTime = 0;

		UpdateAgent();
	}

	void UpdateAgent()
	{
		if (BlackBoard.DontUpdate == true)
			return;

		//update blackboard
		BlackBoard.Update();

		if (PlayerComponent == Player.LocalInstance)
		{
			if (BlackBoard.BusyAction == false)
			{
				GoalManager.UpdateCurrentGoal();

				//Manage the list of goals we have
				GoalManager.ManageGoals();
			}
		}

		BlackBoard.PostUpdate();

		WorldState.SetWSProperty(E_PropKey.Idling, GoalManager.CurrentGoal == null);
	}

	public void HandleAction(AgentAction a)
	{
		if (uLink.Network.isClient)
		{
			if (a is AgentActionCoverEnter)
			{
				SoundPlay(CoverInSound);
			}
			else if (a is AgentActionCoverLeave && (a as AgentActionCoverLeave).TypeOfLeave == AgentActionCoverLeave.E_Type.Jump)
			{
				SoundPlay(CoverJumpSound);
			}
			else if (a is AgentActionInjury)
			{
				if (Random.Range(0, 100) < 30)
					SoundPlay(InjuriesSound);

				AgentActionInjury action = a as AgentActionInjury;
				Vector3 dir = (action.Impulse*-1).normalized;

				CombatEffectsManager.Instance.PlayBloodEffect(Renderer, action.Pos, dir);

				if (IsOwner)
				{
					if (null != BloodFXManager.Instance)
					{
						BloodFXManager.Instance.SpawnBloodDrops((uint)Mathf.Min(Random.Range(1, 2 + action.Damage/15), 4));
					}
				}
			}
			else if (a is AgentActionTeamCommand)
			{
				TeamCommand(a as AgentActionTeamCommand);
			}
			else if (a is AgentActionDeath)
			{
				SoundPlay(DeathSound);

				AgentActionDeath action = a as AgentActionDeath;

				CombatEffectsManager.Instance.PlayBloodEffect(Renderer, action.Pos, (action.Impulse*-1).normalized);

				if (IsOwner)
				{
					if (null != BloodFXManager.Instance)
					{
						BloodFXManager.Instance.SpawnBloodDrops((uint)Mathf.Min(Random.Range(4, 4 + action.Damage/15), 8));
					}
				}
			}
		}
	}

	public void PrepareForStart()
	{
		//BlackBoard.Reset();

//        Debug.Log(GameObject.name + " Preparing for start !!");
	}

	public void Reset()
	{
		ResetAgent();

		EnableCollisions();
	}

	public void Stop(bool stop)
	{
		BlackBoard.Stop = stop;
	}

	// could be called after death.. when agent should disappear
	void ResetAgent()
	{
		CoverStop();

		StopAllCoroutines();
		GoalManager.Reset();
		BlackBoard.Reset();
		WorldState.Reset();

		WorldState.SetWSProperty(E_PropKey.Idling, true);
		WorldState.SetWSProperty(E_PropKey.AtTargetPos, true);
		WorldState.SetWSProperty(E_PropKey.TargetNode, Position);
		WorldState.SetWSProperty(E_PropKey.KillTarget, false);
		WorldState.SetWSProperty(E_PropKey.UseWorldObject, false);
		WorldState.SetWSProperty(E_PropKey.PlayAnim, false);
		WorldState.SetWSProperty(E_PropKey.InDodge, false);
//		WorldState.SetWSProperty(E_PropKey.WeaponChange, false);
		WorldState.SetWSProperty(E_PropKey.WeaponLoaded, false);
		WorldState.SetWSProperty(E_PropKey.CoverState, E_CoverState.None);

		ResetMaterial();

		BlackBoard.DontUpdate = true;
		//BlackBoard.GameObject.SetActiveRecursively(false);
	}

	public void AddGOAPAction(E_GOAPAction action)
	{
		Actions.Add(action, GOAPActionFactory.Create(action, this));
	}

	public void AddGOAPGoal(E_GOAPGoals goal)
	{
		GoalManager.AddGoal(goal);
	}

	public GOAPGoal GetGOAPGoal(E_GOAPGoals goal)
	{
		if (GoalManager == null)
			return null;

		return GoalManager.GetGoal(goal);
	}

	public void InitializeGOAP()
	{
		GoalManager.Initialize();
	}

	public override bool IsFriend(AgentHuman friend)
	{
		if (friend == null)
			return false;

		if (Game.GetMultiplayerGameType() == E_MPGameType.DeathMatch)
		{
			return false;
		}

		return Team == friend.Team;
	}

	//convert the hitZone (bone) name to the body part id
	public E_BodyPart GetBodyPart(HitZone zone)
	{
		if (zone)
		{
			switch (zone.name)
			{
			case "head":
				return E_BodyPart.Head;
			case "stomach":
				return E_BodyPart.Body;

			case "Lforearm":
			case "Larm":
				return E_BodyPart.LeftArm;

			case "Rforearm":
			case "Rarm":
				return E_BodyPart.RightArm;

			case "Lthigh":
			case "Lcalf":
				return E_BodyPart.LeftLeg;

			case "Rthigh":
			case "Rcalf":
				return E_BodyPart.RightLeg;

			case "Hat_Target":
				return E_BodyPart.Hat;
			}
		}

		return E_BodyPart.Body;
	}

	// capa
	public void SetDominantAnimName(string AnimName)
	{
		if (string.Equals(m_DominantAnimName, AnimName) == false)
		{
			m_DominantAnimName = AnimName;
			m_DominantAnimTimer = 0.0f;
			m_DominantAnimSampled = false;
		}
	}

	// capa
	public void SampleDominantAnim()
	{
		if (!m_DominantAnimSampled)
		{
			m_DominantAnimSampled = true;

			Animation.Stop();

			AnimationState animState = Animation[m_DominantAnimName];
			float animPlaybackTime = (animState.speed*m_DominantAnimTimer)/animState.length;

			if (animState.wrapMode == WrapMode.Loop)
			{
				animPlaybackTime -= Mathf.Floor(animPlaybackTime);
			}
			else if (animPlaybackTime > 1.0f)
			{
				animPlaybackTime = 1.0f;
			}

			animState.weight = 1.0f;
			animState.enabled = true;
			animState.time = animState.length*animPlaybackTime;

			Animation.Sample();

			animState.enabled = false;
		}
	}

	// RECIEVE FUNCTIONS

	// capa
	public void OnProjectileHit(Projectile projectile)
	{
		// is it necessary ?
		if ((IsAlive == false) || (IsFriend(projectile.Agent) == true))
			return;

		// update pose / ragdoll by sampling "dominant" animation (only once per frame)
		SampleDominantAnim();

		// hit-detection with ragdoll
		RaycastHit hit;
		float closest = float.MaxValue;
		HitZone closestHZ = null;
		int idx = Colliders != null ? Colliders.Length : 0;
		Ray ray = new Ray(projectile.Pos - projectile.Dir, projectile.Dir);

		while (idx-- > 0)
		{
			if (Colliders[idx].Raycast(ray, out hit, 3.3f))
			{
				if (hit.distance < closest)
				{
					closestHZ = hit.transform.GetComponent<HitZone>();
				}
			}
		}

		// also check hit into a hat
		if ((Hat != null) && (Hat.transform.parent != null))
		{
			bool hh = (closestHZ != null) && (string.Compare(closestHZ.name, "head") == 0);

			if (!hh)
			{
				hh = (Hat.GetComponent<Collider>().Raycast(ray, out hit, 3.3f) == true) && (hit.distance < closest);
			}

			if (hh) // hit into head or/with hat
			{
				Hat.GetComponent<HatObject>().OnProjectileHit(projectile);
				return;
			}
		}

		// report closest hit
		if (closestHZ != null)
		{
			OnProjectileHit(projectile, closestHZ);
		}

		// ignore hits into "proxy" collider
		else
		{
			projectile.ignoreThisHit = true;
		}
	}

	// beny: IHitZoneOwner.OnProjectileHit()
	public void OnProjectileHit(Projectile projectile, HitZone zone)
	{
		if (!IsAlive)
			return;

		if (IsFriend(projectile.Agent))
		{
			projectile.ignoreThisHit = true;
			return;
		}

		projectile.spawnHitEffects = false;

		if (uLink.Network.isServer == false)
			return;

		float damage = projectile.Damage;

		if (zone is HitZoneEffects)
			damage *= (zone as HitZoneEffects).DamageModifier;

		if (BlackBoard.Invulnerable)
		{
			BlackBoard.AbsorbedDamage += damage;
			projectile.ricochetThisHit = true;
			return;
		}

		E_BodyPart bodyPart = GetBodyPart(zone);

//		//flak is intended only for explosions
//		if ( GadgetsComponent.IsPerkAvailableForUse(E_PerkID.FlakJacket) || GadgetsComponent.IsPerkAvailableForUse(E_PerkID.FlakJacketII) )
//			damage *= GadgetsComponent.Perk.CipheredModifier; //GadgetsComponent.Perk.Modifier;

		// Main function with damage processing...
		TakeDamage(projectile.Agent,
				   damage,
				   projectile.Transform.position,
				   projectile.Transform.forward*projectile.Impulse,
				   projectile.WeaponID,
				   projectile.ItemID,
				   bodyPart);
	}

	public void OnExplosionHit(Explosion explosion)
	{
		OnExplosionHit(explosion.Agent, explosion.Damage, explosion.Impulse, explosion.m_WeaponID, explosion.m_ItemID, null);
	}

	public void OnExplosionHit(Agent attacker, float damage, Vector3 impulse, E_WeaponID weaponId, E_ItemID itemId, HitZone zone)
	{
		if (!IsAlive)
			return;

		// CAPA: explosion ingores damage-multiplier
		//	if (zone is HitZoneEffects)
		//		damage *= (zone as HitZoneEffects).DamageModifier;

		//   print("OnExplosionHit : " + attacker.name + " " + damage);
		if (attacker != this && IsFriend(attacker as AgentHuman))
		{
			//print("OnExplosionHit : " + attacker.name + " ignore friend damage");
			return;
		}

		if (uLink.Network.isServer == false)
			return;

		if (BlackBoard.Invulnerable)
		{
			BlackBoard.AbsorbedDamage += damage;
			return;
		}

//		Item item = GadgetsComponent.GetGadgetAvailableWithBehaviour(E_ItemBehaviour.BlastShield);
//		if ( item != null )
		if (GadgetsComponent.IsPerkAvailableForUse(E_PerkID.FlakJacket) || GadgetsComponent.IsPerkAvailableForUse(E_PerkID.FlakJacketII))
		{
//			damage *= item.Settings.DamageMod; 
			damage *= GadgetsComponent.Perk.CipheredModifier; //GadgetsComponent.Perk.Modifier;
		}

		E_BodyPart bodyPart = E_BodyPart.Body; //= GetBodyPart(zone);	- Mara toto u explozi nechce. ;-)

		TakeDamage(attacker as AgentHuman, damage, ChestPosition, impulse, weaponId, itemId, bodyPart);
	}

	void TakeDamage(AgentHuman inAttacker,
					float inDamage,
					Vector3 pos,
					Vector3 inImpuls,
					E_WeaponID weapon,
					E_ItemID item,
					E_BodyPart bodyPart,
					bool ignoreSpawnedRecentlyFlag = false)
	{
		// Only server players should take damage or die as a consequence of damage. Client players die from server messages.

		if (uLink.Network.isServer)
		{
			if (false == ignoreSpawnedRecentlyFlag)
			{
				if (IsSpawnedRecently)
				{
					return;
				}
			}

			inDamage *= GadgetsComponent.GetActiveBoostModifier(E_ItemBoosterBehaviour.Armor);

			if (null != inAttacker)
			{
				if (inAttacker != this)
					UpdateDamageData(inAttacker.NetworkView.owner, inDamage);
			}
			else
			{
				Debug.LogWarning("AgentHuman.TakeDamage() : null inAttacker. Victim : " + this);
			}

			if (inAttacker && weapon != E_WeaponID.None && inAttacker != this)
				inAttacker.StatisticAddWeaponHit(weapon);

			if (BlackBoard.Health - inDamage > 0)
			{
				Injure(inAttacker, inDamage, pos, inImpuls, bodyPart);
			}
			else if (BlackBoard.Health > 0)
			{
				Die(inAttacker, pos, inImpuls, inDamage, bodyPart);

				StatisticAddDeath();

				if (null != inAttacker)
				{
					if (inAttacker != this)
					{
						if (weapon != E_WeaponID.None)
							inAttacker.StatisticAddWeaponKill(weapon, bodyPart);
						else if (item != E_ItemID.None)
							inAttacker.StatisticAddItemKill(item);
					}
					else
					{
						StatisticAddSuicide();
					}
				}
			}

			BlackBoard.LastInjuryTime = Time.timeSinceLevelLoad;

			if (inAttacker)
				inAttacker.NetworkView.UnreliableRPC("TargetHit", uLink.RPCMode.Owner);
		}
	}

	void UpdateDamageData(uLink.NetworkPlayer inAttacker, float damage)
	{
		BlackBoard.AttackersDamageData.Add(new BlackBoard.DamageData() {Attacker = inAttacker, Damage = damage});
	}

	[uSuite.RPC]
	void TargetHit()
	{
		LastHitTime = Time.timeSinceLevelLoad;
	}

	public override void KnockDown(AgentHuman humanAttacker, E_MeleeType meleeType, Vector3 direction)
	{
		if (GoalManager.CurrentGoal != null)
			GoalManager.CurrentGoal.Deactivate();

		AgentActionKnockdown a = AgentActionFactory.Create(AgentActionFactory.E_Type.Knockdown) as AgentActionKnockdown;
		a.Attacker = humanAttacker;
		a.MeleeType = meleeType;
		a.Direction = direction;

		BlackBoard.ActionAdd(a);

		//apply some damage to a knocked-down player
		if (uLink.Network.isServer)
		{
			TakeDamage(humanAttacker, 20, ChestPosition, Forward, E_WeaponID.None, E_ItemID.None, E_BodyPart.Body);
		}
	}

	public void Injure(AgentHuman humanAttacker, float inDamage, Vector3 pos, Vector3 impuls, E_BodyPart bodyPart)
	{
		if (IsServer)
		{
			if (IsSpawnedRecently)
			{
				return;
			}
		}
		// Don't modify health properties if agent is Invulnerable...
		if (IsInvulnerable == false)
		{
			BlackBoard.Health = Mathf.Max(0, BlackBoard.Health - inDamage);
		}

		//Debug.Log("Health is now " + BlackBoard.Health);

		if (BlackBoard.ReactOnHits)
		{
			AgentActionInjury a = AgentActionFactory.Create(AgentActionFactory.E_Type.Injury) as AgentActionInjury;

			//AX :: a.Attacker = inAttacker;
			a.Attacker = humanAttacker;
			a.Impulse = impuls;
			a.Pos = pos;
			a.Damage = Mathf.CeilToInt(inDamage);
			a.BodyPart = bodyPart;
			BlackBoard.ActionAdd(a);

//			Debug.Log ("Injure(), name=" + name + ", damage=" + inDamage + ", bodyPart=" + bodyPart);
		}
	}

	public void Die(AgentHuman humanAttacker, Vector3 pos, Vector3 inImpuls, float inDamage, E_BodyPart bodyPart)
	{
		TeamCommandStop();

		CoverStop();

		BlackBoard.Health = 0;
		IsAliveInternal = false;

		StartCoroutine(Fadeout());

		BlackBoard.Desires.WeaponTriggerOn = false;
		BlackBoard.Desires.WeaponTriggerUp = false;
		BlackBoard.Desires.MeleeTriggerOn = false;
		BlackBoard.Desires.WeaponTriggerUpDisabled = true;

		AgentActionDeath a = AgentActionFactory.Create(AgentActionFactory.E_Type.Death) as AgentActionDeath;
		//AX :: a.Attacker = inAttacker;
		a.Attacker = humanAttacker;
		a.Impulse = inImpuls;
		a.Pos = pos;
		a.Damage = Mathf.CeilToInt(inDamage);
		a.BodyPart = bodyPart;

		BlackBoard.ActionAdd(a);

//		Debug.Log ("Die(), name=" + name + ", damage=" + inDamage + ", bodyPart=" + bodyPart);

		if (uLink.Network.isClient)
		{
			SoundPlay(DeathSound);
		}

		if ((humanAttacker != null) && (humanAttacker != this)) // valid attacker and it's not suicide
		{
			//	PlayerPersistantInfo  ppiPlayer = PPIManager.Instance.GetLocalPlayerPPI();
			PlayerPersistantInfo ppiKiller = PPIManager.Instance.GetPPI(humanAttacker.NetworkView.owner);
			PlayerPersistantInfo ppiVictim = PPIManager.Instance.GetPPI(this.NetworkView.owner);

			//	if ((ppiPlayer.Player == ppiKiller.Player) || (ppiPlayer.Player == ppiVictim.Player)) // local player is either killer or victim
			{
				PPILocalStats.RecordKill(ppiKiller, ppiVictim);
			}
		}

		if (CommandIcon != null)
		{
			CommandIcon.SetActive(false);
		}
	}

	// ------
	public void TeamCommand(AgentActionTeamCommand action)
	{
		if (IsAlive == false)
			return;

		switch (action.Command)
		{
		case E_CommandID.Affirmative:
			Audio.clip = CmdAffirmativeSound;
			break;
		case E_CommandID.Negative:
			Audio.clip = CmdNegativeSound;
			break;
		case E_CommandID.Attack:
			Audio.clip = CmdAttackSound;
			break;
		case E_CommandID.Help:
			Audio.clip = CmdHelpSound;
			break;
		case E_CommandID.CoverMe:
			Audio.clip = CmdCoverMeSound;
			break;
		case E_CommandID.Back:
			Audio.clip = CmdBackSound;
			break;
		case E_CommandID.OutOfAmmo:
			Audio.clip = CmdOutOfAmmoSound;
			break;
		case E_CommandID.Medic:
			Audio.clip = CmdMedicSound;
			break;
		}

		CommandIconMaterial.SetTextureOffset("_MainTex", IconUV[action.Command]);
		CommandIcon.SetActive(true);
		Audio.Play();
		GuiHUD.Instance.StartTeamCommand(this, action.Command);

		CancelInvoke("TeamCommandStop");
		Invoke("TeamCommandStop", 5.0f);
	}

	// ------
	void TeamCommandStop()
	{
		if (CommandIcon)
			CommandIcon.SetActive(false);

		if (GuiHUD.Instance)
			GuiHUD.Instance.StopTeamCommand(this);
		Audio.Stop();
	}

	// ------
	public void PlayAnim(string animName)
	{
		if (animName != null)
		{
			BlackBoard.Desires.Animation = animName;
			WorldState.SetWSProperty(E_PropKey.PlayAnim, true);
		}
	}

	// begins cover - inform cover, modify blackboard and spawn CoverEnter action
	public AgentActionCoverEnter CoverStart(Cover cover, E_CoverDirection direction)
	{
		cover.OccupyPosition(direction, this);

		BlackBoard.Cover = cover;

		AgentActionCoverEnter action = AgentActionFactory.Create(AgentActionFactory.E_Type.CoverEnter) as AgentActionCoverEnter;
		action.Cover = cover;

		BlackBoard.ActionAdd(action);

		return action;
	}

	// @return false if agent isn't in cover situation
	public bool CoverStop()
	{
		if (IsInCover)
		{
			BlackBoard.Cover.FreePosition(this);

			WorldState.SetWSProperty(E_PropKey.CoverState, E_CoverState.None);

			BlackBoard.CoverPosition = E_CoverDirection.Unknown;

			BlackBoard.Cover = null;

			return true;
		}

		return false;
	}

	// return maximal allowed distance
	float TestAgentCollision(AgentHuman Agent, float MyPosition01, float MoveDistance01, bool RightMove)
	{
		if (this == Agent || null == Agent || null == BlackBoard.Cover)
		{
			return MoveDistance01;
		}

		Cover cover = BlackBoard.Cover;

		float DistanceToAgent = cover.GetPositionEdgeRelative(Agent.Position) - MyPosition01;

		// moving away from this agent => movement is allowed => continue with next agent, if any
		if ((DistanceToAgent < 0.0f) == RightMove)
		{
			return MoveDistance01;
		}

		float MinDistAllowed01 = cover.GetDistanceEdgeRelative(Agent.CharacterRadius + CharacterRadius + BlackBoard.CoverSetup.MultiCoverSafeDist);

		float MaxAllowedDistance01 = Mathf.Abs(DistanceToAgent) - MinDistAllowed01;

		// in case current distance is not enough to proceed with movement, we are in collision in other agent
		if (MaxAllowedDistance01 < MoveDistance01)
		{
			return MaxAllowedDistance01;
		}

		return MoveDistance01;
	}

	// proceed movement in cover situation with respect to other agents (if any) hiding at the same cover
	// @returns false if no more move is possible (end of move action)
	public bool RestrictedCoverMove(AgentActionCoverMove.E_Direction Direction, float MoveDistance, bool Test = false)
	{
		// from our point of view, this 'movement' is possible
		if (MoveDistance <= 0.0f)
		{
			return !Test;
		}

		Cover cover = BlackBoard.Cover;

		//DebugUtils.Assert( cover.AllAgents.Contains( this ) ); // We are moving in context of cover

		if (IsOwner)
		{
			if (!cover.AllAgents.Contains(this))
			{
				Debug.LogWarning("Cover move without agent in cover's list ( Agent " + this + ", cover " + cover + " )");
			}
		}

		bool RightMove = (Direction == AgentActionCoverMove.E_Direction.Right);

		float MoveDistance01 = cover.GetDistanceEdgeRelative(MoveDistance);

		// Is there any other agent hiding at this edge?
		if (cover.AllAgents.Count > 1)
		{
			float MyPosition01 = cover.GetPositionEdgeRelative(Position);

			foreach (AgentHuman Agent in cover.AllAgents)
			{
				MoveDistance01 = TestAgentCollision(Agent, MyPosition01, MoveDistance01, RightMove);

				if (MoveDistance01 <= 0.0f)
				{
					return false;
				}
			}
		}

		MoveDistance = cover.GetDistanceEdgeReal(MoveDistance01);

		if (MoveDistance >= Mathf.Epsilon)
		{
			// for further math operations - we will move from right to the left edge
			if (!RightMove)
			{
				MoveDistance *= -1;
			}

			//Transform.position += cover.Right * MoveDistance;
			if (IsOwner)
			{
				//Transform.position += cover.Right * MoveDistance;
				//CharacterController.Move( cover.Right * MoveDistance );
				//CharacterController.SimpleMove( cover.Right * MoveDistance/Time.deltaTime );

				// test just ragdols (!)
				LayerMask mask = ObjectLayerMask.Ragdoll;

				bool TestOK;

				if (MoveDistance > 0)
				{
					TestOK = SweepTest(cover.Right, MoveDistance + BlackBoard.CoverSetup.MultiCoverSafeDist, mask);
				}
				else
				{
					TestOK = SweepTest(-cover.Right, -MoveDistance + BlackBoard.CoverSetup.MultiCoverSafeDist, mask);
				}

				if (TestOK)
				{
					if (false == Test)
					{
						Transform.position += cover.Right*MoveDistance;
					}
					return true;
				}
			}
			else if (IsServer)
			{
				return true;
			}

			//return true;
		}

		return false;
	}

	// @return TRUE if no collision found
	public bool SweepTest(Vector3 Direction, float Distance, LayerMask Mask, bool useSlopeLimit = false)
	{
		RaycastHit[] hits;

		float heightShrinkBy = 0.15f;

		Vector3 p1Low = transform.position + CharacterController.center +
						Vector3.up*(CharacterController.radius + heightShrinkBy - CharacterController.height*0.5F);

		Vector3 p2 = p1Low + Vector3.up*(CharacterController.height - 2*(CharacterController.radius + heightShrinkBy));

		bool InCollision = false;

		hits = Physics.CapsuleCastAll(p1Low, p2, CharacterController.radius, Direction, Distance, Mask);

		float angleLimit = 90 - CharacterController.slopeLimit;

		foreach (RaycastHit hit in hits)
		{
			if (!hit.collider.transform.IsChildOf(transform))
			{
				AgentHuman Human = hit.collider.gameObject.GetComponent<AgentHuman>();

				if (Human != null && !Human.IsAlive)
				{
					continue;
				}

				if (useSlopeLimit)
				{
					if (Vector3.Angle(hit.normal, Vector3.up) < angleLimit)
					{
						continue;
					}
				}

				InCollision = true;

				break;
			}
		}

		return !InCollision;
	}

	public void Teleport(Vector3 position, Quaternion rotation)
	{
		//Debug.Log(Transform.position + " " +position);
		Transform.position = position;
		Transform.rotation = rotation;

		CoverStop();

		BlackBoard.Desires.Reset();
		BlackBoard.Desires.Rotation = rotation;
		BlackBoard.Desires.FireDirection = rotation*Vector3.forward;
	}

	public void Teleport(Transform destination)
	{
		GoalManager.Reset();

		BlackBoard.Desires.Rotation = destination.rotation;
		BlackBoard.Desires.FireDirection = destination.rotation*Vector3.forward;
		BlackBoard.Desires.WeaponTriggerOn = false;
		BlackBoard.Desires.WeaponTriggerUp = false;
		BlackBoard.Desires.MeleeTriggerOn = false;
		BlackBoard.Desires.WeaponTriggerUpDisabled = true;
		BlackBoard.MoveDir = Vector3.zero;
		BlackBoard.FireDir = Transform.forward;

		BlackBoard.BusyAction = false;
		BlackBoard.InteractionObject = null;
		BlackBoard.Invulnerable = false;
		BlackBoard.ReactOnHits = true;

		CoverStop();

		AnimComponent.OnTeleport();

		Transform.position = destination.position;
		Transform.rotation = destination.rotation;
	}

	public void SetInvisibleOn(float amount)
	{
		CombatEffectsManager.Instance.InvisibleEffectMaterial.SetFloat("_EffectAmount", amount);

		if (LodRenderers != null)
			foreach (Renderer r in LodRenderers)
			{
				r.material = CombatEffectsManager.Instance.InvisibleEffectMaterial;
				r.material.SetTexture("_MainTex", DiffuseMaterial.GetTexture("_MainTex"));
				r.material.SetTexture("_BumpMap", DiffuseMaterial.GetTexture("_BumpMap"));
				r.material.SetTexture("_BRDFTex", DiffuseMaterial.GetTexture("_BRDFTex"));
			}

		if (Renderer != null)
		{
			Renderer.material = CombatEffectsManager.Instance.InvisibleEffectMaterial;
			Renderer.material.SetTexture("_MainTex", DiffuseMaterial.GetTexture("_MainTex"));
			Renderer.material.SetTexture("_BumpMap", DiffuseMaterial.GetTexture("_BumpMap"));
			Renderer.material.SetTexture("_BRDFTex", DiffuseMaterial.GetTexture("_BRDFTex"));
		}

		//weapon
		WeaponBase weapon = WeaponComponent.GetCurrentWeapon();
		if (weapon)
			weapon.SetInvisibleMaterial(amount);

		//hat
		if (Hat)
		{
			HatObject hatObj = Hat.gameObject.GetComponent<HatObject>();
			if (hatObj)
				hatObj.SetInvisibleMaterial(amount);
		}

		m_bRestoreShadow = false;

		if (uLink.Network.isClient)
		{
			if (DeviceInfo.PerformanceGrade != DeviceInfo.Performance.Low)
			{
				if (GetComponent<CharacterShadow>().enabled)
				{
					GetComponent<CharacterShadow>().enabled = false;

					m_bRestoreShadow = true;
				}
			}
		}
	}

	public void UpdateEffectAmount(float amount)
	{
		if (LodRenderers != null)
		{
			foreach (Renderer renderer in LodRenderers)
			{
				if (null != renderer.material)
				{
					renderer.material.SetFloat("_EffectAmount", amount);
				}
			}
		}

		if (null != Renderer && Renderer.material != null)
		{
			Renderer.material.SetFloat("_EffectAmount", amount);
		}

		//weapon
		WeaponBase weapon = WeaponComponent.GetCurrentWeapon();

		if (null != weapon)
		{
			weapon.UpdateEffectAmount(amount);
		}

		//hat
		if (null != Hat)
		{
			HatObject hatObj = Hat.gameObject.GetComponent<HatObject>();

			if (null != hatObj)
			{
				hatObj.UpdateEffectAmount(amount);
			}
		}
	}

	public void SetInvisibleOff()
	{
		SetMaterial(DiffuseMaterial);

		if (m_bRestoreShadow)
		{
			m_bRestoreShadow = false;

			GetComponent<CharacterShadow>().enabled = true;
		}
	}

	protected IEnumerator Fadeout()
	{
		// drop weapon
		yield return new WaitForSeconds(0.2f);
		WeaponComponent.GetCurrentWeapon().Drop();

		// wait for some time
		yield return new WaitForSeconds(1.8f);

		// fade-out
		if (uLink.Network.isClient)
		{
			if (Renderer != null && FadeoutMaterial != null)
			{
				SetFadeoutMaterial(-Time.time, 0, 2);
/*				Renderer.material = FadeoutMaterial;
				Renderer.material.SetFloat("_TimeOffs", -Time.time);
				Renderer.material.SetFloat("_Invert", 0);
				Renderer.material.SetFloat("_Duration", 2);
*/
			}
		}

		yield return new WaitForSeconds(2);

		// destroy agent
		if (uLink.Network.isServer)
		{
			ServerRemoveNetworkPlayerFromDamageData(NetworkView.owner);

			uLink.Network.DestroyPlayerObjects(NetworkView.owner);
			uLink.Network.Destroy(gameObject);
		}
	}

	void ServerRemoveNetworkPlayerFromDamageData(uLink.NetworkPlayer player)
	{
		foreach (KeyValuePair<uLink.NetworkPlayer, ComponentPlayer> pair in Player.Players)
		{
			AgentHuman human = pair.Value.Owner;

			if (null != human && null != human.BlackBoard)
			{
				human.BlackBoard.RemoveNetworkPlayerFromDamageData(player);
			}
		}
	}

	public void TeleportFadeOut()
	{
		SetFadeoutMaterial(-Time.time, 1, 1);
/*		if (Renderer)
		{
			Renderer.material = FadeoutMaterial;
			Renderer.material.SetFloat("_TimeOffs", -Time.time);
			Renderer.material.SetFloat("_Invert", 0);
			Renderer.material.SetFloat("_Duration", 1);
		}
*/
	}

	public void TeleportFadeIn(float duration = 1.0f)
	{
		StartCoroutine(FadeIn(duration));
	}

/*	protected IEnumerator FadeIn()
    {
        if (Renderer)
        {
            Renderer.material = FadeoutMaterial;
            Renderer.material.SetFloat("_TimeOffs", -Time.time);
            Renderer.material.SetFloat("_Invert", 1);
            Renderer.material.SetFloat("_Duration", 1);
        }

        yield return new WaitForSeconds(1);

        Renderer.material = DiffuseMaterial;
    }
*/
	//beny: care about LODs
	protected IEnumerator FadeIn(float duration = 1.0f)
	{
		SetFadeoutMaterial(-Time.time, 1, duration);

		yield return new WaitForSeconds(duration);

		SetMaterial(DiffuseMaterial);
	}

	public void ResetMaterial()
	{
//		if (Renderer == null)
//			Renderer = (gameObject.GetComponentInChildren(typeof(SkinnedMeshRenderer)) as SkinnedMeshRenderer);

//		Renderer.material = DiffuseMaterial;
		SetMaterial(DiffuseMaterial);
	}

	//beny: show model - for all LODs
	public void ShowModel(bool show)
	{
		if (Renderer != null)
			Renderer.enabled = show;

		if (LodRenderers != null)
		{
			foreach (Renderer r in LodRenderers)
			{
				r.enabled = show;
			}
		}

		if (Hat != null)
			Hat.GetComponent<Renderer>().enabled = show;

		WeaponBase weapon = WeaponComponent.GetCurrentWeapon();
		if (weapon)
			weapon.ShowWeapon(show);
	}

	//set lightProbeAnchor: since lightProbeAnchor is by default taken from the center of objects Bounding Box, this point sometimes gets too low or under the floor
	// which results in badly lit object. We're setting the lightProbeAnchor to the same object for agent's models, hat and weapon.
	void SetLightProbeAnchor()
	{
		if (Renderer != null)
			Renderer.probeAnchor = TransformTarget;

		if (LodRenderers != null)
		{
			foreach (Renderer r in LodRenderers)
			{
				r.probeAnchor = TransformTarget;
			}
		}
	}

	//is the model show? (just check the main renderer)
	public bool IsShown()
	{
		return Renderer != null ? Renderer.enabled : false;
	}

	//beny: set given material to all LODs
	protected void SetMaterial(Material mtl)
	{
		if (LodRenderers != null)
		{
			foreach (Renderer r in LodRenderers)
			{
				r.material = mtl;
			}
		}

		if (Renderer != null)
		{
			Renderer.material = mtl;
		}

		//weapon
		WeaponBase weapon = WeaponComponent.GetCurrentWeapon();
		if (weapon)
			weapon.SetDefaultMaterial();

		//hat
		if (Hat)
		{
			HatObject hatObj = Hat.gameObject.GetComponent<HatObject>();

			if (hatObj)
				hatObj.SetDefaultMaterial();
		}
	}

	//beny: set Fadeout material to all LODs
	protected void SetFadeoutMaterial(float timeOfs, float invert, float duration)
	{
		if (LodRenderers != null)
		{
			foreach (Renderer r in LodRenderers)
			{
				r.material = FadeoutMaterial;
				r.material.SetFloat("_TimeOffs", timeOfs);
				r.material.SetFloat("_Invert", invert);
				r.material.SetFloat("_Duration", duration);

//				if (FadeoutOverride.enabled)
//					r.material.SetColor("_FXColor", FadeoutOverride.color);
			}
		}

		if (Renderer != null)
		{
			Renderer.material = FadeoutMaterial;
			Renderer.material.SetFloat("_TimeOffs", timeOfs);
			Renderer.material.SetFloat("_Invert", invert);
			Renderer.material.SetFloat("_Duration", duration);

//			if (FadeoutOverride.enabled)
//				Renderer.material.SetColor("_FXColor", FadeoutOverride.color);
		}

		//weapon
		WeaponBase weapon = WeaponComponent.GetCurrentWeapon();
		if (weapon)
			weapon.SetFadeoutMaterial(timeOfs, invert, duration);

		//hat
		if (Hat)
		{
			HatObject hatObj = Hat.gameObject.GetComponent<HatObject>();

			if (hatObj)
				hatObj.SetFadeoutMaterial(timeOfs, invert, duration);
		}
	}

	public void SoundPlay(AudioClip clip)
	{
		if (IsOwner || IsProxy)
		{
			if (Audio.maxDistance*Audio.maxDistance < Vector3.SqrMagnitude(Position - GameCamera.Instance.CameraPosition))
				return;

			if (clip)
				Audio.PlayOneShot(clip);
		}
	}

	public float Heal(float hp)
	{
		if (IsFullyHealed)
			return 0;

		float old = BlackBoard.Health;
		BlackBoard.Health = Mathf.Min(BlackBoard.Health + hp, BlackBoard.RealMaxHealth);

		if (IsFullyHealed)
			ClearDamageData();

		NetworkView.RPC("ClientHeal", uLink.RPCMode.Owner, BlackBoard.Health); // send only byte ? instead of four ?

		return BlackBoard.Health - old;
	}

	[uSuite.RPC]
	public void ClientHeal(float health)
	{
		BlackBoard.Health = health;
		SoundPlay(HealSound);
		GuiHUD.Instance.Heal();
	}

	void EnumerateColliders()
	{
		List<Collider> temp = new List<Collider>();
		string[] bones = new string[] {"pelvis", "head", "Rarm", "Larm", "Rforearm", "Lforearm", "Ribs", "Rthigh", "Lthigh", "Rcalf", "Lcalf"};

		foreach (string b in bones)
		{
			Collider c = Transform.GetChildComponent<Collider>(b);

			if (c != null)
			{
				temp.Add(c);
			}
		}

		Colliders = temp.ToArray();
	}

	public void DisableCollisions()
	{
/*        if (CollisionController)
            CollisionController.enabled = false;

        if (CharacterController)
            CharacterController.enabled = false;
*/
		Collider[] colliders = GameObject.GetComponentsInChildren<Collider>();
		foreach (Collider c in colliders)
		{
			c.enabled = false;
		}

		//disable CharacterController collision - we use the Ragdoll colliders
		if (CharacterController && CharacterController.GetComponent<Collider>())
		{
			///			CharacterController.detectCollisions = false;
			CharacterController.GetComponent<Collider>().enabled = false;
		}

		ToggleCollisions(false, false);
	}

	public void EnableCollisions()
	{
/*        if (CollisionController)
            CollisionController.enabled = true;

        if (CharacterController)
            CharacterController.enabled = true;
*/

		Collider[] colliders = GameObject.GetComponentsInChildren<Collider>();
		foreach (Collider c in colliders)
		{
			c.enabled = true;
		}

		//disable CharacterController collision - we use the Ragdoll colliders
		if (CharacterController && CharacterController.GetComponent<Collider>())
		{
			///			CharacterController.detectCollisions = false;
			CharacterController.GetComponent<Collider>().enabled = false;
		}

		// character controller disabled - we are using ragdoll collisions now
		ToggleCollisions(false, true);
	}

	public void ToggleCollisions(bool BlockPlayer, bool BlockRaycasts)
	{
		// setup "ours new" collision system...

		if (CollSphere)
			CollSphere.SetActive(BlockPlayer);

		// setup "standard" (ragdoll) colliders...

/*		int  layer  = LayerMask.NameToLayer("Ragdoll");
		bool enable = true;

		if (!BlockPlayer)
		{
			layer  = LayerMask.NameToLayer("Ragdoll");
			enable = BlockRaycasts;
		}
		
		if (Colliders != null)
		{
			foreach ( Collider c in Colliders )
			{
				c.enabled          = enable;
				c.gameObject.layer = layer;
			}
		}
*/
		if (CharacterController)
		{
			CharacterController.enabled = BlockPlayer;
		}
	}

	//
	void EnablePhysics(Rigidbody rb, bool enable)
	{
		rb.isKinematic = !enable;
		rb.useGravity = enable;

/*
 * The code below is useful for scaled-down bones and their children (e.g. removed limbs via scale). 
 * These are not used in DeadZone, plus this code causes that the Unity ragdoll becomes reinitialized, taking the current skeleton pose (animation) as a base pose - which fucks up the joint limits.
 * This behaviour should be fixed in (some) next version of Unity.
 * 
		CharacterJoint joint = rb.gameObject.GetComponent<CharacterJoint>();
			
		if (!enable)
		{
			rb.Sleep();
			
			if (joint)
			{
//				Debug.Log ("EnablePhysics = FALSE, rb=" + rb.name + ", joint=" + joint.name + ", joint.connectedBody=" + joint.connectedBody);
				
				joint.connectedBody = null;
			}
		}
		else 					//else rb.WakeUp(); ?		see also rb.detectCollisions
		{
			if (joint && joint.connectedBody == null)
			{
				Transform	parent = rb.transform.parent;
				Rigidbody	parent_rb = parent.GetComponent<Rigidbody>();
				
				//if the parent transform doesn't have Rigidbody, traverse to top until one is found (or parent is null)
				//NOTE: possibly SLOW - TODO: need to Profile it!
				while (parent_rb == null && parent != null)
				{
					parent = parent.transform.parent;
					parent_rb = parent.GetComponent<Rigidbody>();
				}
				
				joint.connectedBody = parent_rb;
				
//				Debug.Log ("EnablePhysics = TRUE, rb=" + rb.name + ", joint=" + joint.name + ", joint.connectedBody=" + joint.connectedBody + ", parent_rb=" + parent_rb);
			}
		}
*/
	}

	//enable/disable Ragdoll
	public void EnableRagdoll(bool enable)
	{
//		Debug.Log ( "EnableRagdoll(), name=" + this.name + ", enable=" + enable + ", RigidBodies:" + (RigidBodies != null ? RigidBodies.Length : 0) );

		//
		EnableHatCollision(!enable);

		//
		if (RigidBodies != null)
		{
			ArrayList scaledBodies = new ArrayList();

			//parse whole Ragdoll
			foreach (Rigidbody rb in RigidBodies)
			{
//				Debug.Log ("EnableRagdoll, name=" + this.name + ", rb=" + rb.name + ", scale=" + rb.transform.localScale);
				if (!enable || (enable && rb.transform.localScale.x > 0.9f)) //enable only if the scale is greater than 0.9 (limb is not decapicated)
				{
					EnablePhysics(rb, enable);
				}
				else
				{
					//add the scaled-down rb.transform to a list, later we need to scale-down its children too
					scaledBodies.Add(rb);
				}
			}

			//disable children of scaled-down RigidBodies
			foreach (Rigidbody rb in scaledBodies)
			{
				Rigidbody[] children = rb.GetComponentsInChildren<Rigidbody>();

				foreach (Rigidbody child in children)
				{
					EnablePhysics(child, false);
//					Debug.Log ("EnableRagdoll, name=" + this.name + ", rb=" + rb.name + ", child=" + child.name);
				}
			}
		}
	}

	public bool CanUseGadget()
	{
		if (IsAlive == false)
			return false;

		if (BlackBoard.BusyAction)
			return false;

		if (BlackBoard.Desires.Sprint)
			return false;

		if (WeaponComponent.GetCurrentWeapon().IsBusy())
			return false;

		if (IsSpawnedRecently)
		{
			return false;
		}

		return true;
	}

	public bool CanMelee()
	{
		if (IsAlive == false)
			return false;

		if (BlackBoard.BusyAction)
			return false;

		if (BlackBoard.Desires.Sprint)
			return false;

		if (BlackBoard.Cover)
			return false;

		if (BlackBoard.Desires.MeleeTarget == null)
			return false;

		if (BlackBoard.Desires.MeleeTarget.IsFriend(this))
			return false;

		if (WeaponComponent.GetCurrentWeapon().IsBusy())
			return false;

		// melee musi byt mozne i bez naboju
		//if (WeaponComponent.GetCurrentWeapon().ClipAmmo == 0)
		//  return false;

		return true;
	}

	// ------
	public bool CanFire()
	{
		return CanFire2() && !WeaponComponent.GetCurrentWeapon().IsBusy();
	}

	// -------
	// Nepocita se zde zda je zbran busy, aby akce zbrane nezrusily priznak WeaponTriggerOn v ComponentPlayerMPOwner
	public bool CanFire2()
	{
		if (IsAlive == false)
			return false;

		if (BlackBoard.BusyAction)
			return false;

		if (BlackBoard.Desires.Sprint)
			return false;

		if (WeaponComponent.GetCurrentWeapon().ClipAmmo == 0)
			return false;

		if (BlackBoard.Cover)
			return BlackBoard.Cover.CanFire(this);

		return true;
	}

	public bool CanUseGadget(Item gadget)
	{
		if (gadget == null)
			return false;

		if (IsAlive == false)
			return false;

		if (gadget.Settings.ItemUse != E_ItemUse.Activate)
			return false; // player can active this type of gadget !!

		if (BlackBoard.BusyAction)
			return false;

		if (BlackBoard.Desires.Sprint)
			return false;

		if (gadget.IsAvailableForUse() == false)
			return false;

		if (IsSpawnedRecently)
		{
			return false;
		}

		if (IsInCover)
		{
			if (gadget.Settings.ItemBehaviour == E_ItemBehaviour.Place)
				return false;

			if (gadget.Settings.ItemBehaviour == E_ItemBehaviour.Throw)
			{
				if (WeaponComponent.GetCurrentWeapon().IsBusy())
					return false;

				return BlackBoard.Cover.CanFire(this);
			}
		}
		else
		{
			if (gadget.Settings.ItemBehaviour == E_ItemBehaviour.Throw || gadget.Settings.ItemBehaviour == E_ItemBehaviour.Place)
			{
				if (WeaponComponent.GetCurrentWeapon().IsBusy())
					return false;
			}
		}

		return true;
	}

	public E_Direction GetMoveDirectionType(Vector3 moveDirection)
	{
		if (BlackBoard.Desires.MoveDirection.sqrMagnitude < 0.1f)
			return E_Direction.Forward;

		Vector3 dir = BlackBoard.Desires.MoveDirection.normalized;

		Vector2 bodyForward = new Vector2(Transform.forward.x, Transform.forward.z);
		Vector2 bodyRight = new Vector2(Transform.right.x, Transform.right.z);

		Vector2 moveDir = new Vector2(dir.x, dir.z);

		float a = Vector2.Angle(bodyForward, moveDir);
		float b = Vector2.Angle(bodyRight, moveDir);

		//  Debug.Log("forward " + a + " right " + b + " " + Owner.BlackBoard.Desires.MoveDirection);

		if (a <= 35)
			return E_Direction.Forward;
		else if (a > 145)
			return E_Direction.Backward;
		else if (b < 90)
			return E_Direction.Right;
		else
			return E_Direction.Left;
	}

	public void ClearDamageData()
	{
		BlackBoard.AttackersDamageData.Clear();
	}

	void uLink_OnNetworkInstantiate(uLink.NetworkMessageInfo info)
	{
		PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(NetworkView.owner);
		Team = ppi.Team;

		//if(Game.Instance.GameLog)  Debug.Log(" Instantiated " + ppi.Name + " " + Team);

		if (IsServer)
		{
#if !DEADZONE_CLIENT
			BlackBoard.SpawnProtectionRestTime = Server.Instance.GameInfo.GetSpawnTimeProtection();
#endif
		}
		else
		{
			BlackBoard.SpawnProtectionRestTime = Client.Instance.GameState.SpawnTimeProtection;
		}
	}

	void AddHat()
	{
		PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(NetworkView.owner);
		E_HatID hat = ppi.EquipList.Outfits.Hat;

		if (hat != E_HatID.None)
		{
			HatSettings s = HatSettingsManager.Instance.Get(hat);

			if (s != null && s.Model != null)
			{
				Hat = GameObject.Instantiate(s.Model) as GameObject;

				Transform t = Hat.transform;
				t.parent = HatTarget;
				t.localPosition = Vector3.zero;
				t.localRotation = Quaternion.identity;
				t.localScale = Vector3.one;
								//Unity sets this to 1/parent_scale, but we want the parent scale applied, i.e. scale this object based on the parent scale.

				Hat.GetComponent<Renderer>().probeAnchor = TransformTarget;
				Hat.SetActive(true);
				EnableHatCollision(true);

				HitZone zone = t.GetComponent<HitZone>();
				if (zone)
					zone.InitHitZoneOwner();
			}
		}
	}

	void RemoveHat()
	{
		if (Hat != null)
		{
			Hat.transform.parent = null;
			Hat.SetActive(false);
		}
	}

	public void DestroyHat()
	{
		if (Hat != null)
		{
			Destroy(Hat);
			Hat = null;
		}
	}

	//
	void EnableHatCollision(bool enable)
	{
		if (Hat != null)
		{
			Hat.GetComponent<Collider>().enabled = enable;
		}
	}

	//this is called when we want to shot off the hat off the player's head and give it a pyhical impulse (see HatObject.cs)
	[uSuite.RPC]
	public void ShotOffHat(Vector3 impulse, bool server)
	{
		if (server)
		{
			DestroyHat();
			return;
		}

		if (Hat != null)
		{
			Hat.transform.parent = null;
			Hat.GetComponent<Renderer>().probeAnchor = null;

			//
			Hat.GetComponent<Rigidbody>().isKinematic = false;
			Hat.GetComponent<Rigidbody>().useGravity = true;

			Hat.GetComponent<Rigidbody>().AddForce(impulse, ForceMode.Impulse);

			HatObject hatObj = Hat.gameObject.GetComponent<HatObject>();

			if (hatObj)
				SoundPlay(hatObj.HitSound);

//			Debug.Log ("ShotOffHat() : hat=" + Hat.name + ", impulse=" + impulse);
		}
	}

	// treba dodelat i do hry
	public void StatisticAddSuicide()
	{
		if (uLink.Network.isServer == false)
			throw new uLink.NetworkException("StatisticAddSuicide: could be called only on server");

		PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(NetworkView.owner);
		if (ppi != null)
			ppi.AddSuicide();
	}

	public void StatisticAddDeath()
	{
		if (uLink.Network.isServer == false)
			throw new uLink.NetworkException("StatisticAddDeath: could be called only on server");

		PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(NetworkView.owner);
		if (ppi != null)
			ppi.AddDeath(Time.timeSinceLevelLoad - RespawnedTime, WeaponComponent.CurrentWeapon);
	}

	public void StatisticAddItemUse(E_ItemID id)
	{
		if (uLink.Network.isServer == false)
			throw new uLink.NetworkException("StatisticAddItemUse: could be called only on server");

		PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(NetworkView.owner);
		if (ppi != null)
			ppi.AddItemUse(id);
		;
	}

	public void StatisticAddItemKill(E_ItemID id)
	{
		if (uLink.Network.isServer == false)
			throw new uLink.NetworkException("StatisticAddItemKill: could be called only on server");

		PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(NetworkView.owner);
		if (ppi != null)
			ppi.AddItemKill(id);
	}

	public void StatisticAddWeaponUse(E_WeaponID id)
	{
		if (uLink.Network.isServer == false)
			throw new uLink.NetworkException("StatisticAddWeaponUse: could be called only on server");

		PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(NetworkView.owner);
		if (ppi != null)
			ppi.AddWeaponUse(id);
	}

	public void StatisticAddWeaponHit(E_WeaponID id)
	{
		if (uLink.Network.isServer == false)
			throw new uLink.NetworkException("StatisticAddWeaponHit: could be called only on server");

		PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(NetworkView.owner);
		if (ppi != null)
			ppi.AddWeaponHit(id);
	}

	public void StatisticAddWeaponKill(E_WeaponID id, E_BodyPart bodyPart)
	{
		if (uLink.Network.isServer == false)
			throw new uLink.NetworkException("StatisticAddWeaponKill: could be called only on server");

		PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(NetworkView.owner);
		if (ppi != null)
			ppi.AddWeaponKill(id, bodyPart);
	}

	// update bones/meshes to current animation frame
	public void SampleAnimations()
	{
		Animation[] Anims = gameObject.GetComponents<Animation>();

		foreach (Animation Anim in Anims)
		{
			Anim.Sample();
		}
	}

	void Update()
	{
		if (false == IsServer)
		{
			return;
		}

		ServerUpdateFalling();

		m_DominantAnimTimer += Time.deltaTime;
	}

	void ServerUpdateFalling()
	{
		bool falling = !CharacterController.isGrounded;

		if (falling)
		{
			//Debug.Log( "Falling ... " );

			if (0.0f == m_FallingTime)
			{
				m_FallingStart = Position.y;
			}

			m_FallingTime += Time.deltaTime;
		}
		else
		{
			if (0.0f != m_FallingTime)
			{
				float minimalHeight = BlackBoard.DamageSetup.FallHeightForDamage;
				float maximalHeight = BlackBoard.DamageSetup.FallHeightForKill;

				float height = m_FallingStart - Position.y;

				//Debug.Log( "Grounded from height : " + height + " meteres");

				m_FallingTime = 0;

				if (height > minimalHeight)
				{
					//height = Mathf.Min( height, maximalHeight );

					float damage = 140*(height - minimalHeight)/(maximalHeight - minimalHeight);

					E_BodyPart hitBodyPart = Random.Range(0, 2) > 0 ? E_BodyPart.LeftLeg : E_BodyPart.RightLeg;

					TakeDamage(this, damage, Position, Forward, E_WeaponID.None, E_ItemID.None, hitBodyPart, true);
					//Debug.Log( "Damage : " + damage + " HP");
				}
			}

			m_FallingTime = 0.0f;
		}
	}

	/*void OnControllerColliderHit( ControllerColliderHit hit )
	{
		// future use
	}*/

	public void cbServerUserInput()
	{
#if !DEADZONE_CLIENT
		if( null != Server.Instance )
		{
			Server.Instance.cbUserInput();
		}
#endif
	}
}
