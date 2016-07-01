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

public struct BanInfo
{
	// ban end time. It is measured as number of miliseconds since standard base time known as "the epoch", namely January 1, 1970, 00:00:00 GM
	public long EndTime;
	public string Message;
}

public class PlayerPersistentInfoData
{
	//
	// Don't change identifiers of members of this class, otherwise it will break online-shopping service. If you really
	// need to change it, modify appropriate constants in ShopUtl.java (located in cloud service servlet src code)
	//
	public struct S_Params // cloud storage
	{
		public int Experience;
		public int Money;
		public int Gold;
		public int Chips;

		// Premium account end time. It is measured as number of miliseconds since standard base time known as "the epoch", namely January 1, 1970, 00:00:00 GM
		public long PremiumAccEndTime;

		public BanInfo Ban;

		// Don't touch this member. It is internal version counter necessary for correct synchronization
		// of player data between client <-> dedicated server <-> cloud service
		public int InternalDataVersion;
	};

	public S_Params Params;

	public PPIInventoryList InventoryList = new PPIInventoryList(); // cloud storage
	public PPIEquipList EquipList = new PPIEquipList(); // cloud storage
	public PPIUpgradeList Upgrades = new PPIUpgradeList(); // cloud storage

	public PPIBankData BankData = new PPIBankData(); // cloud storage

	public PPIPlayerStats Stats = new PPIPlayerStats(); // cloud storage

	public PPIDailyRewards DailyRewards = PPIDailyRewards.Create(); // cloud storage

	public void Write(BitStream stream)
	{
//		stream.Write(Name);
		stream.WriteInt32(Params.Experience);
//        stream.WriteByte((byte)Params.Rank);
		stream.WriteInt32(Params.Money);
		stream.WriteInt32(Params.Gold);

		stream.Write<PPIInventoryList>(InventoryList);
		stream.Write<PPIEquipList>(EquipList);
		stream.Write<PPIUpgradeList>(Upgrades);

//        stream.Write<PPIStatistic>(Statistic);
//        stream.Write<PPIRoundScore>(Score);
	}

	public void Read(BitStream stream)
	{
//        Name = stream.Read<string>();
		Params.Experience = stream.ReadInt32();
//        Params.Rank = stream.ReadByte();
		Params.Money = stream.ReadInt32();
		Params.Gold = stream.ReadInt32();

		InventoryList = stream.Read<PPIInventoryList>();
		EquipList = stream.Read<PPIEquipList>();
		Upgrades = stream.Read<PPIUpgradeList>();

//        Statistic = stream.Read<PPIStatistic>();
//        Score = stream.Read<PPIRoundScore>();
	}
}
