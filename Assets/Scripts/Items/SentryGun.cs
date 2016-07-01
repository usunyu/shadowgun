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

//#####################################################################################################################

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//#####################################################################################################################

[AddComponentMenu("Items/Projectiles/SentryGun")]
[RequireComponent(typeof (AudioSource))]
[RequireComponent(typeof (uLink.NetworkView))]
public class SentryGun : Agent
{
	#region enums /////////////////////////////////////////////////////////////////////////////////////////////////////

	enum E_AimPhase
	{
		Idle,
		Starting,
		InProgress,
		Stopping
	}

	#endregion

	#region constants /////////////////////////////////////////////////////////////////////////////////////////////////

	const float MuzzleRotLimit = 8.00f;
	const float MuzzleScaleLimit = 0.05f;

	#endregion

	#region classes ///////////////////////////////////////////////////////////////////////////////////////////////////

	[System.Serializable]
	public class BaseSettings
	{
		public float HealthPoints = 200;
	}

	[System.Serializable]
	public class PartsSettings
	{
		public Transform m_Base; //
		public Transform m_Neck; //
		public Transform m_Muzzle; //
	}

	[System.Serializable]
	public class AimingSettings
	{
		// horizontal motor
		public float m_HRange = 140.0f; // movement range in degrees ( -1 = unlimited )
		public float m_HSpeed = 40.0f; // degrees per second
		// vertical motor
		public float m_VRange = 80.0f; // movement range in degrees ( -1 = unlimited )
		public float m_VSpeed = 30.0f; // degrees per second
		// detection range of enemies
		public float m_FOV = 60.0f; // in degrees
		// target "in-sights" tolerance
		public float m_InSightsTolerance = 4.0f; // allowed difference from aiming-dir (in degrees)
		// target is "completely" lost after this time
		public float m_TargetLostTimeout = 2.0f;
		// offset for aiming
		public float m_OffsetY = 0.0f;
	}

	[System.Serializable]
	public class WpnSettings
	{
		public int m_AmmoClip = 20;
		public int m_AmmoTotal = 200;
		public float m_FireRate = 0.10f;
		public float m_FireEffectTime = 0.08f;
		public float m_ReloadDelay = 2.0f;
		public float m_Dispersion = 0.05f;
		public float m_RangeMaximal = 30.0f;
		public float m_ProjSpeed = 100.0f;
		public float m_Damage = 10.0f;
		public float m_ProjImpuls = 0.0f;
		public E_ProjectileType m_ProjType = E_ProjectileType.Bullet;
	}

	[System.Serializable]
	public class SoundsSettings
	{
		public AudioClip m_Fire;
		public AudioClip m_AimingStart;
		public AudioClip m_AimingLoop;
		public AudioClip m_AimingStop;
	}

	class TargetData
	{
		public AgentHuman m_Agent;
		public Vector3 m_LastVisiblePos;
		public float m_LastVisibleTime;
		public bool m_InFOV;
		public bool m_InSights;
		public bool m_InRange;
		public bool m_IsVisible;
	}

	[System.Serializable]
	public class DeactivationSettings
	{
		public float m_Delay;
		public Explosion m_Explosion;
		public Transform m_ExplosionOrigin;
		public float m_ExplosionDamage;
		public float m_ExplosionRadius;
	}

	#endregion

	#region fields ////////////////////////////////////////////////////////////////////////////////////////////////////

	// settings...
	public BaseSettings m_BaseSettings = new BaseSettings();
	public PartsSettings m_PartsSettings = new PartsSettings();
	public AimingSettings m_AimSettings = new AimingSettings();
	public WpnSettings m_WpnSettings = new WpnSettings();
	public SoundsSettings m_SndSettings = new SoundsSettings();

	[SerializeField] ItemIcons Icons;

	// aiming...
	Vector3 m_AimDir;
	E_AimPhase m_AimPhase;
	Rotator m_HMotor;
	Rotator m_VMotor;
	float m_InitHAngle;
	float m_InitVAngle;

