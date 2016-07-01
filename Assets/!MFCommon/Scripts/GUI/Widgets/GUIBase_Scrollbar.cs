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

[RequireComponent(typeof (GUIBase_Widget))]
public class GUIBase_Scrollbar : GUIBase_Callback
{
	// PRIVATE MEMBERS

	[SerializeField] bool m_Disabled = false;
	[SerializeField] bool m_HideWhenInactive = true;
	[SerializeField] float m_FadeInDuration = 0.2f;
	[SerializeField] float m_FadeOutDuration = 1.0f;
	[SerializeField] GUIBase_Sprite m_TrackSprite;
	[SerializeField] GUIBase_Sprite m_ThumbSprite;
	[SerializeField] GUIBase_Callback m_ParentWidget;

	int m_Value;
	int m_MaxValue;
	int m_MaxVisible;

	bool m_IsVertical;
	int m_DragStartValue;
	Vector2 m_DragStartPosition;
	float m_DragStep;
	float m_ScrollStep;

	float m_FadeValue;
	float m_FadeDelta;
	float m_FadeDuration;

	// PUBLIC MEMBERS

	public GUIBase_Widget Widget { get; private set; }

	public bool IsDisabled
	{
		get { return m_Disabled; }
		set { SetDisabled(value); }
	}

	public bool IsVertical
	{
		get { return m_IsVertical; }
	}

	public int Value
	{
		get { return m_Value; }
		set { SetValue(value); }
	}

	public int MaxValue
	{
		get { return m_MaxValue; }
		set { SetMaxValue(value); }
	}

	public int MaxVisible
	{
		get { return m_MaxVisible; }
		set { SetMaxVisible(value); }
	}

	public GUIBase_Callback ParentWidget
	{
		get { return m_ParentWidget; }
		set { m_ParentWidget = value; }
	}

	// PUBLIC METHODS

	public void SetValue(int value)
	{
		value = Mathf.Clamp(value, 0, Mathf.Max(0, m_MaxValue - m_MaxVisible));
		if (value != m_Value)
		{
			m_Value = value;

			Widget.SetModify();
		}

		if (Widget.Visible == true)
		{
			FadeIn();
		}
	}

	public void SetMaxValue(int value)
	{
		if (m_MaxValue == value)
			return;

		m_MaxValue = value;

		ComputeSteps();

		SetValue(m_Value);
	}

	public void SetMaxVisible(int value)
	{
		if (m_MaxVisible == value)
			return;

		m_MaxVisible = value;

		ComputeSteps();
	}

	public void SetDisabled(bool state)
	{
		if (m_Disabled == state)
			return;

		m_Disabled = state;
		Widget.InputEnabled = !state;

		Widget.SetModify();
	}

	// MONOBEHAVIOUR INTERFACE

	void Awake()
	{
		Widget = GetComponent<GUIBase_Widget>();
		Widget.InputEnabled = !m_Disabled;

		if (m_TrackSprite == null)
		{
			Debug.LogWarning(GetType().Name + "<" + this.GetFullName('.') + ">.Awake() :: Thers is not any Track Sprite specified!", gameObject);
			return;
		}

		if (m_ThumbSprite == null)
		{
			Debug.LogWarning(GetType().Name + "<" + this.GetFullName('.') + ">.Awake() :: Thers is not any Thumb Sprite specified!", gameObject);
			return;
		}
	}

	void Start()
	{
		int callbackMask = (int)E_CallbackType.E_CT_SHOW
						   + (int)E_CallbackType.E_CT_HIDE
						   + (int)E_CallbackType.E_CT_ON_TOUCH_BEGIN
						   + (int)E_CallbackType.E_CT_ON_TOUCH_UPDATE
						   + (int)E_CallbackType.E_CT_ON_TOUCH_END
						   + (int)E_CallbackType.E_CT_ON_TOUCH_END_OUTSIDE;
		Widget.RegisterCallback(this, callbackMask);
		Widget.RegisterUpdateDelegate(UpdateThumb);

		ComputeSteps();
		SetAlpha(0.0f);
	}

