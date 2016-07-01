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

public enum E_InputPriority : byte
{
	System = 0,
	Debug = 10,
	Menu = 20,
	Hud = 30,
	Game = 40
}

public enum E_InputOpacity : byte
{
	Opaque,
	SemiTransparent,
	Transparent
}

public abstract class InputController
{
	// PRIVATE MEMBERS

	bool m_CaptureInput;
	bool m_IsActive;

	// PUBLIC MEMBERS

	public int Priority = (int)E_InputPriority.Game;
	public E_InputOpacity Opacity = E_InputOpacity.Opaque;

	// GETTERS/SETTERS

	public bool CaptureInput
	{
		get { return m_CaptureInput; }
		set
		{
			if (m_CaptureInput == value)
				return;

			InputManager.FlushInput();

			m_CaptureInput = value;
		}
	}

	public bool IsActive
	{
		get { return m_IsActive; }
		set
		{
			if (m_IsActive == value)
				return;

			InputManager.FlushInput();

			m_IsActive = value;

			if (m_IsActive == true)
			{
				OnActivate();
			}
			else
			{
				OnDeactivate();
			}
		}
	}

	// ABSTRACT INTERFACE

	protected abstract void OnActivate();
	protected abstract void OnDeactivate();
	protected abstract bool OnProcess(ref IInputEvent evt);

	// PUBLIC METHODS

	public bool Process(ref IInputEvent evt)
	{
		return OnProcess(ref evt);
	}
}
