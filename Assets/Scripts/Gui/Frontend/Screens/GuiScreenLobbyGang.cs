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

public static class LobbyGangMessage
{
	public readonly static string INVITE = "lobby/gang/invite";
	public readonly static string KICK = "lobby/gang/kick";
	public readonly static string STATUS = "lobby/gang/status";
	public readonly static string READY = "lobby/gang/ready";
	public readonly static string REFRESH = "lobby/gang/refresh";
}

public static class LobbyGangChat
{
	public readonly static string CHANNEL = "chat/channel/gang_{0}";
}

public static class LobbyGang
{
	public class FriendInfo
	{
		public string PrimaryKey;
		public string Nickname;
		public int Rank;
		public bool IsOnline = false;
		public bool IsPlaying = false;

		public bool IsInvited
		{
			get { return IsOnline ? m_IsInvited : false; }
			set
			{
				m_IsInvited = value;
				m_InvitedAt = Time.realtimeSinceStartup;
			}
		}

		public bool IsReady
		{
			get { return IsInvited ? m_IsReady : false; }
			set { m_IsReady = value; }
		}

		public bool IsWaiting
		{
			get { return IsInvited ? !m_IsReady : false; }
			set { m_IsReady = !value; }
		}

		public bool CanInvite
		{
			get
			{
				if (IsOnline == false)
					return false;
				if (IsPlaying == true)
					return false;
				if (IsInvited == true)
					return false;
				// wait 1 second before we allow re-invite
				return Time.realtimeSinceStartup - m_InvitedAt > 1.0f ? true : false;
			}
		}

		bool m_IsInvited = false;
		bool m_IsReady = false;
		float m_InvitedAt = 0.0f;
	}

	public static FriendInfo[] Friends = new FriendInfo[0];
	public static int FriendsInvited = 0;
	public static int FriendsReady = 0;
}

[AddComponentMenu("GUI/Frontend/Screens/GuiScreenLobbyGang")]
public class GuiScreenLobbyGang : GuiScreenLobbyBase
{
	readonly static int MAX_INVITES = 6 - 1; // My player is one of those who will be invited. So 5 more left. :-)

	readonly static string PLAY_BUTTON = "Play_Button";
	readonly static string GAMETYPE_ROLLER = "Gametype_Roller";
	readonly static string FRIENDS_LIST = "Friends_List";
	readonly static string LISTITEM_NAME = "Name";
	readonly static string LISTITEM_STATUS = "Status";
	readonly static string LISTITEM_RANKVALUE = "TextRank";
	readonly static string LISTITEM_RANKICON = "PlayerRankPic";
	readonly static string LISTITEM_WAITINGBG = "WaitingBackground";
	readonly static string LISTITEM_READYBG = "ReadyBackground";
	readonly static string LISTITEM_ADDBUTTON = "Add_Button";
	readonly static string LISTITEM_REMOVEBUTTON = "Remove_Button";

	class ListRow
	{
		public GUIBase_Widget Root;
		public GUIBase_Label Name;
		public GUIBase_Label Status;
		public GUIBase_Label RankValue;
		public GUIBase_MultiSprite RankIcon;
		public GUIBase_Sprite WaitingBg;
		public GUIBase_Sprite ReadyBg;
		public GUIBase_Button AddButton;
		public GUIBase_Button RemoveButton;
		public int FriendIndex;
	}

	// PRIVATE MEMBERS

	[SerializeField] float m_UpdateInterval = 2.0f;

	GUIBase_Button m_PlayButton;
	GUIBase_Roller m_GametypeRoller;
	GUIBase_List m_FriendList;
	ScreenComponentChat m_Chat;
	string m_PrimaryKey = "default";
	bool m_IsDirty = true;
	float m_NextUpdateTime = 0.0f;
	ListRow[] m_Rows = new ListRow[0];

	// GUISCREEN INTERFACE

	protected override void OnViewInit()
	{
		base.OnViewInit();

		CloudUser.authenticationChanged += OnAuthenticationChanged;

		m_Chat = RegisterComponent<ScreenComponentChat>();
		m_Chat.Channel = string.Format(LobbyGangChat.CHANNEL, m_PrimaryKey);
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

		m_PlayButton = RegisterButtonDelegate(PLAY_BUTTON, () => { OnPlayPressed((E_MPGameType)m_GametypeRoller.Selection); }, null);
		m_GametypeRoller = RegisterRollerDelegate(GAMETYPE_ROLLER, OnGametypeChanged);
		m_FriendList = PrepareList(FRIENDS_LIST, true);

		GameCloudManager.friendList.FriendListChanged += OnFriendListChanged;
		GameCloudManager.friendList.RetriveFriendListFromCloud();

		m_GametypeRoller.Selection = Game.Settings.GetInt("Gametype", (int)E_MPGameType.DeathMatch);

		Refresh();
	}

