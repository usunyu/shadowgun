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

[AddComponentMenu("GUI/Widgets/Roller")]
public class GUIBase_Roller : GUIBase_Callback
{
	public delegate void ChangeDelegate(int value);

	enum E_RollDirection
	{
		None = 0,
		Up = -1,
		Down = +1
	}

	// PRIVATE MEMBERS

	[SerializeField] int[] m_Items;
	[SerializeField] GUIBase_Label m_UpperLabel;
	[SerializeField] GUIBase_Label m_MiddleLabel;
	[SerializeField] GUIBase_Label m_LowerLabel;

	GUIBase_Widget m_Widget;
	int m_CurrentSelection;
	int m_PendingSelection;
	E_RollDirection m_RollDirection;
	ChangeDelegate m_ChangeDelegate;
	bool m_IsDirty;

	// GETTERS / SETTERS

	public GUIBase_Widget Widget
	{
		get { return m_Widget; }
	}

	public int Selection
	{
		get { return m_PendingSelection; }
		set { SetSelection(value); }
	}

	// PUBLIC METHODS

	public void SetSelection(int value, bool immediate = false)
	{
		m_PendingSelection = Mathf.Clamp(value, 0, m_Items.Length - 1);
		if (m_CurrentSelection == m_PendingSelection)
			return;

		if (immediate == true)
		{
			m_CurrentSelection = m_PendingSelection;
			UpdateTexts();
		}
		else
		{
			m_RollDirection = m_CurrentSelection > m_PendingSelection ? E_RollDirection.Down : E_RollDirection.Up;
			m_IsDirty = true;
		}
	}

	public void SelectPrevious()
	{
		if (m_Items.Length <= 1)
			return;

		int idx = Selection - 1;
		if (idx < 0)
		{
			idx = Mathf.Max(0, m_Items.Length - 1);
		}

		Selection = idx;

		m_RollDirection = E_RollDirection.Up;
	}

	public void SelectNext()
	{
		if (m_Items.Length <= 1)
			return;

		int idx = Selection + 1;
		if (idx >= m_Items.Length)
		{
			idx = 0;
		}

		Selection = idx;

		m_RollDirection = E_RollDirection.Down;
	}

	public void RegisterDelegate(ChangeDelegate dlgt)
	{
		m_ChangeDelegate = dlgt;
	}

	// MONEBEHAVIOUR INTERFACE

	void Awake()
	{
		m_Widget = GetComponent<GUIBase_Widget>();
		m_Widget.CreateMainSprite = true;
	}

	void Start()
	{
		// register callbacks
		int callbackMask = (int)E_CallbackType.E_CT_INIT
						   + (int)E_CallbackType.E_CT_SHOW;
		m_Widget.RegisterCallback(this, callbackMask);

		// done
		m_IsDirty = true;
	}

	void LateUpdate()
	{
		if (m_Widget.Visible == false)
			return;

		if (m_IsDirty == false)
			return;
		m_IsDirty = false;

		UpdateLabels();
	}

	// GUIBASE_CALLBACK INTERFACE

	public override bool Callback(E_CallbackType type, object evt)
	{
		switch (type)
		{
		case E_CallbackType.E_CT_INIT:
			m_IsDirty = true;
			return true;
		case E_CallbackType.E_CT_SHOW:
			UpdateLabels();
			return true;
		default:
			return false;
		}
	}

	public override void ChildButtonPressed(float v)
	{
		if (m_RollDirection != E_RollDirection.None)
			return;

		if (v < 0)
		{
			SelectPrevious();
		}
		else
		{
			SelectNext();
		}
	}

	public override void ChildButtonReleased()
	{
	}

	// PRIVATE METHODS

	void SetCurrentSelection(int value)
	{
		if (m_CurrentSelection == value)
			return;

		m_CurrentSelection = value;
		if (m_ChangeDelegate != null)
		{
			m_ChangeDelegate(m_CurrentSelection);
		}
	}

	void UpdateLabels()
	{
		UpdateRoll();
		UpdateTexts();
	}

