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

// Selection screen zobrazuje seznam koupenych zbrani nebo itemu a umoznuje hracovi jednu z nich vybrat.
// Note: Selection by se mohl refactorizovat jako soucast EquipMenu. Duvod proc je singleton instancovany ve scene je ze do nej prirazujeme odkaz na gameobject scroll itemu.
// Pokud bychom dokazali scrollbar item udelat jako prefab a spravne zaregitrovat jeho widgety v MFGuiManageu, tak bychom mohli kod zhjednodusit.
public class GuiEquipSelection : MonoBehaviour
{
	public static GuiEquipSelection Instance;
	public GameObject ScrollBarPrefab;
	bool m_IsInitialized = false;
	public bool IsShown { get; private set; }

	GUIBase_Layout m_Layout;
	GUIBase_Button m_Equip_Button;
	GUIBase_Label m_Equip_Label;
	GUIBase_Button m_Buy_Button;
	GuiShopFunds m_Cost;

	GuiShopItemScroller m_ItemScroller;
	ShopItemId m_EquipedItem = ShopItemId.EmptyId;
	public ShopItemId LastSelectedItem { get; private set; }
	//TODO_DT: tahle promena by se mela nahradit sezname posledni slekce pro kazdy typ slotu

	public delegate void EquipActionDelegate(ShopItemId selId, GuiShop.E_ItemType slotType, int slotIndex);
	public EquipActionDelegate m_OnEquipActionDelegate;

	void Awake()
	{
		if (Instance != null)
		{
			Object.Destroy(Instance);
			Instance = null;
		}
		Instance = this;
		m_IsInitialized = false;
		IsShown = false;

		m_ItemScroller = new GuiShopItemScroller(ScrollBarPrefab);
		//m_ItemScroller.m_DebugName = "Equip scroller";
		LastSelectedItem = ShopItemId.EmptyId;
	}

	void LateUpdate()
	{
		if (!m_IsInitialized)
		{
			//InitGui();
		}
		else if (IsShown)
		{
			m_ItemScroller.Update();
		}
	}

	public void InitGui()
	{
		GUIBase_Pivot pivot = MFGuiManager.Instance.GetPivot("EquipMenu");
		m_Layout = pivot.GetLayout("Main_Layout");
		m_Equip_Button = GuiBaseUtils.RegisterButtonDelegate(m_Layout, "Equip_Button", null, OnEquipButton);
		m_Equip_Label = GuiBaseUtils.PrepareLabel(m_Layout, "Equip_Label");

		m_Buy_Button = GuiBaseUtils.RegisterButtonDelegate(m_Layout, "Buy_Button", null, OnBuyButton);
		m_Cost = new GuiShopFunds(GuiBaseUtils.PrepareSprite(m_Layout, "Cost_Sprite"));

		m_ItemScroller.InitGui();
		m_ItemScroller.RegisterOnSelectionDelegate(OnSelectionChange);

		m_IsInitialized = true;
	}

	public void Hide()
	{
		//MFGuiManager.Instance.ShowLayout (m_Layout, false);

		m_ItemScroller.Hide();
		m_Equip_Button.Widget.Show(false, true);
		m_Buy_Button.Widget.Show(false, true);
		m_Cost.Show(false);

		IsShown = false;
	}

	public void Show(GuiShop.E_ItemType type, ShopItemId equipedId)
	{
		//Debug.Log("Show selection scroller: " +  type + " id: " + equipedId);

		List<ShopItemId> items = null;
		switch (type)
		{
		case GuiShop.E_ItemType.Weapon:
			items = ShopDataBridge.Instance.GetOwnedWeapons();
			break;
		case GuiShop.E_ItemType.Item:
			items = ShopDataBridge.Instance.GetOwnedItems();
			break;
		case GuiShop.E_ItemType.Hat:
			items = ShopDataBridge.Instance.GetOwnedCaps();
			break;
		case GuiShop.E_ItemType.Skin:
			items = ShopDataBridge.Instance.GetOwnedSkins();
			break;
		case GuiShop.E_ItemType.Perk:
			items = ShopDataBridge.Instance.GetOwnedPerks();
			break;
		default:
			Debug.LogError("TODO: support type " + type);
			break;
		}
		//Debug.Log("Inserting: " + items.Count);
		m_ItemScroller.Insert(items, true);

		m_ItemScroller.Show();
		m_Equip_Button.Widget.Show(true, true);

		IsShown = true;

		SetEquipedItem(equipedId);
		SelectItem(equipedId);
	}

	public void EnableControls()
	{
		m_Layout.InputEnabled = true;
		m_ItemScroller.EnableControls();
		m_ItemScroller.FadeIn();
	}

	public void DisableControls()
	{
		m_Layout.InputEnabled = false;
		m_ItemScroller.DisableControls();
		m_ItemScroller.FadeOut();
	}

	public void SetEquipedItem(ShopItemId equipedId)
	{
		if (m_EquipedItem == equipedId && equipedId != ShopItemId.EmptyId)
		{
			ScrollToItem(m_EquipedItem);
		}
		else
		{
			m_EquipedItem = equipedId;
		}
		UpdateItemButtons();
	}

	// 
	public void SelectItem(ShopItemId id)
	{
		m_ItemScroller.SetSelectedItem(id);
	}

	public void ScrollToItem(ShopItemId id)
	{
		m_ItemScroller.ScrollToItem(id);
	}

	public ShopItemId GetItemOverMouse()
	{
		return m_ItemScroller.GetItemOverMouse();
	}

