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

// Collection of data structures for shop and equip menu.
public class GuiShop
{
	public enum E_ItemType
	{
		None,
		Weapon,
		Item,
		Fund,
		Skin,
		Hat,
		Bundle,
		Perk,
		Upgrade,
		Account,
		Ticket,
	}
}

public class ShopItemInfo
{
	public bool EmptySlot;
	public GUIBase_Widget SpriteWidget;
	public GUIBase_Widget WeaponTypeWidget;
	public GUIBase_Widget ScrollerWidget;
	public string NameText;
	public int Upgrade;
	public bool Owned;
	public bool Locked;
	public int RequiresRank;
	public bool PremiumOnly;
	public bool Equiped;
	public bool NewInShop;
	public bool RareItem;
	public bool PriceSale;
	public string DiscountTag;
	public int Cost;
	public int CostBeforeSale;
	public bool GoldCurrency;
	public int Description;
	public string IAPCost;

	public int UpgradeCost;
	public bool UpgradeGoldCurrency;

	public int OwnedCount;
	public int AddCount;
	public bool Consumable;
	public float BoostDuration;
	public float BoostModifier;

	//public string				BundleContains;	
	public int BundleCount; //pocet predmetu v bundlu
	public int BundleOwnedCount; //pocet vlastnenych predmetu v bundlu
	public List<ShopItemId> BundleItems; //seznam predmetu v bundlu

	public int AddGold; //how much gold or cash we get for buying this fund (or for conversion)
	public int AddMoney; //how much gold or cash we get for buying this fund (or for conversion)
};

// Provides shop and equip menu with all information needed. 
// It generate list of items based on shop request (what items are aviable, which are equiped, which are bought, lists by category etc.).
public class ShopDataBridge
{
	public static ShopDataBridge Instance;

	public PlayerPersistantInfo PPI
	{
		get
		{
			PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPlayerPPI();
			//Debug.Log("PPI: " + ((ppi == null) ? "LocalPPI" : "LocalPlayerPPI"));
			return ppi != null ? ppi : PPIManager.Instance.GetLocalPPI();
		}
	}

	public GUIBase_Widget MissingWidget { get; private set; }

#if UNITY_EDITOR
	//pro testing novych itemu v shopu muzeme povolit v editoru zobrazovani i zakazanych predmetu a predmetu ktere se hracum nezobrazuji.
	//public bool Editor_ShowAllSkins {get{return true; }}
	public bool Editor_ShowAllSkins
	{
		get { return false; }
	}

	//public bool Editor_ShowAllHats {get{return true; }}
	public bool Editor_ShowAllHats
	{
		get { return false; }
	}

	//public bool Editor_ShowAllFunds {get{return true; }}
	public bool Editor_ShowAllFunds
	{
		get { return false; }
	}

	//public bool Editor_ShowAllBundles {get{return true; }}
	public bool Editor_ShowAllBundles
	{
		get { return false; }
	}

	//public bool Editor_ShowAllItems {get{return true; }}
	public bool Editor_ShowAllItems
	{
		get { return false; }
	}

#if UNITY_STANDALONE_WIN
	public bool SimulateIAP{ get{ return false; }}
#else
	public bool SimulateIAP
	{
		get { return true; }
	}
#endif

#else
	public bool SimulateIAP{ get{ return false; }}
#endif

	InAppAsyncOpResult m_IAPRequestStatus;
	BuyAndFetchPPI m_SimulateIAPAction;

	public bool IsIAPInProgress()
	{
		if (SimulateIAP)
			return (m_SimulateIAPAction != null);
		else
			return (m_IAPRequestStatus != null);
	}

	public InAppAsyncOpState GetIAPState()
	{
		if (SimulateIAP)
		{
			if (m_SimulateIAPAction.isFailed == true)
				return InAppAsyncOpState.Failed;
			if (m_SimulateIAPAction.isSucceeded == true)
				return InAppAsyncOpState.Finished;
			return InAppAsyncOpState.Waiting;
		}
		else
			return m_IAPRequestStatus.CurrentState;
	}

	public static void CreateInstance()
	{
		Instance = new ShopDataBridge();

		GameObject go = GameObject.Instantiate(Resources.Load("Gui/EmptyWidget")) as GameObject;
		Instance.MissingWidget = go.GetComponent<GUIBase_Widget>();
	}

	ShopItemInfo CreateEmptySlotInfo()
	{
		ShopItemInfo inf = new ShopItemInfo();
		inf.NameText = TextDatabase.instance[0210000];
		inf.EmptySlot = true;

		return inf;
	}

	public E_ItemID FindBestItemUpgrade(E_ItemID ofId)
	{
		List<PPIItemData> inItems = PPI.InventoryList.Items;

		E_ItemID bestUpgrade = ofId;
		string fullName = ofId.ToString();

		string rootName;
		int upgNumber = GetUpgradeLevel(fullName, out rootName);

		foreach (PPIItemData w in inItems)
		{
			string otherName = w.ID.ToString();
			string otherRootName;
			int otherLevel = GetUpgradeLevel(otherName, out otherRootName);
			if (rootName == otherRootName && otherLevel > upgNumber)
			{
				bestUpgrade = w.ID;
			}
		}

		return bestUpgrade;
	}

	public E_PerkID FindBestPerkUpgrade(E_PerkID ofId)
	{
		List<PPIPerkData> inPerks = PPI.InventoryList.Perks;

		E_PerkID bestUpgrade = ofId;
		string fullName = ofId.ToString();

		string rootName;
		int upgNumber = GetUpgradeLevel(fullName, out rootName);

		foreach (PPIPerkData w in inPerks)
		{
			string otherName = w.ID.ToString();
			string otherRootName;
			int otherLevel = GetUpgradeLevel(otherName, out otherRootName);
			if (rootName == otherRootName && otherLevel > upgNumber)
			{
				bestUpgrade = w.ID;
			}
		}

		return bestUpgrade;
	}

	int GetUpgradeLevel(string fullName, out string rootName)
	{
		int upgNumber = 0;
		rootName = fullName;

		//klasicke cislovani (pouzivaji zbrane)
		if (int.TryParse(fullName.Substring(fullName.Length - 1), out upgNumber))
		{
			rootName = fullName.Substring(0, fullName.Length - 1);
			return upgNumber;
		}

		//rimska cisla (itemy a perky)
		if (fullName.Length > 4 && fullName.Substring(fullName.Length - 3) == "III")
		{
			rootName = fullName.Substring(0, fullName.Length - 3);
			return 3;
		}

		if (fullName.Length > 2 && fullName.Substring(fullName.Length - 2) == "II")
		{
			rootName = fullName.Substring(0, fullName.Length - 2);
			return 2;
		}

		if (fullName.Substring(fullName.Length - 1) == "I")
		{
			rootName = fullName.Substring(0, fullName.Length - 1);
			return 1;
		}

		return 0;
	}

	public List<ShopItemId> GetOwnedWeapons()
	{
		List<ShopItemId> ownedWeapons = new List<ShopItemId>();
		PPIInventoryList pil = PPI.InventoryList;
		List<PPIWeaponData> weapons = pil.Weapons;
		foreach (PPIWeaponData w in weapons)
		{
			if (w.ID == E_WeaponID.None || w.ID >= E_WeaponID.MAX_ID)
			{
				Debug.LogError("One of the Weapons in Inventory has invalid ID: " + w.ID);
				continue;
			}

			//skip disabled
			WeaponSettings s = WeaponSettingsManager.Instance.Get(w.ID);
			if (s.DISABLED)
				continue;

			//add weapon
			ShopItemId item = new ShopItemId((int)w.ID, GuiShop.E_ItemType.Weapon);
			ownedWeapons.Add(item);
		}

		return ownedWeapons;
	}

	public List<ShopItemId> GetOwnedItems()
	{
		List<ShopItemId> ownedItems = new List<ShopItemId>();
		PPIInventoryList pil = PPI.InventoryList;
		List<PPIItemData> items = pil.Items;
		foreach (PPIItemData w in items)
		{
			if (w.ID == E_ItemID.None || w.ID >= E_ItemID.MAX_ID)
			{
				Debug.LogError("One of the Items in Inventory has invalid ID: " + w.ID);
				continue;
			}

			//skip disabled
			ItemSettings s = ItemSettingsManager.Instance.Get(w.ID);
			if (s.DISABLED)
				continue;

			//filter out all lower versions 
			E_ItemID bestVersion = FindBestItemUpgrade(w.ID);
			if (w.ID != bestVersion)
				continue;

			//add
			ShopItemId item = new ShopItemId((int)w.ID, GuiShop.E_ItemType.Item);
			ownedItems.Add(item);
		}

		return ownedItems;
	}

