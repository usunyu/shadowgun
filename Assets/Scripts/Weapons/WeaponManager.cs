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

using System;
using UnityEngine;
using System.Collections.Generic;

// !Pridavat pouze na konec seznamu, na poradi zavisi ted nastaveni zbrani pres prefaby!
public enum E_WeaponUpgradeID
{
	Damage,
	ClipSize,
	Dispersion,
	BulletSpeed,
	AmmoSize,
	AimingFov,
	Silencer,
	FireTime,
}

public enum E_WeaponID
{
	None, // must be the 1st item !
	SMG1,
	SMG2,
	SMG3, //reserved
	SMG4, //reserved
	AR1,
	AR2,
	AR3,
	AR4, //reserver
	MG1,
	MG2,
	MG3,
	MG4, //reserved
	Shotgun1,
	Shotgun2,
	Shotgun3, //reserved
	Sniper1,
	Sniper2,
	Sniper3, //reserved
	Launcher1,
	Launcher2, //reserved
	Launcher3, //reserved
	Plasma1,
	Plasma2,
	Plasma3, //reserved
	Plasma4, //reserved
	MAX_ID //add any new IDs prior to this one!
}

public enum E_WeaponType
{
	Rifle,
	Plasma,
	Shotgun,
	Sniper,
	Launcher,
}

[AddComponentMenu("Weapons/WeaponManager")]
class WeaponManager : MonoBehaviour
{
	public static WeaponManager Instance;

	Dictionary<E_WeaponID, ResourceCache> WeaponsCache = new Dictionary<E_WeaponID, ResourceCache>();

	// Use this for initialization
	void Awake()
	{
		Instance = this;

		WeaponsCache[E_WeaponID.SMG1] = new ResourceCache(WeaponSettingsManager.Instance.Get(E_WeaponID.SMG1).Model, 1);
		WeaponsCache[E_WeaponID.SMG2] = new ResourceCache(WeaponSettingsManager.Instance.Get(E_WeaponID.SMG2).Model, 1);
		WeaponsCache[E_WeaponID.SMG3] = new ResourceCache(WeaponSettingsManager.Instance.Get(E_WeaponID.SMG3).Model, 1);

		WeaponsCache[E_WeaponID.AR1] = new ResourceCache(WeaponSettingsManager.Instance.Get(E_WeaponID.AR1).Model, 1);
		WeaponsCache[E_WeaponID.AR2] = new ResourceCache(WeaponSettingsManager.Instance.Get(E_WeaponID.AR2).Model, 1);
		WeaponsCache[E_WeaponID.AR3] = new ResourceCache(WeaponSettingsManager.Instance.Get(E_WeaponID.AR3).Model, 1);

		WeaponsCache[E_WeaponID.MG1] = new ResourceCache(WeaponSettingsManager.Instance.Get(E_WeaponID.MG1).Model, 1);
		WeaponsCache[E_WeaponID.MG2] = new ResourceCache(WeaponSettingsManager.Instance.Get(E_WeaponID.MG2).Model, 1);
		WeaponsCache[E_WeaponID.MG3] = new ResourceCache(WeaponSettingsManager.Instance.Get(E_WeaponID.MG3).Model, 1);

		WeaponsCache[E_WeaponID.Shotgun1] = new ResourceCache(WeaponSettingsManager.Instance.Get(E_WeaponID.Shotgun1).Model, 1);
		WeaponsCache[E_WeaponID.Shotgun2] = new ResourceCache(WeaponSettingsManager.Instance.Get(E_WeaponID.Shotgun2).Model, 1);
		WeaponsCache[E_WeaponID.Shotgun3] = new ResourceCache(WeaponSettingsManager.Instance.Get(E_WeaponID.Shotgun3).Model, 1);

		WeaponsCache[E_WeaponID.Sniper1] = new ResourceCache(WeaponSettingsManager.Instance.Get(E_WeaponID.Sniper1).Model, 1);
		WeaponsCache[E_WeaponID.Sniper2] = new ResourceCache(WeaponSettingsManager.Instance.Get(E_WeaponID.Sniper2).Model, 1);
		WeaponsCache[E_WeaponID.Sniper3] = new ResourceCache(WeaponSettingsManager.Instance.Get(E_WeaponID.Sniper3).Model, 1);

		WeaponsCache[E_WeaponID.Launcher1] = new ResourceCache(WeaponSettingsManager.Instance.Get(E_WeaponID.Launcher1).Model, 1);
		WeaponsCache[E_WeaponID.Launcher2] = new ResourceCache(WeaponSettingsManager.Instance.Get(E_WeaponID.Launcher2).Model, 1);
		WeaponsCache[E_WeaponID.Launcher3] = new ResourceCache(WeaponSettingsManager.Instance.Get(E_WeaponID.Launcher3).Model, 1);

		WeaponsCache[E_WeaponID.Plasma1] = new ResourceCache(WeaponSettingsManager.Instance.Get(E_WeaponID.Plasma1).Model, 1);
		WeaponsCache[E_WeaponID.Plasma2] = new ResourceCache(WeaponSettingsManager.Instance.Get(E_WeaponID.Plasma2).Model, 1);
		WeaponsCache[E_WeaponID.Plasma3] = new ResourceCache(WeaponSettingsManager.Instance.Get(E_WeaponID.Plasma3).Model, 1);

		/*WeaponsCache[E_WeaponID.SMG1] = new ResourceCache("Weapons/WeaponSmg01", 1);
        WeaponsCache[E_WeaponID.SMG2] = new ResourceCache("Weapons/WeaponSmg02", 1);

        WeaponsCache[E_WeaponID.AR1] = new ResourceCache("Weapons/WeaponRifle01", 1);
        WeaponsCache[E_WeaponID.AR2] = new ResourceCache("Weapons/WeaponRifle02", 1);
        WeaponsCache[E_WeaponID.AR3] = new ResourceCache("Weapons/WeaponRifle03", 1);

        WeaponsCache[E_WeaponID.MG1] = new ResourceCache("Weapons/WeaponMagun01", 1);
        WeaponsCache[E_WeaponID.MG2] = new ResourceCache("Weapons/WeaponMagun02", 1);
        WeaponsCache[E_WeaponID.MG3] = new ResourceCache("Weapons/WeaponMagun03", 1);

        WeaponsCache[E_WeaponID.Shotgun1] = new ResourceCache("Weapons/WeaponShotgun01", 1);
        WeaponsCache[E_WeaponID.Shotgun2] = new ResourceCache("Weapons/WeaponShotgun02", 1);

        WeaponsCache[E_WeaponID.Sniper1] = new ResourceCache("Weapons/WeaponSnipe01", 1);
        WeaponsCache[E_WeaponID.Sniper2] = new ResourceCache("Weapons/WeaponSnipe02", 1);

        WeaponsCache[E_WeaponID.Launcher1] = new ResourceCache("Weapons/WeaponLauncher01", 1);

        WeaponsCache[E_WeaponID.Plasma1] = new ResourceCache("Weapons/WeaponPlasma01", 1);
        WeaponsCache[E_WeaponID.Plasma2] = new ResourceCache("Weapons/WeaponPlasma02", 1);*/
	}

