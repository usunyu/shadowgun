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
using FriendInfo = FriendList.FriendInfo;

// =============================================================================================================================
// =============================================================================================================================
public class Statistics : IEnumerable<Statistics.Item>
{
	// -------------------------------------------------------------------------------------------------------------------------	
	const int TEXT_ID_EXPERIENCE = 02040311; //	Experience
	const int TEXT_ID_MONEY = 02040312; //	Total money
	const int TEXT_ID_KILLS = 02040313; //	Kills
	const int TEXT_ID_DEATHS = 02040314; //	Deaths
	const int TEXT_ID_MISSIONS_PLAYED = 02040315; //	Missions played
	const int TEXT_ID_SUCCESSED_MISSIONS = 02040316; //	Successed missions
	const int TEXT_ID_CARNAGES = 02040317; //	Carnages
	const int TEXT_ID_GOLD = 02040318; //	Total gold
	const int TEXT_ID_HEADSHOTS = 02040319; //	Headshots
	const int TEXT_ID_LIMBS = 02040320; //	Limbs
	const int TEXT_ID_TOTAL_TIME = 02040321; //	Game time

	const int TEXT_ID_ARENA_1 = 01010600; //	Bloody Subway
	const int TEXT_ID_ARENA_1_HIGH_SCORE = 02040322; //	<ARENA_1_NAME> high score
	const int TEXT_ID_ARENA_1_TOTAL_TIME = 02040323; //	<ARENA_1_NAME> game time
	const int TEXT_ID_ARENA_1_PLAYED = 02040324; //	<ARENA_1_NAME> played

	// -------------------------------------------------------------------------------------------------------------------------		
	public enum E_Mode
	{
		None,
		Player,
		CompareWithFriend,
		CompareWithBest
	}
	public enum E_Better
	{
		None,
		Smaller,
		Bigger
	}

	// -------------------------------------------------------------------------------------------------------------------------			
	public abstract class Item
	{
		public int m_NameIndex;
		public string m_NameText;
	}

	public class BaseItem<T> : Item
	{
		public T m_PlayerValue;
		public T m_SecondValue;
		public string m_SecondValueFriendName;

		public bool m_HighlightPlayer;
		public bool m_HighlightFriend;
	}

	// -------------------------------------------------------------------------------------------------------------------------
	// some predefined item types...
	public class IntItem : BaseItem<int>
	{
	}
	public class FloatItem : BaseItem<float>
	{
	}
	public class StringItem : BaseItem<string>
	{
	}

	// -------------------------------------------------------------------------------------------------------------------------		
	E_Mode m_CurrentMode = E_Mode.None;
	List<Item> m_StatisticsItems = new List<Item>();

	// -------------------------------------------------------------------------------------------------------------------------			
	public int Count
	{
		get { return m_StatisticsItems.Count; }
	}

	public E_Mode Mode
	{
		get { return m_CurrentMode; }
	}

	// =========================================================================================================================
	// === public interface ====================================================================================================
	/*// mono error... TODO
    public Statistics.Item this[int index]
    {
        get { return m_StatisticsItems[index];  }
    }*/

	// -------------------------------------------------------------------------------------------------------------------------			    
	public Item GetItem(int index)
	{
		return m_StatisticsItems[index];
	}

	// -------------------------------------------------------------------------------------------------------------------------				
	public void PrepareFor(E_Mode inMode, string inFriendName)
	{
		PrepareFor_Internal(inMode, inFriendName);
	}

	// -------------------------------------------------------------------------------------------------------------------------					
	public void Clear()
	{
		m_CurrentMode = E_Mode.None;
		m_StatisticsItems.Clear();
	}

	// =========================================================================================================================
	// === IEnumerable interface ===============================================================================================
	public IEnumerable<Statistics.Item> Range(int inFrom, int inTo)
	{
		if (inFrom < 0 || inFrom >= m_StatisticsItems.Count)
			yield break;
		if (inTo < 0 || inTo >= m_StatisticsItems.Count)
			yield break;
		for (int i = inFrom; i < inTo; i++)
			yield return m_StatisticsItems[i];
	}

