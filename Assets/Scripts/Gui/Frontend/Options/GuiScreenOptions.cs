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
using System.Collections.Generic;

[AddComponentMenu("GUI/Frontend/Screens/GuiScreenOptions")]
public class GuiScreenOptions : GuiScreenMultiPage, IGuiOverlayScreen
{
	readonly static string[] BUTTONS = {"Controls_Button", "Display_Button", "Sounds_Button", "Experimental_Button"};
	readonly static string RESET = "Reset_Button";

	// PRIVATE MEMBERS

	GUIBase_Button[] m_Buttons = new GUIBase_Button[BUTTONS.Length];

	// IGUIOVERLAYSCREEN INTERFACE

	string IGuiOverlayScreen.OverlayButtonTooltip
	{
		get { return null; }
	}

	bool IGuiOverlayScreen.HideOverlayButton
	{
		get { return Ftue.IsActionIdle<FtueAction.Controls>(); }
	}

	bool IGuiOverlayScreen.HighlightOverlayButton
	{
		get { return Ftue.IsActionActive<FtueAction.Controls>(); }
	}

	// GUISCREENMULTIPAGE INTERFACE

	protected override void OnPageVisible(GuiScreen page)
	{
		if (CurrentPageIndex < 0 || CurrentPageIndex >= m_Buttons.Length)
			return;

		m_Buttons[CurrentPageIndex].stayDown = true;
		m_Buttons[CurrentPageIndex].ForceDownStatus(true);
	}

	protected override void OnPageHiding(GuiScreen page)
	{
		if (CurrentPageIndex < 0 || CurrentPageIndex >= m_Buttons.Length)
			return;

		m_Buttons[CurrentPageIndex].stayDown = false;
		m_Buttons[CurrentPageIndex].ForceDownStatus(false);
	}

	// GUISCREEN INTERFACE

	protected override void OnViewInit()
	{
		base.OnViewInit();

		for (int idx = 0; idx < m_Buttons.Length; ++idx)
		{
			m_Buttons[idx] = GuiBaseUtils.GetControl<GUIBase_Button>(Layout, BUTTONS[idx]);
		}
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		// bind buttons
		for (int idx = 0; idx < m_Buttons.Length; ++idx)
		{
			int pageId = idx;
			GuiBaseUtils.RegisterButtonDelegate(m_Buttons[idx],
												null,
												(inside) =>
												{
													if (inside == true)
													{
														GotoPage(pageId);
													}
												});
		}
		RegisterButtonDelegate(RESET, null, OnResetPressed);

#if UNITY_STANDALONE //experimental buttons
		m_Buttons[3].Widget.Show(false, true);
#endif
		//disable experimental on shield
		if (GamepadInputManager.Instance.IsNvidiaShield())
		{
			m_Buttons[3].Widget.Show(false, true);
		}
	}

	protected override void OnViewHide()
	{
		// unbind buttons
		for (int idx = 0; idx < m_Buttons.Length; ++idx)
		{
			GuiBaseUtils.RegisterButtonDelegate(m_Buttons[idx], null, null);
		}
		RegisterButtonDelegate(RESET, null, null);

		// store options
		GuiOptions.Save();

		// call super
		base.OnViewHide();
	}

	// HANDLERS

	void OnResetPressed(bool inside)
	{
		if (inside == false)
			return;

		GuiOptions.ResetToDefaults();
		ApplyGraphicsOptions();

		if (GuiHUD.Instance != null)
		{
			GuiHUD.Instance.UpdateAttackButtonSettings();
		}

		ResetPage();
	}

	void ApplyGraphicsOptions()
	{
#if UNITY_EDITOR
		DeviceInfo.Initialize((DeviceInfo.Performance)GuiOptions.graphicDetail);
#endif
	}
}
