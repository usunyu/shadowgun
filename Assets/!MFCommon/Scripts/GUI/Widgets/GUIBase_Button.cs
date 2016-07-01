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
using System.Linq;

[AddComponentMenu("GUI/Widgets/Button")]
public class GUIBase_Button : GUIBase_Callback
{
	public enum E_ButtonState
	{
		E_BS_NONE = -1,

		E_BS_IDLE = 0,
		E_BS_DISABLED = 1,
		E_BS_MOUSE_OVER = 2,
		E_BS_BUTTON_DOWN = 3,
		E_BS_BUTTON_HIGHLIGHT = 4,

		E_BS_LAST_IDX = 4,
	};

	public bool initStateDisabled = false;

	public float m_TouchableAreaWidthScale = 1.0f;
	public float m_TouchableAreaHeightScale = 1.0f;

	// Idle state (its sprite is directly on button)
	public GUIBase_Sprite idleSprite;
	public AnimationClip idleAnimationLoop;

	// Disabled state
	public GUIBase_Sprite disabledSprite;
	public AnimationClip disabledAnimationLoop;
	public AudioClip disabledSound;

	// Mouse Over
	public GUIBase_Sprite mouseOverSprite;
	public AnimationClip mouseOverAnimationLoop;
	public AudioClip mouseOverSoundIn; // TODO - da se dodelat LOOP property, pokud bude potreba

	// Button Down
	// if buttonDownSprite is undefined, there is OnTouchDelegate invoked instantly after click
	// if it is false, OnTouchDelegate is called if button is released (and cursor is still over button) and button changes its texture if cursor is over
	public GUIBase_Sprite buttonDownSprite;
	public AnimationClip buttonDownAnimationLoop;
	public AudioClip buttonDownSoundIn; // click sound	// TODO - da se dodelat LOOP property, pokud bude potreba
	public AudioClip buttonDownSoundOut;

	//highlight
	public GUIBase_Sprite highlightSprite;

	public GUIBase_Callback m_ParentWidget;
							//sem se preposilaji eventy buttonu. Slouzi pro osetreni specialnich widgetu jako je slider, enum a popup, ktere jsou slozeny z vice casti a museji cooperovat.
	public float m_ValueChangedInParent = 0.0f;
	public string inputButton;

	public delegate void TouchDelegate();
	public delegate void ReleaseDelegate(bool inside);
	//vola se pri ukonceni touche. Parametr inside je true kdyz k nemu doslo uvnitr touch arey buttonu.

	public delegate void TouchDelegate2(GUIBase_Widget w);
	public delegate void ReleaseDelegate2(GUIBase_Widget w); //pri uvolneni buttonu
	public delegate void CancelDelegate2(GUIBase_Widget w); //zruseni akce (napr. hide buttonu kdyz je stisknuty)

	public delegate void TouchDelegate3(GUIBase_Widget widget, object evt);

	TouchDelegate m_TouchDelegate;
	ReleaseDelegate m_ReleaseDelegate;

	TouchDelegate2 m_TouchDelegate2;
	ReleaseDelegate2 m_ReleaseDelegate2;
	CancelDelegate2 m_CancelDelegate2;

	TouchDelegate3 m_TouchDelegate3;
	TouchDelegate3 m_ReleaseDelegate3; //pri uvolneni buttonu
	TouchDelegate3 m_CancelDelegate3; //zruseni akce (napr. hide buttonu kdyz je stisknuty)

	GUIBase_Widget m_Widget; //nas widget
	//MFGuiRenderer			m_GuiRenderer;

	public GUIBase_Widget Widget
	{
		get { return m_Widget; }
	}

	enum E_ButtonSubstate
	{
		E_BSS_IN,
		E_BSS_LOOP,
		E_BSS_OUT
	};

	E_ButtonState m_CurrentState = E_ButtonState.E_BS_NONE;
	E_ButtonState m_NextState = E_ButtonState.E_BS_NONE;
	E_ButtonState m_PrevState = E_ButtonState.E_BS_NONE;
	E_ButtonSubstate m_Substate = E_ButtonSubstate.E_BSS_LOOP;

	Animation m_Anim;

	bool m_WasTouched = false;

	bool m_Disabled = false;
	bool m_IsDown = false;
	bool m_ForceHighlight = false;
	[SerializeField] bool m_AnimateHighlight = false;
	float m_AnimateAlpha = 0.0f;
	bool m_AnimateIn = true;

