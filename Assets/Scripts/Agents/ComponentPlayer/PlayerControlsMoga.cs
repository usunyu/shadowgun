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

#if UNITY_ANDROID //&& !UNITY_EDITOR
public class PlayerControlsMoga
{
	enum E_InputAction
	{
		R1,
		R2,
		L1,
		L2,
		A,
		B,
		X,
		Y,
		Select,
		Pause,
		DpadLeft,
		DpadRight,
		DpadUp,
		DpadDown,
		COUNT //keep last
	}

	class KeyInput
	{
		public int key;
		public bool isDown;
		public delegate void Delegate();
		public Delegate onKeyDown;
		public Delegate onKeyUp;
	}

	Dictionary<E_InputAction, KeyInput> m_KeyInput = new Dictionary<E_InputAction, KeyInput>();

	public bool Enabled = true;
	PlayerControlStates States;

	float ViewSensitivityX
	{
		get { return (1.0f*GuiOptions.sensitivity); }
	}

	float ViewSensitivityY
	{
		get { return (0.75f*GuiOptions.sensitivity); }
	}

	const int slotCOUNT = 3;

	public PlayerControlsMoga(PlayerControlStates inStates)
	{
		States = inStates;
		//setup key codes for actions
		m_KeyInput.Add(E_InputAction.R1, new KeyInput() {key = Moga.KEYCODE_BUTTON_R1, onKeyDown = OnR1Down, onKeyUp = OnR1Up});
		m_KeyInput.Add(E_InputAction.R2, new KeyInput() {key = Moga.KEYCODE_BUTTON_R2, onKeyDown = OnR2Down, onKeyUp = OnR2Up});
		m_KeyInput.Add(E_InputAction.L1, new KeyInput() {key = Moga.KEYCODE_BUTTON_L1, onKeyDown = OnL1Down, onKeyUp = OnL1Up});
		m_KeyInput.Add(E_InputAction.L2, new KeyInput() {key = Moga.KEYCODE_BUTTON_L2, onKeyDown = OnL2Down, onKeyUp = OnL2Up});

		m_KeyInput.Add(E_InputAction.A, new KeyInput() {key = Moga.KEYCODE_BUTTON_A, onKeyDown = OnItemUseDown});
		m_KeyInput.Add(E_InputAction.B, new KeyInput() {key = Moga.KEYCODE_BUTTON_B, onKeyDown = OnItemSelectNextDown});
		m_KeyInput.Add(E_InputAction.X, new KeyInput() {key = Moga.KEYCODE_BUTTON_X, onKeyDown = OnRollDown});
		m_KeyInput.Add(E_InputAction.Y, new KeyInput() {key = Moga.KEYCODE_BUTTON_Y, onKeyDown = OnReloadDown});
		m_KeyInput.Add(E_InputAction.Select, new KeyInput() {key = Moga.KEYCODE_BUTTON_SELECT, onKeyDown = OnSelectDown});
		m_KeyInput.Add(E_InputAction.Pause, new KeyInput() {key = Moga.KEYCODE_BUTTON_START, onKeyDown = OnPauseDown});

		m_KeyInput.Add(E_InputAction.DpadLeft, new KeyInput() {key = Moga.KEYCODE_DPAD_LEFT, onKeyDown = OnDpadLeft});
		m_KeyInput.Add(E_InputAction.DpadRight, new KeyInput() {key = Moga.KEYCODE_DPAD_RIGHT, onKeyDown = OnDpadRight});
		m_KeyInput.Add(E_InputAction.DpadUp, new KeyInput() {key = Moga.KEYCODE_DPAD_UP, onKeyDown = OnDpadUp});
		m_KeyInput.Add(E_InputAction.DpadDown, new KeyInput() {key = Moga.KEYCODE_DPAD_DOWN, onKeyDown = OnDpadDown});
	}

