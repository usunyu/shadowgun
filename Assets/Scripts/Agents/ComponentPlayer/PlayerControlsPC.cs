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

public class PlayerControlsPC
{
#if UNITY_EDITOR || MADFINGER_KEYBOARD_MOUSE
	bool Enabled = true;
	bool lockCursorAfterStart = true; //flag for delayed cursor lock

	//Table of actions and its key codes which is set by user on PC
	//(GamepadInputManager and GuiPopupGamepadConfig)
	JoyInput[] m_InputTable = null;
	KeyEvent m_KeyEventToRelease = new KeyEvent() {State = E_KeyState.Released};
#endif

#pragma warning disable 414
	bool m_IsFiring = false;
#pragma warning restore 414

	PlayerControlStates States;
	MouseCameraControl MouseCameraCtrl = new MouseCameraControl();

	const int WeaponsSlotCOUNT = 3;

	public class MouseCameraControl
	{
		float sensitivityX = 2.4F;
		float sensitivityY = 1.4F;

		public bool CursorLocked { get; private set; }
		public bool EnableCursorLock { get; set; }
		public bool Changed;

		public float OutYaw;
		public float OutPitch;

		public MouseCameraControl()
		{
			EnableCursorLock = true;
		}

		public void SwitchCursor()
		{
			LockCursor(!CursorLocked);
		}

		public void LockCursor(bool state)
		{
#if UNITY_EDITOR || MADFINGER_KEYBOARD_MOUSE
			CursorLocked = state;
			if (!EnableCursorLock && CursorLocked)
				return;
			//InputManager.FlushInput();			//pokud je odkomentovane, generuje nezadouci escape released event. pokud zde bude flush z nejakeho duvodu potreba, je treba najit jiny zpusob ja fixnout zobrazeni pause menu. 
			SysUtils.Screen_lockCursor = CursorLocked;
#endif
		}

		float GetSensitivityX()
		{
			return (sensitivityX*GuiOptions.sensitivity);
		}

		float GetSensitivityY()
		{
			return (sensitivityY*GuiOptions.sensitivity);
		}

		public void Update()
		{
			Changed = false;
			OutYaw = 0;
			OutPitch = 0;

			if (!EnableCursorLock && SysUtils.Screen_lockCursor) //screen is locked, but locking is not allowed
			{
				InputManager.FlushInput();
				SysUtils.Screen_lockCursor = false;
			}
			else if (CursorLocked != SysUtils.Screen_lockCursor)
				LockCursor(CursorLocked);

			if (!CursorLocked || !EnableCursorLock)
			{
				return;
			}

			float additionX = (Input.GetAxis("MouseX")*GetSensitivityX());
			float additionY = (Input.GetAxis("MouseY")*GetSensitivityY());

			Changed = (Mathf.Abs(additionX) > 0.001F || Mathf.Abs(additionY) > 0.001F);

			//chosing the quick voice command on PC
			if (GuiHUD.Instance.CommandMenu != null && GuiHUD.Instance.CommandMenu.IsMenuVisible)
			{
				GuiHUD.Instance.CommandMenu.SetMouseMove(additionX);
				return;
			}

			if (!Changed)
				return;

			float yaw1 = additionX;
			float pitch1 = -additionY;

			//do not turn more then 160 deg in one tick
			OutYaw = ClampAngle(yaw1, -160, 160);
			OutPitch = ClampAngle(pitch1, -160, 160);
		}

		float ClampAngle(float angle, float min, float max)
		{
			angle = angle%360;
			if ((angle >= -360F) && (angle <= 360F))
			{
				if (angle < -360F)
				{
					angle += 360F;
				}
				if (angle > 360F)
				{
					angle -= 360F;
				}
			}
			return Mathf.Clamp(angle, min, max);
		}
	}

	public PlayerControlsPC(PlayerControlStates inStates)
	{
		States = inStates;

		//Load keyboard configuration
		if (GamepadInputManager.Instance == null)
			return;
		GamepadInputManager.Instance.SetConfig("Keyboard");
#if UNITY_EDITOR || MADFINGER_KEYBOARD_MOUSE
		SetInputTable(GamepadInputManager.Instance.InputTable);
#endif
		GamepadInputManager.Instance.SetConfig(Game.CurrentJoystickName());
	}

