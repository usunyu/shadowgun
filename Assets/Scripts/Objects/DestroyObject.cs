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

[AddComponentMenu("Interaction/Destroyable Object")]
public class DestroyObject : MonoBehaviour
{
	public float Health;
	float DefaultHealth; // use for reseting
	public ParticleSystem Emitter;
	public AudioSource Sound;

	protected bool Active = true;
	GameObject GameObject;

	public bool IsActive
	{
		get { return Active; }
	}

	void Start()
	{
		GameObject = gameObject;
		DefaultHealth = Health;
	}

	void OnProjectileHit(Projectile projectile)
	{
		if (Active == false)
			return;

		Health -= projectile.Damage;
		if (Health < 0)
			Break();
	}

	public void OnExplosionHit(Agent attacker, float damage, Vector3 impuls, E_WeaponID weaponType)
	{
		if (Active == false)
			return;

		Health -= damage;
		if (Health < 0)
			Break();
	}

	public virtual void Break()
	{
		//Debug.Log(Time.timeSinceLevelLoad + " " + gameObject.name + " break");
		Active = false;

		if (Emitter)
			Emitter.Play();

		if (Sound)
			Sound.Play();

		GameObject.GetComponent<Renderer>().enabled = false;
		GameObject.GetComponent<Collider>().enabled = false;
	}

	public virtual void Reset()
	{
		Health = DefaultHealth;
		Active = true;

		GameObject.GetComponent<Renderer>().enabled = true;
		GameObject.GetComponent<Collider>().enabled = true;
	}

	public void Enable()
	{
		GameObject.SetActive(true);
		//Debug.Log(Time.timeSinceLevelLoad + " " + gameObject.name + " Enable");
	}

	public void Disable()
	{
		GameObject.SetActive(false);
		//Debug.Log(Time.timeSinceLevelLoad + " " + gameObject.name + " Disable");
	}
}
