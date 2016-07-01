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

//#define DEBUG

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//TODO Effects ::
//- DONE - Particle
//- Explosion sound
//- Camera anim
//- ForceFeedback ??
//- Dynamic Light
//- PostProcess/Vertex effect
//TODO functionalitz
//- Radius/Curve Damage
//- Cache for explosion ??

// NOTE :
///     Start(), OnEnable(), m_StartCalled
/// This messy code is here due to calling order of this function.
/// Start is called only once. so we can't use it as start point for playing / runing explosions which are cached.
/// OnEnable is called imidiately after activating GO or component.
///     Unfortunately this happen during initialization phase of explosiones in cache .
///     Object.Instatiate is activating all components of new instance and this is not wanted...
///
[RequireComponent(typeof (AudioSource))]
public class Explosion : MonoBehaviour
{
#if DEBUG
	static Color ColExp = new Color(1.0f, 0.5f, 0.2f);
	static Color ColHit = new Color(0.4f, 1.0f, 0.4f);
	static Color ColMiss = new Color(1.0f, 0.4f, 0.4f);
#endif

	// Damage properties...
	[SerializeField] float m_DamageRadius = 5.0F;
	[SerializeField] float m_MaxDamage = 5.0F;

	public E_ItemID m_ItemID = E_ItemID.None;
	public E_WeaponID m_WeaponID = E_WeaponID.None;
	public float m_WeaponImpulse = 1; //this is used to multiplicate the resulting impulse; weapon can pass its impulse setting through this

	public AgentHuman Agent { get; set; }
	public float BaseDamage { get; set; }
	public float damageRadius { get; set; }
	public Transform[] noBlocking { get; set; }

	// Wave FX effect...
	public bool m_GenerateWaveFX = false;
	public float m_WaveFXDelay = 0.0f;
	public float m_WaveFXAmplitudeMin = 1.0F;
	public float m_WaveFXAmplitudeMax = 1.0F;
	public float m_WaveFXFreq = 20;
	public float m_WaveFXDuration = 1.5F;
	public float m_WaveFXRadiusMin = 0.1F;
	public float m_WaveFXRadiusMax = 1;
	public float m_WaveFXSpeed = 1.3F;
	public float m_WaveFXMaxWrldDist = 30;

	public float m_ParticleCriticalDistance = 5.0f;

	// internal members...
	protected GameObject m_GameObject;
	protected Transform m_Transform;
	ParticleSystem[] m_Emitters;
	bool m_Exploded = false;

	// categorized victims
	static List<AgentHuman> m_Agents = new List<AgentHuman>(16);
	static List<Collider> m_Others = new List<Collider>(16);

	Vector3 m_ObstacleNormalAccum;
	public float m_ObstacleDamageReduction = 0.15f;

	//this member is used for explosion chaching, don't change it...
	public Explosion cacheKey { get; set; }

	public float Damage { get; private set; }
	public Vector3 Impulse { get; private set; }

	public Vector3 Position
	{
		get { return m_Transform.position; }
	}

	// ==================================================================================================
	// === Default MoneBehaviour interface ==============================================================

	void Awake()
	{
		m_GameObject = gameObject;
		m_Transform = m_GameObject.transform;
		m_Emitters = m_GameObject.GetComponentsInChildren<ParticleSystem>();
		BaseDamage = m_MaxDamage;
		damageRadius = m_DamageRadius;
	}

	void OnDestroy()
	{
		m_Emitters = null;
	}

	void Update()
	{
		// Do explosion in first tick...
		if (m_Exploded == false)
		{
			if (uLink.Network.isServer)
				ServerExplode();

			if (uLink.Network.isClient)
				ClientExplode();

			m_Exploded = true;
			return;
		}

		// test if any of particles are still active...
		bool anyEmiterPlaying = false;

		foreach (ParticleSystem em in m_Emitters)
		{
			if (null != em)
			{
				if (em.isPlaying || em.particleCount > 0)
				{
					anyEmiterPlaying = true;
					break;
				}
			}
			else
			{
				Debug.LogError(
							   "NULL in m_Emmiters! Check the prefab for this explosion - probably the Explosion script is added more times in the prefab hierarchy.");
				continue;
			}
		}

		// If all effects are done. deactivate this explosion object...
		if (!anyEmiterPlaying && GetComponent<AudioSource>().isPlaying == false)
		{
			CleanUp();
		}
	}

	void OnEnable()
	{
	}

	void OnDisable()
	{
	}

//	void OnDrawGizmos()
//	{
//		Gizmos.color = Color.red;
//		Gizmos.DrawWireSphere(transform.position, m_ExplosionRadius);
//	}

	// ==================================================================================================
	// === INTERNAL =====================================================================================

	protected virtual void ServerExplode()
	{
		// Apply damage if ane exist...
		if (damageRadius > 0 && BaseDamage > 0)
			ApplyDamage();
	}

	// ==================================================================================================
	// === INTERNAL =====================================================================================

