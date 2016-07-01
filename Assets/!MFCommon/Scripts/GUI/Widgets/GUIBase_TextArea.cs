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

[AddComponentMenu("GUI/Widgets/TextArea")]
public class GUIBase_TextArea : GUIBase_Callback
{
	public enum HorizontalTextAlignment
	{
		Left,
		Center,
		Right,
		Justify
	};

	public GUIBase_Widget Widget
	{
		get { return m_Widget; }
	}

	// editable properties...
	[SerializeField] string m_Text;
	[LocalizedTextId] [SerializeField] int m_TextID;
	[SerializeField] HorizontalTextAlignment m_Alignment = HorizontalTextAlignment.Left;
	[SerializeField] string m_FontName = "NewFont";
	[SerializeField] Vector2 m_TextScale = Vector2.one;
	[SerializeField] float m_LineSpace = 0.0f;

	// internal...
	bool m_RegenerateSprites = true;
	GUIBase_Widget m_Widget;
	GUIBase_FontEx m_Font;
	Vector2 m_TextSize;

	// public properties...
	public string text
	{
		get { return m_Text; }
	}

	public Vector2 textSize
	{
		get
		{
			RegenerateSprites();
			return m_TextSize;
		}
	}

	public float lineSpace
	{
		get { return m_LineSpace; }
	}

	public HorizontalTextAlignment alignment
	{
		get { return m_Alignment; }
	}

	public GUIBase_FontBase font
	{
		get { return (GUIBase_FontBase)m_Font; }
	}

	public Texture2D fontTexture
	{
		get { return (m_Font && m_Font.fontMaterial) ? (Texture2D)m_Font.fontMaterial.mainTexture : null; }
	}

	public Vector2 textScale
	{
		get { return m_TextScale; }
		set { m_TextScale = value; }
	}

	//---------------------------------------------------------

	public bool IsForTextField { get; set; }

	// ==================================================================================================
	// === Default MoneBehaviour interface ==============================================================

	#region MoneBehaviourInterface

	public void Start()
	{
		m_Widget = GetComponent<GUIBase_Widget>();
		m_Widget.RegisterUpdateDelegate(RegenerateSprites);
		m_Widget.RegisterCallback(this, (int)E_CallbackType.E_CT_INIT);
		m_Widget.m_TextScaleFix_HACK = true;

		if (m_TextID <= 0 && string.IsNullOrEmpty(m_Text))
		{
			m_Text = "This-is-temporary-test-for-testing-TextArea-auto-wrap. This is tooooooo long line. \n\n"
					 + "This is temporary test for testing TextArea auto wrap. This is tooooooo long line. "
					 + "This is temporary test for testing TextArea auto wrap. This is tooooooo long line.";

			//m_Text	=  "Test";
		}

		if (m_Font == null)
		{
			//m_Font = MFGuiManager.Instance.GetFontForLanguage(SystemLanguage.English);
			m_Font = MFFontManager.GetFont(m_FontName) as GUIBase_FontEx;
		}

		SetRegenerationNeeded();
	}

	/*public void Update()
	{
		float scale = Mathf.Abs(Mathf.Sin(Time.timeSinceLevelLoad*0.5f+Mathf.PI*0.5f));
		transform.localScale = Vector3.one*scale;
	}*/

	#endregion MoneBehaviourInterface

	// ==================================================================================================
	// === GUIBase_Wifget interaction ===================================================================

	#region GUIBase_Wifget interaction

	public override bool Callback(E_CallbackType type, object evt)
	{
		switch (type)
		{
		case E_CallbackType.E_CT_INIT:
		{
			m_Widget.ChangeMaterial(m_Font.fontMaterial);
			SetRegenerationNeeded();
		}
			return true;
		}

		return false;
	}