	bool m_IsDirty = true;

	[SerializeField] string m_Text;
	[LocalizedTextId] [SerializeField] int m_TextID;

	[SerializeField] Vector2 m_TextPadding = new Vector2(10, 10);

	public bool IsDisabled
	{
		get { return m_Disabled; }
		set { SetDisabled(value); }
	}

	public bool autoColorLabels { get; set; }

	public bool stayDown { get; set; }

	public bool isDown
	{
		get { return m_IsDown; }
		set { ForceDownStatus(value); }
	}

	public bool isHighlighted
	{
		get { return m_ForceHighlight; }
		set { ForceHighlight(value); }
	}

	public bool animate
	{
		get { return m_AnimateHighlight; }
		set { SetAnimate(value); }
	}

#if MADFINGER_KEYBOARD_MOUSE
				//Button's textfield functionality
	string					m_TextFieldText = "";
	GuiScreen				m_TextFieldOwner = null;
	GuiScreen.KeyboardClose m_TextFieldDelegate = null;
	GUIBase_TextArea		m_TextFieldTextArea = null;
	int						m_TextFieldMaxLength = -1;
	int 					m_TextFieldMaxLines = -1;
	
	bool IsTextField {get {return m_TextFieldOwner != null;}}
	public GuiScreen.KeyboardClose TextFieldDelegate {get {return m_TextFieldDelegate;}}
	public bool TextFieldIsMultiline {get {return m_TextFieldTextArea != null;}}
#endif

	public string TextFieldText {
#if MADFINGER_KEYBOARD_MOUSE
		get {return m_TextFieldText;}	
		set 
		{
			if (m_TextFieldMaxLength >= 0 && value.Length > m_TextFieldMaxLength)
				value = value.Substring(0, m_TextFieldMaxLength);
			
			if (m_TextFieldMaxLines > 0 && TextFieldIsMultiline)
			{
				int lines = m_TextFieldTextArea.GetLineCount(value);
				if (lines > m_TextFieldMaxLines)
					return;
			}
			m_TextFieldText = value;
		}
#else
		get; set;
#endif
	}

	//---------------------------------------------------------
	void Awake()
	{
		m_Disabled = initStateDisabled;

		// setup proxy flag
		m_Widget = GetComponent<GUIBase_Widget>();
		m_Widget.DisallowShowRecursive = true;
		m_Widget.InputEnabled = !m_Disabled;

		// remove proxy flag from all sprites
		for (E_ButtonState state = E_ButtonState.E_BS_IDLE; state < E_ButtonState.E_BS_LAST_IDX; ++state)
		{
			GUIBase_Widget widget = GetWidgetForState(state);
			if (widget != null)
			{
				widget.CreateMainSprite = true;
			}
		}
	}

	//---------------------------------------------------------
	void Start()
	{
		CreateIdleSpriteIfNeeded();

		m_Anim = GetComponent<Animation>();

		int callbackMask = (int)E_CallbackType.E_CT_INIT
						   + (int)E_CallbackType.E_CT_SHOW
						   + (int)E_CallbackType.E_CT_HIDE
						   + (int)E_CallbackType.E_CT_ON_TOUCH_BEGIN
						   + (int)E_CallbackType.E_CT_COLOR;

		if (buttonDownSprite)
		{
			callbackMask += (int)E_CallbackType.E_CT_ON_TOUCH_END;
		}

		if (mouseOverSprite)
		{
			callbackMask += (int)E_CallbackType.E_CT_ON_MOUSEOVER_BEGIN + (int)E_CallbackType.E_CT_ON_MOUSEOVER_END;
		}

		m_Widget.RegisterCallback(this, callbackMask);
		m_Widget.RegisterUpdateDelegate(UpdateButton);
	}

	//---------------------------------------------------------
	void LateUpdate()
	{
		if (m_IsDirty == true && m_Widget.IsVisible())
		{
			UpdateChildrenVisibility(Widget.Visible);
			ForceUpdateButtonVisualState();
			m_IsDirty = false;
		}

		AnimateButton();
	}

	//---------------------------------------------------------
	public void SetNewText(int inTextID)
	{
		m_Text = string.Empty;
		m_TextID = inTextID;

		GUIBase_Label label = GetLabelForState(m_CurrentState);
		if (label == null)
			return;

		label.SetNewText(inTextID);
	}

