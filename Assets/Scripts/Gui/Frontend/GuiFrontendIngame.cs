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

[AddComponentMenu("GUI/Frontend/GuiFrontendIngame")]
public class GuiFrontendIngame : GuiFrontend<GuiFrontendIngame.E_MenuState>
{
	static GuiFrontendIngame m_Instance;

	public enum E_MenuState
	{
		Idle,
		Spawn,
		Pause,
		Final,
		Spectator,
		Exiting
	}

	// PRIVATE MEMBERS

	GuiHUD m_Hud;
	bool m_HudShouldBeVisible;
	ComponentPlayerLocal m_LocalPlayer;

	// GETTERS/SETTERS

	public static bool IsInitialized
	{
		get { return m_Instance != null ? true : false; }
	}

	public static bool IsVisible
	{
		get { return m_Instance != null ? !m_Instance.IsInState(E_MenuState.Idle) : false; }
	}

	public static bool IsHudVisible
	{
		get { return Hud != null ? Hud.IsVisible : false; }
	}

	static GuiHUD Hud
	{
		get { return m_Instance != null ? m_Instance.m_Hud : null; }
	}

	static ComponentPlayerLocal LocalPlayer
	{
		get { return m_Instance != null ? m_Instance.m_LocalPlayer : null; }
	}

	float LastPauseMenuHideTime = 0;

	public static bool PauseMenuCooldown()
	{
		if (m_Instance != null)
			return (Time.timeSinceLevelLoad < m_Instance.LastPauseMenuHideTime + 0.1f);
		else
			return false;
	}

	// PUBLIC METHODS

	public static bool ShowSpawnMenu()
	{
		if (m_Instance != null)
		{
			m_Instance.DeinitHud();
			m_Instance.m_LocalPlayer = null;
		}
		return OpenMenu(E_MenuState.Spawn);
	}

	public static void HideSpawnMenu()
	{
		GoFromCustomToIdleState(E_MenuState.Spawn);
	}

	public static float DontShowPauseMenuUntil { get; set; }

	public static bool ShowPauseMenu()
	{
		if (m_Instance == null || Time.time < DontShowPauseMenuUntil)
			return false;

		switch (m_Instance.CurrentState)
		{
		case E_MenuState.Spawn:
		case E_MenuState.Final:
			return false;
		default:
			break;
		}
		return OpenMenu(E_MenuState.Pause);
	}

	public static void HidePauseMenu()
	{
		GoFromCustomToIdleState(E_MenuState.Pause);
		if (m_Instance)
		{
			m_Instance.LastPauseMenuHideTime = Time.timeSinceLevelLoad;
		}
	}

	public static bool ShowScoreMenu()
	{
		if (Hud == null)
			return false;
		if (Hud.IsVisible == false)
			return false;

		Hud.ShowScore();

		return true;
	}

	public static void HideScoreMenu()
	{
		if (Hud == null)
			return;

		Hud.HideScore();
	}

	public static bool ShowFinalMenu(string screenName = null)
	{
		if (m_Instance.CurrentState != E_MenuState.Final)
		{
			if (OpenMenu(E_MenuState.Final) == false)
				return false;
		}

		if (screenName != null)
		{
			m_Instance.CurrentMenu.ShowScreen(screenName);
		}

		return true;
	}

	public static void HideFinalMenu()
	{
		GoFromCustomToIdleState(E_MenuState.Final);
	}

	public static bool ShowSpectatorMenu()
	{
		return OpenMenu(E_MenuState.Spectator);
	}

	public static void HideSpectatorMenu()
	{
		GoFromCustomToIdleState(E_MenuState.Spectator);
	}

	public static GuiPopupMessageBox ShowMessageBox(int captionID, int textID, PopupHandler handler = null)
	{
		if (m_Instance == null)
			return null;
		if (m_Instance.CurrentMenu == null)
			return null;

		return m_Instance.CurrentMenu.ShowMessageBox(captionID, textID, handler);
	}

	public static GuiPopupMessageBox ShowMessageBox(string caption, string text, PopupHandler handler = null)
	{
		if (m_Instance == null)
			return null;
		if (m_Instance.CurrentMenu == null)
			return null;

		return m_Instance.CurrentMenu.ShowMessageBox(caption, text, handler);
	}

	public static GuiPopupConfirmDialog ShowConfirmDialog(int captionID, int textID, PopupHandler handler = null)
	{
		if (m_Instance == null)
			return null;
		if (m_Instance.CurrentMenu == null)
			return null;

		return m_Instance.CurrentMenu.ShowConfirmDialog(captionID, textID, handler);
	}

	public static GuiPopupConfirmDialog ShowConfirmDialog(string caption, string text, PopupHandler handler = null)
	{
		if (m_Instance == null)
			return null;
		if (m_Instance.CurrentMenu == null)
			return null;

		return m_Instance.CurrentMenu.ShowConfirmDialog(caption, text, handler);
	}

	public static void HideAll()
	{
		if (m_Instance != null)
		{
			m_Instance.m_HudShouldBeVisible = false;
		}

		GotoIdleState();
	}

	public static void GotoMainMenu()
	{
		if (m_Instance == null)
			return;

		if (m_Instance.IsInState(E_MenuState.Exiting) == true)
			return;

		// hide any open menu
		m_Instance.SetState(E_MenuState.Exiting);

		// disconnect client
		Client.DisconnectFromServer();
	}