	//---------------------------------------------------------
	void RegenerateSprites()
	{
		if (m_RegenerateSprites == false || m_Widget.IsVisible() == false)
			return;

		if (m_Font == null)
		{
			m_Font = MFFontManager.GetFont(m_FontName) as GUIBase_FontEx;
			if (m_Font == null)
			{
				Debug.LogError(gameObject.GetFullName() + " Can't load font with name " + m_FontName);
				return;
			}
		}

		if (m_TextID > 0)
		{
			m_Text = TextDatabase.instance[m_TextID];
		}

		// destroy old text if any exist...
		int maxSprites = text != null && text.Length > 0 ? Mathf.CeilToInt(text.Length*0.1f)*10 : 0;
		m_Widget.PrepareSprites(maxSprites);

		if (text != null && text.Length > 0)
		{
			Vector3 scale = transform.lossyScale;
			scale = Vector3.one;

			// setup cursor ...
			Vector2 leftUpPos = new Vector2(m_Widget.GetOrigPos().x - m_Widget.GetWidth()*0.5f*scale.x,
											m_Widget.GetOrigPos().y - m_Widget.GetHeight()*0.5f*scale.y);
			Vector2 cursor = leftUpPos;

			float maxLineSize = m_Widget.GetWidth()*scale.x;

			scale.x = m_TextScale.x;
			scale.y = m_TextScale.y;

			List<TextLine> textLines = GetLines(text, m_Font, alignment, maxLineSize, scale, lineSpace, IsForTextField);
			if (textLines == null || textLines.Count <= 0)
				return;

			float fontHeight = m_Font.GetFontHeight();
			m_TextSize = new Vector2(m_Widget.GetWidth(), textLines.Count*fontHeight*scale.y + (textLines.Count - 1)*lineSpace*fontHeight*scale.y);

			float width;
			Rect spriteRect, texRect;

			int spriteIdx = 0;
			foreach (TextLine line in textLines)
			{
				cursor = leftUpPos + line.m_Offset;

				for (int i = line.m_StartIndex; i < line.m_EndIndex; ++i)
				{
					int character = text[i];

					if (!IsForTextField && character == ' ')
					{
						cursor.x += line.m_SpaceWidth;
						continue;
					}
					switch (character)
					{
					case '\n':
						Debug.LogWarning("function GetLines doesn't work correctly");
						break;
					default:
					{
						if (m_Font.GetCharDescription(text[i], out width, out spriteRect, out texRect, false, false, false))
						{
							Vector2 inCharSize = new Vector2(spriteRect.width*scale.x, spriteRect.height*scale.y);
							Vector2 inCharCenter = cursor + new Vector2(spriteRect.center.x*scale.x, 0.0f);

							m_Widget.SetTextureCoords(spriteIdx, (int)texRect.x, (int)texRect.y, (int)texRect.width, (int)texRect.height);
							m_Widget.UpdateSpritePosAndSize(spriteIdx, inCharCenter.x, inCharCenter.y, inCharSize.x, inCharSize.y);
							m_Widget.ShowSprite(spriteIdx, true);
							cursor.x += width*scale.x;

							spriteIdx++;
						}
						break;
					}
					}
				}
			}

			// hide all unused sprites
			while (spriteIdx < maxSprites)
			{
				m_Widget.SetTextureCoords(spriteIdx, 0.0f, 0.0f, 0.0f, 0.0f);
				m_Widget.UpdateSpritePosAndSize(spriteIdx, 0.0f, -Screen.height, 1.0f, 1.0f);
				m_Widget.ShowSprite(spriteIdx, false);
				spriteIdx++;
			}
		}

		// we have to force widget update.
		m_RegenerateSprites = false;
		m_Widget.SetModify();
	}

	#endregion GUIBase_Wifget interaction

	// ==================================================================================================
	// === GUIBase_Label interface  =====================================================================

	#region GUIBase_Label interface

	public void SetNewText(string inText)
	{
#if MADFINGER_KEYBOARD_MOUSE
		if (string.IsNullOrEmpty(inText))
			inText = " ";
		else
			inText = inText + " ";
		
		inText = inText.Replace("\n", " \n");
#endif

		if (inText == m_Text)
			return;

		m_TextID = 0;
		m_Text = inText;

		SetRegenerationNeeded();
	}

	public void SetNewText(int inTextID)
	{
		if (m_TextID == inTextID)
			return;

		m_TextID = inTextID;
		m_Text = string.Empty;

		SetRegenerationNeeded();
	}

	public void Clear()
	{
		m_TextID = 0;
		m_Text = string.Empty;
		SetRegenerationNeeded();
	}

	#endregion GUIBase_Label interface

	// ==================================================================================================
	// === Text Rendering support =======================================================================

	#region Text Rendering support

	public class TextLine
	{
		public int m_StartIndex = 0;
		public int m_EndIndex = 0;
		public Vector2 m_Size = Vector2.zero;
		public Vector2 m_Offset = Vector2.zero;
		public bool m_EndOfParagraph = false;

		public int m_NumOfSpaces = 0;
		public float m_SpaceWidth = 0;
	}