	public void SetNewText(string inText)
	{
		m_Text = inText;
		m_TextID = 0;

		GUIBase_Label label = GetLabelForState(m_CurrentState);
		if (label == null)
			return;

		label.SetNewText(inText);
	}

	public string GetText()
	{
		return m_TextID != 0 ? TextDatabase.instance[m_TextID] : m_Text;
	}

	//---------------------------------------------------------

	public void SetDisabled(bool disabled)
	{
		if (m_Disabled == disabled)
			return;

		m_Disabled = disabled;
		Widget.InputEnabled = !disabled;

		if (autoColorLabels == true)
		{
			GUIBase_Label label = GetLabelForState(E_ButtonState.E_BS_IDLE);
			if (label != null && label.Widget != null)
			{
				label.Widget.FadeAlpha = m_Disabled ? 0.5f : 1.0f;
			}
		}

		m_IsDirty = true;
	}

	//---------------------------------------------------------

	public void ForceHighlight(bool on)
	{
		if (m_ForceHighlight != on)
		{
			m_ForceHighlight = on;

			ResetAnimation();

			m_IsDirty = true;
		}
	}

	//---------------------------------------------------------	
	public void ForceDownStatus(bool inDown)
	{
		if (m_IsDown != inDown)
		{
			m_IsDown = inDown;

			m_IsDirty = true;
		}
		else if (inDown == false && stayDown == false && m_WasTouched == true)
		{
			m_WasTouched = false;

			m_IsDirty = true;
		}
	}

	//---------------------------------------------------------	
	public void SetAnimate(bool state)
	{
		if (m_AnimateHighlight == state)
			return;
		m_AnimateHighlight = state;

		ResetAnimation();

		m_IsDirty = true;
	}

	//---------------------------------------------------------	
	void ForceUpdateButtonVisualState()
	{
		//Debug.Log("GUIBase_Button<"+name+">.ForceUpdateButtonVisualState()");

		if (m_Widget.IsVisible() == false)
			return;

		if (m_Disabled == true)
		{
			ChangeButtonState(E_ButtonState.E_BS_DISABLED);
		}
		else if (m_IsDown == true)
		{
			ChangeButtonState(E_ButtonState.E_BS_BUTTON_DOWN);
		}
		else if (m_ForceHighlight == true && m_AnimateHighlight == false)
		{
			ChangeButtonState(E_ButtonState.E_BS_BUTTON_HIGHLIGHT);
		}
		else
		{
			ChangeButtonState(E_ButtonState.E_BS_IDLE);
		}
	}

	//---------------------------------------------------------
	void UpdateChildrenVisibility(bool state)
	{
		Transform transform = this.transform;
		foreach (Transform childTransform in transform)
		{
			GUIBase_Sprite child = childTransform.GetComponent<GUIBase_Sprite>();
			if (child == null)
				continue;

			if (idleSprite == child)
				continue;
			if (disabledSprite == child)
				continue;
			if (mouseOverSprite == child)
				continue;
			if (buttonDownSprite == child)
				continue;
			if (highlightSprite == child)
				continue;

			child.Widget.ShowImmediate(state, true);
		}

		ResetAnimation();
	}

	//---------------------------------------------------------
	public void RegisterTouchDelegate(TouchDelegate d)
	{
		m_TouchDelegate = d;
	}

	//---------------------------------------------------------
	public void RegisterReleaseDelegate(ReleaseDelegate d)
	{
		m_ReleaseDelegate = d;
	}

	//---------------------------------------------------------

	public void RegisterTouchDelegate2(TouchDelegate2 d)
	{
		m_TouchDelegate2 = d;
	}

	//---------------------------------------------------------

	public void RegisterReleaseDelegate2(ReleaseDelegate2 d)
	{
		m_ReleaseDelegate2 = d;
	}

	//---------------------------------------------------------

	public void RegisterCancelDelegate2(CancelDelegate2 d)
	{
		m_CancelDelegate2 = d;
	}

	//---------------------------------------------------------

	public void RegisterTouchDelegate3(TouchDelegate3 dlgt)
	{
		m_TouchDelegate3 = dlgt;
	}

	//---------------------------------------------------------

	public void RegisterReleaseDelegate3(TouchDelegate3 dlgt)
	{
		m_ReleaseDelegate3 = dlgt;
	}

	//---------------------------------------------------------

