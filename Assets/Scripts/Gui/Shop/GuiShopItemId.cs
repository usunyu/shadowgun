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

// 
public class ShopItemId : System.IComparable<ShopItemId>, System.IEquatable<ShopItemId>
{
	public ShopItemId(int id, GuiShop.E_ItemType type)
	{
		Id = id;
		ItemType = type;
	}

	public ShopItemId()
	{
		Id = EmptyId.Id;
		ItemType = EmptyId.ItemType;
	}

	public int Id { get; private set; }
	public GuiShop.E_ItemType ItemType { get; private set; }

	public bool IsEmpty()
	{
		return this == EmptyId;
	}

	public override string ToString()
	{
		return "ShopItemId: " + Id + " " + ItemType;
	}

	public string GetName()
	{
		switch (ItemType)
		{
		case GuiShop.E_ItemType.Weapon:
			return ((E_WeaponID)Id).ToString();
		case GuiShop.E_ItemType.Item:
			return ((E_ItemID)Id).ToString();
		case GuiShop.E_ItemType.Skin:
			return ((E_SkinID)Id).ToString();
		case GuiShop.E_ItemType.Hat:
			return ((E_HatID)Id).ToString();
		case GuiShop.E_ItemType.Fund:
			return ((E_FundID)Id).ToString();
		case GuiShop.E_ItemType.Perk:
			return ((E_PerkID)Id).ToString();
		case GuiShop.E_ItemType.Bundle:
			return ((E_BundleID)Id).ToString();
		case GuiShop.E_ItemType.Ticket:
			return ((E_TicketID)Id).ToString();
		default:
			Debug.LogError("ShopItemId - GetName - TODO: Unhandled type");
			break;
		}
		return "Uknown";
	}

	public int CompareTo(ShopItemId other)
	{
		//Debug.Log("comparing: " + this + " to " + other );

		//sort itemu podle predem danych kriterii:
		//1. prednost ma sortovani podle shop categorii
		int result = this.ItemType.CompareTo(other.ItemType);

		if (result != 0)
			return result;

		//2. sort podle parametru itemu
		switch (ItemType)
		{
		case GuiShop.E_ItemType.Weapon:
			result = CompareWeapon(other.Id);
			break;
		case GuiShop.E_ItemType.Item:
			result = CompareItem(other.Id);
			break;
		case GuiShop.E_ItemType.Skin:
			result = CompareSkin(other.Id);
			break;
		case GuiShop.E_ItemType.Hat:
			result = CompareHat(other.Id);
			break;
		case GuiShop.E_ItemType.Fund:
			result = CompareFund(other.Id);
			break;
		case GuiShop.E_ItemType.Perk:
			result = ComparePerk(other.Id);
			break;
		case GuiShop.E_ItemType.Bundle:
			result = CompareBundle(other.Id);
			break;
		default:
			Debug.LogError("TODO: Unhandled type");
			break;
		}

		return result;
	}

	int CompareWeapon(int otherId)
	{
		WeaponSettings otherWs = WeaponSettingsManager.Instance.Get((E_WeaponID)(otherId));
		WeaponSettings myWs = WeaponSettingsManager.Instance.Get((E_WeaponID)(Id));

		int res;
		res = myWs.WeaponType.CompareTo(otherWs.WeaponType);
		if (res != 0)
			return res;

		//sort rifle into subtype
		if (myWs.WeaponType == E_WeaponType.Rifle)
		{
			res = RifleSubtype((E_WeaponID)Id).CompareTo(RifleSubtype((E_WeaponID)otherId));
			if (res != 0)
				return res;
		}

		//otherwise use regular order
		return Id.CompareTo(otherId);
	}

	int RifleSubtype(E_WeaponID id)
	{
		switch (id)
		{
		case E_WeaponID.AR1:
			return 1;
		case E_WeaponID.AR2:
			return 1;
		case E_WeaponID.AR3:
			return 1;
		case E_WeaponID.AR4:
			return 1;
		case E_WeaponID.SMG1:
			return 2;
		case E_WeaponID.SMG2:
			return 2;
		case E_WeaponID.SMG3:
			return 2;
		case E_WeaponID.SMG4:
			return 2;
		case E_WeaponID.MG1:
			return 3;
		case E_WeaponID.MG2:
			return 3;
		case E_WeaponID.MG3:
			return 3;
		case E_WeaponID.MG4:
			return 3;
		default:
			return 0;
		}
	}

	int CompareItem(int otherId)
	{
		ItemSettings otherWs = ItemSettingsManager.Instance.Get((E_ItemID)(otherId));
		ItemSettings myWs = ItemSettingsManager.Instance.Get((E_ItemID)(Id));

		int res = myWs.ItemType.CompareTo(otherWs.ItemType);
		if (res != 0)
			return res;

		//2. behaviour
		return myWs.ItemBehaviour.CompareTo(otherWs.ItemBehaviour);
	}

