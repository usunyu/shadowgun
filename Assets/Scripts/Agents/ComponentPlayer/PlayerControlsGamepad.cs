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

public class PlayerControlsGamepad
{
	public bool Enabled = true;
	PlayerControlStates States;

	float ViewSensitivityX
	{
		get { return (2.0f*GuiOptions.sensitivity); }
	}

	float ViewSensitivityY
	{
		get { return (1.5f*GuiOptions.sensitivity); }
	}

	float LastTouchControlTime = 0;
	const int WeaponsSlotCOUNT = 3;

	public static float ClampAngle(float angle, float min, float max)
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

	public PlayerControlsGamepad(PlayerControlStates inStates)
	{
		States = inStates;
	}

	public enum E_Input
	{
		Fire,
		Reload,
		Sprint,
		Roll,
		Pause,
		Weapon1,
		Weapon2,
		Weapon3,
		Item1,
		Item2,
		Item3,
		Axis_MoveRight,
		Axis_MoveUp,
		Axis_ViewRight,
		Axis_ViewUp,
		WeaponNext,
		WeaponPrev,
		//Voicechat,
		COUNT //last
	}

	void UpdateMove()
	{
		//move by joystick 
		Vector2 dir = Vector2.zero;

		/*dir.x = Input.GetAxis("HorizontalMove");
        dir.y = Input.GetAxis("VerticalMove");*/

		dir.x = GetGpadAxis(E_Input.Axis_MoveRight);
		dir.y = GetGpadAxis(E_Input.Axis_MoveUp);

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
		//Debug.Log("Joystick x: "+Joystick.Direction.x + "y: " +Joystick.Direction.y + "z: " + Joystick.Direction.z+ " | force: " + Joystick.Force); //todo: zakomentovat
	}

	void UpdateView()
	{
		/*float yaw1 = Input.GetAxis("HorizontalView") * ViewSensitivityX;
		float pitch1 = Input.GetAxis("VerticalView") * ViewSensitivityY;*/

		float yaw1 = GetGpadAxis(E_Input.Axis_ViewRight)*ViewSensitivityX;
		float pitch1 = GetGpadAxis(E_Input.Axis_ViewUp)*ViewSensitivityY;

		bool Changed = (Mathf.Abs(yaw1) > 0.001F || Mathf.Abs(pitch1) > 0.001F);

		if (Changed)
		{
			//clamp to frame limit
			//TODO: potrebujeme limitovat pohyb pro gamepad?
			float RotSpeed = 360;
			float FrameLimit = (RotSpeed*Time.deltaTime);
			float OutYaw = ClampAngle(yaw1, -FrameLimit, FrameLimit);
			float OutPitch = ClampAngle(pitch1, -FrameLimit, FrameLimit);

			States.View.SetNewRotation(OutYaw, OutPitch);
		}
	}

	float GetGpadAxis(E_Input inp)
	{
		JoyInput inpButton = GamepadInputManager.Instance.GetActionButton(inp);
		string axisName = GamepadAxis.GetAxis(inpButton.joyAxis);

		if (axisName != "")
		{
			//invert up axis for looking
			if (inp == E_Input.Axis_ViewUp)
				return -Input.GetAxis(axisName);
			else
				return Input.GetAxis(axisName);
		}
		else
		{
			if (inp == E_Input.Axis_MoveRight)
				return Input.GetAxis("HorizontalMove");
			else if (inp == E_Input.Axis_MoveUp)
				return Input.GetAxis("VerticalMove");
			else if (inp == E_Input.Axis_ViewRight)
				return Input.GetAxis("HorizontalView");
			else if (inp == E_Input.Axis_ViewUp)
				return Input.GetAxis("VerticalView");
		}
		return 0;
	}

	public static bool ButtonDown(E_Input inp)
	{
		return GamepadInputManager.Instance.ControlDown(inp);
	}

	public static bool ButtonUp(E_Input inp)
	{
		return GamepadInputManager.Instance.ControlUp(inp);
	}

