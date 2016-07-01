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

//#define DEBUG
//#define TEGRA_3_BUILD

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("Weapons/Projectile")]
public class Projectile : MonoBehaviour
{
	[System.Serializable]
	public class InitSettingsInspector
	{
		public float Damage = 0;
		public float Impulse = 1;
		public float Speed = 10;
	};

	[System.Serializable]
	public class InitSettings
	{
		//	public  E_ProjectileType	Type			= E_ProjectileType.None;
		public float Damage = 0;
		public float Impulse = 1;
		public float Speed = 10;
		public AgentHuman Agent = null;
		public E_WeaponID WeaponID = E_WeaponID.None;
		public E_ItemID ItemID = E_ItemID.None;
		public bool Homing = false;
		public bool Silencer = false;
		public Transform IgnoreTransform = null;

		// default constructor. Properties will be initialized as you see above
		public InitSettings()
		{
		}

		// Copy constructor. Create clone of inSettings object.
		public InitSettings(InitSettings inSettings)
		{
			Damage = inSettings.Damage;
			Impulse = inSettings.Impulse;
			Speed = inSettings.Speed;
			Agent = inSettings.Agent;
			WeaponID = inSettings.WeaponID;
			ItemID = inSettings.ItemID;
			Homing = inSettings.Homing;
			Silencer = inSettings.Silencer;

			IgnoreTransform = inSettings.IgnoreTransform;

			if (null == Agent)
			{
				Debug.LogWarning("### Projectile.InitSettings() : unexpected null agent. WeaponID : " + WeaponID + ", ItemID : " + ItemID);
			}
		}

		// Constructor. Initialize class from inspector object.
		public InitSettings(InitSettingsInspector inSettings)
		{
			Damage = inSettings.Damage;
			Impulse = inSettings.Impulse;
			Speed = inSettings.Speed;
		}
	};

	public E_ProjectileType ProjectileType { get; set; } // hide it in inspector.
	public ProjectileTrail m_ProjectileTrail;

	public GameObject GameObject { get; private set; }
	public Transform Transform { get; private set; }
	protected InitSettings Settings; //	{ get; private set; }

	public Vector3 Pos
	{
		get { return Transform.position; }
	}

	public Vector3 SpawnPos
	{
		get { return m_SpawnPosition; }
	}

	public Vector3 Dir
	{
		get { return m_Forward; }
	}

	public float Damage
	{
		get { return Settings.Damage; }
	}

	public float Speed
	{
		get { return Settings.Speed; }
	}

	public float Impulse
	{
		get { return Settings.Impulse; }
	}

	public AgentHuman Agent
	{
		get { return Settings.Agent; }
	}

	public E_WeaponID WeaponID
	{
		get { return Settings.WeaponID; }
	}

	public E_ItemID ItemID
	{
		get { return Settings.ItemID; }
	}

	public bool Homing
	{
		get { return Settings.Homing; }
	}

	public bool Silencer
	{
		get { return Settings.Silencer; }
	}

	public bool spawnHitEffects { get; set; }
	public bool ignoreThisHit { get; set; }
	public bool ricochetThisHit { get; set; }

	bool m_ScaleProjectile = true;

	protected float Timer;
	protected bool Hit;
	protected bool UseTrail;

	protected Vector3 m_Forward;
	protected Vector3 m_SpawnPosition;

	protected static int RayCastMask;

	Renderer Renderer;

	public void Awake()
	{
		//The ObjectLayerMask static members must not be called from a static ctor as it was before
		//because it causes a weird exception in Unity 4.5 version:
		//ArgumentException: NameToLayer can only be called from the main thread
		if (uLink.Network.isServer)
		{
			// human-agent : hit into character-controller --> sampling dominant anim --> hit-detection with rag-doll ( done in 'AgentHuman.OnProjectileHit' )
			RayCastMask = ~(ObjectLayerMask.IgnoreRayCast | ObjectLayerMask.Ragdoll | ObjectLayerMask.Hat);
		}
		else
		{
			// human-agent : hit directly into rag-doll
			RayCastMask = ~(ObjectLayerMask.IgnoreRayCast | ObjectLayerMask.PhysicBody);
		}

		GameObject = gameObject;
		Transform = transform;
	}

