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
using LitJson;

// =============================================================================================================================
// =============================================================================================================================
public class FriendList : MonoBehaviour
{
	public readonly static string REQUEST_ID = "friends";
	public readonly static string MESSAGE_ADD = "message.add";
	public readonly static string MESSAGE_ACCEPT = "message.accept";

	public enum E_OnlineStatus
	{
		Offline,
		InLobby,
		InGame
	}

	public class PendingFriendInfo
	{
		public string Username
		{
			get { return m_Name; }
			set { m_Name = value; }
		}

		public string PrimaryKey
		{
			get { return GetPrimaryKey(); }
			set { m_PrimaryKey = value; }
		}

		public string Nickname;
		public string Username_New;
		public string Message;
		public double AddedDate;

		public string CloudCommand
		{
			get { return m_CloudCommand; }
			set { m_CloudCommand = value; }
		}

		public bool IsItRequest
		{
			get { return (string.IsNullOrEmpty(CloudCommand) == false); }
		}

		// it's here for backward compatibility
		public string m_Name;
		public string m_CloudCommand;
		string m_PrimaryKey;

		string GetPrimaryKey()
		{
			return string.IsNullOrEmpty(m_PrimaryKey) ? m_Name : m_PrimaryKey;
		}
	}

	// -------------------------------------------------------------------------------------------------------------------------	
	public class FriendInfo
	{
		public string Username
		{
			get { return m_Name; }
			set { m_Name = value; }
		}

		public string PrimaryKey
		{
			get { return GetPrimaryKey(); }
			set { m_PrimaryKey = value; }
		}

		public string Nickname;

		public int Rank
		{
			get { return PPIData != null ? PlayerPersistantInfo.GetPlayerRankFromExperience(PPIData.Params.Experience) : -1; }
		}

		public int Missions
		{
			get { return PPIData != null ? PPIData.Stats.GetPlayedTimes() : 0; }
		}

		public E_OnlineStatus OnlineStatus;
		public double LastOnlineDate;

		public PlayerPersistentInfoData PPIData = new PlayerPersistentInfoData();
		//public System.DateTime				m_LastUpdateTime;

		//private BaseCloudAction				m_GetFriendInfoAction;

		// it's here for backward compatibility
		public string m_Name;
		string m_PrimaryKey;

		string GetPrimaryKey()
		{
			return string.IsNullOrEmpty(m_PrimaryKey) ? m_Name : m_PrimaryKey;
		}
	}

	// -------------------------------------------------------------------------------------------------------------------------
	event System.EventHandler m_FriendListChanged;

	public event System.EventHandler FriendListChanged
	{
		add
		{
			if (value != null)
			{
				m_FriendListChanged -= value; // just to be sure we don't have any doubles
				m_FriendListChanged += value;
				value(this, System.EventArgs.Empty); // call delegate when registering so it recieves current state
			}
		}
		remove { m_FriendListChanged -= value; }
	}

	event System.EventHandler m_PendingFriendListChanged;

	public event System.EventHandler PendingFriendListChanged
	{
		add
		{
			if (value != null)
			{
				m_PendingFriendListChanged -= value; // just to be sure we don't have any doubles
				m_PendingFriendListChanged += value;
				value(this, System.EventArgs.Empty); // call delegate when registering so it recieves current state
			}
		}
		remove { m_PendingFriendListChanged -= value; }
	}

	// -------------------------------------------------------------------------------------------------------------------------		
	public List<FriendInfo> friends
	{
		get { return m_Friends; }
	}

	public List<PendingFriendInfo> pendingFriends
	{
		get { return m_PendingFriends; }
	}

	// -------------------------------------------------------------------------------------------------------------------------	
	List<FriendInfo> m_Friends = new List<FriendInfo>();
	List<PendingFriendInfo> m_PendingFriends = new List<PendingFriendInfo>();

	BaseCloudAction m_GetFriendListAction;

