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

[AddComponentMenu("")]
//[ExecuteInEditMode]
public class GUIBase_FontEx : GUIBase_FontBase
{
	[System.Serializable]
	internal class BitmapCharacterSet
	{
		public int MaxCharHeight;
		public int LineHeight;
		public int Base;
		public int RenderedSize;
		public int Width;
		public int Height;

		public BitmapCharacter[] _CharTable = new BitmapCharacter[1]; // this is here only for serialization...
		public Dictionary<int, BitmapCharacter> Characters = new Dictionary<int, BitmapCharacter>();

		public bool isReady
		{
			get { return Characters.Count == _CharTable.Length; }
		}
	}

	[System.Serializable]
	internal class BitmapCharacter
	{
		public int Char; // this is here only for serialization. We need char key for dictionary...
		public int X;
		public int Y;
		public int Width;
		public int Height;
		public int XOffset;
		public int YOffset;
		public int XAdvance;

		public List<Kerning> KerningList = new List<Kerning>();
	}

	[System.Serializable]
	internal class Kerning
	{
		public int Second;
		public int Amount;
	}

	[SerializeField] BitmapCharacterSet m_CharSet = new BitmapCharacterSet();

	[SerializeField] Material m_Material;
	public TextAsset m_FontDescriptionFile;

	public float m_CorrectionFontSizeScale = 1;
	static float m_ErrorFontHeight = 50;
	static float m_ErrorFontBase = 25; //m_ErrorFontHeight/2.0f
#if UNITY_EDITOR
	List<int> m_MissingChars = new List<int>();
#endif //UNITY_EDITOR

	public override Material fontMaterial
	{
		get { return m_Material; }
	}

	bool m_Initialized = false;

	public float texWidthFixupCoef
	{
		get { return (float)m_Material.mainTexture.width/(float)m_CharSet.Width; }
	}

	public float texHeightFixupCoef
	{
		get { return (float)m_Material.mainTexture.height/(float)m_CharSet.Height; }
	}

	void Reset()
	{
		ReloadCharDescriptionFile();
	}

	void Awake()
	{
		if (m_Material != null)
		{
			m_Material = Instantiate(m_Material) as Material;
		}

		if (m_Material == null)
		{
			Debug.LogError("Can not load or instatiate material for font...");
		}

		ReloadCharDescriptionFile();
/*
#if UNITY_EDITOR
		ProcessFontDescriptionAsset();
		m_CharSet._CharTable = new GUIBase_FontEx.BitmapCharacter[m_CharSet.Characters.Count];
		m_CharSet.Characters.Values.CopyTo(m_CharSet._CharTable, 0);
#else
		foreach(BitmapCharacter charInfo in m_CharSet._CharTable)
		{
			m_CharSet.Characters[charInfo.Char] = charInfo;
		}
#endif
*/
	}

	bool ProcessFontDescriptionAsset()
	{
		//FileInfo theSourceFile = null;
		if (m_FontDescriptionFile == null)
		{
			Debug.Log("FontDescriptionFile -- was not found");
			return false;
		}

		// puzdata.text is a string containing the whole file. To read it line-by-line:
		StringReader reader = new StringReader(m_FontDescriptionFile.text);
		if (reader == null)
		{
			Debug.Log("FontDescriptionFile not found or is not readable");
			return false;
		}
		else
		{
			ParseFNTFile(reader);
			return true;
		}
	}

	/*
    private bool LoadTextFile(string inFileName)
    {
        //FileInfo theSourceFile = null;
        TextAsset textFile = (TextAsset)AssetBundleManager.Instance.LoadFromResources("MainMenuResources", inFileName, typeof(TextAsset));
        if ( textFile == null )
        {
            Debug.Log(inFileName + " -- was not found");
            return false;
        }

        // puzdata.text is a string containing the whole file. To read it line-by-line:
        StringReader reader = new StringReader(textFile.text);
        if ( reader == null )
        {
            Debug.Log("puzzles.txt not found or not readable");
            return false;
        }
        else
        {
			ParseFNTFile(reader);
			return true;
		}
	}
	 */

