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

[AddComponentMenu("GUI/Widgets/Label")]
public class GUIBase_Label : GUIBase_Callback
{
	// TODO ::  This is old text manualy edited in editor. It is here only for security to don't destroy old data.
	//          If we will be sure that everythin works with new system we can remove it.
	//          in that situation rewrite all internal calling of text on m_TextDyn; (don't call properties if it is not needed)
	//          And then remove SerializeField attribute from m_TextDyn. It is not neccesary
	[SerializeField] string m_Text;

	[LocalizedTextId] [SerializeField] int m_TextID;
	int m_TextIDGenerated = 0;

	[HideInInspector] [SerializeField] string m_TextDyn;

	[SerializeField] TextAnchor m_AnchorPoint = TextAnchor.MiddleCenter;
	[SerializeField] TextAlignment m_Alignment = TextAlignment.Left;

	[SerializeField] bool m_Uppercase = false;

#if UNITY_EDITOR
	string m_TextGenerated = "";
	int m_AnchorPointGen = -1;
	int m_AlignmentGen = -1;
	float m_LineSpaceGenerated;
#endif

	[SerializeField] string m_FontName = "Default";
	GUIBase_FontBase m_FontXXX;

	public bool useFontEx
	{
		get { return (font != null) && (font is GUIBase_FontEx); }
	}

#if UNITY_EDITOR
	//public static GUIBase_FontEx 			m_FontEx;
	[SerializeField] bool m_UseFontExGen = false;
#endif

	public string Text
	{
		get { return (m_TextID > 0 && m_TextDyn != null) ? m_TextDyn : m_Text; }
		set { SetNewText(value); }
	}

	public bool isValid
	{
		get { return (Text != null && Text != "" && font); }
	}

	public Vector2 textSize
	{
		get { return GetTextSize(); }
	}

	GUIBase_Widget m_Widget;

	[SerializeField] bool m_AllowMultiline = true;

	[SerializeField] float m_LineSpace2 = 0.0f;
	Vector2 m_LineSize;

	public Vector2 lineSize
	{
		get { return m_LineSize; }
	}

	public float lineSpace
	{
		get { return m_LineSpace2*0.01f; }
	}

	public TextAlignment alignment
	{
		get { return m_Alignment; }
	}

	public GUIBase_Widget Widget
	{
		get { return m_Widget; }
	}

	Rect m_Boundaries;

	public Rect Boundaries
	{
		get { return m_Boundaries; }
		set
		{
			if (m_Boundaries != value)
			{
				m_Boundaries = value;
				SetRegenerationNeeded();
			}
		}
	}

#if UNITY_EDITOR
	int m_ReloadCount = 0;
#endif

	bool m_RegenerationNeeded = true;

	//public  GUIBase_Font 					old_font	{ get { return m_Font; } set { m_Font = value; } }
	public GUIBase_FontBase font
	{
		get
		{
			if (m_FontXXX == null)
			{
				m_FontXXX = MFFontManager.GetFont(m_FontName);
			}
			return m_FontXXX;
		}
	}

	public Texture2D fontTexture
	{
		get
		{
			if (font && font.fontMaterial)
			{
				return (Texture2D)font.fontMaterial.mainTexture;
			}
			return null;
		}
	}

	public bool Uppercase
	{
		get { return m_Uppercase; }
		set
		{
			if (m_Uppercase == value)
				return;
			m_Uppercase = value;

			SetRegenerationNeeded();
		}
	}

	//---------------------------------------------------------

	// ==================================================================================================
	// === Default MoneBehaviour interface ==============================================================

	#region MoneBehaviourInterface

