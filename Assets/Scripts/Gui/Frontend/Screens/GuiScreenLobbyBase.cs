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
using LitJson;

public abstract class GuiScreenLobbyBase : GuiScreen
{
	bool m_IsConnectingToLobby = false;
	bool m_IsLookingForServer = false;
	bool m_IsConnectingToServer = false;
	E_MPGameType m_GameType = E_MPGameType.DeathMatch;
	string[] m_OtherPlayers = null;

	// internal client's id to identify the answer to the most recent join request (there could be several requests running at parallel)
	static int m_NextJoinRequestId = 0;

	int m_LastJoinRequestId = 0;

	//-----------------------------------------------------------------------------------------------------------------
	protected void Play(E_MPGameType gameType, string[] otherPlayers)
	{
		m_GameType = gameType;
		m_OtherPlayers = otherPlayers.Length > 0 ? otherPlayers : null;

		Play();
	}

	//-----------------------------------------------------------------------------------------------------------------
	protected void Play(E_MPGameType gameType)
	{
		m_GameType = gameType;
		m_OtherPlayers = null;

		Play();
	}

	//-----------------------------------------------------------------------------------------------------------------
	public void ServerFound(int serverJoinRequestId, string ipAddress, int port)
	{
		if (m_IsLookingForServer)
		{
			Popup.Hide();

			m_IsLookingForServer = false;

			System.Net.IPAddress serverAddress = null;
			System.Net.IPAddress.TryParse(ipAddress, out serverAddress);

			System.Net.IPEndPoint serverEndpoint = new System.Net.IPEndPoint(serverAddress, port);

			m_IsConnectingToServer = true;

			// there will be no 'cancel' button - functionality is missing
			Popup.Show(TextDatabase.instance[0109061], TextDatabase.instance[0109054], /*TextDatabase.instance[02040009]*/ null, OnMessageBoxEvent);

			Game.Instance.StartNewMultiplayerGame(serverEndpoint, serverJoinRequestId, OnConnectToServerFailed);
		}
	}

	//-----------------------------------------------------------------------------------------------------------------
	public void NoServerAvailable()
	{
		if (m_IsLookingForServer)
		{
			Popup.Hide();

			m_IsLookingForServer = false;
			//Debug.Log( "MatchMaking: join request has been refused (NoServerAvailable)" );

			Popup.Show(TextDatabase.instance[0109061], TextDatabase.instance[0109055], TextDatabase.instance[02040007], OnMessageBoxEvent);
		}
	}

