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

[AddComponentMenu("GUI/Frontend/Menus/GuiMenuPause")]
public class GuiMenuPause : GuiMenuIngame
{
	// ISCREENOWNER INTERFACE

	protected override void OnMenuInit()
	{
		base.OnMenuInit();

		_CreateScreen<NewFriendDialog>("NewFriend");
	}

	protected override void OnMenuShowMenu()
	{
		base.OnMenuShowMenu();

		Game.Instance.GameState = E_GameState.IngameMenu;
	}

	protected override void OnMenuHideMenu()
	{
		Game.Instance.GameState = E_GameState.Game;

		base.OnMenuHideMenu();
	}

	public override void DoCommand(string inCommand)
	{
		switch (inCommand)
		{
		case "ResumeGame":
			GuiFrontendIngame.HidePauseMenu();
			break;
		default:
			base.DoCommand(inCommand);
			break;
		}
	}

	protected override void OnMenuUpdate()
	{
#if !MADFINGER_KEYBOARD_MOUSE
		if ((GamepadInputManager.Instance != null && GamepadInputManager.Instance.ControlDown(PlayerControlsGamepad.E_Input.Fire))
#if UNITY_ANDROID && !UNITY_EDITOR
			|| (MogaGamepad.IsConnected() && MogaGamepad.GetKeyCode(Moga.KEYCODE_BUTTON_A) == Moga.ACTION_DOWN) 
			|| (Input.GetKeyDown("8") || Input.GetKeyDown("1")) //bluetooth
			|| (GamepadInputManager.Instance.IsNvidiaShield() && Input.GetKeyDown(KeyCode.Joystick1Button0))
#endif
						)
		{
			DoCommand("ResumeGame");
		}
#endif
	}

	// GUIMENU INTERFACE

	protected override bool ProcessMenuInput(ref IInputEvent evt)
	{
		if (base.ProcessMenuInput(ref evt) == true)
			return true;

		if (evt.Kind == E_EventKind.Key)
		{
			KeyEvent key = (KeyEvent)evt;
			if (key.Code == KeyCode.Escape)
			{
				if (key.State == E_KeyState.Released)
				{
					DoCommand("ResumeGame");
				}
				return true;
			}
		}

		return false;
	}
}
