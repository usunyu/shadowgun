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

//#define IGNORE_DESIRED_RANK_TO_PLAY

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;
using BitStream = uLink.BitStream;

[System.Serializable]
public class LevelInfo
{
	public string Name;
	public string Level;
	public bool Disabled;
}

public abstract class GameTypeInfo
{
	public List<LevelInfo> MPLevels;
	public int TimerSpawn = 3;
	public float TimerRoundStart = 5;
	//public float TimerShowUnlocks = 5;
	public float TimerRoundEnd = 5;
	public float TimerRoundEarnings = 10;
	public int RoundPerLevel = 5;

	public float SpawnTimeProtection = 2.5f;

	public float MaximumTimeWithoutSpawn = 300.0f;
	public float MaximumTimeWithoutUserInput = 300.0f;

	public int MinimalDesiredRankToPlay = 1;
}

[System.Serializable]
public class DeathMatchInfo : GameTypeInfo
{
	public int KillsLimit = 10;
	public float TimeLimit = 10*60;
}

[System.Serializable]
public class ZoneControlInfo : GameTypeInfo
{
	public int TicketsLimit = 100;
	public float ZoneControlUpdate = 5;
	public float FlagRaiseTime = 9.0f;

	// true if player can choose a team even if the team contains more players than second team
	public bool FreedomOfTeamSelection = false;
}

public static class GameInfoSettings
{
	public readonly static string DEFAULT_GAME_TYPES_ID = "default";

	static GameTypeInfo[] m_GameInfos = new GameTypeInfo[(int)E_MPGameType.None];

	static GameInfoSettings()
	{
		Load(null);
	}

	public static GameTypeInfo GetGameInfo(E_MPGameType gameType)
	{
		if (gameType == E_MPGameType.None)
			return default(GameTypeInfo);
		return m_GameInfos[(int)gameType] ?? default(GameTypeInfo);
	}

	public static T GetGameInfo<T>() where T : GameTypeInfo
	{
		foreach (var gameInfo in m_GameInfos)
		{
			if (gameInfo is T)
				return (T)gameInfo;
		}
		return default(T);
	}

	public static void Load(string json)
	{
		JsonData data;
		try
		{
			data = JsonMapper.ToObject(json);
		}
		catch
		{
			data = new JsonData();
		}

		for (E_MPGameType gameType = E_MPGameType.DeathMatch; gameType < E_MPGameType.None; ++gameType)
		{
			string gameTypeName = GameInfo.GetGameTypeName(gameType);
			GameTypeInfo gameInfo = null;

			if (data.IsObject == true && data.HasValue(gameTypeName) == true)
			{
				JsonData gameData = data[gameTypeName];

				switch (gameType)
				{
				case E_MPGameType.DeathMatch:
					gameInfo = JsonMapper.ToObject<DeathMatchInfo>(gameData.ToJson());
					break;
				case E_MPGameType.ZoneControl:
					gameInfo = JsonMapper.ToObject<ZoneControlInfo>(gameData.ToJson());
					break;
				default:
					throw new System.ArgumentOutOfRangeException("GameInfoSettings.Load() - unknown GameType '" + gameType + "'");
				}
			}
			else
			{
				switch (gameType)
				{
				case E_MPGameType.DeathMatch:
					gameInfo = new DeathMatchInfo();
					break;
				case E_MPGameType.ZoneControl:
					gameInfo = new ZoneControlInfo();
					break;
				default:
					throw new System.ArgumentOutOfRangeException("GameInfoSettings.Load() - unknown GameType '" + gameType + "'");
				}
			}

#if IGNORE_DESIRED_RANK_TO_PLAY
			gameInfo.MinimalDesiredRankToPlay = 1;
#endif

			m_GameInfos[(int)gameType] = gameInfo;
		}
	}

#if UNITY_EDITOR
	public static string Save()
	{
		JsonData data = new JsonData(JsonType.Object);

		for (E_MPGameType gameType = E_MPGameType.DeathMatch; gameType < E_MPGameType.None; ++gameType)
		{
			string gameTypeName = GameInfo.GetGameTypeName(gameType);
			data[gameTypeName] = JsonMapper.ToObject(JsonMapper.ToJson(m_GameInfos[(int)gameType]));
		}

		return data.ToJson();
	}
#endif
}