	protected override void OnViewHide()
	{
		GameCloudManager.friendList.FriendListChanged -= OnFriendListChanged;

		RegisterButtonDelegate(PLAY_BUTTON, null, null);
		RegisterRollerDelegate(GAMETYPE_ROLLER, null);

		PrepareList(FRIENDS_LIST, false);

		Game.Settings.SetInt("Gametype", m_GametypeRoller.Selection);

		base.OnViewHide();
	}

	protected override void OnViewUpdate()
	{
		m_NextUpdateTime -= Time.deltaTime;
		if (m_IsDirty == true || m_NextUpdateTime <= 0.0f)
		{
			Refresh();

			m_NextUpdateTime = m_UpdateInterval;
		}

		base.OnViewUpdate();
	}

	//HANLDERS

	void OnAuthenticationChanged(bool state)
	{
		if (state == true)
		{
			m_PrimaryKey = CloudUser.instance.primaryKey;

			LobbyClient.RegisterPlayerMessageObserver(OnStatusReceived, m_PrimaryKey, LobbyGangMessage.STATUS);
			LobbyClient.RegisterPlayerMessageObserver(OnIsReadyReceived, m_PrimaryKey, LobbyGangMessage.READY);
		}
		else
		{
			LobbyClient.UnregisterPlayerMessageObserver(OnStatusReceived, m_PrimaryKey, LobbyGangMessage.STATUS);
			LobbyClient.UnregisterPlayerMessageObserver(OnIsReadyReceived, m_PrimaryKey, LobbyGangMessage.READY);
		}
	}

	void OnPlayPressed(E_MPGameType gameType)
	{
		Game.Settings.SetInt("Gametype", m_GametypeRoller.Selection);

		List<string> friends = new List<string>();
		for (int idx = 0; idx < LobbyGang.Friends.Length; ++idx)
		{
			LobbyGang.FriendInfo friend = GetFriend(idx);
			if (friend == null)
				continue;
			if (friend.IsReady == false)
				continue;

			friends.Add(friend.PrimaryKey);
		}

		Play(gameType, friends.ToArray());
	}

	void OnGametypeChanged(int value)
	{
		SetDirty();
	}

	void OnFriendListChanged(object sender, System.EventArgs evt)
	{
		SetDirty();
	}

	void OnUpdateListRow(GUIBase_Widget widget, int rowIndex, int itemIndex)
	{
		RefreshListItem(rowIndex, itemIndex);
	}

	void OnAddFriend(int rowIdx, GUIBase_Widget widget, object evt)
	{
		LobbyGang.FriendInfo friend = GetFriendByRowIdx(rowIdx);
		if (friend == null)
			return;
		if (friend.CanInvite == false)
			return;
		if (friend.IsInvited == true)
			return;

		friend.IsInvited = true;
		friend.IsWaiting = true;

		// send invite
		var data = new
		{
			master = m_PrimaryKey,
			gametype = m_GametypeRoller.Selection
		};
		LobbyClient.SendMessageToPlayer(friend.PrimaryKey, LobbyGangMessage.INVITE, JsonMapper.ToJson(data));

		SetDirty();
	}

	void OnRemoveFriend(int rowIdx, GUIBase_Widget widget, object evt)
	{
		LobbyGang.FriendInfo friend = GetFriendByRowIdx(rowIdx);
		if (friend == null)
			return;
		if (friend.IsInvited == false)
			return;

		friend.IsInvited = false;

		// send remove
		var data = new
		{
			master = m_PrimaryKey
		};
		LobbyClient.SendMessageToPlayer(friend.PrimaryKey, LobbyGangMessage.KICK, JsonMapper.ToJson(data));

		SetDirty();
	}

	void OnStatusReceived(string primaryKey, string messageId, string messageText)
	{
		//Debug.Log(">>>> CONFIRMED: "+messageText);

		JsonData data = JsonMapper.ToObject(messageText);

		if (data["master"].ToString() != m_PrimaryKey)
		{
			Debug.Log("LobbyGang :: Received confirmation from non-invited user '" + data["primaryKey"].ToString() + "'!");
			return;
		}

		LobbyGang.FriendInfo friend = GetFriendByPrimaryKey(data["primaryKey"].ToString());
		if (friend == null)
			return;

		bool status;
		if (bool.TryParse(data["status"].ToString(), out status) == true)
		{
			friend.IsInvited = status;
		}
		else
		{
			friend.IsInvited = false;
		}

		SetDirty();
	}

