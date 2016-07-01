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
using System;
using BitStream = uLink.BitStream;
using System.Reflection;

public class PPIInventoryList
{
	enum E_DatatType
	{
		None,
		Item,
		Weapon,
		Skin,
		Hat,
		Perk,
	}

	// -----
	public bool ContainsWeapon(E_WeaponID weaponID)
	{
		foreach (PPIWeaponData data in Weapons)
		{
			if (data.ID == weaponID)
				return true;
		}
		return false;
	}

	// -----
	public bool OwnsWeaponUpgrade(E_WeaponID weaponID, E_WeaponUpgradeID upgradeID)
	{
		foreach (PPIWeaponData weapon in Weapons)
		{
			if (weapon.ID == weaponID)
			{
				List<WeaponUpgrade> upgrades = weapon.Upgrades;
				if (upgrades != null)
				{
					for (int j = 0; j < upgrades.Count; j++)
					{
						if (upgradeID == upgrades[j].ID)
							return true;
					}
				}
				return false;
			}
		}
		return false;
	}

	// -----
	/*public void TMP_CODE_AddWeaponUpgrade(E_WeaponID weaponID, E_WeaponUpgradeID upgradeID)
	{
		for (int i = 0; i < Weapons.Count; i++)
		{
			if (Weapons[i].ID == weaponID)
			{	
				PPIWeaponData tmp = Weapons[i];
				tmp.CreateUpgradesIfNotExisting();
				tmp.Upgrades.Add(upgradeID);
				Weapons[i] = tmp;
				return;
			}	
		}	
	}	
	/**/
	//
	// Don't change identifiers of members of this class, otherwise it will break online-shopping service. If you really
	// need to change it, modify appropriate constants in ShopUtl.java (located in cloud service servlet src code)
	//

	public List<PPIItemData> Items = new List<PPIItemData>();
	public List<PPIWeaponData> Weapons = new List<PPIWeaponData>();
	public List<PPISkinData> Skins = new List<PPISkinData>();
	public List<PPIHatData> Hats = new List<PPIHatData>();
	public List<PPIPerkData> Perks = new List<PPIPerkData>();
	public List<PPIBundleData> Bundles = new List<PPIBundleData>();

	//private PPIWeaponData emptyData = new PPIWeaponData();
	//Network Shit;
	public void Write(BitStream stream)
	{
		//send items
		stream.Write<E_DatatType>(E_DatatType.Item);
		stream.WriteByte((byte)Items.Count);
		foreach (PPIItemData d in Items)
			d.Write(stream);

		//send weapons
		stream.Write<E_DatatType>(E_DatatType.Weapon);
		stream.WriteByte((byte)Weapons.Count);
		foreach (PPIWeaponData d in Weapons)
			d.Write(stream);

		//send skins
		stream.Write<E_DatatType>(E_DatatType.Skin);
		stream.WriteByte((byte)Skins.Count);
		foreach (PPISkinData d in Skins)
			d.Write(stream);

		//send hats
		stream.Write<E_DatatType>(E_DatatType.Hat);
		stream.WriteByte((byte)Hats.Count);
		foreach (PPIHatData d in Hats)
			d.Write(stream);

		//send hats
		stream.Write<E_DatatType>(E_DatatType.Perk);
		stream.WriteByte((byte)Perks.Count);
		foreach (PPIPerkData d in Perks)
			d.Write(stream);

		stream.Write<E_DatatType>(E_DatatType.None);
	}

	public void Read(BitStream stream)
	{
		Items.Clear();
		Weapons.Clear();
		Skins.Clear();
		Hats.Clear();
		Perks.Clear();

		while (stream.isEOF == false)
		{
			switch (stream.Read<E_DatatType>())
			{
			case E_DatatType.None:
				return;
			case E_DatatType.Item:
			{
				int count = stream.ReadByte();
				for (int i = 0; i < count; i++)
				{
					PPIItemData d = new PPIItemData();
					d.Read(stream);
					Items.Add(d);
				}
			}
				break;
			case E_DatatType.Weapon:
			{
				int count = stream.ReadByte();
				for (int i = 0; i < count; i++)
				{
					PPIWeaponData d = new PPIWeaponData();
					d.Read(stream);
					Weapons.Add(d);
				}
			}
				break;
			case E_DatatType.Skin:
			{
				int count = stream.ReadByte();
				for (int i = 0; i < count; i++)
				{
					PPISkinData d = new PPISkinData();
					d.Read(stream);
					Skins.Add(d);
				}
			}
				break;
			case E_DatatType.Hat:
			{
				int count = stream.ReadByte();
				for (int i = 0; i < count; i++)
				{
					PPIHatData d = new PPIHatData();
					d.Read(stream);
					Hats.Add(d);
				}
			}
				break;
			case E_DatatType.Perk:
			{
				int count = stream.ReadByte();
				for (int i = 0; i < count; i++)
				{
					PPIPerkData d = new PPIPerkData();
					d.Read(stream);
					Perks.Add(d);
				}
			}
				break;
			}
		}
	}

	public static void Serialize(BitStream stream, object value, params object[] args)
	{
		PPIInventoryList ppi = (PPIInventoryList)value;
		ppi.Write(stream);
	}

	public static object Deserialize(BitStream stream, params object[] args)
	{
		PPIInventoryList ppi = new PPIInventoryList();
		ppi.Read(stream);
		return ppi;
	}
}
