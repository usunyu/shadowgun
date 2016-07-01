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
public class InGameScreen : GuiScreen
{
	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_ScreenPivot = GetPivot("IngameMenu");
		m_ScreenLayout = GetLayout("IngameMenu", "Menu_Layout");

		PrepareButton(m_ScreenLayout, "Resume_Button", null, ResumeButtonDelegate);
		PrepareButton(m_ScreenLayout, "Opt_Button", null, OptButtonDelegate);
		ButtonDisable(m_ScreenLayout, "Opt_Button", true);

		PrepareButton(m_ScreenLayout, "Equip_Button", null, EquipButtonDelegate);
		ButtonDisable(m_ScreenLayout, "Equip_Button", true);

		PrepareButton(m_ScreenLayout, "HiddenOptions_Demo", null, OptButtonDelegate); //HACK pro demo

		PrepareButton(m_ScreenLayout, "Help_Button", null, HelpButtonDelegate);
		PrepareButton(m_ScreenLayout, "Exit_Button", null, ExitButtonDelegate);
	}

	protected override void OnViewShow()
	{
		MFGuiManager.Instance.ShowPivot(m_ScreenPivot, true);

		base.OnViewShow();
	}

	protected override void OnViewHide()
	{
		MFGuiManager.Instance.ShowPivot(m_ScreenPivot, false);

		base.OnViewHide();
	}

	// #################################################################################################################
	// ###  Delegates  #################################################################################################
	void ResumeButtonDelegate(GUIBase_Widget inInstigator)
	{
		Owner.DoCommand("ResumeGame");
	}

	void OptButtonDelegate(GUIBase_Widget inInstigator)
	{
		Owner.ShowScreen("Options");
	}

	void EquipButtonDelegate(GUIBase_Widget inInstigator)
	{
		Owner.ShowScreen("Equip");
	}

	void HelpButtonDelegate(GUIBase_Widget inInstigator)
	{
		Owner.ShowScreen("Help");
	}

	void ExitButtonDelegate(GUIBase_Widget inInstigator)
	{
		Owner.Exit();
	}

	// #################################################################################################################		
}
