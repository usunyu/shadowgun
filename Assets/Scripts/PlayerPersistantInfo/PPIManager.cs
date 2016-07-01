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
using BitStream = uLink.BitStream;
using NetworkPlayer = uLink.NetworkPlayer;
using Queue = System.Collections.Queue;

public static class GameplayRewards
{
	public static short Kill = 100;
	public static short Suicide = -100;
	public static short HeadShot = 50;
	public static short Ammo = 10;
	public static short Heal = 10;
	public static short TurretKill = 25;
	public static short SpiderKill = 25;
	public static short MineKill = 15;

	public static class ZC
	{
		public static short FlagNeutral = 100;
		public static short FlagOwned = 100;
		public static short FlagDefend = 50;

		public static short Win = 1000;
		public static short Lost = 200;
	}

	public static class DM
	{
		public static short First = 400;
		public static short Second = 200;
		public static short Third = 100;
	}

	public static short MoneyRank = 5000;

	public static float MoneyModificator = 0.5f;
	public static float PremiumAccountModificator = 1.5f;
}

public static class MPMessages
{
	public static int TeamBad = 0400000;
	public static int TeamGood = 0400001;

	public static int Connect = 0500000;
	public static int Disconnect = 0500001;
	public static int Kicked = 0500002;
	public static int Win = 0500010;
	public static int Lost = 0500011;
	public static int NewRound = 0500012;

	public static int ZoneNeutralized = 0500300;
	public static int ZoneControlledBy = 0500301;

	public static int By = 0500103;
	public static int Kill = 0500100;
	public static int Killed = 0500101;
	public static int Suicide = 0500102;

	public static int XpKill = 0500200;
	public static int XpKillAssist = 0500201;
	public static int XPZoneDefend = 0500202;
	public static int XPZoneNeutral = 0500203;
	public static int XPZoneOwned = 0500204;

	public static int XPAmmo = 0500205;
	public static int XPHealing = 0500206;

	public static int XPTurretKill = 0500207;
	public static int XPSpiderKill = 0500208;
}

[AddComponentMenu("Multiplayer/PPI Manager")]
public class PPIManager : uLink.MonoBehaviour
{
	public static PPIManager Instance;
	public static string ProductID = Game.PrimaryProductID;

	uLink.NetworkView NetworkView;
	List<PlayerPersistantInfo> PPIs = new List<PlayerPersistantInfo>();
	PlayerPersistantInfo LocalPPI = new PlayerPersistantInfo();

	public delegate void PlayerPersistantInfoEventHandler(PlayerPersistantInfo ppi);
	static PlayerPersistantInfoEventHandler m_LocalPlayerInfoChanged;

	public static event PlayerPersistantInfoEventHandler localPlayerInfoChanged
	{
		add
		{
			if (value != null)
			{
				m_LocalPlayerInfoChanged -= value; // just to be sure we don't have any doubles
				m_LocalPlayerInfoChanged += value;
				if (Instance != null && Instance.LocalPPI != null)
				{
					value(Instance.LocalPPI); // call delegate when registering so it recieves current state
				}
			}
		}
		remove { m_LocalPlayerInfoChanged -= value; }
	}

	//private bool                        m_GeneratePPIChangeNotifications = false;
	//private string                      m_RefPPIForNotificationsJSON;

	public int GetNumberOfPlayers()
	{
		return PPIs.Count;
	}

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			DestroyImmediate(this);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(this);

		// Load Authentication if exist;
		//CloudUser.LoadAuthenticationData();

