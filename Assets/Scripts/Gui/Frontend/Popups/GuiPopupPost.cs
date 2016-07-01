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
using Message = CloudMailbox.BaseMessage;
using FriendMessage = CloudMailbox.FriendMessage;
using FriendRequest = CloudMailbox.FriendRequest;
using FriendRequestReject = CloudMailbox.FriendRequestReject;
using NewsMessage = CloudMailbox.NewsMessage;
using ResetResearch = CloudMailbox.ResetResearch;
using SystemMessage = CloudMailbox.SystemMessage;

static class MessageExtension
{
	public static string GetSender(this Message message)
	{
		if (message is FriendMessage)
		{
			FriendMessage friendMessage = (FriendMessage)message;
			if (string.IsNullOrEmpty(friendMessage.m_NickName) == false)
				return friendMessage.m_NickName;
			var friend = GameCloudManager.friendList.friends.Find(obj => obj.PrimaryKey == message.m_Sender);
			if (friend != null)
				return friend.Nickname;
		}
		else if (message is FriendRequest)
		{
			FriendRequest friendRequest = (FriendRequest)message;
			if (string.IsNullOrEmpty(friendRequest.m_NickName) == false)
				return friendRequest.m_NickName;
			if (string.IsNullOrEmpty(friendRequest.m_Username) == false)
				return friendRequest.m_Username;
		}
		else if (message is FriendRequestReject)
		{
			FriendRequestReject friendRequest = (FriendRequestReject)message;
			if (string.IsNullOrEmpty(friendRequest.m_NickName) == false)
				return friendRequest.m_NickName;
		}
		return message != null && string.IsNullOrEmpty(message.m_Sender) == false ? message.m_Sender : message.msgType;
	}

	public static string GetSubject(this Message message)
	{
		string text = message != null ? message.m_Message : "";
		if (message is NewsMessage)
		{
			NewsMessage msg = (NewsMessage)message;
			text = string.IsNullOrEmpty(msg.m_Subject) == false ? msg.m_Subject : msg.m_HeadLine;
		}
		else if (message is ResetResearch)
		{
			text = TextDatabase.instance[01170010];
		}
		return string.IsNullOrEmpty(text) == false ? text : "";
	}

	public static string GetMessage(this Message message)
	{
		string text = message != null ? message.m_Message : "";
		if (message is NewsMessage)
		{
			NewsMessage msg = (NewsMessage)message;
			text = string.IsNullOrEmpty(text) == true ? msg.m_HeadLine : text;
		}
		else if (message is ResetResearch)
		{
			text = TextDatabase.instance[01170010].Replace("\n", " ");
		}
		return string.IsNullOrEmpty(text) == false ? text : "";
	}

	public static string GetMultiSpriteState(this Message message)
	{
		return message != null ? message.GetType().Name : GUIBase_MultiSprite.DefaultState;
	}
}

public class UserGuideAction_Post : UserGuideAction_SystemDialogs<GuiPopupPost>
{
	// C-TOR

	public UserGuideAction_Post()
	{
		Priority = (int)E_UserGuidePriority.Post;
		AllowRepeatedExecution = true;
	}

	// USERGUIDEACTION INTERFACE

	protected override bool OnExecute()
	{
		if (base.OnExecute() == false)
			return false;

		if (GuiPopupPost.HasImportantMessage == false)
			return false;

		// display popup
		ShowPopup();

		// done
		return true;
	}
}

public class GuiPopupPost : GuiPopup, IGuiOverlayScreen
{
	readonly static int MAX_SUBJECT_LENGTH = 35;

	readonly static string CLOSE_BUTTON = "Close_Button";
	readonly static string FEEDBACK_BUTTON = "Feedback_Button";

	enum E_MessageAction
	{
		Reply,
		Remove
	}

	delegate void MessageActionDelegate(int messageIndex, E_MessageAction action);

	class MessageArea
	{
		int m_MessageIndex;
		MessageActionDelegate m_MessageActionDelegate;

		GUIBase_Label m_Subject;
		GUIBase_Label m_Date;
		GUIBase_TextArea m_Text;
		GUIBase_Button m_Reply;

		public void Init(GUIBase_Widget root, MessageActionDelegate dlgt)
		{
			m_MessageActionDelegate = dlgt;

			Transform trans = root.transform;

			m_Subject = trans.GetChildComponent<GUIBase_Label>("Subject");
			m_Date = trans.GetChildComponent<GUIBase_Label>("Date");
			m_Text = trans.GetChildComponent<GUIBase_TextArea>("Text");
			m_Reply = trans.GetChildComponent<GUIBase_Button>("Reply_Button");

			m_Reply.RegisterTouchDelegate(() => { OnMessageAction(E_MessageAction.Reply); });
		}

