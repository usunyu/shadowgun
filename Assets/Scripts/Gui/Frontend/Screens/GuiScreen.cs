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
using ComponentContainer = GuiComponentContainer<string, GuiScreen>;
using System;
using System.Runtime.Serialization;

// =====================================================================================================================
// =====================================================================================================================
public class MFScreenInitException : UnityException
{
	public MFScreenInitException() : base("Error during Screen initialization")
	{
	}

	public MFScreenInitException(string message) : base(message)
	{
	}

	public MFScreenInitException(string message, Exception innerException) : base(message, innerException)
	{
	}

	protected MFScreenInitException(SerializationInfo info, StreamingContext context) : base(info, context)
	{
	}
}

// =====================================================================================================================
// =====================================================================================================================

/*public enum E_ScreenInputAction
{
	MoveUp,
	MoveDown,
	MoveLeft,
	MoveRight,
	Press,
	Release,
	Back,
}*/

// =====================================================================================================================
// =====================================================================================================================
public abstract class GuiScreen : GuiView
{
	// PRIVATE MEMBERS

	ComponentContainer m_Components;

	// PROTECTED MEMBERS

	protected GUIBase_Pivot m_ScreenPivot { get; set; }
	[SerializeField] protected GUIBase_Layout m_ScreenLayout;

	// PUBLIC MEMBERS

	[HideInInspector] public int MultiPageIndex = -1;

	// GETTERS / SETTERS

	public override GUIBase_Layout Layout
	{
		get { return m_ScreenLayout; }
	}

	// PUBLIC METHODS

	public T RegisterComponent<T>() where T : ScreenComponent, new()
	{
		return RegisterComponent<T>(typeof (T).Name);
	}

	public T RegisterComponent<T>(string name) where T : ScreenComponent, new()
	{
		if (m_Components == null)
		{
			m_Components = new GuiComponentContainer<string, GuiScreen>();
		}
		return m_Components.Create<T>(name, this);
	}

	// GUIVIEW INTERFACE

	protected override void OnViewDestroy()
	{
		if (m_Components != null)
		{
			m_Components.Destroy(this);
		}

		base.OnViewDestroy();
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		if (m_Components != null)
		{
			m_Components.Show();
		}

#if MADFINGER_KEYBOARD_MOUSE
		foreach(GUIBase_Button textField in m_TextFields)
			textField.SetTextFieldOwner(this);
		UpdateFocus(null);
#endif
	}

	protected override void OnViewHide()
	{
		if (m_Components != null)
		{
			m_Components.Hide();
		}

		base.OnViewHide();
	}

	protected override void OnViewUpdate()
	{
		if (m_Components != null)
		{
			m_Components.Update();
		}

		base.OnViewUpdate();
	}

	protected override GUIBase_Widget OnViewHitTest(ref Vector2 point)
	{
		GUIBase_Widget widget = base.OnViewHitTest(ref point);
		if (widget != null)
			return widget;

		return Layout != null ? Layout.HitTest(ref point) : null;
	}

	protected override bool OnViewProcessInput(ref IInputEvent evt)
	{
		if (base.OnViewProcessInput(ref evt) == true)
			return true;

		if (m_Components != null)
		{
			foreach (var entry in m_Components.Components)
			{
				ScreenComponent component = (ScreenComponent)entry;
				if (component.ProcessInput(ref evt) == true)
					return true;
			}
		}

		return Layout != null ? Layout.ProcessInput(ref evt) : false;
	}

	// PROTECTED METHODS	

	protected GUIBase_Pivot GetPivot(string inPivotName)
	{
		GUIBase_Pivot pivot = MFGuiManager.Instance.GetPivot(inPivotName);
		if (pivot == null)
		{
			throw new MFScreenInitException("Can't find pivot with name [ " + inPivotName + " ]");
		}

		return pivot;
	}

	protected GUIBase_Layout GetLayout(string inPivotName, string inLayoutName)
	{
		GUIBase_Pivot pivot = MFGuiManager.Instance.GetPivot(inPivotName);
		if (pivot == null)
		{
			throw new MFScreenInitException("Can't find pivot with name [ " + inPivotName + " ]");
		}

		GUIBase_Layout layout = pivot.GetLayout(inLayoutName);
		if (layout == null)
		{
			throw new MFScreenInitException("Can't find layout with name [ " + inLayoutName + " ]");
		}

		return layout;
	}

	protected GUIBase_Widget GetWidget(string inName)
	{
		return GetWidget(m_ScreenLayout, inName);
	}