	public static List<TextLine> GetLines(string inText,
										  GUIBase_FontEx inFont,
										  HorizontalTextAlignment inAlignment,
										  float inMaxlineWidth,
										  Vector3 inScale,
										  float inLineSpacePct,
										  bool isForTextField = false)
	{
		if (string.IsNullOrEmpty(inText) == true)
			return null;
		if (inMaxlineWidth <= 0.0f)
			return null;
		if (inScale == Vector3.zero)
			return null;

		List<TextLine> lines = new List<TextLine>();
		TextLine newLine = null;
		int lastSpaceIndex = -1;
		float widthAtLastSpace = 0;
		float widthOfSpace = inFont.GetCharWidth((int)' ')*inScale.x;
		float charWidth = 0;
		float fontHeight = inFont.GetFontHeight();

		for (int i = 0; i < inText.Length; ++i)
		{
			if (newLine == null)
			{
				newLine = new TextLine();
				newLine.m_StartIndex = i;
				newLine.m_SpaceWidth = widthOfSpace;
				lastSpaceIndex = -1;
				widthAtLastSpace = 0;
			}

			int character = (int)inText[i];

			if (!isForTextField && character == ' ')
			{
				lastSpaceIndex = i;
				widthAtLastSpace = newLine.m_Size.x;
				newLine.m_Size.x += widthOfSpace;
				newLine.m_NumOfSpaces++;
				continue;
			}
			switch (character)
			{
			case '\n':
				newLine.m_EndIndex = i;
				newLine.m_EndOfParagraph = true;
				lines.Add(newLine);
				newLine = null;
				break;
			default:
				charWidth = inFont.GetCharWidth(character)*inScale.x;
				if (newLine.m_Size.x + charWidth > inMaxlineWidth)
				{
					if (lastSpaceIndex >= 0)
					{
						newLine.m_NumOfSpaces--;
						newLine.m_Size.x = widthAtLastSpace;
						newLine.m_EndIndex = lastSpaceIndex;
						i = lastSpaceIndex;
					}
					else
					{
						newLine.m_EndIndex = i;
						i = i - 1;
					}

					newLine.m_EndOfParagraph = false;
					if ((newLine.m_EndIndex - newLine.m_StartIndex) < 1)
					{
						//this line is invalid. It looks that actual character is too width for inMaxlineWidth
						// skip it.
						Debug.LogWarning("Can't generate line for character: " + (char)character);
					}
					else
					{
						lines.Add(newLine);
					}

					newLine = null;
				}
				else
				{
					newLine.m_Size.x += charWidth;
				}

				break;
			}
		}

		if (newLine != null)
		{
			newLine.m_EndIndex = inText.Length;
			newLine.m_EndOfParagraph = true;

			if ((newLine.m_EndIndex - newLine.m_StartIndex) == 0)
			{
				Debug.LogWarning("Empty line");
			}
			else
			{
				lines.Add(newLine);
			}
		}

		float yOffset = 0;
		// Compute x offset of lines by TextAlignment.
		foreach (TextLine line in lines)
		{
#if !MADFINGER_KEYBOARD_MOUSE
			// remove spaces from start and end of line...
			TrimSpaces(inText, line, widthOfSpace);
#endif

			// Check line validity...
			if ((line.m_EndIndex - line.m_StartIndex) == 0)
			{
				//Debug.LogWarning("Empty line");
				continue;
			}

			// setup line x offset based on TextAlignment...
			float span = (inMaxlineWidth - line.m_Size.x);

			switch (inAlignment)
			{
			case HorizontalTextAlignment.Left:
				line.m_Offset.x = 0.0f;
				break;
			case HorizontalTextAlignment.Center:
				line.m_Offset.x = span*0.5f;
				break;
			case HorizontalTextAlignment.Right:
				line.m_Offset.x = span;
				break;
			case HorizontalTextAlignment.Justify:
				if (line.m_EndOfParagraph == false && line.m_NumOfSpaces > 0)
				{
					line.m_SpaceWidth += span/(float)line.m_NumOfSpaces;
				}
				break;
			default:
				Debug.LogError("Unknown Horizontal text alignment !!!! " + inAlignment);
				break;
			}

			yOffset += 0.5f*fontHeight*inScale.y;
			line.m_Offset.y = yOffset;
			yOffset += 0.5f*fontHeight*inScale.y;
			yOffset += inLineSpacePct*fontHeight*inScale.y;
		}

		return lines;
	}