	// target...
	TargetData m_Target = new TargetData();
	AgentHuman m_Owner;

	// remaining...
	int m_AmmoClip;
	int m_AmmoRemaining;
	int m_AmmoTotal;
	float m_NextShotTime;
	float m_ShotEffectTime;
	float m_ReloadTime;
	float m_SelectTargetTime;

	// cached, precomputed...
	float m_ThresholdInFOV;
	float m_ThresholdInSight;
	Projectile.InitSettings m_ProjInitSettings;

	Vector3 m_MuzzleOriginalRot;
	Vector3 m_MuzzleOriginalScale;

	float m_Health;
	GameObject n_Muzzle;

	E_ItemID ItemID;

	int GoldReward;

	// deactivation...
	//private   bool                     m_Deactivated  = false;
	public DeactivationSettings m_Deactivation = new DeactivationSettings();

	#endregion

	#region properties ////////////////////////////////////////////////////////////////////////////////////////////////

	public bool OutOfAmmo
	{
		get { return (m_AmmoRemaining == 0) && (m_AmmoClip == 0); }
	}

	public override bool IsAlive
	{
		get { return m_Health > 0; }
	}

	public override bool IsVisible
	{
		get { return true; }
	}

	public override bool IsInvulnerable
	{
		get { return false; }
	}

	public override bool IsInCover
	{
		get { return false; }
	}

	public override bool IsEnteringToCover
	{
		get { return false; }
	}

	public override Vector3 ChestPosition
	{
		get { return m_PartsSettings.m_Neck.position; }
	}

	#endregion

	#region methods ///////////////////////////////////////////////////////////////////////////////////////////////////

	//-----------------------------------------------------------------------------------------------------------------
	//-----------------------------------------------------------------------------------------------------------------
	void Awake()
	{
		base.Initialize();
		// find other component(s)...

		Audio.playOnAwake = false;
		Audio.loop = true;
		Audio.clip = m_SndSettings.m_AimingLoop;

		// init motors...

		m_InitHAngle = 0.0f;
		m_InitVAngle = 0.0f;

		m_HMotor = new Rotator(m_InitHAngle, m_AimSettings.m_HRange*Mathf.Deg2Rad, m_AimSettings.m_HSpeed*Mathf.Deg2Rad);
		m_VMotor = new Rotator(m_InitVAngle, m_AimSettings.m_VRange*Mathf.Deg2Rad, m_AimSettings.m_VSpeed*Mathf.Deg2Rad);

		// muzzle...

		m_MuzzleOriginalRot = m_PartsSettings.m_Muzzle.localEulerAngles;
		m_MuzzleOriginalScale = m_PartsSettings.m_Muzzle.localScale;

		n_Muzzle = m_PartsSettings.m_Muzzle.gameObject;
		n_Muzzle.SetActive(false);

		// init...

		m_ThresholdInFOV = Mathf.Cos(Mathf.Deg2Rad*m_AimSettings.m_FOV*0.5f);
		m_ThresholdInSight = Mathf.Cos(Mathf.Deg2Rad*m_AimSettings.m_InSightsTolerance);

		m_AmmoClip = m_WpnSettings.m_AmmoClip;
		m_AmmoRemaining = m_WpnSettings.m_AmmoTotal - m_AmmoClip;

		m_Health = m_BaseSettings.HealthPoints;
	}

	void OnDestroy()
	{
		StopAllCoroutines();

		if (null != m_Owner)
			m_Owner.GadgetsComponent.UnRegisterLiveGadget(ItemID, GameObject);
	}