	public GUIBase_Widget GetWidget(GUIBase_Layout inLayout, string inName)
	{
		GUIBase_Widget widget = inLayout.GetWidget(inName);
		if (widget == null)
		{
			throw new MFScreenInitException("Can't find widget with name [ " + inName + " ]");
		}
		return widget;
	}

	protected GUIBase_Button PrepareButton(string inName,
										   GUIBase_Button.TouchDelegate2 inTouchDlgt,
										   GUIBase_Button.ReleaseDelegate2 inReleaseDlgt)
	{
		return PrepareButton(m_ScreenLayout, inName, inTouchDlgt, inReleaseDlgt);
	}

	protected GUIBase_Button PrepareButton(string inName,
										   GUIBase_Button.TouchDelegate2 inTouchDlgt,
										   GUIBase_Button.ReleaseDelegate2 inReleaseDlgt,
										   bool inAutoColorLabels,
										   bool inStayDown)
	{
		GUIBase_Button b = PrepareButton(m_ScreenLayout, inName, inTouchDlgt, inReleaseDlgt);
		if (b != null)
		{
			b.autoColorLabels = inAutoColorLabels;
			b.stayDown = inStayDown;
		}
		return b;
	}

	protected GUIBase_Button PrepareButton(GUIBase_Layout inLayout,
										   string inName,
										   GUIBase_Button.TouchDelegate2 inTouchDlgt,
										   GUIBase_Button.ReleaseDelegate2 inRreleaseDlgt)
	{
		GUIBase_Button button = GetWidget(inLayout, inName).GetComponent<GUIBase_Button>();
		if (button == null)
		{
			throw new MFScreenInitException("Widget [ " + inName + " } dosn't have button component");
		}

		button.RegisterTouchDelegate2(inTouchDlgt);
		button.RegisterReleaseDelegate2(inRreleaseDlgt);
		return button;
	}

	protected GUIBase_Button RegisterButtonDelegate(string buttonName,
													GUIBase_Button.TouchDelegate inPressed,
													GUIBase_Button.ReleaseDelegate inReleased)
	{
		if (m_ScreenLayout == null)
		{
			// be quiet when there is not any delegate specified
			if (inPressed != null || inReleased != null)
			{
				Debug.LogError(GetType().Name + "<" + name + ">.RegisterButtonDelegate() :: Attempt to register button '" + buttonName +
							   "' but there is not any layout specified!");
			}
			return null;
		}
		return GuiBaseUtils.RegisterButtonDelegate(m_ScreenLayout, buttonName, inPressed, inReleased);
	}

	protected GUIBase_Roller RegisterRollerDelegate(string rollerName, GUIBase_Roller.ChangeDelegate inChanged)
	{
		if (m_ScreenLayout == null)
		{
			Debug.LogError(GetType().Name + "<" + name + ">.RegisterRollerDelegate() :: Attempt to register roller '" + rollerName +
						   "' but there is not any layout specified!");
			return null;
		}
		return GuiBaseUtils.RegisterRollerDelegate(m_ScreenLayout, rollerName, inChanged);
	}

	protected GUIBase_Slider RegisterSliderDelegate(string sliderName, GUIBase_Slider.ChangeValueDelegate inChanged)
	{
		if (m_ScreenLayout == null)
		{
			Debug.LogError(GetType().Name + "<" + name + ">.RegisterSliderDelegate() :: Attempt to register slider '" + sliderName +
						   "' but there is not any layout specified!");
			return null;
		}
		return GuiBaseUtils.RegisterSliderDelegate(m_ScreenLayout, sliderName, inChanged);
	}

	protected GUIBase_Switch RegisterSwitchDelegate(string inName, GUIBase_Switch.SwitchDelegate inSwitchDlgt)
	{
		return PrepareSwitch(m_ScreenLayout, inName, inSwitchDlgt);
	}

	protected GUIBase_Switch PrepareSwitch(GUIBase_Layout inLayout, string inName, GUIBase_Switch.SwitchDelegate inSwitchDlgt)
	{
		GUIBase_Switch _switch = GetWidget(inLayout, inName).GetComponent<GUIBase_Switch>();
		if (_switch == null)
		{
			throw new MFScreenInitException("Widget [ " + inName + " } dosn't have switch component");
		}

		_switch.RegisterDelegate(inSwitchDlgt);
		return _switch;
	}

	protected GUIBase_Label PrepareLabel(string inName)
	{
		if (m_ScreenLayout == null)
		{
			Debug.LogError(GetType().Name + "<" + name + ">.PrepareLabel() :: Attempt to register label '" + inName +
						   "' but there is not any layout specified!");
			return null;
		}
		return PrepareLabel(m_ScreenLayout, inName);
	}

