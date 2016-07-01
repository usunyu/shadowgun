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

[AddComponentMenu("GUI/Widgets/Enum")]
public class GUIBase_Enum : GUIBase_Callback
{
	public delegate void ChangeValueDelegate(int value);

	// PRIVATE MEMBERS

	[SerializeField] GUIBase_Widget[] m_EnumWidgets = new GUIBase_Widget[1];
	[SerializeField] int m_InitValue = 0;
	[SerializeField] Vector2 m_TextPadding = new Vector2(10, 10);

	GUIBase_Widget m_Widget;
	int m_CurrentSelection = -1;
	ChangeValueDelegate m_ChangeDelegate;
	bool m_IsDirty;
	bool m_IsDisabled;

	// GETTERS/SETTERS

	public GUIBase_Widget Widget
	{
		get { return m_Widget; }
	}

	public int Selection
	{
		get { return m_CurrentSelection; }
		set { SetSelection(value); }
	}

	// PUBLIC METHODS

	public void SetSelection(int value, bool immediate = false)
	{
		if (m_EnumWidgets.Length == 0)
			return;

		// Cycle
		if (value > m_EnumWidgets.Length - 1)
		{
			value = 0;
		}
		else if (value < 0)
		{
			value = m_EnumWidgets.Length - 1;
		}

		// update selection
		if (m_CurrentSelection != value)
		{
			m_CurrentSelection = value;

			//user callback
			if (m_ChangeDelegate != null)
			{
				m_ChangeDelegate(m_CurrentSelection);
			}
		}

		// update labels
		if (immediate == true)
		{
			UpdateLabels();
		}
		else
		{
			m_IsDirty = true;
		}
	}

	public void RegisterDelegate(ChangeValueDelegate dlgt)
	{
		m_ChangeDelegate = dlgt;
	}

	public void SetDisabled(bool disable)
	{
		//store flag 
		m_IsDisabled = disable;
		Widget.InputEnabled = !disable;

		//find child buttons and disable them
		GUIBase_Button[] buttons = Widget.GetComponentsInChildren<GUIBase_Button>();
		foreach (var button in buttons)
		{
			if (button.m_ParentWidget == this)
			{
				button.SetDisabled(disable);
			}
		}

		//update apearence
		UpdateLabels();
	}

	// MONOBEHAVIOUR INTERFACE

	void Awake()
	{
		// setup proxy flag
		m_Widget = GetComponent<GUIBase_Widget>();
		m_Widget.CreateMainSprite = true;
		m_Widget.InputEnabled = !m_IsDisabled;

		//cancel flag for labels
		foreach (var widget in m_EnumWidgets)
		{
			if (widget)
				widget.m_VisibleOnLayoutShow = false;
		}
	}

	public void Start()
	{
		int flags = (int)E_CallbackType.E_CT_INIT
					+ (int)E_CallbackType.E_CT_SHOW;

		m_Widget.RegisterCallback(this, flags);
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
			LateInit();
			break;
		case E_CallbackType.E_CT_SHOW:
			SetSelection(m_CurrentSelection, true);
			break;
		}

		return true;
	}

	public override void ChildButtonPressed(float value)
	{
		int selection = m_CurrentSelection;

		if (value >= 0.0f)
		{
			selection++;
		}
		else
		{
			selection--;
		}

		SetSelection(selection);
	}

	public override void ChildButtonReleased()
	{
	}

	// PRIVATE METHODS

	void LateInit()
	{
		if (m_EnumWidgets.Length == 0)
			return;

		// clamp initial value if needed
		if (m_CurrentSelection < 0)
		{
			m_CurrentSelection = Mathf.Clamp(m_InitValue, 0, m_EnumWidgets.Length - 1);
		}

		// Hide all enum posibilities
		UpdateLabels();
	}

	void UpdateLabels()
	{
		for (int idx = 0; idx < m_EnumWidgets.Length; ++idx)
		{
			GUIBase_Widget widget = m_EnumWidgets[idx];
			if (widget == null)
				return;

			widget.FadeAlpha = m_IsDisabled ? 0.5f : 1.0f; //set alpha to visually reflect disabled state
			widget.Show(idx == m_CurrentSelection ? m_Widget.Visible : false, true);

			GUIBase_Label label = widget.GetComponent<GUIBase_Label>();
			if (label != null)
			{
				label.Boundaries = GetClientRect();
			}
		}
	}

	Rect GetRect()
	{
#if UNITY_EDITOR
		GUIBase_Widget widget = Widget != null ? Widget : GetComponent<GUIBase_Widget>();
#else
		GUIBase_Widget widget = Widget;
#endif
		if (widget == null)
			return default(Rect);

		Transform trans = transform;
		Vector3 pos = trans.position;
		Vector3 lossyScale = trans.lossyScale;
		float width = widget.GetWidth()*lossyScale.x;
		float height = widget.GetHeight()*lossyScale.y;
		return new Rect(
						pos.x - width*0.5f,
						pos.y - height*0.5f,
						width,
						height
						);
	}

	Rect GetClientRect()
	{
		Vector3 lossyScale = transform.lossyScale;
		return GetRect().Deflate(
								 m_TextPadding.x*lossyScale.x,
								 m_TextPadding.y*lossyScale.y
						);
	}

#if UNITY_EDITOR
	void OnDrawGizmosSelected()
	{
		GUIBase_Widget widget = Widget != null ? Widget : GetComponent<GUIBase_Widget>();
		if (widget == null)
			return;
		if (widget.Visible == false)
			return;

		GuiBaseUtils.RenderRect(GetClientRect(), Color.yellow);
		GuiBaseUtils.RenderRect(GetRect(), Color.green);
	}
#endif
}