	// -------------------------------------------------------------------------------------------------------------------------		
	const int SKIP_UPDATE_TIMEOUT = 1; // in minutes
	const float ONLINE_STATUS_FREQUENCY = 60.0f; // in seconds
	System.DateTime m_LastSyncTime;
	string m_PrimaryKey;

	// -------------------------------------------------------------------------------------------------------------------------		
	LobbyClient.PlayerStatusMultiRequest m_OnlineStatusRequest;

	// =========================================================================================================================
	// === public interface ====================================================================================================
	public void RetriveFriendListFromCloud(bool inSkipTimeOutCheck = false)
	{
		// check preconditions...	
		if (CloudUser.instance.isUserAuthenticated == false)
		{
			Debug.LogError("user is not authenticated, can't fetch friends list");
			return;
		}

		if (m_GetFriendListAction != null)
			return;

		if (inSkipTimeOutCheck == false)
		{
			if (Mathf.Abs((float)(m_LastSyncTime - CloudDateTime.UtcNow).TotalMinutes) < SKIP_UPDATE_TIMEOUT)
			{
				return; // don't update friends info from cloud
			}
		}

		m_LastSyncTime = CloudDateTime.UtcNow;

		//Debug_PrepareFriendList();
		StartCoroutine(GetFriendListFromCloud_Corutine());
	}

	//..........................................................................................................................
	public bool AddNewFriend(string inPrimaryKey, string inUsername, string inNickName, string inMessage)
	{
		// check preconditions...	
		if (CloudUser.instance.isUserAuthenticated == false)
		{
			Debug.LogError("user is not authenticated, can't fetch friends list");
			return false;
		}

		FriendInfo fInfo = m_Friends.Find(f => f.PrimaryKey == inPrimaryKey);
		if (fInfo != null)
		{
			//Debug.Log("User with name " + inFriendName + " is already your friend");
			return false;
		}

		PendingFriendInfo pfInfo = m_PendingFriends.Find(f => f.PrimaryKey == inPrimaryKey);
		if (pfInfo != null)
		{
			//Debug.Log("User with name " + inFriendName + " is already in pending friend list");
			return false;
		}

		if (string.IsNullOrEmpty(inMessage) == true)
		{
			inMessage = string.Format(TextDatabase.instance[02040236], CloudUser.instance.nickName);
		}

		// create message object...
		CloudMailbox.FriendRequest msg = new CloudMailbox.FriendRequest();
		msg.m_TargetSystem = "Game.FriendList";
		msg.m_Mailbox = CloudMailbox.E_Mailbox.Global;
		msg.m_Sender = CloudUser.instance.primaryKey;
		msg.m_Username = CloudUser.instance.userName_TODO;
		msg.m_NickName = CloudUser.instance.nickName;
		msg.m_Message = inMessage; // this will not be shown...

		//GameCloudManager.mailbox.SendMessage(m_FriendName, msg);
		GameCloudManager.mailbox.SendMessage(inPrimaryKey, msg);

		// add into pending friends...
		PendingFriendInfo friend = new PendingFriendInfo();
		friend.PrimaryKey = inPrimaryKey;
		friend.Username_New = inUsername;
		friend.Nickname = inNickName;
		friend.AddedDate = GuiBaseUtils.DateToEpoch(CloudDateTime.UtcNow);
		m_PendingFriends.Add(friend);
		OnPendingFriendListChanged();
		Save();

		return true;
	}

	//..........................................................................................................................
	public void RemoveFriend(string inFriendName)
	{
		// check preconditions...	
		if (CloudUser.instance.isUserAuthenticated == false)
		{
			Debug.LogError("user is not authenticated, can't fetch friends list");
			return;
		}

		int friendIndex = m_Friends.FindIndex(f => f.PrimaryKey == inFriendName);
		if (friendIndex < 0)
		{
			//Debug.Log("User with name " + inFriendName + " is not in friendlist");
			return;
		}

		// remove friend from list...
		m_Friends.RemoveAt(friendIndex);

		// send reguest into cloud and ignore result. Friend was removed...		
		GameCloudManager.AddAction(new CancelFriendship(CloudUser.instance.authenticatedUserID, inFriendName));

		// notify recipient about change and save new friend list...
		OnFriendListChanged();
		Save();
	}

