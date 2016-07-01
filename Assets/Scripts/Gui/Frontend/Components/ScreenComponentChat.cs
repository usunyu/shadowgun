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
using DateTime = System.DateTime;
using Regex = System.Text.RegularExpressions.Regex;
using ButtonDelegate = GUIBase_Button.TouchDelegate3;

public class ScreenComponentChat : ScreenComponent, Chat.IListener
{
	public readonly static int MAX_MESSAGES = 99;
	readonly static int MAX_MESSAGE_LENGTH = 150;
	readonly static int MAX_ROWS = 5;
	readonly static float UPDATE_INTERVAL = 0.2f;

	readonly static string CHAT_COMPONENT = "Chat_Component";
	readonly static string ROWS_CONTAINER = "Rows_Container";
	readonly static string SEND_BUTTON = "Send_Button";
	readonly static string MESSAGE_BUTTON = "Message_Button";
	readonly static string CLEAR_BUTTON = "Clear_Button";
	readonly static string TEMPLATE_WIDGET = "Template_Widget";

	delegate void RowButtonDelegate(string rowId, GUIBase_Widget widget, object evt);

	class Row
	{
		GUIBase_Widget m_Root;
		GUIBase_Widget m_ContentHolder;
		//GUIBase_Widget      m_ButtonsHolder;
		GUIBase_Label m_Name;
		GUIBase_TextArea m_Text;
		GUIBase_MultiSprite m_RankIcon;
		GUIBase_Label m_RankText;
		GUIBase_Widget m_HighlightBkg;
		bool m_IsSelected = false;
		Rect m_Rect = new Rect();
		GUIBase_Button[] m_Buttons = new GUIBase_Button[0];
		RowButtonDelegate m_ButtonDelegate;
		GUIBase_Button m_HoverButton = null; //for simulating mouse events

		public string Id { get; private set; }

		public bool IsVisible
		{
			get { return m_ContentHolder != null ? m_ContentHolder.Visible : m_Root.Visible; }
			set
			{
				if (m_ContentHolder != null)
				{
					m_ContentHolder.ShowImmediate(value, true);
				}
				else
				{
					m_Root.ShowImmediate(value, true);
				}
				if (m_HighlightBkg != null)
				{
					m_HighlightBkg.ShowImmediate(value && m_IsSelected, true);
				}
				foreach (var button in m_Buttons)
				{
					button.Widget.Show(value && m_IsSelected, true);
					button.IsDisabled = IsSelectable ? false : true;
				}
			}
		}

		public bool IsSelectable;

		public bool IsSelected
		{
			get { return m_IsSelected; }
			set
			{
				m_IsSelected = value;
				IsVisible = IsVisible;
			}
		}

		public int Width
		{
			get { return (int)m_Rect.width; }
		}

		public int Height
		{
			get { return (int)m_Rect.height; }
		}

		public Vector3 Position
		{
			get { return m_Root.transform.position; }
			set
			{
				m_Root.transform.position = value;
				m_Root.SetModify(true);
				m_Rect.x = value.x - m_Rect.width*0.5f;
				m_Rect.y = value.y;
			}
		}

		public bool IsMouseOver(ref Vector3 point)
		{
			return m_Rect.Contains(point);
		}

		public void Initialize(GUIBase_Widget root, RowButtonDelegate dlgt)
		{
			m_Root = root;
			m_ButtonDelegate = dlgt;

			CollectControls(m_Root.transform);
		}

		public void Destroy()
		{
			foreach (var button in m_Buttons)
			{
				if (button != null)
				{
					button.RegisterReleaseDelegate3(null);
				}
			}
			GameObject.Destroy(m_Root.gameObject);
		}

