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

//Vybavovani predmetu je async operace. Pri stisknuti buttonu slotu vyvolame operaci na serveru a cekame az skonci. 
//Po dobu behu operace zakazeme equip do slotu. Po dobu bezici libovolne operace zakazeme odchod z equip screenu.

//Trida sdruzujici nekolik slotu stejneho typu, ktera na stisknuti slotu vyvola popup selekci predmetu daneho typu.
class GuiEquipSlots
{
	class CSlot
	{
		public ShopItemId m_ItemId;
		public ISlotView m_SlotView;
		public GUIBase_Button m_Button;
		public bool m_Locked;
		public bool m_Waiting; //blocked for async op
		public int m_BlockedSlotIndex = -1; //index of concurrent slot blocked by switch action on this one
		public bool m_PremiumOnly;
		public GUIBase_Sprite m_WaitSprite;
		public Rect m_ScreenRect = new Rect();

		public void UpdateSlotView()
		{
			m_SlotView.Show(m_ItemId, m_Locked, m_Waiting, m_PremiumOnly);
			m_WaitSprite.Widget.Show(m_Waiting, true);
			m_Button.SetDisabled(m_Waiting || m_Locked);
		}
	};

	List<CSlot> m_Items = new List<CSlot>();
	GuiShop.E_ItemType m_SlotItemType;
	bool m_IsShown;

	public delegate void SlotSelectionDelegate(int slotIndex);
	public SlotSelectionDelegate m_OnSlotSelectionDone;

	public GuiEquipSlots(GuiShop.E_ItemType type)
	{
		m_SlotItemType = type;
	}

	public void AddSlot(ISlotView view, GUIBase_Widget widget)
	{
		CSlot slot = new CSlot();
		slot.m_ItemId = ShopItemId.EmptyId;
		slot.m_SlotView = view;
		slot.m_Button = GuiBaseUtils.GetChildButton(widget, "Button");

		slot.m_WaitSprite = GuiBaseUtils.GetChildSprite(widget, "Wait_Sprite");

		slot.m_Button.RegisterTouchDelegate2(OnSlotButton);

		slot.m_ScreenRect = slot.m_Button.Widget.GetRectInScreenCoords();

		m_Items.Add(slot);
	}

	int FindSlotIndex(GUIBase_Widget w)
	{
		for (int i = 0; i < m_Items.Count; i++)
		{
			if (m_Items[i].m_Button.Widget == w)
				return i;
		}
		return -1;
	}

	CSlot GetSlot(int index)
	{
		return m_Items[index];
	}

	void OnSlotButton(GUIBase_Widget w)
	{
		//remember current slot and show selection scroll bar 
		int pressedIndex = FindSlotIndex(w);
		OnSlotPressed_ByIndex(pressedIndex);
	}

	void OnSlotPressed_ByIndex(int index)
	{
		if (index == -1)
			return;

		//check lock status
		if (GetSlot(index).m_Locked)
			return;

		GuiEquipSelection.Instance.m_OnEquipActionDelegate = OnEquipAction;

		ShopItemId selId = GetSlot(index).m_ItemId;
		if (!GuiEquipSelection.Instance.IsShown || GuiEquipSelection.Instance.GetCurrentSelection().ItemType != m_SlotItemType)
			GuiEquipSelection.Instance.Show(m_SlotItemType, selId);
		else
			GuiEquipSelection.Instance.SetEquipedItem(selId);

		GuiEquipMenu.Instance.SetSelectedSlot(m_SlotItemType, index);
		GuiEquipSelection.Instance.UpdateItemButtons();
	}

	public void SelectSlotHACK(int index)
	{
		OnSlotPressed_ByIndex(index);
	}

