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

//Obsahuje stavy slouzici pro ovladani playera. Stavy jsou sdileny a updatovany jednotlivymi typy ovladani.
public class PlayerControlStates : InputController
{
	public class MoveState
	{
		public bool Enabled = true;
		public Vector3 Direction = new Vector3();
		public float Force;

		public void ZeroInput()
		{
			Direction = Vector3.zero;
			Force = 0;
		}
	}

	public class ViewState
	{
		public bool Enabled = true;
		public float YawAdd;
		public float PitchAdd;

		public void ZeroInput()
		{
			YawAdd = 0;
			PitchAdd = 0;
		}

		public void SetNewRotation(float Yaw, float Pitch)
		{
			// ------------
			// TS: reduce y-axis changes
			float yawAbs = Mathf.Abs(Yaw);
			float pitchAbs = Mathf.Abs(Pitch);
			float limit = yawAbs*0.5f;
			if (pitchAbs < limit)
				Pitch = 0;
			// ------------ 			

			YawAdd = Yaw;
			PitchAdd = GuiOptions.invertYAxis ? -Pitch : Pitch;
		}
	}

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

	// stavy do kterych zapisuji jednotlive controls styly (touch, gpad, mys) 
	public MoveState Move = new MoveState();
	public ViewState View = new ViewState();
	public bool ActionsEnabled = true;

	public bool Fire = false;
	public bool Use = false;

	public delegate void ButtonDelegate();
	public ButtonDelegate FireDownDelegate;
	public ButtonDelegate FireUpDelegate;
	public ButtonDelegate UseDelegate;
	public ButtonDelegate ReloadDelegate;
	public ButtonDelegate RollDelegate;
	public ButtonDelegate SprintDownDelegate;
	public ButtonDelegate SprintUpDelegate;

	public delegate void TouchDelegate(InteractionObject obj);
	public TouchDelegate UseObjectDelegate;

	public delegate void WeaponDelegate(E_WeaponID weaponType);
	public WeaponDelegate ChangeWeaponDelegate;

	public delegate void GadgetDelegate(E_ItemID gadget);
	public GadgetDelegate UseGadgetDelegate;

	public delegate void CommandDelegate(E_CommandID gadget);
	public CommandDelegate SendCommandDelegate;

	public Transform _Temp;

	PlayerControlsTouch TouchControls = null;
#if UNITY_EDITOR || MADFINGER_KEYBOARD_MOUSE
	PlayerControlsPC PCControls;
#endif
	PlayerControlsGamepad GamepadControls;
#if UNITY_ANDROID && !UNITY_EDITOR
	PlayerControlsMoga MogaControls;
	PlayerControlsBlueTooth BlueToothControls;
#endif

	PlayerControlsDrone DroneControls;

	public void LockCursor(bool state)
	{
#if UNITY_EDITOR || MADFINGER_KEYBOARD_MOUSE
		if (PCControls != null)
			PCControls.LockCursor = state;
#endif
	}

	public void EnableLockCursor(bool state)
	{
#if UNITY_EDITOR || MADFINGER_KEYBOARD_MOUSE
		if (PCControls != null)
			PCControls.EnableLockCursor = state;
#endif
	}

	public void Start()
	{
		GameObject t = new GameObject();
		GameObject.DontDestroyOnLoad(t);
		_Temp = t.transform;

#if !MADFINGER_KEYBOARD_MOUSE
		TouchControls = new PlayerControlsTouch(this);
#endif
		GamepadControls = new PlayerControlsGamepad(this);

#if UNITY_EDITOR || MADFINGER_KEYBOARD_MOUSE
		PCControls = new PlayerControlsPC(this);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
		MogaControls = new PlayerControlsMoga(this);
		BlueToothControls = new PlayerControlsBlueTooth(this);
#endif

		if (PlayerControlsDrone.Enabled)
		{
			DroneControls = new PlayerControlsDrone(this);
		}

		Priority = (int)E_InputPriority.Game;
		Opacity = E_InputOpacity.Opaque;
		InputManager.Register(this);
	}

	public void Destroy()
	{
		InputManager.Unregister(this);

		if (_Temp != null)
		{
			GameObject.Destroy(_Temp.gameObject);
		}
	}

	public void SwitchToUseMode()
	{
		GuiHUD.Instance.ShowActionButton(GuiHUD.E_ActionButton.Use);
		Move.Enabled = true;
		//Debug.Log("Use mode");
	}

	public void SwitchToCombatMode()
	{
		//Todo: ComponentPlayer vola Enable input jeste predtim nez je inicializovane gui, pokud to bude vadit (tj nezobrazi se spravne hud na zacatku  hry), tak treba domyslet.
		GuiHUD.Instance.ShowActionButton(GuiHUD.E_ActionButton.Fire);

		Move.Enabled = true;
		//Debug.Log("Combat mode");
	}

	public void DisableInput()
	{
		Reset();

		CaptureInput = false;

		GuiHUD.Instance.ShowActionButton(GuiHUD.E_ActionButton.None);
		Move.Enabled = false;
		View.Enabled = false;
		ActionsEnabled = false;
	}

	public void EnableInput()
	{
		SwitchToCombatMode();
		Move.Enabled = true;
		View.Enabled = true;
		ActionsEnabled = true;

		CaptureInput = true;
	}

	public void ControlSchemeChanged()
	{
		if (TouchControls != null)
			TouchControls.OnControlSchemeChange();
	}

	public void Reset()
	{
		Fire = false;
		Move.Direction = Vector3.zero;
		Move.Force = 0;

		InputManager.FlushInput();

		if (TouchControls != null)
			TouchControls.Reset();

		//GuiManager.Instance.ResetControls();		
	}

	public void Update()
	{
		if (CaptureInput == false)
			return;
		if (Game.Instance.GameState != E_GameState.Game)
			return;

#if UNITY_EDITOR || MADFINGER_KEYBOARD_MOUSE
		if (PCControls != null)
			PCControls.Update();
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
		if(MogaControls != null && MogaGamepad.IsConnected())
			MogaControls.Update();
		
		if(BlueToothControls != null)
			BlueToothControls.Update();
#endif
		GamepadControls.Update();

		if (TouchControls != null)
			TouchControls.Update();

		if (null != DroneControls)
		{
			DroneControls.Update();
		}
	}

	// INPUTCONTROLLER INTERFACE

	protected override void OnActivate()
	{
	}

	protected override void OnDeactivate()
	{
	}

	protected override bool OnProcess(ref IInputEvent evt)
	{
		if (TouchControls != null && TouchControls.Process(ref evt) == true)
			return true;
#if UNITY_EDITOR || MADFINGER_KEYBOARD_MOUSE
		if (PCControls != null && PCControls.Process(ref evt) == true)
			return true;
#endif
		return false;
	}

	public void SetKeyboardInputTable(JoyInput[] inputTable)
	{
#if UNITY_EDITOR || MADFINGER_KEYBOARD_MOUSE
		if (PCControls != null)
			PCControls.SetInputTable(inputTable);
#endif
	}
}
