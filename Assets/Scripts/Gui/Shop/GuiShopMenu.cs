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

public class GuiShopMenu : GuiScreenMultiPage, IGuiOverlayScreen
{
	[SerializeField] string[] m_TabButtonsNames = new string[0];

	readonly static string BUY_BUTTON = "Buy_Button";
	readonly static string BUY_BUTTON_LABEL = "Buy_Button_Label";

	public GameObject ScrollBarPrefab;

	GUIBase_Button m_BuyButton;
	GUIBase_Label m_Buy_Button_Label;
	GUIBase_Button m_Buy_Premium_Button;

	GuiShopItemScroller m_ShopScroller;

//--------------------------------------------		MONO BEAHVIOUR	
	void Awake()
	{
		//create scrollbar instance
		m_ShopScroller = new GuiShopItemScroller(ScrollBarPrefab);
	}

//------------------------------------   	MULTIPAGE INTERFACE
	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_ScreenPivot = MFGuiManager.Instance.GetPivot("ShopMenu");

		//register buy button (pokud bude mit kazda page vlastni, tak to presunout do page tridy) 
		GUIBase_Layout buyLayout = m_ScreenPivot.GetLayout("InfoScreen_Layout");
		m_BuyButton = GuiBaseUtils.GetButton(buyLayout, BUY_BUTTON);
		m_Buy_Button_Label = GuiBaseUtils.PrepareLabel(buyLayout, BUY_BUTTON_LABEL);
		m_Buy_Premium_Button = GuiBaseUtils.GetButton(buyLayout, "Buy_Premium_Button");

		CloudUser.premiumAcctChanged += OnUserPremiumAcctChanged;
	}

	protected override void OnViewDestroy()
	{
		CloudUser.premiumAcctChanged -= OnUserPremiumAcctChanged;
		base.OnViewDestroy();
	}

	protected override void OnViewShow()
	{
		//bind buttons
		for (int idx = 0; idx < m_TabButtonsNames.Length; ++idx)
		{
			int pageIdx = idx;
			RegisterButtonDelegate(m_TabButtonsNames[pageIdx],
								   () =>
								   {
									   if (CurrentPageIndex != pageIdx)
									   {
										   GotoPage(pageIdx);
									   }
								   },
								   null);
		}

		//RegisterButtonDelegate(FREEGOLD_BUTTON, OnFreeGoldTouch, null);

		//setup button callback and text (this may be moved into separate pages if function will different)
		m_BuyButton.RegisterReleaseDelegate(OnBuyButton);
		m_Buy_Premium_Button.RegisterReleaseDelegate(OnBuyPremiumButton);

		//set label (this may be updated by page)
		m_Buy_Button_Label.SetNewText(2030020); //buy text

		m_ShopScroller.InitGui();
		m_ShopScroller.RegisterOnSelectionDelegate(OnSelectionChange);

		m_ShopScroller.Show();

		//Flurry.logEvent(AnalyticsTag.Shop, true);

		base.OnViewShow();
	}

	protected override void OnViewHide()
	{
		// unbind buttons
		for (int idx = 0; idx < m_TabButtonsNames.Length; ++idx)
		{
			int pageIdx = idx;
			RegisterButtonDelegate(m_TabButtonsNames[pageIdx], null, null);
		}

		m_BuyButton.RegisterReleaseDelegate(null);
		m_Buy_Premium_Button.RegisterReleaseDelegate(null);

		//Flurry.endTimedEvent(AnalyticsTag.Shop);

		m_ShopScroller.Hide();

		base.OnViewHide();
	}

	protected override void OnViewUpdate()
	{
		if (IsVisible)
		{
			m_ShopScroller.Update();
		}

		base.OnViewUpdate();
	}

	protected override void OnViewEnable()
	{
		//Debug.Log("Enable Shop menu");
		m_ScreenPivot.InputEnabled = true;
		m_ShopScroller.EnableControls();
		m_ShopScroller.FadeIn();
#if IAP_USE_MFLIVE //when using MFLive, amount of gold is chosen at the paywall page, so we don't show scroller
		if (CurrentPage.GetType() == typeof(GuiShopPageFunds))
			m_ShopScroller.Hide();
#endif
		base.OnViewEnable();
	}

	protected override void OnViewDisable()
	{
		//Debug.Log("Disable Shop menu");
		m_ScreenPivot.InputEnabled = false;
		m_ShopScroller.DisableControls();
		m_ShopScroller.FadeOut();

		base.OnViewDisable();
	}

	protected override void OnPageVisible(GuiScreen page)
	{
		base.OnPageVisible(page);

		HighlightTabButton(CurrentPageIndex, true);

		RefreshPage(page as GuiShopPageBase);
	}

	protected override void OnPageHiding(GuiScreen page)
	{
		HighlightTabButton(CurrentPageIndex, false);

		base.OnPageHiding(page);
	}

