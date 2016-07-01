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

// ProjectileGrenade.
// DSC  :: This class is handled different then other projectiles.. its not cached, because it use network instantiate function
[AddComponentMenu("Items/Projectiles/AmmoKit")]
public class AmmoKit : uLink.MonoBehaviour
{
	public float Speed = 5;
	public float Timer = 20;
	public float RechargeUses = 15.0f;

	public int PlayHitSoundCount = 2;
	int HitSoundsLeft;

	[SerializeField] ItemIcons Icons;

	Collider WaterVolume;

	Transform Transform;
	Rigidbody Rigidbody;
	public AgentHuman Owner { get; private set; }
	GameObject GameObject;

	E_ItemID ItemID;

	int EarnedExp;
	//private uLink.NetworkView NetworkView;

	void Awake()
	{
		GameObject = gameObject;
		Transform = transform;
		Rigidbody = GetComponent<Rigidbody>();
		//NetworkView = networkView;
	}

	void OnDestroy()
	{
		if (GuiHUD.Instance)
			GuiHUD.Instance.UnregisterAmmokit(this);
		StopAllCoroutines();
		WaterVolume = null;
		CancelInvoke();
		Rigidbody.Sleep();

		Owner.GadgetsComponent.UnRegisterLiveGadget(ItemID, GameObject);
	}

	void uLink_OnNetworkInstantiate(uLink.NetworkMessageInfo info)
	{
		HitSoundsLeft = PlayHitSoundCount;

		Owner = Player.GetPlayer(info.networkView.owner).Owner;
		if (GuiHUD.Instance)
			GuiHUD.Instance.RegisterAmmokit(this);

		Rigidbody.velocity = info.networkView.initialData.ReadVector3()*Speed;
		Rigidbody.SetDensity(1.5F);
		Rigidbody.WakeUp();

		ItemID = info.networkView.initialData.Read<E_ItemID>();

		//Debug.Log("Instantiate " + uLink.Network.isServer);

		if (uLink.Network.isServer)
			StartCoroutine(Recharge());

		Owner.GadgetsComponent.RegisterLiveGadget(ItemID, GameObject);

		Icons.SetTeamIcon(Owner.Team);
	}

	internal void OnTriggerEnter(Collider other)
	{
		if (uLink.Network.isServer)
			return;

		//Debug.Log(name + " OnCollisionEnter " + other.name);
#if UNITY_MFG && UNITY_EDITOR //we don't have FluidSurface in WebPlayer
        FluidSurface fluid = other.GetComponent<FluidSurface>();
        if(fluid != null)
        {
            WaterVolume = other;
            fluid.AddDropletAtWorldPos(Transform.position, 0.5f, +0.5f);
            ProjectileManager.Instance.PlayGrenadeHitSound(other.gameObject.layer, Transform.position);
            CombatEffectsManager.Instance.PlayHitEffect(other.gameObject, Transform.position, Vector3.up, E_ProjectileType.None, Owner != null && Owner.IsOwner);
        }
#endif
	}

	internal void OnTriggerExit(Collider other)
	{
		//Debug.Log(name + " OnTriggerExit " + other.name);
		if (other != null && WaterVolume == other)
		{
			WaterVolume = null;
		}
	}

	void OnCollisionEnter(Collision collision)
	{
		// ignore collisions with owner and his transforms...
		if (collision.transform.IsChildOf(Owner.Transform))
			return;

		if (uLink.Network.isClient)
		{
			if (WaterVolume == null && HitSoundsLeft > 0)
			{
				HitSoundsLeft--;
				ProjectileManager.Instance.PlayGrenadeHitSound(GetComponent<Collider>().gameObject.layer, Transform.position);
			}
		}
	}

	IEnumerator Recharge()
	{
		yield return new WaitForSeconds(2.0f);

		float timeToEnd = Time.timeSinceLevelLoad + Timer;
		float rechargeUses = 0;
		Vector3 center = Transform.position;
		AgentHuman agent;

		while ((Time.timeSinceLevelLoad < timeToEnd) && (rechargeUses < RechargeUses))
		{
			// TODO: A design question: Should we destroy such objects at the moment the owner dies? Keeping them in the level
			// but non-funcional is weird though.
			//if (Owner.IsAlive == false)
			//	break;

			bool used = false;
			foreach (KeyValuePair<uLink.NetworkPlayer, ComponentPlayer> pair in Player.Players)
			{
				agent = pair.Value.Owner;

				//There was a bug - the reload sound was propagated to the client via ReachargeAmmo() even when the related player was already dead.
				if (!agent.IsAlive)
					continue;

				if (!agent.IsFriend(Owner) && agent != Owner)
					continue;

				if ((agent.Position - center).sqrMagnitude > 4*4)
					continue;

				int earned = (agent.WeaponComponent as ComponentWeaponsServer).RechargeAmmo()*(agent != Owner ? 2 : 1);
				EarnedExp += earned;

				if (!Mathf.Approximately(earned, 0))
					used = true;
			}
			if (used)
				++rechargeUses;

			if (EarnedExp > 20)
			{
				PPIManager.Instance.ServerAddScoreForAmmoRecharge(Owner.NetworkView.owner);
				EarnedExp = 0;
			}

			yield return new WaitForSeconds(1.0f);
		}
		uLink.Network.Destroy(gameObject);
	}
}
