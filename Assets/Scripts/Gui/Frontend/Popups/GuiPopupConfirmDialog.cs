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

// =====================================================================================================================
// =====================================================================================================================
public class GuiPopupConfirmDialog : GuiPopupAnimatedBase
{
	GUIBase_Button m_OKButton;
	GUIBase_Button m_CancelButton;

	GUIBase_Label m_Caption;
	GUIBase_Label m_Message;

	public override bool CanCloseByEscape
	{
		get { return m_CancelButton.Widget.Visible && !m_CancelButton.IsDisabled; }
	}

	public override void SetCaption(string inCaption)
	{
		m_Caption.SetNewText(inCaption);
	}

	public override void SetText(string inText)
	{
		m_Message.SetNewText(inText);
	}

	protected override void OnViewInit()
	{
		base.OnViewInit();

		if (m_ScreenLayout == null)
		{
			Debug.LogError("GuiConfirmDialog<" + name + "> :: There is not any layout specified for confirm dialog!");
			return;
		}

		m_OKButton = GuiBaseUtils.GetControl<GUIBase_Button>(m_ScreenLayout, "OK_Button");
		m_CancelButton = GuiBaseUtils.GetControl<GUIBase_Button>(m_ScreenLayout, "Cancel_Button");
		m_Caption = PrepareLabel(m_ScreenLayout, "Caption_Label");
		m_Message = PrepareLabel(m_ScreenLayout, "Text_Label");
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		m_OKButton.RegisterReleaseDelegate2(Delegate_OK);
		m_CancelButton.RegisterReleaseDelegate2(Delegate_Cancel);
	}

	protected override void OnViewHide()
	{
		m_OKButton.RegisterReleaseDelegate2(null);
		m_CancelButton.RegisterReleaseDelegate2(null);

		base.OnViewHide();
	}

	protected override void OnViewUpdate()
	{
		if (IsVisible == false)
			return;

		base.OnViewUpdate();
	}

	// #################################################################################################################
	// ###  Delegates  #################################################################################################
	void Delegate_OK(GUIBase_Widget inInstigator)
	{
		Owner.Back();
		SendResult(E_PopupResultCode.Ok);
	}

	void Delegate_Cancel(GUIBase_Widget inInstigator)
	{
		Owner.Back();
		SendResult(E_PopupResultCode.Cancel);
	}
}
