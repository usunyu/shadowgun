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
using System.Collections.Generic;

[AddComponentMenu("Weapons/TestProjectiles")]
public class TestProjectiles : MonoBehaviour
{
//	[System.Serializable]
//	public class ProjectileSettings
//	{
//		public		E_WeaponType 	m_Type 					= E_WeaponType.GrenadeLauncher;
//		public 		float			m_Speed 				= 5;
//		public 		float 			m_Damage 				= 5;
//	};

	public GameObject m_ProjectilePrefab;
	public E_ProjectileType m_ProjectileType = E_ProjectileType.None;
	public bool m_UseProjectileType = true;

	public GameObject m_SpawnPos;
//	public 		ProjectileSettings 	                m_ProjectileSettings;
	public Projectile.InitSettingsInspector m_ProjectileSettingsEx;

	public float m_LaunchRepeatTime = 2.0f;

	// list of projectiles in air
	List<Projectile> ActiveProjectiles = new List<Projectile>();

	void LaunchProjectile()
	{
		//print("LaunchProjectile");

		Projectile.InitSettings projSettings = new Projectile.InitSettings(m_ProjectileSettingsEx);
		projSettings.IgnoreTransform = transform;

		if (m_UseProjectileType)
		{
			/* Use default Projectile manager, This is using before finel commit. We are testing if projectile manager is properly initialized.
             * Chnage first argument for how you want and test it...
             */
			ProjectileManager.Instance.SpawnProjectile(m_ProjectileType, m_SpawnPos.transform.position, m_SpawnPos.transform.forward, projSettings);
		}
		else
		{
			GameObject instance = Instantiate(m_ProjectilePrefab, m_SpawnPos.transform.position, m_SpawnPos.transform.rotation) as GameObject;
			Projectile proj = instance.GetComponent<Projectile>();
			proj.ProjectileInit(m_SpawnPos.transform.position, m_SpawnPos.transform.forward.normalized, projSettings);
			ActiveProjectiles.Add(proj);
		}

		/*  //This was used for grenade tuning (old code)...
		GameObject instance = Instantiate(m_ProjectilePrefab, m_SpawnPos.transform.position, m_SpawnPos.transform.rotation) as GameObject;
		instance.rigidbody.velocity = transform.forward * m_ProjectileSettings.m_Speed;  //Random.insideUnitSphere * 5;
		instance.rigidbody.SetDensity(1.5F);
		//instance.rigidbody.centerOfMass = new Vector3(0, 0.0f, 0);
		//instance.rigidbody.AddTorque(Vector3.up * 10, ForceMode.VelocityChange);
		instance.rigidbody.AddTorque(Random.insideUnitSphere * 10, ForceMode.VelocityChange);
		instance.SetActiveRecursively(true);
		*/
	}

	// Use this for initialization
	void Awake()
	{
		InvokeRepeating("LaunchProjectile", m_LaunchRepeatTime, m_LaunchRepeatTime);
	}

	// Update is called once per frame
	void Update()
	{
		// if we are not in regular game don't update projectiles...
		if (Game.Instance.GameState != E_GameState.Game)
			return;

		// if game is paused don't update projectiles...
		if (Time.deltaTime <= 0.0f)
			return;

		// Update all projectiles in air...
		foreach (Projectile proj in ActiveProjectiles)
		{
			if (proj.IsFinished() == true)
				continue;

			proj.ProjectileUpdate();
		}
	}

	void FixedUpdate()
	{
		for (int i = 0; i < ActiveProjectiles.Count; i++)
		{
			if (ActiveProjectiles[i].IsFinished() == false)
				continue;

			ActiveProjectiles[i].ProjectileDeinit();
			DestroyObject(ActiveProjectiles[i], 0.1f);
			ActiveProjectiles.RemoveAt(i--);
		}
	}
}
