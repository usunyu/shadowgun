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

//#define GEIGER_TEST

using UnityEngine;
using uLink;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Net;

/*
 * 
 * client workflow
 * connect to server
 * when connected , server will send mission name
 * when mission is loaded ask for spawn
 */

public class Client : uLink.MonoBehaviour
{
	public enum E_MessageType
	{
		Kill,
		HeadShot,
		KillAssist,
		Turret,
		Spider,
		ZoneNeutral,
		ZoneControl,
		ZoneDefended,
		ZoneAttacked,
		Rank,
		Unlock,
		Win,
		Lost,
		Ammo,
		Heal,
		ExclusiveKill,
	}

	[System.Serializable]
	public class NetworkSettings
	{
		/** The server drops a client connection automaticaly when there was no packet received in given interval [seconds] */
		public int TimeoutDelay = 60;
	}

	public enum ClientGameState
	{
		WaitingToStart,
		SpawnMenu,
		WaitingForSpawn,
		Running,
		WaitingToRestart,
	}

	public class GameInfo
	{
		public class DeathMatchInfo
		{
			public int KillsLimit;
			public float TimeLimit;
			public int RestTimeSeconds;
		}

		public class ZoneControlInfo
		{
			public Dictionary<E_Team, int> TeamScore = new Dictionary<E_Team, int>(TeamComparer.Instance);
		}

		public class CaptureFlagInfo
		{
			public Dictionary<E_Team, int> TeamScore = new Dictionary<E_Team, int>(TeamComparer.Instance);
			public float TimeLimit;
			public int RestTimeSeconds;
		}

		public ClientGameState State = ClientGameState.WaitingToStart;

		public E_MPGameType GameType;
		public DeathMatchInfo DMInfo = new DeathMatchInfo();
		public ZoneControlInfo ZCInfo = new ZoneControlInfo();
		public int RoundPerLevel;
		public string LevelName;
		public int Round;
		public int CurrentTime;
		public float SpawnTimeProtection;

		public GameInfo()
		{
			ZCInfo.TeamScore.Add(E_Team.Bad, 0);
			ZCInfo.TeamScore.Add(E_Team.Good, 0);
		}

		public E_Team GetWinnerTeam()
		{
			if (GameType == E_MPGameType.ZoneControl)
			{
				if (ZCInfo.TeamScore[E_Team.Good] == 0)
					return E_Team.Bad;
				else if (ZCInfo.TeamScore[E_Team.Bad] == 0)
					return E_Team.Good;
			}
			return E_Team.None;
		}
	};

	[System.Serializable]
	public class ClientSounds
	{
		[System.Serializable]
		public class UpgradesSounds
		{
			public AudioClip Rank;
			public AudioClip Money;
		}

		[System.Serializable]
		public class DominationSounds
		{
			public AudioClip FlagNeutralized;
			public AudioClip FlagLost;
			public AudioClip FlagOwned;
		}

		[System.Serializable]
		public class SpawnSounds
		{
			public AudioClip Tick;
			public AudioClip Spawn;
			public AudioClip Death;
		}

		[System.Serializable]
		public class ScoreSounds
		{
			public AudioClip FirstPlace;
			public AudioClip SecondPlace;
			public AudioClip ThirdPlace;
		}

		[System.Serializable]
		public class DetectorSounds
		{
			public AudioClip AgentDetected;
			public AudioClip MineDetected;
		}

		public UpgradesSounds Upgrades = new UpgradesSounds();
		public DominationSounds Domination = new DominationSounds();
		public SpawnSounds Spawn = new SpawnSounds();
		public ScoreSounds Score = new ScoreSounds();
		public DetectorSounds DetectorSnd = new DetectorSounds();

		public AudioClip CombatMessage;
		public AudioClip CombatMessageGold;

		public AudioClip SoundDisabled;
	}

	[SerializeField] ClientSounds Sounds;

	public GameInfo GameState = new GameInfo();

	public static Client Instance { get; private set; }
	public static int TimeToRespawn { get; private set; }

	AudioSource Audio;
	//float guiUpdateTime = 0;

	uLink.NetworkBufferedRPC[] BufferedRPCs;

	public delegate void SpawnDelegate();

	[SerializeField] NetworkSettings ClientSetup;
	[SerializeField] public NetUtils.ConnectionQuality TestingConnectionQuality;
	bool LastShowNetworkStatistics;
	public bool ShowNetworkStatistics;

