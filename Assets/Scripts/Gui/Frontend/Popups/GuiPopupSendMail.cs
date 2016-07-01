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
using FriendMessage = CloudMailbox.FriendMessage;
using FriendInfo = FriendList.FriendInfo;

public class GuiPopupSendMail : GuiPopupAnimatedBase
{
	readonly static int MAX_MESSAGE_LENGTH = 400;

	readonly static string CAPTION_LABEL = "Caption_Label";
	readonly static string MESSAGE_TEXT = "Message_Text";
	readonly static string MESSAGE_BUTTON = "Message_Button";
	readonly static string CANCEL_BUTTON = "Cancel_Button";
	readonly static string SEND_BUTTON = "Send_Button";

	// PRIVATE MEMBERS

	string m_PrimaryKey;
	string m_Message;
	GUIBase_Label m_CaptionLabel;
	GUIBase_TextArea m_MessageText;
	GUIBase_Button m_SendButton;
	GUIBase_Button m_MessageButton;

	// GUIPOPUP INTERFACE

	public override void SetCaption(string inPrimaryKey)
	{
		m_PrimaryKey = inPrimaryKey;

		FriendInfo friend = GameCloudManager.friendList.friends.Find(obj => obj.PrimaryKey == m_PrimaryKey);
		string nickname = friend != null ? friend.Nickname : m_PrimaryKey;
		string caption = string.Format(TextDatabase.instance[0104114], GuiBaseUtils.FixNameForGui(nickname));
		m_CaptionLabel.SetNewText(caption);
	}

	public override void SetText(string inText)
	{
		m_Message = inText ?? "";

		m_MessageText.SetNewText(m_Message);
		m_MessageButton.TextFieldText = m_Message;
	}

	// GUIVIEW INTERFACE

	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_CaptionLabel = GuiBaseUtils.GetControl<GUIBase_Label>(Layout, CAPTION_LABEL);
		m_MessageText = GuiBaseUtils.GetControl<GUIBase_TextArea>(Layout, MESSAGE_TEXT);

		m_MessageButton = GuiBaseUtils.GetControl<GUIBase_Button>(Layout, MESSAGE_BUTTON);
		AddTextField(m_MessageButton, Delegate_OnKeyboardClose, m_MessageText, MAX_MESSAGE_LENGTH, 8);
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		m_MessageButton = GuiBaseUtils.RegisterButtonDelegate(Layout, MESSAGE_BUTTON, () => { WriteMessage(); }, null);

		GuiBaseUtils.RegisterButtonDelegate(Layout, CANCEL_BUTTON, () => { Owner.Back(); }, null);

		m_SendButton = GuiBaseUtils.RegisterButtonDelegate(Layout,
														   SEND_BUTTON,
														   () =>
														   {
															   SendMessage();
															   Owner.Back();
														   },
														   null);
	}

	protected override void OnViewHide()
	{
		GuiBaseUtils.RegisterButtonDelegate(Layout, MESSAGE_BUTTON, null, null);
		GuiBaseUtils.RegisterButtonDelegate(Layout, CANCEL_BUTTON, null, null);
		GuiBaseUtils.RegisterButtonDelegate(Layout, SEND_BUTTON, null, null);

		base.OnViewHide();
	}

	protected override void OnViewUpdate()
	{
		base.OnViewUpdate();

		m_SendButton.IsDisabled = string.IsNullOrEmpty(m_Message);
	}

	// PRIVATE METHODS

	void Delegate_OnKeyboardClose(GUIBase_Button input, string text, bool cancelled)
	{
		if (cancelled == false && string.IsNullOrEmpty(text) == false)
		{
			SetText(text.RemoveDiacritics());
		}
		else
		{
			SetText(null);
		}
	}

	void WriteMessage()
	{
		//SetText("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec felis massa, facilisis eu varius et, imperdiet sit amet libero. Etiam molestie turpis a mi ultrices vitae consectetur dui venenatis. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Suspendisse potenti. Quisque malesuada iaculis nisl, sit amet pellentesque leo mattis non. Phasellus massa nunc.");
#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID)
		ShowKeyboard(m_SendButton, GuiScreen.E_KeyBoardMode.Default, Delegate_OnKeyboardClose, m_Message, MAX_MESSAGE_LENGTH);
#endif
	}

	void SendMessage()
	{
		if (string.IsNullOrEmpty(m_Message) == true)
			return;

		FriendMessage message = new FriendMessage();
		message.m_Mailbox = CloudMailbox.E_Mailbox.Product;
		message.m_Sender = CloudUser.instance.primaryKey;
		message.m_NickName = CloudUser.instance.nickName;
		message.m_Message = m_Message;
		GameCloudManager.mailbox.SendMessage(m_PrimaryKey, message);
	}
}