	void UpdateRoll()
	{
		// prepare data
		float[] origins = new float[3];
		GetPositions(ref origins);

		float offset = origins[1] - origins[0];
		float speed = offset*0.25f;

		// compute new position
		float newY = m_MiddleLabel.Widget.GetRectInScreenCoords().center.y - speed;
		float[] pos = new float[3];
		if (CanSwapLabels(newY, origins[0] + offset*0.5f) == true)
		{
			pos[0] = newY;
			pos[1] = pos[0] + offset;
			pos[2] = pos[1] + offset;

			SetCurrentSelection(m_PendingSelection);

			m_IsDirty = true;
		}
		else if (CanRollLabels(newY, origins[1]) == true)
		{
			pos[0] = newY - offset;
			pos[1] = newY;
			pos[2] = newY + offset;

			m_IsDirty = true;
		}
		else
		{
			pos[0] = origins[0];
			pos[1] = origins[1];
			pos[2] = origins[2];
		}

		// set new position
		SetPositions(ref pos);

		// update direction state
		if (m_IsDirty == false)
		{
			m_RollDirection = E_RollDirection.None;
		}
	}

	void UpdateTexts()
	{
		int maxItemIdx = m_Items.Length - 1;
		int upperTextId = Mathf.Clamp(m_CurrentSelection > 0 ? m_CurrentSelection - 1 : maxItemIdx, 0, maxItemIdx);
		int middleTextId = Mathf.Clamp(m_CurrentSelection, 0, maxItemIdx);
		int lowerTextId = Mathf.Clamp(m_CurrentSelection < maxItemIdx ? m_CurrentSelection + 1 : 0, 0, maxItemIdx);

		m_UpperLabel.SetNewText(m_Items[upperTextId]);
		m_MiddleLabel.SetNewText(m_Items[middleTextId]);
		m_LowerLabel.SetNewText(m_Items[lowerTextId]);
	}

	bool CanSwapLabels(float pos, float limit)
	{
		switch (m_RollDirection)
		{
		case E_RollDirection.Up:
			return pos > limit ? true : false;
		case E_RollDirection.Down:
			return pos <= limit ? true : false;
		default:
			return false;
		}
	}

	bool CanRollLabels(float pos, float limit)
	{
		if (m_CurrentSelection != m_PendingSelection)
			return true;

		switch (m_RollDirection)
		{
		case E_RollDirection.Up:
			return pos <= limit ? true : false;
		case E_RollDirection.Down:
			return pos > limit ? true : false;
		default:
			return false;
		}
	}

	void GetPositions(ref float[] pos)
	{
		switch (m_RollDirection)
		{
		case E_RollDirection.Up:
			pos[2] = m_UpperLabel.Widget.GetOrigPos().y;
			pos[1] = m_MiddleLabel.Widget.GetOrigPos().y;
			pos[0] = m_LowerLabel.Widget.GetOrigPos().y;
			break;
		default:
			pos[0] = m_UpperLabel.Widget.GetOrigPos().y;
			pos[1] = m_MiddleLabel.Widget.GetOrigPos().y;
			pos[2] = m_LowerLabel.Widget.GetOrigPos().y;
			break;
		}
	}

	void SetPositions(ref float[] pos)
	{
		switch (m_RollDirection)
		{
		case E_RollDirection.Up:
			SetPosition(m_UpperLabel.Widget, pos[2]);
			SetPosition(m_MiddleLabel.Widget, pos[1]);
			SetPosition(m_LowerLabel.Widget, pos[0]);
			break;
		default:
			SetPosition(m_UpperLabel.Widget, pos[0]);
			SetPosition(m_MiddleLabel.Widget, pos[1]);
			SetPosition(m_LowerLabel.Widget, pos[2]);
			break;
		}
	}

	void SetPosition(GUIBase_Widget widget, float y)
	{
		Transform trans = widget.transform;
		Vector3 pos = trans.position;
		pos.y = y;
		trans.position = pos;
		widget.SetModify();
	}
}
