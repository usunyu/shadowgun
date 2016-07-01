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

[AddComponentMenu("GUI/Frontend/GuiFrontendLoading")]
public class GuiFrontendLoading : GuiFrontend<GuiFrontendLoading.E_MenuState>
{
	public enum E_MenuState
	{
		Idle,
		Loading
	}

	// PRIVATE MEMBERS

	// we have to delay initialization, unfortuantely some dependent classes are not initialized in our init...
	int m_HACK_FrameCount = 1;

	// MONOBEHAVIOUR INTERFACE

	void Start()
	{
		// register menus for states
		RegisterMenu<GuiMenuLoading>(E_MenuState.Loading);
	}

	void LateUpdate()
	{
		if (CurrentState == E_MenuState.Loading)
			return;
		if (--m_HACK_FrameCount > 0)
			return;

		// open loading menu
		GuiMenu menu = SetState(E_MenuState.Loading);
		if (menu != null)
		{
			if (menu.IsInitialized == false)
			{
				menu.InitMenu(this);
			}

			menu.ShowMenu();
		}
	}
}
