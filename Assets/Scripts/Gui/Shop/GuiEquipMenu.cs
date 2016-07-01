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

public class GuiEquipMenu : GuiScreen, IGuiOverlayScreen
{
	public static GuiEquipMenu Instance;

	public delegate void HideDelegate();
	public HideDelegate m_OnHideDelegate;

	public readonly static float WAITING_ITEM_ALPHA = 0.25f;

	CSkinModels m_SkinModel = new CSkinModels();
	CAccessoryModel m_CapModel = new CAccessoryModel();
	CAccessoryModel m_GunModel = new CAccessoryModel();

	GuiEquipSlots m_WeaponSlots = new GuiEquipSlots(GuiShop.E_ItemType.Weapon);
	GuiEquipSlots m_ItemSlots = new GuiEquipSlots(GuiShop.E_ItemType.Item);
	GuiEquipSlots m_HatSlots = new GuiEquipSlots(GuiShop.E_ItemType.Hat);
	GuiEquipSlots m_SkinSlots = new GuiEquipSlots(GuiShop.E_ItemType.Skin);
	GuiEquipSlots m_PerkSlots = new GuiEquipSlots(GuiShop.E_ItemType.Perk);

	GuiPopupViewItemDescription m_ItemDescription = new GuiPopupViewItemDescription();

	public GuiShop.E_ItemType SelectedSlotType { get; private set; }
	public int SelectedSlotIndex { get; private set; }

	int m_LastWeaponSlot = 0;

	int m_HACK_DelayedShowRemaining = 10;
	GuiPopupMessageBox m_MessageBox;

	const int maxWeaponSlot = 3;
	const int maxItemsSlot = 3;

	void Awake()
	{
		if (Instance != null)
		{
			Object.Destroy(Instance);
			Instance = null;
		}
		Instance = this;
		SelectedSlotType = GuiShop.E_ItemType.Weapon;
		SelectedSlotIndex = 0;

		m_SkinModel.Init();
		ShopDataBridge.CreateInstance();
	}

	protected override void OnViewInit()
	{
		base.OnViewInit();
		GuiEquipSelection.Instance.InitGui();

		m_ScreenPivot = MFGuiManager.Instance.GetPivot("EquipMenu");
		m_ScreenLayout = m_ScreenPivot.GetLayout("Main_Layout");

		m_ItemDescription.Init(m_ScreenPivot.GetLayout("ItemDescription_Layout"));

		/*GuiBaseUtils.RegisterButtonDelegate(m_ScreenLayout, "Back_Button", null, OnButtonBack);
		GuiBaseUtils.RegisterButtonDelegate(m_ScreenLayout, "Shop_Button", null, OnButtonShop);
		GuiBaseUtils.RegisterButtonDelegate(m_ScreenLayout, "Hide_Button", null, OnButtonBack);*/

		//weapons slot init
		for (int i = 0; i < maxWeaponSlot; i++)
		{
			GUIBase_Widget slotWidget = m_ScreenLayout.GetWidget("Gun_Slot" + i);

			SlotViewWeapon w = new SlotViewWeapon();
			w.InitGui(m_ScreenLayout, slotWidget, i);
			m_WeaponSlots.AddSlot(w, slotWidget);
			m_WeaponSlots.SetSlotLocked(i, ShopDataBridge.Instance.IsWeaponSlotLocked(i), ShopDataBridge.Instance.IsPremiumWeaponSlot(i));

			m_WeaponSlots.m_OnSlotSelectionDone = OnWeaponSelected;
		}

		//items slots init
		for (int i = 0; i < maxItemsSlot; i++)
		{
			GUIBase_Widget slotWidget = m_ScreenLayout.GetWidget("Item_Slot" + i);

			SlotViewItem w = new SlotViewItem();
			w.InitGui(m_ScreenLayout, slotWidget, i);
			m_ItemSlots.AddSlot(w, slotWidget);
			m_ItemSlots.SetSlotLocked(i, ShopDataBridge.Instance.IsItemSlotLocked(i), ShopDataBridge.Instance.IsPremiumItemSlot(i));

			m_ItemSlots.m_OnSlotSelectionDone = OnItemSelected;
		}

		//hat slots
		{
			GUIBase_Widget slotWidget = m_ScreenLayout.GetWidget("Hat_Slot");

			SlotViewSimple wA = new SlotViewSimple();
			wA.InitGui(m_ScreenLayout, slotWidget);
			m_HatSlots.AddSlot(wA, slotWidget);
			m_HatSlots.m_OnSlotSelectionDone = OnHatSelected;
		}

		//skins
		{
			GUIBase_Widget slotWidget = m_ScreenLayout.GetWidget("Skin_Slot");

			SlotViewSimple wA = new SlotViewSimple();
			wA.InitGui(m_ScreenLayout, slotWidget);
			m_SkinSlots.AddSlot(wA, slotWidget);
			m_SkinSlots.m_OnSlotSelectionDone = OnSkinSelected;
		}

		//perk
		{
			GUIBase_Widget slotWidget = m_ScreenLayout.GetWidget("Perk_Slot");

			SlotViewPerk wA = new SlotViewPerk();
			wA.InitGui(m_ScreenLayout, slotWidget);
			m_PerkSlots.AddSlot(wA, slotWidget);
			m_PerkSlots.m_OnSlotSelectionDone = OnPerkSelected;
		}

		//register for ppi update notification
		PPIManager.localPlayerInfoChanged += OnUpdatePPIInfo;

		CloudUser.premiumAcctChanged += OnUserPremiumAcctChanged;
	}

