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

using System;
using UnityEngine;
using System.Collections.Generic;

class ResourceCache
{
	String m_ResourceName;
	int m_InitialCacheSize;
	GameObject m_Prefab;
	List<GameObject> m_FreeObjects = new List<GameObject>();

	public ResourceCache(String inName, int inInitialCacheSize)
	{
		m_ResourceName = inName;
		m_InitialCacheSize = inInitialCacheSize;
		if (m_InitialCacheSize > 0)
			CreateResources(m_InitialCacheSize);
	}

	public ResourceCache(GameObject inPrefab, int inInitialCacheSize)
	{
		//m_ResourceName      = inName;
		m_InitialCacheSize = inInitialCacheSize;
		m_Prefab = inPrefab;
		if (m_InitialCacheSize > 0)
			CreateResources(m_InitialCacheSize);
	}

	~ResourceCache()
	{
		m_Prefab = null;
		//foreach(GameObject go in m_FreeObjects)
		//    GameObject.Destroy(go);

		m_FreeObjects.Clear();
	}

	public GameObject Get()
	{
		if (m_FreeObjects.Count == 0)
		{
			CreateResources(Mathf.Max(1, m_InitialCacheSize));
		}

		if (m_FreeObjects.Count > 0)
		{
			GameObject go = m_FreeObjects[0];
			m_FreeObjects.RemoveAt(0);
			return go;
		}

		return null;
	}

	public void Return(GameObject inObject)
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

	// === INTERNAL ==================================================================================
	internal void CreateResources(int inNumberOfItems)
	{
		if (m_Prefab == null)
		{
			m_Prefab = Resources.Load(m_ResourceName) as GameObject;
		}

		if (m_Prefab == null)
		{
			Debug.LogWarning("Can't initialize ResourceCache for resource : " + m_ResourceName);
		}
		else
		{
			for (int i = inNumberOfItems; i > 0; --i)
			{
				GameObject go = GameObject.Instantiate(m_Prefab) as GameObject;
				go.SetActive(false);
				go.name = go.name + m_FreeObjects.Count.ToString();
				m_FreeObjects.Add(go);
			}
		}
	}
};

// !!! AX :: Not tested !!!
class MultiResourceCache<CacheKeyType, CacheType>
				where CacheType : ResourceCache
{
	// Caches for resources
	Dictionary<CacheKeyType, CacheType> Caches = new Dictionary<CacheKeyType, CacheType>();

	// Define the indexer, which will allow client code to use [] notation on the class instance itself.
	public CacheType this[CacheKeyType inKey]
	{
		get { return Caches[inKey]; }
		set { Caches[inKey] = value; }
	}

	public GameObject GetWeapon(CacheKeyType type)
	{
		// test if we have configured cache for this type of weapon...
		if (Caches.ContainsKey(type) == false)
		{
			Debug.LogError("MultiResourceCache: unknown type " + type);
			return null;
		}

		// if we known this weapon type but we don't have resource cache than go out,
		// this is corect situation...
		else if (Caches[type] == null)
		{
			Debug.Log("MultiResourceCache: We don't have resource for this type " + type);
			return null;
		}

		else
		{
			return Caches[type].Get();
		}
	}

	public void Return(CacheKeyType type, GameObject inObject)
	{
		// sanity check...
		if (inObject == null)
		{
			Debug.LogError("MultiResourceCache: sombody is trying return null object to cache");
		}

		// test if we have configured cache for this type of weapon...
		else if (Caches.ContainsKey(type) == false)
		{
			Debug.LogError("MultiResourceCache: unknown type " + type);
		}

		// if we known this weapon type but we don't have resource cache than go out,
		// THIS is imposible This weapon was not constructed by this manager ...
		else if (Caches[type] == null)
		{
			Debug.LogError("MultiResourceCache: We don't have cache for this type. This object was not created by this factory");
		}

		else
		{
			Caches[type].Return(inObject);
		}
	}
};

//class MultiResourceCache< CacheKeyType, CacheType> :
//    MultiResourceCache< CacheKeyType, CacheType, GameObject>
//    where CacheType : ResourceCache
//{   };
