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

/***************************************************************
 * Class Name :	Blackboard
 * Function   : Central memory for GOAPController and other subsystems. 
 * 
 * Created by : Marek Rabas
 *
 **************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

public class MissionBlackBoard : MonoBehaviour
{
	public List<Transform> EnemiesTransform;
	public float LastAttackTime;

	public static MissionBlackBoard Instance = null;

	void Awake()
	{
		Instance = this;
	}

	void Reset()
	{
		LastAttackTime = 0;
	}
}