	public List<ShopItemId> GetOwnedCaps()
	{
		List<ShopItemId> ownedCaps = new List<ShopItemId>();

		PPIInventoryList pil = PPI.InventoryList;
		List<PPIHatData> items = pil.Hats;
		foreach (PPIHatData w in items)
		{
			if (w.ID == E_HatID.None || w.ID >= E_HatID.MAX_ID)
			{
				Debug.LogError("One of the Hats in Inventory has invalid ID: " + w.ID);
				continue;
			}

			//skip disabled
			HatSettings s = HatSettingsManager.Instance.Get(w.ID);
			if (s.DISABLED)
				continue;

			//skip items that are only for premium account
			if (s.PremiumOnly && !IsPremiumAccount())
				continue;

			ShopItemId item = new ShopItemId((int)w.ID, GuiShop.E_ItemType.Hat);
			ownedCaps.Add(item);
		}
		return ownedCaps;
	}

	public List<ShopItemId> GetOwnedSkins()
	{
		List<ShopItemId> ownedSkins = new List<ShopItemId>();
		PPIInventoryList pil = PPI.InventoryList;
		List<PPISkinData> items = pil.Skins;
		foreach (PPISkinData w in items)
		{
			if (w.ID == E_SkinID.None || w.ID >= E_SkinID.MAX_ID)
			{
				Debug.LogError("One of the Skins in Inventory has invalid ID: " + w.ID);
				continue;
			}

			//skip disabled
			SkinSettings s = SkinSettingsManager.Instance.Get(w.ID);
			if (s.DISABLED)
				continue;

			//skip items that are only for premium account
			if (s.PremiumOnly && !IsPremiumAccount())
				continue;

			//add
			ShopItemId item = new ShopItemId((int)w.ID, GuiShop.E_ItemType.Skin);
			ownedSkins.Add(item);
		}
		return ownedSkins;
	}

	public List<ShopItemId> GetOwnedPerks()
	{
		List<ShopItemId> ownedPerks = new List<ShopItemId>();
		PPIInventoryList pil = PPI.InventoryList;
		List<PPIPerkData> items = pil.Perks;
		foreach (PPIPerkData w in items)
		{
			if (w.ID == E_PerkID.None || w.ID >= E_PerkID.MAX_ID)
			{
				Debug.LogError("One of the Skins in Inventory has invalid ID: " + w.ID);
				continue;
			}

			//skip disabled
			PerkSettings s = PerkSettingsManager.Instance.Get(w.ID);
			if (s.DISABLED)
				continue;

			//filter out all lower versions 
			E_PerkID bestVersion = FindBestPerkUpgrade(w.ID);
			if (w.ID != bestVersion)
				continue;

			//add
			ShopItemId item = new ShopItemId((int)w.ID, GuiShop.E_ItemType.Perk);
			ownedPerks.Add(item);
		}
		return ownedPerks;
	}

	public List<ShopItemId> GetHats()
	{
		HatSettings[] sets = HatSettingsManager.Instance.GetAll();
		List<ShopItemId> allCaps = new List<ShopItemId>();
		foreach (HatSettings s in sets)
		{
			if (s.ID == E_HatID.None || s.ID >= E_HatID.MAX_ID)
			{
				Debug.LogError("One of the Hats has invalid ID: " + s.ID);
				continue;
			}
#if UNITY_EDITOR
			if (!Editor_ShowAllHats && (s.DISABLED || !s.AvailableInShop || s.BundleOnly))
				continue;
#else			
            if (s.DISABLED || !s.AvailableInShop || s.BundleOnly)
                continue;
#endif

			allCaps.Add(new ShopItemId((int)s.ID, GuiShop.E_ItemType.Hat));
		}
		return allCaps;
	}

	public List<ShopItemId> GetSkins()
	{
		SkinSettings[] sets = SkinSettingsManager.Instance.GetAll();
		List<ShopItemId> allSkins = new List<ShopItemId>();
		foreach (SkinSettings s in sets)
		{
			if (s.ID == E_SkinID.None || s.ID >= E_SkinID.MAX_ID)
			{
				Debug.LogError("One of the Weapons has invalid ID: " + s.ID);
				continue;
			}

#if UNITY_EDITOR
			if (!Editor_ShowAllSkins && (s.DISABLED || !s.AvailableInShop || s.BundleOnly))
				continue;
#else			
            if (s.DISABLED || !s.AvailableInShop  || s.BundleOnly)
                continue;
#endif

			allSkins.Add(new ShopItemId((int)s.ID, GuiShop.E_ItemType.Skin));
		}
		return allSkins;
	}

	public List<ShopItemId> GetFunds()
	{
		FundSettings[] sets = FundSettingsManager.Instance.GetAll();
		List<ShopItemId> allFunds = new List<ShopItemId>();
		foreach (FundSettings s in sets)
		{
			if (s.ID == E_FundID.None || s.ID >= E_FundID.MAX_ID)
			{
				Debug.LogError("One of the Funds has invalid ID: " + s.ID);
				continue;
			}

#if UNITY_EDITOR
			if (!Editor_ShowAllFunds && (s.DISABLED || !s.AvailableInShopOnCurrentPlatform || s.BundleOnly))
				continue;
#else			
            if (s.DISABLED || !s.AvailableInShopOnCurrentPlatform || s.BundleOnly)
                continue;
#endif
			if (s.AddGold <= 0)
				continue;

			allFunds.Add(new ShopItemId((int)s.ID, GuiShop.E_ItemType.Fund));
		}
		return allFunds;
	}

	public List<ShopItemId> GetMoney()
	{
		FundSettings[] sets = FundSettingsManager.Instance.GetAll();
		List<ShopItemId> allFunds = new List<ShopItemId>();
		foreach (FundSettings s in sets)
		{
			if (s.ID == E_FundID.None || s.ID >= E_FundID.MAX_ID)
			{
				Debug.LogError("One of the Funds has invalid ID: " + s.ID);
				continue;
			}

#if UNITY_EDITOR
			if (!Editor_ShowAllFunds && (s.DISABLED || !s.AvailableInShop || s.BundleOnly))
				continue;
#else			
            if (s.DISABLED || !s.AvailableInShop || s.BundleOnly)
                continue;
#endif
			if (s.AddMoney <= 0)
				continue;

			allFunds.Add(new ShopItemId((int)s.ID, GuiShop.E_ItemType.Fund));
		}
		return allFunds;
	}

	public List<ShopItemId> GetFreeGolds()
	{
		FundSettings[] sets = FundSettingsManager.Instance.GetAll();
		List<ShopItemId> allFunds = new List<ShopItemId>();
		foreach (FundSettings s in sets)
		{
			if (s.ID == E_FundID.None || s.ID >= E_FundID.MAX_ID)
			{
				Debug.LogError("One of the Funds has invalid ID: " + s.ID);
				continue;
			}

#if UNITY_EDITOR
			if (!Editor_ShowAllFunds && (s.DISABLED || !s.AvailableInShop || s.BundleOnly))
				continue;
#else			
            if (s.DISABLED || !s.AvailableInShop || s.BundleOnly)
                continue;
#endif

			ShopItemId itemId = new ShopItemId((int)s.ID, GuiShop.E_ItemType.Fund);
			if (!IsFreeGold(itemId))
				continue;

			allFunds.Add(itemId);
		}
		return allFunds;
	}

	public List<ShopItemId> GetBundles()
	{
		BundleSettings[] sets = BundleSettingsManager.Instance.GetAll();
		List<ShopItemId> allBundles = new List<ShopItemId>();
		foreach (BundleSettings s in sets)
		{
			if (s.ID == E_BundleID.None || s.ID >= E_BundleID.MAX_ID)
			{
				Debug.LogError("One of the Upgrades has invalid ID: " + s.ID);
				continue;
			}

#if UNITY_EDITOR
			if (!Editor_ShowAllBundles && (s.DISABLED || !s.AvailableInShop))
				continue;
#else			
            if (s.DISABLED || !s.AvailableInShop)
                continue;
#endif

			allBundles.Add(new ShopItemId((int)s.ID, GuiShop.E_ItemType.Bundle));
		}
		return allBundles;
	}