	//..........................................................................................................................		
	public IEnumerator<Statistics.Item> GetEnumerator()
	{
		for (int i = 0; i < m_StatisticsItems.Count; ++i)
		{
			yield return m_StatisticsItems[i];
		}
	}

	//..........................................................................................................................		
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	// =========================================================================================================================
	// === internal ============================================================================================================
	void PrepareFor_Internal(E_Mode inMode, string inFriendName)
	{
/*
		// check actual state of statistics...
		//if(m_CurrentMode == inMode)
		//	return;

		// reset internal values...			
		m_CurrentMode = inMode;
		m_StatisticsItems.Clear();
		
		
		// prepare data for correct statistics harvesting...
		PlayerPersistentInfoData	playerdata = null;
		
		try {
			playerdata = Game.Instance.PlayerPersistentInfo.GetPlayerData_ForStatistics();		
		}
		catch {
			PlayerPersistantInfo __ppinfo = new PlayerPersistantInfo();
			__ppinfo.InitPlayerDataFromStr(GameSaveLoadUtl.OpenReadPlayerData().GetString("PPI"));
			playerdata  = __ppinfo.GetPlayerData_ForStatistics();
		}
		
		List<FriendInfo> 			friendsData = new List<FriendInfo>();
		
		if(inMode == E_Mode.CompareWithFriend)
		{
			if(string.IsNullOrEmpty(inFriendName) == false)
			{
				FriendInfo fi = GameCloudManager.friendList.friends.Find(f => f.m_Name == inFriendName);
				if(fi != null && fi.m_PPIData != null)
				{
					friendsData.Add(fi);
				}
			}
			else
			{
				Debug.LogError("Invalid friend name: " + inFriendName);
			}
		}
		else if(inMode == E_Mode.CompareWithBest)
		{
			friendsData = GameCloudManager.friendList.friends;
		}
		
		
		// and now harvest all interesting statistics from player and friends data...
		m_StatisticsItems = HarvestStatistics(playerdata, friendsData);
		*/

		m_StatisticsItems = HarvestStatistics(null, null);
	}

	//..........................................................................................................................		
	static List<Item> HarvestStatistics(PlayerPersistentInfoData inPlayerData, List<FriendInfo> inFriendsData)
	{
		List<Item> statistics = new List<Item>();

		statistics.Add(Get_Experiance(TEXT_ID_EXPERIENCE, inPlayerData, inFriendsData));
//		statistics.Add( Get_Money	   		(TEXT_ID_MONEY    				, inPlayerData, inFriendsData) );
//		statistics.Add( Get_Gold 			(TEXT_ID_GOLD					, inPlayerData, inFriendsData) );
		statistics.Add(Get_Kills(TEXT_ID_KILLS, inPlayerData, inFriendsData));
		statistics.Add(Get_Deaths(TEXT_ID_DEATHS, inPlayerData, inFriendsData));
//		statistics.Add( Get_HeadShots 		(TEXT_ID_HEADSHOTS				, inPlayerData, inFriendsData) );
//		statistics.Add( Get_Carnages 		(TEXT_ID_CARNAGES				, inPlayerData, inFriendsData) );
//		statistics.Add( Get_Limbs			(TEXT_ID_LIMBS					, inPlayerData, inFriendsData) );
//		statistics.Add( Get_PlayedMissions	(TEXT_ID_MISSIONS_PLAYED		, inPlayerData, inFriendsData) );
//		statistics.Add( Get_MissionSuccess	(TEXT_ID_SUCCESSED_MISSIONS		, inPlayerData, inFriendsData) );
		statistics.Add(Get_GameTime(TEXT_ID_TOTAL_TIME, inPlayerData, inFriendsData, E_Better.Bigger));
		/*
		// Specific statistics for 1. Arena
		string arena1_name 		= TextDatabase.instance[TEXT_ID_ARENA_1];
		string arena1_HighScore = TextDatabase.instance[TEXT_ID_ARENA_1_HIGH_SCORE]	.Replace("<ARENA_1_NAME>", arena1_name);
		string arena1_TotalTime = TextDatabase.instance[TEXT_ID_ARENA_1_TOTAL_TIME]	.Replace("<ARENA_1_NAME>", arena1_name);
		string arena1_Played 	= TextDatabase.instance[TEXT_ID_ARENA_1_PLAYED]		.Replace("<ARENA_1_NAME>", arena1_name);

		statistics.Add( Get_Arena1_HighScore(arena1_HighScore				, inPlayerData, inFriendsData) );
		statistics.Add( Get_Arena1_GameTime	(arena1_TotalTime				, inPlayerData, inFriendsData, E_Better.Bigger) );
		statistics.Add( Get_Arena1_Played	(arena1_Played					, inPlayerData, inFriendsData) );
		*/
		return statistics;
	}

