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

public class GuiResearchItem : IScrollItem
{
	GUIBase_Widget m_RootWidget; //main widget
	//int				m_ButtonId;

	//gui
	GUIBase_Sprite m_Sprite;
	GUIBase_Label m_Label;
	GUIBase_Sprite m_IconSprite;
	int m_TextID;

	public GuiResearchItem(GUIBase_Widget w, GUIBase_Sprite icon, int textID)
	{
		m_RootWidget = w;
		//m_ButtonId 	= id;
		m_IconSprite = icon;
		m_TextID = textID;
		InitGui();
	}

	public override void UpdateItemInfo()
	{
	}

	public override void Show()
	{
		m_RootWidget.Show(true, true);

		m_Label.SetNewText(m_TextID);
		m_Label.Widget.SetModify(true);
		m_Sprite.Widget.CopyMaterialSettings(m_IconSprite.Widget);
	}

	public override void Hide()
	{
		m_RootWidget.Show(false, true);
	}

	void InitGui()
	{
		//Debug.Log("B:" + m_RootWidget.GetFullName());
		m_Sprite = GuiBaseUtils.GetChildSprite(m_RootWidget, "Sprite");
		m_Label = GuiBaseUtils.GetChildLabel(m_RootWidget, "Label");

		//Debug.Log("C:" + m_Label.GetFullName());
	}
}
