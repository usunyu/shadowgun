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

[AddComponentMenu("GUI/Hierarchy/Layout")]
public class GUIBase_Layout : GUIBase_Element
{
	public readonly static int MAX_LAYERS = 10;

	public AnimationClip m_InAnimation;
	public AnimationClip m_OutAnimation;
	public AudioClip m_InSound;
	public AudioClip m_OutSound;

	public int m_LayoutLayer = 0;

	public enum E_FocusChangeDir
	{
		E_FCD_RIGHT,
		E_FCD_LEFT,
		E_FCD_UP,
		E_FCD_DOWN
	}

	// Delegate for Focus
	// returns focus ID of next widget (if focus is changed from currntFocusID in desired direction) 
	public delegate int FocusDelegate(int currentFocusID, E_FocusChangeDir dir);

	//
	// Private section
	//

	bool m_Initialized = false;

	GUIBase_Element[] m_Elements;
	GUIBase_Widget[] m_TouchSensitiveWidgets;

	GUIBase_Widget m_TouchedWidget; // currently touched widget
	GUIBase_Widget m_MouseOverWidget; // widget where mouse is hovering over

	// Resolution of screen: 
	//Slouzi pro prepocitani souradnic z fyzicke obrazovky na orig rozliseni layoutu.
	//Priklad: Layout je vytvoren pro rozliseni 1900x1200. Spousti se na rozliseni 800x600. V m_PlatformSize bude (1900,1200) a v m_LayoutScale bude (0.4, 0.5).
	Vector2 m_LayoutScale = Vector2.one;

	public Vector2 LayoutScale
	{
		get { return m_LayoutScale; }
	}

	// Platform size
	Vector2 m_PlatformSize = Vector2.one;

	public Vector2 PlatformSize
	{
		get { return m_PlatformSize; }
	}

	// Animation component
	Animation m_Anim;
	bool m_IsPlayingAnimation; // can't use m_Anim.isPlaying variable, because its not working when (Time.scale == 0.0f)

	// Focus delegate
	//FocusDelegate			m_FocusDelegate = null;

	int m_LayoutUniqueId = 0;
	static int ms_LayoutUniqueIdCnt = 1;

	public delegate void LayoutTouchDelegate();
	LayoutTouchDelegate m_LayoutTouchDelegate;

	GUIBase_Layout()
	{
		m_LayoutUniqueId = ms_LayoutUniqueIdCnt++;
	}

	public bool isInitialized
	{
		get { return m_Initialized; }
	}

	//---------------------------------------------------------
	public bool ShowDone
	{
		get { return (!m_IsPlayingAnimation && IsVisible()); }
	}

	public bool HideDone
	{
		get { return (!m_IsPlayingAnimation && !IsVisible()); }
	}

	//---------------------------------------------------------
	public void OnElementStart()
	{
		if (GuiManager)
		{
			GuiManager.RegisterLayout(this);

			m_Initialized = false;

			m_Anim = GetComponent<Animation>();

			m_IsPlayingAnimation = false;
		}
		else
		{
			Debug.LogError("GuiManager prefab is not present!");
		}
	}

	public void OnElementVisible()
	{
		// Inicializace neni v Start, protoze ve Startu se registruji platformy, ktere musi byt zaregistrovane driv nez layouty
		if (m_Initialized == false)
		{
			LateInit();
		}
	}

	void OnDestroy()
	{
		m_InAnimation = null;
		m_OutAnimation = null;
		m_InSound = null;
		m_OutSound = null;
	}

	//---------------------------------------------------------
	protected override void OnLayoutChanged()
	{
		m_Elements = null;
		m_Initialized = false;
		SetModify(true, true);
	}

	//---------------------------------------------------------
	public void RegisterFocusDelegate(FocusDelegate f)
	{
		//m_FocusDelegate = f;
	}

	//---------------------------------------------------------
	public void RegisterLayoutTouchDelegate(LayoutTouchDelegate d)
	{
		m_LayoutTouchDelegate = d;
	}

	//---------------------------------------------------------
	public void SetPlatformSize(int width, int height, float scaleX, float scaleY)
	{
		m_PlatformSize.x = width;
		m_PlatformSize.y = height;

		m_LayoutScale.x = scaleX;
		m_LayoutScale.y = scaleY;

		NotifyLayoutChanged(true, true);
	}

	//---------------------------------------------------------
	protected override void OnGUIUpdate(float parentAlpha)
	{
		if (m_Initialized == false)
			return;
		if (m_Elements == null)
			return;

		float alpha = FadeAlpha*parentAlpha;

		// Call update to all widgets
		foreach (GUIBase_Element element in m_Elements)
		{
			element.GUIUpdate(alpha);
		}
	}

