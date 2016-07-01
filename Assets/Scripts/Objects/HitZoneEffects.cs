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
// NOTE: This code was taken from Dead Trigger. Properties were modified to not allow limbs dismemberment by their default states since this feature is not desired for SG: DeadZone.
//

//
public class HitZoneEffects : HitZone
{
	//effects
	public bool MustDieToDestroy = true; //if true, the Destroy effects are played only if the agent dies by the current hit
	public float DestroyCumulativePercentage = 1.0f;
				 //when the CumulativeDamage reaches this percentage <0;1> of the total agent Health, the associated body part is destroyed
	public float DestroyBashPercentage = 1.0f;
				 //when the actual damage reaches this percentage <0;1> of the total agent Health, the associated body part is destroyed

	public ParticleSystem DestroyParticle; //particle which is played when the associated limb is decapitated (totally destroyed)

	//
	float _cumulativeDamage = 0; //this is collecting the damage that this HitZone has received

	public float CumulativeDamage
	{
		get { return _cumulativeDamage; }
	}

	//
	void Start()
	{
		_cumulativeDamage = 0;
	}

	//
	public override void Reset()
	{
		base.Reset();

		_cumulativeDamage = 0;
	}

	//call OnHitZoneProjectileHit() passing in HitZoneEffects
	public override void OnProjectileHit(Projectile projectile)
	{
		//we're modifying the cumulativeDamage by BodyPartDamageModif and DamageModifier, because BodyPartDamageModif now drives the effect probability per Weapon. 
		//Health is NOT affected by the BodyPartDamageModif
		bool invulnerable = false;
//		float			damageModifier	= (projectile.ProjectileType != E_ProjectileType.Melee) ? DamageModifier : 1;	//do not use DamageModifiers for Melee weapons
//		ComponentEnemy	enemy 			= HitZoneOwner != null ? HitZoneOwner as ComponentEnemy : null;

		//is the enemy invulnerable?
//		if (enemy != null && enemy.Owner && enemy.Owner.IsInvulnerable)
//			invulnerable = true;

		if (!invulnerable)
			_cumulativeDamage += projectile.Damage*DamageModifier;

		//this can happen with objects that are not directly linked to agents, but are instantiated and attached to agents in runtime (e.g. hats)
		if (HitZoneOwner == null)
			InitHitZoneOwner();

		if (HitZoneOwner != null)
			HitZoneOwner.OnProjectileHit(projectile, this);
	}

	//call OnReceiveRangeDamage() passing in HitZoneEffects
	/*public override void OnReceiveRangeDamage(Agent attacker, float damage, Vector3 impulse, E_WeaponID weaponId, E_ItemID itemId)
	{
//		bool			invulnerable	= false;
//		ComponentEnemy	enemy 			= HitZoneOwner != null ? HitZoneOwner as ComponentEnemy : null;
		
		//is the enemy invulnerable?
//		if (enemy != null && enemy.Owner && enemy.Owner.IsInvulnerable)
//			invulnerable = true;
		
//		if (!invulnerable)
//			_cumulativeDamage += damage * DamageModifier;
		
		//call this extended one
		if (HitZoneOwner != null)
		{
			//HACK: agents were receiving damage from single explosion by every HitZone (i.e. grenade causing 100 hp damage was applied e.g. 6x). So now only Body will get it!
//			if ( enemy )
//			{
//				if ( enemy.GetBodyPart(this) == E_BodyPart.Body )
//					HitZoneOwner.OnRangeDamage( this, attacker, damage, impulse, weaponID, weaponType );
//			}
//			else
			{
//				HitZoneOwner.OnRangeDamage( this, attacker, damage, impulse, weaponID, weaponType );
				HitZoneOwner.OnExplosionHit( attacker, damage, impulse, weaponId, itemId, this );
			}
		}
	}*/
}
