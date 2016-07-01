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

public class JoyInput
{
	public JoyInput(KeyCode _key, E_JoystickAxis _joyAxis)
	{
		key = _key;
		joyAxis = _joyAxis;
	}

	public KeyCode key;
	public E_JoystickAxis joyAxis;
};

class GamepadInputManager
{
	public static GamepadInputManager Instance;

	public static void CreateInstance()
	{
		Instance = new GamepadInputManager();
		Instance.Init();
	}

	public static void DestroyInstance()
	{
		Instance = null;
	}

	List<JoyInput> m_InputK = new List<JoyInput>();

	bool[] m_WasAxisPressed = new bool[(int)E_JoystickAxis.COUNT];
	bool[] m_AxisDown = new bool[(int)E_JoystickAxis.COUNT];
	bool[] m_AxisUp = new bool[(int)E_JoystickAxis.COUNT];

	GamepadDefaultMappings m_DefaulMappings = new GamepadDefaultMappings();

	const int MaxSaveSlots = 10;

	public JoyInput[] InputTable
	{
		get
		{
			if (m_InputK == null || m_InputK.Count != (int)E_JoystickAxis.COUNT + 1)
				return null;
			return m_InputK.ToArray();
		}
	}

	bool m_ShieldCached = false;
	bool m_IsNvidiaShield = false;

	public bool IsNvidiaShield()
	{
		if (!m_ShieldCached)
			DetectShield();
		return m_IsNvidiaShield;
	}

	void DetectShield()
	{
		string curName = Game.CurrentJoystickName();
		m_IsNvidiaShield = (curName != null) &&
						   (curName.Contains(GamepadDefaultMappings.NvidiaShieldName1) || curName.Contains(GamepadDefaultMappings.NvidiaShieldName2));
		m_ShieldCached = true;
	}

//----------------------------------------------	
//	interface
//----------------------------------------------	

	//is button fopr this action currently pressed
	public bool ControlPressed(PlayerControlsGamepad.E_Input inpAction)
	{
		if ((int)inpAction >= GetActionCount())
			return false;

		JoyInput btn = m_InputK[(int)inpAction];
		if (btn.joyAxis == E_JoystickAxis.NONE)
		{
			return Input.GetKey(btn.key);
		}
		else
		{
			return Input.GetAxis(GamepadAxis.GetAxis(btn.joyAxis)) > 0.95f;
		}
	}

	public bool ControlDown(PlayerControlsGamepad.E_Input inpAction)
	{
		if ((int)inpAction >= GetActionCount())
			return false;

		JoyInput btn = m_InputK[(int)inpAction];
		if (btn.joyAxis == E_JoystickAxis.NONE)
		{
			return Input.GetKeyDown(btn.key);
		}
		else
		{
			return m_AxisDown[(int)btn.joyAxis];
		}
	}

	public bool ControlUp(PlayerControlsGamepad.E_Input inpAction)
	{
		if ((int)inpAction >= GetActionCount())
			return false;

		JoyInput btn = m_InputK[(int)inpAction];
		if (btn.joyAxis == E_JoystickAxis.NONE)
		{
			return Input.GetKeyUp(btn.key);
		}
		else
		{
			return m_AxisUp[(int)btn.joyAxis];
		}
	}

	public JoyInput GetActionButton(PlayerControlsGamepad.E_Input inpAction)
	{
		return m_InputK[(int)inpAction];
	}

	public void SetActionButton(PlayerControlsGamepad.E_Input inpAction, JoyInput btn)
	{
		m_InputK[(int)inpAction] = btn;
	}

	public static void Update()
	{
		if (Instance != null)
			Instance.OnUpdate();
	}

	public bool HasConfig(string gpadName)
	{
		return (HasStoredConfig(gpadName) || HasDefaultConfig(gpadName));
	}

	public void ClearConfig()
	{
		SetDefaults(m_DefaulMappings.GetEmptyConfig());
	}

	public void SetConfig(string gpadName)
	{
		//pokud mame ulozeny config, pokus se ho nacist
		if (HasStoredConfig(gpadName))
		{
			int loadSlot = StoredConfigIndex(gpadName);
			//Debug.Log("Loading config from slot: " + loadSlot + "gpad: " + gpadName);

			if (loadConfig(loadSlot))
			{
				// keyboard with wrong key codes
				// loaded setting contains right KeyCode which is not working on the keyboard with wrong codes
				if (InputDriverKeyboard.IsOSXRussianWrongKeyCodesKeyboard &&
					GetActionButton(PlayerControlsGamepad.E_Input.Axis_MoveUp).key == KeyCode.W)
				{
					// not return
					// delete saved config and set default config
				}
				else
				{
					return;
				}
			}

			//load failed, delete corrupted save
			//Debug.LogWarning("Load from slot " + loadSlot + " failed, deleting saved data");
			DeleteConfigKeys(loadSlot);
		}

		//try defaults, otherwise set empty
		if (HasDefaultConfig(gpadName))
			SetDefaultConfig(gpadName);
		else
			ClearConfig();
	}

