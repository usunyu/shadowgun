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

public class GuiMogaPopup : MonoBehaviour
{
	public static GuiMogaPopup Instance;

	GUIBase_Pivot m_Pivot;
	GUIBase_Layout m_Layout;
	GUIBase_Label m_TextLabel;

	//help
	GUIBase_Pivot m_HelpPivot;
	GUIBase_Layout m_HelpLayout;
	GUIBase_Button m_HelpCloseButton;
	GUIBase_Switch m_HelpSwitch;
	List<GUIBase_Widget> m_TouchWidgets;
	GUIBase_Widget m_MogaPocket;
	GUIBase_Widget m_MogaPro;

	bool m_initialised = false;
	bool m_IsHelpOn = false;
	bool m_IsShown = false;

	void Awake()
	{
		Instance = this;
	}

	void OnDestroy()
	{
		Hide();
		//StopAllCoroutines();
		CancelInvoke();
		Instance = null;
	}

	public void Init()
	{
		m_initialised = true;
		m_Pivot = MFGuiManager.Instance.GetPivot("MogaGui_Pivot");
		m_Layout = m_Pivot.GetLayout("Connection_Layout");
		m_TextLabel = GuiBaseUtils.PrepareLabel(m_Layout, "Text_Label");

		//help
		m_HelpPivot = MFGuiManager.Instance.GetPivot("MogaHelp_Pivot");
		m_HelpLayout = m_HelpPivot.GetLayout("MogaHelp_Layout");
		m_HelpCloseButton = GuiBaseUtils.GetControl<GUIBase_Button>(m_HelpLayout, "Close_Button");
		m_HelpSwitch = GuiBaseUtils.GetControl<GUIBase_Switch>(m_HelpLayout, "ShowHelp_Switch");

		m_MogaPocket = GuiBaseUtils.GetChild<GUIBase_Widget>(m_HelpLayout, "Moga", true);
		m_MogaPro = GuiBaseUtils.GetChild<GUIBase_Widget>(m_HelpLayout, "MogaPro", true);

		m_TouchWidgets = new List<GUIBase_Widget>();
		m_TouchWidgets.Add(m_HelpCloseButton.Widget);
		foreach (var btn in m_HelpSwitch.m_Buttons)
			m_TouchWidgets.Add(btn.Widget);
	}

	public void Show(int msgId, float hideTime)
	{
		if (m_initialised && MFGuiManager.Instance != null)
		{
			string message = TextDatabase.instance[msgId];

			MFGuiManager.Instance.ShowLayout(m_Layout, true);
			m_TextLabel.SetNewText(message);
			m_IsShown = true;
			Invoke("Hide", hideTime);
		}
	}

	public bool IsShown()
	{
		return m_IsShown;
	}

	public bool IsHelpShown()
	{
		return m_IsHelpOn;
	}

	public void Hide()
	{
		if (m_initialised && MFGuiManager.Instance != null)
		{
			MFGuiManager.Instance.ShowLayout(m_Layout, false);
		}
		m_IsShown = false;
	}

	public void ShowHelp(bool showSwitch)
	{
		if (m_initialised && MFGuiManager.Instance != null)
		{
			m_HelpSwitch.SetValue(GuiOptions.showMogaHelp);
			GuiBaseUtils.RegisterButtonDelegate(m_HelpCloseButton, null, OnCloseButton);
			GuiBaseUtils.RegisterSwitchDelegate(m_HelpLayout, "ShowHelp_Switch", OnHelpSwitch);

			MFGuiManager.Instance.ShowLayout(m_HelpLayout, true);

			if (MogaGamepad.IsMogaPro())
			{
				m_MogaPro.Show(true, true);
			}
			else
			{
				m_MogaPocket.Show(true, true);
			}

			InputManager.FlushInput();
			InputManager.IsEnabled = false;
			m_IsHelpOn = true;

			//hide switch pokud jej nechceme zobrazovat
			m_HelpSwitch.Widget.Show(showSwitch, true);
		}
	}

	public void HideHelp()
	{
		if (m_initialised && MFGuiManager.Instance != null)
		{
			GuiBaseUtils.RegisterButtonDelegate(m_HelpCloseButton, null, null);
			GuiBaseUtils.RegisterSwitchDelegate(m_HelpLayout, "ShowHelp_Switch", null);
			MFGuiManager.Instance.ShowLayout(m_HelpLayout, false);
			InputManager.IsEnabled = true;
			m_IsHelpOn = false;
		}
	}

	public void LateUpdate()
	{
		if (!m_initialised)
			Init();
	}

	void OnCloseButton(bool inside)
	{
		if (!inside)
			return;

		HideHelp();
	}

	void OnHelpSwitch(bool val)
	{
		GuiOptions.showMogaHelp = val;
	}

	void Update()
	{
		if (m_IsHelpOn)
		{
			if (MogaGamepad.MenuKeyPressed())
			{
				HideHelp();
			}
			else
			{
				UpdateTouchableWidgets();
			}
		}
	}

	void UpdateTouchableWidgets()
	{
		//send touches to our widgets
		foreach (var touch in Input.touches)
		{
			Vector2 pos = touch.position;
			pos.y = Screen.height - touch.position.y;

			if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Ended)
			{
				foreach (var widget in m_TouchWidgets)
				{
					if (widget.Visible && widget.InputEnabled && widget.IsMouseOver(pos))
					{
						Debug.Log("Touch " + widget.name + ", visible: " + widget.Visible + ", enabled: " + widget.InputEnabled);
						if (touch.phase == TouchPhase.Began)
							widget.HandleTouchEvent(GUIBase_Widget.E_TouchPhase.E_TP_CLICK_BEGIN, null);
						else if (touch.phase == TouchPhase.Ended)
							widget.HandleTouchEvent(GUIBase_Widget.E_TouchPhase.E_TP_CLICK_RELEASE, null);
					}
				}
			}
		}
	}
};
