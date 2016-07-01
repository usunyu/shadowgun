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

[AddComponentMenu("Items/ItemSettings")]

// !!!  NEMAZAT POLOZKY, PRIDAVAT POUZE NA KONEC SEZNAMU (pred MAX_ID) !!! - reflectuje se na CLOUD jako INT
public enum E_ItemID
{
	None,
	BoxHealth = 1, //done
	BoxAmmo = 2, //done
	Sprint = 3, //done beny: obsolete, Perk used instead
	BlastShield = 4, //done beny: obsolete, Perk used instead
	Remove = 5, // remove
	Remove2 = 6,
	Remove3 = 7,
	GrenadeFrag = 8, //timer
	GrenadeFlash = 9, //timer
	GrenadeEMP = 10, //touch
	Mine = 11,
	SpiderHuman = 12,
	SpiderEmp = 13,
	SentryGun = 14,
	SentryGunRail = 15,
	SentryGunRockets = 16,

	BoxHealthII = 17,
	BoxAmmoII = 18,
	EnemyDetector = 19,
	EnemyDetectorII = 20,
	Jammer = 21,
	GrenadeEMPII = 22, //touch
	MineEMP = 23,
	MineEMPII = 24,
	GrenadeFragII = 25,
	SpiderEmpII = 26,
	SentryGunII = 27,
	GrenadeSting = 28,
	BoosterSpeed = 29,
	BoosterAccuracy = 30,
	BoosterArmor = 31,
	BoosterDamage = 32,
	BoosterInvicible = 33,

	MAX_ID //add any new IDs prior to this one!
}

public enum E_ItemType
{
	Defense,
	Offense,
	PowerUps,
}

public enum E_ItemUse
{
	Passive,
	Activate,
}

public enum E_ItemBehaviour
{
	None,
	Sprint, //beny: obsolete, to be removed
	Throw,
	Place, //grenades,flashbangs, mines,turrects,ammobox,healbox,
	IncreaseMoney,
	IncreaseXp,
	IncreaseAmmo,
	Detector, //replaced BlastShield
	Jammer,
	Booster,
}

public enum E_ItemBoosterBehaviour
{
	None,
	Speed,
	Accuracy,
	Damage,
	Armor,
	Invisible,
}
// -----------------
public class ItemSettings : Settings<E_ItemID>
{
	// --------------------------------------------------------------------------------------------
	// P U B L I C    P A R T
	// --------------------------------------------------------------------------------------------

	[OnlineShopItemProperty] // Please don't change names of members tagged with 'OnlineShopItemProperty' as it breaks online shopping service
	public int MoneyCost;

	[OnlineShopItemProperty] // Please don't change names of members tagged with 'OnlineShopItemProperty' as it breaks online shopping service
	public int GoldCost;

	[OnlineShopItemProperty] // Please don't change names of members tagged with 'OnlineShopItemProperty' as it breaks online shopping service
	public int GoldReward;

	public E_ItemType ItemType;
	public E_ItemUse ItemUse;
	public E_ItemBehaviour ItemBehaviour;

	public GameObject SpawnObject;

	//throw or construct
	[OnlineShopItemProperty] // Please don't change names of members tagged with 'OnlineShopItemProperty' as it breaks online shopping service
	public int Count;

	[OnlineShopItemProperty] // Please don't change names of members tagged with 'OnlineShopItemProperty' as it breaks online shopping service
	public int MaxCountInMission;

	//sprint, construct 
	[OnlineShopItemProperty] // Please don't change names of members tagged with 'OnlineShopItemProperty' as it breaks online shopping service
	public float Timer;

	[OnlineShopItemProperty] // Please don't change names of members tagged with 'OnlineShopItemProperty' as it breaks online shopping service
	public float RechargeModificator;

	[OnlineShopItemProperty] // Please don't change names of members tagged with 'OnlineShopItemProperty' as it breaks online shopping service
	public bool AllowedInZoneControl = true;

	[OnlineShopItemProperty] // Please don't change names of members tagged with 'OnlineShopItemProperty' as it breaks online shopping service
	public bool AllowedInDeathmatch = true;

	[OnlineShopItemProperty] // Please don't change names of members tagged with 'OnlineShopItemProperty' as it breaks online shopping service
	public float Range = 0; //context-dependent range of the item

	[OnlineShopItemProperty] // Please don't change names of members tagged with 'OnlineShopItemProperty' as it breaks online shopping service
	public bool Replaceable = false;

	public E_ItemBoosterBehaviour BoosterBehaviour = E_ItemBoosterBehaviour.None;

	[OnlineShopItemProperty] // Please don't change names of members tagged with 'OnlineShopItemProperty' as it breaks online shopping service
	public float BoostModifier = 0;

	[OnlineShopItemProperty] // Please don't change names of members tagged with 'OnlineShopItemProperty' as it breaks online shopping service
	public float BoostTimer = 0;

	public ParticleSystem BoostEffect;
	public AudioClip BoostSoundOn;
	public AudioClip BoostSoundOff;

	public override bool IsDefault()
	{
		return (MoneyCost == 0) && (GoldCost == 0) && (!DISABLED);
	}

	public override string GetSettingsClass()
	{
		return "item";
	}

	public override string ToString()
	{
		return ID.ToString();
	}
}
