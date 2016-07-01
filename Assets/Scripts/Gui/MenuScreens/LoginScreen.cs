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

// =====================================================================================================================
// =====================================================================================================================
public class LoginScreen : GuiScreen
{
	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_ScreenLayout = GetLayout("Login", "MMLogin_Layout");

		m_CreateNewUserButton = PrepareButton(m_ScreenLayout, "Create_Button", null, Delegate_CreateUser);
		m_LoginButton = PrepareButton(m_ScreenLayout, "Login_Button", null, Delegate_Login);
		m_UserNameButton = PrepareButton(m_ScreenLayout, "Username_Button", null, Delegate_UserName);
		m_PasswordButton = PrepareButton(m_ScreenLayout, "Pass_Button", null, Delegate_Password);
		m_ForgotButton = PrepareButton(m_ScreenLayout, "Forgot_Button", null, Delegate_ForgotPassword);
		m_RememberMeButton = PrepareSwitch(m_ScreenLayout, "Remember_Switch", Delegate_RememberMe);
		m_AutoLoginButton = PrepareSwitch(m_ScreenLayout, "AutoLogin_Switch", Delegate_AutoLogin);

		AddTextField(m_UserNameButton, Delegate_OnKeyboardClose, null, 60 /*CloudUser.MAX_ACCOUNT_NAME_LENGTH*/);
						//60 should be enough for everyone (this allows guests to type in their long name)
		AddTextField(m_PasswordButton, Delegate_OnKeyboardClose);

#if !UNITY_STANDALONE //there is no back on PC/MAC
		RegisterButtonDelegate("Back_Button", () => { Owner.Back(); }, null);
#endif

		m_LoginButton.autoColorLabels = true;
	}

	protected override void OnViewShow()
	{
		m_PasswordLength = -1;

		CloudUser cloudUser = CloudUser.instance;
		if (cloudUser.GetLoginData(ref m_LoadedNickName,
								   ref m_LoadedUserName,
								   ref m_LoadedPassword,
								   ref m_LoadedPasswordLength,
								   ref m_LoadedRememberMe,
								   ref m_LoadedAutoLogin) == false)
		{
			m_LoadedUserName = m_UserName = s_DefaultUserNameText;
			m_LoadedPassword = m_PasswordHash = s_DefaultPasswordText;
			m_LoadedPasswordLength = m_PasswordLength = 0;
			m_LoadedRememberMe = m_RememberMe = true;
			m_LoadedAutoLogin = m_AutoLogin = false;
		}
		else
		{
			m_UserName = m_LoadedUserName.ToLower();
			m_PasswordHash = m_LoadedPassword;
			m_RememberMe = m_LoadedRememberMe;
			m_PasswordLength = m_LoadedPasswordLength;
			m_AutoLogin = m_LoadedAutoLogin;
		}

		string userName = GuiBaseUtils.FixNameForGui(m_UserName);
		m_UserNameButton.SetNewText(userName);
		if (m_UserName != s_DefaultUserNameText)
			m_UserNameButton.TextFieldText = userName;

		m_PasswordButton.TextFieldText = "";
		m_PasswordButton.SetNewText(passwordGUIText);
		;
		m_RememberMeButton.SetValue(m_RememberMe);
		m_AutoLoginButton.SetValue(m_AutoLogin);

		UpdateLoginButton();

#if UNITY_STANDALONE //there is no back on PC/MAC
		GetWidget("Back_Button").Show(false, true);
#endif
		base.OnViewShow();
	}

	protected override void OnViewHide()
	{
		base.OnViewHide();
	}

	protected override void OnViewUpdate()
	{
		base.OnViewUpdate();
	}

	protected override void OnViewEnable()
	{
		m_ScreenLayout.InputEnabled = true;
		base.OnViewEnable();
	}

	protected override void OnViewDisable()
	{
		m_ScreenLayout.InputEnabled = false;
		base.OnViewDisable();
	}

