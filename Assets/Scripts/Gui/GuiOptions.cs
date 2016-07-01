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
using LitJson;

public static class GuiOptions
{
	public enum E_ControlScheme
	{
		FloatingMovePad, //floating joystick for move
		FixedMovePad, //fixed joystick for move
	};

	public enum E_Language
	{
		English,
		Spanish,
		French,
		Germany,
		Italian,
		Russian,
		Chinese,
		Korean,
		Japan,
	};

	public static SystemLanguage[] convertLanguageToSysLanguage = new SystemLanguage[]
	{
		SystemLanguage.English,
		SystemLanguage.Spanish,
		SystemLanguage.French,
		SystemLanguage.German,
		SystemLanguage.Italian,
		SystemLanguage.Russian,
		SystemLanguage.Chinese,
		SystemLanguage.Korean,
		SystemLanguage.Japanese,
	};

	public static string[] convertLanguageToFullName = new string[]
	{
		"English",
		"Spanish",
		"French",
		"German",
		"Italian",
		"Russian",
		"Chinese",
		"Korean",
		"Japanese",
	};

	static string m_PrimaryKey; // we need this to use back-compatible approach

	public static float sensitivity = 1.0f;
	public static float soundVolume = 0.5f;
	public static float musicVolume = 0.7f;
	public static bool subtitles = true;
	public static bool invertYAxis = false;
	public static bool floatingFireButton = false;
	public static float fireButtonScale = 1.0f;
	public static bool leftHandAiming = false;
	static E_ControlScheme DefaultScheme = E_ControlScheme.FloatingMovePad;
	public static E_ControlScheme m_ControlScheme = DefaultScheme;
	//static public float ListenerVolume = 1.0f; //zatim na maximum, ale mohli bychom pridat do audio menu
	public static int graphicDetail = 0;
	public static bool musicOn = true;
	public static bool showHints = true;
	public static Resolution fullScreenResolution;
	public static E_Language _language = E_Language.English;

	public static E_Language language
	{
		get { return _language; }
		set
		{
			if (!System.Enum.IsDefined(typeof (E_Language), value))
			{
				throw new System.Exception("Unsupported language: " + value);
			}
			else
			{
				_language = value;
				OnLanguageChange();
			}
		}
	}

	public static bool showMogaHelp = true;

	//default values
	static float DefaultSensitivity
	{
		get
		{
			if (Game.IsHDResolution() && (Application.platform != RuntimePlatform.WindowsEditor))
				return 2.5f;
			else
				return 1.5f;
		}
	}

	static float DefaultSoundVolume = 0.5f;
	static float DefaultMusicVolume = 0.7f;
	static bool DefaultSubtitles = true;
	static bool DefaultInvertYAxis = false;
	public static bool DefaultFloatingFireButton = false;
	static bool DefaultLeftHandAiming = false;
	static bool DefaultMusicOn = true;
	static E_Language DefaultLanguage = E_Language.English;

	//pomocne flagy
	public static bool leftHandControlsNeedUpdate = false;
	public static bool customControlsInitialised = false;

	static GuiOptions()
	{
		if (PlayerPrefs.HasKey("GraphicDetail") == true)
		{
			graphicDetail = PlayerPrefs.GetInt("GraphicDetail", (int)DeviceInfo.Performance.Medium);
		}
		if (PlayerPrefs.HasKey("Language") == true)
		{
			_language = (E_Language)PlayerPrefs.GetInt("Language", (int)DefaultLanguage);
			if (!System.Enum.IsDefined(typeof (E_Language), _language))
			{
				Debug.LogWarning("Invalid language loaded from options!");
				_language = DefaultLanguage;
			}
		}
		if (PlayerPrefs.HasKey("SoundVolume") == true)
		{
			soundVolume = PlayerPrefs.GetFloat("SoundVolume", DefaultSoundVolume);
		}
		if (PlayerPrefs.HasKey("MusicVolume") == true)
		{
			musicVolume = PlayerPrefs.GetFloat("MusicVolume", DefaultMusicVolume);
		}
		if (PlayerPrefs.HasKey("MusicOn") == true)
		{
			musicOn = PlayerPrefs.GetInt("MusicOn", DefaultMusicOn ? 1 : 0) == 1 ? true : false;
		}
	}

	public static void SetNewLeftHandAiming(bool newVal)
	{
		if (leftHandAiming == newVal)
			return;

		leftHandAiming = newVal;
		SwitchLeftHandAimingControls();
	}

