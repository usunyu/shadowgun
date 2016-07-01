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

// #define DEBUG

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("Weapons/ProjectileRocket")]
public class ProjectileRocket : Projectile
{
	public Explosion m_Explosion;
	public Vector3 m_ExplosionOffset = Vector3.zero;
//	private    Agent             m_Target;           // used for auto navigating
	public float m_AngularSpeed = 90;
	float m_Radius;

	// in case user fire an rocket into the sky (for example), projectile will explode after this time limit
	public float TimedExplosion = 5.0f;

	public ParticleSystem m_Trail;
	Vector3 HitNormal;

	// === OBJECT INTERFACE ======================

	// called from projectile manager when projectile is get from cache...
	public override void ProjectileInit(Vector3 pos, Vector3 dir, InitSettings inSettings)
	{
		//	string n = gameObject.name;
		//	string fulln = gameObject.GetFullName();

		Transform.position = pos;
		Transform.forward = dir;
		m_SpawnPosition = pos;
		Settings = inSettings;

		Timer = 0;
		Hit = false;
		//	m_FinishTime         = float.MaxValue;

		//	rigidbody.isKinematic      = false;
		GetComponent<Rigidbody>().detectCollisions = true;
		//	rigidbody.velocity         = dir*Settings.Speed;

		// radius of casted sphere during hits detection
		CapsuleCollider capsule = GetComponent<Rigidbody>().GetComponent<Collider>() as CapsuleCollider;
		m_Radius = (capsule != null) ? capsule.radius : 0.03f;

		// first meter is tested with line to prevent shooting through walls
		RaycastHit[] hits = Physics.RaycastAll(Transform.position, Transform.forward, 1.0f);

		ProceedHits(hits, true, Vector3.zero);
	}

	// called from projectile manager every tick...
	public override void ProjectileUpdate()
	{
		//	if (IsFinished())
		//		return;

		Timer += Time.deltaTime;

		if (Hit == false)
		{
			//	// update rotation by target pos...
			//	if(m_Target != null)
			//	{
			//		NavigateToTarget(m_Target.ChestPosition);
			//	}

			float dist = Settings.Speed*Time.deltaTime;
			Vector3 newPos = Transform.position + Transform.forward*dist;

			int mask = ~(ObjectLayerMask.PhysicBody | ObjectLayerMask.IgnoreRayCast);
			RaycastHit[] hits = Physics.SphereCastAll(Transform.position, m_Radius, Transform.forward, dist, mask);

			ProceedHits(hits, false, newPos);
		}
	}

	void ProceedHits(RaycastHit[] hits, bool softInit, Vector3 newPos)
	{
		// launched from cover (fast dirty solution)
		Cover cover = null;
		bool hitIsValid = false;

		if ((Agent != null) && (Agent.IsInCover == true) && (Agent.BlackBoard.Cover != null))
		{
			cover = Agent.BlackBoard.Cover;
		}

		// sort hits by distance
		if (hits.Length > 1)
		{
			System.Array.Sort(hits, CollisionUtils.CompareHits);
		}

		// process hits
		foreach (RaycastHit hit in hits)
		{
			//Debug.Log("ProjectileUpdate Hit" + hit.transform.name);
			if (hit.transform == Settings.IgnoreTransform)
				continue;

			//skip the Owner of this shot when his HitZone got hit
			HitZone zone = hit.transform.GetComponent<HitZone>();
			if (zone && (zone.HitZoneOwner is AgentHuman) && (zone.HitZoneOwner as AgentHuman).transform == Settings.IgnoreTransform)
				continue;

			//HACK The projectile belongs to the "Default" collision layer.
			//This is probably bug but we do not want to modify the data at the moment.
			//The only chance now is to ignore such hits
			if (hit.transform.gameObject.name.StartsWith("Projectile"))
				continue;

			//skip friends when the projectile should explode near the player (this solves the unwanted suicide when a friend suddenly enters the area in front of me)
			AgentHuman hitAgent = hit.transform.gameObject.GetFirstComponentUpward<AgentHuman>();
			if (Agent != null && hitAgent != null)
			{
				float dist = Vector3.Distance(Agent.Position, hitAgent.Position);
				if (dist < 3) //ignore only if the projectile is still within X m radius
				{
					if (Agent.IsFriend(hitAgent))
						continue;
				}
			}

			Transform.position = hit.point;

			if (hit.collider.isTrigger)
			{
				if (!softInit)
				{
					hit.transform.SendMessage("OnProjectileHit", this, SendMessageOptions.DontRequireReceiver);
					continue;
				}
			}

			if ((cover != null) && (cover.IsPartOfCover(hit.collider.gameObject) == true))
			{
				continue;
			}

			newPos = hit.point;

#if DEBUG
			DebugDraw.DepthTest = true;
			DebugDraw.DisplayTime = 10.0f;
			DebugDraw.Diamond(Color.red, 0.02f, newPos);
#endif

			Hit = true;
			HitNormal = hit.normal;
			hitIsValid = ValidateHit(hit);

			break;
		}

		if (false == softInit)
		{
			Transform.position = newPos;
		}

		if (Hit)
		{
			if (hitIsValid)
			{
				HitReaction();
			}
			else
			{
				//This function probably has no effect here but I want to be sure that I did not miss anything important
				//This code is triggered on the server side and only in the case when attacker is cheating
				InvalidHitReaction();
			}
		}
	}

