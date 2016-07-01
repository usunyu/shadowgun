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

[AddComponentMenu("GUI/Frontend/Menus/GuiMenuFinal")]
public class GuiMenuFinal : GuiMenu
{
	protected override void OnMenuInit()
	{
	}

	protected override void OnMenuUpdate()
	{
	}

	protected override void OnMenuShowMenu()
	{
		ShowScreen("FinalResults");
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
		// user is about to leave the game
		// ask him if we can do that
		ShowConfirmDialog(0106000, 0106011, OnLeaveConfirmation);
	}

	// HANDLERS

	void OnLeaveConfirmation(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		if (inResult != E_PopupResultCode.Ok)
			return;

		//Debug.Log("Exiting game");

		GuiFrontendIngame.GotoMainMenu();
	}
}
