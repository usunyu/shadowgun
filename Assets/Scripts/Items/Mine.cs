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

[AddComponentMenu("Items/Projectiles/Mine")]
public class Mine : uLink.MonoBehaviour
{
	enum E_State
	{
		Activating,
		Sweeping,
		Exploded,
	}

	// class variables...
	//private static  float                           s_MaxAudioPitchMod = 0.1f;

	// object variables...
	AgentHuman m_Owner;
	public Explosion m_Explosion;
	public Vector3 m_ExplosionOffset = new Vector3(0, 0.5f, 0);
	public float m_MaxDamage;
	public float m_DamageRadius;

	public float m_DetectionDistance = 5;
	public float m_ActivationDelay = 2.0f;

//	[System.NonSerialized]
	bool m_Detected = false; //was the mine detected by a Detector gadget?

	public bool IsDetected
	{
		get { return m_Detected; }
	}

	[SerializeField] ItemIcons Icons;

	Transform Transform;
	Animation Animation;
	AudioSource Audio;
	GameObject GameObject;
	uLink.NetworkView NetworkView;

	GameObject GameObjectMine;
	GameObject GameObjectDecal;

	Collider m_WaterVolume;
	float CheckDistance;
	int HitSoundsLeft;

	E_State State;

	E_ItemID ItemID;

	bool IsClient
	{
		get { return uLink.Network.isClient; }
	}

	bool IsServer
	{
		get { return uLink.Network.isServer; }
	}

	// Use this for initialization
	void Awake()
	{
		Transform = transform;
		Animation = GetComponent<Animation>();
		Audio = GetComponent<AudioSource>();
		GameObject = gameObject;
		NetworkView = networkView;

		GameObjectMine = Transform.FindChild("MINA_top").gameObject;
		GameObjectDecal = Transform.FindChild("destroy_plane").gameObject;
		CheckDistance = m_DetectionDistance*m_DetectionDistance;

		//State = E_State.Activating;
	}

	void OnDestroy()
	{
		StopAllCoroutines();
		m_Explosion = null;

		m_Owner.GadgetsComponent.UnRegisterLiveGadget(ItemID, GameObject);
	}

	void uLink_OnNetworkInstantiate(uLink.NetworkMessageInfo info)
	{
		m_Owner = Player.GetPlayer(info.networkView.owner).Owner;

		//Transform.rotation.SetLookRotation(info.networkView.initialData.ReadVector3());

//        Transform.eulerAngles = new Vector3(-90, 0, 0); // fuck off  !!!!

		info.networkView.initialData.ReadVector3();
		ItemID = info.networkView.initialData.Read<E_ItemID>();
		m_Owner.GadgetsComponent.RegisterLiveGadget(ItemID, GameObject);

		GameObjectDecal.SetActive(false);

		//State = E_State.Sweeping;

		if (uLink.Network.isServer == false)
		{
			Icons.SetTeamIcon(m_Owner.Team);
			StartCoroutine(HideOnClient());
			return;
		}
		else
		{
			StartCoroutine(SweepForEnemy());
		}
	}

	//
	public void SetDetected(bool detected)
	{
		m_Detected = detected;

		Icons.SetTeamIcon(m_Owner.Team, !m_Detected);

		if (Client.Instance)
			Client.Instance.PlaySoundMineDetected();
	}

	IEnumerator HideOnClient()
	{
		yield return new WaitForSeconds(2.0f);
		GameObjectMine.SetActive(false);
	}

	IEnumerator SweepForEnemy()
	{
		yield return new WaitForSeconds(2.0f);

		GameObjectMine.SetActive(false);

		while (IsEnemyInRange() == false)
			yield return new WaitForSeconds(0.1f);

		GameObjectMine.SetActive(true);

		if (Animation != null && Animation.clip != null)
		{
			Animation.Play();
			Audio.Play();
			yield return new WaitForSeconds(Animation.clip.length);
		}

		Explode();

		NetworkView.RPC("ExplodeOnClient", uLink.RPCMode.Others);

		yield return new WaitForSeconds(20);
		uLink.Network.Destroy(gameObject);
	}

	public bool IsEnemyInRange()
	{
		foreach (KeyValuePair<uLink.NetworkPlayer, ComponentPlayer>  pair in Player.Players)
		{
			AgentHuman h = pair.Value.Owner;

			if (h.IsAlive == false)
				continue;

			if (m_Owner.IsFriend(h) || m_Owner == h)
				continue;

			Vector3 targetPos = h.Position;
			Vector3 dirToTarget = targetPos - Transform.position;

			if (dirToTarget.y < -0.5f || dirToTarget.y > 2.5f)
				continue;

			if (dirToTarget.sqrMagnitude > CheckDistance)
				continue;
			return true;
		}

		return false;
	}

	public void Explode()
	{
		m_Owner.GadgetsComponent.UnRegisterLiveGadget(ItemID, GameObject);
		if (m_Explosion != null)
		{
			//Explosion explosion = Object.Instantiate(m_Explosion, transform.position, transform.rotation) as Explosion;
			Explosion explosion = Mission.Instance.ExplosionCache.Get(m_Explosion,
																	  Transform.position + m_ExplosionOffset,
																	  Transform.rotation,
																	  new Transform[] {Transform, m_Owner.Transform});

			if (explosion != null)
			{
				explosion.Agent = m_Owner;
				explosion.BaseDamage = m_MaxDamage;
				explosion.damageRadius = m_DamageRadius;
				explosion.m_ItemID = ItemID;

				if (null == m_Owner)
				{
					Debug.LogWarning("### Mine.Explode() : unexpected null agent. Mine : " + this);
				}
			}
		}

		GameObjectMine.SetActive(false);
		GameObjectDecal.SetActive(true);
	}

	[uSuite.RPC]
	void ExplodeOnClient()
	{
		// hide team icon
		Icons.SetTeamIcon(E_Team.None);

		if (m_Explosion != null)
		{
			//Explosion explosion = Object.Instantiate(m_Explosion, Transform.position, Transform.rotation) as Explosion;
			Explosion explosion = Mission.Instance.ExplosionCache.Get(m_Explosion, Transform.position + m_ExplosionOffset, Transform.rotation);
			explosion.Agent = m_Owner;
			explosion.BaseDamage = 0;

			if (null == m_Owner)
			{
				Debug.LogWarning("### Mine.ExplodeOnClient() : unexpected null agent. Explosion : " + explosion);
			}
		}

		GameObjectMine.SetActive(false);
		GameObjectDecal.SetActive(true);
	}

	// mine hitted by EMP explosion
	public void OnEMPExplosionHit(ExplosionEMP Explosion)
	{
		if (m_Owner.IsFriend(Explosion.Agent))
		{
			return;
		}

		if (uLink.Network.isServer)
		{
			Explode();

			PPIManager.Instance.ServerAddScoreForMineKill(Explosion.Agent.NetworkView.owner);

			NetworkView.RPC("ExplodeOnClient", uLink.RPCMode.Others);

			uLink.Network.Destroy(gameObject);
		}
	}
}