		public void Update(Chat.Message message)
		{
			Id = message.Id;

			if (m_Name != null)
			{
				m_Name.SetNewText(GuiBaseUtils.FixNameForGui(message.Nickname));
			}

			if (m_Text != null)
			{
				m_Text.SetNewText(m_Name == null ? string.Format("{0}: {1}", GuiBaseUtils.FixNameForGui(message.Nickname), message.Text) : message.Text);

				Transform trans = m_Text.transform;
				Vector3 scale = trans.localScale;
				m_Rect.width = Mathf.RoundToInt(m_Text.textSize.x*scale.x);
				m_Rect.height = Mathf.RoundToInt(m_Text.textSize.y*scale.y);
			}

			if (m_RankIcon != null)
			{
				m_RankIcon.State = string.Format("Rank_{0}", Mathf.Min(message.Rank, m_RankIcon.Count - 1).ToString("D2"));
			}

			if (m_RankText != null)
			{
				m_RankText.SetNewText(message.Rank.ToString());
			}

			if (m_HighlightBkg != null)
			{
				Vector2 pos = m_HighlightBkg.GetOrigPos();
				Transform trans = m_HighlightBkg.transform;
				Vector3 scale = trans.lossyScale;
				int offset = Mathf.RoundToInt(pos.y - m_Root.GetOrigPos().y);
				int width = Mathf.RoundToInt(m_HighlightBkg.GetWidth());
				int height = (int)m_Rect.height + 4;

				offset -= offset - Mathf.RoundToInt(m_HighlightBkg.GetHeight()*0.5f*scale.y);
				pos.y = pos.y - offset + height*0.5f*scale.y;

				m_HighlightBkg.UpdateSpritePosAndSize(0, pos.x, pos.y, width, height);

				m_Rect.width = Mathf.RoundToInt(width*scale.x);
				//m_Rect.height = Mathf.RoundToInt(height * scale.y);
			}

			foreach (var button in m_Buttons)
			{
				Transform trans = button.transform;
				Vector3 pos = trans.localPosition;

				pos.y = m_Rect.height*0.5f;
				trans.localPosition = pos;
			}

			m_HoverButton = null;
		}

		void CollectControls(Transform trans)
		{
			foreach (Transform child in trans)
			{
				switch (child.name)
				{
				case "Name":
					m_Name = child.GetComponent<GUIBase_Label>();
					break;
				case "Text":
					m_Text = child.GetComponent<GUIBase_TextArea>();
					break;
				case "PlayerRankPic":
					m_RankIcon = child.GetComponent<GUIBase_MultiSprite>();
					break;
				case "TextRank":
					m_RankText = child.GetComponent<GUIBase_Label>();
					break;
				case "HighlightBackground":
					m_HighlightBkg = child.GetComponent<GUIBase_Widget>();
					break;
				case "Buttons":
					//m_ButtonsHolder = child.GetComponent<GUIBase_Widget>();
					CollectButtons(child);
					break;
				case "Content":
					m_ContentHolder = child.GetComponent<GUIBase_Widget>();
					CollectControls(child);
					break;
				default:
					break;
				}
			}
		}

		void CollectButtons(Transform trans)
		{
			var buttons = new List<GUIBase_Button>();
			foreach (Transform child in trans)
			{
				GUIBase_Button button = child.GetComponent<GUIBase_Button>();
				if (button != null)
				{
					button.RegisterReleaseDelegate3((widget, evt) =>
													{
														if (m_ButtonDelegate != null)
														{
															m_ButtonDelegate(Id, widget, evt);
														}
													});
					buttons.Add(button);
				}
			}
			m_Buttons = buttons.ToArray();
		}

		// when scrollbar is present, it catches all of the mouse/touch callbacks
		// following two methods will simulate the necessary callbacks
		public void ProcessMouseMovement(MouseEvent mouseEvent)
		{
			if (m_Buttons == null)
				return;

			if (m_HoverButton != null)
			{
				if (m_HoverButton.Widget.IsMouseOver(mouseEvent.Position))
					return;
				else
				{
					m_HoverButton.Callback(GUIBase_Callback.E_CallbackType.E_CT_ON_MOUSEOVER_END, mouseEvent);
					m_HoverButton = null;
				}
			}

			foreach (GUIBase_Button btn in m_Buttons)
			{
				if (btn.Widget.IsMouseOver(mouseEvent.Position))
				{
					m_HoverButton = btn;
					m_HoverButton.Callback(GUIBase_Callback.E_CallbackType.E_CT_ON_MOUSEOVER_BEGIN, mouseEvent);
					return;
				}
			}
		}

