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
public class GuiShopStatusIAP : GuiPopup
{
	GUIBase_Label m_StatusLabel;
	GUIBase_Label m_CaptionLabel;
	GUIBase_Button m_CloseButton;

	E_PopupResultCode m_Result;
	public ShopItemId BuyIAPItem { get; set; }

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
		base.OnViewInit();

		m_ScreenPivot = MFGuiManager.Instance.GetPivot("ShopPopups");
		m_ScreenLayout = m_ScreenPivot.GetLayout("Wait_Layout");

		m_StatusLabel = PrepareLabel(m_ScreenLayout, "Text_Label");
		m_CaptionLabel = PrepareLabel(m_ScreenLayout, "Caption_Label");
		m_CloseButton = PrepareButton(m_ScreenLayout, "OK_Button", null, null);
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();
#if IAP_USE_MFLIVE
		m_CloseButton.Widget.Show(true, true);
#else
		m_CloseButton.Widget.Show(false, false);
#endif
	}

	protected override void OnViewUpdate()
	{
		if (IsVisible)
		{
			if (ShopDataBridge.Instance.IsIAPInProgress())
			{
				switch (ShopDataBridge.Instance.GetIAPState())
				{
				case InAppAsyncOpState.Waiting:
					return;
				case InAppAsyncOpState.Finished:
					//IAP success, fetch ppi with new golds
					FetchPlayerPersistantInfo action = new FetchPlayerPersistantInfo(CloudUser.instance.authenticatedUserID);
					GameCloudManager.AddAction(action);
					//Debug.Log("IAP finished: " + BuyIAPItem );
					CloseDialog(E_PopupResultCode.Success, 02030048);
					return;
				case InAppAsyncOpState.Failed:
					CloseDialog(E_PopupResultCode.Failed, 02030049);
					return;
				case InAppAsyncOpState.Cancelled:
					CloseDialog(E_PopupResultCode.Cancel, 02030050);
					return;
				case InAppAsyncOpState.CannotVerify:
					CloseDialog(E_PopupResultCode.Failed, 02030072);
					return;
				}
			}
		}

		base.OnViewUpdate();
	}

	void CloseDialog(E_PopupResultCode res, int msgTextId)
	{
		//Debug.Log("IAP Close Dialog: " + TextDatabase.instance[msgTextId]);

		//store result
		m_Result = res;

		//clean request
		ShopDataBridge.Instance.IAPCleanRequest();

#if !IAP_USE_MFLIVE //when using MFLive, we don't know the real result
		//show result and button for dialog close
		m_CaptionLabel.SetNewText(TextDatabase.instance[02030046]);
		m_StatusLabel.SetNewText(TextDatabase.instance[msgTextId]);
#endif
		m_CloseButton.Widget.Show(true, true);
		m_CloseButton.RegisterReleaseDelegate(OnCloseButton);
	}

	void OnCloseButton(bool inside)
	{
		if (!inside)
			return;

		Owner.Back(); //hide wait dialog
		SendResult(m_Result);
		BuyIAPItem = ShopItemId.EmptyId;

#if IAP_USE_MFLIVE //when using MFLive the real purchase can be delayed, so we check for PPI update later
		PPIManager.Instance.UpdateFromCloud();
		PPIManager.Instance.UpdateFromCloudDelayed(15);
		PPIManager.Instance.UpdateFromCloudDelayed(60);
#endif
	}
}
