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
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class ComponentWeapons : MonoBehaviour
{
	public Transform Hand;
	public Dictionary<E_WeaponID, WeaponBase> Weapons { get; protected set; }
	public E_WeaponID CurrentWeapon { get; protected set; }

	protected AgentHuman Owner;

	public WeaponBase GetCurrentWeapon()
	{
		return CurrentWeapon != E_WeaponID.None ? Weapons[CurrentWeapon] : null;
	}

	public WeaponBase GetWeapon(E_WeaponID t)
	{
		return Weapons.ContainsKey(t) ? Weapons[t] : null;
	}

	protected virtual void Awake()
	{
		Owner = GetComponent<AgentHuman>();
		Weapons = new Dictionary<E_WeaponID, WeaponBase>();

		CurrentWeapon = E_WeaponID.None;
	}

	// Use this for initialization
	protected virtual void Activate()
	{
//        Debug.Log(gameObject.name + "CW activate");

		PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(Owner.NetworkView.owner);

		//Debug.Log(ppi.EquipList.SelectedWeapon);

		/*if(ppi.EquipList.SelectedWeapon != E_WeaponID.None)
            CurrentWeapon = ppi.EquipList.SelectedWeapon;*/

		if (Owner.BlackBoard.ProxyDataForSpawn.IsValid)
			CurrentWeapon = Owner.BlackBoard.ProxyDataForSpawn.CurrentWeapon;
		else
		{
//else find first weapon (it suck, because slot could be empty)
			for (int i = 0; i < 3 && CurrentWeapon == E_WeaponID.None; i++)
			{
				PPIWeaponData d = ppi.EquipList.Weapons.Find(ps => ps.EquipSlotIdx == i);

				if (d.IsValid() == false)
					continue;

				CurrentWeapon = d.ID;
				break;
			}
		}

		foreach (PPIWeaponData winfo in ppi.EquipList.Weapons)
		{
			if (winfo.ID == E_WeaponID.None)
				continue;

			//Debug.Log("add weapon " + winfo.ID);
			AddWeapon(winfo.ID);

			if (CurrentWeapon == E_WeaponID.None)
				CurrentWeapon = winfo.ID;
		}

		AddWeaponToOwnerHand(Weapons[CurrentWeapon]);
		Owner.WorldState.SetWSProperty(E_PropKey.WeaponLoaded, GetCurrentWeapon().ClipAmmo > 0);

		//change FOV if desired
		if (uLink.Network.isClient && Owner.NetworkView.isMine)
		{
			float newFOV = GameCamera.Instance.DefaultFOV;
			if (Owner.IsInCover)
				newFOV *= Owner.WeaponComponent.GetCurrentWeapon().CoverFovModificator;
			GameCamera.Instance.SetFov(newFOV, 60);
		}
	}

	protected void Deactivate()
	{
		//Debug.Log(gameObject.name + "CW deactivate");

		foreach (KeyValuePair<E_WeaponID, WeaponBase> weaponPair in Weapons)
		{
			if (WeaponManager.Instance)
				WeaponManager.Instance.Return(weaponPair.Value);
		}

		Weapons.Clear();

		CurrentWeapon = E_WeaponID.None;
	}

	void OnDestroy()
	{
		Deactivate();
	}

	//add weapon to list only
	public virtual void AddWeapon(E_WeaponID weapon)
	{
		if (Weapons == null)
			throw new System.MissingMemberException();

		if (WeaponManager.Instance == null)
			throw new System.ArgumentNullException();

		if (Weapons.ContainsKey(weapon))
			throw new System.Exception(weapon + " already exist in the list");

		Weapons.Add(weapon, WeaponManager.Instance.GetWeapon(Owner, weapon));
	}

	public void AddWeaponAndSelect(E_WeaponID weapon)
	{
		if (Weapons.ContainsKey(weapon))
		{
			Weapons[weapon].AddAmmo(-1);
			return;
		}

		WeaponBase newWeapon = WeaponManager.Instance.GetWeapon(Owner, weapon);

		Weapons.Add(newWeapon.WeaponID, newWeapon);
		Owner.BlackBoard.Desires.Weapon = weapon;
	}

	//activate weapon 
	protected void AddWeaponToOwnerHand(WeaponBase weapon)
	{
//        Debug.Log(Owner.name +  "CW Add to owner");
		CurrentWeapon = weapon.WeaponID;

		Owner.WorldState.SetWSProperty(E_PropKey.WeaponLoaded, weapon.ClipAmmo > 0);

		weapon.LinkToOwner(Owner, Hand);
	}

	//activate weapon 
	public void SwitchWeapons(E_WeaponID weaponType)
	{
		//Debug.Log(Owner.name +  "CW Add to owner, weapon " + weaponType);
		if (Weapons.ContainsKey(weaponType) == false)
			return;

		float busyTime = Weapons[CurrentWeapon].GetBusyTime();

		Weapons[CurrentWeapon].SetDefaultMaterial();
		Weapons[CurrentWeapon].UnlinkFromOwner();

		CurrentWeapon = weaponType;
		Weapons[weaponType].LinkToOwner(Owner, Hand);

		Owner.WorldState.SetWSProperty(E_PropKey.WeaponLoaded, Weapons[weaponType].ClipAmmo > 0);

		if (Owner.GadgetsComponent.IsBoostActive(E_ItemBoosterBehaviour.Invisible))
		{
			float power = Owner.GadgetsComponent.GetActiveBoostPower(E_ItemBoosterBehaviour.Invisible);

			Weapons[weaponType].SetInvisibleMaterial(power);
		}

		Weapons[weaponType].WeaponArm(busyTime);
	}

	public virtual void DisableCurrentWeapon(float disableTime)
	{
		if (CurrentWeapon != E_WeaponID.None)
			Weapons[CurrentWeapon].SetBusy(disableTime);
	}

	public virtual void AddAmmoToWeapon(AmmoBox a)
	{
		//do nothing, only on server 
	}
}
