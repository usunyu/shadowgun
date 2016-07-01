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

public class GuiShopNotFundsPopup : GuiPopupAnimatedBase
{
	public static GuiShopNotFundsPopup Instance;
	public ShopItemId AddFundsID;

	void Awake()
	{
		Instance = this;
	}

	public override void SetCaption(string inCaption)
	{
	}

	public override void SetText(string inText)
	{
	}

	protected override void OnViewInit()
	{
		GUIBase_Pivot Pivot = MFGuiManager.Instance.GetPivot("ShopPopups");
		m_ScreenLayout = Pivot.GetLayout("NotFunds_Layout");

		base.OnViewInit();

		GuiBaseUtils.RegisterButtonDelegate(m_ScreenLayout, "Back_Button", null, OnButtonBack);
		GuiBaseUtils.RegisterButtonDelegate(m_ScreenLayout, "Accept_Button", null, OnAddFunds);
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		Debug.Log("Not Funds Show");
		//Show fund info
		if (!ShopDataBridge.Instance.IsIAPFund(AddFundsID))
			Debug.LogError("Selected funds is not IAP: " + AddFundsID);

		ShopItemInfo inf = ShopDataBridge.Instance.GetItemInfo(AddFundsID);
		string strBuy = TextDatabase.instance[02030093];
		strBuy = strBuy.Replace("%d1", inf.AddGold.ToString());

		string productId = FundSettingsManager.Instance.Get((E_FundID)(AddFundsID.Id)).GUID.ToString();

		InAppInventory inventory = InAppPurchaseMgr.Instance.Inventory;
		InAppProduct product = null;

		if (inventory != null)
			product = inventory.Product(productId);

		if (product != null)
						// add price to the buy label string
			strBuy += " (" + product.Price + " " + product.CurrencyCode + ")";

		GUIBase_Label buyLabel = GuiBaseUtils.PrepareLabel(m_ScreenLayout, "Buy_Label");
		buyLabel.SetNewText(strBuy);
	}

	protected override void OnViewHide()
	{
		AddFundsID = ShopItemId.EmptyId;

		base.OnViewHide();
	}

	protected override void OnViewUpdate()
	{
		base.OnViewUpdate();
	}

	protected override void OnViewEnable()
	{
		base.OnViewEnable();
	}

	protected override void OnViewDisable()
	{
		base.OnViewDisable();
	}

	//Close button (zobrazuje se a reaguje na nej kdyz fungujeme jako popup dialog  vyvolany z equip menu)
	void OnButtonBack(bool inside)
	{
		if (!inside)
			return;

		Owner.Back(); //hide buy dialog
		SendResult(E_PopupResultCode.Cancel);
	}

	void OnAddFunds(bool inside)
	{
		if (!inside)
			return;

		//za realne penize kupujeme pouze IAP funds
		bool iap = ShopDataBridge.Instance.IsIAPFund(AddFundsID);
		if (iap)
		{
			if (ShopDataBridge.Instance.IAPServiceAvailable())
			{
				ShopDataBridge.Instance.IAPRequestPurchase(AddFundsID);
#if IAP_USE_MFLIVE
				GuiShopStatusIAP iapPopup = Owner.ShowPopup("ShopStatusIAP", TextDatabase.instance[02900014], TextDatabase.instance[02030097], WaitForAIPurchaseHandler) as GuiShopStatusIAP;
#else
				GuiShopStatusIAP iapPopup =
								Owner.ShowPopup("ShopStatusIAP", TextDatabase.instance[02900014], TextDatabase.instance[02900015], WaitForAIPurchaseHandler) as
								GuiShopStatusIAP;
#endif
				iapPopup.BuyIAPItem = AddFundsID;
			}
			else
			{
				//service not available
				Owner.ShowPopup("ShopMessageBox", TextDatabase.instance[02900016], TextDatabase.instance[02900017], NoIAPServiceHandler);
			}
		}
		else
			Debug.LogError("Fund item is not IAP: " + AddFundsID);
	}

	void WaitForAIPurchaseHandler(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		Owner.Back(); //hide buy dialog
		SendResult(inResult);
	}

	void NoIAPServiceHandler(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		Owner.Back(); //hide buy dialog
		SendResult(E_PopupResultCode.Failed);
	}
};
