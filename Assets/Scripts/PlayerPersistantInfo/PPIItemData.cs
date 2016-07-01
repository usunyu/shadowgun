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

public struct PPIItemData
{
	// don't add any other data members here and don't change their names - otherwise it would break online shopping service

	public E_ItemID ID;
	public int EquipSlotIdx;
	public int StatsUseCount;
	public int StatsKills;
	public int Count;

	public void Write(BitStream stream)
	{
		stream.Write<E_ItemID>(ID);
		stream.WriteByte((byte)EquipSlotIdx);
		stream.WriteInt16((short)Count);
		//stream.WriteChar(ID);
	}

	public void Read(BitStream stream)
	{
		ID = stream.Read<E_ItemID>();
		EquipSlotIdx = stream.ReadByte();
		Count = stream.ReadInt16();
	}

	public bool IsValid()
	{
		return this.ID != E_ItemID.None;
	}

	public override string ToString()
	{
		return string.Format("[PPIItemData] - Use:" + StatsUseCount + " Kills:" + StatsKills);
	}
}