	public bool LockCursor
	{
		get { return MouseCameraCtrl.CursorLocked; }
		set { MouseCameraCtrl.LockCursor(value); }
	}

	public bool EnableLockCursor
	{
		get { return MouseCameraCtrl.EnableCursorLock; }
		set { MouseCameraCtrl.EnableCursorLock = value; }
	}

	public void Update()
	{
#if UNITY_EDITOR || MADFINGER_KEYBOARD_MOUSE
		//ovladani input controllerem
		if (!Enabled)
			return;

		if (m_KeyEventToRelease.State != E_KeyState.Released)
		{
			m_KeyEventToRelease.State = E_KeyState.Released;
			IInputEvent inputEvent = m_KeyEventToRelease;
			Process(ref inputEvent);
		}

		//we nedd to delay lock cursor, since in editor on pc it do not work properly if invoked too early on start.
		if (Time.timeSinceLevelLoad > 0 && lockCursorAfterStart)
		{
			MouseCameraCtrl.LockCursor(true);
			lockCursorAfterStart = false;
		}

		/*if(Input.GetMouseButtonDown(1) || Input.GetKeyDown("g"))
		{
            States.UseGadgetDelegate(E_ItemID.Grenade);
		}

		if(Input.GetMouseButtonDown(2) || Input.GetKeyDown("f"))
		{
            States.UseGadgetDelegate(E_ItemID.FlashBang);
		}*/

		//otestuj zda jsme clicknuli na iconu ve scene
		UpdateMouseInteractionTouch();

		//move joystick 
		if (States.Move.Enabled)
		{
			Vector2 dir;

			// If controls are set up by user (using GamepadInputManager)
			// Axis_ViewRight is used for move left on PC
			// Axis_ViewUp is used for move down on PC
			if (m_InputTable != null)
			{
				float horizontal = 0;
				if (Input.GetKey(m_InputTable[(int)PlayerControlsGamepad.E_Input.Axis_MoveRight].key))
					horizontal++;
				if (Input.GetKey(m_InputTable[(int)PlayerControlsGamepad.E_Input.Axis_ViewRight].key))
					horizontal--;

				float vertical = 0;
				if (Input.GetKey(m_InputTable[(int)PlayerControlsGamepad.E_Input.Axis_MoveUp].key))
					vertical++;
				if (Input.GetKey(m_InputTable[(int)PlayerControlsGamepad.E_Input.Axis_ViewUp].key))
					vertical--;

				dir = new Vector2(horizontal, vertical);
			}
			else
			{
				dir.x = Input.GetAxis("HorizontalMovePC");
				dir.y = Input.GetAxis("VerticalMovePC");
			}

			float dist = dir.magnitude;

			if (dist > 0.001f)
			{
				States.Move.Direction.x = dir.x;
				States.Move.Direction.z = dir.y;
				States.Move.Direction.Normalize();

				States._Temp.eulerAngles = new Vector3(0, Player.LocalInstance.Owner.Transform.rotation.eulerAngles.y, 0);
				States.Move.Direction = States._Temp.TransformDirection(States.Move.Direction);
				States.Move.Force = dist;
			}
		}
		//Debug.Log("Joystick x: "+Joystick.Direction.x + "y: " +Joystick.Direction.y + "z: " + Joystick.Direction.z+ " | force: " + Joystick.Force); //todo: zakomentovat

		//view joysticks
		if (States.View.Enabled)
		{
			MouseCameraCtrl.Update();
			//GpadCameraCtrl.Update();
			if (MouseCameraCtrl.Changed)
			{
				States.View.SetNewRotation(MouseCameraCtrl.OutYaw, MouseCameraCtrl.OutPitch);
			}
		}

		// if controls are set up by user (using GamepadInputManager)
		// Mouse wheel up/down is interpreted as Mouse5/Mouse6 to be used in ProcessKey
		if (m_InputTable != null)
		{
			float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
			if (scrollWheel != 0) //scroll wheel is simulated as Mouse5 and Mouse6
			{
				m_KeyEventToRelease = new KeyEvent()
				{
					Code = scrollWheel < 0 ? KeyCode.Mouse5 : KeyCode.Mouse6,
					State = E_KeyState.Pressed
				};
				IInputEvent inputEvent = m_KeyEventToRelease;
				Process(ref inputEvent);
			}
		}
#endif
	}