	void OnEquipAction(ShopItemId selItem, GuiShop.E_ItemType slotType, int slotIndex)
	{
		//zkontroluj zda evokujeme akci na spravnem typu slotu
		if (m_SlotItemType != slotType)
		{
			Debug.LogWarning("Trying equip action " + slotType + " on slot " + m_SlotItemType);
			return;
		}

		//zkontroluj zda se nesnazime vlozit predmet do spatneho typu slotu. Empty id znamena ze delame unequip.	
		if (!selItem.IsEmpty() && selItem.ItemType != slotType)
		{
			Debug.LogWarning("Trying to equip item " + selItem + " into slot " + slotType + " with index " + slotIndex);
			return;
		}

		// check if we block any concurrent slot during switch action
		m_Items[slotIndex].m_BlockedSlotIndex = -1;
		if (!selItem.IsEmpty())
		{
			for (int idx = 0; idx < m_Items.Count; ++idx)
			{
				if (idx == slotIndex)
					continue;
				if (m_Items[idx].m_ItemId.Id != selItem.Id)
					continue;

				m_Items[slotIndex].m_BlockedSlotIndex = idx;
				SetWaiting(idx, true);

				break;
			}
		}

		//check what is in our slot
		ShopItemId slotItem = GetSlot(slotIndex).m_ItemId;

		//operace kdy se snazime vlozit prazdny predmet do prazdneho slotu nema smysl
		if (selItem.IsEmpty() && slotItem.IsEmpty())
		{
			Debug.LogWarning("Trying to equip empty item into empty slot: slotType " + slotType + " index " + slotIndex);
			return;
		}

		//show waiting sprite, disable button 
		SetWaiting(slotIndex, true);

		ShopItemId groupItem = ShopDataBridge.Instance.EquipedGroupItem(selItem);
		//pokud uz je equipnuty predmet se stejnou grupou jakou ma predmet ktery se chystame vlozit, odtran predchozi predmet z equipu
		if (!selItem.IsEmpty() && !groupItem.IsEmpty() && GetItemSlotIndex(groupItem) != slotIndex && !ShopDataBridge.Instance.IsEquiped(selItem))
		{
			//do slotu ktery mame vybrany vlozime to mame v selection
			int itemGUID = ShopDataBridge.Instance.GetShopItemGUID(selItem);

			//do slotu ktery obsahuje item ze stejne grupy jako je predmet ktery vkladame vlozime to co je v aktualnim slotu
			int slotGUID2 = ShopDataBridge.Instance.GetShopItemGUID(slotItem.IsEmpty() ? groupItem : slotItem);
			int groupSlot2 = GetItemSlotIndex(groupItem);

			//Debug.Log("Switching: " + selItem + ", slot " + slotIndex + " with " + slotItem + ", slot " + slotIndex2);
			SwitchAndFetchPPI action = new SwitchAndFetchPPI(CloudUser.instance.authenticatedUserID,
															 itemGUID,
															 slotIndex,
															 selItem,
															 slotGUID2,
															 groupSlot2,
															 slotItem,
															 OnSwitchActionDone);
			GameCloudManager.AddAction(action);
		}
		else
		//pokud se do slotu snazime vlozit item ktery je jiz v jinem slotu, provedemi misto pouheho vlozen prohozeni obou predmetu.
			if (!selItem.IsEmpty() && !slotItem.IsEmpty() && ShopDataBridge.Instance.IsEquiped(selItem))
			{
				//async switch action

				int itemGUID = ShopDataBridge.Instance.GetShopItemGUID(selItem);
				int slotIndex2 = GetItemSlotIndex(selItem);
				int itemGUID2 = ShopDataBridge.Instance.GetShopItemGUID(slotItem);

				//Debug.Log("Switching: " + selItem + ", slot " + slotIndex + " with " + slotItem + ", slot " + slotIndex2);
				SwitchAndFetchPPI action = new SwitchAndFetchPPI(CloudUser.instance.authenticatedUserID,
																 itemGUID,
																 slotIndex,
																 selItem,
																 itemGUID2,
																 slotIndex2,
																 slotItem,
																 OnSwitchActionDone);
				GameCloudManager.AddAction(action);
			}
			else
			{
				//async equip/unequip action

				//pokud je selId empty, znamena ze budeme provadet unequip a musime ziskat guid predmetu ktery je ve slotu ted
				int itemGUID = ShopDataBridge.Instance.GetShopItemGUID(selItem.IsEmpty() ? slotItem : selItem);
				EquipAndFetchPPI action = new EquipAndFetchPPI(CloudUser.instance.authenticatedUserID, itemGUID, slotIndex, selItem, OnEquipActionDone);
				GameCloudManager.AddAction(action);
			}

		//Debug.Log("Starting equip action: time " + Time.time + " index " + slotIndex + " item " + selItem);

		// update scroll bar items
		GuiEquipSelection.Instance.UpdateScroller();
	}

