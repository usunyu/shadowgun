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

[RequireComponent(typeof (SphereCollider))]
[RequireComponent(typeof (Animation))]
[RequireComponent(typeof (AudioSource))]
[RequireComponent(typeof (Rigidbody))]
[RequireComponent(typeof (NavMeshAgent))]
public class Spider : Agent
{
	[System.Serializable]
	public class AnimInfo
	{
		public AnimationClip Idle;
		public AnimationClip Move;
	}

	[System.Serializable]
	public class SoundInfo
	{
		public AudioClip Moving;
		public AudioClip Activate;
	}

	public enum State
	{
		None,
		Spawning,
		Waiting,
		Attack,
		Destroyed
	};

	//-- Members ----------------------------------------------------------------
	State m_State = State.None;
	AgentHuman m_Target;
	E_Team Team = E_Team.None;

	public float m_HitPoints = 50.0f;
	float m_DefaultHitPoints;
	[System.NonSerialized] public float m_DetectionDistance = 500.0f;
	//private 		float 				m_DefaultDetectionDistance;
	public float m_ActivationDistance = 5.0f;
	public float m_ExplosionTimeout = 5.0f;

	public Explosion m_Explosion;
	public Vector3 m_ExplosionOffset = Vector3.zero;

	public AnimInfo m_AnimInfo;
	public SoundInfo m_SoundInfo;

	NavMeshAgent m_NavMeshAgent;
	Vector3 m_MoveTargetPos;
	float m_AgentRadius = 0.5f;

	Animation m_Animations;
	AudioSource m_Audio;
	static string m_SpawnAnimName = "SpawnAnim";

	//agent overrides
	public virtual bool IsPlayer
	{
		get { return false; }
	}

	public override bool IsAlive
	{
		get { return m_HitPoints > 0; }
	}

	public override bool IsVisible
	{
		get { return true; }
	}

	public override bool IsInvulnerable
	{
		get { return false; }
	}

	public override bool IsInCover
	{
		get { return false; }
	}

	public override bool IsEnteringToCover
	{
		get { return false; }
	}

	public override Vector3 ChestPosition
	{
		get { return Position; }
	}

	public override bool IsFriend(AgentHuman target)
	{
		return (target.Team == Team);
	}

	public override void KnockDown(AgentHuman humanAttacker, E_MeleeType meleeType, Vector3 direction)
	{
	}

	// Use this for initialization
	void Awake()
	{
		base.Initialize();
		m_DefaultHitPoints = m_HitPoints;
		m_DetectionDistance = 500.0f;
		//m_DefaultDetectionDistance 	= m_DefaultHitPoints;

		m_NavMeshAgent = GetComponentInChildren<NavMeshAgent>();
		m_Animations = GetComponent<Animation>();
		m_Animations.SyncLayer(0);

		m_Audio = GetComponent<AudioSource>();
		if (m_SoundInfo.Moving != null)
		{
			//m_Audio.Stop();
			m_Audio.clip = m_SoundInfo.Moving;
			m_Audio.loop = true;
		}
	}

	// Start is called just before any of the
	// Update methods is called the first time.
	void Start()
	{
		///beny: SG1
/*		
        if(m_Target == null)
        {
            m_Target = Player.Instance.Owner;
        }

        if (Game.Instance.GameDifficulty == E_GameDifficulty.Easy)
        {
            m_NavMeshAgent.speed *= 0.8f;
        }
        else if (Game.Instance.GameDifficulty == E_GameDifficulty.Hard)
        {
            m_NavMeshAgent.speed *= 1.2f;
        }
*/
	}

	void Activate(SpawnPoint spawn)
	{
		//Debug.Log("Spider:Activate BEG " + name + " " + m_HitPoints);
		Transform.position = spawn.Transform.position; // + Vector3.up;
		Transform.rotation = spawn.Transform.rotation;

		m_HitPoints = m_DefaultHitPoints;
		m_Animations.playAutomatically = false;

		///beny: SG1
/*		
        SpawnPointEnemy spEnemy = spawn as SpawnPointEnemy;
        if(spEnemy != null && spEnemy.InitState == SpawnPointEnemy.E_InitState.PlayAnim)
        {
			if(animation.GetClip(m_SpawnAnimName) == null)
	            animation.AddClip(spEnemy.SpawnAnimation, m_SpawnAnimName);
            m_Animations.Play(m_SpawnAnimName);
            m_State     = State.Spawning;
        }
        else
        {
            m_State     = State.Waiting;
        }
*/

		//m_DetectionDistance = Mathf.Max(spEnemy.InitSightRange, m_DefaultDetectionDistance);

		//Debug.Log("Spider:Activate END " + name + " " + m_HitPoints);
	}

	// This function is called when the object is returned to cache
	// 

	void Deactivate()
	{
		//Debug.Log("Spider:Deactivate BEG " + name + " " + m_HitPoints);		
		GetComponent<AudioSource>().Stop();

		GetComponent<Animation>().Stop();
		GetComponent<Animation>().playAutomatically = false;

		m_NavMeshAgent.enabled = false;

		m_State = State.None;
		m_HitPoints = 0;
		StopAllCoroutines();
		CancelInvoke();
		//Debug.Log("Spider:Deactivate END " + name + " " + m_HitPoints);		
	}

	//TODO: implement this method properly, right now it's just to resolve a 'm_Target is always null' warning
	void SearchForTarget()
	{
		AgentHuman agent = null;

		//enumerate surrounding enemies and find the best target
		//...

		//
		m_Target = agent;
	}

