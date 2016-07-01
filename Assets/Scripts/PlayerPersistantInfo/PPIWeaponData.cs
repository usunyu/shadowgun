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
using BitStream = uLink.BitStream;

public struct WeaponUpgrade
{
	public E_WeaponUpgradeID ID;
}

public struct PPIWeaponData
{
	// don't add any other data members here and don't change their names - otherwise it would break online shopping service
	public E_WeaponID ID;
	public int EquipSlotIdx;

	public int StatsFire;
	public int StatsHits;
	public int StatsKills;
	//public int				StatsHeadShots;

	/*PPIWeaponData(PPIWeaponData copyFrom)
	{
		ID		   	= copyFrom.ID;
		EquipSlotIdx= copyFrom.EquipSlotIdx;
		StatsFire	= copyFrom.StatsFire;
		StatsHits	= copyFrom.StatsHits;
		StatsKills	= copyFrom.StatsKills;
		m_Upgrades 	= copyFrom.Upgrades;	
	}
	/**/
	public List<WeaponUpgrade> Upgrades;

	public void CreateUpgradesIfNotExisting()
	{
		if (Upgrades == null)
			Upgrades = new List<WeaponUpgrade>();
	}

	public void Write(BitStream stream)
	{
		CreateUpgradesIfNotExisting();
		stream.Write<E_WeaponID>(ID);
		stream.WriteByte((byte)EquipSlotIdx);

		stream.WriteByte((byte)Upgrades.Count);
		foreach (WeaponUpgrade id in Upgrades)
			stream.Write<E_WeaponUpgradeID>(id.ID);
	}

	public void Read(BitStream stream)
	{
		CreateUpgradesIfNotExisting();
		ID = stream.Read<E_WeaponID>();
		EquipSlotIdx = stream.ReadByte();

		Upgrades.Clear();
		int count = stream.ReadByte();
		for (int i = 0; i < count; i++)
		{
			WeaponUpgrade upg = new WeaponUpgrade();
			upg.ID = stream.Read<E_WeaponUpgradeID>();
			Upgrades.Add(upg);
		}
	}

	public bool IsValid()
	{
		return this.ID != E_WeaponID.None;
	}

	public override string ToString()
	{
		return string.Format("[PPIWeaponData] - Shots:" + StatsFire + " Hits:" + StatsHits + " Kills:" + StatsKills);
	}
}
