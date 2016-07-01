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
using System.Net;

//using uLink;
//using uLobby;

// =====================================================================================================================
// =====================================================================================================================
public class ServerListScreen : GuiScreen
{
	public bool isConnectingToServer = false;
	public System.Net.IPEndPoint selectedServerEndpoint;

	public bool failedToConnectToServer;
	public uLink.NetworkConnectionError connectionError;

	static float masterServerRefreshHostListIntervalSec = 2.0F;
	//uLink.MasterServer.gameType has been set in Resources/uLinkNetworkPrefs.cs
	static uLink.HostDataFilter hostFilter;

	uLink.HostData[] lobbyHostData;

	//gui info for one line of serverlist
	class ServerLineGui
	{
		public GUIBase_Button button;
		public GUIBase_Label labelMapName;
		public GUIBase_Label labelMode;
		public GUIBase_Label labelPing;
		public GUIBase_Label labelPlayers;
		public GUIBase_Label labelServerName;
		public uLink.HostData hostData;
		public IPEndPoint EndPoint;
	};

	int mSelectedIndex = -1;
	const int maxLines = 6;
	ServerLineGui[] linesGui = new ServerLineGui[maxLines];
	GUIBase_Button.TouchDelegate[] selectDelegates = new GUIBase_Button.TouchDelegate[maxLines];

	void AwakeSelectDelegates()
	{
		selectDelegates[0] = OnSelect0;
		selectDelegates[1] = OnSelect1;
		selectDelegates[2] = OnSelect2;
		selectDelegates[3] = OnSelect3;
		selectDelegates[4] = OnSelect4;
		selectDelegates[5] = OnSelect5;
	}

	void Awake()
	{
		hostFilter = new uLink.HostDataFilter();

		// currently the comment field is used to filter out non-compatible servers...
		hostFilter.comment = NetUtils.CurrentVersion.ToString();

		//TODO ::
		AwakeSelectDelegates();
	}

	void Start()
	{
		/*
		// try to connect to the uLobby, so we can enumerate servers
		if( null != LobbyClient.Instance )
		{
			//TODO disabled for now becasue there would be some concurency with GuiScreenLobby
			// LobbyClient.ConnectToLobby( NetUtils.GeoRegion.Debug );
		}
		*/
	}

	void Update()
	{
		// Get host data array from master server as fast as possible (retry every one second) and then keep on 
		//refreshing it (very 6 seconds) to see ping times and number of players 
		// for every game server with nice up-to-date numbers.

		if (IsVisible)
		{
			lobbyHostData = uLink.MasterServer.PollAndRequestHostList(hostFilter, masterServerRefreshHostListIntervalSec);
			masterServerRefreshHostListIntervalSec = (lobbyHostData.Length != 0) ? 8F : 2F;
            //poll every 2 sec until we have a list. then poll every 8 seconds.
		}
	}

	void uLink_OnMasterServerEvent(uLink.MasterServerEvent msEvent)
	{
		if (msEvent == uLink.MasterServerEvent.HostListReceived)
		{
			//Debug.Log("DAVE: Got host list: " + msEvent);

			foreach (uLink.HostData hostData in lobbyHostData)
			{
				uLink.MasterServer.AddKnownHostData(hostData);
			}
		}
	}

	/* TODO moved from GuiMenuMain. Is it still necesary ????
	void UpadatePlayerProfile()
	{
        PPIManager.Instance.GetLocalPPI().Name = PlayerName;
	}
	*/

	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_ScreenPivot = GetPivot(s_PivotName);
		m_ScreenLayout = GetLayout(s_PivotName, s_ScreenLayoutName);

		//PrepareButton(m_ScreenLayout, s_BackButton, null, OnServerBack);
		PrepareButton(m_ScreenLayout, s_ConnectButton, null, OnConnect);

		m_LanButton = PrepareButton(m_ScreenLayout, s_LanButtonName, OnLanButton, null);
		m_LobbyButton = PrepareButton(m_ScreenLayout, s_LobbyButtonName, OnLobbyButton, null);
		m_FilterLabel = PrepareLabel(m_ScreenLayout, s_FilterLabelName);

		InitServerList();

		OnLanButton(null); //  default list			

