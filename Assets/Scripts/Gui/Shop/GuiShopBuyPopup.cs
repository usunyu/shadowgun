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

// Dialog pro potvrzeni koupe itemu (za ingame menu).
class GuiShopBuyPopup : GuiPopupAnimatedBase
{
	public static GuiShopBuyPopup Instance;

	public bool IsShown { get; private set; }

	GUIBase_Label m_Caption_Label;
	GUIBase_Sprite m_Thumbnail;
	GUIBase_Widget m_SaleRoot;
	GUIBase_Label m_Sale_Label;
	GuiShopFunds m_Cost;

	ShopItemId m_BuyItemId = ShopItemId.EmptyId;

	BaseCloudAction m_BuyCloudAction;

	public ShopItemId BuyItemId
	{
		get { return m_BuyItemId; }
	}

	void Awake()
	{
		Instance = this;
		IsShown = false;
	}

	public override void SetCaption(string inCaption)
	{
	}

	public override void SetText(string inText)
	{
	}

	protected override void OnViewInit()
	{
		GUIBase_Pivot pivot = MFGuiManager.Instance.GetPivot("ShopPopups");
		m_ScreenLayout = pivot.GetLayout("Buy_Layout");

		base.OnViewInit();

		m_Caption_Label = GuiBaseUtils.PrepareLabel(m_ScreenLayout, "Caption_Label");
		GuiBaseUtils.RegisterButtonDelegate(m_ScreenLayout, "Back_Button", null, OnCloseButton);
		GuiBaseUtils.RegisterButtonDelegate(m_ScreenLayout, "Accept_Button", null, OnAcceptButton);
		m_SaleRoot = GetWidget("SaleRoot");
		m_Sale_Label = GuiBaseUtils.PrepareLabel(m_ScreenLayout, "Sale_Label");
		m_Cost = new GuiShopFunds(GuiBaseUtils.PrepareSprite(m_ScreenLayout, "Cost_Sprite"));
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		if (m_BuyItemId == ShopItemId.EmptyId)
		{
			Debug.LogError("Call SetBuyItem with valid item id first!");
			return;
		}

		ShopItemInfo inf = ShopDataBridge.Instance.GetItemInfo(m_BuyItemId);

		//different caption for differen confirmantion (buy, upgrade, add gold, convert gold)
		int idBuy = 02030020;
		int idAddFunds = 02030036;

		//format caption, different for adding funds
		string str1 = (m_BuyItemId.ItemType == GuiShop.E_ItemType.Fund) ? TextDatabase.instance[idAddFunds] : TextDatabase.instance[idBuy];
		string caption = str1 + " " + inf.NameText;

		m_Caption_Label.SetNewText(caption);

		m_Thumbnail = GuiBaseUtils.PrepareSprite(m_ScreenLayout, ThumbName(m_BuyItemId.ItemType));
		m_Thumbnail.Widget.Show(true, false);

		//stejne sprity jako ve scrollbaru
		if (inf.ScrollerWidget != null)
			m_Thumbnail.Widget.CopyMaterialSettings(inf.ScrollerWidget);
		else
			m_Thumbnail.Widget.CopyMaterialSettings(inf.SpriteWidget);

		m_SaleRoot.Show(inf.PriceSale, true);
		if (inf.PriceSale)
		{
			m_Sale_Label.SetNewText(inf.DiscountTag);
		}

		if (m_Cost != null)
		{
			m_Cost.Show(true);
			if (m_BuyItemId.ItemType == GuiShop.E_ItemType.Fund)
			{
				bool isGold = inf.AddGold > 0;
				m_Cost.SetValue(isGold ? inf.AddGold : inf.AddMoney, isGold, true);
			}
			else
				m_Cost.SetValue(inf.Cost, inf.GoldCurrency);
		}
	}

	protected override void OnViewUpdate()
	{
		if (IsVisible)
		{
			//todo: update statusu buy operace?			
		}

		base.OnViewUpdate();
	}

	public void SetBuyItem(ShopItemId itemId)
	{
		m_BuyItemId = itemId;
	}

	void OnCloseButton(bool inside)
	{
		if (!inside)
			return;

		CloseDialog();
		SendResult(E_PopupResultCode.Cancel);
	}

	void OnAcceptButton(bool inside)
	{
		if (!inside)
			return;

		//check if we have enough money

		//if(true) //pro test not funds
		if (!ShopDataBridge.Instance.HaveEnoughMoney(m_BuyItemId, -1))
		{
			//zobrazit not funds popup
			//GuiShopNotFundsPopup.Instance.DesiredItem = m_BuyItemId;
			//Owner.ShowPopup("NotFundsPopup", "", "", NoFundsResultHandler);
			Owner.ShowPopup("ShopMessageBox", TextDatabase.instance[02030091], TextDatabase.instance[02030092], null);
		}
		else
		{
			//TODO_DT - tady by se mohlo rozlisovat mezi asyc koupi a lokalni koupi.
			//spawn server request
			int guid = ShopDataBridge.Instance.GetShopItemGUID(m_BuyItemId);
			m_BuyCloudAction = new BuyAndFetchPPI(CloudUser.instance.authenticatedUserID, guid);
			GameCloudManager.AddAction(m_BuyCloudAction);

			//tohle se mi moc nelibi, vyvolavame wait box a result vlastne ani nepotrebujeme
			Owner.ShowPopup("ShopStatusBuy", TextDatabase.instance[02030020], TextDatabase.instance[02030096], BuyWaitResultHandler);

			//Debug.Log(" Starting buy request: time " + Time.time + " item " + m_BuyItemId);

			//pri lokalni koupi by stacilo poslat jen result success
			//CloseDialog();
			//SendResult(E_PopupResultCode.Success);
		}
	}

	void NoFundsResultHandler(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		//so far nothing to do. (all handled in NoFundsDialog)
		//Debug.Log("Not funds result: " + inResult);
	}

	public BaseCloudAction.E_Status BuyActionStatus
	{
		get { return DeduceActionStatus(m_BuyCloudAction); }
	}

	BaseCloudAction.E_Status DeduceActionStatus(BaseCloudAction action)
	{
		if (action.isDone == false)
			return BaseCloudAction.E_Status.InProggres;
		if (action.isFailed == true)
			return BaseCloudAction.E_Status.Failed;
		if (action.isSucceeded == true)
			return BaseCloudAction.E_Status.Success;
		return BaseCloudAction.E_Status.Pending;
	}

	void BuyWaitResultHandler(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		//Debug.Log("Waiting for buy finished. popup: " + inResult + " action status " + BuyActionStatus);
		if (inResult == E_PopupResultCode.Success)
		{
			CloseDialog();
			SendResult(E_PopupResultCode.Success);
		}
		else
		{
			//buy failed
			string strBuyFailed = TextDatabase.instance[02030042];
			Owner.ShowPopup("ShopMessageBox", "", strBuyFailed, FailMessageBoxHandler);
		}

		m_BuyItemId = ShopItemId.EmptyId;
		m_BuyCloudAction = null;
	}

	void CloseDialog()
	{
		Owner.Back();
	}

	void FailMessageBoxHandler(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		CloseDialog();
		SendResult(E_PopupResultCode.Failed);
	}

	string ThumbName(GuiShop.E_ItemType t)
	{
		switch (t)
		{
		case GuiShop.E_ItemType.Skin:
			return "Thumbnail_Skin";
		case GuiShop.E_ItemType.Hat:
			return "Thumbnail_Hat";
		default:
			return "Thumbnail_Default";
		}
	}
};