	public void Start()
	{
		m_Widget = GetComponent<GUIBase_Widget>();
		m_Widget.m_TextScaleFix_HACK = true;
		m_Widget.RegisterUpdateDelegate(RegenerateSprites);
		m_Widget.RegisterCallback(this, (int)E_CallbackType.E_CT_INIT);

		m_TextDyn = null;
		m_TextIDGenerated = 0;
#if UNITY_EDITOR
		m_TextGenerated = "";
		m_AnchorPointGen = -1;
		m_AlignmentGen = -1;
		m_LineSpaceGenerated = 0.0f;
#endif
		SetRegenerationNeeded();

		if (m_TextID != 0)
		{
			m_TextDyn = m_AllowMultiline == true ? TextDatabase.instance[m_TextID] : TextDatabase.instance[m_TextID].Replace("\n", " ");
			if (m_TextDyn == "<UNKNOWN TEXT>")
				Debug.Log("Invalid text id on label: " + gameObject.GetFullName());
		}
		else if (string.IsNullOrEmpty(m_Text) == false && m_AllowMultiline == false)
		{
			m_Text = m_Text.Replace("\n", " ");
		}
	}

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
			if (m_FontName == "Default")
				m_FontName = "NewFont"; // HACK, Don't use old 'default' font

			m_Widget.ChangeMaterial(font.fontMaterial);
			SetRegenerationNeeded();
		}
			return true;
		}

		return false;
	}

	//---------------------------------------------------------
	void RegenerateSprites()
	{
		if (IsDataRegenerationNeaded() == false || m_Widget.IsVisible() == false)
			return;

		// regenerate runtime data for new text...
		GenerateRunTimeData();

		string text = Text;
		if (string.IsNullOrEmpty(text) == false && m_Uppercase)
		{
			text = text.ToUpper();
		}

		// destroy old text if any exist...
		m_Widget.PrepareSprites(string.IsNullOrEmpty(text) == true ? 0 : text.Length);

		if (string.IsNullOrEmpty(text) == false && text.Length > 0)
		{
			Vector3 scale = Vector3.one;

			Texture texture = fontTexture;

			int texWidth = texture ? texture.width : 1;
			int texHeight = texture ? texture.height : 1;

			float lineHeight = m_LineSize.y*texHeight;
			float lineWidth = m_LineSize.x;
			float widthMult = m_Widget.GetWidth()/lineWidth;

			// compute down scale if needed
			if (m_Boundaries.IsEmpty() == false)
			{
				Rect clientRect = GetRect();
				Rect rect = m_Boundaries.Intersect(clientRect);

				float scaleX = rect.width/clientRect.width;
				float scaleY = rect.height/clientRect.height;

				if (scaleY > scaleX)
					scaleY = scaleX;
				else
					scaleX = scaleY;

				scale.x *= scaleX;
				scale.y *= scaleY;
			}

			// setup cursor ...
			Vector2 cursor = GetLeftUpPos(m_Widget.GetOrigPos(), scale);
			float lineBegin = cursor.x;
			cursor.y += lineHeight*scale.y*0.5f; // textHeight*0.5f;

			bool multiline = IsMultiline(text);

			if (multiline == true)
			{
				cursor = SetupCursorForTextAlign(cursor, text, 0, m_Alignment, font, lineBegin, lineWidth, widthMult*scale.x);
			}

			for (int i = 0; i < text.Length; ++i)
			{
				int character = text[i];
				switch (character)
				{
				case '\n':
					if (multiline == true)
					{
						cursor = SetupCursorForTextAlign(cursor, text, i + 1, m_Alignment, font, lineBegin, lineWidth, widthMult*scale.x);
					}
					else
					{
						cursor.x = lineBegin;
					}

					cursor.y = cursor.y + (lineHeight + lineSpace*texHeight)*scale.y;
					m_Widget.SetTextureCoords(i, 0.0f, 0.0f, 0.0f, 0.0f);
					m_Widget.UpdateSpritePosAndSize(i, 0, -Screen.height, 1.0f, 1.0f);
					m_Widget.ShowSprite(i, false);
					break;
				default:
				{
					if (useFontEx == false)
					{
						Vector2 inTexPos = new Vector2();
						Vector2 inTexSize = new Vector2();
						float width;

						GUIBase_Font fontOld = font as GUIBase_Font;
						fontOld.GetCharDscr(character, out width, ref inTexPos, ref inTexSize);
						width *= widthMult;

						int texU = (int)(texWidth*inTexPos.x);
						int texV = (int)(texHeight*inTexPos.y);
						int texW = (int)(texWidth*inTexSize.x);
						int texH = (int)(texHeight*inTexSize.y);

						cursor.x += 0.5f*width*scale.x;
						m_Widget.SetTextureCoords(i, texU, texV, texW, texH);
						m_Widget.UpdateSpritePosAndSize(i, cursor.x, cursor.y, width, lineHeight);
						cursor.x += 0.5f*width*scale.x;
					}
					else
					{
						float width;
						Rect spriteRect, texRect;

						GUIBase_FontEx fontEx = font as GUIBase_FontEx;

						if (fontEx.GetCharDescription(text[i], out width, out spriteRect, out texRect, false, false))
						{
							width *= widthMult;

							Vector2 inCharSize = new Vector2(spriteRect.width*scale.x, spriteRect.height /*****/*texHeight*scale.y);
											// TODO :: rewite without texHeight...
							Vector2 inCharCenter = cursor + new Vector2(spriteRect.center.x*scale.x, 0.0f);

							m_Widget.SetTextureCoords(i, (int)texRect.x, (int)texRect.y, (int)texRect.width, (int)texRect.height);
							m_Widget.UpdateSpritePosAndSize(i, inCharCenter.x, inCharCenter.y, inCharSize.x, inCharSize.y);
							cursor.x += width*scale.x;
						}
					}
					break;
				}
				}
			}
		}

		// we have to force widget update.
		m_RegenerationNeeded = false;
		m_Widget.SetModify();
	}

	#endregion GUIBase_Wifget interaction

	//---------------------------------------------------------
	Rect GetRect()
	{
#if UNITY_EDITOR
		GUIBase_Widget widget = Widget != null ? Widget : GetComponent<GUIBase_Widget>();
#else
		GUIBase_Widget widget = Widget;
#endif
		if (widget == null)
			return default(Rect);

		Transform trans = transform;
		Vector3 lossyScale = trans.lossyScale;
		Vector3 pos = GetLeftUpPos(trans.position, widget.GetWidth(), widget.GetHeight(), lossyScale);
		float width = widget.GetWidth()*lossyScale.x;
		float height = widget.GetHeight()*lossyScale.y;
		return new Rect(
						pos.x,
						pos.y,
						width,
						height
						);
	}

