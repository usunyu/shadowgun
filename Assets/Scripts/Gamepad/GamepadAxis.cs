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

//all available axis
public enum E_JoystickAxis
{
	NONE = -1,
	JoystickUp = 0,
	JoystickDown,
	JoystickLeft,
	JoystickRight,
	Joystick_3a,
	Joystick_3b,
	Joystick_4a,
	Joystick_4b,
	Joystick_5a,
	Joystick_5b,
	Joystick_6a,
	Joystick_6b,
	Joystick_7a,
	Joystick_7b,
	Joystick_8a,
	Joystick_8b,
	COUNT //last
};

public class GamepadAxis
{
	class JAxis
	{
		public JAxis(string _axis, string _name)
		{
			axis = _axis;
			name = _name;
		}

		public string axis;
		public string name;
	};

	static JAxis[] JoyAxis = new JAxis[(int)E_JoystickAxis.COUNT]
	{
		new JAxis("JoystickUp", "Up"),
		new JAxis("JoystickDown", "Down"),
		new JAxis("JoystickLeft", "Left"),
		new JAxis("JoystickRight", "Right"),
		new JAxis("Joystick_3a", "Axis 3 +"),
		new JAxis("Joystick_3b", "Axis 3 -"),
		new JAxis("Joystick_4a", "Axis 4 +"),
		new JAxis("Joystick_4b", "Axis 4 -"),
		new JAxis("Joystick_5a", "Axis 5 +"), //comment this out to support the xbox360 controller on the MAC (shoulder buttons)
		new JAxis("Joystick_5b", "Axis 5 -"),
		new JAxis("Joystick_6a", "Axis 6 +"), //comment this out to support the xbox360 controller on the MAC (shoulder buttons)
		new JAxis("Joystick_6b", "Axis 6 -"),
		new JAxis("Joystick_7a", "Axis 7 +"),
		new JAxis("Joystick_7b", "Axis 7 -"),
		new JAxis("Joystick_8a", "Axis 8 +"),
		new JAxis("Joystick_8b", "Axis 8 -"),
	};

	public static string GetAxis(E_JoystickAxis axis)
	{
		if (axis > E_JoystickAxis.NONE && axis < E_JoystickAxis.COUNT)
			return JoyAxis[(int)axis].axis;
		else
			return "";
	}

	public static string GetAxisLabel(E_JoystickAxis axis)
	{
		if (axis > E_JoystickAxis.NONE && axis < E_JoystickAxis.COUNT)
			return JoyAxis[(int)axis].name;
		else
			return "";
	}
}
