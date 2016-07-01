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
using System;
using System.Collections;
using System.Collections.Generic;
using LitJson;
using BitStream = uLink.BitStream;
using NetworkPlayer = uLink.NetworkPlayer;

public class PlayerPersistantInfo
{
	public enum E_State
	{
		Disconnected,
		Connected,
		WaitingForSpawn,
		Spawned,
	}

	public NetworkPlayer Player;
	public int Ping;
	public E_Team Team = E_Team.None;
	public int ZoneIndex;
	public string Name;

	public string NameForGui
	{
		get { return GuiBaseUtils.FixNameForGui(Name); }
	}

	public string UserName_TODO
	{
		get { return PrimaryKey; }
	} //TODO: PRIMARY KEY - udelat lepsi impl		// Don't synchronize this over network to other players !

	public string UserNameForGui
	{
		get { return GuiBaseUtils.FixNameForGui(UserName_TODO); }
	}

	public string PrimaryKey; // Don't synchronize this over network to other players !
	public UInt64 PrimaryKeyHash;
	public RuntimePlatform Platform = Application.platform;
	public bool IsValid = false; // Used for server side PPI data validation
	public bool IsFirstRun = false;
	public bool IsPlayerConnected;
				// To allow all the asynchronous operation (keeping the reference to the PPI) to deal gracefully with the situation when the related player is no longer connected to the game

	public int Experience
	{
		get { return PlayerData.Params.Experience; }
		set
		{
			PlayerData.Params.Experience = value;
			PPIManager.Instance.NotifyLocalPPIChanged();
		}
	}

	public int Rank
	{
		get { return GetPlayerRankFromExperience(Experience); }
	}

	public int LastMajorRank
	{
		get { return BankData.LastMajorRank; }
		set { BankData.LastMajorRank = value; }
	}

	public int Money
	{
		get { return PlayerData.Params.Money; }
		set
		{
			PlayerData.Params.Money = value;
			PPIManager.Instance.NotifyLocalPPIChanged();
		}
	}

	public int Gold
	{
		get { return PlayerData.Params.Gold; }
		set
		{
			PlayerData.Params.Gold = value;
			PPIManager.Instance.NotifyLocalPPIChanged();
		}
	}

	public int Chips
	{
		get { return PlayerData.Params.Chips; }
	}

	public PPIInventoryList InventoryList
	{
		get { return PlayerData.InventoryList; }
	}

	public PPIEquipList EquipList
	{
		get { return PlayerData.EquipList; }
		private set { PlayerData.EquipList = value; }
	}

	public PPIUpgradeList Upgrades
	{
		get { return PlayerData.Upgrades; }
	}

	public PPIBankData BankData
	{
		get { return PlayerData.BankData; }
	}

	public PPIPlayerStats Statistics
	{
		get { return PlayerData.Stats; }
	}

	public PPIDailyRewards DailyRewards
	{
		get { return PlayerData.DailyRewards; }
	}

	public BanInfo Ban
	{
		get { return PlayerData.Params.Ban; }
	}

	public long PremiumAccountEndTimeMs
	{
		get { return PlayerData.Params.PremiumAccEndTime; }
	}

	public DateTime PremiumAccountEndDateTime
	{
		get { return GetPremiumAccountEndDateTime(); }
	}

	public bool IsPremiumAccountActive
	{
		get { return PremiumAccountEndDateTime > CloudDateTime.UtcNow ? true : false; }
	}

	public PPIRoundScore Score = new PPIRoundScore(); // synchro only by ppi manager
	public PPILocalStats LocalStats = new PPILocalStats(); // not synchronized !!!

	public E_State State { get; set; }

	PlayerPersistentInfoData PlayerData = new PlayerPersistentInfoData();

	internal List<CloudServices.AsyncOpResult> PendingSpawnRequests = new List<CloudServices.AsyncOpResult>();

	bool PlayerDataChanged = false;

	bool m_StatsRunning = false;
	float m_GameStartedAt = 0.0f;

#if !DEADZONE_CLIENT
	public PlayerAnticheat		Anticheat;
#endif

