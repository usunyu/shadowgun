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

// base class for grenade projectiles

using UnityEngine;
using System.Collections;

public abstract class GrenadeProjectileBase : uLink.MonoBehaviour
{
	public float Damage = 0;
	public float Radius = 4;
	public float Speed = 25;

	public Explosion Explosion;
	public Vector3 ExplosionOffset = Vector3.zero;
	public float ExplodeAfter = 2.2f;

	public int PlayHitSoundCount = 3;

	protected bool bLocalExplode = true;

	protected int HitSoundsLeft;
	protected Collider WaterVolume;

	protected Transform Transform;
	protected Rigidbody Rigidbody;

	protected AgentHuman Owner;

	protected E_ItemID ItemID;

	protected uLink.NetworkView NetworkView;

	float FlightTime;
	Vector3 Velocity = new Vector3(0, 0, 0);
	Vector3 StartPos = new Vector3(0, 0, 0);
	bool ThrowedFromCover = false;

	bool AlreadyExploded = false;

	protected virtual void Awake()
	{
		Transform = transform;
		Rigidbody = GetComponent<Rigidbody>();
		NetworkView = networkView;
	}

	void OnDestroy()
	{
		WaterVolume = null;

		CancelInvoke();

		Rigidbody.Sleep();

		Explosion = null;
	}

	// Method will found first collider on transform's hierarchy 
	// searching upwards and disable it. this is useful during throwing objects f.e.
	void PrepareIgnoredCollision(Collider collider, Transform IgnoreFrom, int Deep)
	{
		if (null == IgnoreFrom || null == collider || 0 == Deep || !collider.enabled)
		{
			return;
		}

		Collider Found = IgnoreFrom.GetComponent<Collider>();

		if (null != Found)
		{
			if (Found.enabled)
			{
				Physics.IgnoreCollision(collider, Found);
			}
		}

		PrepareIgnoredCollision(collider, IgnoreFrom.parent, Deep - 1);
	}

	void PrepareIgnoredCollisionAll(Collider collider, Transform IgnoreFrom)
	{
		if (null == collider || !collider.enabled)
		{
			return;
		}

		Collider[] Colliders = IgnoreFrom.GetComponentsInChildren<Collider>();

		foreach (Collider C in Colliders)
		{
			if (null != C && C.enabled)
			{
				Physics.IgnoreCollision(collider, C);
			}
		}
	}

	void uLink_OnNetworkInstantiate(uLink.NetworkMessageInfo info)
	{
		HitSoundsLeft = PlayHitSoundCount;
		Owner = Player.GetPlayer(info.networkView.owner).Owner;

		PrepareIgnoredCollisionAll(Rigidbody.GetComponent<Collider>(), Owner.Transform);
		Rigidbody.isKinematic = true;

		Rigidbody.SetDensity(1.5f);
		Rigidbody.WakeUp();
		Rigidbody.AddTorque(0, 10, 0);

		StartPos = Transform.position;
		Rigidbody.velocity = Velocity = info.networkView.initialData.ReadVector3()*Speed;
		ThrowedFromCover = Owner.IsInCover;
		FlightTime = 0;

		ItemID = info.networkView.initialData.Read<E_ItemID>();

		Owner.GadgetsComponent.RegisterUsedGadget(ItemID);

		AlreadyExploded = false;

		if (uLink.Network.isServer == false)
		{
			return;
		}

		Invoke("Explode", ExplodeAfter);
	}

	void FixedUpdate()
	{
		InternalUpdate(Time.fixedDeltaTime);
	}

	void InternalUpdate(float deltaTime)
	{
		if (!Rigidbody.isKinematic)
		{
			return;
		}

		bool MovementAllowed = true;
		bool CoverHit = false;

		Vector3 CurrentTheoreticalPosition = ComputeThrowPosition(StartPos, Velocity, FlightTime);

		Vector3 v = CurrentTheoreticalPosition - Transform.position;

		FlightTime += deltaTime;

		float sqrDistanceToOrigin = (StartPos - Transform.position).magnitude;

		if (!ThrowedFromCover || sqrDistanceToOrigin > 1.5f*1.5f)
		{
			RaycastHit[] hits = Physics.RaycastAll(Transform.position, v.normalized, v.magnitude, ~ (ObjectLayerMask.IgnoreRayCast));

			if (hits.Length > 0)
			{
				foreach (RaycastHit hit in hits)
				{
					//skip friends when the projectile should explode near the player (this solves the unwanted suicide when a friend suddenly enters the area in front of me)
					AgentHuman hitAgent = hit.transform.gameObject.GetFirstComponentUpward<AgentHuman>();
					if (hitAgent != null)
					{
						float dist = Vector3.Distance(Owner.Position, hitAgent.Position);

//						Debug.Log ("GHIT: hitAgent=" + hitAgent.name + ", dist=" + dist);

						if (dist < 3) //ignore only if the projectile is still within X m radius
						{
							if (Owner.IsFriend(hitAgent))
							{
//								Debug.Log ("GHIT: hitAgent=" + hitAgent.name + ", dist=" + dist + " -- FRIENDS");
								continue;
							}
//							else
//							{
//								Debug.Log ("GHIT: hitAgent=" + hitAgent.name + ", dist=" + dist + " -- ENEMIES");
//							}
						}
					}

					if (!hit.collider.transform.IsChildOf(Owner.transform))
					{
						MovementAllowed = false;

						if (null != hit.collider.transform.GetComponentInChildren<Cover>())
						{
							CoverHit = true;
						}

						break;
					}
				}
			}
		}

		if (MovementAllowed)
		{
			// movement
			Transform.position += v;
			Transform.Rotate(new Vector3(1, 1, 1), deltaTime*6.0f*Mathf.Rad2Deg);
		}
		else
		{
			Rigidbody.isKinematic = false;

			if (CoverHit)
			{
				Rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
			}

			// slow down in case of thin cover - otherwise grenade will go through the wall
			//Rigidbody.velocity = CoverHit ? 0.1f*v/deltaTime : v/deltaTime;
			// 'Velocity' is not right, it should be 'v/deltaTime' - but current physics settings is based on this
			Rigidbody.velocity = CoverHit ? 0.1f*v/deltaTime : Velocity;
			//Rigidbody.velocity = Velocity;
		}
	}

