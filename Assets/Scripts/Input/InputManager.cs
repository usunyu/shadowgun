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

public class InputManager : ScriptableObject
{
	static InputManager m_Instance;

	// PRIVATE MEMBERS

	List<InputDriver> m_Drivers;
	List<InputController> m_Controllers = new List<InputController>();
	bool m_ProcessingInput = false;
	bool m_PendingFlushInput = false;
	bool m_IsEnabled = true;

	// PUBLIC MEMBERS

	public static bool IsEnabled
	{
		get { return m_Instance ? m_Instance.m_IsEnabled : false; }
		set
		{
			if (m_Instance != null)
			{
				m_Instance.m_IsEnabled = value;
			}
		}
	}

	public static bool HasTouchScreenControl()
	{
		foreach (var driver in m_Instance.m_Drivers)
		{
			if (driver is InputDriverTouch)
				return true;
		}

		return false;
	}

	// PUBLIC METHODS

	public static bool Register(InputController controller)
	{
		if (m_Instance == null)
			return false;
		if (controller == null)
			return false;
		if (m_Instance.m_Controllers.Contains(controller) == true)
			return true;

		FlushInput();

		int idx = m_Instance.m_Controllers.Count - 1;
		for (; idx >= 0; --idx)
		{
			if (m_Instance.m_Controllers[idx].Priority <= controller.Priority)
			{
				break;
			}
		}

		m_Instance.m_Controllers.Insert(idx + 1, controller);

		return true;
	}

	public static bool Unregister(InputController controller)
	{
		if (m_Instance == null)
			return false;
		if (controller == null)
			return false;
		if (m_Instance.m_Controllers.Contains(controller) == false)
			return true;

		FlushInput();

		m_Instance.m_Controllers.Remove(controller);

		return true;
	}

	public static void FlushInput()
	{
		if (m_Instance == null)
			return;

		//Debug.Log(">>>> FLUSH INPUT :: m_ProcessingInput="+m_Instance.m_ProcessingInput);

		if (m_Instance.m_ProcessingInput == false)
		{
			// it's safe to flush input now
			foreach (var driver in m_Instance.m_Drivers)
			{
				driver.Flush();
			}

			Input.ResetInputAxes();
		}
		else
		{
			// we are processing input now
			// se we will delay flush until finished
			m_Instance.m_PendingFlushInput = true;
		}
	}

	// INTERNAL METHODS

	internal static void Initialize()
	{
		//System.DateTime time = System.DateTime.UtcNow;

#if DEADZONE_DEDICATEDSERVER
		
			return;
		
#else // DEADZONE_DEDICATEDSERVER

		if (m_Instance == null)
		{
			m_Instance = ScriptableObject.CreateInstance<InputManager>();
			if (m_Instance == null)
			{
				Debug.LogError("Can't create InputManager");
			}
			else
			{
				m_Instance.CreateDrivers();
				ScriptableObject.DontDestroyOnLoad(m_Instance);
			}
		}

#endif // DEADZONE_DEDICATEDSERVER

		//Debug.Log(GetInstance().GetType().Name + " initialized in " + System.DateTime.UtcNow.Subtract(time).TotalMilliseconds + " miliseconds.");
	}

	internal static void Deinitialize()
	{
		FlushInput();

		if (m_Instance != null)
		{
			m_Instance.ReleaseDrivers();
			m_Instance.m_Controllers.Clear();

			ScriptableObject.Destroy(m_Instance);
		}

		m_Instance = null;

		//Debug.Log(GetInstance().GetType().Name + " deinitialized.");
	}

	internal static void Update()
	{
		if (m_Instance == null)
			return;
		if (m_Instance.m_IsEnabled == false)
			return;

		// activate/deactivate controllers
		foreach (var controller in m_Instance.m_Controllers)
		{
			controller.IsActive = controller.CaptureInput;
		}

		// process input
		m_Instance.m_ProcessingInput = true;
		foreach (var driver in m_Instance.m_Drivers)
		{
			driver.Update();
		}
		m_Instance.m_ProcessingInput = false;

		// process pending flush if asked for
		if (m_Instance.m_PendingFlushInput == true)
		{
			FlushInput();
			m_Instance.m_PendingFlushInput = false;
		}

		if (PlayerControlsDrone.Enabled)
		{
			PlayerControlsDrone.UpdateMenu();
		}
	}

	internal void Process(IInputEvent evt)
	{
		//Debug.Log(GetType().Name + ".Process() :: event=" + evt);

		foreach (var controller in m_Controllers)
		{
			if (controller.CaptureInput == false)
				continue;

			bool result = controller.Process(ref evt);

			//TODO ... do some code here ...

			if (controller.Opacity == E_InputOpacity.SemiTransparent && result == true)
				break;
			if (controller.Opacity == E_InputOpacity.Opaque)
				break;

			//TODO ... do some code here ...
		}
	}

	// PRIVATE METHODS

	void CreateDrivers()
	{
		m_Drivers = new List<InputDriver>();

#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
		m_Drivers.Add(new InputDriverKeyboard());
		m_Drivers.Add(new InputDriverMouse());
#else
		m_Drivers.Add(new InputDriverKeyboard());
		
		if(SystemInfo.deviceModel.Contains("MOJO"))
			m_Drivers.Add(new InputDriverMouse());
		
		m_Drivers.Add(new InputDriverTouch());
#endif

		foreach (var driver in m_Drivers)
		{
			driver.Initialize(this);
		}

		m_Drivers.Capacity = m_Drivers.Count;
	}

	void ReleaseDrivers()
	{
		foreach (var driver in m_Drivers)
		{
			driver.Deinitialize(this);
		}

		m_Drivers = null;
	}
}
