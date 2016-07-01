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

[AddComponentMenu("Items/FundSettings")]

// !!!  NEMAZAT POLOZKY, PRIDAVAT POUZE NA KONEC SEZNAMU (pred MAX_ID) !!! - reflectuje se na CLOUD jako INT
public enum E_FundID
{
	None,
	Gold10,
	Gold20,
	Gold40,
	Gold100,
	Money1000,
	Money3000,
	Money5000,
	TapJoyWeb, //go to tapjoy.com 
	TapJoyInApp, //lanch inApp	offerwall
	Gold200,
	Gold500,
	Gold990,
	FreeOffer, //Sponsorpay offerwall
	FreeWeb, //Sponsorpay web
	Money6k,
	Money15k,
	Money25k,
	Money30k,
	Money50k,
	Convert99,
	Convert299,
	Convert499,
	Convert999,
	Convert1999,

	MAX_ID //add any new IDs prior to this one!
}

// -----------------
public class FundSettings : Settings<E_FundID>
{
	// --------------------------------------------------------------------------------------------
	// P U B L I C    P A R T
	// --------------------------------------------------------------------------------------------

	[OnlineShopItemProperty] // Please don't change names of members tagged with 'OnlineShopItemProperty' as it breaks online shopping service
	public int GoldCost; //how much gold we get for buying this item

	[OnlineShopItemProperty] // Please don't change names of members tagged with 'OnlineShopItemProperty' as it breaks online shopping service
	public int MoneyCost; //how much money we get for buying this item

	[OnlineShopItemProperty] public int AddGold; //how much gold we get for buying this item

	[OnlineShopItemProperty] public int AddMoney; //how much money we get for buying this item

	public override string GetSettingsClass()
	{
		return "fund";
	}

	public override string ToString()
	{
		return ID.ToString();
	}
}