	//Close button (zobrazuje se a reaguje na nej kdyz fungujeme jako popup dialog  vyvolany z equip menu)
	/*void OnButtonBack(bool inside)
	{
		if(!inside)
			return;
		
		Hide();
		m_OnHideDelegate(ShopItemId.EmptyId, true);  //cancel, none selected
	}*/

	void OnEquipButton(bool inside)
	{
		if (!inside)
			return;

		ShopItemId selItm = m_ItemScroller.GetSelectedItem();

		//pokud je selectnuty predmet ten stejny jako ve slotu, zmen akci na unequip
		bool isSkin = (selItm.ItemType == GuiShop.E_ItemType.Skin); //skins jsou vyjimka		

		if (m_EquipedItem.Equals(selItm) && !isSkin)
		{
			selItm = ShopItemId.EmptyId;
		}

		m_OnEquipActionDelegate(selItm, GuiEquipMenu.Instance.SelectedSlotType, GuiEquipMenu.Instance.SelectedSlotIndex);
		SetEquipedItem(selItm);
	}

	public void UpdateScroller()
	{
		m_ItemScroller.UpdateItemsViews();
	}

	void OnSelectionChange(ShopItemId selectedItem)
	{
		if (!m_IsInitialized || !IsShown)
			return;

		if (m_ItemScroller == null)
			return;

		LastSelectedItem = m_ItemScroller.GetSelectedItem();
		UpdateItemButtons();
	}

	public void UpdateItemButtons()
	{
		if (!IsShown)
			return;

		ShopItemId selId = m_ItemScroller.GetSelectedItem();

		if (m_ItemScroller.IsScrolling == false && selId != ShopItemId.EmptyId)
		{
			//equip
			ShopItemInfo inf = ShopDataBridge.Instance.GetItemInfo(selId);

			bool shopEnabled = GuiFrontendMain.IsVisible;
			bool isDepleatedItem = (inf.Consumable && inf.OwnedCount <= 0);
			if (shopEnabled && isDepleatedItem)
			{
				m_Buy_Button.Widget.Show(true, true);
				m_Equip_Button.Widget.Show(false, true);

				m_Cost.SetValue(inf.Cost, inf.GoldCurrency, false);
				m_Cost.Show(true);
			}
			else
			{
				m_Buy_Button.Widget.Show(false, true);
				m_Equip_Button.Widget.Show(true, true);
				m_Cost.Show(false);

				//bool canEquip = (inf != null && !inf.Locked); //TODO_tady muzeme zakazat equip pro locknute itemy
				bool isSlotWaiting = GuiEquipMenu.Instance.IsSlotWaiting(selId.ItemType, GuiEquipMenu.Instance.SelectedSlotIndex);
				bool isItemWaiting = GuiEquipMenu.Instance.IsSlotItemWaiting(selId);
				bool canEquip = (inf != null) && !isSlotWaiting && !isItemWaiting;

				//nepovol equip skinu pokud uz je uquipnuty
				if (selId.ItemType == GuiShop.E_ItemType.Skin && m_EquipedItem.Equals(selId))
					canEquip = false;

				//nepovol equip boostu pokud uz je vypotrebovany (povol pouze unequip)
				if (isDepleatedItem && (m_EquipedItem.IsEmpty() || !m_EquipedItem.Equals(selId)))
					canEquip = false;

				m_Equip_Button.SetDisabled(!canEquip);

				int strEquip = 02050001;
				int strUnequip = 02050002;
				int strSelect = 02050038;
				int strSwitch = 02050040;
				int strReplace = 02050042;

				int txtId = strEquip;
				if (selId.ItemType == GuiShop.E_ItemType.Skin)
					txtId = strSelect;
				else if (m_EquipedItem.Equals(selId))
					txtId = strUnequip;
				else if (m_EquipedItem.IsEmpty())
					txtId = strEquip;
				else if (ShopDataBridge.Instance.IsEquiped(selId))
								//predpokladame ze co je equipnuje je soucasne ve slotu (presnejsi by bylo prohledat sloty).
					txtId = strSwitch;
				else
					txtId = strReplace;

				m_Equip_Label.SetNewText(txtId);
			}
		}
		else
		{
			//m_Equip_Button.Widget.Show(false, true);
			m_Equip_Button.SetDisabled(true);
		}
	}

	public ShopItemId GetCurrentSelection()
	{
		return m_ItemScroller.GetSelectedItem();
	}

	public void RefreshScrollbar()
	{
		if (!IsShown)
			return;

		if (GuiEquipMenu.Instance != null)
		{
			Show(GuiEquipMenu.Instance.SelectedSlotType, m_EquipedItem);
		}
	}

	void OnBuyButton(bool inside)
	{
		if (!inside)
			return;

		ShopItemId selId = m_ItemScroller.GetSelectedItem();

		//IAP funds kupujeme za realne penize a bez confirm dialogu (uzivatel musi nastesti potvrdit system dialog)
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
			GuiPopup popup = GuiEquipMenu.Instance.Owner.ShowPopup("NotFundsPopup",
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
			GuiEquipMenu.Instance.Owner.ShowPopup("ShopBuyPopup", "", "", BuyResultHandler);
		}
	}

	void BuyResultHandler(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		if (inResult == E_PopupResultCode.Success || inResult == E_PopupResultCode.Cancel)
		{
			//success
			//ShopItemId selItem = m_ItemScroller.GetSelectedItem();
			UpdateItemButtons();
		}
	}
};
