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
using System.IO;

[ExecuteInEditMode]
public class TextDatabase : ScriptableObject
{
	public struct GameText
	{
		public int m_Index;
		public string m_Text;
	};

	// =======================================================================
	Dictionary<int, GameText> _DataBase = new Dictionary<int, GameText>();
	static SystemLanguage _DatabaseLanguage = SystemLanguage.Unknown;
	const SystemLanguage _DefaultLanguage = SystemLanguage.English;
	int _ReloadCount = 0;

	static TextDatabase s_Instance;

	// =======================================================================
	public static TextDatabase instance
	{
		get { return GetInstance(); }
	}

	public int reloadCount
	{
		get { return _ReloadCount; }
	} // TODO :: This is hack for editor. we need better solution....

	public SystemLanguage databaseLangugae
	{
		get { return _DatabaseLanguage; }
	}

	// =======================================================================
	public static SystemLanguage GetLanguage()
	{
		return _DatabaseLanguage != SystemLanguage.Unknown ? _DatabaseLanguage : _DefaultLanguage;
	}

	// =======================================================================
	public static void SetLanguage(SystemLanguage inLanguage)
	{
		if (s_Instance != null)
		{
			s_Instance.Reload(inLanguage);
		}
		else
		{
			_DatabaseLanguage = inLanguage;
		}
	}

	/// Public part ...
	public bool Reload()
	{
		//SystemLanguage language = _DatabaseLangugae != SystemLanguage.Unknown ? _DatabaseLangugae : Application.systemLanguage;
		SystemLanguage language = GetLanguage();
		if (Reload(language) == false)
		{
			// load default language if selected language can't be found...
			return Reload(SystemLanguage.Unknown);
		}

		return true;
	}

	public bool Reload(SystemLanguage inLanguage)
	{
		string defaultLangPostfix;
		if (GetLanguageFilePostfix(_DefaultLanguage, out defaultLangPostfix) == false)
		{
			Debug.LogError("Can't obtain file extension for default language... : " + _DefaultLanguage);
			return false;
		}

		string languagePostfix;
		if (GetLanguageFilePostfix(inLanguage, out languagePostfix) == false)
		{
			inLanguage = _DefaultLanguage;
			languagePostfix = defaultLangPostfix;
		}

		// create new database (this is only for save, if something wrong happen we still can use old one)...
		Dictionary<int, GameText> newDataBase = new Dictionary<int, GameText>();

		List<string> textFiles = new List<string>();
		textFiles.Add("Texts/Texts.");
#if MADFINGER_KEYBOARD_MOUSE
		textFiles.Add("Texts/Texts_pc.");
#else
		if (GamepadInputManager.Instance != null && GamepadInputManager.Instance.IsNvidiaShield())
			textFiles.Add("Texts/Texts_shield.");
		else
			textFiles.Add("Texts/Texts_touch.");
#endif

		foreach (string textFile in textFiles)
		{
			// first try process localize texts.
			if (inLanguage != _DefaultLanguage)
			{
				// try load localized texts if any exist...
				string fileName = textFile + languagePostfix; // + ".txt";
				if (LoadTextFile(fileName, newDataBase) == false)
				{
					Debug.LogError("Can't process text file : " + fileName + ".\n   So now we try to process default language text file.");
				}
				else
				{
					// we load that file so we can continue on other...
					continue;
				}
			}

			// if localized text doesn't exist we can load default one.
			{
				// Construct file name and read file ...
				string fileName = textFile + defaultLangPostfix; // + ".txt";
				if (LoadTextFile(fileName, newDataBase) == false)
				{
					// this is alreade a error because default texts MUST EXIST...
					Debug.LogError("Can't process text file : " + fileName);
					return false;
				}
			}
		}

		// replace old database with new one...
		_DataBase = newDataBase;
		_DatabaseLanguage = inLanguage;
		_ReloadCount++;

		return true;
	}

	public string this[int i]
	{
		get
		{
			if (_DataBase.ContainsKey(i) == true)
				return _DataBase[i].m_Text;
			Debug.LogWarning("TextDatabase Text with ID " + i + " is not in database");
			return "<UNKNOWN TEXT>";
		}
	}

#if UNITY_EDITOR
	public static bool Contains(int i)
	{
		return instance._DataBase.ContainsKey(i);
	}
#endif

	public static Dictionary<int, GameText> GetDatabase_ForInspector()
	{
		return TextDatabase.instance._DataBase;
	}