	void UpdateActions()
	{
		if (ButtonDown(E_Input.Fire))
		{
			//fire only when there is no use
			if (Player.LocalInstance.InUseMode)
				States.UseDelegate();
			else
				States.FireDownDelegate();
		}

		if (ButtonUp(E_Input.Fire))
		{
			//delegate up event only for fire 
			if (Player.LocalInstance.InUseMode == false)
				States.FireUpDelegate();
		}

		if (ButtonDown(E_Input.Reload) /* || Input.GetKeyDown("7")*/) //JV FIX
		{
			States.ReloadDelegate();
		}

		if (ButtonDown(E_Input.Sprint))
		{
			States.SprintDownDelegate();
		}

		if (ButtonUp(E_Input.Sprint))
		{
			States.SprintUpDelegate();
		}

		if (ButtonDown(E_Input.Roll))
		{
			States.RollDelegate();
		}

		//FIX chyby na androidu - device obcas hlasi spatne escape stav kdyz delame multi touch
		bool escapeKeyPressed = Input.GetKeyUp("escape") && (Time.timeSinceLevelLoad > LastTouchControlTime + 0.25f);

		if ((ButtonDown(E_Input.Pause) || escapeKeyPressed))
		{
			if (MFGuiFader.Fading == false && GuiFrontendIngame.PauseMenuCooldown() == false)
				GuiFrontendIngame.ShowPauseMenu();
		}

		if (ButtonDown(E_Input.Item1))
		{
			ChangeItem(0);
		}
		else if (ButtonDown(E_Input.Item2))
		{
			ChangeItem(1);
		}
		else if (ButtonDown(E_Input.Item3))
		{
			ChangeItem(2);
		}

		if (ButtonDown(E_Input.Weapon1))
		{
			ChangeWeapon(0);
		}
		else if (ButtonDown(E_Input.Weapon2))
		{
			ChangeWeapon(1);
		}
		else if (ButtonDown(E_Input.Weapon3))
		{
			ChangeWeapon(2);
		}

		if (ButtonDown(E_Input.WeaponNext))
		{
			ChangeWeaponNext();
		}
		else if (ButtonDown(E_Input.WeaponPrev))
		{
			ChangeWeaponPrev();
		}

		/*if(ButtonDown(E_Input.Voicechat))
		{
			if(Client.Instance)
				Client.Instance.SetVoiceChat(true);
		}
		else if (ButtonUp(E_Input.Voicechat))
		{
			if(Client.Instance)
				Client.Instance.SetVoiceChat(false);
		}*/
	}

	public void Update()
	{
		//ovladani input controllerem
		if (!Enabled)
			return;

		if (Input.touchCount != 0)
		{
			LastTouchControlTime = Time.timeSinceLevelLoad;
		}

		//Debug.Log("States.ActionsEnabled: " + States.ActionsEnabled);

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

	void ChangeItem(int slotId)
	{
		E_ItemID id = GuiHUD.Instance.GetGadgetInInventoryIndex(slotId);

		if (id != E_ItemID.None && Player.LocalInstance.Owner.GadgetsComponent.GetGadget(id).Settings.ItemUse == E_ItemUse.Activate)
			States.UseGadgetDelegate(id);
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

	/*void ChangeWeaponPrev()
	{
		if ( !Player.Instance.CanChangeWeapon() || !Player.Instance.Owner.WeaponComponent || !Player.Instance.Owner.WeaponComponent.GetCurrentWeapon() )
			return;
		
		E_WeaponID curWeaponID = Player.Instance.Owner.WeaponComponent.GetCurrentWeapon().WeaponID;
		int newId = (int)curWeaponID;

		do
		{
			newId = (newId <= 0) ? ((int)E_WeaponID.Max) - 1 : newId - 1;
			if(CanChangeToWeapon((E_WeaponID) newId))
			{
				ChangeWeapon( (E_WeaponID) newId );
				break;
			}
		}
		while(newId != (int)curWeaponID);
	}

	void ChangeWeaponNext()
	{
		if ( !Player.Instance.CanChangeWeapon() || !Player.Instance.Owner.WeaponComponent || !Player.Instance.Owner.WeaponComponent.GetCurrentWeapon() )
			return;
		
		E_WeaponID curWeaponID = Player.Instance.Owner.WeaponComponent.GetCurrentWeapon().WeaponID;
		int newId = (int)curWeaponID;
		
		do
		{
			newId = (newId + 1) % (int)E_WeaponID.Max;
			if(CanChangeToWeapon((E_WeaponID) newId))
			{
				ChangeWeapon( (E_WeaponID) newId );
				break;
			}
		}
		while(newId != (int)curWeaponID);
	}*/
}