	void ClientExplode()
	{
		// Check if this explosion is
		MFDebugUtils.Assert(m_Exploded == false);

		if (GetComponent<AudioSource>() != null && GetComponent<AudioSource>().clip != null)
			GetComponent<AudioSource>().Play();

		Frustum.SetupCamera(GameCamera.Instance);
		Frustum.E_Location loc = Frustum.PointInFrustum(Position);

		if (loc == Frustum.E_Location.Outside) //do not spawn the projectile if it's not visible at all
			return;

		// Run particles only when explosion is longer as is m_ParticleCriticalDistance.
		// Reason : Particles from explosion are full scree when explosion is too near, and
		//          full-screen particles are too slow. So we don't run them.
#if !UNITY_EDITOR
		if (m_ParticleCriticalDistance < 0 || Camera.main == null || Vector3.Magnitude(Camera.main.transform.position - Position) > m_ParticleCriticalDistance)
#endif
		{
			// Run all particles...
			foreach (ParticleSystem em in m_Emitters)
			{
				if (null != em)
					em.Play();
			}
		}

		//if (audio != null && audio.clip != null)
		//	audio.Play();

		// Do screen fx...
		if (m_GenerateWaveFX && CamExplosionFXMgr.Instance)
		{
			GenerateWaveFX();
		}
	}

	public void CleanUp()
	{
		// handle caching...
		if (cacheKey != null)
		{
			Mission.Instance.ExplosionCache.Return(this);
		}
		else
		{
			Destroy(gameObject);
		}
	}

	public void Reset()
	{
		foreach (ParticleSystem em in m_Emitters)
		{
			if (null != em)
			{
				em.Stop();
			}
		}

		GetComponent<AudioSource>().Stop();

		m_Exploded = false;
		Agent = null;
		BaseDamage = m_MaxDamage;
		damageRadius = m_DamageRadius;
		noBlocking = null;

		m_WeaponID = E_WeaponID.None;
		m_ItemID = E_ItemID.None;
	}

	void ApplyDamage()
	{
		if (uLink.Network.isServer == false)
			return;

		// enumerate victims...
		Vector3 tmp;
		Vector3 pos = Position;
		int mask = ~ (ObjectLayerMask.IgnoreRayCast | ObjectLayerMask.Ragdoll | ObjectLayerMask.Hat);
		Collider[] colliders = Physics.OverlapSphere(Position, damageRadius, mask);

#if DEBUG
		DebugDraw.DepthTest = true;
		DebugDraw.DisplayTime = 8.0f;
		DebugDraw.Diamond(ColExp, 0.04f, pos);
#endif

		// categorize victims...
		foreach (Collider c in colliders)
		{
			AgentHuman a = c.gameObject.GetFirstComponentUpward<AgentHuman>();

			if (a == null)
			{
				m_Others.Add(c);
			}
			else if (m_Agents.Contains(a) == false)
			{
				m_Agents.Add(a);
			}
		}

		// process human-agents...
		foreach (AgentHuman a in m_Agents)
		{
			m_ObstacleNormalAccum = Vector3.zero;

			a.SampleDominantAnim();

			if (ApplyDamage(a, pos, 1.0f) == false)
			{
				if ((m_ObstacleDamageReduction > 0.0f) && (m_ObstacleNormalAccum.sqrMagnitude > 0.0f))
				{
					Vector3 u = Vector3.up;
					Vector3 v = Vector3.zero;
					Vector3 n = m_ObstacleNormalAccum;

					Vector3.OrthoNormalize(ref n, ref u, ref v);

					tmp = pos;
					tmp += 0.5f*n;
					//	tmp += 1.0f * u;

					Vector3 diff = a.Position - pos;
					float dot = Vector3.Dot(diff, v);

					//	if (Mathf.Abs(dot) > 0.3f)
					{
						tmp += 0.5f*Mathf.Sign(dot)*v;
					}

#if DEBUG
					DebugDraw.LineOriented(ColExp, pos, tmp, 0.04f);
#endif

					ApplyDamage(a, tmp, m_ObstacleDamageReduction);
				}
			}
		}

		// process other objects...
		foreach (Collider c in m_Others)
		{
			ApplyDamage(c, pos);
		}

		// clean-up...
		m_Agents.Clear();
		m_Others.Clear();
	}

	// for human agents
	bool ApplyDamage(AgentHuman inAgent, Vector3 inExplosionPos, float inDmgMultiplier)
	{
		Vector3 tmp = ClosestPoint.PointBounds(inExplosionPos, inAgent.CharacterController.bounds);
		float dist = (inExplosionPos - tmp).sqrMagnitude;

		if (dist > damageRadius*damageRadius)
			return false;

#if DEBUG
		DebugDraw.Diamond(Color.grey, 0.02f, tmp);
		DebugDraw.LineOriented(Color.grey, inExplosionPos, tmp, 0.04f);
#endif

		int idx = inAgent.ExplosionHitTargets != null ? inAgent.ExplosionHitTargets.Length : 0;

		while (idx-- > 0)
		{
			if (CheckHit(inAgent.ExplosionHitTargets[idx], inExplosionPos, ref tmp))
			{
				float coef = 1.0f - Mathf.Clamp01(Mathf.Sqrt(dist)/damageRadius);

				Damage = BaseDamage*coef*inDmgMultiplier;
				Impulse = tmp;

				inAgent.SendMessage("OnExplosionHit", this, SendMessageOptions.DontRequireReceiver);

				return true;
			}
		}

		return false;
	}