	public void Start()
	{
		Renderer = GetComponent<Renderer>();
	}

	public virtual void ProjectileInit(Vector3 pos, Vector3 dir, InitSettings inSettings)
	{
		Settings = inSettings;
		Transform.position = pos;
		Transform.forward = dir;
		m_Forward = dir;
		m_SpawnPosition = pos;

		//Debug.Log(Time.timeSinceLevelLoad  + " Projetile Init " + pos + " " + dir);

		if (m_ScaleProjectile == true)
		{
			Transform.localScale = new Vector3(1, 1, 0);
		}

		if (m_ProjectileTrail != null)
		{
			if (Silencer)
				UseTrail = false;
			else if (DeviceInfo.PerformanceGrade == DeviceInfo.Performance.Low)
				UseTrail = UnityEngine.Random.value < 0.5f;
			else
				UseTrail = true;

			if (UseTrail)
			{
				m_ProjectileTrail.InitTrail(pos);
			}
		}

		if (Renderer != null)
			Renderer.enabled = true;

		Timer = 0;
		Hit = false;

		spawnHitEffects = true;
		ignoreThisHit = false;
		ricochetThisHit = false;

#if DEBUG

		DebugDraw.DepthTest = true;
		DebugDraw.DisplayTime = 10.0f;
		DebugDraw.Line(Color.white, pos, pos + dir*30.0f);

#endif
	}

	protected bool ValidateHitAgainstEnemy(RaycastHit hit)
	{
#if !DEADZONE_CLIENT
		if (uLink.Network.isServer)
		{
			AgentHuman other = hit.transform.gameObject.GetComponent<AgentHuman>();
			if ((other != null) && !other.IsFriend(Agent))
			{
				return ServerAnticheat.ValidateHit(Agent, this, hit);
			}
		}
#endif

		return true;
	}

	protected bool ValidateHit(RaycastHit hit)
	{
#if !DEADZONE_CLIENT
		if (uLink.Network.isServer)
			return ServerAnticheat.ValidateHit(Agent, this, hit);
#endif

		return true;
	}

