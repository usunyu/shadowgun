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
using System.IO;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using LitJson;

public class PPIDailyRewards
{
	public readonly static int DAYS_PER_CYCLE = 5;

	// do NOT change order of enumerators
	public enum E_RewardType
	{
		None,
		Chips,
		Gold,
		Item
	}

	// do NOT change order of enumerators
	public enum E_ConditionType
	{
		None,
		GainExperience,
		PlayNumberOfMatches,
		WinNumberOfMatches
	}

	[System.Serializable]
	public class Reward
	{
		public E_RewardType Type;
		public int Id;
		public int Value;

		public Reward DeepCopy()
		{
			return new Reward()
			{
				Type = Type,
				Id = Id,
				Value = Value
			};
		}
	}

	[System.Serializable]
	public class Condition
	{
		public E_ConditionType Type;
		public int Value;

		public Condition DeepCopy()
		{
			return new Condition()
			{
				Type = Type,
				Value = Value
			};
		}
	}

	[System.Serializable]
	public class Day
	{
		public Reward Reward = new Reward();
		public Condition Condition = new Condition();

		[HideInInspector] public bool Received;

		public Day DeepCopy()
		{
			return new Day()
			{
				Reward = Reward.DeepCopy(),
				Condition = Condition.DeepCopy(),
				Received = Received
			};
		}
	}

	// PUBLIC MEMBERS

	public int CurrentDay;
	public int PreviousDay;
	public Day[] Instant = new Day[DAYS_PER_CYCLE];
	public Day[] Conditional = new Day[DAYS_PER_CYCLE];

	// Last time player was rewarded. It is measured as number of days since standard base time known as "the epoch", namely January 1, 1970
	public long DailyRewardEndTime;

	// number of days player logged in row
	//public long    DailyRewardInRow; // the same as CurrentDay

	public static PPIDailyRewards Create()
	{
		PPIDailyRewards rewards = new PPIDailyRewards();
		return rewards.Fix();
	}
}

static class PPIDailyRewardsExtension
{
	public static PPIDailyRewards.Day GetCurrentInstantDay(this PPIDailyRewards @this)
	{
		if (@this.Instant == null)
			return new PPIDailyRewards.Day();
		if (@this.Instant.Length <= @this.CurrentDay)
			return new PPIDailyRewards.Day();
		return @this.Instant[@this.CurrentDay] ?? new PPIDailyRewards.Day();
	}

	public static PPIDailyRewards.Day GetCurrentConditionalDay(this PPIDailyRewards @this)
	{
		if (@this.Conditional == null)
			return new PPIDailyRewards.Day();
		if (@this.Conditional.Length <= @this.CurrentDay)
			return new PPIDailyRewards.Day();
		return @this.Conditional[@this.CurrentDay] ?? new PPIDailyRewards.Day();
	}

	public static bool IsAccomplished(this PPIDailyRewards.Day @this, PPIPlayerStats.TransientData stats)
	{
		switch (@this.Condition.Type)
		{
		case PPIDailyRewards.E_ConditionType.None:
			return true;
		case PPIDailyRewards.E_ConditionType.GainExperience:
			return stats.Experience >= @this.Condition.Value;
		case PPIDailyRewards.E_ConditionType.PlayNumberOfMatches:
			return stats.GamesFinished >= @this.Condition.Value;
		case PPIDailyRewards.E_ConditionType.WinNumberOfMatches:
			return stats.GamesWon >= @this.Condition.Value;
		default:
			throw new System.IndexOutOfRangeException();
		}
	}

	public static float Progress(this PPIDailyRewards.Day @this, PPIPlayerStats.TransientData stats)
	{
		switch (@this.Condition.Type)
		{
		case PPIDailyRewards.E_ConditionType.None:
			return 1.0f;
		case PPIDailyRewards.E_ConditionType.GainExperience:
			return Mathf.Min(@this.Condition.Value > 0 ? stats.Experience/(float)@this.Condition.Value : 1.0f, 1.0f);
		case PPIDailyRewards.E_ConditionType.PlayNumberOfMatches:
			return Mathf.Min(@this.Condition.Value > 0 ? stats.GamesFinished/(float)@this.Condition.Value : 1.0f, 1.0f);
		case PPIDailyRewards.E_ConditionType.WinNumberOfMatches:
			return Mathf.Min(@this.Condition.Value > 0 ? stats.GamesWon/(float)@this.Condition.Value : 1.0f, 1.0f);
		default:
			throw new System.IndexOutOfRangeException();
		}
	}

	public static PPIDailyRewards Restore(string primaryKey, ref System.DateTime date, string filename = "rewards")
	{
		DictionaryFile file = GetFile(primaryKey, filename);
		if (file == null)
			return null;

		file.Load();

		string json = file.GetString("rewards", "");
		if (string.IsNullOrEmpty(json) == true)
			return null;

		if (System.DateTime.TryParse(file.GetString("date", ""), out date) == false)
		{
			date = CloudDateTime.UtcNow.Date;
		}

		return Fix(JsonMapper.ToObject<PPIDailyRewards>(json));
	}

	public static void Store(this PPIDailyRewards @this, string primaryKey, string filename = "rewards")
	{
		DictionaryFile file = GetFile(primaryKey, filename);
		if (file == null)
			return;

		file.SetString("rewards", JsonMapper.ToJson(@this));
		file.SetString("date", CloudDateTime.UtcNow.Date.ToString());

		file.Save();
	}

	public static PPIDailyRewards Fix(this PPIDailyRewards @this)
	{
		PPIDailyRewards rewards = @this ?? new PPIDailyRewards();

		// fix instant rewards
		if (rewards.Instant == null || rewards.Instant.Length != PPIDailyRewards.DAYS_PER_CYCLE)
		{
			rewards.Instant = new PPIDailyRewards.Day[PPIDailyRewards.DAYS_PER_CYCLE];
		}
		for (int idx = 0; idx < PPIDailyRewards.DAYS_PER_CYCLE; ++idx)
		{
			rewards.Instant[idx] = rewards.Instant[idx] ?? new PPIDailyRewards.Day();
		}

		// fix conditional rewards
		if (rewards.Conditional == null || rewards.Conditional.Length != PPIDailyRewards.DAYS_PER_CYCLE)
		{
			rewards.Conditional = new PPIDailyRewards.Day[PPIDailyRewards.DAYS_PER_CYCLE];
		}
		for (int idx = 0; idx < PPIDailyRewards.DAYS_PER_CYCLE; ++idx)
		{
			rewards.Conditional[idx] = rewards.Conditional[idx] ?? new PPIDailyRewards.Day();
		}

		return rewards;
	}

	static DictionaryFile GetFile(string primaryKey, string filename)
	{
		DictionaryFile file = null;

		if (string.IsNullOrEmpty(primaryKey) == false)
		{
			string filepath = string.Format("users/{0}/." + filename, GuiBaseUtils.GetCleanName(primaryKey));
			file = new DictionaryFile(filepath);
		}

		return file;
	}
}
