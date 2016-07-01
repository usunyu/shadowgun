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

[AddComponentMenu("Multiplayer/GameZone DeathMatch")]
public class GameZoneDeathMatch : GameZoneMP
{
	public List<SpawnPointPlayer> PlayerSpawns = new List<SpawnPointPlayer>();

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
	}

	// try to choose best spawnpoint for spawning new player
	public SpawnPoint GetPlayerSpawnPoint()
	{
		// grab informations about existing spawnpoints
		// @see GameZoneMP.BuildSpawnPointsInfo()
		List<SpawnPointInfo> SPointsInfo = BuildSpawnPointsInfo(PlayerSpawns);

		// this will hold list of choosen spawnpoints
		List<SpawnPoint> SelectedSpawns = new List<SpawnPoint>();

		string usedMethod = "perfect (1)";

		// I. Try to find optimal spawnpoints - these which distance to nearest player is in desired range <Min, Max>
		if (!GrabOptimalSpawnPoints(SPointsInfo, SelectedSpawns, SpawnSetup.OptimalSpawnDistanceMin, SpawnSetup.OptimalSpawnDistanceMax))
		{
			usedMethod = "far (2)";
			// II. If there was no optimal spawnpoint found, try to find far spawnpoints
			if (!GrabOptimalSpawnPoints(SPointsInfo, SelectedSpawns, SpawnSetup.OptimalSpawnDistanceMin, float.MaxValue))
			{
				usedMethod = "non occupied (3)";
				// III. If there was no spawnpoint found still, try to find unoccupied one at least
				if (!GrabOptimalSpawnPoints(SPointsInfo, SelectedSpawns, SpawnSetup.MinimalSpawnDistanceToOtherPlayers, float.MaxValue))
				{
					usedMethod = "random (4)";
					// all spawnpoints are occupied now - should we try to spawn player at slightly different place ?
					// in fact this should not happen - there should be always one spare spawnpoint (there should be MaxPlayers + 1 spawnpoints)
				}
			}
		}

		// return one of selected spawnpoints, if any
		if (SelectedSpawns.Count > 0)
		{
			if (Game.Instance.GameLog)
				Debug.Log("GameZoneDeathMatch.GetPlayerSpawnPoint() - used method :" + usedMethod);

			return SelectedSpawns[Random.Range(0, SelectedSpawns.Count)];
		}

		if (Game.Instance.GameLog)
			Debug.Log("GameZoneDeathMatch.GetPlayerSpawnPoint() - used method : fallback, random (5) ");

		// IV. If all other methods failed, just return one of spawnpoints
		return PlayerSpawns[Random.Range(0, PlayerSpawns.Count)];
	}
}