	const string ERR_STR_UNSYNCED_PPI_UPDATE = "Ignoring PPI update command on unsynced PPI";
	const int MAX_EXPERIENCE = 3000000;

	public void Write(BitStream stream)
	{
		stream.Write<NetworkPlayer>(Player);

		stream.Write<E_Team>(Team);
		stream.WriteByte((byte)ZoneIndex);
		stream.WriteInt16((short)Platform);
		stream.WriteString(Name);
		stream.WriteString(PrimaryKey); //TODO: PRIMARY KEY - pridat username?

		PlayerData.Write(stream);
	}

	public void Read(BitStream stream)
	{
		Player = stream.Read<NetworkPlayer>();
		Team = stream.Read<E_Team>();
		ZoneIndex = stream.ReadByte();
		Platform = (RuntimePlatform)stream.ReadInt16();
		Name = stream.ReadString();
		PrimaryKey = stream.ReadString(); //TODO: PRIMARY KEY - pridat username?

		PlayerData.Read(stream);
	}

	public static void Serialize(BitStream stream, object value, params object[] args)
	{
		PlayerPersistantInfo ppi = (PlayerPersistantInfo)value;
		ppi.Write(stream);
	}

	public static object Deserialize(BitStream stream, params object[] args)
	{
		PlayerPersistantInfo ppi = new PlayerPersistantInfo();
		ppi.Read(stream);
		return ppi;
	}

	public void UpdateFromPPIForRespawn(PlayerPersistantInfo ppi)
	{
		Team = ppi.Team;
		Name = ppi.Name;
		Platform = ppi.Platform;
		ZoneIndex = ppi.ZoneIndex;
		EquipList = ppi.EquipList;
	}

	public void ResetPlayerData()
	{
		PlayerData = new PlayerPersistentInfoData();

		IsValid = false;
		PlayerDataChanged = false;
	}

	public string GetPlayerDataAsJsonStr()
	{
		return JsonMapper.ToJson(PlayerData);
	}

	public bool InitPlayerDataFromStr(string jsonStr)
	{
		PlayerPersistentInfoData newPlayerData;

		try
		{
			newPlayerData = JsonMapper.ToObject<PlayerPersistentInfoData>(jsonStr);
		}

		catch (JsonException e)
		{
			Debug.LogError("JSON exception caught: " + e.Message);

			return false;
		}

		// fix daily rewards
		// which can be corrupted just after new account was created
		newPlayerData.DailyRewards = newPlayerData.DailyRewards ?? new PPIDailyRewards();
		newPlayerData.DailyRewards.Fix();

		PlayerData = newPlayerData;

		return true;
	}

	public string GetInventoryAsJSON()
	{
		return JsonMapper.ToJson(PlayerData.InventoryList);
	}

	public string GetEquipListAsJSON()
	{
		return JsonMapper.ToJson(PlayerData.EquipList);
	}

	public float GetRankDifferenceModificator(PlayerPersistantInfo ppi)
	{
		int rank = Rank;
		int rank2 = ppi.Rank;

		if (rank == rank2)
			return 1;

		if (rank > rank2)
		{
			int diff = Mathf.Min(5, rank - rank2);
			return 1 - 0.1f*diff; //max is 0.5f 
		}
		else
		{
			int diff = Mathf.Min(5, rank2 - rank);
			return 1 + 0.1f*diff; //max is 1.5f 
		}
	}

	public void AddScore(int score)
	{
		Score.Score += score;
	}

	public void AddExperience(int amount, E_AddMoneyAction moneyAction)
	{
		if (uLink.Network.isServer == false)
			throw new uLink.NetworkException("AddExperience: could be called only on server");

#if !DEADZONE_CLIENT
		AddMoney((int)(amount * GameplayRewards.MoneyModificator), moneyAction);
		
		if(IsPremiumAccountActive)
			amount = Mathf.CeilToInt(amount * GameplayRewards.PremiumAccountModificator);
		
		int oldRank = GetPlayerRankFromExperience(PlayerData.Params.Experience + Score.Experience);
		
		Score.Experience += (short)amount;
		
		int newRank = GetPlayerRankFromExperience(PlayerData.Params.Experience + Score.Experience);
		
		if(oldRank != newRank)
		{
			Server.Instance.SendRankUpToClient(this);
			
			AddMoney(GameplayRewards.MoneyRank, E_AddMoneyAction.Rank);
			
			Server.Instance.ShowCombatMsgOnClient(Player, Client.E_MessageType.Rank, 0, (short)(GameplayRewards.MoneyRank) );
		}
#endif
	}