		public bool ProcessTouchEnd(TouchEvent touch)
		{
			if (m_Buttons == null)
				return false;
			foreach (GUIBase_Button btn in m_Buttons)
			{
				if (btn.Widget.IsMouseOver(touch.Position))
				{
					if (m_ButtonDelegate != null)
					{
						m_ButtonDelegate(Id, btn.Widget, touch);
					}
					return true;
				}
			}
			return false;
		}
	}

	public delegate void MessageReceivedDelegate(Chat.Message message);
	public delegate bool IsMessageSelectableDelegate(Chat.Message message);
	public delegate void MessageActionDelegate(Chat.Message message, GUIBase_Widget instigator);

	// PRIVATE MEMBERS

	int m_MaxRows = MAX_ROWS;
	List<Row> m_Rows = new List<Row>();
	CircularBuffer<Chat.Message> m_Messages = new CircularBuffer<Chat.Message>(MAX_MESSAGES);
	int m_UnseenMessages = 0;
	string m_MessageToSend = "";
	string m_Channel = null;
	bool m_CanSendMessage = true;
	int m_TouchId = -1;
	string m_SelectedMessageId;
	bool m_IsDirty = true;
	GUIBase_Widget m_RowsContainer;
	GUIBase_Button m_SendButton;
	GUIBase_Button m_MessageButton;
	GUIBase_Button m_ClearButton;
	GUIBase_List m_ScrollingTable;
	int m_ScrollingOffset = 0;

	// PUBLIC MEMBERS

	public override string ParentName
	{
		get { return CHAT_COMPONENT; }
	}

	public override float UpdateInterval
	{
		get { return UPDATE_INTERVAL; }
	}

	public MessageReceivedDelegate OnMessageReceived;
	public MessageActionDelegate OnMessageAction;
	public IsMessageSelectableDelegate OnIsMessageSelectable;

	public bool CanSendMessage
	{
		get { return m_CanSendMessage; }
		set { m_CanSendMessage = value; }
	}

	public string Channel
	{
		get { return m_Channel; }
		set
		{
			if (m_Channel == value)
				return;

			if (string.IsNullOrEmpty(m_Channel) == false)
			{
				Chat.Unregister(m_Channel, this);
			}

			m_Channel = value;

			if (string.IsNullOrEmpty(m_Channel) == false)
			{
				Chat.Register(m_Channel, this);
			}
		}
	}

	public int MaxRows
	{
		get { return m_MaxRows; }
		set
		{
			if (m_MaxRows == value)
				return;
			m_MaxRows = value;

			PrepareRows();
			Refresh();
		}
	}

	public Chat.Message[] Messages
	{
		get { return m_Messages.ToArray(); }
		set
		{
			m_Messages.Clear();

			if (value != null)
			{
				foreach (var message in value)
				{
					m_Messages.Enqueue(message);
				}
			}

			Refresh();
		}
	}

	// PUBLIC METHODS

	public void AddMessage(Chat.Message message)
	{
		m_Messages.Enqueue(message);

		if (m_ScrollingTable != null && m_ScrollingOffset != 0) //if the list is scrolled, this will prevent from another movement,
			m_ScrollingTable.ChildButtonPressed(-1); //so the user can read old messages

		m_UnseenMessages++;

		if (OnMessageReceived != null)
		{
			OnMessageReceived(message);
		}
	}

	public void Clear()
	{
		SelectRow(null);

#if UNITY_EDITOR
		m_NextFakeIndex = 0;
#endif

		m_Messages.Clear();
		m_TouchId = -1;

		Refresh();
	}

	public void Refresh()
	{
		m_IsDirty = true;
	}

	public IEnumerator FakeMessages_Coroutine()
	{
#if UNITY_EDITOR
		while (IsVisible == true)
		{
			SendFakeMessage();

			yield return new WaitForSeconds(Random.Range(0.05f, 1.0f));
		}
#else
		yield break;
#endif
	}

	// GUICOMPONENT INTERFACE