#if UNITY_EDITOR
	void OnDrawGizmosSelected()
	{
		GUIBase_Widget widget = Widget != null ? Widget : GetComponent<GUIBase_Widget>();
		if (widget == null)
			return;
		if (widget.Visible == false)
			return;

		Rect rect = GetRect();

		GuiBaseUtils.RenderRect(rect, Color.gray);

		if (m_Boundaries.IsEmpty() == false)
		{
			GuiBaseUtils.RenderRect(m_Boundaries.Intersect(rect), Color.white);
		}
	}
#endif

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
#endif
		// ignore same text...
		if (m_TextID == 0 && inText == m_Text)
			return;

		Clear();

		m_TextID = 0;
		m_TextIDGenerated = 0;

		if (inText != null)
			m_Text = inText;

		SetRegenerationNeeded();
	}

	public void SetNewText(int inTextID)
	{
		// ignore same text...
		if (m_TextID == inTextID)
			return;

		Clear();

		m_TextID = inTextID;
		m_TextIDGenerated = 0;

		SetRegenerationNeeded();
	}

	public string GetText()
	{
		if (m_TextID != 0)
			return m_AllowMultiline == true ? TextDatabase.instance[m_TextID] : TextDatabase.instance[m_TextID].Replace("\n", " ");

		return m_Text;
	}

	public void Clear()
	{
		if (m_TextID > 0 ||
			m_TextIDGenerated > 0 ||
			string.IsNullOrEmpty(m_Text) == false ||
			string.IsNullOrEmpty(m_TextDyn) == false)
		{
			m_TextID = 0;
			m_TextIDGenerated = -1;
			m_TextDyn = "";
			m_Text = "";
			SetRegenerationNeeded();
		}
	}

	#endregion GUIBase_Label interface

	// ==================================================================================================
	// === Text Support Functions =======================================================================

	#region Text Support Functions

	public static bool IsMultiline(string inText)
	{
		return (inText.IndexOf('\n') >= 0);
	}

	static float GetCurentTextLineWidth(string inText, int inStartIndex, GUIBase_FontBase inFont)
	{
		// Get current line....
		int index = inText.IndexOf('\n', inStartIndex);
		int count = (index == -1) ? inText.Length - inStartIndex : index - inStartIndex;
		if (count <= 0)
			return -1;

		string subString = inText.Substring(inStartIndex, count);

		// compute size of line...
		return GetLineWidth(subString, inFont);
	}

	static float GetLineWidth(string subString, GUIBase_FontBase inFont)
	{
		float lineWidth = 0;
		GUIBase_FontEx fontEx = inFont as GUIBase_FontEx;
		GUIBase_Font font = inFont as GUIBase_Font;
		if (fontEx != null)
		{
			for (int i = 0; i < subString.Length; ++i)
			{
				lineWidth += fontEx.GetCharWidth((int)subString[i]);
			}
		}
		else
		{
			for (int i = 0; i < subString.Length; ++i)
			{
				lineWidth += font.GetCharWidth((int)subString[i]);
			}
		}
		return lineWidth;
	}

	Vector2 GetTextSize()
	{
		Vector2 charPos = Vector2.zero;
		Vector2 charSize = Vector2.zero;
		Vector2 lineSize = Vector2.zero;
		Vector2 totalSize = Vector2.zero;
		int numOfLine = 1;
		float width = 0;

		string text = Text;
		if (string.IsNullOrEmpty(text) == false)
		{
			if (m_Uppercase)
			{
				text = text.ToUpper();
			}

			for (int i = 0; i < text.Length; ++i)
			{
				int character = text[i];
				switch (character)
				{
				case '\n':
					totalSize.x = Mathf.Max(totalSize.x, lineSize.x);
					lineSize.x = 0;
					numOfLine++;
					break;
				default:
					if (useFontEx == false)
					{
						charSize = Vector2.zero;
						width = 0;
						GUIBase_Font fontOld = font as GUIBase_Font;
						if (fontOld.GetCharDscr(character, out width, ref charPos, ref charSize))
						{
							lineSize.x += width;
							lineSize.y = Mathf.Max(lineSize.y, charSize.y);
						}
					}
					else
					{
						Rect screenRect, sourceRect;
						width = 0;
						GUIBase_FontEx fontEx = font as GUIBase_FontEx;
						if (fontEx.GetCharDescription(text[i], out width, out screenRect, out sourceRect, true, false))
						{
							lineSize.x += width;
							lineSize.y = Mathf.Max(lineSize.y, screenRect.height);
						}
					}
					break;
				}
			}
		}

		m_LineSize = lineSize;
		totalSize.x = Mathf.Max(totalSize.x, lineSize.x);
		m_LineSize.x = totalSize.x;
		totalSize.y = lineSize.y*numOfLine + (numOfLine - 1)*lineSpace;
		return totalSize;
	}

	#endregion Text Support Functions

	// ==================================================================================================
	// === editor support ===============================================================================

	#region editor support

	internal bool IsDataRegenerationNeaded()
	{
		if (m_RegenerationNeeded == true)
			return true;

#if UNITY_EDITOR
		if (Application.isPlaying == true)
			return false;

		if (m_TextID != m_TextIDGenerated)
			return true;

		// test if generated text is same as requested.
		if (m_TextID == 0 && Text != m_TextGenerated)
			return true;

		if (m_AnchorPointGen != (int)m_AnchorPoint)
			return true;

		if (m_AlignmentGen != (int)m_Alignment)
			return true;

		if (m_LineSpaceGenerated != m_LineSpace2)
			return true;

		if (m_ReloadCount != TextDatabase.instance.reloadCount)
			return true;

		if (useFontEx != m_UseFontExGen)
			return true;
#endif
		return false;
	}

	//---------------------------------------------------------
	public bool GenerateRunTimeData()
	{
		bool regenerated = IsDataRegenerationNeaded();
		if (regenerated)
		{
			//RebuildRuntimeData();
			if (font == null)
			{
				Debug.LogWarning("GUIBase_Label have not a font assigned " + MFDebugUtils.GetFullName(gameObject));
			}
			else
			{
				GUIBase_Widget widget = GetComponent<GUIBase_Widget>();

				m_TextDyn = null;
				m_TextIDGenerated = 0;
				if (m_TextID != 0)
				{
					m_TextDyn = m_AllowMultiline == true ? TextDatabase.instance[m_TextID] : TextDatabase.instance[m_TextID].Replace("\n", " ");
#if UNITY_EDITOR
					if (m_TextDyn == "<UNKNOWN TEXT>")
						Debug.Log("Invalid text id on gameobject " + gameObject.GetFullName());
#endif

					m_TextIDGenerated = m_TextID;
				}
#if UNITY_EDITOR
				m_ReloadCount = TextDatabase.instance.reloadCount;
#endif

				Texture2D texture = fontTexture;
				float texHeight = texture.height;
				Vector2 textSize = GetTextSize();

				widget.SetScreenSize(textSize.x, textSize.y*texHeight);
#if UNITY_EDITOR
				m_AnchorPointGen = (int)m_AnchorPoint;
				m_AlignmentGen = (int)m_Alignment;
				m_LineSpaceGenerated = m_LineSpace2;
				m_UseFontExGen = useFontEx;
#endif
			}
		}

		return regenerated;
	}

	#endregion editor support

	// ==================================================================================================
	// === Text Rendering support =======================================================================

	#region Text Rendering support

