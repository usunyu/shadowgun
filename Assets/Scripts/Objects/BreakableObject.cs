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

[AddComponentMenu("Interaction/Break Object With Anim")]
public class BreakableObject : MonoBehaviour
{
	[System.Serializable]
	public class InteractionParticle
	{
		public ParticleSystem Emitter;
		public float Delay;
		public bool LinkOnRoot;
	}

	[System.Serializable]
	public class InteractionSound
	{
		public AudioSource Audio;
		public float Delay;
		public float Life;
		public Transform Parent;
	}

	public float Health;
	public AnimationClip AnimBreak;
	public InteractionParticle[] Emitters;
	public InteractionSound[] Sounds;

	protected bool Active = true;
	protected Transform Root;
	Animation Animation;
	GameObject GameObject;

	public bool IsActive
	{
		get { return Active; }
	}

	public Vector3 Position
	{
		get { return Root.position; }
	}

	public void Initialize()
	{
		GameObject = gameObject;
		Root = transform;
		Animation = GameObject.GetComponent<Animation>();

		if (Animation)
			Animation.wrapMode = WrapMode.Once;

		// Debug.Log(Time.timeSinceLevelLoad + " " + gameObject.name + " Initialize");
	}

	void OnProjectileHit(Projectile projectile)
	{
		if (Active == false)
			return;

		Health -= projectile.Damage;
		if (Health < 0)
			Break();
	}

	public virtual void Break()
	{
		//Debug.Log(Time.timeSinceLevelLoad + " " + gameObject.name + " break");
		Active = false;

		if (Animation && AnimBreak)
			Animation.Play(AnimBreak.name);

		for (int i = 0; Emitters != null && i < Emitters.Length; i++)
		{
			Mission.Instance.StartCoroutine(ParticleRun(Emitters[i].Emitter, Emitters[i].Delay));

			if (Emitters[i].LinkOnRoot)
				Emitters[i].Emitter.transform.parent = Root;
		}

		for (int i = 0; Sounds != null && i < Sounds.Length; i++)
		{
			Mission.Instance.StartCoroutine(SoundRun(Sounds[i].Audio, Sounds[i].Delay));
			Mission.Instance.StartCoroutine(SoundStop(Sounds[i].Audio, Sounds[i].Delay + Sounds[i].Life));

			if (Sounds[i].Parent)
				Sounds[i].Audio.transform.parent = Sounds[i].Parent;
		}

		OnStart();
	}

	protected virtual void OnStart()
	{
	}

	protected virtual void OnDone()
	{
	}

	public virtual void Reset()
	{
		if (Animation && AnimBreak)
		{
			Animation.Stop();
			AnimBreak.SampleAnimation(GameObject, 0);
		}

		Active = true;
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

	IEnumerator ParticleRun(ParticleSystem emitter, float delay)
	{
		yield return new WaitForSeconds(delay);
		emitter.Play();

		//Debug.Log(Time.timeSinceLevelLoad + " ParticleRun"); 
	}

	IEnumerator ParticleStop(ParticleEmitter emitter, float delay)
	{
		yield return new WaitForSeconds(delay);
		emitter.emit = false;

		// Debug.Log(Time.timeSinceLevelLoad + " ParticleStop");
	}

	IEnumerator SoundRun(AudioSource audio, float delay)
	{
		yield return new WaitForSeconds(delay);
		audio.Play();
		//Debug.Log(Time.timeSinceLevelLoad + " " + audio.name + " audio start");
	}

	IEnumerator SoundStop(AudioSource audio, float delay)
	{
		yield return new WaitForSeconds(delay);
		audio.Stop();
		//Debug.Log(Time.timeSinceLevelLoad + " " + audio.name + " audio stop ");
	}
}