	void OnUpdatePPIInfo(PlayerPersistantInfo info)
	{
		//Debug.Log("OnUpdatePPIInfo");
		//FixEquipList();

		SyncSlots();
		UpdateAllViews();
	}

	void SyncWeaponsSlots()
	{
		for (int i = 0; i < maxWeaponSlot; i++)
		{
			bool locked = ShopDataBridge.Instance.IsWeaponSlotLocked(i);
			bool premium = ShopDataBridge.Instance.IsPremiumWeaponSlot(i);
			ShopItemId itmId = ShopDataBridge.Instance.GetWeaponInSlot(i);

			m_WeaponSlots.SetSlotParams(i, itmId, locked, premium);
		}
	}

	void SyncItemsSlots()
	{
		for (int i = 0; i < maxItemsSlot; i++)
		{
			bool locked = ShopDataBridge.Instance.IsItemSlotLocked(i);
			bool premium = ShopDataBridge.Instance.IsPremiumItemSlot(i);
			ShopItemId itmId = ShopDataBridge.Instance.GetItemInSlot(i);
			m_ItemSlots.SetSlotParams(i, itmId, locked, premium);
		}
	}

	void SyncSkinSlot()
	{
		m_SkinSlots.SetSlotParams(0, ShopDataBridge.Instance.GetPlayerSkin(), false, false);
	}

	void SyncHatSlot()
	{
		m_HatSlots.SetSlotParams(0, ShopDataBridge.Instance.GetPlayerHat(), false, false);
	}

	void SyncPerkSlot()
	{
		bool locked = ShopDataBridge.Instance.IsPerkSlotLocked();
		m_PerkSlots.SetSlotParams(0, ShopDataBridge.Instance.GetPlayerPerk(), locked, false);
	}

	void LockSlots()
	{
		for (int i = 0; i < maxWeaponSlot; i++)
		{
			m_WeaponSlots.SetSlotParams(i, ShopItemId.EmptyId, true, false);
		}

		for (int i = 0; i < maxItemsSlot; i++)
		{
			m_ItemSlots.SetSlotParams(i, ShopItemId.EmptyId, true, false);
		}

		m_SkinSlots.SetSlotParams(0, ShopItemId.EmptyId, true, false);

		m_HatSlots.SetSlotParams(0, ShopItemId.EmptyId, true, false);

		m_PerkSlots.SetSlotParams(0, ShopItemId.EmptyId, true, false);
	}

	void SyncSlots()
	{
		//ShopDataBridge.Instance.Debug_LogOwnedUpgrades();

		//weapons
		SyncWeaponsSlots();

		//items
		SyncItemsSlots();

		//skins
		SyncSkinSlot();

		//skins
		SyncPerkSlot();

		//hats
		SyncHatSlot();
	}