	public void RegisterCancelDelegate3(TouchDelegate3 dlgt)
	{
		m_CancelDelegate3 = dlgt;
	}

	//---------------------------------------------------------
	public override bool Callback(E_CallbackType type, object evt)
	{
		switch (type)
		{
		case E_CallbackType.E_CT_INIT:
			m_IsDirty = true;
			return true;

		case E_CallbackType.E_CT_SHOW:

			m_WasTouched = false;

			// force update children and states
			UpdateChildrenVisibility(Widget.Visible);
			ForceUpdateButtonVisualState();

			// set dirty flag anyway
			// so it will update to correct state later if needed
			m_IsDirty = true;

			return true;

		case E_CallbackType.E_CT_HIDE:
			m_CurrentState = E_ButtonState.E_BS_NONE;
			m_Substate = E_ButtonSubstate.E_BSS_LOOP;

			if (stayDown == false)
				m_IsDown = false;

			//pri sryti butonu zavolej release na buttony ktere mohli byt stisknute
			if (m_ReleaseDelegate != null)
			{
				m_ReleaseDelegate(false);
			}

			if (m_CancelDelegate2 != null)
			{
				m_CancelDelegate2(m_Widget);
			}

			if (m_CancelDelegate3 != null)
			{
				m_CancelDelegate3(m_Widget, evt);
			}

			return true;

		//
		// Touch begin
		//
		case E_CallbackType.E_CT_ON_TOUCH_BEGIN:

			if (m_Disabled)
			{
				if (disabledSound != null)
				{
					MFGuiManager.Instance.PlayOneShot(disabledSound);
				}

				break;
			}

			m_WasTouched = true;

			if (stayDown == true)
				m_IsDown = true;

			if (m_ParentWidget)
			{
				m_ParentWidget.ChildButtonPressed(m_ValueChangedInParent);
			}

			if (buttonDownSprite)
			{
				ChangeButtonState(E_ButtonState.E_BS_BUTTON_DOWN);
			}
			else
			{
				MFGuiManager.Instance.PlayOneShot(buttonDownSoundIn);
			}

			if (m_TouchDelegate != null)
			{
				m_TouchDelegate();
				//Debug.Log("On touch - BUTTON" + name);
			}

			if (m_TouchDelegate2 != null)
			{
				m_TouchDelegate2(m_Widget);
			}

			if (m_TouchDelegate3 != null)
			{
				m_TouchDelegate3(m_Widget, evt);
			}

			return true;

		//
		/// Touch release
		//
		case E_CallbackType.E_CT_ON_TOUCH_END:

			if (m_Disabled)
				break;

			if (stayDown == false)
				m_IsDown = false;

			if (m_WasTouched)
			{
				m_WasTouched = false;
				m_IsDirty = true;

				if (m_ReleaseDelegate != null)
				{
					m_ReleaseDelegate(true);
				}

				if (m_ReleaseDelegate2 != null)
				{
					m_ReleaseDelegate2(m_Widget);
				}
#if MADFINGER_KEYBOARD_MOUSE
				if (IsTextField)
					m_TextFieldOwner.UpdateFocus(this);
#endif
				if (m_ReleaseDelegate3 != null)
				{
					m_ReleaseDelegate3(m_Widget, evt);
				}
			}

			return true;

		//
		// Touch release (not over button but outside of its)
		//
		case E_CallbackType.E_CT_ON_TOUCH_END_OUTSIDE:

			if (m_Disabled)
				break;

			if (stayDown == false)
				m_IsDown = false;

			if (m_WasTouched)
			{
				m_WasTouched = false;
				//pokud jsme se dotkli buttonu a pustili jej mimo, povazujme to za uspesne clicknuti pro pohodlnejsi ovladani na malych displayich
				if (m_ReleaseDelegate != null)
				{
					m_ReleaseDelegate(true);
				}

				if (m_ReleaseDelegate2 != null)
				{
					m_ReleaseDelegate2(m_Widget);
				}
#if MADFINGER_KEYBOARD_MOUSE
				if (IsTextField)
					m_TextFieldOwner.UpdateFocus(this);
#endif
				if (m_ReleaseDelegate3 != null)
				{
					m_ReleaseDelegate3(m_Widget, evt);
				}
			}
			else
			{
				if (m_ReleaseDelegate != null)
				{
					m_ReleaseDelegate(false);
				}

				if (m_CancelDelegate2 != null)
				{
					m_CancelDelegate2(m_Widget);
				}

				if (m_CancelDelegate3 != null)
				{
					m_CancelDelegate3(m_Widget, evt);
				}
			}

			m_IsDirty = true;

			// Send "info" to parent widget about mouse button released somewhere outside of this widget
			if (m_ParentWidget)
			{
				m_ParentWidget.ChildButtonReleased();
			}

			return true;

		// SOUCAST HACKU s inputem z klavesnice - viz. GUIBase_Layout - input
		case E_CallbackType.E_CT_ON_TOUCH_END_KEYBOARD:

			if (m_Disabled)
				break;

			m_IsDown = (m_IsDown && stayDown);

			m_IsDirty = true;

			if (m_WasTouched)
			{
				m_WasTouched = false;

				if (m_ReleaseDelegate != null)
				{
					m_ReleaseDelegate(true);
				}

				if (m_ReleaseDelegate2 != null)
				{
					m_ReleaseDelegate2(m_Widget);
				}

				if (m_ReleaseDelegate3 != null)
				{
					m_ReleaseDelegate3(m_Widget, evt);
				}
			}

			return true;

		//
		// Mouse over button
		//
		case E_CallbackType.E_CT_ON_MOUSEOVER_BEGIN:

			if (m_Disabled)
				break;

			if (mouseOverSprite && m_IsDown == false)
			{
				ChangeButtonState(E_ButtonState.E_BS_MOUSE_OVER);
			}
			return true;

		//
		// End of mouse over
		//
		case E_CallbackType.E_CT_ON_MOUSEOVER_END:

			if (m_Disabled)
				break;

			m_IsDirty = true;

			return true;

		case E_CallbackType.E_CT_COLOR:

			UpdateColorForStates();

			return true;
		}

		return false;
	}

