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

public abstract class GuiOverlay : GuiView
{
	// PRIVATE MEMBERS

	[SerializeField] GUIBase_Layout m_Layout;

	// GETTERS / SETTERS

	public override GUIBase_Layout Layout
	{
		get { return m_Layout; }
	}

	// ABSTRACT INTERFACE

	protected virtual void OnActiveScreen(string screenName)
	{
	}

	// PUBLIC METHODS

	public void SetActiveScreen(string screenName)
	{
		if (IsInitialized == false)
			return;
		if (Owner == null)
			return;
		if (IsVisible == false)
			return;

		OnActiveScreen(screenName);
	}

	// GUIVIEW INTERFACE

	protected override GUIBase_Widget OnViewHitTest(ref Vector2 point)
	{
		if (Owner == null)
			return null;

		GUIBase_Widget widget = base.OnViewHitTest(ref point);
		if (widget != null)
			return widget;

		if (Layout.Visible == false)
			return null;
		if (Layout.InputEnabled == false)
			return null;

		return Layout.HitTest(ref point);
	}

	protected override bool OnViewProcessInput(ref IInputEvent evt)
	{
		if (Owner == null)
			return false;

		if (Layout.Visible == false)
			return false;
		if (Layout.InputEnabled == false)
			return false;

		if (base.OnViewProcessInput(ref evt) == true)
			return true;

		return Layout.ProcessInput(ref evt);
	}
}
