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
using PremiumAccountDesc = CloudServices.PremiumAccountDesc;
using TimeSpan = System.TimeSpan;

public class GuiPopupPremiumAccount : GuiPopup
{
	public readonly static int MAX_TYPES = 5;

	public readonly static string CLOSE_BUTTON = "Close_Button";
	//public static readonly string BUY_BUTTON          = "Buy_Button";
	public readonly static string TYPE_BUTTON = "Buy_Button";
	//public static readonly string EXCHANGEGOLD_BUTTON = "ExchangeGold_Button";
	//public static readonly string FREEGOLD_BUTTON     = "FreeGold_Button";

	struct AccountInfo
	{
		public string Id;
		public TimeSpan Duration;
		public float Discount;
		public int FullPrice;
		public int RealPrice; // price with discount applied on it
		public int Save;
	}

	// PRIVATE MEMBERS

	int m_PendingAcct = 0;
	int m_SelectedAcct = -1;
	AccountInfo[] m_Accounts;

	// GUIPOPUP INTERFACE

	public override void SetCaption(string inCaption)
	{
	}

	public override void SetText(string inText)
	{
	}

	// GUIVIEW INTERFACE

	protected override void OnViewShow()
	{
		base.OnViewShow();

		PrepareAccounts();

		int gold = ShopDataBridge.Instance.PlayerGold;

		// deduce enabled buttons and initial selection
		if (gold >= m_Accounts[4].RealPrice)
		{
			m_PendingAcct = 5;
		}
		else if (gold >= m_Accounts[3].RealPrice)
		{
			m_PendingAcct = 3;
		}
		else if (gold >= m_Accounts[2].RealPrice)
		{
			m_PendingAcct = 3;
		}
		else if (gold >= m_Accounts[1].RealPrice)
		{
			m_PendingAcct = 2;
		}
		else if (gold >= m_Accounts[0].RealPrice)
		{
			m_PendingAcct = 1;
		}
		else
		{
			m_PendingAcct = 3;
		}

		// register buttons
		for (int idx = 0; idx < MAX_TYPES; ++idx)
		{
			int acctType = idx + 1;
			GUIBase_Button button = PrepareButton(TYPE_BUTTON + acctType,
												  (widget) =>
												  {
													  SelectAccount(acctType, false);
													  BuySelectedAccount();
												  },
												  null);
			UpdateAcctButton(button, ref m_Accounts[idx]);
		}

		//PrepareButton(BUY_BUTTON,          null, OnBuyPressed);
		//PrepareButton(EXCHANGEGOLD_BUTTON, null, OnExchangeGoldPressed);
		//PrepareButton(FREEGOLD_BUTTON,     null, OnFreeGoldPressed);
		PrepareButton(CLOSE_BUTTON, null, OnClosePressed);

		// update selection
#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR //highlight the account button only on devices
		SelectAccount(m_PendingAcct, true);
#else
		m_SelectedAcct = -1;
						//this is to reset the button state (needed when user clicks on one of the account buttons and than reopens the premium accounts window)
		SelectAccount(m_SelectedAcct, true);
#endif
	}

	protected override void OnViewHide()
	{
		//StopCoroutine("BlinkBuyButton_Coroutine");
		//CancelInvoke("BlinkGoldButtons");

		// unregister buttons
		for (int idx = 0; idx < MAX_TYPES; ++idx)
		{
			int acctType = idx + 1;
			PrepareButton(TYPE_BUTTON + acctType, null, null);
		}

		//PrepareButton(BUY_BUTTON,          null, null);
		//PrepareButton(EXCHANGEGOLD_BUTTON, null, null);
		//PrepareButton(FREEGOLD_BUTTON,     null, null);
		PrepareButton(CLOSE_BUTTON, null, null);

		// call super
		base.OnViewHide();
	}

	protected override void OnViewUpdate()
	{
		base.OnViewUpdate();

		SelectAccount(m_PendingAcct, false);
	}

	// HANDLERS

	/*private void OnBuyPressed(GUIBase_Widget widget)
	{
		BuySelectedAccount();
	}*/

	void OnExchangeGoldPressed(GUIBase_Widget widget)
	{
		IViewOwner owner = Owner;
		owner.Back();
		owner.ShowScreen("Shop:3");
	}

	void OnFreeGoldPressed(GUIBase_Widget widget)
	{
		IViewOwner owner = Owner;
		owner.Back();
		owner.ShowScreen("Shop:4");
	}

	void OnClosePressed(GUIBase_Widget widget)
	{
		Owner.Back();
		SendResult(E_PopupResultCode.Cancel);
	}

