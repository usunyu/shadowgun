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
using Match = System.Text.RegularExpressions.Match;

public class GuiPopupMigrateGuest : GuiPopupAnimatedBase
{
	const string EMAIL_PATTERN =
					@"^([0-9a-zA-Z]" + // Start with a digit or alphabate
					@"([\+\-_\.][0-9a-zA-Z]+)*" + // No continues or ending +-_. chars in email
					@")+" +
					@"@(([0-9a-zA-Z][-\w]*[0-9a-zA-Z]*\.)+[a-zA-Z0-9]{2,17})$";

	public enum E_Usage
	{
		QuickPlay,
		LoginMenu,
		LoginScreen,
		MainMenu
	}

	GUIBase_Button m_MigrateButton;
	GUIBase_Button m_CancelButton;
	GUIBase_Button m_EmailButton;

	GUIBase_Label m_Caption;
	GUIBase_TextArea m_Message;

	string m_Email = null;

	E_Usage m_Usage = E_Usage.MainMenu;
	int m_FinalMessageID;

	public string PrimaryKey { get; set; }
	public string Password { get; set; }

	public E_Usage Usage
	{
		get { return m_Usage; }
		set
		{
			m_Usage = value;
			switch (m_Usage)
			{
			case E_Usage.QuickPlay:
				SetText(TextDatabase.instance[00107029]);
				m_CancelButton.SetNewText(00107039);
				m_FinalMessageID = 00107041;
				break;
			case E_Usage.LoginMenu:
				SetText(TextDatabase.instance[00107029]);
				m_CancelButton.SetNewText(00107039);
				m_FinalMessageID = 00107041;
				break;
			case E_Usage.LoginScreen:
				SetText(TextDatabase.instance[00107028]);
				m_CancelButton.SetNewText(00107038);
				m_FinalMessageID = 00107033;
				break;
			case E_Usage.MainMenu:
				SetText(TextDatabase.instance[00107028]);
				m_CancelButton.SetNewText(00107034);
				m_FinalMessageID = 00107033;
				break;
			default:
				throw new System.NotImplementedException();
			}
		}
	}

	public override bool CanCloseByEscape
	{
		get { return false; }
	}

	public override void SetCaption(string caption)
	{
		if (caption != null)
			m_Caption.SetNewText(caption);
	}

	public override void SetText(string text)
	{
		if (text != null)
			m_Message.SetNewText(text);
	}

	protected override void OnViewInit()
	{
		base.OnViewInit();

		if (m_ScreenLayout == null)
		{
			Debug.LogError("GuiPopupMigrateGuestDialo :: There is not any layout specified for this dialog!");
			return;
		}

		m_MigrateButton = GuiBaseUtils.GetControl<GUIBase_Button>(m_ScreenLayout, "Migrate_Button");
		m_CancelButton = GuiBaseUtils.GetControl<GUIBase_Button>(m_ScreenLayout, "Cancel_Button");
		m_EmailButton = GuiBaseUtils.GetControl<GUIBase_Button>(m_ScreenLayout, "Email_Button");
		m_Caption = PrepareLabel(m_ScreenLayout, "Caption");
		m_Message = PrepareTextArea(m_ScreenLayout, "Message");

		AddTextField(m_EmailButton, Delegate_OnKeyboardClose, null);
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		m_Email = null;
		m_EmailButton.TextFieldText = string.Empty;
		m_EmailButton.SetNewText(107030);
		UpdateMigrateButton();

		m_MigrateButton.RegisterReleaseDelegate2(Delegate_Migrate);
		m_CancelButton.RegisterReleaseDelegate2(Delegate_Cancel);
		m_EmailButton.RegisterReleaseDelegate2(Delegate_Email);
	}

	protected override void OnViewHide()
	{
		m_MigrateButton.RegisterReleaseDelegate2(null);
		m_CancelButton.RegisterReleaseDelegate2(null);
		m_EmailButton.RegisterReleaseDelegate2(null);

		base.OnViewHide();
	}

	protected override void OnViewUpdate()
	{
		if (IsVisible == false)
			return;

		base.OnViewUpdate();
	}

	void Delegate_Migrate(GUIBase_Widget inInstigator)
	{
		bool invalid = false;
		if (string.IsNullOrEmpty(m_Email) == true)
		{
			invalid = true;
		}

		if (invalid == false)
		{
			Match match = new Regex(EMAIL_PATTERN).Match(m_Email ?? "");
			invalid = match.Success ? false : true;
		}

		if (invalid == false)
		{
			UserRequestMigrate action = new UserRequestMigrate(PrimaryKey, Password, m_Email);
			GameCloudManager.AddAction(action);

			PrimaryKey = string.Empty;
			Password = string.Empty;

			IViewOwner owner = Owner;
			owner.Back();
			SendResult(E_PopupResultCode.Ok);

			owner.ShowPopup("MessageBox", TextDatabase.instance[00107032], TextDatabase.instance[m_FinalMessageID], InfoPopupConfirmation);
		}
		else
		{
			Owner.ShowPopup("MessageBox", TextDatabase.instance[00107030], TextDatabase.instance[00107040], null);
		}
	}

	void InfoPopupConfirmation(GuiPopup popup, E_PopupResultCode result)
	{
		CloudUser.instance.LogoutLocalUser();

		GuiFrontendMain.ShowLoginMenu();
	}

	void Delegate_Cancel(GUIBase_Widget inInstigator)
	{
		if (Usage != E_Usage.MainMenu)
		{
			Owner.ShowPopup("ConfirmDialog",
							TextDatabase.instance[107035],
							TextDatabase.instance[107036],
							(popup, result) =>
							{
								if (result == E_PopupResultCode.Ok)
								{
									Owner.Back();
									SendResult(E_PopupResultCode.Cancel);
								}
							});
		}
		else
		{
			Owner.Back();
			SendResult(E_PopupResultCode.Cancel);
		}
	}

	void Delegate_Email(GUIBase_Widget inInstigator)
	{
		if (IsKeyboardControlEnabled)
			m_EmailButton.SetNewText(string.Empty);

#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID)
		ShowKeyboard(m_EmailButton, GuiScreen.E_KeyBoardMode.Default, Delegate_OnKeyboardClose, 
					 string.Empty, TextDatabase.instance[2040524]);
#endif
	}

	void Delegate_OnKeyboardClose(GUIBase_Button inInput, string inKeyboardText, bool inInputCanceled)
	{
		MFDebugUtils.Assert(m_EmailButton == inInput);

		m_Email = inKeyboardText.ToLower();

		m_EmailButton.SetNewText(m_Email);

		UpdateMigrateButton();
	}

	void UpdateMigrateButton()
	{
		if (string.IsNullOrEmpty(m_Email))
		{
			m_MigrateButton.isHighlighted = false;
			return;
		}

		Match match = new Regex(EMAIL_PATTERN).Match(m_Email ?? "");
		m_MigrateButton.isHighlighted = match.Success;
		m_MigrateButton.animate = match.Success;
	}
}
