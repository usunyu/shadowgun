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

public class InputDriverTouch : InputDriver
{
	// PRIVATE MEMBERS

	InputManager m_Owner;
	List<TouchEvent> m_Touches = new List<TouchEvent>();

	// INPUTDRIVER INTERFACE

	internal override void Initialize(InputManager manager)
	{
		m_Owner = manager;
	}

	internal override void Deinitialize(InputManager manager)
	{
		Flush();

		m_Touches = null;
		m_Owner = null;
	}

	internal override void Update()
	{
		foreach (var temp in Input.touches)
		{
			CreateTouchEventIfNeeded(temp.fingerId);
		}

		Touch touch = new Touch();
		for (int idx = 0; idx < m_Touches.Count; ++idx)
		{
			TouchEvent evt = m_Touches[idx];

			bool modified = false;
			if (GetTouchById(evt.Id, ref touch) == true)
			{
				modified = RefreshTouchEvent(ref evt, ref touch);
			}
			else
			{
				modified = CancelTouchEvent(ref evt);
			}

			if (modified == true)
			{
				m_Touches[idx] = evt;
				m_Owner.Process(evt);
			}
		}
	}

	internal override void Flush()
	{
		float time = Time.time;

		for (int idx = 0; idx < m_Touches.Count; ++idx)
		{
			TouchEvent evt = m_Touches[idx];
			if (evt.Finished == true)
				continue;

			evt.Phase = TouchPhase.Canceled;
			evt.EndTime = time;

			m_Touches[idx] = evt;
			m_Owner.Process(evt);
		}
	}

	// PRIVATE METHODS

	void CreateTouchEventIfNeeded(int Id)
	{
		for (int idx = 0; idx < m_Touches.Count; ++idx)
		{
			if (m_Touches[idx].Id == Id)
				return;
		}

		m_Touches.Add(new TouchEvent()
		{
			Id = Id,
			Phase = TouchPhase.Canceled,
			Type = E_TouchType.Finger
		});

		//Debug.Log(m_Touches[m_Touches.Count - 1]);
	}

	bool GetTouchById(int Id, ref Touch touch)
	{
		for (int idx = 0; idx < Input.touchCount; ++idx)
		{
			touch = Input.touches[idx];
			if (touch.fingerId == Id)
				return true;
		}
		return false;
	}

	bool RefreshTouchEvent(ref TouchEvent evt, ref Touch touch)
	{
		bool modified = evt.Active;
		if (touch.phase != evt.Phase)
		{
			if (touch.phase == TouchPhase.Began)
			{
				evt.Position = touch.position;
				evt.StartPosition = touch.position;
				evt.DeltaPosition = Vector2.zero;
				evt.StartTime = Time.time;
				evt.DeltaTime = 0.0f;
				evt.EndTime = 0.0f;
			}
			else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
			{
				evt.EndTime = Time.time;
			}

			evt.Phase = touch.phase;
			modified = true;
		}

		if (modified == true || touch.position != evt.Position)
		{
			evt.Position = touch.position;
			evt.DeltaPosition = touch.deltaPosition;
			modified = true;
		}

		if (modified == true || touch.deltaTime != evt.DeltaTime)
		{
			evt.DeltaTime = touch.deltaTime;
			modified = true;
		}

		return modified;
	}

	bool CancelTouchEvent(ref TouchEvent evt)
	{
		if (evt.Finished == true)
			return false;

		evt.Phase = TouchPhase.Canceled;
		evt.DeltaPosition = Vector2.zero;
		evt.DeltaTime = 0.0f;
		evt.EndTime = Time.time;

		return true;
	}
}
