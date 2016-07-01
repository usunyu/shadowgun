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

// (c)2011 Unity Park. All Rights Reserved.

using System.Collections.Generic;
using UnityEngine;
using uLink;

/// <summary>
/// Use this script to make a pool of pre-instantiated prefabs that will be used 
/// when prefabs are network instantiated via uLink.Network.Instantiate().
/// </summary>
///
/// <remarks>
/// This wonderful script makes it possible to pool game objects (instantiated prefabs)
/// and uLink will use this pool of objects as soon as the gameplay code makes a call to
/// Network.Instantiate.
///
/// The wonderful part is that you do not have to change your code at all to use the pool
/// mechanism. This makes it very easy to check if your game is faster and more smooth
/// with a pool.
///
/// In some games, with lots of spawning NPCs, guided missiles etc, the pool
/// is very convenient to avoid the overhead of calling Object.Instantiate() and Object.Destroy().
///
/// The need for a pool is mainly to increase performance, Instantiating objects is a 
/// heavy operation in Unity. By load-testing the game with the uTsung tool it is easier to
/// make decisions when to use a pool for prefabs.
///
/// A normal scenario is to pool creator prefabs in the server scene and pool proxy prefabs
/// in the client scene.
/// 
/// The value minSize is the number of prefabs that will be instantiated at startup.
/// If the number of Instantiate calls does go above this value, the pool will be increased 
/// at run-time.
/// </remarks>
public class NetworkInstantiatePool
{
	protected uLink.NetworkView Prefab;

	readonly Stack<uLink.NetworkView> Pool = new Stack<uLink.NetworkView>();

	readonly List<uLink.NetworkView> Active = new List<uLink.NetworkView>();

	Transform Parent;

	int MaxSize;

	public delegate void ActivateDelegate(GameObject gameObject);
	ActivateDelegate Activate;

	public delegate void PrefabInitDelegate(GameObject gameObject);

	public NetworkInstantiatePool(GameObject prefab, int size, ActivateDelegate activate, PrefabInitDelegate prefabInitDelegate)
	{
		Prefab = prefab.GetComponent<uLink.NetworkView>();

		if (null != prefabInitDelegate)
		{
			prefabInitDelegate(prefab);
		}

		MaxSize = size;

		if (Prefab._manualViewID != 0)
		{
			Debug.LogError("Prefab viewID must be set to Allocated or Unassigned", Prefab);
			return;
		}

		Parent = new GameObject("_" + Prefab.name + "-Pool").transform;

		GameObject.DontDestroyOnLoad(Parent);

		for (int i = 0; i < size; i++)
		{
			uLink.NetworkView instance = (uLink.NetworkView)Object.Instantiate(Prefab);
			instance.transform.parent = Parent;
			instance.gameObject.SetActive(false);
			GameObject.DontDestroyOnLoad(instance.gameObject);

			Pool.Push(instance);
		}

		Activate = activate;

		uLink.NetworkInstantiator.Add(Prefab.name, Creator, Destroyer);
	}

	public void Reset()
	{
		while (Active.Count > 0)
			Destroyer(Active[0]);
	}

	public void Destroy()
	{
		if (Parent == null)
			return;

		uLink.NetworkInstantiator.Remove(Prefab.name);

		//LEAK ?
		Pool.Clear();
		Active.Clear();

		Object.Destroy(Parent.gameObject);
		Parent = null;
	}

	/*
	private uLink.NetworkView PreInstantiator(string prefabName, Vector3 position, Quaternion rotation, uLink.BitStream stream)
	{
		if (Pool.Count > 0)
		{
			uLink.NetworkView instance = Pool.Pop();
			instance.transform.position = position;
			instance.transform.rotation = rotation;
			//instance.gameObject.SetActive(true);

            //Debug.Log("PreInstantiator " + instance.name);

			return instance;
		}
		else
		{
			uLink.NetworkView instance = (uLink.NetworkView)Object.Instantiate(Prefab);
			instance.transform.parent = Parent;
			instance.transform.position = position;
			instance.transform.rotation = rotation;

            //Debug.Log("PreInstantiator " + instance.name);

			return instance;
		}
	}

	private void PostInstantiator(uLink.NetworkView instance, uLink.NetworkMessageInfo info)
	{
		instance.BroadcastMessage("uLink_OnNetworkInstantiate", info, SendMessageOptions.DontRequireReceiver);
		
		if(Activate != null)
			Activate(instance.gameObject);
		
        Active.Add(instance);
	}
	*/

	uLink.NetworkView Creator(string prefabName, uLink.NetworkInstantiateArgs args, uLink.NetworkMessageInfo info)
	{
		uLink.NetworkView instance = null;

		if (Pool.Count > 0)
		{
			instance = Pool.Pop();
			args.SetupNetworkView(instance);

			//Debug.Log("Creator " + instance.name);
		}
		else
		{
			instance = uLink.NetworkInstantiatorUtility.Instantiate(Prefab, args);

			//Debug.Log("Creator " + instance.name);
		}

		uLink.NetworkInstantiatorUtility.BroadcastOnNetworkInstantiate(instance, info);

		if (Activate != null)
			Activate(instance.gameObject);

		Active.Add(instance);

		return instance;
	}

	void Destroyer(uLink.NetworkView instance)
	{
		// Debug.Log(Time.timeSinceLevelLoad + "Destroyer " + instance.name);

		instance.SendMessage("Deactivate", SendMessageOptions.DontRequireReceiver);

		instance.gameObject.SetActive(false);

		Active.Remove(instance);

		if (MaxSize == Pool.Count)
		{
			Object.Destroy(instance.gameObject);
			return;
		}
		Pool.Push(instance);
	}
}
