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

public class UserGuideAction_Offers : UserGuideAction_SystemDialogs<GuiPopupOffer>
{
	public class ItemDesc
	{
		public string Id;
		public ResearchItem Item;
		public int Rank;
		public bool Owned;
		public int Used;
		public int Price;
	}

	public class HatDesc
	{
		public string Id;
		public HatSettings Item;
		public int Price;
	}

	public class ConsumableDesc
	{
		public string Id;
		public ItemSettings Item;
		public int Price;
	}

	// C-TOR

	public UserGuideAction_Offers()
	{
		Priority = (int)E_UserGuidePriority.Offers;
	}

	// USERGUIDEACTION INTERFACE

	protected override bool OnExecute()
	{
		if (base.OnExecute() == false)
			return false;

		if (GuideData.ShowOffers == false)
			return false;

		var ppi = GuideData.LocalPPI;
		if (ppi == null)
			return false;

		// get offer
		object data;
		GuiPopupOffer.E_Type type = DeduceOffer(ppi, GuideData.LastRoundResult, out data);
		if (type == GuiPopupOffer.E_Type.None)
			return false;

#if UNITY_STANDALONE
		if (type == GuiPopupOffer.E_Type.FreeGold)
			return false;
#endif
		// display popup
		ShowPopup().SetData(type, data);

		// done
		return true;
	}

	// PRIVATE METHODS

	GuiPopupOffer.E_Type DeduceOffer(PlayerPersistantInfo ppi, RoundFinalResult results, out object data)
	{
		data = null;

		// Do not display these offers for experienced premium players
		if (ppi.IsPremiumAccountActive && (ppi.Rank >= 35))
			return GuiPopupOffer.E_Type.None;

		short earnedMoney = results != null ? results.Money : (short)0;
		short earnedGold = results != null ? results.Gold : (short)0;
		bool hasNewRank = results != null ? results.NewRank : false;
		int preferredRank = hasNewRank == true ? ppi.Rank : 0;
		bool showNonItem = Random.Range(0, 100) < 10 ? true : false;
		GuiPopupOffer.E_Type type = GuiPopupOffer.E_Type.None;

		// try get any item to offer
		if ((hasNewRank == true || showNonItem == false) && (earnedMoney > 0 || earnedGold > 0 || Random.Range(0, 100) < 20))
		{
			if (type == GuiPopupOffer.E_Type.None && Random.Range(0, 100) < 10)
			{
				List<HatDesc> hats = CollectHats(ppi, results);
				if (hats.Count > 0)
				{
					hats.Sort(SortHats);

					string lastId = RestoreString("LastOfferedHat", null);
					string newId = null;

					data = ChooseHat(ref hats, lastId, ppi.Gold, out newId);

					StoreString("LastOfferedHat", newId);

					if (data != null)
					{
						// display hat offer
						type = GuiPopupOffer.E_Type.Hat;
					}
				}
			}

			if (type == GuiPopupOffer.E_Type.None && Random.Range(0, 100) < 40)
			{
				List<ItemDesc> items = CollectItems(ppi, results);
				if (items.Count > 0)
				{
					items.Sort(SortItems);

					string lastId = RestoreString("LastOfferedItem", null);
					string newId = null;

					data = ChooseItem(ref items, lastId, ppi.Money, preferredRank, out newId);

					StoreString("LastOfferedItem", newId);

					if (data != null)
					{
						// display item offer
						type = GuiPopupOffer.E_Type.Item;
					}
				}
			}

			if (type == GuiPopupOffer.E_Type.None)
			{
				List<ConsumableDesc> consumables = CollectConsumables(ppi, results);
				if (consumables.Count > 0)
				{
					string lastId = RestoreString("LastOfferedConsumable", null);
					string newId = null;

					data = ChooseConsumable(ref consumables, lastId, ppi.Gold, out newId);

					StoreString("LastOfferedConsumable", newId);

					if (data != null)
					{
						// display consumable offer
						type = GuiPopupOffer.E_Type.Consumable;
					}
				}
			}

			if (type == GuiPopupOffer.E_Type.None)
			{
				// increase the chance to display any non-item offer
				showNonItem = Random.Range(0, 100) < 10 ? true : false;
			}
		}

		// deduce non-item offer if needed
		if (showNonItem == true && type == GuiPopupOffer.E_Type.None)
		{
			GuiPopupOffer.E_Type[] types = new GuiPopupOffer.E_Type[]
			{
				GuiPopupOffer.E_Type.PremiumAcct,
				GuiPopupOffer.E_Type.MoreApps,
				GuiPopupOffer.E_Type.FreeGold
			};

			type = types[Random.Range(0, types.Length)];
			if (type == GuiPopupOffer.E_Type.PremiumAcct)
			{
				if (ppi.Gold == 0)
				{
					// wo do not have any gold so offer free gold instead
					type = GuiPopupOffer.E_Type.FreeGold;
				}
				else if ((CloudUser.instance.GetPremiumAccountEndDateTime() - CloudDateTime.UtcNow).TotalDays > 7)
				{
					// user has enough time left, skip this offer for now
					type = GuiPopupOffer.E_Type.None;
				}
			}
		}

		// done
		return type;
	}