	void OnIsReadyReceived(string primaryKey, string messageId, string messageText)
	{
		//Debug.Log(">>>> IS READY: "+messageText);

		JsonData data = JsonMapper.ToObject(messageText);

		if (data["master"].ToString() != m_PrimaryKey)
		{
			Debug.Log("LobbyGang :: Received status update from non-invited user '" + data["primaryKey"].ToString() + "'!");
			return;
		}

		LobbyGang.FriendInfo friend = GetFriendByPrimaryKey(data["primaryKey"].ToString());
		if (friend == null)
			return;
		if (friend.IsInvited == false)
			return;

		bool status;
		if (bool.TryParse(data["status"].ToString(), out status) == true)
		{
			friend.IsReady = status;
		}
		else
		{
			friend.IsReady = false;
		}

		SetDirty();
	}

	// PRIVATE METHODS

	void Refresh()
	{
		RefreshFriends();
		RefreshControls();

		m_IsDirty = false;
	}

	void RefreshFriends()
	{
		LobbyGang.FriendsInvited = 0;
		LobbyGang.FriendsReady = 0;

		bool updateInvited = m_IsDirty;

		// update firend list
		List<FriendList.FriendInfo> friends = GameCloudManager.friendList.friends;
		if (LobbyGang.Friends.Length != friends.Count)
		{
			LobbyGang.FriendInfo[] newFriends = new LobbyGang.FriendInfo[friends.Count];

			for (int idx = 0; idx < friends.Count; ++idx)
			{
				string primaryKey = friends[idx].PrimaryKey;
				LobbyGang.FriendInfo friend = System.Array.Find(LobbyGang.Friends, obj => obj.PrimaryKey == primaryKey);

				newFriends[idx] = new LobbyGang.FriendInfo()
				{
					PrimaryKey = primaryKey,
					Nickname = friends[idx].Nickname,
					Rank = friends[idx].Rank,
					IsOnline = friend != null ? friend.IsOnline : false,
					IsInvited = friend != null ? friend.IsInvited : false,
					IsReady = friend != null ? friend.IsReady : false,
				};
			}

			LobbyGang.Friends = newFriends;

			updateInvited = true;
		}

		// add user first
		List<object> data = new List<object>();
		data.Add(new
		{
			primaryKey = m_PrimaryKey,
			nickname = CloudUser.instance.nickName,
			rank = PPIManager.Instance.GetLocalPPI().Rank,
			status = true
		});

		// check friends and add invited ones
		for (int idx = 0; idx < LobbyGang.Friends.Length; ++idx)
		{
			LobbyGang.FriendInfo friend = GetFriend(idx);
			if (friend == null)
				continue;

			CheckFriendOnlineStatus(friend.PrimaryKey, friends, ref friend.IsOnline, ref friend.IsPlaying);
			if (friend.IsOnline == false || friend.IsPlaying == true)
			{
				friend.IsInvited = false;
				updateInvited = true;
			}

			if (friend.IsInvited == true)
			{
				LobbyGang.FriendsInvited += 1;

				data.Add(new
				{
					primaryKey = friend.PrimaryKey,
					nickname = friend.Nickname,
					rank = friend.Rank,
					status = friend.IsReady
				});
			}

			if (friend.IsReady == true)
			{
				LobbyGang.FriendsReady += 1;
			}
		}

		// sort list by online status and primaryKey
		System.Array.Sort(LobbyGang.Friends,
						  (x, y) =>
						  {
							  if (x.IsOnline != y.IsOnline)
							  {
								  if (x.IsOnline == true)
									  return -1;
								  if (y.IsOnline == true)
									  return 1;
							  }
							  return string.IsNullOrEmpty(x.PrimaryKey) == false ? x.PrimaryKey.CompareTo(y.PrimaryKey) : 0;
						  });

		// forward information to invited friends
		if (updateInvited == true)
		{
			string json = JsonMapper.ToJson(new
			{
				gametype = m_GametypeRoller.Selection,
				friends = data
			});
			foreach (var friend in LobbyGang.Friends)
			{
				if (friend.IsInvited == true)
				{
					LobbyClient.SendMessageToPlayer(friend.PrimaryKey, LobbyGangMessage.REFRESH, json);
				}
			}
		}
	}

