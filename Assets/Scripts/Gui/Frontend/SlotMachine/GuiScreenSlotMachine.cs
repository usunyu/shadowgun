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
using LitJson;

public class GuiScreenSlotMachine : GuiScreen, IGuiOverlayScreen
{
	readonly static float JACKPOT_REFRESH_DELAY = 60.0f;

	readonly static string CLOSE_BUTTON = "Close_Button";
	readonly static string SPIN_BUTTON = "Spin_Button";
	readonly static string BUYCHIPS_BUTTON = "BuyChips_Button";
	readonly static string CHIPS_LABEL = "Chips_Label";
	readonly static string GOLDS_LABEL = "Golds_Label";
	readonly static string JACKPOT_LABEL = "Jackpot_Label";

	// PRIVATE MEMBERS

	[SerializeField] SlotMachine m_SlotMachine;

	GUIBase_Button m_CloseButton;
	GUIBase_Button m_SpinButton;
	GUIBase_Button m_BuyChipsButton;
	GUIBase_Label m_ChipsLabel;
	GUIBase_Label m_GoldsLabel;
	GUIBase_Label m_JackpotLabel;
	bool m_NeedsFetchPPI;
	int m_Prize;
	int m_Jackpot;
	bool m_UpdatingJackpot;

	// PUBLIC MEMBERS

	public int Jackpot
	{
		get { return m_Jackpot; }
		set
		{
			m_Jackpot = value;

			if (IsVisible == true)
			{
				m_JackpotLabel.SetNewText(m_Jackpot.ToString("N0"));
			}
		}
	}

	// IGUIOVERLAYSCREEN INTERFACE

	string IGuiOverlayScreen.OverlayButtonTooltip
	{
		get
		{
			if (IsVisible == true)
				return null;
			PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();
			if (ppi == null || ppi.Chips == 0)
				return null;
			return ppi.Chips <= 99 ? ppi.Chips.ToString() : "99+";
		}
	}

	bool IGuiOverlayScreen.HideOverlayButton
	{
		get { return Ftue.IsActive /*Ftue.IsActionIdle<FtueAction.Slotmachine>()*/; }
	}

	bool IGuiOverlayScreen.HighlightOverlayButton
	{
		get
		{
			if (IsVisible == true)
				return false;
			PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();
			return ppi != null && ppi.Chips > 0 ? true : false;
		}
	}

	// GUIVIEW INTERFACE

	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_SlotMachine.Initialize();

		RefreshJackpot(false);
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		GuiMenu menu = Owner as GuiMenu;
		if (menu != null)
		{
			menu.SetOverlaysVisible(false);
			menu.SetBackgroundVisibility(false);
		}

		m_Prize = 0;

		m_SlotMachine.Activate();

		m_ChipsLabel = GuiBaseUtils.GetControl<GUIBase_Label>(Layout, CHIPS_LABEL);
		m_GoldsLabel = GuiBaseUtils.GetControl<GUIBase_Label>(Layout, GOLDS_LABEL);
		m_JackpotLabel = GuiBaseUtils.GetControl<GUIBase_Label>(Layout, JACKPOT_LABEL);

		m_CloseButton = GuiBaseUtils.RegisterButtonDelegate(Layout, CLOSE_BUTTON, () => { StartCoroutine(Close_Coroutine()); }, null);

		m_SpinButton = GuiBaseUtils.RegisterButtonDelegate(Layout, SPIN_BUTTON, () => { StartCoroutine(Spin_Coroutine()); }, null);

		m_BuyChipsButton = GuiBaseUtils.RegisterButtonDelegate(Layout, BUYCHIPS_BUTTON, () => { StartCoroutine(BuyChips_Coroutine()); }, null);

		PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();

		m_CloseButton.IsDisabled = false;

		if (ppi.Chips <= 0)
		{
			StartCoroutine(BuyChips_Coroutine());
		}

		// make sure we have correct value displayed
		Jackpot = m_Jackpot;