	void AddMoney(int money, E_AddMoneyAction moneyAction)
	{
		if (uLink.Network.isServer == false)
			throw new uLink.NetworkException("AddMoney: could be called only on server");

		if (IsPremiumAccountActive)
			money = Mathf.CeilToInt(money*GameplayRewards.PremiumAccountModificator);

		Score.Money += (short)money;
	}

	public void AddGoldFromGameplay(int gold)
	{
		if (gold == 0)
			return;

		if (uLink.Network.isServer == false)
			throw new uLink.NetworkException("AddMoney: could be called only on server");

		if (IsPremiumAccountActive)
			gold = Mathf.CeilToInt(gold*GameplayRewards.PremiumAccountModificator);

		Score.Gold += (short)gold;
	}

	public void AddGold(int gold)
	{
		// used by slot machine to be able to modify this value localy
		// it will fetch PPI later to be in sync with cloud
		PlayerData.Params.Gold += gold;
	}

	public void AddChips(int chips)
	{
		// used by slot machine to be able to modify this value localy
		// it will fetch PPI later to be in sync with cloud
		PlayerData.Params.Chips += chips;
	}

	public void EndOfRound()
	{
#if !DEADZONE_CLIENT
		RoundFinalResult result = new RoundFinalResult(); //gather final result and send it to client
		
		result.GameType = Server.Instance.GameInfo.GameType;
		result.Team = Team;
		result.Place = PPIManager.Instance.GetPPIList().FindIndex(ps => ps.Player == Player);

		if (Server.Instance.GameInfo.GameType == E_MPGameType.ZoneControl )
		{
			E_Team winner = Server.Instance.GameInfo.GetWinnerTeam();	
			
			if(Team == winner)
			{
				AddExperience(GameplayRewards.ZC.Win, E_AddMoneyAction.ZoneControl);

				result.MissionExp = (short)(GameplayRewards.ZC.Win * (IsPremiumAccountActive? GameplayRewards.PremiumAccountModificator : 1));	
				result.MissioMoney = (short)(GameplayRewards.ZC.Win * GameplayRewards.MoneyModificator * (IsPremiumAccountActive? GameplayRewards.PremiumAccountModificator : 1));	
				
				result.Winner = true;
			}
			else 
			{
				AddExperience(GameplayRewards.ZC.Lost, E_AddMoneyAction.ZoneControl);
				
				result.MissionExp = (short)(GameplayRewards.ZC.Lost * (IsPremiumAccountActive? GameplayRewards.PremiumAccountModificator : 1));	
				result.MissioMoney = (short)(GameplayRewards.ZC.Lost * GameplayRewards.MoneyModificator * (IsPremiumAccountActive? GameplayRewards.PremiumAccountModificator : 1));	
				
			}
		}
		else 
		{
			int amount = 0;
			if(result.Place == 0)
				amount = GameplayRewards.DM.First;
			else if(result.Place == 1)
				amount = GameplayRewards.DM.Second;
			else if(result.Place == 2)
				amount = GameplayRewards.DM.Third;
			
			AddExperience(amount, E_AddMoneyAction.DM);

			result.MissionExp = (short)(amount * (IsPremiumAccountActive? GameplayRewards.PremiumAccountModificator : 1));	
			result.MissioMoney = (short)(amount * GameplayRewards.MoneyModificator * (IsPremiumAccountActive? GameplayRewards.PremiumAccountModificator : 1));	
			
			result.Winner = result.Place < 3;
		}
		
		if( GetPlayerRankFromExperience(PlayerData.Params.Experience) != GetPlayerRankFromExperience(PlayerData.Params.Experience + Score.Experience))
			result.NewRank = true;
		
		if(IsFirstGameToday(Server.Instance.GameInfo.GameType))
		{// FIRST RUN FOR TODAY .. Multiply !!!!
			result.FirstRound = true; 
			Score.Experience *= 2;
			Score.Money *= 2;
		}

		//check new rank after bonus
		if (!result.NewRank && GetPlayerRankFromExperience(PlayerData.Params.Experience) != GetPlayerRankFromExperience(PlayerData.Params.Experience + Score.Experience))
		{
			result.NewRank = true;
			AddMoney(GameplayRewards.MoneyRank , E_AddMoneyAction.Rank);
			Score.Money += GameplayRewards.MoneyRank; //add money twice, because later they ore once substracted
		}

		result.Experience = Score.Experience;
		result.Money = Score.Money;
		result.Gold	= Score.Gold;
		
		int gameType = (int)Game.GetMultiplayerGameType();
		PPIPlayerStats.GameData gameData = PlayerData.Stats.Games[gameType];
		gameData.Score += Score.Score;
		gameData.Money += Score.Money;
		gameData.Experience += Score.Experience;
		gameData.Golds += Score.Gold;
		gameData.Kills += Score.Kills;
		gameData.Deaths += Score.Deaths;
		gameData.Hits += Score.Hits;
		gameData.Shots += Score.Shots;
		gameData.Headshots += Score.HeadShots;
		gameData.PlayedTimes += Score.PlayedTimes;
		gameData.LastPlayedDate = Score.LastPlayedDate;
		gameData.TimeSpent += Score.TimeSpent;
		
		PlayerData.Stats.Games[gameType] = gameData;
		
		foreach (var key in Score.WeaponStats.Keys)
		{
			int index = PlayerData.InventoryList.Weapons.FindIndex(p => p.ID == key);
			
			if(index >= 0)
			{
				PPIWeaponData weaponData = PlayerData.InventoryList.Weapons[index];
				PPIRoundScore.WeaponStatistics weaponStats = Score.WeaponStats[key];
				
				MFDebugUtils.Assert(weaponData.IsValid());
				
				weaponData.StatsFire += weaponStats.StatsFire;
				weaponData.StatsHits += weaponStats.StatsHits;
				weaponData.StatsKills += weaponStats.StatsKills;
				
				PlayerData.InventoryList.Weapons[index] = weaponData;
			}
		}
		
		foreach (var key in Score.ItemStats.Keys)
		{
			int index = PlayerData.InventoryList.Items.FindIndex(p => p.ID == key);
				
			if(index >= 0)
			{
				PPIItemData itemData = PlayerData.InventoryList.Items[index];
				PPIRoundScore.ItemStatistics itemStats = Score.ItemStats[key];
				
				MFDebugUtils.Assert(itemData.IsValid());
				
				itemData.StatsKills += itemStats.StatsKills;
				itemData.StatsUseCount += itemStats.StatsUseCount;
				
				PlayerData.InventoryList.Items[index] = itemData;
			}
		}
		
		PlayerData.Stats.Today.Experience += Score.Experience;
		
		PlayerData.Params.Money += Score.Money; 
		PlayerData.Params.Experience += Score.Experience;
		PlayerData.Params.Gold 	+= Score.Gold;
		
		MarkLastFinishedGame(result.Winner);
		
		PlayerDataChanged = true;
		
		Server.Instance.SendEndRoundToClient(Player, result);
		//Debug.Log("End Round: " + Name + " exp"  + Score.Experience  + " money " + Score.Money + " experience new " + Experience + " experience old " + oldExperience); 
#endif
	}

