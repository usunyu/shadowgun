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

[System.Obsolete]
public class ProjectileGrenadeSticky : Projectile
{
/*	public 		Explosion		m_Explosion;
	public 		float 			m_ExplodeAfter = 5.0f;

    public      int             m_PlayHitSoundCount = 3;
    private     int             m_PlayHitSound;

    /* AX :: See class decsription
	// Use this for initialization
	void Start () {
		
		Invoke("Explode", m_ExplodeAfter);
	}
	
	// Update is called once per frame
	void Update () {

	}
    


    // === OBJECT INTERFACE ======================

    // called from projectile manager when projectile is get from cache...
    public override void ProjectileInit(Vector3 pos, Vector3 dir, InitSettings inSettings)
    {
        Transform.position   = pos;
        Transform.forward    = dir;
        Settings             = inSettings;
        m_PlayHitSound       = m_PlayHitSoundCount;

        Invoke("Explode", m_ExplodeAfter);

        //print("ProjectileGrenade.m_Speed = " + m_Speed);
        rigidbody.velocity = transform.forward * Speed;  //Random.insideUnitSphere * 5;
        rigidbody.SetDensity(1.5F);
        //instance.rigidbody.centerOfMass = new Vector3(0, 0.0f, 0);
        //instance.rigidbody.AddTorque(Vector3.up * 10, ForceMode.VelocityChange);
        rigidbody.AddTorque(Random.insideUnitSphere * 10, ForceMode.VelocityChange);
    }

    // called from projectile manager every tick...
    public override void ProjectileUpdate()
    {
    }

    // called from projectile manager before return projectile back to cache...
    public override void ProjectileDeinit()
    {
        Transform.parent = null;
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

		if (m_Explosion != null) {
			//Explosion explosion = Object.Instantiate(m_Explosion, Transform.position, Transform.rotation) as Explosion;
            Explosion explosion = Mission.Instance.ExplosionCache.Get(m_Explosion, Transform.position, Transform.rotation);
            explosion.causer    = Agent;
            explosion.damage    = Damage;
			explosion.m_WeaponImpulse	= Settings.Impulse;
		}

        // destroy projectile ...
		// Destroy(gameObject); - !!! Don't do it. This object is cached now.
	}

    internal void OnCollisionEnter(Collision collision)
    {
        // ignore collisions with owner and his transforms...
        if (Agent && collision.transform.IsChildOf(Agent.Transform))
            return;

        Agent agent = collision.gameObject.GetComponent<Agent>();
        if(agent && agent.IsPlayer == true && (IsInvoking("Explode") == true))
        {
            Transform.parent = agent.transform;
        }
        else if(m_PlayHitSound > 0)
        {
            m_PlayHitSound--;
            PlayHitSound(collider.gameObject.layer);
        }
    }
 * */
}
