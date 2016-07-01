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
using System.Linq;

public class GuiPopupGamepadConfig : GuiPopup
{
	readonly static string TITLE_LABEL = "Title_Label";
	readonly static int PAUSE_ACTION = 4;
	readonly static int VIEW_RIGHT_ACTION = 12;
	readonly static int MOVE_RIGHT_ACTION = 13;

	GUIBase_Label m_TitleLabel;

	List<GUIBase_Button> m_ActionButtons;
	const string s_ActionButtonName = "Action_Button";
	int m_InputIndex = -1;

	const int MaxConfigButtons = 16;

	GUIBase_Label m_PauseActionLabel;
	GUIBase_Label m_ViewRightActionLabel;
	GUIBase_Label m_MoveRightActionLabel;

	float m_TimeOfLastChange = 0;
	bool m_CanCloseByEscape = false;

	//This popup is exploited for setting keyboard on PC
	bool m_IsForKeyboard = false;

	public bool IsForKeyboard
	{
		set
		{
			m_IsForKeyboard = value;
			if (m_IsForKeyboard)
			{
				//Some gamepad actions are used for different purposes on PC -> labels need to be changed
				m_PauseActionLabel.SetNewText(2020026);
				m_ViewRightActionLabel.SetNewText(2020023);
				m_MoveRightActionLabel.SetNewText(2020024);
				GamepadInputManager.Instance.SetConfig("Keyboard");
				m_TitleLabel.SetNewText(02020025);
			}
			else
			{
				m_PauseActionLabel.SetNewText(2020009);
				m_ViewRightActionLabel.SetNewText(2020018);
				m_MoveRightActionLabel.SetNewText(2020019);
				GamepadInputManager.Instance.SetConfig(Game.CurrentJoystickName());
				m_TitleLabel.SetNewText(2020000);
			}
			UpdateAllLabels();
		}
	}

	KeyCode[] m_KeyboardDisabledInput;

	struct GpadAction
	{
		public PlayerControlsGamepad.E_Input actionID;
		public int textID;
	};

	static GpadAction[] s_GActions = new GpadAction[]
	{
		new GpadAction() {actionID = PlayerControlsGamepad.E_Input.Fire, textID = 02020005},
		new GpadAction() {actionID = PlayerControlsGamepad.E_Input.Reload, textID = 02020006},
		new GpadAction() {actionID = PlayerControlsGamepad.E_Input.Sprint, textID = 02020007},
		new GpadAction() {actionID = PlayerControlsGamepad.E_Input.Roll, textID = 02020008},
		new GpadAction() {actionID = PlayerControlsGamepad.E_Input.Pause, textID = 02020009},
		new GpadAction() {actionID = PlayerControlsGamepad.E_Input.WeaponNext, textID = 02020021},
		new GpadAction() {actionID = PlayerControlsGamepad.E_Input.WeaponPrev, textID = 02020022},
		new GpadAction() {actionID = PlayerControlsGamepad.E_Input.Item1, textID = 02020013},
		new GpadAction() {actionID = PlayerControlsGamepad.E_Input.Item2, textID = 02020014},
		new GpadAction() {actionID = PlayerControlsGamepad.E_Input.Item3, textID = 02020015},
		new GpadAction() {actionID = PlayerControlsGamepad.E_Input.Axis_MoveRight, textID = 02020016},
		new GpadAction() {actionID = PlayerControlsGamepad.E_Input.Axis_MoveUp, textID = 02020017},
		new GpadAction() {actionID = PlayerControlsGamepad.E_Input.Axis_ViewRight, textID = 02020018},
		new GpadAction() {actionID = PlayerControlsGamepad.E_Input.Axis_ViewUp, textID = 02020019},
	};

	public override void SetCaption(string inCaption)
	{
	}

	public override void SetText(string inText)
	{
	}

	public override bool CanCloseByEscape
	{
		get { return m_CanCloseByEscape; }
	}