	protected override bool OnInit()
	{
		if (base.OnInit() == false)
			return false;

		// get controls
		m_RowsContainer = Owner.Layout.GetWidget(ROWS_CONTAINER);
		m_SendButton = GuiBaseUtils.GetControl<GUIBase_Button>(Owner.Layout, SEND_BUTTON, false);
		m_MessageButton = GuiBaseUtils.GetControl<GUIBase_Button>(Owner.Layout, MESSAGE_BUTTON, false);
		m_ClearButton = GuiBaseUtils.GetControl<GUIBase_Button>(Owner.Layout, CLEAR_BUTTON, false);
		m_ScrollingTable = GuiBaseUtils.GetControl<GUIBase_List>(Owner.Layout, "Table", false);
		if (m_ScrollingTable != null)
		{
			m_ScrollingTable.numOfLines = MAX_ROWS;
			m_ScrollingTable.MaxItems = MAX_MESSAGES;
			m_ScrollingTable.Widget.SetModify();
		}

		// prepare rows
		PrepareRows();

		Owner.AddTextField(m_MessageButton, Delegate_OnKeyboardClose, null, MAX_MESSAGE_LENGTH);

		// done
		return true;
	}

	protected override void OnDestroy()
	{
		// unregister channel from chat
		Channel = null;

		base.OnDestroy();
	}

	protected override void OnShow()
	{
		base.OnShow();

		if (m_SendButton != null)
		{
			m_SendButton.RegisterTouchDelegate(() =>
											   {
												   if (!string.IsNullOrEmpty(m_MessageToSend))
													   SendMessage(
																   CloudUser.instance.primaryKey,
																   CloudUser.instance.nickName,
																   PPIManager.Instance.GetLocalPPI().Rank,
																   m_MessageToSend);
												   m_MessageToSend = "";
												   m_MessageButton.SetNewText("");
												   m_MessageButton.TextFieldText = "";
											   });
		}

		if (m_ClearButton != null)
		{
			m_ClearButton.RegisterTouchDelegate(() => { Clear(); });
		}

		if (m_MessageButton != null)
		{
			m_MessageButton.RegisterTouchDelegate(() => { WriteMessage(); });
		}

		if (m_ScrollingTable != null)
		{
			m_ScrollingTable.OnUpdateRow += OnUpdateTableRow;
			m_ScrollingTable.OnProcessInput += OnProcessInput;
		}

		RefreshRows();
	}

	protected override void OnHide()
	{
		if (m_SendButton != null)
		{
			m_SendButton.RegisterTouchDelegate(null);
		}

		if (m_ClearButton != null)
		{
			m_ClearButton.RegisterTouchDelegate(null);
		}

		if (m_ScrollingTable != null)
		{
			m_ScrollingTable.OnUpdateRow -= OnUpdateTableRow;
			m_ScrollingTable.OnProcessInput -= OnProcessInput;
		}

		base.OnHide();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		UpdateChat();
	}

	protected override bool OnProcessInput(ref IInputEvent evt)
	{
		if (base.OnProcessInput(ref evt) == true)
			return true;

		if (evt.Kind == E_EventKind.Touch)
		{
			TouchEvent touch = (TouchEvent)evt;
			return ForwardInputToRows(ref touch);
		}

		if (evt.Kind == E_EventKind.Mouse && m_ScrollingTable != null)
						//when a scrollbar is present, it catches all of the mouse callbacks, so we have to simulate them
		{
			MouseEvent mouseEvent = (MouseEvent)evt;
			foreach (Row row in m_Rows)
			{
				if (row.IsSelected && row.IsVisible)
				{
					mouseEvent.Position.y = Screen.height - mouseEvent.Position.y;
					row.ProcessMouseMovement(mouseEvent);
					break;
				}
			}
		}

		return false;
	}

	// HANDLERS

	void OnRowButtonDelegate(string rowId, GUIBase_Widget widget, object evt)
	{
		if (OnMessageAction == null)
			return;

		Chat.Message[] messages = m_Messages.ToArray();
		Chat.Message message = System.Array.Find(messages, obj => obj.Id == rowId);

		if (message.Id == rowId)
		{
			OnMessageAction(message, widget);
		}
	}

	// PRIVATE METHODS

