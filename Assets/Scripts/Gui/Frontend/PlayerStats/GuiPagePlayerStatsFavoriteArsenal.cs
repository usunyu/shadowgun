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

public class GuiPagePlayerStatsFavoriteArsenal : GuiScreen, IGuiPagePlayerStats
{
	// IGUIPAGEPLAYERSTATS INTERFACE

	void IGuiPagePlayerStats.Refresh(PlayerPersistantInfo ppi)
	{
		PPIWeaponData weaponData;

		// collected values

		WeaponSettings weapon = ChooseWeapon(ppi, out weaponData);
		ItemSettings item = ChooseItem(ppi);
		PerkSettings perk = ChoosePerk(ppi);
		HatSettings hat = ChooseHat(ppi);
		SkinSettings skin = ChooseSkin(ppi);
		float totalShots = weaponData.StatsFire;
		float totalKills = weaponData.StatsKills;
		int killsTextId = totalKills == 1 ? 01160034 : 01160026;
		int computedAccurancy = totalShots > 0 ? Mathf.RoundToInt((weaponData.StatsHits/(float)totalShots)*100) : 0;

		// weapon

		SetImage("WeaponImage", weapon != null ? weapon.ResearchWidget : null);
		SetText("WeaponName", weapon != null && weapon.Name != 0 ? TextDatabase.instance[weapon.Name] : "");
		SetText("Kills_Enum", string.Format(TextDatabase.instance[killsTextId], totalKills));
		SetText("Accuracy_Enum", computedAccurancy.ToString());
		SetText("Headshots_Enum", "N/A" /*weaponData.StatsHeadShots.ToString()*/);
		SetText("Shotsfired_Enum", weaponData.StatsFire.ToString());
		SetText("TotalTime_Enum", "N/A");
		SetText("TotalTime_Units", TextDatabase.instance[01160014]);

		// item

		SetImage("ItemImage", item != null ? item.ShopWidget : null);
		SetText("ItemName", item != null && item.Name != 0 ? TextDatabase.instance[item.Name] : "");

		// perk

		SetImage("PerkImage", perk != null ? perk.ShopWidget : null);
		SetText("PerkName", perk != null && perk.Name != 0 ? TextDatabase.instance[perk.Name] : "");

		// hat

		SetImage("HatImage", hat != null ? hat.ShopWidget : null);
		SetText("HatName", hat != null && hat.Name != 0 ? TextDatabase.instance[hat.Name] : "");

		// skin

		SetImage("SkinImage", skin != null ? skin.ScrollerWidget : null);
		SetText("SkinName", skin != null && skin.Name != 0 ? TextDatabase.instance[skin.Name] : "");
	}

	// PRIVATE METHODS

	WeaponSettings ChooseWeapon(PlayerPersistantInfo ppi, out PPIWeaponData weaponData)
	{
		PPIInventoryList inventory = ppi.InventoryList;
		PPIEquipList equipList = ppi.EquipList;

		weaponData = GetFavourite<PPIWeaponData>(inventory.Weapons,
												 (left, right) =>
												 {
													 if (left.StatsFire > right.StatsFire)
														 return -1;
													 if (left.StatsFire < right.StatsFire)
														 return +1;

													 int leftIdx = equipList.Weapons.FindIndex((other) => { return other.ID == left.ID; });
													 int rightIdx = equipList.Weapons.FindIndex((other) => { return other.ID == right.ID; });
													 if (rightIdx != -1 && leftIdx > rightIdx || leftIdx != -1 && rightIdx == -1)
														 return -1;
													 if (leftIdx != -1 && leftIdx < rightIdx || leftIdx == -1 && rightIdx != -1)
														 return +1;

													 return 0;
												 });

		return weaponData.ID != E_WeaponID.None ? WeaponSettingsManager.Instance.Get(weaponData.ID) : null;
	}

	ItemSettings ChooseItem(PlayerPersistantInfo ppi)
	{
		PPIInventoryList inventory = ppi.InventoryList;
		PPIEquipList equipList = ppi.EquipList;
		PPIItemData favouriteItem = GetFavourite<PPIItemData>(inventory.Items,
															  (left, right) =>
															  {
																  if (left.StatsUseCount > right.StatsUseCount)
																	  return -1;
																  if (left.StatsUseCount < right.StatsUseCount)
																	  return +1;

																  int leftIdx = equipList.Items.FindIndex((other) => { return other.ID == left.ID; });
																  int rightIdx = equipList.Items.FindIndex((other) => { return other.ID == right.ID; });
																  if (rightIdx != -1 && leftIdx > rightIdx || leftIdx != -1 && rightIdx == -1)
																	  return -1;
																  if (leftIdx != -1 && leftIdx < rightIdx || leftIdx == -1 && rightIdx != -1)
																	  return +1;

																  return 0;
															  });

		return favouriteItem.ID != E_ItemID.None ? ItemSettingsManager.Instance.Get(favouriteItem.ID) : null;
	}

	PerkSettings ChoosePerk(PlayerPersistantInfo ppi)
	{
		PPIEquipList equipList = ppi.EquipList;
		return equipList.Perk != E_PerkID.None ? PerkSettingsManager.Instance.Get(equipList.Perk) : null;
	}

	HatSettings ChooseHat(PlayerPersistantInfo ppi)
	{
		PPIEquipList equipList = ppi.EquipList;
		return equipList.Outfits.Hat != E_HatID.None ? HatSettingsManager.Instance.Get(equipList.Outfits.Hat) : null;
	}

	SkinSettings ChooseSkin(PlayerPersistantInfo ppi)
	{
		PPIEquipList equipList = ppi.EquipList;
		return equipList.Outfits.Skin != E_SkinID.None ? SkinSettingsManager.Instance.Get(equipList.Outfits.Skin) : null;
	}

	T GetFavourite<T>(List<T> list, System.Comparison<T> comparison)
	{
		if (list.Count == 0)
			return default(T);
		if (list.Count == 1)
			return list[0];

		T[] array = list.ToArray();
		System.Array.Sort(array, comparison);
		return array[0];
	}

	void SetText(string name, string text)
	{
		GUIBase_Label label = GuiBaseUtils.GetControl<GUIBase_Label>(Layout, name);
		label.SetNewText(text);
	}

	void SetImage(string name, GUIBase_Widget image)
	{
		GUIBase_Widget widget = Layout.GetWidget(name);
		if (image != null)
		{
			widget.CopyMaterialSettings(image);
		}
		widget.Show(image != null ? true : false, true);
	}
}
