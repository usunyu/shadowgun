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

// TODO ::
//	- we have to change how individual fonts are referenced. 
//	  Now they all are referenced and loaded into memory, so we have to change
//	  this direct asset reference model into string and resource load reference model.

[AddComponentMenu("GUI/Hierarchy/Font Manager")]
public class MFFontManager : MonoBehaviour
{
	[System.Serializable]
	public class FontItem
	{
		public string m_FontAssetPath;
		public GUIBase_FontBase m_Font;

		public void Prepare(bool inLoad)
		{
			if (inLoad == false)
			{
				//DestroyObject(m_Font); /// ???
				m_Font = null;
			}
			else if (m_Font == null && string.IsNullOrEmpty(m_FontAssetPath) == false)
			{
				Debug.Log("Load asset from path: " + m_FontAssetPath);
				GameObject go = Resources.Load(m_FontAssetPath) as GameObject;

				if (go == null)
				{
					Debug.LogWarning("Filed to load font! path=" + m_FontAssetPath);
					return;
				}

				Debug.Log("GameObject go: " + go);
				m_Font = go.GetComponent<GUIBase_FontBase>();
			}
		}
	}

	[System.Serializable]
	public class FontSetup
	{
		public string m_FontName;
		public FontItem m_Default;

		// per language versions. If is null default will be used.
		public FontItem m_German;
		public FontItem m_French;
		public FontItem m_Italian;
		public FontItem m_Spanish;
		public FontItem m_Russian;
		public FontItem m_Japanese;
		public FontItem m_Chinese;
		public FontItem m_Korean;

		public GUIBase_FontBase GetFont(SystemLanguage inLanguage)
		{
			GUIBase_FontBase font = m_Default.m_Font;

			switch (inLanguage)
			{
			case SystemLanguage.English:
				break; // english is default...
			case SystemLanguage.German:
				font = m_German.m_Font;
				break;
			case SystemLanguage.French:
				font = m_French.m_Font;
				break;
			case SystemLanguage.Italian:
				font = m_Italian.m_Font;
				break;
			case SystemLanguage.Spanish:
				font = m_Spanish.m_Font;
				break;
			case SystemLanguage.Russian:
				font = m_Russian.m_Font;
				break;
			case SystemLanguage.Chinese:
				font = m_Chinese.m_Font;
				break;
			case SystemLanguage.Japanese:
				font = m_Japanese.m_Font;
				break;
			case SystemLanguage.Korean:
				font = m_Korean.m_Font;
				break;
			default:
				break;
			}

			if (font == null)
				font = m_Default.m_Font;

			return font;
		}

		public void Prepare(string inLanguage, bool inForce = false)
		{
			m_Default.Prepare(true); // default font have to be loaded always...
			m_German.Prepare(inForce == true || inLanguage == "German");
			m_French.Prepare(inForce == true || inLanguage == "French");
			m_Italian.Prepare(inForce == true || inLanguage == "Italian");
			m_Spanish.Prepare(inForce == true || inLanguage == "Spanish");
			m_Russian.Prepare(inForce == true || inLanguage == "Russian");
			m_Chinese.Prepare(inForce == true || inLanguage == "Chinese");
			m_Japanese.Prepare(inForce == true || inLanguage == "Japanese");
			m_Korean.Prepare(inForce == true || inLanguage == "Korean");
		}

		public void CleanFontReference()
		{
			//m_Default   .m_Font = null;
			m_German.m_Font = null;
			m_French.m_Font = null;
			m_Italian.m_Font = null;
			m_Spanish.m_Font = null;
			m_Russian.m_Font = null;
			m_Chinese.m_Font = null;
			m_Japanese.m_Font = null;
			m_Korean.m_Font = null;
		}
	};

	static MFFontManager Instance;
	static string DefaultFontName_Static = "Default";

	[SerializeField] string m_DefaultFontName = DefaultFontName_Static;
	[SerializeField] GUIBase_FontBase m_DefaultFont;
	[SerializeField] List<FontSetup> m_Fonts = new List<FontSetup>();

	public static string defaultFontName
	{
		get { return (Instance != null) ? Instance.m_DefaultFontName : DefaultFontName_Static; }
	}

