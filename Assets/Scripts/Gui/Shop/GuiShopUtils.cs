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
using System.Collections.Generic;

class GuiShopUtils
{
#if UNITY_IPHONE
	const string TAPJOY_DEADZONE_URL = "https://www.tapjoy.com/earn?eid=47d8c74bc66af2cd6b2e10e672ad868913e17cadf5c7e0f1f25a490322449df3fa5aeac845a9da979eca831627749893&referral=madfinger_deadzone";
#elif UNITY_ANDROID
	const string TAPJOY_DEADZONE_URL =
					"https://www.tapjoy.com/earn?eid=effca5eff450fc7cecbe5d30a15e6ae45793de386b49f0188117cd187dca22125f8612a1bc979aee548ea9c422cf3e17&referral=madfinger_deadzone";
#endif

	public static void EarnFreeGold(ShopItemId goldId)
	{
		//Debug.Log("Earn Free gold " + goldId);
		if (!ShopDataBridge.Instance.IsFreeGold(goldId))
			return;

		E_FundID fundId = (E_FundID)(goldId.Id);

		if (fundId == E_FundID.TapJoyWeb)
		{
#if UNITY_IPHONE
			MFNativeUtils.OpenURLExternal(TAPJOY_DEADZONE_URL); //Tapjoy has to be opened in external browser on iOS due to Apple's restrictions
#elif UNITY_ANDROID
			EtceteraWrapper.ShowWeb(TAPJOY_DEADZONE_URL);
#endif
		}
		else if (fundId == E_FundID.TapJoyInApp)
		{
			TapJoy.ShowOffers();
		}
		else if (fundId == E_FundID.FreeOffer)
		{
			// Used to be SponsorPay Plugin - now unused
		}
		else if (fundId == E_FundID.FreeWeb)
		{
#if (UNITY_ANDROID || UNITY_IPHONE)
			//SponsorPay plugin has been removed in r16601
//			string sponsorPayStr = string.Format("http://iframe.sponsorpay.com/mbrowser?appid={0}&device_id={1}&uid={2}&pub0=", Game.SponsorPayCredentials.AppId, SysUtils.GetUniqueDeviceID(), CloudUser.instance.userName);
//			EtceteraWrapper.ShowWeb(sponsorPayStr);
#endif
		}
	}

	public static void ShowOffers()
	{
	}