	//..........................................................................................................................		
	static Item Get_Experiance(int inItemNameTextID, PlayerPersistentInfoData inPlayerData, List<FriendInfo> inFriendsData)
	{
		return _GetIntItem(inItemNameTextID, inPlayerData, inFriendsData, E_Better.Bigger, val => val.Params.Experience);
	}

	//..........................................................................................................................		
//	private static Item Get_Money (int inItemNameTextID, PlayerPersistentInfoData inPlayerData, List<FriendInfo> inFriendsData)
//	{	return _GetIntItem(inItemNameTextID, inPlayerData, inFriendsData, E_Better.Bigger, val => val.Params.TotalMoney );				}

	//..........................................................................................................................		
//	private static Item Get_Gold (int inItemNameTextID, PlayerPersistentInfoData inPlayerData, List<FriendInfo> inFriendsData)
//	{	return _GetIntItem(inItemNameTextID, inPlayerData, inFriendsData, E_Better.Bigger, val => val.Params.TotalGold );				}

	//..........................................................................................................................			
	static Item Get_Kills(int inItemNameTextID, PlayerPersistentInfoData inPlayerData, List<FriendInfo> inFriendsData)
	{
		return _GetIntItem(inItemNameTextID, inPlayerData, inFriendsData, E_Better.Bigger, val => val.Stats.GetKills());
	}

	//..........................................................................................................................	
	static Item Get_Deaths(int inItemNameTextID, PlayerPersistentInfoData inPlayerData, List<FriendInfo> inFriendsData)
	{
		return _GetIntItem(inItemNameTextID, inPlayerData, inFriendsData, E_Better.Smaller, val => val.Stats.GetDeaths());
	}

	//..........................................................................................................................			
//	private static Item Get_HeadShots (int inItemNameTextID, PlayerPersistentInfoData inPlayerData, List<FriendInfo> inFriendsData)
//	{	return _GetIntItem(inItemNameTextID, inPlayerData, inFriendsData, E_Better.Bigger, val => val.Params.HeadShots );				}

	//..........................................................................................................................			
//	private static Item Get_Carnages (int inItemNameTextID, PlayerPersistentInfoData inPlayerData, List<FriendInfo> inFriendsData)
//	{	return _GetIntItem(inItemNameTextID, inPlayerData, inFriendsData, E_Better.Bigger, val => val.Params.Carnage );				}

	//..........................................................................................................................			
//	private static Item Get_Limbs (int inItemNameTextID, PlayerPersistentInfoData inPlayerData, List<FriendInfo> inFriendsData)
//	{	return _GetIntItem(inItemNameTextID, inPlayerData, inFriendsData, E_Better.Bigger, val => val.Params.Limbs );				}