		//string name = string.IsNullOrEmpty(CloudUser.nickName) ? PlayerName : CloudUser.nickName;
		//Debug_SetDefaultData(name);  // debug shit        
	}

	void Start()
	{
		//NetworkView = gameObject.AddComponent<uLink.NetworkView>();
		NetworkView = networkView;
	}

	public bool IsLocalPPI(PlayerPersistantInfo ppi)
	{
		return ppi.Player == GetLocalPPI().Player;
	}

	public PlayerPersistantInfo GetLocalPPI()
	{
		if (uLink.Network.status == uLink.NetworkStatus.Connected && uLink.Network.isClient == false)
			throw new uLink.NetworkException("PPIManager::GetLocalPPI : could be called only on client");

		return LocalPPI;
	}

	public void NotifyLocalPPIChanged()
	{
		if (uLink.Network.status == uLink.NetworkStatus.Connected && uLink.Network.isClient == false)
			throw new uLink.NetworkException("PPIManager::NotifyLocalPPIChanged : could be called only on client");

		OnLocalPlayerInfoChanged();
	}

	public PlayerPersistantInfo GetLocalPlayerPPI()
	{
		//	if (uLink.Network.isClient == false)
		//		throw new uLink.NetworkException("PPIManager::GetLocalPlayerPPI : could be called only on client");

		return PPIs.Find(ps => ps.Player == uLink.Network.player);
	}

	public PlayerPersistantInfo GetPPI(uLink.NetworkPlayer player)
	{
		return PPIs.Find(ps => ps.Player == player);
	}

	public List<PlayerPersistantInfo> GetPPIList()
	{
		return PPIs;
	}

	public void ServerSwitchTeams()
	{
#if !DEADZONE_CLIENT
        if (uLink.Network.isServer == false)
            throw new uLink.NetworkException("PPIManager::ServerSwitchTeams : could be called only on server");

        foreach (PlayerPersistantInfo ppi in PPIs)
        {
            ppi.Team = ppi.Team == E_Team.Bad ? E_Team.Good : E_Team.Bad;
            NetworkView.RPC("ClientUpdateTeam", uLink.RPCMode.Others, ppi.Player, ppi.Team);
        }
#endif
	}

	public void ServerRebalanceTeams()
	{
#if !DEADZONE_CLIENT
		if (Server.Instance.GameInfo.GameType == E_MPGameType.ZoneControl)
		{
			if (uLink.Network.isServer == false)
			{
				throw new uLink.NetworkException("PPIManager::ServerSwitchTeams : could be called only on server");
			}
			
			List<PlayerPersistantInfo> good = new List<PlayerPersistantInfo>();
			List<PlayerPersistantInfo> bad = new List<PlayerPersistantInfo>();
			
			foreach ( PlayerPersistantInfo ppi in PPIs )
			{
				if( ppi.Team == E_Team.Bad )
					bad.Add( ppi );
				if( ppi.Team == E_Team.Good )
					good.Add( ppi );
			}
			
			int toSwitch = Math.Abs( bad.Count - good.Count) / 2;
			
			if( toSwitch > 0 )
			{
				List<PlayerPersistantInfo> listFrom  = ( bad.Count > good.Count ) ? bad : good;
				
				for( int i = 0; i < toSwitch; i++ )
				{
					if( listFrom.Count <= 0 )
					{
						break;
					}
					
					int index = UnityEngine.Random.Range( 0, listFrom.Count );
					
					listFrom[index].Team = ( bad.Count < good.Count ) ? E_Team.Bad : E_Team.Good;
					
					NetworkView.RPC("ClientUpdateTeam", uLink.RPCMode.Others, listFrom[index].Player, listFrom[index].Team);
					
					listFrom.RemoveAt( index );
				}
			}
		}
#endif
	}

	public void ServerSendAllPPItoPlayer(uLink.NetworkPlayer player)
	{
#if !DEADZONE_CLIENT
        foreach (PlayerPersistantInfo ppi in PPIs)
        {
            NetworkView.RPC("ClientAddPPI", player, ppi);
        }
#endif
	}

	public void ServerAddPPI(PlayerPersistantInfo ppi)
	{
#if !DEADZONE_CLIENT
        if (uLink.Network.isServer == false)
            throw new uLink.NetworkException("PPIManager::AddPPI : could be called only on server");

        if(PPIs.Find(ps => ps.Player == ppi.Player) != null)
            throw new uLink.NetworkException("PPIManager::AddPPI ppi is already in the list");

		FixNickname(ppi);
		
        PPIs.Add(ppi);

        Server.Printf("ServerAddPPI - " +  ppi.Name + " " + ppi.Player);

        NetworkView.RPC("ClientAddPPI", uLink.RPCMode.Others, ppi);
#endif
	}

	public void ServerUpdatePPI(PlayerPersistantInfo ppi)
	{
#if !DEADZONE_CLIENT
        Server.Printf("Server update ppi " + ppi.Name + " " + ppi.Team);

        if (uLink.Network.isServer == false)
            throw new uLink.NetworkException("PPIManager::AddPPI : could be called only on server");

        int index = PPIs.FindIndex(ps => ps.Player == ppi.Player);

        if (index == -1)
            throw new uLink.NetworkException("PPIManager::AddPPI ppi is NOT in the list");

        FixNickname(ppi);
        
        PPIs[index].UpdateFromPPIForRespawn(ppi);
					
        NetworkView.RPC("ClientUpdatePPI", uLink.RPCMode.Others, ppi);
#endif
	}

	public void ServerRemovePPI(uLink.NetworkPlayer player)
	{
#if !DEADZONE_CLIENT
        if (uLink.Network.isServer == false)
            throw new uLink.NetworkException("PPIManager::AddPPI : could be called only on server");

        int index = PPIs.FindIndex(ps => ps.Player == player);

        if (index == -1)
            throw new uLink.NetworkException("PPIManager::AddPPI ppi is NOT in the list");

        Server.Printf("Server remove ppi " + PPIs[index].Name + " " + PPIs[index].Team);
        PPIs.RemoveAt(index);
		
        NetworkView.RPC( "ClientRemovePPI", uLink.RPCMode.Others, player );
#endif
	}

	[uSuite.RPC]
	void ClientUpdateTeam(uLink.NetworkPlayer player, E_Team team)
	{
		if (uLink.Network.isClient == false)
			throw new uLink.NetworkException("PPIManager::ClientUpdateTeam: could be called only on client");

		int index = PPIs.FindIndex(ps => ps.Player == player);

		if (index == -1)
			throw new uLink.NetworkException("PPIManager::ClientUpdateTeam ppi is NOT in the list");

		PPIs[index].Team = team;

		UpdateTeamLocalPPI(PPIs[index]);
	}

	[uSuite.RPC]
	public void ClientAddPPI(PlayerPersistantInfo ppi)
	{
		Client.Printf("Client add ppi " + ppi.Name + " " + ppi.Team + " " + ppi.Player);

		if (uLink.Network.isClient == false)
			throw new uLink.NetworkException("PPIManager::AddPPI : could be called only on client");

		if (PPIs.Find(ps => ps.Player == ppi.Player) != null)
			throw new uLink.NetworkException("PPIManager::AddPPI ppi is already in the list");

		FixNickname(ppi);

		PPIs.Add(ppi);

		UpdateTeamLocalPPI(ppi);

		Client.Instance.ConsoleMsg(ppi.NameForGui + " " + TextDatabase.instance[0500000]);
	}

	[uSuite.RPC]
	public void ClientUpdatePPI(PlayerPersistantInfo ppi)
	{
		Client.Printf("Client update ppi " + ppi.Name + " " + ppi.Team + " player " + ppi.Player);

		if (uLink.Network.isClient == false)
			throw new uLink.NetworkException("PPIManager::AddPPI : could be called only on client");

		int index = PPIs.FindIndex(ps => ps.Player == ppi.Player);

		if (index == -1)
			throw new uLink.NetworkException("PPIManager::AddPPI ppi is NOT in the list");

		FixNickname(ppi);

		PPIs[index].CopyFrom(ppi);

		UpdateTeamLocalPPI(ppi);
	}

	[uSuite.RPC]
	public void ClientRemovePPI(uLink.NetworkPlayer player)
	{
		if (uLink.Network.isClient == false)
			throw new uLink.NetworkException("PPIManager::AddPPI : could be called only on server");

		int index = PPIs.FindIndex(ps => ps.Player == player);

		if (index == -1)
			throw new uLink.NetworkException("PPIManager::AddPPI ppi is NOT in the list");

		Client.Printf("client remove ppi " + PPIs[index].Name + " " + PPIs[index].Team);

		Client.Instance.ConsoleMsg(PPIs[index].NameForGui + " " + TextDatabase.instance[0500001]);

		PPIs.RemoveAt(index);
	}

	void UpdateTeamLocalPPI(PlayerPersistantInfo ppi)
	{
		if (ppi.Player == LocalPPI.Player)
		{
			LocalPPI.Team = ppi.Team;
			LocalPPI.State = ppi.State;
		}
	}

	public void ServerEndRound()
	{
#if !DEADZONE_CLIENT
		LeaderboardsUpdateScore();
			
		ServerSetState(PlayerPersistantInfo.E_State.Connected, true);
		
        foreach(PlayerPersistantInfo ppi in PPIs)
		{
			if(ppi.IsValid)
			{
				ppi.SynchronizePendingPPIChanges();
				//SynchronizeScore(ppi); ??
			}
			else 
			{
				Debug.LogWarning("Server End Round - ppi is not valid " + ppi.Name);
			}
		}
#endif
	}

	public void LeaderboardsUpdateScore()
	{
		List<CloudServices.S_LeaderBoardScoreInfo> scores = new List<CloudServices.S_LeaderBoardScoreInfo>();
		List<CloudServices.S_LeaderBoardScoreInfo> daily = new List<CloudServices.S_LeaderBoardScoreInfo>();

		foreach (PlayerPersistantInfo ppi in PPIs)
		{
			if (ppi.IsValid)
			{
				scores.Add(new CloudServices.S_LeaderBoardScoreInfo(ppi.Experience, ppi.PrimaryKey));
				daily.Add(new CloudServices.S_LeaderBoardScoreInfo(ppi.Score.Score, ppi.PrimaryKey));
			}
		}

		CloudServices.GetInstance()
					 .LeaderboardSetScores(PPIManager.ProductID, "Default", CloudConfiguration.DedicatedServerPasswordHash, scores.ToArray());
	}

	public void ServerPrepareForNewRound()
	{
#if !DEADZONE_CLIENT
        foreach (PlayerPersistantInfo ppi in PPIs)
            ppi.Score.Reset();

		
        NetworkView.RPC("ClientPrepareForNewRound", uLink.RPCMode.Others);
#endif
	}

	public void ServerStartNewRound()
	{
#if !DEADZONE_CLIENT
#endif
	}

	[uSuite.RPC]
	public void ClientPrepareForNewRound()
	{
		foreach (PlayerPersistantInfo ppi in PPIs)
			ppi.Score.Reset();
	}