	protected GUIBase_Label PrepareLabel(GUIBase_Layout inLayout, string inName)
	{
		GUIBase_Label label = GetWidget(inLayout, inName).GetComponent<GUIBase_Label>();
		if (label == null)
		{
			throw new MFScreenInitException("Widget [ " + inName + " } dosn't have label component");
		}

		return label;
	}

	protected GUIBase_TextArea PrepareTextArea(GUIBase_Layout inLayout, string inName)
	{
		GUIBase_TextArea textarea = GetWidget(inLayout, inName).GetComponent<GUIBase_TextArea>();
		if (textarea == null)
		{
			throw new MFScreenInitException("Widget [ " + inName + " } dosn't have TextArea component");
		}

		return textarea;
	}

	protected GUIBase_Number PrepareNumber(GUIBase_Layout inLayout, string inName)
	{
		GUIBase_Number number = GetWidget(inLayout, inName).GetComponent<GUIBase_Number>();
		if (number == null)
		{
			throw new MFScreenInitException("Widget [ " + inName + " } dosn't have number component");
		}

		return number;
	}

	protected void ButtonDisable(GUIBase_Layout inLayout, string inName, bool inDisable)
	{
		GUIBase_Button button = GetWidget(inLayout, inName).GetComponent<GUIBase_Button>();
		if (button == null)
		{
			throw new MFScreenInitException("Widget [ " + inName + " } dosn't have button component");
		}

		button.SetDisabled(inDisable);
	}

	//Simulating gui text fields using buttons and Unity's GUI
#if MADFINGER_KEYBOARD_MOUSE
	List<GUIBase_Button> m_TextFields = new List<GUIBase_Button>();
	GUIBase_Button m_FocusedTextField = null;
	bool m_FocusChanged = false;
	bool m_TextChanged = false;
	

	Material m_CaretMaterial = null;
	protected Color m_CaretColor = Color.white;
	Rect m_CaretRect = new Rect(-1, -1, 0, 50);
	bool m_ShowCaret = false;
	int m_LastCaretPos = -1;
	int m_LastScreenWidth = 0;
	int m_LastScreenHeight = 0;
#endif

	public static bool IsKeyboardControlEnabled
	{
		get
		{
#if MADFINGER_KEYBOARD_MOUSE
			return true;
#else
			return false;
#endif
		}
	}

	public void AddTextField(GUIBase_Button textfield,
							 GuiScreen.KeyboardClose update,
							 GUIBase_TextArea multilineTextArea = null,
							 int maxLength = -1,
							 int maxLines = -1)
	{
#if MADFINGER_KEYBOARD_MOUSE
		textfield.SetTextField(this, update, multilineTextArea, maxLength, maxLines);
		if (!m_TextFields.Contains(textfield))
			m_TextFields.Add(textfield);
#endif
	}

	public void UpdateFocus(GUIBase_Button focusedTextField)
	{
#if MADFINGER_KEYBOARD_MOUSE
		m_FocusChanged = true;
		m_TextChanged = true;
		m_LastCaretPos = -1;		
		
		if (m_FocusedTextField != null)
			m_FocusedTextField.ForceHighlight(false);
		
		m_FocusedTextField = focusedTextField;		
		if (m_FocusedTextField == null)
		{
			HideCaret();
			return;
		}
			
		m_FocusedTextField.ForceHighlight(true);
#endif
	}

#if MADFINGER_KEYBOARD_MOUSE
	void TabPressed()
	{
		if (m_TextFields == null || m_TextFields.Count == 0)
			return;
		
		int next = 0;
		if (m_FocusedTextField != null)
		{
			int focused = m_TextFields.FindIndex(f => f == m_FocusedTextField);
			if (focused < 0)
				return;
			int i = 1;
			do
			{
				next = (focused + i++) % m_TextFields.Count;
				if (!m_TextFields[next].IsDisabled)
					break;
			}
			while (next != focused);
			if (next == focused)
				return;
		}
		m_TextFields[next].Callback(GUIBase_Callback.E_CallbackType.E_CT_ON_TOUCH_BEGIN, null);
		m_TextFields[next].Callback(GUIBase_Callback.E_CallbackType.E_CT_ON_TOUCH_END, null);
	}
	
	void ShowCaret()
	{
		m_ShowCaret = true;		
		
		CancelInvoke("BlinkCaret");
		InvokeRepeating("BlinkCaret", 0.5f, 0.5f);
	}
	
