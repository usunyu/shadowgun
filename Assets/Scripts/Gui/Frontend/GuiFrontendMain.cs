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

[AddComponentMenu("GUI/Frontend/GuiFrontendMain")]
public class GuiFrontendMain : GuiFrontend<GuiFrontendMain.E_MenuState>
{
	static GuiFrontendMain m_Instance;

	public enum E_MenuState
	{
		Idle,
		Login,
		Menu
	}

	// PRIVATE MEMBERS

	// we have to delay initialization, unfortuantely some dependent classes are not initialized in our init...
	int m_HACK_FrameCount = 1;

	// GETTERS/SETTERS

	public static bool IsInitialized
	{
		get { return m_Instance != null ? true : false; }
	}

	public static bool IsVisible
	{
		get { return m_Instance != null ? !m_Instance.IsInState(E_MenuState.Idle) : false; }
	}

	// PUBLIC METHODS

	public static bool ShowLoginMenu()
	{
		//FIXME it does work but there could be some potential problem in the future because this message is called
		//  each tick when player log-outs from his/her account (and stays in login screen)
		LobbyClient.DisconnectFromLobby();

		return OpenMenu(E_MenuState.Login);
	}

	public static void HideLoginMenu()
	{
		CloseMenu(E_MenuState.Login);
	}

	public static void StartMainMenuOrCommand()
	{
		if (!CheckTriggerImmediateCommand())
		{
			ShowMainMenu();
		}
	}

	static bool ShowMainMenu()
	{
		if (OpenMenu(E_MenuState.Menu) == false)
			return false;

		LobbyClient.ConnectToLobby(CloudUser.instance.region);

		if (!ChartBoost.hasCachedMoreApps())
			ChartBoost.cacheMoreApps();

		return true;
	}

	public static void HideMainMenu()
	{
		CloseMenu(E_MenuState.Menu);
	}

	public static GuiPopup ShowPopup(string popupName, string caption, string text, PopupHandler handler = null)
	{
		if (m_Instance == null)
			return null;
		if (m_Instance.CurrentMenu == null)
			return null;

		return m_Instance.CurrentMenu.ShowPopup(popupName, caption, text, handler);
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

	public static void Hide()
	{
		if (m_Instance == null)
			return;

		m_Instance.SetState(E_MenuState.Idle);
	}

	public static void Clear()
	{
		if (m_Instance == null)
			return;

		m_Instance.SetState(E_MenuState.Idle);
		m_Instance.StopAllCoroutines();
		m_Instance.CancelInvoke();
		m_Instance = null;
	}

	// MONOBEHAVIOUR INTERFACE

	void Start()
	{
		m_Instance = this;

		RegisterMenu<GuiMenuLogin>(E_MenuState.Login);
		RegisterMenu<GuiMenuMain>(E_MenuState.Menu);
	}

	void LateUpdate()
	{
		if (--m_HACK_FrameCount > 0)
			return;

		if (CloudUser.instance.isUserAuthenticated == false)
		{
			ShowLoginMenu();
		}
		else if (IsVisible == false)
		{
			ShowMainMenu();
		}

		GuiMenu menu = CurrentMenu;
		if (menu != null)
		{
			menu.UpdateMenu();
		}
	}

	// PRIVATE METHODS

	static GuiMenu PrepareMenu(E_MenuState state)
	{
		if (m_Instance == null)
			return null;

		return m_Instance.SetState(state);
	}

	static bool OpenMenu(E_MenuState state)
	{
		GuiMenu menu = PrepareMenu(state);
		if (menu == null)
			return false;

		if (menu.IsInitialized == false)
		{
			menu.InitMenu(m_Instance);
		}

		menu.ShowMenu();

		return true;
	}

	static bool CloseMenu(E_MenuState state)
	{
		if (m_Instance == null)
			return false;

		if (m_Instance.IsInState(state) == false)
			return false;

		m_Instance.SetState(E_MenuState.Idle);

		return true;
	}

	static System.Net.IPEndPoint ParseIPandPort(string endPoint)
	{
		string[] ep = endPoint.Split(':');
		if (ep.Length != 2)
			throw new System.FormatException("Invalid endpoint format");

		System.Net.IPAddress ip;
		if (!System.Net.IPAddress.TryParse(ep[0], out ip))
		{
			throw new System.FormatException("Invalid ip-adress");
		}

		int port;
		if(!int.TryParse(ep[1], System.Globalization.NumberStyles.None, System.Globalization.NumberFormatInfo.CurrentInfo, out port))
		{
			throw new System.FormatException("Invalid port");
		}

		return new System.Net.IPEndPoint(ip, port);
	}
	
	static void GoToMainMenu(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		ShowMainMenu();
	}

	static GuiPopupMessageBox JoiningMessageBox = null;

	static void OnConnectToServerFailed(uLink.NetworkConnectionError reason)
	{
		if (JoiningMessageBox != null)
		{
			JoiningMessageBox.ForceClose();
			JoiningMessageBox = null;
		}

		ShowMessageBox("Network error", "Unable to join server (" + reason + ")", GoToMainMenu);
	}

	static bool CheckTriggerImmediateCommand()
	{
		string[] arguments = System.Environment.GetCommandLineArgs();
		//string[] arguments = new string[1] { "-join=127.0.0.1:8101" };

		if (arguments == null)
			return false;

		foreach (string str in arguments)
		{
			if (str.StartsWith("-join="))
			{
				string[] param = str.Split('=');
				System.Net.IPEndPoint endPoint = null;

				try
				{
					endPoint = ParseIPandPort(param[1]);
				}
				catch (System.Exception exc)
				{
					Debug.Log("The -connect commandline parameter problem: " + exc.ToString());
					return false;
				}

				JoiningMessageBox = ShowMessageBox("Joining to a server", "Joining to " + param[1]);
				Game.Instance.StartNewMultiplayerGame(endPoint, 0, OnConnectToServerFailed);
				return true;
			}
		}

		return false;
	}
}
