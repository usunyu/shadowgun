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
using LitJson;

public class GuiPopupLobbyGangGuest : GuiPopup
{
	readonly static string CAPTION_LABEL = "Caption_Label";
	readonly static string READY_BUTTON = "Ready_Button";
	readonly static string CLOSE_BUTTON = "Close_Button";
	readonly static string GAMETYPE_ROLLER = "Gametype_Roller";
	readonly static string FRIENDS_LIST = "Friends_List";
	readonly static string LISTITEM_NAME = "Name";
	readonly static string LISTITEM_STATUS = "Status";
	readonly static string LISTITEM_RANKVALUE = "TextRank";
	readonly static string LISTITEM_RANKICON = "PlayerRankPic";
	readonly static string LISTITEM_WAITINGBG = "WaitingBackground";
	readonly static string LISTITEM_READYBG = "ReadyBackground";

	class FriendInfo
	{
		public string PrimaryKey;
		public string Nickname;
		public int Rank;
		public bool IsReady;
	}

	class ListRow
	{
		public GUIBase_Widget Root;
		public GUIBase_Label Name;
		public GUIBase_Label Status;
		public GUIBase_Label RankValue;
		public GUIBase_MultiSprite RankIcon;
		public GUIBase_Sprite WaitingBg;
		public GUIBase_Sprite ReadyBg;
		public int FriendIndex;
	}

	// PRIVATE MEMBERS

	GUIBase_Label m_CaptionLabel;
	GUIBase_Button m_ReadyButton;
	GUIBase_Roller m_GametypeRoller;
	GUIBase_List m_FriendList;
	ScreenComponentChat m_Chat;
	string m_Master = null;
	string m_PrimaryKey = "";
	bool m_IsDirty = true;
	bool m_IsReady = false;
	bool m_IsJoining = false;
	E_MPGameType m_Gametype = E_MPGameType.DeathMatch;
	ListRow[] m_Rows = new ListRow[0];
	FriendInfo[] m_Friends = new FriendInfo[0];
	GuiPopupMessageBox m_MessageBox = null;

	// GUIPOPUP

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

		CloudUser.authenticationChanged += OnAuthenticationChanged;