//------------------------------------------ BUTTON HANDLERS

	void OnFreeGoldTouch()
	{
		GuiShopUtils.ShowOffers();
	}

	//--------------------------------------   

	void OnBuyPremiumButton(bool inside)
	{
		if (!inside)
			return;

		ShopItemId selId = m_ShopScroller.GetSelectedItem();

		if (ShopDataBridge.Instance.GetItemInfo(selId).PremiumOnly)
		{
			Owner.ShowPopup("PremiumAccount", null, null, null);
		}
	}

	void OnBuyButton(bool inside)
	{
		if (!inside)
			return;

		ShopItemId selId = m_ShopScroller.GetSelectedItem();

		//IAP funds kupujeme za realne penize a bez confirm dialogu (uzivatel musi nastesti potvrdit system dialog)
		bool iap = ShopDataBridge.Instance.IsIAPFund(selId);
		if (iap)
		{
			//buy iap (different from buying other stuff)
			if (ShopDataBridge.Instance.IAPServiceAvailable())
			{
				ShopDataBridge.Instance.IAPRequestPurchase(selId);
#if IAP_USE_MFLIVE
				Owner.ShowPopup("ShopStatusIAP", TextDatabase.instance[02900014], TextDatabase.instance[02030097], WaitForAIPurchaseHandler);
#else
				GuiShopStatusIAP iapPopup =
								Owner.ShowPopup("ShopStatusIAP", TextDatabase.instance[02900014], TextDatabase.instance[02900015], WaitForAIPurchaseHandler) as
								GuiShopStatusIAP;
				iapPopup.BuyIAPItem = selId;
#endif
			}
			else
			{
				//service not available
				Owner.ShowPopup("ShopMessageBox", TextDatabase.instance[02900016], TextDatabase.instance[02900017], NoIAPServiceHandler);
			}
		}
		else if (ShopDataBridge.Instance.IsFreeGold(selId))
		{
			GuiShopUtils.EarnFreeGold(selId);
		}
		else
		{
			//check funds, show buy if not enought and show buy confirm dialog
			StartCoroutine("BuyCoroutine", selId);
		}
	}

	IEnumerator BuyCoroutine(ShopItemId selId)
	{
		if (!ShopDataBridge.Instance.HaveEnoughMoney(selId, -1))
		{
			ShopItemId reqIAP = ShopDataBridge.Instance.GetIAPNeededForItem(selId, -1);

			if (reqIAP.IsEmpty())
			{
				yield break;
			}

			bool buySucceed = true;
			GuiShopNotFundsPopup.Instance.AddFundsID = reqIAP;
			GuiPopup popup = Owner.ShowPopup("NotFundsPopup",
											 "",
											 "",
											 (inPopup, inResult) =>
											 {
												 switch (inResult)
												 {
												 case E_PopupResultCode.Cancel:
													 buySucceed = false;
													 break;
												 case E_PopupResultCode.Failed:
													 buySucceed = false;
													 break;
												 }
											 });

			//Debug.Log("Popup Visible:" + popup.IsVisible);
			while (popup.IsVisible == true)
			{
				yield return new WaitForEndOfFrame();
			}

			if (buySucceed == false)
			{
				yield break;
			}
			//Debug.Log("IAP success:" + buySucceed);
		}

		if (ShopDataBridge.Instance.HaveEnoughMoney(selId, -1))
		{
			//show buy confirm dialog
			GuiShopBuyPopup.Instance.SetBuyItem(selId);
			Owner.ShowPopup("ShopBuyPopup", "", "", BuyResultHandler);
		}
	}

