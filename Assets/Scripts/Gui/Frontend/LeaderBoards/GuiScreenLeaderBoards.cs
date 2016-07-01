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

public class GuiScreenLeaderBoards : GuiScreenModal, IGuiOverlayScreen
{
	GUIBase_Button m_Button_Overall;
	GUIBase_Button m_Button_Day;
	GUIBase_Button m_Button_Week;
	GUIBase_Button m_Button_Friends;
	GUIBase_Button m_Button_Facebook;

	GUIBase_Widget m_ActiveLeaderBoard_Widget;

	LeaderBoard m_LeaderBoard_Overall;
	LeaderBoard m_LeaderBoard_Day;
	LeaderBoard m_LeaderBoard_Week;
	LeaderBoard m_LeaderBoard_Friends;

	LeaderBoardListView m_LeaderBoardView;

	// =================================================================================================================

	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_Button_Overall = PrepareButton("01_Button", null, OnSelectLeaderBoard, true, true);
		m_Button_Friends = PrepareButton("04_Button", null, OnSelectLeaderBoard, true, true);

		m_Button_Day = PrepareButton("02_Button", null, OnSelectLeaderBoard, true, true);
		m_Button_Week = PrepareButton("03_Button", null, OnSelectLeaderBoard, true, true);

		m_LeaderBoard_Overall = new LeaderBoard("Default", 8);
		m_LeaderBoard_Day     = new LeaderBoard("Daily", 8);
		m_LeaderBoard_Week    = new LeaderBoard("Default", 8);
		m_LeaderBoard_Friends = new LeaderBoardFriends("Default", 8);

//      m_LeaderBoard_Overall = new LeaderBoard("Overall", 8);
//		m_LeaderBoard_Day     = new LeaderBoard("Day"    , 8);
//      m_LeaderBoard_Week    = new LeaderBoard("Week"   , 8);
//		m_LeaderBoard_Friends = new LeaderBoard("Friends", 8);

		// initialize view
		{
			GUIBase_Layout l = GetLayout("MainMenu", "LeaderBoards_Layout");
			GUIBase_List list = GetWidget(l, "Table").GetComponent<GUIBase_List>();

			m_LeaderBoardView = gameObject.AddComponent<LeaderBoardListView>();
			m_LeaderBoardView.GUIView_Init(l, list);
		}
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		OnSelectLeaderBoard(m_ActiveLeaderBoard_Widget ?? m_Button_Overall.Widget);

		m_LeaderBoardView.GUIView_Show();
	}

	protected override void OnViewUpdate()
	{
		base.OnViewUpdate();

		m_LeaderBoardView.GUIView_Update();
	}

	// IGUIOVERLAYSCREEN INTERFACE

	string IGuiOverlayScreen.OverlayButtonTooltip
	{
		get { return null; }
	}

	bool IGuiOverlayScreen.HideOverlayButton
	{
		get { return Ftue.IsActionIdle<FtueAction.Leaderboards>(); }
	}

	bool IGuiOverlayScreen.HighlightOverlayButton
	{
		get { return Ftue.IsActionActive<FtueAction.Leaderboards>(); }
	}

	// =================================================================================================================
	// ===  Delegates  =================================================================================================

	void OnSelectLeaderBoard(GUIBase_Widget inWidget)
	{
		UpdateButtonStatus(inWidget);
		m_ActiveLeaderBoard_Widget = inWidget;

		if (inWidget == m_Button_Overall.Widget)
		{
			m_LeaderBoardView.SetActiveLeaderBoard(m_LeaderBoard_Overall, false);
		}
		else if (inWidget == m_Button_Day.Widget)
		{
			m_LeaderBoardView.SetActiveLeaderBoard(m_LeaderBoard_Day, false);
		}
		else if (inWidget == m_Button_Week.Widget)
		{
			m_LeaderBoardView.SetActiveLeaderBoard(m_LeaderBoard_Week, false);
		}
		else if (inWidget == m_Button_Friends.Widget)
		{
			m_LeaderBoardView.SetActiveLeaderBoard(m_LeaderBoard_Friends, false);
		}
		else
		{
			m_ActiveLeaderBoard_Widget = null;
			Debug.LogWarning("OnSelectLeaderBoard - Unknown leaderboard");
		}
	}

	// =================================================================================================================
	// ===  internal  ==================================================================================================
	void UpdateButtonStatus(GUIBase_Widget inWidget)
	{
		m_Button_Overall.ForceDownStatus(inWidget == m_Button_Overall.Widget);
		m_Button_Day.ForceDownStatus(inWidget == m_Button_Day.Widget);
		m_Button_Week.ForceDownStatus(inWidget == m_Button_Week.Widget);
		m_Button_Friends.ForceDownStatus(inWidget == m_Button_Friends.Widget);
	}
}