	void InitWorker(AgentHuman Human)
	{
		m_Owner = Human;

		transform.rotation = m_Owner.Transform.rotation;

		m_ProjInitSettings = new Projectile.InitSettings();
		m_ProjInitSettings.Agent = m_Owner;
		m_ProjInitSettings.IgnoreTransform = m_Owner.Transform;
		m_ProjInitSettings.Speed = m_WpnSettings.m_ProjSpeed;
		m_ProjInitSettings.Damage = m_WpnSettings.m_Damage;
		m_ProjInitSettings.Impulse = m_WpnSettings.m_ProjImpuls;

		m_Owner.GadgetsComponent.RegisterLiveGadget(ItemID, GameObject);

		m_ProjInitSettings.ItemID = ItemID;
		Icons.SetTeamIcon(m_Owner.Team);
	}

	void uLink_OnNetworkInstantiate(uLink.NetworkMessageInfo info)
	{
		ComponentPlayer P = Player.GetPlayer(info.networkView.owner);

		info.networkView.initialData.ReadVector3();
		ItemID = info.networkView.initialData.Read<E_ItemID>();

		GoldReward = ItemSettingsManager.Instance.Get(ItemID).GoldReward;

		if (null != P)
		{
			InitWorker(P.Owner);
		}
		else
		{
			Debug.LogError(" SentryGyn.uLink_OnNetworkInstantiate() : unexpected error ");
		}
	}

	public void OnProjectileHit(Projectile projectile)
	{
		if (IsFriend(projectile.Agent))
		{
			projectile.ignoreThisHit = true;
			return;
		}

		// Main function with damage processing...
		TakeDamage(projectile.Agent,
				   projectile.Damage*0.1f,
				   projectile.Transform.forward*projectile.Impulse,
				   projectile.WeaponID,
				   projectile.ItemID);
	}

	public void OnExplosionHit(Explosion explosion)
	{
		//   print("OnExplosionHit : " + attacker.name + " " + damage);

		if (IsFriend(explosion.Agent))
		{
			//print("OnExplosionHit : " + attacker.name + " ignore friend damage");
			return;
		}

		// Main function with damage processing...
		TakeDamage(explosion.Agent, explosion.Damage*0.1f, explosion.Impulse, explosion.m_WeaponID, explosion.m_ItemID);
	}

	// sentrygun hitted by EMP explosion
	public void OnEMPExplosionHit(ExplosionEMP Explosion)
	{
		if (IsFriend(Explosion.Agent))
		{
			return;
		}

		TakeDamage(Explosion.Agent, float.MaxValue, Explosion.Impulse, Explosion.m_WeaponID, Explosion.m_ItemID);
	}

	void TakeDamage(AgentHuman inAttacker, float inDamage, Vector3 inImpuls, E_WeaponID weaponID, E_ItemID itemID)
	{
		// Only server players should take damage or die as a consequence of damage. Client players die from server messages.
		if (uLink.Network.isServer)
		{
			if (m_Health - inDamage > 0)
			{
				m_Health = Mathf.Max(0, m_Health - inDamage);
			}
			else if (m_Health > 0)
			{
				m_Health = 0;

				PPIManager.Instance.ServerAddScoreForTurretKill(inAttacker.NetworkView.owner, GoldReward);
				OnDeactivation();
			}
		}
	}

	//-----------------------------------------------------------------------------------------------------------------
	//-----------------------------------------------------------------------------------------------------------------
	void Update()
	{
		UpdateAiming(Time.deltaTime);

		if (!OutOfAmmo)
		{
			UpdateTarget(Time.timeSinceLevelLoad);

			UpdateReload(Time.deltaTime);
		}

		UpdateShooting(Time.deltaTime);
	}

	float tm;

