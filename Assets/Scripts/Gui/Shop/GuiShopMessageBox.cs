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

//Zobrazi text a caption, ma ok button ktery jde zakazat.
public class GuiShopMessageBox : GuiPopupAnimatedBase
{
	GUIBase_Button m_OKButton;
	GUIBase_Label m_StatusLabel;
	GUIBase_Label m_CaptionLabel;

	public override bool CanCloseByEscape
	{
		get { return m_OKButton.Widget.Visible && !m_OKButton.IsDisabled; }
	}

	public override void SetCaption(string inCaption)
	{
		m_CaptionLabel.SetNewText(inCaption);
	}

	public override void SetText(string inText)
	{
		m_StatusLabel.SetNewText(inText);
	}

	protected override void OnViewInit()
	{
		m_ScreenPivot = MFGuiManager.Instance.GetPivot("ShopPopups");
		m_ScreenLayout = m_ScreenPivot.GetLayout("MessageBox_Layout");

		base.OnViewInit();

		m_OKButton = PrepareButton(m_ScreenLayout, "OK_Button", null, OnButtonOK);
		m_StatusLabel = PrepareLabel(m_ScreenLayout, "Text_Label");
		m_CaptionLabel = PrepareLabel(m_ScreenLayout, "Caption_Label");
	}

	void OnButtonOK(GUIBase_Widget inWidget)
	{
		Owner.Back();
		SendResult(E_PopupResultCode.Ok);
	}
}
