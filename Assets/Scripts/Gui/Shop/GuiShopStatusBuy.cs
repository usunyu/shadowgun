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
public class GuiShopStatusBuy : GuiPopupAnimatedBase
{
	GUIBase_Label m_StatusLabel;
	GUIBase_Label m_CaptionLabel;

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
		m_ScreenLayout = m_ScreenPivot.GetLayout("Wait_Layout");

		base.OnViewInit();

		m_StatusLabel = PrepareLabel(m_ScreenLayout, "Text_Label");
		m_CaptionLabel = PrepareLabel(m_ScreenLayout, "Caption_Label");
	}

	protected override void OnViewUpdate()
	{
		if (IsVisible)
		{
			if (GuiShopBuyPopup.Instance.BuyActionStatus == BaseCloudAction.E_Status.Success)
			{
				CloseDialog();
				SendResult(E_PopupResultCode.Success);
			}
			else if (GuiShopBuyPopup.Instance.BuyActionStatus == BaseCloudAction.E_Status.Failed)
			{
				CloseDialog();
				SendResult(E_PopupResultCode.Failed);
			}
		}

		base.OnViewUpdate();
	}

	void CloseDialog()
	{
		//Debug.Log("ClosingDialog");
		Owner.Back();
	}
}