	public List<ShopItemId> GetBoostItems()
	{
		ItemSettings[] sets = ItemSettingsManager.Instance.GetAll();
		List<ShopItemId> allBoostItems = new List<ShopItemId>();
		foreach (ItemSettings s in sets)
		{
			if (s.ID == E_ItemID.None || s.ID >= E_ItemID.MAX_ID)
			{
				Debug.LogError("One of the Items has invalid ID: " + s.ID);
				continue;
			}

#if UNITY_EDITOR
			if (!Editor_ShowAllItems && (s.DISABLED || !s.AvailableInShop || s.BundleOnly))
				continue;
#else			
            if (s.DISABLED || !s.AvailableInShop || s.BundleOnly)
                continue;
#endif
			if (!s.Consumable)
				continue;

			allBoostItems.Add(new ShopItemId((int)s.ID, GuiShop.E_ItemType.Item));
		}
		return allBoostItems;
	}

	public ShopItemInfo GetItemInfo(ShopItemId itemId)
	{
		if (itemId == ShopItemId.EmptyId)
		{
			return CreateEmptySlotInfo();
		}

		switch (itemId.ItemType)
		{
		case GuiShop.E_ItemType.Weapon:
			return GetShopWeaponInfo(itemId.Id);
		case GuiShop.E_ItemType.Item:
			return GetShopItemInfo(itemId.Id);
		case GuiShop.E_ItemType.Hat:
			return GetShopHatInfo(itemId.Id);
		case GuiShop.E_ItemType.Skin:
			return GetShopSkinInfo(itemId.Id);
		case GuiShop.E_ItemType.Fund:
			return GetShopFundInfo(itemId.Id);
		case GuiShop.E_ItemType.Bundle:
			return GetShopBundleInfo(itemId.Id);
		case GuiShop.E_ItemType.Perk:
			return GetShopPerkInfo(itemId.Id);
		case GuiShop.E_ItemType.Upgrade:
			return GetShopUpgradeInfo(itemId.Id);
		case GuiShop.E_ItemType.Account:
			return GetShopAccountInfo(itemId.Id);
		case GuiShop.E_ItemType.Ticket:
			return GetShopTicketInfo(itemId.Id);
		default:
			Debug.LogError("TODO: Unsupported type" + itemId.ItemType + " ( " + itemId + " )");
			break;
		}
		return CreateEmptySlotInfo();
	}

	PPIWeaponData GetOwnedWeaponData(E_WeaponID id)
	{
		PPIInventoryList pil = PPI.InventoryList;
		List<PPIWeaponData> weapons = pil.Weapons;
		foreach (PPIWeaponData w in weapons)
		{
			if (w.ID == id)
			{
				return w;
			}
		}
		return new PPIWeaponData();
	}

	void SetOwnedWeaponUpgrade(E_WeaponID id)
	{
		//update inventory
		bool found = false;
		List<PPIWeaponData> invWeapons = PPI.InventoryList.Weapons;
		for (int i = 0; i < invWeapons.Count; i++)
		{
			PPIWeaponData w = invWeapons[i];
			if (w.ID == id)
			{
				invWeapons[i] = w;
				found = true;
				break;
			}
		}

		if (!found)
		{
			Debug.LogError("Weapon not found in inventory: " + id);
			return;
		}

		//pokud je predmet i v equipu tak je updatuj i tam
		List<PPIWeaponData> eqWeapons = PPI.EquipList.Weapons;
		for (int i = 0; i < eqWeapons.Count; i++)
		{
			PPIWeaponData w = eqWeapons[i];
			if (w.ID == id)
			{
				eqWeapons[i] = w;
				break;
			}
		}
	}

	void SetWeaponSlot(E_WeaponID id, int slot)
	{
		PPIInventoryList pil = PPI.InventoryList;
		List<PPIWeaponData> weapons = pil.Weapons;
		for (int i = 0; i < weapons.Count; i++)
		{
			PPIWeaponData w = weapons[i];
			if (w.ID == id)
			{
				w.EquipSlotIdx = slot;
				weapons[i] = w;
				return;
			}
		}
		Debug.LogError("Weapon not found in inventory: " + id);
	}

	internal PPIItemData GetOwnedItemData(E_ItemID id)
	{
		PPIInventoryList pil = PPI.InventoryList;
		List<PPIItemData> sets = pil.Items;
		foreach (PPIItemData s in sets)
		{
			if (s.ID == id)
			{
				return s;
			}
		}
		return new PPIItemData();
	}

	void SetOwnedItemCount(E_ItemID id, int newCount)
	{
		//inventory
		List<PPIItemData> invItems = PPI.InventoryList.Items;
		bool found = false;
		for (int i = 0; i < invItems.Count; i++)
		{
			PPIItemData itm = invItems[i];
			if (itm.ID == id)
			{
				itm.Count = newCount;
				invItems[i] = itm;
				found = true;
				break;
			}
		}

		if (!found)
		{
			Debug.LogError("Item not found in inventory: " + id);
			return;
		}

		//equip
		List<PPIItemData> eqItems = PPI.EquipList.Items;
		for (int i = 0; i < eqItems.Count; i++)
		{
			PPIItemData itm = eqItems[i];
			if (itm.ID == id)
			{
				itm.Count = newCount;
				eqItems[i] = itm;
				break;
			}
		}
	}

	PPIHatData GetOwnedHatData(E_HatID id)
	{
		PPIInventoryList pil = PPI.InventoryList;
		List<PPIHatData> sets = pil.Hats;
		foreach (PPIHatData s in sets)
		{
			if (s.ID == id)
			{
				return s;
			}
		}
		return new PPIHatData();
	}

	PPISkinData GetOwnedSkinData(E_SkinID id)
	{
		PPIInventoryList pil = PPI.InventoryList;
		List<PPISkinData> sets = pil.Skins;
		foreach (PPISkinData s in sets)
		{
			if (s.ID == id)
			{
				return s;
			}
		}
		return new PPISkinData();
	}

	void SetItemSlot(E_ItemID id, int slot)
	{
		PPIInventoryList pil = PPI.InventoryList;
		List<PPIItemData> items = pil.Items;
		for (int i = 0; i < items.Count; i++)
		{
			PPIItemData itm = items[i];
			if (itm.ID == id)
			{
				itm.EquipSlotIdx = slot;
				items[i] = itm;
				return;
			}
		}
		Debug.LogError("Item not found in inventory: " + id);
	}

	PPIBundleData GetOwnedBundleData(E_BundleID id)
	{
		PPIInventoryList pil = PPI.InventoryList;
		List<PPIBundleData> sets = pil.Bundles;
		foreach (PPIBundleData s in sets)
		{
			if (s.ID == id)
			{
				return s;
			}
		}
		return new PPIBundleData();
	}

	bool HasOwnedUpgrade(E_UpgradeID id)
	{
		UpgradeSettings ws = UpgradeSettingsManager.Instance.Get(id);

		//skip items that are only for premium account
		if (ws.PremiumOnly && !IsPremiumAccount())
			return false;

		PPIUpgradeList pil = PPI.Upgrades;
		List<PPIUpgradeList.UpgradeData> upgs = pil.Upgrades;
		foreach (PPIUpgradeList.UpgradeData s in upgs)
		{
			if (s.ID == id)
			{
				return true;
			}
		}
		return false;
	}

	PPIPerkData GetOwnedPerkData(E_PerkID id)
	{
		PPIInventoryList pil = PPI.InventoryList;
		List<PPIPerkData> sets = pil.Perks;
		foreach (PPIPerkData s in sets)
		{
			if (s.ID == id)
			{
				return s;
			}
		}
		return new PPIPerkData();
	}

	ShopItemInfo GetShopWeaponInfo(int id)
	{
		WeaponSettings ws = WeaponSettingsManager.Instance.Get((E_WeaponID)(id));
		if (ws != null)
		{
			ShopItemInfo inf = new ShopItemInfo();

			inf.NameText = TextDatabase.instance[ws.Name];
			inf.Description = ws.Description;
			inf.SpriteWidget = (ws.ShopWidget == null) ? MissingWidget : ws.ShopWidget;
			inf.WeaponTypeWidget = ws.TypeWidget;
			inf.Locked = false;
			inf.NewInShop = ws.NewInShop;
			inf.RareItem = ws.RareItem;
			inf.Owned = IsWeaponOwned((E_WeaponID)id);

			return inf;
		}

		Debug.LogError("Weapon not found: " + id);
		return new ShopItemInfo();
	}

