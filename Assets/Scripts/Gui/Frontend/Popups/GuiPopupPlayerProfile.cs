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

public class GuiPopupPlayerProfile : GuiPopup, IGuiOverlayScreen
{
	readonly static string USERNAME_LABEL = "Username";
	readonly static string NICKNAME_BUTTON = "Nickname_Button";
	readonly static string PASSWORD1_BUTTON = "Password1_Button";
	readonly static string PASSWORD2_BUTTON = "Password2_Button";
	readonly static string NEWS_SWITCH = "News_Switch";
	readonly static string CLOSE_BUTTON = "Close_Button";
	readonly static string MIGRATE_BUTTON = "Migrate_Button";
	readonly static string CONFIRM_BUTTON = "Confirm_Button";
	readonly static string REGION_DECOR = "Region_Decoration";
	readonly static string REGION_ROLLER = "Region_Roller";
	readonly static string HINT_LABEL = "Hint_Label";
	readonly static string ITEM_PREFIX = "Item_";

	// PRIVATE MEMBERS

	string m_Nickname = null;
	int m_PasswordLength = -1;
	string m_Password1Hash = null;
	string m_Password2Hash = null;
	bool m_ReceiveNews = true;
	NetUtils.GeoRegion m_Region = NetUtils.GeoRegion.Europe;

	GUIBase_Button m_NicknameBtn = null;
	GUIBase_Button m_Password1Btn = null;
	GUIBase_Button m_Password2Btn = null;

	// IGUIOVERLAYSCREEN INTERFACE

	string IGuiOverlayScreen.OverlayButtonTooltip
	{
		get { return null; }
	}

	bool IGuiOverlayScreen.HideOverlayButton
	{
		get { return Ftue.IsActionIdle<FtueAction.Profile>(); }
	}

	bool IGuiOverlayScreen.HighlightOverlayButton
	{
		get { return Ftue.IsActionActive<FtueAction.Profile>(); }
	}

	// GUIPOPUP INTERFACE

	public override void SetCaption(string inCaption)
	{
	}

	public override void SetText(string inText)
	{
	}

	// GUIVIEW INTERFACE

	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_NicknameBtn = GuiBaseUtils.GetControl<GUIBase_Button>(Layout, NICKNAME_BUTTON);
		m_Password1Btn = GuiBaseUtils.GetControl<GUIBase_Button>(Layout, PASSWORD1_BUTTON);
		m_Password2Btn = GuiBaseUtils.GetControl<GUIBase_Button>(Layout, PASSWORD2_BUTTON);

		AddTextField(m_NicknameBtn, NickNameKeyboardClose, null, CloudUser.MAX_ACCOUNT_NAME_LENGTH);
		AddTextField(m_Password1Btn, Password1KeyboardClose);
		AddTextField(m_Password2Btn, Password2KeyboardClose);
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		string username = "";
		bool rememberMe = true;
		bool autoLogin = true;
		CloudUser.instance.GetLoginData(ref m_Nickname, ref username, ref m_Password1Hash, ref m_PasswordLength, ref rememberMe, ref autoLogin);

		m_ReceiveNews = CloudUser.instance.receiveNews;
		m_Region = CloudUser.instance.region;

		PrepareLabel(USERNAME_LABEL).SetNewText(CloudUser.instance.userName_TODO.MFNormalize()); // display unmodified username here!

		PrepareButton(CLOSE_BUTTON, null, OnClosePressed);
		PrepareButton(CONFIRM_BUTTON, null, OnConfirmPressed);
		m_NicknameBtn = PrepareButton(NICKNAME_BUTTON, null, OnNicknamePressed);
		m_Password1Btn = PrepareButton(PASSWORD1_BUTTON, null, OnPassword1Pressed);
		m_Password2Btn = PrepareButton(PASSWORD2_BUTTON, null, OnPassword2Pressed);

		if (CloudUser.instance.userAccountKind == E_UserAcctKind.Guest)
		{
			GUIBase_Button button = PrepareButton(MIGRATE_BUTTON, null, OnMigrateGuestPressed);
			button.Widget.m_VisibleOnLayoutShow = true;
			if (button.Widget.Visible == false)
			{
				button.Widget.ShowImmediate(true, true);
			}
		}
		else
		{
			GUIBase_Button button = PrepareButton(Layout, MIGRATE_BUTTON, null, null);
			button.Widget.m_VisibleOnLayoutShow = false;
			if (button.Widget.Visible == true)
			{
				button.Widget.ShowImmediate(false, true);
			}
		}