	/// <summary>Parses the FNT file.</summary>
	void ParseFNTFile(StringReader inReader)
	{
		m_CharSet = new BitmapCharacterSet();
		m_CharSet.MaxCharHeight = 0;
		//string fntFile = Utility.GetMediaFile( m_fntFile );
		//StreamReader stream = new StreamReader( fntFile );
		StringReader stream = inReader;
		string line;
		char[] separators = new char[] {' ', '='};
		while ((line = stream.ReadLine()) != null)
		{
			string[] tokens = line.Split(separators);
			if (tokens[0] == "info")
			{
				// Get rendered size
				for (int i = 1; i < tokens.Length; i++)
				{
					if (tokens[i] == "size")
					{
						m_CharSet.RenderedSize = int.Parse(tokens[i + 1]);
					}
				}
			}
			else if (tokens[0] == "common")
			{
				// Fill out BitmapCharacterSet fields
				for (int i = 1; i < tokens.Length; i++)
				{
					if (tokens[i] == "lineHeight")
					{
						m_CharSet.LineHeight = int.Parse(tokens[i + 1]);
					}
					else if (tokens[i] == "base")
					{
						m_CharSet.Base = int.Parse(tokens[i + 1]);
					}
					else if (tokens[i] == "scaleW")
					{
						m_CharSet.Width = int.Parse(tokens[i + 1]);
					}
					else if (tokens[i] == "scaleH")
					{
						m_CharSet.Height = int.Parse(tokens[i + 1]);
					}
				}
			}
			else if (tokens[0] == "char")
			{
				// New BitmapCharacter
				int index = 0;
				for (int i = 1; i < tokens.Length; i++)
				{
					if (tokens[i] == "id")
					{
						index = int.Parse(tokens[i + 1]);
						m_CharSet.Characters[index] = new BitmapCharacter();
						m_CharSet.Characters[index].Char = index;
					}
					else if (tokens[i] == "x")
					{
						m_CharSet.Characters[index].X = int.Parse(tokens[i + 1]);
					}
					else if (tokens[i] == "y")
					{
						m_CharSet.Characters[index].Y = int.Parse(tokens[i + 1]);
					}
					else if (tokens[i] == "width")
					{
						m_CharSet.Characters[index].Width = int.Parse(tokens[i + 1]);
					}
					else if (tokens[i] == "height")
					{
						m_CharSet.Characters[index].Height = int.Parse(tokens[i + 1]);
						m_CharSet.MaxCharHeight = Mathf.Max(m_CharSet.MaxCharHeight, m_CharSet.Characters[index].Height);
					}
					else if (tokens[i] == "xoffset")
					{
						m_CharSet.Characters[index].XOffset = int.Parse(tokens[i + 1]);
					}
					else if (tokens[i] == "yoffset")
					{
						m_CharSet.Characters[index].YOffset = int.Parse(tokens[i + 1]);
					}
					else if (tokens[i] == "xadvance")
					{
						m_CharSet.Characters[index].XAdvance = int.Parse(tokens[i + 1]);
					}
				}
			}
			else if (tokens[0] == "kerning")
			{
				// Build kerning list
				int index = 0;
				Kerning k = new Kerning();
				for (int i = 1; i < tokens.Length; i++)
				{
					if (tokens[i] == "first")
					{
						index = int.Parse(tokens[i + 1]);
					}
					else if (tokens[i] == "second")
					{
						k.Second = int.Parse(tokens[i + 1]);
					}
					else if (tokens[i] == "amount")
					{
						k.Amount = int.Parse(tokens[i + 1]);
					}
				}
				m_CharSet.Characters[index].KerningList.Add(k);
			}
		}

		stream.Close();
	}

	/*void OnGUI()
	{
		if(Event.current.type.Equals(EventType.Repaint))
		{
			//Rect screenRect = new Rect(0, 0, 500, 500);
			//Rect sourceRect = new Rect(0, 0, 1, 1);
			//Graphics.DrawTexture(screenRect, m_Texture, sourceRect, 0, 0, 0, 0);// mat);

			//string testString = "This . is test\\ //,# string ygpd W";
			string testString = "John: Damn it... that didn't go well.";

			float x = Screen.width*0.4f;
			float y = 120;

			Rect screenRect, sourceRect;
			float width;
			for(int i = 0; i < testString.Length; i++)
			{
				if(GetCharDescription(testString[i], out width, out screenRect, out sourceRect))
				{
					screenRect.x += x;
					screenRect.y += y;

					Graphics.DrawTexture(screenRect, m_Texture, sourceRect, 0, 0, 0, 0, m_Material);

					x += width;
				}
			}
		}
	}*/