	//..........................................................................................................................		
	public void AcceptFriendRequest(string inFriendName)
	{
		//Debug.Log("AcceptFriendRequest " + inFriendName);

		// check preconditions...	
		if (CloudUser.instance.isUserAuthenticated == false)
		{
			Debug.LogError("user is not authenticated, can't Accept Friend Request");
			return;
		}

		PendingFriendInfo fInfo = m_PendingFriends.Find(f => (f.PrimaryKey == inFriendName && f.IsItRequest == true));
		if (fInfo == null)
		{
			Debug.LogError("Can't accept friend which is not in pending list");
			return;
		}

		GameCloudManager.AddAction(new SendAcceptFriendCommand(CloudUser.instance.authenticatedUserID, inFriendName, fInfo.CloudCommand));

		m_PendingFriends.Remove(fInfo);
		OnPendingFriendListChanged();
		Save();

		FriendInfo friend = new FriendInfo()
		{
			PrimaryKey = fInfo.PrimaryKey,
			Username = fInfo.Username,
			Nickname = fInfo.Nickname,
			OnlineStatus = E_OnlineStatus.Offline
		};
		m_Friends.Add(friend);

		// force redownload friend list...
		RetriveFriendListFromCloud(true);
	}

	//..........................................................................................................................	
	public void RejectFriendRequest(string inFriendName)
	{
		//Debug.Log("RejectFriendRequest " + inFriendName);

		// check preconditions...	
		if (CloudUser.instance.isUserAuthenticated == false)
		{
			Debug.LogError("user is not authenticated, can't Accept Friend Request");
			return;
		}

		PendingFriendInfo fInfo = m_PendingFriends.Find(f => f.PrimaryKey == inFriendName);
		if (fInfo == null || fInfo.IsItRequest == false)
		{
			Debug.LogError("Can't reject friend which is not in pending list");
			return;
		}

		// create message object...
		CloudMailbox.FriendRequestReject msg = new CloudMailbox.FriendRequestReject();
		msg.m_TargetSystem = "Game.FriendList";
		msg.m_Mailbox = CloudMailbox.E_Mailbox.Global;
		msg.m_Sender = CloudUser.instance.primaryKey;
		msg.m_NickName = CloudUser.instance.nickName;
		msg.m_Message = "FriendShip rejected"; // this will not be shown...

		GameCloudManager.mailbox.SendMessage(inFriendName, msg);

		m_PendingFriends.Remove(fInfo);
		OnPendingFriendListChanged();
		Save();
	}

	//..........................................................................................................................	
	public void RemovePendingFriendRequest(string inFriendName)
	{
		PendingFriendInfo fInfo = m_PendingFriends.Find(f => f.PrimaryKey == inFriendName);
		if (fInfo == null || fInfo.IsItRequest == true)
		{
			Debug.LogError("Can't remove friend which is not in pending list");
			return;
		}

		m_PendingFriends.Remove(fInfo);
		OnPendingFriendListChanged();
		Save();
	}

