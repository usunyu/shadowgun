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

public enum E_ProjectileType
{
	None = 0,
	// standard weapon projectiles
	Bullet,
	Shotgun,
	Rocket,
	Plasma,
	Rail,
}

// should be used within every dictionary that uses 'E_ProjectileType' as key to speed things little bit up :o)
public class ProjectileTypeComparer : IEqualityComparer<E_ProjectileType>
{
	public readonly static ProjectileTypeComparer Instance = new ProjectileTypeComparer();

	public bool Equals(E_ProjectileType A, E_ProjectileType B)
	{
		return A == B;
	}

	public int GetHashCode(E_ProjectileType X)
	{
		return (int)X;
	}
}

[AddComponentMenu("Weapons/ProjectileManager")]
public class ProjectileManager : MonoBehaviour
{
	[System.Serializable]
	public class SoundInfo
	{
		public AudioClip[] DefaultHits = new AudioClip[0];
		public AudioClip[] MetalHits = new AudioClip[0];
		public AudioClip[] BloodHits = new AudioClip[0];
		public AudioClip[] WaterHits = new AudioClip[0];

		public AudioClip HitDefault
		{
			get
			{
				if (DefaultHits.Length == 0)
					return null;
				return DefaultHits[Random.Range(0, DefaultHits.Length)];
			}
		}

		public AudioClip HitMetal
		{
			get
			{
				if (MetalHits.Length == 0)
					return null;
				return MetalHits[Random.Range(0, MetalHits.Length)];
			}
		}

		public AudioClip HitBlood
		{
			get
			{
				if (BloodHits.Length == 0)
					return null;
				return BloodHits[Random.Range(0, BloodHits.Length)];
			}
		}

		public AudioClip HitWater
		{
			get
			{
				if (WaterHits.Length == 0)
					return null;
				return WaterHits[Random.Range(0, WaterHits.Length)];
			}
		}
	}

	// static instance of this manager...
	public static ProjectileManager Instance;

	// Audio members which are shared for all projectiles...
	public SoundInfo ProjectileSounds = new SoundInfo();
	public SoundInfo GrenadeSounds = new SoundInfo();
	public SoundInfo PlasmaSounds = new SoundInfo();
	GameObject Audio;
	AudioSource AudioSource;
	Transform AudioTransform;

	// Cache for all known projectile types. See configuration bellow in Awake function
	Dictionary<E_ProjectileType, ProjectileCacheEx> CacheOfProjectiles =
					new Dictionary<E_ProjectileType, ProjectileCacheEx>(ProjectileTypeComparer.Instance);
	// list of projectiles in air
	List<Projectile> ActiveProjectiles = new List<Projectile>();

	// Use this for initialization
	void Awake()
	{
		Instance = this;

		// initialize caches of projectiles...
		RegisterProjectile("Weapons/ProjectileSMGWithTrace", E_ProjectileType.Bullet, 10);
		RegisterProjectile("Weapons/ProjectileSMG", E_ProjectileType.Shotgun, 20);
		RegisterProjectile("Weapons/ProjectileRocket", E_ProjectileType.Rocket, 10);
		RegisterProjectile("Weapons/ProjectilePlasma", E_ProjectileType.Plasma, 10);
		RegisterProjectile("Weapons/ProjectileLaserBeam", E_ProjectileType.Rail, 10);

		// setup sound source for all projectle sounds...
		Audio = new GameObject("ProjectilesAudio", typeof (AudioSource));
		AudioSource = Audio.GetComponent<AudioSource>();
		AudioSource.playOnAwake = false;
		AudioSource.minDistance = 1;
		AudioSource.maxDistance = 30;
		AudioSource.rolloffMode = AudioRolloffMode.Linear;
		AudioTransform = AudioSource.transform;
	}

	void OnDestroy()
	{
		ProjectileSounds = null;
		GrenadeSounds = null;
		PlasmaSounds = null;
		Audio = null;
		AudioSource = null;

		CacheOfProjectiles.Clear();
		ActiveProjectiles.Clear();
	}

	// Update is called once per frame
	void Update()
	{
		if (null == Game.Instance)
			return;

		// if we are not in regular game or ingame menu don't update projectiles...
		switch (Game.Instance.GameState)
		{
		case E_GameState.Game:
		case E_GameState.IngameMenu:
			break;
		default:
			return;
		}

		// if game is paused don't update projectiles...
		if (Time.deltaTime <= 0.0f)
			return;

		// Update all projectiles in air...
		foreach (Projectile proj in ActiveProjectiles)
		{
			//Debug.Log(Time.timeSinceLevelLoad + " Update proj finished ? " + proj.IsFinished());
			if (proj.IsFinished() == true)
				continue;

			proj.ProjectileUpdate();
		}
	}

	void FixedUpdate()
	{
		for (int i = ActiveProjectiles.Count - 1; i >= 0; i--)
		{
			if (ActiveProjectiles[i].IsFinished() == false)
				continue;

			ReturnProjectile(ActiveProjectiles[i]);
			ActiveProjectiles.RemoveAt(i);
		}
	}

	void RegisterProjectile(string inPrefabPath, E_ProjectileType inType, int inInitCount)
	{
		CacheOfProjectiles[inType] = new ProjectileCacheEx(inPrefabPath, inType, inInitCount);
	}

