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

public abstract class GuiMenuIngame : GuiMenu
{
	// PRIVATE MEMBERS

	bool m_DebugLevel = false;
	int m_ActiveSpawnScreen = -1;

	// PUBLIC METHODS

	public string GetGameScreenNameByGameType()
	{
		if (m_DebugLevel == true)
		{
			return ((++m_ActiveSpawnScreen)%2 == 0) ? "Domination" : "DeadtMatch";
		}

		// Set Map Mode name ...
		if (Client.Instance != null)
		{
			switch (Client.Instance.GameState.GameType)
			{
			case E_MPGameType.DeathMatch:
				return "DeadMatch";
			case E_MPGameType.ZoneControl:
				return "Domination";
			default:
				break;
			}
		}

		return "UNKNOWN_GAME_TYPE";
	}

	// GUIMENU INTERFACE

	protected override void OnMenuInit()
	{
		m_DebugLevel = ApplicationDZ.loadedLevelName == "SpawnMenu";
	}

	protected override void OnMenuShowMenu()
	{
		ShowScreen(GetGameScreenNameByGameType());
	}

	protected override void OnMenuHideMenu()
	{
	}

	protected override void OnMenuRefreshMenu(bool anyPopupVisible)
	{
	}

	protected override string FixActiveScreenName(string screenName)
	{
		if (screenName == GetGameScreenNameByGameType())
			return "Game";
		return base.FixActiveScreenName(screenName);
	}

	// ISCREENOWNER INTERFACE

	public override void ShowScreen(string inScreenName, bool inClearStack = false)
	{
		switch (inScreenName)
		{
		case "Game":
			break;
		default:
			base.ShowScreen(inScreenName, inClearStack);
			break;
		}
	}

	public override void Exit()
	{
		// user is about to leave the game
		// ask him if we can do that
		ShowPopup("ConfirmDialogBig", TextDatabase.instance[0106000], TextDatabase.instance[0106012], OnLeaveConfirmation);
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