		public void Update(Message message, int index)
		{
			m_MessageIndex = index;

			m_Subject.SetNewText(message != null ? GuiBaseUtils.TrimLongText(message.GetSubject(), MAX_SUBJECT_LENGTH) : "");
			m_Date.SetNewText(message != null ? message.m_SendTime.ToLongRegionalString() : "");
			m_Text.SetNewText(message != null ? message.GetMessage() : TextDatabase.instance[0104112]);

			bool showReply = m_MessageIndex >= 0 && message is FriendMessage ? true : false;
			if (showReply != m_Reply.Widget.Visible)
			{
				m_Reply.Widget.Show(showReply, true);
			}
		}

		void OnMessageAction(E_MessageAction action)
		{
			if (m_MessageActionDelegate != null)
			{
				m_MessageActionDelegate(m_MessageIndex, action);
			}
		}
	}

	class MessageLine
	{
		// ---------------------------------------------------------------------------------------------------------------------	
		int m_MessageIndex;
		MessageActionDelegate m_MessageActionDelegate;

		GUIBase_Widget m_Line;
		GUIBase_Widget m_Highlight;
		GUIBase_Label m_Sender;
		GUIBase_MultiSprite m_SenderIcon;
		GUIBase_Sprite m_IsReadIcon;
		GUIBase_Label m_Subject;
		GUIBase_Button m_Remove;

		// ---------------------------------------------------------------------------------------------------------------------							
		public MessageLine(GUIBase_Widget line, MessageActionDelegate dlgt)
		{
			m_MessageActionDelegate = dlgt;

			Transform trans = line.transform;

			m_Line = line;
			m_Highlight = trans.GetChildComponent<GUIBase_Widget>("SelectedBackground");
			m_Sender = trans.GetChildComponent<GUIBase_Label>("Sender");
			m_SenderIcon = trans.GetChildComponent<GUIBase_MultiSprite>("SenderIcon");
			m_IsReadIcon = trans.GetChildComponent<GUIBase_Sprite>("IsReadIcon");
			m_Subject = trans.GetChildComponent<GUIBase_Label>("Subject");
			m_Remove = trans.GetChildComponent<GUIBase_Button>("Remove_Button");

			m_Remove.RegisterTouchDelegate(() => { OnMessageAction(E_MessageAction.Remove); });
		}

		public void Update(Message message, int index, bool selected)
		{
			m_MessageIndex = index;

			m_Highlight.Show(m_Line.Visible && selected, true);
			m_Sender.SetNewText(GuiBaseUtils.FixNameForGui(message.GetSender()));
			m_Subject.SetNewText(GuiBaseUtils.TrimLongText(message.GetSubject(), MAX_SUBJECT_LENGTH));

			m_SenderIcon.State = message.GetMultiSpriteState();

			if (message.m_IsRead != m_IsReadIcon.Widget.Visible)
			{
				m_IsReadIcon.Widget.Show(message.m_IsRead, true);
			}
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

		void OnMessageAction(E_MessageAction action)
		{
			if (m_MessageActionDelegate != null)
			{
				m_MessageActionDelegate(m_MessageIndex, action);
			}
		}
	};

	// PRIVATE MEMBERS

	Message[] m_Messages = new Message[0];
	MessageLine[] m_MessageLines = new MessageLine[0];
	int m_ActiveMessage = -1;
	int m_UnreadMessages = 0;
	MessageArea m_MessageArea = new MessageArea();
	GUIBase_List m_MessageList;
	UserGuideAction m_UserGuideAction;
	static int m_ImportantMessage = -1;

	// PUBLIC MEMBERS

	public int ActiveMessage
	{
		get { return m_ActiveMessage; }
		private set
		{
			m_ActiveMessage = Mathf.Clamp(value, -1, m_Messages.Length - 1);

			Message message = m_ActiveMessage >= 0 ? m_Messages[m_ActiveMessage] : null;
			GameCloudManager.mailbox.SetMessageReadState(message, true);
			m_MessageArea.Update(message, m_ActiveMessage);
		}
	}

	public static bool HasImportantMessage
	{
		get { return m_ImportantMessage >= 0; }
	}

	// IGUIOVERLAYSCREEN INTERFACE

	string IGuiOverlayScreen.OverlayButtonTooltip
	{
		get
		{
			if (m_UnreadMessages == 0)
				return null;
			return m_UnreadMessages <= 99 ? m_UnreadMessages.ToString() : "99+";
		}
	}