	void UpdateAllViews()
	{
		if (IsVisible)
		{
			m_WeaponSlots.UpdateViews();
			m_ItemSlots.UpdateViews();
			m_HatSlots.UpdateViews();
			m_SkinSlots.UpdateViews();
			m_PerkSlots.UpdateViews();

			UpdateOutfitView();
			GuiEquipSelection.Instance.UpdateScroller();
		}
	}

	protected override void OnViewShow()
	{
		//Debug.Log("Equip menu Show");
		//ShopDataBridge.Instance.Debug_LogEquipedItems();
		//ShopDataBridge.Instance.Debug_LogEquipedWeapons();

		base.OnViewShow();
		MFGuiManager.Instance.ShowPivot(m_ScreenPivot, true);

		if (m_HACK_DelayedShowRemaining > 0)
		{
			m_MessageBox = Owner.ShowPopup("MessageBox", TextDatabase.instance[0100001], TextDatabase.instance[02050041]) as GuiPopupMessageBox;
			m_MessageBox.SetButtonVisible(false);

			LockSlots();
		}

		m_WeaponSlots.Show();
		m_ItemSlots.Show();
		m_HatSlots.Show();
		m_SkinSlots.Show();
		m_PerkSlots.Show();

		if (m_HACK_DelayedShowRemaining <= 0)
		{
			DelayedShow();
		}

		m_ItemDescription.Show(false);
	}

	protected override void OnViewUpdate()
	{
		base.OnViewUpdate();

		if (m_HACK_DelayedShowRemaining > 0)
		{
			m_HACK_DelayedShowRemaining -= 1;
			if (m_HACK_DelayedShowRemaining > 0)
				return;

			DelayedShow();
		}

		string popupFooter;
		ShopItemId id = GetItemUnderMouse(out popupFooter);

		/*if(id.IsEmpty())
		{
			Debug.Log("Hightlited item " +  "None"); 
		}
		else 
		{
			Debug.Log("Hightlited item " +  id.GetName());*/

		switch (id.ItemType)
		{
		case GuiShop.E_ItemType.Weapon:
			m_ItemDescription.SetItem(new PreviewItem((E_WeaponID)id.Id));
			m_ItemDescription.Key = popupFooter;
			m_ItemDescription.Show(true);
			break;
		case GuiShop.E_ItemType.Item:
			m_ItemDescription.SetItem(new PreviewItem((E_ItemID)id.Id));
			m_ItemDescription.Key = popupFooter;
			m_ItemDescription.Show(true);
			break;
		case GuiShop.E_ItemType.Perk:
			m_ItemDescription.SetItem(new PreviewItem((E_PerkID)id.Id));
			m_ItemDescription.Key = popupFooter;
			m_ItemDescription.Show(true);
			break;
		case GuiShop.E_ItemType.Upgrade:
			m_ItemDescription.SetItem(new PreviewItem((E_UpgradeID)id.Id));
			m_ItemDescription.Key = popupFooter;
			m_ItemDescription.Show(true);
			break;
		default:
			m_ItemDescription.Show(false);
			break;
		}
		//}
	}

	ShopItemId GetItemUnderMouse(out string popupFooter)
	{
		popupFooter = null;
		int slotIndex;

		//check if mouse is over scroller
		ShopItemId id = GuiEquipSelection.Instance.GetItemOverMouse();
		if (id.ItemType != GuiShop.E_ItemType.None)
			return id;
		//not over scroller, check perks
		id = m_PerkSlots.GetItemOverMouse(out slotIndex);
		if (id.ItemType != GuiShop.E_ItemType.None)
		{
			string keyString = GetUseKeyString(PlayerControlsGamepad.E_Input.Sprint);
			if (!string.IsNullOrEmpty(keyString))
				popupFooter = TextDatabase.instance[02020002] + ": " + keyString;
			return id;
		}
		//not over perks, check usable items
		id = m_ItemSlots.GetItemOverMouse(out slotIndex);
		if (id.ItemType != GuiShop.E_ItemType.None)
		{
			PlayerControlsGamepad.E_Input command;
			if (slotIndex == 0)
			{
				command = PlayerControlsGamepad.E_Input.Item1;
			}
			else if (slotIndex == 1)
			{
				command = PlayerControlsGamepad.E_Input.Item2;
			}
			else
			{
				command = PlayerControlsGamepad.E_Input.Item3;
			}
			string keyString = GetUseKeyString(command);
			if (!string.IsNullOrEmpty(keyString))
				popupFooter = TextDatabase.instance[02020002] + ": " + keyString;
			return id;
		}
		//not over usable items, check weapon slots
		id = m_WeaponSlots.GetItemOverMouse(out slotIndex);

		return id;
	}