#if false
				// we don't want to let server to fetch local ppi
				// it will be done later after round results display
	public void ClientNotifyPPIChange(uLink.NetworkPlayer player)
	{
		if (uLink.Network.isServer == false)
            throw new uLink.NetworkException("PPIManager::ClientNotifyPPIChange : could be called only on server");
		
		if (player.isConnected == true)
		{
			NetworkView.RPC("FetchPPIFromCloud", player);
		}
	}
#endif

#if false
				// we don't want to let server to fetch local ppi
				// it will be done later after round results display
	[uSuite.RPC]
	public void FetchPPIFromCloud()
	{
        if (uLink.Network.isServer == true)
			throw new uLink.NetworkException("PPIManager::FetchPPIFromCloud : could be called only on client");
		
		
		/*if (LocalPPI != null)
		{
			m_GeneratePPIChangeNotifications = true;			
			m_RefPPIForNotificationsJSON = LocalPPI.GetPlayerDataAsJsonStr();
		}*/
		
		//Debug.Log(Time.timeSinceLevelLoad + " FetchPPI "); 
		
		GetCloudPPI();
	}
#endif

	public void ServerAddScoreForSpiderKill(uLink.NetworkPlayer player)
	{
#if !DEADZONE_CLIENT
        PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(player);

        ppi.AddScore(GameplayRewards.SpiderKill);
        ppi.AddExperience(GameplayRewards.SpiderKill, E_AddMoneyAction.KillSpider);

        Server.Instance.ShowCombatMsgOnClient(ppi.Player,  Client.E_MessageType.Spider,  GameplayRewards.SpiderKill,  (short)(GameplayRewards.SpiderKill * GameplayRewards.MoneyModificator) );

        SortDescending();
        SynchronizeScore(ppi);
#endif
	}

	public void ServerAddScoreForTurretKill(uLink.NetworkPlayer player, int extraGold)
	{
#if !DEADZONE_CLIENT
        PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(player);

        ppi.AddScore(GameplayRewards.TurretKill);
        ppi.AddExperience(GameplayRewards.TurretKill, E_AddMoneyAction.KillSentry);
		ppi.AddGoldFromGameplay(extraGold);
		
        Server.Instance.ShowCombatMsgOnClient(ppi.Player,  Client.E_MessageType.Turret,  GameplayRewards.TurretKill,   (short)(GameplayRewards.TurretKill * GameplayRewards.MoneyModificator) );
		
		if(extraGold > 0)
		{
			Server.Instance.ShowGoldMsgOnClient(player, (short)extraGold);
		}

        SortDescending();
        SynchronizeScore(ppi);
#endif
	}

	public void ServerAddScoreForMineKill(uLink.NetworkPlayer player)
	{
#if !DEADZONE_CLIENT
		PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(player);
		
		ppi.AddScore(GameplayRewards.MineKill);

		//TODO I added the E_AddMoneyAction.KillMine enumerator but I'm not sure if it is supported on the Kontangent side. On the other side -
		//the E_AddMoneyAction do not interact with clients at all - thus such modification should be safe.
		ppi.AddExperience(GameplayRewards.MineKill, E_AddMoneyAction.KillMine);

		//FIXME The message type should be E_MessageType.Mine but there is no such message on the current clients at the moment
		//(and it is unclear if we are ever going to update clients) - thus we have to use what we already have supported on the client side.
		Server.Instance.ShowCombatMsgOnClient(ppi.Player, Client.E_MessageType.Turret, GameplayRewards.MineKill, (short)(GameplayRewards.MineKill * GameplayRewards.MoneyModificator) );
		
		SortDescending();
		SynchronizeScore(ppi);
#endif
	}

	public void ServerAddScoreForAmmoRecharge(uLink.NetworkPlayer player)
	{
#if !DEADZONE_CLIENT
        PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(player);

        ppi.AddScore(GameplayRewards.Ammo);

        ppi.AddExperience(GameplayRewards.Ammo, E_AddMoneyAction.AmmoKit);

        Server.Instance.ShowCombatMsgOnClient(ppi.Player,  Client.E_MessageType.Ammo,  GameplayRewards.Ammo,   (short)(GameplayRewards.Ammo * GameplayRewards.MoneyModificator) );

        SortDescending();
        SynchronizeScore(ppi);
#endif
	}

	// Fixed cloud PPI sync
	public void ServerAddScoreForHealing(uLink.NetworkPlayer player)
	{
#if !DEADZONE_CLIENT
        PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(player);

        ppi.AddScore(GameplayRewards.Heal);

        ppi.AddExperience(GameplayRewards.Heal, E_AddMoneyAction.MedKit);

        Server.Instance.ShowCombatMsgOnClient(ppi.Player,  Client.E_MessageType.Heal,  GameplayRewards.Heal,   (short)(GameplayRewards.Heal * GameplayRewards.MoneyModificator) );

        SortDescending();
        SynchronizeScore(ppi);
#endif
	}

	// Fixed cloud PPI sync
	public void ServerAddScoreForZoneControl(ZoneControlFlag.E_ZoneControlEvent zoneEvent, uLink.NetworkPlayer player)
	{
#if !DEADZONE_CLIENT
        PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(player);
        switch (zoneEvent)
        {
            case ZoneControlFlag.E_ZoneControlEvent.Attacked:
                ppi.AddScore(GameplayRewards.ZC.FlagDefend);
				ppi.AddExperience(GameplayRewards.ZC.FlagDefend, E_AddMoneyAction.ZoneControl);
                Server.Instance.ShowCombatMsgOnClient(ppi.Player,  Client.E_MessageType.ZoneAttacked,  GameplayRewards.ZC.FlagDefend, (short)(GameplayRewards.ZC.FlagDefend  * GameplayRewards.MoneyModificator) );
                break;
            case ZoneControlFlag.E_ZoneControlEvent.Defend:
                ppi.AddScore(GameplayRewards.ZC.FlagDefend);
				ppi.AddExperience(GameplayRewards.ZC.FlagDefend, E_AddMoneyAction.ZoneControl);
                Server.Instance.ShowCombatMsgOnClient(ppi.Player,  Client.E_MessageType.ZoneDefended,  GameplayRewards.ZC.FlagDefend, (short)(GameplayRewards.ZC.FlagDefend  * GameplayRewards.MoneyModificator) );
                break;
            case ZoneControlFlag.E_ZoneControlEvent.FlagNeutral:
                ppi.AddScore(GameplayRewards.ZC.FlagNeutral);
				ppi.AddExperience(GameplayRewards.ZC.FlagNeutral, E_AddMoneyAction.ZoneControl);
                Server.Instance.ShowCombatMsgOnClient(ppi.Player,  Client.E_MessageType.ZoneNeutral,  GameplayRewards.ZC.FlagNeutral,  (short)( GameplayRewards.ZC.FlagNeutral * GameplayRewards.MoneyModificator) );
                break;
            case ZoneControlFlag.E_ZoneControlEvent.Owned:
                ppi.AddScore(GameplayRewards.ZC.FlagOwned);
                ppi.AddExperience(GameplayRewards.ZC.FlagOwned, E_AddMoneyAction.ZoneControl);
                Server.Instance.ShowCombatMsgOnClient(ppi.Player,  Client.E_MessageType.ZoneControl,  GameplayRewards.ZC.FlagOwned,   (short)(GameplayRewards.ZC.FlagOwned * GameplayRewards.MoneyModificator) );
                break;
        }
        SortDescending();
        SynchronizeScore(ppi);
#endif
	}

	public void ServerAddScoreForKill(uLink.NetworkPlayer victim,
									  uLink.NetworkPlayer killer,
									  List<BlackBoard.DamageData> damageData,
									  E_BodyPart bodyPart,
									  int extraGold)
	{
#if !DEADZONE_CLIENT
        PlayerPersistantInfo victimPPI = PPIManager.Instance.GetPPI(victim);
        PlayerPersistantInfo killerPPI = PPIManager.Instance.GetPPI(killer);
		
        if (killer != victim)
        {   
			victimPPI.Score.Deaths++; // check this!!!!
			SynchronizeScore(victimPPI);

		    killerPPI.Score.Kills++; // check this!!!!
			killerPPI.AddGoldFromGameplay(extraGold);
			
		    Server.Instance.ShowMessageOnClients(killerPPI.NameForGui + " " + TextDatabase.instance[MPMessages.Kill] + " " + victimPPI.NameForGui);
			
			DistributeKills(victim, killer, bodyPart);
			
			if(extraGold > 0)
			{
				Server.Instance.ShowGoldMsgOnClient(killerPPI.Player, (short)extraGold);
			}
        }
        else
        {
			killerPPI.Score.Deaths++; // check this!!!!
            killerPPI.AddScore(GameplayRewards.Suicide);
            Server.Instance.ShowMessageOnClients(killerPPI.NameForGui + " " + TextDatabase.instance[MPMessages.Suicide]);
            SynchronizeScore(killerPPI);
        }
		
		ServerSetState(victim, PlayerPersistantInfo.E_State.Connected, true);
		
        SortDescending();
#endif
	}