	protected override void OnViewInit()
	{
		base.OnViewInit();

		if (m_ScreenLayout == null)
		{
			Debug.LogError("GuiPopupGamepadConfig<" + name + "> :: There is not any layout specified!");
			return;
		}
		m_TitleLabel = GuiBaseUtils.GetControl<GUIBase_Label>(Layout, TITLE_LABEL);

		PrepareButton(m_ScreenLayout, "Reset_Button", null, OnResetButton);
		PrepareButton(m_ScreenLayout, "Close_Button", null, OnCloseButton);

		m_ActionButtons = new List<GUIBase_Button>();
		for (int i = 0; i < s_GActions.Length; i++)
		{
			GUIBase_Button bt = PrepareButton(m_ScreenLayout, s_ActionButtonName + (i + 1), null, OnActionButton);
			m_ActionButtons.Add(bt);

			//setup action name
			GUIBase_Label lb = GuiBaseUtils.GetChildLabel(bt.Widget, "Action_Label", false);
			lb.SetNewText(s_GActions[i].textID);

			if (i == PAUSE_ACTION)
				m_PauseActionLabel = lb;
			else if (i == VIEW_RIGHT_ACTION)
				m_ViewRightActionLabel = lb;
			else if (i == MOVE_RIGHT_ACTION)
				m_MoveRightActionLabel = lb;
		}

		//hide the rest of buttons
		HideUnusedButtons();

		//
		UpdateAllLabels();

		SetKeyboardDisabledInput();
	}

	protected override void OnViewShow()
	{
		base.OnViewInit();

		//hide the rest of buttons
		HideUnusedButtons();

		//update labels
		UpdateAllLabels();

		m_CanCloseByEscape = false;
	}

	protected override void OnViewHide()
	{
		base.OnViewHide();
		m_InputIndex = -1;
	}

	void HideUnusedButtons()
	{
		//hide the rest of buttons
		for (int k = s_GActions.Length; k < MaxConfigButtons; k++)
		{
			GUIBase_Button bt = GuiBaseUtils.GetButton(m_ScreenLayout, s_ActionButtonName + (k + 1));
			bt.Widget.Show(false, true);
		}
	}

	protected override void OnViewUpdate()
	{
		base.OnViewUpdate();

		if (Input.GetKeyDown(KeyCode.Escape))
		{
			if (m_InputIndex == -1)
				m_CanCloseByEscape = true;
			else
			{
				m_InputIndex = -1;
				UpdateAllLabels();
			}
			return;
		}

		DetectInputSetup();
	}

	void OnResetButton(GUIBase_Widget inWidget)
	{
		Debug.Log("Reset to defaults");

		m_InputIndex = -1;

		if (m_IsForKeyboard)
		{
			GamepadInputManager.Instance.DeleteConfig("Keyboard");
			GamepadInputManager.Instance.SetDefaultConfig("Keyboard");
		}
		else
		{
			GamepadInputManager.Instance.DeleteConfig(Game.CurrentJoystickName());
			GamepadInputManager.Instance.SetDefaultConfig(Game.CurrentJoystickName());
		}

		UpdateAllLabels();
	}

	void OnCloseButton(GUIBase_Widget inWidget)
	{
		// save our configuration
		if (m_IsForKeyboard)
		{
			GamepadInputManager.Instance.SaveConfig("Keyboard");
			if (Player.LocalInstance != null && Player.LocalInstance.Controls != null)
				Player.LocalInstance.Controls.SetKeyboardInputTable(GamepadInputManager.Instance.InputTable);
			GamepadInputManager.Instance.SetConfig(Game.CurrentJoystickName());
		}
		else
			GamepadInputManager.Instance.SaveConfig(Game.CurrentJoystickName());

		Owner.Back();
	}

	void OnActionButton(GUIBase_Widget inWidget)
	{
		if (Time.timeSinceLevelLoad - m_TimeOfLastChange < 0.5)
			return;

		Debug.Log(inWidget.name + " pressed, index: " + GetButtonIndex(inWidget));
		WaitForInput(inWidget);
	}

	int GetButtonIndex(GUIBase_Widget inWidget)
	{
		if (inWidget.name.StartsWith(s_ActionButtonName))
		{
			int index = 0;
			string strIndex = inWidget.name.Substring(s_ActionButtonName.Length);
			if (System.Int32.TryParse(strIndex, out index))
			{
				return (index - 1);
			}
		}
		return -1;
	}

