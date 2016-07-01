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

[AddComponentMenu("Multiplayer/GameZone ZoneControl")]
public class GameZoneZoneControl : GameZoneMP
{
	public List<ZoneControlFlag> Zones;

	//E_Team FirstZoneOwner = E_Team.Good;
	//E_Team LastZoneOwner = E_Team.Bad;

	void Awake()
	{
		base.Init();
	}

	void Start()
	{
	}

	public override void Reset()
	{
		base.Reset();

		foreach (ZoneControlFlag z in Zones)
			z.Reset();
	}

	// @return SpawnPoint selected by given criteria
	public SpawnPoint GetPlayerSpawnPoint(int zoneIndex, E_Team team)
	{
		SpawnPointPlayer[] spawns;

		if (team == E_Team.Good)
			spawns = Zones[zoneIndex].GoodSpawnPoints;
		else
			spawns = Zones[zoneIndex].BadSpawnPoints;

		// grab informations about existing spawnpoints
		// @see GameZoneMP.BuildSpawnPointsInfo()
		List<SpawnPointInfo> SPointsInfo = BuildSpawnPointsInfo(spawns);

		// this will hold list of choosen spawnpoints
		List<SpawnPoint> SelectedSpawns = new List<SpawnPoint>();

		// I. Try to find optimal spawnpoints - these which distance to nearest player is in desired range <Min, Max>
		if (!GrabOptimalSpawnPoints(SPointsInfo, SelectedSpawns, SpawnSetup.OptimalSpawnDistanceMin, SpawnSetup.OptimalSpawnDistanceMax))
		{
			// II. If there was no spawnpoint found still, try to find unoccupied one at least
			if (!GrabOptimalSpawnPoints(SPointsInfo, SelectedSpawns, SpawnSetup.MinimalSpawnDistanceToOtherPlayers, float.MaxValue))
			{
				// all spawnpoints are occupied now - should we try to spawn player at slightly different place ?
			}
		}

		// return one of selected spawnpoints, if any
		if (SelectedSpawns.Count > 0)
		{
			return SelectedSpawns[Random.Range(0, SelectedSpawns.Count)];
		}

		// III. If all other methods failed, just return one of spawnpoints
		return spawns[Random.Range(0, spawns.Length)];
	}
}
