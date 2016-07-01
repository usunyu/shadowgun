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

[AddComponentMenu("GUI/Frontend/Menus/GuiMenuLoading")]
public class GuiMenuLoading : GuiMenu
{
	// CONFIGURATION

	[SerializeField] GuiScreen[] m_Screens = new GuiScreen[0];
#pragma warning disable 414
	[SerializeField] GuiScreen[] m_ScreensPC = new GuiScreen[0];
#pragma warning restore 414

	// GUIMENU INTERFACE

	protected override void OnMenuInit()
	{
	}

	protected override void OnMenuUpdate()
	{
	}

	protected override void OnMenuShowMenu()
	{
		GuiScreen screen = 
#if MADFINGER_KEYBOARD_MOUSE
			m_ScreensPC.Length > 0 ? m_ScreensPC[Random.Range(0, m_ScreensPC.Length)] :
				m_Screens[Random.Range(0, m_Screens.Length)];
#else
						m_Screens[Random.Range(0, m_Screens.Length)];
#endif

		ShowScreen(GetScreenName(screen));
	}

	protected override void OnMenuHideMenu()
	{
	}

	protected override void OnMenuRefreshMenu(bool anyPopupVisible)
	{
	}

	// ISCREENOWNER INTERFACE

	public override void Exit()
	{
	}
}
