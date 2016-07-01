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

[AddComponentMenu("GUI/Frontend/OptionPages/GuiPageOptionsExperimental")]
public class GuiPageOptionsExperimental : GuiScreen
{
	readonly static string FLOATING_FIRE = "FloatingFire_Switch";

	// PRIVATE MEMBERS

	GUIBase_Switch m_SwitchFloatingFire;

	// GUIVIEW INTERFACE

	protected override void OnViewInit()
	{
		m_SwitchFloatingFire = GuiBaseUtils.GetControl<GUIBase_Switch>(Layout, FLOATING_FIRE);
	}

	protected override void OnViewShow()
	{
		BindControls(true);
	}

	protected override void OnViewHide()
	{
		BindControls(false);
	}

	protected override void OnViewReset()
	{
		m_SwitchFloatingFire.SetValue(GuiOptions.floatingFireButton);
	}

	// HANDLERS

	void OnSwitchFloatingFire(bool state)
	{
		GuiOptions.floatingFireButton = state;

		if (GuiHUD.Instance != null)
		{
			GuiHUD.Instance.UpdateAttackButtonSettings();
		}
	}

	// PRIVATE METHODS

	void BindControls(bool state)
	{
		// bind callbacks
		m_SwitchFloatingFire.RegisterDelegate(state ? OnSwitchFloatingFire : (GUIBase_Switch.SwitchDelegate)null);
	}
}