	public void SpawnProjectile(E_ProjectileType inProjeType, Vector3 inPos, Vector3 inDir, Projectile.InitSettings inSettings)
	{
		//Debug.Log("New SpawnProjectile " + inProjeType);
		// test if we have configured cache for this type of resource...
		if (CacheOfProjectiles.ContainsKey(inProjeType) == false)
		{
			Debug.LogError("ProjectileFactory: unknown type " + inProjeType);
			return;
		}

		// if we known this type but we don't have resource cache than go out,
		// this is corect situation...
		else if (CacheOfProjectiles[inProjeType] == null)
		{
			Debug.LogError("ProjectileFactory: For this type " + inProjeType + " we don't have resource");
			return;
		}

		//check the projectile against the camera frustum - when it doesn't intersect it, do not bother spawning it on client
		if (uLink.Network.isServer == false && inSettings.Agent && inSettings.Agent.IsProxy)
		{
			Frustum.SetupCamera(GameCamera.Instance, 100);
			// variant A: faster but not accurate
			Frustum.E_Location loc = Frustum.LineInFrustumFast(inPos, inPos + inDir*GameCamera.Instance.MainCamera.farClipPlane);
			// variant B: accurate but slower
			//	Frustum.E_Location loc = Frustum.LineInFrustum(GameCamera.Instance.MainCamera,inPos, inPos + inDir * GameCamera.Instance.MainCamera.far);

			if (loc == Frustum.E_Location.Outside) //do not spawn the projectile if it's not visible at all
			{
//				Debug.Log ("Projectile NOT SPAWNED, pos=" + inPos.ToString("F3") + ", dir=" + inDir.ToString("F3"));
				return;
			}
//			else
//			{
//				Debug.Log ("Projectile SPAWNED, pos=" + inPos.ToString("F3") + ", dir=" + inDir.ToString("F3"));
//			}
		}
		//

		Projectile proj = CacheOfProjectiles[inProjeType].Get();
		if (proj == null)
		{
			Debug.LogError("ProjectileFactory: Can't create projectile for type " + inProjeType);
			return;
		}

		proj.ProjectileInit(inPos, inDir.normalized, inSettings);
		ActiveProjectiles.Add(proj);
	}

	public void ReturnProjectile(Projectile inProjectile)
	{
		// Debug.Log(Time.timeSinceLevelLoad + "Return ReturnProjectile " + inProjectile.ProjectileType);
		// sanity check...
		if (inProjectile == null)
		{
			Debug.LogError("ProjectileFactory: sombody is trying return null object to cache");
		}

		// test if we have configured cache for this type of resource...
		else if (CacheOfProjectiles.ContainsKey(inProjectile.ProjectileType) == false)
		{
			Debug.LogError("ProjectileFactory: unknown type " + inProjectile.ProjectileType);
		}

		// if we known this projectile type but we don't have resource cache than go out,
		// THIS is imposible This weapon was not constructed by this manager ...
		else if (CacheOfProjectiles[inProjectile.ProjectileType] == null)
		{
			Debug.LogError("ProjectileFactory: We don't have cache for this projectile type. This object was not created by this manager");
		}

		else
		{
			inProjectile.ProjectileDeinit();
			CacheOfProjectiles[inProjectile.ProjectileType].Return(inProjectile);
		}
	}

	public void Reset()
	{
		for (int i = 0; i < ActiveProjectiles.Count; i++)
		{
			ActiveProjectiles[i].ProjectileDeinit();
			ReturnProjectile(ActiveProjectiles[i]);
		}

		ActiveProjectiles.Clear();
	}

	public void PlaySound(AudioClip clip, Vector3 pos)
	{
		if (uLink.Network.isServer)
			return;

		if (AudioSource.maxDistance*AudioSource.maxDistance < Vector3.SqrMagnitude(pos - GameCamera.Instance.CameraPosition))
			return;

		AudioTransform.position = pos;
		AudioSource.PlayOneShot(clip);
	}

	public void PlayGrenadeHitSound(int layer, Vector3 pos)
	{
		if (uLink.Network.isServer)
			return;

		AudioClip clip = null;

		switch (layer)
		{
		case 27:
			clip = GrenadeSounds.HitWater;
			break;
		case 28:
			clip = GrenadeSounds.HitDefault;
			break;
		case 29:
			clip = GrenadeSounds.HitMetal;
			break;
		case 31:
			clip = GrenadeSounds.HitBlood;
			break;
		default:
			clip = GrenadeSounds.HitDefault;
			break;
		}
		//assert(clip);
		if (clip != null)
		{
			PlaySound(clip, pos);
		}
	}

	public void PlayHitSound(int layer, Vector3 pos, E_ProjectileType inProjType)
	{
		AudioClip clip = null;

		if (inProjType == E_ProjectileType.Plasma)
		{
			clip = PlasmaSounds.HitDefault;
		}
/*        else if(inProjType == E_ProjectileType.Grenade)
        {
            switch (layer)
            {
                case 27:    clip = GrenadeSounds.HitWater;          break;
                case 28:    clip = GrenadeSounds.HitDefault;        break;
                case 29:    clip = GrenadeSounds.HitMetal;          break;
                case 31:    clip = GrenadeSounds.HitBlood;          break;
                default:    clip = GrenadeSounds.HitDefault;        break;
            }
        }
        else*/
		{
			switch (layer)
			{
			case 27:
				clip = ProjectileSounds.HitWater;
				break;
			case 28:
				clip = ProjectileSounds.HitDefault;
				break;
			case 29:
				clip = ProjectileSounds.HitMetal;
				break;
			case 31:
				clip = ProjectileSounds.HitBlood;
				break;
			default:
				clip = ProjectileSounds.HitDefault;
				break;
			}
		}

		//assert(clip);
		if (clip != null)
		{
			PlaySound(clip, pos);
		}
	}
}
