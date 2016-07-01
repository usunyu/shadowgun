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
using System.Collections.Generic;
using LitJson;

public class GuiScreenChatFriends : GuiScreen, IGuiPageChat, Chat.IListener
{
	readonly static int MAX_ROWS = 15;

	readonly static string CMD_START = "lobby/chat/friend/start";
	readonly static string CMD_JOINED = "lobby/chat/friend/joined";
	readonly static string CMD_LEAVE = "lobby/chat/friend/leave";
	readonly static string CMD_FAILED = "lobby/chat/friend/failed";

	readonly static string CHANNEL = "chat/channel/friend/{0}_{1}";

	enum E_FriendAction
	{
		Remove
	}
	delegate void FriendActionDelegate(string friendName, E_FriendAction action);

	class Messages : CircularBuffer<Chat.Message>
	{
		public Messages(int size) : base(size)
		{
		}

		public Chat.Message[] Data
		{
			get { return base.ToArray(); }
			set { base.FromArray(value ?? new Chat.Message[0]); }
		}
	}

	class FriendInfo
	{
		public string Master;
		public string Slave;
		public string Channel;

		public bool IsJoined
		{
			get { return string.IsNullOrEmpty(Master) == false && string.IsNullOrEmpty(Slave) == false ? true : false; }
		}

		public Messages Messages = new Messages(ScreenComponentChat.MAX_MESSAGES);
		public int UnreadMessages;

		public string PrimaryKey;
		public string Nickname;
		public int Rank;
	}

	class FriendLine
	{
		// ---------------------------------------------------------------------------------------------------------------------	
		string m_FriendName;
		FriendActionDelegate m_FriendActionDelegate;
		GUIBase_Widget m_Line;

		GUIBase_Widget m_Highlight;
		GUIBase_Label m_Nickname;
		GUIBase_Label m_RankText;
		GUIBase_MultiSprite m_RankIcon;
		GUIBase_Label m_UnreadMsgs;
		GUIBase_Button m_Remove;

		// ---------------------------------------------------------------------------------------------------------------------							
		public FriendLine(GUIBase_Widget line, FriendActionDelegate dlgt)
		{
			m_FriendActionDelegate = dlgt;

			Transform trans = line.transform;

			m_Line = line;
			m_Highlight = trans.GetChildComponent<GUIBase_Widget>("SelectedBackground");
			m_Nickname = trans.GetChildComponent<GUIBase_Label>("Nickname");
			m_RankText = trans.GetChildComponent<GUIBase_Label>("TextRank");
			m_RankIcon = trans.GetChildComponent<GUIBase_MultiSprite>("PlayerRankPic");
			m_UnreadMsgs = trans.GetChildComponent<GUIBase_Label>("UnreadMsgs");
			m_Remove = trans.GetChildComponent<GUIBase_Button>("Remove_Button");

			m_Remove.RegisterTouchDelegate(() => { OnFriendAction(E_FriendAction.Remove); });
		}

		public void Update(FriendInfo friend, bool selected)
		{
			m_FriendName = friend.PrimaryKey;

			m_Highlight.Show(m_Line.Visible && selected, true);
			m_Nickname.SetNewText(GuiBaseUtils.FixNameForGui(friend.Nickname));
			m_RankText.SetNewText(friend.Rank <= 0 ? "" : friend.Rank.ToString());
			m_UnreadMsgs.SetNewText(string.Format(TextDatabase.instance[0506013], friend.UnreadMessages));

			string rankState = string.Format("Rank_{0}", Mathf.Min(friend.Rank, m_RankIcon.Count - 1).ToString("D2"));
			m_RankIcon.State = friend.Rank <= 0 ? GUIBase_MultiSprite.DefaultState : rankState;
		}

		public void Show()
		{
			if (m_Line.Visible == false)
			{
				m_Line.Show(true, true);
			}
		}

		public void Hide()
		{
			m_Line.Show(false, true);
		}

		void OnFriendAction(E_FriendAction action)
		{
			if (m_FriendActionDelegate != null)
			{
				m_FriendActionDelegate(m_FriendName, action);
			}
		}
	};

	public static class ChannelCache
	{
		static Dictionary<string, string> m_Cache = new Dictionary<string, string>();

		public static string GetChannelFor(string identifier, string master, string slave)
		{
			string channel;
			if (m_Cache.TryGetValue(identifier, out channel) == false)
			{
				channel = string.Format(CHANNEL, master, slave);
				m_Cache[identifier] = channel;
			}
			return channel;
		}
	}