		GuiBaseUtils.RegisterSwitchDelegate(Layout, NEWS_SWITCH, OnNewsChanged);

		RegisterRollerDelegate(REGION_ROLLER, OnRegionChanged);

		m_NicknameBtn.TextFieldText = m_Nickname;
		m_Password1Btn.SetNewText("");
		m_Password1Btn.TextFieldText = "";

		m_Password2Btn.SetNewText("");
		m_Password2Btn.TextFieldText = "";

		RefreshPage();
	}

	protected override void OnViewHide()
	{
		RegisterRollerDelegate(REGION_ROLLER, null);
		GuiBaseUtils.RegisterSwitchDelegate(Layout, NEWS_SWITCH, null);
		PrepareButton(PASSWORD2_BUTTON, null, null);
		PrepareButton(PASSWORD1_BUTTON, null, null);
		PrepareButton(NICKNAME_BUTTON, null, null);
		PrepareButton(CONFIRM_BUTTON, null, null);
		PrepareButton(CLOSE_BUTTON, null, null);
		PrepareButton(MIGRATE_BUTTON, null, null);

		base.OnViewHide();
	}

	// HANDLERS

	void OnClosePressed(GUIBase_Widget widget)
	{
		Owner.Back();
	}

	void OnConfirmPressed(GUIBase_Widget widget)
	{
		if (CloudUser.instance.userAccountKind == E_UserAcctKind.Normal)
		{
			if (IsKeyboardControlEnabled)
			{
				StartCoroutine(CheckUserPasswordAndConfirm());
			}
			else
			{
				ShowKeyboard(widget.GetComponent<GUIBase_Button>(),
							 GuiScreen.E_KeyBoardMode.Password,
							 (input, text, cancelled) =>
							 {
								 if (cancelled == false)
								 {
									 if (CloudServices.CalcPasswordHash(text) == CloudUser.instance.passwordHash)
									 {
										 // we can update profile now
										 StartCoroutine(UpdateProfile_Coroutine());
									 }
									 else
									 {
										 // inform user that he enters invalid password
										 Owner.ShowPopup("MessageBox", TextDatabase.instance[0107000], TextDatabase.instance[0107023]);
									 }
								 }
							 },
							 string.Empty,
							 TextDatabase.instance[0107016]);
			}
		}
		else
		{
			// we don't ask for password if guest or facebook user logged-in
			StartCoroutine(UpdateProfile_Coroutine());
		}
	}

	void OnMigrateGuestPressed(GUIBase_Widget widget)
	{
		GuiPopupMigrateGuest migratePopup = (GuiPopupMigrateGuest)Owner.ShowPopup("MigrateGuest", null, null);
		migratePopup.Usage = GuiPopupMigrateGuest.E_Usage.MainMenu;
		migratePopup.PrimaryKey = CloudUser.instance.primaryKey;
		migratePopup.Password = CloudUser.instance.passwordHash;
	}

	IEnumerator CheckUserPasswordAndConfirm()
	{
		bool paswordsMatch = false;
		bool popupCanceled = true;

		ForgotPasswordDialog popup = (ForgotPasswordDialog)Owner.ShowPopup("ForgotPassword", null, null, null);
		popup.IsForPassword = true;
		popup.SetHandler((inPopup, inResult) =>
						 {
							 if (inResult == E_PopupResultCode.Ok)
							 {
								 popupCanceled = false;
								 if (CloudServices.CalcPasswordHash(popup.TextFieldData) == CloudUser.instance.passwordHash)
									 paswordsMatch = true;
							 }
							 Owner.Back();
						 });

		while (popup.IsVisible == true)
			yield return new WaitForEndOfFrame();

		popup.IsForPassword = false;

		popup.ForceClose();

		if (popupCanceled)
			yield break;

		if (paswordsMatch)
			StartCoroutine(UpdateProfile_Coroutine());
		else
			Owner.ShowPopup("MessageBox", TextDatabase.instance[0107000], TextDatabase.instance[0107023]);
	}

	void NickNameKeyboardClose(GUIBase_Button input, string text, bool cancelled)
	{
		if (cancelled == false)
		{
			m_Nickname = text;
			if (!string.IsNullOrEmpty(text))
				m_Nickname = text.RemoveDiacritics().AsciiOnly(false).FilterSwearWords(true);
			RefreshPage();
		}
	}

	void OnNicknamePressed(GUIBase_Widget widget)
	{
#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID)
		ShowKeyboard(widget.GetComponent<GUIBase_Button>(), GuiScreen.E_KeyBoardMode.Default, NickNameKeyboardClose, m_Nickname, TextDatabase.instance[0107017], CloudUser.MAX_ACCOUNT_NAME_LENGTH);
#endif
	}

	void Password1KeyboardClose(GUIBase_Button input, string text, bool cancelled)
	{
		if (cancelled == false)
		{
			m_Password1Hash = CloudServices.CalcPasswordHash(text);
			m_PasswordLength = text.Length;
			m_Password1Btn.SetNewText(new string('*', m_PasswordLength));
			RefreshPage();
		}
	}

	void OnPassword1Pressed(GUIBase_Widget widget)
	{
#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID)
		ShowKeyboard(widget.GetComponent<GUIBase_Button>(), GuiScreen.E_KeyBoardMode.Password, Password1KeyboardClose, string.Empty, TextDatabase.instance[0107018]);
#endif
	}

	void Password2KeyboardClose(GUIBase_Button input, string text, bool cancelled)
	{
		if (cancelled == false)
		{
			m_Password2Hash = CloudServices.CalcPasswordHash(text);
			m_Password2Btn.SetNewText(new string('*', text.Length));
			RefreshPage();
		}
	}

	void OnPassword2Pressed(GUIBase_Widget widget)
	{
#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID)
		ShowKeyboard(widget.GetComponent<GUIBase_Button>(), GuiScreen.E_KeyBoardMode.Password, Password2KeyboardClose, string.Empty, TextDatabase.instance[0107019]);
#endif
	}

	void OnNewsChanged(bool state)
	{
		m_ReceiveNews = state;
		RefreshPage();
	}

	void OnRegionChanged(int value)
	{
		m_Region = (NetUtils.GeoRegion)value;
		RefreshPage();
	}

	// PRIVATE METHODS

	void RefreshPage()
	{
		bool hasCustomAccount = CloudUser.instance.userAccountKind == E_UserAcctKind.Normal;
		bool canChangePassword = hasCustomAccount;

		// nickname
		bool nicknameChanged = m_Nickname != CloudUser.instance.nickName ? true : false;
		bool nicknameIsValid = string.IsNullOrEmpty(m_Nickname) == false && m_Nickname.Length >= CloudUser.MIN_ACCOUNT_NAME_LENGTH ? true : false;
		bool canUpdateNickname = nicknameChanged == true ? nicknameIsValid : true;

		// password
		bool passwordChanged = m_Password1Hash != CloudUser.instance.passwordHash ? true : false;
		bool passwordIsValid = m_PasswordLength >= CloudUser.MIN_PASSWORD_LENGTH ? true : false;
		bool passwordsMatch = m_Password1Hash == m_Password2Hash ? true : false;
		bool canUpdatePassword = passwordChanged == true ? passwordIsValid && passwordsMatch : true;

		// receive news
		bool receiveNewsChanged = m_ReceiveNews != CloudUser.instance.receiveNews ? true : false;

		// region
		bool regionChanged = m_Region != CloudUser.instance.region ? true : false;

		// all together
		bool anyChanged = nicknameChanged || passwordChanged || receiveNewsChanged || regionChanged;
		bool canApplyChanges = canUpdateNickname && canUpdatePassword;

		// error hint
		GUIBase_Label hint = GuiBaseUtils.GetControl<GUIBase_Label>(Layout, HINT_LABEL);
		if (nicknameChanged == true && nicknameIsValid == false)
		{
			hint.SetNewText(string.Format(TextDatabase.instance[0107013], CloudUser.MIN_ACCOUNT_NAME_LENGTH));
			hint.Widget.Show(true, true);
		}
		else if (hasCustomAccount == true && anyChanged == true && canApplyChanges == true)
		{
			hint.SetNewText(TextDatabase.instance[0107026]);
			hint.Widget.Show(true, true);
		}
		else if (canChangePassword == false)
		{
			hint.SetNewText(string.Format(TextDatabase.instance[0107025], CloudUser.MIN_PASSWORD_LENGTH));
			hint.Widget.Show(true, true);
		}
		else if (passwordChanged == true && passwordIsValid == false)
		{
			hint.SetNewText(string.Format(TextDatabase.instance[0107014], CloudUser.MIN_PASSWORD_LENGTH));
			hint.Widget.Show(true, true);
		}
		else if (passwordChanged == true && passwordsMatch == false)
		{
			hint.SetNewText(TextDatabase.instance[0107015]);
			hint.Widget.Show(true, true);
		}
		else
		{
			hint.Widget.Show(false, true);
		}

		// controls
		GuiBaseUtils.GetControl<GUIBase_Button>(Layout, NICKNAME_BUTTON).SetNewText(m_Nickname);
		GuiBaseUtils.GetControl<GUIBase_Button>(Layout, PASSWORD1_BUTTON).SetDisabled(canChangePassword ? false : true);
		GuiBaseUtils.GetControl<GUIBase_Button>(Layout, PASSWORD2_BUTTON)
					.SetDisabled(canChangePassword && passwordChanged ? !passwordIsValid : true);
		GuiBaseUtils.GetControl<GUIBase_Roller>(Layout, REGION_ROLLER).SetSelection((int)m_Region);
		GuiBaseUtils.GetControl<GUIBase_Button>(Layout, CONFIRM_BUTTON).SetDisabled(anyChanged ? !canApplyChanges : true);

		GUIBase_Switch news = GuiBaseUtils.GetControl<GUIBase_Switch>(Layout, NEWS_SWITCH);
		news.IsDisabled = CloudUser.instance.userAccountKind == E_UserAcctKind.Guest;
		news.SetValue(m_ReceiveNews);

		UpdateDecoration(REGION_DECOR, (int)m_Region);
	}

	void UpdateDecoration(string name, int value)
	{
		GUIBase_Widget decor = GetWidget(name);
		if (decor == null)
			return;

		GUIBase_Widget[] widgets = decor.GetComponentsInChildren<GUIBase_Widget>();
		foreach (GUIBase_Widget widget in widgets)
		{
			if (widget.name.StartsWith(ITEM_PREFIX) == false)
				continue;

			int idx;
			if (int.TryParse(widget.name.Substring(ITEM_PREFIX.Length), out idx) == false)
				continue;

			widget.Show(idx == value, true);
		}
	}

	IEnumerator UpdateProfile_Coroutine()
	{
		// show message box with some info
		GuiPopupMessageBox msgbox =
						Owner.ShowPopup("MessageBox", TextDatabase.instance[0107000], TextDatabase.instance[0107022]) as GuiPopupMessageBox;
		msgbox.SetButtonVisible(false);

		// update profile
		UpdateMFAccountAndFetchPPI action = CloudUser.instance.UpdateUser(m_Password1Hash,
																		  GuiBaseUtils.FixNickname(m_Nickname, CloudUser.instance.userName_TODO),
																		  null,
																		  m_ReceiveNews,
																		  m_Region);
		while (action.isDone == false)
		{
			yield return new WaitForSeconds(0.2f);
		}

		if (action.isSucceeded == true)
		{
			// update cloud user with actual informations
			string username = "";
			string nickname = "";
			string password = "";
			int pwdLength = 0;
			bool rememberMe = true;
			bool autoLogin = true;
			CloudUser.instance.GetLoginData(ref nickname, ref username, ref password, ref pwdLength, ref rememberMe, ref autoLogin);
			CloudUser.instance.SetLoginData(CloudUser.instance.primaryKey,
											m_Nickname,
											username,
											m_Password1Hash,
											m_PasswordLength,
											rememberMe,
											autoLogin);
			CloudUser.instance.receiveNews = m_ReceiveNews;
			CloudUser.instance.region = m_Region;

			PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();
			if (ppi != null)
			{
				ppi.Name = m_Nickname;
				PPIManager.Instance.NotifyLocalPPIChanged();
			}

			// connect to correct lobby
			LobbyClient.ConnectToLobby(m_Region);

			// inform user that we are done
			msgbox.SetText(TextDatabase.instance[0107020]);
			msgbox.SetHandler((popup, result) => { Owner.Back(); });
		}
		else
		{
			// inform user that there were a problem with updating
			msgbox.SetText(TextDatabase.instance[0107021]);
		}

		// let user press 'OK'
		msgbox.SetButtonText(TextDatabase.instance[0107024]);
		msgbox.SetButtonVisible(true);
	}
}
