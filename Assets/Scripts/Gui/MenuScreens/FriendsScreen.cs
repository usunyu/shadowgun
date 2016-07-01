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
public class FriendsScreen : GuiScreenModal, IGuiOverlayScreen
{
	protected override void OnViewInit()
	{
		m_ScreenLayout = GetLayout("FriendList_Dialog", "Friends_Layout");

		base.OnViewInit();

		m_ActiveButton = PrepareButton(m_ScreenLayout, "Active_Button", null, OnActive);
		m_PendingButton = PrepareButton(m_ScreenLayout, "Pending_Button", null, OnPending);
		m_AddFriendButton = PrepareButton(m_ScreenLayout, "AddFriend_Button", null, OnAddFriend);

		m_ActiveButton.stayDown = true;
		m_PendingButton.stayDown = true;
		m_AddFriendButton.stayDown = true;

		// initialize active view
		{
			GUIBase_Layout l = GetLayout("FriendList_Dialog", "ActiveFriends_Layout");
			GUIBase_List list = GetWidget(l, "Table").GetComponent<GUIBase_List>();

			m_FriendsView = gameObject.AddComponent<FriendListView>();
			m_FriendsView.GUIView_Init(this, l, list);
		}

		// initialize pending view
		{
			GUIBase_Layout l = GetLayout("FriendList_Dialog", "PendingFriends_Layout");
			GUIBase_List list = GetWidget(l, "Table").GetComponent<GUIBase_List>();

			m_PendingFriendsView = gameObject.AddComponent<PendingFriendListView>();
			m_PendingFriendsView.GUIView_Init(l, list);
		}

		// initialize add view
		{
			GUIBase_Layout l = GetLayout("FriendList_Dialog", "AddFriends_Layout");
			GUIBase_List list = GetWidget(l, "Table").GetComponent<GUIBase_List>();

			m_AddFriendsView = gameObject.AddComponent<AddFriendListView>();
			m_AddFriendsView.GUIView_Init(this, l, list);
		}
	}

	protected override void OnViewShow()
	{
		if (m_ActiveView == null || m_ActiveView == m_FriendsView)
		{
			ShowActive();
		}
		else if (m_ActiveView == m_PendingFriendsView)
		{
			ShowPending();
		}
		else if (m_ActiveView == m_AddFriendsView)
		{
			ShowAddFriend();
		}

		GameCloudManager.friendList.RetriveFriendListFromCloud();

		GameCloudManager.mailbox.FetchMessages();

		base.OnViewShow();
	}

	protected override void OnViewHide()
	{
		m_FriendsView.GUIView_Hide();
		m_PendingFriendsView.GUIView_Hide();
		m_AddFriendsView.GUIView_Hide();

		base.OnViewHide();
	}

	protected override void OnViewUpdate()
	{
		int count = GetPendingRequestsCount();

		string caption = TextDatabase.instance[2040203];
		if (count > 0)
		{
			caption = string.Format("{0} ({1})", caption, count);
		}
		m_PendingButton.SetNewText(caption);
		m_PendingButton.ForceHighlight(m_ActiveView != m_PendingFriendsView && count > 0 ? true : false);

		if (m_ActiveView != null)
		{
			m_ActiveView.GUIView_Update();
		}

		base.OnViewUpdate();
	}

	protected override void OnViewEnable()
	{
		//m_ScreenPivot.FadeAlpha = 1.0f;
		//m_ScreenLayout.EnableControls(true);
	}

	protected override void OnViewDisable()
	{
		//m_ScreenPivot.FadeAlpha = 0.5f;
		//m_ScreenLayout.EnableControls(false);		
	}

	protected override GUIBase_Widget OnViewHitTest(ref Vector2 point)
	{
		if (Owner == null)
			return null;

		GUIBase_Widget widget = base.OnViewHitTest(ref point);
		if (widget != null)
			return widget;

		return m_ActiveView != null ? m_ActiveView.GUIView_HitTest(ref point) : null;
	}

	protected override bool OnViewProcessInput(ref IInputEvent evt)
	{
		if (base.OnViewProcessInput(ref evt) == true)
			return true;

		if (m_ActiveView != null)
			return m_ActiveView.GUIView_ProcessInput(ref evt);

		return false;
	}

	// IGUIOVERLAYSCREEN INTERFACE

	string IGuiOverlayScreen.OverlayButtonTooltip
	{
		get
		{
			if (IsVisible == true)
				return null;
			int count = GetPendingRequestsCount();
			if (count == 0)
				return null;
			return count <= 99 ? count.ToString() : "99+";
		}
	}

	bool IGuiOverlayScreen.HideOverlayButton
	{
		get { return Ftue.IsActionIdle<FtueAction.Friends>(); }
	}

	bool IGuiOverlayScreen.HighlightOverlayButton
	{
		get
		{
			if (Ftue.IsActionActive<FtueAction.Friends>() == true)
				return true;
			if (IsVisible == true)
				return false;
			int count = GetPendingRequestsCount();
			return count > 0 ? true : false;
		}
	}

	// #################################################################################################################
	// ###  Delegates  #################################################################################################
	void OnActive(GUIBase_Widget inWidget)
	{
		//Debug.Log("OnActive");
		ShowActive();
	}

	void OnPending(GUIBase_Widget inWidget)
	{
		//Debug.Log("OnPending");
		ShowPending();
	}

	void OnAddFriend(GUIBase_Widget inWidget)
	{
		//Debug.Log("OnAddFriend");
		ShowAddFriend();
	}

	// #################################################################################################################		
	// ###  internal  ##################################################################################################
	void ShowActive()
	{
		m_FriendsView.GUIView_Show();
		m_PendingFriendsView.GUIView_Hide();
		m_AddFriendsView.GUIView_Hide();

		m_ActiveButton.ForceDownStatus(true);
		m_PendingButton.ForceDownStatus(false);
		m_AddFriendButton.ForceDownStatus(false);

		m_ActiveView = m_FriendsView;

		GameCloudManager.friendList.RetriveFriendListFromCloud();
	}

	void ShowPending()
	{
		m_FriendsView.GUIView_Hide();
		m_PendingFriendsView.GUIView_Show();
		m_AddFriendsView.GUIView_Hide();

		m_ActiveView = m_PendingFriendsView;

		m_ActiveButton.ForceDownStatus(false);
		m_PendingButton.ForceDownStatus(true);
		m_AddFriendButton.ForceDownStatus(false);

		// refresh friend list from cloud...
		GameCloudManager.friendList.RetriveFriendListFromCloud();
	}

	void ShowAddFriend()
	{
		m_FriendsView.GUIView_Hide();
		m_PendingFriendsView.GUIView_Hide();
		m_AddFriendsView.GUIView_Show();

		m_ActiveView = m_AddFriendsView;

		m_ActiveButton.ForceDownStatus(false);
		m_PendingButton.ForceDownStatus(false);
		m_AddFriendButton.ForceDownStatus(true);

		// refresh friend list from cloud...
		GameCloudManager.friendList.RetriveFriendListFromCloud();
	}

	int GetPendingRequestsCount()
	{
		int count = 0;
		GameCloudManager.friendList.pendingFriends.ForEach((obj) =>
														   {
															   if (obj.IsItRequest == true)
															   {
																   count++;
															   }
														   });
		return count;
	}

	// #################################################################################################################		

	AddFriendListView m_AddFriendsView;
	PendingFriendListView m_PendingFriendsView;
	FriendListView m_FriendsView;
	BaseListView m_ActiveView;

	GUIBase_Button m_ActiveButton;
	GUIBase_Button m_PendingButton;
	GUIBase_Button m_AddFriendButton;
}
