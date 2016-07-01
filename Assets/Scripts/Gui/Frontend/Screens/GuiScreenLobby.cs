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

[AddComponentMenu("GUI/Frontend/Screens/GuiScreenLobby")]
public class GuiScreenLobby : GuiScreenMultiPage, IGuiOverlayScreen
{
	readonly static string[] BUTTONS = {"Random_Button", "Gang_Button", "Develop_Button"};
	readonly static string VOICECHAT_SWITCH = "Voicechat_Switch";

	// PRIVATE MEMBERS

	GUIBase_Button[] m_Buttons = new GUIBase_Button[BUTTONS.Length];
	Dictionary<int, int> m_RequestMap = new Dictionary<int, int>();
	GUIBase_Switch m_VoiceChatSwitch;
	bool m_PreferVoiceChat;

	// PUBLIC MEMBERS

	public bool PreferVoiceChat
	{
		get { return m_PreferVoiceChat; }
	}

	// PUBLIC METHODS

	public void RegisterRequest(int joinRequestId)
	{
		m_RequestMap[joinRequestId] = CurrentPageIndex;
	}

	// IGUIOVERLAYSCREEN INTERFACE

	string IGuiOverlayScreen.OverlayButtonTooltip
	{
		get { return IsVisible == false && HasFirstGameBonus() ? "2xXP" : null; }
	}

	bool IGuiOverlayScreen.HideOverlayButton
	{
		get { return Ftue.IsActionIdle<FtueAction.DeathMatch>(); }
	}

	bool IGuiOverlayScreen.HighlightOverlayButton
	{
		get
		{
			FtueAction.Base action = Ftue.ActiveAction ?? Ftue.PendingAction;
			if (action is FtueAction.DeathMatch)
				return true;
			if (action is FtueAction.ZoneControl)
				return true;
			if (action is FtueAction.RankUp)
				return true;
			if (action != null && action.ShouldBeIngame == true)
				return true;
			if (Ftue.IsActive == true)
				return false;
			return HasFirstGameBonus();
		}
	}

	// GUISCREENMULTIPAGE INTERFACE

	protected override void OnPageVisible(GuiScreen page)
	{
		if (CurrentPageIndex < 0 || CurrentPageIndex >= m_Buttons.Length)
			return;

		m_Buttons[CurrentPageIndex].stayDown = true;
		m_Buttons[CurrentPageIndex].ForceDownStatus(true);
	}

	protected override void OnPageHiding(GuiScreen page)
	{
		if (CurrentPageIndex < 0 || CurrentPageIndex >= m_Buttons.Length)
			return;

		m_Buttons[CurrentPageIndex].stayDown = false;
		m_Buttons[CurrentPageIndex].ForceDownStatus(false);
	}

	// GUISCREEN INTERFACE

	protected override void OnViewInit()
	{
		base.OnViewInit();

		for (int idx = 0; idx < m_Buttons.Length; ++idx)
		{
			m_Buttons[idx] = GuiBaseUtils.GetControl<GUIBase_Button>(Layout, BUTTONS[idx]);
		}
	}

	protected override void OnViewDestroy()
	{
		base.OnViewDestroy();
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		m_PreferVoiceChat = Game.Settings.GetBool("PreferVoiceChat", false);

		bool isTutorial = Ftue.IsActive;

		// bind buttons
		for (int idx = 0; idx < m_Buttons.Length; ++idx)
		{
			int pageId = idx;
			GuiBaseUtils.RegisterButtonDelegate(m_Buttons[idx],
												() =>
												{
													if (CurrentPageIndex != pageId)
													{
														GotoPage(pageId);
													}
												},
												null);
			if (idx > 0 && isTutorial == true)
			{
				m_Buttons[idx].Widget.Show(false, true);
			}
		}

		m_VoiceChatSwitch = RegisterSwitchDelegate(VOICECHAT_SWITCH, (state) => { m_PreferVoiceChat = state; });
		m_VoiceChatSwitch.SetValue(m_PreferVoiceChat);

		LobbyClient.OnServerFound += ServerFound;
		LobbyClient.OnNoServerAvailable += NoServerAvailable;

		// Disable the voice chat switch but keep it in the dialog data. Maybe we will re-use it later
		// for something else
		m_VoiceChatSwitch.Widget.Show(false,true);
	}

	protected override void OnViewHide()
	{
		LobbyClient.OnServerFound -= ServerFound;
		LobbyClient.OnNoServerAvailable -= NoServerAvailable;

		// unbind buttons
		for (int idx = 0; idx < m_Buttons.Length; ++idx)
		{
			GuiBaseUtils.RegisterButtonDelegate(m_Buttons[idx], null, null);
		}

		RegisterSwitchDelegate(VOICECHAT_SWITCH, null);

		Game.Settings.SetBool("PreferVoiceChat", m_PreferVoiceChat);

		// call super
		base.OnViewHide();
	}

	protected override void OnViewUpdate()
	{
		base.OnViewUpdate();

		bool enabled = LobbyClient.IsConnected ? CloudUser.instance.isPremiumAccountActive : false;
		m_VoiceChatSwitch.IsDisabled = !enabled;
	}

	// PRIVATE METHODS

	void ServerFound(int clientJoinRequestId, int serverJoinRequestId, string ipAddress, int port)
	{
		GuiScreenLobbyBase page = GetLobbyPage(clientJoinRequestId);
		if (page != null)
		{
			page.ServerFound(serverJoinRequestId, ipAddress, port);
		}
	}

	void NoServerAvailable(int clientJoinRequestId)
	{
		GuiScreenLobbyBase page = GetLobbyPage(clientJoinRequestId);
		if (page != null)
		{
			page.NoServerAvailable();
		}
	}

	GuiScreenLobbyBase GetLobbyPage(int clientJoinRequestId)
	{
		int pageIdx;
		if (m_RequestMap.TryGetValue(clientJoinRequestId, out pageIdx) == true)
		{
			return GetPage(pageIdx) as GuiScreenLobbyBase;
		}
		return null;
	}

	bool HasFirstGameBonus()
	{
		PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();
		if (ppi == null)
			return false;

		int rank = ppi.Rank;

		for (E_MPGameType gameType = E_MPGameType.DeathMatch; gameType < E_MPGameType.None; ++gameType)
		{
			GameTypeInfo gameInfo = GameInfoSettings.GetGameInfo(gameType);
			if (gameInfo == null)
				continue;
			if (gameInfo.MinimalDesiredRankToPlay > rank)
				continue;
			if (ppi.IsFirstGameToday(gameType) == false)
				continue;

			return true;
		}

		return false;
	}
}