	public void SaveConfig(string gpadName)
	{
		int saveSlot = FindSaveSlot(gpadName);
		saveInputs(saveSlot, gpadName);
	}

	public void SetDefaultConfig(string gpadName)
	{
		SetDefaults(m_DefaulMappings.GetConfig(gpadName));
	}

	public void DeleteConfig(string gpadName)
	{
		int slot = StoredConfigIndex(gpadName);
		//pokud neni config s timto jmenem ulozeny, uloz do nasledujiciho slotu od posledniho
		if (slot > -1)
		{
			DeleteConfigKeys(slot);
		}
	}

//---------------------------------------------------------------------------------------	
//  private	
//---------------------------------------------------------------------------------------	

	//TODO: prepsat na savovani do souboru misto do player prefs
	static string sKeyGpadName = "GamepadName";
	static string sKeyGpadKeys = "GamepadKeys";
	static string sKeyGpadAxis = "GamepadAxis";
	static string sKeyGpadKeyLength = "GamepadLength";
	static string sKeyLastSaveSlot = "ConfigLastSaveSlot";

	//must be called every frame
	void OnUpdate()
	{
		int numAxis = (int)E_JoystickAxis.COUNT;
		for (int i = 0; i < numAxis; i++)
		{
			float axisVal = Input.GetAxis(GamepadAxis.GetAxis((E_JoystickAxis)i));
			if (m_WasAxisPressed[i] == false && axisVal > 0.65f)
			{
				m_WasAxisPressed[i] = true;
				m_AxisDown[i] = true;
			}
			else if (m_WasAxisPressed[i] == true && axisVal < 0.25f)
			{
				m_WasAxisPressed[i] = false;
				m_AxisUp[i] = true;
			}
			else
			{
				m_AxisDown[i] = false;
				m_AxisUp[i] = false;
			}
		}
	}

	//TODO: doresit pridavani novych akci
	void Init()
	{
		m_InputK.Clear();
		for (int i = 0; i < (int)PlayerControlsGamepad.E_Input.COUNT; i++)
			m_InputK.Add(new JoyInput(KeyCode.None, E_JoystickAxis.NONE));
	}

	int GetActionCount()
	{
		return (int)PlayerControlsGamepad.E_Input.COUNT;
	}

//----------------------------------------------------------
//  Saving / loading	
//----------------------------------------------------------	
	bool HasStoredConfig(string gpadName)
	{
		return (StoredConfigIndex(gpadName) >= 0);
	}

	bool HasDefaultConfig(string gpadName)
	{
		return m_DefaulMappings.HasConfig(gpadName);
	}

	void SetDefaults(JoyInput[] defaults)
	{
		string KeyCodes_TempString = "";
		string Joystick_TempString = "";

		if (defaults.Length != GetActionCount())
		{
			Debug.LogError("Defaults length (" + defaults + ") do not match count of actions  (" + GetActionCount() + ")");
		}

		// go through all keycodes
		for (int sn = 0; sn < GetActionCount(); sn++)
		{
			// add every key to our temp. keycode string,also add "*" seperators
			KeyCodes_TempString += (int)defaults[sn].key + "*";
			// add joystick data to our temp. Joystick string,also add "*" seperators
			Joystick_TempString += (int)defaults[sn].joyAxis + "*";
		}

		Load(KeyCodes_TempString, Joystick_TempString);
	}

	bool loadConfig(int Slot)
	{
		// *** load input configuration ***
		// ********************************
		// load the input from the playerprefs
		int savedLength = PlayerPrefs.GetInt(sKeyGpadKeyLength + Slot);
		if (savedLength != GetActionCount())
		{
			Debug.LogWarning("Load failed - savedLength (" + savedLength + ") do not match DescriptionString.Length (" + GetActionCount() + ")");
			return false;
		}

		if (!PlayerPrefs.HasKey(sKeyGpadKeys + Slot) || !PlayerPrefs.HasKey(sKeyGpadAxis + Slot))
		{
			Debug.LogError("Load failed - keys not saved");
			return false;
		}

		string KeyCodes_loadstring = PlayerPrefs.GetString(sKeyGpadKeys + Slot);
		string Joystick_loadstring = PlayerPrefs.GetString(sKeyGpadAxis + Slot);

		return Load(KeyCodes_loadstring, Joystick_loadstring);

		/*Debug.Log("Loading config slot: " + Slot + ", gpad: " + PlayerPrefs.GetString(sKeyGpadName+Slot, ""));
        for (int sn = 0; sn < DescriptionString.Length; sn++)
			Debug.Log(" " + sDescription[sn] + ": " + inputKey[sn] + ", " + joystickString[sn]);
		Debug.Log("ConfigLastSaveSlot" + PlayerPrefs.GetInt(sKeyLastSaveSlot, -1)); /**/
	}