	// PRIVATE MEMBERS

	static GuiScreenChatFriends m_Instance;

	ScreenComponentChat m_Chat;
	string m_PrimaryKey = "default";
	FriendLine[] m_GuiLines;
	List<FriendInfo> m_Friends = new List<FriendInfo>();
	GUIBase_List m_FriendList;
	int m_ActiveFriend = -1;

	// PUBLIC METHODS

	public static bool StartChat(string primaryKey)
	{
		if (m_Instance == null)
			return false;
		return m_Instance.StartChatImpl(primaryKey);
	}

	// IGUIPAGECHAT INTERFACE

	public string CaptionText
	{
		get { return string.Format(TextDatabase.instance[0506010], UnreadMessages, m_Friends.Count); }
	}

	public int UnreadMessages
	{
		get
		{
			int result = 0;
			m_Friends.ForEach((obj) => { result += obj.UnreadMessages; });
			return result;
		}
	}

	public bool NotifyUser
	{
		get { return UnreadMessages > 0 ? true : false; }
	}

	// GUIOVERLAY INTERFACE

	protected override void OnViewInit()
	{
		m_Instance = this;

		base.OnViewInit();

		CloudUser.authenticationChanged += OnAuthenticationChanged;

		m_Chat = RegisterComponent<ScreenComponentChat>();
		m_Chat.MaxRows = MAX_ROWS;

		m_FriendList = GuiBaseUtils.GetControl<GUIBase_List>(Layout, "FriendList");
		InitGuiLines();
	}

	protected override void OnViewDestroy()
	{
		CloudUser.authenticationChanged -= OnAuthenticationChanged;
		OnAuthenticationChanged(false);

		base.OnViewDestroy();
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		m_FriendList.OnUpdateRow += OnUpdateTableRow;
		m_FriendList.OnSelectRow += OnSelectTableRow;

		ActivateChat(m_ActiveFriend < 0 && m_Friends.Count > 0 ? 0 : m_ActiveFriend);
	}

	protected override void OnViewHide()
	{
		m_Chat.CanSendMessage = false;

		m_FriendList.OnUpdateRow -= OnUpdateTableRow;
		m_FriendList.OnSelectRow -= OnSelectTableRow;

		base.OnViewHide();
	}

	protected override void OnViewUpdate()
	{
		base.OnViewUpdate();

		FriendInfo info = GetFriendInfo(m_ActiveFriend);
		m_Chat.CanSendMessage = info != null ? info.IsJoined : false;
	}

	// HANDLERS

	void OnAuthenticationChanged(bool state)
	{
		if (state == true)
		{
			m_PrimaryKey = CloudUser.instance.primaryKey;

			LobbyClient.RegisterPlayerMessageObserver(OnChatStart, m_PrimaryKey, CMD_START);
			LobbyClient.RegisterPlayerMessageObserver(OnChatJoined, m_PrimaryKey, CMD_JOINED);
			LobbyClient.RegisterPlayerMessageObserver(OnChatLeave, m_PrimaryKey, CMD_LEAVE);
			LobbyClient.RegisterPlayerMessageObserver(OnChatFailed, m_PrimaryKey, CMD_FAILED);
		}
		else
		{
			while (m_Friends.Count > 0)
			{
				LeaveChat(0, true);
			}

			LobbyClient.UnregisterPlayerMessageObserver(OnChatStart, m_PrimaryKey, CMD_START);
			LobbyClient.UnregisterPlayerMessageObserver(OnChatJoined, m_PrimaryKey, CMD_JOINED);
			LobbyClient.UnregisterPlayerMessageObserver(OnChatLeave, m_PrimaryKey, CMD_LEAVE);
			LobbyClient.UnregisterPlayerMessageObserver(OnChatFailed, m_PrimaryKey, CMD_FAILED);
		}
	}

	void Chat.IListener.ReceiveMessage(string channel, Chat.Message message)
	{
		FriendInfo info = m_Friends.Find(obj => obj.Channel == channel);
		if (info == null)
		{
			Chat.Unregister(channel, this);
		}
		else
		{
			AddChatMessage(info, message);
		}
	}

