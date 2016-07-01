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
using FriendInfo = FriendList.FriendInfo;
using LitJson;

// =============================================================================================================================
// =============================================================================================================================
public class FriendListView : BaseListView
{
	enum E_FriendAction
	{
		Select,
		ShowStats,
		SendMail,
		Chat,
		Remove
	}

	delegate void OnFriendActionDelegate(string inFriendName, E_FriendAction action);

	class FriendLine
	{
		// ---------------------------------------------------------------------------------------------------------------------
		// last online info messages...
		const int LOI_UNKNOWN = 02040221; //	UNKNOWN
		const int LOI_NOW = 02040222; //	NOW
		const int LOI_PLAYING = 02040237; //	PLAYING

		// ---------------------------------------------------------------------------------------------------------------------				
		OnFriendActionDelegate m_OnFriendActionDelegate;

		// ---------------------------------------------------------------------------------------------------------------------	
		string m_FriendName;

		GUIBase_Widget m_Line;

		GUIBase_Label m_Username;
		GUIBase_Label m_Nickname;
		GUIBase_Label m_RankText;
		GUIBase_MultiSprite m_RankIcon;
		GUIBase_Label m_Missions;
		GUIBase_Label m_Online;
		GUIBase_Button m_Stats;
		GUIBase_Button m_Mail;
		GUIBase_Button m_Chat;
		GUIBase_Button m_Remove;

		GUIBase_Label m_FacebookName;
		GUIBase_Widget m_FacebookIcon;

		// ---------------------------------------------------------------------------------------------------------------------							
		public Vector3 spritePos
		{
			get { return m_Line.transform.position; }
		}

		public FriendLine(GUIBase_Widget inLine, OnFriendActionDelegate inFriendAction) : this(inLine)
		{
			m_OnFriendActionDelegate = inFriendAction;

			GUIBase_Button button = inLine.GetComponent<GUIBase_Button>();
			if (button != null)
			{
				button.RegisterTouchDelegate2(Delegate_OnFriendSelect);
			}
		}

		public FriendLine(GUIBase_Widget inLine)
		{
			Transform trans = inLine.transform;

			m_Line = inLine;
			m_Nickname = trans.GetChildComponent<GUIBase_Label>("Username"); // display nickaname first
			m_Username = trans.GetChildComponent<GUIBase_Label>("Nickname"); // display nickaname second
			m_RankText = trans.GetChildComponent<GUIBase_Label>("TextRank");
			m_RankIcon = trans.GetChildComponent<GUIBase_MultiSprite>("PlayerRankPic");
			m_Missions = trans.GetChildComponent<GUIBase_Label>("Missions");
			m_Online = trans.GetChildComponent<GUIBase_Label>("OnlineStatus");
			m_Stats = trans.GetChildComponent<GUIBase_Button>("Stats_Button");
			m_Mail = trans.GetChildComponent<GUIBase_Button>("SendMail_Button");
			m_Chat = trans.GetChildComponent<GUIBase_Button>("Chat_Button");
			m_Remove = trans.GetChildComponent<GUIBase_Button>("Remove_Button");
			m_FacebookName = trans.GetChildComponent<GUIBase_Label>("FacebookName");
			m_FacebookIcon = trans.GetChildComponent<GUIBase_Widget>("FacebookIcon");

			m_Stats.IsDisabled = GuiFrontendIngame.IsVisible;
			m_Mail.IsDisabled = GuiFrontendIngame.IsVisible;
			m_Chat.IsDisabled = true;

			m_Stats.RegisterTouchDelegate(Delegate_Stats);
			m_Mail.RegisterTouchDelegate(Delegate_SendMail);
			m_Chat.RegisterTouchDelegate(Delegate_Chat);
			m_Remove.RegisterTouchDelegate(Delegate_Remove);
		}