	//---------------------------------------------------------
	public override void GetTouchAreaScale(out float scaleWidth, out float scaleHeight)
	{
		scaleWidth = m_TouchableAreaWidthScale;
		scaleHeight = m_TouchableAreaHeightScale;
	}

	//---------------------------------------------------------
	void SwitchButtonSprite(E_ButtonState nextState, E_ButtonState prevState)
	{
		//Debug.Log("GUIBase_Button<"+this.GetFullName('.')+">.SwitchButtonSprite("+nextState+", "+prevState+")");

		HideAllNonActiveStates(nextState, prevState);

		// update sprites
		GUIBase_Widget prevWidget = GetWidgetForState(prevState);
		GUIBase_Widget nextWidget = GetWidgetForState(nextState);

		if (prevWidget != null && prevWidget != nextWidget)
		{
			prevWidget.ShowImmediate(false, true);
		}

		if (nextWidget != null)
		{
			nextWidget.ShowImmediate(Widget.Visible, true);
		}

		// update labels
		GUIBase_Label prevLabel = GetLabelForState(prevState);
		GUIBase_Label nextLabel = GetLabelForState(nextState);

		if (prevLabel != null && prevLabel.Widget != null && prevLabel != nextLabel)
		{
			prevLabel.Widget.ShowImmediate(false, true);
		}

		if (nextLabel != null && nextLabel.Widget != null)
		{
			// update label
			if (m_TextID != 0)
				nextLabel.SetNewText(m_TextID);
			else if (m_Text.Length > 0)
				nextLabel.SetNewText(m_Text);

			// update label bounds
			nextLabel.Boundaries = GetClientRect();

			// force display label
			nextLabel.Widget.ShowImmediate(Widget.Visible, true);
		}
	}

	//---------------------------------------------------------
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

	public Rect GetTouchRect()
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

		float width = widget.GetWidth()*lossyScale.x*m_TouchableAreaWidthScale;
		float height = widget.GetHeight()*lossyScale.y*m_TouchableAreaHeightScale;
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

		Rect rect = GetRect();
		Vector2 touchSize = new Vector2(
						rect.width*m_TouchableAreaWidthScale,
						rect.height*m_TouchableAreaHeightScale
						);
		Rect touchRect = new Rect(
						rect.center.x - touchSize.x*0.5f,
						rect.center.y - touchSize.y*0.5f,
						touchSize.x,
						touchSize.y
						);

		GuiBaseUtils.RenderRect(GetClientRect(), Color.yellow);
		GuiBaseUtils.RenderRect(touchRect, Color.blue);
		GuiBaseUtils.RenderRect(rect, Color.red);
	}