	// PRIVATE METHODS

	void SelectAccount(int newAcctType, bool force)
	{
		// always store requested account type as pending
		m_PendingAcct = newAcctType;

		if (newAcctType == m_SelectedAcct && force == false)
			return;
		m_SelectedAcct = newAcctType;

		for (int idx = 0; idx < MAX_TYPES; ++idx)
		{
			int acctType = idx + 1;
			GUIBase_Button button = GuiBaseUtils.GetControl<GUIBase_Button>(Layout, TYPE_BUTTON + acctType);
			button.ForceHighlight(m_SelectedAcct == acctType ? true : false);
		}

		//GuiBaseUtils.GetControl<GUIBase_Button>(Layout, BUY_BUTTON).SetDisabled(m_SelectedAcct < 0 ? true : false);

		/*if (m_SelectedAcct < 0)
		{
			InvokeRepeating("BlinkGoldButtons", 1.0f, 1.0f);
		}
		else
		{
			CancelInvoke("BlinkGoldButtons");
		}*/

		//StopCoroutine("BlinkBuyButton_Coroutine");
		//StartCoroutine("BlinkBuyButton_Coroutine");
	}

	/*private IEnumerator BlinkBuyButton_Coroutine()
	{
		GUIBase_Button button = GuiBaseUtils.GetControl<GUIBase_Button>(Layout, BUY_BUTTON);
		
		int count = 4;
		while (count-- > 0)
		{
			button.ForceHighlight(true);
			yield return new WaitForSeconds(0.1f);
	
			button.ForceHighlight(false);
			yield return new WaitForSeconds(0.1f);
		}
	}*/

	/*private void BlinkGoldButtons()
	{
		GUIBase_Button exchangeGold = GuiBaseUtils.GetControl<GUIBase_Button>(Layout, EXCHANGEGOLD_BUTTON);
		exchangeGold.ForceHighlight(!exchangeGold.isHighlighted);

		GUIBase_Button freeGold = GuiBaseUtils.GetControl<GUIBase_Button>(Layout, FREEGOLD_BUTTON);
		freeGold.ForceHighlight(!freeGold.isHighlighted);
	}*/

	void BuySelectedAccount()
	{
		StartCoroutine(BuyPremiumAccount_Coroutine(m_SelectedAcct));
	}