	void OnChatStart(string primaryKey, string messageId, string messageText)
	{
		JsonData data = JsonMapper.ToObject(messageText);
		string master = data["master"].ToString();
		string slave = data["slave"].ToString();

		var friend = GameCloudManager.friendList.friends.Find(obj => obj.PrimaryKey == master);
		bool isFriend = friend != null ? true : false;

		primaryKey = m_PrimaryKey == master ? slave : master;
		FriendInfo info = m_Friends.Find(obj => obj.PrimaryKey == primaryKey);

		if (GuiFrontendMain.IsVisible == false || isFriend == false || JoinChat(master, slave, false) == false)
		{
			var result = new
			{
				master = master,
				slave = slave
			};
			LobbyClient.SendMessageToPlayer(master == m_PrimaryKey ? slave : master, CMD_FAILED, JsonMapper.ToJson(result));
		}
		else
		{
			if (info == null)
			{
				info = m_Friends.Find(obj => obj.PrimaryKey == primaryKey);
				AddSystemMessage(info, string.Format(TextDatabase.instance[0506011], GuiBaseUtils.FixNameForGui(info.Nickname)));
			}
		}
	}

	void OnChatJoined(string primaryKey, string messageId, string messageText)
	{
		JsonData data = JsonMapper.ToObject(messageText);
		string master = data["master"].ToString();
		string slave = data["slave"].ToString();

		primaryKey = m_PrimaryKey == master ? slave : master;
		FriendInfo info = m_Friends.Find(obj => obj.PrimaryKey == primaryKey);
		if (info == null)
			return;

		AddSystemMessage(info, string.Format(TextDatabase.instance[0506011], GuiBaseUtils.FixNameForGui(info.Nickname)));
	}

	void OnChatLeave(string primaryKey, string messageId, string messageText)
	{
		JsonData data = JsonMapper.ToObject(messageText);
		string master = data["master"].ToString();
		string slave = data["slave"].ToString();

		primaryKey = m_PrimaryKey == master ? slave : master;
		FriendInfo info = m_Friends.Find(obj => obj.PrimaryKey == primaryKey);
		if (info == null)
			return;

		info.Master = null;
		info.Slave = null;

		AddSystemMessage(info, string.Format(TextDatabase.instance[0506008], GuiBaseUtils.FixNameForGui(info.Nickname)));
	}

	void OnChatFailed(string primaryKey, string messageId, string messageText)
	{
		JsonData data = JsonMapper.ToObject(messageText);
		string master = data["master"].ToString();
		string slave = data["slave"].ToString();

		primaryKey = m_PrimaryKey == master ? slave : master;
		FriendInfo info = m_Friends.Find(obj => obj.PrimaryKey == primaryKey);
		if (info == null)
			return;

		info.Master = null;
		info.Slave = null;

		AddSystemMessage(info, TextDatabase.instance[0506012]);
	}

	// HANDLERS

	void OnUpdateTableRow(GUIBase_Widget widget, int rowIndex, int itemIndex)
	{
		FriendLine row = m_GuiLines[rowIndex];

		if (itemIndex < m_Friends.Count)
		{
			FriendInfo info = m_Friends[itemIndex];
			bool active = m_ActiveFriend == itemIndex ? true : false;

			if (active == true)
			{
				info.UnreadMessages = 0;
			}

			row.Show();
			row.Update(info, active);
		}
		else
		{
			row.Hide();
		}
	}

	void OnSelectTableRow(GUIBase_Widget widget, int rowIndex, int itemIndex)
	{
		ActivateChat(itemIndex);
	}

	void OnFriendAction(string primaryKey, E_FriendAction action)
	{
		switch (action)
		{
		case E_FriendAction.Remove:
			int idx = m_Friends.FindIndex(obj => obj.PrimaryKey == primaryKey);
			if (idx >= 0)
			{
				LeaveChat(idx, true);
			}
			break;
		default:
			break;
		}
	}

	// PRIVATE METHODS

	void AddChatMessage(FriendInfo info, Chat.Message message)
	{
		bool active = m_Friends.FindIndex(obj => obj.PrimaryKey == info.PrimaryKey) == m_ActiveFriend;

		AddMessage(info, active, message);
	}

	void AddSystemMessage(FriendInfo info, string text)
	{
		Chat.Message message = Chat.Message.Create("", "System", -1, text);

		bool active = m_Friends.FindIndex(obj => obj.PrimaryKey == info.PrimaryKey) == m_ActiveFriend;
		if (active == true)
		{
			m_Chat.AddMessage(message);
		}

		AddMessage(info, active, message);
	}