	//-----------------------------------------------------------------------------------------------------------------
	//-----------------------------------------------------------------------------------------------------------------
	void UpdateAiming(float DeltaTime)
	{
		bool aiming = false;
		bool dirInTolerance = Mathf.Max(m_HMotor.AbsDiff, m_VMotor.AbsDiff) <= 1.0f*Mathf.Deg2Rad;

		if (dirInTolerance)
		{
			tm = 0.0f;
		}
		else
		{
			tm += DeltaTime;
			aiming = tm > 0.1f;
		}

		// update aiming direction...

		m_HMotor.Update(DeltaTime);
		m_VMotor.Update(DeltaTime);

		m_AimDir = MathUtils.AnglesToVector(m_PartsSettings.m_Base.forward,
											m_PartsSettings.m_Base.up,
											m_HMotor.Angle,
											m_VMotor.Angle);

		m_PartsSettings.m_Neck.rotation = Quaternion.LookRotation(m_AimDir, m_PartsSettings.m_Base.up);

		// update aiming phase, sounds,...

		E_AimPhase prevPhase = m_AimPhase;

		if (aiming)
		{
			if ((m_AimPhase == E_AimPhase.Idle) || (m_AimPhase == E_AimPhase.Stopping))
			{
				ChangeAimPhase(E_AimPhase.Starting);
			}
		}
		else
		{
			if ((m_AimPhase == E_AimPhase.Starting) || (m_AimPhase == E_AimPhase.InProgress))
			{
				ChangeAimPhase(E_AimPhase.Stopping);
			}
		}

		if (prevPhase == m_AimPhase)
		{
			if ((Audio == null) || (Audio.isPlaying == false))
			{
				if (m_AimPhase == E_AimPhase.Starting)
				{
					ChangeAimPhase(E_AimPhase.InProgress);
				}
				else if (m_AimPhase == E_AimPhase.Stopping)
				{
					ChangeAimPhase(E_AimPhase.Idle);
				}
			}
		}

		// set new target direction...

		if (m_Target.m_Agent != null)
		{
			float angleH = 0.0f;
			float angleV = 0.0f;
			Vector3 tarDir = Vector3.Normalize(m_Target.m_LastVisiblePos - m_PartsSettings.m_Neck.position);

			MathUtils.VectorToAngles(m_PartsSettings.m_Base.forward,
									 m_PartsSettings.m_Base.up,
									 tarDir,
									 ref angleH,
									 ref angleV);

			m_HMotor.TargetAngle = angleH;
			m_VMotor.TargetAngle = angleV;
		}
		else if (!OutOfAmmo)
		{
			m_HMotor.TargetAngle = m_InitHAngle;
			m_VMotor.TargetAngle = m_InitVAngle;
		}
	}

	//-----------------------------------------------------------------------------------------------------------------
	//-----------------------------------------------------------------------------------------------------------------
	void ChangeAimPhase(E_AimPhase NewPhase)
	{
		if (Audio != null)
		{
			float coef = 0.0f;

			if ((Audio.isPlaying == true) && (m_AimPhase != E_AimPhase.InProgress))
			{
				coef = 1.0f - Audio.time/Audio.clip.length;
			}

			Audio.Stop();
			Audio.clip = null;
			Audio.loop = false;

			switch (NewPhase)
			{
			case E_AimPhase.Starting:
			{
				if (m_SndSettings.m_AimingStart != null)
				{
					Audio.clip = m_SndSettings.m_AimingStart;
					Audio.time = coef*Audio.clip.length;
				}
			}
				break;
			case E_AimPhase.InProgress:
			{
				Audio.clip = m_SndSettings.m_AimingLoop;
				Audio.loop = true;
			}
				break;
			case E_AimPhase.Stopping:
			{
				if (m_SndSettings.m_AimingStop != null)
				{
					Audio.clip = m_SndSettings.m_AimingStop;
					Audio.time = coef*Audio.clip.length;
				}
			}
				break;
			}

			Audio.Play();
		}

		m_AimPhase = NewPhase;
	}

