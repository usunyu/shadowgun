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

#if !DEADZONE_CLIENT

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using uLink;


public class Server : uLink.MonoBehaviour
{
    [System.Serializable]
    public class ServerInfo
    {
        public int MaxConnections = 8;

        public bool CleanupAfterPlayers = true;
        public bool RegisterHost = true;

		/** The server drops a client connection automaticaly when there was no packet received in given interval [seconds] */
		public int TimeoutDelay = 60;
    }
	
	public delegate void OnLevelLoadHandler( string name );	
	public delegate void OnLevelLoadedHandler( string name );
	public delegate void OnPlayerConnectedHandler( uLink.NetworkPlayer player, int joinRequestId );
	public delegate void OnPlayerDisconnectedHandler( uLink.NetworkPlayer player );
	public delegate void OnRoundEndedHandler();
	public delegate void OnPreparingNewRoundHandler();
	public delegate void OnRoundStartedHandler();
	
	public OnLevelLoadHandler			OnLevelLoad;
	public OnLevelLoadedHandler			OnLevelLoaded;
	public OnPlayerConnectedHandler		OnPlayerConnected;
	public OnPlayerDisconnectedHandler	OnPlayerDisconnected;
	public OnRoundEndedHandler			OnRoundEnded;
	public OnPreparingNewRoundHandler	OnPreparingNewRound;
	public OnRoundStartedHandler		OnRoundStarted;

	[SerializeField]
	private ServerInfo ServerSetup = new ServerInfo();
	[SerializeField]
	NetUtils.ConnectionQuality	TestingConnectionQuality;
		
	//Game session related parameters
	[SerializeField]
	int LevelsPerGameSession = 9999;
	int LevelOrderNumber;
	
	public PlayerSlotReservation	SlotReservation = null;

	private bool ShowNetworkStatisticsEnabled = true;
	private bool LastShowNetworkStatistics;
	public bool ShowNetworkStatistics;
	
	public E_MPGameType MultiplayerType = E_MPGameType.None;

	public GameInfo GameInfo = new GameInfo();

	public static Server Instance { get; private set; }

	int ExplicitServerPort = 0;
	// keep track of original time set 
	private int TimeToRespawnOriginal = 0;
	
	// current time left before respawn wave
	private int TimeToRespawnCurrent = 0;

	private int LastRestTimeUpdated = 0;
	
	private bool IgnoreDebugSettings = false;
	
	private bool m_EndRoundInProgress = false;
	
	private float m_HealthyTimeWithoutSpawn;
	private float m_HealthyTimeWithoutMovement;

	private bool m_UserInput;
	
	uLink.NetworkView	m_NetworkView;

	//
    public void Awake()
    {
		LogProcessID();
		
        DontDestroyOnLoad(gameObject);

        Instance = this;
		
		// The member m_NetworkView must be set before the uLink.Network.InitializeServer function is called.
		// The problem here is that InitializeServer calls uLink_OnServerInitialized which starts the new game and the m_NetworkView member is needed there.
		m_NetworkView = networkView;

		if (UnityParkConfiguration.HasLicenseKey)
			uLink.Network.licenseKey = UnityParkConfiguration.LicenseKey;
		
		// Interprets command line arguments
		SetupServerFromCommandLine();
		
		SettingsCloudSync.GetInstance();

        QualitySettings.masterTextureLimit = 4;	

		if (Debug.isDebugBuild && !IgnoreDebugSettings)
		{
			// the timeout interval needs to be extended for debug purpose (to avoid disconnect the game network when debugging a code)
			uLink.Network.config.timeoutDelay = 10 * 60;
			
			Debug.Log( "Debug: The network timeout value has been set to " + uLink.Network.config.timeoutDelay + " seconds" );
			
			NetUtils.SetConnectionQualityEmulation( TestingConnectionQuality );
		}
		else
		{
			uLink.Network.config.timeoutDelay = ServerSetup.TimeoutDelay;
		}

		if (ShowNetworkStatisticsEnabled)
		{
			NetUtils.DisplayNetworkStatistics();
			LastShowNetworkStatistics = ShowNetworkStatistics = true;
		}
		
		Debug.Log("Server version " + NetUtils.CurrentVersion);
		
		// Update2: The stopwatch timer implementation has been fixed
		// Update3: batch send re-enabled - let's turn the optimization on again as the new uLink fixed the slow packet re-send
		//...
		// Update6: batch send still not working (same issue as before; appears during several fast consecutive Knockdowns), so disabled yet again!
		
		uLink.Network.config.timeMeasurementFunction = uLink.NetworkTimeMeasurementFunction.TickCount;
		uLink.Network.config.batchSendAtEndOfFrame = false;
		uLink.MasterServer.comment = NetUtils.CurrentVersion.ToString();
		uLink.NetworkConnectionError err;

		if (ExplicitServerPort != 0)
		{
			err = uLink.Network.InitializeServer(ServerSetup.MaxConnections, ExplicitServerPort);
		}
		else
		{
			err = uLink.Network.InitializeServer(ServerSetup.MaxConnections, NetUtils.ServerPortMin, NetUtils.ServerPortMax);
		}

		if (err != uLink.NetworkConnectionError.NoError)
		{
			Debug.LogError( "Cannot establish the server, err=" + err.ToString() );
			Application.Quit();
		}
    }
	
	/// <summary>
	/// This function logs the PID into the server log. This information is used later by an erlang script to monitor the server.
	/// </summary>
	private void LogProcessID()
	{
		System.Console.WriteLine("OS_PID=" + System.Diagnostics.Process.GetCurrentProcess().Id);
		System.Console.Out.Flush();
	}
	
    private void uLink_OnServerInitialized()
    {
        Printf("Server successfully started on port " + uLink.Network.listenPort + ", network version is " + NetUtils.CurrentVersion );
        
		if ( MultiplayerType != E_MPGameType.None )
		{
			StartNewGame( MultiplayerType );
		}
		else
		{
			// the server will be configured by match-making / lobby
		}
    }

	public void StartNewGame(E_MPGameType gameType, bool randomMap = false)
	{
		LevelOrderNumber = 0;
		
		if ( OnPreparingNewRound != null )
			OnPreparingNewRound();
		
		GameInfo.PrepareForGame(gameType, randomMap);
        StartNewLevel();
	}
	
