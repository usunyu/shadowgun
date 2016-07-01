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

public abstract class GameZoneControledObject : uLink.MonoBehaviour
{
	protected uLink.NetworkView NetworkView { get; private set; }

	protected virtual void Awake()
	{
		NetworkView = networkView;
	}

	public abstract void Reset();
};

public abstract class GameZoneMP : MonoBehaviour
{
	// helper struct for keeping informations about spawnpoint
	protected class SpawnPointInfo
	{
		// spawnpoint reference
		public SpawnPoint SPoint;

		// distance from spawnpoint to nearest player
		public float DistMin;
	};

	// settings for player's spawning
	[System.Serializable]
	public class SpawnSettings
	{
		// server will try to keep this minimal distance from others during spawn of new player
		public float OptimalSpawnDistanceMin = 5;

		// server will try to keep this maximal distance from others during spawn of new player
		public float OptimalSpawnDistanceMax = 15;

		// in case there is no optimal spawnpoint, lets try to choose unoccupied one
		public float MinimalSpawnDistanceToOtherPlayers = 2;

		// over this height is spawnpoint assumed to be in other floor
		public float FloorToFloorHeight = 2.0f;

		// modifier used in case spawnpoint is in other floor
		public float AnotherFloorMultiplier = 2.5f;

		// value added to computed distance in case of other floor
		public float AnotherFloorDistanceAdded = 0.0f;
	};

	// settings for player's spawning
	public SpawnSettings SpawnSetup = new SpawnSettings();

	GameObject GameObject;

	Cover[] Covers;
	AmmoBox[] AmmoBoxes;

	GameZoneControledObject[] ControledObject;

	protected void Init()
	{
		GameObject = gameObject;

		ControledObject = GameObject.GetComponentsInChildren<GameZoneControledObject>(true);

		// FIXME: Non-deterministic, we need a 100% deterministic way howto gurante the exactly same order on clients and servers.
		Covers = gameObject.GetComponentsInChildren<Cover>(true);
		AmmoBoxes = GameObject.GetComponentsInChildren<AmmoBox>(true);

		if (uLink.Network.isServer)
		{
#if !DEADZONE_CLIENT
			Server.Printf( "Number of ControledObjects " + ControledObject.Length );
#endif
		}
		else
		{
			Client.Printf("Number of ControledObjects " + ControledObject.Length);
		}
	}

	public virtual void Reset()
	{
		StopAllCoroutines();

		if (uLink.Network.isAuthoritativeServer)
		{
			foreach (GameZoneControledObject o in ControledObject)
				o.Reset();
		}
	}

	public Cover GetCoverAtPosition(Vector3 position)
	{
		foreach (Cover cover in Covers)
			if (cover.Position == position)
				return cover;

		return null;
	}

	public Cover GetCover(int index)
	{
		if (index < 0 || index >= Covers.Length)
			return null;

		return Covers[index];
	}

	public int GetCoverIndex(Cover cover)
	{
		for (int i = 0; i < Covers.Length; i++)
			if (Covers[i] == cover)
				return i;

		return -1;
	}

	public Cover GetCoverForPlayer(AgentHuman Agent, float distance)
	{
		Vector3 pos = Agent.Position;
		Vector3 coverdir = Agent.Forward;

		Cover c;

		for (int i = 0; i < Covers.Length; i++)
		{
			c = Covers[i];

			//Debug.Log(Covers[i].name + " Angle:"+c.GetCoverDirAngle(coverdir));

			if (c.GetCoverDirAngle(coverdir) <= 0.7f)
				continue;

			Vector3 dirToPos = (pos - c.Position).normalized;

			//Debug.Log(Covers[i].name + "dot: " + Vector3.Dot(c.Forward, dirToPos));

			if (Vector3.Dot(c.Forward, dirToPos) > 0.0f)
				continue;

			//test if pos is on the cover edge
			Vector3 dir = c.RightEdge - c.LeftEdge; //edge
			float d = dir.magnitude; //edge distance
			dir.Normalize();

			float dot = Vector3.Dot(dir, pos - c.LeftEdge); // get distance on edge

			//Debug.Log(Covers[i].name + " dot:" + dot + "cover len" + d);

			if (dot < -.3f || dot > d + .3f)
				continue;

			//Debug.Log(Covers[i].name + "distance: " + c.GetDistanceTo(pos));

			Vector3 CoverPos = c.GetNearestPointOnCover(pos);
			float DistToCover = (CoverPos - pos).magnitude;

			if (DistToCover > distance)
			{
				continue;
			}

			if (c.IsLockedForPlayer(pos, Agent.CharacterRadius + Agent.BlackBoard.CoverSetup.MultiCoverSafeDist))
			{
				//   Debug.Log(Covers[i].name + " locked");
				continue;
			}

			// test just ragdols
			LayerMask mask = ObjectLayerMask.Ragdoll;

			// there is nobody blocking agent's movement
			if (Agent.SweepTest((CoverPos - pos).normalized, DistToCover, mask))
			{
				return c;
			}
		}

		return null;
	}