	// treba dodelat i do hry
	public void MarkStartGame()
	{
		if (true == m_StatsRunning)
		{
			return;
		}

		m_StatsRunning = true;

		m_GameStartedAt = Time.realtimeSinceStartup;

		if (PlayerData.Stats.Today.Date != DateTime.UtcNow.Date)
		{
			PlayerData.Stats.Today = new PPIPlayerStats.TransientData() {Date = DateTime.UtcNow.Date};
		}

		Score.PlayedTimes++;
	}

	public void MarkEndGame()
	{
		if (false == m_StatsRunning)
		{
			return;
		}

		m_StatsRunning = false;

		TimeSpan timeSpan = CloudDateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		float span = (Time.realtimeSinceStartup - m_GameStartedAt)/60.0f; // in minutes

		Score.TimeSpent += span;
		Score.LastPlayedDate = timeSpan.TotalSeconds;
	}

	public void MarkLastFinishedGame(bool winner)
	{
		PlayerData.Stats.Today.GamesFinished += 1;
		PlayerData.Stats.Today.GamesWon += winner ? 1 : 0;

		int gameType = (int)Game.GetMultiplayerGameType();
		PPIPlayerStats.GameData gameData = PlayerData.Stats.Games[gameType];
		gameData.LastFinishedGameDate = GuiBaseUtils.DateToEpoch(CloudDateTime.UtcNow);
		PlayerData.Stats.Games[gameType] = gameData;
		PlayerDataChanged = true;
	}

