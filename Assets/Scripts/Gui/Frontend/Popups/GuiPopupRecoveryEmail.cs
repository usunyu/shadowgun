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

public class GuiPopupRecoveryEmail : GuiPopupAnimatedBase
{
	readonly static int ERROR_EMAIL_NOT_VALID = 02040521; // Email address is not in a valid format!

	readonly static int MAX_EMAIL_DISPLAY_LEGTH = 46;

	// PRIVATE MEMBERS

	GUIBase_Button m_ButtonOK;
	GUIBase_Button m_EmailButton;
	GUIBase_Label m_HintLabel;
	GUIBase_Switch m_ReceiveNewsSwitch;

	public string m_Email;
	public bool m_ReceiveNews = true;

	// PUBLIC MEMBERS

	public string Email
	{
		get { return m_Email; }
		set
		{
			m_Email = (value ?? "").ToLower();
			if (m_Email.Length >= MAX_EMAIL_DISPLAY_LEGTH)
			{
				m_EmailButton.TextFieldText = m_Email.Substring(0, Mathf.Min(m_Email.Length, MAX_EMAIL_DISPLAY_LEGTH - 3)) + "...";
				m_EmailButton.SetNewText(m_EmailButton.TextFieldText);
			}
			else
			{
				m_EmailButton.TextFieldText = m_Email;
				m_EmailButton.SetNewText(m_Email);
			}

			CheckEmailValidity();
		}
	}

	public bool ReceiveNews
	{
		get { return m_ReceiveNews; }
		set
		{
			m_ReceiveNews = value;
			m_ReceiveNewsSwitch.SetValue(m_ReceiveNews);
		}
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
		m_ScreenLayout = GetLayout("Login", "MMEmail_Layout");

		base.OnViewInit();
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		m_ButtonOK = RegisterButtonDelegate("OK_Button", () => { SendResult(E_PopupResultCode.Ok); }, null);

		RegisterButtonDelegate("Skip_Button", () => { SendResult(E_PopupResultCode.Cancel); }, null);

		m_EmailButton = RegisterButtonDelegate("Email_Button",
											   () =>
											   {
#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID)
			ShowKeyboard(m_EmailButton, GuiScreen.E_KeyBoardMode.Email, OnKeyboardClose, m_Email, TextDatabase.instance[0103041]);
#endif
											   },
											   null);

		AddTextField(m_EmailButton, OnKeyboardClose);

		m_HintLabel = GuiBaseUtils.GetControl<GUIBase_Label>(Layout, "Hint_Label");
		m_HintLabel.SetNewText("");

		m_ReceiveNewsSwitch = GuiBaseUtils.RegisterSwitchDelegate(Layout, "WantNews_Switch", (switchValue) => { m_ReceiveNews = switchValue; });

		CheckEmailValidity();
	}

	protected override void OnViewHide()
	{
		RegisterButtonDelegate("OK_Button", null, null);
		RegisterButtonDelegate("Skip_Button", null, null);

		base.OnViewHide();
	}

	// HANDLERS

	void OnKeyboardClose(GUIBase_Button inInput, string inText, bool inInputCanceled)
	{
		if (inInputCanceled == true)
			return;

		Email = (inText ?? "").Trim();
	}

	// PRIVATE METHODS

	void CheckEmailValidity()
	{
		const string pattern =
						@"^([0-9a-zA-Z]" + // Start with a digit or alphabate
						@"([\+\-_\.][0-9a-zA-Z]+)*" + // No continues or ending +-_. chars in email
						@")+" +
						@"@(([0-9a-zA-Z][-\w]*[0-9a-zA-Z]*\.)+[a-zA-Z0-9]{2,17})$";

		Match match = new Regex(pattern).Match(m_Email ?? "");
		if (match.Success == true)
		{
			m_HintLabel.SetNewText("");
			m_ReceiveNewsSwitch.IsDisabled = false;
			m_ButtonOK.IsDisabled = false;
		}
		else
		{
			m_HintLabel.SetNewText(string.IsNullOrEmpty(m_Email) ? "" : TextDatabase.instance[ERROR_EMAIL_NOT_VALID]);
			m_ReceiveNewsSwitch.IsDisabled = true;
			m_ButtonOK.IsDisabled = true;
		}
	}
}