	public enum E_ControlSide
	{
		Neutral,
		LeftHand,
		RightHand,
	};

	public class ControlPos
	{
		public ControlPos(E_ControlSide _side)
		{
			Side = _side;
		}

		public Vector2 OrigPos;
		public Vector2 Offset;

		public Vector2 Positon
		{
			get { return OrigPos + Offset; }
		}

		public E_ControlSide Side;
	};

	public static ControlPos FireUseButton = new ControlPos(E_ControlSide.RightHand);
	public static ControlPos ReloadButton = new ControlPos(E_ControlSide.RightHand);
	public static ControlPos RollButton = new ControlPos(E_ControlSide.RightHand);
	public static ControlPos SprintButton = new ControlPos(E_ControlSide.RightHand);
	public static ControlPos WeaponButton = new ControlPos(E_ControlSide.Neutral); //weapon automaticky nepresouvame, protoze je naproti radar
	//static public ControlPos 	PauseButton 		= new ControlPos(E_ControlSide.LeftHand);
	public static ControlPos MoveStick = new ControlPos(E_ControlSide.LeftHand);
	public static ControlPos[] GadgetButtons = new ControlPos[3]
	{new ControlPos(E_ControlSide.RightHand), new ControlPos(E_ControlSide.RightHand), new ControlPos(E_ControlSide.RightHand)};

	public static int GetDefaultGraphics()
	{
		return (int)DeviceInfo.GetDetectedPerformanceLevel();
	}

	public static void ResetToDefaults()
	{
		Load(null);

		SetNewLeftHandAiming(DefaultLeftHandAiming);
	}

	//tohle by se melo volat kdyz se zmeni lefthand option, ale soucasne to lze volat az pote co se nacte gui (takze ne z hlevniho menu)
	//take by se to nemelo aplikovat kdyz nekdo po zapnuti lefthand jeste pozici zmodifikuje (ale kdyz ji nejprve zmodifikuje a pak da lefthand tak ano).
	public static void SwitchLeftHandAimingControls()
	{
		//Debug.Log("SwitchLeftHandAimingControls, controls initialised: " + customControlsInitialised );
		if (!customControlsInitialised)
		{
			leftHandControlsNeedUpdate = true;
			return;
		}

		//setup list of all controls
		List<ControlPos> m_Controls = new List<ControlPos>();
		m_Controls.Add(FireUseButton);
		//m_Controls.Add(PauseButton);
		m_Controls.Add(WeaponButton);
		m_Controls.Add(MoveStick);
		m_Controls.Add(ReloadButton);
		m_Controls.Add(RollButton);
		m_Controls.Add(SprintButton);
		for (int i = 0; i < GadgetButtons.Length; i++)
		{
			m_Controls.Add(GadgetButtons[i]);
		}

		//adjust all that are affected by lefthandswitch
		foreach (ControlPos cp in m_Controls)
		{
			if (cp.Side == GuiOptions.E_ControlSide.Neutral)
				continue;

			bool doMirror = false;

			//check if control needs to mirror (keep it on place when its already adjusted to correct side)
			if (cp.Side == GuiOptions.E_ControlSide.LeftHand)
			{
				//control by mel byt defaultne vlevo, pri lefthandAiming vpravo. Pokud je na druhe strane, udelej mirror.
				doMirror = (!leftHandAiming && cp.Positon.x > Screen.width/2) || (leftHandAiming && cp.Positon.x < Screen.width/2);
			}
			else if (cp.Side == GuiOptions.E_ControlSide.RightHand)
			{
				//control by mel byt defaultne vpravo, pri lefthandAiming vlevo. Pokud je na druhe strane, udelej mirror.
				doMirror = (!leftHandAiming && cp.Positon.x < Screen.width/2) || (leftHandAiming && cp.Positon.x > Screen.width/2);
			}

			if (doMirror)
			{
				//mirror by y axis
				float mirrorPosX = Screen.width - cp.Positon.x;
				//compute offset, keep orig pos
				float mirrorOffset = mirrorPosX - cp.OrigPos.x;
				//store to guiOptions
				cp.Offset.x = mirrorOffset;
			}
		}

		if (GuiHUD.Instance && GuiHUD.Instance.IsInitialized)
			GuiHUD.Instance.UpdateControlsPosition();

		if (Player.LocalInstance != null && Player.LocalInstance.Controls != null)
		{
			Player.LocalInstance.Controls.ControlSchemeChanged(); //update positions of joysticks
		}

		leftHandControlsNeedUpdate = false;
	}

