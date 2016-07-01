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
public class ComponentWeaponsOwner : ComponentWeapons
{
	AgentActionAttack AttackAction = null;
	E_WeaponID mAutoSwitchWeapon = E_WeaponID.None;

	protected override void Awake()
	{
		base.Awake();
		Owner.BlackBoard.ActionHandler += HandleAction;
	}

	// Update is called once per frame
	protected override void Activate()
	{
		base.Activate();

		GuiHUD.Instance.RegisterWeaponsInInventory();
	}

	void Update()
	{
		if (null != AttackAction)
		{
			if (AttackAction.IsSuccess() || AttackAction.IsFailed())
			{
				WeaponBase weapon = Weapons[CurrentWeapon];

				if (false == weapon.HasAnyAmmo)
				{
					mAutoSwitchWeapon = CurrentWeapon;
				}
			}
		}

		if (mAutoSwitchWeapon != E_WeaponID.None)
		{
			if (CurrentWeapon == mAutoSwitchWeapon)
			{
				if (Player.LocalInstance.CanChangeWeapon())
				{
					WeaponAutoSwitch();
					mAutoSwitchWeapon = E_WeaponID.None;
				}
			}
			else
			{
				mAutoSwitchWeapon = E_WeaponID.None;
			}
		}
	}

	void LateUpdate()
	{
		if (CurrentWeapon == E_WeaponID.None)
			return;

		WeaponBase weapon = Weapons[CurrentWeapon];

		if (weapon.IsBusy())
		{
			weapon.PrepareForFire(false);
			Owner.BlackBoard.Desires.WeaponTriggerUp = false;
			if (!Owner.IsInCover && !weapon.IsFiring())
			{
				float fov = GameCamera.Instance.DefaultFOV;
				if (!Mathf.Approximately(GameCamera.Instance.DesiredCameraFov, fov))
					GameCamera.Instance.SetFov(fov, 60);
			}
			return;
		}

		if (Owner.BlackBoard.Desires.WeaponTriggerOn && !weapon.UseFireUp ||
			Owner.BlackBoard.Desires.WeaponTriggerUp && weapon.UseFireUp && Owner.CanFire())
		{
			if (!Owner.IsInCover)
			{
				float fov = GameCamera.Instance.DefaultFOV;
				fov *= Owner.WeaponComponent.GetCurrentWeapon().FireFovModificator;
				if (!Mathf.Approximately(GameCamera.Instance.DesiredCameraFov, fov))
					GameCamera.Instance.SetFov(fov, 60);
			}

			if ((weapon.ClipAmmo > 0) && weapon.PreparedForFire)
			{
				AgentActionAttack a = AgentActionFactory.Create(AgentActionFactory.E_Type.Attack) as AgentActionAttack;
				ComponentPlayerMPOwner MPOwner = Owner.PlayerComponent as ComponentPlayerMPOwner;
				if (null != MPOwner)
				{
					a.AttackDir = MPOwner.GetRealFireDir(); //Owner.BlackBoard.FireDir;
				}
				else
				{
					Debug.LogWarning("Missing ComponentPlayerMPOwner on " + Owner);
					a.AttackDir = Owner.BlackBoard.FireDir;
				}
				a.FromPos = GetCurrentWeapon().ShotPos;
				Owner.BlackBoard.ActionAdd(a);
			}
			else
			{
				weapon.PrepareForFire(true);
				Owner.BlackBoard.Desires.WeaponTriggerUp = false;
				return;
			}

			if (weapon.IsFullAuto == false)
				Owner.BlackBoard.Desires.WeaponTriggerOn = false;
			Owner.BlackBoard.Desires.WeaponTriggerUp = false;
		}
		else if (Owner.BlackBoard.Desires.WeaponTriggerOn && weapon.UseFireUp)
		{
			weapon.PrepareForFire(true);
			if (!Owner.IsInCover)
			{
				float fov = GameCamera.Instance.DefaultFOV;
				fov *= Owner.WeaponComponent.GetCurrentWeapon().FireFovModificator;
				if (!Mathf.Approximately(GameCamera.Instance.DesiredCameraFov, fov))
					GameCamera.Instance.SetFov(fov, 60);
			}
		}
		else
		{
			weapon.PrepareForFire(false);
			if (!Owner.IsInCover && Owner.IsAlive)
			{
				float fov = GameCamera.Instance.DefaultFOV;
				if (!Mathf.Approximately(GameCamera.Instance.DesiredCameraFov, fov))
					GameCamera.Instance.SetFov(fov, 60);
			}
		}
	}

