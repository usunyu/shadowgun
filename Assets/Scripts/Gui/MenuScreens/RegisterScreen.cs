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
using Regex = System.Text.RegularExpressions.Regex;

public class RegisterScreen : GuiScreen
{
	const int TEXT_ID_USERNAME_HEADER = 02040503; // 	* USERNAME (from {0} to {1} chars)
	const int TEXT_ID_PASSWORD_HEADER = 02040504; //  * PASSWORD (at least {0} chars)

	const int ERROR_CANT_CONTACT_SERVER = 02040517; //	Can't connect to SafeHaven server!
	const int ERROR_USERNAME_EXIST = 02040518; //	This account name already exist!
	const int ERROR_USERNAME_TOO_SHORT = 02040519; //	Account name is too short!
	const int ERROR_PASSWORD_NOT_MATCH = 02040520; //	Passwords do not match!
	const int ERROR_PASSWORD_TOO_SHORT = 02040522; //	Password is too short!
	const int ERROR_USERNAME_INVALID_FORMAT = 02040523; //  Invalid account name. Please use alphanumeric characters only.

	static Color DefaultColor = Color.white;
	static Color ErrorColor = Color.red;

	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_ScreenLayout = GetLayout("Login", "MMRegister_Layout");

		m_UserNameButton = PrepareButton(m_ScreenLayout, "UserName_Button", null, Delegate_UserName);
		m_PasswordButton = PrepareButton(m_ScreenLayout, "Pass_Button", null, Delegate_Password);
		m_ConfirmPasswordButton = PrepareButton(m_ScreenLayout, "PassConfirm_Button", null, Delegate_ConfirmPassword);
		PrepareButton(m_ScreenLayout, "Baxk_Button", null, Delegate_Back);

		m_LicenceToggle = PrepareSwitch(m_ScreenLayout, "Agree_Switch", Delegate_Licence);

		m_CreateAccountButton = PrepareButton(m_ScreenLayout, "Create_Button", null, Delegate_CreateAccount);
		m_CreateAccountButton.autoColorLabels = true;

		m_HintLabel = PrepareLabel(m_ScreenLayout, "Hint_Label");

		m_UserNameHeader = PrepareLabel(m_ScreenLayout, "UserName_Header");
		m_PasswordHeader = PrepareLabel(m_ScreenLayout, "Pass_Header");
		m_ConfirmPasswordHeader = PrepareLabel(m_ScreenLayout, "PassConfirm_Header");

		AddTextField(m_UserNameButton, Delegate_OnKeyboardClose, null, CloudUser.MAX_ACCOUNT_NAME_LENGTH);
		AddTextField(m_PasswordButton, Delegate_OnKeyboardClose);
		AddTextField(m_ConfirmPasswordButton, Delegate_OnKeyboardClose);