	ShopItemInfo GetShopItemInfo(int id)
	{
		ItemSettings ws = ItemSettingsManager.Instance.Get((E_ItemID)id);
		if (ws != null)
		{
			ShopItemInfo inf = new ShopItemInfo();
			inf.NameText = TextDatabase.instance[ws.Name];
			inf.Description = ws.Description;
			inf.SpriteWidget = (ws.ShopWidget == null) ? MissingWidget : ws.ShopWidget;
			inf.GoldCurrency = (ws.GoldCost > 0);
			inf.Cost = (inf.GoldCurrency) ? ws.GoldCost : ws.MoneyCost;

			inf.Locked = false;
			inf.NewInShop = ws.NewInShop;
			inf.RareItem = ws.RareItem;
			inf.AddCount = ws.Count;
			inf.Consumable = ws.Consumable;
			inf.BoostDuration = ws.BoostTimer;
			inf.BoostModifier = ws.BoostModifier;

			PPIItemData s = GetOwnedItemData((E_ItemID)id);
			if (s.IsValid())
			{
				inf.OwnedCount = s.Count;
				inf.Owned = true;
			}

			if (ws.SaleInPercent > 0)
			{
				inf.CostBeforeSale = inf.Cost;
				inf.Cost = (int)(inf.Cost*(Mathf.Clamp(100 - ws.SaleInPercent, 0, 100)/100.0f));
				inf.PriceSale = true;
				const int strOff = 02030068;
				inf.DiscountTag = TextDatabase.instance[strOff];
				inf.DiscountTag = inf.DiscountTag.Replace("%d1", ws.SaleInPercent.ToString());
			}

			return inf;
		}
		Debug.LogError("Item not found: " + id);
		return new ShopItemInfo();
	}

	ShopItemInfo GetShopHatInfo(int id)
	{
		HatSettings ws = HatSettingsManager.Instance.Get((E_HatID)id);
		if (ws != null)
		{
			ShopItemInfo inf = new ShopItemInfo();
			inf.NameText = TextDatabase.instance[ws.Name];
			inf.Description = ws.Description;
			inf.SpriteWidget = ws.ShopWidget;
			inf.ScrollerWidget = ws.ScrollerWidget;

			inf.GoldCurrency = (ws.GoldCost > 0);
			inf.Cost = (inf.GoldCurrency) ? ws.GoldCost : ws.MoneyCost;
			inf.NewInShop = ws.NewInShop;
			inf.RareItem = ws.RareItem;
			inf.PremiumOnly = ws.PremiumOnly;
			inf.Locked = ws.PremiumOnly && !IsPremiumAccount();
			if (!inf.Locked)
			{
				inf.Owned = IsHatOwned((E_HatID)id);
			}

			if (ws.SaleInPercent > 0)
			{
				inf.CostBeforeSale = inf.Cost;
				inf.Cost = (int)(inf.Cost*(Mathf.Clamp(100 - ws.SaleInPercent, 0, 100)/100.0f));
				inf.PriceSale = true;
				const int strOff = 02030068;
				inf.DiscountTag = TextDatabase.instance[strOff];
				inf.DiscountTag = inf.DiscountTag.Replace("%d1", ws.SaleInPercent.ToString());
			}

			return inf;
		}
		Debug.LogError("Hat not found: " + id);
		return new ShopItemInfo();
	}

	bool IsHatOwned(E_HatID hatId)
	{
		PPIHatData s = GetOwnedHatData(hatId);
		return (s.IsValid());
	}

	ShopItemInfo GetShopSkinInfo(int id)
	{
		SkinSettings ws = SkinSettingsManager.Instance.Get((E_SkinID)id);
		if (ws != null)
		{
			ShopItemInfo inf = new ShopItemInfo();
			inf.NameText = TextDatabase.instance[ws.Name];
			inf.Description = ws.Description;
			inf.SpriteWidget = ws.ShopWidget;
			inf.ScrollerWidget = ws.ScrollerWidget;
			inf.GoldCurrency = (ws.GoldCost > 0);
			inf.Cost = (inf.GoldCurrency) ? ws.GoldCost : ws.MoneyCost;
			inf.NewInShop = ws.NewInShop;
			inf.RareItem = ws.RareItem;
			inf.PremiumOnly = ws.PremiumOnly;
			inf.Locked = ws.PremiumOnly && !IsPremiumAccount();
			if (!inf.Locked)
			{
				inf.Owned = IsSkinOwned((E_SkinID)id);
			}

			if (ws.SaleInPercent > 0)
			{
				inf.CostBeforeSale = inf.Cost;
				inf.Cost = (int)(inf.Cost*(Mathf.Clamp(100 - ws.SaleInPercent, 0, 100)/100.0f));
				inf.PriceSale = true;
				const int strOff = 02030068;
				inf.DiscountTag = TextDatabase.instance[strOff];
				inf.DiscountTag = inf.DiscountTag.Replace("%d1", ws.SaleInPercent.ToString());
			}

			return inf;
		}
		Debug.LogError("Skin not found: " + id);
		return new ShopItemInfo();
	}

	bool IsSkinOwned(E_SkinID skinId)
	{
		PPISkinData s = GetOwnedSkinData(skinId);
		return (s.IsValid());
	}

	bool IsWeaponOwned(E_WeaponID weaponId)
	{
		PPIWeaponData s = GetOwnedWeaponData(weaponId);
		return (s.IsValid());
	}

	bool IsItemOwned(E_ItemID itemId)
	{
		PPIItemData s = GetOwnedItemData(itemId);
		return (s.IsValid());
	}

	ShopItemInfo GetShopPerkInfo(int id)
	{
		PerkSettings ws = PerkSettingsManager.Instance.Get((E_PerkID)id);
		if (ws != null)
		{
			ShopItemInfo inf = new ShopItemInfo();
			inf.NameText = TextDatabase.instance[ws.Name];
			inf.Description = ws.Description;
			inf.SpriteWidget = (ws.ShopWidget == null) ? MissingWidget : ws.ShopWidget;
			inf.GoldCurrency = (ws.GoldCost > 0);
			inf.Cost = (inf.GoldCurrency) ? ws.GoldCost : ws.MoneyCost;

			PPIPerkData s = GetOwnedPerkData((E_PerkID)id);
			if (s.IsValid())
			{
				inf.Owned = true;
			}
			return inf;
		}
		Debug.LogError("Perk not found: " + id);
		return new ShopItemInfo();
	}

	ShopItemInfo GetShopUpgradeInfo(int id)
	{
		UpgradeSettings ws = UpgradeSettingsManager.Instance.Get((E_UpgradeID)id);
		if (ws != null)
		{
			ShopItemInfo inf = new ShopItemInfo();
			inf.NameText = TextDatabase.instance[ws.Name];
			inf.Description = ws.Description;
			inf.SpriteWidget = (ws.ShopWidget == null) ? MissingWidget : ws.ShopWidget;
			inf.GoldCurrency = (ws.GoldCost > 0);
			inf.Cost = (inf.GoldCurrency) ? ws.GoldCost : ws.MoneyCost;

			inf.Owned = HasOwnedUpgrade((E_UpgradeID)id);
			return inf;
		}
		Debug.LogError("Upgrade not found: " + id);
		return new ShopItemInfo();
	}

	ShopItemInfo GetShopAccountInfo(int id)
	{
		AccountSettings ws = AccountSettingsManager.Instance.Get((E_AccountID)id);
		if (ws != null)
		{
			ShopItemInfo inf = new ShopItemInfo();
			inf.NameText = GetSubscriptionName(ws);

			//inf.Description = ws.Description;
			inf.Owned = false;
			inf.SpriteWidget = (ws.ShopWidget == null) ? MissingWidget : ws.ShopWidget;

			inf.NewInShop = ws.NewInShop;
			inf.RareItem = ws.RareItem;

			if (ws.GoldCost > 0 || ws.MoneyCost > 0)
			{
				inf.GoldCurrency = (ws.GoldCost > 0);
				inf.Cost = (inf.GoldCurrency) ? ws.GoldCost : ws.MoneyCost;
			}

			return inf;
		}
		Debug.LogError("Account not found: " + id);
		return new ShopItemInfo();
	}

