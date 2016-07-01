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

public abstract class GuiScreenModal : GuiScreenMultiPage
{
	readonly static string CLOSE_BUTTON = "Close_Button";

	protected override void OnViewShow()
	{
		base.OnViewShow();

		GuiMenu menu = Owner as GuiMenu;
		if (menu != null)
		{
			menu.SetOverlaysEnabled(false);
			menu.SetBackgroundVisibility(false);
		}

		RegisterButtonDelegate(CLOSE_BUTTON, null, (inside) => { Owner.Back(); });
	}

	protected override void OnViewHide()
	{
		RegisterButtonDelegate(CLOSE_BUTTON, null, null);

		GuiMenu menu = Owner as GuiMenu;
		if (menu != null)
		{
			menu.SetOverlaysEnabled(true);
			menu.SetBackgroundVisibility(true);
		}

		base.OnViewHide();
	}

	protected override bool OnViewProcessInput(ref IInputEvent evt)
	{
		if (evt.Kind == E_EventKind.Key)
		{
			KeyEvent key = (KeyEvent)evt;
			if (key.Code == KeyCode.Escape)
			{
				if (key.State == E_KeyState.Released && Owner != null)
				{
					Owner.Back();
				}
				return true;
			}
		}

		return base.OnViewProcessInput(ref evt);
	}
}