	bool IGuiOverlayScreen.HideOverlayButton
	{
		get { return Ftue.IsActive; }
	}

	bool IGuiOverlayScreen.HighlightOverlayButton
	{
		get { return m_UnreadMessages > 0 ? true : false; }
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

		m_UserGuideAction = new UserGuideAction_Post();
		UserGuide.RegisterAction(m_UserGuideAction);

		PrepareList(GuiBaseUtils.GetControl<GUIBase_List>(Layout, "MessageList"));

		m_MessageArea.Init(Layout.GetWidget("MessageArea"), OnMessageAction);

		CloudMailbox.inboxChanged += ProcessMessages;
	}

	protected override void OnViewDestroy()
	{
		UserGuide.UnregisterAction(m_UserGuideAction);

		CloudMailbox.inboxChanged -= ProcessMessages;

		base.OnViewDestroy();
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		GameCloudManager.mailbox.FetchMessages();

		GuiBaseUtils.RegisterButtonDelegate(Layout, CLOSE_BUTTON, () => { Owner.Back(); }, null);

		GuiBaseUtils.RegisterButtonDelegate(Layout, FEEDBACK_BUTTON, () => { Owner.DoCommand("FeedbackForm"); }, null);

		m_MessageList.OnUpdateRow += OnUpdateTableRow;
		m_MessageList.OnSelectRow += OnSelectTableRow;

		if (HasImportantMessage == true)
		{
			ActiveMessage = m_ImportantMessage;
			m_ImportantMessage = -1;
		}
	}

	protected override void OnViewHide()
	{
		m_MessageList.OnUpdateRow -= OnUpdateTableRow;
		m_MessageList.OnSelectRow -= OnSelectTableRow;

		GuiBaseUtils.RegisterButtonDelegate(Layout, CLOSE_BUTTON, null, null);
		GuiBaseUtils.RegisterButtonDelegate(Layout, FEEDBACK_BUTTON, null, null);

		base.OnViewHide();
	}

	// HANDLERS

	void OnUpdateTableRow(GUIBase_Widget widget, int rowIndex, int itemIndex)
	{
		MessageLine row = m_MessageLines[rowIndex];

		if (itemIndex < m_Messages.Length)
		{
			Message message = m_Messages[itemIndex];
			bool active = m_ActiveMessage == itemIndex ? true : false;

			row.Show();
			row.Update(message, itemIndex, active);
		}
		else
		{
			row.Hide();
		}
	}

	void OnSelectTableRow(GUIBase_Widget widget, int rowIndex, int itemIndex)
	{
		if (ActiveMessage == itemIndex)
			return;

		ActiveMessage = itemIndex;
	}

	void OnMessageAction(int messageIndex, E_MessageAction action)
	{
		switch (action)
		{
		case E_MessageAction.Reply:
			if (messageIndex >= 0 && messageIndex < m_Messages.Length)
			{
				Owner.ShowPopup("SendMail", m_Messages[messageIndex].m_Sender, null);
			}
			break;
		case E_MessageAction.Remove:
			if (messageIndex >= 0 && messageIndex < m_Messages.Length)
			{
				GameCloudManager.mailbox.RemoveMessage(m_Messages[messageIndex]);
			}
			break;
		default:
			break;
		}
	}

	// PRIVATE METHODS

	void PrepareList(GUIBase_List list)
	{
		m_MessageList = list;

		m_MessageLines = new MessageLine[m_MessageList.numOfLines];
		for (int idx = 0; idx < m_MessageLines.Length; ++idx)
		{
			m_MessageLines[idx] = new MessageLine(m_MessageList.GetWidgetOnLine(idx), OnMessageAction);
		}
	}

	void ProcessMessages(Message[] messages)
	{
		m_Messages = messages;
		m_ImportantMessage = -1;

		System.Array.Sort(m_Messages, (x, y) => { return x.m_SendTime.CompareTo(y.m_SendTime)*-1; });

		int unreadMessage = 0;
		for (int idx = 0; idx < m_Messages.Length; ++idx)
		{
			Message message = m_Messages[idx];
			if (message.m_IsRead == true)
				continue;

			if (message is FriendMessage)
			{
				unreadMessage++;
			}
			else if (message is SystemMessage)
			{
				if (m_ImportantMessage == -1)
				{
					m_ImportantMessage = idx;
				}
				unreadMessage++;
			}
		}
		;

		m_MessageList.MaxItems = m_Messages.Length;
		m_MessageList.Widget.SetModify();

		m_UnreadMessages = unreadMessage;
		ActiveMessage = m_ActiveMessage;
	}
}
