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

public class PPIEquipList
{
	enum E_DatatType
	{
		None,
		Item,
		Weapon,
	}

	//
	// Don't change identifiers of members of this class, otherwise it will break online-shopping service. If you really
	// need to change it, modify appropriate constants in ShopUtl.java (located in cloud service servlet src code)
	//
	public PPIOutfits Outfits = new PPIOutfits();

	public List<PPIItemData> Items = new List<PPIItemData>();
	public List<PPIWeaponData> Weapons = new List<PPIWeaponData>();

	public E_PerkID Perk = E_PerkID.None;

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

	public void Write(BitStream stream)
	{
		stream.Write<PPIOutfits>(Outfits);
		stream.Write<E_PerkID>(Perk);

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

		stream.Write<E_DatatType>(E_DatatType.None);
	}

	public void Read(BitStream stream)
	{
		Items.Clear();
		Weapons.Clear();

		Outfits = stream.Read<PPIOutfits>();
		Perk = stream.Read<E_PerkID>();

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
			}
		}
	}

	public static void Serialize(BitStream stream, object value, params object[] args)
	{
		PPIEquipList ppi = (PPIEquipList)value;
		ppi.Write(stream);
	}

	public static object Deserialize(BitStream stream, params object[] args)
	{
		PPIEquipList ppi = new PPIEquipList();
		ppi.Read(stream);
		return ppi;
	}
}
