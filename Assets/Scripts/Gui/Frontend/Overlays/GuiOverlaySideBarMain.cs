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

[AddComponentMenu("GUI/Frontend/Overlays/GuiOverlaySideBarMain")]
public class GuiOverlaySideBarMain : GuiOverlaySideBar
{
#if UNITY_STANDALONE
	protected override bool ShouldDisplayButton(GUIBase_Button button)
	{
		if (button.name == "FreeGold_Button")
			return false;

		if (button.name == "InviteFB_Button")
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
			return false;
#else
			return Ftue.IsActionFinished<FtueAction.Friends>();
#endif

		return base.ShouldDisplayButton(button);
	}
#else
	protected override bool ShouldDisplayButton(GUIBase_Button button)
	{
		if (button.name == "InviteFB_Button")
			return false;

		return base.ShouldDisplayButton(button);
	}
#endif
}