	IEnumerator BuyPremiumAccount_Coroutine(int acctType)
	{
		int idx = acctType - 1;
		int gold = ShopDataBridge.Instance.PlayerGold;
		int price = m_Accounts[idx].RealPrice;

		string acctTypeID = m_Accounts[idx].Id;

		CloudUser user = CloudUser.instance;

		// not enough golds
		if (price > gold)
		{
			ShopItemId itemId = ShopDataBridge.Instance.GetPremiumAcct(acctTypeID);
			if (itemId == ShopItemId.EmptyId)
			{
				// ...
				yield break;
			}

			int fundsNeeded = price - gold;
			bool buySucceed = true;

			ShopItemId reqIAP = ShopDataBridge.Instance.FindFundsItem(fundsNeeded, true);

			if (reqIAP.IsEmpty())
			{
				yield break;
			}

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

		// store current status of premium account
		bool hadPremiumAccount = user.isPremiumAccountActive;

		// create cloud action
		BaseCloudAction action = new BuyPremiumAccountAndFetchPPI(user.authenticatedUserID, acctTypeID);
		GameCloudManager.AddAction(action);

		// show message box
		GuiPopupMessageBox msgBox =
						(GuiPopupMessageBox)Owner.ShowPopup("MessageBox",
															TextDatabase.instance[01140017],
															TextDatabase.instance[01140018],
															(popup, result) =>
															{
																if (action.isSucceeded == true)
																{
																	Owner.Back();
																	SendResult(E_PopupResultCode.Ok);
																}
															});
		msgBox.SetButtonVisible(false);
		msgBox.SetButtonEnabled(false);

		// wait for async action...
		while (action.isDone == false)
		{
			yield return new WaitForEndOfFrame();
		}

		// update message box
		int textId = 01140021;
		if (action.isSucceeded == true)
		{
			textId = hadPremiumAccount == true ? 01140020 : 01140019;
		}

		msgBox.SetText(TextDatabase.instance[textId]);
		msgBox.SetButtonVisible(true);
		msgBox.SetButtonEnabled(true);
	}

	void PrepareAccounts()
	{
		// get available accounts from cloud
		PremiumAccountDesc[] accounts = CloudUser.instance.availablePremiumAccounts;
		System.Array.Sort(accounts, (x, y) => { return x.m_DurationInMinutes.CompareTo(y.m_DurationInMinutes); });

		// compute price of one minute
		float pricePerMin = (float)(accounts[0].m_PriceGold/(float)accounts[0].m_DurationInMinutes);

		// prepare infos
		m_Accounts = new AccountInfo[MAX_TYPES];
		for (int idx = 0; idx < accounts.Length; ++idx)
		{
			// create account
			PremiumAccountDesc desc = accounts[idx];
			AccountInfo acct = new AccountInfo()
			{
				Id = desc.m_Id,
				Duration = TimeSpan.FromMinutes(desc.m_DurationInMinutes),
				Discount = desc.m_DiscountMultiplier,
				FullPrice = desc.m_PriceGold,
				RealPrice = Mathf.RoundToInt(desc.m_PriceGold*(1.0f - desc.m_DiscountMultiplier))
			};

			// compute how many golds player save with this account
			double price = acct.Duration.TotalMinutes*pricePerMin;
			double diff = price - acct.RealPrice;
			acct.Save = Mathf.RoundToInt((float)(diff/price)*100.0f);

			// store account
			m_Accounts[idx] = acct;
		}
	}

	void UpdateAcctButton(GUIBase_Button button, ref AccountInfo acct)
	{
		GUIBase_Label[] labels = button.GetComponentsInChildren<GUIBase_Label>();
		foreach (var label in labels)
		{
			switch (label.name)
			{
			case "Time_Name":
				SetTimeLabel(label, ref acct);
				break;
			case "Price":
				SetPriceValue(label, ref acct);
				break;
			case "Save":
				SetSaveValue(label, ref acct);
				break;
			case "Discount":
				SetDiscount(label, ref acct);
				break;
			default:
				break;
			}
		}

		GUIBase_Number[] numbers = button.GetComponentsInChildren<GUIBase_Number>();
		foreach (var number in numbers)
		{
			switch (number.name)
			{
			case "Time_Value":
				SetTimeValue(number, ref acct);
				break;
			default:
				break;
			}
		}

		//button.SetDisabled(acct.RealPrice > ShopDataBridge.Instance.PlayerGold ? true : false);
	}

	void SetTimeValue(GUIBase_Number number, ref AccountInfo acct)
	{
		int hours = Mathf.RoundToInt((float)acct.Duration.TotalHours);
		int days = Mathf.RoundToInt((float)acct.Duration.TotalDays);
		int weeks = Mathf.RoundToInt(days/7.0f);
		int months = Mathf.RoundToInt(weeks/4.0f);

		if (hours < 24)
		{
			number.Value = hours;
		}
		else if (days < 7)
		{
			number.Value = days;
		}
		else if (weeks < 4)
		{
			number.Value = weeks;
		}
		else
		{
			number.Value = months;
		}
	}

	void SetTimeLabel(GUIBase_Label label, ref AccountInfo acct)
	{
		int hours = Mathf.RoundToInt((float)acct.Duration.TotalHours);
		int days = Mathf.RoundToInt((float)acct.Duration.TotalDays);
		int weeks = Mathf.RoundToInt(days/7.0f);
		int months = Mathf.RoundToInt(weeks/4.0f);

		if (hours == 1)
		{
			label.SetNewText(01140022);
		}
		else if (hours < 24)
		{
			label.SetNewText(01140007);
		}
		else if (days == 1)
		{
			label.SetNewText(01140023);
		}
		else if (days < 7)
		{
			label.SetNewText(01140008);
		}
		else if (weeks == 1)
		{
			label.SetNewText(01140009);
		}
		else if (weeks < 4)
		{
			label.SetNewText(01140024);
		}
		else if (months == 1)
		{
			label.SetNewText(01140010);
		}
		else
		{
			label.SetNewText(01140011);
		}
	}

	void SetPriceValue(GUIBase_Label label, ref AccountInfo acct)
	{
		int key = 01140025; //acct.RealPrice == 1 ? 01140006 : 01140005;
		label.SetNewText(string.Format(TextDatabase.instance[key], acct.RealPrice));
	}

	void SetDiscount(GUIBase_Label label, ref AccountInfo acct)
	{
		if (acct.Discount > 0.0f)
		{
			int key = acct.FullPrice == 1 ? 01140006 : 01140005;
			label.SetNewText(string.Format(TextDatabase.instance[key], acct.FullPrice));
		}
		label.Widget.Show(acct.Discount > 0.0f ? true : false, true);
	}

	void SetSaveValue(GUIBase_Label label, ref AccountInfo acct)
	{
		label.SetNewText(string.Format(TextDatabase.instance[01140012], acct.Save));
	}
}