	void UpdateMove()
	{
		if (!MogaGamepad.IsConnected())
			return;

		//move by joystick 
		Vector2 dir = Vector2.zero;

		dir.x = MogaGamepad.GetAxis(Moga.AXIS_X);
		dir.y = -MogaGamepad.GetAxis(Moga.AXIS_Y); //moga gamepad has inverted y axis

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

	void UpdateView()
	{
		if (!MogaGamepad.IsConnected())
			return;

		float yaw1 = MogaGamepad.GetAxis(Moga.AXIS_Z)*ViewSensitivityX;
		float pitch1 = MogaGamepad.GetAxis(Moga.AXIS_RZ)*ViewSensitivityY;

		bool Changed = (Mathf.Abs(yaw1) > 0.001F || Mathf.Abs(pitch1) > 0.001F);

		if (Changed)
		{
			//clamp to frame limit
			float RotSpeed = 360;
			float FrameLimit = (RotSpeed*Time.deltaTime);
			float OutYaw = PlayerControlStates.ClampAngle(yaw1, -FrameLimit, FrameLimit);
			float OutPitch = PlayerControlStates.ClampAngle(pitch1, -FrameLimit, FrameLimit);

			States.View.SetNewRotation(OutYaw, OutPitch);
		}
	}

	void UpdateKeys()
	{
		if (!MogaGamepad.IsConnected())
			return;

		foreach (E_InputAction a in m_KeyInput.Keys)
		{
			KeyInput ki = m_KeyInput[a];
			if (MogaGamepad.GetKeyCode(ki.key) == Moga.ACTION_DOWN && !ki.isDown)
			{
				ki.isDown = true;

				if (ki.onKeyDown != null)
					ki.onKeyDown();
			}
			else if (MogaGamepad.GetKeyCode(ki.key) == Moga.ACTION_UP && ki.isDown)
			{
				ki.isDown = false;

				if (ki.onKeyUp != null)
					ki.onKeyUp();
			}
		}
	}

	bool ButtonDown(E_InputAction inp)
	{
		return m_KeyInput[inp].isDown;
	}

	bool ButtonUp(E_InputAction inp)
	{
		return !m_KeyInput[inp].isDown;
	}

	void OnFireDown()
	{
		//fire only when there is no use
		if (Player.LocalInstance.InUseMode)
			States.UseDelegate();
		else
			States.FireDownDelegate();
	}

	void OnSprintDown()
	{
		States.SprintDownDelegate();
	}

	void OnSprintUp()
	{
		States.SprintUpDelegate();
	}

	void OnReloadDown()
	{
		States.ReloadDelegate();
	}

	void OnPauseDown()
	{
		if (MFGuiFader.Fading == false)
			GuiFrontendIngame.ShowPauseMenu();
	}

	void OnFireUp()
	{
		if (Player.LocalInstance.InUseMode == false)
			States.FireUpDelegate();
	}

	void OnItemSelectNextDown()
	{
		GuiHUD.Instance.Gadgets.SelectNext();
	}

	void OnItemUseDown()
	{
		int selItem = GuiHUD.Instance.Gadgets.GetSelected();
		UseItem(selItem);
	}

	void OnNextWeaponDown()
	{
		ChangeWeaponNext();
	}

	void OnRollDown()
	{
		States.RollDelegate();
	}

	void UpdateActions()
	{
		UpdateKeys();
	}

	public void Update()
	{
		//ovladani input controllerem
		if (!Enabled)
			return;

		if (States.Move.Enabled)
			UpdateMove();

		if (States.View.Enabled)
			UpdateView();

		if (States.ActionsEnabled)
			UpdateActions();
	}

	void ChangeWeapon(int slotId)
	{
		E_WeaponID id = GuiHUD.Instance.GetWeaponInInventoryIndex(slotId);

		//weapon is different then current one
		if (id != E_WeaponID.None && id != Player.LocalInstance.Owner.WeaponComponent.CurrentWeapon)
			States.ChangeWeaponDelegate(id);
	}

	int GetCurrentWeaponSlot()
	{
		E_WeaponID curId = Player.LocalInstance.Owner.WeaponComponent.CurrentWeapon;
		int currentSlot = 0;
		for (int i = 0; i < slotCOUNT; i++)
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
		//find current weapon slot
		int currentSlot = GetCurrentWeaponSlot();

		//move to next
		int nextSlot = currentSlot;
		do
		{
			nextSlot = (nextSlot + 1)%slotCOUNT;
			if (GuiHUD.Instance.GetWeaponInInventoryIndex(nextSlot) != E_WeaponID.None)
			{
				ChangeWeapon(nextSlot);
			}
		} while (nextSlot != currentSlot);
	}

	void ChangeWeaponPrev()
	{
		//find current weapon slot
		int currentSlot = GetCurrentWeaponSlot();
		int nextSlot = currentSlot;

		do
		{
			nextSlot = (nextSlot <= 0) ? (slotCOUNT - 1) : (nextSlot - 1);
			if (GuiHUD.Instance.GetWeaponInInventoryIndex(nextSlot) != E_WeaponID.None)
			{
				ChangeWeapon(nextSlot);
				break;
			}
		} while (nextSlot != currentSlot);
	}

	void UseItem(int slotId)
	{
		E_ItemID id = GuiHUD.Instance.GetGadgetInInventoryIndex(slotId);

		if (id != E_ItemID.None && Player.LocalInstance.Owner.GadgetsComponent.GetGadget(id).Settings.ItemUse == E_ItemUse.Activate)
			States.UseGadgetDelegate(id);
	}

	//------------------------------------key handlers	
	void OnR1Down()
	{
		if (MogaGamepad.IsMogaPro())
			OnReloadDown();
		else
			OnFireDown();
	}

	void OnR1Up()
	{
		if (MogaGamepad.IsMogaPro() == false)
			OnFireUp();
	}

	void OnL1Down()
	{
		if (MogaGamepad.IsMogaPro())
			OnRollDown();
		else
			OnSprintDown();
	}

	void OnL1Up()
	{
		if (MogaGamepad.IsMogaPro() == false)
			OnSprintUp();
	}

	void OnR2Down()
	{
		if (MogaGamepad.IsMogaPro())
			OnFireDown();
	}

	void OnR2Up()
	{
		if (MogaGamepad.IsMogaPro())
			OnFireUp();
	}

	void OnL2Down()
	{
		if (MogaGamepad.IsMogaPro())
			OnSprintDown();
	}

	void OnL2Up()
	{
		if (MogaGamepad.IsMogaPro())
			OnSprintUp();
	}

	void OnSelectDown()
	{
		if (MogaGamepad.IsMogaPro() == false)
			OnNextWeaponDown();
	}

	void OnDpadLeft()
	{
		if (MogaGamepad.IsMogaPro())
		{
			ChangeWeaponPrev();
		}
	}

	void OnDpadRight()
	{
		if (MogaGamepad.IsMogaPro())
		{
			ChangeWeaponNext();
		}
	}

	void OnDpadUp()
	{
		if (MogaGamepad.IsMogaPro())
		{
			GuiHUD.Instance.Gadgets.SelectPrev();
		}
	}

	void OnDpadDown()
	{
		if (MogaGamepad.IsMogaPro())
		{
			GuiHUD.Instance.Gadgets.SelectNext();
		}
	}
}
#endif