	//Funkce pro fixnuti equipu na ppi
	//Projdeme casti equipu ktere by nemuseli byt validni a vytvorime seznam akci ktere je treba vykonat aby byl zase v poradku.
	//Pokud je vse v poradku, vraci null, jinak serial cloud action ktera se ma provest
	public static BaseCloudAction ValidateEquip()
	{
		List<BaseCloudAction> outActions = new List<BaseCloudAction>();
		PlayerPersistantInfo ppi = ShopDataBridge.Instance.PPI;
		UnigueUserID userID = CloudUser.instance.authenticatedUserID;

		//weapons:
		//bool anyWeaponEquipped = false;
		foreach (PPIWeaponData w in ppi.EquipList.Weapons)
		{
			//ignoruj prazdna id
			if (w.ID == E_WeaponID.None)
				continue;

			//pokud je item zakazany, nebo ma locknuty slot, odstran ho z equipu.
			WeaponSettings s = WeaponSettingsManager.Instance.Get(w.ID);
			if (s.DISABLED || ShopDataBridge.Instance.IsWeaponSlotLocked(w.EquipSlotIdx))
			{
				//unequip it
				outActions.Add(new SlotEquipAction(userID, s.GUID, w.EquipSlotIdx, false));
				continue;
			}

			//	anyWeaponEquipped = true;
		}

		/* we don't need this functionality right now,
		 * because we don't allow player to spawn without any weapon now
		//pokud nemame equpnutou zadnou zbran, pridej nejakou do prvniho slotu
		if(!anyWeaponEquipped)
		{
			List<ShopItemId> ownedWeapons = ShopDataBridge.Instance.GetOwnedWeapons();
			if(ownedWeapons.Count > 0)
			{
				int guid = ShopDataBridge.Instance.GetShopItemGUID(ownedWeapons[0]);	
				outActions.Add(new SlotEquipAction(userID, guid, 0, true ));
			}
		}*/

		//items:
		foreach (PPIItemData w in ppi.EquipList.Items)
		{
			//ignoruj prazdna id
			if (w.ID == E_ItemID.None)
				continue;

			//pokud je item zakazany, nebo ma locknuty slot, odstran ho z equipu.
			ItemSettings s = ItemSettingsManager.Instance.Get(w.ID);
			if (s.DISABLED || ShopDataBridge.Instance.IsItemSlotLocked(w.EquipSlotIdx))
			{
				//unequip it
				outActions.Add(new SlotEquipAction(userID, s.GUID, w.EquipSlotIdx, false));
				continue;
			}

			//pokud mame od itemu v inventari lepsi nebo naopak horsi verzi, updatni jej ve slotu.
			E_ItemID bestVersion = ShopDataBridge.Instance.FindBestItemUpgrade(w.ID);
			if (w.ID != bestVersion)
			{
				//replace it with actual version from inventory
				int bestVersionGUID = ItemSettingsManager.Instance.Get(bestVersion).GUID;
				outActions.Add(new SlotEquipAction(userID, bestVersionGUID, w.EquipSlotIdx, true));
				continue;
			}
		}

		//perk:
		if (ppi.EquipList.Perk != E_PerkID.None)
		{
			E_PerkID perk = ppi.EquipList.Perk;
			E_PerkID bestVersion = ShopDataBridge.Instance.FindBestPerkUpgrade(ppi.EquipList.Perk);

			//pokud je zakazany, odstran jej ze slotu
			PerkSettings s = PerkSettingsManager.Instance.Get(perk);
			if (s.DISABLED)
			{
				//unequip it
				outActions.Add(new SlotEquipAction(userID, s.GUID, 0, false));
			}
			else if (perk != bestVersion)
			{
				//replace it with actual version from inventory
				int bestVersionGUID = PerkSettingsManager.Instance.Get(bestVersion).GUID;
				outActions.Add(new SlotEquipAction(userID, bestVersionGUID, 0, true));
			}
		}

		//skins:
		{
			ShopItemId currSkin = new ShopItemId((int)ppi.EquipList.Outfits.Skin, GuiShop.E_ItemType.Skin);
			List<ShopItemId> ownedSkins = ShopDataBridge.Instance.GetOwnedSkins();
			if (!ownedSkins.Contains(currSkin))
			{
				//Debug.Log("Equiped skin " + currSkin + " is not in inventory, switching to: " + ( (ownedSkins.Count > 0) ? ownedSkins[0].ToString() : "-none-") );

				if (ownedSkins.Count > 0)
				{
					//equipnuty skin neni v inventari, vyber misto neho nejaky jiny
					ownedSkins.Sort();
					int guid = ShopDataBridge.Instance.GetShopItemGUID(ownedSkins[0]);
					outActions.Add(new SlotEquipAction(userID, guid, 0, true));
				}
				else
				{
					Debug.LogError("No skins in inventory!");
				}
			}
		}

		//upgrades:
		{
			List<PPIUpgradeList.UpgradeData> upgs = ppi.Upgrades.Upgrades;
			if (upgs.Find(u => u.ID == E_UpgradeID.ItemSlot2).ID != E_UpgradeID.ItemSlot2)
			{
				int guid = ShopDataBridge.Instance.GetShopItemGUID(new ShopItemId((int)E_UpgradeID.ItemSlot2, GuiShop.E_ItemType.Upgrade));
				outActions.Add(new ShopBuyAction(userID, guid));
			}

			if (upgs.Find(u => u.ID == E_UpgradeID.WeaponSlot2).ID != E_UpgradeID.WeaponSlot2)
			{
				int guid = ShopDataBridge.Instance.GetShopItemGUID(new ShopItemId((int)E_UpgradeID.WeaponSlot2, GuiShop.E_ItemType.Upgrade));
				outActions.Add(new ShopBuyAction(userID, guid));
			}
		}

		//pokud jsme neco zmenili, updatuj na zaver PPI
		if (outActions.Count > 0)
		{
			outActions.Add(new FetchPlayerPersistantInfo(userID));
			BaseCloudAction resultAction = new CloudActionSerial(userID, BaseCloudAction.NoTimeOut, outActions.ToArray());
			return resultAction;
		}
		else
			return null;
	}
}