	void UpdateButtonLabel(GUIBase_Widget inWidget)
	{
		//get label 
		GUIBase_Label l = GuiBaseUtils.GetChildLabel(inWidget, "GUIBase_Label", false);

		//get index of button
		int btnIndex = GetButtonIndex(inWidget);

		//check if it is awaiting input
		if (m_InputIndex == btnIndex)
		{
			const string strAwaitingInput = "...";
			l.Clear();
			l.SetNewText(strAwaitingInput);
		}
		else
		{
			string s = GetButtonLabel(btnIndex);
			l.SetNewText(s);
		}
	}

	void WaitForInput(GUIBase_Widget inWidget)
	{
		//already awaing input
		if (m_InputIndex != -1)
			return;

		m_InputIndex = GetButtonIndex(inWidget);
		UpdateButtonLabel(inWidget);
	}

	string GetButtonLabel(int btnIndex)
	{
		PlayerControlsGamepad.E_Input actionID = s_GActions[btnIndex].actionID;
		JoyInput command = GamepadInputManager.Instance.GetActionButton(actionID);
		return GetButtonLabel(command);
	}

	public static string GetButtonLabel(JoyInput command)
	{
		if (command.joyAxis != E_JoystickAxis.NONE)
		{
			return GamepadAxis.GetAxisLabel(command.joyAxis);
		}
		else
		{
#if MADFINGER_KEYBOARD_MOUSE
			switch (command.key)
			{
				case KeyCode.Mouse0:
					return "Mouse Left";
				case KeyCode.Mouse1:
					return "Mouse Right";
				case KeyCode.Mouse5:
					return "Mouse Wheel Down";
				case KeyCode.Mouse6:
					return "Mouse Wheel Up";
			}
#endif
			string keyName = command.key.ToString();
			if (keyName.StartsWith("Joystick0") || keyName.StartsWith("Joystick1") || keyName.StartsWith("Joystick2") ||
				keyName.StartsWith("Joystick3") || keyName.StartsWith("Joystick4"))
				keyName = keyName.Substring(9);
			else if (keyName.StartsWith("Joystick"))
				keyName = keyName.Substring(8);

			return keyName;
		}
	}

	void UpdateAllLabels()
	{
		if (m_ActionButtons == null)
			return;

		for (int i = 0; i < s_GActions.Length; i++)
		{
			GUIBase_Button btn = m_ActionButtons[i];
			UpdateButtonLabel(btn.Widget);
		}
	}

	//--------------------------------------------------

	void DetectInputSetup()
	{
		if (m_InputIndex != -1)
		{
			JoyInput pi;

			if (m_IsForKeyboard)
				pi = GetKeyboardPressedInput();
			else
				pi = GetPressedInput();
			if (pi != null)
			{
				m_TimeOfLastChange = Time.timeSinceLevelLoad;
				PlayerControlsGamepad.E_Input actionID = s_GActions[m_InputIndex].actionID;
				GamepadInputManager.Instance.SetActionButton(actionID, pi);

				//currently we do not allow duplicite keys
				if (pi.joyAxis == E_JoystickAxis.NONE)
					ChecDupliciteKey(pi.key, m_InputIndex);
				else
					ChecDupliciteAxis(pi.joyAxis, m_InputIndex);

				GUIBase_Button updateBtn = m_ActionButtons[m_InputIndex];
				m_InputIndex = -1;
				UpdateButtonLabel(updateBtn.Widget);
			}
		}
	}

