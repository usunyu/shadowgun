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

[AddComponentMenu("")]
public class GUIBase_Font : GUIBase_FontBase
{
	//---------------------------------------------------------------------------------
	[System.Serializable]
	public class C_CharDscr
	{
		public int m_Idx;
		public float m_Width;
		public float m_CX;
		public float m_CY;
		public float m_CW;
		public float m_CH;

		public C_CharDscr(int cIdx, float width, float cx, float cy, float cw, float ch)
		{
			m_Idx = cIdx;
			m_Width = width;
			m_CX = cx;
			m_CY = cy;
			m_CW = cw;
			m_CH = ch;
		}
	}

	//---------------------------------------------------------------------------------
	[System.Serializable]
	public class C_FontDscr
	{
		public C_CharDscr[] m_CharTable;
		public int m_CharMaxWidth;

		bool m_IsInitialized;
		Hashtable m_CharLookUpTable;

		//---------------------------------------------------------------------------------
		public C_FontDscr()
		{
			m_IsInitialized = false;
		}

		//---------------------------------------------------------------------------------
		public void SetCharMaxWidth(int maxWidth)
		{
			m_CharMaxWidth = maxWidth;
		}

		//---------------------------------------------------------------------------------
		public void AddChar(int cIdx, float width, float cx, float cy, float cw, float ch)
		{
			C_CharDscr cDscr = new C_CharDscr(cIdx, width, cx, cy, cw, ch);
			int length = 0;

			if (m_CharTable != null)
			{
				length = m_CharTable.Length;
			}

			C_CharDscr[] tmpArray = new C_CharDscr[length + 1];

			if (length != 0)
			{
				m_CharTable.CopyTo(tmpArray, 0);
			}

			tmpArray[length] = cDscr;
			m_CharTable = tmpArray;
		}

		//---------------------------------------------------------------------------------
		public float GetCharWidth(int cIdx)
		{
			float width = 0.0f;

			if (Initialize())
			{
				if (m_CharLookUpTable != null && m_CharLookUpTable.ContainsKey(cIdx))
				{
					width = ((C_CharDscr)m_CharLookUpTable[cIdx]).m_Width*m_CharMaxWidth;
				}
			}

			//Debug.Log("width of '" + cIdx + "' = " + width);

			return width;
		}

		//---------------------------------------------------------------------------------
		public float GetCharHeight(int cIdx)
		{
			float height = 0.0f;

			if (Initialize())
			{
				if (m_CharLookUpTable != null && m_CharLookUpTable.ContainsKey(cIdx))
				{
					C_CharDscr cDscr = (C_CharDscr)m_CharLookUpTable[cIdx];

					height = cDscr.m_CH - cDscr.m_CY;
				}
			}

			//Debug.Log("height of '" + cIdx + "' = " + height);

			return height;
		}

		//---------------------------------------------------------------------------------
		//public bool GetCharDscr(int cIdx, out float width, out float height, ref Vector2 inTexPos, ref Vector2 inTexSize)
		public bool GetCharDscr(int cIdx, out float width, ref Vector2 inTexPos, ref Vector2 inTexSize)
		{
			bool res = false;

			width = 0;
			//height = 0;

			if (Initialize())
			{
				if (m_CharLookUpTable.ContainsKey(cIdx))
				{
					C_CharDscr cDscr = (C_CharDscr)m_CharLookUpTable[cIdx];

					width = cDscr.m_Width*m_CharMaxWidth;
					inTexPos.x = cDscr.m_CX;
					inTexPos.y = cDscr.m_CY;
					inTexSize.x = cDscr.m_CW - cDscr.m_CX;
					inTexSize.y = cDscr.m_CH - cDscr.m_CY;
					//height 		= inTexSize.y * 1024; // 1024 is size of our old texture

					res = true;
				}
			}

			return res;
		}

		public bool GetTextSize(string inText, out Vector2 outSize, bool inTreatSpecialChars, char inMissingChar)
		{
			outSize = Vector2.zero;

			if (Initialize() == false)
				return false;

			bool hasMissingCharacter = inMissingChar > 0 ? m_CharLookUpTable.ContainsKey(inMissingChar) : false;

			for (int i = 0; i < inText.Length; i++)
			{
				int ch = inText[i];
				if (m_CharLookUpTable.ContainsKey(ch) == false)
				{
					if (hasMissingCharacter == false)
						continue; // or return false ???

					ch = inMissingChar;
				}

				C_CharDscr cDscr = (C_CharDscr)m_CharLookUpTable[ch];

				outSize.x += cDscr.m_Width*m_CharMaxWidth;
				float h = cDscr.m_CH - cDscr.m_CY;

				if (h > outSize.y)
					outSize.y = h;
			}

			return true;
		}

		//---------------------------------------------------------------------------------
		bool Initialize()
		{
			if (!m_IsInitialized || m_CharLookUpTable == null)
			{
				if (m_CharTable != null && m_CharTable.Length > 0)
				{
					m_CharLookUpTable = new Hashtable();

					for (int i = 0; i < m_CharTable.Length; ++i)
					{
						C_CharDscr charDscr = m_CharTable[i];

						m_CharLookUpTable[charDscr.m_Idx] = charDscr;
					}

					m_IsInitialized = true;
				}
			}

			return m_IsInitialized;
		}
	}

	[SerializeField] public C_FontDscr m_FontDscr;
	[SerializeField] Material m_Material;

	public override Material fontMaterial
	{
		get { return m_Material; }
	}

	//---------------------------------------------------------------------------------
	//
	// GetCharWidth
	//
	//---------------------------------------------------------------------------------
	public float GetCharWidth(int cIdx)
	{
		return m_FontDscr.GetCharWidth(cIdx);
	}

	//---------------------------------------------------------------------------------
	//
	// GetCharHeight
	//
	//---------------------------------------------------------------------------------
	public float GetCharHeight(int cIdx)
	{
		return m_FontDscr.GetCharHeight(cIdx);
	}

	//---------------------------------------------------------------------------------
	//
	// GetCharDscr
	//
	//---------------------------------------------------------------------------------
	//public override bool GetCharDscr(int cIdx, out float width, out float height, ref Vector2 inTexPos, ref Vector2 inTexSize)
	public bool GetCharDscr(int cIdx, out float width, ref Vector2 inTexPos, ref Vector2 inTexSize)
	{
		//return m_FontDscr.GetCharDscr(cIdx, out width, out height, ref inTexPos, ref inTexSize);
		return m_FontDscr.GetCharDscr(cIdx, out width, ref inTexPos, ref inTexSize);
	}

	//---------------------------------------------------------------------------------
	//
	// GetTextSize
	//
	//---------------------------------------------------------------------------------
	public bool GetTextSize(string inText, out Vector2 inSize, bool inTreatSpecialChars)
	{
		return m_FontDscr.GetTextSize(inText, out inSize, inTreatSpecialChars, ' ');
	}
}
