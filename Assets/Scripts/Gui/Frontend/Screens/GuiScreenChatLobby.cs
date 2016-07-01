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

//#define FAKE_MESSAGES

using UnityEngine;
using System.Collections;

public class GuiScreenChatLobby : GuiScreen, IGuiPageChat
{
	readonly static int MAX_ROWS = 15;

	readonly static string CHANNEL = "chat/channel/lobby";

	// PRIVATE MEMBERS

	ScreenComponentChat m_Chat;
	int m_UnreadMessages;

	// IGUIPAGECHAT INTERFACE

	public string CaptionText
	{
		get { return string.Format(TextDatabase.instance[0506009], UnreadMessages); }
	}

	public int UnreadMessages
	{
		get { return m_UnreadMessages; }
	}

	public bool NotifyUser
	{
		get { return false; }
	}

	// GUIOVERLAY INTERFACE

	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_Chat = RegisterComponent<ScreenComponentChat>();
		m_Chat.MaxRows = MAX_ROWS;
		m_Chat.Channel = CHANNEL;
		m_Chat.OnMessageReceived = OnMessageReceived;
		m_Chat.OnIsMessageSelectable = OnIsMessageSelectable;
		m_Chat.OnMessageAction = OnMessageAction;
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		m_UnreadMessages = 0;

#if FAKE_MESSAGES
		StartCoroutine(m_Chat.FakeMessages_Coroutine());
#endif
	}

	protected override void OnViewHide()
	{
#if FAKE_MESSAGES
		StopAllCoroutines();
#endif

		base.OnViewHide();
	}

	// HANDLERS

	void OnMessageReceived(Chat.Message message)
	{
		if (IsVisible == false)
		{
			m_UnreadMessages++;
		}
	}

	bool OnIsMessageSelectable(Chat.Message message)
	{
		// filter out friends
		FriendList.FriendInfo friend = GameCloudManager.friendList.friends.Find(obj => obj.PrimaryKey == message.PrimaryKey);
		if (friend != null)
			return false;

		// filter out pending friends
		FriendList.PendingFriendInfo pending = GameCloudManager.friendList.pendingFriends.Find(obj => obj.PrimaryKey == message.PrimaryKey);
		if (pending != null)
			return false;

		// done
		return true;
	}

	void OnMessageAction(Chat.Message message, GUIBase_Widget instigator)
	{
		if (instigator.name == "AddFriend_Button")
		{
			NewFriendDialog popup =
							(NewFriendDialog)Owner.ShowPopup("NewFriend", string.Empty, string.Empty, (inPopup, inResult) => { m_Chat.Refresh(); });
			popup.Nickname = message.Nickname;
			popup.Username = string.Empty;
			popup.PrimaryKey = message.PrimaryKey;
		}
	}
}