	JoyInput GetPressedInput()
	{
		//JOYSTICK BUTTONS
		for (int joyK = (int)KeyCode.Joystick1Button0; joyK <= (int)KeyCode.Joystick4Button19; joyK++)
		{
			// check for all joystick buttons
			KeyCode kc = (KeyCode)joyK;

			if (Input.GetKey(kc))
				Debug.Log(kc + ": " + Input.GetKey(kc));

			if (Input.GetKey(kc) && !Input.GetKeyDown(KeyCode.Escape))
			{
				return new JoyInput(kc, E_JoystickAxis.NONE);
			}
		}

		//JOYSTICK AXS
		//----------------------------------------------------------------
		// we set the axis in the unity inputmanager and then use them here
		//----------------------------------------------------------------

		//hack pro F510 ktery vraci na tabletech pro osy 4 a 0 stale 1 (a pro 5 a 11 -1 kdyz nejsou stisknute)
		bool f510_Hack = (Game.CachedJoystickName == "Generic X-Box pad") &&
						 (Input.GetAxis(GamepadAxis.GetAxis((E_JoystickAxis)5)) < 0 || Input.GetAxis(GamepadAxis.GetAxis((E_JoystickAxis)11)) < 0);

		int result = -1;
		for (int i = 0; i < (int)E_JoystickAxis.COUNT; i++)
		{
			if (f510_Hack)
			{
				//if(i == 4 || i == 5 || i == 10 || i == 11)	
				if (i == 4 || i == 10)
					continue;
			}

			float trashHold = (i < 4) ? 0.5f : 0.8f;
			string axis = GamepadAxis.GetAxis((E_JoystickAxis)i);
			if (Input.GetAxis(axis) > trashHold && !Input.GetKeyDown(KeyCode.Escape))
			{
				result = i;
			}
		}
		if (result != -1)
			return new JoyInput(KeyCode.None, (E_JoystickAxis)result);

		return null;
	}

	void ChecDupliciteKey(KeyCode testkey, int btnIndex)
	{
		for (int m = 0; m < s_GActions.Length; m++)
		{
			PlayerControlsGamepad.E_Input actionID = s_GActions[m].actionID;
			// check if we allready have testkey in our list and make sure we dont compare with itself
			JoyInput button = GamepadInputManager.Instance.GetActionButton(actionID);
			if (testkey == button.key && m != btnIndex)
			{
				// reset the double key
				GamepadInputManager.Instance.SetActionButton(actionID, new JoyInput(KeyCode.None, E_JoystickAxis.NONE));

				//update label
				GUIBase_Button updateBtn = m_ActionButtons[m];
				UpdateButtonLabel(updateBtn.Widget);
			}
		}
	}

	void ChecDupliciteAxis(E_JoystickAxis testAxis, int btnIndex)
	{
		for (int m = 0; m < s_GActions.Length; m++)
		{
			PlayerControlsGamepad.E_Input actionID = s_GActions[m].actionID;
			// check if we allready have testkey in our list and make sure we dont compare with itself
			JoyInput button = GamepadInputManager.Instance.GetActionButton(actionID);
			if (testAxis == button.joyAxis && m != btnIndex)
			{
				// reset the double key
				GamepadInputManager.Instance.SetActionButton(actionID, new JoyInput(KeyCode.None, E_JoystickAxis.NONE));

				//update label
				GUIBase_Button updateBtn = m_ActionButtons[m];
				UpdateButtonLabel(updateBtn.Widget);
			}
		}
	}

	void SetKeyboardDisabledInput()
	{
#if MADFINGER_KEYBOARD_MOUSE
		m_KeyboardDisabledInput = new KeyCode[] { KeyCode.Escape, KeyCode.Tab, KeyCode.Return };
#else
		m_KeyboardDisabledInput = new KeyCode[] {};
#endif
	}

	JoyInput GetKeyboardPressedInput()
	{
		// key is pressed
		if (Input.anyKeyDown)
		{
			int minValue = (int)System.Enum.GetValues(typeof (KeyCode)).Cast<KeyCode>().Min();
			int maxValue = (int)System.Enum.GetValues(typeof (KeyCode)).Cast<KeyCode>().Max();

			// go through all integer values between min KeyCode and max KeyCode not only enum values
			// OSX with russian language return key codes which are not contained in the KeyCode enum
			for (int i = minValue; i <= maxValue; i++)
			{
				KeyCode key = (KeyCode)i;

				// pressed key found
				if (Input.GetKeyDown(key))
				{
					if (m_KeyboardDisabledInput.Contains(key) == false)
					{
						return new JoyInput(key, E_JoystickAxis.NONE);
					}
				}
			}
		}

		//Scroll wheel actions are saved as Mouse5 and Mouse6
		float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
		if (scrollWheel < 0)
			return new JoyInput(KeyCode.Mouse5, E_JoystickAxis.NONE);
		else if (scrollWheel > 0)
			return new JoyInput(KeyCode.Mouse6, E_JoystickAxis.NONE);
		return null;
	}

	//---------------------------------------------------------
}