    void StartNewLevel()
    {
        //Debug.Log("Loading next level " + nextLevel);
        string name  = GameInfo.CurrentLevel;
		
		if (OnLevelLoad != null)
			OnLevelLoad( name );

		LevelOrderNumber++;
		
        Server.Printf("Loading level : " + name + " " + GameInfo.GameType + "order number=" + LevelOrderNumber);
		
		
        Game.Instance.LoadLevel(name);

        GameInfo.CurrentRound = 0;
        GameInfo.State = GameInfo.GameState.Loading;

        UpdateGameInfoOnClients();
		
        m_NetworkView.RPC("LoadMission", uLink.RPCMode.Others, name);
    }

    public void OnMissionLoaded(string scene)
    {
        Printf("level was loaded " + scene);
		
		if ( OnLevelLoaded != null )
			OnLevelLoaded( scene );

		WaitForPlayers();

		uLink.MasterServer.gameName = scene;
		uLink.MasterServer.gameMode = GameInfo.GetGameTypeName(GameInfo.GameType);
		
        //PrepareForNewRound();
    }
	
	private void WaitForPlayers()
	{
		StopCoroutine( "WaitForPlayers" );
		
		StartCoroutine( WaitForPlayersCoroutine() );
	}
	
	private IEnumerator WaitForPlayersCoroutine()
	{
		while( true )
		{
			if( PPIManager.Instance.GetPPIList().Count > 0 )
			{
				break;
			}
			
			//Debug.Log( "Waiting for players ..." );
			
			yield return new WaitForSeconds(1.0f);
		}
		
		PrepareForNewRound();
	}

    //end round 
    //          - show score - show TimerRoundEnd
    //          - show unlocks - 
    // if necessary load new mission
    // startround
    //          - switch teams
    //          - spawn spawn menu
    //          - wait 20 seconds
    //          - start


    private void PrepareForNewRound()
    {
        GameInfo.StartRound();
        UpdateGameInfoOnClients();
		
		// respawn timer used for new round is slightly different
		TimeToRespawnCurrent = TimeToRespawnOriginal = (int)GameInfo.GameSetup.TimerRoundStart;
        UpdateTimeToRespawnOnClients();

        PPIManager.Instance.ServerStartNewRound();
		
        SendPrepareForNewRoundToClient();

        StartCoroutine(UpdateRespawns());
		
		LastRestTimeUpdated = 0;
		
		m_HealthyTimeWithoutSpawn = Time.timeSinceLevelLoad + GameInfo.GetMaxAllowedTimeWithoutRespawn();
		
		if ( OnRoundStarted != null )
			OnRoundStarted();
    }
	

	private void EndRound()
	{
		StopAllCoroutines();
		CancelInvoke();
		
		List<PlayerPersistantInfo> ppis = PPIManager.Instance.GetPPIList();
		
		if(ppis.Count > 0)
		{
			int avrgScore = 0; 
			foreach(PlayerPersistantInfo ppi in  ppis)
				avrgScore += ppi.Score.Score;
		
			avrgScore/= ppis.Count;
		}
		
		StartCoroutine( _EndRound() );
	}

    private IEnumerator _EndRound()
    {
		m_EndRoundInProgress = true;
        Printf("Round End");
		
		if ( OnRoundEnded != null )
			OnRoundEnded();
		
        GameInfo.State = GameInfo.GameState.Waiting;

        DestroyAllPlayersObjects();
		
		yield return new WaitForSeconds(1);
		
		foreach(PlayerPersistantInfo ppi in PPIManager.Instance.GetPPIList())
		{
			if(ppi.IsValid == false)
				continue;
			
			ppi.EndOfRound();
		}
		
		yield return new WaitForSeconds(0.1f);
		
		PPIManager.Instance.ServerEndRound();	//send data to cloud and update score on clients				
		
		yield return new WaitForSeconds(GameInfo.GameSetup.TimerRoundEnd - 3);
		
		/*
		SendShowRoundEarningsToClient();
		yield return new WaitForSeconds(GameInfo.GameSetup.TimerRoundEarnings);
		*/

		// PPIManager.Instance.ServerPrepareForNewRound();

		GameInfo.CurrentRound++;

        if (GameInfo.GameType == E_MPGameType.ZoneControl)
        {
            PPIManager.Instance.ServerSwitchTeams();
			PPIManager.Instance.ServerRebalanceTeams();
		}

		if (ShouldTerminateGameSession())
		{
			while(uLink.Network.connections.Count() > 0)
				uLink.Network.CloseConnection(uLink.Network.connections[0], true);
		}
		else
		{
			PPIManager.Instance.ServerPrepareForNewRound();
			ResetMission();
			
			if ( OnPreparingNewRound != null )
				OnPreparingNewRound();
			
			if (ShouldSwitchToNextLevel())
			{
				GameInfo.SetNextLevel();
				StartNewLevel();
			}
			else
			{
				WaitForPlayers();
			}
		}
			
		m_EndRoundInProgress = false;
    }
	
	public void SendEndRoundToClient(uLink.NetworkPlayer player, RoundFinalResult result)
	{
		List<PlayerPersistantInfo> plist = PPIManager.Instance.GetPPIList();
		
		for(int i =0; i < plist.Count;i++)
		{
			PlayerPersistantInfo p = plist[i];
			
			if(p.IsValid)
			result.PlayersScore.Add(new RoundFinalResult.PlayerResult() {
					Deaths = p.Score.Deaths,
					Kills = p.Score.Kills,
					PrimaryKey = p.PrimaryKey,
					NickName = p.Name,
					Score = p.Score.Score,
					Team = p.Team,
					Platform = p.Platform
				});
		}
		
		m_NetworkView.RPC("RoundEnded",player, result);
	}
			
	private bool ShouldSwitchToNextLevel()
	{
		return GameInfo.GameSetup.RoundPerLevel == GameInfo.CurrentRound;
	}
	
	private bool ShouldTerminateGameSession()
	{
		return ShouldSwitchToNextLevel() && (LevelsPerGameSession == LevelOrderNumber);
	}

    public void SendRankUpToClient(PlayerPersistantInfo ppi)
    {
		m_NetworkView.RPC("RankUp", ppi.Player, PlayerPersistantInfo.GetPlayerRankFromExperience(ppi.Score.Experience + ppi.Experience));
    }
	
	private void SendShowRoundEarningsToClient()
	{
        m_NetworkView.RPC("ShowRoundEarnings", uLink.RPCMode.Others );
	}
	
