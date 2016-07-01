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

public class UserSettings
{
	// PRIVATE MEMBERS

	DictionaryFile m_File;
	bool m_IsDirty;

	// PUBLIC MEMBERS

	public string PrimaryKey { get; private set; }

	public string Filename
	{
		get { return m_File != null ? m_File.Filename : string.Empty; }
	}

	public string Filepath
	{
		get { return m_File != null ? m_File.Filepath : string.Empty; }
	}

	public static UserSettings Empty
	{
		get { return new UserSettings() {PrimaryKey = string.Empty}; }
	}

	public delegate void SettingsEventHandler(UserSettings settings);
	public static event SettingsEventHandler SettingsLoaded;
	public static event SettingsEventHandler SettingsSaving;

	// C-TOR

	UserSettings()
	{
	}

	// PUBLIC METHODS

	public static UserSettings Load(string primaryKey)
	{
		if (string.IsNullOrEmpty(primaryKey) == true)
			return new UserSettings();

		var filename = string.Format("users/{0}/.settings", GuiBaseUtils.GetCleanName(primaryKey));
		var settings = new UserSettings()
		{
			PrimaryKey = primaryKey,
			m_File = new DictionaryFile(filename)
		};
#if UNITY_EDITOR
		try
		{
#endif
			settings.m_File.Load();
#if UNITY_EDITOR
		}
		catch
		{
		}
#endif

		settings.OnSettingsLoaded();

		return settings;
	}

	public void Save()
	{
		if (m_File == null)
			return;

		OnSettingsSaving();

		if (m_IsDirty == true)
		{
			m_File.Save();
			m_IsDirty = false;
		}
	}

	public void SetInt(string key, int val)
	{
		if (m_File != null)
		{
			m_File.SetInt(key, val);
			m_IsDirty = true;
		}
	}

	public void SetString(string key, string val)
	{
		if (m_File != null)
		{
			m_File.SetString(key, val);
			m_IsDirty = true;
		}
	}

	public void SetFloat(string key, float val)
	{
		if (m_File != null)
		{
			m_File.SetFloat(key, val);
			m_IsDirty = true;
		}
	}

	public void SetBool(string key, bool val)
	{
		if (m_File != null)
		{
			m_File.SetBool(key, val);
			m_IsDirty = true;
		}
	}

	public int GetInt(string key, int defVal)
	{
		return m_File != null ? m_File.GetInt(key, defVal) : defVal;
	}

	public string GetString(string key, string defVal)
	{
		return m_File != null ? m_File.GetString(key, defVal) : defVal;
	}

	public float GetFloat(string key, float defVal)
	{
		return m_File != null ? m_File.GetFloat(key, defVal) : defVal;
	}

	public bool GetBool(string key, bool defVal)
	{
		return m_File != null ? m_File.GetBool(key, defVal) : defVal;
	}

	public bool HasKey(string key)
	{
		return m_File != null ? m_File.HasKey(key) : false;
	}

	// PRIVATE METHODS

	void OnSettingsLoaded()
	{
		// make a temporary copy of the event to avoid possibility of 
		// a race condition if the last subscriber unsubscribes 
		// immediately after the null check and before the event is raised
		SettingsEventHandler handler = null;
		lock (this)
		{
			handler = SettingsLoaded;
		}

		// raise event
		if (handler != null)
		{
			handler(this);
		}
	}

	void OnSettingsSaving()
	{
		// make a temporary copy of the event to avoid possibility of 
		// a race condition if the last subscriber unsubscribes 
		// immediately after the null check and before the event is raised
		SettingsEventHandler handler = null;
		lock (this)
		{
			handler = SettingsSaving;
		}

		// raise event
		if (handler != null)
		{
			handler(this);
		}
	}
}