	// Update is called every frame, if the
	// MonoBehaviour is enabled.
	void Update()
	{
		// no target no action...
		if (m_Target == null || Time.deltaTime <= Mathf.Epsilon)
			return;

		Vector3 actTargetPos = m_Target.transform.position;
		Vector3 dirToTarget = actTargetPos - transform.position;
		dirToTarget.y = 0;
		if (m_State == State.Spawning)
		{
			if (m_Animations[m_SpawnAnimName].enabled && (m_Animations[m_SpawnAnimName].length - m_Animations[m_SpawnAnimName].time) > 0.3f)
				return;

			m_State = State.Waiting;
		}
		else if (m_State == State.Waiting)
		{
			if ((dirToTarget.magnitude - m_AgentRadius) < m_DetectionDistance)
			{
				m_MoveTargetPos = actTargetPos;

				m_NavMeshAgent.enabled = true;
				m_NavMeshAgent.SetDestination(m_MoveTargetPos);

				m_State = State.Attack;
			}
		}

		else if (m_State == State.Attack)
		{
			if (!IsInvoking("SpawnExplosion") && (dirToTarget.magnitude - m_AgentRadius) < m_ActivationDistance)
			{
				//SpawnExplosion();
				Invoke("SpawnExplosion", m_ExplosionTimeout);

				if (m_SoundInfo.Activate != null)
				{
					GetComponent<AudioSource>().PlayOneShot(m_SoundInfo.Activate);
				}
			}
			else if ((dirToTarget.magnitude - m_AgentRadius) < 0.5f)
			{
				SpawnExplosion(false);
			}

			if (m_State == State.Attack)
			{
				Vector3 diff = actTargetPos - m_MoveTargetPos;
				diff.y = 0;
				if (diff.magnitude > 0.5f)
				{
					m_MoveTargetPos = actTargetPos;
					m_NavMeshAgent.SetDestination(m_MoveTargetPos);
				}

				Vector3 vec_velocity = m_NavMeshAgent.velocity;
				vec_velocity.y = 0;
				float abs_velocity = vec_velocity.magnitude;
				if (abs_velocity > 0.1f)
				{
					if (false == m_Audio.isPlaying)
						m_Audio.Play();

					if (false == m_Animations.IsPlaying(m_AnimInfo.Move.name))
						m_Animations.CrossFade(m_AnimInfo.Move.name);
				}
				else
				{
					if (false == m_Audio.isPlaying)
						m_Audio.Stop();

					if (m_AnimInfo.Idle == null)
					{
						m_Animations.Stop(m_AnimInfo.Move.name);
					}
					else if (false == m_Animations.IsPlaying(m_AnimInfo.Idle.name))
						m_Animations.CrossFade(m_AnimInfo.Idle.name);
				}
			}
		}
	}

	// This function is called when the MonoBehaviour
	// will be destroyed.
	void OnDestroy()
	{
	}

	// Implement this OnDrawGizmos if you want
	// to draw gizmos that are also pickable
	// and always drawn.
	void OnDrawGizmos()
	{
	}

	public virtual void OnReceiveRangeDamage(Agent attacker, float damage, Vector3 impuls, E_WeaponType weaponType)
	{
		//Debug.Log("OnProjectileHit " + name);
		// this object was already destroyed, or is undestroyable...
		if (m_HitPoints <= 0 || m_State == State.Destroyed || m_State == State.Spawning)
			return;

		// Decrease health by projectile damege. And if health will be still positive ( >0 ) finish.
		m_HitPoints -= damage;
		//Debug.Log("OnProjectileHit " + name + " " + m_HitPoints);
		if (m_HitPoints > 0)
		{
			if (m_State != State.Attack)
			{
				m_MoveTargetPos = m_Target.transform.position;

				m_NavMeshAgent.enabled = true;
				m_NavMeshAgent.SetDestination(m_MoveTargetPos);

				m_State = State.Attack;
			}
			return;
		}

		SpawnExplosion(true);
	}

	public void OnProjectileHit(Projectile projectile)
	{
		//Debug.Log("OnProjectileHit " + name);
		// this object was already destroyed, or is undestroyable...
		if (m_HitPoints <= 0 || m_State == State.Destroyed || m_State == State.Spawning)
			return;

		// Decrease health by projectile damege. And if health will be still positive ( >0 ) finish.
		m_HitPoints -= projectile.Damage;
		//Debug.Log("OnProjectileHit " + name + " " + m_HitPoints);
		if (m_HitPoints > 0)
		{
			if (m_State != State.Attack)
			{
				m_MoveTargetPos = m_Target.transform.position;

				m_NavMeshAgent.enabled = true;
				m_NavMeshAgent.SetDestination(m_MoveTargetPos);

				m_State = State.Attack;
			}
			return;
		}

		SpawnExplosion(true);
	}

	void SpawnExplosion()
	{
		SpawnExplosion(false);
	}

	void SpawnExplosion(bool inClearDamage)
	{
		CancelInvoke("SpawnExplosion");

		// Spawn explosion ...
		if (m_Explosion != null)
		{
			//Explosion explosion = Object.Instantiate(m_Explosion, transform.position, transform.rotation) as Explosion;
			Explosion explosion = Mission.Instance.ExplosionCache.Get(m_Explosion, transform.position + m_ExplosionOffset, transform.rotation);
			if (inClearDamage == true)
			{
				///beny: SG1                explosion.damage = 0;
				explosion.BaseDamage = 0;
			}
		}

		// Setup correct inner state to stop reacting.
		m_State = State.Destroyed;

		// destroy owner gameobject..
		//Destroy(gameObject);
		GetComponent<Animation>().Stop();
		GetComponent<Animation>().playAutomatically = false;
		///beny: SG1		Mission.Instance.ReturnAgent(gameObject);
	}
}