		DefaultColor = m_UserNameHeader.Widget.Color;
	}

	protected override void OnViewShow()
	{
		// reset all data...
		m_UserName = null;
		m_PasswordHash = null;
		m_ConfirmPasswordHash = null;
		m_PasswordLength = 0;
		m_ConfirmPasswordLength = 0;
		m_Email = null;
		m_IWantNews = true;
		m_IAgreeWithLicence = false;

		//language can change during the game, so this can't be in OnViewInit()
		{
// fixup user name header...		
			string userNameHeaderTemplate = TextDatabase.instance[TEXT_ID_USERNAME_HEADER];
			string text = string.Format(userNameHeaderTemplate,
										CloudUser.MIN_ACCOUNT_NAME_LENGTH,
										CloudUser.MAX_ACCOUNT_NAME_LENGTH);
			m_UserNameHeader.SetNewText(text);
		}
		{
// fixup password header...
			string passwordHeaderTemplate = TextDatabase.instance[TEXT_ID_PASSWORD_HEADER];
			string text = string.Format(passwordHeaderTemplate, CloudUser.MIN_PASSWORD_LENGTH);
			m_PasswordHeader.SetNewText(text);
		}

		m_UserNameButton.SetNewText(string.Empty);
		m_UserNameButton.TextFieldText = string.Empty;
		m_PasswordButton.SetNewText(string.Empty);
		m_PasswordButton.TextFieldText = string.Empty;
		m_ConfirmPasswordButton.SetNewText(string.Empty);
		m_ConfirmPasswordButton.TextFieldText = string.Empty;
		m_LicenceToggle.SetValue(false);
		m_HintLabel.SetNewText(null);

		m_UserNameHeader.Widget.Color = DefaultColor;
		m_PasswordHeader.Widget.Color = DefaultColor;
		m_ConfirmPasswordHeader.Widget.Color = DefaultColor;

		UpdateCreateAccountButton();

		base.OnViewShow();
	}

	protected override void OnViewHide()
	{
		//StopCoroutine("ShowHint_Corutine");

		base.OnViewHide();
	}

	protected override void OnViewUpdate()
	{
		if (m_CloudActionUserName != null && m_CloudActionUserName.isDone == true)
		{
			if (m_CloudActionUserName.isFailed == true)
			{
				//Debug.LogWarning("Can't contact cloud service");
				ShowHint(ERROR_CANT_CONTACT_SERVER);
				m_AccountDataError = true;
			}
			else if (m_CloudActionUserName.userExist == true)
			{
				//Debug.LogWarning("User with this name already exist");
				ShowHint(ERROR_USERNAME_EXIST);
				m_UserNameHeader.Widget.Color = ErrorColor;
				m_AccountDataError = true;
			}
			else
			{
				//Debug.LogWarning("User name is free");
				m_IsUserNameFree = true;
				m_FreeUserName = m_CloudActionUserName.userName;
				UpdateCreateAccountButton();
			}

			m_CloudActionUserName = null;
		}

		base.OnViewUpdate();
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
			//Owner.ShowScreen("Login", true);
		}
	}

	// #################################################################################################################
	// ###  Delegates  #################################################################################################
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

#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID)
				// TODO move pLace holder into text database
		ShowKeyboard(button, GuiScreen.E_KeyBoardMode.Default, Delegate_OnKeyboardClose, button.GetText(), "Enter username", CloudUser.MAX_ACCOUNT_NAME_LENGTH);
#endif
	}

	//------------------------------------------------------------------------------------------------------------------	
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

#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID)
				// TODO move pLace holder into text database
		ShowKeyboard(button, GuiScreen.E_KeyBoardMode.Password, Delegate_OnKeyboardClose, string.Empty, "Enter password");