		//if(uLobby.Lobby.isConnected == false)
		//	uLobby.Lobby.ConnectAsClient(Game.Instance.lobbyIP, Game.Instance.lobbyPort);
	}

	protected override void OnViewShow()
	{
		InvokeRepeating("RefreshLocalHosts", 0, 3.0f);
		MFGuiManager.Instance.ShowPivot(m_ScreenPivot, true);

		base.OnViewShow();
	}

	protected override void OnViewHide()
	{
		CancelInvoke("RefreshLocalHosts");
		MFGuiManager.Instance.ShowPivot(m_ScreenPivot, false);
		base.OnViewHide();
	}

	protected override void OnViewUpdate()
	{
		switch (m_ServerListType)
		{
		case E_ServerListType.Lobby:
			UpdateLobby();
			break;
		case E_ServerListType.Lan:
			UpdateLocalHosts();
			break;
		}

		// TODO: it would be nice to have it in an Init method but I tried and it does not work there.
		GUIBase_Button button = GetWidget(m_ScreenLayout, s_BackButton).GetComponent<GUIBase_Button>();
		button.Widget.ShowImmediate(false, true);

		base.OnViewUpdate();
	}

	protected override void OnViewDestroy()
	{
		CancelInvoke("RefreshLocalHosts");
		base.OnViewDestroy();
	}

	// #################################################################################################################
	// ###  Delegates  #################################################################################################

	void OnConnect(GUIBase_Widget inWidget)
	{
		if (mSelectedIndex == -1)
			return;

		//Debug.Log("Going to connect server on line " + mSelectedIndex);

		//UpadatePlayerProfile();
		ServerLineGui line = linesGui[mSelectedIndex];

		switch (m_ServerListType)
		{
		case E_ServerListType.Lan:
		{
			if (line.hostData != null)
			{
				ConnectToServer(line.hostData);
			}
		}
			break;
		case E_ServerListType.Lobby:
		{
			if (line.hostData != null)
			{
				ConnectToServer(line.hostData);
			}
			else
			{
				if (null != line.EndPoint)
				{
					ConnectToServer(line.EndPoint);
				}
			}
		}
			break;
		}
	}

	void OnServerBack(GUIBase_Widget inWidget)
	{
		Owner.ShowScreen("LobbyHack", true);
	}

	void RefreshLocalHosts()
	{
		if (m_ServerListType == E_ServerListType.Lan)
		{
			// That server port is too big (several hundreds) and it is causing problems on wifi routers because of hundreds broadcasted messages sent to those ports
			//uLink.MasterServer.DiscoverLocalHosts(hostFilter, NetUtils.ServerPortMin, NetUtils.ServerPortMax);

			uLink.MasterServer.DiscoverLocalHosts(hostFilter, NetUtils.ServerPortMin, NetUtils.ServerPortMin + 4); //five servers per machine
		}
	}

	void OnLanButton(GUIBase_Widget inWidget)
	{
		//Debug.Log("OnLanButton");
		m_ServerListType = E_ServerListType.Lan;
		m_FilterLabel.SetNewText(0109033);
		m_LanButton.Widget.Color = Color.gray;
		m_LobbyButton.Widget.Color = Color.white;

		RefreshLocalHosts();
	}

	void OnLobbyButton(GUIBase_Widget inWidget)
	{
		m_ServerListType = E_ServerListType.Lobby;
		m_FilterLabel.SetNewText(0109034);
		m_LanButton.Widget.Color = Color.white;
		m_LobbyButton.Widget.Color = Color.gray;
	}

	void InitServerList()
	{
		//server line
		for (int i = 0; i < maxLines; i++)
		{
			string btnName = s_ServerLinePrefix + i;

			linesGui[i] = new ServerLineGui();
			linesGui[i].button = GuiBaseUtils.RegisterButtonDelegate(m_ScreenLayout, btnName, selectDelegates[i], null);

			//Debug.Log("Register " +i + " " + btnName + " " + linesGui[i].button);

			//gui for server list lines
			GUIBase_Label[] labels = linesGui[i].button.Widget.GetComponentsInChildren<GUIBase_Label>();
			foreach (GUIBase_Label l in labels)
			{
				if (l.name == "Mapname_Label")
					linesGui[i].labelMapName = l;
				if (l.name == "Mode_Label")
					linesGui[i].labelMode = l;
				else if (l.name == "Ping_Label")
					linesGui[i].labelPing = l;
				else if (l.name == "Players_Label")
					linesGui[i].labelPlayers = l;
				else if (l.name == "Servermame_Label")
					linesGui[i].labelServerName = l;

				l.Clear();
			}
		}
	}

	public void ShowStatusMessage( /*int*/ string msgID)
	{
		//TODO: zobrazeni message v menu, nacitani z text database
		//Debug.Log(msgID);
	}

	void UpdateLobby()
	{
		//uLink.HostData[] hostData = uLink.MasterServer.PollHostList(hostFilter, masterServerRefreshHostListIntervalSec);
		// UpdateScreenList(lobbyHostData);

		if (LobbyClient.IsConnected)
		{
			int index = 0;

			List<LobbyClient.Server> Servers = LobbyClient.GetServers();

			foreach (LobbyClient.Server Server in Servers)
			{
				UpdateHostLine(index, Server);
				index++;
			}

			//pokd jeste neni vybrany radek (nebo je mimo zobrazeny rozsah), vyber prvni radek
			if ((mSelectedIndex == -1 || mSelectedIndex >= index) && index > 0)
			{
				//but only when widget is already visible
				int hg = 0;

				ServerLineGui line = linesGui[hg];

				if (line.button.Widget.IsVisible())
					SelectServerLine(hg);
			}
			else if (index == 0)
			{
				SelectServerLine(-1);
			}

			//skryj zbytek nevyuzitych radku
			for (int i = index; i < maxLines; i++)
			{
				HideServerLine(i);
			}
		}
	}

	void UpdateHostLine(int index, LobbyClient.Server Server)
	{
		if (index < 0 || index >= linesGui.Length)
			return;

		//Debug.Log("UpdateServerLine " + index);
		ServerLineGui line = linesGui[index];

		if (line != null)
		{
			if (!line.button.Widget.IsVisible())
			{
				ShowServerLine(index);
			}

			//Game Name
			line.labelServerName.SetNewText(Server.Name);


			line.labelMapName.SetNewText(Server.IPName);

			line.labelPing.Clear();
			//Show ping times only for game server hosts found via the master server, not LAN servers. LAN server ping times might have a bug in uLink.
			/*if (m_ServerListType == E_ServerListType.Lobby)
			{
				//Prints the ping to the master server, not the actual game server. This is OK as long as the 
				//game servers and the master server is hosted in the same datacenter. 
				//TODO: remove this GUI column from the list of game servers and put the ping value on top of the screen 
				//becuse the ping value will be identiacal for all game servers in the same data center
				//line.labelPing.SetNewText(((int)(uLink.MasterServer.ping * 1000)).ToString());
			}*/
			line.labelPlayers.Clear();
			//line.labelPlayers.SetNewText(data.connectedPlayers.ToString());

			line.hostData = null;
			line.EndPoint = Server.EndPoint;
		}
	}

	void UpdateLocalHosts()
	{
		uLink.HostData[] hostData = uLink.MasterServer.PollDiscoveredHosts();
		//Debug.Log(".........hostData: " + hostData.Length);

		UpdateScreenList(hostData);
	}

	bool IsLocalHost(uLink.HostData hostData)
	{
		if (null != hostData)
		{
			try
			{
				IPAddress[] hostIPs = Dns.GetHostAddresses(hostData.ipAddress);
				IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());

				foreach (IPAddress hostIP in hostIPs)
				{
					if (IPAddress.IsLoopback(hostIP))
					{
						return true;
					}

					foreach (IPAddress localIP in localIPs)
					{
						if (hostIP.Equals(localIP))
						{
							return true;
						}
					}
				}
			}
			catch
			{
			}
		}

		return false;
	}

	void UpdateScreenList(uLink.HostData[] hostData)
	{
		int index = 0;

		foreach (uLink.HostData data in hostData)
		{
			if (IsLocalHost(data))
			{
				UpdateHostLine(index, data);
				index++;
			}
		}

		foreach (uLink.HostData data in hostData)
		{
			if (!IsLocalHost(data))
			{
				UpdateHostLine(index, data);
				index++;
			}
		}

		//pokd jeste neni vybrany radek (nebo je mimo zobrazeny rozsah), vyber prvni radek
		if ((mSelectedIndex == -1 || mSelectedIndex >= index) && index > 0)
		{
			//but only when widget is already visible
			int hg = 0;
			ServerLineGui line = linesGui[hg];
			if (line.button.Widget.IsVisible())
				SelectServerLine(hg);
		}
		else if (index == 0)
		{
			SelectServerLine(-1);
		}

		//skryj zbytek nevyuzitych radku
		for (int i = index; i < maxLines; i++)
		{
			HideServerLine(i);
		}
	}

	void UpdateHostLine(int index, uLink.HostData data)
	{
		if (PlayerControlsDrone.Enabled)
		{
			if (!isConnectingToServer || failedToConnectToServer)
			{
				if (uLink.Network.status != uLink.NetworkStatus.Connected && uLink.Network.status != uLink.NetworkStatus.Connecting)
				{
					if (data.externalIP == PlayerControlsDrone.DesiredIP)
					{
						ConnectToServer(data);
					}
				}
			}
		}

		if (index < 0 || index >= linesGui.Length)
			return;

		//Debug.Log("UpdateServerLine " + index);
		ServerLineGui line = linesGui[index];
		if (line != null)
		{
			if (!line.button.Widget.IsVisible())
			{
				ShowServerLine(index);
			}

			//We have no server name but ip:port is a good identifier and a very usefull information
			line.labelServerName.SetNewText(data.ipAddress + ":" + data.port);

			//IP
			line.labelMapName.SetNewText(data.gameName);
			line.labelMode.SetNewText(data.gameMode);

			//Show ping times only for game server hosts found via the master server, not LAN servers. LAN server ping times might have a bug in uLink.
			if (m_ServerListType == E_ServerListType.Lobby)
			{
				//Prints the ping to the master server, not the actual game server. This is OK as long as the 
				//game servers and the master server is hosted in the same datacenter. 
				//TODO: remove this GUI column from the list of game servers and put the ping value on top of the screen 
				//becuse the ping value will be identiacal for all game servers in the same data center
				line.labelPing.SetNewText(((int)(uLink.MasterServer.ping*1000)).ToString());
			}
			else
			{
				line.labelPing.Clear();
			}
			line.labelPlayers.SetNewText(data.connectedPlayers.ToString());

			line.hostData = data;
		}
	}

	void ConnectToServer(IPEndPoint EndPoint, int joinRequestId = 0)
	{
		Game.Instance.StartNewMultiplayerGame(EndPoint, joinRequestId);

		isConnectingToServer = true;
		selectedServerEndpoint = EndPoint;
		failedToConnectToServer = false;
	}

	void ConnectToServer(uLink.HostData hostData, int joinRequestId = 0)
	{
		Game.Instance.StartNewMultiplayerGame(hostData, joinRequestId);

		isConnectingToServer = true;
		selectedServerEndpoint = hostData.internalEndpoint;
		failedToConnectToServer = false;
	}

