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

public class AddFriendListView : BaseListView
{
	class FriendInfo
	{
		public string PrimaryKey;
		public string Nickname;
		public int Rank;
	}

	class FriendLine
	{
		FriendInfo m_FriendInfo;

		AddFriendListView m_Owner;
		GUIBase_Widget m_Line;

		GUIBase_Label m_Nickname;
		GUIBase_Label m_RankText;
		GUIBase_MultiSprite m_RankIcon;
		GUIBase_Button m_Add;

		public FriendLine(AddFriendListView owner, GUIBase_Widget inLine) : this(inLine)
		{
			m_Owner = owner;
		}

		public FriendLine(GUIBase_Widget inLine)
		{
			Transform trans = inLine.transform;

			m_Line = inLine;
			m_Nickname = trans.GetChildComponent<GUIBase_Label>("Nickname");
			m_RankText = trans.GetChildComponent<GUIBase_Label>("TextRank");
			m_RankIcon = trans.GetChildComponent<GUIBase_MultiSprite>("PlayerRankPic");
			m_Add = trans.GetChildComponent<GUIBase_Button>("Add_Button");

			m_Add.RegisterTouchDelegate(Delegate_Add);
		}

		public void Update(FriendInfo inFriend)
		{
			m_FriendInfo = inFriend;

			m_Nickname.SetNewText(GuiBaseUtils.FixNameForGui(m_FriendInfo.Nickname));
			m_RankText.SetNewText(m_FriendInfo.Rank <= 0 ? "" : m_FriendInfo.Rank.ToString());

			string rankState = string.Format("Rank_{0}", Mathf.Min(m_FriendInfo.Rank, m_RankIcon.Count - 1).ToString("D2"));
			m_RankIcon.State = m_FriendInfo.Rank <= 0 ? GUIBase_MultiSprite.DefaultState : rankState;
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

		void Delegate_Add()
		{
			m_Owner.AddFriend(m_FriendInfo.PrimaryKey, m_FriendInfo.Nickname);
		}
	};

	// PRIVATE MEMBERS

	FriendLine[] m_GuiLines;
	FriendInfo[] m_Friends = new FriendInfo[0];
	GUIBase_List m_Table;
	GUIBase_Layout m_View;
	GuiScreen m_Owner;
	bool m_IsDirty;

	GUIBase_Button m_AddFacebookFriendsBtn;

	// PUBLIC METHODS

	public void AddFriend(string primaryKey, string nickname)
	{
		NewFriendDialog popup = (NewFriendDialog)m_Owner.Owner.ShowPopup("NewFriend", string.Empty, string.Empty);
		popup.Nickname = nickname;
		popup.Username = string.Empty;
		popup.PrimaryKey = primaryKey;
	}

	// BASELISTVIEW INTERFACE

	public void GUIView_Init(GuiScreen owner, GUIBase_Layout inView, GUIBase_List inList)
	{
		m_Owner = owner;
		m_View = inView;
		m_Table = GuiBaseUtils.GetControl<GUIBase_List>(m_View, "Table");

		InitGuiLines(inList);
	}

	public void GUIView_Destroy()
	{
	}

	public override void GUIView_Show()
	{
		if (m_View.Visible == true)
			return;

		m_Table.OnUpdateRow += OnUpdateTableRow;

		MFGuiManager.Instance.ShowLayout(m_View, true);

		UpdateView();

		GameCloudManager.friendList.FriendListChanged += OnFriendListChanged;
		GameCloudManager.friendList.PendingFriendListChanged += OnFriendListChanged;

		GuiBaseUtils.RegisterButtonDelegate(m_View, "AddByUsername_Button", OnAddNewFriend, null);
	}

	public override void GUIView_Hide()
	{
		if (m_View.Visible == false)
			return;

		GuiBaseUtils.RegisterButtonDelegate(m_View, "AddByUsername_Button", null, null);

		m_Table.OnUpdateRow -= OnUpdateTableRow;

		GameCloudManager.friendList.FriendListChanged -= OnFriendListChanged;
		GameCloudManager.friendList.PendingFriendListChanged -= OnFriendListChanged;

		MFGuiManager.Instance.ShowLayout(m_View, false);
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

	public override void GUIView_Update()
	{
		if (m_IsDirty == false)
			return;
		m_IsDirty = false;

		UpdateView();
	}

	// HANDLERS

	void OnAddNewFriend()
	{
		AddFriend(string.Empty, string.Empty);
	}

	void OnAddFacebookFriend()
	{
		GameCloudManager.facebookFriendList.AddAllFacebookFriends(AddAllFriendsCallback);
	}

	void AddAllFriendsCallback(int number)
	{
		m_Owner.Owner.ShowPopup("MessageBox", TextDatabase.instance[2040252], string.Format(TextDatabase.instance[2040253], number));
	}

	void OnFriendListChanged(object sender, System.EventArgs e)
	{
		m_IsDirty = true;
	}

	// PRIVATE METHODS

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
			m_GuiLines[i] = new FriendLine(this, inParent.GetWidgetOnLine(i));
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

	void UpdateView()
	{
		var primaryKey = CloudUser.instance.primaryKey;
		var ppiList = PPIManager.Instance.GetPPIList();
		var friends = GameCloudManager.friendList.friends;
		var pendings = GameCloudManager.friendList.pendingFriends;

		List<FriendInfo> list = new List<FriendInfo>();
		foreach (var ppi in ppiList)
		{
			if (ppi.PrimaryKey == primaryKey)
				continue;

			bool isFriend = friends.Find(obj => obj.PrimaryKey == ppi.PrimaryKey) != null ? true : false;
			if (isFriend == false)
			{
				isFriend = pendings.Find(obj => obj.PrimaryKey == ppi.PrimaryKey) != null ? true : false;
			}
			if (isFriend == true)
				continue;

			list.Add(new FriendInfo()
			{
				PrimaryKey = ppi.PrimaryKey,
				Nickname = ppi.Name,
				Rank = ppi.Rank,
			});
		}

		list.Sort((x, y) => { return string.Compare(x.PrimaryKey, y.PrimaryKey); });

		m_Friends = list.ToArray();
		m_Table.MaxItems = m_Friends.Length;
		m_Table.Widget.SetModify();

		m_View.GetWidget("Hint_Label").Show(m_Friends.Length == 0 ? true : false, true);
	}
}