	ShopItemInfo GetShopTicketInfo(int id)
	{
		TicketSettings ws = TicketSettingsManager.Instance.Get((E_TicketID)id);
		if (ws != null)
		{
			ShopItemInfo inf = new ShopItemInfo();
			inf.NameText = TextDatabase.instance[ws.Name];
			inf.Description = ws.Description;
			inf.Owned = false;
			inf.SpriteWidget = (ws.ShopWidget == null) ? MissingWidget : ws.ShopWidget;
			inf.NewInShop = ws.NewInShop;

			if (ws.GoldCost > 0 || ws.MoneyCost > 0)
			{
				inf.GoldCurrency = (ws.GoldCost > 0);
				inf.Cost = (inf.GoldCurrency) ? ws.GoldCost : ws.MoneyCost;
			}

			return inf;
		}
		Debug.LogError("Ticket not found: " + id);
		return new ShopItemInfo();
	}

	ShopItemInfo GetShopBundleInfo(int id)
	{
		BundleSettings ws = BundleSettingsManager.Instance.Get((E_BundleID)id);
		if (ws != null)
		{
			ShopItemInfo inf = new ShopItemInfo();
			inf.NameText = TextDatabase.instance[ws.Name];
			inf.Description = ws.Description;
			inf.SpriteWidget = ws.ShopWidget;
			inf.GoldCurrency = (ws.GoldCost > 0);
			inf.Cost = (inf.GoldCurrency) ? ws.GoldCost : ws.MoneyCost;
			inf.NewInShop = ws.NewInShop;
			inf.RareItem = ws.RareItem;
			inf.BundleCount = BundleItemsCount(ws);
			inf.RequiresRank = ws.MinRank;
			inf.Locked = (PPI.Rank < ws.MinRank);

			inf.BundleOwnedCount = BundleItemsOwnedCount(ws);
			inf.BundleItems = GetBundleItems(ws);

			//bundle povazujeme za owned pokud jej jiz vlastnime, nebo pokud vlastnime vsechny predmety ktere obsahuje
			inf.Owned = (IsBundleOwned((E_BundleID)id) && !ws.Consumable) || (inf.BundleOwnedCount > 0 && inf.BundleOwnedCount == inf.BundleCount);

			if (ws.SaleInPercent > 0)
			{
				inf.CostBeforeSale = inf.Cost;
				inf.Cost = (int)(inf.Cost*(Mathf.Clamp(100 - ws.SaleInPercent, 0, 100)/100.0f));
				inf.PriceSale = true;
				const int strOff = 02030068;
				inf.DiscountTag = TextDatabase.instance[strOff];
				inf.DiscountTag = inf.DiscountTag.Replace("%d1", ws.SaleInPercent.ToString());
			}

			return inf;
		}
		Debug.LogError("Bundle not found: " + id);
		return new ShopItemInfo();
	}

	bool IsBundleOwned(E_BundleID id)
	{
		PPIBundleData s = GetOwnedBundleData(id);
		return (s.IsValid());
	}

	int BundleItemsCount(BundleSettings ws)
	{
		int res = 0;
		foreach (SettingsBase b in ws.Items)
		{
			if (b != null)
			{
				//skip funds and accounts
				if (b.GetSettingsClass() == "fund" || b.GetSettingsClass() == "account")
					continue;

				res++;
			}
		}

		return res;
	}

	int BundleItemsOwnedCount(BundleSettings ws)
	{
		int res = 0;
		foreach (SettingsBase b in ws.Items)
		{
			if (b == null)
				continue;

			switch (b.GetSettingsClass())
			{
			case "hat":
			{
				HatSettings hs = b as HatSettings;
				if (IsHatOwned(hs.ID))
					res++;
			}
				break;
			case "skin":
			{
				SkinSettings ss = b as SkinSettings;
				if (IsSkinOwned(ss.ID))
					res++;
			}
				break;
			case "weapon":
			{
				WeaponSettings ss = b as WeaponSettings;
				if (IsWeaponOwned(ss.ID))
					res++;
			}
				break;
			case "item":
			{
				ItemSettings ss = b as ItemSettings;
				if (IsItemOwned(ss.ID))
					res++;
			}
				break;
			case "upgrade":
			{
				UpgradeSettings ss = b as UpgradeSettings;
				if (HasOwnedUpgrade(ss.ID))
					res++;
			}
				break;
			case "fund":
			{
				//DO NOT COUNT FUND AS SOMETHING OWNED
			}
				break;

			case "account":
			{
				//DO NOT COUNT ACCOUNT AS SOMETHING OWNED
			}
				break;
			default:
			{
				Debug.LogError("Unexpected class in bundle: " + b.GetSettingsClass());
				continue;
			}
			}
		}

		return res;
	}

	List<ShopItemId> GetBundleItems(BundleSettings ws)
	{
		List<ShopItemId> retList = new List<ShopItemId>();

		foreach (SettingsBase b in ws.Items)
		{
			//skip empty
			if (b == null)
				continue;

			ShopItemId itemID = null;

			//cast to correct type, get name and other info
			switch (b.GetSettingsClass())
			{
			case "hat":
			{
				HatSettings hs = b as HatSettings;
				itemID = new ShopItemId((int)hs.ID, GuiShop.E_ItemType.Hat);
			}
				break;
			case "skin":
			{
				SkinSettings ss = b as SkinSettings;
				itemID = new ShopItemId((int)ss.ID, GuiShop.E_ItemType.Skin);
			}
				break;
			case "weapon":
			{
				WeaponSettings ss = b as WeaponSettings;
				itemID = new ShopItemId((int)ss.ID, GuiShop.E_ItemType.Weapon);
			}
				break;
			case "item":
			{
				ItemSettings ss = b as ItemSettings;
				itemID = new ShopItemId((int)ss.ID, GuiShop.E_ItemType.Item);
			}
				break;
			case "upgrade":
			{
				UpgradeSettings ss = b as UpgradeSettings;
				itemID = new ShopItemId((int)ss.ID, GuiShop.E_ItemType.Upgrade);
			}
				break;
			case "fund":
			{
				FundSettings ss = b as FundSettings;
				itemID = new ShopItemId((int)ss.ID, GuiShop.E_ItemType.Fund);
			}
				break;
			case "account":
			{
				AccountSettings ss = b as AccountSettings;
				itemID = new ShopItemId((int)ss.ID, GuiShop.E_ItemType.Account);
			}
				break;
			default:
			{
				Debug.LogError("Unexpected class in bundle: " + b.GetSettingsClass());
				continue;
			}
			}

			retList.Add(itemID);
		}

		return retList;
	}

	static void AddItemName(string itmName, bool firstItem, bool lastItem, ref string strResult)
	{
		//add contains text before first item and delimeter for the rest 
		if (firstItem)
		{
			int textContains = 0214001;
			strResult = TextDatabase.instance[textContains] + " ";
		}
		else if (lastItem)
		{
			int textAnd = 0214003;
			strResult += " " + TextDatabase.instance[textAnd] + " ";
		}
		else
		{
			strResult += ", ";
		}

		//add name to list
		strResult += itmName;

		if (lastItem)
		{
			strResult += ".";
		}
	}

	//zjisti nazev subscription
	static string GetSubscriptionName(AccountSettings ws)
	{
		CloudServices.PremiumAccountDesc[] accounts = CloudUser.instance.availablePremiumAccounts;
		CloudServices.PremiumAccountDesc acct = System.Array.Find(accounts, acc => acc.m_Id == ws.PremiumAccountId);

		if (acct != null)
		{
			System.TimeSpan Duration = System.TimeSpan.FromMinutes(acct.m_DurationInMinutes);

			int hours = Mathf.RoundToInt((float)Duration.TotalHours);
			int days = Mathf.RoundToInt((float)Duration.TotalDays);
			int weeks = Mathf.RoundToInt(days/7.0f);
			int months = Mathf.RoundToInt(weeks/4.0f);

			if (hours == 1)
				return string.Format(TextDatabase.instance[0214200], hours);
			else if (hours < 24)
				return string.Format(TextDatabase.instance[0214201], hours);
			else if (days == 1)
				return string.Format(TextDatabase.instance[0214202], days);
			else if (days < 7)
				return string.Format(TextDatabase.instance[0214203], days);
			else if (weeks == 1)
				return string.Format(TextDatabase.instance[0214204], weeks);
			else if (weeks < 4)
				return string.Format(TextDatabase.instance[0214205], weeks);
			else if (months == 1)
				return string.Format(TextDatabase.instance[0214206], months);
			else
				return string.Format(TextDatabase.instance[0214207], months);
		}
		return null;
	}

