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

[AddComponentMenu("GUI/Widgets/Counter")]
public class GUIBase_Counter : GUIBase_Callback
{
	public int m_MaxCount = 3; // for example 'max count of stars'
	public GUIBase_Sprite[] m_UsedSprites = new GUIBase_Sprite[1];

	GUIBase_Widget m_Widget;
	//MFGuiRenderer			m_GuiRenderer;
	public GUIBase_Widget Widget
	{
		get { return m_Widget; }
	}

	public struct S_SpriteUV
	{
		public Vector2 m_LowerLeftUV;
		public Vector2 m_UvDimensions;
	};

	S_SpriteUV[] m_UsedSpritesUV = new S_SpriteUV[1];
	static int MAX_COUNT = 10;

	//---------------------------------------------------------
	public void Start()
	{
		m_Widget = GetComponent<GUIBase_Widget>();

		int flags = (int)E_CallbackType.E_CT_INIT;

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
		}

		return true;
	}

	//---------------------------------------------------------
	void CustomInit()
	{
		//m_GuiRenderer	= m_Widget.GetGuiRenderer();

		m_MaxCount = Mathf.Clamp(m_MaxCount, 1, MAX_COUNT);

		Transform trans = transform;
		Vector3 scale = trans.lossyScale;
		float width = m_Widget.GetWidth()/m_MaxCount;
		float height = m_Widget.GetHeight();

		float halfWidth = m_Widget.GetWidth()*scale.x/2.0f;
		Vector3 deltaPos = new Vector3(halfWidth, 0.0f, 0.0f);
		Vector2 d = new Vector2();

		d = deltaPos/(m_MaxCount*0.5f);

		Vector3 leftPos = new Vector3();
		leftPos = trans.position - deltaPos;

		for (int idx = 0; idx < m_MaxCount; ++idx)
		{
			m_Widget.AddSprite(new Vector2(leftPos.x + (idx + 0.5f)*d.x, leftPos.y + (idx + 0.5f)*d.y),
							   width,
							   height,
							   scale.x,
							   scale.y,
							   0.0f,
							   0,
							   0,
							   1,
							   1);
		}

		// Prepare UV for sprites

		if (m_UsedSprites.Length > 0)
		{
			// prepare array for used sprites
			m_UsedSpritesUV = new S_SpriteUV[m_UsedSprites.Length];

			for (int i = 0; i < m_UsedSprites.Length; ++i)
			{
				if (m_UsedSprites[i])
				{
					GUIBase_Widget tmpW = m_UsedSprites[i].GetComponent<GUIBase_Widget>();

					if (tmpW)
					{
						float UVLeft;
						float UVTop;
						float UVWidth;
						float UVHeight;

						tmpW.GetTextureCoord(out UVLeft, out UVTop, out UVWidth, out UVHeight);

						m_UsedSpritesUV[i].m_LowerLeftUV = new Vector2(UVLeft, 1.0f - (UVTop + UVHeight));
						m_UsedSpritesUV[i].m_UvDimensions = new Vector2(UVWidth, UVHeight);
					}
				}
			}
		}
	}

	//---------------------------------------------------------
	public void SetValue(int idx, int type)
	{
		if (idx >= 0 && idx < m_MaxCount)
		{
			// set correct UV 
			MFGuiSprite s = m_Widget.GetSprite(idx + 1);

			if (s != null)
			{
				if (type == -1)
				{
					// TODO - nerenderovat nic
					//s.lowerLeftUV	= usedSpritesUV[0].lowerLeftUV;
					//s.uvDimensions	= usedSpritesUV[0].uvDimensions;
				}
				else if (type >= 0 && type < m_UsedSprites.Length)
				{
					s.uvCoords = new MFGuiUVCoords(m_UsedSpritesUV[idx].m_LowerLeftUV, m_UsedSpritesUV[idx].m_UvDimensions);
				}
			}
		}
	}

	//---------------------------------------------------------
	//Slouzi pro binarni counter (on/ off sprite). Sprity na pozicich mensi nez vlozena hodnota se zobrazi jako typ 1(full), sprity od hodnoty nahoru jako typ 0 (empty)
	public void SetCountSimple(int val)
	{
		for (int i = 0; i < m_MaxCount; i++)
		{
			SetValue(i, (i < val) ? 1 : 0); //pro hodnoty mensi nez val zobraz typ 1 , pro zbytek 0
		}
	}

	//---------------------------------------------------------
	// Editor only!
	public GUIBase_Widget GetSpriteWidget(int idx)
	{
		if (idx >= 0 && idx < m_UsedSprites.Length)
		{
			if (m_UsedSprites[idx])
			{
				GUIBase_Widget w = m_UsedSprites[idx].Widget;

				return w;
			}
		}

		return null;
	}
}
