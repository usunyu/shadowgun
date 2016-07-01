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

public class CombatEffectsManager : MonoBehaviour
{
	public class CacheData
	{
		public GameObject GameObject;
		public ParticleSystem[] Emitters;
		public Transform Transform;

		~CacheData()
		{
			GameObject = null;
			Emitters = null;
			Transform = null;
		}
	}

	[System.Serializable]
	public class CombatEffect
	{
		Queue<CacheData> Cache = new Queue<CacheData>();
		List<CacheData> InUse = new List<CacheData>();
		public GameObject Prefab;

		Quaternion Temp = new Quaternion();

		~CombatEffect()
		{
			Cache.Clear();
			InUse.Clear();
			Prefab = null;
		}

		public void Init(int count)
		{
			if (Prefab == null)
				return;

			for (int i = 0; i < count; i++)
			{
				CacheData c = new CacheData();
				c.GameObject = GameObject.Instantiate(Prefab) as GameObject;

				c.Emitters = c.GameObject.GetComponentsInChildren<ParticleSystem>();
				c.Transform = c.GameObject.transform;
				Cache.Enqueue(c);
				c.GameObject.SetActive(false);
			}
		}

		public void Update()
		{
			for (int i = InUse.Count - 1; i >= 0; i--)
			{
				CacheData c = InUse[i];
				bool emitting = false;
				for (int ii = 0; ii < c.Emitters.Length; ii++)
				{
					if (c.Emitters[ii].isPlaying || c.Emitters[ii].particleCount != 0)
					{
						//     Debug.Log(Time.timeSinceLevelLoad + " dont remove" + c.GameObject.name + " emitter " + i + " is playing " + c.Emitters[ii].time + " count " + c.Emitters[ii].particleCount);
						emitting = true;
					}
				}

				if (emitting == false)
				{
					//Debug.Log(Time.timeSinceLevelLoad + " remove " + c.GameObject.name);
					c.Transform.parent = null;
					Cache.Enqueue(InUse[i]);
					InUse.RemoveAt(i);
					c.GameObject.SetActive(false);
				}
			}

			//Debug.Log(Prefab.name + " InUse " + InUse.Count + " Free " + Cache.Count);
		}

		/*public CacheData Get()
        {
            if (Cache.Count == 0)
                Init(2);

            return Cache.Dequeue();
        }*/

		public void Return(CacheData c)
		{
			InUse.Add(c);
		}

		public void Play(Vector3 pos, Vector3 dir)
		{
			if (Cache.Count == 0)
			{
				if (InUse.Count == 0)
					Init(2);
				else
				{
					CacheData old = InUse[0];
					old.Transform.parent = null;

					for (int ii = 0; ii < old.Emitters.Length; ii++)
						old.Emitters[ii].Stop(true);

					Cache.Enqueue(InUse[0]);
					InUse.RemoveAt(0);
				}
			}

			CacheData c = Cache.Dequeue();
			InUse.Add(c);

			c.GameObject.SetActive(true);

			c.Transform.position = pos;

			Temp.SetLookRotation(dir);
			c.Transform.rotation = Temp;

			for (int i = 0; i < c.Emitters.Length; i++)
				c.Emitters[i].Play();
		}
	}

	[SerializeField] CombatEffect BloodHit = new CombatEffect();
	[SerializeField] CombatEffect DefaultHit = new CombatEffect();
	[SerializeField] CombatEffect MetalHit = new CombatEffect();
	[SerializeField] CombatEffect WaterHit = new CombatEffect();
	[SerializeField] CombatEffect PlasmaGunHit = new CombatEffect();
	[SerializeField] CombatEffect RailGunHit = new CombatEffect();

	public Material InvisibleEffectMaterial;

	Dictionary<int, CombatEffect> HitEffects = new Dictionary<int, CombatEffect>();

	public static CombatEffectsManager Instance = null;

	void Awake()
	{
		if (Instance != null)
			Debug.LogError(this.name + " is singleton, somebody is creating another instance !!");

		Instance = this;

		if (uLink.Network.isClient)
		{
			BloodHit.Init(4);
			DefaultHit.Init(5);
			MetalHit.Init(5);
			WaterHit.Init(5);

			HitEffects.Add(0, DefaultHit);
			HitEffects.Add(27, WaterHit);
			HitEffects.Add(28, DefaultHit);
			HitEffects.Add(29, MetalHit);

			// special hits...
			PlasmaGunHit.Init(5);
			RailGunHit.Init(5);
		}
	}

	void OnDestroy()
	{
		BloodHit = null;
		DefaultHit = null;
		MetalHit = null;
		WaterHit = null;

		RailGunHit = null;
		PlasmaGunHit = null;

		HitEffects.Clear();
	}

	void LateUpdate()
	{
		if (uLink.Network.isServer)
			return;

		BloodHit.Update();
		DefaultHit.Update();
		MetalHit.Update();
		WaterHit.Update();

		RailGunHit.Update();
		PlasmaGunHit.Update();
	}

	public void PlayHitEffect(GameObject parent, Vector3 pos, Vector3 dir, bool localPlayer)
	{
		PlayHitEffect(parent, parent.layer, pos, dir, E_ProjectileType.None, localPlayer);
	}

	public void PlayHitEffect(GameObject parent, Vector3 pos, Vector3 dir, E_ProjectileType inProjectileType, bool localPlayer)
	{
		PlayHitEffect(parent, parent.layer, pos, dir, inProjectileType, localPlayer);
	}

	public void PlayBloodEffect(Renderer renderer, Vector3 pos, Vector3 dir)
	{
		if (uLink.Network.isServer == true || renderer == null)
			return;

		// capa: I don't know why but 'renderer.isVisible' was always 'false' even if I was looking at corresponding enemy ?!?
		//	if (renderer != null && renderer.isVisible == false)
		//		return;

		Frustum.SetupCamera(GameCamera.Instance, 30);
		if (Frustum.PointInFrustum(pos) == Frustum.E_Location.Outside)
			return;

		BloodHit.Play(pos, dir);
	}

	public void PlayHitEffect(GameObject parent, int layer, Vector3 pos, Vector3 dir, E_ProjectileType inProjectileType, bool localPlayer)
	{
		if (uLink.Network.isServer)
			return;

		if (localPlayer == false)
		{
			Frustum.SetupCamera(GameCamera.Instance, 30);
			Frustum.E_Location loc = Frustum.PointInFrustum(pos);

			if (loc == Frustum.E_Location.Outside) //do not spawn the projectile if it's not visible at all
				return;
		}

		if (inProjectileType == E_ProjectileType.Plasma)
		{
			if (PlasmaGunHit.Prefab != null)
			{
				//shift it a little to avoid static object penetration
				PlasmaGunHit.Play(pos + dir*0.4f, dir);
				return;
			}
		}

		if (inProjectileType == E_ProjectileType.Rail)
		{
			if (PlasmaGunHit.Prefab != null)
			{
				PlasmaGunHit.Play(pos, dir);
				return;
			}
		}

		// default, old, behavior...
		if (HitEffects.ContainsKey(layer) == true)
		{
			HitEffects[layer].Play(pos, dir);
		}
		else
		{
			HitEffects[0].Play(pos, dir);
		}
	}
}