	void OnConnectToServerFailed(uLink.NetworkConnectionError error)
	{
		Popup.Hide();
		Popup.Show(TextDatabase.instance[0109061],
				   TextDatabase.instance[0109062] + "\n (" + error.ToString() + ")",
				   TextDatabase.instance[02040009],
				   OnMessageBoxEvent);
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	//-----------------------------------------------------------------------------------------------------------------
	void Play()
	{
		if (LobbyClient.IsConnected)
		{
			string gameType = (m_GameType == E_MPGameType.ZoneControl) ? "zc" : "dm";
			bool voiceChat = GetPreferVoiceChat();

			m_LastJoinRequestId = m_NextJoinRequestId;
			m_NextJoinRequestId += 1;

			bool result;

			if (m_OtherPlayers == null)
				result = LobbyClient.FindServerForOnePlayer(m_LastJoinRequestId, gameType, voiceChat);
			else
				result = LobbyClient.FindServerForGang(m_LastJoinRequestId, gameType, voiceChat, m_OtherPlayers);

			if (!result)
			{
				if (LobbyClient.GameVersionNoLongerSupported)
				{
					Popup.Show(TextDatabase.instance[0109061], TextDatabase.instance[0109056], TextDatabase.instance[02040007], OnMessageBoxEvent);
				}
				else
				{
					Popup.Show(TextDatabase.instance[0109061], TextDatabase.instance[0109057], TextDatabase.instance[02040007], OnMessageBoxEvent);
				}

				return;
			}

			m_IsLookingForServer = true;

			GuiScreenLobby lobby = Owner as GuiScreenLobby;
			if (lobby != null)
			{
				lobby.RegisterRequest(m_LastJoinRequestId);
			}

			Popup.Show(TextDatabase.instance[0109061], TextDatabase.instance[0109058], TextDatabase.instance[02040009], OnMessageBoxEvent);
		}
		else
		{
			LobbyClient.ToLobbyConnectionResult += OnLobbyConnectionResult;

			m_IsConnectingToLobby = true;
			LobbyClient.ConnectToLobby(CloudUser.instance.region);

			Popup.Show(TextDatabase.instance[0109061], TextDatabase.instance[0109059], TextDatabase.instance[02040009], OnMessageBoxEvent);
		}
	}

	//-----------------------------------------------------------------------------------------------------------------
	void OnMessageBoxEvent(GuiPopup popup, E_PopupResultCode result)
	{
		if (m_IsConnectingToLobby)
		{
			m_IsConnectingToLobby = false;
			LobbyClient.ToLobbyConnectionResult -= OnLobbyConnectionResult;
		}

		if (m_IsLookingForServer)
		{
			if (result == E_PopupResultCode.Ok) // User pressed the cancel button
			{
				LobbyClient.CancelFindServer(m_LastJoinRequestId);
			}

			m_IsLookingForServer = false;
		}

		if (m_IsConnectingToServer)
		{
			m_IsConnectingToServer = false;
		}
	}

	//-----------------------------------------------------------------------------------------------------------------
	void OnLobbyConnectionResult(bool connected)
	{
		LobbyClient.ToLobbyConnectionResult -= OnLobbyConnectionResult;

		Popup.Hide();

		if (connected)
		{
			Play();
		}
		else
		{
			Popup.Show(TextDatabase.instance[0109061], TextDatabase.instance[0109060], TextDatabase.instance[02040007], null);
		}
	}

	bool GetPreferVoiceChat()
	{
#if UNITY_STANDALONE_WIN
		string[] arguments = System.Environment.GetCommandLineArgs();
		
		if (arguments != null)
		{
			foreach (string str in arguments)
			{
				if (str.StartsWith("-voice="))
				{
					string[] param = str.Split('=');
					return System.Convert.ToInt32(param[1]) == 1;
				}
			}
		}
#endif

		GuiScreenLobby lobby = Owner as GuiScreenLobby;
		return lobby ? lobby.PreferVoiceChat : false;
	}

	string TimeToString(int time)
	{
		if (time < 5)
			return "0_5";
		else if (time < 10)
			return "5_10";
		else if (time < 15)
			return "10_15";
		else if (time < 20)
			return "15_20";
		else if (time < 30)
			return "20_30";
		else if (time < 60)
			return "30_60";

		return "30_N";
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	static class Popup
	{
		static GuiPopupMessageBox m_Popup = null;
		static PopupHandler m_Handler = null;

		//-------------------------------------------------------------------------------------------------------------
		public static void Show(string caption, string message)
		{
			Show(caption, message, null, null);
		}

		//-------------------------------------------------------------------------------------------------------------
		public static void Show(string caption, string message, string button, PopupHandler handler)
		{
			m_Popup = GuiBaseUtils.ShowMessageBox(caption, message, OnResult) as GuiPopupMessageBox;
			m_Handler = null;

			if (m_Popup == null)
				return;

			if ((button != null) && (button.Length > 0))
			{
				m_Popup.SetButtonVisible(true);
				m_Popup.SetButtonText(button);
			}
			else
			{
				m_Popup.SetButtonVisible(false);
			}

			m_Handler = handler;
		}

		//-------------------------------------------------------------------------------------------------------------
		public static void Hide()
		{
			if (m_Popup != null)
			{
				m_Popup.ForceClose();
			}
		}

		//-------------------------------------------------------------------------------------------------------------
		static void OnResult(GuiPopup popup, E_PopupResultCode result)
		{
			m_Popup.SetButtonVisible(true);

			if (m_Handler != null)
			{
				m_Handler(popup, result);
			}
		}
	}
}