	void HideCaret()
	{
		m_ShowCaret = false;
		
		CancelInvoke("BlinkCaret");		
	}
	
	void BlinkCaret()
	{
		m_ShowCaret = !m_ShowCaret;
	}
	
	void SimulateTextField()
	{
		string oldText = m_FocusedTextField.TextFieldText;

		if (oldText == null)
			oldText = "";
		string newText = "";
		GUI.SetNextControlName(m_FocusedTextField.name);		
		if (m_FocusedTextField.TextFieldIsMultiline)
			newText = GUI.TextArea(new Rect(0,-200,5000,200), oldText);
		else
			newText = GUI.TextField(new Rect(0,-20,5000,20), oldText);
		if (newText != oldText)
		{
			m_TextChanged = true;
			m_FocusedTextField.TextFieldText = newText;
			if (m_FocusedTextField.TextFieldDelegate != null && m_FocusedTextField.TextFieldText != oldText)
				m_FocusedTextField.TextFieldDelegate(m_FocusedTextField, m_FocusedTextField.TextFieldText, false);
		}
	}
	
	protected virtual void OnGUI()
	{
		if (!IsVisible || !IsEnabled)
			return;
		
		Event e = Event.current;
		if (e.keyCode == KeyCode.Tab && e.type == EventType.KeyUp)
		{
			TabPressed();
			return;
		}
		
		if (Screen.width != m_LastScreenWidth || Screen.height != m_LastScreenHeight)
		{
			m_LastScreenWidth = Screen.width;
			m_LastScreenHeight = Screen.height;
			UpdateFocus(null);
			return;
		}
		
		if (m_FocusedTextField != null)
		{
			GUI.FocusControl(m_FocusedTextField.name);
			TextEditor te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
			if (te.text != m_FocusedTextField.TextFieldText)
				te.text  = m_FocusedTextField.TextFieldText;
			if (te != null)
			{
				if (m_FocusChanged)		//if the focus has changed, we make sure that caret is at the end of the field
				{
					if (te.cursorIndex != m_FocusedTextField.TextFieldText.Length)
						te.cursorIndex = m_FocusedTextField.TextFieldText.Length;
					else
						m_FocusChanged = false;
				}
				else if (m_LastCaretPos != te.cursorIndex || m_TextChanged)
				{
					m_TextChanged = false;
					Vector3 newCaretPos;
					float newCaretHeight;
					if (m_FocusedTextField.GetCaretPositionAndHeight(te.cursorIndex, out newCaretPos, out newCaretHeight))
					{
						m_CaretRect.x = newCaretPos.x;
						m_CaretRect.y = newCaretPos.y - newCaretHeight/2;
						m_CaretRect.height = newCaretHeight;
						
						ShowCaret();
						m_LastCaretPos = te.cursorIndex;
					}				
				}
				te.selectIndex = te.cursorIndex;	//this will disable selection of text (using shift+arrow keys)
			}
			SimulateTextField();
			DrawCaret();
		}
		else
			GUI.FocusControl(null);
	}

	Material LoadCarretMaterial()
	{
		/*
		 m_CaretMaterial = new Material( "Shader \"Lines/Colored Blended\" {" +
				"SubShader { Pass { " +
				"    Blend SrcAlpha OneMinusSrcAlpha " +
				"    ZWrite Off Cull Off Fog { Mode Off } " +
				"    BindChannels {" +
				"    Bind \"vertex\", vertex Bind \"color\", color }" +
				"} } }" );

		m_CaretMaterial.hideFlags = HideFlags.HideAndDontSave;
		m_CaretMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
		*/

		return Resources.Load("Effects/m_carret", typeof(Material)) as Material;
	}

	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_CaretMaterial = LoadCarretMaterial();
		if (m_CaretMaterial == null)
		{
			Debug.LogWarning("Caret material failed to load");
		}
	}

	void DrawCaret()
	{
		if (!m_ShowCaret || Event.current.type != EventType.Repaint)
			return;
		
		Rect position = m_CaretRect.MakePixelPerfect();

		if (m_CaretMaterial != null)
		{
			m_CaretMaterial.SetPass(0);
		}
		
		GL.Color (m_CaretColor);
		GL.Begin (GL.QUADS);
			GL.Vertex3 (position.x, position.y, 0);
			GL.Vertex3 (position.x + position.width, position.y, 0);
			GL.Vertex3 (position.x + position.width, position.y + position.height, 0);
			GL.Vertex3 (position.x, position.y + position.height, 0);
		GL.End ();
	}
#endif
}
