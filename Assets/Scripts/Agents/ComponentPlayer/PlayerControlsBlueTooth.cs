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

public class PlayerControlsBlueTooth
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

	public PlayerControlsBlueTooth(PlayerControlStates inStates)
	{
		States = inStates;
	}

	void UpdateMove()
	{
		//move by joystick 
		Vector2 dir = Vector2.zero;

		{
			dir.x = Input.GetAxis("HorizontalMove");
			dir.y = Input.GetAxis("VerticalMove");
		}

		float dist = Mathf.Clamp(dir.magnitude, 0f, 1f);

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
		Vector2 viewDir = Vector2.zero;

		{
			viewDir.x = Input.GetAxisRaw("HorizontalView")*ViewSensitivityX;
			viewDir.y = Input.GetAxisRaw("VerticalView")*ViewSensitivityY;
		}

		float yaw1 = viewDir.x;
		float pitch1 = viewDir.y;

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
			UpdateActionsBlueTooth();
	}

	void UpdateActionsBlueTooth()
	{
		//ovladani tlacitky
		//Fire:
		if (Input.GetKeyDown("8"))
		{
			//fire only when there is no use
			if (Player.LocalInstance.InUseMode)
				States.UseDelegate();
			else
				States.FireDownDelegate();
		}

		if (Input.GetKeyUp("8"))
		{
			//delegate up event only for fire 
			if (Player.LocalInstance.InUseMode == false)
				States.FireUpDelegate();
		}

		//reload
		if (Input.GetKeyDown("7"))
		{
			States.ReloadDelegate();
		}

		//roll
		if (Input.GetKeyDown("6"))
		{
			States.RollDelegate();
		}

		//items
		if (Input.GetKeyDown("3"))
		{
			ChangeItem(0);
		}
		else if (Input.GetKeyDown("4"))
		{
			ChangeItem(1);
		}
		else if (Input.GetKeyDown("2"))
		{
			ChangeItem(2);
		}

		//weapons
		if (Input.GetKeyDown("a"))
		{
			ChangeWeapon(0);
		}
		else if (Input.GetKeyDown("w"))
		{
			ChangeWeapon(1);
		}
		else if (Input.GetKeyDown("d"))
		{
			ChangeWeapon(2);
		}
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
}