	public static void Save()
	{
		//Debug.Log("Saving options");

		Save(Game.Settings);
	}

	public static void Load()
	{
		//Debug.Log("Loading options");

		Load(Game.Settings);
	}

	// PRIVATE METHODS

	static void Save(UserSettings settings)
	{
		JsonData data = new JsonData();

		SetFloat(data, "Sensitivity", sensitivity);
		SetFloat(data, "SoundVolume", soundVolume);
		SetFloat(data, "MusicVolume", musicVolume);
		SetBool(data, "Subtitles", subtitles);
		SetBool(data, "InvertYAxis", invertYAxis);
		SetInt(data, "FullScreenWidth", fullScreenResolution.width);
		SetInt(data, "FullScreenHeight", fullScreenResolution.height);

		SetBool(data, "LeftHandAiming", leftHandAiming);
		SetInt(data, "ControlScheme", (int)m_ControlScheme);

		//controls
		SetFloat(data, "FireButtonX", FireUseButton.Offset.x);
		SetFloat(data, "FireButtonY", FireUseButton.Offset.y);

		//SetFloat(data, "PauseButtonX", PauseButton.Offset.x);
		//SetFloat(data, "PauseButtonY", PauseButton.Offset.y);

		SetFloat(data, "WeaponButtonX", WeaponButton.Offset.x);
		SetFloat(data, "WeaponButtonY", WeaponButton.Offset.y);

		for (int i = 0; i < GadgetButtons.Length; i++)
		{
			SetFloat(data, "GadgetButtonX" + i, GadgetButtons[i].Offset.x);
			SetFloat(data, "GadgetButtonY" + i, GadgetButtons[i].Offset.y);
		}

		SetFloat(data, "MoveStickX", MoveStick.Offset.x);
		SetFloat(data, "MoveStickY", MoveStick.Offset.y);

		SetFloat(data, "ReloadButtonX", ReloadButton.Offset.x);
		SetFloat(data, "ReloadButtonY", ReloadButton.Offset.y);

		SetFloat(data, "RollButtonX", RollButton.Offset.x);
		SetFloat(data, "RollButtonY", RollButton.Offset.y);

		SetFloat(data, "SprintButtonX", SprintButton.Offset.x);
		SetFloat(data, "SprintButtonY", SprintButton.Offset.y);

		//SetFloat(data, "AimButtonX", AimButton.Offset.x);
		//SetFloat(data, "AimButtonY", AimButton.Offset.y);

		SetBool(data, "FireButtonFloating", floatingFireButton);
		SetFloat(data, "FireButtonScale", fireButtonScale);

		SetBool(data, "MusicOn", musicOn);

		SetBool(data, "ShowHints", showHints);

		SetInt(data, "Language", (int)language);

		SetBool(data, "ShowMogaHelp", showMogaHelp);

		string json = data.ToJson();
		settings.SetString("options", json);

		// we need to store these values into player prefs
		// so it's global and we can read it on app start
		PlayerPrefs.SetInt("GraphicDetail", graphicDetail);
		PlayerPrefs.SetInt("Language", (int)language);
		PlayerPrefs.SetFloat("SoundVolume", soundVolume);
		PlayerPrefs.SetFloat("MusicVolume", musicVolume);
		PlayerPrefs.SetInt("MusicOn", musicOn ? 1 : 0);
		PlayerPrefs.Save();
	}