		public void Update(FriendInfo inFriend)
		{
			m_FriendName = inFriend.PrimaryKey;

			{
				m_FacebookIcon.Show(false, false);
				m_FacebookName.Widget.Show(false, false);
				m_Nickname.Widget.Show(true, false);

				m_Username.SetNewText(GuiBaseUtils.FixNameForGui(inFriend.Username));
				m_Nickname.SetNewText(GuiBaseUtils.FixNameForGui(inFriend.Nickname));
			}

			m_RankText.SetNewText(inFriend.Rank <= 0 ? "" : inFriend.Rank.ToString());
			m_Missions.SetNewText(inFriend.Missions.ToString());

			string rankState = string.Format("Rank_{0}", Mathf.Min(inFriend.Rank, m_RankIcon.Count - 1).ToString("D2"));
			m_RankIcon.State = inFriend.Rank <= 0 ? GUIBase_MultiSprite.DefaultState : rankState;

			string onlineStatus = GetLastOnlineInfo(inFriend);
			m_Online.SetNewText(onlineStatus);

			bool isOnline = inFriend.OnlineStatus == FriendList.E_OnlineStatus.InLobby && LobbyClient.IsConnected == true &&
							GuiFrontendMain.IsVisible == true;
			m_Chat.IsDisabled = GuiFrontendIngame.IsVisible ? true : !isOnline;

			//Debug.Log("show PPI " + inPPI.Name + " " + inPPI.Score.Score.ToString());
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

		string GetLastOnlineInfo(FriendInfo inFriend)
		{
			switch (inFriend.OnlineStatus)
			{
			case FriendList.E_OnlineStatus.InLobby:
				return TextDatabase.instance[LOI_NOW];
			case FriendList.E_OnlineStatus.InGame:
				return TextDatabase.instance[LOI_PLAYING];
			default:
				break;
			}

			double lastOnlineDate = inFriend.PPIData.Stats.GetLastPlayedDate();
			if (inFriend.LastOnlineDate > lastOnlineDate)
			{
				lastOnlineDate = inFriend.LastOnlineDate;
			}

			return lastOnlineDate <= 0 ? TextDatabase.instance[LOI_UNKNOWN] : GuiBaseUtils.EpochToString(lastOnlineDate);
		}

		void OnFriendAction(string inFriendName, E_FriendAction inAction)
		{
			if (m_OnFriendActionDelegate != null)
			{
				m_OnFriendActionDelegate(inFriendName, inAction);
			}
		}

		void Delegate_OnFriendSelect(GUIBase_Widget inInstigator)
		{
			OnFriendAction(m_FriendName, FriendListView.E_FriendAction.Select);
		}

		void Delegate_Stats()
		{
			OnFriendAction(m_FriendName, FriendListView.E_FriendAction.ShowStats);
		}

		void Delegate_SendMail()
		{
			OnFriendAction(m_FriendName, FriendListView.E_FriendAction.SendMail);
		}

		void Delegate_Chat()
		{
			OnFriendAction(m_FriendName, FriendListView.E_FriendAction.Chat);
		}

		void Delegate_Remove()
		{
			OnFriendAction(m_FriendName, FriendListView.E_FriendAction.Remove);
		}
	};

	// -------------------------------------------------------------------------------------------------------------------------
	FriendLine[] m_GuiLines;
	FriendInfo[] m_Friends = new FriendInfo[0];
	GUIBase_List m_Table;
	GUIBase_Layout m_View;
	GuiScreen m_Owner;
	bool m_IsDirty;

	// =========================================================================================================================
	// === public interface ====================================================================================================
	public void GUIView_Init(GuiScreen owner, GUIBase_Layout inView, GUIBase_List inList)
	{
		m_Owner = owner;
		m_View = inView;
		m_Table = GuiBaseUtils.GetControl<GUIBase_List>(m_View, "Table");

		InitGuiLines(inList);
	}

	public override void GUIView_Show()
	{
		if (m_View.Visible == true)
			return;

		m_IsDirty = true;

		m_Table.OnUpdateRow += OnUpdateTableRow;

		MFGuiManager.Instance.ShowLayout(m_View, true);

		// register friendListChange calleck.
		GameCloudManager.friendList.FriendListChanged += OnFriendListChanged;

		UpdateView();
	}

	public override void GUIView_Hide()
	{
		if (m_View.Visible == false)
			return;

		m_Table.OnUpdateRow -= OnUpdateTableRow;

		MFGuiManager.Instance.ShowLayout(m_View, false);

		// unregister friendListChange calleck.
		GameCloudManager.friendList.FriendListChanged -= OnFriendListChanged;
	}

	public override void GUIView_Update()
	{
		if (m_IsDirty == false)
			return;

		UpdateView();
		m_IsDirty = false;
	}

	public override GUIBase_Widget GUIView_HitTest(ref Vector2 point)
	{
		GUIBase_Widget widget = base.GUIView_HitTest(ref point);
		if (widget != null)
			return widget;

		return m_View != null ? m_View.HitTest(ref point) : null;
	}