	int CompareSkin(int otherId)
	{
		SkinSettings otherWs = SkinSettingsManager.Instance.Get((E_SkinID)(otherId));
		SkinSettings myWs = SkinSettingsManager.Instance.Get((E_SkinID)(Id));

		//1. gold
		int res = myWs.GoldCost.CompareTo(otherWs.GoldCost);
		if (res != 0)
			return res;

		//2. money
		res = myWs.MoneyCost.CompareTo(otherWs.MoneyCost);

		return myWs.ID.CompareTo(otherWs.ID);
	}

	int ComparePerk(int otherId)
	{
		PerkSettings otherWs = PerkSettingsManager.Instance.Get((E_PerkID)(otherId));
		PerkSettings myWs = PerkSettingsManager.Instance.Get((E_PerkID)(Id));

		return myWs.ID.CompareTo(otherWs.ID);
	}

	int CompareHat(int otherId)
	{
		HatSettings otherWs = HatSettingsManager.Instance.Get((E_HatID)(otherId));
		HatSettings myWs = HatSettingsManager.Instance.Get((E_HatID)(Id));

		//1. gold
		int res = myWs.GoldCost.CompareTo(otherWs.GoldCost);
		if (res != 0)
			return res;

		//2. money
		res = myWs.MoneyCost.CompareTo(otherWs.MoneyCost);
		if (res != 0)
			return res;

		//otherwise use regular order
		return Id.CompareTo(otherId);
	}

	public static int FreeGoldType(E_FundID id)
	{
		switch (id)
		{
		case E_FundID.TapJoyInApp:
			return 1;
		case E_FundID.TapJoyWeb:
			return 2;
		case E_FundID.FreeOffer:
			return 3;
		case E_FundID.FreeWeb:
			return 4;
		default:
			return 0;
		}
	}

	int CompareFund(int otherId)
	{
		FundSettings otherWs = FundSettingsManager.Instance.Get((E_FundID)(otherId));
		FundSettings myWs = FundSettingsManager.Instance.Get((E_FundID)(Id));

		int res;

		//convert currency displayed towards the end
		/*res = myWs.GoldCost.CompareTo(otherWs.GoldCost);
		if(res != 0)
			return res;
		
		res = myWs.MoneyCost.CompareTo(otherWs.MoneyCost);
		if(res != 0)
			return res;*/

		//2. sort by how much money we get
		res = myWs.AddMoney.CompareTo(otherWs.AddMoney);
		if (res != 0)
			return res;

		//3. sort by how much gold we get

		//free gold na konec goldu (protoze jej duplikujeme v zalozce FREE GOLD)
		res = FreeGoldType((E_FundID)Id).CompareTo(FreeGoldType((E_FundID)otherId));
		if (res != 0)
			return res;

		res = myWs.AddGold.CompareTo(otherWs.AddGold);
		if (res != 0)
			return res;

		//Debug.Log("res " + res);

		return res;
	}

	int CompareBundle(int otherId)
	{
		BundleSettings otherWs = BundleSettingsManager.Instance.Get((E_BundleID)(otherId));
		BundleSettings myWs = BundleSettingsManager.Instance.Get((E_BundleID)(Id));

		//1. gold
		int res = myWs.GoldCost.CompareTo(otherWs.GoldCost);
		if (res != 0)
			return res;

		//2. money
		res = myWs.MoneyCost.CompareTo(otherWs.MoneyCost);
		if (res != 0)
			return res;

		//otherwise use regular order
		return Id.CompareTo(otherId);
	}

	public bool Equals(ShopItemId other)
	{
		if (other == null)
			return false;

		return (other.Id == Id && other.ItemType == ItemType);
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
			return base.Equals(obj);

		ShopItemId other = obj as ShopItemId;
		if (other == null)
			return false;
		else
			return Equals(other);
	}

	public override int GetHashCode()
	{
		return Id ^ (int)ItemType;
	}

	public static bool operator ==(ShopItemId a, ShopItemId b)
	{
		// If both are null, or both are same instance, return true.
		if (System.Object.ReferenceEquals(a, b))
		{
			return true;
		}

		// If one is null, but not both, return false.
		if (((object)a == null) || ((object)b == null))
		{
			return false;
		}

		// Return true if the fields match:
		return a.Id == b.Id && a.ItemType == b.ItemType;
	}

	public static bool operator !=(ShopItemId a, ShopItemId b)
	{
		return !(a == b);
	}

	public static ShopItemId EmptyId = new ShopItemId(-1, GuiShop.E_ItemType.None);
};