	string GetUseKeyString(PlayerControlsGamepad.E_Input command)
	{
		string keyString = null;

		GamepadInputManager.Instance.SetConfig(Game.CurrentJoystickName());
		JoyInput inputButton = GamepadInputManager.Instance.GetActionButton(command);

		if (inputButton.key != KeyCode.None || inputButton.joyAxis != E_JoystickAxis.NONE)
			keyString = GuiPopupGamepadConfig.GetButtonLabel(inputButton);

#if MADFINGER_KEYBOARD_MOUSE
		GamepadInputManager.Instance.SetConfig("Keyboard");
		JoyInput inputKey = GamepadInputManager.Instance.GetActionButton(command);
		if (inputKey.key != KeyCode.None || inputKey.joyAxis != E_JoystickAxis.NONE)
		{
			if (string.IsNullOrEmpty(keyString))
				keyString = GuiPopupGamepadConfig.GetButtonLabel(inputKey);
			else
				keyString = GuiPopupGamepadConfig.GetButtonLabel(inputKey) + ", " + keyString;
		}
#endif
		return keyString;
	}

	void DelayedShow()
	{
		SyncSlots();

		UpdateAllViews();

		UpdateOutfitView(true);
		GuiEquipSelection.Instance.UpdateScroller();

		//TODO_DT: integrovat do GuiEquipMenu!
		//GuiEquipSelection uz by nemel byt singleton a nejspis by nemusel byt ani samostatna trida. Touch na Equip slotech by mel primarne take chodit do Equip menu a ne do EquipSlots, kod by se tak mohl vyrazne zjednodusit.
		ShopItemId lastItem = GuiEquipSelection.Instance.LastSelectedItem;

		if (IsSlotLocked(SelectedSlotType, SelectedSlotIndex))
		{
			SelectedSlotType = GuiShop.E_ItemType.Weapon;
			SelectedSlotIndex = 0;
		}

		SelectSlot(SelectedSlotType, SelectedSlotIndex);
		if (lastItem != ShopItemId.EmptyId)
			GuiEquipSelection.Instance.SelectItem(lastItem);

		//Flurry.logEvent(AnalyticsTag.Equip, true);

		if (m_MessageBox != null)
		{
			m_MessageBox.ForceClose();
			m_MessageBox = null;
		}
		FixEquipList();
	}

	protected override void OnViewHide()
	{
		//Debug.Log("Equip menu Hide");

		MFGuiManager.Instance.ShowPivot(m_ScreenPivot, false);

		GuiEquipSelection.Instance.Hide();

		m_WeaponSlots.Hide();
		m_ItemSlots.Hide();
		m_HatSlots.Hide();
		m_SkinSlots.Hide();
		m_PerkSlots.Hide();

		m_SkinModel.HideSkin();
		m_CapModel.HideActiveModel();
		m_GunModel.HideActiveModel();

		//IsShown = false;

		base.OnViewHide();
	}

	protected override void OnViewEnable()
	{
		//Debug.Log("Enable Equip menu");
		GuiEquipSelection.Instance.EnableControls();
		base.OnViewEnable();
	}

	protected override void OnViewDisable()
	{
		//Debug.Log("Disable Equip menu");
		GuiEquipSelection.Instance.DisableControls();
		base.OnViewDisable();
	}

	protected override void OnViewDestroy()
	{
		CloudUser.premiumAcctChanged -= OnUserPremiumAcctChanged;
		PPIManager.localPlayerInfoChanged -= OnUpdatePPIInfo;
		base.OnViewDestroy();
	}

	public void UpdateLockedSlots()
	{
		//weapons slot init
		for (int i = 0; i < maxWeaponSlot; i++)
		{
			m_WeaponSlots.SetSlotLocked(i, ShopDataBridge.Instance.IsWeaponSlotLocked(i), ShopDataBridge.Instance.IsPremiumWeaponSlot(i));
		}

		//items slots init
		for (int i = 0; i < maxItemsSlot; i++)
		{
			m_ItemSlots.SetSlotLocked(i, ShopDataBridge.Instance.IsItemSlotLocked(i), ShopDataBridge.Instance.IsPremiumItemSlot(i));
		}
	}