#if UNITY_STANDALONE //there is no back on PC/MAC
	protected override bool OnViewProcessInput(ref IInputEvent evt)
	{
		if (evt.Kind == E_EventKind.Key)
		{
			KeyEvent key = (KeyEvent)evt;
			if (key.Code == KeyCode.Escape)
				return true;
		}
		return base.OnViewProcessInput(ref evt);
	}
#endif

	// #################################################################################################################
	// ###  Delegates  #################################################################################################
	void Delegate_CreateUser(GUIBase_Widget inInstigator)
	{
		//Debug.Log("Delegate_CreateUser: ");	
		MFDebugUtils.Assert(inInstigator == m_CreateNewUserButton.Widget);
		Owner.ShowScreen("CreateNewUser");
	}

	void Delegate_Login(GUIBase_Widget inInstigator)
	{
		//Debug.Log("Delegate_Login: ");	
		MFDebugUtils.Assert(inInstigator == m_LoginButton.Widget);

		if (logindataValid == true)
		{
			StartCoroutine(Login_Coroutine());
		}
		else
		{
			Debug.LogError("Internal error. Login button has to be disabled if Login Data are not valid");

			//PPIManager.Instance.DummyAuthentication_TESTING = true;
			//PPIManager.Instance.AuthenticateLocalUser();
		}
	}

	void Delegate_UserName(GUIBase_Widget inInstigator)
	{
		//Debug.Log("Delegate_UserName: ");
		MFDebugUtils.Assert(inInstigator == m_UserNameButton.Widget);

		if (isInputActive == true)
		{
			Debug.LogWarning("Internal error. Interaction has to be disabled if Keyboard is active");
			return;
		}

		GUIBase_Button button = inInstigator.GetComponent<GUIBase_Button>();
		if (button == null)
		{
			Debug.LogError("Internal error !!! ");
			return;
		}

		if (m_UserName == s_DefaultUserNameText)
		{
			m_UserNameButton.TextFieldText = string.Empty;
			Delegate_OnKeyboardClose(m_UserNameButton, string.Empty, false);
		}

#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID)
				// TODO move pLace holder into text database		
		string default_text = (m_UserName != s_DefaultUserNameText) ? m_UserName : string.Empty;
		ShowKeyboard(button, GuiScreen.E_KeyBoardMode.Default, Delegate_OnKeyboardClose, default_text, "Enter username");
#endif
	}

	void Delegate_Password(GUIBase_Widget inInstigator)
	{
		//Debug.Log("Delegate_Password: ");
		MFDebugUtils.Assert(inInstigator == m_PasswordButton.Widget);

		if (isInputActive == true)
		{
			Debug.LogWarning("Internal error. Interaction has to be disabled if Keyboard is active");
			return;
		}

		GUIBase_Button button = inInstigator.GetComponent<GUIBase_Button>();
		if (button == null)
		{
			Debug.LogError("Internal error !!! ");
			return;
		}

		if (m_PasswordHash == s_DefaultPasswordText || string.IsNullOrEmpty(m_PasswordButton.TextFieldText))
		{
			m_PasswordButton.TextFieldText = string.Empty;
			Delegate_OnKeyboardClose(m_PasswordButton, string.Empty, false);
		}

#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID)
				// TODO move pLace holder into text database		
		ShowKeyboard(button, GuiScreen.E_KeyBoardMode.Password, Delegate_OnKeyboardClose, string.Empty, "Enter password");
#endif
	}

	void Delegate_RememberMe(bool switchValue)
	{
		m_RememberMe = switchValue;
	}

	void Delegate_AutoLogin(bool switchValue)
	{
		m_AutoLogin = switchValue;
	}

	void Delegate_ForgotPassword(GUIBase_Widget inInstigator)
	{
		//Debug.Log("Delegate_ForgotPassword: ");
		MFDebugUtils.Assert(inInstigator == m_ForgotButton.Widget);

		if (isInputActive == true)
		{
			Debug.LogWarning("Internal error. Interaction has to be disabled if Keyboard is active");
			return;
		}

		GUIBase_Button button = inInstigator.GetComponent<GUIBase_Button>();
		if (button == null)
		{
			Debug.LogError("Internal error !!! ");
			return;
		}

		Owner.ShowPopup("ForgotPassword", "", "", ForgotPasswordHandler);
	}

	void ForgotPasswordHandler(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		if (inResult != E_PopupResultCode.Cancel)
		{
			Owner.ShowPopup("MessageBox", TextDatabase.instance[0103044], TextDatabase.instance[inResult == E_PopupResultCode.Ok ? 0103047 : 0103042]);
		}
	}

	void Delegate_OnKeyboardClose(GUIBase_Button inInput, string inKeyboardText, bool inInputCanceled)
	{
		if (inInput == m_UserNameButton)
		{
			if (m_UserName != inKeyboardText)
			{
				string tmpName = ""; //m_LoadedUserName;
				if (string.IsNullOrEmpty(inKeyboardText) == false)
				{
					tmpName = inKeyboardText.ToLower();
				}

				m_UserName = tmpName;
				m_UserNameButton.SetNewText(m_UserName.MFNormalize());
				UpdateLoginButton();
			}
		}
		else if (inInput == m_PasswordButton)
		{
			if (string.IsNullOrEmpty(inKeyboardText) == true)
			{
				m_PasswordHash = m_LoadedPassword;
				m_PasswordLength = 0; //m_LoadedPasswordLength;
			}
			else
			{
				m_PasswordHash = CloudServices.CalcPasswordHash(inKeyboardText);
				m_PasswordLength = inKeyboardText.Length;
			}

			m_PasswordButton.SetNewText(new string('*', m_PasswordLength));
			UpdateLoginButton();
		}
		else
		{
			Debug.LogError("Unknown input widget !!!");
		}
	}