	//..........................................................................................................................	
	public void ProcessMessage(CloudMailbox.BaseMessage inMessage)
	{
		// add into pending friends..
		CloudMailbox.FriendRequest req = inMessage as CloudMailbox.FriendRequest;
		if (req != null)
		{
			FriendInfo fInfo = m_Friends.Find(f => f.PrimaryKey == inMessage.m_Sender);
			if (fInfo != null)
			{
				//Debug.Log("User with name " + inMessage.m_Sender + " is already your friend");
				return;
			}

			List<PendingFriendInfo> pfInfo = m_PendingFriends.FindAll(f => f.PrimaryKey == inMessage.m_Sender);
			if (pfInfo != null && pfInfo.Count > 0)
			{
				foreach (PendingFriendInfo p in pfInfo)
				{
					if (p != null && p.IsItRequest == true)
					{
						//Debug.Log("Request from this person is already in pending list");
						return;
					}
				}
			}

			PendingFriendInfo friend = new PendingFriendInfo();
			friend.PrimaryKey = req.m_Sender;
			friend.Nickname = req.m_NickName;
			friend.Username_New = req.m_Username;
			friend.AddedDate = GuiBaseUtils.DateToEpoch(inMessage.m_SendTime);
			friend.Message = inMessage.m_Message;
			friend.CloudCommand = req.m_ConfirmCommand;

			m_PendingFriends.Add(friend);
			OnPendingFriendListChanged();
			Save();
			return;
		}

		CloudMailbox.FriendRequestReject reject = inMessage as CloudMailbox.FriendRequestReject;
		if (reject != null)
		{
			// remove this friend from pending list if it is still there.
			List<PendingFriendInfo> pfInfo = m_PendingFriends.FindAll(friend => friend.PrimaryKey == inMessage.m_Sender);
			if (pfInfo != null && pfInfo.Count > 0)
			{
				foreach (PendingFriendInfo p in pfInfo)
					m_PendingFriends.Remove(p);
			}

			OnPendingFriendListChanged();
			Save();
			return;
		}

		Debug.LogError("Unknown message " + inMessage + " " + inMessage.msgType);
	}

	void OnUserAuthenticationChanged(bool state)
	{
		if (state == true)
		{
			m_PrimaryKey = CloudUser.instance.primaryKey;

			LobbyClient.RegisterPlayerMessageObserver(OnFriendMessageReceived, REQUEST_ID);

			Load();

#if UNITY_EDITOR
			// debug code...
			if (m_Friends.Count == 0)
			{
				//Debug_GenerateRandomFriends(true );
				//Save();
			}

			if (m_PendingFriends.Count == 0)
			{
				//Debug_GenerateRandomFriends(false);
				//Save();
			}
#endif
		}
		else
		{
			LobbyClient.UnregisterPlayerMessageObserver(OnFriendMessageReceived, REQUEST_ID);

			m_Friends.Clear();
			m_PendingFriends.Clear();

			m_PrimaryKey = string.Empty;
			m_LastSyncTime = new System.DateTime();
			CancelOnlineStatusRequest();
		}
	}

	// =========================================================================================================================
	// === MonoBehaviour interface =============================================================================================
	void Awake()
	{
		CloudUser.authenticationChanged += OnUserAuthenticationChanged;
	}

	void OnDestroy()
	{
		CancelOnlineStatusRequest();

		CloudUser.authenticationChanged -= OnUserAuthenticationChanged;

		m_Friends = null;
		m_PendingFriends = null;
	}

	// =========================================================================================================================
	// === internal ============================================================================================================
	void OnPendingFriendListChanged()
	{
		if (m_PendingFriendListChanged != null)
		{
			m_PendingFriendListChanged(this, System.EventArgs.Empty);
		}
	}

	void OnFriendListChanged()
	{
		if (m_FriendListChanged != null)
		{
			m_FriendListChanged(this, System.EventArgs.Empty);
		}
	}

	void OnFriendMessageReceived(string primaryKey, string messageId, string messageText)
	{
		if (messageText == MESSAGE_ADD)
		{
			GameCloudManager.mailbox.FetchMessages(true);
		}
		else if (messageText == MESSAGE_ACCEPT)
		{
			RetriveFriendListFromCloud(true);
		}
	}

	IEnumerator GetFriendListFromCloud_Corutine()
	{
		BaseCloudAction action = new GetUserData(CloudUser.instance.authenticatedUserID, CloudServices.PROP_ID_FRIENDS);
		GameCloudManager.AddAction(action);
		m_GetFriendListAction = action;

		// wait for authentication...
		while (action.isDone == false)
			yield return new WaitForSeconds(0.2f);

		if (action.isFailed == true)
		{
			Debug.LogError("Can't obtain frinds list " + action.result);
		}
		else
		{
			//Debug.Log("frinds list is here " + action.result);
			RegenerateFriendList(action.result);
		}

		m_GetFriendListAction = null;
	}

