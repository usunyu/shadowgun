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

public enum ResearchState
{
	Active,
	Available,
	Unavailable,
}

public interface IResearchItem
{
	int GetName();
	string GetDescription();
	GUIBase_Widget GetImage();
	int GetGUID();
	int GetPrice(out bool isGold);

	int GetNumOfParams();
	int GetParamName(int paramIndex);
	bool UpgradeIsAppliedOnParam(int paramIndex);
	string GetParamValue(int paramIndex);

	int GetNumOfUpgrades();
	int GetUpgradeName(int index);
	string GetUpgradeValueText(int index);
	bool OwnsUpgrade(int index);
	WeaponSettings.Upgrade GetUpgrade(int index);

	string GetCantBuyExplanation();
	ResearchState GetState();
	void StateChanged();
}