	private void ResetMission()
	{
		Mission.Instance.Reset();
		
		m_NetworkView.RPC("MissionReset", uLink.RPCMode.Others);
	}
	
    private void SendPrepareForNewRoundToClient()
    {
        m_NetworkView.RPC("NewRound", uLink.RPCMode.Others);
    }
	
	E_Team GetTeam_ZoneControl( PlayerSlotReservation.Reservation reservation )
	{
		if ( reservation != null )
		{
			return reservation.Team;
		}
		
		E_Team result = E_Team.Good;
		
		// Now we are going to assume that all the remaining reservation will be successfully applied and no current user will disconnect.
		int goodAssumption = GetNumberOfPlayersInTeam( E_Team.Good );
		int badAssumption = GetNumberOfPlayersInTeam( E_Team.Bad );
		
		if ( SlotReservation != null )
		{
			goodAssumption += SlotReservation.ReservedSlotCount( E_Team.Good );
			badAssumption += SlotReservation.ReservedSlotCount( E_Team.Bad );
		}
		
		if ( badAssumption == goodAssumption )
		{
			int score = GameInfo.TeamScore[E_Team.Good] - GameInfo.TeamScore[E_Team.Bad];
			int goodFlags = 0;
			int badFlags = 0;
			
			foreach ( ZoneControlFlag z in (Mission.Instance.GameZone as GameZoneZoneControl).Zones )
			{
				if ( z.FlagOwner == E_Team.Good )
					goodFlags++;
				if ( z.FlagOwner == E_Team.Bad )
					badFlags++;
			}
			
			score += (goodFlags-badFlags)*10;
			result = (score <= 0) ? E_Team.Good : E_Team.Bad;
		}
		else
		{	
			result = (badAssumption < goodAssumption) ? E_Team.Bad : E_Team.Good;
		}
		
		return result;
	}
	
	bool PlayerSlotAvailableCheck(PlayerSlotReservation.Reservation reservation)
	{
		// nowdays the functionality of the function is strictly related to the 
		if (SlotReservation != null)
		{
			if (GameInfo.GameType == E_MPGameType.ZoneControl)
			{
				if (reservation != null)
					return true;
					
				int reservedSlots = SlotReservation.ReservedSlotCount();
				int usedSlots = PPIManager.Instance.GetPPIList().Count;
				
				return (reservedSlots + usedSlots) < ServerSetup.MaxConnections;
			}
		}
		
		return true;
	}
	
	PlayerSlotReservation.Reservation FindReservation(int joinRequestId)
	{
		PlayerSlotReservation.Reservation	result = null;
			
		if (SlotReservation != null)
		{
			//I don't want to put it to an Update() function as it is required just for accepting join requests
			SlotReservation.RemoveAllInvalid();
			
			result = SlotReservation.FindValid(joinRequestId);
		}
		
		return result;
	}

	public void uLink_OnPlayerApproval(uLink.NetworkPlayerApproval approval)
	{
		// we need to copy the bitstream here because the OnPlayerConnected callback receives exactly the same bitream object
		// in theory we could read only the version information here and let the following callback to read the rest of the information
		uLink.BitStream loginData = new uLink.BitStream(approval.loginData._data, approval.loginData._bitIndex, approval.loginData._bitCount, false, approval.loginData.isTypeSafe);
		
		string versionString = loginData.Read<string>();
		NetUtils.Version playerVersion = new NetUtils.Version(versionString);
		
		if (!NetUtils.CurrentVersion.Equals(playerVersion))
		{
			Printf("Refusing player connection from " + approval.info.sender.endpoint + " because of version incompatibility. server version=" + NetUtils.CurrentVersion + ", client version=" + playerVersion);
			approval.Deny(uLink.NetworkConnectionError.IncompatibleVersions);
			return;
		}
		
		/*string userName			= */loginData.Read<string>();
		string nickName				= loginData.Read<string>();
		/*RuntimePlatform platform	= (RuntimePlatform)*/loginData.Read<int>();
		int joinRequestId			= loginData.Read<int>();
		
		// Debug.Log( "New player request: nick=" + nickName + " joinRequestId=" + joinRequestId );
		
		PlayerSlotReservation.Reservation reservation = FindReservation( joinRequestId );
	
		if (!PlayerSlotAvailableCheck(reservation))
		{
			Printf("Refusing player connection from " + approval.info.sender.endpoint + " because there is no available player slot. joinrequestId=" + joinRequestId);
			
			// This is not a best description of the problem but it is the unique one.
			// Thus it should help us to identify the problem once a customer will report such issue to us.
			approval.Deny(uLink.NetworkConnectionError.LimitedPlayers);
			return;
		}
			
		Printf("New player approved, nick=" + nickName);
		approval.Approve();
	}
	
	PlayerPersistantInfo EstablishNewPlayer( uLink.NetworkPlayer player, out int joinRequestId )
	{
		PlayerPersistantInfo ppi = new PlayerPersistantInfo(); /* player.loginData.Read<PlayerPersistantInfo>();*/
		
		ppi.Player 				= player;
		ppi.IsPlayerConnected	= true;
		/*string versionString = */player.loginData.Read<string>();
		ppi.PrimaryKey			= player.loginData.Read<string>(); //TODO: PRIMARY KEY - pridat username?
		ppi.Name				= player.loginData.Read<string>();
		ppi.Platform			= (RuntimePlatform)player.loginData.Read<int>();
		ppi.PrimaryKeyHash    	= CloudServices.CalcHash64(ppi.PrimaryKey);
		
		joinRequestId			= player.loginData.Read<int>();
		
		PlayerSlotReservation.Reservation reservation = FindReservation( joinRequestId );
		if ( reservation != null )
		{
			SlotReservation.Apply( reservation );
		}
		
		if (GameInfo.GameType == E_MPGameType.ZoneControl)
		{
			ppi.Team = GetTeam_ZoneControl( reservation );
			ppi.ZoneIndex = GetZoneIndexForTeam(ppi.Team);
		}
		else
			ppi.Team = E_Team.None;
		
		Printf("User " + ppi.UserName_TODO + " connected as " + ppi.Name + " - joined " + ppi.Team + " zone " + ppi.ZoneIndex);
		
		PPIManager.Instance.ServerAddPPI(ppi);
		
		return ppi;
	}
	