	public bool Process(ref IInputEvent evt)
	{
#if UNITY_EDITOR || MADFINGER_KEYBOARD_MOUSE
		switch (evt.Kind)
		{
		case E_EventKind.Touch:
		{
			// If controls are set up by user, everything happens in ProcessKey
			// Therefore mouse events can't be processed as Touch events
			if (m_InputTable != null)
			{
				IInputEvent keyEvent = (IInputEvent)TouchEventToKeyEvent((TouchEvent)evt);
				return ProcessKey(ref keyEvent);
			}
			return ProcessTouch(ref evt);
		}
		case E_EventKind.Key:
			return ProcessKey(ref evt);
		default:
			break;
		}
#endif
		return false;
	}

#if UNITY_EDITOR || MADFINGER_KEYBOARD_MOUSE
	bool ProcessTouch(ref IInputEvent evt)
	{
		if (SysUtils.Screen_lockCursor == false)
			return false;

		TouchEvent touch = (TouchEvent)evt;

		if (touch.Type != E_TouchType.MouseButton)
			return false;

		switch (touch.Id)
		{
		case 0:
			if (touch.Started == true)
			{
				Fire(true);
			}
			else if (touch.Finished == true)
			{
				Fire(false);
			}
			return true;
		case 1:
			States.ReloadDelegate();
			return true;
		default:
			break;
		}

		return false;
	}