//	public Vector3 GetLeftUpPos()
//	{	return GetLeftUpPos(m_Widget, transform.position);	}
	public Vector3 GetLeftUpPos(Vector3 inRefPoint)
	{
		return GetLeftUpPos(inRefPoint, Widget.GetWidth(), Widget.GetHeight(), Vector3.one);
	}

	public Vector3 GetLeftUpPos(Vector3 inRefPoint, Vector3 scale)
	{
		return GetLeftUpPos(inRefPoint, Widget.GetWidth(), Widget.GetHeight(), scale);
	}

	public Vector3 GetLeftUpPos(GUIBase_Widget inWidget)
	{
		Transform trans = transform;
		return GetLeftUpPos(trans.position, inWidget.GetWidth(), inWidget.GetHeight(), trans.lossyScale);
	}

	public Vector3 GetLeftUpPos(GUIBase_Widget inWidget, Vector3 scale)
	{
		Transform trans = transform;
		return GetLeftUpPos(trans.position, inWidget.GetWidth(), inWidget.GetHeight(), scale);
	}

	public Vector3 GetLeftUpPos(Vector3 inRefPoint, float width, float height, Vector3 inScale)
	{
		Vector3 widgetSize = new Vector3(width*inScale.x, height*inScale.y, 0);

		Vector3 leftUpPos = inRefPoint;
		switch (m_AnchorPoint)
		{
		case TextAnchor.UpperLeft:
			leftUpPos.x -= (widgetSize.x*0.0f);
			leftUpPos.y -= (widgetSize.y*0.0f);
			break;
		case TextAnchor.UpperCenter:
			leftUpPos.x -= (widgetSize.x*0.5f);
			leftUpPos.y -= (widgetSize.y*0.0f);
			break;
		case TextAnchor.UpperRight:
			leftUpPos.x -= (widgetSize.x*1.0f);
			leftUpPos.y -= (widgetSize.y*0.0f);
			break;
		case TextAnchor.MiddleLeft:
			leftUpPos.x -= (widgetSize.x*0.0f);
			leftUpPos.y -= (widgetSize.y*0.5f);
			break;
		case TextAnchor.MiddleCenter:
			leftUpPos.x -= (widgetSize.x*0.5f);
			leftUpPos.y -= (widgetSize.y*0.5f);
			break;
		case TextAnchor.MiddleRight:
			leftUpPos.x -= (widgetSize.x*1.0f);
			leftUpPos.y -= (widgetSize.y*0.5f);
			break;
		case TextAnchor.LowerLeft:
			leftUpPos.x -= (widgetSize.x*0.0f);
			leftUpPos.y -= (widgetSize.y*1.0f);
			break;
		case TextAnchor.LowerCenter:
			leftUpPos.x -= (widgetSize.x*0.5f);
			leftUpPos.y -= (widgetSize.y*1.0f);
			break;
		case TextAnchor.LowerRight:
			leftUpPos.x -= (widgetSize.x*1.0f);
			leftUpPos.y -= (widgetSize.y*1.0f);
			break;
		}

		return leftUpPos;
	}

	public static Vector3 SetupCursorForTextAlign(Vector3 inCursor,
												  string inText,
												  int inStartIndex,
												  TextAlignment inAlignment,
												  GUIBase_FontBase inFont,
												  float inLineBegin,
												  float inMaxlineWidth,
												  float inScale)
	{
		Vector3 cursor = inCursor;
		cursor.x = inLineBegin;

		if (inAlignment != TextAlignment.Left)
		{
			float curLineWidth = GetCurentTextLineWidth(inText, inStartIndex, inFont);
			float span = (inMaxlineWidth - curLineWidth)*inScale;

			if (inAlignment == TextAlignment.Center)
			{
				cursor.x += span*0.5f;
			}
			else if (inAlignment == TextAlignment.Right)
			{
				cursor.x += span;
			}
		}

		/*
		switch (inAlignment)
		{
			case TextAlignment.Left:
				cursor.x = inLineBegin;
				break;
			case TextAlignment.Center:
			{
				float curLineWidth = GetCurentTextLineWidth(inText, inStartIndex, m_Font);
				cursor.x = inLineBegin + (inMaxlineWidth - curLineWidth)*0.5f*inScale;
				break;
			}
			case TextAlignment.Right:
			{
				float curLineWidth = GetCurentTextLineWidth(inText, inStartIndex, m_Font);
				cursor.x = inLineBegin + (inMaxlineWidth - curLineWidth)*inScale;
				break;
			}
		}*/

		return cursor;
	}

	public void OnLanguageChanged(string inNewLanguage)
	{
		if (m_Widget == null)
			return;

		if ("English.Old" == inNewLanguage)
		{
			ChangeFont(MFFontManager.GetFont("Default"));
		}
		else
		{
			string font_name = m_FontName;
			if (m_FontName == "Default")
				font_name = "NewFont";

			ChangeFont(MFFontManager.GetFont(font_name));
		}
	}

	void ChangeFont(GUIBase_FontBase inNewFont)
	{
		// destroy old text if any exist...
		m_FontXXX = inNewFont;
		SetRegenerationNeeded();

#if UNITY_EDITOR
		// this code can be only in editor. In real game m_Widget property has to be setup in start function...
		m_Widget = m_Widget ?? GetComponent<GUIBase_Widget>();
#endif

		m_Widget.ChangeMaterial(m_FontXXX.fontMaterial);
	}

	IEnumerator GenerateRandomText_Coroutine(int inMinSize, int inMaxSize)
	{
		while (true)
		{
			yield return new WaitForSeconds(Random.Range(1, 3));
			yield return new WaitForFixedUpdate();
			SetNewText(MFDebugUtils.GetRandomString(Random.Range(inMinSize, inMaxSize)));
		}
	}

	#endregion Text Rendering support

	void SetRegenerationNeeded()
	{
		m_RegenerationNeeded = true;
		if (m_Widget != null)
		{
			m_Widget.SetModify();
		}
	}
}
