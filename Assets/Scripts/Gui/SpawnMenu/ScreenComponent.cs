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

public abstract class ScreenComponent : GuiComponent<GuiScreen>
{
	// GETTERS / SETTERS

	public GUIBase_Widget Parent { get; private set; }

	// ABSTRACT INTERFACE

	public abstract string ParentName { get; }

	protected virtual bool OnProcessInput(ref IInputEvent evt)
	{
		return false;
	}

	// PUBLIC METHODS

	public bool ProcessInput(ref IInputEvent evt)
	{
		return OnProcessInput(ref evt);
	}

	// GUICOMPONENT INTERFACE

	protected override bool OnInit()
	{
		if (base.OnInit() == false)
			return false;

		if (string.IsNullOrEmpty(ParentName) == true)
			return false;

		Parent = Owner.GetWidget(Owner.Layout, ParentName);
		if (Parent == null)
			return false;

		return true;
	}
}