	bool ForwardInputToRows(ref TouchEvent touch)
	{
		Vector3 point = touch.Position;
		point.y = Screen.height - point.y;

		bool hover = m_RowsContainer.IsMouseOver(point);
		Row hoverRow = hover ? HitTest(point) : null;
		bool refresh = false;

		switch (touch.Phase)
		{
		case TouchPhase.Began:
			if (m_TouchId == -1)
			{
				refresh = SelectRow(hoverRow);
				m_TouchId = touch.Id;
			}
			break;
		case TouchPhase.Canceled:
		case TouchPhase.Ended:
			if (m_TouchId == touch.Id)
			{
				if (m_TouchId == -1) //this is faked event from scrollbar (user clicked but didn't dragged)
								//we have to simulate the touch callbacks
				{
					bool buttonPressed = false;
					foreach (Row row in m_Rows)
					{
						if (row.IsSelected && row.IsVisible)
						{
							touch.Position = point;
							buttonPressed = row.ProcessTouchEnd(touch);
							break;
						}
					}
					if (!buttonPressed)
						refresh = SelectRow(hoverRow);
				}
				m_TouchId = -1;
			}
			break;
		default:
			if (m_TouchId == touch.Id)
			{
				refresh = SelectRow(hoverRow);
			}
			break;
		}

		if (refresh == true)
		{
			RefreshRows();
		}

		return refresh;
	}

	bool SelectRow(Row row)
	{
		if (row != null)
			m_SelectedMessageId = row.Id;

		return true;
	}

	Row HitTest(Vector3 point)
	{
		for (int idx = 0; idx < m_MaxRows; ++idx)
		{
			Row row = m_Rows[idx];
			if (row.IsVisible == false)
				continue;
			if (row.IsMouseOver(ref point) == false)
				continue;

			return row;
		}

		return null;
	}

	void UpdateChat()
	{
		if (m_IsDirty == true || m_UnseenMessages > 0 && m_TouchId == -1)
		{
			RefreshRows();
		}

		if (m_SendButton != null)
		{
			bool disabled = LobbyClient.IsConnected && m_CanSendMessage && string.IsNullOrEmpty(m_Channel) == false ? false : true;
			m_SendButton.SetDisabled(disabled);
			if (m_MessageButton != null)
				m_MessageButton.SetDisabled(disabled);
		}

		if (m_ClearButton != null)
		{
			m_ClearButton.SetDisabled(m_Messages.Count > 0 ? false : true);
		}

		m_IsDirty = false;
	}

	void Delegate_OnKeyboardClose(GUIBase_Button input, string text, bool cancelled)
	{
		m_MessageToSend = "";

		if (string.IsNullOrEmpty(text) == false && Regex.IsMatch(text, @"\S+") == true)
		{
			m_MessageToSend = text.RemoveDiacritics();
		}
		m_MessageButton.SetNewText(m_MessageToSend);
	}

	void WriteMessage()
	{
		GuiView view = Owner as GuiView;
		if (view == null)
			return;
		if (m_SendButton == null)
			return;

#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID)
		view.ShowKeyboard(m_MessageButton, GuiScreen.E_KeyBoardMode.Default, Delegate_OnKeyboardClose, m_MessageToSend, MAX_MESSAGE_LENGTH);
#endif
	}

	void SendMessage(string primaryKey, string nickname, int rank, string text)
	{
		Chat.SendMessage(m_Channel, Chat.Message.Create(primaryKey, nickname, rank, text));
	}

#if UNITY_EDITOR
	int m_NextFakeIndex = 0;

	void SendFakeMessage()
	{
		string loremIpsum =
						"Lorem ipsum dolor sit amet, consectetur adipiscing elit. In viverra sapien vitae sem tristique bibendum. Suspendisse ut nibh tellus. Nullam non mattis est. Nulla facilisis luctus elit, viverra congue dolor ullamcorper eget. Suspendisse potenti. Duis et leo magna. Nam mollis.";
		//string loremIpsum = "Shit Lorem Piss ipsum Fuck dolor Cuntsit amet, Cocksucker consectetur adipiscing elit Motherfucker Tits. Suspendisse ut nibh tellus.";
		int length = loremIpsum.IndexOf(" ", Random.Range(10, loremIpsum.Length));
		if (length < 0)
			length = loremIpsum.Length;
		SendMessage(
				    (CloudUser.instance.nickName + "_" + m_NextFakeIndex).ToLower(),
					CloudUser.instance.nickName + "_" + m_NextFakeIndex,
					Mathf.Max(1, m_NextFakeIndex%PlayerPersistantInfo.MAX_RANK),
					loremIpsum.Substring(0, length)
						);
		m_NextFakeIndex++;
	}
#endif

