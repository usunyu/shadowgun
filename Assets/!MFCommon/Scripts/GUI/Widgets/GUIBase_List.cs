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
using System.Collections.Generic;

[AddComponentMenu("GUI/Widgets/Widgets List")]
[RequireComponent(typeof (GUIBase_Widget))]
public class GUIBase_List : GUIBase_Callback
{
	public delegate void UpdateRowDelegate(GUIBase_Widget widget, int rowIndex, int itemIndex);
	public delegate void SelectRowDelegate(GUIBase_Widget widget, int rowIndex, int itemIndex);
	public delegate bool ProcessInputDelegate(ref IInputEvent evt);

	public GUIBase_Widget m_FirstListLine;
	public GUIBase_Scrollbar m_Scrollbar;
	public GUIBase_Button m_ButtonUp;
	public GUIBase_Button m_ButtonDown;
	public int m_NumOfLines = 1;
	public Vector2 m_LinesOffset;
	public bool m_IsReversed = false; //list is filling in the opposite way (from the bottom)

	int m_ItemOffset = 0;
	int m_MaxItems = 0;

	GUIBase_Widget m_Widget;
	GUIBase_Widget[] m_Lines = new GUIBase_Widget[0];

	public GUIBase_Widget Widget
	{
		get { return m_Widget; }
	}

	public int numOfLines
	{
		get { return m_NumOfLines; }
		set
		{
			if (m_NumOfLines == value)
				return;
			m_NumOfLines = value;

			if (m_Scrollbar != null)
			{
				m_Scrollbar.MaxVisible = m_NumOfLines;
			}
		}
	}

	public int MaxItems
	{
		get { return m_MaxItems; }
		set
		{
			if (m_MaxItems == value)
				return;
			m_MaxItems = value;
			m_ItemOffset = Mathf.Clamp(m_ItemOffset, 0, Mathf.Max(0, m_MaxItems - m_NumOfLines));

			if (m_Scrollbar != null)
			{
				if (m_IsReversed)
				{
					if (m_MaxItems <= m_NumOfLines) //make sure, that the scroll thumb is at the bottom
					{
						m_Scrollbar.MaxValue = m_NumOfLines + 1;
						m_Scrollbar.Value = 1;
					}
					else
					{
						m_Scrollbar.MaxValue = value;
						m_Scrollbar.Value = m_MaxItems - m_ItemOffset - m_NumOfLines;
					}
				}
				else
				{
					m_Scrollbar.MaxValue = value;
					m_Scrollbar.Value = m_ItemOffset;
				}
			}

			Widget.SetModify();
		}
	}

	public UpdateRowDelegate OnUpdateRow;
	public SelectRowDelegate OnSelectRow;
	public ProcessInputDelegate OnProcessInput; //unhandled touch events will be sended to this delegate

	// Use this for initialization
	void Start()
	{
		m_Widget = GetComponent<GUIBase_Widget>();

		int callbackMask = (int)E_CallbackType.E_CT_ON_TOUCH_END;
		m_Widget.RegisterCallback(this, callbackMask);
		m_Widget.RegisterUpdateDelegate(UpdateList);

		if (m_FirstListLine != null && m_NumOfLines > 1)
		{
			InitializeChilds();
		}

		if (m_Scrollbar != null)
		{
			m_Scrollbar.ParentWidget = this;
			m_Scrollbar.MaxVisible = m_NumOfLines;
		}

		if (m_ButtonUp != null)
		{
			m_ButtonUp.Widget.m_VisibleOnLayoutShow = false;
			m_ButtonUp.m_ParentWidget = this;
		}

		if (m_ButtonDown != null)
		{
			m_ButtonDown.Widget.m_VisibleOnLayoutShow = false;
			m_ButtonDown.m_ParentWidget = this;
		}
	}

	public GUIBase_Widget GetWidgetOnLine(int inLineIndex)
	{
		if (inLineIndex >= 0 && inLineIndex < m_Lines.Length)
			return m_Lines[inLineIndex];

		return null;
	}

	// GUIBASE_CALLBACK INTERFACE

	public override bool Callback(E_CallbackType type, object evt)
	{
		switch (type)
		{
		case E_CallbackType.E_CT_ON_TOUCH_END:
			if (OnSelectRow != null)
			{
				TouchEvent touch = (TouchEvent)evt;
				Vector2 point = touch.Position;
				point.y = Screen.height - point.y;
				for (int idx = 0; idx < m_Lines.Length; ++idx)
				{
					GUIBase_Widget row = m_Lines[idx];
					if (row.Visible == false)
						continue;
					if (row.IsMouseOver(point) == false)
						continue;

					OnSelectRow(row, idx, m_ItemOffset + idx);
					break;
				}
			}
			if (OnProcessInput != null) //send the events to the parent
			{
				TouchEvent touch = (TouchEvent)evt;
				if (m_Scrollbar != null) //this event probably comes from scrollbar (because scrollbar is catching all touch events)
					touch.Id = -1; //and it doesn't have proper begin, this marks the event as 'faked'
				IInputEvent inputEvent = (IInputEvent)touch;
				return OnProcessInput(ref inputEvent);
			}
			return true;
		default:
			return false;
		}
	}

	public override void ChildButtonPressed(float value)
	{
		if (m_IsReversed)
			value = -value;

		int offset = Mathf.Clamp(m_ItemOffset + (int)value, 0, Mathf.Max(0, m_MaxItems - m_NumOfLines));
		if (m_ItemOffset == offset)
			return;

		m_ItemOffset = offset;

		if (m_Scrollbar != null)
		{
			if (m_IsReversed)
				m_Scrollbar.Value = m_MaxItems - offset - m_NumOfLines;
			else
				m_Scrollbar.Value = offset;
		}

		Widget.SetModify();
	}

	public override void ChildButtonReleased()
	{
	}

	//PRIVATE METHODS

	void UpdateList()
	{
		if (OnUpdateRow == null)
			return;

		for (int idx = 0; idx < m_Lines.Length; ++idx)
		{
			OnUpdateRow(GetWidgetOnLine(idx), idx, m_ItemOffset + idx);
		}

		GUIBase_Button btnUp = m_ButtonUp;
		GUIBase_Button btnDown = m_ButtonDown;

		if (m_IsReversed)
		{
			btnUp = m_ButtonDown;
			btnDown = m_ButtonUp;
		}

		if (btnUp != null)
		{
			btnUp.Widget.Show(m_ItemOffset > 0 ? true : false, true);
		}

		if (btnDown != null)
		{
			btnDown.Widget.Show(m_ItemOffset < Mathf.Max(0, m_MaxItems - m_NumOfLines) ? true : false, true);
		}
	}

	// Update is called once per frame
	void InitializeChilds()
	{
		Transform trans = m_FirstListLine.transform;
		Vector3 pos = trans.position;
		Quaternion rot = trans.rotation;

		Vector3 offset = Vector3.zero;

		m_Lines = new GUIBase_Widget[m_NumOfLines];
		m_Lines[0] = m_FirstListLine;

		for (int idx = 1; idx < m_NumOfLines; ++idx)
		{
			offset.x += m_LinesOffset.x;
			offset.y += m_LinesOffset.y;

			GUIBase_Widget widget = Instantiate(m_FirstListLine, pos + offset, rot) as GUIBase_Widget;
			Transform widgetTrans = widget.transform;
			widgetTrans.parent = trans.parent;
			widgetTrans.localScale = trans.localScale;
			widgetTrans.localPosition = trans.localPosition + offset;
			widget.name = m_FirstListLine.name + " [ " + idx + " ]";

			m_Lines[idx] = widget;
		}
	}
}