	void Update()
	{
		UpdateFade();
	}

	// GUIBASE_CALLBACK INTERFACE

	public override bool Callback(E_CallbackType type, object evt)
	{
		switch (type)
		{
		case E_CallbackType.E_CT_SHOW:
			UpdateThumb();
			return true;
		case E_CallbackType.E_CT_HIDE:
			m_FadeDuration = 0.0f;
			SetAlpha(0.0f);
			return true;
		case E_CallbackType.E_CT_ON_TOUCH_BEGIN:
			if (m_DragStep != 0.0f)
			{
				TouchEvent touch = (TouchEvent)evt;
				m_DragStartPosition = touch.Position;
				m_DragStartValue = m_Value;
			}
			return true;
		case E_CallbackType.E_CT_ON_TOUCH_UPDATE:
		{
			if (evt is MouseEvent)
			{
				MouseEvent mEvt = (MouseEvent)evt;
				if (mEvt.ScrollWheel > 0)
				{
					m_ParentWidget.ChildButtonPressed(-1);
				}
				else if (mEvt.ScrollWheel < 0)
				{
					m_ParentWidget.ChildButtonPressed(1);
				}
			}
			else
			{
				TouchEvent touch = (TouchEvent)evt;
				if (m_DragStep != 0.0f)
				{
					Vector2 diff = touch.Position - m_DragStartPosition;
					int offset = m_IsVertical ? Mathf.RoundToInt(diff.y/m_DragStep) : Mathf.RoundToInt(diff.x/m_DragStep);
					int dragValue = Mathf.Clamp(m_DragStartValue + offset, 0, Mathf.Max(0, m_MaxValue - m_MaxVisible));

					int delta = dragValue - m_Value;

					SetValue(dragValue);

					if (m_ParentWidget != null)
					{
						m_ParentWidget.ChildButtonPressed(delta);
					}

					//Debug.Log(">>>> diff="+diff+", offset="+offset+", m_DragStartValue="+m_DragStartValue+", m_Value="+m_Value+", m_DragStep="+m_DragStep);
				}
			}
		}
			return true;
		case E_CallbackType.E_CT_ON_TOUCH_END:
			if (m_ParentWidget != null)
			{
				// forward touch to parent if the begin and the end of touch is the same
				// so scrollbar can't handle it
				TouchEvent touch = (TouchEvent)evt;
				Vector2 diff = touch.Position - m_DragStartPosition;
				float minDist = Mathf.Max(1.0f, Screen.height*0.01f);
				if ( /*(touch.EndTime - touch.StartTime) <= 0.1f &&*/
								Mathf.Abs(touch.DeltaPosition.x) <= minDist &&
								Mathf.Abs(touch.DeltaPosition.y) <= minDist &&
								Mathf.Abs(diff.x) <= minDist &&
								Mathf.Abs(diff.y) <= minDist)
				{
					if (m_ParentWidget.TestFlag((int)E_CallbackType.E_CT_ON_TOUCH_BEGIN) == true)
					{
						TouchEvent touchBegin = new TouchEvent()
						{
							Id = touch.Id,
							Phase = TouchPhase.Began,
							Type = touch.Type,
							Position = touch.Position,
							StartPosition = m_DragStartPosition,
							DeltaPosition = Vector2.zero,
							StartTime = touch.StartTime,
							DeltaTime = 0.0f,
							EndTime = 0.0f
						};
						m_ParentWidget.Callback(E_CallbackType.E_CT_ON_TOUCH_BEGIN, touchBegin);
					}
					if (m_ParentWidget.TestFlag((int)E_CallbackType.E_CT_ON_TOUCH_END) == true)
					{
						m_ParentWidget.Callback(E_CallbackType.E_CT_ON_TOUCH_END, touch);
					}
				}
			}
			FadeOut();
			return true;
		case E_CallbackType.E_CT_ON_TOUCH_END_OUTSIDE:
			FadeOut();
			return true;
		default:
			return false;
		}
	}

	// PRIVATE MEMBERS

