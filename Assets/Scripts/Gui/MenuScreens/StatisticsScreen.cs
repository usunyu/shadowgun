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
public class StatisticsScreen : GuiScreen
{
	StatisticsView m_StatisticsView;

	GUIBase_Button m_SelectFriends;
	GUIBase_Label m_FriendNameLabel;

	Statistics.E_Mode m_Mode = Statistics.E_Mode.Player;
	//private string 								m_FriendName = string.Empty;

	// =========================================================================================================================
	// === GuiScreen interface ============================================================================================
	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_ScreenPivot = GetPivot("Statistics_Screen");
		m_ScreenLayout = GetLayout("Statistics_Screen", "Statistics_Layout");

		PrepareButton(m_ScreenLayout, "03_SelectFriend_Button", null, OnSelectFriend);

		GUIBase_Button prevButton = PrepareButton(m_ScreenLayout, "Prev_Button", null, null);
		GUIBase_Button nextButton = PrepareButton(m_ScreenLayout, "Next_Button", null, null);

		prevButton.autoColorLabels = true;
		nextButton.autoColorLabels = true;

		GUIBase_List list = GetWidget(m_ScreenLayout, "Table").GetComponent<GUIBase_List>();
		m_StatisticsView = gameObject.AddComponent<StatisticsView>();
		m_StatisticsView.GUIView_Init(m_ScreenLayout, list, prevButton, nextButton);

		GUIBase_Widget header = GetWidget(m_ScreenLayout, "Header");

		m_SelectFriends = header.transform.GetChildComponent<GUIBase_Button>("03_SelectFriend_Button");
		m_SelectFriends.autoColorLabels = true;
		m_FriendNameLabel = header.transform.GetChildComponent<GUIBase_Label>("04_friend_name");
	}

	protected override void OnViewShow()
	{
		MFGuiManager.Instance.ShowPivot(m_ScreenPivot, true);
		m_StatisticsView.GUIView_Show();
		UpdateHeader();
		base.OnViewShow();

		//Flurry.logEvent(AnalyticsTag.Statistic, true);
	}

	protected override void OnViewHide()
	{
		m_StatisticsView.GUIView_Hide();
		MFGuiManager.Instance.ShowPivot(m_ScreenPivot, false);

		//Flurry.endTimedEvent(AnalyticsTag.Statistic);

		base.OnViewHide();
	}

	protected override void OnViewUpdate()
	{
		m_StatisticsView.GUIView_Update();

		base.OnViewUpdate();
	}

	protected override void OnViewEnable()
	{
		m_ScreenPivot.InputEnabled = true;
		base.OnViewEnable();
	}

	protected override void OnViewDisable()
	{
		m_ScreenPivot.InputEnabled = false;
		base.OnViewDisable();
	}

	void UpdateHeader()
	{
		List<FriendList.FriendInfo> allFriends = GameCloudManager.friendList.friends;
		bool noFriends = allFriends == null || allFriends.Count <= 0;
		m_SelectFriends.SetDisabled(noFriends);
		bool showFriendName = (m_Mode == Statistics.E_Mode.CompareWithBest);
		m_FriendNameLabel.Widget.FadeAlpha = showFriendName ? 1.0f : 0.5f;
	}

	// =========================================================================================================================
	// === delegates ===========================================================================================================
	void OnSelectFriend(GUIBase_Widget inInstigator)
	{
		Owner.ShowPopup("SelectFriend", string.Empty, string.Empty, SelectFriendPopupHandler);
	}

	void SelectFriendPopupHandler(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		SelectFriendDialog popup = inPopup as SelectFriendDialog;
		if (popup == null)
		{
			Debug.LogError("This handler can be used only with SelectFriendDialog !!!");
			return;
		}

		if (popup.selectedFriend == SelectFriendDialog.SHOW_BEST_RESULTS)
		{
			m_StatisticsView.SetStatisticsMode(Statistics.E_Mode.CompareWithBest, "");
		}
		else
		{
			m_StatisticsView.SetStatisticsMode(Statistics.E_Mode.CompareWithFriend, popup.selectedFriend);
		}
	}
}
