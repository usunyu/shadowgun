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

class GamepadDefaultMappings
{
	public bool HasConfig(string gpadName)
	{
		if (gpadName == null)
			return false;
		for (int i = 0; i < m_SupportedGpads.Length; i++)
		{
			if (gpadName.Contains(m_SupportedGpads[i].idName))
				return true;
		}
		return false;
	}

	public JoyInput[] GetConfig(string gpadName)
	{
		if (gpadName == null)
			return EmptyDefaults;
		for (int i = 0; i < m_SupportedGpads.Length; i++)
		{
			if (gpadName.Contains(m_SupportedGpads[i].idName))
				return m_SupportedGpads[i].defaults;
		}
		return EmptyDefaults;
	}

	public JoyInput[] GetEmptyConfig()
	{
		return EmptyDefaults;
	}

//--------------------------------------------------------------------------------------------	
//	
//--------------------------------------------------------------------------------------------	
	struct GpadConfig
	{
		public GpadConfig(string _idName, JoyInput[] _defaults)
		{
			idName = _idName;
			defaults = _defaults;
		}

		public string idName;
		public JoyInput[] defaults;
	};

	public static string NvidiaShieldName1 = "nvidia_Corporation nvidia_joypad";
	public static string NvidiaShieldName2 = "NVIDIA Corporation NVIDIA Controller v01";
	public const string MadCatzName = "Mad Catz C.T.R.L.R (Smart)";

	GpadConfig[] m_SupportedGpads =
	{
		new GpadConfig("Logitech Logitech Dual Action", LogitechDualDefault),
		new GpadConfig("Sony PLAYSTATION(R)3 Controller", PS3Default),
		new GpadConfig("Microsoft X-Box 360 pad", X360Default),
		new GpadConfig("Generic X-Box pad", LogitechF710Default),
		new GpadConfig("WikiPad Controller", WikiPadControllerDefault),
		new GpadConfig("Moga Pro HID", MogaProHIDDefault),
		// OSX with russian language has a specific settings
		new GpadConfig("Keyboard", (!InputDriverKeyboard.IsOSXRussianWrongKeyCodesKeyboard ? KeyboardDefaultGenaral : KeyboardDefaultOSXRussian)),
		new GpadConfig(NvidiaShieldName1, NVidiaShieldDefault),
		new GpadConfig(NvidiaShieldName2, NVidiaShieldDefault),
		new GpadConfig("Controller (XBOX 360 For Windows)", X360WinDefault),
		new GpadConfig(MadCatzName, MadCatzDefault),
	};

//-------------------------------------------------------------------------------------------	
//  Default configs	
//-------------------------------------------------------------------------------------------	
	static JoyInput[] EmptyDefaults = new JoyInput[(int)PlayerControlsGamepad.E_Input.COUNT]
	{
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //fire
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //reload
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //Sprint
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //Roll
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //Pause
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //Weapon1
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //Weapon2
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //Weapon3
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //Item1
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //Item2
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //Item3
		
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //move right
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //move up
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //look right
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //look up
		
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //next weapon
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //prev weapon
	};

