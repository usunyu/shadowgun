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

[System.Serializable]
public class ExplosionCache
{
	[System.Serializable]
	public class ExplosionPrefabCache : PrefabCache<Explosion>
	{
	};

	public ExplosionPrefabCache WaterExplosion;
	public List<ExplosionPrefabCache> Definition = new List<ExplosionPrefabCache>();
	Dictionary<Explosion, ExplosionPrefabCache> _Cache = new Dictionary<Explosion, ExplosionPrefabCache>();

	public void Init()
	{
		foreach (ExplosionPrefabCache item in Definition)
		{
			_Cache[item.m_Prefab] = item;
			_Cache[item.m_Prefab].Init();
		}

		if (WaterExplosion != null && WaterExplosion.m_Prefab != null)
		{
			_Cache[WaterExplosion.m_Prefab] = WaterExplosion;
			_Cache[WaterExplosion.m_Prefab].Init();
		}
	}

	public Explosion GetWaterExplosion(Vector3 inPosition, Quaternion inRotation)
	{
		if (WaterExplosion != null && WaterExplosion.m_Prefab != null)
			return Get(WaterExplosion.m_Prefab, inPosition, inRotation);

		Debug.LogWarning("ExplosionCache: Water explosion is not defined ");
		return null;
	}

	public Explosion Get(Explosion inPrefab, Vector3 inPosition, Quaternion inRotation)
	{
		return Get(inPrefab, inPosition, inRotation, default(Transform[]));
	}

	public Explosion Get(Explosion inPrefab, Vector3 inPosition, Quaternion inRotation, Transform inNoBlocking)
	{
		return Get(inPrefab, inPosition, inRotation, new Transform[] {inNoBlocking});
	}

	public Explosion Get(Explosion inPrefab, Vector3 inPosition, Quaternion inRotation, Transform[] inNoBlocking)
	{
		//Debug.Log("New SpawnProjectile " + inProjeType);
		// test if we have configured cache for this type of resource...
		if (_Cache.ContainsKey(inPrefab) == false)
		{
			Debug.LogError("ExplosionCache: unknown type " + inPrefab);
			return null;
		}

		// if we known this type but we don't have resource cache than go out,
		// this is corect situation...
		else if (_Cache[inPrefab] == null)
		{
			Debug.Log("ExplosionCache: For this type " + inPrefab + " we don't have resource");
			return null;
		}

		Explosion obj = _Cache[inPrefab].Get();
		if (obj == null)
		{
			Debug.LogWarning("ExplosionCache: Cache for item " + inPrefab + " is empty we must create new one.");
			_Cache[inPrefab].CreateNewInstance();
			obj = _Cache[inPrefab].Get();
		}

		if (obj == null)
		{
			Debug.LogError("ExplosionCache: Can't create explosion for type " + inPrefab);
			return null;
		}

		obj.cacheKey = inPrefab;
		obj.noBlocking = inNoBlocking;
		obj.transform.position = inPosition;
		obj.transform.rotation = inRotation;
		obj.gameObject.SetActive(true);

		//Debug.Log("GetObjectFromCache : " + obj + " Remaning size is " + _Cache[inPrefab].m_FreeObjects.Count);

		return obj;
	}

	public void Return(Explosion inExplosion)
	{
		//Debug.Log("New ReturnProjectile " + inProjectile.ProjectileType);
		// sanity check...
		if (inExplosion == null)
		{
			Debug.LogError("ExplosionCache: sombody is trying return null object to cache");
		}

		// test if we have configured cache for this type of resource...
		else if (_Cache.ContainsKey(inExplosion.cacheKey) == false)
		{
			Debug.LogError("ExplosionCache: unknown type " + inExplosion.cacheKey);
		}

		// if we known this projectile type but we don't have resource cache than go out,
		// THIS is imposible This weapon was not constructed by this manager ...
		else if (_Cache[inExplosion.cacheKey] == null)
		{
			Debug.LogError("ExplosionCache: We don't have cache for this type. This object was not created by this manager");
		}

		else
		{
			Explosion key = inExplosion.cacheKey;
			//Debug.Log("ReturnObjectToCache : " + inExplosion);
			inExplosion.Reset();
			inExplosion.gameObject.SetActive(false);
			inExplosion.cacheKey = null;
			_Cache[key].Return(inExplosion);
		}
	}

	public void Reset()
	{
		// reset all explosions, which are active in scene now...
		Explosion[] explosions = GameObject.FindObjectsOfType(typeof (Explosion)) as Explosion[];
		foreach (Explosion expl in explosions)
		{
			if (expl.cacheKey != null && _Cache.ContainsKey(expl.cacheKey) == true)
			{
				Return(expl);
			}
		}
	}

	public void Clear()
	{
		if (WaterExplosion != null)
			WaterExplosion.Clear();

		foreach (ExplosionPrefabCache cache in Definition)
			cache.Clear();

		Definition.Clear();
		_Cache.Clear();
	}
}

[System.Serializable]
public class PrefabCache<T> where T : UnityEngine.Object
{
	public T m_Prefab;
	public int m_InitialCacheSize;

	public List<T> m_FreeObjects = new List<T>();

	~PrefabCache()
	{
		Clear();
	}

	public T Get()
	{
		if (m_FreeObjects.Count > 0)
		{
			T go = m_FreeObjects[0];
			m_FreeObjects.RemoveAt(0);
			return go;
		}

		return null;
	}

	public void Return(T inObject)
	{
		// safe checks...
		if (inObject == null)
		{
			Debug.LogWarning("You are trying return null object");
			return;
		}

		if (m_FreeObjects.Contains(inObject) == true)
		{
			Debug.LogWarning("Object [" + inObject + "] is already in Free list");
			return;
		}

		m_FreeObjects.Add(inObject);
	}

	public void Init()
	{
		if (m_InitialCacheSize <= 0)
			return;

		for (int i = m_InitialCacheSize; i > 0; --i)
		{
			CreateNewInstance();
		}

		//Debug.Log("ExplosionCache: Size of cache for item " + m_Prefab + " is + " + m_FreeObjects.Count);
	}

	public void CreateNewInstance()
	{
		if (m_Prefab != null)
		{
			T obj = Object.Instantiate(m_Prefab) as T;

			GameObject go = obj as GameObject;
			Component comp = obj as Component;
			if (go != null)
			{
				go.SetActive(false);
			}
			else if (comp != null)
			{
				comp.gameObject.SetActive(false);
			}

			m_FreeObjects.Add(obj);
		}
	}

	public void Clear()
	{
		m_Prefab = null;
		m_FreeObjects.Clear();
	}
};