		m_Chat = RegisterComponent<ScreenComponentChat>();
	}

	protected override void OnViewDestroy()
	{
		UpdateStatus(m_Master, false);

		CloudUser.authenticationChanged -= OnAuthenticationChanged;
		OnAuthenticationChanged(false);

		UnregisterFromLobby();

		base.OnViewDestroy();
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		RegisterToLobby();

		m_IsReady = false;

		m_CaptionLabel = GuiBaseUtils.GetControl<GUIBase_Label>(Layout, CAPTION_LABEL);
		m_ReadyButton = GuiBaseUtils.RegisterButtonDelegate(Layout, READY_BUTTON, OnReadyPressed, null);
		m_GametypeRoller = GuiBaseUtils.GetControl<GUIBase_Roller>(Layout, GAMETYPE_ROLLER);
		m_FriendList = PrepareList(FRIENDS_LIST, true);
		GuiBaseUtils.RegisterButtonDelegate(Layout, CLOSE_BUTTON, OnClosePressed, null);

		Refresh();
	}

	protected override void OnViewHide()
	{
		GuiBaseUtils.RegisterButtonDelegate(Layout, READY_BUTTON, null, null);
		GuiBaseUtils.RegisterButtonDelegate(Layout, CLOSE_BUTTON, null, null);

		LeaveGang(m_Master);

		UnregisterFromLobby();

		base.OnViewHide();
	}

	protected override void OnViewUpdate()
	{
		if (m_IsDirty == true)
		{
			Refresh();
		}

		base.OnViewUpdate();
	}

	// HANDLERS

	void OnAuthenticationChanged(bool state)
	{
		if (state == true)
		{
			m_PrimaryKey = CloudUser.instance.primaryKey;

			LobbyClient.RegisterPlayerMessageObserver(OnInviteReceived, m_PrimaryKey, LobbyGangMessage.INVITE);
			LobbyClient.RegisterPlayerMessageObserver(OnKickReceived, m_PrimaryKey, LobbyGangMessage.KICK);
			LobbyClient.RegisterPlayerMessageObserver(OnRefreshReceived, m_PrimaryKey, LobbyGangMessage.REFRESH);
		}
		else
		{
			LobbyClient.UnregisterPlayerMessageObserver(OnInviteReceived, m_PrimaryKey, LobbyGangMessage.INVITE);
			LobbyClient.UnregisterPlayerMessageObserver(OnKickReceived, m_PrimaryKey, LobbyGangMessage.KICK);
			LobbyClient.UnregisterPlayerMessageObserver(OnRefreshReceived, m_PrimaryKey, LobbyGangMessage.REFRESH);
		}
	}

	void OnReadyPressed()
	{
		if (string.IsNullOrEmpty(m_Master) == false)
		{
			m_IsReady = !m_IsReady;

			m_ReadyButton.SetNewText(m_IsReady ? 109048 : 109071); //READY! : READY?

			var data = new
			{
				master = m_Master,
				primaryKey = m_PrimaryKey,
				status = m_IsReady
			};
			LobbyClient.SendMessageToPlayer(m_Master, LobbyGangMessage.READY, JsonMapper.ToJson(data));

			SetDirty();
		}
	}

	void OnClosePressed()
	{
		UpdateStatus(m_Master, false);

		Owner.Back();
	}

	void OnUpdateListRow(GUIBase_Widget widget, int rowIndex, int itemIndex)
	{
		RefreshListItem(rowIndex, itemIndex);
	}

	void OnInviteReceived(string primaryKey, string messageId, string messageText)
	{
		//Debug.Log(">>>> INVITATION: "+messageText);

		JsonData data = JsonMapper.ToObject(messageText);

		string master = data["master"].ToString();

		if (m_IsJoining == false && CanJoinGang(master) == true && LobbyGang.FriendsInvited == 0)
		{
			StartCoroutine(JoinGang_Coroutine(master));
		}
		else
		{
			UpdateStatus(master, false);
		}
	}

	void OnKickReceived(string primaryKey, string messageId, string messageText)
	{
		//Debug.Log(">>>> KICKED: "+messageText);

		if (m_IsJoining == false)
		{
			JsonData data = JsonMapper.ToObject(messageText);

			string master = data["master"].ToString();

			LeaveGang(master);
		}
		else
		{
			m_IsJoining = false;
		}
	}

	void OnRefreshReceived(string inPrimaryKey, string messageId, string messageText)
	{
		//Debug.Log(">>>> REFRESH: "+messageText);

		JsonData data = JsonMapper.ToObject(messageText);

		int gametype;
		if (int.TryParse(data["gametype"].ToString(), out gametype) == true)
		{
			m_Gametype = (E_MPGameType)gametype;
		}
		else
		{
			m_Gametype = E_MPGameType.DeathMatch;
		}

		JsonData friends = data["friends"];
		m_Friends = new FriendInfo[friends.Count - 1];
		int idx = 0;
		for (int i = 0; i < friends.Count; i++)
		{
			JsonData item = friends[i];

			string primaryKey = (string)item["primaryKey"];
			if (primaryKey == m_PrimaryKey)
				continue;

			m_Friends[idx] = new FriendInfo()
			{
				PrimaryKey = primaryKey,
				Nickname = (string)item["nickname"],
				Rank = (int)item["rank"],
				IsReady = (bool)item["status"]
			};

			idx++;
		}

		SetDirty();
	}

	// PRIVATE METHODS

	void Refresh()
	{
		m_IsDirty = false;

		RefreshControls();
	}

	void RefreshControls()
	{
		for (int idx = 0; idx < m_Rows.Length; ++idx)
		{
			RefreshListItem(idx, m_Rows[idx].FriendIndex);
		}
		m_FriendList.MaxItems = m_Friends.Length;

		var friend = string.IsNullOrEmpty(m_Master) == false ? GameCloudManager.friendList.friends.Find(obj => obj.PrimaryKey == m_Master) : null;
		string nickname = friend != null ? GuiBaseUtils.FixNameForGui(friend.Nickname) : null;
		string caption = string.IsNullOrEmpty(nickname) == false
										 ? string.Format(TextDatabase.instance[109052], nickname)
										 : TextDatabase.instance[109053];
		m_CaptionLabel.SetNewText(caption);

		m_ReadyButton.IsDisabled = m_Master == null ? true : false;
		m_ReadyButton.isHighlighted = string.IsNullOrEmpty(m_Master) == false ? m_IsReady : false;

		m_GametypeRoller.Selection = (int)m_Gametype;
	}

	bool RefreshListItem(int rowIdx, int friendIdx)
	{
		ListRow row = GetRow(rowIdx);
		FriendInfo friend = GetFriend(friendIdx);

		if (friend != null)
		{
			row.FriendIndex = friendIdx;

			int statusTextId = friend.IsReady ? 0109047 : 0109046;

			if (row.Root.Visible == false)
			{
				row.Root.Show(true, true);
			}

			row.Name.SetNewText(GuiBaseUtils.FixNameForGui(friend.Nickname));
			row.Status.SetNewText(string.Format("({0})", TextDatabase.instance[statusTextId]));
			row.RankValue.SetNewText(friend.Rank.ToString());
			row.RankIcon.State = string.Format("Rank_{0}", Mathf.Min(friend.Rank, row.RankIcon.Count - 1).ToString("D2"));

			row.WaitingBg.Widget.Show(!friend.IsReady, true);
			row.ReadyBg.Widget.Show(friend.IsReady, true);

			return friend.IsReady;
		}
		else
		{
			row.FriendIndex = -1;

			row.Root.Show(false, true);

			return false;
		}
	}

	GUIBase_List PrepareList(string listName, bool state)
	{
		GUIBase_List list = GuiBaseUtils.GetControl<GUIBase_List>(Layout, listName);

		if (state == true)
		{
			list.OnUpdateRow += OnUpdateListRow;
		}
		else
		{
			list.OnUpdateRow -= OnUpdateListRow;
		}

		// prepare list
		if (m_Rows.Length != list.numOfLines)
		{
			m_Rows = new ListRow[list.numOfLines];

			for (int idx = 0; idx < list.numOfLines; ++idx)
			{
				GUIBase_Widget root = list.GetWidgetOnLine(idx);
				Transform trans = list.GetWidgetOnLine(idx).transform;

				ListRow row = new ListRow()
				{
					Root = root,
					Name = trans.FindChild(LISTITEM_NAME).GetComponent<GUIBase_Label>(),
					Status = trans.FindChild(LISTITEM_STATUS).GetComponent<GUIBase_Label>(),
					RankValue = trans.FindChild(LISTITEM_RANKVALUE).GetComponent<GUIBase_Label>(),
					RankIcon = trans.FindChild(LISTITEM_RANKICON).GetComponent<GUIBase_MultiSprite>(),
					WaitingBg = trans.FindChild(LISTITEM_WAITINGBG).GetComponent<GUIBase_Sprite>(),
					ReadyBg = trans.FindChild(LISTITEM_READYBG).GetComponent<GUIBase_Sprite>(),
				};
				m_Rows[idx] = row;
			}
		}

		return list;
	}

	ListRow GetRow(int rowIdx)
	{
		return rowIdx >= 0 && rowIdx < m_Rows.Length ? m_Rows[rowIdx] : null;
	}

	FriendInfo GetFriend(int friendIdx)
	{
		return friendIdx >= 0 && friendIdx < m_Friends.Length ? m_Friends[friendIdx] : null;
	}

	FriendInfo GetFriendByRowIdx(int rowIdx)
	{
		ListRow row = GetRow(rowIdx);
		return row != null ? GetFriend(row.FriendIndex) : null;
	}

	void UpdateStatus(string master, bool status)
	{
		if (string.IsNullOrEmpty(master) == true)
			return;

		var data = new
		{
			master = master,
			primaryKey = m_PrimaryKey,
			status = status
		};
		LobbyClient.SendMessageToPlayer(master, LobbyGangMessage.STATUS, JsonMapper.ToJson(data));
	}

	bool CanJoinGang(string master)
	{
		if (string.IsNullOrEmpty(master) == true)
			return false;
		if (string.IsNullOrEmpty(m_Master) == false && m_Master != master)
			return false;

		// don't allow invite from non-friend
		if (GameCloudManager.friendList.friends.Find(obj => obj.PrimaryKey == master) == null)
			return false;

		return true;
	}

	IEnumerator JoinGang_Coroutine(string master)
	{
		m_IsJoining = true;

		bool canJoin = GuiFrontendMain.IsVisible;
		if (canJoin == true)
		{
			if (m_Master == null && IsVisible == false)
			{
				var friend = GameCloudManager.friendList.friends.Find(obj => obj.PrimaryKey == master);
				string nickname = GuiBaseUtils.FixNameForGui(friend.Nickname);
				string caption = TextDatabase.instance[0109064]; //string.Format(TextDatabase.instance[0109064], nickname);
				string text = string.Format(TextDatabase.instance[0109065], nickname);
				GuiPopup popup = GuiFrontendMain.ShowPopup("ConfirmDialog",
														   caption,
														   text,
														   (inPopup, inResult) =>
														   {
															   if (inResult != E_PopupResultCode.Ok)
															   {
																   canJoin = false;
															   }
														   });

				while (popup.IsVisible == true)
				{
					yield return new WaitForEndOfFrame();

					if (m_IsJoining == false)
					{
						popup.ForceClose();
					}
				}
			}

			if (canJoin == true)
			{
				canJoin = JoinGang(master);
			}
		}

		UpdateStatus(master, canJoin);

		m_IsJoining = false;
	}

	bool JoinGang(string master)
	{
		m_Master = master;

		m_Chat.Channel = string.Format(LobbyGangChat.CHANNEL, m_Master);

		if (IsVisible == false)
		{
			GuiFrontendMain.ShowPopup("LobbyGangGuest", "", "");
		}
		else
		{
			SetDirty();
		}

		return true;
	}

	void LeaveGang(string master)
	{
		if (string.IsNullOrEmpty(m_Master) == true)
			return;
		if (m_Master != master)
			return;

		m_Chat.Channel = null;
		m_Chat.Clear();

		m_Master = null;
		m_IsReady = false;
		m_IsJoining = false;
		m_Friends = new FriendInfo[0];

		SetDirty();
	}

	void SetDirty()
	{
		m_IsDirty = true;
	}

	// COOPERATION WITH LOBBY SERVICES

	void RegisterToLobby()
	{
		LobbyClient.OnServerFound += OnServerFound;
		LobbyClient.OnNoServerAvailable += OnNoServerAvailable;
		LobbyClient.OnSearchingInvitedGame += OnSearchingInvitedGame;
	}

	void UnregisterFromLobby()
	{
		LobbyClient.OnServerFound -= OnServerFound;
		LobbyClient.OnNoServerAvailable -= OnNoServerAvailable;
		LobbyClient.OnSearchingInvitedGame -= OnSearchingInvitedGame;
	}

	void OnSearchingInvitedGame(int clientJoinRequestId, string invitingPrimaryKey)
	{
		//Popup.Show( TextDatabase.instance[0109061], "Searching for a suitable game...", "Cancel", OnMessageBoxEvent );
		m_MessageBox =
						(GuiPopupMessageBox)
						Owner.ShowPopup("MessageBox",
										TextDatabase.instance[0109061],
										TextDatabase.instance[0109058],
										(inPopup, inResult) =>
										{
											//inPopup.ForceClose();
											m_MessageBox = null;

											if (inResult == E_PopupResultCode.Ok)
											{
												//FIXME We do not have the id now on the client side. Reimplement later.
												// The problem here is that this client does not pass any information to the lobby.
												// All the communication happens between the master (inviter) and the lobby. The childs (friends) receives just the OnSearchingInvitedGame a notification.
												LobbyClient.CancelFindServer(-1);

												UpdateStatus(m_Master, false);
												LeaveGang(m_Master);
											}
										});
		m_MessageBox.SetButtonText(TextDatabase.instance[02040009]);
	}

	void OnServerFound(int clientJoinRequestId, int serverJoinRequestId, string ipAddress, int port)
	{
		if (m_MessageBox == null || m_MessageBox.IsVisible == false)
			return;

		m_MessageBox.SetText(TextDatabase.instance[0109054]);
		m_MessageBox = null;

		System.Net.IPAddress serverAddress = null;
		System.Net.IPAddress.TryParse(ipAddress, out serverAddress);

		System.Net.IPEndPoint serverEndpoint = new System.Net.IPEndPoint(serverAddress, port);

		Game.Instance.StartNewMultiplayerGame(serverEndpoint, serverJoinRequestId);
	}

	void OnNoServerAvailable(int clientJoinRequestId)
	{
		if (m_MessageBox == null || m_MessageBox.IsVisible == false)
			return;

		m_MessageBox.SetText(TextDatabase.instance[0109055]);
		m_MessageBox.SetButtonText(TextDatabase.instance[02040007]);
		m_MessageBox = null;

		LeaveGang(m_Master);
	}
}
