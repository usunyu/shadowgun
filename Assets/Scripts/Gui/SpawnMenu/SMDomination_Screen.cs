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

public class SMDomination_Screen : GuiScreenMultiPage
{
	// GUIVIEW INTERFACE

	protected override void OnViewInit()
	{
		base.OnViewInit();

		Client client = Client.Instance;
		if (client == null)
			return;
		if (client.GameState.GameType != E_MPGameType.ZoneControl)
			return;

		RegisterComponent<ScreenComponent_Map2>();
		RegisterComponent<ScreenComponent_ShortStats>();

		for (int idx = 0; idx < 2; ++idx)
		{
			int pageIdx = idx;
			GuiBaseUtils.RegisterButtonDelegate(Layout, string.Format("Tab{0}_Button", idx + 1), () => { GotoPage(pageIdx); }, null);
		}
	}

	// GUISCREENMULTIPAGE INTERFACE

	protected override void OnPageVisible(GuiScreen page)
	{
		for (int idx = 0; idx < 2; ++idx)
		{
			bool highlight = idx == CurrentPageIndex;

			GUIBase_Button button = GuiBaseUtils.GetControl<GUIBase_Button>(Layout, string.Format("Tab{0}_Button", idx + 1));
			button.stayDown = highlight;
			button.ForceDownStatus(highlight);
		}
	}
}