#endif

	//---------------------------------------------------------
	public void ChangeButtonState(E_ButtonState newState)
	{
		//Debug.Log("GUIBase_Button<"+this.GetFullName('.')+">.ChangeButtonState("+newState+")");

		// immediate
		if (newState == E_ButtonState.E_BS_NONE)
		{
			m_CurrentState = newState;
			m_Substate = E_ButtonSubstate.E_BSS_LOOP;

			// Stop animations
			m_Widget.StopAnim(m_Anim);
			return;
		}

		if (m_CurrentState != newState)
		{
			switch (m_CurrentState)
			{
			case E_ButtonState.E_BS_NONE:
				SwitchButtonSprite(newState, m_CurrentState);

				m_CurrentState = newState;
				m_Substate = E_ButtonSubstate.E_BSS_IN;
				break;

			case E_ButtonState.E_BS_IDLE:
			case E_ButtonState.E_BS_DISABLED:
			case E_ButtonState.E_BS_MOUSE_OVER:
			case E_ButtonState.E_BS_BUTTON_DOWN:
			case E_ButtonState.E_BS_BUTTON_HIGHLIGHT:
				m_Substate = E_ButtonSubstate.E_BSS_OUT;
				m_NextState = newState;
				break;
			}

			// update it immediately in this frame

			while (m_Substate != E_ButtonSubstate.E_BSS_LOOP)
			{
				UpdateButton();
			}
		}
		else if (m_Substate == E_ButtonSubstate.E_BSS_LOOP)
		{
			HideAllNonActiveStates(m_CurrentState, m_CurrentState);
		}
	}

	//---------------------------------------------------------
	void UpdateButton()
	{
		/*if(name == "Help_Button")
		{
			Debug.Log("Updated button: "  +  name  + " state: " + m_CurrentState.ToString() + "  substate: " + m_Substate.ToString());
		}*/

		switch (m_CurrentState)
		{
		case E_ButtonState.E_BS_IDLE:
			UpdateSubstate(idleAnimationLoop, null, null);
			break;
		case E_ButtonState.E_BS_DISABLED:
			UpdateSubstate(disabledAnimationLoop, null, null);
			break;
		case E_ButtonState.E_BS_MOUSE_OVER:
			UpdateSubstate(mouseOverAnimationLoop, mouseOverSoundIn, null);
			break;
		case E_ButtonState.E_BS_BUTTON_DOWN:
			UpdateSubstate(buttonDownAnimationLoop, buttonDownSoundIn, buttonDownSoundOut);
			break;
		case E_ButtonState.E_BS_BUTTON_HIGHLIGHT:
			UpdateSubstate(null, null, null);
			break;
		default:
			break;
		}

		// update label bounds
		GUIBase_Label label = GetLabelForState(m_CurrentState);
		if (label != null)
		{
			label.Boundaries = GetClientRect();
		}
	}

	//---------------------------------------------------------
	void UpdateSubstate(AnimationClip animLoop, AudioClip sndIn, AudioClip sndOut)
	{
		switch (m_Substate)
		{
		case E_ButtonSubstate.E_BSS_IN:

			if (m_PrevState != E_ButtonState.E_BS_BUTTON_DOWN)
			{
				if (sndIn)
				{
					MFGuiManager.Instance.PlayOneShot(sndIn);
				}
			}

			m_Substate = E_ButtonSubstate.E_BSS_LOOP;

			if (animLoop)
			{
				m_Anim.clip = animLoop;

				m_Widget.StopAnim(m_Anim);
				m_Widget.PlayAnim(m_Anim, this.Widget);
			}

			break;

		case E_ButtonSubstate.E_BSS_LOOP:

			break;

		case E_ButtonSubstate.E_BSS_OUT:

			if (animLoop)
			{
				m_Widget.StopAnim(m_Anim);
			}

			if (sndOut)
			{
				MFGuiManager.Instance.PlayOneShot(sndOut);
			}

			SwitchButtonSprite(m_NextState, m_CurrentState);

			m_PrevState = m_CurrentState;
			m_CurrentState = m_NextState;
			m_Substate = E_ButtonSubstate.E_BSS_IN;

			break;
		}
	}

	void AnimateButton()
	{
		if (Widget == null)
			return;
		if (Widget.Visible == false)
			return;
		if (m_AnimateHighlight == false)
			return;
		if (m_ForceHighlight == false)
			return;
		if (m_CurrentState == E_ButtonState.E_BS_NONE)
			return;

		GUIBase_Sprite state = GetSpriteForState(E_ButtonState.E_BS_BUTTON_HIGHLIGHT);
		if (state == null)
			return;

		m_AnimateAlpha = m_AnimateIn == true ? m_AnimateAlpha + Time.deltaTime : m_AnimateAlpha - Time.deltaTime;
		if (m_AnimateAlpha < 0.0f)
		{
			m_AnimateIn = true;
		}
		else if (m_AnimateAlpha > 1.0f)
		{
			m_AnimateIn = false;
		}

		state.Widget.SetFadeAlpha(m_AnimateAlpha, true);
		state.Widget.SetModify(true);
	}

	void ResetAnimation()
	{
		GUIBase_Widget widget = GetWidgetForState(E_ButtonState.E_BS_BUTTON_HIGHLIGHT);
		if (widget == null)
			return;

		m_AnimateAlpha = 0.0f;
		m_AnimateIn = true;

		widget.SetFadeAlpha(m_AnimateHighlight == true ? m_AnimateAlpha : 1.0f, true);
		widget.SetModify(true);

		if (m_AnimateHighlight == true)
		{
			widget.Show(Widget.Visible && m_ForceHighlight, true);
		}
	}

	// PRIVATE METHODS

	GUIBase_Sprite GetSpriteForState(E_ButtonState state)
	{
		GUIBase_Sprite sprite = idleSprite;
		switch (state)
		{
		case E_ButtonState.E_BS_DISABLED:
			sprite = disabledSprite;
			break;
		case E_ButtonState.E_BS_MOUSE_OVER:
			sprite = mouseOverSprite;
			break;
		case E_ButtonState.E_BS_BUTTON_DOWN:
			sprite = buttonDownSprite;
			break;
		case E_ButtonState.E_BS_BUTTON_HIGHLIGHT:
			sprite = highlightSprite ?? mouseOverSprite;
			break;
		default:
			break;
		}
		return sprite;
	}

	GUIBase_Widget GetWidgetForState(E_ButtonState state)
	{
		GUIBase_Sprite sprite = GetSpriteForState(state);
		return sprite ? sprite.GetComponent<GUIBase_Widget>() : null;
	}

	GUIBase_Label GetLabelForState(E_ButtonState state)
	{
		// try to get label for asked state
		GUIBase_Sprite sprite = GetSpriteForState(state);
		if (sprite != null)
		{
			foreach (Transform child in sprite.transform)
			{
				GUIBase_Label label = child.GetComponent<GUIBase_Label>();
				if (label != null)
					return label;
			}
		}

		// try to get default label if any
		foreach (Transform child in this.transform)
		{
			GUIBase_Label label = child.GetComponent<GUIBase_Label>();
			if (label != null)
				return label;
		}

		// not found
		return null;
	}

	void UpdateColorForStates()
	{
		Color color = m_Widget.Color;

		for (E_ButtonState state = E_ButtonState.E_BS_IDLE; state < E_ButtonState.E_BS_LAST_IDX; ++state)
		{
			GUIBase_Widget widget = GetWidgetForState(state);
			if (widget != null)
			{
				widget.Color = color;
			}
		}
	}

	void HideAllNonActiveStates(E_ButtonState currState, E_ButtonState prevState)
	{
		GUIBase_Widget currWidget = GetWidgetForState(currState);
		GUIBase_Widget prevWidget = prevState != currState ? GetWidgetForState(prevState) : null;

		for (E_ButtonState state = E_ButtonState.E_BS_IDLE; state < E_ButtonState.E_BS_LAST_IDX; ++state)
		{
			GUIBase_Widget widget = GetWidgetForState(state);
			if (widget == null)
				continue;
			if (widget == currWidget)
				continue;
			if (widget == prevWidget)
				continue;

			widget.ShowImmediate(false, true);
		}
	}

	// this method is used by build process to cache states
	public void CreateIdleSpriteIfNeeded()
	{
		if (idleSprite != null)
			return;

		GUIBase_Widget thisWidget = this.GetComponent<GUIBase_Widget>();
		if (thisWidget == null)
			return;

		Object idleObject = GameObject.Instantiate(thisWidget);
		GUIBase_Widget idleWidget = idleObject as GUIBase_Widget;
		if (idleWidget == null)
		{
			Object.DestroyImmediate(idleObject);
			return;
		}

		// set new name
		idleWidget.name = "GUI_button_idle";

		// clear all unwanted flags
		idleWidget.m_VisibleOnLayoutShow = false;
		idleWidget.CreateMainSprite = true;

		// remove all children
		foreach (Transform child in idleWidget.transform)
		{
			Object.Destroy(child.gameObject);
		}

		// remove all components but widget
		GUIBase_Button tempBtn = idleWidget.GetComponent<GUIBase_Button>();
		if (tempBtn != null)
		{
			Object.DestroyImmediate(tempBtn);
		}
		AudioSource tempAudio = idleWidget.GetComponent<AudioSource>();
		if (tempAudio != null)
		{
			Object.DestroyImmediate(tempAudio);
		}
		Animation tempAnim = idleWidget.GetComponent<Animation>();
		if (tempAnim != null)
		{
			Object.DestroyImmediate(tempAnim);
		}

		// create sprite component
		idleSprite = idleWidget.gameObject.AddComponent<GUIBase_Sprite>();

		// set parent and position
		Transform idleTransform = idleSprite.transform;
		idleTransform.parent = this.transform;
		idleTransform.localPosition = Vector3.zero;
		idleTransform.localScale = Vector3.one;
		idleTransform.localRotation = Quaternion.identity;
	}

	// Enable/Disable button's text field function
	// Owner is a screen that manages this button, if it's null, text field function is disabled.
	// If multilineTextArea is null the text field is singlelined and it uses it's own label to determine caret position,
	// otherwise the text field is multilined and it uses the multilineTextArea to determine caret position.
	public void SetTextField(GuiScreen owner,
							 GuiScreen.KeyboardClose update,
							 GUIBase_TextArea multilineTextArea = null,
							 int maxLength = -1,
							 int maxLines = -1)
	{
#if MADFINGER_KEYBOARD_MOUSE
		m_TextFieldOwner = owner;
		m_TextFieldTextArea = multilineTextArea;
		m_TextFieldDelegate = update;
		m_TextFieldMaxLength = maxLength;
		m_TextFieldMaxLines = maxLines;
		
		if (m_TextFieldTextArea != null)
			m_TextFieldTextArea.IsForTextField = true;
#endif
	}