		RefreshValues();
		UpdateButtons();
	}

	protected override void OnViewHide()
	{
		StopAllCoroutines();

		// allow update next time
		m_UpdatingJackpot = false;

		GuiBaseUtils.RegisterButtonDelegate(Layout, CLOSE_BUTTON, null, null);
		GuiBaseUtils.RegisterButtonDelegate(Layout, SPIN_BUTTON, null, null);
		GuiBaseUtils.RegisterButtonDelegate(Layout, BUYCHIPS_BUTTON, null, null);

		m_SlotMachine.Deactivate();

		GuiMenu menu = Owner as GuiMenu;
		if (menu != null)
		{
			menu.SetOverlaysVisible(true);
			menu.SetBackgroundVisibility(true);
		}

		base.OnViewHide();
	}

	protected override void OnViewUpdate()
	{
		base.OnViewUpdate();

		RefreshJackpot(true);
	}

	protected override bool OnViewProcessInput(ref IInputEvent evt)
	{
		if (evt.Kind == E_EventKind.Key)
		{
			KeyEvent key = (KeyEvent)evt;
			if (key.Code == KeyCode.Escape)
			{
				if (key.State == E_KeyState.Released)
				{
					Owner.Back();
				}
				return true;
			}
		}

		return base.OnViewProcessInput(ref evt);
	}

	// PRIVATE MEMBERS

	IEnumerator Close_Coroutine()
	{
		m_CloseButton.IsDisabled = true;
		m_SpinButton.IsDisabled = true;

		if (m_NeedsFetchPPI == true)
		{
			m_NeedsFetchPPI = false;

			FetchPlayerPersistantInfo action = new FetchPlayerPersistantInfo(CloudUser.instance.authenticatedUserID);
			GameCloudManager.AddAction(action);

			while (action.isDone == false)
			{
				yield return new WaitForEndOfFrame();
			}
		}

		Owner.Back();
	}

	IEnumerator Spin_Coroutine()
	{
		PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();
		if (ppi.Chips <= 0)
			yield break;

		if (m_SlotMachine.Spin() == false)
			yield break;

		//use ticket
		m_NeedsFetchPPI = true;
		
		UpdateButtons();

		ppi.AddChips(-1);

		RefreshValues();

		m_SpinButton.IsDisabled = true;

		int reward = 0;
		while (m_SlotMachine.IsBusy)
		{
			if (m_SlotMachine.Reward != reward)
			{
				reward = m_SlotMachine.Reward;
				m_GoldsLabel.SetNewText((m_Prize + reward).ToString());
			}

			yield return new WaitForEndOfFrame();
		}

		if (m_SlotMachine.Reward > 0)
		{
			m_Prize += m_SlotMachine.Reward;

			ppi.AddGold(m_SlotMachine.Reward);
			
			//Owner.ShowPopup("SlotMachineReward", "", string.Format(TextDatabase.instance[0505003], m_SlotMachine.Reward));
		}

		RefreshValues();
		UpdateButtons();

		if (ppi.Chips <= 0)
		{
			yield return StartCoroutine(BuyChips_Coroutine());
		}
	}

	IEnumerator BuyChips_Coroutine()
	{
		ShopItemId itemId = new ShopItemId((int)E_TicketID.TicketPackSmall, GuiShop.E_ItemType.Ticket);

		// buy gold is needed
		if (!ShopDataBridge.Instance.HaveEnoughMoney(itemId, -1))
		{
			ShopItemId reqIAP = ShopDataBridge.Instance.GetIAPNeededForItem(itemId, -1);

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

		// buy chips
		{
			GuiShopBuyPopup.Instance.SetBuyItem(itemId);
			GuiShopBuyPopup popup = (GuiShopBuyPopup)Owner.ShowPopup("ShopBuyPopup",
																	 null,
																	 null,
																	 (inPopup, inResult) =>
																	 {
																		 if (inResult == E_PopupResultCode.Success)
																		 {
																			 GuiShopBuyPopup temp = inPopup as GuiShopBuyPopup;
																			 ShopItemInfo info = ShopDataBridge.Instance.GetItemInfo(temp.BuyItemId);
																			 m_Prize = Mathf.Max(0, m_Prize - info.Cost);
																		 }
																	 });

			while (popup.IsVisible == true)
			{
				yield return new WaitForSeconds(0.2f);
			}
		}

		RefreshValues();
		UpdateButtons();
	}

	IEnumerator UpdateJackpot_Coroutine(bool animate)
	{
		m_UpdatingJackpot = true;

		GetSlotmachineJackpot action = new GetSlotmachineJackpot(CloudUser.instance.authenticatedUserID);
		GameCloudManager.AddAction(action);

		while (action.isDone == false)
		{
			yield return new WaitForSeconds(0.1f);
		}

		int jackpot = 0;
		if (action.isSucceeded == true)
		{
			JsonData data = JsonMapper.ToObject(action.result);
			jackpot = int.Parse(data["count"].ToString());
		}

		if (animate == false)
		{
			Jackpot = jackpot;
		}
		else if (Jackpot == 0 || jackpot <= Jackpot)
		{
			Jackpot = jackpot;

			yield return new WaitForSeconds(JACKPOT_REFRESH_DELAY);
		}
		else
		{
			int delta = jackpot - Jackpot;
			float delay = JACKPOT_REFRESH_DELAY/delta;
			float value = (float)Jackpot;
			while (Jackpot < jackpot)
			{
				value += delta/JACKPOT_REFRESH_DELAY*delay;
				Jackpot = Mathf.Min(Mathf.RoundToInt(value), jackpot);

				yield return new WaitForSeconds(delay);
			}
		}

		m_UpdatingJackpot = false;
	}

	void RefreshJackpot(bool animate)
	{
		if (m_UpdatingJackpot == false)
		{
			StartCoroutine(UpdateJackpot_Coroutine(animate));
		}
	}

	void RefreshValues()
	{
		PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();

		m_ChipsLabel.SetNewText(ppi.Chips.ToString());
		m_GoldsLabel.SetNewText(m_Prize.ToString());
	}

	void UpdateButtons()
	{
		PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();

		m_SpinButton.IsDisabled = false;

		m_SpinButton.Widget.Show(ppi.Chips > 0, true);
		m_BuyChipsButton.Widget.Show(ppi.Chips == 0, true);
	}
}