	public void AddSuicide()
	{
		/*int gameType = (int)Game.GetMultiplayerGameType();
		PPIPlayerStats.GameData gameData = PlayerData.Stats.Games[gameType];
		gameData.Suicides++;
		PlayerData.Stats.Games[gameType] = gameData;
		PlayerDataChanged = true;
		
		Debug.Log("Suicide use " + PlayerData.Stats.Games[gameType].ToString());*/
	}

	public void AddDeath(float time, E_WeaponID currentWeapon)
	{
//		Debug.Log("Death use " + PlayerData.Stats.Games[(int)Game.GetMultiplayerGameType()].ToString());
	}

	public void AddItemUse(E_ItemID id)
	{
		if (PlayerData.InventoryList.Items.FindIndex(p => p.ID == id) < 0)
			return;

		PPIRoundScore.ItemStatistics itemStats;

		try
		{
			itemStats = Score.ItemStats[id];
			itemStats.StatsUseCount++;
		}
		catch (KeyNotFoundException)
		{
			itemStats = new PPIRoundScore.ItemStatistics();
			itemStats.StatsUseCount = 1;
		}

		Score.ItemStats[id] = itemStats;

		// Debug.Log("Item use " + PlayerData.InventoryList.Items[index].ToString());
	}

	public void AddItemKill(E_ItemID id)
	{
		if (PlayerData.InventoryList.Items.FindIndex(p => p.ID == id) < 0)
			return;

		PPIRoundScore.ItemStatistics itemStats;

		try
		{
			itemStats = Score.ItemStats[id];
			itemStats.StatsKills++;
		}
		catch (KeyNotFoundException)
		{
			itemStats = new PPIRoundScore.ItemStatistics();
			itemStats.StatsKills = 1;
		}

		Score.ItemStats[id] = itemStats;

		// Debug.Log("Item kill " + PlayerData.InventoryList.Items[index].ToString() +  " " + PlayerData.Stats.Games[gameType].ToString());
	}

	public void AddWeaponUse(E_WeaponID id)
	{
		int index = PlayerData.InventoryList.Weapons.FindIndex(p => p.ID == id);
		if (index < 0)
			return;

		PPIRoundScore.WeaponStatistics weaponStats;

		try
		{
			weaponStats = Score.WeaponStats[id];
			weaponStats.StatsFire++;
		}
		catch (KeyNotFoundException)
		{
			weaponStats = new PPIRoundScore.WeaponStatistics();
			weaponStats.StatsFire = 1;
		}

		Score.WeaponStats[id] = weaponStats;
		Score.Shots++;
	}

	public void AddWeaponHit(E_WeaponID id)
	{
		if (PlayerData.InventoryList.Weapons.FindIndex(p => p.ID == id) < 0)
			return;

		PPIRoundScore.WeaponStatistics weaponStats;

		try
		{
			weaponStats = Score.WeaponStats[id];
			weaponStats.StatsHits++;
		}
		catch (KeyNotFoundException)
		{
			weaponStats = new PPIRoundScore.WeaponStatistics();
			weaponStats.StatsHits = 1;
		}

		Score.WeaponStats[id] = weaponStats;
		Score.Hits++;

		// Debug.Log("Weapon hit " + PlayerData.InventoryList.Weapons[index].ToString() +  " " + PlayerData.Stats.Games[gameType].ToString());
	}

