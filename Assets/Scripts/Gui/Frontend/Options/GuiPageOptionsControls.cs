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

[AddComponentMenu("GUI/Frontend/OptionPages/GuiPageOptionsControls")]
public class GuiPageOptionsControls : GuiScreen
{
#if MADFINGER_KEYBOARD_MOUSE
	static readonly string KEYBOARD_BUTTON    = "KeyboardButton";
#else
	readonly static string CONTROLS_SCHEME = "ControlScheme_Enum";
	readonly static string FIRE_BUTTON_SIZE = "FireButtonSize_Slider";
	readonly static string SWITCH_LEFT_HANDED = "Lefthanded_Switch";
	readonly static string CUSTOMIZE_BUTTON = "CustomiseButton";
	readonly static string MOGA_HELP_BUTTON = "MogaHelpButton";
#endif
	readonly static string SLIDER_SENSITIVITY = "Sensitivity_Slider";
	readonly static string SWITCH_INVERT_Y = "InvertY_Switch";
	readonly static string GAMEPAD_BUTTON = "GamepadButton";

	// PRIVATE MEMBERS

	[SerializeField] GuiCustomizeControls m_CustomizeControls;
	//[SerializeField] custom_inputs        m_GamepadControls;

	GUIBase_Enum m_ControlSchemeEnum;
	GUIBase_Slider m_SliderSensitivity;
	GUIBase_Slider m_FireButtonSize;
	GUIBase_Switch m_SwitchYAxis;
	GUIBase_Switch m_SwitchLefthanded;
	GUIBase_Button m_CustomizeButton;
	GUIBase_Button m_GamepadButton;
	GUIBase_Button m_MogaHelpButton;
	GUIBase_Button m_KeyboardButton;

	string m_LastGpadConected;

	// GUIVIEW INTERFACE

	protected override void OnViewInit()
	{
#if MADFINGER_KEYBOARD_MOUSE
		m_ScreenLayout 	= GetLayout("MainOpt", "00Controls_Layout_PC");
		m_KeyboardButton 	= GuiBaseUtils.GetControl<GUIBase_Button>(Layout, KEYBOARD_BUTTON);
#else
		if (GamepadInputManager.Instance.IsNvidiaShield())
		{
			m_ScreenLayout = GetLayout("MainOpt", "00Controls_Layout_Shield");
		}
		else
		{
			m_ControlSchemeEnum = GuiBaseUtils.GetControl<GUIBase_Enum>(Layout, CONTROLS_SCHEME);
			m_FireButtonSize = GuiBaseUtils.GetControl<GUIBase_Slider>(Layout, FIRE_BUTTON_SIZE);
			m_SwitchLefthanded = GuiBaseUtils.GetControl<GUIBase_Switch>(Layout, SWITCH_LEFT_HANDED);
			m_CustomizeButton = GuiBaseUtils.GetControl<GUIBase_Button>(Layout, CUSTOMIZE_BUTTON);
			m_MogaHelpButton = GuiBaseUtils.GetControl<GUIBase_Button>(Layout, MOGA_HELP_BUTTON);
		}
#endif
		m_SliderSensitivity = GuiBaseUtils.GetControl<GUIBase_Slider>(Layout, SLIDER_SENSITIVITY);
		m_SwitchYAxis = GuiBaseUtils.GetControl<GUIBase_Switch>(Layout, SWITCH_INVERT_Y);
		m_GamepadButton = GuiBaseUtils.GetControl<GUIBase_Button>(Layout, GAMEPAD_BUTTON);
	}

	protected override void OnViewShow()
	{
		BindControls(true);

#if UNITY_IPHONE && !UNITY_EDITOR
				//skryj gpad button na ios
		m_GamepadButton.Widget.Show(false, true);  
		m_MogaHelpButton.Widget.Show(false, true); 
#endif

#if UNITY_ANDROID || UNITY_EDITOR || UNITY_STANDALONE
		// na androideck detekuj pripojeni gamepadu 
		InvokeRepeating("CheckGpadChange", 0, 2.0f);
#endif
#if UNITY_ANDROID
		MogaGamepad.OnConnectionChange += OnMogaChange;
		OnMogaChange(MogaGamepad.IsConnected());
#endif
	}

	protected override void OnViewHide()
	{
		BindControls(false);
#if UNITY_ANDROID || UNITY_EDITOR || UNITY_STANDALONE
		CancelInvoke("CheckGpadChange");
#endif
#if UNITY_ANDROID
		MogaGamepad.OnConnectionChange -= OnMogaChange;
#endif
	}

	protected override void OnViewReset()
	{
#if !MADFINGER_KEYBOARD_MOUSE
		if (!GamepadInputManager.Instance.IsNvidiaShield())
		{
			m_ControlSchemeEnum.Selection = (int)GuiOptions.m_ControlScheme;
			m_FireButtonSize.SetValue(GuiOptions.fireButtonScale - 0.5f); // offset scale by 0.5 so the final range is 0.5-1.5
			m_SwitchLefthanded.SetValue(GuiOptions.leftHandAiming);
		}
#endif
		m_SliderSensitivity.SetValue(GuiOptions.sensitivity);
		m_SwitchYAxis.SetValue(GuiOptions.invertYAxis);
	}

	// HANDLERS

