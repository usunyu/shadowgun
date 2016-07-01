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

// =============================================================================================================================
// =============================================================================================================================
public class SelectFriendDialog : GuiPopup
{
	public const string SHOW_BEST_RESULTS = "{[__BEST__]}";

	FriendListView m_FriendsView;

	GUIBase_Button m_BestButton;

	public string selectedFriend { get; private set; }

	// =========================================================================================================================
	public override void SetCaption(string inCaption)
	{
	}

	public override void SetText(string inText)
	{
	}

	// =========================================================================================================================
	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_ScreenPivot = MFGuiManager.Instance.GetPivot("SelectFriend_Dialog");
		m_ScreenLayout = m_ScreenPivot.GetLayout("SelectFriend_Layout");

		//GUIBase_Button prevButton	= PrepareButton(m_ScreenLayout, "Prev_Button",	null, null);
		//GUIBase_Button nextButton	= PrepareButton(m_ScreenLayout, "Next_Button",	null, null);
		m_BestButton = PrepareButton(m_ScreenLayout, "Best_Button", null, Delegate_SelectBest);

		//prevButton.autoColorLabels 		= true;
		//nextButton.autoColorLabels 		= true;
		m_BestButton.autoColorLabels = true;

		GUIBase_List list = GetWidget(m_ScreenLayout, "Table").GetComponent<GUIBase_List>();

		m_FriendsView = gameObject.AddComponent<FriendListView>();
		//m_FriendsView.m_OnFriendSelectDelegate = Delegate_OnSelect;
		m_FriendsView.GUIView_Init(this, m_ScreenLayout, list /*, prevButton, nextButton*/);
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		List<FriendList.FriendInfo> allFriends = GameCloudManager.friendList.friends;

		m_BestButton.SetDisabled(allFriends == null || allFriends.Count <= 0);

		m_FriendsView.GUIView_Show();
	}

	protected override void OnViewHide()
	{
		m_FriendsView.GUIView_Hide();
		base.OnViewHide();
	}

	protected override void OnViewUpdate()
	{
		m_FriendsView.GUIView_Update();

		base.OnViewUpdate();
	}

	// #################################################################################################################
	// ###  Delegates  #################################################################################################
	void Delegate_SelectBest(GUIBase_Widget inInstigator)
	{
		selectedFriend = SHOW_BEST_RESULTS;
		//Debug.LogWarning("Selected friend :: " + selectedFriend);

		SendResult(E_PopupResultCode.Ok);

		Owner.Back();
	}

	void Delegate_OnSelect(string inFriendName)
	{
		selectedFriend = inFriendName;
		//Debug.LogWarning("Selected friend :: " + selectedFriend);

		SendResult(E_PopupResultCode.Ok);

		Owner.Back();
	}

	// =================================================================================================================
	// internal functionality...
}
