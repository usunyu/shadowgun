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

public class GuiMenuLogin : GuiMenu
{
	static string m_InfoDisplayedForUser;
#if UNITY_IPHONE
	private static bool m_MigrationOffered = false;
#endif

	static bool IsTegra3()
	{
		//Debug.Log("device: " + SystemInfo.graphicsDeviceVendor + ", " + SystemInfo.graphicsDeviceName);
		return (SystemInfo.graphicsDeviceVendor.ToLower().IndexOf("nvidia") >= 0 && SystemInfo.graphicsDeviceName.IndexOf("Tegra 3") != -1);
	}

	static void OnTegra3(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		Application.Quit();
	}

	protected override void OnMenuInit()
	{
		// screens
#if FALSE
		_CreateScreen<EditorLoginScreen>("Login");
#else
		_CreateScreen<LoginScreen>("Login");
#endif
		_CreateScreen<RegisterScreen>("CreateNewUser");

		// popups
		_CreateScreen<AuthenticationDialog>("Authentication");
		_CreateScreen<ForgotPasswordDialog>("ForgotPassword");
		RegisterScreen<GuiPopupRecoveryEmail>();
	}

	protected override void OnMenuUpdate()
	{
	}

	protected override void OnMenuShowMenu()
	{
#if false //UNITY_ANDROID && !UNITY_EDITOR
		if(!IsTegra3() )
		{
			ShowPopup("MessageBox", "Not a Tegra 3 device", "Only TEGRA 3 devices currently supported", OnTegra3);
			return;
		}
#endif

#if UNITY_IPHONE
		string storedPrimaryKey = PlayerPrefs.GetString(CloudUser.PRIMARY_KEY_KEY,   string.Empty);
		string storedUsername   = PlayerPrefs.GetString(CloudUser.USERNAME_KEY,      string.Empty);
		string storedPassword   = PlayerPrefs.GetString(CloudUser.PASSWORD_HASH_KEY, string.Empty);
		if (CheckGuestAccount(storedPrimaryKey, storedUsername) == false)
		{
			StartCoroutine(CheckGuestAccount_Coroutine(storedPrimaryKey, storedPassword));
			return;
		}
#endif
		StartLogin();
	}

	void StartLogin(bool useAutoAuthenticate = true)
	{
		if (useAutoAuthenticate && CloudUser.instance.CanAutoAuthenticate() == true)
		{
			ShowPopup("Authentication", TextDatabase.instance[02040016], "", OnAuthenticationResult);
		}
		else
		{
			ShowScreen("FirstLogin", true);
		}
	}

	protected override void OnMenuHideMenu()
	{
	}

	protected override void OnMenuRefreshMenu(bool anyPopupVisible)
	{
	}

	// ISCREENOWNER INTERFACE

	public override void Exit()
	{
		GuiFrontendMain.StartMainMenuOrCommand();
	}

	// GUIMENU INTERFACE

	protected override bool ProcessMenuInput(ref IInputEvent evt)
	{
		if (evt.Kind == E_EventKind.Key)
		{
			KeyEvent key = (KeyEvent)evt;
			if (key.Code == KeyCode.Escape)
			{
				if (key.State == E_KeyState.Released)
				{
					ShowConfirmDialog("Exit", TextDatabase.instance[00106013], ExitHandler);
				}
				return true;
			}
		}

		return base.ProcessMenuInput(ref evt);
	}

	// HANDLERS

	void OnAuthenticationResult(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		if (inResult == E_PopupResultCode.Success)
		{
			Exit();
		}
		else
		{
			ShowScreen("FirstLogin", true);
		}
	}

	void ExitHandler(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		if (inResult == E_PopupResultCode.Ok)
		{
			QuitApplication();
		}
	}

#if UNITY_IPHONE
	bool CheckGuestAccount(string primaryKey, string username)
	{
		if (m_MigrationOffered == true)
			return true; // don't bother anymore

		if (string.IsNullOrEmpty(username) == true)
			return true;  // not a guest account
		if (username.StartsWith("guest") == false)
			return true;  // not a guest account

		if (string.IsNullOrEmpty(primaryKey) == false && primaryKey.Contains(username) == false)
			return true;  // account migrated already

		return false;     // ok, let him know than
	}

	IEnumerator CheckGuestAccount_Coroutine(string primaryKey, string password)
	{
		GetPublicUserData getUserAccountType = 
			new GetPublicUserData(primaryKey, CloudServices.PROP_ID_ACCT_KIND);
		GameCloudManager.AddAction(getUserAccountType);
		
		while(!getUserAccountType.isDone)
			yield return new WaitForEndOfFrame();
		
		E_PopupResultCode migrateResult = E_PopupResultCode.Cancel;
		
		if (!getUserAccountType.isFailed && !string.IsNullOrEmpty(getUserAccountType.result) &&
			getUserAccountType.result == E_UserAcctKind.Guest.ToString())
		{
			m_MigrationOffered = true;
			bool migrateFinished = false;
			GuiPopupMigrateGuest migratePopup = (GuiPopupMigrateGuest)ShowPopup("MigrateGuest", null, null, (popup, result) =>
			{
				migrateResult = result;
				migrateFinished = true;
			});
			migratePopup.Usage      = GuiPopupMigrateGuest.E_Usage.LoginMenu;
			migratePopup.PrimaryKey = primaryKey;
			migratePopup.Password   = password;
			while (!migrateFinished)
				yield return new WaitForEndOfFrame();
			
			migratePopup.HideView(this);
		}
		
		StartLogin(migrateResult == E_PopupResultCode.Cancel);
	}
#endif
}