#if !DEADZONE_CLIENT
	void DistributeKills(uLink.NetworkPlayer victim, uLink.NetworkPlayer killer, E_BodyPart bodyPart)
	{
		ComponentPlayer p =  Player.GetPlayer(victim);
		
		float left = p.Owner.BlackBoard.RealMaxHealth;
		
		Dictionary<uLink.NetworkPlayer, float> killers = new Dictionary<uLink.NetworkPlayer, float>();
		
		foreach(BlackBoard.DamageData data in p.Owner.BlackBoard.AttackersDamageData)
		{
			float damage = Mathf.Min(left, data.Damage);
			
			if(Player.GetPlayer(data.Attacker) == null) //player is fadeout , fuck him.. or it cause crash in hud on client
				continue;
			
			if(killers.ContainsKey(data.Attacker))
				killers[data.Attacker] += damage;
			else
				killers.Add(data.Attacker, damage);
			
			left -= damage;
			
			if(left <= 0)
				break;
		}
		
		
		//PlayerPersistantInfo victimPPI  = PPIManager.Instance.GetPPI(victim);
		
		
		foreach(KeyValuePair<uLink.NetworkPlayer, float> pair in killers)
		{
			PlayerPersistantInfo killerPPI = PPIManager.Instance.GetPPI(pair.Key);
			
			if(killerPPI == null)
				continue; // one of the killers could disconnect
			
			float percent = pair.Value / p.Owner.BlackBoard.RealMaxHealth;
			
			if(pair.Key == killer)
				percent = 1.0f;
			else if(percent < 0.1f)
				percent = 0.1f;
			else if(percent > 0.6f)
				percent = 0.6f;
			
			
			
			int score = Mathf.FloorToInt(GameplayRewards.Kill * percent);
			
			
			if(killer == pair.Key)
			{
				Server.Instance.ShowCombatMsgOnClient(killerPPI.Player,  Client.E_MessageType.Kill, (short) score, (short)(score * GameplayRewards.MoneyModificator) );

				if (bodyPart == E_BodyPart.Head)
				{
					Server.Instance.ShowCombatMsgOnClient(killerPPI.Player, Client.E_MessageType.HeadShot, GameplayRewards.HeadShot, (short)(GameplayRewards.HeadShot * GameplayRewards.MoneyModificator));
					score += GameplayRewards.HeadShot;
				}
			}
			else
				Server.Instance.ShowCombatMsgOnClient(killerPPI.Player,  Client.E_MessageType.KillAssist, (short) score,   (short)(score * GameplayRewards.MoneyModificator) );

			
			killerPPI.AddScore(score);
			
			if (Server.Instance.GameInfo.GameType ==  E_MPGameType.DeathMatch)
				killerPPI.AddExperience(score, E_AddMoneyAction.KillDM);	
			else
				killerPPI.AddExperience(score, E_AddMoneyAction.KillZC);
			
			SynchronizeScore(killerPPI);
		}
	}
