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

[AddComponentMenu("GUI/Widgets/Slider")]
public class GUIBase_Slider : GUIBase_Callback
{
	[SerializeField] bool m_IsDisabled;
	public float m_MinValue = 0.0f;
	public float m_MaxValue = 1.0f;
	public float m_InitValue = 0.5f;

	// bar
	public GUIBase_Sprite m_BarSprite;

	public float m_TouchableAreaWidthScale = 1.0f;
	public float m_TouchableAreaHeightScale = 1.0f;

	public delegate void ChangeValueDelegate(float v);

	public bool IsDisabled
	{
		get { return m_IsDisabled; }
		set
		{
			if (m_IsDisabled == value)
				return;
			m_IsDisabled = value;
			m_Widget.InputEnabled = !value;
			RefreshComponents();
		}
	}

	GUIBase_Widget m_Widget;
	ChangeValueDelegate m_ChangeValueDelegate;

	float m_CurrentValue;
	bool m_WasTouched = false;

	//---------------------------------------------------------
	void Awake()
	{
		m_Widget = GetComponent<GUIBase_Widget>();
		m_Widget.CreateMainSprite = true;
		m_Widget.InputEnabled = !m_IsDisabled;

		GUIBase_Button[] buttons = this.GetComponentsInChildren<GUIBase_Button>();
		foreach (var button in buttons)
		{
			if (button.m_ParentWidget == this)
			{
				button.initStateDisabled = m_IsDisabled;
			}
		}
	}

	//---------------------------------------------------------
	void Start()
	{
		int flags = (int)E_CallbackType.E_CT_INIT + (int)E_CallbackType.E_CT_ON_TOUCH_BEGIN;

		m_Widget.RegisterCallback(this, flags);

		RefreshComponents();
	}

	//---------------------------------------------------------
	void Update()
	{
		if (m_Widget != null && m_Widget.Visible == true)
		{
			UpdateSlider();
		}
	}

	//---------------------------------------------------------
	public void RegisterChangeValueDelegate(ChangeValueDelegate d)
	{
		m_ChangeValueDelegate = d;
	}

	//---------------------------------------------------------
	public override bool Callback(E_CallbackType type, object evt)
	{
		switch (type)
		{
		case E_CallbackType.E_CT_INIT:
			CustomInit();
			break;

		case E_CallbackType.E_CT_ON_TOUCH_BEGIN:

			m_WasTouched = true;

			UpdateSlider();

			break;
		}

		return true;
	}

	//---------------------------------------------------------
	public override void GetTouchAreaScale(out float scaleWidth, out float scaleHeight)
	{
		scaleWidth = m_TouchableAreaWidthScale;
		scaleHeight = m_TouchableAreaHeightScale;
	}

	//---------------------------------------------------------
	public override void ChildButtonPressed(float v)
	{
		float p = v*0.01f*(m_MaxValue - m_MinValue);
		float newValue = m_CurrentValue + p;

		SetValue(newValue);

		if (m_ChangeValueDelegate != null)
		{
			m_ChangeValueDelegate(m_CurrentValue);
		}
	}

	//---------------------------------------------------------
	public override void ChildButtonReleased()
	{
	}

	//---------------------------------------------------------
	void CustomInit()
	{
		// Set initial value
		SetValue(m_InitValue);

		// Hide bar sprite
		if (m_BarSprite != null)
		{
			m_BarSprite.Widget.ShowImmediate(false, true);
		}
	}

	//-----------------------------------------------------
	public void SetValue(float v)
	{
		m_CurrentValue = Mathf.Clamp(v, m_MinValue, m_MaxValue);

		// Set new position and size of sprite

		Transform trans = transform;
		Vector3 pos = trans.position;
		Vector3 scale = trans.lossyScale;

		float f = (m_CurrentValue - m_MinValue)/(m_MaxValue - m_MinValue);

		float posX = pos.x;
		float posY = pos.y;
		float width = m_Widget.GetWidth()*f;
		float height = m_Widget.GetHeight();

		posX -= m_Widget.GetWidth()*0.5f*scale.x;
		posX += width*0.5f*scale.x;

		if (m_BarSprite != null)
		{
			m_BarSprite.Widget.UpdateSpritePosAndSize(0, posX, posY, width, height);
		}
	}

	//---------------------------------------------------------
	void UpdateSlider()
	{
		if (m_IsDisabled == true)
			return;
		if (m_WasTouched == false)
			return;

		Vector2 touchPos = new Vector2();
		bool touch = false;

		if (Input.touchCount != 0)
		{
			Touch t = Input.touches[0];

			touchPos.x = t.position.x;
			//touchPos.y	= t.position.y;

			touch = true;
		}
		else if (Input.GetMouseButton(0))
		{
			touchPos.x = Input.mousePosition.x;
			//touchPos.y	= Input.mousePosition.y;

			touch = true;
		}

		if (touch)
		{
			//Debug.Log(touchPos.x);

			Transform trans = transform;
			float minX = trans.position.x - m_Widget.GetWidth()*0.5f*trans.lossyScale.x;
			float maxX = minX + m_Widget.GetWidth()*trans.lossyScale.x;

			float f = (touchPos.x - minX)/(maxX - minX);
			float v = Mathf.Lerp(m_MinValue, m_MaxValue, f);

			SetValue(v);

			if (m_ChangeValueDelegate != null)
			{
				m_ChangeValueDelegate(v);
			}
		}
		else
		{
			m_WasTouched = false;
		}
	}

	//---------------------------------------------------------
	void RefreshComponents()
	{
		GUIBase_Button[] buttons = this.GetComponentsInChildren<GUIBase_Button>();
		foreach (var button in buttons)
		{
			if (button.m_ParentWidget == this)
			{
				button.SetDisabled(m_IsDisabled);
			}
		}

		if (m_BarSprite != null)
		{
			m_BarSprite.Widget.FadeAlpha = m_IsDisabled ? 0.1f : 1.0f;
		}
	}
}
