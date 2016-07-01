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

[AddComponentMenu("GUI/Widgets/Switch")]
public class GUIBase_Switch : GUIBase_Callback
{
	[SerializeField] bool m_IsDisabled;
	public GUIBase_Button[] m_Buttons = new GUIBase_Button[2];
	public bool m_InitValue;

	public delegate void SwitchDelegate(bool switchValue);

	GUIBase_Widget m_Widget;
	bool m_Value;
	bool m_Dirty = true;
	SwitchDelegate m_SwitchDelegate;

	public GUIBase_Widget Widget
	{
		get { return m_Widget; }
	}

	public bool IsDisabled
	{
		get { return m_IsDisabled; }
		set
		{
			if (m_IsDisabled == value)
				return;
			m_IsDisabled = value;
			Widget.InputEnabled = !value;
			RefreshComponents();
		}
	}

	//---------------------------------------------------------
	//SetValue se nesmi volat kdyz jeste nejsou inicializovane sprity.
	public void SetValue(bool val)
	{
		m_Value = val;
		m_Dirty = true;
	}

	//---------------------------------------------------------
	public bool GetValue()
	{
		return m_Value;
	}

	//---------------------------------------------------------
	public void Awake()
	{
		m_Widget = GetComponent<GUIBase_Widget>();
		m_Widget.InputEnabled = !m_IsDisabled;

		foreach (var button in m_Buttons)
		{
			if (button != null)
			{
				button.initStateDisabled = m_IsDisabled;
			}
		}
	}

	//---------------------------------------------------------
	public void Start()
	{
		int flags = (int)E_CallbackType.E_CT_SHOW + (int)E_CallbackType.E_CT_HIDE;
		m_Widget.RegisterCallback(this, flags);
		SetValue(m_InitValue);

		RefreshComponents();
	}

	//---------------------------------------------------------
	void LateUpdate()
	{
		if (m_Dirty == true && m_Widget.IsVisible())
		{
			ShowSwitchButton(true);
			m_Dirty = false;
		}
	}

	//---------------------------------------------------------
	public override bool Callback(E_CallbackType type, object evt)
	{
		switch (type)
		{
		case E_CallbackType.E_CT_SHOW:
			ShowSwitchButton(true);
			break;
		case E_CallbackType.E_CT_HIDE:
			ShowSwitchButton(false);
			break;
		}

		return true;
	}

	//---------------------------------------------------------
	public void RegisterDelegate(SwitchDelegate d)
	{
		m_SwitchDelegate = d;
	}

	//---------------------------------------------------------
	public override void ChildButtonPressed(float v)
	{
		//Debug.Log("Switch pressed" + v);

		m_Value = (v == 1) ? true : false;
		m_Dirty = true;

		if (m_SwitchDelegate != null)
		{
			m_SwitchDelegate(m_Value);
		}
	}

	//---------------------------------------------------------
	void ShowSwitchButton(bool state)
	{
		//Debug.Log("Switch show");

		// show button belonging to current value
		int showBtn = (m_Value) ? 1 : 0;

		for (int i = 0; i < m_Buttons.Length; ++i)
		{
			if (m_Buttons[i])
			{
				GUIBase_Widget widget = m_Buttons[i].Widget;

				if (widget)
				{
					widget.Show((i == showBtn) ? state : false, true);
				}
			}
		}
	}

	//---------------------------------------------------------
	public override void ChildButtonReleased()
	{
	}

	//---------------------------------------------------------
	void RefreshComponents()
	{
		foreach (var button in m_Buttons)
		{
			if (button != null)
			{
				button.SetDisabled(m_IsDisabled);
			}
		}

		m_Dirty = true;
	}
}