#endif

	void ClientOnMoneyChange(PlayerPersistantInfo ppi)
	{
		if (IsLocalPPI(ppi))
			Client.Instance.PlaySoundMoney();
	}

	void uLink_OnConnectedToServer()
	{
		LocalPPI.Player = uLink.Network.player;
	}

	//FIX ME
	void uLink_OnDisconnectedFromServer(uLink.NetworkDisconnection mode)
	{
		// remove all ppis, except local player !!!!

		PPIs.RemoveAll(ps => ps.Player != uLink.Network.player);
	}

	void SortDescending()
	{
		PPIs.Sort((ps1, ps2) => ps2.Score.Score.CompareTo(ps1.Score.Score));
	}

	public int GetLocalPlayerPlaceInScore()
	{
		PPIs.Sort((ps1, ps2) => ps2.Score.Score.CompareTo(ps1.Score.Score));

		return PPIs.FindIndex(ps => ps.Player == uLink.Network.player);
	}

	public void ServerSetState(PlayerPersistantInfo.E_State state, bool Synchronize)
	{
#if !DEADZONE_CLIENT
        foreach (PlayerPersistantInfo ppi in PPIs)
                ppi.State = state;

        NetworkView.RPC("SynchronizeStateAll", uLink.RPCMode.Others, state);
#endif
	}

	public void ServerSetState(uLink.NetworkPlayer player, PlayerPersistantInfo.E_State state, bool Synchronize)
	{
#if !DEADZONE_CLIENT
        PlayerPersistantInfo ppi = GetPPI(player);
        ppi.State = state;

        Server.Printf("PPI " + ppi.Name + " " + state);
        NetworkView.RPC("SynchronizeStatePlayer", uLink.RPCMode.Others, player, state);
#endif
	}

	[uSuite.RPC]
	void SynchronizeStateAll(PlayerPersistantInfo.E_State state)
	{
		foreach (PlayerPersistantInfo ppi in PPIs)
			ppi.State = state;

		LocalPPI.State = state;
	}

	[uSuite.RPC]
	void SynchronizeStatePlayer(uLink.NetworkPlayer player, PlayerPersistantInfo.E_State state)
	{
		PlayerPersistantInfo ppi = GetPPI(player);
		ppi.State = state;

		if (LocalPPI.Player == player)
		{
			LocalPPI.State = state;
		}
	}

	void SynchronizeScore(PlayerPersistantInfo ppi)
	{
		NetworkView.RPC("SynchroScoreOnClient", uLink.RPCMode.Others, ppi.Player, ppi.Score);
	}

	[uSuite.RPC]
	void SynchroScoreOnClient(uLink.NetworkPlayer player, PPIRoundScore score)
	{
		PlayerPersistantInfo p = PPIManager.Instance.GetPPI(player);
		p.Score.Update(score);
	}