	public static void Clear()
	{
		if (m_Instance == null)
			return;

		if (m_Instance.m_Hud != null)
		{
			m_Instance.m_Hud.Hide();
		}

		m_Instance.SetState(E_MenuState.Idle);
		m_Instance.StopAllCoroutines();
		m_Instance.CancelInvoke();
		m_Instance = null;
	}

	// MONOBEHAVIOUR INTERFACE

	void Awake()
	{
		// store reference to instance
		m_Instance = this;

		// register delegates
		ComponentPlayerMPOwner.OnActivated += OnComponentPlayerActivated;
		ComponentPlayerMPOwner.OnDeactivated += OnComponentPlayerDeactivated;

		// get hud
		m_Hud = GuiHUD.Instance;

		// register menus for states
		RegisterMenu<GuiMenuSpawn>(E_MenuState.Spawn);
		RegisterMenu<GuiMenuPause>(E_MenuState.Pause);
		RegisterMenu<GuiMenuFinal>(E_MenuState.Final);
		RegisterMenu<GuiMenuSpectator>(E_MenuState.Spectator);
	}

	protected override void OnDestroy()
	{
		// unregister delegates
		ComponentPlayerMPOwner.OnActivated -= OnComponentPlayerActivated;
		ComponentPlayerMPOwner.OnDeactivated -= OnComponentPlayerDeactivated;

		// reset reference to instance
		m_Instance = null;

		base.OnDestroy();
	}

	void Update()
	{
		if (IsHudVisible == true)
		{
			Hud.OnUpdate();
		}
	}

	void LateUpdate()
	{
		if (IsHudVisible == true)
		{
			Hud.OnLateUpdate();
		}

		if (IsVisible == false)
			return;

		if (CurrentMenu == null)
			return;

		GuiMenu menu = CurrentMenu;
		if (menu != null)
		{
			menu.UpdateMenu();
		}
	}

	// HANDLERS

	void OnComponentPlayerActivated(ComponentPlayerMPOwner localPlayer)
	{
		if (Game.Instance.GameLog)
			Debug.Log(">>>> PLAYER ACTIVATED :: m_LocalPlayer=" + (m_LocalPlayer ? m_LocalPlayer.GetInstanceID().ToString() : "null") +
					  ", localPlayer=" + localPlayer.GetInstanceID());

		m_LocalPlayer = localPlayer;

		InitHud();

		GotoIdleState();
	}

	void OnComponentPlayerDeactivated(ComponentPlayerMPOwner localPlayer)
	{
		if (Game.Instance.GameLog)
			Debug.Log(">>>> PLAYER DEACTIVATED :: m_LocalPlayer=" + (m_LocalPlayer ? m_LocalPlayer.GetInstanceID().ToString() : "null") +
					  ", localPlayer=" + localPlayer.GetInstanceID());

		if (m_LocalPlayer != localPlayer)
			return;

		DeinitHud();

		m_LocalPlayer = null;
	}

	// PRIVATE METHODS

	static bool CanChangeToState(E_MenuState state)
	{
		if (m_Instance == null)
			return false;
		if (m_Instance.IsInState(E_MenuState.Exiting) == true)
			return false;
		if (m_Instance.IsInState(state) == true)
			return false;
		return true;
	}

	static bool OpenMenu(E_MenuState state)
	{
		if (CanChangeToState(state) == false)
			return false;

		GuiMenu menu = m_Instance.SetState(state);
		if (menu == null)
			return false;

		m_Instance.UpdateHudVisibility(false);

		if (menu.IsInitialized == false)
		{
			menu.InitMenu(m_Instance);
		}

		menu.ShowMenu();

		return true;
	}

	static bool GotoIdleState()
	{
		if (CanChangeToState(E_MenuState.Idle) == false)
			return false;

		m_Instance.SetState(E_MenuState.Idle);

		m_Instance.UpdateHudVisibility(true);

		return true;
	}

	static bool GoFromCustomToIdleState(E_MenuState state)
	{
		if (m_Instance == null)
			return false;

		if (m_Instance.IsInState(state) == false)
			return false;

		return GotoIdleState();
	}

	void InitHud()
	{
		if (Hud.IsInitialized == true)
			return;

		m_HudShouldBeVisible = true;

		if (Hud != null)
		{
			Hud.Init(m_LocalPlayer);
		}
	}

	void DeinitHud()
	{
		if (Hud.IsInitialized == false)
			return;

		if (Hud != null)
		{
			UpdateHudVisibility(false);
			Hud.Deinit(m_LocalPlayer);
		}

		m_HudShouldBeVisible = false;
	}

	void UpdateHudVisibility(bool state)
	{
		if (m_Instance == null)
			return;
		if (Hud == null)
			return;

		if (state == true && m_Instance.m_HudShouldBeVisible == true)
		{
			Time.timeScale = 1.0f;
			AudioListener.pause = false;

			if (LocalPlayer != null && LocalPlayer.Controls != null)
			{
				LocalPlayer.Controls.LockCursor(true);
			}

			Hud.Show();
		}
		else
		{
			Hud.Hide();

			if (LocalPlayer != null && LocalPlayer.Controls != null)
			{
				LocalPlayer.Controls.LockCursor(false);
			}

//			AudioListener.pause = true;						//beny: we're not 100% sure if this is needed for SOME E_MenuState, but we're sure it is not desirable for states like Spawn or Spactator.
			//Time.timeScale      = 0; //zrusene pro MP
		}
	}
}