	public void AddWeaponKill(E_WeaponID id, E_BodyPart bodyPart, int num = 1)
	{
		if (uLink.Network.isServer == false)
			throw new uLink.NetworkException("AddWeaponKill: could be called only on server");

		int index = PlayerData.InventoryList.Weapons.FindIndex(p => p.ID == id);
		if (index < 0)
			return;

		PPIWeaponData weaponData = PlayerData.InventoryList.Weapons[index];

		//DebugUtils.Assert( weaponData.IsValid() );

		// nemusi byt validni napriklad pri pouziti itemu se spatne nastavenym ID itemu (takze se vola WeaponKill na zadanou zbran)
		if (weaponData.IsValid())
		{
			PPIRoundScore.WeaponStatistics weaponStats;

			try
			{
				weaponStats = Score.WeaponStats[id];
				weaponStats.StatsKills += num;
			}
			catch (KeyNotFoundException)
			{
				weaponStats = new PPIRoundScore.WeaponStatistics();
				weaponStats.StatsKills = num;
			}

			Score.WeaponStats[id] = weaponStats;
		}
		else
		{
			Debug.LogError("AddWeaponKill() with weapon '" + id + "' : weapon not found");
		}

		Score.HeadShots += bodyPart == E_BodyPart.Head ? 1 : 0;

		// Debug.Log("Weapon kill " + PlayerData.InventoryList.Weapons[index].ToString() +  " " + PlayerData.Stats.Games[(int)Game.GetMultiplayerGameType()].ToString());
	}

	public void ConsumableItemUsed(E_ItemID id)
	{
		int index = PlayerData.InventoryList.Items.FindIndex(p => p.ID == id);
		if (index < 0)
			return;

		PPIItemData itemData = PlayerData.InventoryList.Items[index];
		MFDebugUtils.Assert(itemData.IsValid());

		itemData.Count--;
		PlayerData.InventoryList.Items[index] = itemData;

		if (uLink.Network.isServer)
		{
			//SEND TO CLOUD !!!!
			ItemSettings settings = ItemSettingsManager.Instance.Get(id);

			CloudServices.GetInstance()
						 .ModifyItem(PrimaryKey,
									 PPIManager.ProductID,
									 settings.GUID,
									 "Count",
									 itemData.Count.ToString(),
									 CloudConfiguration.DedicatedServerPasswordHash);
		}
	}

	public void SynchronizePendingPPIChanges()
	{
		MFDebugUtils.Assert(IsValid);

		if (PlayerDataChanged)
		{
			string PPIAsJSON = GetPlayerDataAsJsonStr();
			/*CloudServices.AsyncOpResult	res;
			
			res =*/
			CloudServices.GetInstance()
						 .UserSetPerProductData(PrimaryKey,
												PPIManager.ProductID,
												CloudServices.PROP_ID_PLAYER_DATA,
												PPIAsJSON,
												CloudConfiguration.DedicatedServerPasswordHash,
												PPISyncToCloudFinished);

			PlayerDataChanged = false;
		}
	}

	void PPISyncToCloudFinished(CloudServices.AsyncOpResult res)
	{
		if (res.m_Res && res.m_ResultDesc == CloudServices.RESP_OK)
		{
//			Debug.Log("Synced PPI to cloud for user " + UserName);

			// we don't want to fetch local ppi here
			// it will be done later after round results display
			//PPIManager.Instance.ClientNotifyPPIChange(Player);
		}
		else
		{
			Debug.LogError("Error synchronizing PPI to cloud : " + Name + "(" + res.m_ResultDesc + ")");
		}
	}

	public void CopyPlayerData(PlayerPersistantInfo otherPPI)
	{
		PlayerData = otherPPI.PlayerData;
	}

	public void CopyFrom(PlayerPersistantInfo otherPPI)
	{
		Team = otherPPI.Team;
		ZoneIndex = otherPPI.ZoneIndex;
		//	Score = otherPPI.Score;

		PlayerData = otherPPI.PlayerData;
	}