#if false
				// we don't want to let server to fetch local ppi
				// it will be done later after round results display
				// =================================================================================================================
				// =================================================================================================================	
    #region --- Update player persistent info ...    
	private void GetCloudPPI()
	{
		if (CloudUser.instance.isUserAuthenticated == false)
		{
			Debug.LogError("Cannot get PPI from cloud. User is not authenticated");
			return;
   		}
	
   		StartCoroutine(GetCloudPPI_Coroutine());
	}
	
	private IEnumerator GetCloudPPI_Coroutine()
	{
		//Debug.Log(Time.timeSinceLevelLoad + " GetPPIFromCloud Begin "); 
		GetPlayerPersistantInfo action = new GetPlayerPersistantInfo( CloudUser.instance.authenticatedUserID );
		GameCloudManager.AddAction(action);
		
		// wait for authentication...		
		while( action.isDone == false )
		{
			yield return new WaitForSeconds(0.2f);
		}
		
		if(action.isSucceeded == true)
		{
			//Debug.Log(Time.timeSinceLevelLoad + " GetPPIFromCloud End "); 
			PlayerPersistantInfo PPIFromCloud = new PlayerPersistantInfo();
			
			//Debug.LogWarning("Cloud PlayerPersistantInfo = " + action.result);
			
			if (PPIFromCloud.InitPlayerDataFromStr( action.result ))
			{
				SetPPIFromCloud(PPIFromCloud);
				
				yield break;
			}
		}
		
		// TODO Is this correct? Maybe null is better...
//		Debug.LogError("Cannot get PPI from cloud: using fake PPI");
//		PPIFromCloud = new PlayerPersistantInfo();
		
		Debug.LogError("Cannot get PPI from cloud" + action.status);
	}
	#endregion     
