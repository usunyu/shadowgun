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
using System.Collections.Generic;

public class InputDriverKeyboard : InputDriver
{
	readonly static float DELAY_BEFORE_PRESSED = 0.0f;
	readonly static float DELAY_BEFORE_REPEATING = 0.0f;
	readonly static float DELAY_BETWEEN_REPEATING = 0.0f;

	// PRIVATE MEMBERS

	InputManager m_Owner;
	KeyEvent[] m_Keys;
	bool m_WasPressed;

	// INPUTDRIVER INTERFACE

	internal override void Initialize(InputManager manager)
	{
		m_Owner = manager;

		List<string> names = new List<string>(System.Enum.GetNames(typeof (KeyCode)));
		List<KeyEvent> tmp = new List<KeyEvent>();
		for (KeyCode code = KeyCode.Backspace; code <= KeyCode.Menu; ++code)
		{
			// key codes returned on OSX (Russian) are different from the key codes on the other platforms
			// you can not test keys by enum value (keyCode == KeyCode.W) because integer value is different
			// so on OSX (Russian) are included all integer values between defined codes not only enum values
			// this solution is no ideal (general) but works
			// if returned key codes will be outside defined codes this solution will not work
			if (!InputDriverKeyboard.IsOSXRussianWrongKeyCodesKeyboard)
			{
				if (names.Contains(code.ToString()) == false)
					continue;
			}

			tmp.Add(new KeyEvent()
			{
				Code = code,
				State = E_KeyState.Released
			});
		}
		m_Keys = tmp.ToArray();
	}

	internal override void Deinitialize(InputManager manager)
	{
		Flush();

		m_Keys = null;
		m_Owner = null;
	}

	internal override void Update()
	{
		if (Input.anyKey == false && m_WasPressed == false)
			return;
		m_WasPressed = false;

		float time = Time.time;

		for (int idx = 0; idx < m_Keys.Length; ++idx)
		{
			KeyEvent evt = m_Keys[idx];

			bool pressed = Input.GetKey(evt.Code);
			bool modified = false;

			m_WasPressed |= pressed;

			switch (evt.State)
			{
			case E_KeyState.Released:
				if (pressed == true)
				{
					float delta = time - evt.EndTime;
					if (delta >= DELAY_BEFORE_PRESSED)
					{
						evt.State = E_KeyState.Pressed;
						evt.StartTime = time;
						evt.EndTime = 0.0f;
						modified = true;
					}
				}
				break;
			case E_KeyState.Pressed:
				if (pressed == true)
				{
					float delta = time - evt.StartTime;
					if (delta >= DELAY_BEFORE_REPEATING)
					{
						evt.State = E_KeyState.Repeating;
						evt.StartTime = time;
						modified = true;
					}
				}
				else
				{
					evt.State = E_KeyState.Released;
					evt.EndTime = time;
					modified = true;
				}
				break;
			case E_KeyState.Repeating:
				if (pressed == true)
				{
					float delta = time - evt.StartTime;
					if (delta >= DELAY_BETWEEN_REPEATING)
					{
						evt.StartTime = time;
						modified = true;
					}
				}
				else
				{
					evt.State = E_KeyState.Released;
					evt.EndTime = time;
					modified = true;
				}
				break;
			default:
				throw new System.IndexOutOfRangeException();
			}

			if (modified == true)
			{
				m_Keys[idx] = evt;
				m_Owner.Process(evt);
			}
		}
	}

	internal override void Flush()
	{
		float time = Time.time;

		for (int idx = 0; idx < m_Keys.Length; ++idx)
		{
			KeyEvent evt = m_Keys[idx];

			if (evt.State == E_KeyState.Released)
				continue;

			evt.State = E_KeyState.Released;
			evt.EndTime = time;

			m_Keys[idx] = evt;
			m_Owner.Process(evt);
		}
	}

	// key codes returned on OSX (Russian, Ukrainian) are different from the key codes on the other platforms
	// you can not test keys by enum value (keyCode == KeyCode.W) because integer value is different
	public static bool IsOSXRussianWrongKeyCodesKeyboard
	{
		get { return PlatformHelper.IsOSXPlayerOrOSXEditorRussianOrUkrainian; }
	}
}