	public void FetchPPIDataFromCloudAsyncOpFinished(CloudServices.AsyncOpResult res)
	{
		if (res.m_Res)
		{
			PlayerPersistantInfo PPIFromCloud = new PlayerPersistantInfo();

			if (PPIFromCloud.InitPlayerDataFromStr(res.m_ResultDesc))
			{
//				Debug.Log("Server: got user data from cloud for : " + UserName);

				CopyPlayerData(PPIFromCloud);
				IsValid = true;
				EnforceDataValidity();

				//Debug.Log("Updated Score: " + Name + " exp"  + Score.Experience  + " money " + Score.Money + " experience old " + Experience); 

				return;
			}
		}

		PlayerData.Params.Experience = 0;
		PlayerData.Params.Money = 0;
		PlayerData.Params.Gold = 0;

		Debug.LogWarning("FetchPPIDataFromCloudAsyncOpFinished(): error getting PPI from cloud for user : " + PrimaryKey + "(" + Name + ")");
	}

	void EnforceDataValidity()
	{
		const int MAX_EXPERIENCE = 100000000;
		const int MAX_MONEY = 100000000;
		const int MAX_GOLD = 100000000;

		bool dataCorrected = false;

		MFDebugUtils.Assert(IsValid);

		if (PlayerData.Params.Experience < 0 || PlayerData.Params.Experience > MAX_EXPERIENCE)
		{
			PlayerData.Params.Experience = 0;

			dataCorrected = true;
		}

		if (PlayerData.Params.Money < 0 || PlayerData.Params.Money > MAX_MONEY)
		{
			PlayerData.Params.Money = 10000;

			dataCorrected = true;
		}

		if (PlayerData.Params.Gold < 0 || PlayerData.Params.Gold > MAX_GOLD)
		{
			PlayerData.Params.Gold = 1000;

			dataCorrected = true;
		}

		// Cloud does not test/filter the weapon list and weapon slots
		// The main purpose of this check is to disallow cheaters to use the premium slot. We ignore
		// the check for the extra weapon slot because it is super-cheap and everyone can easily buy it.
		int validWeaponSlots = IsPremiumAccountActive ? 3 : 2;

		if (PlayerData.EquipList.Weapons.Count > validWeaponSlots)
		{
			PlayerData.EquipList.Weapons.RemoveAll(e => e.EquipSlotIdx>=validWeaponSlots);
			dataCorrected = true;
		}

		// Cloud does not test/filter the weapon list and weapon slots
		// The main purpose of this check is to disallow cheaters to use the premium slot. We ignore
		// the check for the extra item slot because it is super-cheap and everyone can easily buy it.
		int validItemSlots = IsPremiumAccountActive ? 3 : 2;

		if (PlayerData.EquipList.Items.Count > validItemSlots)
		{
			PlayerData.EquipList.Items.RemoveAll(e => e.EquipSlotIdx>=validItemSlots);
			dataCorrected = true;
		}

		if (dataCorrected)
		{
			Debug.LogWarning("Invalid PPI data detected for user " + PrimaryKey + "(" + Name + ")");
		}
	}

	public readonly static int MAX_RANK = 50;
	readonly static int[] MajorRanks = new int[] {10, 20, 30, 40, 50};
	readonly static int[] Ranks =
	{
		0, 1000, 5000, 11000, 18000, 26000, 35000, 45000, 56000, 68000, 82000, 98000, 116000, 136000, 158000, 182000, 208000, 236000, 266000,
		298000, 333000, 371000, 412000, 456000, 503000, 553000, 606000, 662000, 721000, 783000, 849000, 919000, 993000, 1071000, 1153000, 1239000,
		1329000, 1423000, 1521000, 1623000,
		1730000, 1842000, 1959000, 2081000, 2208000, 2340000, 2477000, 2619000, 2766000, 2918000
	};

	public static int GetPlayerMinExperienceForRank(int rank) //1.... MAX_RANK
	{
		if (rank < 1 || rank > MAX_RANK)
			throw new System.ArgumentOutOfRangeException("Rank should be between 1 and " + MAX_RANK + " - variable is " + rank);

		return Ranks[rank - 1];
	}

