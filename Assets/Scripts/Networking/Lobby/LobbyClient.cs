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
using System;
using System.Net;
using System.Collections.Generic;
using System.Collections;
using LitJson;

class LobbyClient : MonoBehaviour
{
	public struct Server
	{
		public string Name;
		public string IPName;
		public IPEndPoint EndPoint;
	};

	internal enum LobbyMode
	{
		Normal,
		Maintenance,
		VersionNotSupported
	}

	static LobbyMode m_Mode = LobbyMode.Normal;
	static bool m_IsInitialized = false;
	static LobbyClient m_Instance = null;
	static GameObject m_InstanceGameObject = null;

	static Chat m_Chat = null;

	static List<Server> LobbyServers = new List<Server>();

	public static event Action<bool> ToLobbyConnectionResult;

	public static bool IsConnected
	{
		get { return false; }
	}

	public static bool IsConnecting
	{
		get { return false; }
	}

	public static bool IsInitialized
	{
		get { return m_IsInitialized; }
	}

	public static bool GameVersionNoLongerSupported
	{
		get { return m_Mode == LobbyMode.VersionNotSupported; }
	}

	public static bool IsServiceRunning
	{
		get { return m_Mode == LobbyMode.Normal; }
	}

	public static Chat Chat
	{
		get
		{
			if (m_Chat == null)
			{
				m_Chat = new Chat();
			}
			return m_Chat;
		}
	}

	static void CreateInstance()
	{
		GameObject go = new GameObject("LobbyClient");
		GameObject.DontDestroyOnLoad(go);

		m_Instance = go.AddComponent<LobbyClient>() as LobbyClient;
	}

	static void DestroyInstance()
	{
		GameObject.Destroy(m_Instance);
		m_Instance = null;

		GameObject.DestroyImmediate(m_InstanceGameObject);
		m_InstanceGameObject = null;
	}

	public void OnDestroy()
	{
		if (IsInitialized)
		{
			// this condition is valid only when the object is destroyed on AppExit
			DeinitializeLobby();
		}

		m_Instance = null;
		m_InstanceGameObject = null;
	}
	
	public static List<Server> GetServers()
	{
		return LobbyServers;
	}

	static void InitializeLobby()
	{
		m_IsInitialized = true;
	}

	static void DeinitializeLobby()
	{
		m_IsInitialized = false;
	}

	public static void ConnectToLobby(NetUtils.GeoRegion region)
	{
		if (!IsInitialized)
		{
			InitializeLobby();
		}
	}

	public static void DisconnectFromLobby()
	{
		if (IsInitialized)
		{
			DeinitializeLobby();
		}
	}
	

	// =================================== Player vs player lobby messagging ===================================

	public delegate void OnPlayerMessageEvent(string userName, string messageId, string messageText);
	
	public static void SendMessageToPlayer(string userName, string messageId, string messageText)
	{
	}

	public static void RegisterPlayerMessageObserver(OnPlayerMessageEvent callback, string userName, string messageId)
	{
	}

	public static void RegisterPlayerMessageObserver(OnPlayerMessageEvent callback, string messageId)
	{
	}

	public static void UnregisterPlayerMessageObserver(OnPlayerMessageEvent callback, string userName, string messageId)
	{
	}

	public static void UnregisterPlayerMessageObserver(OnPlayerMessageEvent callback, string messageId)
	{
	}
	
		// ============================== Player status asynchronous support ==============================

	public delegate void PlayerStatusChangedEvent(PlayerStatusMultiRequest request);

	public interface PlayerStatus
	{
		string UserName { get; }

		bool IsOnline { get; }
		bool IsInGame { get; }
		bool IsInLobby { get; }
	};

	public interface PlayerStatusMultiRequest
	{
		PlayerStatus GetPlayerStatus(string userName);

		IEnumerable<PlayerStatus> AllPlayers();

		void Cancel();

		bool IsPending { get; }
		bool IsDone { get; }
		bool HasSucceeded { get; }
		bool WasCanceled { get; }
	};
	

	public static PlayerStatusMultiRequest CreatePlayerStatusRequest(string[] userNames, PlayerStatusChangedEvent onStatusChanged)
	{
		return null;
	}
	



	// =================================== match making ===================================

	public delegate void OnServerFoundEvent(int clientJoinRequestId, int serverJoinRequestId, string ipAddress, int port);
	public delegate void OnNoServerAvailableEvent(int clientJoinRequestId);
	public delegate void OnSearchingInvitedGameEvent(int clientJoinRequestId, string invitingUserName);

	public static OnServerFoundEvent OnServerFound;
	public static OnNoServerAvailableEvent OnNoServerAvailable;
	public static OnSearchingInvitedGameEvent OnSearchingInvitedGame;



	public static bool FindServerForOnePlayer(int joinRequestId, string request, bool prefferVoiceChat)
	{
		return false;
	}
	
	public static bool FindServerForGang(int joinRequestId, string request, bool prefferVoiceChat, string[] otherPlayers)
	{
		return false;
	}

	public static bool CancelFindServer(int joinRequestId)
	{
		return false;
	}
	
	public static string MatchMakingStatistics
	{
		get
		{
			return "This version does not support match making";
		}
	}
}