	//..........................................................................................................................		
//	private static Item Get_PlayedMissions (int inItemNameTextID, PlayerPersistentInfoData inPlayerData, List<FriendInfo> inFriendsData)
//	{	return _GetIntItem(inItemNameTextID, inPlayerData, inFriendsData, E_Better.Bigger, val => val.Params.MissionCount );		}		

	//..........................................................................................................................		
//	private static Item Get_MissionSuccess (int inItemNameTextID, PlayerPersistentInfoData inPlayerData, List<FriendInfo> inFriendsData)
//	{	return _GetIntItem(inItemNameTextID, inPlayerData, inFriendsData, E_Better.Bigger, val => val.Params.MissionSuccess );}

	//public float	GameTime;// done

	//..........................................................................................................................		
	public delegate int IntExtractor(PlayerPersistentInfoData inData);

	static Item _GetIntItem(int inItemNameTextID,
							PlayerPersistentInfoData inPlayerData,
							List<FriendInfo> inFriendsData,
							E_Better inBetter,
							IntExtractor inExtractor)
	{
		IntItem item = new IntItem();
		item.m_NameIndex = inItemNameTextID;

		return _GetIntItem(item, inPlayerData, inFriendsData, inBetter, inExtractor);
	}

	static Item _GetIntItem(string inItemNameText,
							PlayerPersistentInfoData inPlayerData,
							List<FriendInfo> inFriendsData,
							E_Better inBetter,
							IntExtractor inExtractor)
	{
		IntItem item = new IntItem();
		item.m_NameText = inItemNameText;
		item.m_NameIndex = 0;

		return _GetIntItem(item, inPlayerData, inFriendsData, inBetter, inExtractor);
	}

	static Item _GetIntItem(IntItem inItem,
							PlayerPersistentInfoData inPlayerData,
							List<FriendInfo> inFriendsData,
							E_Better inBetter,
							IntExtractor inExtractor)
	{
		IntItem item = inItem;
		item.m_PlayerValue = -1;
		item.m_SecondValue = -1;
		return item;

		/*
		IntItem item 	   = inItem;
		item.m_PlayerValue = inExtractor(inPlayerData);
		item.m_SecondValue = -1;
		
		foreach(FriendInfo fi in inFriendsData)
		{
			int friendValue = inExtractor(fi.m_PPIData);
			if(friendValue > item.m_SecondValue)
			{
				item.m_SecondValue 			 = friendValue;
				item.m_SecondValueFriendName = fi.m_Name;
			}
		}
		
		if(inBetter != E_Better.None && inFriendsData.Count > 0)
		{
			item.m_HighlightPlayer = (inBetter == E_Better.Bigger  && item.m_PlayerValue > item.m_SecondValue) ||
									 (inBetter == E_Better.Smaller && item.m_PlayerValue < item.m_SecondValue);
			item.m_HighlightFriend = (inBetter == E_Better.Bigger  && item.m_PlayerValue < item.m_SecondValue) ||
									 (inBetter == E_Better.Smaller && item.m_PlayerValue > item.m_SecondValue);
		}
								 
		return item;
		*/
	}

