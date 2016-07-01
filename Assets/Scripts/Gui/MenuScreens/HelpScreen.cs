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
public class HelpScreen : GuiScreen
{
	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_ScreenPivot = GetPivot("MainHelp");
		m_ScreenLayout = GetLayout("MainHelp", "01Buttons_Layout");

		PrepareButton(m_ScreenLayout, "Back_Button", null, OnHelpButtonBack);
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
	void OnHelpButtonBack(GUIBase_Widget inInstigator)
	{
		Owner.Back();
	}

	// #################################################################################################################		
}
