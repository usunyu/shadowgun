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

[AddComponentMenu("Items/Projectiles/MedKit")]
public class MedKit : uLink.MonoBehaviour
{
	public float Speed = 5;
	public float Timer = 20;
	public float HealCapacity = 100.0f;
	public float HealRate = 5;
	const float HealInterval = 0.5f;

	public int PlayHitSoundCount = 3;

	[SerializeField] ItemIcons Icons;

	int HitSoundsLeft;
	Collider WaterVolume;

	Transform Transform;
	Rigidbody Rigidbody;
	public AgentHuman Owner { get; private set; }

	int EarnedExp;

	E_ItemID ItemID;
	GameObject GameObject;
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
			GuiHUD.Instance.UnregisterMedkit(this);
		StopAllCoroutines();
		WaterVolume = null;
		CancelInvoke();
		Rigidbody.Sleep();

		Owner.GadgetsComponent.UnRegisterLiveGadget(ItemID, GameObject);
	}

	void uLink_OnNetworkInstantiate(uLink.NetworkMessageInfo info)
	{
		Owner = Player.GetPlayer(info.networkView.owner).Owner;

		if (GuiHUD.Instance)
			GuiHUD.Instance.RegisterMedkit(this);
		HitSoundsLeft = PlayHitSoundCount;

		Rigidbody.velocity = info.networkView.initialData.ReadVector3()*Speed;
		Rigidbody.SetDensity(1.5F);
		Rigidbody.WakeUp();

		ItemID = info.networkView.initialData.Read<E_ItemID>();

		if (uLink.Network.isServer)
			StartCoroutine(Heal());

		Owner.GadgetsComponent.RegisterLiveGadget(ItemID, GameObject);

		Icons.SetTeamIcon(Owner.Team);
	}

	void OnTriggerEnter(Collider other)
	{
		//Debug.Log(name + " OnCollisionEnter " + other.name);
#if UNITY_MFG && UNITY_EDITOR //we don't have FluidSurface in WebPlayer
        FluidSurface fluid = other.GetComponent<FluidSurface>();
        if(fluid != null)
        {
            WaterVolume = other;
            fluid.AddDropletAtWorldPos(Transform.position, 0.5f, +0.5f);
            ProjectileManager.Instance.PlayGrenadeHitSound(other.gameObject.layer, Transform.position);
            CombatEffectsManager.Instance.PlayHitEffect(other.gameObject, Transform.position, Vector3.up, E_ProjectileType.None, Owner  != null && Owner.IsOwner);
        }
#endif
	}

	void OnTriggerExit(Collider other)
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

	IEnumerator Heal()
	{
		yield return new WaitForSeconds(1.0f);

		float timeToEnd = Time.timeSinceLevelLoad + Timer;
		float healConsumed = 0;
		AgentHuman agent;
		while ((Time.timeSinceLevelLoad < timeToEnd) && (healConsumed < HealCapacity))
		{
			// TODO: A design question: Should we destroy such objects at the moment the owner dies? Keeping them in the level
			// but non-funcional is weird though.
			// if (Owner.IsAlive == false)
			//	break;

			Vector3 center = transform.position;
			bool anyHeal = false;

			foreach (KeyValuePair<uLink.NetworkPlayer, ComponentPlayer> pair in Player.Players)
			{
				agent = pair.Value.Owner;

				//There was a bug - the healing sound was propagated to the client via agent.Heal() even when the related player was already dead.
				if (!agent.IsAlive)
					continue;

				if (!agent.IsFriend(Owner) && Owner != agent)
					continue;

				if (agent.IsFullyHealed)
					continue;

				if ((agent.Position - center).sqrMagnitude > 4*4)
					continue;

				//Debug.Log("Heal consumed " + healConsumed + ". Healed = " + agent.IsFullyHealed + " BlackBoard.Health " + agent.BlackBoard.Health + " BlackBoard.RealMaxHealth " + agent.BlackBoard.RealMaxHealth);
				float healValue = HealRate*HealInterval;
				EarnedExp += Mathf.RoundToInt(Mathf.FloorToInt(agent.Heal(healValue)));
				anyHeal = true;
			}
			if (anyHeal)
				healConsumed += HealRate*HealInterval;

			if (EarnedExp > 20)
			{
				PPIManager.Instance.ServerAddScoreForHealing(Owner.NetworkView.owner);
				EarnedExp = 0;
			}

			yield return new WaitForSeconds(HealInterval);
		}

		uLink.Network.Destroy(gameObject);
	}
}