[System.Serializable]
public class GameInfo
{
	public enum GameState
	{
		Initializing,
		Waiting,
		Running,
		Loading,
	}

	public E_MPGameType GameType { get; set; }

	public string GameTypeName
	{
		get { return GetGameTypeName(GameType); }
	}

	public static string GetGameTypeName(E_MPGameType gameType)
	{
		switch (gameType)
		{
		case E_MPGameType.ZoneControl:
			return "ZC";
		case E_MPGameType.DeathMatch:
			return "DM";
		default:
			throw new System.ArgumentOutOfRangeException("GameInfo.GetGameTypeName() - unknown GameType '" + gameType + "'");
		}
	}

	public static E_MPGameType GetGameTypeFromName(string gameTypeName)
	{
		switch (gameTypeName)
		{
		case "ZC":
			return E_MPGameType.ZoneControl;
		case "DM":
			return E_MPGameType.DeathMatch;
		default:
			throw new System.ArgumentOutOfRangeException("GameInfo.GetGameTypeFromName() - unknown GameType name '" + gameTypeName + "'");
		}
	}

	public int CurrentLevelIndex { get; private set; }
	public int CurrentRound { get; set; }
	public GameState State { get; set; }
	public float RoundStartTime { get; private set; }

	public Dictionary<E_Team, int> TeamScore = new Dictionary<E_Team, int>(TeamComparer.Instance);

	[SerializeField] DeathMatchInfo DeathMatchSetup = new DeathMatchInfo();
	[SerializeField] public ZoneControlInfo ZoneControlSetup = new ZoneControlInfo();

	public GameTypeInfo GameSetup { get; private set; }

	public float NextGameUpdate { get; set; }

	public GameInfo()
	{
		State = GameState.Initializing;
		TeamScore.Add(E_Team.Good, 0);
		TeamScore.Add(E_Team.Bad, 0);
	}

	public string CurrentLevelName
	{
		get { return GameSetup.MPLevels[CurrentLevelIndex].Name; }
	}

	public string CurrentLevel
	{
		get { return GameSetup.MPLevels[CurrentLevelIndex].Level; }
	}

	public bool IsLastRound()
	{
		return CurrentRound == DeathMatchSetup.RoundPerLevel;
	}

	public void PrepareForGame(E_MPGameType type, bool randomMap = false)
	{
		GameType = type;

		if (type == E_MPGameType.DeathMatch)
			GameSetup = DeathMatchSetup;
		else if (type == E_MPGameType.ZoneControl)
			GameSetup = ZoneControlSetup;

		CurrentLevelIndex = 0;
		int validIndex = 0;

		if (randomMap)
		{
			int validCount = 0;

			for (int i = 0; i < GameSetup.MPLevels.Count; ++i)
			{
				if (GameSetup.MPLevels[i].Disabled == false)
					validCount++;
			}

			System.Random random = new System.Random(System.Environment.TickCount + System.Diagnostics.Process.GetCurrentProcess().Id);
			validIndex = random.Next(validCount);
		}

		// Find first non-disabled level
		for (int i = 0; i < GameSetup.MPLevels.Count; ++i)
		{
			if (GameSetup.MPLevels[i].Disabled == false)
			{
				if (validIndex == 0)
				{
					CurrentLevelIndex = i;
					break;
				}

				validIndex--;
			}
		}
	}

	public void StartRound()
	{
		RoundStartTime = Time.timeSinceLevelLoad;
		State = GameState.Running;

		if (GameType == E_MPGameType.ZoneControl)
		{
			TeamScore[E_Team.Good] = ZoneControlSetup.TicketsLimit;
			TeamScore[E_Team.Bad] = ZoneControlSetup.TicketsLimit;
		}
		else
		{
			TeamScore[E_Team.Good] = 0;
			TeamScore[E_Team.Bad] = 0;
		}

		NextGameUpdate = 0;
	}

	public E_Team GetWinnerTeam()
	{
		if (GameType == E_MPGameType.ZoneControl)
		{
			if (TeamScore[E_Team.Good] == 0)
				return E_Team.Bad;
			else if (TeamScore[E_Team.Bad] == 0)
				return E_Team.Good;
		}

		return E_Team.None;
	}