	public virtual void ProjectileUpdate()
	{
		MFDebugUtils.Assert(IsFinished() == false);

		Timer += Time.deltaTime;

		if (Hit == true)
			return;

		if (Transform.localScale.z != 1)
			Transform.localScale = new Vector3(1, 1, Mathf.Min(1, Transform.localScale.z + Time.deltaTime*8));

		float dist = Settings.Speed*Time.deltaTime;
		Vector3 newPos = Pos + Dir*dist;
		RaycastHit[] hits = Physics.RaycastAll(Pos, Dir, dist, RayCastMask);

		if (hits.Length > 1)
			System.Array.Sort(hits, CollisionUtils.CompareHits);

		foreach (RaycastHit hit in hits)
		{
			//Debug.Log (Time.timeSinceLevelLoad + "Test: " + hit.transform.name);

			if (hit.transform == Settings.IgnoreTransform)
				continue;

			//skip the Owner of this shot when his HitZone got hit
			HitZone zone = hit.transform.GetComponent<HitZone>();
			if (zone && (zone.HitZoneOwner is AgentHuman) && (zone.HitZoneOwner as AgentHuman).transform == Settings.IgnoreTransform)
				continue;

			//Debug.Log (Time.timeSinceLevelLoad + "HIT: " + hit.transform.name);
//			Debug.DrawLine(Transform.position, Transform.position + hit.normal, Color.blue, 4.0f);
//			Debug.DrawLine(Transform.position, hit.point, Color.red, 3.0f);

			Transform.position = hit.point;

			//HACK The projectile belongs to the "Default" collision layer.
			//This is probably bug but we do not want to modify the data at the moment.
			//The only chance now is to ignore such hits
			if (hit.transform.gameObject.name.StartsWith("Projectile"))
				continue;

			if (!ValidateHitAgainstEnemy(hit))
				continue;

			hit.transform.SendMessage("OnProjectileHit", this, SendMessageOptions.DontRequireReceiver);

			if (ignoreThisHit)
			{
				ignoreThisHit = false;
				continue;
			}

			if (uLink.Network.isClient)
			{
#if UNITY_MFG && UNITY_EDITOR //we don't have FluidSurface in WebPlayer
				FluidSurface fluid = hit.collider.GetComponent<FluidSurface>();
				if (fluid != null)
				{
					fluid.AddDropletAtWorldPos(hit.point,0.3f,0.15f);
				}
#endif
				//	else if (hit.rigidbody)
				//	{
				//		// TODO: potrebujem poladit korektni hodnotu impulzu per-zbran
				//		float force = Vector3.Dot(-hit.normal,Transform.forward) * 30; 
				//		
				//		hit.rigidbody.AddForceAtPosition(Transform.forward * force,hit.point);
				//	}
			}

			if (hit.collider.isTrigger)
				continue;

			newPos = hit.point;

			if (ricochetThisHit)
			{
				ricochetThisHit = false;

				Transform.forward = hit.normal;
				m_Forward = hit.normal;

				if (spawnHitEffects)
				{
					PlayHitSound(29);
					CombatEffectsManager.Instance.PlayHitEffect(hit.transform.gameObject,
																29,
																hit.point,
																hit.normal,
																ProjectileType,
																Agent != null && Agent.IsOwner);
				}

				if ((m_ProjectileTrail != null) && UseTrail)
				{
					m_ProjectileTrail.AddTrailPos(newPos);
				}
			}
			else
			{
				Hit = true;

				if (spawnHitEffects)
				{
					PlayHitSound(hit.transform.gameObject.layer);
					CombatEffectsManager.Instance.PlayHitEffect(hit.transform.gameObject,
																hit.transform.gameObject.layer,
																hit.point,
																hit.normal,
																ProjectileType,
																Agent != null && Agent.IsOwner);
				}
			}

			if (GetComponent<Renderer>() != null)
			{
				GetComponent<Renderer>().enabled = false;
			}

			break;
		}

		Transform.position = newPos;

		if ((m_ProjectileTrail != null) && (UseTrail == true))
		{
			m_ProjectileTrail.UpdateTrailPos(newPos);
		}
	}

	// called from projectile manager before return projectile back to cache...
	public virtual void ProjectileDeinit()
	{
	}

	public virtual bool IsFinished()
	{
		if (UseTrail)
		{
			return (Timer > 1 || m_ProjectileTrail.IsVisible() == false);
		}
		else
		{
			return (Timer > 1 || Hit == true);
		}
	}

	protected void PlayHitSound(int layer)
	{
		if (uLink.Network.isClient)
			ProjectileManager.Instance.PlayHitSound(layer, Transform.position, ProjectileType);
	}
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////

[System.Serializable]
class ProjectileCacheEx : ResourceCache
{
	E_ProjectileType m_ProjectileType;

	public ProjectileCacheEx(String inName, E_ProjectileType inProjectileType, int inInitialCacheSize)
					: base(inName, inInitialCacheSize)
	{
		m_ProjectileType = inProjectileType;
	}
	
	public new Projectile Get()
	{
		GameObject go = base.Get();
		go.transform.position = new Vector3(0, 0, 10000);
		go.SetActive(true);


		Projectile proj = go.GetComponent<Projectile>();
		proj.ProjectileType = m_ProjectileType;
		return proj;
	}

	public void Return(Projectile projectile)
	{
		projectile.gameObject.SetActive(false);
		base.Return(projectile.gameObject);
	}
}
