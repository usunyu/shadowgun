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
public class ComponentWeaponsProxy : ComponentWeapons
{
	protected override void Awake()
	{
		base.Awake();
		Owner.BlackBoard.ActionHandler += HandleAction;
	}

	public override void DisableCurrentWeapon(float disableTime)
	{
		//do nothing on proxy
	}

	[uSuite.RPC]
	protected void ClientSetAmmo(E_WeaponID type, int clipAmmo, int weaponAmmo)
	{
		Weapons[type].SetAmmo(clipAmmo, weaponAmmo);

		Owner.SoundPlay(Player.LocalInstance.SoundTakeAmmo);
	}

	public void HandleAction(AgentAction action)
	{
		if (action.IsFailed())
			return;

		if (action is AgentActionAttack)
		{
			AgentActionAttack a = action as AgentActionAttack;
			WeaponBase weapon = Weapons[CurrentWeapon];

			weapon.Fire(a.AttackDir);
		}
		else if (action is AgentActionReload)
		{
			WeaponBase weapon = Weapons[CurrentWeapon];
			weapon.Reload();
//            Owner.WorldState.SetWSProperty(E_PropKey.WeaponLoaded, true);
		}
	}
}