	public static int GetPlayerRankFromExperience(int experience)
	{
		int i = 0;
		while (i < Ranks.Length && Ranks[i] <= experience)
			i++;

		return i;
	}

	public static bool IsMajorRank(int rank)
	{
		return Array.IndexOf(MajorRanks, rank) != -1;
	}

	public bool IsFirstGameToday(E_MPGameType gameType)
	{
		DateTime date = GuiBaseUtils.EpochToDate(PlayerData.Stats.GetGameData(gameType).LastFinishedGameDate);
		return (CloudDateTime.UtcNow.Date - date.Date).TotalDays > 0 ? true : false;
	}

	public DateTime GetPremiumAccountEndDateTime()
	{
		return new DateTime(new DateTime(1970, 1, 1).Ticks + PremiumAccountEndTimeMs*System.TimeSpan.TicksPerMillisecond);
	}

	// temporary initial settings only for testing 
	public static PlayerPersistantInfo GetDefaultPPI()
	{
		return GetFinalDefaultPPI();
	}

	public static PlayerPersistantInfo GetFinalDefaultPPI()
	{
		PlayerPersistantInfo ppi = new PlayerPersistantInfo();
		ppi.Name = "PlayerName";

		foreach (ItemSettings item in ItemSettingsManager.Instance.GetAll())
		{
			if (item.IsDefault() || item.PremiumOnly)
				ppi.InventoryList.Items.Add(new PPIItemData() {ID = item.ID});
		}

		ppi.InventoryList.Items.Add(new PPIItemData() {ID = E_ItemID.BoosterAccuracy});
		ppi.InventoryList.Items.Add(new PPIItemData() {ID = E_ItemID.BoosterArmor});
		ppi.InventoryList.Items.Add(new PPIItemData() {ID = E_ItemID.BoosterDamage});
		ppi.InventoryList.Items.Add(new PPIItemData() {ID = E_ItemID.BoosterInvicible});
		ppi.InventoryList.Items.Add(new PPIItemData() {ID = E_ItemID.BoosterSpeed});
		ppi.InventoryList.Items.Add(new PPIItemData() {ID = E_ItemID.SentryGunRail});
		ppi.InventoryList.Items.Add(new PPIItemData() {ID = E_ItemID.SentryGunRockets});

		foreach (WeaponSettings item in WeaponSettingsManager.Instance.GetAll())
		{
			if (item.IsDefault() || item.PremiumOnly)
				ppi.InventoryList.Weapons.Add(new PPIWeaponData() {ID = item.ID});
		}

		foreach (PerkSettings item in PerkSettingsManager.Instance.GetAll())
		{
			if (item.IsDefault() || item.PremiumOnly)
				ppi.InventoryList.Perks.Add(new PPIPerkData() {ID = item.ID});
		}

		foreach (SkinSettings item in SkinSettingsManager.Instance.GetAll())
		{
			if (item.IsDefault() || item.PremiumOnly)
				ppi.InventoryList.Skins.Add(new PPISkinData() {ID = item.ID});
		}

		foreach (HatSettings item in HatSettingsManager.Instance.GetAll())
		{
			if (item.IsDefault() || item.PremiumOnly)
				ppi.InventoryList.Hats.Add(new PPIHatData() {ID = item.ID});
		}

		foreach (UpgradeSettings item in UpgradeSettingsManager.Instance.GetAll())
		{
			if (item.IsDefault() || item.PremiumOnly)
				ppi.Upgrades.Upgrades.Add(new PPIUpgradeList.UpgradeData() {ID = item.ID});
		}

		//default equip
		if (ppi.InventoryList.Weapons.Count > 0)
			ppi.EquipList.Weapons.Add(ppi.InventoryList.Weapons[0]);

		if (ppi.InventoryList.Items.Count > 0)
			ppi.EquipList.Items.Add(ppi.InventoryList.Items[0]);

		if (ppi.InventoryList.Perks.Count > 0)
			ppi.EquipList.Perk = ppi.InventoryList.Perks[0].ID;

		ppi.EquipList.Outfits.Skin = E_SkinID.Skin01_Soldier;

		return ppi;
	}
}
