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

public class GuiPopupNewFeaturesDialog : GuiPopup
{
	public readonly static int FEATURES_VERSION = 2;

	public override bool CanCloseByEscape
	{
		get { return true; }
	}

	public override void SetCaption(string inCaption)
	{
	}

	public override void SetText(string inText)
	{
	}

	protected override void OnViewInit()
	{
		base.OnViewInit();

		if (m_ScreenLayout == null)
		{
			Debug.LogError("GuiPopupNewFeaturesDialog<" + name + "> :: There is not any layout specified for message box!");
			return;
		}

		PrepareButton(m_ScreenLayout, "OK_Button", null, (inWidget) => { SendResult(E_PopupResultCode.Ok); });
	}
}