	void Chat.IListener.ReceiveMessage(string channel, Chat.Message message)
	{
		AddMessage(message);
	}

	void PrepareRows()
	{
		GUIBase_Widget template = Owner.Layout.GetWidget(TEMPLATE_WIDGET);

		while (m_Rows.Count > m_MaxRows)
		{
			int idx = m_Rows.Count - 1;
			Row row = m_Rows[idx];
			m_Rows.RemoveAt(idx);

			row.Destroy();
		}

		while (m_Rows.Count < m_MaxRows)
		{
			GUIBase_Widget root = GameObject.Instantiate(template) as GUIBase_Widget;

			root.Relink(m_RowsContainer);
			root.name = string.Format("Row{0}_Widget", m_Rows.Count.ToString("D2"));

			Row row = new Row();
			row.Initialize(root, OnRowButtonDelegate);
			m_Rows.Add(row);
		}
	}

	void RefreshRows()
	{
		if (IsVisible == false)
			return;
		if (Parent == null)
			return;

		Transform trans = m_RowsContainer.transform;
		Vector3 pos = trans.position;
		Vector3 scale = trans.lossyScale;

		float chatHeight = scale.y*m_RowsContainer.GetHeight();
		pos.y += chatHeight/2; //bottom row

		float rowMinY = pos.y - chatHeight; //upper row

		// get messages from buffer
		Chat.Message[] messages = m_Messages.ToArray();
		System.Array.Reverse(messages);

		int visibleRows = 0;

		for (int idx = 0; idx < m_MaxRows; ++idx)
		{
			int idxScrolled = idx + m_ScrollingOffset;
			bool isVisible = idxScrolled < messages.Length && pos.y >= rowMinY ? IsVisible : false;

			Row row = m_Rows[idx];

			if (isVisible == true)
			{
				Chat.Message message = messages[idxScrolled];

				row.IsSelectable = IsRowSelectable(message);
				row.IsVisible = isVisible;
				row.Update(message);

				pos.y -= 5.0f;
				pos.y -= row.Height*scale.y;
				pos.y = Mathf.RoundToInt(pos.y);
				row.Position = pos;
				row.IsSelected = m_SelectedMessageId != null && row.Id == m_SelectedMessageId;

				if (pos.y < rowMinY)
				{
					isVisible = false;
				}
				else
					visibleRows++;
			}

			if (isVisible == false)
			{
				row.IsSelectable = false;
				row.IsSelected = false;
				row.IsVisible = false;
			}
		}

		m_UnseenMessages = 0;

		if (m_ScrollingTable != null)
		{
			if (visibleRows <= 0)
				visibleRows = m_MaxRows;

			m_ScrollingTable.numOfLines = visibleRows;
			m_ScrollingTable.MaxItems = messages.Length;
			m_ScrollingTable.Widget.SetModify();
		}
	}

	bool IsRowSelectable(Chat.Message message)
	{
		// filter out local user
		if (message.PrimaryKey == CloudUser.instance.primaryKey)
			return false;

		// filter out system messages
		// system messages has no primaryKey specified, just nickname
		if (string.IsNullOrEmpty(message.PrimaryKey) == true)
			return false;

		// let someone else decide
		if (OnIsMessageSelectable != null)
			return OnIsMessageSelectable(message);

		return false;
	}

	void OnUpdateTableRow(GUIBase_Widget widget, int rowIndex, int itemIndex)
	{
		int offset = itemIndex - rowIndex;
		if (m_ScrollingOffset != offset)
		{
			m_ScrollingOffset = offset;
			RefreshRows();
		}
	}
}