	public override bool GUIView_ProcessInput(ref IInputEvent evt)
	{
		if (base.GUIView_ProcessInput(ref evt) == true)
			return true;

		return m_View != null ? m_View.ProcessInput(ref evt) : false;
	}

	// =========================================================================================================================
	// === MonoBehaviour interface =============================================================================================

	// !!!!!! DON"T USE Standard MonoBehaviour Update functions. This view is controled from parrent object...

	// =========================================================================================================================
	// === private part ========================================================================================================
	void InitGuiLines(GUIBase_List inParent)
	{
		if (inParent.numOfLines <= 0)
		{
			Debug.LogError("inParent.numOfLines = " + inParent.numOfLines);
			return;
		}

		m_GuiLines = new FriendLine[inParent.numOfLines];
		for (int i = 0; i < inParent.numOfLines; i++)
		{
			m_GuiLines[i] = new FriendLine(inParent.GetWidgetOnLine(i), Delegate_OnFriendAction);
		}
	}

	void OnUpdateTableRow(GUIBase_Widget widget, int rowIndex, int itemIndex)
	{
		FriendLine row = m_GuiLines[rowIndex];

		if (itemIndex < m_Friends.Length)
		{
			row.Show();
			row.Update(m_Friends[itemIndex]);
		}
		else
		{
			row.Hide();
		}
	}

	int GetOnlineStatusPriority(FriendList.E_OnlineStatus status)
	{
		if (status == FriendList.E_OnlineStatus.InLobby)
			return 2;
		if (status == FriendList.E_OnlineStatus.InGame)
			return 1;
		return 0;
	}

	void UpdateView()
	{
		List<FriendInfo> allFriends = GameCloudManager.friendList.friends;

		m_Friends = allFriends.ToArray();
		System.Array.Sort(m_Friends,
						  (x, y) =>
						  {
							  int res = GetOnlineStatusPriority(y.OnlineStatus).CompareTo(GetOnlineStatusPriority(x.OnlineStatus));
							  return res == 0 ? string.Compare(x.Nickname, y.Nickname) : res;
						  });

		m_Table.MaxItems = m_Friends.Length;
		m_Table.Widget.SetModify();
	}

	void OnFriendListChanged(object sender, System.EventArgs e)
	{
		m_IsDirty = true;
	}

	// =========================================================================================================================
	// === internal gui delegates ==============================================================================================
	void Delegate_OnFriendAction(string inFriendName, E_FriendAction inAction)
	{
		switch (inAction)
		{
		case E_FriendAction.Select:
			break;
		case E_FriendAction.ShowStats:
		{
			PlayerPersistantInfo ppi = null;
			List<PlayerPersistantInfo> ppis = new List<PlayerPersistantInfo>();
			foreach (var friend in m_Friends)
			{
				PlayerPersistantInfo temp = new PlayerPersistantInfo();
				temp.Name = friend.Nickname;
				temp.PrimaryKey = friend.PrimaryKey;
				temp.InitPlayerDataFromStr(JsonMapper.ToJson(friend.PPIData));

				ppis.Add(temp);

				if (friend.PrimaryKey == inFriendName)
				{
					ppi = temp;
				}
			}

			if (ppi != null)
			{
				GuiScreenPlayerStats.UserPPIs = ppis.ToArray();
				GuiScreenPlayerStats.UserPPI = ppi;
				m_Owner.Owner.ShowScreen("PlayerStats");
			}
		}
			break;
		case E_FriendAction.SendMail:
			m_Owner.Owner.ShowPopup("SendMail", inFriendName, null);
			break;
		case E_FriendAction.Chat:
			if (GuiScreenChatFriends.StartChat(inFriendName) == true)
			{
				m_Owner.Owner.ShowScreen("Chat:1", true);
			}
			break;
		case E_FriendAction.Remove:
		{
			string text = string.Format(TextDatabase.instance[02040235], GuiBaseUtils.FixNameForGui(inFriendName));
			m_Owner.Owner.ShowPopup("ConfirmDialog",
									TextDatabase.instance[02040234],
									text,
									(inPopup, inResult) =>
									{
										if (inResult == E_PopupResultCode.Ok)
										{
											GameCloudManager.friendList.RemoveFriend(inFriendName);
										}
									});
		}
			break;
		default:
			break;
		}
	}
}