	void Save()
	{
		// delete old cache
		string rootKey = "Player[" + CloudUser.instance.primaryKey + "].FriendList";
		if (PlayerPrefs.HasKey(rootKey + ".ActiveFriends") == true)
		{
			PlayerPrefs.DeleteKey(rootKey + ".ActiveFriends");
		}
		if (PlayerPrefs.HasKey(rootKey + ".PendingFriends") == true)
		{
			PlayerPrefs.DeleteKey(rootKey + ".PendingFriends");
		}

		// store firend list
		DictionaryFile file = GetFile();
		if (file == null)
			return;

		file.SetString("active", JsonMapper.ToJson(m_Friends));
		file.SetString("pendings", JsonMapper.ToJson(m_PendingFriends));

		file.Save();
	}

	void Load()
	{
		DictionaryFile file = GetFile();
		if (file == null)
			return;

		file.Load();

		string json = file.GetString("active", "");
		m_Friends = JsonMapper.ToObject<List<FriendInfo>>(json);
		m_Friends = m_Friends ?? new List<FriendInfo>();

		// fixing old saves when we don't serialze m_PPIData...
		foreach (FriendInfo fi in m_Friends)
		{
			fi.PPIData = fi.PPIData ?? new PlayerPersistentInfoData();
		}

		// try to read from old cache first so we don't old lose pending list
		string rootKey = "Player[" + CloudUser.instance.primaryKey + "].FriendList";
		if (PlayerPrefs.HasKey(rootKey + ".PendingFriends") == true)
		{
			json = PlayerPrefs.GetString(rootKey + ".PendingFriends", "");
		}
		else
		{
			json = file.GetString("pendings", "");
		}
		m_PendingFriends = JsonMapper.ToObject<List<PendingFriendInfo>>(json);
		m_PendingFriends = m_PendingFriends ?? new List<PendingFriendInfo>();

		// get friend list from cloud
		RetriveFriendListFromCloud(true);
	}

	// =========================================================================================================================
	// === regenerate friend list ==============================================================================================
	void RegenerateFriendList(string inFriendListInJSON)
	{
		List<FriendInfo> newFriendList = new List<FriendInfo>();
		List<string> detailedFriendInfo = new List<string>();

		string[] friends = JsonMapper.ToObject<string[]>(inFriendListInJSON);

		for (int i = 0; i < friends.Length; i++)
		{
			//Debug.Log("Friend name : " + friends[i]);

			FriendInfo fInfo = m_Friends.Find(friend => friend.PrimaryKey == friends[i]);
			if (fInfo == null)
			{
				FriendInfo friend = new FriendInfo();
				friend.PrimaryKey = friends[i];
				friend.Username = friends[i];
				friend.Nickname = friends[i];
				friend.OnlineStatus = E_OnlineStatus.Offline;
				friend.LastOnlineDate = 0;
				newFriendList.Add(friend);

				detailedFriendInfo.Add(friends[i]);
			}
			else
			{
				fInfo.OnlineStatus = E_OnlineStatus.Offline;

				newFriendList.Add(fInfo);
				/*
				if( player info is too old )
				{
					detailedFriendInfo.Add(friends[i]);
				}
				*/

				// TODO :: now retrive friend PPI always.
				detailedFriendInfo.Add(friends[i]);
			}

			// remove this friend from pending list if it is still there.
			List<PendingFriendInfo> pfInfo = m_PendingFriends.FindAll(friend => friend.PrimaryKey == friends[i]);
			if (pfInfo != null && pfInfo.Count > 0)
			{
				foreach (PendingFriendInfo p in pfInfo)
					m_PendingFriends.Remove(p);
			}
		}

		if (detailedFriendInfo.Count > 0)
		{
			// TODO :: retrive detailed friend info...

			StartCoroutine(UpdateFriendsData_Corutine(detailedFriendInfo));
		}

		CreateOnlineStatusRequest();

		m_Friends = newFriendList;

		OnFriendListChanged();
		OnPendingFriendListChanged();
		Save();
	}