#endif

	public void SetPPIFromCloud(PlayerPersistantInfo inCloudPPI)
	{
		if (Game.Instance != null && Game.Instance.GameLog)
			Debug.Log(Time.timeSinceLevelLoad + " setppifromcloud exp " + LocalPPI.Experience + " new " + inCloudPPI.Experience);
		LocalPPI.CopyPlayerData(inCloudPPI);

		LocalPPI.Name = CloudUser.instance.nickName;

		PlayerPersistantInfo ppiInList = GetLocalPlayerPPI();

		if (ppiInList != null)
			ppiInList.CopyPlayerData(inCloudPPI);

		OnLocalPlayerInfoChanged();

		if (Game.Instance != null && Game.Instance.GameLog)
			Debug.Log("PPI updated from cloud");
	}

	public void UpdateFromCloudDelayed(float seconds)
	{
		Invoke("UpdateFromCloud", seconds);
	}

	public void UpdateFromCloud()
	{
		if (ApplicationDZ.loadedLevelName != Game.MainMenuLevelName) //update only in main menu
			return;

		FetchPlayerPersistantInfo action = new FetchPlayerPersistantInfo(CloudUser.instance.authenticatedUserID);
		GameCloudManager.AddAction(action);
	}

	void OnLocalPlayerInfoChanged()
	{
		// make a temporary copy of the event to avoid possibility of 
		// a race condition if the last subscriber unsubscribes 
		// immediately after the null check and before the event is raised
		PlayerPersistantInfoEventHandler handler = null;
		lock (this)
		{
			handler = m_LocalPlayerInfoChanged;
		}

		// raise event
		if (handler != null)
		{
			handler(LocalPPI);
		}
	}

	void FixNickname(PlayerPersistantInfo ppi)
	{
		string nickname = string.IsNullOrEmpty(ppi.Name) ? ppi.UserName_TODO : ppi.Name;
		ppi.Name = GuiBaseUtils.FixNickname(nickname, ppi.UserName_TODO);
	}

	//*********************************

	#region --- Debug methods ...

	public void ReportAllPPI()
	{
		if (Game.Instance.GameLog == false)
			return;

		Debug.Log("REPORTING PPIs: ");
		foreach (PlayerPersistantInfo ppi in GetPPIList())
			Debug.Log("PPI " + ppi.Name + " NP - " + ppi.Player);

		Debug.Log("END OF REPORT");
	}

	#endregion
}
