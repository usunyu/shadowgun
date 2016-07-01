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

public class PPIRoundScore
{
	// This Server / Clients parts can be definitely better separated
	// Server part /////////////////////////////////////////////////////
	public struct ItemStatistics
	{
		public int StatsKills;
		public int StatsUseCount;
	}

	public struct WeaponStatistics
	{
		public int StatsFire;
		public int StatsKills;
		public int StatsHits;
	}

	public Dictionary<E_ItemID, ItemStatistics> ItemStats = new Dictionary<E_ItemID, ItemStatistics>();
	public Dictionary<E_WeaponID, WeaponStatistics> WeaponStats = new Dictionary<E_WeaponID, WeaponStatistics>();
	public int Hits;
	public int Shots;
	public int HeadShots;
	public int PlayedTimes;
	public float TimeSpent;
	public double LastPlayedDate;

	// Client + Server common part ////////////////////////////////////
	public int Score;
	public short Deaths;
	public short Kills;
	public short Experience;
	public short Money;
	public short Gold;

	public void Reset()
	{
		Hits = Shots = HeadShots = PlayedTimes = Score = Deaths = Kills = Experience = Money = Gold = 0;
		TimeSpent = 0.0f;
		LastPlayedDate = 0.0;
		ItemStats.Clear();
		WeaponStats.Clear();
	}

	public void Update(PPIRoundScore score)
	{
		Score = score.Score;
		Deaths = score.Deaths;
		Kills = score.Kills;
		Experience = score.Experience;
		Money = score.Money;
		Gold = score.Gold;
	}

	public void Write(BitStream stream)
	{
		stream.WriteInt16((short)Score);
		stream.WriteInt16(Kills);
		stream.WriteInt16(Deaths);
		stream.WriteInt16(Experience);
		stream.WriteInt16(Money);
		stream.WriteInt16(Gold);
	}

	public void Read(BitStream stream)
	{
		Score = stream.ReadInt16();
		Kills = stream.ReadInt16();
		Deaths = stream.ReadInt16();
		Experience = stream.ReadInt16();
		Money = stream.ReadInt16();
		Gold = stream.ReadInt16();
	}

	public static void Serialize(BitStream stream, object value, params object[] args)
	{
		PPIRoundScore ppi = (PPIRoundScore)value;
		ppi.Write(stream);
	}

	public static object Deserialize(BitStream stream, params object[] args)
	{
		PPIRoundScore ppi = new PPIRoundScore();
		ppi.Read(stream);
		return ppi;
	}
}