	List<HatDesc> CollectHats(PlayerPersistantInfo ppi, RoundFinalResult results)
	{
		int golds = ppi.Gold;
		var result = new List<HatDesc>();
		var hats = HatSettingsManager.Instance.GetAll();

		foreach (var hat in hats)
		{
			if (hat.GoldCost > golds)
				continue;

			if (hat.DISABLED == true || hat.AvailableInShop == false || hat.BundleOnly == true)
				continue;

			ShopItemInfo info = ShopDataBridge.Instance.GetItemInfo(new ShopItemId((int)hat.ID, GuiShop.E_ItemType.Hat));
			if (info.Owned == true)
				continue;

			result.Add(new HatDesc()
			{
				Id = hat.ID.ToString(),
				Item = hat,
				Price = hat.GoldCost
			});
		}

		return result;
	}

	List<ConsumableDesc> CollectConsumables(PlayerPersistantInfo ppi, RoundFinalResult results)
	{
		int money = ppi.Money;
		int gold = ppi.Gold;
		var result = new List<ConsumableDesc>();
		var items = ItemSettingsManager.Instance.GetAll();

		foreach (var item in items)
		{
			if (item.AvailableInShop == false)
				continue;
			if (item.Consumable == false)
				continue;

			bool isGold = item.GoldCost > 0;
			int price = isGold ? item.GoldCost : item.MoneyCost;
			bool canBuy = isGold ? item.GoldCost <= gold : item.MoneyCost <= money;
			if (canBuy == false)
				continue;

			result.Add(new ConsumableDesc()
			{
				Id = item.ID.ToString(),
				Item = item,
				Price = price,
			});
		}

		return result;
	}

	List<ItemDesc> CollectItems(PlayerPersistantInfo ppi, RoundFinalResult results)
	{
		int money = ppi.Money;
		int gold = ppi.Gold;
		var result = new List<ItemDesc>();
		var items = ResearchSupport.Instance.GetItems();

		int currentRank = ppi.Rank;
		int minimalRank = currentRank - Mathf.RoundToInt(PlayerPersistantInfo.MAX_RANK*0.4f);

		foreach (var item in items)
		{
			int desiredRank = item.GetRequiredRank();
			if (desiredRank > currentRank)
				continue;
			if (desiredRank < minimalRank)
				continue;
			if (CanOffer(item) == false)
				continue;

			ResearchState state = item.GetState();
			if (state == ResearchState.Unavailable)
				continue;

			bool isGold;
			int price = item.GetPrice(out isGold);
			bool canBuy = isGold ? price <= gold : price <= money;
			if (state == ResearchState.Available && canBuy == false)
				continue;

			//List<WeaponSettings.Upgrade> upgrades = CollectUpgrades(item, money);
			if (state == ResearchState.Active /*&& upgrades.Count == 0*/)
				continue;

			ItemSettings itemSettings = GetItem(item);
			if (itemSettings != null && itemSettings.Consumable == true)
				continue;

			result.Add(new ItemDesc()
			{
				Id = GetId(item),
				Item = item,
				Rank = desiredRank,
				Owned = state == ResearchState.Active ? true : false,
				Used = GetUsedTimes(item, ppi, results),
				Price = price,
				//Upgrades = upgrades
			});
		}

		return result;
	}

