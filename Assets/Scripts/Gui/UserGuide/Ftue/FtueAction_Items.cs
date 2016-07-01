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

namespace FtueAction
{
	public abstract class ItemBase : Base
	{
		public GuiShop.E_ItemType ItemType { get; private set; }
		public int ItemId { get; private set; }
		public ResearchItem Item { get; private set; }

		// C-TOR

		public ItemBase(Ftue ftue, int minimalRank, GuiShop.E_ItemType itemType, int itemId)
						: base(ftue, minimalRank, false)
		{
			ItemType = itemType;
			ItemId = itemId;
		}

		// USERGUIDEACTION INTERFACE

		protected override bool OnExecute()
		{
			if (base.OnExecute() == false)
				return false;

			E_WeaponID weaponID = ItemType == GuiShop.E_ItemType.Weapon ? (E_WeaponID)ItemId : E_WeaponID.None;
			E_ItemID itemID = ItemType == GuiShop.E_ItemType.Item ? (E_ItemID)ItemId : E_ItemID.None;
			E_UpgradeID upgradeID = ItemType == GuiShop.E_ItemType.Upgrade ? (E_UpgradeID)ItemId : E_UpgradeID.None;
			E_PerkID perkID = ItemType == GuiShop.E_ItemType.Perk ? (E_PerkID)ItemId : E_PerkID.None;

			ResearchItem[] items = ResearchSupport.Instance.GetItems();

			foreach (var item in items)
			{
				if (item.weaponID == weaponID && weaponID != E_WeaponID.None ||
					item.itemID == itemID && itemID != E_ItemID.None ||
					item.upgradeID == upgradeID && upgradeID != E_UpgradeID.None ||
					item.perkID == perkID && perkID != E_PerkID.None)
				{
					Item = item;
					break;
				}
			}

			return true;
		}
	}

	public class Research : ItemBase
	{
		// C-TOR

		public Research(Ftue ftue, int minimalRank, GuiShop.E_ItemType itemType, int itemId)
						: base(ftue, minimalRank, itemType, itemId)
		{
			ScreenType = typeof (GuiScreenResearchMain); //GuiPopupViewResearchItem);
			LabelId = 9960051;
			DescriptionId = 9960052;
		}

		// USERGUIDEACTION INTERFACE

		protected override bool OnUpdate()
		{
			if (base.OnUpdate() == false)
				return false;

			if (Item == null)
				return false;

			if (Item.GetState() != ResearchState.Active)
				return false;

			if (Screen.IsVisible == true)
				return false;

			State = FtueState.Finished;

			return true;
		}

		// FTUEACTION.BASE INTERFACE

		public override bool CanActivate()
		{
			PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();
			return ppi != null && ppi.Rank >= MinimalRank ? base.CanActivate() : false;
		}
	}

	public class Equip : ItemBase
	{
		// C-TOR

		public Equip(Ftue ftue, int minimalRank, GuiShop.E_ItemType itemType, int itemId)
						: base(ftue, minimalRank, itemType, itemId)
		{
			ScreenType = typeof (GuiEquipMenu);
			LabelId = 9960061;
			DescriptionId = 9960062;
		}

		// USERGUIDEACTION INTERFACE

		protected override bool OnUpdate()
		{
			if (base.OnUpdate() == false)
				return false;

			if (Item == null)
				return false;

			if (Item.GetState() != ResearchState.Active)
				return false;

			switch (ItemType)
			{
			case GuiShop.E_ItemType.Weapon:
				var weapons = GuideData.LocalPPI.EquipList.Weapons;
				var weapon = weapons.Find(obj => (int)obj.ID == ItemId);
				if (weapon.IsValid() == false)
					return false;
				break;
			default:
				Terminate();
				return false;
			}

			if (Screen.IsVisible == true)
				return false;

			State = FtueState.Finished;

			return true;
		}

		// FTUEACTION.BASE INTERFACE

		public override string HintText()
		{
			if (Item != null && Item.GetState() != ResearchState.Active)
				return string.Format(TextDatabase.instance[09900011], TextDatabase.instance[Item.GetName()]);
			return base.HintText();
		}

		public override bool CanActivate()
		{
			if (Item == null)
				return false;
			if (Item.GetState() != ResearchState.Active)
				return false;
			PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();
			return ppi != null && ppi.Rank >= MinimalRank ? base.CanActivate() : false;
		}
	}

	public class Shop : Base
	{
		// PRIVATE METHODS

		bool WasVisible = false;

		// C-TOR

		public Shop(Ftue ftue, int minimalRank = 0)
						: base(ftue, minimalRank, false)
		{
			ScreenType = typeof (GuiShopMenu);
			LabelId = 9960101;
			DescriptionId = 9960102;
		}

		// USERGUIDEACTION INTERFACE

		protected override bool OnUpdate()
		{
			if (base.OnUpdate() == false)
				return false;

			if (WasVisible == false && Screen.IsVisible == false)
				return false;

			WasVisible = true;
			if (Screen.IsVisible == true)
				return false;

			State = FtueState.Finished;

			return true;
		}

		// FTUEACTION.BASE INTERFACE

		public override bool CanActivate()
		{
			PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();
			return ppi != null && ppi.Rank >= MinimalRank ? base.CanActivate() : false;
		}
	}
}