	void OnCollisionEnter(Collision collision)
	{
		// ignore collisions with owner and his transforms...
		if (collision.transform.IsChildOf(Owner.Transform))
		{
			return;
		}

		if (uLink.Network.isClient)
		{
			if (WaterVolume == null && HitSoundsLeft > 0)
			{
				HitSoundsLeft--;

				ProjectileManager.Instance.PlayGrenadeHitSound(GetComponent<Collider>().gameObject.layer, Transform.position);
			}
		}

		if (uLink.Network.isServer)
		{
			Agent Agent = collision.gameObject.GetComponent<Agent>();

			HitZone HitZone = collision.gameObject.GetComponent<HitZone>();

			//AgentHuman human = agent as AgentHuman;

			// add team shit here
			/*if (human && human.BlackBoard.GrenadesExplodeOnHit == false)
			{
			}
			else*/
			if (null != Agent) // grenade must explode immediately, if we hit player
			{
				Explode();
			}
			else if (null != HitZone)
			{
				Explode();
			}
		}
	}

	internal void OnTriggerEnter(Collider other)
	{
		//Debug.Log( name + " OnCollisionEnter " + other.name );

#if UNITY_MFG && UNITY_EDITOR //we don't have FluidSurface in WebPlayer
		FluidSurface fluid = other.GetComponent<FluidSurface>();
		
		if( fluid != null )
		{
			WaterVolume = other;
			
			if( !uLink.Network.isServer )
			{
				fluid.AddDropletAtWorldPos( Transform.position, 0.5f, +0.5f );
			}
			
			ProjectileManager.Instance.PlayGrenadeHitSound( collider.gameObject.layer, Transform.position );
			
			CombatEffectsManager.Instance.PlayHitEffect( other.gameObject, Transform.position, Vector3.up, Owner  != null && Owner.IsOwner );
		}
#endif
	}

	internal void OnTriggerExit(Collider other)
	{
		//Debug.Log( name + " OnTriggerExit " + other.name );

		if (other != null && WaterVolume == other)
		{
			WaterVolume = null;
		}
	}

	internal void Explode()
	{
		//This boolean variable prevents us from calling some important parts of the code more than once.
		//There are two problems here: The first problem is that the Explode function is called from OnCollisionEnter() which is called from physics thus
		//independently from Update (it is powered by FixedUpdate() instead). The second one is that uLink.Network.Destroy() calls
		//internally Object.Destroy() which postpones the object destruction to the end of the current frame. Thus OnCollisionEnter() and subsequently Explode()
		//can be called several times before the object is really destroyed.
		//
		//It is difficult to say what is really wrong here. Probably the fact that such notifications like OnCollisionEnter() are still called on a object
		//which is already marked to be destroyed. We shouldask Unity engine architect about the correct solution in such situation. (how to destroy object from within
		//physics/FixedUpdate callback)

		if (AlreadyExploded)
			return;

		AlreadyExploded = true;
		CancelInvoke("Explode");

		if (bLocalExplode)
		{
			_ExplodeWorker(Damage, Radius, Transform.position);
		}

		if (uLink.Network.isServer)
		{
			NetworkView.RPC("ExplodeOnClient", uLink.RPCMode.Others, Transform.position);
		}

		uLink.Network.Destroy(gameObject);
	}

	[uSuite.RPC]
	internal void ExplodeOnClient(Vector3 position)
	{
		_ExplodeWorker(0, Radius, position);
	}

	protected Explosion _ExplodeWorker(float _Damage, float _Radius, Vector3 position)
	{
		Explosion explosion = null;

		if (WaterVolume != null)
		{
			explosion = Mission.Instance.ExplosionCache.GetWaterExplosion(Transform.position + ExplosionOffset, Quaternion.identity
							/* Transform.rotation */);

			if (explosion != null)
			{
				explosion.Agent = Owner;
				explosion.BaseDamage = _Damage;
				explosion.damageRadius = _Radius;
			}
		}
		else if (Explosion != null)
		{
			explosion = Mission.Instance.ExplosionCache.Get(Explosion,
															position + ExplosionOffset,
															Quaternion.identity /* Transform.rotation */,
															new Transform[] {Transform, Owner.Transform});

			if (explosion != null)
			{
				explosion.Agent = Owner;

				explosion.BaseDamage = _Damage;
				explosion.damageRadius = _Radius;
				explosion.m_ItemID = ItemID;
			}
		}

		if (null != explosion && null == Owner)
		{
			Debug.LogWarning("### GrenadeProjectileBase._ExplodeWorker() : unexpected null agent. Explosion : " + explosion);
		}

		return explosion;
	}

	public static Vector3 ComputeThrowPosition(Vector3 SrcPos, Vector3 Velocity, float ElapsedTime)
	{
		return SrcPos + ElapsedTime*Velocity + 0.5f*(ElapsedTime*ElapsedTime)*Physics.gravity;
	}
}
