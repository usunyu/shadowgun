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

//
public interface IHitZoneOwner
{
//	void OnProjectileHit( HitZone zone, Projectile projectile );
//	void OnRangeDamage( HitZone zone, Agent attacker, float damage, Vector3 impulse, E_WeaponID weaponID, E_WeaponType weaponType );

	void OnProjectileHit(Projectile inProjectile, HitZone inHitZone);
	void OnExplosionHit(Agent attacker, float inDamage, Vector3 impulse, E_WeaponID weaponId, E_ItemID itemId, HitZone inHitZone);
};

//
public class HitZone : MonoBehaviour
{
	public GameObject GameObj { get; private set; }
	public IHitZoneOwner HitZoneOwner { get; protected set; }
	public float DamageModifier = 1.0f;

	//
	void Awake()
	{
		GameObj = gameObject;

//		Collider collider = GameObj.GetComponent<Collider>();
//		if(collider == null)
//		{
//			Debug.LogError("Invalid HitZone [ " + DebugUtils.GetFullName(GameObj) + " ] . Some collider must exist on gameObject");
//		}

		InitHitZoneOwner();
	}

	//
	public virtual void Reset()
	{
	}

	//
	public void InitHitZoneOwner()
	{
		HitZoneOwner = GameObj.GetFirstComponentUpwardWithInterface<IHitZoneOwner>();
		if (HitZoneOwner == null)
		{
			//changed from error to log, currently we're using HitZones on object that will become attached to an IHitZoneOwner parent in runtime.
//			Debug.Log("Invalid HitZone [ " + DebugUtils.GetFullName(GameObj) + " ] . HIt Zone Owner must implement IHitZoneOwner interface ");
		}
	}

	//
	public virtual void OnProjectileHit(Projectile projectile)
	{
		//  Debug.Log("HitZone::OnProjectileHit " + name + " Damage = " + projectile.Damage);
		if (HitZoneOwner != null)
		{
//			HitZoneOwner.OnProjectileHit( this, projectile );
			HitZoneOwner.OnProjectileHit(projectile, this);
		}
	}

	public void OnExplosionHit(Explosion explosion)
	{
		if (HitZoneOwner != null)
		{
			HitZoneOwner.OnExplosionHit(explosion.Agent, explosion.Damage, explosion.Impulse, explosion.m_WeaponID, explosion.m_ItemID, this);
		}
	}

	/*
	public virtual void OnReceiveRangeDamage(Agent attacker, float damage, Vector3 impulse, E_WeaponID weaponId, E_ItemID itemId)
	{
	//  Debug.Log("HitZone::OnReceiveRangeDamage " + name + " Damage = " + damage);
		if (HitZoneOwner != null)
		{
//			HitZoneOwner.OnRangeDamage( this, attacker, damage, impulse, weaponID,weaponType );
			HitZoneOwner.OnExplosionHit( attacker, damage, impulse, weaponId, itemId, this );
		}
	}*/
}