#if MADFINGER_KEYBOARD_MOUSE
	public void SetTextFieldOwner(GuiScreen owner)
	{
		m_TextFieldOwner = owner;
	}

	public bool GetCaretPositionAndHeight(int textPos, out Vector3 pos, out float height)
	{
		pos = new Vector3(0, 0, 0);
		height = 50;		
		
		GUIBase_Widget textWidget = null;
		string text = null;
		
		if (m_TextFieldTextArea != null)
		{
			textWidget = m_TextFieldTextArea.Widget;
			text = m_TextFieldTextArea.text;
		}
		else
		{
			GUIBase_Label label = GetLabelForState(m_CurrentState);
			if (label != null)
			{
				textWidget = label.Widget;
				text = label.GetText();
			}
		}
		
		if (textPos >= text.Length)
			return false;
		
		//sometimes Unity gui is faster than MF gui and sprites are not yet updated, 
		//this will roughly check for this situation
		int newLines = text.Count(f => f == '\n');
		int checkIdx = text.Length - 1 - newLines;
		//The situation is sometimes even worse. Some widgets tends to generate the sprites in context of OnGUIUpdate call which
		//is called in context of Unity's LateUpdate() but this update is called sooner than the LateUpdate(). In other words
		//it results in some scenarios in accessing invalid sprite index.
		if (!textWidget.IsSpriteIndexValid(checkIdx))
			return false;
		MFGuiSprite lastSprite = textWidget.GetSprite(checkIdx);
		if (lastSprite == null || !lastSprite.visible)
			return false;
		
		Vector2 spriteSize;
		Vector3 spritePosition;
		MFGuiSprite sprite = textWidget.GetSprite(textPos);
		if (sprite == null || !sprite.GetSpriteSizeAndPosition(out spriteSize, out spritePosition))
			return false;
		
		pos.x = Screen.width / 2 + spritePosition.x;
		pos.y = Screen.height / 2 - spritePosition.y + spriteSize.y / 2;
		pos.z = spritePosition.z;
		
		height = spriteSize.y;
		
		return true;
	}
#endif
}