	static Item Get_GameTime(int inItemNameTextID,
							 PlayerPersistentInfoData inPlayerData,
							 List<FriendInfo> inFriendsData,
							 E_Better inBetter)
	{
		StringItem item = new StringItem();
		item.m_NameIndex = inItemNameTextID;
		item.m_PlayerValue = "??:??";
		item.m_SecondValue = "??:??";
		return item;
		/*
		StringItem item    = new StringItem();
		item.m_NameIndex   = inItemNameTextID;
		
		float playersTime = inPlayerData.Params.GameTime;
		float friendsTime = -1;
		
		foreach(FriendInfo fi in inFriendsData)
		{
			float friendValue = fi.m_PPIData.Params.GameTime;
			if(friendValue > friendsTime)
			{
				friendsTime 			 	 = friendValue;
				item.m_SecondValueFriendName = fi.m_Name;
			}
		}
		
		if(inBetter != E_Better.None && inFriendsData.Count > 0)
		{
			item.m_HighlightPlayer = (inBetter == E_Better.Bigger  && playersTime > friendsTime) ||
									 (inBetter == E_Better.Smaller && playersTime < friendsTime);
			item.m_HighlightFriend = (inBetter == E_Better.Bigger  && playersTime < friendsTime) ||
									 (inBetter == E_Better.Smaller && playersTime > friendsTime);
		}
		
		System.TimeSpan duration = System.TimeSpan.FromSeconds( playersTime );
		
		
		//item.m_PlayerValue = string.Format("{0,3}:{1,2}", (int )duration.TotalHours, (int )duration.Minutes );
		item.m_PlayerValue = ((int )duration.TotalHours).ToString("00") +":"+((int )duration.Minutes).ToString("00");
		duration 		   = System.TimeSpan.FromSeconds( friendsTime );
		//item.m_SecondValue = string.Format("{0,3}:{1,2}", (int )duration.TotalHours, (int )duration.Minutes );
		item.m_SecondValue = ((int )duration.TotalHours).ToString("00") +":"+((int )duration.Minutes).ToString("00");		
		
		return item;
		*/
	}

	//..........................................................................................................................		
	/*
	private static Item Get_Arena1_HighScore (string inItemNameText, PlayerPersistentInfoData inPlayerData, List<FriendInfo> inFriendsData)
	{	return _GetIntItem(inItemNameText, inPlayerData, inFriendsData, E_Better.Bigger, val => val.Params.Arena1HiScore );}

	private static Item Get_Arena1_Played 	 (string inItemNameText, PlayerPersistentInfoData inPlayerData, List<FriendInfo> inFriendsData)
	{	return _GetIntItem(inItemNameText, inPlayerData, inFriendsData, E_Better.Bigger, val => val.Params.Arena1Played );}
	
	private static Item Get_Arena1_GameTime(string inItemNameText, PlayerPersistentInfoData inPlayerData, List<FriendInfo> inFriendsData,
									  E_Better inBetter)
	{
		StringItem item    = new StringItem();
		item.m_NameIndex   = 0;
		item.m_NameText    = inItemNameText;
		
		float playersTime = inPlayerData.Params.Arena1Time;
		float friendsTime = -1;
		
		foreach(FriendInfo fi in inFriendsData)
		{
			float friendValue = fi.m_PPIData.Params.Arena1Time;
			if(friendValue > friendsTime)
			{
				friendsTime 			 	 = friendValue;
				item.m_SecondValueFriendName = fi.m_Name;
			}
		}
		
		if(inBetter != E_Better.None && inFriendsData.Count > 0)
		{
			item.m_HighlightPlayer = (inBetter == E_Better.Bigger  && playersTime > friendsTime) ||
									 (inBetter == E_Better.Smaller && playersTime < friendsTime);
			item.m_HighlightFriend = (inBetter == E_Better.Bigger  && playersTime < friendsTime) ||
									 (inBetter == E_Better.Smaller && playersTime > friendsTime);
		}
		
		System.TimeSpan duration = System.TimeSpan.FromSeconds( playersTime );
		
		
		//item.m_PlayerValue = string.Format("{0,3}:{1,2}", (int )duration.TotalHours, (int )duration.Minutes );
		item.m_PlayerValue = ((int )duration.TotalHours).ToString("00") +":"+((int )duration.Minutes).ToString("00");
		duration 		   = System.TimeSpan.FromSeconds( friendsTime );
		//item.m_SecondValue = string.Format("{0,3}:{1,2}", (int )duration.TotalHours, (int )duration.Minutes );
		item.m_SecondValue = ((int )duration.TotalHours).ToString("00") +":"+((int )duration.Minutes).ToString("00");		
		
		return item;
	}
	*/
}
