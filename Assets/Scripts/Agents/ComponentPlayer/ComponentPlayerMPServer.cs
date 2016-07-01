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

[RequireComponent(typeof (AgentHuman))]
[RequireComponent(typeof (CharacterController))]
[RequireComponent(typeof (Animation))]
[RequireComponent(typeof (AnimSetPlayer))]
[RequireComponent(typeof (CameraBehaviour))]
[RequireComponent(typeof (AnimComponent))]
[RequireComponent(typeof (ComponentSensors))]
[RequireComponent(typeof (ComponentWeaponsServer))]
[RequireComponent(typeof (ComponentGadgets))]
public class ComponentPlayerMPServer : ComponentPlayerMP
{
	float SpeedTestLastTimeReceived = -1;

	void LateUpdate()
	{
		if (Owner.IsAlive == false)
			return;

		AmmoBox a = Mission.Instance.GameZone.GetNearestDroppedAmmoClip(Owner, 0.1f);

		if (a != null)
			Owner.WeaponComponent.AddAmmoToWeapon(a);
	}

	protected override void Activate()
	{
		base.Activate();

		PlayerPersistantInfo ppi = GetPPI();

		if (null != ppi)
		{
			ppi.MarkStartGame();
		}

		ServerSynchronizeSpeeds(true);
	}

	protected override void Deactivate()
	{
		base.Deactivate();

		PlayerPersistantInfo ppi = GetPPI();

		if (null != ppi)
		{
			ppi.MarkEndGame();
		}
	}

	PlayerPersistantInfo GetPPI()
	{
		if (null != Owner)
		{
			if (uLink.NetworkPlayer.unassigned != Owner.NetworkView.owner)
			{
				return PPIManager.Instance.GetPPI(Owner.NetworkView.owner);
			}
		}

		return null;
	}

	public void ServerSynchronizeSpeeds(bool initialSynchronization)
	{
		if (initialSynchronization)
		{
			SpeedTestLastTimeReceived = -1;
		}

		if (null == Owner || false == uLink.Network.isServer || null == Owner.GadgetsComponent)
		{
			return;
		}

		float maxRunSpeed = Owner.BlackBoard.BaseSetup.MaxRunSpeed;
		float maxWalkSpeed = Owner.BlackBoard.BaseSetup.MaxWalkSpeed;
		float perkModifier = 0;

		PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(Owner.NetworkView.owner);

		if (ppi.EquipList.Perk != E_PerkID.None)
		{
			perkModifier = PerkSettingsManager.Instance.Get(ppi.EquipList.Perk).Modifier;
		}

		Owner.NetworkView.RPC("RPCClientSynchronizeSpeeds", Owner.NetworkView.owner, maxRunSpeed, maxWalkSpeed, perkModifier);

		Debug.Log("ServerSynchronizeSpeeds: to=" + ppi.Name + " maxRunSpeed=" + maxRunSpeed + " maxWalkSpeed=" + maxWalkSpeed + " perkModifier=" +
				  perkModifier + " init=" + initialSynchronization);
	}

	[uSuite.RPC]
	void RPCTimeCheck(uLink.NetworkMessageInfo info)
	{
		float currentTime = Time.time;

		if (SpeedTestLastTimeReceived >= 0 && (currentTime - SpeedTestLastTimeReceived + 1.0f) < SPEED_TEST_DELAY)
		{
			//Debug.Log( "HIT! current " + currentTime + " last " + SpeedTestLastTimeReceived + " diff " + ( currentTime - SpeedTestLastTimeReceived ) );
			Owner.Die(Owner, transform.position, Vector3.zero, 100, E_BodyPart.Body);
		}

		SpeedTestLastTimeReceived = currentTime;
	}
}
