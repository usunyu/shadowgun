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
public class NewFriendDialog : GuiPopupAnimatedBase
{
	readonly static int MAX_MESSAGE_LENGTH = 250;

	public override void SetCaption(string inCaption)
	{
	}

	public override void SetText(string inText)
	{
	}

	protected override void OnViewInit()
	{
		m_ScreenLayout = GetLayout("FriendList_Dialog", "AddNewFriend_Layout");

		base.OnViewInit();

		m_OKButton = PrepareButton(m_ScreenLayout, "OK_Button", null, null);
		m_NameButton = PrepareButton(m_ScreenLayout, "Username_Button", null, null);
		m_MessageButton = PrepareButton(m_ScreenLayout, "Message_Button", null, null);
		m_MessageText = GuiBaseUtils.GetControl<GUIBase_TextArea>(m_ScreenLayout, "Message_Text");

		m_CancelButton = PrepareButton(m_ScreenLayout, "Cancel_Button", null, null);

		AddTextField(m_NameButton, Delegate_OnKeyboardClose, null, CloudUser.MAX_ACCOUNT_NAME_LENGTH);
		AddTextField(m_MessageButton, Delegate_OnKeyboardClose, m_MessageText, MAX_MESSAGE_LENGTH, 5);
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		Username = string.Empty;
		Message = string.Format(TextDatabase.instance[02040236], CloudUser.instance.nickName);

		m_OKButton.RegisterReleaseDelegate2(Delegate_OK);
		m_CancelButton.RegisterReleaseDelegate2(Delegate_Cancel);
		m_NameButton.RegisterReleaseDelegate2(Delegate_Username);
		m_MessageButton.RegisterReleaseDelegate2(Delegate_Message);
	}

	protected override void OnViewHide()
	{
		m_Username = string.Empty;
		m_PrimaryKey = string.Empty;
		m_Nickname = string.Empty;
		m_Message = string.Empty;

		base.OnViewHide();
	}

	public string Username
	{
		get { return m_Username; }
		set
		{
			m_Username = value;
			if (string.IsNullOrEmpty(Nickname) == true)
			{
				m_NameButton.TextFieldText = GuiBaseUtils.FixNameForGui(m_Username);
				m_NameButton.SetNewText(m_NameButton.TextFieldText);
			}
			UpdateOKButton();
		}
	}

	public string PrimaryKey
	{
		get { return m_PrimaryKey; }
		set
		{
			m_PrimaryKey = value;
			UpdateOKButton();
		}
	}

	public string Nickname
	{
		get { return m_Nickname; }
		set
		{
			m_Nickname = value;
			m_NameButton.TextFieldText = GuiBaseUtils.FixNameForGui(m_Nickname);
			m_NameButton.SetNewText(m_NameButton.TextFieldText);
			m_NameButton.IsDisabled = string.IsNullOrEmpty(m_Nickname) ? false : true;
		}
	}

	public string Message
	{
		get { return m_Message; }
		set
		{
			m_Message = value;
			m_MessageText.SetNewText(m_Message);
			m_MessageButton.TextFieldText = m_Message;
			UpdateOKButton();
		}
	}

	// #################################################################################################################
	// ###  Delegates  #################################################################################################
	void Delegate_OK(GUIBase_Widget inInstigator)
	{
		//Debug.Log("Delegate_OK: ");
		MFDebugUtils.Assert(inInstigator == m_OKButton.Widget);

		if (string.IsNullOrEmpty(Username) == false || string.IsNullOrEmpty(PrimaryKey) == false)
		{
			StartCoroutine(AddNewFriend_Coroutine());
		}
		else
		{
			Owner.Back();
		}
	}

	void Delegate_Cancel(GUIBase_Widget inInstigator)
	{
		Owner.Back();
	}

	void Delegate_Username(GUIBase_Widget inInstigator)
	{
		//Debug.Log("Delegate_Username: ");
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
		ShowKeyboard(button, GuiScreen.E_KeyBoardMode.Default, Delegate_OnKeyboardClose, button.GetText(), "Enter username");
#endif
	}

	void Delegate_Message(GUIBase_Widget inInstigator)
	{
		GUIBase_Button button = inInstigator.GetComponent<GUIBase_Button>();
		if (button == null)
		{
			Debug.LogError("Internal error !!! ");
			return;
		}

#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID)
				// TODO move pLace holder into text database		
		ShowKeyboard(button, GuiScreen.E_KeyBoardMode.Default, Delegate_OnKeyboardClose, button.GetText(), "Enter username", MAX_MESSAGE_LENGTH);
#endif
	}

	void Delegate_OnKeyboardClose(GUIBase_Button inInput, string inKeyboardText, bool inInputCanceled)
	{
		string text = inKeyboardText.RemoveDiacritics();

		if (inInput == m_NameButton)
		{
			Username = text != null ? text.ToLower() : string.Empty;
		}
		else if (inInput == m_MessageButton)
		{
			Message = text;
		}

		UpdateOKButton();
	}

	// =================================================================================================================
	// internal functionality...
	void UpdateOKButton()
	{
		bool disabled = (string.IsNullOrEmpty(m_Username) && string.IsNullOrEmpty(m_PrimaryKey)) || string.IsNullOrEmpty(m_Message);

		m_OKButton.SetDisabled(disabled);
		//m_OKButton.Widget.Color = disabled ? Color.gray : Color.white;
	}

	IEnumerator AddNewFriend_Coroutine()
	{
		string username = Username;
		string primaryKey = PrimaryKey;
		string nickname = Nickname;
		string message = Message;
		IViewOwner owner = Owner;

		owner.Back();

		GuiPopupMessageBox popup =
						(GuiPopupMessageBox)owner.ShowPopup("MessageBox", TextDatabase.instance[02040204], TextDatabase.instance[00103043]);
		popup.SetButtonVisible(false);

		yield return new WaitForSeconds(0.2f);

		if (string.IsNullOrEmpty(primaryKey) == true)
		{
			UserGetPrimaryKey action = new UserGetPrimaryKey(username);
			GameCloudManager.AddAction(action);

			while (action.isDone == false)
				yield return new WaitForEndOfFrame();

			primaryKey = action.primaryKey;
		}

		GameCloudManager.friendList.AddNewFriend(primaryKey, username, nickname, message);

		popup.ForceClose();

		SendResult(E_PopupResultCode.Ok);
	}

	// #################################################################################################################		
	GUIBase_Button m_OKButton;
	GUIBase_Button m_CancelButton;
	GUIBase_Button m_NameButton;
	GUIBase_Button m_MessageButton;
	GUIBase_TextArea m_MessageText;

	string m_Username = string.Empty;
	string m_PrimaryKey = string.Empty;
	string m_Nickname = string.Empty;
	string m_Message = string.Empty;
}