	public static string GetFundName(FundSettings ws)
	{
		string nameText = TextDatabase.instance[ws.Name];
		//format string for gold and money from actual numbers
		if (nameText.Contains("%i1"))
			nameText = nameText.Replace("%i1", (ws.AddGold > 0) ? ws.AddGold.ToString() : ws.AddMoney.ToString());

		return nameText;
	}

	ShopItemInfo GetShopFundInfo(int id)
	{
		FundSettings ws = FundSettingsManager.Instance.Get((E_FundID)id);
		if (ws != null)
		{
			ShopItemInfo inf = new ShopItemInfo();
			inf.NameText = GetFundName(ws);

			inf.Description = ws.Description;
			inf.Owned = false;
			inf.SpriteWidget = (ws.ShopWidget == null) ? MissingWidget : ws.ShopWidget;
			inf.ScrollerWidget = ws.ScrollerWidget;

			inf.AddGold = ws.AddGold;
			inf.AddMoney = ws.AddMoney;
			inf.NewInShop = ws.NewInShop;
			inf.RareItem = ws.RareItem;

			int guid = GetShopItemGUID(new ShopItemId(id, GuiShop.E_ItemType.Fund));
#pragma warning disable 219
			string productId = guid.ToString();
#pragma warning restore 219			

#if (UNITY_IPHONE || UNITY_ANDROID || UNITY_STANDALONE_OSX) && !UNITY_EDITOR

			if (InAppPurchaseMgr.Instance.Inventory.IsProductAvailable(productId))
			{
				InAppProduct product = InAppPurchaseMgr.Instance.Inventory.Product(productId);
				inf.IAPCost = product.Price + " " + product.CurrencyCode;
			}
			else
			{
				inf.IAPCost = "N/A";	
			}
			
#else
			inf.IAPCost = TextDatabase.instance[02030094];
#endif

			if (ws.GoldCost > 0 || ws.MoneyCost > 0)
			{
				inf.GoldCurrency = (ws.GoldCost > 0);
				inf.Cost = (inf.GoldCurrency) ? ws.GoldCost : ws.MoneyCost;
			}

			int saleInPercent = ws.SaleInPercent;

			if (saleInPercent > 0)
			{
				if (ws.AvailableAsIAPOnly)
				{
					inf.PriceSale = true;
					const int strSale = 02030064;
					inf.DiscountTag = TextDatabase.instance[strSale];
					inf.DiscountTag = inf.DiscountTag.Replace("%d1", saleInPercent.ToString());
				}
				else
				{
					inf.CostBeforeSale = inf.Cost;
					inf.Cost = (int)(inf.Cost*(Mathf.Clamp(100 - saleInPercent, 0, 100)/100.0f));
					inf.PriceSale = true;
					const int strOff = 02030068;
					inf.DiscountTag = TextDatabase.instance[strOff];
					inf.DiscountTag = inf.DiscountTag.Replace("%d1", saleInPercent.ToString());
				}
			}

			return inf;
		}
		Debug.LogError("Fund not found: " + id);
		return new ShopItemInfo();
	}

	//vraci true pokud je slot zamceny
	public bool IsWeaponSlotLocked(int slotIndex)
	{
		//prvni slot je vzdy odemceny
		int unlockedCount = 1;

		//dalsi sloty zavisi od upgradu
		if (HasOwnedUpgrade(E_UpgradeID.WeaponSlot1))
			unlockedCount++;

		if (HasOwnedUpgrade(E_UpgradeID.WeaponSlot2))
			unlockedCount++;

		return (slotIndex >= unlockedCount);
	}

	public bool IsPremiumWeaponSlot(int slotIndex)
	{
		int premiumSlotIndex = !HasOwnedUpgrade(E_UpgradeID.WeaponSlot1) && HasOwnedUpgrade(E_UpgradeID.WeaponSlot2) ? 1 : 2;
		return (slotIndex == premiumSlotIndex);
	}

	public bool IsPerkSlotLocked()
	{
		return (PPI.InventoryList.Perks.Count <= 0);
	}

	//vraci true pokud je slot zamceny
	public bool IsItemSlotLocked(int slotIndex)
	{
		//prvni slot je vzdy odemceny
		int unlockedCount = 1;

		//dalsi sloty zavisi od upgradu
		if (HasOwnedUpgrade(E_UpgradeID.ItemSlot1))
			unlockedCount++;

		if (HasOwnedUpgrade(E_UpgradeID.ItemSlot2))
			unlockedCount++;

		return (slotIndex >= unlockedCount);
	}

	public bool IsPremiumItemSlot(int slotIndex)
	{
		int premiumSlotIndex = !HasOwnedUpgrade(E_UpgradeID.ItemSlot1) && HasOwnedUpgrade(E_UpgradeID.ItemSlot2) ? 1 : 2;
		return (slotIndex == premiumSlotIndex);
	}

	//jestli mame pro koupi dostake penez
	public bool HaveEnoughMoney(ShopItemId itemId, int upgradeId)
	{
		int fundsMissing;
		bool isGold;
		MissingFunds(itemId, upgradeId, out fundsMissing, out isGold);
		return (fundsMissing <= 0);
	}

	public void RequiredFunds(ShopItemId itemId, int upgradeID, out int fundsNeeded, out bool isGold)
	{
		if (itemId.ItemType == GuiShop.E_ItemType.Weapon && upgradeID > -1)
		{
			ResearchItem item = System.Array.Find(ResearchSupport.Instance.GetItems(), obj => (int)obj.weaponID == itemId.Id);
			WeaponSettings.Upgrade upgrade = item != null ? item.GetUpgrade(upgradeID) : null;
			if (upgrade != null)
			{
				fundsNeeded = upgrade.GoldCost;
				isGold = true;
			}
			else
			{
				fundsNeeded = 0;
				isGold = false;
			}
		}
		else
		{
			ShopItemInfo inf = GetItemInfo(itemId);
			isGold = inf.GoldCurrency;
			fundsNeeded = inf.Cost;
		}
	}

	public ShopItemId GetIAPNeededForItem(ShopItemId itemId, int upgradeId)
	{
		int fundsNeeded;
		bool gold;

		ShopDataBridge.Instance.RequiredFunds(itemId, upgradeId, out fundsNeeded, out gold);

		//offer user appropriete fund item to cover funds he is missing
		ShopItemId buyId = ShopDataBridge.Instance.FindFundsItem(fundsNeeded, gold);

		return buyId;
	}

	//vraci true pokud nemame dostatek penez pro kopi predmetu. parametr fundsMissin vraci castku ktra nam pro koupi chybi.
	void MissingFunds(ShopItemId itemId, int upgradeID, out int fundsMissing, out bool isGold)
	{
		int fundsNeeded;
		RequiredFunds(itemId, upgradeID, out fundsNeeded, out isGold);

		//odecti kolik ma hrac k dispozici
		fundsMissing = fundsNeeded - (isGold ? PlayerGold : PlayerMoney);

		// show status bar hint
		if (fundsMissing > 0)
		{
			GuiBaseUtils.PendingHint = isGold ? E_Hint.Gold : E_Hint.Money;
		}
	}

	//najde polozku s neblizsim vetsim funds objemem. 
	//soucasna verze skipuje convertovani zlata, vyzadovalo by dalsi dialogy a testy.
	public ShopItemId FindFundsItem(int fundsRequest, bool isGold)
	{
		E_FundID bestId = E_FundID.None;
		int bestFunds = 0;
		FundSettings[] fundsAll = FundSettingsManager.Instance.GetAll();
		foreach (FundSettings fs in fundsAll)
		{
			bool currentIsGold = (fs.AddGold > 0);
			int currentFunds = currentIsGold ? fs.AddGold : fs.AddMoney;
			if (fs.MoneyCost > 0 || fs.GoldCost > 0)
			{
				//TODO: pokud bychom chteli nechat vybrat convert polozku, museli bychom zde kontrolovat zda meme dostatek goldu na converzi
				continue;
			}

			//skip different currency
			if (isGold != currentIsGold)
				continue;

			//pokud jsme jeste nenasli castku vyssi nebo stejnou jako pozadovanou, a soucastna je vetsi nez minula, zapamatuj si ji
			if (bestFunds < fundsRequest && currentFunds > bestFunds)
			{
				bestFunds = currentFunds;
				bestId = fs.ID;
			}
			//pokud uz jsme nalezli vyssi castku nez pozadovanou, a soucastna castka je nizsi nez nalezena, a pritom stale vetsi nez pozadovana, dej ji prednost
			else if (bestFunds > fundsRequest && currentFunds >= fundsRequest && currentFunds < bestFunds)
			{
				bestFunds = currentFunds;
				bestId = fs.ID;
			}
		}

		Debug.Log("Best funds found: " + bestId + " requested " + fundsRequest);

		if (bestId != E_FundID.None)
			return new ShopItemId((int)bestId, GuiShop.E_ItemType.Fund);
		else
			return ShopItemId.EmptyId;
	}