	public void RemoveFromStack()
	{
		Owner.Back();
	}

	public bool IsSlotWaiting(GuiShop.E_ItemType slotType, int slot)
	{
		switch (slotType)
		{
		case GuiShop.E_ItemType.Weapon:
			return m_WeaponSlots.IsSlotWaiting(slot);
		case GuiShop.E_ItemType.Item:
			return m_ItemSlots.IsSlotWaiting(slot);
		case GuiShop.E_ItemType.Skin:
			return m_SkinSlots.IsSlotWaiting(slot);
		case GuiShop.E_ItemType.Hat:
			return m_HatSlots.IsSlotWaiting(slot);
		case GuiShop.E_ItemType.Perk:
			return m_PerkSlots.IsSlotWaiting(slot);
		default:
			Debug.LogWarning("Unexpected type for slot selection: " + slotType);
			return false;
		}
	}

	public bool IsSlotItemWaiting(ShopItemId itemId)
	{
		switch (itemId.ItemType)
		{
		case GuiShop.E_ItemType.Weapon:
			return m_WeaponSlots.IsItemWaiting(itemId);
		case GuiShop.E_ItemType.Item:
			return m_ItemSlots.IsItemWaiting(itemId);
		case GuiShop.E_ItemType.Skin:
			return m_SkinSlots.IsItemWaiting(itemId);
		case GuiShop.E_ItemType.Hat:
			return m_HatSlots.IsItemWaiting(itemId);
		case GuiShop.E_ItemType.Perk:
			return m_PerkSlots.IsItemWaiting(itemId);
		default:
			Debug.LogWarning("Unexpected type for slot selection: " + itemId.ItemType);
			return false;
		}
	}

	void OnUserPremiumAcctChanged(bool state)
	{
		FixEquipList();

		if (IsVisible)
			OnViewShow();

		if (GuiEquipSelection.Instance.IsShown)
			GuiEquipSelection.Instance.RefreshScrollbar();
	}

	/*void OnButtonBack(bool inside)
	{
		if(!inside)
			return;
		
		//GUI_Hide();
		//m_OnHideDelegate();
	
		Owner.Back();
	}*/

	void SelectSlot(GuiShop.E_ItemType slotType, int slotIndex)
	{
		switch (slotType)
		{
		case GuiShop.E_ItemType.Weapon:
			m_WeaponSlots.SelectSlotHACK(slotIndex);
			break;
		case GuiShop.E_ItemType.Item:
			m_ItemSlots.SelectSlotHACK(slotIndex);
			break;
		case GuiShop.E_ItemType.Skin:
			m_SkinSlots.SelectSlotHACK(slotIndex);
			break;
		case GuiShop.E_ItemType.Hat:
			m_HatSlots.SelectSlotHACK(slotIndex);
			break;
		case GuiShop.E_ItemType.Perk:
			m_PerkSlots.SelectSlotHACK(slotIndex);
			break;
		default:
			Debug.LogWarning("Unexpected type for slot selection: " + slotType);
			break;
		}
	}

	bool IsSlotLocked(GuiShop.E_ItemType slotType, int slotIndex)
	{
		switch (slotType)
		{
		case GuiShop.E_ItemType.Weapon:
			return m_WeaponSlots.IsSlotLocked(slotIndex);
		case GuiShop.E_ItemType.Item:
			return m_ItemSlots.IsSlotLocked(slotIndex);
		case GuiShop.E_ItemType.Skin:
			return m_SkinSlots.IsSlotLocked(slotIndex);
		case GuiShop.E_ItemType.Hat:
			return m_HatSlots.IsSlotLocked(slotIndex);
		case GuiShop.E_ItemType.Perk:
			return m_PerkSlots.IsSlotLocked(slotIndex);
		default:
			return true;
		}
	}

	void OnWeaponSelected(int slotIndex)
	{
		SyncWeaponsSlots();
		m_WeaponSlots.UpdateViews();
		UpdateWeaponModel();
	}

	void OnItemSelected(int slotIndex)
	{
		SyncItemsSlots();
		m_ItemSlots.UpdateViews();
	}