//--------------------------------------   PRIVATE

	void RefreshPage(GuiShopPageBase pageBase)
	{
		//items for current page
		List<ShopItemId> items = pageBase.GetItems();

		//insert new items into scroller
		m_ShopScroller.Insert(items, false);

#if IAP_USE_MFLIVE //when using MFLive, amount of gold is chosen at the paywall page, so we don't show scroller
		if (CurrentPage.GetType() == typeof(GuiShopPageFunds))
			m_ShopScroller.Hide();
		else
#endif
		m_ShopScroller.Show();

		//scroll to last item
		m_ShopScroller.SetSelectedItem(pageBase.LastId);
	}

	void HighlightTabButton(int pageIdx, bool state)
	{
		if (pageIdx < 0 || pageIdx >= m_TabButtonsNames.Length)
			return;

		GUIBase_Widget widget = Layout.GetWidget(m_TabButtonsNames[pageIdx]);
		GUIBase_Button button = widget ? widget.GetComponent<GUIBase_Button>() : null;

		if (button)
		{
			button.stayDown = state;
			button.ForceDownStatus(state);
		}
	}

	void OnSelectionChange(ShopItemId selItem)
	{
		if (!IsVisible || !IsEnabled)
			return;

		//Update buy button
		UpdateBuyButton(selItem);

		//update info about item
		UpdateCurrentPage(selItem, false);
	}

	//-----------------------------------------------------------------------------------------------------------------------------------------------------

	void UpdateCurrentPage(ShopItemId selItem, bool forceUpdateView)
	{
		GuiShopPageBase pageBase = CurrentPage as GuiShopPageBase;
		if (pageBase != null)
		{
			pageBase.OnItemChange(selItem, forceUpdateView);
		}
	}

	//-----------------------------------------------------------------------------------------------------------------------------------------------------
	void UpdateBuyButton(ShopItemId selItem)
	{
		//texty v db
		const int textBuy = 02030020;
		const int textConvert = 02030040;
		const int textOwned = 02030054;
		const int textEarn = 02030058;
		const int textLocked = 02030053;

		// this will displays status bar hints if needed
		ShopDataBridge.Instance.HaveEnoughMoney(selItem, -1);

		//pro veci ktere uz mame zakaz buy
		ShopItemInfo inf = ShopDataBridge.Instance.GetItemInfo(selItem);

		if (inf.PremiumOnly && inf.Locked)
		{
			m_BuyButton.Widget.Show(false, true);
			m_Buy_Premium_Button.Widget.Show(true, true);
		}
		else
		{
			m_BuyButton.Widget.Show(true, true);
			m_Buy_Premium_Button.Widget.Show(false, true);
			m_BuyButton.SetDisabled(inf.Owned && !inf.Consumable || inf.Locked);
		}

		//zobraz vhodny label:
		int textId = textBuy;

		if (inf.Owned && !inf.Consumable)
			textId = textOwned;
		else if (ShopDataBridge.Instance.IsFreeGold(selItem))
			textId = textEarn;
		else if (ShopDataBridge.Instance.IsFundConvertor(selItem))
			textId = textConvert;
		else if (inf.Locked)
			textId = textLocked;
		else
			textId = textBuy;

		m_Buy_Button_Label.SetNewText(textId);
	}

	void WaitForAIPurchaseHandler(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		//Debug.Log("Buy IAP finished, result: " + inResult);
	}

	void NoIAPServiceHandler(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		//Debug.Log("No IAP Service");
	}

	void BuyResultHandler(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		if (inResult == E_PopupResultCode.Success || inResult == E_PopupResultCode.Cancel)
		{
			//success
			ShopItemId selItem = m_ShopScroller.GetSelectedItem();
			UpdateBuyButton(selItem);
			UpdateCurrentPage(selItem, true);
		}
	}

	void OnUserPremiumAcctChanged(bool state)
	{
		if (IsVisible)
			OnViewShow();
	}

	void OnPreview()
	{
		Owner.ShowPopup("Preview", "", "", null);
	}

	// IGUIOVERLAYSCREEN INTERFACE

	string IGuiOverlayScreen.OverlayButtonTooltip
	{
		get { return null; }
	}

	bool IGuiOverlayScreen.HideOverlayButton
	{
		get { return Ftue.IsActionIdle<FtueAction.Shop>(); }
	}

	bool IGuiOverlayScreen.HighlightOverlayButton
	{
		get { return IsVisible == false ? Ftue.IsActionActive<FtueAction.Shop>() : false; }
	}
}