	public void Debug_LogEquipedWeapons()
	{
		Debug.Log("Equiped Weapons");
		PPIEquipList eq = PPI.EquipList;
		foreach (PPIWeaponData w in eq.Weapons)
		{
			Debug.Log(" - " + w.ID + " , slot: " + w.EquipSlotIdx);
		}
	}

	public void Debug_LogOwnedWeapons()
	{
		//Debug.Log("Owned Weapons:");
		PPIInventoryList pil = PPI.InventoryList;
		List<PPIWeaponData> weapons = pil.Weapons;
		foreach (PPIWeaponData w in weapons)
		{
			Debug.Log(" - " + w.ID);
		}
	}

	public void Debug_LogOwnedItems()
	{
		Debug.Log("Owned Items:");
		PPIInventoryList pil = PPI.InventoryList;
		List<PPIItemData> items = pil.Items;
		foreach (PPIItemData w in items)
		{
			Debug.Log(" - " + w.ID);
		}
	}

	public void Debug_LogEquipedItems()
	{
		Debug.Log("Equiped Items");
		PPIEquipList eq = PPI.EquipList;
		foreach (PPIItemData w in eq.Items)
		{
			Debug.Log(" - " + w.ID + " , slot: " + w.EquipSlotIdx);
		}
	}

	/*public void Debug_LogOwnedUpgrades()
	{
		Debug.Log("DebugOwned Upgrades:");
		PPIUpgradeList pil = PPI.Upgrades;
		List<E_UpgradeID> upgs = pil.Upgrades;
		foreach (E_UpgradeID u in upgs)
		{
			Debug.Log("Have upgrade: " + u);
		}
	}*/

	bool IsWeaponEquiped(int id)
	{
		PPIEquipList eq = PPI.EquipList;
		foreach (PPIWeaponData w in eq.Weapons)
		{
			if (w.ID == (E_WeaponID)id)
				return true;
		}
		return false;
	}

	bool IsItemEquiped(int id)
	{
		PPIEquipList eq = PPI.EquipList;
		//Debug.Log("eq: " + eq + " items: " + eq.Items.Count);
		foreach (PPIItemData w in eq.Items)
		{
			//Debug.Log("w: " + w);
			if (w.ID == (E_ItemID)id)
				return true;
		}
		return false;
	}

	bool IsSkinAssigned(int id)
	{
		PPIEquipList eq = PPIManager.Instance.GetLocalPPI().EquipList;
		return eq.Outfits.Skin == (E_SkinID)id;
	}

	bool IsHatAssigned(int id)
	{
		PPIEquipList eq = PPIManager.Instance.GetLocalPPI().EquipList;
		return eq.Outfits.Hat == (E_HatID)id;
	}

	bool IsPerkAssigned(int id)
	{
		PPIEquipList eq = PPIManager.Instance.GetLocalPPI().EquipList;
		return eq.Perk == (E_PerkID)id;
	}

	public bool IsEquiped(ShopItemId itemId)
	{
		switch (itemId.ItemType)
		{
		case GuiShop.E_ItemType.Weapon:
		{
			return IsWeaponEquiped(itemId.Id);
		}
		case GuiShop.E_ItemType.Item:
		{
			return IsItemEquiped(itemId.Id);
		}
		case GuiShop.E_ItemType.Hat:
		{
			return IsHatAssigned(itemId.Id);
		}
		case GuiShop.E_ItemType.Skin:
		{
			return IsSkinAssigned(itemId.Id);
		}
		case GuiShop.E_ItemType.Perk:
		{
			return IsPerkAssigned(itemId.Id);
		}
		}
		return false;
	}

	public int GetShopItemGroup(ShopItemId itm)
	{
		switch (itm.ItemType)
		{
		case GuiShop.E_ItemType.Weapon:
		{
			WeaponSettings st = WeaponSettingsManager.Instance.Get((E_WeaponID)(itm.Id));
			return st.EquipGroup;
		}
		case GuiShop.E_ItemType.Item:
		{
			ItemSettings st = ItemSettingsManager.Instance.Get((E_ItemID)(itm.Id));
			return st.EquipGroup;
		}
		case GuiShop.E_ItemType.Skin:
		{
			SkinSettings st = SkinSettingsManager.Instance.Get((E_SkinID)(itm.Id));
			return st.EquipGroup;
		}
		case GuiShop.E_ItemType.Hat:
		{
			HatSettings st = HatSettingsManager.Instance.Get((E_HatID)(itm.Id));
			return st.EquipGroup;
		}
		case GuiShop.E_ItemType.Fund:
		{
			FundSettings st = FundSettingsManager.Instance.Get((E_FundID)(itm.Id));
			return st.EquipGroup;
		}
		case GuiShop.E_ItemType.Perk:
		{
			PerkSettings st = PerkSettingsManager.Instance.Get((E_PerkID)(itm.Id));
			return st.EquipGroup;
		}
		case GuiShop.E_ItemType.Bundle:
		{
			BundleSettings st = BundleSettingsManager.Instance.Get((E_BundleID)(itm.Id));
			return st.EquipGroup;
		}
		case GuiShop.E_ItemType.Ticket:
		{
			TicketSettings st = TicketSettingsManager.Instance.Get((E_TicketID)(itm.Id));
			return st.EquipGroup;
		}
		case GuiShop.E_ItemType.Upgrade:
		{
			UpgradeSettings st = UpgradeSettingsManager.Instance.Get((E_UpgradeID)(itm.Id));
			return st.EquipGroup;
		}
		default:
			Debug.LogError("TODO: unhandled item type: " + itm);
			return 0;
		}
	}

	ShopItemId GetEquipedGroupWeapon(ShopItemId itemId)
	{
		int itmGroup = GetShopItemGroup(itemId);
		PPIEquipList eq = PPI.EquipList;
		foreach (PPIWeaponData w in eq.Weapons)
		{
			if (WeaponSettingsManager.Instance.Get(w.ID).EquipGroup == itmGroup)
				return new ShopItemId((int)w.ID, itemId.ItemType);
		}
		return ShopItemId.EmptyId;
	}

	ShopItemId GetEquipedGroupItem(ShopItemId itemId)
	{
		int itmGroup = GetShopItemGroup(itemId);
		PPIEquipList eq = PPI.EquipList;
		foreach (PPIItemData w in eq.Items)
		{
			if (ItemSettingsManager.Instance.Get(w.ID).EquipGroup == itmGroup)
				return new ShopItemId((int)w.ID, itemId.ItemType);
		}
		return ShopItemId.EmptyId;
	}

	public ShopItemId EquipedGroupItem(ShopItemId itemId)
	{
		if (itemId.IsEmpty() || GetShopItemGroup(itemId) <= 0)
			return ShopItemId.EmptyId;

		switch (itemId.ItemType)
		{
		case GuiShop.E_ItemType.Weapon:
		{
			return GetEquipedGroupWeapon(itemId);
		}
		case GuiShop.E_ItemType.Item:
		{
			return GetEquipedGroupItem(itemId);
		}
		case GuiShop.E_ItemType.Hat:
		{
			return ShopItemId.EmptyId;
		}
		case GuiShop.E_ItemType.Skin:
		{
			return ShopItemId.EmptyId;
		}
		case GuiShop.E_ItemType.Perk:
		{
			return ShopItemId.EmptyId;
		}
		}
		return ShopItemId.EmptyId;
	}

	public bool IsGroupItemEquiped(ShopItemId itemId)
	{
		return EquipedGroupItem(itemId).IsEmpty();
	}

	public int PlayerXP
	{
		get { return PPI.Experience; }
	}

	public int PlayerLevel
	{
		get { return PPI.Rank; }
	}

	public int PlayerGold
	{
		get { return PPI.Gold; }
	}

	public int PlayerMoney
	{
		get { return PPI.Money; }
	}

	public string PlayerName
	{
		get { return PPI.Name; }
	}

