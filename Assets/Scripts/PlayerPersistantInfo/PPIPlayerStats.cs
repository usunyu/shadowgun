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
using Array = System.Array;
using DateTime = System.DateTime;

public class PPIPlayerStats
{
	public struct GameData
	{
		public int PlayedTimes;
		public float TimeSpent;

		public int Score;
		public int Experience;
		public int Money;
		public int Golds;

		public int Shots;
		public int Hits;
		public int Kills;
		public int Headshots;
		public int Deaths;

		//public int    Suicides;

		//public int    Position;       // DM match

		//public int    TeamResult;     // ZC match
		//public int    ZonesCaptured;  // ZC match

		public double LastPlayedDate; // TimeSpan in seconds since 1.1.1970
		public double LastFinishedGameDate; // TimeSpan in seconds since 1.1.1970

		public override string ToString()
		{
			return string.Format("[GameData] - Shots:" + Shots + " Hits:" + Hits + " Kills:" + Kills + " Deaths:" + Deaths);
		}
	}

	public struct TransientData
	{
		public DateTime Date;
		public int Experience;
		public int GamesFinished;
		public int GamesWon;
	}

	// PUBLIC MEMBERS

	public GameData[] Games = new GameData[(int)E_MPGameType.None];
	public TransientData Today = new TransientData();

	// PUBLIC METHODS

	public int GetPlayedTimes()
	{
		int value = 0;
		Array.ForEach(Games, (data) => { value += data.PlayedTimes; });
		return value;
	}

	public float GetTimeSpent()
	{
		float value = 0;
		Array.ForEach(Games, (data) => { value += data.TimeSpent; });
		return value;
	}

	public double GetLastPlayedDate()
	{
		double value = 0;
		Array.ForEach(Games,
					  (data) =>
					  {
						  if (value < data.LastPlayedDate)
							  value = data.LastPlayedDate;
					  });
		return value;
	}

	public int GetScore()
	{
		int value = 0;
		Array.ForEach(Games, (data) => { value += data.Score; });
		return value;
	}

	public int GetExperience()
	{
		int value = 0;
		Array.ForEach(Games, (data) => { value += data.Experience; });
		return value;
	}

	public int GetMoney()
	{
		int value = 0;
		Array.ForEach(Games, (data) => { value += data.Money; });
		return value;
	}

	public int GetGolds()
	{
		int value = 0;
		Array.ForEach(Games, (data) => { value += data.Golds; });
		return value;
	}

	public int GetShots()
	{
		int value = 0;
		Array.ForEach(Games, (data) => { value += data.Shots; });
		return value;
	}

	public int GetHits()
	{
		int value = 0;
		Array.ForEach(Games, (data) => { value += data.Hits; });
		return value;
	}

	public int GetKills()
	{
		int value = 0;
		Array.ForEach(Games, (data) => { value += data.Kills; });
		return value;
	}

	public int GetHeadshots()
	{
		int value = 0;
		Array.ForEach(Games, (data) => { value += data.Headshots; });
		return value;
	}

	public int GetDeaths()
	{
		int value = 0;
		Array.ForEach(Games, (data) => { value += data.Deaths; });
		return value;
	}

	//public int        GetSuicides()       { int    value = 0; Array.ForEach(Games, (data) => { value += data.Suicides;    }); return value; }

	public GameData GetGameData(E_MPGameType gameType)
	{
		if ((int)gameType >= Games.Length)
			return default(GameData);
		return Games[(int)gameType];
	}
}