	// for human agents
	bool CheckHit(Collider inCollider, Vector3 inExplosionPos, ref Vector3 outImpulse)
	{
		Vector3 closestPoint = ClosestPoint.PointBounds(inExplosionPos, inCollider.bounds);

		if (IsCollisionBetween(inExplosionPos, closestPoint, inCollider))
		{
			return false;
		}

		outImpulse = inCollider.bounds.center - inExplosionPos;
		outImpulse.y = closestPoint.y - inExplosionPos.y;
		outImpulse.Normalize();
		outImpulse *= m_WeaponImpulse;

		return true;
	}

	// for others
	void ApplyDamage(Collider inCollider, Vector3 inExplosionPos)
	{
		Vector3 imp = Vector3.zero;

		Damage = ComputeDamage(inCollider, inExplosionPos, ref imp);

		if (Damage > 0.0f)
		{
			Impulse = imp;

			inCollider.SendMessage("OnExplosionHit", this, SendMessageOptions.DontRequireReceiver);
		}
	}

	// for others
	float ComputeDamage(Collider inVictim, Vector3 inExplosionPos, ref Vector3 outImpulse)
	{
		Vector3 closestPoint = ClosestPoint.PointBounds(inExplosionPos, inVictim.bounds);
		float distance = Vector3.Distance(closestPoint, inExplosionPos);

		if ((distance >= damageRadius) || (IsCollisionBetween(inExplosionPos, closestPoint, inVictim) == true))
		{
			return -1.0f;
		}

		float coef = 1.0f - Mathf.Clamp01(distance/damageRadius);
		float dmg = BaseDamage*coef;

		outImpulse = inVictim.bounds.center - inExplosionPos;
		outImpulse.y = closestPoint.y - inExplosionPos.y;
		outImpulse.Normalize();
		outImpulse *= m_WeaponImpulse;

		return dmg;
	}

	bool IsCollisionBetween(Vector3 inFrom, Vector3 inTo, Collider inVictim)
	{
		Vector3 dir = inTo - inFrom;
		float lng = dir.magnitude;

		dir /= lng;
		inFrom -= dir*0.04f;

		int mask = ~(ObjectLayerMask.IgnoreRayCast | ObjectLayerMask.Ragdoll | ObjectLayerMask.PhysicBody | ObjectLayerMask.Hat);
		RaycastHit[] hits = Physics.RaycastAll(inFrom, dir, lng, mask);

		if (hits.Length > 1)
		{
			System.Array.Sort(hits, CollisionUtils.CompareHits);
		}

		foreach (RaycastHit hit in hits)
		{
			if ((hit.collider.isTrigger == true) || (hit.collider == inVictim))
				continue;

			if (noBlocking != null && IgnoreInBlockingTest(hit.collider.transform) == true)
				continue;

			m_ObstacleNormalAccum += hit.normal;

#if DEBUG
			DebugDraw.LineOriented(ColMiss, inFrom, inTo);
			DebugDraw.Collider(ColMiss, inVictim, 0.96f);
			DebugDraw.Diamond(ColMiss, 0.02f, hit.point);
#endif

			return true;
		}

#if DEBUG
		DebugDraw.LineOriented(ColHit, inFrom, inTo);
		DebugDraw.Collider(ColHit, inVictim, 0.96f);
#endif

		return false;
	}

	bool IgnoreInBlockingTest(Transform inTransform)
	{
		foreach (Transform tr in noBlocking)
		{
			if (tr == inTransform)
				return true;
		}
		return false;
	}

	void GenerateWaveFX()
	{
		if (Camera.main != null)
		{
			Vector3 cameraPos = Camera.main.transform.position;
			Vector3 dir = Position - cameraPos;
			float wrldDist = (dir).magnitude;

			if (Physics.Raycast(cameraPos, dir.normalized, wrldDist))
				return;

			float att = Mathf.Min(wrldDist/m_WaveFXMaxWrldDist, 1);

			MFExplosionPostFX.S_WaveParams waveParams;

			waveParams.m_Amplitude = Mathf.Lerp(m_WaveFXAmplitudeMax, m_WaveFXAmplitudeMin, att);
			waveParams.m_Duration = m_WaveFXDuration;
			waveParams.m_Freq = m_WaveFXFreq;
			waveParams.m_Speed = m_WaveFXSpeed;
			waveParams.m_Radius = Mathf.Lerp(m_WaveFXRadiusMax, m_WaveFXRadiusMin, att);
			waveParams.m_Delay = m_WaveFXDelay;

			CamExplosionFXMgr.Instance.SpawnExplosionWaveFX(Position, waveParams, m_WaveFXDelay);

			//	Debug.Log("Att : " + att + " Dist " + wrldDist + " radius " + waveParams.m_Radius + " amp " + waveParams.m_Amplitude);
		}
	}
}
