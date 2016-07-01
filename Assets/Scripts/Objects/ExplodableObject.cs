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

public class ExplodableObject : GameZoneControledObject, IHitZoneOwner
{
	float m_DefaultHitPoints;
	public float m_HitPoints = 100.0f;

	public Explosion m_Explosion;
	public Transform m_ExplosionOrigin;
	public float m_ExplosionDamage = -1;
	public GameObject[] m_HideObjects;
	public GameObject[] m_ShowObjects;

	GameObject m_GameObject;

	protected override void Awake()
	{
		base.Awake();

		m_GameObject = gameObject;
		m_DefaultHitPoints = m_HitPoints;

		m_ExplosionOrigin = m_ExplosionOrigin != null ? m_ExplosionOrigin : transform;
	}

	void Start()
	{
		m_HitPoints = m_DefaultHitPoints;

		m_GameObject.SetActive(true);
		foreach (GameObject go in m_ShowObjects)
			go.SetActive(false);

		if (uLink.Network.isServer == false)
		{
			//This bool is a safe workaround for an exception which is thrown when the scene is launced directly in the 
			//editor when no client connection is established
			bool sendIt = true;

#if UNITY_EDITOR
			sendIt = uLink.Network.isClient;
#endif

			if (sendIt)
				NetworkView.RPC("Sync", uLink.RPCMode.Server);
		}
	}

	void OnDestroy()
	{
		m_Explosion = null;
	}

	public void OnProjectileHit(Projectile inProjectile, HitZone inHitZone)
	{
		if (uLink.Network.isServer == false)
			return;

		if (m_HitPoints <= 0)
			return;

		m_HitPoints -= inProjectile.Damage*inHitZone.DamageModifier;
		if (m_HitPoints <= 0)
			Break(inProjectile.Agent, inProjectile.WeaponID, E_ItemID.None);
	}

	public void OnExplosionHit(Explosion explosion)
	{
		OnExplosionHit(explosion.Agent, explosion.Damage, explosion.Impulse, explosion.m_WeaponID, explosion.m_ItemID, null);
	}

	public void OnExplosionHit(Agent attacker, float inDamage, Vector3 impulse, E_WeaponID weaponId, E_ItemID itemId, HitZone inHitZone)
	{
		if (uLink.Network.isServer == false)
			return;

		if (m_HitPoints <= 0)
			return;

		m_HitPoints -= inDamage*inHitZone.DamageModifier;
		if (m_HitPoints <= 0)
			Break(attacker as AgentHuman, weaponId, itemId);
	}

	[uSuite.RPC]
	public override void Reset()
	{
		m_HitPoints = m_DefaultHitPoints;

		m_GameObject.SetActive(true);
		foreach (GameObject go in m_ShowObjects)
			go.SetActive(false);

		if (uLink.Network.isServer)
			NetworkView.RPC("Reset", uLink.RPCMode.Others);
	}

	protected virtual void Break(AgentHuman attacker, E_WeaponID weaponID, E_ItemID itemID)
	{
		if (m_Explosion != null)
		{
			Explosion explosion = Mission.Instance.ExplosionCache.Get(m_Explosion, m_ExplosionOrigin.position, m_ExplosionOrigin.rotation);
			explosion.Agent = attacker;
			if (explosion != true && m_ExplosionDamage >= 0)
			{
				explosion.BaseDamage = m_ExplosionDamage;
			}

			explosion.m_WeaponID = weaponID;
			explosion.m_ItemID = itemID;
		}

		foreach (GameObject go in m_HideObjects)
			go.SetActive(false);
		foreach (GameObject go in m_ShowObjects)
			go.SetActive(true);

		if (uLink.Network.isServer)
			NetworkView.RPC("BreakOnClients", uLink.RPCMode.Others);
	}

	[uSuite.RPC]
	protected virtual void BreakOnClients()
	{
		if (m_Explosion != null)
		{
			Explosion explosion = Mission.Instance.ExplosionCache.Get(m_Explosion, m_ExplosionOrigin.position, m_ExplosionOrigin.rotation);
			if (explosion != true && m_ExplosionDamage >= 0)
			{
				explosion.BaseDamage = m_ExplosionDamage;
			}
		}

		foreach (GameObject go in m_HideObjects)
			go.SetActive(false);
		foreach (GameObject go in m_ShowObjects)
			go.SetActive(true);
	}

	[uSuite.RPC]
	protected virtual void Sync(uLink.NetworkMessageInfo info)
	{
		if (m_HitPoints <= 0)
			NetworkView.RPC("InitBreakOnClient", info.sender);
	}

	[uSuite.RPC]
	protected virtual void InitBreakOnClient()
	{
		foreach (GameObject go in m_HideObjects)
			go.SetActive(false);
		foreach (GameObject go in m_ShowObjects)
			go.SetActive(true);
	}
}
