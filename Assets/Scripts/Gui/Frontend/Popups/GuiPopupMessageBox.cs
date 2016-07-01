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

public class GuiPopupMessageBox : GuiPopupAnimatedBase
{
	GUIBase_Button m_Button;
	GUIBase_Label m_Text;
	GUIBase_Label m_Caption;

	public override bool CanCloseByEscape
	{
		get { return m_Button.Widget.Visible && !m_Button.IsDisabled; }
	}

	public override void SetCaption(string inCaption)
	{
		m_Caption.SetNewText(inCaption);
	}

	public override void SetText(string inText)
	{
		m_Text.SetNewText(inText);
	}

	public void SetButtonText(string text)
	{
		m_Button.SetNewText(text);
	}

	public void SetButtonVisible(bool state)
	{
		m_Button.Widget.Show(state, true);
	}

	public void SetButtonEnabled(bool state)
	{
		m_Button.SetDisabled(!state);
	}

	protected override void OnViewInit()
	{
		base.OnViewInit();

		if (m_ScreenLayout == null)
		{
			Debug.LogError("GuiPopupMessageBox<" + name + "> :: There is not any layout specified for message box!");
			return;
		}

		m_Button = PrepareButton(m_ScreenLayout, "OK_Button", null, OnButtonOK);
		m_Text = PrepareLabel(m_ScreenLayout, "Text_Label");
		m_Caption = PrepareLabel(m_ScreenLayout, "Caption_Label");
	}

	void OnButtonOK(GUIBase_Widget inWidget)
	{
		if (Owner != null)
		{
			Owner.Back();
		}
		SendResult(E_PopupResultCode.Ok);
	}
}
