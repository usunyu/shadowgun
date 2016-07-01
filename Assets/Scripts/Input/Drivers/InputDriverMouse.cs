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

public class InputDriverMouse : InputDriver
{
	readonly static int MAX_BUTTONS = 2;

	// PRIVATE MEMBERS

	InputManager m_Owner;
	MouseEvent m_Mouse;
	TouchEvent[] m_Buttons;

	// INPUTDRIVER INTERFACE

	internal override void Initialize(InputManager manager)
	{
		m_Owner = manager;

		// initialize mouse
		m_Mouse = new MouseEvent()
		{
			Phase = TouchPhase.Stationary,
			Buttons = new bool[MAX_BUTTONS]
		};

		// initialize buttons
		m_Buttons = new TouchEvent[MAX_BUTTONS];
		for (int idx = 0; idx < MAX_BUTTONS; ++idx)
		{
			m_Buttons[idx] = new TouchEvent()
			{
				Id = idx,
				Phase = TouchPhase.Canceled,
				Type = E_TouchType.MouseButton
			};
		}
	}

	internal override void Deinitialize(InputManager manager)
	{
		Flush();

		m_Buttons = null;
		m_Owner = null;
	}

	internal override void Update()
	{
		UpdateMouse();
		UpdateButtons();
	}

	internal override void Flush()
	{
		float time = Time.time;

		for (int idx = 0; idx < m_Buttons.Length; ++idx)
		{
			TouchEvent evt = m_Buttons[idx];
			if (evt.Finished == true)
				continue;

			evt.Phase = TouchPhase.Canceled;
			evt.EndTime = time;

			m_Buttons[idx] = evt;
			m_Owner.Process(evt);
		}
	}

	// PRIVATE METHODS

	void UpdateMouse()
	{
		float time = Time.time;
		Vector2 position = Input.mousePosition;
		try //get scroll wheel if it is present
		{
			if (Input.GetAxis("Mouse ScrollWheel") != 0)
			{
				m_Mouse.ScrollWheel = Input.GetAxis("Mouse ScrollWheel");
				m_Owner.Process(m_Mouse);
				m_Mouse.ScrollWheel = 0;
			}
		}
		catch
		{
			m_Mouse.ScrollWheel = 0;
		}

		if (m_Mouse.Position != position)
		{
			if (m_Mouse.Phase == TouchPhase.Stationary)
			{
				m_Mouse.StartPosition = position;
				m_Mouse.DeltaPosition = Vector2.zero;
				m_Mouse.StartTime = time;
				m_Mouse.DeltaTime = 0.0f;
			}
			else
			{
				m_Mouse.DeltaPosition = new Vector2(position.x - m_Mouse.Position.x, position.y - m_Mouse.Position.y);
				m_Mouse.DeltaTime = Time.deltaTime;
			}

			m_Mouse.Phase = TouchPhase.Moved;
			m_Mouse.Position = position;

			for (int idx = 0; idx < m_Mouse.Buttons.Length; ++idx)
			{
				m_Mouse.Buttons[idx] = Input.GetMouseButton(idx);
			}

			m_Owner.Process(m_Mouse);
		}
		else if (m_Mouse.Phase == TouchPhase.Moved)
		{
			m_Mouse.Phase = TouchPhase.Stationary;
			m_Mouse.Position = position;
			m_Mouse.DeltaPosition = Vector2.zero;
			m_Mouse.DeltaTime = Time.deltaTime;
			m_Mouse.EndTime = time;

			m_Owner.Process(m_Mouse);
		}
	}

	void UpdateButtons()
	{
		float time = Time.time;
		Vector2 position = Input.mousePosition;

		for (int idx = 0; idx < m_Buttons.Length; ++idx)
		{
			TouchEvent evt = m_Buttons[idx];

			bool pressed = Input.GetMouseButton(idx);
			bool modified = false;
			Vector2 delta = new Vector2(position.x - evt.Position.x, position.y - evt.Position.y);
			TouchPhase phase = delta.x != 0.0f && delta.y != 0.0f ? TouchPhase.Moved : TouchPhase.Stationary;

			switch (evt.Phase)
			{
			case TouchPhase.Canceled:
			case TouchPhase.Ended:
				if (pressed == true)
				{
					evt.Phase = TouchPhase.Began;
					evt.Position = position;
					evt.StartPosition = position;
					evt.DeltaPosition = Vector2.zero;
					evt.StartTime = time;
					evt.DeltaTime = 0.0f;
					evt.EndTime = 0.0f;
					modified = true;
				}
				break;
			case TouchPhase.Began:
			case TouchPhase.Moved:
			case TouchPhase.Stationary:
				evt.Phase = pressed == true ? phase : TouchPhase.Ended;
				evt.Position = position;
				evt.DeltaPosition = delta;
				evt.DeltaTime = Time.deltaTime;
				evt.EndTime = pressed == true ? 0.0f : time;
				modified = true;
				break;
			default:
				throw new System.IndexOutOfRangeException();
			}

			if (modified == true)
			{
				m_Buttons[idx] = evt;
				m_Owner.Process(evt);
			}
		}
	}
}