	public void uLink_OnPlayerConnected(uLink.NetworkPlayer player)
	{
		PPIManager.Instance.ServerSendAllPPItoPlayer(player);
		
		int joinRequestId = 0;
		PlayerPersistantInfo ppi = EstablishNewPlayer(player, out joinRequestId);
		
		PPIManager.Instance.ServerSetState(player, PlayerPersistantInfo.E_State.Connected, true);
		
		UpdateGameInfoOnClient(player);
		
		if ( OnPlayerConnected != null )
			OnPlayerConnected( player, joinRequestId );
		
		//CloudServices.AsyncOpResult asyncOpRes =
		CloudServices.GetInstance().UserGetPerProductData(
			ppi.PrimaryKey,PPIManager.ProductID,CloudServices.PROP_ID_PLAYER_DATA,CloudConfiguration.DedicatedServerPasswordHash, OnPlayerConnectedFetchPPIDataDone, ppi );

		// moved to constructor of AsyncOpResult to prevent problems caused by super-fast reply from cloud
		//asyncOpRes.m_UserData = ppi;		
		//asyncOpRes.m_Listeners.Add(ppi.FetchPPIDataFromCloudAsyncOpFinished);
		//asyncOpRes.m_Listeners.Add(OnPlayerConnectedFetchPPIDataDone);
		//asyncOpRes.m_Listeners.Add(PPIFetchFromCloudFinished);
	}
	
	private void OnPlayerConnectedFetchPPIDataDone(CloudServices.AsyncOpResult res)
	{
		PlayerPersistantInfo ppi = res.m_UserData as PlayerPersistantInfo;
		
		//if(res.m_Finished) // AsyncOpResult is always finished at this point
		if( res.m_Res )
		{
			if( null == ppi )
			{
				Printf( "Unexpected null PPI in opResult" );
				return;
			}

			if ( !ppi.IsPlayerConnected )
			{
				Printf( "OnPlayerConnectedFetchPPIDataDone - Player disconnected before the request was answered" );
				return;
			}
			
			ppi.FetchPPIDataFromCloudAsyncOpFinished( res );
			
			PPIManager.Instance.ServerUpdatePPI(ppi);
			
			m_NetworkView.RPC("LoadMission", ppi.Player, GameInfo.CurrentLevel);
		}
		else
		{
			Printf( "OnPlayerConnectedFetchPPIDataDone - Fetching ppi failed, player " + ( ( null != ppi ) ? ppi.PrimaryKey : "-unknown-" ) );
		}
	}
		
	
	void uLink_OnPlayerDisconnected(uLink.NetworkPlayer player)
    {
        Printf(" remove player " + player);
		
		// Keep it here, before anything happens with ppi! There are 2 good reasons for it:
		// - If there would be an exception in the ppi handlindlig code it would result in dangling player/server in the match-making applications
		// - OnPlayerConnected notification is triggered after all the ppi is initialized -> OnPlayerDisconnected should be called before anything
		//   happens with the ppi, giving to others a chance to even modify the ppi.
		if ( OnPlayerDisconnected != null )
			OnPlayerDisconnected( player );
		
		// we need to deactivate player before ppi synchonize
		// so we can modify stats
        uLink.Network.DestroyPlayerObjects(player);

		PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(player);
        PPIManager.Instance.ServerRemovePPI(player);
		ppi.IsPlayerConnected = false;

        uLink.Network.RemoveRPCs(player);
        // this is not really necessery unless you are removing NetworkViews without calling uLink.Network.Destroy
        uLink.Network.RemoveInstantiates(player);
    }

    [uSuite.RPC]
    public void RequestForSpawn(uLink.NetworkPlayer player, int zoneIndex, uLink.NetworkMessageInfo info)
	{
		PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(player);
        
        if (ppi == null)
        {
            Debug.Log(player + " - Request for spawn - error");

            PPIManager.Instance.ReportAllPPI();
        }

        Printf(ppi.Name + " - Request for spawn " + ppi.Team + " zone " + zoneIndex);

		ppi.ResetPlayerData();

		if(GameInfo.GameType == E_MPGameType.ZoneControl)
		{
			ppi.ZoneIndex = zoneIndex;
        	ValidateSpawnZone(ppi);
		}
		
		StartCoroutine( _SendSpawnAsyncRequest(ppi) );
	}

	//
	private IEnumerator _SendSpawnAsyncRequest(PlayerPersistantInfo ppi)
	{
		const int MaxRequestAttempts = 3;
		int	attempts = MaxRequestAttempts;
		bool done = false;
				
		while (!done)
		{
			//Spawn a new request and remember it
			CloudServices.AsyncOpResult asyncOpRes = CloudServices.GetInstance().UserGetPerProductData(ppi.PrimaryKey,PPIManager.ProductID,CloudServices.PROP_ID_PLAYER_DATA,CloudConfiguration.DedicatedServerPasswordHash, PPIFetchFromCloudFinished, ppi);
			ppi.PendingSpawnRequests.Add(asyncOpRes);
			
			yield return new WaitForSeconds(5.0f);
			
			//99% of requests use to be normally resolved within 1 second
			//If our request is still pending, we need to re-send it (it could be either completelly lost or stuck on the cloud server)
			if (!ppi.IsPlayerConnected || ppi.PendingSpawnRequests.Count==0)
			{
				//either the request was succeffully processed or the player left the game -> no need to get from cloud again
				done = true;
			}
			else
			{
				if (--attempts <= 0)
				{
					//this is to make sure that we will exit if something is REALLY fucked up
					Printf(ppi.Name + " - SpawnAsyncRequest: the maximum number of retries (" + MaxRequestAttempts + ") has been reached. Giving up...");
					done = true;
				}
			}
		}
	}
	
    [uSuite.RPC]
    public void RequestForTeamSwitch(uLink.NetworkPlayer player, E_Team team, uLink.NetworkMessageInfo info)
    {
        PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(player);
        if (ppi.Team == team)
            return;

        int currentTeam = GetNumberOfPlayersInTeam(ppi.Team);
        int newTeam = GetNumberOfPlayersInTeam(team);
		
		// if FreedomOfTeamSelection set to true, every request for team selection will be granted
		if( !GameInfo.ZoneControlSetup.FreedomOfTeamSelection )
		{
			if (currentTeam < newTeam)
				return;
		}

        ppi.Team = team;
        PPIManager.Instance.ServerUpdatePPI(ppi);
		
        m_NetworkView.RPC("ClientTeamSwitched", player, team);
    }
	
