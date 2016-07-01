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

[AddComponentMenu("GUI/Widgets/ProgressBar")]
public class GUIBase_ProgressBar : GUIBase_Callback
{
	const float m_MinValue = 0.0f;
	const float m_MaxValue = 1.0f;
	public float m_InitValue = 1.0f;
	float m_CurrentValue;

	Animation m_Anim;

	// bar
	public GUIBase_Sprite m_BarSprite;
	float barWidth;
	float barHeight;

	GUIBase_Widget m_Widget;

	//---------------------------------------------------------
	public float CurentValue
	{
		get { return m_CurrentValue; }
	}

	//---------------------------------------------------------
	public void Start()
	{
		m_Widget = GetComponent<GUIBase_Widget>();
		m_Anim = GetComponent<Animation>();

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
		// prepare sprite for slider bar
		Transform trans = transform;
		Vector3 pos = trans.position;
		Vector3 scale = trans.lossyScale;
		Vector3 rot = trans.eulerAngles;

		barWidth = m_Widget.GetWidth();
		barHeight = m_Widget.GetHeight();

		Texture texture = m_Widget.GetTexture();

		int texWidth = 1;
		int texHeight = 1;

		if (texture)
		{
			texWidth = texture.width;
			texHeight = texture.height;
		}

		int texU = 0;
		int texV = 0;
		int texW = 1;
		int texH = 1;

		if (m_BarSprite)
		{
			GUIBase_Widget barW = m_BarSprite.Widget;

			if (barW)
			{
				barWidth = barW.GetWidth();
				barHeight = barW.GetHeight();

				texU = (int)(texWidth*barW.m_InTexPos.x);
				texV = (int)(texHeight*barW.m_InTexPos.y);
				texW = (int)(texWidth*barW.m_InTexSize.x);
				texH = (int)(texHeight*barW.m_InTexSize.y);

				m_Widget.AddSprite(new Vector2(pos.x, pos.y), barWidth, barHeight, scale.x, scale.y, rot.z, texU, texV + texH, texW, texH);
			}
		}

		// Set initial value
		SetValue(m_InitValue);

		// Hide bar sprite
		m_Widget.ShowSprite(1, false);
	}

	//-----------------------------------------------------
	public void SetValue(float v)
	{
		// Show bar sprite
		m_Widget.ShowSprite(1, true);

		m_CurrentValue = Mathf.Clamp(v, m_MinValue, m_MaxValue);

		// Set new position and size of sprite

		Transform trans = transform;
		Vector3 pos = trans.position;
		Vector3 scale = trans.lossyScale;

		float f = (m_CurrentValue - m_MinValue)/(m_MaxValue - m_MinValue);

		float posX = pos.x;
		float posY = pos.y;
		float partialWidth = (barWidth*f);

		posX -= barWidth*0.5f*scale.x;
		posX += partialWidth*0.5f*scale.x;

		m_Widget.UpdateSpritePosAndSize(1, posX, posY, partialWidth, barHeight);
	}

	public void SetBarColor(Color c)
	{
		m_Widget.Color = c;
	}

	public void PlayAnimClip(AnimationClip clip)
	{
		if (clip != null)
		{
			m_Anim.clip = clip;
			m_Widget.PlayAnim(m_Anim, m_Widget);
		}
	}

	public void StopAnimClip()
	{
		if (m_Anim.clip != null)
		{
			m_Widget.StopAnim(m_Anim);
		}
	}
}