	//-----------------------------------------------------------------------------------------------------------------
	//-----------------------------------------------------------------------------------------------------------------
	void UpdateTarget(float CurrTime)
	{
		bool selectNewTarget = false;

		selectNewTarget |= (m_Target.m_Agent == null) || (m_Target.m_Agent.IsAlive == false);
		selectNewTarget |= (CurrTime - m_Target.m_LastVisibleTime >= m_AimSettings.m_TargetLostTimeout);

		if (selectNewTarget)
		{
			SelectTarget(CurrTime);
		}
		else if (m_Target.m_Agent != null)
		{
			Vector3 dir = m_Target.m_Agent.TransformTarget.position - m_PartsSettings.m_Neck.position;
			float dist = Vector3.Magnitude(dir);

			bool hasJammer = m_Target.m_Agent.GadgetsComponent.IsGadgetAvailableWithBehaviour(E_ItemBehaviour.Jammer);
			if (hasJammer)
			{
				const float distMult = 3;
				dist *= distMult; //when the target has Jammer, lower down the detection distance for him
				dir *= distMult;
			}

			float dot = Vector3.Dot(m_AimDir, dir/dist);

			m_Target.m_InFOV = dot > m_ThresholdInFOV;
			m_Target.m_InSights = dot > m_ThresholdInSight;
			m_Target.m_InRange = dist < m_WpnSettings.m_RangeMaximal;
			m_Target.m_IsVisible = IsTargetVisible(m_Target.m_Agent);

			if (m_Target.m_InRange && m_Target.m_InFOV && m_Target.m_IsVisible)
			{
				if (m_Target.m_Agent.IsInCover)
				{
					m_Target.m_LastVisiblePos = m_Target.m_Agent.TransformEye.position;
				}
				else
				{
					m_Target.m_LastVisiblePos = m_Target.m_Agent.TransformTarget.position;
					m_Target.m_LastVisiblePos.y += m_AimSettings.m_OffsetY;
				}
				m_Target.m_LastVisibleTime = CurrTime;
			}
		}
	}

	//-----------------------------------------------------------------------------------------------------------------
	//-----------------------------------------------------------------------------------------------------------------
	void SelectTarget(float CurrTime)
	{
		m_Target.m_Agent = null;

		AgentHuman e, best = null;
		Vector3 dir;
		float dist, defl, dot, bestDot = 0.0f;
		float rating, bestRating = 0.0f;

		foreach (KeyValuePair<uLink.NetworkPlayer, ComponentPlayer> pair in Player.Players)
		{
			e = pair.Value.Owner;

			if ((e == null) || (IsTargetValid(e) == false))
				continue; // invalid

			dir = e.TransformTarget.position - m_PartsSettings.m_Neck.position;
			dist = Vector3.Magnitude(dir);

			bool hasJammer = e.GadgetsComponent.IsGadgetAvailableWithBehaviour(E_ItemBehaviour.Jammer);
			if (hasJammer)
			{
				const float distMult = 3;
				dist *= distMult; //when the target has Jammer, lower down the detection distance for him
				dir *= distMult;
			}

			if (dist > m_WpnSettings.m_RangeMaximal)
				continue; // too far

			dot = Vector3.Dot(m_AimDir, dir/dist);

			if ((dot < m_ThresholdInFOV) || (IsTargetVisible(e) == false))
				continue; // outside view or not visible

			dist = 1.0f - (dist/m_WpnSettings.m_RangeMaximal); // distance .................. in range [0,1]
			defl = 2.0f/(dot + 1.0f); // deflection from aim-dir ... in range [0,1]
			rating = 2.0f*dist + defl;

			if (dot > m_ThresholdInSight)
			{
				rating += 0.5f;
			}

			if (rating > bestRating)
			{
				best = e;
				bestRating = rating;
				bestDot = dot;
			}
		}

		if (best != null)
		{
			m_Target.m_Agent = best;
			m_Target.m_LastVisiblePos = best.TransformTarget.position;
			m_Target.m_LastVisibleTime = CurrTime;
			m_Target.m_InFOV = true;
			m_Target.m_InSights = bestDot > m_ThresholdInSight;
			m_Target.m_InRange = true;
			m_Target.m_IsVisible = true;
		}
	}

