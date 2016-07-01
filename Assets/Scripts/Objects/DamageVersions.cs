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

[AddComponentMenu("Interaction/Damage Versions")]

// TODO: I was unable to find any reference to this class in both: the scenes and the code.
// What is this class good for? Is it still needed?
[RequireComponent(typeof (AudioSource))]
public class DamageVersions : GameZoneControledObject, IHitZoneOwner
{
	[System.Serializable]
	public class Version
	{
		public List<GameObject> GameObjects = new List<GameObject>();
		public AudioClip Audio;
		public ParticleSystem Effect;
		public float MaxHealth;

		public float Health { get; set; }
		public GameObject GameObject { get; set; }

		public void Activate()
		{
			Health = MaxHealth;
			GameObject = GameObjects[Random.Range(0, GameObjects.Count)];
			GameObject.SetActive(true);
		}

		public void DeActivate()
		{
			if (GameObject != null)
				GameObject.SetActive(false);

			GameObject = null;
		}

		public void Initialize()
		{
			foreach (GameObject o in GameObjects)
				o.SetActive(false);
		}
	}

	public List<Version> Versions = new List<Version>();
	int CurrentVersion;

	AudioSource Audio;
	float Health;

	protected override void Awake()
	{
		Audio = gameObject.GetComponent<AudioSource>();

		foreach (Version v in Versions)
			v.Initialize();
	}

	public override void Reset()
	{
		foreach (Version v in Versions)
			v.DeActivate();

		CurrentVersion = 0;
		Versions[CurrentVersion].Activate();
	}

	public void OnProjectileHit(Projectile p)
	{
	}

	public void OnProjectileHit(Projectile p, HitZone inHitZone)
	{
		if (CurrentVersion == Versions.Count - 1)
			return;

		DoDamage(p.Damage);
	}

	public void OnExplosionHit(Agent attacker, float inDamage, Vector3 impulse, E_WeaponID weaponId, E_ItemID itemId, HitZone inHitZone)
	{
		if (CurrentVersion == Versions.Count - 1)
			return;

		DoDamage(inDamage);
	}

	void DoDamage(float damage)
	{
		Version v = Versions[CurrentVersion];
		v.Health -= damage;

		if (v.Health <= 0)
		{
			if (v.Effect)
				v.Effect.Play();

			if (v.Audio)
				Audio.PlayOneShot(v.Audio);

			v.DeActivate();

			CurrentVersion++;
			Versions[CurrentVersion].Activate();
		}
	}
}