#if MADFINGER_KEYBOARD_MOUSE
	protected override void OnGUI() 	//login with enter, OnViewProcessInput couldn't be used because it was blocked by text fields
	{
		if (!IsVisible || !IsEnabled)
			return;
		
		Event e = Event.current;

		if (e.keyCode == KeyCode.Return && e.type == EventType.keyUp && logindataValid) 
			Delegate_Login(m_LoginButton.Widget);
		else
			base.OnGUI();
	}
#endif

	// #################################################################################################################		
	void AuthenticationDeadLoopFixer()
	{
		// TODO :: 
	}

	void UpdateLoginButton()
	{
		m_LoginButton.SetDisabled(logindataValid == false);
	}

	void AuthenticationResultHandler(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		if (inResult == E_PopupResultCode.Success)
		{
			// close login menu
			Owner.Exit();
		}
		else
		{
			//Owner.Back();
		}
	}

	IEnumerator Login_Coroutine()
	{
		GuiPopupMessageBox msgBox =
						Owner.ShowPopup("MessageBox", TextDatabase.instance[02040016], TextDatabase.instance[02040017]) as GuiPopupMessageBox;

		// get primary key
		string primaryKey;
		{
			UserGetPrimaryKey action = new UserGetPrimaryKey(m_UserName);
			GameCloudManager.AddAction(action);

			while (action.isDone == false)
				yield return new WaitForEndOfFrame();

			primaryKey = action.primaryKey;
		}

#if UNITY_IPHONE || TEST_IOS_VENDOR_ID
		if (string.IsNullOrEmpty(m_UserName) == false && m_UserName.StartsWith("guest") == true)
		{
			string   userid = SysUtils.GetUniqueDeviceID();
			string vendorID = null;

			while (string.IsNullOrEmpty(vendorID) == true)
			{
				vendorID = MFNativeUtils.IOS.VendorId;
				
				yield return new WaitForEndOfFrame();
			}

			string id     = string.IsNullOrEmpty(userid) ? vendorID : userid;
			string idtype = string.IsNullOrEmpty(userid) ? CloudServices.LINK_ID_TYPE_IOSVENDOR : CloudServices.LINK_ID_TYPE_DEVICE;

			//Debug.Log(">>>> ID="+id+", IDType="+idtype);

			GetPrimaryKeyLinkedWithID action = new GetPrimaryKeyLinkedWithID(E_UserAcctKind.Guest, id, idtype);
			GameCloudManager.AddAction(action);
			
			while (action.isDone == false)
				yield return new WaitForEndOfFrame();

			msgBox.ForceClose();

			//Debug.Log(">>>> action.isSucceeded="+action.isSucceeded+", action.primaryKey="+action.primaryKey+", primaryKey="+primaryKey);

			bool   force = action.isFailed == true || action.isPrimaryKeyForSHDZ == false || action.primaryKey != primaryKey;
			bool migrate = false;
			GuiPopupMigrateGuest migratePopup = (GuiPopupMigrateGuest)Owner.ShowPopup("MigrateGuest", null, null, (inPopup, inResult) => {
				migrate = inResult == E_PopupResultCode.Ok;
			});
			migratePopup.Usage      = force ? GuiPopupMigrateGuest.E_Usage.QuickPlay : GuiPopupMigrateGuest.E_Usage.LoginScreen;
			migratePopup.PrimaryKey = primaryKey;
			migratePopup.Password   = m_PasswordHash;
			
			while (migratePopup.IsVisible == true)
				yield return new WaitForEndOfFrame();
			
			if (migrate == true)
			{
				yield break;
			}
		}
#endif

		if (msgBox.IsVisible == true)
		{
			msgBox.ForceClose();
		}

		CloudUser cloudUser = CloudUser.instance;
		cloudUser.SetLoginData(primaryKey, m_UserName, m_UserName, m_PasswordHash, m_PasswordLength, m_RememberMe, m_AutoLogin);

		CloudUser.instance.AuthenticateLocalUser();
		Owner.ShowPopup("Authentication", TextDatabase.instance[02040016], "", AuthenticationResultHandler);

		// Invoke("AuthenticationDeadLoopFixer", 20);
		// TODO disable Login screen.
	}

	// #################################################################################################################	
	string m_LoadedNickName = null;

	static string s_DefaultUserNameText = "USER NAME"; // TODO :: Integrate with TextDatabase
	string m_LoadedUserName = s_DefaultUserNameText;
	string m_UserName = s_DefaultUserNameText;

	bool userNameChanged
	{
		get { return m_LoadedUserName != m_UserName; }
	}

	bool inUserNmaeValid
	{
		get { return (string.IsNullOrEmpty(m_UserName) == false && m_UserName != s_DefaultUserNameText); }
	}

	static string s_DefaultPasswordText = "PASSWORD"; // TODO :: Integrate with TextDatabase
	string m_LoadedPassword = s_DefaultPasswordText;
	string m_PasswordHash = s_DefaultPasswordText;

	bool inPasswordValid
	{
		get { return (string.IsNullOrEmpty(m_PasswordHash) == false && m_PasswordHash != s_DefaultPasswordText && m_PasswordLength > 0); }
	}

	bool passwordChanged
	{
		get { return m_LoadedPassword != m_PasswordHash; }
	}

	string passwordGUIText
	{
		get { return (inPasswordValid ? new string('*', m_PasswordLength) : s_DefaultPasswordText); }
	}

	int m_LoadedPasswordLength = -1;
	int m_PasswordLength = -1;

	bool m_LoadedRememberMe = true;
	bool m_RememberMe = true;

	bool rememberMeChanged
	{
		get { return m_LoadedRememberMe != m_RememberMe; }
	}

	bool m_LoadedAutoLogin = true;
	bool m_AutoLogin = true;

	bool autoLoginChanged
	{
		get { return m_LoadedAutoLogin != m_AutoLogin; }
	}

	bool logindataChanged
	{
		get { return (userNameChanged || passwordChanged || rememberMeChanged || autoLoginChanged); }
	}

	bool logindataValid
	{
		get { return (inUserNmaeValid && inPasswordValid); }
	}

	GUIBase_Button m_CreateNewUserButton;
	GUIBase_Button m_LoginButton;
	GUIBase_Button m_UserNameButton;
	GUIBase_Button m_PasswordButton;
	GUIBase_Button m_ForgotButton;

	GUIBase_Switch m_RememberMeButton;
	GUIBase_Switch m_AutoLoginButton;
}