/*	void uLink_OnPreBufferedRPCs(uLink.NetworkBufferedRPC[] rpcs)
	{
		foreach (uLink.NetworkBufferedRPC rpc in rpcs)
			rpc.DontExecuteOnConnected();

		BufferedRPCs = rpcs;
	}*/

	void uLink_OnConnectedToServer(System.Net.IPEndPoint server)
	{
		isConnectingToServer = false;
	}

	void uLink_OnFailedToConnectToServer(uLink.NetworkConnectionError error)
	{
		failedToConnectToServer = true;
		connectionError = error;
		isConnectingToServer = false;
	}

	void SelectServerLine(int lineIndex)
	{
		if (mSelectedIndex != -1)
			HighlightLine(mSelectedIndex, false);

		mSelectedIndex = lineIndex;
		HighlightLine(mSelectedIndex, true);
	}

	void OnSelect0()
	{
		SelectServerLine(0);
	}

	void OnSelect1()
	{
		SelectServerLine(1);
	}

	void OnSelect2()
	{
		SelectServerLine(2);
	}

	void OnSelect3()
	{
		SelectServerLine(3);
	}

	void OnSelect4()
	{
		SelectServerLine(4);
	}

	void OnSelect5()
	{
		SelectServerLine(5);
	}

	void HideServerLine(int index)
	{
		ServerLineGui line = linesGui[index];

		//Debug.Log("Hiding line " + index + " " + line);
		if (line != null)
		{
			line.button.Widget.Show(false, true);
			line.labelMapName.Widget.Show(false, true);
			line.labelMode.Widget.Show(false, true);
			line.labelPing.Widget.Show(false, true);
			line.labelPlayers.Widget.Show(false, true);
			line.labelServerName.Widget.Show(false, true);
		}
	}

	void ShowServerLine(int index)
	{
		ServerLineGui line = linesGui[index];
		
		//Debug.Log("Hiding line " + index + " " + line);
		if (line != null)
		{
			line.button.Widget.Show(true, true);
			line.labelMapName.Widget.Show(true, true);
			line.labelMode.Widget.Show(true, true);
			line.labelPing.Widget.Show(true, true);
			line.labelPlayers.Widget.Show(true, true);
			line.labelServerName.Widget.Show(true, true);
		}
	}

	void HighlightLine(int index, bool on)
	{
		if (index < 0 || index >= linesGui.Length)
			return;

		ServerLineGui line = linesGui[index];
		if (line.button.Widget.IsVisible())
			line.button.ForceHighlight(on);
	}

	// #################################################################################################################	
	enum E_ServerListType
	{
		Lobby,
		Lan,
	};
	E_ServerListType m_ServerListType = E_ServerListType.Lobby;

	static string s_PivotName = "MainPlayServer";
	static string s_ScreenLayoutName = "01Buttons_Layout";

	static string s_BackButton = "Back_Button";
	static string s_LanButtonName = "Lan_Button";
	static string s_LobbyButtonName = "Lobby_Button";
	static string s_FilterLabelName = "Filter_Label";

	static string s_ConnectButton = "Connect_Button";
	static string s_ServerLinePrefix = "Server01_Button_";

	GUIBase_Label m_FilterLabel;
	GUIBase_Button m_LanButton;
	GUIBase_Button m_LobbyButton;
}