	static int TrimSpaces(string inText, TextLine inLine, float inSpaceWidth)
	{
		int numOfRemovedSpaces = 0;
		// remove spaces from beginning...
		for (int i = inLine.m_StartIndex; i < inLine.m_EndIndex; ++i)
		{
			if (inText[i] != (char)' ')
				break;

			inLine.m_StartIndex++;
			numOfRemovedSpaces++;
		}

		// remove spaces from end...			
		for (int i = inLine.m_EndIndex - 1; i >= inLine.m_StartIndex; --i)
		{
			if (inText[i] != (char)' ')
				break;

			inLine.m_EndIndex--;
			numOfRemovedSpaces++;
		}

		inLine.m_Size.x -= numOfRemovedSpaces*inSpaceWidth;
		inLine.m_NumOfSpaces -= numOfRemovedSpaces;
		return numOfRemovedSpaces;
	}

	public void OnLanguageChanged(string inNewLanguage)
	{
		if (m_Widget == null)
			return;

		string font_name = m_FontName;
		if (m_FontName == "Default")
			font_name = "NewFont";

		GUIBase_FontEx newFont = null;
		if ("English.Old" == inNewLanguage)
		{
			newFont = MFFontManager.GetFont(m_FontName, SystemLanguage.English) as GUIBase_FontEx;
		}
		else
		{
			newFont = MFFontManager.GetFont(font_name) as GUIBase_FontEx;
		}

		if (newFont != m_Font)
		{
			m_Font = newFont;
			m_Widget.ChangeMaterial(m_Font.fontMaterial);
		}

		SetRegenerationNeeded();
	}

	#endregion Text Rendering suppor

	void SetRegenerationNeeded()
	{
		m_RegenerateSprites = true;
		m_Widget.SetModify();
	}

	public int GetLineCount(string inText)
	{
		if (string.IsNullOrEmpty(inText))
			inText = " ";
		else
			inText = inText + " ";

		inText = inText.Replace("\n", " \n");

		Vector3 scale = transform.lossyScale;
		scale = Vector3.one;

		//	Vector2	leftUpPos   = new Vector2(m_Widget.GetOrigPos().x-m_Widget.GetWidth()*0.5f*scale.x, m_Widget.GetOrigPos().y-m_Widget.GetHeight()*0.5f*scale.y);

		float maxLineSize = m_Widget.GetWidth()*scale.x;

		scale.x = m_TextScale.x;
		scale.y = m_TextScale.y;

		List<TextLine> lines = GetLines(inText, m_Font, alignment, maxLineSize, scale, lineSpace, IsForTextField);
		return (lines == null) ? 1 : lines.Count;
	}
}