#endif
	}

	//------------------------------------------------------------------------------------------------------------------	
	void Delegate_ConfirmPassword(GUIBase_Widget inInstigator)
	{
		//Debug.Log("Delegate_ConfirmPassword: ");
		MFDebugUtils.Assert(inInstigator == m_ConfirmPasswordButton.Widget);

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

#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID)
				// TODO move pLace holder into text database
		ShowKeyboard(button, GuiScreen.E_KeyBoardMode.Password, Delegate_OnKeyboardClose, string.Empty, "Enter password");
#endif
	}

	//------------------------------------------------------------------------------------------------------------------
	void Delegate_Back(GUIBase_Widget inInstigator)
	{
		Owner.Back();
	}

	//------------------------------------------------------------------------------------------------------------------	
	void Delegate_Licence(bool switchValue)
	{
		// TODO Show licence...
		m_IAgreeWithLicence = switchValue;
		UpdateCreateAccountButton();
	}

	//------------------------------------------------------------------------------------------------------------------	
	void Delegate_CreateAccount(GUIBase_Widget inInstigator)
	{
		//Debug.Log("Delegate_Login: ");
		MFDebugUtils.Assert(inInstigator == m_CreateAccountButton.Widget);

		StartCoroutine(CreateAccount_Coroutine());
	}

	IEnumerator CreateAccount_Coroutine()
	{
		// ask for email
		{
			GuiPopupRecoveryEmail popup = (GuiPopupRecoveryEmail)Owner.ShowPopup("RecoveryEmail",
																				 null,
																				 null,
																				 (inPopup, inResult) =>
																				 {
																					 if (inResult == E_PopupResultCode.Ok)
																					 {
																						 GuiPopupRecoveryEmail temp = (GuiPopupRecoveryEmail)inPopup;
																						 m_Email = temp.Email;
																						 m_IWantNews = temp.ReceiveNews;
																					 }
																					 else
																					 {
																						 m_Email = "";
																						 m_IWantNews = false;
																					 }
																					 Owner.Back();
																				 });
			popup.Email = m_Email;
			popup.ReceiveNews = m_IWantNews;

			while (popup.IsVisible == true)
			{
				yield return new WaitForEndOfFrame();
			}
		}

		if (accountDataValid == true)
		{
			UpdateCreateAccountButton();

			m_CreateUserPopup = GuiBaseUtils.ShowMessageBox("Create new account", "Contacting server...") as GuiPopupMessageBox;
			if (m_CreateUserPopup != null)
			{
				m_CreateUserPopup.SetButtonVisible(false);
			}

			bool success = false;
			yield return
							CloudUser.instance.CreateNewUser(m_UserName,
															 m_PasswordHash,
															 m_NickName,
															 m_Email,
															 m_IWantNews,
															 E_UserAcctKind.Normal,
															 (result) => { success = result; });

			if (success == false)
			{
				if (m_CreateUserPopup != null)
				{
					m_CreateUserPopup.SetText(TextDatabase.instance[ERROR_CANT_CONTACT_SERVER]);
					m_CreateUserPopup.SetButtonVisible(true);
				}

				ShowHint(ERROR_CANT_CONTACT_SERVER);
				Debug.LogWarning("Can't create new account."); // TODO textDatabase
			}
			else
			{
				m_CreateUserPopup.ForceClose();
				//Debug.LogWarning("New account successfuly created.");	// TODO textDatabase

				// get primary key
				string primaryKey;
				{
					UserGetPrimaryKey action = new UserGetPrimaryKey(m_UserName);
					GameCloudManager.AddAction(action);

					while (action.isDone == false)
						yield return new WaitForEndOfFrame();

					primaryKey = action.primaryKey;
				}

				CloudUser.instance.SetLoginData(primaryKey, m_NickName, m_UserName, m_PasswordHash, m_PasswordLength, false, false);
				//CloudUser.instance.AuthenticateLocalUser();
				Owner.ShowPopup("Authentication", TextDatabase.instance[02040016], "", AuthenticationResultHandler);
			}

			m_CreateUserPopup = null;

			UpdateCreateAccountButton();

			// Invoke("AuthenticationDeadLoopFixer", 20);
			// TODO disable Login screen.
		}
		else
		{
			Debug.LogError("Internal error. Create account button has to be disabled if Account Data are not valid");
		}
	}

	//------------------------------------------------------------------------------------------------------------------	
	void Delegate_OnKeyboardClose(GUIBase_Button inInput, string inKeyboardText, bool inInputCanceled)
	{
		if (inInput == m_UserNameButton)
		{
			if (string.IsNullOrEmpty(inKeyboardText) == true)
			{
				m_NickName = string.Empty;
				m_UserName = string.Empty;
				m_FreeUserName = null;

				m_UserNameButton.SetNewText("");

				VerifyAccountData();
				UpdateCreateAccountButton();
			}
			else
			{
				string text = GuiBaseUtils.FixNickname(inKeyboardText, m_UserName, false);
				if (string.IsNullOrEmpty(text) == false && m_UserName != text.ToLower())
				{
					// filter out swear words
					m_NickName = text.FilterSwearWords(true);
					// we allow lowercase username only
					m_UserName = text.ToLower();
					m_UserNameButton.SetNewText(GuiBaseUtils.FixNameForGui(m_UserName));
					m_FreeUserName = null;

					VerifyAccountData();
					UpdateCreateAccountButton();
				}
			}
		}
		else if (inInput == m_PasswordButton)
		{
			if (string.IsNullOrEmpty(inKeyboardText) == true)
			{
				m_PasswordHash = null;
				m_PasswordLength = 0;
			}
			else
			{
				m_PasswordHash = CloudServices.CalcPasswordHash(inKeyboardText);
				m_PasswordLength = inKeyboardText.Length;
			}

			m_PasswordButton.SetNewText(passwordGUIText);
			VerifyAccountData();
			UpdateCreateAccountButton();
		}
		else if (inInput == m_ConfirmPasswordButton)
		{
			if (string.IsNullOrEmpty(inKeyboardText) == true)
			{
				m_ConfirmPasswordHash = null;
				m_ConfirmPasswordLength = 0;
			}
			else
			{
				m_ConfirmPasswordHash = CloudServices.CalcPasswordHash(inKeyboardText);
				m_ConfirmPasswordLength = inKeyboardText.Length;
			}

			m_ConfirmPasswordButton.SetNewText(confirmGUIText);
			VerifyAccountData();
			UpdateCreateAccountButton();
		}
		else
		{
			Debug.LogError("Unknown input widget !!!");
		}
	}

	// #################################################################################################################
	// ###  Internal functions  ########################################################################################
	void UpdateCreateAccountButton()
	{
//		string message = "UpdateCreateAccountButton :: " + accountDataValid + "\n";
//	
//		message += "m_IsUserNameFree " + m_IsUserNameFree + "\n";
//		message += "!m_AccountDataError " + !m_AccountDataError + "\n";
//		message += "isUserNmaeValid " + isUserNmaeValid + "\n";		
//		message += "isPasswordValid " + isPasswordValid + "\n";		
//		message += "isEmailValid " + isEmailValid + "\n";		
//		message += "m_IAgreeWithLicence " + m_IAgreeWithLicence + "\n";				
//		
//		Debug.LogWarning(message);		

		m_CreateAccountButton.SetDisabled(accountDataValid == false);
	}

	void CheckUserName(string inName)
	{
		m_IsUserNameFree = false;
		m_CloudActionUserName = null;

		if (string.IsNullOrEmpty(inName))
			return;

		m_CloudActionUserName = CloudUser.instance.CheckIfUserNameExist(inName);
	}

	void VerifyAccountData()
	{
		m_AccountDataError = false;

		if (string.IsNullOrEmpty(m_PasswordHash) == false && string.IsNullOrEmpty(m_ConfirmPasswordHash) == false)
		{
			if (m_PasswordHash != m_ConfirmPasswordHash)
			{
				m_AccountDataError = true;
				m_HintLabel.SetNewText(ERROR_PASSWORD_NOT_MATCH);
				m_PasswordHeader.Widget.Color = ErrorColor;
				m_ConfirmPasswordHeader.Widget.Color = ErrorColor;
			}
			else if (m_PasswordLength < CloudUser.MIN_PASSWORD_LENGTH)
			{
				m_AccountDataError = true;
				m_HintLabel.SetNewText(ERROR_PASSWORD_TOO_SHORT);
				m_PasswordHeader.Widget.Color = ErrorColor;
				m_ConfirmPasswordHeader.Widget.Color = ErrorColor;
			}
			else
			{
				m_PasswordHeader.Widget.Color = DefaultColor;
				m_ConfirmPasswordHeader.Widget.Color = DefaultColor;
			}
		}

		// Check user name...
		if (string.IsNullOrEmpty(m_UserName) == false)
		{
			if (m_UserName.Length < CloudUser.MIN_ACCOUNT_NAME_LENGTH)
			{
				m_AccountDataError = true;
				m_HintLabel.SetNewText(ERROR_USERNAME_TOO_SHORT);
				m_UserNameHeader.Widget.Color = ErrorColor;
			}
			else
			{
				m_UserNameHasValidFormat = UserNameHasValidFormat(m_UserName);
				if (m_UserNameHasValidFormat == false)
				{
					m_AccountDataError = true;
					m_HintLabel.SetNewText(ERROR_USERNAME_INVALID_FORMAT);
					m_UserNameHeader.Widget.Color = ErrorColor;
				}
				else if (m_FreeUserName != m_UserName)
				{
					m_UserNameHeader.Widget.Color = DefaultColor;
					CheckUserName(m_UserName);
				}
			}
		}

		if (m_AccountDataError == false)
		{
			ClearHint();
		}
	}

	void ClearHint()
	{
		//StopCoroutine("ShowHint_Corutine");
		m_HintLabel.Clear();
	}

	void ShowHint(int inHintText)
	{
		//StopCoroutine("ShowHint_Corutine");
		//StartCoroutine("ShowHint_Corutine", inHintText);
		m_HintLabel.SetNewText(inHintText);
	}

	/*private IEnumerator ShowHint_Corutine(int inHintText)
	{
		m_HintLabel.SetNewText(inHintText);
		yield return new WaitForSeconds(5.0f);
		m_HintLabel.Clear();
	}*/

	public static bool UserNameHasValidFormat(string inUserName)
	{
		if (string.IsNullOrEmpty(inUserName))
			return false;

		if (inUserName.Length < CloudUser.MIN_ACCOUNT_NAME_LENGTH)
			return false;
		if (inUserName.Length > CloudUser.MAX_ACCOUNT_NAME_LENGTH)
			return false;

		return Regex.IsMatch(inUserName, @"^[a-z][a-z0-9]*$");
	}

	// #################################################################################################################	
	UsernameAlreadyExists m_CloudActionUserName;
	GuiPopupMessageBox m_CreateUserPopup = null;

	string m_NickName = null;

	string m_UserName = null;

	bool isUserNmaeValid
	{
		get
		{
			return (string.IsNullOrEmpty(m_UserName) == false && m_UserName.Length >= CloudUser.MIN_ACCOUNT_NAME_LENGTH &&
					m_UserNameHasValidFormat == true);
		}
	} // TODO Check if is name UNIQUE
	bool m_IsUserNameFree = false;
	string m_FreeUserName = null;
	bool m_UserNameHasValidFormat = false;

	string m_PasswordHash = null;
	string m_ConfirmPasswordHash = null;

	bool isPasswordValid
	{
		get { return (string.IsNullOrEmpty(m_PasswordHash) == false && m_PasswordHash == m_ConfirmPasswordHash); }
	}

	int m_PasswordLength = 0;
	int m_ConfirmPasswordLength = 0;

	string passwordGUIText
	{
		get { return (m_PasswordLength > 0 ? new string('*', m_PasswordLength) : string.Empty); }
	}

	string confirmGUIText
	{
		get { return (m_ConfirmPasswordLength > 0 ? new string('*', m_ConfirmPasswordLength) : string.Empty); }
	}

	string m_Email = null;
	bool isEmailValid = true; //{ get { return (string.IsNullOrEmpty(m_Email) == false); } }	// TODO propper email validation

	bool m_IWantNews = true;
	bool m_IAgreeWithLicence = false;

	bool m_AccountDataError = false;

	bool accountDataValid
	{
		get { return (m_IsUserNameFree && !m_AccountDataError && isUserNmaeValid && isPasswordValid && isEmailValid && m_IAgreeWithLicence); }
	}

	//private string 			m_HintText					= null;
	//private float				m_HintTextShowTime			= 0;
	//private const float 		m_DefaultHintTimeOut		= 3.0f;

	GUIBase_Button m_UserNameButton;
	GUIBase_Button m_PasswordButton;
	GUIBase_Button m_ConfirmPasswordButton;
	GUIBase_Switch m_LicenceToggle;
	GUIBase_Button m_CreateAccountButton;
	GUIBase_Label m_HintLabel;

	GUIBase_Label m_UserNameHeader;
	GUIBase_Label m_PasswordHeader;
	GUIBase_Label m_ConfirmPasswordHeader;
}