	void OnHatSelected(int slotIndex)
	{
		SyncHatSlot();
		m_HatSlots.UpdateViews();
		UpdateHatModel();
	}

	//vola se po vybrani skinu do slotu
	void OnSkinSelected(int slotIndex)
	{
		SyncSkinSlot();
		m_SkinSlots.UpdateViews();
		UpdateOutfitView();
	}

	void OnPerkSelected(int slotIndex)
	{
		SyncPerkSlot();
		m_PerkSlots.UpdateViews();
	}

	//Updatatuj vzhled modelu playera - skin hat, etc.
	//View index rika ktery model updatujeme - 0 good team, 1 bad team
	void UpdateOutfitView(bool forceShow = false)
	{
		//Debug.Log("UpdateOutfitView: visible " + IsVisible );
		if (IsVisible || forceShow)
		{
			ShopItemId skinId = m_SkinSlots.GetSlotItem(0);
			//Debug.Log("UpdateOutfitView: " + skinId);

			//should not be empty but check anyway
			if (!skinId.IsEmpty())
			{
				//show skin
				m_SkinModel.ShowSkin(skinId);

				//pri zmene skinu musime objevit model
				m_CapModel.HideActiveModel();
				m_GunModel.HideActiveModel();

				//show hat
				UpdateHatModel(forceShow);

				//show weapon
				UpdateWeaponModel(forceShow);
			}
		}
	}

	void UpdateHatModel(bool forceShow = false)
	{
		if (IsVisible || forceShow)
		{
			//show hat
			ShopItemId capId = m_HatSlots.GetSlotItem(0);
			m_CapModel.Show(capId, m_SkinModel.GetActiveHeadTransform());
		}
	}

	void UpdateWeaponModel(bool forceShow = false)
	{
		if (IsVisible || forceShow)
		{
			ShopItemId gunId = m_WeaponSlots.GetSlotItem(m_LastWeaponSlot);
			m_GunModel.Show(gunId, m_SkinModel.GetWeaponHolderTransform());
		}
	}

	public void SetSelectedSlot(GuiShop.E_ItemType slotType, int slotIndex)
	{
		//hide prev selection (if any)
		if (SelectedSlotType != GuiShop.E_ItemType.None && SelectedSlotIndex != -1)
			SetSlotButtonMark(SelectedSlotType, SelectedSlotIndex, false);

		//show new selection
		SetSlotButtonMark(slotType, slotIndex, true);

		//remember selection
		SelectedSlotType = slotType;
		SelectedSlotIndex = slotIndex;

		if (slotType == GuiShop.E_ItemType.Weapon)
		{
			m_LastWeaponSlot = slotIndex;
			UpdateOutfitView();
		}
	}

	void SetSlotButtonMark(GuiShop.E_ItemType slotType, int slotIndex, bool highlight)
	{
		switch (slotType)
		{
		case GuiShop.E_ItemType.Weapon:
			m_WeaponSlots.Highlight(slotIndex, highlight);
			break;
		case GuiShop.E_ItemType.Item:
			m_ItemSlots.Highlight(slotIndex, highlight);
			break;
		case GuiShop.E_ItemType.Hat:
			m_HatSlots.Highlight(slotIndex, highlight);
			break;
		case GuiShop.E_ItemType.Skin:
			m_SkinSlots.Highlight(slotIndex, highlight);
			break;
		case GuiShop.E_ItemType.Perk:
			m_PerkSlots.Highlight(slotIndex, highlight);
			break;
		default:
			Debug.LogError("Unexpected slot type: " + slotType);
			break;
		}
	}

	void FixEquipList()
	{
		BaseCloudAction action = GuiShopUtils.ValidateEquip();
		if (action != null)
		{
			//Debug.Log("FixEquipListAfterResearch");
			GameCloudManager.AddAction(action);
		}
	}

	// IGUIOVERLAYSCREEN INTERFACE

	string IGuiOverlayScreen.OverlayButtonTooltip
	{
		get { return null; }
	}

	bool IGuiOverlayScreen.HideOverlayButton
	{
		get { return Ftue.IsActionIdle<FtueAction.Equip>(); }
	}

	bool IGuiOverlayScreen.HighlightOverlayButton
	{
		get { return Ftue.IsActionActive<FtueAction.Equip>(); }
	}
};