	// =========================================================================================================================
	// === MonoBehaviour functions =============================================================================================
	void Awake()
	{
		if (Application.isPlaying)
		{
			if (Instance)
			{
				Destroy(this.gameObject);
				return;
			}
		}

		Instance = this;

		// preload default/english fonts...
		//Instance._Prepare(GuiOptions.convertLanguageToFullName[(int)GuiOptions.language]);
	}

	public static void Release()
	{
		if (Instance != null)
		{
			Instance.StopAllCoroutines();
			Instance.CancelInvoke();
			Instance._Prepare("");
			Instance = null;
		}
	}

	// =========================================================================================================================
	// === public interface ====================================================================================================
	public static GUIBase_FontBase GetFont(string inFontName)
	{
		return GetFont(inFontName, TextDatabase.instance.databaseLangugae);
	}

	public static GUIBase_FontBase GetFont(string inFontName, SystemLanguage inLanguage)
	{
#if UNITY_EDITOR
		if (Instance == null && Application.isEditor == true && Application.isPlaying == false)
		{
			// fix some strange bug in editor... Instance of FontManager is somettimes lost in editor.
			Instance = GetManager_InEditor();
			Instance._Prepare("English", true);
		}
#endif

		if (Instance == null)
			return null;

		return Instance._GetFont(inFontName, inLanguage);
	}

	public static void Prepare(string inNewLanguage)
	{
		if (Instance == null)
			return;

		Instance._Prepare(inNewLanguage);
	}

	// =========================================================================================================================
	// === internals ===========================================================================================================
	GUIBase_FontBase _GetFont(string inFontName, SystemLanguage inLanguage)
	{
		GUIBase_FontBase font = null;

		foreach (FontSetup setup in m_Fonts)
		{
			if (setup.m_FontName == inFontName)
			{
				font = setup.GetFont(inLanguage);
				break;
			}
		}

		if (font == null)
		{
			font = m_DefaultFont;
		}

		return font;
	}

	void _Prepare(string inNewLanguage, bool inForce = false)
	{
		foreach (FontSetup setup in m_Fonts)
			setup.Prepare(inNewLanguage, inForce);
	}

	// =========================================================================================================================
	// === Debug interface =====================================================================================================
	static MFFontManager GetManager_InEditor()
	{
		MFFontManager[] managers = FindObjectsOfType(typeof (MFFontManager)) as MFFontManager[];

		if (managers == null || managers.Length == 0)
		{
			Debug.LogError("can't find MFFontManager component in active scene");
			return null;
		}
		else if (managers.Length > 1)
		{
			Debug.LogWarning("There are more then one MFFontManager objects in scene, first one will be used !!!");
		}

		return managers[0];
	}

	public virtual void CheckDataConsistency()
	{
		if (m_DefaultFont == null)
		{
			Debug.LogError("Default font is not assigned", this);
		}

		MFDebugUtils.Assert(m_DefaultFont != null);

		// verify that default font name is not used in additional fonts.
		List<FontSetup> result;
		result = m_Fonts.FindAll(font => font.m_FontName == m_DefaultFontName);
		if (result != null && result.Count > 0)
		{
			Debug.LogError(string.Format("'{0}' is reserved name and can't be used for other fonts", m_DefaultFontName), this);
		}

		if (m_DefaultFontName != DefaultFontName_Static)
		{
			result = m_Fonts.FindAll(font => font.m_FontName == DefaultFontName_Static);
			if (result != null && result.Count > 0)
			{
				Debug.LogError(string.Format("'{0}' is reserved name and can't be used for other fonts", DefaultFontName_Static), this);
			}
		}

		// and now check that all names are unique...
		foreach (FontSetup setup in m_Fonts)
		{
			if (setup.m_Default.m_Font == null)
			{
				Debug.LogError(string.Format("Font setup [{0}] doesn't have assigned default font", setup.m_FontName), this);
			}

			result = m_Fonts.FindAll(font => font.m_FontName == setup.m_FontName);
			if (result.Count != 1)
			{
				Debug.LogError(string.Format("Font name {0} isn't unique [{1}]", setup.m_FontName, result.Count), this);
			}
		}
	}

	public void EDITOR_PostProcessScene()
	{
		foreach (FontSetup setup in m_Fonts)
			setup.CleanFontReference();
	}
}
