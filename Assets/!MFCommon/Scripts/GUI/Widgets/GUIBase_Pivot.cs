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

[AddComponentMenu("GUI/Hierarchy/Pivot")]
public class GUIBase_Pivot : GUIBase_Element
{
	public AnimationClip m_InAnimation;
	public AnimationClip m_LoopAnimation;
	public AnimationClip m_OutAnimation;

	//
	// Private data
	//

	Animation m_Anim;

	GUIBase_Element[] m_Elements;

	//---------------------------------------------------------
	public void OnElementStart()
	{
		Visible = false;

		m_Anim = GetComponent<Animation>();
	}

	//---------------------------------------------------------
	public void Show(bool show)
	{
		if (show)
		{
			ShowLayouts(true);

			// Run "In" animation
			if (m_InAnimation && m_Anim != null)
			{
				m_Anim.clip = m_InAnimation;
				GuiManager.GetPlatform(this).PlayAnim(m_Anim, null, PivotAnimFinished, (int)GUIBase_Platform.E_SpecialAnimIdx.E_SAI_INANIM);
			}
		}
		else
		{
			// Run "Out" animation
			if (m_OutAnimation && m_Anim != null)
			{
				m_Anim.clip = m_OutAnimation;
				GuiManager.GetPlatform(this).PlayAnim(m_Anim, null, PivotAnimFinished, (int)GUIBase_Platform.E_SpecialAnimIdx.E_SAI_OUTANIM);
			}
			else
			{
				ShowLayouts(false);
			}
		}
	}

	//---------------------------------------------------------
	void PivotAnimFinished(int idx)
	{
		switch ((GUIBase_Platform.E_SpecialAnimIdx)idx)
		{
		case GUIBase_Platform.E_SpecialAnimIdx.E_SAI_INANIM:
			// Run "Loop" animation
			if (m_LoopAnimation && m_Anim != null)
			{
				m_Anim.clip = m_LoopAnimation;
				GuiManager.GetPlatform(this).PlayAnim(m_Anim, null);
			}
			break;
		case GUIBase_Platform.E_SpecialAnimIdx.E_SAI_OUTANIM:
			ShowLayouts(false);
			break;
		}
	}

	//---------------------------------------------------------
	void ShowLayouts(bool show)
	{
		if (m_Elements == null)
		{
			InitElements();
		}

		Visible = show;

		if (m_Elements != null)
		{
			foreach (var element in m_Elements)
			{
				var layout = element as GUIBase_Layout;
				if (layout != null)
				{
					layout.Show(show);
				}
			}
		}
	}

	//---------------------------------------------------------
	void InitElements()
	{
		List<GUIBase_Element> elements = new List<GUIBase_Element>(GetComponentsInChildren<GUIBase_Element>());

		if (elements != null)
		{
			elements.Remove(this);

			if (elements.Count > 0)
			{
				m_Elements = new GUIBase_Element[elements.Count];
				elements.CopyTo(m_Elements, 0);
			}
		}
	}

	//---------------------------------------------------------
	public GUIBase_Layout GetLayout(string layoutName)
	{
		if (m_Elements == null)
		{
			InitElements();
		}

		if (m_Elements != null)
		{
			foreach (var element in m_Elements)
			{
				if (element.name == layoutName && element is GUIBase_Layout)
					return (GUIBase_Layout)element;
			}
		}

		return null;
	}
}