	public GameObject GetOutfitModel(ShopItemId id)
	{
		if (id == ShopItemId.EmptyId)
			return null;

		GameObject go = null;
		switch (id.ItemType)
		{
		case GuiShop.E_ItemType.Skin:
		{
			SkinSettings ws = SkinSettingsManager.Instance.Get((E_SkinID)id.Id);
			if (ws.Model != null)
				go = GameObject.Instantiate(ws.Model) as GameObject;
		}
			break;

		case GuiShop.E_ItemType.Hat:
		{
			HatSettings ws = HatSettingsManager.Instance.Get((E_HatID)id.Id);
			if (ws.Model != null)
				go = GameObject.Instantiate(ws.Model) as GameObject;
		}
			break;

		case GuiShop.E_ItemType.Weapon:
		{
			WeaponSettings ws = WeaponSettingsManager.Instance.Get((E_WeaponID)id.Id);
			if (ws.Model != null)
				go = GameObject.Instantiate(ws.Model) as GameObject;
		}
			break;

		default:
		{
			Debug.LogError("Unexpected type: " + id);
		}
			break;
		}
		return go;
	}

	public ShopItemId GetPlayerSkin()
	{
		E_SkinID skinId = PPIManager.Instance.GetLocalPPI().EquipList.Outfits.Skin;
		//Debug.Log("GetPlayerSkin: " + skinId);
		return new ShopItemId((int)skinId, GuiShop.E_ItemType.Skin);
	}

	public ShopItemId GetPlayerHat()
	{
		E_HatID hatId = PPIManager.Instance.GetLocalPPI().EquipList.Outfits.Hat;

		if (hatId == E_HatID.None)
			return ShopItemId.EmptyId;
		else
			return new ShopItemId((int)hatId, GuiShop.E_ItemType.Hat);
	}

	public ShopItemId GetPlayerPerk()
	{
		E_PerkID perkId = PPIManager.Instance.GetLocalPPI().EquipList.Perk;

		if (perkId == E_PerkID.None)
			return ShopItemId.EmptyId;
		else
			return new ShopItemId((int)perkId, GuiShop.E_ItemType.Perk);
	}

	public int GetShopItemGUID(ShopItemId itm)
	{
		switch (itm.ItemType)
		{
		case GuiShop.E_ItemType.Weapon:
		{
			WeaponSettings st = WeaponSettingsManager.Instance.Get((E_WeaponID)(itm.Id));
			return st.GUID;
		}
		case GuiShop.E_ItemType.Item:
		{
			ItemSettings st = ItemSettingsManager.Instance.Get((E_ItemID)(itm.Id));
			return st.GUID;
		}
		case GuiShop.E_ItemType.Skin:
		{
			SkinSettings st = SkinSettingsManager.Instance.Get((E_SkinID)(itm.Id));
			return st.GUID;
		}
		case GuiShop.E_ItemType.Hat:
		{
			HatSettings st = HatSettingsManager.Instance.Get((E_HatID)(itm.Id));
			return st.GUID;
		}
		case GuiShop.E_ItemType.Fund:
		{
			FundSettings st = FundSettingsManager.Instance.Get((E_FundID)(itm.Id));
			return st.GUID;
		}
		case GuiShop.E_ItemType.Perk:
		{
			PerkSettings st = PerkSettingsManager.Instance.Get((E_PerkID)(itm.Id));
			return st.GUID;
		}
		case GuiShop.E_ItemType.Bundle:
		{
			BundleSettings st = BundleSettingsManager.Instance.Get((E_BundleID)(itm.Id));
			return st.GUID;
		}
		case GuiShop.E_ItemType.Ticket:
		{
			TicketSettings st = TicketSettingsManager.Instance.Get((E_TicketID)(itm.Id));
			return st.GUID;
		}
		case GuiShop.E_ItemType.Upgrade:
		{
			UpgradeSettings st = UpgradeSettingsManager.Instance.Get((E_UpgradeID)(itm.Id));
			return st.GUID;
		}
		default:
			Debug.LogError("TODO: unhandled item type: " + itm);
			return 0;
		}
	}

	public ShopItemId GetWeaponInSlot(int slotIdx)
	{
		PPIEquipList eq = PPI.EquipList;
		foreach (PPIWeaponData w in eq.Weapons)
		{
			if (w.EquipSlotIdx == slotIdx)
				return new ShopItemId((int)w.ID, GuiShop.E_ItemType.Weapon);
		}
		return ShopItemId.EmptyId;
	}

	public ShopItemId GetItemInSlot(int slotIdx)
	{
		PPIEquipList eq = PPI.EquipList;
		foreach (PPIItemData w in eq.Items)
		{
			if (w.EquipSlotIdx == slotIdx)
				return new ShopItemId((int)w.ID, GuiShop.E_ItemType.Item);
		}
		return ShopItemId.EmptyId;
	}

	static int CompareWeaponData(PPIWeaponData x, PPIWeaponData y)
	{
		return x.EquipSlotIdx.CompareTo(y.EquipSlotIdx);
	}

	static int CompareItemData(PPIItemData x, PPIItemData y)
	{
		return x.EquipSlotIdx.CompareTo(y.EquipSlotIdx);
	}

	public bool IsIAPFund(ShopItemId itemId)
	{
		if (itemId.ItemType != GuiShop.E_ItemType.Fund || IsFreeGold(itemId))
			return false;

		FundSettings s = FundSettingsManager.Instance.Get((E_FundID)itemId.Id);
		return s.AvailableAsIAPOnly;
	}

	public bool IAPServiceAvailable()
	{
		if (SimulateIAP)
			return true;

		return InAppPurchaseMgr.Instance.CurrentState < InAppPurchaseMgr.STATE_FAILURE;
	}

	public void IAPRequestPurchase(ShopItemId item)
	{
		//Debug.Log("Requesting AIPurchase " + item);

		if (item == null)
		{
			Debug.Log("ShopDataBridge->IAPRequestPurchase: Item is null.");

			m_IAPRequestStatus = new InAppAsyncOpResult();
			m_IAPRequestStatus.CurrentState = InAppAsyncOpState.Failed;
			return;
		}

		if (IsIAPInProgress())
		{
			Debug.LogError("AIP Request already running!");
			return;
		}

		int guid = GetShopItemGUID(item);
		if (guid == 0)
		{
			Debug.LogError("Item " + item + " do not have set IAP guid!");
			return;
		}

		if (SimulateIAP)
		{
			Debug.Log("ShopDataBridge->IAPRequestPurchase: SimulateIAP");

			m_SimulateIAPAction = new BuyAndFetchPPI(CloudUser.instance.authenticatedUserID, guid);
			GameCloudManager.AddAction(m_SimulateIAPAction);
		}
		else
		{
			try
			{
				m_IAPRequestStatus = null;
				InAppProduct product = InAppPurchaseMgr.Instance.Inventory.Product(guid.ToString());

				if (product != null)
				{
					m_IAPRequestStatus = InAppPurchaseMgr.Instance.RequestPurchaseProduct(product);
				}
				else
				{
					Debug.Log("ShopDataBridge->IAPRequestPurchase: Product " + guid.ToString() + " is not contained in the InAppPurchaseMgr Inventory.");

					m_IAPRequestStatus = new InAppAsyncOpResult();
					m_IAPRequestStatus.CurrentState = InAppAsyncOpState.Failed;
					return;
				}
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
			}
		}
	}

	public void IAPCleanRequest()
	{
		m_IAPRequestStatus = null;
		m_SimulateIAPAction = null;
	}

	public bool IsFreeGold(ShopItemId item)
	{
		if (item.ItemType != GuiShop.E_ItemType.Fund)
			return false;

		E_FundID fundId = (E_FundID)(item.Id);
		return (ShopItemId.FreeGoldType(fundId) > 0);
	}

	public bool IsFundConvertor(ShopItemId item)
	{
		return false;
	}

	public bool NewItemsUnlocked(int rank)
	{
		return false;
	}

	public ShopItemId GetPremiumAcct(string acctId)
	{
		AccountSettings[] settings = AccountSettingsManager.Instance.GetAll();
		AccountSettings acctSettings = System.Array.Find(settings, obj => obj.PremiumAccountId == acctId);
		return acctSettings != null ? new ShopItemId((int)acctSettings.ID, GuiShop.E_ItemType.Account) : ShopItemId.EmptyId;
	}

	bool IsPremiumAccount()
	{
		return CloudUser.instance.isPremiumAccountActive;
	}
};