	public WeaponBase GetWeapon(AgentHuman Owner, E_WeaponID id)
	{
		// test if we have configured cache for this type of weapon...
		if (WeaponsCache.ContainsKey(id) == false)
		{
			Debug.LogError("WeaponManager: unknown type " + id);
			return null;
		}

		// if we known this weapon type but we don't have resource cache than go out,
		// this is corect situation...
		else if (WeaponsCache[id] == null)
		{
			Debug.LogError("WeaponManager: For this type " + id + " we don't have resource");
			return null;
		}

		else
		{
			GameObject go = WeaponsCache[id].Get();
			// assert if go is null... oops we don't have asserts...
			if (go)
			{
				WeaponBase weapon = go.GetComponent<WeaponBase>();

				if (null != weapon)
				{
					weapon.Initialize(Owner, id);
				}
				else
				{
					Debug.LogWarning("WeaponManager.GetWeapon() : weapon object without WeaponBase component - weapon '" + id + "'");
				}

				return weapon;
			}
			else
			{
				return null;
			}
		}
	}

	public void Return(WeaponBase weapon)
	{
		// sanity check...
		if (weapon == null)
		{
			Debug.LogError("WeaponManager: sombody is trying return null object to cache");
		}

		// test if we have configured cache for this type of weapon...
		else if (WeaponsCache.ContainsKey(weapon.WeaponID) == false)
		{
			Debug.LogError("WeaponManager: unknown type " + weapon.WeaponType);
		}

		// if we known this weapon type but we don't have resource cache than go out,
		// THIS is imposible This weapon was not constructed by this manager ...
		else if (WeaponsCache[weapon.WeaponID] == null)
		{
			Debug.LogError("WeaponManager: We don't have cache for this weapon type. This object was not created by this manager");
		}

		else
		{
			weapon.Reset(true);
			WeaponsCache[weapon.WeaponID].Return(weapon.gameObject);
		}
	}

	public void Clear()
	{
		WeaponsCache.Clear();
	}
}