	public void OnEquipActionDone(ShopItemId selItem, int slotIndex, bool success)
	{
		if (success)
		{
			//Debug.Log("Equip slot success: time " + Time.time + " index " + slotIndex + " item " + selItem);
			m_OnSlotSelectionDone(slotIndex);
		}

		//hide wait sprite, enable button again
		SetWaiting(slotIndex, false);

		// unblock other slot if any blocked
		int blockedSlotIndex = m_Items[slotIndex].m_BlockedSlotIndex;
		if (blockedSlotIndex >= 0 && blockedSlotIndex < m_Items.Count)
		{
			m_Items[slotIndex].m_BlockedSlotIndex = -1;
			SetWaiting(blockedSlotIndex, false);
		}

		// update equip button and scroll bar items
		GuiEquipSelection.Instance.UpdateScroller();
		GuiEquipSelection.Instance.UpdateItemButtons();
	}

	public void OnSwitchActionDone(ShopItemId selItem, int slotIndex, ShopItemId selItem2, int slotIndex2, bool success)
	{
		//samotna jedna akce udela vsechno potrebne, neni treba volat callback i pro druhy slot
		OnEquipActionDone(selItem, slotIndex, success);
	}

	public void Show()
	{
		//Debug.Log("Slot show");
		m_IsShown = true;
		UpdateViews();
	}

	public void Hide()
	{
		//Debug.Log("Slot hide");
		m_IsShown = false;
		foreach (CSlot slot in m_Items)
			slot.m_SlotView.Hide();
	}

	//just set id in slot, no visual or other update is done
	public void SetSlotParams(int index, ShopItemId itmId, bool locked, bool premium)
	{
		CSlot slot = GetSlot(index);
		slot.m_Locked = locked;
		slot.m_ItemId = itmId;
		slot.m_PremiumOnly = premium;
	}

	public void UpdateViews()
	{
		if (m_IsShown)
		{
			foreach (CSlot slot in m_Items)
				slot.UpdateSlotView();
		}
	}

	public void SetSlotLocked(int index, bool locked, bool premium)
	{
		CSlot slot = GetSlot(index);
		slot.m_Locked = locked;
		slot.m_PremiumOnly = premium;

		if (m_IsShown)
			slot.UpdateSlotView();
	}

	public bool IsSlotLocked(int slot)
	{
		return m_Items[slot].m_Locked;
	}

	public bool IsSlotWaiting(int slot)
	{
		if (slot < 0 || slot >= m_Items.Count)
			return false;
		if (m_Items[slot].m_Waiting)
			return true;

		for (int i = 0; i < m_Items.Count; i++)
		{
			if (m_Items[i].m_BlockedSlotIndex == slot)
				return true;
		}

		return false;
	}

	public bool IsItemWaiting(ShopItemId itemId)
	{
		for (int i = 0; i < m_Items.Count; i++)
		{
			if (m_Items[i].m_ItemId.Id == itemId.Id)
				return IsSlotWaiting(i);
		}
		return false;
	}

	public ShopItemId GetSlotItem(int slot)
	{
		if (slot < 0 || slot >= m_Items.Count)
			Debug.LogError("Invalid index: " + slot + " range " + m_Items.Count);

		return m_Items[slot].m_ItemId;
	}

	void SetWaiting(int slotIdx, bool waiting)
	{
		CSlot slot = GetSlot(slotIdx);
		slot.m_Waiting = waiting;
		slot.UpdateSlotView();
	}

	public int GetFreeSlotIndex()
	{
		int index = m_Items.FindIndex(p => p.m_Locked == false && p.m_ItemId.IsEmpty());
		return index;
	}

	public int GetItemSlotIndex(ShopItemId findItem)
	{
		int index = m_Items.FindIndex(p => p.m_Locked == false && p.m_ItemId == findItem);
		return index;
	}

	public void Highlight(int slot, bool highlight)
	{
		if (slot >= 0 && slot < m_Items.Count)
		{
			m_Items[slot].m_Button.ForceHighlight(highlight);
		}
	}

	public ShopItemId GetItemOverMouse(out int index)
	{
		for (int i = 0; i < m_Items.Count; i++)
		{
			//Invery y-axis of input (because it is inverted for some god knows why reason...)
			Vector3 input = Input.mousePosition;
			input.y = Screen.height - input.y;
			if (m_Items[i].m_ScreenRect.Contains(input))
			{
				index = i;
				return m_Items[i].m_ItemId;
			}
		}
		index = -1;
		return new ShopItemId();
	}
};
