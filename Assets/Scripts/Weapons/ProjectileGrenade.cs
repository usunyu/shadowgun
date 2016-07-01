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

// ProjectileGrenade.
// DSC  :: This class is cached and managed from ProjectileManager
//         We are not using standard MonoBehaviour functions like Start, Update, and so on,
//         because we need manage this function in one place and in one time.
// NOTE :: Never call destroy on this object, this object is CACHED !!!
[AddComponentMenu("Weapons/ProjectileGrenade")]
public class ProjectileGrenade : Projectile
{
	public Explosion m_Explosion;
	public Vector3 m_ExplosionOffset = Vector3.zero;
	public float m_ExplodeAfter = 5.0f;

	public int m_PlayHitSoundCount = 3;
	int m_PlayHitSound;

	Collider m_WaterVolume;

	/* AX :: See class decsription
	// Use this for initialization
	void Start () {
		
		Invoke("Explode", m_ExplodeAfter);
	}
	
	// Update is called once per frame
	void Update () {

	}
    */

	void OnDestroy()
	{
		m_Explosion = null;
	}

	// === OBJECT INTERFACE ======================

	// called from projectile manager when projectile is get from cache...
	public override void ProjectileInit(Vector3 pos, Vector3 dir, InitSettings inSettings)
	{
		Transform.position = pos;
		Transform.forward = dir;
		Settings = inSettings;
		m_SpawnPosition = pos;
		m_PlayHitSound = m_PlayHitSoundCount;

		Invoke("Explode", m_ExplodeAfter);

		//print("ProjectileGrenade.m_Speed = " + m_Speed);
		GetComponent<Rigidbody>().velocity = transform.forward*Speed; //Random.insideUnitSphere * 5;
		GetComponent<Rigidbody>().SetDensity(1.5F);
		GetComponent<Rigidbody>().WakeUp();
		//instance.rigidbody.centerOfMass = new Vector3(0, 0.0f, 0);
		//instance.rigidbody.AddTorque(Vector3.up * 10, ForceMode.VelocityChange);
		//rigidbody.AddTorque(Random.insideUnitSphere * 10, ForceMode.VelocityChange);
	}

	// called from projectile manager every tick...
	public override void ProjectileUpdate()
	{
	}

	// called from projectile manager before return projectile back to cache...
	public override void ProjectileDeinit()
	{
		m_WaterVolume = null;
		CancelInvoke();
		GetComponent<Rigidbody>().Sleep();
	}

	// Grenades are finished after axplosion. So we can test if they already exploded...
	public override bool IsFinished()
	{
		return (IsInvoking("Explode") == false);
	}

	// === INTERNAl ==============================

	internal void Explode()
	{
		CancelInvoke("Explode");

		if (m_WaterVolume != null)
		{
			Explosion explosion = Mission.Instance.ExplosionCache.GetWaterExplosion(Transform.position + m_ExplosionOffset, Transform.rotation);
			if (explosion != null)
			{
				explosion.Agent = Agent;
				explosion.BaseDamage = Damage;
			}
		}
		else if (m_Explosion != null)
		{
			//Explosion explosion = Object.Instantiate(m_Explosion, Transform.position, Transform.rotation) as Explosion;
			Explosion explosion = Mission.Instance.ExplosionCache.Get(m_Explosion,
																	  Transform.position + m_ExplosionOffset,
																	  Transform.rotation,
																	  new Transform[] {transform, Settings.IgnoreTransform});
			explosion.Agent = Agent;
			explosion.BaseDamage = Damage;
			explosion.m_WeaponImpulse = Settings.Impulse;

			if (null == Agent)
			{
				Debug.LogWarning("### ProjectileGrenade.Explode() : unexpected null agent. WeaponID : " + WeaponID);
			}
		}

		// destroy projectile ...
		// Destroy(gameObject); - !!! Don't do it. This object is cached now.
	}

	internal void OnTriggerEnter(Collider other)
	{
		//Debug.Log(name + " OnCollisionEnter " + other.name);
#if UNITY_MFG && UNITY_EDITOR //we don't have FluidSurface in WebPlayer
        FluidSurface fluid = other.GetComponent<FluidSurface>();
        if(fluid != null)
        {
            m_WaterVolume = other;
            fluid.AddDropletAtWorldPos(Transform.position, 0.5f, +0.5f);
            PlayHitSound(other.gameObject.layer);
            CombatEffectsManager.Instance.PlayHitEffect(other.gameObject, Transform.position, Vector3.up, ProjectileType, Agent != null && Agent.IsOwner);
        }
#endif
	}

	internal void OnTriggerExit(Collider other)
	{
		//Debug.Log(name + " OnTriggerExit " + other.name);
		if (other != null && m_WaterVolume == other)
		{
			m_WaterVolume = null;
		}
	}

	internal void OnCollisionEnter(Collision collision)
	{
		//Debug.Log(name + " OnCollisionEnter " + collision.gameObject.name);

		if (IsInvoking("Explode") == false)
			return;

		// ignore collisions with owner and his transforms...
		if (Agent && collision.transform.IsChildOf(Agent.Transform))
			return;

		Agent agent = collision.gameObject.GetComponent<Agent>();
		HitZone hz = collision.gameObject.GetComponent<HitZone>();
		AgentHuman human = agent as AgentHuman;

		if (human && human.BlackBoard.GrenadesExplodeOnHit == false)
		{
			if (m_PlayHitSound > 0)
			{
				m_PlayHitSound--;
				PlayHitSound(GetComponent<Collider>().gameObject.layer);
			}
		}
		else if (agent)
		{
			// grenade must explode immediately, if we hit player
			Explode();
		}
		else if (hz)
		{
			// grenade must explode immediately, if we hit player
			Explode();
		}
		else if (m_WaterVolume == null && m_PlayHitSound > 0)
		{
			m_PlayHitSound--;
			PlayHitSound(GetComponent<Collider>().gameObject.layer);
		}
	}
}
