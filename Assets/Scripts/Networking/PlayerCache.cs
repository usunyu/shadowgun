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
using uLink;
using System.Collections.Generic;

[AddComponentMenu("Multiplayer/Player Cache")]
public class PlayerCache : uLink.MonoBehaviour
{
	[System.Serializable]
	public class ClientCache
	{
		[System.Serializable]
		public class PlayerInfo
		{
			public GameObject PrefabOwner;
			public GameObject PrefabProxy;

			NetworkInstantiatePool PoolP;
			NetworkInstantiatePool PoolO;

			public void Init()
			{
				Printf("Initialize proxy player pools");

				PoolO = new NetworkInstantiatePool(PrefabOwner, 0, PlayerCache.OwnerActivatePlayer, PlayerCache.PrefabInitDelegate);
				PoolP = new NetworkInstantiatePool(PrefabProxy, 0, PlayerCache.ProxyActivatePlayer, PlayerCache.PrefabInitDelegate);
			}

			public void Destroy()
			{
				Printf("Destroy proxy player pools");

				if (PoolO != null)
					PoolO.Destroy();

				if (PoolP != null)
					PoolP.Destroy();

				PoolP = null;
				PoolO = null;
			}

			public void Reset()
			{
				Printf("Reset proxy player pools");

				PoolO.Reset();
				PoolP.Reset();
			}
		}
		[SerializeField] List<PlayerInfo> PlayerPrefabs;

		public void Init()
		{
			foreach (PlayerInfo p in PlayerPrefabs)
				p.Init();
		}

		public void Destroy()
		{
			foreach (PlayerInfo p in PlayerPrefabs)
				p.Destroy();
		}

		public void Reset()
		{
			foreach (PlayerInfo p in PlayerPrefabs)
				p.Reset();
		}
	}

//   [System.Serializable]
//    public class ServerCache
//    {
//        [System.Serializable]
//        public class PlayerInfo
//        {
//            public E_SkinID Skin;
//            public GameObject PrefabCreator;
//            public GameObject PrefabOwner;
//            public GameObject PrefabProxy;
//
//            NetworkInstantiatePool PoolC;
//
//            public void Init()
//            {
//                if (PrefabCreator)
//                    PoolC = new NetworkInstantiatePool( PrefabCreator, 0, PlayerCache.ServerActivatePlayer, PlayerCache.PrefabInitDelegate );
//            }
//            public void Destroy()
//            {
//                if (PoolC != null)
//                    PoolC.Destroy();
//                PoolC = null;
//            }
//
//            public void Reset()
//            {
//                if (PoolC != null)
//                    PoolC.Reset();
//            }
//        }
//
//        [SerializeField]
//        private List<PlayerInfo> PlayerPrefabs;
//
//
//        public void Init()
//        {
//            foreach (PlayerInfo p in PlayerPrefabs)
//                p.Init();
//        }
//
//        public void Destroy()
//        {
//            foreach (PlayerInfo p in PlayerPrefabs)
//                p.Destroy();
//        }
//
//        public void Reset()
//        {
//            foreach (PlayerInfo p in PlayerPrefabs)
//                p.Reset();
//        }
//
//
//        public PlayerInfo Find(E_SkinID skin)
//        {
//            return PlayerPrefabs.Find(ps => ps.Skin == skin);
//        }
//    }

	// Capa: Only one 'Creator' prefab for all skins.
	[System.Serializable]
	public class ServerCache
	{
		public GameObject PrefabCreator;
		NetworkInstantiatePool PoolCreator;

		[System.Serializable]
		public class PlayerInfo
		{
			public E_SkinID Skin;
			public GameObject PrefabOwner;
			public GameObject PrefabProxy;
		}

		[SerializeField] List<PlayerInfo> PlayerPrefabs;

		public void Init()
		{
			if (PrefabCreator)
				PoolCreator = new NetworkInstantiatePool(PrefabCreator, 0, PlayerCache.ServerActivatePlayer, PlayerCache.PrefabInitDelegate);
		}

		public void Destroy()
		{
			if (PoolCreator != null)
				PoolCreator.Destroy();
			PoolCreator = null;
		}

		public void Reset()
		{
			if (PoolCreator != null)
				PoolCreator.Reset();
		}

		public PlayerInfo Find(E_SkinID skin)
		{
			return PlayerPrefabs.Find(ps => ps.Skin == skin);
		}
	}

	public static PlayerCache Instance { get; private set; }

	public GameObject PlayerCreatorPrefab
	{
		get { return CacheServer.PrefabCreator; }
	}

	[SerializeField] ServerCache CacheServer = new ServerCache();
	[SerializeField] ClientCache CacheClient = new ClientCache();

	public bool IsDebug = false;

	void OnLevelWasLoaded(int level)
	{
		if (Instance != null && Instance != this)
		{
			DestroyImmediate(this);
			return;
		}
	}

	public void Awake()
	{
		if (Instance != null)
			return;

		Instance = this;
		DontDestroyOnLoad(this);
	}

	void Start()
	{
		if (Game.Instance.AppType == Game.E_AppType.Game)
			InitializeForClient();
		else if (Game.Instance.AppType == Game.E_AppType.DedicatedServer)
			InitializeForServer();
	}

	public void InitializeForClient()
	{
		Printf("initialize for client");

		CacheClient.Init();
	}

	public void DestroyForClient()
	{
		Printf("destroy for client");

		CacheClient.Destroy();
	}

	public void InitializeForServer()
	{
		Printf("initialize for server");

		CacheServer.Init();
	}

	public void DestoryForServer()
	{
		Printf("destory for server");
		CacheServer.Destroy();
	}

	public void Reset()
	{
		Printf("Reset");

		if (CacheClient != null)
			CacheClient.Reset();
		// CacheClient = null;

		if (CacheServer != null)
			CacheServer.Reset();
	}

	void uLink_OnDisconnectedFromServer(uLink.NetworkDisconnection mode)
	{
		Reset();
	}

	void OnDestroy()
	{
		Printf("Destroy");

		if (CacheClient != null)
			CacheClient.Destroy();
		CacheClient = null;

		if (CacheServer != null)
			CacheServer.Destroy();

		CacheServer = null;
	}

	public ServerCache.PlayerInfo Find(E_SkinID skin)
	{
		if (CacheServer == null)
			return null;

		return CacheServer.Find(skin);
	}

	public static void Printf(string s)
	{
		if (PlayerCache.Instance.IsDebug)
			print(Time.timeSinceLevelLoad + " PlayerCache - " + s);
	}

	public static void ServerActivatePlayer(GameObject gameObject)
	{
		gameObject.SetActive(true);
		gameObject.SendMessage("Activate", SendMessageOptions.RequireReceiver);
	}

	public static void OwnerActivatePlayer(GameObject gameObject)
	{
		gameObject.SetActive(true);
		gameObject.SendMessage("Activate", SendMessageOptions.RequireReceiver);
	}

	public static void ProxyActivatePlayer(GameObject gameObject)
	{
		gameObject.GetComponent<AgentHuman>().ActivateProxy();
	}

	public static void PrefabInitDelegate(GameObject gameObject)
	{
		AgentHuman human = gameObject.GetComponent<AgentHuman>();

		if (null != human)
		{
			human.PrefabPreAwake();
		}
	}
}
