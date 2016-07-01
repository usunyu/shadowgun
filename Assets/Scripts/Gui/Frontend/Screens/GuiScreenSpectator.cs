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

public class GuiScreenSpectator : GuiScreen
{
	GUIBase_Button m_Minimize_Button;
	GUIBase_Widget m_FollowPlayer_Dummy;
	GUIBase_Button m_FollowPlayer_Button;
	GUIBase_Label m_PlayerName_Label;
	GUIBase_Button m_Cancel_Button;
	GUIBase_Button m_Feedback_Button;
	GUIBase_Widget m_NextSpawn_Dummy;
	GUIBase_Label m_NextSpawn_Label;
	GUIBase_Button m_ScoreButton;
	GUIBase_Button m_Spawn_Button;
	GUIBase_Button m_AnticheatButton;

	GuiScreenScore m_Score;

	GadgetZoneControlState m_ZoneControlState;
	GadgetDeathMatchState m_DeathMatchState;

	bool m_Minimized;

	public bool FollowingPlayer
	{
		get { return GameCamera.GetCurrentMode() == GameCamera.E_State.Spectator_FollowPlayer ? true : false; }
	}

	public bool WaitingForSpawn
	{
		get { return Client.Instance != null && Client.Instance.GameState.State == Client.ClientGameState.WaitingForSpawn ? true : false; }
	}

	// =================================================================================================================

	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_Minimize_Button = PrepareButton("Minimize_Button", null, OnMinimize);
		m_Spawn_Button = PrepareButton("Spawn_Button", null, OnSpawn);

		m_FollowPlayer_Dummy = GetWidget("FollowPlayer_Dummy");
		m_PlayerName_Label = PrepareLabel("PlayerName_Label");
		m_FollowPlayer_Button = PrepareButton("FollowPlayer_Button", null, OnFollowPlayer);
		PrepareButton("PrevPlayer_Button", null, OnPrevPlayer);
		PrepareButton("NextPlayer_Button", null, OnNextPlayer);

		m_Cancel_Button = PrepareButton("Cancel_Button", null, OnCancel);
		m_Feedback_Button = PrepareButton("Feedback_Button", null, OnFeedback);

		m_NextSpawn_Dummy = GetWidget("NextSpawn_Dummy");
		m_NextSpawn_Label = PrepareLabel("NextSpawn");

		m_ScoreButton = PrepareButton("ScoreButton", OnScorePressed, OnScoreReleased);
		m_AnticheatButton = PrepareButton("AnticheatButton", null, OnAnticheatPressed);

		if (m_Feedback_Button != null)
		{
			bool showFeedback = BuildInfo.Version.Stage == BuildInfo.Stage.Beta;
			m_Feedback_Button.Widget.m_VisibleOnLayoutShow = showFeedback;
		}

		if (m_Score == null)
		{
			m_Score = gameObject.AddComponent<GuiScreenScore>();
			m_Score.InitView();
			m_Score.HideView(null);
		}

		Client client = Client.Instance;
		if (client != null)
		{
			switch (client.GameState.GameType)
			{
			case E_MPGameType.ZoneControl:
				m_ZoneControlState = new GadgetZoneControlState(GetWidget("Domination_State"));
				break;
			case E_MPGameType.DeathMatch:
				m_DeathMatchState = new GadgetDeathMatchState(GetWidget("DeathMatch_State"));
				break;
			default:
				break;
			}
		}

		{
			GUIBase_Widget anchor = Layout.GetWidget("AnchorTop");
			Vector3 pos = anchor.transform.position;
			pos.y = 0.0f;
			anchor.transform.position = pos;
		}

		{
			GUIBase_Widget anchor = Layout.GetWidget("AnchorBottom");
			Vector3 pos = anchor.transform.position;
			pos.y = Screen.height;
			anchor.transform.position = pos;
		}
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		m_Minimized = false;