	//-----------------------------------------------------------------------------------------------------------------
	//-----------------------------------------------------------------------------------------------------------------
	protected virtual bool IsTargetValid(AgentHuman T)
	{
		return T != null && T.IsAlive && IsFriend(T) == false;
	}

	//-----------------------------------------------------------------------------------------------------------------
	//-----------------------------------------------------------------------------------------------------------------
	protected virtual bool IsTargetVisible(AgentHuman T)
	{
		if (T.GadgetsComponent.IsBoostActive(E_ItemBoosterBehaviour.Invisible))
			return false;

		Vector3 p0 = T.TransformTarget.position;
		Vector3 p1 = m_PartsSettings.m_Neck.position;
		Vector3 dir = p0 - p1;
		float lng = Vector3.Magnitude(dir);

		LayerMask mask = ~(ObjectLayerMask.Ragdoll | ObjectLayerMask.IgnoreRayCast);
		RaycastHit[] hits = Physics.RaycastAll(p1, dir/lng, lng, mask);

		foreach (RaycastHit hit in hits)
		{
			if ((hit.collider.isTrigger != true) && (hit.transform != T.transform))
			{
				return false;
			}
		}

		return true;
	}

	//-----------------------------------------------------------------------------------------------------------------
	//-----------------------------------------------------------------------------------------------------------------
	void UpdateReload(float DeltaTime)
	{
		if (m_ReloadTime > 0.0f)
		{
			if ((m_ReloadTime -= DeltaTime) <= 0.0f)
			{
				m_AmmoClip = Mathf.Min(m_AmmoRemaining, m_WpnSettings.m_AmmoClip);
				m_AmmoRemaining -= m_AmmoClip;
			}
		}
	}

	//-----------------------------------------------------------------------------------------------------------------
	//-----------------------------------------------------------------------------------------------------------------
	void UpdateShooting(float DeltaTime)
	{
		// hide muzzle...

		if (m_ShotEffectTime > 0.0f)
		{
			if ((m_ShotEffectTime -= DeltaTime) <= 0.0f)
			{
				n_Muzzle.SetActive(false);
			}
		}

		// time to next shot...

		if (uLink.Network.isServer)
		{
			m_NextShotTime -= Time.deltaTime;

			// shoot...

			if ((m_Target.m_Agent != null) && (m_NextShotTime <= 0.0f) && (m_ReloadTime <= 0.0f))
			{
				if ((m_Target.m_InSights == true) && (m_AmmoClip > 0))
				{
					ServerShootAtTarget();
				}
			}
		}
	}

	//-----------------------------------------------------------------------------------------------------------------
	//-----------------------------------------------------------------------------------------------------------------
	void ServerShootAtTarget()
	{
		// sfx/gfx effects...

		/*Vector3  rot = m_MuzzleOriginalRot;
		Vector3  scl = m_MuzzleOriginalScale;

		rot.z += Random.Range( -MuzzleRotLimit, +MuzzleRotLimit );
		scl   += Vector3.one * Random.Range( -MuzzleScaleLimit, +MuzzleScaleLimit );

		n_Muzzle.SetActive(true);
		m_PartsSettings.m_Muzzle.localRotation     = Quaternion.Euler( rot );
		m_PartsSettings.m_Muzzle.localScale        = scl;

		//Audio.PlayOneShot( m_SndSettings.m_Fire );
		 */
		// spawn projectile...

		Vector3 dir = MathUtils.RandomVectorInsideCone(m_AimDir, m_WpnSettings.m_Dispersion);

		ProjectileManager.Instance.SpawnProjectile(m_WpnSettings.m_ProjType,
												   m_PartsSettings.m_Muzzle.position,
												   dir,
												   m_ProjInitSettings);

//		NetworkView.RPC("ClientFire", uLink.RPCMode.Others, dir);
		NetworkView.UnreliableRPC("ClientFire", uLink.RPCMode.Others, dir);

		// update timer(s)...
		m_NextShotTime = m_WpnSettings.m_FireRate;
		m_ShotEffectTime = m_WpnSettings.m_FireEffectTime;

		if (--m_AmmoClip == 0)
		{
			// schedule reload
			if (m_AmmoRemaining > 0)
			{
				m_ReloadTime = m_WpnSettings.m_ReloadDelay;
			}
			// out-of-ammo ==> deactivate weapon
			else
			{
				float rV = MathUtils.InRange(m_AimSettings.m_VRange, 0.0f, 360.0f) ? m_AimSettings.m_VRange/2.0f : 0.0f;

				m_HMotor.TargetAngle = m_InitHAngle;
				m_VMotor.TargetAngle = m_InitVAngle - Mathf.Deg2Rad*rV;

				//m_Deactivated    = true;
				m_Target.m_Agent = null;

				if (uLink.Network.isServer)
					Invoke("OnDeactivation", m_Deactivation.m_Delay);
			}
		}
	}