	static void Load(UserSettings settings)
	{
		m_PrimaryKey = CloudUser.instance.primaryKey; // we need this to use back-compatible approach

		string json = settings != null ? settings.GetString("options", null) : null;
		JsonData data = string.IsNullOrEmpty(json) == false ? JsonMapper.ToObject(json) : null;

		sensitivity = GetFloat(data, "Sensitivity", DefaultSensitivity);
		soundVolume = GetFloat(data, "SoundVolume", DefaultSoundVolume);
		musicVolume = GetFloat(data, "MusicVolume", DefaultMusicVolume);
		subtitles = GetBool(data, "Subtitles", DefaultSubtitles);
		invertYAxis = GetBool(data, "InvertYAxis", DefaultInvertYAxis);
		fullScreenResolution.width = GetInt(data, "FullScreenWidth", 0);
		fullScreenResolution.height = GetInt(data, "FullScreenHeight", 0);

		leftHandAiming = GetBool(data, "LeftHandAiming", DefaultLeftHandAiming);
		m_ControlScheme = (GuiOptions.E_ControlScheme)GetInt(data, "ControlScheme", (int)DefaultScheme);

		//rozmisteni controls
		FireUseButton.Offset.x = GetFloat(data, "FireButtonX", 0);
		FireUseButton.Offset.y = GetFloat(data, "FireButtonY", 0);

		//PauseButton.Offset.x = GetFloat(data, "PauseButtonX", 0);
		//PauseButton.Offset.y = GetFloat(data, "PauseButtonY", 0);

		WeaponButton.Offset.x = GetFloat(data, "WeaponButtonX", 0);
		WeaponButton.Offset.y = GetFloat(data, "WeaponButtonY", 0);

		for (int i = 0; i < GadgetButtons.Length; i++)
		{
			GadgetButtons[i].Offset.x = GetFloat(data, "GadgetButtonX" + i, 0);
			GadgetButtons[i].Offset.y = GetFloat(data, "GadgetButtonY" + i, 0);
		}

		MoveStick.Offset.x = GetFloat(data, "MoveStickX", 0);
		MoveStick.Offset.y = GetFloat(data, "MoveStickY", 0);

		ReloadButton.Offset.x = GetFloat(data, "ReloadButtonX", 0);
		ReloadButton.Offset.y = GetFloat(data, "ReloadButtonY", 0);

		RollButton.Offset.x = GetFloat(data, "RollButtonX", 0);
		RollButton.Offset.y = GetFloat(data, "RollButtonY", 0);

		SprintButton.Offset.x = GetFloat(data, "SprintButtonX", 0);
		SprintButton.Offset.y = GetFloat(data, "SprintButtonY", 0);

		floatingFireButton = GetBool(data, "FireButtonFloating", false);
		fireButtonScale = GetFloat(data, "FireButtonScale", 1.0f);

		// we need to store this value into player prefs
		// so it's global and we can read it on app start
		graphicDetail = PlayerPrefs.GetInt("GraphicDetail", GetDefaultGraphics());

		musicOn = GetBool(data, "MusicOn", DefaultMusicOn);

		showHints = GetBool(data, "ShowHints", true);

		showMogaHelp = GetBool(data, "ShowMogaHelp", true);
		// apply options ...

		if (MusicManager.Instance)
			MusicManager.Instance.ApplyOptionsChange();

		DeviceInfo.Initialize((DeviceInfo.Performance)graphicDetail);

		try
		{
			language = (E_Language)GetInt(data, "Language", (int)DefaultLanguage);
		}
		catch (System.Exception e)
		{
			Debug.LogException(e);
			language = DefaultLanguage;
		}

		AudioListener.volume = soundVolume;
	}

	static void OnLanguageChange()
	{
//		Debug.Log("Language is: " + language);
		TextDatabase.SetLanguage(convertLanguageToSysLanguage[(int)language]);
		MFGuiManager.OnLanguageChanged(convertLanguageToFullName[(int)language]);
	}

	static int GetInt(JsonData data, string key, int defVal)
	{
		// we just return default value when no data is specified
		// we use this to reset to defaults
		if (data == null)
			return defVal;
		if (data.IsObject == true && data.HasValue(key) == true)
			return (int)data[key];
		return PlayerPrefs.GetInt(ConstructKey(key), defVal);
	}

	static void SetInt(JsonData data, string key, int val)
	{
		data[key] = val;
	}

	static float GetFloat(JsonData data, string key, float defVal)
	{
		// we just return default value when no data is specified
		// we use this to reset to defaults
		if (data == null)
			return defVal;
		if (data.IsObject == true && data.HasValue(key) == true)
			return (float)(double)data[key];
		return PlayerPrefs.GetFloat(ConstructKey(key), defVal);
	}

	static void SetFloat(JsonData data, string key, float val)
	{
		data[key] = val;
	}

	static bool GetBool(JsonData data, string key, bool defVal)
	{
		// we just return default value when no data is specified
		// we use this to reset to defaults
		if (data == null)
			return defVal;
		if (data.IsObject == true && data.HasValue(key) == true)
			return (bool)data[key];
		return PlayerPrefs.GetInt(ConstructKey(key), defVal ? 1 : 0) != 0 ? true : false;
	}

	static void SetBool(JsonData data, string key, bool val)
	{
		data[key] = val;
	}

	static string ConstructKey(string key)
	{
		return string.Format("{0}.options.{1}", m_PrimaryKey ?? "default", key);
	}
}
