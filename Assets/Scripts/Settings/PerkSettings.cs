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

[AddComponentMenu("Upgrades/PerkSettings")]

// !!!  NEMAZAT POLOZKY, PRIDAVAT POUZE NA KONEC SEZNAMU (pred MAX_ID) !!! - reflectuje se na CLOUD jako INT
public enum E_PerkID
{
	None,
	ExtendedHealth,
	ExtendedHealthII,
	Sprint,
	SprintII,
	SprintIII,
	FasterMove,
	FasterMoveII,
	FlakJacket,
	FlakJacketII,
	MAX_ID //add any new IDs prior to this one!
}

// -----------------
public class PerkSettings : Settings<E_PerkID>
{
	// --------------------------------------------------------------------------------------------
	// P U B L I C    P A R T
	// --------------------------------------------------------------------------------------------

	[OnlineShopItemProperty] // Please don't change names of members tagged with 'OnlineShopItemProperty' as it breaks online shopping service
	public int MoneyCost;
	[OnlineShopItemProperty] // Please don't change names of members tagged with 'OnlineShopItemProperty' as it breaks online shopping service
	public int GoldCost;

	[OnlineShopItemProperty] public float Modifier;
	[OnlineShopItemProperty] public float Timer; //perk stamina
	[OnlineShopItemProperty] public float Recharge; //recharge modifier (recharges by this amount every second)

	public override bool IsDefault()
	{
		return (MoneyCost == 0) && (GoldCost == 0) && (!DISABLED);
	}

	public override string GetSettingsClass()
	{
		return "perk";
	}

	public override string ToString()
	{
		return ID.ToString();
	}
}
