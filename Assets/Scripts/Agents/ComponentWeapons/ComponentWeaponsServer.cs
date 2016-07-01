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
public class ComponentWeaponsServer : ComponentWeapons
{
	protected override void Awake()
	{
		base.Awake();
		Owner.BlackBoard.ActionHandler += HandleAction;
	}

	public override void AddAmmoToWeapon(AmmoBox a)
	{
		if (Owner.IsServer == false)
			return;

		if (Weapons.ContainsKey(a.ForWeaponType) && Weapons[a.ForWeaponType].IsFull == false)
		{
			Weapons[a.ForWeaponType].AddAmmo(a.Ammo);

			a.Disable();

			Owner.NetworkView.RPC("ClientSetAmmo",
								  Owner.NetworkView.owner,
								  a.ForWeaponType,
								  Weapons[a.ForWeaponType].ClipAmmo,
								  Weapons[a.ForWeaponType].WeaponAmmo);
		}
	}

	public void HandleAction(AgentAction action)
	{
		if (action.IsFailed())
			return;

		if (action is AgentActionAttack)
		{
			AgentActionAttack a = action as AgentActionAttack;
			WeaponBase weapon = Weapons[CurrentWeapon];

			weapon.Fire(a.FromPos, a.AttackDir);

			PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(Owner.NetworkView.owner);
			if (ppi != null)
				ppi.AddWeaponUse(CurrentWeapon);
		}
		else if (action is AgentActionReload)
		{
			WeaponBase weapon = Weapons[CurrentWeapon];
			weapon.Reload();
			//            Owner.WorldState.SetWSProperty(E_PropKey.WeaponLoaded, true);
		}
	}

	public int RechargeAmmo()
	{
		if (Owner.IsServer == false)
			return 0;

		int count = 0;

		foreach (KeyValuePair<E_WeaponID, WeaponBase> pair in Weapons)
		{
			if (pair.Value.IsFull)
				continue;

			pair.Value.AddAmmo(pair.Value.RechargeAmmoCount);

			count += 5; //pair.Value.RechargeAmmoCount;

			Owner.NetworkView.RPC("RechargeAmmo", Owner.NetworkView.owner, pair.Key, pair.Value.ClipAmmo, pair.Value.WeaponAmmo);
		}

		return count;
	}
}
