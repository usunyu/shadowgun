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

public abstract class GuiInput : InputController
{
	class ActiveTouches : Dictionary<int, GUIBase_Widget>
	{
	}

	// PRIVATE MEMBERS

	ActiveTouches m_ActiveTouches = new ActiveTouches();
	GUIBase_Widget m_HoverWidget = null;

	// PUBLIC MEMEBRS

	public delegate bool ProcessInput(ref IInputEvent evt);
	public delegate GUIBase_Widget InputHitTest(ref Vector2 point);

	public ProcessInput OnProcessInput;
	public InputHitTest OnInputHitTest;

	public int MaxActiveTouches = 42;

	// INPUTCONTROLLER INTERFACE

	protected override void OnActivate()
	{
	}

	protected override void OnDeactivate()
	{
		foreach (var activeTouch in m_ActiveTouches)
		{
			HandleTouchEvent(activeTouch.Value, GUIBase_Widget.E_TouchPhase.E_TP_CLICK_RELEASE, null, false);
		}
		m_ActiveTouches.Clear();

		HandleTouchEvent(m_HoverWidget, GUIBase_Widget.E_TouchPhase.E_TP_MOUSEOVER_END, null, false);
		m_HoverWidget = null;
	}

	protected override bool OnProcess(ref IInputEvent evt)
	{
		switch (evt.Kind)
		{
		case E_EventKind.Key:
			return ProcessImpl(ref evt);
		case E_EventKind.Mouse:
			return ProcessMouse(ref evt);
		case E_EventKind.Touch:
			return ProcessTouch(ref evt);
		default:
			throw new System.IndexOutOfRangeException();
		}
	}

	// PRIVATE METHODS

	bool ProcessMouse(ref IInputEvent evt)
	{
		MouseEvent mouse = (MouseEvent)evt;
		GUIBase_Widget widget = HitTest(mouse.Position);

		if (evt is MouseEvent)
			ProcessMouseWheel(widget, (MouseEvent)evt);

		if (m_HoverWidget != widget)
		{
			if (m_HoverWidget != null)
			{
				widget = m_HoverWidget;
				m_HoverWidget = null;
				return HandleTouchEvent(widget, GUIBase_Widget.E_TouchPhase.E_TP_MOUSEOVER_END, evt, false);
			}

			if (m_HoverWidget == null)
			{
				m_HoverWidget = widget;
				return HandleTouchEvent(widget, GUIBase_Widget.E_TouchPhase.E_TP_MOUSEOVER_BEGIN, evt, true);
			}
		}

		return ProcessImpl(ref evt);
	}

	void ProcessMouseWheel(GUIBase_Widget widget, MouseEvent evt)
	{
		if (evt.ScrollWheel != 0)
		{
			if (widget == false)
				return;
			if (widget.Visible == false)
				return;
			widget.HandleTouchEvent(GUIBase_Widget.E_TouchPhase.E_TP_NONE, evt);
		}
	}

	bool ProcessTouch(ref IInputEvent evt)
	{
		TouchEvent touch = (TouchEvent)evt;

		/*if (touch.Started || touch.Finished)
		{
			Debug.Log(">>>> touch="+touch);
		}*/

		GUIBase_Widget widget = null;
		if (touch.Started == true)
		{
			if (m_ActiveTouches.Count >= MaxActiveTouches)
				return false;

			if (m_ActiveTouches.ContainsKey(touch.Id) == true)
				return false;

			widget = HitTest(touch.Position);
		}
		else
		{
			if (m_ActiveTouches.TryGetValue(touch.Id, out widget) == false)
			{
				ProcessImpl(ref evt);
				return false;
			}
		}

		E_InputOpacity opacity = E_InputOpacity.SemiTransparent;

		if (widget != null)
		{
			opacity = widget.InputOpacity;

			bool hover = HitTest(widget, touch.Position);

			switch (touch.Phase)
			{
			case TouchPhase.Began:
				//Debug.Log(">>>> TOUCH STARTED :: widget="+widget);

				m_ActiveTouches[touch.Id] = widget;
				HandleTouchEvent(widget, GUIBase_Widget.E_TouchPhase.E_TP_CLICK_BEGIN, evt, hover);
				break;
			case TouchPhase.Stationary:
			case TouchPhase.Moved:
				HandleTouchEvent(widget, GUIBase_Widget.E_TouchPhase.E_TP_NONE, evt, hover);
				break;
			case TouchPhase.Ended:
			case TouchPhase.Canceled:
				//Debug.Log(">>>> TOUCH FINISHED :: widget="+widget);

				m_ActiveTouches.Remove(touch.Id);
				HandleTouchEvent(widget, GUIBase_Widget.E_TouchPhase.E_TP_CLICK_RELEASE, evt, hover);
				break;
			default:
				throw new System.IndexOutOfRangeException();
			}
		}
		else
		{
			ProcessImpl(ref evt);
		}

		return opacity == E_InputOpacity.Opaque ? true : false;
	}

	GUIBase_Widget HitTest(Vector2 point)
	{
		point.y = Screen.height - point.y;
		return OnInputHitTest != null ? OnInputHitTest(ref point) : null;
	}

	bool HitTest(GUIBase_Widget widget, Vector2 point)
	{
		point.y = Screen.height - point.y;
		return widget != null ? widget.IsMouseOver(point) : false;
	}

	bool HandleTouchEvent(GUIBase_Widget widget, GUIBase_Widget.E_TouchPhase phase, object evt, bool hover)
	{
		if (widget == false)
			return false;
		if (widget.Visible == false)
			return false;
		return widget.HandleTouchEvent(phase, evt, hover);
	}

	bool ProcessImpl(ref IInputEvent evt)
	{
		return OnProcessInput != null ? OnProcessInput(ref evt) : false;
	}
}