	public bool GetCharDescription(int inCharacter,
								   out float outWidth,
								   out Rect outSprite,
								   out Rect outTexUV,
								   bool inNormalizeTextCoord = true,
								   bool inFliped = true,
								   bool inFixHeightForGUI = true)
	{
		outWidth = 0;
		outSprite = new Rect();
		outTexUV = new Rect();

		if (Initialize() == false)
			return false;

		BitmapCharacter c = GetCharacterInfo(inCharacter);
		if (c == null)
			return false;

		float xOffset = c.XOffset*m_CorrectionFontSizeScale;
		float yOffset = c.YOffset*m_CorrectionFontSizeScale;
		float xAdvance = c.XAdvance*m_CorrectionFontSizeScale;
		float width = c.Width*m_CorrectionFontSizeScale;
		float height = c.Height*m_CorrectionFontSizeScale;

		if (inFixHeightForGUI)
		{
			yOffset /= (float)m_CharSet.Height;
			height /= (float)m_CharSet.Height*texHeightFixupCoef;
		}

		outWidth = xAdvance;
		outSprite = new Rect(xOffset, yOffset, width, height);

		float texU = (float)c.X;
		float texV = (float)c.Y;
		float texW = (float)c.Width;
		float texH = (float)c.Height;

		if (inFliped == true)
		{
			outTexUV = new Rect(texU, m_CharSet.Height - (texV + texH), texW, texH);
		}
		else
		{
			outTexUV = new Rect(texU, texV, texW, texH);
		}

		if (inNormalizeTextCoord == true)
		{
			outTexUV.x /= (float)m_CharSet.Width;
			outTexUV.y /= (float)m_CharSet.Height;
			outTexUV.width /= (float)m_CharSet.Width;
			outTexUV.height /= (float)m_CharSet.Height;
		}
		else
		{
			outTexUV.x *= (float)texWidthFixupCoef;
			outTexUV.y *= (float)texHeightFixupCoef;
			outTexUV.width *= (float)texWidthFixupCoef;
			outTexUV.height *= (float)texHeightFixupCoef;
		}

		return true;
	}

	/*public bool GetCharDescriptionNew(int inCharacter, out float outWidth, out Rect outSprite, out Rect outTexUV)
	{	return GetCharDescription(inCharacter, out outWidth, out outSprite, out outTexUV, true, true);	}
	public bool GetCharDescriptionNew(int inCharacter, out float outWidth, out Rect outSprite, out Rect outTexUV, bool inNormalizeTextCoord, bool inFliped)
	{
		outWidth = 0;
		outSprite = new Rect();
		outTexUV = new Rect();

		if(Initialize() == false)
			return false;

		BitmapCharacter c = GetCharacterInfo(inCharacter);
		if(c == null)
			return false;

		float xOffset 		= c.XOffset 	* m_CorrectionFontSizeScale;
		float yOffset 		= c.YOffset		* m_CorrectionFontSizeScale;
		float xAdvance 		= c.XAdvance 	* m_CorrectionFontSizeScale;
		float width 		= c.Width 		* m_CorrectionFontSizeScale;
		float height 		= c.Height 		* m_CorrectionFontSizeScale;

		outWidth = xAdvance;
		outSprite = new Rect(xOffset, yOffset, width, height);

		float texU =  (float)c.X;
		float texV =  (float)c.Y;
		float texW =  (float)c.Width;
		float texH =  (float)c.Height;
		
		if(inFliped == true)
		{
			outTexUV = new Rect(texU, m_CharSet.Height-(texV+texH), texW, texH);
		}
		else
		{
			outTexUV = new Rect(texU, texV, texW, texH);
		}

		if(inNormalizeTextCoord == true)
		{
			outTexUV.x 		/= (float)m_CharSet.Width;
			outTexUV.y 		/= (float)m_CharSet.Height;
			outTexUV.width 	/= (float)m_CharSet.Width;
			outTexUV.height /= (float)m_CharSet.Height;
		}
		else
		{
			outTexUV.x 		*= (float)m_WidthFixupCoef;
			outTexUV.y 		*= (float)m_HeightFixupCoef;
			outTexUV.width 	*= (float)m_WidthFixupCoef;
			outTexUV.height *= (float)m_HeightFixupCoef;
		}
		
		return true;
	}*/