	//---------------------------------------------------------
	void LateInit()
	{
		if (m_Initialized == true)
			return;

		//
		// Initialize widgets
		//			

		List<GUIBase_Element> elements = new List<GUIBase_Element>(GetComponentsInChildren<GUIBase_Element>());
		if (elements != null)
		{
			elements.Remove(this);

			if (elements.Count > 0)
			{
				m_Elements = new GUIBase_Element[elements.Count];
				elements.CopyTo(m_Elements, 0);
			}
		}

		if (m_Elements != null)
		{
//			m_Elements = new GUIBase_Widget[elements.Length];
//			elements.CopyTo(m_Elements, 0);

			int numTouchables = 0;
			GUIBase_Widget[] touchableWidgets = new GUIBase_Widget[m_Elements.Length];

			for (int i = 0; i < MAX_LAYERS; ++i)
			{
				foreach (GUIBase_Element element in m_Elements)
				{
					GUIBase_Widget w = element as GUIBase_Widget;
					if (w && w.m_GuiWidgetLayer == i)
					{
						// Init sprite if there is any
						w.Initialization(this, m_LayoutScale);

						// Register "touch sensitive" widget
						if (w.IsTouchSensitive())
						{
							touchableWidgets[numTouchables] = w;
							numTouchables++;
						}
					}
				}
			}

			//
			// Touchable widgets
			//

			if (numTouchables != 0)
			{
				m_TouchSensitiveWidgets = new GUIBase_Widget[numTouchables];
				System.Array.Copy(touchableWidgets, m_TouchSensitiveWidgets, numTouchables);
				System.Array.Sort(m_TouchSensitiveWidgets, (x, y) => { return x.m_GuiWidgetLayer.CompareTo(y.m_GuiWidgetLayer); });
			}
		}

		SetModify();

		m_Initialized = true;
	}

	//---------------------------------------------------------
	public bool ProcessInput(ref IInputEvent evt)
	{
		switch (evt.Kind)
		{
		case E_EventKind.Touch:
			if (m_LayoutTouchDelegate != null)
			{
				m_LayoutTouchDelegate();
			}
			return false;
		case E_EventKind.Key:
			return ProcessKey((KeyEvent)evt);
		default:
			return false;
		}
	}

	public Vector2 ScreenPosToLayoutSpace(Vector2 pos)
	{
		return new Vector2(pos.x/m_LayoutScale.x, m_PlatformSize.y - pos.y);
	}

	public Vector2 ScreenDeltaToLayoutSpace(Vector2 offset)
	{
		return new Vector2(offset.x/m_LayoutScale.x, offset.y);
	}

	// added by AX but I don't need it...
	public Vector2 LayoutSpacePosToScreen(Vector2 pos)
	{
		return new Vector2(pos.x, (m_PlatformSize.y - pos.y));
	}

	public Vector2 LayoutSpaceDeltaToScreen(Vector2 offset)
	{
		return new Vector2(offset.x, offset.y);
	}

	public GUIBase_Widget HitTest(ref Vector2 point)
	{
		if (m_TouchSensitiveWidgets == null)
			return null;

		foreach (var widget in m_TouchSensitiveWidgets)
		{
			if (widget != null && widget.Visible && widget.InputEnabled)
			{
				if (widget.IsMouseOver(point) == true)
					return widget;
			}
		}

		return null;
	}

	bool ProcessKey(KeyEvent evt)
	{
		bool result = false;

#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
		if (m_TouchSensitiveWidgets == null)
			return false;

		//keyboard and gamepad buttons
		foreach (GUIBase_Widget widget in m_TouchSensitiveWidgets)
		{
			if (widget.IsVisible())
			{
				//check input button
				GUIBase_Button btn = widget.GetComponent<GUIBase_Button>();
				if (btn && !string.IsNullOrEmpty(btn.inputButton))
				{
					//podle typu eventu posli zavolej delegata
					if (Input.GetButtonDown(btn.inputButton))
					{
						result = widget.HandleTouchEvent(GUIBase_Widget.E_TouchPhase.E_TP_CLICK_BEGIN, btn.inputButton);
						if (result)
						{
							break;
						}
					}
					else if (Input.GetButtonUp(btn.inputButton))
					{
						result = widget.HandleTouchEvent(GUIBase_Widget.E_TouchPhase.E_TP_CLICK_RELEASE_KEYBOARD, btn.inputButton);
						if (result)
						{
							break;
						}
					}
				}
			}
		}
#endif

		return result;
	}

	//---------------------------------------------------------
	public void Show(bool state)
	{
		Show(state, true);
	}

	//---------------------------------------------------------
	public void ShowWidget(string widgetID, bool state)
	{
		GUIBase_Widget widget = GetWidget(widgetID, false);
		if (widget != null)
		{
			widget.Show(state, false);
		}
	}

