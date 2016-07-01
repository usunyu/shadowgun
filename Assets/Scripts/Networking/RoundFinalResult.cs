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
using NetworkPlayer = uLink.NetworkPlayer;

public class RoundFinalResult
{
	public class PlayerResult
	{
		public E_Team Team;
		public string PrimaryKey;
		public string NickName;
		public int Score;
		public int Deaths;
		public int Kills;
		public int Experience;
		public RuntimePlatform Platform;

		public void Write(BitStream stream)
		{
			stream.Write<E_Team>(Team);
			stream.WriteString(PrimaryKey);
			stream.WriteString(NickName);
			stream.WriteInt16((short)Score);
			stream.WriteByte((byte)Deaths);
			stream.WriteByte((byte)Kills);
			stream.Write<RuntimePlatform>(Platform);
		}

		public void Read(BitStream stream)
		{
			Team = stream.Read<E_Team>();
			PrimaryKey = stream.ReadString();
			NickName = stream.ReadString();
			Score = stream.ReadInt16();
			Deaths = stream.ReadByte();
			Kills = stream.ReadByte();
			Platform = stream.Read<RuntimePlatform>();
		}

		public static void Serialize(BitStream stream, object value, params object[] args)
		{
			PlayerResult r = (PlayerResult)value;
			r.Write(stream);
		}

		public static object Deserialize(BitStream stream, params object[] args)
		{
			PlayerResult r = new PlayerResult();
			r.Read(stream);
			return r;
		}
	}

	public class PreyNightmare
	{
		public string PrimaryKey;
		public int KilledMe;
		public int KilledByMe;
	}

	public E_MPGameType GameType;
	public E_Team Team;
	public bool Winner;
	public int Place;
	public short Experience;
	public short Money;
	public short Gold;
	public bool NewRank;
	public short MissionExp;
	public short MissioMoney;
	public bool FirstRound;
	public PreyNightmare Prey = new PreyNightmare();
	public PreyNightmare Nightmare = new PreyNightmare();
	public string MapName;

	public List<PlayerResult> PlayersScore = new List<PlayerResult>();

	public void Write(BitStream stream)
	{
		stream.Write<E_MPGameType>(GameType);
		stream.Write<E_Team>(Team);
		stream.WriteBoolean(Winner);
		stream.WriteChar((char)Place);
		stream.WriteInt16(Experience);
		stream.WriteInt16(Money);
		stream.WriteInt16(Gold);
		stream.WriteBoolean(NewRank);
		stream.WriteInt16(MissionExp);
		stream.WriteInt16(MissioMoney);
		stream.WriteBoolean(FirstRound);

		stream.Write<PlayerResult[]>(PlayersScore.ToArray());
	}

	public void Read(BitStream stream)
	{
		GameType = stream.Read<E_MPGameType>();
		Team = stream.Read<E_Team>();
		Winner = stream.ReadBoolean();
		Place = stream.ReadChar();
		Experience = stream.ReadInt16();
		Money = stream.ReadInt16();
		Gold = stream.ReadInt16();
		NewRank = stream.ReadBoolean();
		MissionExp = stream.ReadInt16();
		MissioMoney = stream.ReadInt16();
		FirstRound = stream.ReadBoolean();

		PlayersScore.Clear();
		PlayersScore.InsertRange(0, stream.Read<PlayerResult[]>());
	}

	public static void Serialize(BitStream stream, object value, params object[] args)
	{
		RoundFinalResult r = (RoundFinalResult)value;
		r.Write(stream);
	}

	public static object Deserialize(BitStream stream, params object[] args)
	{
		RoundFinalResult r = new RoundFinalResult();
		r.Read(stream);
		return r;
	}
}