	void OnControlSchemeChanged(int value)
	{
		GuiOptions.m_ControlScheme = (GuiOptions.E_ControlScheme)value;

		if (Player.LocalInstance != null)
		{
			Player.LocalInstance.Controls.ControlSchemeChanged();
		}
	}

	void OnSensitivityChanged(float value)
	{
		GuiOptions.sensitivity = value;
	}

	void OnFireButtonSize(float value)
	{
		GuiOptions.fireButtonScale = value + 0.5f; // offset scale by 0.5 so the final range is 0.5-1.5

		if (GuiHUD.Instance != null)
		{
			GuiHUD.Instance.UpdateAttackButtonSettings();
		}
	}

	void OnInvertYToggled(bool state)
	{
		GuiOptions.invertYAxis = state;
	}

	void OnLefthandedToggled(bool state)
	{
		GuiOptions.SetNewLeftHandAiming(state);
	}

	void OnCustomizePressed(bool inside)
	{
		if (inside == false)
			return;
		if (Owner == null)
			return;

		Owner.ShowScreen("CustomizeControls");
	}

	void OnGamepadPressed(bool inside)
	{
		if (inside == false)
			return;
		if (Owner == null)
			return;

		//Owner.ShowScreen("custom_inputs");
		GuiPopupGamepadConfig popup = (GuiPopupGamepadConfig)Owner.ShowPopup("GamepadConfig", "", "", null);
		popup.IsForKeyboard = false;
	}

	void OnKeyboardPressed(bool inside)
	{
		if (inside == false)
			return;
		if (Owner == null)
			return;

		GuiPopupGamepadConfig popup = (GuiPopupGamepadConfig)Owner.ShowPopup("GamepadConfig", "", "", null);
		popup.IsForKeyboard = true;
	}

	void OnMogaHelpPressed(bool inside)
	{
		if (inside == false)
			return;
		if (Owner == null)
			return;

#if UNITY_ANDROID
		GuiMogaPopup.Instance.ShowHelp(true);
#endif
	}

	// PRIVATE METHODS

	void BindControls(bool state)
	{
		// bind callbacks
#if MADFINGER_KEYBOARD_MOUSE
		m_KeyboardButton.RegisterReleaseDelegate(state ? OnKeyboardPressed : (GUIBase_Button.ReleaseDelegate)null);
#else
		if (!GamepadInputManager.Instance.IsNvidiaShield())
		{
			m_ControlSchemeEnum.RegisterDelegate(state ? OnControlSchemeChanged : (GUIBase_Enum.ChangeValueDelegate)null);
			m_FireButtonSize.RegisterChangeValueDelegate(state ? OnFireButtonSize : (GUIBase_Slider.ChangeValueDelegate)null);
			m_SwitchLefthanded.RegisterDelegate(state ? OnLefthandedToggled : (GUIBase_Switch.SwitchDelegate)null);
			m_CustomizeButton.RegisterReleaseDelegate(state ? OnCustomizePressed : (GUIBase_Button.ReleaseDelegate)null);
			m_MogaHelpButton.RegisterReleaseDelegate(state ? OnMogaHelpPressed : (GUIBase_Button.ReleaseDelegate)null);

			// enable/disable controls
			m_CustomizeButton.SetDisabled(m_CustomizeControls != null ? false : true);
			m_CustomizeButton.Widget.Show((m_CustomizeControls != null), true);
		}
#endif
		m_SliderSensitivity.RegisterChangeValueDelegate(state ? OnSensitivityChanged : (GUIBase_Slider.ChangeValueDelegate)null);
		m_SwitchYAxis.RegisterDelegate(state ? OnInvertYToggled : (GUIBase_Switch.SwitchDelegate)null);
		m_GamepadButton.RegisterReleaseDelegate(state ? OnGamepadPressed : (GUIBase_Button.ReleaseDelegate)null);
	}

	void CheckGpadChange()
	{
		//if(custom_inputs.Instance == null)
		//	return;

		//check what gamepad is connected
		string gname = Game.CurrentJoystickName();
//always enable gamepad button in editor fou our testing convenience		
#if !UNITY_EDITOR		
		if(gname == null)
		{
			m_GamepadButton.SetDisabled(true);
			return;
		}
		else
		{
			m_GamepadButton.SetDisabled(false);
		}
#endif

		Debug.Log("gamepad connected: " + gname + " last copnected: " + m_LastGpadConected + " has config: " + GamepadInputManager.Instance.HasConfig(gname));

		//display message only once for each connection
		if (gname == m_LastGpadConected)
			return;

		m_LastGpadConected = gname;

		//zobraz hlasku pokud player pripojil novy gamepad (pro ktery jeste neni ulozeno zadne nastaveni)
		if (!GamepadInputManager.Instance.HasConfig(gname))
		{
			GamepadInputManager.Instance.ClearConfig();
			//new gamepad connected
			string strGampadTitle = TextDatabase.instance[02010040];
			string strGampadMessage = TextDatabase.instance[02010041];
			Owner.ShowPopup("MessageBox", strGampadTitle, strGampadMessage, null);
		}
	}

#if UNITY_ANDROID
	void OnMogaChange(bool connected)
	{
		if (!GamepadInputManager.Instance.IsNvidiaShield())
		{
			m_MogaHelpButton.Widget.Show(connected, true);
		}
	}
#endif
}