	// called from projectile manager before return projectile back to cache...
	public override void ProjectileDeinit()
	{
		GetComponent<Rigidbody>().detectCollisions = true;
	}

	// Grenades are finished after axplosion. So we can test if they already exploded...
	public override bool IsFinished()
	{
		if (Hit == true && IsTrailVisible() == false)
		{
			return true;
		}

		if (TimedExplosion > 0 && Timer >= TimedExplosion)
		{
			DoTimedExplosion();
		}

		return false;
	}

	void DoTimedExplosion()
	{
		if (false == Hit)
		{
			Hit = true;
			HitReaction();
		}
	}

	public void OnCollisionEnter(Collision collision)
	{
		Hit = true;
		ContactPoint contact = collision.contacts[0];
		Transform.position = contact.point;

		HitReaction();
		//CombatEffectsManager.Instance.PlayHitEffect(contact.otherCollider.gameObject, contact.point, -contact.normal );
	}

	// === INTERNAl ==============================
	internal bool IsTrailVisible()
	{
		return (m_Trail && (m_Trail.isPlaying == true || m_Trail.particleCount > 0));
	}

	internal void HitReaction()
	{
		//Hit = true;
		//m_FinishTime = m_DelayedFinishAfterExplosion + Time.timeSinceLevelLoad;

		if (IsTrailVisible() == true)
		{
			m_Trail.Stop();
			DeactivateAllWithoutTrail();
		}

		SpawnExplosion();
	}

	void InvalidHitReaction()
	{
		if (IsTrailVisible() == true)
		{
			m_Trail.Stop();
			DeactivateAllWithoutTrail();
		}
	}

	internal void DeactivateAllWithoutTrail()
	{
		DeactivateGameObjects(gameObject, m_Trail.gameObject);
	}

	internal void DeactivateGameObjects(GameObject inGameObject, GameObject inIgnore)
	{
		if (inGameObject == inIgnore)
			return;

		inGameObject.SetActive(false);
		Transform trans = inGameObject.transform;
		foreach (Transform child in trans)
			DeactivateGameObjects(child.gameObject, inIgnore);
	}

	internal void NavigateToTarget(Vector3 inTargetPosition)
	{
		Vector3 dirToTarget = inTargetPosition - Transform.position;
		Quaternion targetRotation = new Quaternion();
		targetRotation.SetLookRotation(dirToTarget);
		Transform.rotation = Quaternion.RotateTowards(Transform.rotation, targetRotation, m_AngularSpeed*Time.deltaTime);
	}

	internal Quaternion RotateToward(Quaternion inFrom, Quaternion inTo, float inRotSpeed, float inTime)
	{
		float angle = Quaternion.Angle(inFrom, inTo);
		float rotationTime = angle/inRotSpeed;
		float t = rotationTime == 0 ? 0 : Mathf.Clamp(inTime/rotationTime, 0, 1);
		return Quaternion.Slerp(inFrom, inTo, t);
	}

	internal void SpawnExplosion()
	{
		if (m_Explosion != null)
		{
			//Debug.Log("ProjectileRocket.SpawnExplosion " + name);

			float dot = -Vector3.Dot(HitNormal, Transform.forward);
			float offNormal = 0.1f + 0.6f*(dot/2.0f);
			float offDir = 0.0f + 0.3f*(1.0f - dot);
			Vector3 pos = Transform.position + m_ExplosionOffset - offDir*Transform.forward + offNormal*HitNormal;

			//	Explosion explosion = Object.Instantiate(m_Explosion, transform.position, transform.rotation) as Explosion;
			//	Explosion explosion = Mission.Instance.ExplosionCache.Get(m_Explosion, Transform.position + m_ExplosionOffset, Transform.rotation);
			Explosion explosion = Mission.Instance.ExplosionCache.Get(m_Explosion, pos, Transform.rotation);

			if (null != explosion && Agent != null)
			{
				explosion.Agent = Agent;
				explosion.BaseDamage = Damage;
				explosion.m_WeaponID = WeaponID;
				explosion.m_WeaponImpulse = Settings.Impulse;

#if DEBUG
				DebugDraw.DepthTest = true;
				DebugDraw.DisplayTime = 10.0f;
				DebugDraw.Diamond(Color.yellow, 0.04f, explosion.Position);
#endif
			}
		}

		// destroy projectile ...
		// Destroy(gameObject); - !!! Don't do it. This object is cached now.
	}

	internal Agent FindBestTarget(float inMaxAngle)
	{
		if (Mission.Instance.GameZone == null)
			return null;

		return null;
	}
}
