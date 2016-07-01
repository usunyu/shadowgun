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
using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;

public class DictionaryFile
{
#if UNITY_EDITOR
	readonly static string FILEPATH_PATTERN = "{0}/.editor/{1}";
#else
	static readonly string FILEPATH_PATTERN = "{0}/{1}";
#endif

	// PRIVATE MEMBERS

	Dictionary<string, object> m_Dictionary = new Dictionary<string, object>();
	string m_Filename;

	// PUBLIC MEMBERS

	public string Filename
	{
		get { return m_Filename ?? "default"; }
		set { m_Filename = value; }
	}

	public string Filepath
	{
		get { return string.Format(FILEPATH_PATTERN, Application.persistentDataPath, m_Filename); }
	}

	// C-TOR

	public DictionaryFile(string filename)
	{
		m_Filename = filename;
	}

	// PUBLIC METHODS

	public void SetInt(string key, int val)
	{
		Add(key, val);
	}

	public void SetString(string key, string val)
	{
		Add(key, val);
	}

	public void SetFloat(string key, float val)
	{
		Add(key, val);
	}

	public void SetBool(string key, bool val)
	{
		SetInt(key, val ? 1 : 0);
	}

	public int GetInt(string key, int defVal)
	{
		if (m_Dictionary.ContainsKey(key))
			return (int)m_Dictionary[key];
		else
			return defVal;
	}

	public string GetString(string key, string defVal)
	{
		if (m_Dictionary.ContainsKey(key))
			return (string)m_Dictionary[key];
		else
			return defVal;
	}

	public float GetFloat(string key, float defVal)
	{
		if (m_Dictionary.ContainsKey(key))
			return (float)m_Dictionary[key];
		else
			return defVal;
	}

	public bool GetBool(string key, bool defVal)
	{
		return GetInt(key, defVal ? 1 : 0) != 0 ? true : false;
	}

	public bool HasKey(string key)
	{
		return m_Dictionary.ContainsKey(key);
	}

	public void Save()
	{
		string path = Filepath;
		string dir = Path.GetDirectoryName(path);
		if (string.IsNullOrEmpty(dir) == true)
			return;

		if (Directory.Exists(dir) == false)
		{
			Directory.CreateDirectory(dir);
		}

		//Debug.Log("Saving " + filepath);
		ArrayList stringVals = new ArrayList();
		ArrayList intVals = new ArrayList();
		ArrayList floatVals = new ArrayList();

		//collect ints, floats, and strings
		foreach (var pair in m_Dictionary)
		{
			if (pair.Value is string)
			{
				KeyValuePair<string, string> p = new KeyValuePair<string, string>(pair.Key, pair.Value as string);
				stringVals.Add(p);
			}
			else if (pair.Value is int)
			{
				KeyValuePair<string, int> p = new KeyValuePair<string, int>(pair.Key, (int)pair.Value);
				intVals.Add(p);
			}
			else if (pair.Value is float)
			{
				KeyValuePair<string, float> p = new KeyValuePair<string, float>(pair.Key, (float)pair.Value);
				floatVals.Add(p);
			}
		}

		try
		{
			string tempPath = path + ".temp";

#if UNITY_EDITOR
			using (Stream fs = File.OpenWrite(tempPath))
			using (BinaryWriter writer = new BinaryWriter(fs))
#else
			using (Stream fs = File.OpenWrite(tempPath))
			using (Stream cs = new CryptoStream(fs, GetCryptic().CreateEncryptor(), CryptoStreamMode.Write))
			using (BinaryWriter writer = new BinaryWriter(cs))
#endif
			{
				//write strings
				writer.Write(stringVals.Count);
				foreach (KeyValuePair<string, string> pair in stringVals)
				{
					writer.Write(pair.Key);
					writer.Write(pair.Value);
				}
				//write ints
				writer.Write(intVals.Count);
				foreach (KeyValuePair<string, int> pair in intVals)
				{
					writer.Write(pair.Key);
					writer.Write(pair.Value);
				}
				//write floats
				writer.Write(floatVals.Count);
				foreach (KeyValuePair<string, float> pair in floatVals)
				{
					writer.Write(pair.Key);
					writer.Write(pair.Value);
				}
			}

			if (File.Exists(tempPath) == true)
			{
				if (File.Exists(path) == true)
				{
					File.Delete(path);
				}
				File.Move(tempPath, path);
			}
		}
		catch
		{
			Debug.Log("DictionaryFile.Save() :: Unable to write to '" + Filename + "'!");
		}
	}

	public void Load()
	{
		string path = Filepath;
		if (File.Exists(path) == false)
			return;

		//Debug.Log("Loading " + filepath);

		m_Dictionary = new Dictionary<string, object>();

		try
		{
#if UNITY_EDITOR
			using (Stream fs = File.OpenRead(path))
			using (BinaryReader reader = new BinaryReader(fs))
#else
			using (Stream fs = File.OpenRead(path))
			using (Stream cs = new CryptoStream(fs, GetCryptic().CreateDecryptor(), CryptoStreamMode.Read))
			using (BinaryReader reader = new BinaryReader(cs))
#endif
			{
				// read string pairs
				{
					int count = reader.ReadInt32();
					for (int i = 0; i < count; i++)
					{
						string key = reader.ReadString();
						string value = reader.ReadString();
						m_Dictionary[key] = value;
					}
				}

				// read int pairs 
				{
					int count = reader.ReadInt32();
					for (int i = 0; i < count; i++)
					{
						string key = reader.ReadString();
						int value = reader.ReadInt32();
						m_Dictionary[key] = value;
					}
				}

				// read float pairs 
				{
					int count = reader.ReadInt32();
					for (int i = 0; i < count; i++)
					{
						string key = reader.ReadString();
						float value = reader.ReadSingle();
						m_Dictionary[key] = value;
					}
				}
			}
		}
		catch
		{
			Debug.Log("DictionaryFile.Load() :: File '" + Filename + "' is corrupted!");
		}
	}

	// PRIVATE METHODS

	void Add(string key, object val)
	{
		m_Dictionary[key] = val;
	}

	DESCryptoServiceProvider GetCryptic()
	{
		DESCryptoServiceProvider cryptic = new DESCryptoServiceProvider();
		cryptic.Key = ASCIIEncoding.ASCII.GetBytes("6uTafewa");
		cryptic.IV = ASCIIEncoding.ASCII.GetBytes("5PebrupR");
		return cryptic;
	}
};
