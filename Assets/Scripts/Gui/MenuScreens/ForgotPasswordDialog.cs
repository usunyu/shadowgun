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
public class ForgotPasswordDialog : GuiPopup
{
	public override void SetCaption(string inCaption)
	{
	}

	public override void SetText(string inText)
	{
	}

	protected override void OnViewInit()
	{
		m_ScreenLayout = GetLayout("Login", "MMForgotPasword_Dialog");

		base.OnViewInit();

		m_OKButton = PrepareButton(m_ScreenLayout, "OK_Button", null, null);
		m_NameButton = PrepareButton(m_ScreenLayout, "Username_Button", null, null);

		m_CancelButton = PrepareButton(m_ScreenLayout, "Cancel_Button", null, null);
		m_CaptionLabel = PrepareLabel("Caption_Label");

		AddTextField(m_NameButton, Delegate_OnKeyboardClose, null, CloudUser.MAX_ACCOUNT_NAME_LENGTH);
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		m_OKButton.RegisterReleaseDelegate2(Delegate_OK);
		m_CancelButton.RegisterReleaseDelegate2(Delegate_Cancel);
		m_NameButton.RegisterReleaseDelegate2(Delegate_UserName);

		if (m_IsForPassword)
		{
			m_CaptionLabel.SetNewText(TextDatabase.instance[0107016]);
			m_NameButton.TextFieldText = "";
			m_NameButton.SetNewText("");
		}
		else
		{
			m_CaptionLabel.SetNewText(TextDatabase.instance[0103013]);

			if (CloudUser.instance.authenticationDataPresent == true)
			{
				m_UserName = CloudUser.instance.userName_TODO; //TODO: PRIMARY KEY - nepotrebujeme nahodou i primary key?
			}
			else
			{
				m_UserName = string.Empty;
			}

			m_NameButton.TextFieldText = GuiBaseUtils.FixNameForGui(m_UserName);
			m_NameButton.SetNewText(m_NameButton.TextFieldText);
		}
		UpdateOKButton();
	}

	protected override void OnViewHide()
	{
		m_UserName = string.Empty;

		base.OnViewHide();
	}

	// #################################################################################################################
	// ###  Delegates  #################################################################################################
	void Delegate_OK(GUIBase_Widget inInstigator)
	{
		//Debug.Log("Delegate_OK: ");
		MFDebugUtils.Assert(inInstigator == m_OKButton.Widget);

		if (m_IsForPassword)
			SendResult(E_PopupResultCode.Ok);
		else
			StartCoroutine(PasswordRecoveryRequest_Coroutine());
	}

	void Delegate_Cancel(GUIBase_Widget inInstigator)
	{
		Owner.Back();
		SendResult(E_PopupResultCode.Cancel);
	}

	void Delegate_UserName(GUIBase_Widget inInstigator)
	{
		//Debug.Log("Delegate_FriendName: ");
		MFDebugUtils.Assert(inInstigator == m_NameButton.Widget);

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

	void Delegate_OnKeyboardClose(GUIBase_Button inInput, string inKeyboardText, bool inInputCanceled)
	{
		MFDebugUtils.Assert(m_NameButton == inInput);

		m_UserName = inKeyboardText.ToLower();

		if (m_IsForPassword)
			m_NameButton.SetNewText(new string('*', GuiBaseUtils.FixNameForGui(m_UserName).Length));
		else
			m_NameButton.SetNewText(GuiBaseUtils.FixNameForGui(m_UserName));

		UpdateOKButton();
	}

	// =================================================================================================================
	// internal functionality...
	void UpdateOKButton()
	{
#if UNITY_EDITOR
		bool disabled = false;
#else
		bool disabled = string.IsNullOrEmpty(m_UserName) == true || m_UserName.Length < CloudUser.MIN_ACCOUNT_NAME_LENGTH ? true : false;
#endif

		m_OKButton.SetDisabled(disabled);
		m_OKButton.Widget.Color = disabled ? Color.gray : Color.white;
	}

	IEnumerator PasswordRecoveryRequest_Coroutine()
	{
		if (string.IsNullOrEmpty(m_UserName) == true)
			yield break;

		string username = m_UserName;
		IViewOwner owner = Owner;

		owner.Back();

		GuiPopupMessageBox popup =
						(GuiPopupMessageBox)owner.ShowPopup("MessageBox", TextDatabase.instance[0103044], TextDatabase.instance[0103043]);
		popup.SetButtonVisible(false);

		yield return new WaitForSeconds(0.2f);

		string primaryKey;
		{
			UserGetPrimaryKey action = new UserGetPrimaryKey(username);
			GameCloudManager.AddAction(action);

			while (action.isDone == false)
				yield return new WaitForEndOfFrame();

			primaryKey = action.primaryKey;
		}

		E_PopupResultCode result;
		{
			ForgotPassword action = new ForgotPassword(primaryKey);
			GameCloudManager.AddAction(action);

			while (action.isDone == false)
				yield return new WaitForEndOfFrame();

			result = action.isSucceeded == true ? E_PopupResultCode.Ok : E_PopupResultCode.Failed;
		}

		popup.ForceClose();

		SendResult(result);
	}

	// #################################################################################################################		
	GUIBase_Button m_OKButton;
	GUIBase_Button m_CancelButton;
	GUIBase_Button m_NameButton;
	GUIBase_Label m_CaptionLabel;

	bool m_IsForPassword = false;

	public bool IsForPassword
	{
		get { return m_IsForPassword; }
		set
		{
			m_IsForPassword = value;
			OnViewShow();
		}
	}

	public string TextFieldData
	{
		get { return (m_NameButton == null) ? null : m_NameButton.TextFieldText; }
	}

	string m_UserName = string.Empty;
}