	// debug
#if GEIGER_TEST
	private int m_GeigerTimer = 0;
	
	[SerializeField]
	private AudioClip m_GeigerSound;
#endif // GEIGER_TEST	

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
		Printf("Awake");

		if (Instance != null)
		{
			Destroy(gameObject);
			return;
		}

		DontDestroyOnLoad(gameObject);

		gameObject.SetActive(true);

		Audio = gameObject.AddComponent<AudioSource>();
#if UNITY_STANDALONE_WIN
#else
		Application.logMessageReceived += CaptureLog;
#endif
		Instance = this;
	}

	void Start()
	{
		Printf("start");
	}

	void OnDestroy()
	{
		Printf("Destroy");
	}

	public void Update()
	{
#if GEIGER_TEST
		if ( BuildInfo.Version.Major.Stage != BuildInfo.Stage.Release )
		{
			int currentTime = (int)uLink.Network.time;
			
			if( currentTime > m_GeigerTimer )
			{
				m_GeigerTimer = currentTime;
				
				if( null != m_GeigerSound )
				{
					Audio.PlayOneShot( m_GeigerSound );
				}
			}
		}
#endif // GEIGER_TEST		

		/*if(Time.timeSinceLevelLoad > guiUpdateTime)
		{
			guiUpdateTime = Time.timeSinceLevelLoad + 2.0f;
			if(GuiSpawnScreen.Instance)
				GuiSpawnScreen.Instance.UpdateStats(ScoreBoard);
			
			if(GuiScoreScreen.Instance)
				GuiScoreScreen.Instance.UpdateStats(ScoreBoard);
		}*/
	}

	void uLink_OnPreBufferedRPCs(uLink.NetworkBufferedRPC[] rpcs)
	{
		foreach (uLink.NetworkBufferedRPC rpc in rpcs)
		{
			rpc.DontExecuteOnConnected();
		}
		BufferedRPCs = rpcs;
	}

	public void LateUpdate()
	{
		if (Debug.isDebugBuild)
		{
			if (ShowNetworkStatistics != LastShowNetworkStatistics)
			{
				if (ShowNetworkStatistics)
				{
					NetUtils.DisplayNetworkStatistics();
				}
				else
				{
					NetUtils.HideNetworkStatistics();
				}

				LastShowNetworkStatistics = ShowNetworkStatistics;
			}
		}
	}

	public delegate void ConnectToServerFailedDelegate(uLink.NetworkConnectionError reason);
	ConnectToServerFailedDelegate OnConnectToServerFailed = null;

	void PrepareNetworkConnection(ConnectToServerFailedDelegate onConnectToServerFailed)
	{
		OnConnectToServerFailed = onConnectToServerFailed;

		//TODO remove or keep it: this line returns the old timer function which  have impact to uLink's network.Time implementation
		// maybe it will solve a lagging bug
		// Update1: yes keep it for now. It is not going to solve all the problems but there was a serious problem detected in the default timeMeasurementFunction (StopWatch)
		// Update2: The stopwatch timer implementation has been fixed
		// Update3: batch send re-enabled - let's turn the optimization on again as the new uLink fixed the slow packet re-send
		// Update4: batch send disabled back again as it causes some data serialization issues (throwing exception during RPC calls). Still not fixed in uLink 1.5.2
		// Update5: we are enabling it back again because a patch in uLink 1.5.3 Lina: "Fixed reported issue "Trying to read past buffer size" during serialization, which previously was also made more evident by batchSendAtEndOfFrame"
		// Update6: batch send still not working (same issue as before; appears during several fast consecutive Knockdowns), so disabled yet again!
		uLink.Network.config.timeMeasurementFunction = uLink.NetworkTimeMeasurementFunction.TickCount;
		uLink.Network.config.batchSendAtEndOfFrame = false;

		float timeBetweenPings = 1.0f;

		Printf("Changing uLink.Network.config.timeBetweenPings from " + uLink.Network.config.timeBetweenPings + " to " + timeBetweenPings +
			   " seconds.");

		uLink.Network.config.timeBetweenPings = timeBetweenPings;
	}

	public void ConnectToServer(uLink.HostData hostData, int joinRequestId, ConnectToServerFailedDelegate onConnectToServerFailed)
	{
		Printf("Connecting to lan server");

		PrepareNetworkConnection(onConnectToServerFailed);

		uLink.Network.Connect(hostData.internalEndpoint,
							  "",
							  NetUtils.CurrentVersion.ToString(),
							  CloudUser.instance.primaryKey,
							  CloudUser.instance.nickName,
							  (int)Application.platform,
							  joinRequestId);

		uLinkStatisticsGUI.HACK_ConnectedServerEndpoint = hostData.internalEndpoint;
	}

	public void ConnectToServer(IPEndPoint EndPoint, int joinRequestId, ConnectToServerFailedDelegate onConnectToServerFailed)
	{
		Printf("Connecting to lan server");

		PrepareNetworkConnection(onConnectToServerFailed);

		uLink.Network.Connect(EndPoint,
							  "",
							  NetUtils.CurrentVersion.ToString(),
							  CloudUser.instance.primaryKey,
							  CloudUser.instance.nickName,
							  (int)Application.platform,
							  joinRequestId);

		uLinkStatisticsGUI.HACK_ConnectedServerEndpoint = EndPoint;
	}

	public void uLink_OnConnectedToServer(System.Net.EndPoint server)
	{
		Printf("Connected to server");

		OnConnectToServerFailed = null;

		if (Debug.isDebugBuild)
		{
			// the timeout interval needs to be extended for debug purpose (to avoid disconnect the game network when debugging a code)
			uLink.Network.config.timeoutDelay = 10*60;

			Debug.Log("Debug: The network timeout value has been set to " + uLink.Network.config.timeoutDelay + " seconds");

			NetUtils.SetConnectionQualityEmulation(TestingConnectionQuality);

			if (ShowNetworkStatistics)
			{
				NetUtils.DisplayNetworkStatistics();
				LastShowNetworkStatistics = ShowNetworkStatistics;
			}
		}
		else
		{
			uLink.Network.config.timeoutDelay = ClientSetup.TimeoutDelay;
		}
	}

	public void uLink_OnFailedToConnect(uLink.NetworkConnectionError error)
	{
		Printf("Unable to connect to the server: " + error);

		if (OnConnectToServerFailed != null)
			OnConnectToServerFailed(error);
	}

	[uSuite.RPC]
	void LoadMission(string name)
	{
		Printf("Loading level : " + name);

		uLink.Network.isMessageQueueRunning = false;

		uLink.Network.config.timeoutDelay = 120;

		Game.Instance.LoadLevel(name);
	}

	public void OnMissionLoaded(string scene)
	{
		uLink.Network.config.timeoutDelay = ClientSetup.TimeoutDelay;

		Printf("level was loaded " + scene);

		if (BufferedRPCs != null)
		{
			foreach (uLink.NetworkBufferedRPC rpc in BufferedRPCs) // TODO Hack.
				rpc.ExecuteNow();

			BufferedRPCs = null;
		}

		uLink.Network.isMessageQueueRunning = true;

		StartSpawnMenu();

		//force play sounds: in Standalone players the ambient sounds in the level are muted for some obscure reason, this is to fix it
		ForcePlaySounds();
	}

	//
	void ForcePlaySounds()
	{
		StartCoroutine(_ForcePlaySounds());
	}

	//force play sounds: in Standalone players the ambient sounds in the level are muted for some obscure reason, this is to fix it
	IEnumerator _ForcePlaySounds()
	{
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame(); //we have to wait for 4 frames.
		yield return new WaitForSeconds(0.1f); //just for safety (maybe on some systems we'll have to wait a bit longer)

		AudioSource[] sounds = FindObjectsOfType(typeof (AudioSource)) as AudioSource[];

//		Debug.Log("ForcePlaySounds, time=" + Time.timeSinceLevelLoad + ", sounds.Length=" + sounds.Length);

		if (sounds != null && sounds.Length > 0)
		{
			for (int i = 0; i < sounds.Length; i++)
			{
				if (sounds[i].playOnAwake && !sounds[i].mute && sounds[i].clip != null)
				{
					sounds[i].Play();
//					Debug.Log("ForcePlaySounds, sounds[" + i + "]=" + sounds[i].name);
				}
			}
		}
	}

	void StartSpawnMenu()
	{
		Printf("Start spawn menu ");

		GameState.State = ClientGameState.SpawnMenu;

		GuiFrontendIngame.ShowSpawnMenu( /*GameState.GameType, SendRequestForSpawn, GameState.LevelName*/);
	}

	public void StartSpawnMenu(float delay)
	{
		Invoke("StartSpawnMenu", delay);
	}

	public void SendRequestForSpawn()
	{
		GameState.State = ClientGameState.WaitingForSpawn;

		//check equip before spawn:
		BaseCloudAction action = GuiShopUtils.ValidateEquip();
		if (action != null)
		{
			Debug.Log("Fixing Equip before spawn");
			GameCloudManager.AddAction(action);
		}

		StartCoroutine(WaitForCloudManagerForSpawn());
	}

	IEnumerator WaitForCloudManagerForSpawn()
	{
		while (GameCloudManager.isBusy == true)
		{
			//Debug.Log(">>>> GameCloudManager is busy");
			yield return new WaitForSeconds(0.2f);
		}

		if (true != Client.Instance.IsReadyForSpawn()) // client should have at least one weapon
		{
			StartSpawnMenu();
			GuiFrontendIngame.ShowMessageBox(0501004, 0501005);
		}
		else
		{
			//Debug.Log(">>>> GO GO GO");

			PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();

			Printf("SendRequestForSpawn : " + ppi.Name + " On Zone " + ppi.ZoneIndex);

			if (null == networkView)
			{
				Debug.LogError("null networkview");
			}

			if (null == ppi)
			{
				Debug.LogError("null local ppi");
			}

			networkView.RPC("RequestForSpawn", uLink.RPCMode.Server, ppi.Player, ppi.ZoneIndex);
			GameState.State = ClientGameState.Running;
		}
	}

	public void SendRequestForTeamSwitch(E_Team team)
	{
		PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();

		networkView.RPC("RequestForTeamSwitch", uLink.RPCMode.Server, ppi.Player, team);
	}

	public bool IsReadyForSpawn()
	{
		PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();

		return ppi.EquipList.Weapons.Count > 0 ? true : false;
	}

	[uSuite.RPC]
	protected void RoundEnded(RoundFinalResult results)
	{
		CancelInvoke();

		GameState.State = ClientGameState.WaitingToRestart;

		PlayerPersistantInfo localPPI = PPIManager.Instance.GetLocalPlayerPPI();

		GuiFrontendIngame.ShowFinalMenu( /*GameState.GameType, true*/);

		int place = results.Place;

		if ((GameState.GameType == E_MPGameType.ZoneControl) && (localPPI.Team == GameState.GetWinnerTeam()))
			Audio.PlayOneShot(Sounds.Score.FirstPlace);
		else if (place == 0)
			Audio.PlayOneShot(Sounds.Score.FirstPlace);
		else if (place == 1)
			Audio.PlayOneShot(Sounds.Score.SecondPlace);
		else if (place == 2)
			Audio.PlayOneShot(Sounds.Score.ThirdPlace);
		else if (Mission.Instance && Mission.Instance.RoundEnd)
			Audio.PlayOneShot(Mission.Instance.RoundEnd);

		PlayerPersistantInfo ppi = PPILocalStats.GetBestVictim(localPPI, ref results.Prey.KilledByMe);

		if (ppi != null)
		{
			results.Prey.PrimaryKey = ppi.PrimaryKey;
			results.Prey.KilledMe = PPILocalStats.GetKills(ppi, localPPI);
		}

		ppi = PPILocalStats.GetBestKiller(localPPI, ref results.Nightmare.KilledMe);

		if (ppi != null)
		{
			results.Nightmare.PrimaryKey = ppi.PrimaryKey;
			results.Nightmare.KilledByMe = PPILocalStats.GetKills(localPPI, ppi);
		}

		foreach (var score in results.PlayersScore)
		{
			ppi = PPIManager.Instance.GetPPIList().Find(obj => obj.PrimaryKey == score.PrimaryKey);
			if (ppi != null)
				score.Experience = ppi.Experience;
		}

		results.MapName = GameState.LevelName;

		Game.Instance.LastRoundResult = results;
	}

	[uSuite.RPC]
	protected void MissionReset()
	{
		Mission.Instance.Reset();
	}

	[uSuite.RPC]
	protected void ShowRoundEarnings()
	{
		//GuiFrontendIngame.ShowFinalMenu("PlayerEarnings");

		// this is one of the best moments to recalibrate the network time	
		RecalibrateNetworkTime();
	}

	[uSuite.RPC]
	protected void NewRound()
	{
		Printf("start new round");

		ConsoleMsg(TextDatabase.instance[MPMessages.NewRound]);

		Game.Instance.LastRoundResult = null;

		StartSpawnMenu();
	}

	[uSuite.RPC]
	protected void UpdateDeathMatchRestTime(int TimeSeconds)
	{
		GameState.DMInfo.RestTimeSeconds = TimeSeconds;
	}

	[uSuite.RPC]
	protected void OnSpawningCanceled()
	{
		//if( GuiFrontendIngame.IsInState( GuiFrontendIngame.E_MenuState.Spectator ) )
		{
			StartSpawnMenu();
		}
	}

	[uSuite.RPC]
	protected void UpdateTime2Respawn(byte time)
	{
		TimeToRespawn = time;

		if (TimeToRespawn < 6 && PPIManager.Instance.GetLocalPPI().State == PlayerPersistantInfo.E_State.WaitingForSpawn)
			PlaySoundSpawnTick();
	}

	[uSuite.RPC]
	void ClientTeamSwitched(E_Team team)
	{
	}

	[uSuite.RPC]
	public void ConsoleMsg(string msg)
	{
		if (HudComponentConsole.Instance != null)
			HudComponentConsole.Instance.ShowMessage(msg, Color.white);
	}

	[uSuite.RPC]
	protected void GoldMsg(short gold)
	{
		GuiHUD.Instance.ShowCombatText(Client.E_MessageType.ExclusiveKill, string.Format(TextDatabase.instance[00502077], gold));
	}

	[uSuite.RPC]
	protected void CombatMsg(Client.E_MessageType type, short exp, short cash)
	{
		if (exp == 0 && cash == 0)
			GuiHUD.Instance.ShowCombatText(type, "");
		else if (exp == 0)
			GuiHUD.Instance.ShowCombatText(type, cash.ToString() + TextDatabase.instance[0500921]);
		else if (cash == 0)
			GuiHUD.Instance.ShowCombatText(type, exp.ToString() + TextDatabase.instance[0500920]);
		else
			GuiHUD.Instance.ShowCombatText(type,
										   exp.ToString() + TextDatabase.instance[0500920] + "/" + cash.ToString() + TextDatabase.instance[0500921]);
	}

	[uSuite.RPC]
	protected void UpdateGameInfoDM(E_MPGameType type,
									int KillsLimit,
									float TimeLimit,
									int RoundPerLevel,
									string levelName,
									int round,
									float spawnTimeProtection)
	{
		Printf("updating game info");
		GameState.GameType = type;
		GameState.DMInfo.KillsLimit = KillsLimit;
		GameState.DMInfo.TimeLimit = TimeLimit;
		GameState.RoundPerLevel = RoundPerLevel;
		GameState.LevelName = levelName;
		GameState.Round = round;
		GameState.SpawnTimeProtection = spawnTimeProtection;
	}

	[uSuite.RPC]
	protected void UpdateGameInfoZC(E_MPGameType type,
									int GoodTickets,
									int BadTickets,
									int RoundPerLevel,
									string levelName,
									int round,
									float spawnTimeProtection)
	{
		Printf("updating game info");
		GameState.GameType = type;
		GameState.ZCInfo.TeamScore[E_Team.Bad] = BadTickets;
		GameState.ZCInfo.TeamScore[E_Team.Good] = GoodTickets;
		GameState.RoundPerLevel = RoundPerLevel;
		GameState.LevelName = levelName;
		GameState.Round = round;
		GameState.SpawnTimeProtection = spawnTimeProtection;
	}

	[uSuite.RPC]
	protected void UpdateTeamScore(short good, short bad)
	{
		if (GameState.GameType == E_MPGameType.ZoneControl)
		{
			GameState.ZCInfo.TeamScore[E_Team.Bad] = bad;
			GameState.ZCInfo.TeamScore[E_Team.Good] = good;

			if (Game.Instance.GameLog)
				Debug.Log(E_Team.Bad + " " + GameState.ZCInfo.TeamScore[E_Team.Bad] + " : " + E_Team.Good + " " +
						  GameState.ZCInfo.TeamScore[E_Team.Good]);
		}
	}

	/// <summary>
	/// Notification from the match-making that the game server had become invisible (in other words is closed for new players)
	/// </summary>
	[uSuite.RPC]
	protected void ServerInvisible()
	{
	}

	void OnApplicationPause(bool pause)
	{
		if (pause == false)
		{
		}
	}

	[uSuite.RPC]
	void RankUp(int newRank)
	{
		GuiHUD.Instance.ShowCombatText(E_MessageType.Rank, newRank.ToString());
		PlaySoundRank();
	}

	[uSuite.RPC]
	void PlayRoundStartSound()
	{
		if (Mission.Instance.RoundStart)
			Audio.PlayOneShot(Mission.Instance.RoundStart);
	}

	[uSuite.RPC]
	void PlaySoundDisabled()
	{
		if (null != Sounds.SoundDisabled)
		{
			Audio.PlayOneShot(Sounds.SoundDisabled);
		}
	}

	void CaptureLog(string condition, string stacktrace, LogType type)
	{
	}

	void uLink_OnDisconnectedFromServer(uLink.NetworkDisconnection mode)
	{
		Printf("Disconnected " + mode);

		CancelInvoke();
		Destroy(this.gameObject);

		// we don't want to fetch local ppi here
		// it will be done later after round results display
		//PPIManager.Instance.FetchPPIFromCloud();

		Game.Instance.LoadMainMenu();
	}

	void uLink_OnSerializeNetworkView(uLink.BitStream stream, uLink.NetworkMessageInfo info)
	{
		GameState.CurrentTime = stream.Read<int>();
		//Debug.Log("uLink_OnSerializeNetworkView, time: " + GameState.CurrentTime);
	}

	public void GetAvailableZones(E_Team forTeam, ref List<int> zonesIndex)
	{
		zonesIndex.Clear();

		List<ZoneControlFlag> zones = (Mission.Instance.GameZone as GameZoneZoneControl).Zones;

		for (int i = 0; i < zones.Count; i++)
		{
			if (zones[i].FlagOwner == forTeam)
				zonesIndex.Add(i);
		}
	}

	public static void Printf(string s)
	{
		if (Game.Instance && Game.Instance.GameLog)
			print(Time.timeSinceLevelLoad + " Client - " + s);
	}

	public static void DisconnectFromServer()
	{
		uLink.Network.Disconnect();
	}

	[uSuite.RPC]
	void RecalibrateNetworkTime()
	{
		uLink.Network.ResynchronizeClock(1.000f);
	}

	#region  PlaySounds....

	public void PlaySoundMoney()
	{
		Audio.PlayOneShot(Sounds.Upgrades.Money);
	}

	public void PlaySoundRank()
	{
		Audio.PlayOneShot(Sounds.Upgrades.Rank);
	}

	public void PlaySoundFlagLost()
	{
		Audio.PlayOneShot(Sounds.Domination.FlagLost);
	}

	public void PlaySoundFlagNeutral()
	{
		Audio.PlayOneShot(Sounds.Domination.FlagNeutralized);
	}

	public void PlaySoundFlagOwned()
	{
		Audio.PlayOneShot(Sounds.Domination.FlagOwned);
	}

	public void PlaySoundPlayerDie()
	{
		Audio.PlayOneShot(Sounds.Spawn.Death);
	}

	public void PlaySoundPlayerSpawn()
	{
		Audio.PlayOneShot(Sounds.Spawn.Spawn);
	}

	public void PlaySoundSpawnTick()
	{
		Audio.PlayOneShot(Sounds.Spawn.Tick);
	}

	public void PlaySoundAgentDetected()
	{
		Audio.PlayOneShot(Sounds.DetectorSnd.AgentDetected);
	}

	public void PlaySoundMineDetected()
	{
		Audio.PlayOneShot(Sounds.DetectorSnd.MineDetected);
	}

	public void PlaySoundCombatMessage()
	{
		Audio.PlayOneShot(Sounds.CombatMessage);
	}

	public void PlaySoundCombatMessageGold()
	{
		Audio.PlayOneShot(Sounds.CombatMessageGold);
	}

	[System.Serializable]
	public class DominationSounds
	{
		public AudioClip FlagNeutralized;
		public AudioClip FlagLost;
		public AudioClip FlagOwned;
	}

	[System.Serializable]
	public class SpawnSounds
	{
		public AudioClip Tick;
		public AudioClip Spawn;
		public AudioClip Death;
	}

	#endregion
}