	[uSuite.RPC]
	void ClientFire(Vector3 dir)
	{
		Vector3 rot = m_MuzzleOriginalRot;
		Vector3 scl = m_MuzzleOriginalScale;

		rot.z += Random.Range(-MuzzleRotLimit, +MuzzleRotLimit);
		scl += Vector3.one*Random.Range(-MuzzleScaleLimit, +MuzzleScaleLimit);

		n_Muzzle.SetActive(true);
		m_PartsSettings.m_Muzzle.localRotation = Quaternion.Euler(rot);
		m_PartsSettings.m_Muzzle.localScale = scl;

		Audio.PlayOneShot(m_SndSettings.m_Fire);

		m_ShotEffectTime = m_WpnSettings.m_FireEffectTime;

		ProjectileManager.Instance.SpawnProjectile(m_WpnSettings.m_ProjType,
												   m_PartsSettings.m_Muzzle.position,
												   dir,
												   m_ProjInitSettings);
	}

	//-----------------------------------------------------------------------------------------------------------------
	//-----------------------------------------------------------------------------------------------------------------
	void OnDeactivation()
	{
		NetworkView.RPC("ClientExplode", uLink.RPCMode.Others);

		if (m_Deactivation.m_Explosion != null)
		{
			Vector3 pos = m_Deactivation.m_ExplosionOrigin != null ? m_Deactivation.m_ExplosionOrigin.position : m_PartsSettings.m_Base.position;
			Quaternion rot = m_Deactivation.m_ExplosionOrigin != null ? m_Deactivation.m_ExplosionOrigin.rotation : m_PartsSettings.m_Base.rotation;
			Explosion exp = Mission.Instance.ExplosionCache.Get(m_Deactivation.m_Explosion, pos, rot);
			exp.Agent = m_Owner;

			if (exp != null)
			{
				if (m_Deactivation.m_ExplosionDamage > 0.0f)
					exp.BaseDamage = m_Deactivation.m_ExplosionDamage;
				if (m_Deactivation.m_ExplosionRadius > 0.0f)
					exp.damageRadius = m_Deactivation.m_ExplosionRadius;
				exp.m_ItemID = ItemID;
			}
		}

		uLink.Network.Destroy(GameObject);
	}

	[uSuite.RPC]
	void ClientExplode()
	{
		if (m_Deactivation.m_Explosion != null)
		{
			Vector3 pos = m_Deactivation.m_ExplosionOrigin != null ? m_Deactivation.m_ExplosionOrigin.position : m_PartsSettings.m_Base.position;
			Quaternion rot = m_Deactivation.m_ExplosionOrigin != null ? m_Deactivation.m_ExplosionOrigin.rotation : m_PartsSettings.m_Base.rotation;
			Explosion exp = Mission.Instance.ExplosionCache.Get(m_Deactivation.m_Explosion, pos, rot);
			exp.Agent = m_Owner;

			if (exp != null)
			{
				if (m_Deactivation.m_ExplosionDamage > 0.0f)
					exp.BaseDamage = m_Deactivation.m_ExplosionDamage;
				if (m_Deactivation.m_ExplosionRadius > 0.0f)
					exp.damageRadius = m_Deactivation.m_ExplosionRadius;
				exp.m_ItemID = ItemID;
			}
		}
	}