	bool ProcessKey(ref IInputEvent evt)
	{
		if (States.ActionsEnabled == false)
			return false;

		if (Player.LocalInstance == null)
			return false;

		KeyEvent key = (KeyEvent)evt;

		PlayerControlsGamepad.E_Input action = GetInputTableAction(key.Code);

		if (key.State == E_KeyState.Released)
		{
			// Escape and Tab are always hardcoded and can't be set by user
			switch (key.Code)
			{
			case KeyCode.Escape:
				if (MFGuiFader.Fading == false && GuiFrontendIngame.PauseMenuCooldown() == false)
				{
					GuiFrontendIngame.ShowPauseMenu();
				}
				return true;
			case KeyCode.Tab:
				GuiFrontendIngame.HideScoreMenu();
				return true;
			}

			if (m_InputTable != null)
			{
				switch (action)
				{
				case PlayerControlsGamepad.E_Input.Fire:
					Fire(false);
					return true;
				case PlayerControlsGamepad.E_Input.Sprint:
					States.SprintUpDelegate();
					return true;
				case PlayerControlsGamepad.E_Input.Pause:
					if (GuiHUD.Instance.CommandMenu != null && GuiHUD.Instance.CommandMenu.IsShown)
						GuiHUD.Instance.CommandMenu.Hide();
					return true;
				}
				return false;
			}

			switch (key.Code)
			{
			case KeyCode.RightControl:
			case KeyCode.LeftControl:
				Fire(false);
				return true;
			case KeyCode.RightShift:
			case KeyCode.LeftShift:
				States.SprintUpDelegate();
				return true;
			default:
				break;
			}
		}
		else if (key.State == E_KeyState.Pressed)
		{
			switch (key.Code)
			{
			case KeyCode.Return:
				//check for mouse lock unlock
				MouseCameraCtrl.SwitchCursor();
				return true;
			case KeyCode.Tab:
				//E_MPGameType gameType = Client.Instance.GameState.GameType;
				GuiFrontendIngame.ShowScoreMenu( /*gameType, false*/);
				return true;
			}

			if (m_InputTable != null)
			{
				switch (action)
				{
				case PlayerControlsGamepad.E_Input.Fire:
					Fire(true);
					return true;
				case PlayerControlsGamepad.E_Input.Sprint:
					States.SprintDownDelegate();
					return true;
				case PlayerControlsGamepad.E_Input.Roll:
					States.RollDelegate();
					return true;
				case PlayerControlsGamepad.E_Input.Reload:
					States.ReloadDelegate();
					return true;
				case PlayerControlsGamepad.E_Input.Weapon1:
					ChangeWeapon(0);
					return true;
				case PlayerControlsGamepad.E_Input.Weapon2:
					ChangeWeapon(1);
					return true;
				case PlayerControlsGamepad.E_Input.Weapon3:
					ChangeWeapon(2);
					return true;
				case PlayerControlsGamepad.E_Input.Item1:
					UseGadget(0);
					return true;
				case PlayerControlsGamepad.E_Input.Item2:
					UseGadget(1);
					return true;
				case PlayerControlsGamepad.E_Input.Item3:
					UseGadget(2);
					return true;
				case PlayerControlsGamepad.E_Input.WeaponNext:
					ChangeWeaponNext();
					return true;
				case PlayerControlsGamepad.E_Input.WeaponPrev:
					ChangeWeaponPrev();
					return true;
				case PlayerControlsGamepad.E_Input.Pause:
					if (GuiHUD.Instance.CommandMenu != null && !m_IsFiring)
					{
						GuiHUD.Instance.CommandMenu.Show();
						GuiHUD.Instance.CommandMenu.OpenMenu();
					}
					return true;
				}
				return false;
			}

			switch (key.Code)
			{
			case KeyCode.RightControl:
			case KeyCode.LeftControl:
				Fire(true);
				return true;
			case KeyCode.RightShift:
			case KeyCode.LeftShift:
				States.SprintDownDelegate();
				return true;
			case KeyCode.Space:
				States.RollDelegate();
				return true;
			case KeyCode.R:
				States.ReloadDelegate();
				return true;
			case KeyCode.Alpha1:
			case KeyCode.Keypad1:
				ChangeWeapon(0);
				return true;
			case KeyCode.Alpha2:
			case KeyCode.Keypad2:
				ChangeWeapon(1);
				return true;
			case KeyCode.Alpha3:
			case KeyCode.Keypad3:
				ChangeWeapon(2);
				return true;
			case KeyCode.Alpha4:
			case KeyCode.Keypad4:
				ChangeWeapon(3);
				return true;
			case KeyCode.Alpha7:
			case KeyCode.Keypad7:
				UseGadget(3);
				return true;
			case KeyCode.Alpha8:
			case KeyCode.Keypad8:
			case KeyCode.H:
			case KeyCode.Q:
				UseGadget(2);
				return true;
			case KeyCode.Alpha9:
			case KeyCode.Keypad9:
			case KeyCode.G:
			case KeyCode.E:
				UseGadget(1);
				return true;
			case KeyCode.Alpha0:
			case KeyCode.Keypad0:
			case KeyCode.F:
				UseGadget(0);
				return true;
			case KeyCode.O:
				SelectNextGadget();
				return true;
			case KeyCode.I:
			case KeyCode.L:
				SelectPrevGadget();
				return true;
			case KeyCode.P:
				UseSelectedGadget();
				return true;
			default:
				break;
			}
		}

		return false;
	}

	public void SetInputTable(JoyInput[] inputTable)
	{
		m_InputTable = inputTable;
	}

	public PlayerControlsGamepad.E_Input GetInputTableAction(KeyCode keyCode)
	{
		if (m_InputTable == null)
			return PlayerControlsGamepad.E_Input.COUNT;

		for (PlayerControlsGamepad.E_Input action = PlayerControlsGamepad.E_Input.Fire;
			 action < PlayerControlsGamepad.E_Input.COUNT;
			 action++)
		{
			if (m_InputTable[(int)action].key == keyCode)
				return action;
		}
		return PlayerControlsGamepad.E_Input.COUNT;
	}

	KeyEvent TouchEventToKeyEvent(TouchEvent touchEvent)
	{
		KeyEvent keyEvent = new KeyEvent();
		keyEvent.StartTime = touchEvent.StartTime;
		keyEvent.State = E_KeyState.Repeating;
		if (touchEvent.Started)
			keyEvent.State = E_KeyState.Pressed;
		else if (touchEvent.Finished)
			keyEvent.State = E_KeyState.Released;

		switch (touchEvent.Id)
		{
		default:
		case 0:
			keyEvent.Code = KeyCode.Mouse0;
			break;
		case 1:
			keyEvent.Code = KeyCode.Mouse1;
			break;
		case 2:
			keyEvent.Code = KeyCode.Mouse2;
			break;
		case 3:
			keyEvent.Code = KeyCode.Mouse3;
			break;
		case 4:
			keyEvent.Code = KeyCode.Mouse4;
			break;
		}
		return keyEvent;
	}
#endif