	IEnumerator UpdateFriendsData_Corutine(List<string> inFriends)
	{
		string jsonStr = JsonMapper.ToJson(inFriends);

		BaseCloudAction action = new QueryFriendsInfo(CloudUser.instance.authenticatedUserID, jsonStr);
		GameCloudManager.AddAction(action);

		// wait for authentication...
		while (action.isDone == false)
			yield return new WaitForSeconds(0.2f);

		if (action.isSucceeded == true)
		{
			//Debug.Log("frinds info is here " + action.result);
			ProcessFriendsDetails(action.result);
			Save();
		}
		else
		{
			Debug.LogError("Can't obtain frinds info " + action.result);
		}
	}

	void ProcessFriendsDetails(string inFriendsDetails)
	{
		JsonData[] friendDetails = JsonMapper.ToObject<JsonData[]>(inFriendsDetails);

		foreach (JsonData jsonData in friendDetails)
		{
			try
			{
				//JsonData jsonData = JsonMapper.ToObject( info );
				string primaryKey = (string)jsonData["name"];
				string ppidata = (string)jsonData["data"];
				string username = null;
				string nickname = null;

				try
				{
					username = (string)jsonData["username"];
				}
				catch
				{
					// ignore...
				}

				try
				{
					nickname = (string)jsonData["nickname"];
				}
				catch
				{
					// ignore...
				}

				FriendInfo fInfo = m_Friends.Find(friend => friend.PrimaryKey == primaryKey);
				if (fInfo != null)
				{
					fInfo.Username = string.IsNullOrEmpty(username) ? primaryKey : username;
					fInfo.Nickname = string.IsNullOrEmpty(nickname) ? primaryKey : nickname;
					fInfo.PPIData = InitPlayerDataFromStr(ppidata);
					if (fInfo.PPIData == null)
					{
						// set default data if we are not able to read original one
						Debug.LogWarning("Can't read PlayerPersistentInfoData");
						fInfo.PPIData = new PlayerPersistentInfoData();
					}
				}
				else
				{
					Debug.LogWarning("Code error this is imposible!!!");
				}

				OnFriendListChanged();
			}
			catch
			{
				Debug.Log("Mesage is not a valid JSON object");
			}
		}
	}

	PlayerPersistentInfoData InitPlayerDataFromStr(string jsonStr)
	{
		try
		{
			return JsonMapper.ToObject<PlayerPersistentInfoData>(jsonStr);
		}
		catch (JsonException e)
		{
			Debug.LogError("JSON exception caught: " + e.Message);
		}

		return null;
	}

	// =========================================================================================================================
	// === online status =======================================================================================================
	void CreateOnlineStatusRequest()
	{
		CancelOnlineStatusRequest();

		string[] friends = new string[m_Friends.Count];
		for (int idx = 0; idx < m_Friends.Count; ++idx)
		{
			friends[idx] = m_Friends[idx].PrimaryKey;
		}

		m_OnlineStatusRequest = LobbyClient.CreatePlayerStatusRequest(friends, OnOnlineStatusRequest);
	}

	void CancelOnlineStatusRequest()
	{
		CancelInvoke("CreateOnlineStatusRequest");

		if (m_OnlineStatusRequest != null)
		{
			m_OnlineStatusRequest.Cancel();
			m_OnlineStatusRequest = null;
		}
	}

	void OnOnlineStatusRequest(LobbyClient.PlayerStatusMultiRequest request)
	{
		if (request.HasSucceeded == true)
		{
			foreach (var friend in m_Friends)
			{
				LobbyClient.PlayerStatus status = request.GetPlayerStatus(friend.PrimaryKey);
				if (status == null || status.IsOnline == false)
				{
					friend.OnlineStatus = E_OnlineStatus.Offline;
				}
				else
				{
					friend.OnlineStatus = status.IsInGame == true ? E_OnlineStatus.InGame : E_OnlineStatus.InLobby;
					friend.LastOnlineDate = GuiBaseUtils.DateToEpoch(CloudDateTime.UtcNow);
				}
			}

			OnFriendListChanged();
		}

		Invoke("CreateOnlineStatusRequest", ONLINE_STATUS_FREQUENCY);
	}

