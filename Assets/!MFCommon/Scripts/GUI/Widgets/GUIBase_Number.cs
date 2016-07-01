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

[AddComponentMenu("GUI/Widgets/Number")]
public class GUIBase_Number : GUIBase_Callback
{
	readonly static int MAX_NUMBER_DIGITS = 9;

	public int numberDigits = 1;

	[SerializeField] int m_Value = int.MinValue;
	[SerializeField] bool m_KeepZeros = false;
	[SerializeField] TextAlignment m_Alignment = TextAlignment.Right;

	GUIBase_Widget m_Widget;

	float m_UvLeft;
	float m_UvTop;
	float m_UvWidth;
	float m_UvHeight;

	int m_LastVisibleDigits = 0;

	//---------------------------------------------------------
	public GUIBase_Widget Widget
	{
		get { return m_Widget; }
	}

	public int Value
	{
		get { return m_Value; }
		set { SetNumber(value, 999999); }
	}

	//---------------------------------------------------------
	public void Start()
	{
		m_Widget = GetComponent<GUIBase_Widget>();

		int flags = (int)E_CallbackType.E_CT_INIT + (int)E_CallbackType.E_CT_SHOW;

		m_Widget.RegisterCallback(this, flags);
	}

	//---------------------------------------------------------
	public override bool Callback(E_CallbackType type, object evt)
	{
		switch (type)
		{
		case E_CallbackType.E_CT_INIT:
			CustomInit();
			break;

		case E_CallbackType.E_CT_SHOW:
			SetNumber(m_Value, 999999, true);
			break;
		}

		return true;
	}

#if false
	void Update()
	{
		if (m_Widget != null && m_Widget.Visible == true)
		{
			SetNumber(m_Value, 999999, true);
		}
	}
#endif

	//---------------------------------------------------------
	void CustomInit()
	{
		//m_GuiRenderer	= m_Widget.GetGuiRenderer();

		//
		// Prepare sprites for digits
		//
		numberDigits = Mathf.Clamp(numberDigits, 1, MAX_NUMBER_DIGITS);

		Texture texture = m_Widget.GetTexture();

		int texWidth = 1;
		int texHeight = 1;

		if (texture)
		{
			texWidth = texture.width;
			texHeight = texture.height;
		}

		int texU = (int)(texWidth*m_Widget.m_InTexPos.x);
		int texV = (int)(texHeight*m_Widget.m_InTexPos.y);
		int texW = (int)(texWidth*m_Widget.m_InTexSize.x);
		int texH = (int)(texHeight*m_Widget.m_InTexSize.y);

		Vector3 scale = transform.lossyScale;

		for (int i = 0; i < numberDigits; ++i)
		{
			m_Widget.AddSprite(Vector2.zero, 1.0f, 1.0f, scale.x, scale.y, 0.0f, texU, texV + texH, texW, texH);
		}

		// Read UV info from widget
		m_Widget.GetTextureCoord(out m_UvLeft, out m_UvTop, out m_UvWidth, out m_UvHeight);

		// Hide widget's main sprite forever
	}

	public void SetNumber(int number, int max)
	{
		SetNumber(number, max, false);
	}

	//-----------------------------------------------------
	void SetNumber(int number, int max, bool force)
	{
		if (m_Value == number && false == force)
			return;

		int absNumber = Mathf.Abs(number);

		if (absNumber > max)
			absNumber = max;

		m_Value = number;

		int div1 = 1;
		int div2 = 10;

		int visibleDigits = 0;
		for (int digitIdx = 0; digitIdx < numberDigits; ++digitIdx)
		{
			int rest = (absNumber%div2)/div1;

			if ((absNumber > (div1 - 1)) || (digitIdx == 0) || m_KeepZeros)
			{
				// Show digit
				m_Widget.ShowSprite(digitIdx, true);

				// set correct UV 
				MFGuiSprite s = m_Widget.GetSprite(digitIdx);
				MFGuiUVCoords uvCoords = s.uvCoords;
				uvCoords.U = m_UvLeft + m_UvWidth*rest;
				uvCoords.V = 1.0f - (m_UvTop + m_UvHeight);
				s.uvCoords = uvCoords;

				visibleDigits++;
			}
			else
			{
				// Hide digit
				m_Widget.ShowSprite(digitIdx, false);
			}

			div1 = div2;
			div2 *= 10;
		}

		if (visibleDigits == 0)
			return;
		if (visibleDigits == m_LastVisibleDigits && force == false)
			return;
		m_LastVisibleDigits = visibleDigits;

		Transform trans = transform;
		Vector3 scale = trans.lossyScale;

		float width = m_Widget.GetWidth()/numberDigits;
		float height = m_Widget.GetHeight();
		float halfWidth = m_Widget.GetWidth()*scale.x*0.5f;
		Vector3 deltaPos = new Vector3(halfWidth, 0.0f);
		Vector3 rightPos = m_Widget.GetOrigPos() + deltaPos;

		Vector3 delta;
		switch (m_Alignment)
		{
		case TextAlignment.Left:
			delta = deltaPos/(numberDigits*0.5f);
			rightPos.x -= (numberDigits - visibleDigits)*delta.x;
			break;
		case TextAlignment.Center:
			delta = deltaPos/(visibleDigits*0.5f);
			break;
		default:
			delta = deltaPos/(numberDigits*0.5f);
			break;
		}

		for (int idx = 0; idx < visibleDigits; ++idx)
		{
			m_Widget.UpdateSpritePosAndSize(idx, rightPos.x - (idx + 0.5f)*delta.x, rightPos.y - (idx + 0.5f)*delta.y, width, height);
		}
	}
}
