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

public class HatSettingsManager : SettingsManager<HatSettings, E_HatID>
{
	// --------------------------------------------------------------------------------------------
	// P U B L I C    P A R T
	// --------------------------------------------------------------------------------------------
	public static HatSettingsManager Instance
	{
		get { return GetInstance(); }
	}

	// --------------------------------------------------------------------------------------------
	// P R I V A T E    P A R T
	// --------------------------------------------------------------------------------------------
	static HatSettingsManager s_Instance = null;

	// --------
	static HatSettingsManager GetInstance()
	{
		if (s_Instance == null)
		{
			s_Instance = ScriptableObject.CreateInstance<HatSettingsManager>();
			s_Instance.Init("_Settings/_HatSettings");
			ScriptableObject.DontDestroyOnLoad(s_Instance);
		}

		return s_Instance;
	}
}