	public void SetNextLevel()
	{
		// Find next non-disabled level
		for (int i = 0; i < GameSetup.MPLevels.Count; ++i)
		{
			CurrentLevelIndex++;
			if (CurrentLevelIndex > GameSetup.MPLevels.Count - 1)
				CurrentLevelIndex = 0;

			if (GameSetup.MPLevels[CurrentLevelIndex].Disabled == false)
				break;
		}
	}

	public void AllModes_SetTimerSpawn(int timerSpawn)
	{
		DeathMatchSetup.TimerSpawn = timerSpawn;
		ZoneControlSetup.TimerSpawn = timerSpawn;
	}

	public void AllModes_SetTimerRoundStart(float timerStart)
	{
		DeathMatchSetup.TimerRoundStart = timerStart;
		ZoneControlSetup.TimerRoundStart = timerStart;
	}

	public void AllModes_SetTimerRoundEnd(float timerEnd)
	{
		DeathMatchSetup.TimerRoundEnd = timerEnd;
		ZoneControlSetup.TimerRoundEnd = timerEnd;
	}

	public void AllModes_SetRoundsPerLevel(int value)
	{
		DeathMatchSetup.RoundPerLevel = value;
		ZoneControlSetup.RoundPerLevel = value;
	}

	public void ZoneControl_SetTickets(int tickects)
	{
		ZoneControlSetup.TicketsLimit = tickects;
	}

	public void DeahMatch_SetKillLimit(int limit)
	{
		DeathMatchSetup.KillsLimit = limit;
	}

	public void DeahMatch_SetTimeLimit(float limit)
	{
		DeathMatchSetup.TimeLimit = limit;
	}

	// setup conditions for ending of current round
	// debug purpose mainly
	public void SimulateEndRound()
	{
		State = GameState.Running;

		if (GameType == E_MPGameType.DeathMatch)
		{
			RoundStartTime = float.MinValue;
		}
		else if (GameType == E_MPGameType.ZoneControl)
		{
			TeamScore[E_Team.Bad] = 0;
		}
	}

	public float GetSpawnTimeProtection()
	{
		return GameSetup.SpawnTimeProtection;
	}

	public float GetMaxAllowedTimeWithoutRespawn()
	{
		return GameSetup.MaximumTimeWithoutSpawn;
	}

	public float GetMaxAllowedTimeWithoutUserInput()
	{
		return GameSetup.MaximumTimeWithoutUserInput;
	}

	/*public void Write(BitStream stream)
    {
        stream.Write<E_MPGameType>(GameType);
        stream.WriteByte((byte)CurrentRound);

        stream.WriteString(GameSetup.MPLevels[CurrentLevelIndex].Name);
        stream.WriteByte((byte)GameSetup.RoundPerLevel);

        switch(GameType)
        {
            case E_MPGameType.DeathMatch:
                stream.WriteByte((byte)(GameSetup as DeathMatchInfo).KillsLimit);
                stream.WriteInt16((short)(GameSetup as DeathMatchInfo).TimeLimit);
                break;
            case E_MPGameType.ZoneControl:
                stream.WriteInt16((short)TeamScore[E_Team.Good]);
                stream.WriteInt16((short)TeamScore[E_Team.Bad]);
                break
        }
    }

    public void Read(BitStream stream)
    {
        GameType = stream.Read<E_MPGameType>();
        CurrentRound = stream.ReadByte();

        stream.ReadString(GameSetup.MPLevels[CurrentLevelIndex].Name);
        stream.WriteByte((byte)GameSetup.RoundPerLevel);

        switch(GameType)
        {
            case E_MPGameType.DeathMatch:
                stream.WriteByte((byte)(GameSetup as DeathMatchInfo).KillsLimit);
                stream.WriteInt16((short)(GameSetup as DeathMatchInfo).TimeLimit);
                break;
            case E_MPGameType.ZoneControl:
                stream.WriteInt16((short)TeamScore[E_Team.Good]);
                stream.WriteInt16((short)TeamScore[E_Team.Bad]);
                break
        }
    }

    public static void Serialize(BitStream stream, object value, params object[] args)
    {
        GameInfo gi = (GameInfo)value;
        gi.Write(stream);
    }

    public static object Deserialize(BitStream stream, params object[] args)
    {
        GameInfo gi = new GameInfo();
        gi.Read(stream);
        return gi;
    }*/
}
