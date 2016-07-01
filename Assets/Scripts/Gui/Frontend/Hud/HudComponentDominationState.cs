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

public class GadgetZoneControlState
{
	// PRIVATE MEMBERS

	GUIBase_Widget m_Root;

	GUIBase_Label m_BlueTickets;
	GUIBase_Label m_BlueState;

	GUIBase_Label m_RedTickets;
	GUIBase_Label m_RedState;

	// PUBLIC MEMBERS

	public bool IsVisible
	{
		get { return m_Root.Visible; }
		set { m_Root.Show(value, true); }
	}

	// C-TOR

	public GadgetZoneControlState(GUIBase_Widget root)
	{
		m_Root = root;

		m_BlueTickets = GuiBaseUtils.GetChild<GUIBase_Label>(m_Root, "BlueSpawns");
		m_BlueState = GuiBaseUtils.GetChild<GUIBase_Label>(m_Root, "BlueStand");

		m_RedTickets = GuiBaseUtils.GetChild<GUIBase_Label>(m_Root, "RedSpawns");
		m_RedState = GuiBaseUtils.GetChild<GUIBase_Label>(m_Root, "RedStand");

		m_BlueTickets.Widget.Color = ZoneControlFlag.Colors[E_Team.Good];
		m_BlueState.Widget.Color = ZoneControlFlag.Colors[E_Team.Good];
		m_RedTickets.Widget.Color = ZoneControlFlag.Colors[E_Team.Bad];
		m_RedState.Widget.Color = ZoneControlFlag.Colors[E_Team.Bad];
	}

	// PUBLIC METHOD

	public void Update()
	{
		PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPlayerPPI();
		if (ppi == null)
			return;

		int blueScore = Client.Instance.GameState.ZCInfo.TeamScore[E_Team.Good];
		int redScore = Client.Instance.GameState.ZCInfo.TeamScore[E_Team.Bad];

		SetText(m_BlueTickets, blueScore.ToString("D2"));
		SetText(m_RedTickets, redScore.ToString("D2"));

		ShowText(m_BlueState, ppi.Team == E_Team.Good);
		ShowText(m_RedState, ppi.Team == E_Team.Bad);
	}

	void SetText(GUIBase_Label label, string text)
	{
		if (label != null)
		{
			label.SetNewText(text);
		}
	}

	void ShowText(GUIBase_Label label, bool state)
	{
		if (label != null && label.Widget != null)
		{
			label.Widget.Show(state, true);
		}
	}
}

public class HudComponentDominationState : HudComponent
{
	string s_PivotMainName = "MainHUD";
	string s_LayoutMainName = "HUD_Layout";
	string s_Parent = "Domination_State";

	GadgetZoneControlState m_Gadget;

	// ---------------------------------------------------------------------------------------------------------------------------------
	// 						P U B L I C      P A R T
	// ---------------------------------------------------------------------------------------------------------------------------------
	// ---------
	protected override bool OnInit()
	{
		if (base.OnInit() == false)
			return false;
		if (Client.Instance == null)
			return false;
		if (Client.Instance.GameState.GameType != E_MPGameType.ZoneControl)
			return false;

		GUIBase_Pivot pivot = MFGuiManager.Instance.GetPivot(s_PivotMainName);
		GUIBase_Layout layout = pivot.GetLayout(s_LayoutMainName);

		m_Gadget = new GadgetZoneControlState(layout.GetWidget(s_Parent));

		return true;
	}

	// ---------
	public override float UpdateInterval
	{
		get { return 1.0f; }
	}

	// ---------
	protected override void OnUpdate()
	{
		base.OnUpdate();

		m_Gadget.Update();
	}

	// -----
	protected override void OnShow()
	{
		base.OnShow();

		m_Gadget.IsVisible = Client.Instance.GameState.GameType == E_MPGameType.ZoneControl;
		m_Gadget.Update();
	}

	// -----
	protected override void OnHide()
	{
		m_Gadget.IsVisible = false;

		base.OnHide();
	}
}
