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

public class SettingsInitializer : MonoBehaviour
{
	// Use this for initialization
	void Start()
	{
		GameObject.DontDestroyOnLoad(gameObject);
		int count = 0;
		if (null != BundleSettingsManager.Instance)
			count++;
		if (null != FundSettingsManager.Instance)
			count++;
		if (null != HatSettingsManager.Instance)
			count++;
		if (null != ItemSettingsManager.Instance)
			count++;
		if (null != PerkSettingsManager.Instance)
			count++;
		if (null != SkinSettingsManager.Instance)
			count++;
		if (null != TechSettingsManager.Instance)
			count++;
		if (null != UpgradeSettingsManager.Instance)
			count++;
		if (null != WeaponSettingsManager.Instance)
			count++;

		//Debug.Log("Settings: " + count);

		ApplicationDZ.LoadLevel("MainMenu");
	}

	// Update is called once per frame
	void Update()
	{
	}
}