	// =========================================================================================================================
	// === debug ===============================================================================================================
#if UNITY_EDITOR
	void Debug_GenerateRandomFriends(bool inActive)
	{
		if (inActive == true)
		{
			m_Friends.Clear();
			for (int i = 0; i < 10; i++)
			{
				FriendInfo friend = new FriendInfo();
				friend.PrimaryKey = MFDebugUtils.GetRandomString(Random.Range(5, 12));
				//friend.m_Level 		= Random.Range(1,20);
				//friend.m_Missions	= Random.Range(0,200);
				friend.LastOnlineDate = 0; //MiscUtils.RandomValue( new string[] {"unknown", "yesterday", "tomorrow"});
				m_Friends.Add(friend);
			}
		}
		else
		{
			m_PendingFriends.Clear();
			for (int i = 0; i < 10; i++)
			{
				PendingFriendInfo friend = new PendingFriendInfo();
				friend.PrimaryKey = MFDebugUtils.GetRandomString(Random.Range(5, 12));
				friend.AddedDate = GuiBaseUtils.DateToEpoch(CloudDateTime.UtcNow);

				if (Random.Range(0, 2) == 0)
				{
					// create dummy message, for testing gui behavior...
					friend.CloudCommand = MFDebugUtils.GetRandomString(Random.Range(512, 512));
				}

				m_PendingFriends.Add(friend);
			}
		}
	}

	void Debug_PrepareFriendList()
	{
//		CloudUser.instance.CreateNewUser("alex_01", CloudServices.CalcPasswordHash("alex"), "alex_01", "none@test.test", false);
//		CloudUser.instance.CreateNewUser("alex_02", CloudServices.CalcPasswordHash("alex"), "alex_02", "none@test.test", false);
//		CloudUser.instance.CreateNewUser("alex_03", CloudServices.CalcPasswordHash("alex"), "alex_03", "none@test.test", false);
//		CloudUser.instance.CreateNewUser("alex_04", CloudServices.CalcPasswordHash("alex"), "alex_04", "none@test.test", false);
//		CloudUser.instance.CreateNewUser("alex_05", CloudServices.CalcPasswordHash("alex"), "alex_05", "none@test.test", false);
//		CloudUser.instance.CreateNewUser("alex_06", CloudServices.CalcPasswordHash("alex"), "alex_06", "none@test.test", false);
//		CloudUser.instance.CreateNewUser("alex_07", CloudServices.CalcPasswordHash("alex"), "alex_07", "none@test.test", false);
//		CloudUser.instance.CreateNewUser("alex_08", CloudServices.CalcPasswordHash("alex"), "alex_08", "none@test.test", false);
//		CloudUser.instance.CreateNewUser("alex_09", CloudServices.CalcPasswordHash("alex"), "alex_09", "none@test.test", false);
//		CloudUser.instance.CreateNewUser("alex_10", CloudServices.CalcPasswordHash("alex"), "alex_10", "none@test.test", false);
//
//		string jsonStr = JsonMapper.ToJson(new string [] {"alex_01", "alex_02", "alex_03", "alex_04", "alex_05", "alex_06", "alex_07", "alex_08", "alex_09", "alex_10", } );
//		GameCloudManager.AddAction( new SetUserData(CloudUser.instance.authenticatedUserID, CloudServices.PROP_ID_FRIENDS, jsonStr) );
	}
#endif

	DictionaryFile GetFile()
	{
		DictionaryFile file = null;

		if (string.IsNullOrEmpty(m_PrimaryKey) == false)
		{
			string filename = string.Format("users/{0}/.friendlist", GuiBaseUtils.GetCleanName(m_PrimaryKey));
			file = new DictionaryFile(filename);
		}

		return file;
	}
}