	public void HandleAction(AgentAction action)
	{
		if (action.IsFailed())
			return;

		if (action is AgentActionAttack)
		{
			AttackAction = action as AgentActionAttack;

			WeaponBase weapon = Weapons[CurrentWeapon];

			weapon.Fire(AttackAction.AttackDir);

			if (weapon.ClipAmmo == 0)
				Owner.WorldState.SetWSProperty(E_PropKey.WeaponLoaded, false);
		}
		else if (action is AgentActionReload)
		{
			WeaponBase weapon = Weapons[CurrentWeapon];
			weapon.Reload();
			Owner.WorldState.SetWSProperty(E_PropKey.WeaponLoaded, true);
		}
	}

	protected void WeaponAutoSwitch()
	{
		//FIXME tady se ( proted ) musim oprit o GUI - jinak nevim poradi zbrani

		List<E_WeaponID> weapons = new List<E_WeaponID>();

		int weaponsCount = GuiHUD.Instance.GetInventoryWeaponsCount();
		int startIndex = -1;

		for (int i = 0; i < weaponsCount; i++)
		{
			E_WeaponID W = GuiHUD.Instance.GetWeaponInInventoryIndex(i);

			if (E_WeaponID.None == W)
			{
				continue;
			}

			if (W == CurrentWeapon)
			{
				startIndex = weapons.Count;
			}

			WeaponBase wBase = GetWeapon(W);

			if (null != wBase && wBase.HasAnyAmmo)
			{
				weapons.Add(W);

				if (startIndex > 0)
					break;
			}
		}

		if (startIndex >= 0 && weapons.Count > 0)
		{
			if (startIndex == weapons.Count)
			{
				startIndex = 0;
			}
			// take it

			Owner.BlackBoard.Desires.Weapon = weapons[startIndex];

			//workaround to avoid sliding during weapon change (when this goes through GOAP, the GOAPMove action is terminated and it goes through Idle to the GOAPMove again)
//			Owner.WorldState.SetWSProperty( E_PropKey.WeaponChange, true );
			AgentActionWeaponChange Action = AgentActionFactory.Create(AgentActionFactory.E_Type.WeaponChange) as AgentActionWeaponChange;
			Action.NewWeapon = Owner.BlackBoard.Desires.Weapon;
			Owner.BlackBoard.ActionAdd(Action);
		}
	}

	[uSuite.RPC]
	protected void ClientSetAmmo(E_WeaponID type, int clipAmmo, int weaponAmmo)
	{
		Weapons[type].SetAmmo(clipAmmo, weaponAmmo);
		Owner.SoundPlay(Player.LocalInstance.SoundTakeAmmo);

		//GuiHUD.Instance.ShowMessageTimed(GuiHUD.WeaponAmmoMessage(type), GuiHUD.InfoMessageTime);
	}

	[uSuite.RPC]
	protected void RechargeAmmo(E_WeaponID weapon, int clipAmmo, int weaponAmmo)
	{
		Weapons[weapon].SetAmmo(clipAmmo, weaponAmmo);
		Owner.SoundPlay(Player.LocalInstance.SoundTakeAmmo);
		GuiHUD.Instance.RechargeAmmo();
	}
}
