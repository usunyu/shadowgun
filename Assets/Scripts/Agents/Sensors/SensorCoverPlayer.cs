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

public class SensorCoverPlayer : SensorBase
{
	float UpdateTime = 0;

	public SensorCoverPlayer(AgentHuman owner)
					: base(owner)
	{
	}

	public override void Update()
	{
		if (UpdateTime < Time.timeSinceLevelLoad)
		{
			Owner.BlackBoard.Desires.CoverNear.Cover = Mission.Instance.GameZone.GetCoverForPlayer(Owner, 0.75f);
			//Owner.BlackBoard.Desires.CoverPosition = E_CoverDirection.Unknown;

//			Debug.Log("updating cover sensor: " + (Owner.BlackBoard.Desires.CoverNear.Cover ? Owner.BlackBoard.Desires.CoverNear.Cover.name : "none"));

			UpdateTime = Time.timeSinceLevelLoad + 0.1f; //10x per second should be enough for everyone ;)
		}
	}

	public override void Reset()
	{
	}
}