	public AmmoBox GetNearestDroppedAmmoClip(AgentHuman agent, float dist)
	{
		float sqrDist = dist*dist;

		foreach (AmmoBox a in AmmoBoxes)
		{
			if (a.IsActive == false)
				continue;

			Vector3 pos = agent.CharacterController.ClosestPointOnBounds(a.Transform.position);

			if ((a.Transform.position - pos).sqrMagnitude < sqrDist)
				return a;
		}

		return null;
	}

	// grab informations about existing spawnpoints from the given list of SpawnPointPlayer
	protected List<SpawnPointInfo> BuildSpawnPointsInfo(List<SpawnPointPlayer> SpawnPointList)
	{
		List<SpawnPointInfo> SPointsInfo = new List<SpawnPointInfo>();

		// grab informations about every spawnpoint
		foreach (SpawnPoint SPoint in SpawnPointList)
		{
			SPointsInfo.Add(CreateSpawnPointInfo(SPoint));
		}

		return SPointsInfo;
	}

	// grab informations about existing spawnpoints from the given array of SpawnPointPlayer
	protected List<SpawnPointInfo> BuildSpawnPointsInfo(SpawnPointPlayer[] SpawnPointList)
	{
		List<SpawnPointInfo> SPointsInfo = new List<SpawnPointInfo>();

		// grab informations about every spawnpoint
		foreach (SpawnPoint SPoint in SpawnPointList)
		{
			SPointsInfo.Add(CreateSpawnPointInfo(SPoint));
		}

		return SPointsInfo;
	}

	// false if no spawnpoints found
	protected bool GrabOptimalSpawnPoints(List<SpawnPointInfo> SPointsInfo, List<SpawnPoint> SelectedSpawns, float Minimal, float Maximal)
	{
		foreach (SpawnPointInfo Info in SPointsInfo)
		{
			if (Info.DistMin >= Minimal && Info.DistMin <= Maximal)
			{
				SelectedSpawns.Add(Info.SPoint);
			}
		}

		return (SelectedSpawns.Count > 0) ? true : false;
	}

	// create and initialize SpawnPointInfo object based on given SpawnPoint
	SpawnPointInfo CreateSpawnPointInfo(SpawnPoint SPoint)
	{
		SpawnPointInfo SPointInfo = new SpawnPointInfo();

		SPointInfo.SPoint = SPoint;

		SPointInfo.DistMin = float.MaxValue;

		// find distance to nearest existing player
		foreach (KeyValuePair<uLink.NetworkPlayer, ComponentPlayer> pair in Player.Players)
		{
			Vector3 deltaToPlayer = pair.Value.Owner.Position - SPoint.Transform.position;

			float height = deltaToPlayer.y;

			deltaToPlayer.y = 0.0f;

			float distanceToAPlayer = deltaToPlayer.magnitude;

			if (height > SpawnSetup.FloorToFloorHeight)
			{
				distanceToAPlayer *= SpawnSetup.AnotherFloorMultiplier;
				distanceToAPlayer += SpawnSetup.AnotherFloorDistanceAdded;
			}

			SPointInfo.DistMin = Mathf.Min(distanceToAPlayer, SPointInfo.DistMin);
		}

		return SPointInfo;
	}
}
