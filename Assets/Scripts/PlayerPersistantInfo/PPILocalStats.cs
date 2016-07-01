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

//#####################################################################################################################

public class PPILocalStats
{
	#region fields ////////////////////////////////////////////////////////////////////////////////////////////////////

	// how many times owner killed others
	Dictionary<uLink.NetworkPlayer, int> m_Kills = new Dictionary<uLink.NetworkPlayer, int>(12);

	#endregion

	#region methods ///////////////////////////////////////////////////////////////////////////////////////////////////

	//-----------------------------------------------------------------------------------------------------------------
	public static void RecordKill(PlayerPersistantInfo killer, PlayerPersistantInfo victim)
	{
		PPILocalStats stats = killer.LocalStats;

		if (stats.m_Kills.ContainsKey(victim.Player))
		{
			stats.m_Kills[victim.Player] += 1;
		}
		else
		{
			stats.m_Kills[victim.Player] = 1;
		}
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static int GetKills(PlayerPersistantInfo killer, PlayerPersistantInfo victim)
	{
		PPILocalStats stats = killer.LocalStats;

		if (stats.m_Kills.ContainsKey(victim.Player))
		{
			return stats.m_Kills[victim.Player];
		}

		return 0;
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static PlayerPersistantInfo GetBestVictim(PlayerPersistantInfo killer, ref int killsNum)
	{
		PlayerPersistantInfo bestPPI = null;
		int bestKills = 0;
		List<PlayerPersistantInfo> ppiList = PPIManager.Instance.GetPPIList();

		foreach (PlayerPersistantInfo other in ppiList)
		{
			int kills = GetKills(killer, other);

			if (bestKills < kills)
			{
				bestPPI = other;
				bestKills = kills;
			}
		}

		killsNum = bestKills;

		return bestPPI;
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static PlayerPersistantInfo GetBestKiller(PlayerPersistantInfo victim, ref int killsNum)
	{
		PlayerPersistantInfo bestPPI = null;
		int bestKills = 0;
		List<PlayerPersistantInfo> ppiList = PPIManager.Instance.GetPPIList();

		foreach (PlayerPersistantInfo other in ppiList)
		{
			int kills = GetKills(other, victim);

			if (bestKills < kills)
			{
				bestPPI = other;
				bestKills = kills;
			}
		}

		killsNum = bestKills;

		return bestPPI;
	}

	#endregion
}