/*
public struct Glyph
{
	char 		Character;
	Vector2 	Advance;
	Rect 		ScreenRect;
	Rect 		TextureRect;
	
	public Glyph(char inCharacter, Vector2 inAdvance, Rect inScreenRect, Rect inTextureRect)
	{
		Character 	= inCharacter;
		Advance 	= inAdvance;
		ScreenRect 	= inScreenRect;
		TextureRect = inTextureRect;
	}
}


public class TextProcessor
{
	//..............................................................................
	public class _TextItem
	{
		public Vector2 	Position = Vector2.zero;
		public Vector2 	Size 	 = Vector2.zero;
	}
	//..............................................................................
	public class _Space : _TextItem
	{	}
	//..............................................................................	
	public class _NewLine : _TextItem
	{	}
	//..............................................................................	
	public class _Text : _TextItem
	{
		public string 		Text = string.Empty;
	}
	//..............................................................................	
	public class _Line : _TextItem
	{
		public List<_TextItem>		Items = new List<_TextItem>();
		public List<_Space>			Spaces = new List<_Space>();
		
		public void Add(_TextItem inItem)
		{
			if(inItem is _Space)
			{
				Spaces.Add(inItem as _Space);
			}
			
			Items.Add(inItem);
			Size.x += inItem.Size.x;
			Size.y  = Mathf.Max(Size.y, inItem.Size.y);
		}
	}
	//..............................................................................
	public class _Block : _TextItem
	{
		public List<_Line>		Lines = new List<_Line>();
		
		public void Add(_Line inLine)
		{
			Lines.Add(inLine);
		}
	}
	//..............................................................................
	
			
	private _Block 				m_Block;
	private Rect 				m_TargetRegion;
	private string 				m_Text;
	private GUIBase_FontEx 		m_Font;
	
	
	public TextProcessor(Rect inTargetTect, string inText, GUIBase_FontEx inFont)
	{
		m_TargetRegion = inTargetTect;
		m_Text 		   = inText;
		m_Font 		   = inFont;
	
		Process(inTargetTect, inText, inFont);
	}
	
	private void Process(Rect inTargetTect, string inText, GUIBase_FontEx inFont)
	{
		m_Block = null;
		
		// Fragment input string into parts...
		List<_TextItem> textFragments;
		textFragments = Fragment( inText );
		
		// Compute size of all fragments...
		RecalculateFragmentSize(ref textFragments, inFont);
		
		// and now reposition text fragments by input settings...
		m_Block = CreateTextBlock(inTargetTect, textFragments);
	}
	
	static private List<_TextItem> Fragment(string inText)
	{
		List<_TextItem> 	textFragments = new List<_TextItem>();
		_Text 				fragment = new _Text();
		
		for (int i = 0; i < inText.Length; ++i)
		{
			int character = inText[i];
			switch(character)
			{
				case '\n':
					if( string.IsNullOrEmpty(fragment.Text) == false )
					{
						textFragments.Add(fragment);
						fragment = new _Text();
					}
					
					textFragments.Add(new _NewLine());
					break;
				case ' ':
					if( string.IsNullOrEmpty(fragment.Text) == false )
					{
						textFragments.Add(fragment);
						fragment = new _Text();
					}
					
					textFragments.Add(new _Space());
					break;
				default:
					fragment.Text += (char )character;
					break;
			}
		}
		
		return textFragments;
	}
	
	static private void RecalculateFragmentSize(ref List<_TextItem> inTextFragments, GUIBase_FontEx inFont)
	{
		// in first retrieve size of space. 
		float spaceSize = inFont.GetCharWidth((int)' ');
		
		foreach(_TextItem fragment in inTextFragments)
		{
			if(fragment is _NewLine)
			{
				// nothing. NewLine dosn't have size, So continue...
			}
			else if(fragment is _Space)
			{
				// all spaces has same width in this moment...
				fragment.Size.x = spaceSize;
			}
			else if(fragment is _Text)
			{
				fragment.Size = inFont.GetTextSize( ((_Text)fragment).Text );
			}
			else
			{
				Debug.LogError("Unknown text fragment type !!!");
			}
		}
	}
	
	static private _Block CreateTextBlock(Rect inTargetTect, List<_TextItem>  inTextFragments)
	{
		_Block textBlock = new _Block();
		_Line  textLine  = new _Line();
	
		foreach(_TextItem fragment in inTextFragments)
		{
			if(fragment is _NewLine)
			{
				textBlock.Add(textLine);
				textLine  = new _Line();
				// nothing. NewLine dosn't have size, So continue...
			}
			else if(fragment is _Space || fragment is _Text)
			{
				if((textLine.Size.x + fragment.Size.x) > inTargetTect.width)
				{
					textBlock.Add(textLine);
					textLine  = new _Line();
				}
				
				textLine.Add(fragment);
			}
			else
			{
				Debug.LogError("Unknown text fragment type !!!");
			}
		}
		
		textBlock.Add(textLine);
		return textBlock;
	}
	
    public IEnumerator<Glyph> GetEnumerator()
    {
      	yield break;    
      	/*
    	if(m_Block == null || m_Block.Lines.Count == 0)
      		yield break;
      		
      	Vector2 cursor = new Vector2 (m_TargetRect.x, m_TargetRect.y);
      		
      	foreach(_Line line in m_Block.Lines)
      	{
      		foreach(_TextItem textItem in line.Items)
      		{
      			if(textItem is _Text)
      			{
      				_Text text = textItem as _Text;
      				foreach(char ch in text.Text)
      				{
						float width;
						Rect  spriteRect, texRect;
						if(m_FontEx.GetCharDescription(text[i], out width, out spriteRect, out texRect, false, false))
						{
							yield return new Glyph(ch, Vector2.zero, texRect, texRect);
							/*
							Vector2	inCharSize    = new Vector2(spriteRect.width, spriteRect.height/***** / * texHeight);	// TODO :: rewite without texHeight...
							Vector2	inCharCenter  = new Vector2(cursor.x, cursor.y) + new Vector2((spriteRect.x + spriteRect.width)*0.5f*scale.x, /*spriteRect.y*0.5f* /0.0f);

							m_Widget.AddSprite(inCharCenter, inCharSize.x, inCharSize.y, scale.x, scale.y, 0.0f, (int)texRect.x, (int)(texRect.y + texRect.height), (int)texRect.width, (int)texRect.height);
							cursor.x += width * scale.x;
							* /
						}
      				}
      			}
      		}
      	}
      	* /
    }
}
*/