	void UpdateThumb()
	{
		if (m_TrackSprite == null)
			return;
		if (m_ThumbSprite == null)
			return;

		Transform trackTrans = m_TrackSprite.transform;
		Vector3 trackScale = trackTrans.localScale;
		float trackWidth = m_TrackSprite.Widget.GetWidth()*trackScale.x;
		float trackHeight = m_TrackSprite.Widget.GetHeight()*trackScale.y;

		Vector3 thumbPosition = trackTrans.localPosition;
		//Vector3    thumbScale = m_ThumbSprite.transform.localScale;
		float thumbWidth = m_ThumbSprite.Widget.GetWidth();
		float thumbHeight = m_ThumbSprite.Widget.GetHeight();

		if (m_IsVertical == true)
		{
			trackHeight -= thumbHeight + (trackWidth - thumbWidth);
			thumbPosition.y += Mathf.Clamp(m_ScrollStep*m_Value, 0.0f, trackHeight) - trackHeight*0.5f;
		}
		else
		{
			trackWidth -= thumbWidth + (trackHeight - thumbHeight);
			thumbPosition.x += Mathf.Clamp(m_ScrollStep*m_Value, 0.0f, trackWidth) - trackWidth*0.5f;
		}

		m_ThumbSprite.transform.localPosition = thumbPosition;
		m_ThumbSprite.Widget.SetModify(true);
	}

	void UpdateFade()
	{
		if (m_TrackSprite == null)
			return;
		if (m_ThumbSprite == null)
			return;
		if (m_FadeDuration <= 0.0f)
			return;

		float value = m_FadeDelta*(Time.deltaTime/m_FadeDuration);

		SetAlpha(Widget.FadeAlpha + value);

		if (Widget.FadeAlpha <= 0.0f)
		{
			m_FadeDuration = 0.0f;
		}
		else if (Widget.FadeAlpha >= 0.999f)
		{
			FadeOut();
		}
	}

	void ComputeSteps()
	{
		Transform trackTrans = m_TrackSprite.transform;
		Vector3 trackScale = trackTrans.localScale;
		float trackWidth = m_TrackSprite.Widget.GetWidth()*trackScale.x;
		float trackHeight = m_TrackSprite.Widget.GetHeight()*trackScale.y;

		//Vector3   thumbScale = m_ThumbSprite.transform.localScale;
		float thumbWidth = m_ThumbSprite.Widget.GetWidth();
		float thumbHeight = m_ThumbSprite.Widget.GetHeight();

		int maxScroll = Mathf.Max(0, m_MaxValue - m_MaxVisible);
		if (trackHeight > trackWidth)
		{
			trackHeight -= thumbHeight + (trackWidth - thumbWidth);
			m_DragStep = m_MaxValue > 0 ? trackHeight/m_MaxValue : 0.0f;
			m_ScrollStep = maxScroll > 0 ? trackHeight/maxScroll : 0.0f;
			m_IsVertical = true;
		}
		else
		{
			trackWidth -= thumbWidth + (trackHeight - thumbHeight);
			m_DragStep = m_MaxValue > 0 ? trackWidth/m_MaxValue : 0.0f;
			m_ScrollStep = maxScroll > 0 ? trackWidth/maxScroll : 0.0f;
			m_IsVertical = false;
		}
	}

	void FadeIn()
	{
		if (m_HideWhenInactive == false)
			return;

		m_FadeDelta = 1.0f - Widget.FadeAlpha;
		m_FadeDuration = m_FadeInDuration;
	}

	void FadeOut()
	{
		if (m_HideWhenInactive == false)
			return;

		m_FadeDelta = 0.0f - Widget.FadeAlpha;
		m_FadeDuration = m_FadeOutDuration;
	}

	void SetAlpha(float value)
	{
		value = Mathf.Clamp(value, 0.0f, 1.0f);
		if (Widget.FadeAlpha == value)
			return;

		Widget.FadeAlpha = m_HideWhenInactive == true ? value : 1.0f;
		m_TrackSprite.Widget.FadeAlpha = Widget.FadeAlpha;
		m_ThumbSprite.Widget.FadeAlpha = Widget.FadeAlpha;
	}
}