	public override bool IsFriend(AgentHuman target)
	{
		if (m_Owner == null)
			return true;

		if (target == m_Owner)
			return true;

		return m_Owner.IsFriend(target);
	}

	public override void KnockDown(AgentHuman humanAttacker, E_MeleeType meleeType, Vector3 direction)
	{
		if (uLink.Network.isServer)
		{
			TakeDamage(humanAttacker, float.MaxValue, Vector3.zero, E_WeaponID.None, E_ItemID.None);
		}
	}

	#endregion
}

//#####################################################################################################################

class Rotator
{
	#region constants /////////////////////////////////////////////////////////////////////////////////////////////////

	const float ErrorTolerance = 0.5f*Mathf.Deg2Rad;

	#endregion

	#region fields ////////////////////////////////////////////////////////////////////////////////////////////////////

	// current / target angle (in radians)
	float m_Current;
	float m_Target;
	// limits of angle
	bool m_AngleLimited;
	float m_AngleLimitMin;
	float m_AngleLimitMax;
	// speed (in radians per second)
	float m_Speed;
	//
	bool m_Active;

	#endregion

	#region properties ////////////////////////////////////////////////////////////////////////////////////////////////

	public float Angle
	{
		get { return m_Current; }
	}

	public float AbsDiff
	{
		get { return Mathf.Abs(m_Target - m_Current); }
	}

	public float TargetAngle
	{
		set { SetTarget(value); }
	}

	public bool IsActive
	{
		get { return m_Active; }
	}

	#endregion

	#region methods ///////////////////////////////////////////////////////////////////////////////////////////////////

	//-----------------------------------------------------------------------------------------------------------------
	//-----------------------------------------------------------------------------------------------------------------
	public Rotator(float Angle, float Range, float Speed)
	{
		m_Current = MathUtils.SanitizeRadians(Angle);
		m_Target = m_Current;
		m_Active = false;
		m_Speed = Speed;

		if ((Range >= 0.0f) && (Range < 360.0f))
		{
			m_AngleLimited = true;
			m_AngleLimitMin = m_Current - Range*0.5f;
			m_AngleLimitMax = m_Current + Range*0.5f;
		}
		else
		{
			m_AngleLimited = false;
		}
	}

	//-----------------------------------------------------------------------------------------------------------------
	//-----------------------------------------------------------------------------------------------------------------
	public void Update(float DeltaTime)
	{
		if (m_Active)
		{
			float limit = m_Speed*DeltaTime;
			float diff = Mathf.Clamp(m_Target - m_Current, -limit, +limit);

			m_Current += diff;
			m_Active = Mathf.Abs(m_Target - m_Current) > ErrorTolerance;
		}
	}

	//-----------------------------------------------------------------------------------------------------------------
	//-----------------------------------------------------------------------------------------------------------------
	void SetTarget(float Angle)
	{
		if (m_AngleLimited)
		{
			Angle = Mathf.Clamp(Angle, m_AngleLimitMin, m_AngleLimitMax);
		}

		while (Angle - m_Current > Mathf.PI)
			Angle -= 2.0f*Mathf.PI;
		while (m_Current - Angle > Mathf.PI)
			Angle += 2.0f*Mathf.PI;

		m_Target = Angle;
		m_Active = Mathf.Abs(m_Target - m_Current) > ErrorTolerance;
	}

	#endregion
}

//#####################################################################################################################