	ItemDesc ChooseItem(ref List<ItemDesc> items, string lastOffer, int money, int rank, out string id)
	{
		id = null;

		// offer item from current rank if asked for
		if (rank > 0)
		{
			foreach (var item in items)
			{
				if (item.Rank < rank)
					continue;
				if (item.Rank > rank)
					break;

				id = item.Id;
				if (lastOffer != id)
					return item;
			}
		}

		// offer upgrade for owned item
		int idx = 0;
		/*if (Random.Range(0, 100) < 20)
		{
			for (; idx < items.Count; ++idx)
			{
				if (items[idx].Used == 0)
					break;
				
				id = items[idx].Id;
				if (lastOffer != id)
					return items[idx];
			}
		}*/

		// find first available item 
		for (; idx < items.Count; ++idx)
		{
			if (items[idx].Owned == false)
				break;
		}

		if (idx >= items.Count)
			return null;

		// safe check for the last item in the list
		id = items[idx].Id;
		if ((items.Count - idx) <= 1)
		{
			return id == lastOffer ? null : items[idx];
		}

		// offer random item
		int tmp = idx;
		do
		{
			idx = Random.Range(tmp, items.Count);
			id = items[idx].Id;
		} while (id == lastOffer);

		return items[idx];
	}

	HatDesc ChooseHat(ref List<HatDesc> hats, string lastOffer, int money, out string id)
	{
		id = null;

		if (hats.Count < 1)
			return null;
		if (hats.Count == 1 && hats[0].Id == lastOffer)
			return null;

		int idx = 0;
		do
		{
			idx = Random.Range(0, hats.Count);
			id = hats[idx].Id;
		} while (id == lastOffer);

		return hats[idx];
	}

	ConsumableDesc ChooseConsumable(ref List<ConsumableDesc> consumables, string lastOffer, int gold, out string id)
	{
		id = null;

		if (consumables.Count < 1)
			return null;
		if (consumables.Count == 1 && consumables[0].Id == lastOffer)
			return null;

		int idx = 0;
		do
		{
			idx = Random.Range(0, consumables.Count);
			id = consumables[idx].Id;
		} while (id == lastOffer);

		return consumables[idx];
	}

	bool CanOffer(ResearchItem item)
	{
		if (item.weaponID != E_WeaponID.None)
		{
			return true;
		}
		else if (item.itemID != E_ItemID.None)
		{
			return true;
		}

		return false;
	}

	string GetId(ResearchItem item)
	{
		if (item.weaponID != E_WeaponID.None)
		{
			return item.weaponID.ToString();
		}
		else if (item.itemID != E_ItemID.None)
		{
			return item.itemID.ToString();
		}

		return null;
	}

	ItemSettings GetItem(ResearchItem item)
	{
		if (item.itemID != E_ItemID.None)
		{
			return ItemSettingsManager.Instance.Get(item.itemID);
		}

		return null;
	}

	int GetUsedTimes(ResearchItem item, PlayerPersistantInfo ppi, RoundFinalResult results)
	{
		int result = 0;

		if (item.weaponID != E_WeaponID.None)
		{
			PPIWeaponData temp = ppi.InventoryList.Weapons.Find(obj => obj.ID == item.weaponID);
			if (temp.ID != E_WeaponID.None)
			{
				result = temp.StatsFire;
			}
		}
		else if (item.itemID != E_ItemID.None)
		{
			PPIItemData temp = ppi.InventoryList.Items.Find(obj => obj.ID == item.itemID);
			if (temp.ID != E_ItemID.None)
			{
				result = temp.StatsUseCount;
			}
		}

		return result;
	}

	List<WeaponSettings.Upgrade> CollectUpgrades(ResearchItem item, int money)
	{
		List<WeaponSettings.Upgrade> result = new List<WeaponSettings.Upgrade>();

		int count = item.GetNumOfUpgrades();
		for (int idx = 0; idx < count; ++idx)
		{
			if (item.OwnsUpgrade(idx) == true)
				continue;

			WeaponSettings.Upgrade upgrade = item.GetUpgrade(idx);
			if (upgrade.MoneyCost > money)
				continue;

			result.Add(upgrade);
		}

		return result;
	}

	int SortItems(ItemDesc x, ItemDesc y)
	{
		if (x.Owned != y.Owned && x.Owned == true)
			return -1;
		if (x.Owned != y.Owned && x.Owned == false)
			return 1;

		if (x.Used > y.Used)
			return -1;
		if (x.Used < y.Used)
			return 1;

		if (x.Rank > y.Rank)
			return -1;
		if (x.Rank < y.Rank)
			return 1;

		if (x.Price > y.Price)
			return 1;
		if (x.Price < y.Price)
			return -1;

		return 0;
	}

	int SortHats(HatDesc x, HatDesc y)
	{
		if (x.Price > y.Price)
			return 1;
		if (x.Price < y.Price)
			return -1;

		return 0;
	}
}