	void PPIFirstFetchFromCloudFinished(CloudServices.AsyncOpResult res)
	{
		if (res.m_Res)
		{
			PlayerPersistantInfo ppi = res.m_UserData as PlayerPersistantInfo;
			PPIManager.Instance.ServerUpdatePPI(ppi);
		}
	}

	void PPIFetchFromCloudFinished(CloudServices.AsyncOpResult res)
	{
		PlayerPersistantInfo ppi = res.m_UserData as PlayerPersistantInfo;
			
		if (res.m_Res)
		{
			MFDebugUtils.Assert(ppi != null);
			
			if( null == ppi )
			{
				Printf( "PPIFetchFromCloudFinished - Unexpected null PPI in opResult" );
				
				return;
			}
			
			if (!ppi.PendingSpawnRequests.Contains(res))
			{
				Printf( ppi.Name + " PPIFetchFromCloudFinished - spawn request was not found in the pending list (probably a re-sent request), dropping..." );
				return;
			}
			
			ppi.PendingSpawnRequests.Clear();
			
			ppi.FetchPPIDataFromCloudAsyncOpFinished( res );
			
			if (ppi.IsPlayerConnected)
			{
				PlayerReadyToSpawn(ppi);
			}
		}
		else
		{
			Printf( "PPIFetchFromCloudFinished - Fetching ppi failed, player " + ( ( null != ppi ) ? ppi.PrimaryKey : "-unknown-" ) );
		}
		
	}

	private void PlayerReadyToSpawn(PlayerPersistantInfo ppi)
	{
        Printf(ppi.Name + " - ready  to spawn in team " + ppi.Team + " and zone " + ppi.ZoneIndex);
        PPIManager.Instance.ServerSetState(ppi.Player, PlayerPersistantInfo.E_State.WaitingForSpawn, true);
	}