	void RefreshControls()
	{
		for (int idx = 0; idx < m_Rows.Length; ++idx)
		{
			RefreshListItem(idx, m_Rows[idx].FriendIndex);
		}
		m_FriendList.MaxItems = LobbyGang.Friends.Length;

		m_PlayButton.IsDisabled = LobbyGang.FriendsReady > 0 ? false : true;
		m_Chat.CanSendMessage = LobbyGang.FriendsInvited > 0 ? true : false;
	}

	void RefreshListItem(int rowIdx, int friendIdx)
	{
		ListRow row = GetRow(rowIdx);
		LobbyGang.FriendInfo friend = GetFriend(friendIdx);

		if (friend != null)
		{
			row.FriendIndex = friendIdx;

			int statusTextId;
			if (friend.IsReady)
			{
				statusTextId = 0109047;
			}
			else if (friend.IsWaiting)
			{
				statusTextId = 0109046;
			}
			else if (friend.IsOnline)
			{
				statusTextId = 0109044;
			}
			else if (friend.IsPlaying)
			{
				statusTextId = 0109063;
			}
			else
			{
				statusTextId = 0109045;
			}

			if (row.Root.Visible == false)
			{
				row.Root.Show(true, true);
			}

			row.Name.SetNewText(GuiBaseUtils.FixNameForGui(friend.Nickname));
			row.Status.SetNewText(string.Format("({0})", TextDatabase.instance[statusTextId]));
			row.RankValue.SetNewText(friend.Rank.ToString());
			row.RankIcon.State = string.Format("Rank_{0}", Mathf.Min(friend.Rank, row.RankIcon.Count - 1).ToString("D2"));

			bool available = LobbyGang.FriendsInvited < MAX_INVITES ? friend.IsOnline : false;
			bool canInvite = available ? friend.CanInvite : false;

			row.AddButton.IsDisabled = friend.IsInvited ? true : !canInvite;
			row.RemoveButton.IsDisabled = friend.IsInvited ? false : !available;

			row.WaitingBg.Widget.Show(friend.IsWaiting, true);
			row.ReadyBg.Widget.Show(friend.IsReady, true);
			row.AddButton.Widget.Show(!friend.IsInvited && available, true);
			row.RemoveButton.Widget.Show(friend.IsInvited && available, true);
		}
		else
		{
			row.FriendIndex = -1;

			row.Root.Show(false, true);
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
					AddButton = trans.FindChild(LISTITEM_ADDBUTTON).GetComponent<GUIBase_Button>(),
					RemoveButton = trans.FindChild(LISTITEM_REMOVEBUTTON).GetComponent<GUIBase_Button>()
				};
				m_Rows[idx] = row;
			}
		}

		// register delegates
		for (int idx = 0; idx < m_Rows.Length; ++idx)
		{
			ListRow row = m_Rows[idx];

			if (state == true)
			{
				int rowIdx = idx;

				row.AddButton.RegisterTouchDelegate3((widget, evt) => { OnAddFriend(rowIdx, widget, evt); });

				row.RemoveButton.RegisterTouchDelegate3((widget, evt) => { OnRemoveFriend(rowIdx, widget, evt); });
			}
			else
			{
				row.AddButton.RegisterTouchDelegate3(null);
				row.RemoveButton.RegisterTouchDelegate3(null);
			}
		}

		return list;
	}

	ListRow GetRow(int rowIdx)
	{
		return rowIdx >= 0 && rowIdx < m_Rows.Length ? m_Rows[rowIdx] : null;
	}

	LobbyGang.FriendInfo GetFriend(int friendIdx)
	{
		return friendIdx >= 0 && friendIdx < LobbyGang.Friends.Length ? LobbyGang.Friends[friendIdx] : null;
	}

	LobbyGang.FriendInfo GetFriendByRowIdx(int rowIdx)
	{
		ListRow row = GetRow(rowIdx);
		return row != null ? GetFriend(row.FriendIndex) : null;
	}

	LobbyGang.FriendInfo GetFriendByPrimaryKey(string primaryKey)
	{
		return System.Array.Find(LobbyGang.Friends, obj => obj.PrimaryKey == primaryKey);
	}

	void CheckFriendOnlineStatus(string primaryKey, List<FriendList.FriendInfo> friends, ref bool isOnline, ref bool isPlaying)
	{
		FriendList.FriendInfo friend = friends.Find(obj => obj.PrimaryKey == primaryKey);
		if (friend != null)
		{
			isOnline = friend.OnlineStatus == FriendList.E_OnlineStatus.InLobby ? true : false;
			isPlaying = friend.OnlineStatus == FriendList.E_OnlineStatus.InGame ? true : false;
		}
		else
		{
			isOnline = false;
			isPlaying = false;
		}
	}

	void SetDirty()
	{
		m_IsDirty = true;
	}
}