	//---------------------------------------------------------
	public GUIBase_Widget GetWidget(string wName, bool reportError = true)
	{
		if (m_Initialized == false)
		{
			LateInit();
		}

		if (m_Elements != null)
		{
			foreach (GUIBase_Element element in m_Elements)
			{
				if (element && element.name == wName && element is GUIBase_Widget)
				{
					return (GUIBase_Widget)element;
				}
			}
		}

		if (reportError)
			Debug.LogError("Cant find '" + wName + "' in layout '" + this.GetFullName('.') + "' ");
							//(znamena ze hledany widget neni v actualne nactene gui scene, nebo ze se jmenuje jinak.)

		return null;
	}

	//---------------------------------------------------------
	public void RegisterButtonTouchDelegate(string widgetID, GUIBase_Button.TouchDelegate f)
	{
		if (m_Initialized == false)
		{
			LateInit();
		}

		if (m_Elements != null)
		{
			foreach (GUIBase_Element element in m_Elements)
			{
				if (element.name == widgetID)
				{
					GUIBase_Button button = element.GetComponent<GUIBase_Button>();

					if (button != null)
					{
						button.RegisterTouchDelegate(f);
					}

					return;
				}
			}
		}
	}

	//---------------------------------------------------------
	// Show/Hide all widgets
	public void ShowImmediate(bool showFlag, bool playAnimAndSound = true)
	{
		// Animation, sound
		if (showFlag)
		{
			Visible = true;

			if (m_InAnimation && playAnimAndSound && m_Anim != null)
			{
				StopAnim(m_Anim);

				m_Anim.clip = m_InAnimation;
				PlayAnim(m_Anim, null, LayoutAnimFinished, (int)GUIBase_Platform.E_SpecialAnimIdx.E_SAI_INANIM);

				m_IsPlayingAnimation = true;
			}

			if (m_InSound && playAnimAndSound)
			{
				MFGuiManager.Instance.PlayOneShot(m_InSound);
			}

			//
			// Show widgets
			//

			if (m_Elements != null)
			{
				foreach (GUIBase_Element element in m_Elements)
				{
					GUIBase_Widget widget = element as GUIBase_Widget;
					if (widget != null)
					{
						widget.ShowImmediate(widget.m_VisibleOnLayoutShow || widget.IsVisible(), false);
					}
				}
			}
		}
		else
		{
			if (m_OutAnimation && playAnimAndSound && m_Anim != null)
			{
				StopAnim(m_Anim);

				m_Anim.clip = m_OutAnimation;
				PlayAnim(m_Anim, null, LayoutAnimFinished, (int)GUIBase_Platform.E_SpecialAnimIdx.E_SAI_OUTANIM);

				m_IsPlayingAnimation = true;
			}

			if (m_OutSound && playAnimAndSound)
			{
				MFGuiManager.Instance.PlayOneShot(m_OutSound);
			}

			if (!m_OutAnimation || !playAnimAndSound)
			{
				// make layout invisible 
				Visible = false;

				// hide widgets instantly

				if (m_Elements != null)
				{
					foreach (GUIBase_Element element in m_Elements)
					{
						GUIBase_Widget widget = element as GUIBase_Widget;
						if (widget != null)
						{
							widget.ShowImmediate(false, false);
						}
					}
				}
			}
		}
	}

	//---------------------------------------------------------
	public int GetUniqueId()
	{
		return m_LayoutUniqueId;
	}

	//---------------------------------------------------------
	public void PlayAnim(Animation animation,
						 GUIBase_Widget widget,
						 GUIBase_Platform.AnimFinishedDelegate finishDelegate = null,
						 int customIdx = -1)
	{
		GuiManager.GetPlatform(this).PlayAnim(animation, widget, finishDelegate, customIdx);
	}

	//---------------------------------------------------------
	public void StopAnim(Animation animation)
	{
		GuiManager.GetPlatform(this).StopAnim(animation);
	}

	public void PlayAnimClip(AnimationClip clip)
	{
		if (m_Anim == null)
			return;

		m_Anim.clip = clip;
		PlayAnim(m_Anim, null);
	}

	public void StopCurrentAnimClip()
	{
		if (m_Anim == null)
			return;

		StopAnim(m_Anim);
	}

	//---------------------------------------------------------
	void LayoutAnimFinished(int idx)
	{
		switch ((GUIBase_Platform.E_SpecialAnimIdx)idx)
		{
		case GUIBase_Platform.E_SpecialAnimIdx.E_SAI_INANIM:
			m_IsPlayingAnimation = false;
			break;

		case GUIBase_Platform.E_SpecialAnimIdx.E_SAI_OUTANIM:
			m_IsPlayingAnimation = false;

			// make layout invisible 
			Visible = false;

			// Hide widgets
			if (m_Elements != null)
			{
				foreach (GUIBase_Element element in m_Elements)
				{
					element.Show(false, false);
				}
			}

			break;
		}
	}
}