	void UpdateMouseInteractionTouch()
	{
		if (Input.GetMouseButtonDown(0) && SysUtils.Screen_lockCursor == false)
		{
			//Debug.Log("MouseTouch test");
			Vector2 mousepos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
			InteractionObject o = PlayerControlsTouch.TouchedInteractionIcon(mousepos);

			if (o != null)
				States.UseObjectDelegate(o);
		}
	}

	void Fire(bool state)
	{
		if (GuiHUD.Instance.CommandMenu != null && GuiHUD.Instance.CommandMenu.IsMenuVisible)
		{
			GuiHUD.Instance.CommandMenu.ConfirmMouseSelection();
			return;
		}

		//podle toho ve kterem jsem modu (USE/combat), zavolam delagata na strelbu nebo na use.
		if (null != Player.LocalInstance && Player.LocalInstance.InUseMode)
		{
			if (state == true)
			{
				States.UseDelegate();
			}
		}
		else
		{
			if (state == true)
			{
				States.FireDownDelegate();
				m_IsFiring = true;
			}
			else
			{
				States.FireUpDelegate();
				m_IsFiring = false;
			}
		}
	}

	int CurrentWeaponSlot()
	{
		//find current weapon slot
		E_WeaponID curId = Player.LocalInstance.Owner.WeaponComponent.CurrentWeapon;
		int currentSlot = 0;
		for (int i = 0; i < WeaponsSlotCOUNT; i++)
		{
			if (GuiHUD.Instance.GetWeaponInInventoryIndex(i) == curId)
			{
				currentSlot = i;
				break;
			}
		}
		return currentSlot;
	}

	void ChangeWeaponNext()
	{
		int currentSlot = CurrentWeaponSlot();

		//move to next
		int nextSlot = currentSlot;
		do
		{
			nextSlot = (nextSlot + 1)%WeaponsSlotCOUNT;
			if (GuiHUD.Instance.GetWeaponInInventoryIndex(nextSlot) != E_WeaponID.None)
			{
				ChangeWeapon(nextSlot);
			}
		} while (nextSlot != currentSlot);
	}

	void ChangeWeaponPrev()
	{
		int currentSlot = CurrentWeaponSlot();
		int nextSlot = currentSlot;
		do
		{
			nextSlot = (nextSlot <= 0) ? WeaponsSlotCOUNT - 1 : nextSlot - 1;
			if (GuiHUD.Instance.GetWeaponInInventoryIndex(nextSlot) != E_WeaponID.None)
			{
				ChangeWeapon(nextSlot);
			}
		} while (nextSlot != currentSlot);
	}

	void ChangeWeapon(int idx)
	{
		E_WeaponID id = GuiHUD.Instance.GetWeaponInInventoryIndex(idx);

		if (id != E_WeaponID.None && id != Player.LocalInstance.Owner.WeaponComponent.CurrentWeapon)
		{
			States.ChangeWeaponDelegate(id);
		}
	}

	void UseGadget(int idx)
	{
		E_ItemID id = GuiHUD.Instance.GetGadgetInInventoryIndex(idx);

		if (id != E_ItemID.None && Player.LocalInstance.Owner.GadgetsComponent.GetGadget(id).Settings.ItemUse == E_ItemUse.Activate)
		{
			States.UseGadgetDelegate(id);
		}
	}

	void SelectPrevGadget()
	{
		GuiHUD.Instance.Gadgets.SelectPrev();
	}

	void SelectNextGadget()
	{
		GuiHUD.Instance.Gadgets.SelectNext();
	}

	void UseSelectedGadget()
	{
		int selGadget = GuiHUD.Instance.Gadgets.GetSelected();
		UseGadget(selGadget);
	}
}
