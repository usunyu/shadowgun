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

// -----------------
[AddComponentMenu("Weapons/WeaponSettings")]
public class WeaponSettings : Settings<E_WeaponID>
{
	// --------------------------------------------------------------------------------------------
	// P U B L I C    P A R T
	// --------------------------------------------------------------------------------------------

	[System.Serializable]
	public class Upgrade : ISettings
	{
		public bool Disabled;

		[OnlineShopItemSystemProperty] public E_WeaponUpgradeID ID;

		[OnlineShopItemSystemProperty] public int GUID;

		[OnlineShopItemProperty] public E_WeaponID ParentID;

		[OnlineShopItemProperty] // Please don't change names of members tagged with 'OnlineShopItemProperty' as it breaks online shopping service
		public int MoneyCost;

		[OnlineShopItemProperty] // Please don't change names of members tagged with 'OnlineShopItemProperty' as it breaks online shopping service
		public int GoldCost;

		[OnlineShopItemProperty] public float Modifier;

		public virtual int GetGUID()
		{
			return GUID;
		}

		public virtual string GetSettingsClass()
		{
			return "weaponUpgrade";
		}

		public virtual string GetIdAsStr()
		{
			return ID.ToString();
		}

		public virtual bool IsDefault()
		{
			return false;
		} // true, pokud ma byt zarazen do DefaultPPI

		public virtual bool SkipZeroCostWarning()
		{
			return false;
		}

		public void GenerateGuid(E_WeaponID parentID, string parentIdAsStr)
		{
			ParentID = parentID;
			GUID = Mathf.Abs(FNVHash.CalcModFNVHash(parentIdAsStr + GetSettingsClass() + GetIdAsStr()));
		}
	}

	[OnlineShopItemProperty] // Please don't change names of members tagged with 'OnlineShopItemProperty' as it breaks online shopping service
	public int MoneyCost;

	[OnlineShopItemProperty] // Please don't change names of members tagged with 'OnlineShopItemProperty' as it breaks online shopping service
	public int GoldCost;

	[OnlineShopItemAttribute] public int UpgradeLevel = 0;

	[OnlineShopItemAttribute] public int NumKills = 0;

	public E_WeaponType WeaponType;
	public E_ProjectileType ProjectileType;
	public bool FullAuto;

	[OnlineShopItemProperty] public int RechargeAmmoCount;

	public Transform TypeWidgetPrefab;

	public GUIBase_Widget TypeWidget
	{
		get
		{
			if (TypeWidgetPrefab)
				return TypeWidgetPrefab.GetComponent<GUIBase_Widget>();
			else
				return null;
		}
	}

	public GameObject Model;

	[System.Serializable]
	public class Data
	{
		//TODO_DT_SHOP  OnlineShopItemProperty
		[OnlineShopItemProperty] public float FireFovModificator = 1.0f;

		[OnlineShopItemProperty] public float CoverFovModificator = 1.0f;

		[OnlineShopItemProperty] public float CoverFireFovModificator = 0.80f;

		[OnlineShopItemProperty] public int MaxAmmoInClip;

		[OnlineShopItemProperty] public int MaxAmmoInWeapon;

		[OnlineShopItemProperty] public float CriticalAmmo;

		[OnlineShopItemProperty] public float FireTime;

		[OnlineShopItemProperty] public float Damage;

		[OnlineShopItemProperty] public float Speed;

		public float Impulse;

		[OnlineShopItemProperty] public float Dispersion;

		public float DispersionAddMove;
	}

	[OnlineShopItemProperty] // Please don't change names of members tagged with 'OnlineShopItemProperty' as it breaks online shopping service
	public Data BaseData = new Data();
	[OnlineShopItemProperty] public List<Upgrade> Upgrades = new List<Upgrade>();

	public override bool IsDefault()
	{
		return (MoneyCost == 0) && (GoldCost == 0) && (!DISABLED);
	}

	public override string GetSettingsClass()
	{
		return "weapon";
	}

	public override string ToString()
	{
		return ID.ToString();
	}

	public override List<ISettings> GetChildSettings()
	{
		List<ISettings> res = new List<ISettings>();

		foreach (ISettings curr in Upgrades)
		{
			res.Add(curr);
		}

		return res;
	}

	public override void OnGenerateGuid()
	{
		string parentIdAsStr = SettingsBase.GUID_CALC_PREFIX + GetIdAsStr();

		foreach (Upgrade curr in Upgrades)
		{
			curr.GenerateGuid(ID, parentIdAsStr);
		}

		GUID = CalcGUIDFromID();
	}
}
