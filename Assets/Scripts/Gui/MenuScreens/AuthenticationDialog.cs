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
public class AuthenticationDialog : GuiPopupAnimatedBase
{
	CloudUser.E_AuthenticationStatus lastAuthStatus;

	public override void SetCaption(string inCaption)
	{
	}

	public override void SetText(string inText)
	{
	}

	protected override void OnViewInit()
	{
		m_ScreenLayout = GetLayout("Login", "MMAuthentication_Dialog");

		base.OnViewInit();

		m_OKButton = PrepareButton(m_ScreenLayout, "OK_Button", null, Delegate_OK);
		m_ProggresLabel = PrepareLabel(m_ScreenLayout, "Text_Label");
		m_CaptionLabel = PrepareLabel(m_ScreenLayout, "Caption_Label");
		m_CaptionLabel.SetNewText(TextDatabase.instance[02040016]);
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		CloudUser.instance.AuthenticateLocalUser();
		SetStatus(CloudUser.E_AuthenticationStatus.None);
	}

	protected override void OnViewUpdate()
	{
		if (IsVisible && lastAuthStatus != CloudUser.instance.authenticationStatus)
		{
			SetStatus(CloudUser.instance.authenticationStatus);
		}

		base.OnViewUpdate();
	}

	// #################################################################################################################
	// ###  Delegates  #################################################################################################
	void Delegate_OK(GUIBase_Widget inInstigator)
	{
		Owner.Back();
	}

	// =================================================================================================================
	// internal functionality...
	void UpdateOKButton()
	{
		bool disabled = lastAuthStatus != CloudUser.E_AuthenticationStatus.Failed;

		m_OKButton.Widget.Show(!disabled, true);
		m_OKButton.SetDisabled(disabled);
		m_OKButton.Widget.Color = disabled ? Color.gray : Color.white;
	}

	void SetStatus(CloudUser.E_AuthenticationStatus status)
	{
		switch (status)
		{
		case CloudUser.E_AuthenticationStatus.None:
			m_ProggresLabel.SetNewText(AUTHENTICATION_WAIT);
			break;
		case CloudUser.E_AuthenticationStatus.InProgress:
			m_ProggresLabel.SetNewText(AUTHENTICATION_IN_PROGRESS);
			break;
		case CloudUser.E_AuthenticationStatus.RetryIAP:
			m_ProggresLabel.SetNewText(AUTHENTICATION_CHECK_IAP);
			break;
		case CloudUser.E_AuthenticationStatus.RetrievingPPI:
			m_ProggresLabel.SetNewText(AUTHENTICATION_GET_PPI);
			break;
		case CloudUser.E_AuthenticationStatus.Failed:
			m_ProggresLabel.SetNewText(AUTHENTICATION_FAILED);
			SendResult(E_PopupResultCode.Failed);
			break;
		case CloudUser.E_AuthenticationStatus.Ok:
			m_ProggresLabel.SetNewText(AUTHENTICATION_OK);
			SendResult(E_PopupResultCode.Success);
			break;
		}
		;

		lastAuthStatus = status;
		UpdateOKButton();
	}

	// #################################################################################################################		
	static int AUTHENTICATION_WAIT = 02040017;
	static int AUTHENTICATION_IN_PROGRESS = 02040011;
	static int AUTHENTICATION_CHECK_IAP = 02040015;
	static int AUTHENTICATION_GET_PPI = 02040012;
	static int AUTHENTICATION_FAILED = 02040013;
	static int AUTHENTICATION_OK = 02040014;

	GUIBase_Button m_OKButton;
	GUIBase_Label m_ProggresLabel;
	GUIBase_Label m_CaptionLabel;
}