	void saveInputs(int Slot, string gamepadName = null)
	{
		// *** save input configuration ***
		// ********************************
		// temporary string to hold the KeyCodes
		string KeyCodes_TempString = "";
		string Joystick_TempString = "";

		// go through all keycodes
		for (int sn = 0; sn < GetActionCount(); sn++)
		{
			// add every key to our temp. keycode string,also add "*" seperators
			PlayerControlsGamepad.E_Input inp = (PlayerControlsGamepad.E_Input)(sn);
			JoyInput btn = GamepadInputManager.Instance.GetActionButton(inp);
			KeyCodes_TempString += (int)btn.key + "*";
			// add joystick data to our temp. Joystick string,also add "*" seperators
			Joystick_TempString += (int)btn.joyAxis + "*";
		}

		PlayerPrefs.SetInt(sKeyLastSaveSlot, Slot);
		Save(Slot,
			 gamepadName == null ? Game.CurrentJoystickName() : gamepadName,
			 KeyCodes_TempString,
			 Joystick_TempString,
			 GetActionCount());
		// ********************************
	}

	int FindSaveSlot(string gpadName)
	{
		int slot = StoredConfigIndex(gpadName);
		//pokud neni config s timto jmenem ulozeny, uloz do nasledujiciho slotu od posledniho
		if (slot < 0)
		{
			slot = PlayerPrefs.GetInt(sKeyLastSaveSlot, -1);
			slot++;
			if (slot > MaxSaveSlots)
				slot = 0;
		}
		return slot;
	}

	void Save(int Slot, string gpadName, string KeyCodes, string JoysticInput, int KeyLength)
	{
		//uloz jmeno gamepadu
		PlayerPrefs.SetString(sKeyGpadName + Slot, gpadName);

		// save the strings to the PlayerPrefs
		PlayerPrefs.SetString(sKeyGpadKeys + Slot, KeyCodes);
		PlayerPrefs.SetString(sKeyGpadAxis + Slot, JoysticInput);

		// save the length to the PlayerPrefs
		PlayerPrefs.SetInt(sKeyGpadKeyLength + Slot, KeyLength);
	}

	bool Load(string KeyCodes_loadstring, string Joystick_loadstring)
	{
		if (KeyCodes_loadstring == "" || Joystick_loadstring == "")
		{
			Debug.LogError("Load failed - empty strings");
			return false;
		}

		// split them up and put them in an array
		string[] KeyCode_prefs = KeyCodes_loadstring.Split('*');
		string[] JoyAxis_prefs = Joystick_loadstring.Split('*');

		int len = GetActionCount();
		if (KeyCode_prefs.Length < len || JoyAxis_prefs.Length < len)
		{
			//Debug.Log(KeyCodes_loadstring + " " + Joystick_loadstring + " " + Names_loadstring);
			Debug.LogError("Load failed - lengt of parset strings do not match: " + len + ", " + KeyCode_prefs.Length + ", " + JoyAxis_prefs.Length);
			return false;
		}

		for (int sn = 0; sn < len; sn++)
		{
			// convert the strings -> ints -> KeyCodes array
			int KeyCode_prefs_temp;
			int.TryParse(KeyCode_prefs[sn], out KeyCode_prefs_temp);
			int joyAxis_temp;
			int.TryParse(JoyAxis_prefs[sn], out joyAxis_temp);

			JoyInput btn = new JoyInput((KeyCode)KeyCode_prefs_temp, (E_JoystickAxis)joyAxis_temp);
			PlayerControlsGamepad.E_Input inp = (PlayerControlsGamepad.E_Input)(sn);
			GamepadInputManager.Instance.SetActionButton(inp, btn);
		}

		return true;
	}

	void DeleteConfigKeys(int Slot)
	{
		PlayerPrefs.DeleteKey(sKeyGpadName + Slot);

		PlayerPrefs.DeleteKey(sKeyGpadKeys + Slot);
		PlayerPrefs.DeleteKey(sKeyGpadAxis + Slot);

		PlayerPrefs.DeleteKey(sKeyGpadKeyLength + Slot);
	}

	int StoredConfigIndex(string gpadName)
	{
		for (int slot = 0; slot < MaxSaveSlots; slot++)
		{
			if (PlayerPrefs.HasKey(sKeyGpadName + slot))
			{
				string storedName = PlayerPrefs.GetString(sKeyGpadName + slot, "");
				if (storedName == gpadName)
					return slot;
			}
		}
		return -1;
	}
}