	void AddMessage(FriendInfo info, bool active, Chat.Message message)
	{
		info.Messages.Enqueue(message);

		if (active == false || IsVisible == false)
		{
			info.UnreadMessages++;
		}

		if (IsVisible == true)
		{
			m_FriendList.Widget.SetModify();
		}
	}

	bool StartChatImpl(string other)
	{
		FriendInfo info = m_Friends.Find(obj => obj.PrimaryKey == other);
		bool wasJoined = info != null ? info.IsJoined : false;

		if (JoinChat(m_PrimaryKey, other, true) == false)
			return false;

		if (wasJoined == false)
		{
			var result = new
			{
				master = m_PrimaryKey,
				slave = other
			};
			LobbyClient.SendMessageToPlayer(other, CMD_START, JsonMapper.ToJson(result));
		}

		return true;
	}

	bool JoinChat(string master, string slave, bool activate)
	{
		string primaryKey = m_PrimaryKey == master ? slave : master;
		FriendList.FriendInfo friend = GameCloudManager.friendList.friends.Find(obj => obj.PrimaryKey == primaryKey);
		if (friend == null)
			return false;

		int idx = m_Friends.FindIndex(obj => obj.PrimaryKey == primaryKey);
		if (idx < 0)
		{
			idx = m_Friends.Count;

			m_Friends.Add(new FriendInfo()
			{
				PrimaryKey = friend.PrimaryKey,
				Nickname = friend.Nickname,
				Rank = friend.Rank
			});

			m_FriendList.MaxItems = m_Friends.Count;
		}

		FriendInfo info = m_Friends[idx];

		bool sendJoined = false;
		if (info.Master != master)
		{
			Chat.Unregister(info.Channel, this);
			sendJoined = true;
		}

		info.Master = master;
		info.Slave = slave;
		info.Channel = ChannelCache.GetChannelFor(m_PrimaryKey + primaryKey, master, slave);

		Chat.Register(info.Channel, this);

		if (activate == true || m_ActiveFriend < 0)
		{
			ActivateChat(idx);
		}

		m_FriendList.Widget.SetModify();

		if (sendJoined == true)
		{
			var result = new
			{
				master = info.Master,
				slave = info.Slave
			};
			LobbyClient.SendMessageToPlayer(info.Master == m_PrimaryKey ? info.Slave : info.Master, CMD_JOINED, JsonMapper.ToJson(result));
		}

		return true;
	}

	void LeaveChat(int friendIdx, bool informOtherSide)
	{
		if (friendIdx < 0 || friendIdx >= m_Friends.Count)
			return;

		FriendInfo info = m_Friends[friendIdx];
		m_Friends.RemoveAt(friendIdx);
		if (info == null)
			return;

		if (informOtherSide == true && info.IsJoined == true)
		{
			var result = new
			{
				master = info.Master,
				slave = info.Slave
			};
			LobbyClient.SendMessageToPlayer(info.Master == m_PrimaryKey ? info.Slave : info.Master, CMD_LEAVE, JsonMapper.ToJson(result));
		}

		Chat.Unregister(info.Channel, this);

		info.Master = null;
		info.Slave = null;

		ActivateChat(Mathf.Min(friendIdx, m_Friends.Count - 1));
	}

	void ActivateChat(int friendIdx)
	{
		// deactivate old friend
		//FriendInfo info = GetFriendInfo(m_ActiveFriend);
		m_Chat.Channel = null;
		m_Chat.Clear();

		// store new friend index
		m_ActiveFriend = friendIdx;

		// activate old friend
		FriendInfo info = GetFriendInfo(m_ActiveFriend);
		if (info != null)
		{
			m_Chat.Channel = info.Channel;
			m_Chat.Messages = info.Messages.Data;
		}
	}

	FriendInfo GetFriendInfo(int idx)
	{
		return idx >= 0 && idx < m_Friends.Count ? m_Friends[idx] : null;
	}

	void InitGuiLines()
	{
		if (m_FriendList.numOfLines <= 0)
			return;

		m_GuiLines = new FriendLine[m_FriendList.numOfLines];
		for (int idx = 0; idx < m_FriendList.numOfLines; ++idx)
		{
			m_GuiLines[idx] = new FriendLine(m_FriendList.GetWidgetOnLine(idx), OnFriendAction);
		}
	}
}