	static JoyInput[] X360Default = new JoyInput[(int)PlayerControlsGamepad.E_Input.COUNT]
	{
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_8b), //fire
		new JoyInput(KeyCode.Joystick1Button3, E_JoystickAxis.NONE), //reload
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_7b), //sprint
		new JoyInput(KeyCode.Joystick1Button6, E_JoystickAxis.NONE), //roll
		new JoyInput(KeyCode.Joystick1Button11, E_JoystickAxis.NONE), //pause
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon 1
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon 2
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon 3
		new JoyInput(KeyCode.Joystick1Button0, E_JoystickAxis.NONE), //Item1	
		new JoyInput(KeyCode.Joystick1Button1, E_JoystickAxis.NONE), //Item2
		new JoyInput(KeyCode.Joystick1Button4, E_JoystickAxis.NONE), //Item3
		
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickRight), //move right
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickUp), //move up
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_3b), //look right
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_4a), //look up
		
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_5b), //next weapon
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_5a), //prev weapon
	};

	static JoyInput[] PS3Default = new JoyInput[(int)PlayerControlsGamepad.E_Input.COUNT]
	{
		new JoyInput(KeyCode.Joystick1Button9, E_JoystickAxis.NONE), //fire
		new JoyInput(KeyCode.Joystick1Button15, E_JoystickAxis.NONE), //reload
		new JoyInput(KeyCode.Joystick1Button8, E_JoystickAxis.NONE), //sprint
		new JoyInput(KeyCode.Joystick1Button10, E_JoystickAxis.NONE), //roll
		new JoyInput(KeyCode.Joystick1Button3, E_JoystickAxis.NONE), //pause
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon1
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon2
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon3
		new JoyInput(KeyCode.Joystick1Button14, E_JoystickAxis.NONE), //item1
		new JoyInput(KeyCode.Joystick1Button13, E_JoystickAxis.NONE), //item2
		new JoyInput(KeyCode.Joystick1Button12, E_JoystickAxis.NONE), //item3
		
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickRight), //move right
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickUp), //move up
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_3b), //look right
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_4a), //look up
		
		new JoyInput(KeyCode.Joystick1Button5, E_JoystickAxis.NONE), //next weapon
		new JoyInput(KeyCode.Joystick1Button7, E_JoystickAxis.NONE), //prev weapon
	};

	static JoyInput[] LogitechDualDefault = new JoyInput[(int)PlayerControlsGamepad.E_Input.COUNT]
	{
		new JoyInput(KeyCode.Joystick1Button7, E_JoystickAxis.NONE), //fire
		new JoyInput(KeyCode.Joystick1Button0, E_JoystickAxis.NONE), //reload
		new JoyInput(KeyCode.Joystick1Button6, E_JoystickAxis.NONE), //sprint
		new JoyInput(KeyCode.Joystick1Button4, E_JoystickAxis.NONE), //roll
		new JoyInput(KeyCode.Joystick1Button9, E_JoystickAxis.NONE), //pause
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon1
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon2
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon3
		new JoyInput(KeyCode.Joystick1Button1, E_JoystickAxis.NONE), //item1
		new JoyInput(KeyCode.Joystick1Button2, E_JoystickAxis.NONE), //item2
		new JoyInput(KeyCode.Joystick1Button3, E_JoystickAxis.NONE), //item3
		
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickRight), //move right
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickUp), //move up
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_3b), //look right
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_4a), //look up
		
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_5b), //next weapon
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_5a), //prev weapon
	};

	static JoyInput[] LogitechF710Default = new JoyInput[(int)PlayerControlsGamepad.E_Input.COUNT]
	{
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_8b), //fire
		new JoyInput(KeyCode.Joystick1Button3, E_JoystickAxis.NONE), //reload
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_7b), //sprint
		new JoyInput(KeyCode.Joystick1Button6, E_JoystickAxis.NONE), //roll
		new JoyInput(KeyCode.Joystick1Button11, E_JoystickAxis.NONE), //pause
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon1
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon2
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon3
		new JoyInput(KeyCode.Joystick1Button0, E_JoystickAxis.NONE), //item1	
		new JoyInput(KeyCode.Joystick1Button1, E_JoystickAxis.NONE), //item2
		new JoyInput(KeyCode.Joystick1Button4, E_JoystickAxis.NONE), //item3
		
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickRight), //move right
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickUp), //move up
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_3b), //look right
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_4a), //look up
		
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_5b), //next weapon
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_5a), //prev weapon
	};

	static JoyInput[] WikiPadControllerDefault = new JoyInput[(int)PlayerControlsGamepad.E_Input.COUNT]
	{
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_8b), //fire
		new JoyInput(KeyCode.Joystick1Button7, E_JoystickAxis.NONE), //reload
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_7b), //sprint
		new JoyInput(KeyCode.Joystick1Button6, E_JoystickAxis.NONE), //roll 
		new JoyInput(KeyCode.Joystick1Button11, E_JoystickAxis.NONE), //pause
		
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weap1
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weap2
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weap3
		
		new JoyInput(KeyCode.Joystick1Button0, E_JoystickAxis.NONE), //item1
		new JoyInput(KeyCode.Joystick1Button1, E_JoystickAxis.NONE), //item2
		new JoyInput(KeyCode.Joystick1Button3, E_JoystickAxis.NONE), //item3
		
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickRight), //move right
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickUp), //move up
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_3b), //look right
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_4a), //look up
		
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_5b), //next weapon
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_5a), //prev weapon
	};

	static JoyInput[] MogaProHIDDefault = new JoyInput[(int)PlayerControlsGamepad.E_Input.COUNT]
	{
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_7b), //fire
		new JoyInput(KeyCode.Joystick1Button7, E_JoystickAxis.NONE), //reload
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_8b), //sprint
		new JoyInput(KeyCode.Joystick1Button6, E_JoystickAxis.NONE), //roll
		new JoyInput(KeyCode.Joystick1Button11, E_JoystickAxis.NONE), //pause
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon1
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon2
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon3
		new JoyInput(KeyCode.Joystick1Button3, E_JoystickAxis.NONE), //item1	
		new JoyInput(KeyCode.Joystick1Button4, E_JoystickAxis.NONE), //item2
		new JoyInput(KeyCode.Joystick1Button1, E_JoystickAxis.NONE), //item3
		
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickRight), //move right
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickUp), //move up
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_3b), //look right
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_4a), //look up
		
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_5b), //next weapon
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_5a), //prev weapon
	};

	// general keyboard settings
	static JoyInput[] KeyboardDefaultGenaral = new JoyInput[(int)PlayerControlsGamepad.E_Input.COUNT]
	{
		new JoyInput(KeyCode.Mouse0, E_JoystickAxis.NONE), //fire
		new JoyInput(KeyCode.R, E_JoystickAxis.NONE), //reload
		new JoyInput(KeyCode.LeftShift, E_JoystickAxis.NONE), //sprint
		new JoyInput(KeyCode.Space, E_JoystickAxis.NONE), //roll
		new JoyInput(KeyCode.Q, E_JoystickAxis.NONE), //pause, it is used for quick command menu on PC
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon1
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon2
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon3
		new JoyInput(KeyCode.Alpha1, E_JoystickAxis.NONE), //item1	
		new JoyInput(KeyCode.Alpha2, E_JoystickAxis.NONE), //item2
		new JoyInput(KeyCode.Alpha3, E_JoystickAxis.NONE), //item3
		
		new JoyInput(KeyCode.D, E_JoystickAxis.NONE), //move right
		new JoyInput(KeyCode.W, E_JoystickAxis.NONE), //move up
		new JoyInput(KeyCode.A, E_JoystickAxis.NONE), //look right, it is used for move left on PC
		new JoyInput(KeyCode.S, E_JoystickAxis.NONE), //look up, it is used for move down on pc
		
		new JoyInput(KeyCode.Mouse6, E_JoystickAxis.NONE), //next weapon, Mouse6 is interpreted as mouse wheel up
		new JoyInput(KeyCode.Mouse5, E_JoystickAxis.NONE), //prev weapon, Mouse5 is interpreted as mouse wheel down
	};

	// default keyboard for OSX with russian language
	// OSX with russian language return key codes which are not contained in the KeyCode enum
	static JoyInput[] KeyboardDefaultOSXRussian = new JoyInput[(int)PlayerControlsGamepad.E_Input.COUNT]
	{
		new JoyInput(KeyCode.Mouse0, E_JoystickAxis.NONE), //fire
		//174 is code of the key R on Russian OSX
		new JoyInput((KeyCode)174, E_JoystickAxis.NONE), //reload
		new JoyInput(KeyCode.LeftShift, E_JoystickAxis.NONE), //sprint
		new JoyInput(KeyCode.Space, E_JoystickAxis.NONE), //roll
		//171 is code of the key Q on Russian OSX
		new JoyInput((KeyCode)171, E_JoystickAxis.NONE), //pause, it is used for quick command menu on PC
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon1
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon2
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon3
		new JoyInput(KeyCode.Alpha1, E_JoystickAxis.NONE), //item1	
		new JoyInput(KeyCode.Alpha2, E_JoystickAxis.NONE), //item2
		new JoyInput(KeyCode.Alpha3, E_JoystickAxis.NONE), //item3
		
		//162 is code of the key D on Russian OSX
		new JoyInput((KeyCode)162, E_JoystickAxis.NONE), //move right
		//172 is code of the key W on Russian OSX
		new JoyInput((KeyCode)172, E_JoystickAxis.NONE), //move up
		//160 is code of the key A on Russian OSX
		new JoyInput((KeyCode)160, E_JoystickAxis.NONE), //look right, it is used for move left on PC
		//161 is code of the key S on Russian OSX
		new JoyInput((KeyCode)161, E_JoystickAxis.NONE), //look up, it is used for move down on pc
		
		new JoyInput(KeyCode.Mouse6, E_JoystickAxis.NONE), //next weapon, Mouse6 is interpreted as mouse wheel up
		new JoyInput(KeyCode.Mouse5, E_JoystickAxis.NONE), //prev weapon, Mouse5 is interpreted as mouse wheel down
	};

	static JoyInput[] NVidiaShieldDefault = new JoyInput[(int)PlayerControlsGamepad.E_Input.COUNT]
	{
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_8b), //fire
		new JoyInput(KeyCode.Joystick1Button7, E_JoystickAxis.NONE), //reload
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_7b), //sprint
		new JoyInput(KeyCode.Joystick1Button6, E_JoystickAxis.NONE), //roll
		new JoyInput(KeyCode.Joystick1Button11, E_JoystickAxis.NONE), //pause
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon 1
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon 2
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon 3
		new JoyInput(KeyCode.Joystick1Button0, E_JoystickAxis.NONE), //Item1	
		new JoyInput(KeyCode.Joystick1Button1, E_JoystickAxis.NONE), //Item2
		new JoyInput(KeyCode.Joystick1Button4, E_JoystickAxis.NONE), //Item3
		
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickRight), //move right
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickUp), //move up
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_3b), //look right
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_4a), //look up
		
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_5b), //next weapon
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_5a), //prev weapon
	};

	static JoyInput[] X360WinDefault = new JoyInput[(int)PlayerControlsGamepad.E_Input.COUNT]
	{
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_3a), //fire
		new JoyInput(KeyCode.JoystickButton5, E_JoystickAxis.NONE), //reload
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_3b), //sprint
		new JoyInput(KeyCode.JoystickButton0, E_JoystickAxis.NONE), //roll
		new JoyInput(KeyCode.JoystickButton7, E_JoystickAxis.NONE), //pause
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon 1
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon 2
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon 3
		new JoyInput(KeyCode.JoystickButton2, E_JoystickAxis.NONE), //Item1	
		new JoyInput(KeyCode.JoystickButton3, E_JoystickAxis.NONE), //Item2
		new JoyInput(KeyCode.JoystickButton1, E_JoystickAxis.NONE), //Item3
		
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickRight), //move right
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickUp), //move up
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_4b), //look right
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_5a), //look up
		
		new JoyInput(KeyCode.JoystickButton4, E_JoystickAxis.NONE), //next weapon
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //prev weapon
	};

	static JoyInput[] MadCatzDefault = new JoyInput[(int)PlayerControlsGamepad.E_Input.COUNT]
	{
		new JoyInput(KeyCode.JoystickButton7, E_JoystickAxis.NONE), //fire
		new JoyInput(KeyCode.JoystickButton5, E_JoystickAxis.NONE), //reload
		new JoyInput(KeyCode.Joystick1Button6, E_JoystickAxis.NONE), //sprint
		new JoyInput(KeyCode.JoystickButton3, E_JoystickAxis.NONE), //roll
		new JoyInput(KeyCode.JoystickButton9, E_JoystickAxis.NONE), //pause
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon 1
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon 2
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE), //weapon 3
		new JoyInput(KeyCode.JoystickButton0, E_JoystickAxis.NONE), //Item1	
		new JoyInput(KeyCode.JoystickButton1, E_JoystickAxis.NONE), //Item2
		new JoyInput(KeyCode.JoystickButton2, E_JoystickAxis.NONE), //Item3
		
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickRight), //move right
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickUp), //move up
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_3b), //look right
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_4a), //look up
		
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_5b), //next weapon
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_5a), //prev weapon
	};
}