	// =======================================================================
	/// Private part ...
	static TextDatabase GetInstance()
	{
		if (s_Instance == null)
		{
			s_Instance = ScriptableObject.CreateInstance<TextDatabase>();
			if (s_Instance == null)
			{
				Debug.LogError("Can't create TextDatabase");
				return null;
			}

			ScriptableObject.DontDestroyOnLoad(s_Instance);

			// TODO reload default language. This nead to be changed...
			s_Instance.Reload();
		}

		return s_Instance;
	}

	static bool GetLanguageFilePostfix(SystemLanguage inLanguage, out string outlanguagePostfix)
	{
		switch (inLanguage)
		{
		case SystemLanguage.English:
			outlanguagePostfix = "eng";
			return true;
		case SystemLanguage.German:
			outlanguagePostfix = "ger";
			return true;
		case SystemLanguage.French:
			outlanguagePostfix = "fre";
			return true;
		case SystemLanguage.Italian:
			outlanguagePostfix = "ita";
			return true;
		case SystemLanguage.Spanish:
			outlanguagePostfix = "spa";
			return true;
		case SystemLanguage.Russian:
			outlanguagePostfix = "rus";
			return true;
		case SystemLanguage.Japanese:
			outlanguagePostfix = "jpn";
			return true;
		case SystemLanguage.Chinese:
			outlanguagePostfix = "chi";
			return true;
		case SystemLanguage.Korean:
			outlanguagePostfix = "kor";
			return true;
		default:
			outlanguagePostfix = "eng";
			return false;
		}
	}

	bool LoadTextFile(string inFileName, Dictionary<int, GameText> inoutNewDictionary)
	{
		//FileInfo theSourceFile = null;
		TextAsset textFile = (TextAsset)Resources.Load(inFileName, typeof (TextAsset));
		if (textFile == null)
		{
			Debug.Log(inFileName + " -- was not found");
			return false;
		}

		// puzdata.text is a string containing the whole file. To read it line-by-line:
		StringReader reader = new StringReader(textFile.text);
		if (reader == null)
		{
			Debug.Log("puzzles.txt not found or not readable");
			return false;
		}
		else
		{
			// Read each line from the file
			string line;
			int lineIdx = 0;
			int textID;
			string text;
			while ((line = reader.ReadLine()) != null)
			{
				lineIdx++;
				if (false == ProcessLine(line, out textID, out text))
				{
					Debug.LogError("Parse error in file " + inFileName + " on line [" + lineIdx + "] line is: " + line);
				}

				// this is coorect situation. fx. comments are valid lines, but they are not added to database...
				else if (textID < 0)
				{
					continue;
				}

				else
				{
					if (inoutNewDictionary.ContainsKey(textID) == true)
					{
						Debug.LogError("Text with ID [" + textID + "] already exist in text database. Content \"" + inoutNewDictionary[textID].m_Text +
									   "\" will be replaced by \"" + text + "\"");
					}

					inoutNewDictionary[textID] = new GameText() {m_Index = textID, m_Text = text};
				}
			}

//            foreach(var gtext in _DataBase) {
//                Debug.Log("GameText :: " + gtext.Key + " -- " + gtext.Value.m_Text );
//            }
		}

		return true;
	}

	bool ProcessLine(string inLine, out int outTextID, out string outText)
	{
		outTextID = -1;
		outText = "";
		// remove coments from line...
		int commentChar = inLine.IndexOf('#');
		if (commentChar == 0)
			return true;
		else if (commentChar > 0)
			inLine = inLine.Remove(commentChar);

		// trim spacese from start and end...
		inLine = inLine.Trim();
		if (inLine.Length == 0)
			return true;

		// get first whitespace...
		char[] charSeparators = new char[] {' ', '\t', '\n', '\r'};
		int space = inLine.IndexOfAny(charSeparators);

		if (space <= 0)
		{
			if (int.TryParse(inLine, out outTextID) == false)
				return false;
		}
		else
		{
			//split line <number> whitespace <text> \n
			string number = inLine.Substring(0, space).Trim();
			if (int.TryParse(number, out outTextID) == false)
				return false;

			outText = inLine.Substring(space).Trim();
			outText = outText.Replace("\\n", "\n");
		}

		outText = RemoveSpacesAroundNewLine(outText);

		//Debug.Log("LINE :: " + textID + " -- " + text);
		return true;
	}

	string RemoveSpacesAroundNewLine(string inText)
	{
		// AX I know that this is really stupid and not very effective alghoritm
		// how to remove spaceses aroun new line... but working :-))
		// so please don't hit me, but change it if it will be needed...

		string outText = inText;
		int oldSize, newSize;
		do
		{
			oldSize = outText.Length;
			outText = outText.Replace(" \n", "\n");
			outText = outText.Replace("\n ", "\n");
			newSize = outText.Length;
		} while (newSize < oldSize);

		return outText;
	}
}