		var ppi = PPIManager.Instance.GetLocalPPI();
		if (ppi != null && m_NextSpawn_Dummy != null)
		{
			HSLColor color = ZoneControlFlag.Colors[ppi.Team];
			color.L *= 0.8f;

			GUIBase_Sprite[] sprites = m_NextSpawn_Dummy.GetComponentsInChildren<GUIBase_Sprite>();
			foreach (var sprite in sprites)
			{
				sprite.Widget.Color = color;
			}
		}

		UpdateControlsVisibility();
		UpdateButtonsState();
		UpdateZoneControlState();
		UpdateDeathMatchState();
	}

	protected override void OnViewHide()
	{
		m_Score.HideView(null);

		base.OnViewHide();
	}

	protected override void OnViewUpdate()
	{
		base.OnViewUpdate();

		if (m_Score.IsVisible == true)
		{
			m_Score.UpdateView();
		}

		if (WaitingForSpawn == false)
		{
			int minutes = Client.TimeToRespawn/60;
			int seconds = Client.TimeToRespawn%60;
			string time = string.Format("{0:00}:{1:00}", minutes, seconds);

			// Get time to next spawn...
			m_NextSpawn_Label.SetNewText(time);

			if (m_NextSpawn_Label.Widget.Visible == false)
			{
				UpdateControlsVisibility();
			}
		}

		// update following player name
		if (FollowingPlayer == true)
		{
			m_PlayerName_Label.SetNewText(GuiBaseUtils.FixNameForGui(SpectatorCamera.GetSpectatedPlayerName()));
		}

		// update state of 'follow player' and 'free' spectator buttons
		UpdateButtonsState();
		UpdateZoneControlState();
		UpdateDeathMatchState();

		SetAnticheatButtonVisibility(SecurityTools.UserHasAnticheatManagementPermissions());
	}

	protected override bool OnViewProcessInput(ref IInputEvent evt)
	{
		if (evt.Kind == E_EventKind.Key)
		{
			KeyEvent key = (KeyEvent)evt;
			switch (key.Code)
			{
			case KeyCode.Tab:
				if (key.State == E_KeyState.Pressed)
				{
					OnScorePressed(null);
				}
				else if (key.State == E_KeyState.Released)
				{
					OnScoreReleased(null);
				}
				return true;
			case KeyCode.Escape:
				if (Client.Instance.GameState.State != Client.ClientGameState.Running && WaitingForSpawn == false)
				{
					OnCancel(null);
				}
				return true;
			default:
				break;
			}
		}

		return base.OnViewProcessInput(ref evt);
	}

	// =================================================================================================================
	// ===  Delegates  =================================================================================================

	void OnMinimize(GUIBase_Widget inWidget)
	{
		m_Minimized = !m_Minimized;

		m_Minimize_Button.SetNewText(m_Minimized ? 0502031 : 0502030);

		UpdateControlsVisibility();
	}

	void OnSpawn(GUIBase_Widget inWidget)
	{
		Client.Instance.SendRequestForSpawn();
	}

	void OnFollowPlayer(GUIBase_Widget inWidget)
	{
		GameCamera.ChangeMode(FollowingPlayer ? GameCamera.E_State.Spectator_Free : GameCamera.E_State.Spectator_FollowPlayer);

		UpdateControlsVisibility();
	}

	void OnPrevPlayer(GUIBase_Widget inWidget)
	{
		SpectatorCamera.PrevPlayer();
	}

	void OnNextPlayer(GUIBase_Widget inWidget)
	{
		SpectatorCamera.NextPlayer();
	}

	void OnCancel(GUIBase_Widget inWidget)
	{
		GuiFrontendIngame.ShowSpawnMenu();
		//Debug.Log("OnPrev");
	}

	void OnFeedback(GUIBase_Widget inWidget)
	{
		if (m_Feedback_Button.Widget.Visible == true)
		{
#if UNITY_IPHONE || UNITY_ANDROID

			// try to open an email client
			if (EmailHelper.ShowSupportEmailComposerForIOSOrAndroid() == true)
				return;

			Application.OpenURL(Constants.SUPPORT_URL);

#else	
		
			Application.OpenURL(Constants.SUPPORT_URL);
		
#endif
		}
	}

	void OnScorePressed(GUIBase_Widget inWidget)
	{
		if (m_Score.IsVisible == true)
			return;

		m_Score.ShowView(null);
	}

	void OnScoreReleased(GUIBase_Widget inWidget)
	{
		if (m_Score.IsVisible == false)
			return;

		m_Score.HideView(null);
	}

	void OnAnticheatPressed(GUIBase_Widget inWidget)
	{
		SecurityTools.ToggleAnticheatLogging();
	}

	// =================================================================================================================
	// ===  internal  ==================================================================================================

	// update state of 'follow player' and 'free' spectator buttons
	void UpdateButtonsState()
	{
		bool canFollowPlayer = SpectatorCamera.Spectator_CanFollowPlayer();
		m_FollowPlayer_Button.SetNewText(FollowingPlayer ? 0502019 : 0502018);
		m_FollowPlayer_Button.SetDisabled(!canFollowPlayer);

		Client client = Client.Instance;
		if (client != null)
		{
			bool disable = client.GameState.State == Client.ClientGameState.Running || WaitingForSpawn;
			bool canSpawn = true;

			switch (client.GameState.GameType)
			{
			case E_MPGameType.DeathMatch:
				canSpawn = Client.TimeToRespawn <= client.GameState.DMInfo.RestTimeSeconds ? true : false;
				break;
			case E_MPGameType.ZoneControl:
				PlayerPersistantInfo localPPI = PPIManager.Instance.GetLocalPPI();
				canSpawn = localPPI.Team != E_Team.None && localPPI.ZoneIndex >= 0 ? true : false;
				break;
			default:
				Debug.LogError("Unknown Game Type ");
				break;
			}

			m_Cancel_Button.SetDisabled(disable);
			m_Spawn_Button.SetDisabled(disable || !client.IsReadyForSpawn() || GameCloudManager.isBusy || !canSpawn);

			if (canSpawn == false)
			{
				UpdateControlsVisibility();
			}
		}
	}

	void UpdateZoneControlState()
	{
		if (m_ZoneControlState == null)
			return;

		m_ZoneControlState.Update();
	}

	void UpdateDeathMatchState()
	{
		if (m_DeathMatchState == null)
			return;

		m_DeathMatchState.Update();
	}

	void UpdateControlsVisibility()
	{
		Client client = Client.Instance;
		bool canSpawn = client != null && client.GameState.GameType == E_MPGameType.DeathMatch
										? Client.TimeToRespawn <= client.GameState.DMInfo.RestTimeSeconds
										: true;

		m_FollowPlayer_Dummy.Show(!m_Minimized && FollowingPlayer, true);
		m_FollowPlayer_Button.Widget.Show(!m_Minimized, true);
		m_Cancel_Button.Widget.Show(!m_Minimized, true);
		m_ScoreButton.Widget.Show(!m_Minimized, true);

		bool showNextSpawnTimer = !m_Minimized && !WaitingForSpawn && canSpawn;
		m_NextSpawn_Dummy.Show(showNextSpawnTimer, true);

		if (m_ZoneControlState != null)
		{
			m_ZoneControlState.IsVisible = !m_Minimized;
		}

		if (m_DeathMatchState != null)
		{
			m_DeathMatchState.IsVisible = !m_Minimized;
		}

		if (m_Minimized == true)
		{
			OnScoreReleased(null);
		}
	}

	void SetAnticheatButtonVisibility(bool state)
	{
		if (m_AnticheatButton == null)
			return;

		if (m_AnticheatButton.Widget.Visible == state)
			return;

		m_AnticheatButton.Widget.ShowImmediate(state, true);
	}
}