    private IEnumerator UpdateRespawns()
    {
        while(true)
        {
            // Debug.Log("TimeToREspawn " + TimeToRespawnCurrent);
            if (GameInfo.State != global::GameInfo.GameState.Running)
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }
			
            yield return new WaitForSeconds(1);
			
			// update respawn timers just in case there are some players
			if( PPIManager.Instance.GetPPIList().Count > 0 )
            {
				TimeToRespawnCurrent -= 1;
				
				foreach( PlayerPersistantInfo ppi in PPIManager.Instance.GetPPIList() )
                {
					if( ppi.State == PlayerPersistantInfo.E_State.WaitingForSpawn )
					{
						if( TimeToRespawnCurrent <= 0 )
						{
							if( !RespawnPlayer( ppi.Player ) )
							{
								// Respawn was cancelled - switch client back to spawn menu
								CancelSpawning( ppi.Player );
							}
						}
						else
						{
							// Zone is neutralized
							if( !IsSelectedZoneIndexValid( ppi.ZoneIndex, ppi.Team ) )
							{
								CancelSpawning( ppi.Player );
							}
						}
					}
				}
				
				if( TimeToRespawnCurrent <= 0 )
				{
					TimeToRespawnCurrent = TimeToRespawnOriginal = GameInfo.GameSetup.TimerSpawn;
				}
				
				UpdateTimeToRespawnOnClients();
			}
			else // waiting for one player at least - after connecting, timer will start counting again
			{
				TimeToRespawnCurrent = TimeToRespawnOriginal;
				
				// there are no clients to update
				// UpdateTimeToRespawnOnClients();
			}
        }
    }
	
	private void CancelSpawning( uLink.NetworkPlayer player )
	{
		PPIManager.Instance.ServerSetState( player, PlayerPersistantInfo.E_State.Connected, true );
		
		// Respawn was cancelled - switch client back to spawn menu
		m_NetworkView.RPC("OnSpawningCanceled", player );
	}

    private void UpdateTimeToRespawnOnClients()
    {
        m_NetworkView.RPC("UpdateTime2Respawn", uLink.RPCMode.Others, (byte)TimeToRespawnCurrent);
    }
	
	private bool IsSelectedZoneIndexValid( int ZoneIndex, E_Team Team )
    {
		if ( GameInfo.GameType == E_MPGameType.ZoneControl )
		{
			if( ZoneIndex < 0 )
			{
				Debug.Log("Respawn on invalid zone index - cancelling");
				return false;
			}
			
			ZoneControlFlag Flag = (Mission.Instance.GameZone as GameZoneZoneControl).Zones[ZoneIndex];
			
			if( Team  != Flag.FlagOwner)
			{
				Debug.Log("Respawn on enemy zone index - cancelling");
				
				return false;
			}
		}
		
		return true;
	}
	
	// @return FALSE if respawn was cancelled
	private bool RespawnPlayer( uLink.NetworkPlayer player )
	{
		//uLink.Network.DestroyPlayerObjects(player);

		PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(player);
		
		Debug.Log(ppi.Name + " Respawn " + ppi.Team + " zone " + ppi.ZoneIndex);
		
		if( !IsSelectedZoneIndexValid( ppi.ZoneIndex, ppi.Team ) )
		{
			return false;
		}

		PPIManager.Instance.ServerSetState(player, PlayerPersistantInfo.E_State.Spawned, true);

		SpawnPoint p = null;
		E_SkinID skin = ppi.EquipList.Outfits.Skin;

		if (GameInfo.GameType == E_MPGameType.DeathMatch)
		{
			p = (Mission.Instance.GameZone as GameZoneDeathMatch).GetPlayerSpawnPoint();
		}
		else
		{
			p = (Mission.Instance.GameZone as GameZoneZoneControl).GetPlayerSpawnPoint(ppi.ZoneIndex, ppi.Team);
		}
		
		PPIManager.Instance.ServerUpdatePPI(ppi);
		
        Vector3 spawnPos = p.Transform.position;
        Quaternion spawnRot = p.Transform.rotation;

		spawnPos = CollisionUtils.GetGroundedPos(spawnPos);
		
        PlayerCache.ServerCache.PlayerInfo pi =  PlayerCache.Instance.Find(skin);
		
		uLink.Network.Instantiate(player, pi.PrefabProxy, pi.PrefabOwner, PlayerCache.Instance.PlayerCreatorPrefab, spawnPos, spawnRot, 0);
	//	uLink.Network.Instantiate(player, pi.PrefabProxy, pi.PrefabOwner, pi.PrefabCreator,                         spawnPos, spawnRot, 0);

        if (GameInfo.GameType == E_MPGameType.ZoneControl)
        {
            GameInfo.TeamScore[ppi.Team]--;
			
			UpdateTeamScore();
			
            if (GameInfo.TeamScore[ppi.Team] <= 0)
                EndRound();
        }
		
		// stop counting time without spawn
		m_HealthyTimeWithoutSpawn = -1;
		
		ResetHealthyTimeWithoutUserInput();
		
		return true;
    }

    private void DestroyAllPlayersObjects()
    {
        // This destroys all network-aware objects except those owned by the server.
        foreach (var player in uLink.Network.connections.Where(player => player.isClient))
        {
            uLink.Network.DestroyPlayerObjects(player);
        }
    }
	
	//TODO - checking for lags - remove later
	float m_LastUpdateTimeStamp = 1000.0f;
	
    void LateUpdate()
    {
        if(GameInfo.State != GameInfo.GameState.Running)
		{
			m_LastUpdateTimeStamp = Time.realtimeSinceStartup;
            return;
		}
		
		//frame time check
		float timeNow = Time.realtimeSinceStartup;
		float frameTime = timeNow - m_LastUpdateTimeStamp;
		if ( frameTime > 0.1f )
		{
#if !UNITY_EDITOR
			
			if ( frameTime > 0.5f )
				Debug.Log( "!!! Extremly long frame detected: " + frameTime + "  " + System.DateTime.Now.ToString() );
			else
				Debug.Log( "!!! Long frame detected: " + frameTime + "  " + System.DateTime.Now.ToString() );
#endif
		}
		
		m_LastUpdateTimeStamp = timeNow;

        if (GameInfo.GameType == E_MPGameType.DeathMatch)
            UpdateDeathMatch();
        else if (GameInfo.GameType == E_MPGameType.ZoneControl)
            UpdateZoneControl();
		
		if( m_UserInput )
		{
			m_UserInput = false;
			
			ResetHealthyTimeWithoutUserInput();
		}
		
		if (ShowNetworkStatisticsEnabled)
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

	void UpdateDeathMatch()
	{
		float RestTime = GameInfo.RoundStartTime + (GameInfo.GameSetup as DeathMatchInfo).TimeLimit - Time.timeSinceLevelLoad;
		
		int RestTimeSeconds = Mathf.Max( (int)RestTime, 0 );
		
		if( RestTimeSeconds != LastRestTimeUpdated )
		{
			if ( RestTimeSeconds <= 0)
			{
				EndRound();
			}
			
			LastRestTimeUpdated = RestTimeSeconds;
			
			//m_NetworkView.RPC("UpdateTime2Respawn", uLink.RPCMode.Others, (byte)TimeToRespawnCurrent);
			m_NetworkView.RPC( "UpdateDeathMatchRestTime", uLink.RPCMode.Others, LastRestTimeUpdated );
		}
	}

	void UpdateZoneControl()
	{
		int bad = 0;
		int good = 0;
		
		int max = ( Mission.Instance.GameZone as GameZoneZoneControl ).Zones.Count;
		
		foreach ( ZoneControlFlag z in ( Mission.Instance.GameZone as GameZoneZoneControl ).Zones )
		{
			if ( z.FlagOwner == E_Team.None )
			{
				continue;
			}
			
			if ( z.FlagOwner == E_Team.Good )
			{
				good++;
			}
			else if ( z.FlagOwner == E_Team.Bad )
			{
				bad++;
			}
		}
		
		if( GameInfo.NextGameUpdate < Time.timeSinceLevelLoad )
		{
			GameInfo.NextGameUpdate = Time.timeSinceLevelLoad + GameInfo.ZoneControlSetup.ZoneControlUpdate;
			
			if ( good - bad > 0 )
			{
				int score = good - bad;
				
				if ( good == max )
				{
					GameInfo.TeamScore[E_Team.Bad] = Mathf.Max( 0, GameInfo.TeamScore[E_Team.Bad] - score * 4 );
					
					GameInfo.NextGameUpdate = Time.timeSinceLevelLoad + 1;
				}
				else
				{
					GameInfo.TeamScore[E_Team.Bad] = Mathf.Max( 0, GameInfo.TeamScore[E_Team.Bad] - score );
				}
			}
			
			if ( bad - good > 0 )
			{
				int score = bad - good;
				
				if ( bad == max )
				{
					GameInfo.TeamScore[E_Team.Good] = Mathf.Max(0, GameInfo.TeamScore[E_Team.Good] - score * 4);
					
					GameInfo.NextGameUpdate = Time.timeSinceLevelLoad + 1;
				}
				else
				{
					GameInfo.TeamScore[E_Team.Good] = Mathf.Max(0, GameInfo.TeamScore[E_Team.Good] - score);
				}
			}
			
			if ( good > 1 || bad > 1 )
			{
				UpdateTeamScore();
			}
		}
				
		if ( GameInfo.TeamScore[E_Team.Good] <= 0 || GameInfo.TeamScore[E_Team.Bad] <= 0 )
		{
			if( !m_EndRoundInProgress )
			{
				EndRound();
			}
		}
	}
	
	private int AliveMembersForTeam( E_Team Team )
	{
		int Result = 0;
		
		foreach (KeyValuePair<uLink.NetworkPlayer, ComponentPlayer> pair in Player.Players)
        {
            AgentHuman a = pair.Value.Owner;
			
            if( a.IsAlive == false || a.Team != Team )
                continue;
			Result++;
        }
		
		return Result;
	}

    private E_Team GetTeamWithLessPlayers()
    {
        int GoodTeam = 0;
        int BadTeam = 0;
        foreach (PlayerPersistantInfo ppi in PPIManager.Instance.GetPPIList())
        {
            if (ppi.Team == E_Team.Bad)
                BadTeam++;
            else if (ppi.Team == E_Team.Good)
                GoodTeam++;
        }

        if (GoodTeam > BadTeam)
            return E_Team.Bad;
        else
            return E_Team.Good;
    }

    private int GetNumberOfPlayersInTeam(E_Team team)
    {
        int num = 0;
        foreach (PlayerPersistantInfo ppi in PPIManager.Instance.GetPPIList())
        {
            if (ppi.Team == team)
                num++;
        }
        return num;
    }

    private int GetZoneIndexForTeam(E_Team team)
    {
		if(Mission.Instance == null)
			return -1; //loading ?
		
        List<ZoneControlFlag> zones = (Mission.Instance.GameZone as GameZoneZoneControl).Zones;

        int i = zones.FindIndex(z => z.StartBase == team && z.FlagOwner == team);

        if (i >= 0)// we found base and it still belong to team
            return i;

        //base is not owned by team already

        return -1;
    }

    private void ValidateSpawnZone(PlayerPersistantInfo ppi)
    {
		GameZoneZoneControl		ZoneControlMode = Mission.Instance.GameZone as GameZoneZoneControl;
		if ( ZoneControlMode == null )
			return;
		
        List<ZoneControlFlag> spawnZones = ZoneControlMode.Zones;

        if (ppi.ZoneIndex < 0 || spawnZones.Count <= ppi.ZoneIndex)
            ppi.ZoneIndex = -1;
        else if (spawnZones[ppi.ZoneIndex] == null || spawnZones[ppi.ZoneIndex].FlagOwner != ppi.Team)
            ppi.ZoneIndex = -1;

        if (ppi.ZoneIndex < 0)
        {
            if (ppi.Team == E_Team.Good)
            {
                // get first availible zone, forward...

                for (int i = 0; i < spawnZones.Count; i++)
                {
                    if (spawnZones[i] == null || spawnZones[i].FlagOwner != ppi.Team)
                        continue;

                    ppi.ZoneIndex = i;
                    break;
                }
            }
            else if (ppi.Team == E_Team.Bad)
            {
                // get first availible zone, backward...

                for (int i = spawnZones.Count - 1; i >= 0; i--)
                {
                    if (spawnZones[i] == null || spawnZones[i].FlagOwner != ppi.Team)
                        continue;

                    ppi.ZoneIndex = i;
                    break;
                }
            }
        }
    }

    private void UpdateTeamScore()
    {
        m_NetworkView.RPC("UpdateTeamScore", uLink.RPCMode.Others, (short)GameInfo.TeamScore[E_Team.Good], (short)GameInfo.TeamScore[E_Team.Bad]);
    }

    public void ShowMessageOnClients(string msg)
    {
        m_NetworkView.RPC("ConsoleMsg", uLink.RPCMode.Others, msg);
    }

	public void ShowMessageOnClient(uLink.NetworkPlayer client, string msg)
	{
		m_NetworkView.RPC("ConsoleMsg", client, msg);
	}
	
	public void ShowGoldMsgOnClient(uLink.NetworkPlayer onClient, short gold )
    {
        m_NetworkView.RPC("GoldMsg", onClient, gold);
    }
	
		
    public void ShowCombatMsgOnClient(uLink.NetworkPlayer onClient, Client.E_MessageType type, short exp, short cash )
    {
        m_NetworkView.RPC("CombatMsg", onClient, type, exp, cash);
    }
	

	private void UpdateGameInfoOnClients()
	{
		GameTypeInfo setup = GameInfo.GameSetup;
		
		if ( GameInfo.GameType == E_MPGameType.DeathMatch )
		{
			DeathMatchInfo info = GameInfo.GameSetup as DeathMatchInfo;
			
			m_NetworkView.RPC("UpdateGameInfoDM", uLink.RPCMode.Others, GameInfo.GameType, info.KillsLimit, info.TimeLimit, setup.RoundPerLevel, 
												setup.MPLevels[GameInfo.CurrentLevelIndex].Name, GameInfo.CurrentRound, setup.SpawnTimeProtection );
		}
		else if ( GameInfo.GameType == E_MPGameType.ZoneControl )
		{
			m_NetworkView.RPC("UpdateGameInfoZC", uLink.RPCMode.Others, GameInfo.GameType, GameInfo.TeamScore[E_Team.Good], GameInfo.TeamScore[E_Team.Bad], 
												setup.RoundPerLevel, setup.MPLevels[GameInfo.CurrentLevelIndex].Name, GameInfo.CurrentRound, setup.SpawnTimeProtection );
		}
	}
	
	private void UpdateGameInfoOnClient( uLink.NetworkPlayer player )
	{
		GameTypeInfo setup = GameInfo.GameSetup;
		
		if ( GameInfo.GameType == E_MPGameType.DeathMatch )
		{
			DeathMatchInfo info = GameInfo.GameSetup as DeathMatchInfo;
			
			m_NetworkView.RPC("UpdateGameInfoDM", player, GameInfo.GameType, info.KillsLimit, info.TimeLimit, setup.RoundPerLevel, 
												setup.MPLevels[GameInfo.CurrentLevelIndex].Name, GameInfo.CurrentRound, setup.SpawnTimeProtection );
		}
		else if ( GameInfo.GameType == E_MPGameType.ZoneControl )
		{
			m_NetworkView.RPC("UpdateGameInfoZC", player, GameInfo.GameType, GameInfo.TeamScore[E_Team.Good], GameInfo.TeamScore[E_Team.Bad], 
												setup.RoundPerLevel, setup.MPLevels[GameInfo.CurrentLevelIndex].Name, GameInfo.CurrentRound, setup.SpawnTimeProtection );
		}
	}
	
    void uLink_OnSerializeNetworkView(uLink.BitStream stream, uLink.NetworkMessageInfo info)
    {
        stream.Write<int>((int)(Mathf.Max(0, Time.timeSinceLevelLoad - GameInfo.RoundStartTime)));
    } 
	
    void uLink_OnServerUninitialized()
    {
        Printf("Server is UnInitialized");
    }
	
	void SetStartupGameType( E_MPGameType newGameType )
	{
		MultiplayerType = newGameType;
		
		Debug.Log("Setting GameType to " + MultiplayerType);
	}
	
	public static E_MPGameType GetGameTypeByShortcutName( string name )
	{
		string lowerCaseName = name.ToLower();
		
		switch( lowerCaseName )
		{
		case "zc":
			return E_MPGameType.ZoneControl;
			
		case "dm":
			return E_MPGameType.DeathMatch;
			
		case "none":
			return E_MPGameType.None;
		}
		
		throw new System.ArgumentOutOfRangeException( "Unsupported game type (" + name + ")" );
	}
	
	public static string GetGameTypeShortcutName( E_MPGameType type )
	{
		switch( type )
		{
		case E_MPGameType.DeathMatch:
			return "dm";
		case E_MPGameType.ZoneControl:
			return "zc";
		case E_MPGameType.None:
			return "none";
		}
		
		throw new System.NotSupportedException( "Not supported for " + type );
	}
		
	public void SetGameParams( string[] parameters )
	{
		foreach (string str in parameters)
		{
			if (str.StartsWith("-timerspawn="))
			{
				string[] param = str.Split('=');
				int timerSpawn = System.Convert.ToInt32(param[1]);
				GameInfo.AllModes_SetTimerSpawn(timerSpawn);
					
				Debug.Log("GameType set to " + timerSpawn);
			}
			else if (str.StartsWith("-timerstart="))
			{
				string[] param = str.Split('=');
				float timerStart = System.Convert.ToSingle(param[1]);
				GameInfo.AllModes_SetTimerRoundStart( timerStart );
				
				Debug.Log("Timer Round Start set to " + timerStart);
			}
			else if (str.StartsWith("-timerend="))
			{
				string[] param = str.Split('=');
				float timerEnd = System.Convert.ToSingle(param[1]);				
				GameInfo.AllModes_SetTimerRoundEnd( timerEnd );
				
				Debug.Log("Timer Round End set to " + timerEnd);
			}
			else if (str.StartsWith("-zctickets="))
			{
				string[] param = str.Split('=');
				GameInfo.ZoneControl_SetTickets(System.Convert.ToInt32(param[1]));
				
				Debug.Log("Zone Control tickets set to " + System.Convert.ToInt32(param[1]));
			}
			else if (str.StartsWith("-dmtimelimit="))
			{
				string[] param = str.Split('=');
				GameInfo.DeahMatch_SetTimeLimit(System.Convert.ToSingle(param[1]));
				
				Debug.Log("DM Time Limit set to " + System.Convert.ToSingle(param[1]));
			}
			else if (str.StartsWith("-dmkilllimit="))
			{
				string[] param = str.Split('=');
				GameInfo.DeahMatch_SetKillLimit(System.Convert.ToInt32(param[1]));
				
				Debug.Log("DM Kill Limit set to " + System.Convert.ToInt32(param[1]));
			}
			else if (str.StartsWith("-roundsperlevel="))
			{
				string[] param = str.Split('=');
				GameInfo.AllModes_SetRoundsPerLevel(System.Convert.ToInt32(param[1]));
				Debug.Log("Number of rounds per level set to " + System.Convert.ToInt32(param[1]));
			}
			else if (str.StartsWith("-levelspersession="))
			{
				string[] param = str.Split('=');
				LevelsPerGameSession = System.Convert.ToInt32(param[1]);
				
				Debug.Log("Number of levels per game session set to " + System.Convert.ToInt32(param[1]));
			}
		}
	}
	
	void SetupServerFromCommandLine()
	{
		bool initializeAnticheat = true;

#if UNITY_EDITOR
		bool developAnticheat = true;
#else
		bool developAnticheat = false;
#endif

		string[] arguments = System.Environment.GetCommandLineArgs();
		//string[] arguments = new string[1] { "-gametype=none" };
		
		foreach (string str in arguments)
		{
			if (str.StartsWith("-master="))
			{
				string[] param = str.Split('=');
				uLink.MasterServer.ipAddress = param[1];
				
				Debug.Log("Setting Master Server ip to " + param[1]);
			}
			else if (str.StartsWith("-gametype="))
			{
				string[] param = str.Split('=');
				E_MPGameType type = MultiplayerType;
				
				try
				{
					type = GetGameTypeByShortcutName( param[1] );					
				}
				catch( System.ArgumentException excp )
				{
					Debug.LogWarning( excp.Message + ", the default mode will be used" );					
				}
				
				SetStartupGameType( type );
			}
			else if (str.StartsWith("-serverport="))
			{
				string[] param = str.Split('=');

				try
				{
					ExplicitServerPort = System.Convert.ToInt32(param[1]);
				}
				catch
				{
					Debug.LogWarning("Unable to read the -serverport command line argument (" + param[1] + ")." );
				}
			}
			else if (str.StartsWith("-remotepid="))
			{
				//TODO this can be removed once we will be deploying non-debug builds to our servers
				IgnoreDebugSettings = true;
			}
			else if (str.StartsWith("-connectionstatistics"))
			{
				gameObject.AddComponent<PlayerConnectionStatistics>();
			}
			else if (str.StartsWith("-anticheat"))
			{
				string[] param = str.Split('=');

				if (string.Equals(param[1], "off", System.StringComparison.OrdinalIgnoreCase))
				{
					initializeAnticheat = false;
				}
				else if (string.Equals(param[1], "on", System.StringComparison.OrdinalIgnoreCase))
				{
					initializeAnticheat = true;
					developAnticheat = false;
				}
				else if (string.Equals(param[1], "develop", System.StringComparison.OrdinalIgnoreCase))
				{
					initializeAnticheat = true;
					developAnticheat = true;
				}
			}
			else if (str.StartsWith("-batchmode"))
			{
				ShowNetworkStatisticsEnabled = false;
			}
		}

		if (initializeAnticheat)
		{
			ServerAnticheat anticheat = gameObject.AddComponent<ServerAnticheat>();
			anticheat.DevelopMode = developAnticheat;
		}
		
		SetGameParams( arguments );
	}

    public static void Printf(string s)
    {
        print(Time.timeSinceLevelLoad + " Server - " + s);
    }
	
	public void PlaySoundDisabledOnClient( uLink.NetworkPlayer player )
	{
		m_NetworkView.RPC("PlaySoundDisabled", player );
	}
	
	public void NotifyServerInvisible()
	{
		m_NetworkView.RPC("ServerInvisible", uLink.RPCMode.Others);
	}
	
	// false if no player was spawned in last N minutes
	public bool IsGameplayHealthy()
	{
		if( m_HealthyTimeWithoutSpawn > 0 )
		{
			if( Time.timeSinceLevelLoad > m_HealthyTimeWithoutSpawn )
			{
				return false;
			}
			
			return true;
		}
		
		// there are players spawned, it is time for some activity test
		if( Time.timeSinceLevelLoad > m_HealthyTimeWithoutMovement )
		{
			return false;
		}
		
		return true;
	}
	
	private void ResetHealthyTimeWithoutUserInput()
	{
		m_HealthyTimeWithoutMovement = Time.timeSinceLevelLoad + GameInfo.GetMaxAllowedTimeWithoutUserInput();
	}
	
	// called when player is moving
	public void cbUserInput()
	{
		m_UserInput = true;
	}
}

#endif