	public float GetFontHeight()
	{
		if (m_CharSet == null)
		{
			Debug.LogError("FontEx - GetFontHeight : No charset !!!");
			return m_ErrorFontHeight;
		}

		//return m_CharSet.LineHeight;
		//return m_CharSet.MaxCharHeight;
		return m_CharSet.MaxCharHeight*m_CorrectionFontSizeScale;
	}

	public float GetFontBase()
	{
		if (m_CharSet == null)
		{
			Debug.LogError("FontEx - GetFontBase : No charset !!!");
			return m_ErrorFontBase;
		}

		//return m_CharSet.LineHeight;
		return m_CharSet.Base;
	}

	public float GetCharWidth(int inCharacter)
	{
		if (Initialize() == false)
			return 0.0f;

		BitmapCharacter c = GetCharacterInfo(inCharacter);
		if (c == null)
			return 0.0f;

		float xAdvance = c.XAdvance*m_CorrectionFontSizeScale;
		return xAdvance;
	}

	public Vector2 GetTextSize(string inText)
	{
		// TODO :: 	computing height is not correct there can be fonts with glyphs which has 
		//         	different height and y offset. So for example [ _ , ~ ] can have same height 
		//			but lie on different base line.

		Vector2 size = Vector2.zero;

		if (Initialize() == true)
		{
			foreach (char ch in inText)
			{
				BitmapCharacter bc = GetCharacterInfo(ch);
				if (bc == null)
					continue;

				size.x += (bc.XAdvance*m_CorrectionFontSizeScale);
				size.y = Mathf.Max(size.y, bc.Height*m_CorrectionFontSizeScale);
			}
		}

		return size;
	}

	BitmapCharacter GetCharacterInfo(int inCharacter)
	{
#if UNITY_EDITOR
		if (m_CharSet == null)
		{
			Debug.LogWarning("FontEx - GetCharDescription : No charset !!!");
			return null;
		}
#endif //UNITY_EDITOR		

		if (m_CharSet.Characters.ContainsKey(inCharacter) == false)
		{
#if UNITY_EDITOR
			if (inCharacter != '\n' && inCharacter != '\r' && m_MissingChars.Contains(inCharacter) == false)
			{
				m_MissingChars.Add(inCharacter);
				Debug.LogWarning("FontEx - GetCharDescription - Can't find char description for character [ " + (char)inCharacter + " ] [ " +
								 inCharacter.ToString("X4") + " ]");
			}
#endif //UNITY_EDITOR

			if (m_CharSet.Characters.ContainsKey(-1) == true)
			{
				inCharacter = -1;
			}
			else if (m_CharSet.Characters.ContainsKey(' ') == true)
			{
				inCharacter = ' ';
			}
			else
			{
				return null;
			}
		}

		return m_CharSet.Characters[inCharacter];
	}

	[ContextMenu("Reload Char Description File")]
	void ReloadCharDescriptionFile()
	{
		ProcessFontDescriptionAsset();
		m_CharSet._CharTable = new GUIBase_FontEx.BitmapCharacter[m_CharSet.Characters.Count];
		m_CharSet.Characters.Values.CopyTo(m_CharSet._CharTable, 0);
	}

	bool Initialize()
	{
		if (m_Initialized == false || m_CharSet.isReady == false)
		{
			if (Application.isPlaying == false)
			{
				ReloadCharDescriptionFile();
			}
			else
			{
				m_CharSet.Characters.Clear();
				foreach (BitmapCharacter charInfo in m_CharSet._CharTable)
					m_CharSet.Characters[charInfo.Char] = charInfo;
			}

			m_Initialized = true;
		}

		return m_Initialized;
	}
}
