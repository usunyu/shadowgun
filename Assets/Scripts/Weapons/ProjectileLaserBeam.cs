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

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("Weapons/Projectile Laser Beam")]
public class ProjectileLaserBeam : Projectile
{
	public float m_BeamNoFadeDuration = 1.0f;
	public float m_BeamFadeDuration = 2.0f;
	public float m_MaxBeamSize = 200.0f;

	//public Color			m_StartColor;
	//public Color			m_EndColor;

	LineRenderer m_LineRenderer;

	/*public void Awake()
	{
		//base.Awake();
		m_LineRenderer  = GetComponent<LineRenderer>();
	}*/

	public override void ProjectileInit(Vector3 pos, Vector3 dir, InitSettings inSettings)
	{
		Transform.position = pos;
		Transform.forward = dir;
		m_Forward = dir;
		m_SpawnPosition = pos;

		Settings = inSettings;
		Timer = Time.timeSinceLevelLoad;

		spawnHitEffects = true;
		ignoreThisHit = false;

		float beamSize = PerformLineHit(pos, dir, m_MaxBeamSize);

		//	Debug.Log("ProjectileLaserBeam.PerformLineHit " + beamSize);

		if (m_LineRenderer == null)
		{
			m_LineRenderer = GetComponent<LineRenderer>();
		}

		//m_LineRenderer.SetColors(m_StartColor, m_EndColor);
		if (beamSize > 0.5f && m_LineRenderer != null && m_LineRenderer)
		{
			m_LineRenderer.SetPosition(0, -Vector3.forward*beamSize);
			m_LineRenderer.SetPosition(1, Vector3.zero);
		}
	}

	public override void ProjectileUpdate()
	{
		if (Time.timeSinceLevelLoad - Timer > m_BeamNoFadeDuration && m_BeamFadeDuration > 0)
		{
			float relTime = Time.timeSinceLevelLoad - Timer - m_BeamNoFadeDuration;
			float alpha = 1 - relTime/m_BeamFadeDuration;

			Vector4 col = m_LineRenderer.material.GetVector("_TintColor");
			col.w = alpha;
			m_LineRenderer.material.SetVector("_TintColor", col);
		}
	}

	// called from projectile manager before return projectile back to cache...
	public override void ProjectileDeinit()
	{
	}

	public override bool IsFinished()
	{
		return (Time.timeSinceLevelLoad - Timer > m_BeamNoFadeDuration + m_BeamFadeDuration);
	}

	// return size of beam in [m]
	float PerformLineHit(Vector3 inFrom, Vector3 inDir, float inMaxDistance)
	{
		float lng = inMaxDistance;
		RaycastHit[] hits = Physics.RaycastAll(inFrom, inDir, lng, RayCastMask);

		if (hits.Length > 1)
		{
			System.Array.Sort(hits, CollisionUtils.CompareHits);
		}

		Transform.position = inFrom + inDir*inMaxDistance;

		foreach (RaycastHit hit in hits)
		{
			if (hit.transform == Settings.IgnoreTransform)
				continue;

			//skip the Owner of this shot when his HitZone got hit
			HitZone zone = hit.transform.GetComponent<HitZone>();
			if (zone && (zone.HitZoneOwner is AgentHuman) && (zone.HitZoneOwner as AgentHuman).transform == Settings.IgnoreTransform)
				continue;

			Transform.position = hit.point;

			if (!ValidateHitAgainstEnemy(hit))
				continue;

			hit.transform.SendMessage("OnProjectileHit", this, SendMessageOptions.DontRequireReceiver);

			if (ignoreThisHit)
			{
				ignoreThisHit = false;
				continue;
			}

			if (hit.collider.isTrigger)
				continue;

			lng = hit.distance;

			if (spawnHitEffects)
			{
				PlayHitSound(hit.transform.gameObject.layer);
				CombatEffectsManager.Instance.PlayHitEffect(hit.transform.gameObject,
															hit.point,
															hit.normal,
															ProjectileType,
															Agent != null && Agent.IsOwner);
			}

			//Debug.DrawLine(Transform.position, Transform.position + hit.normal, Color.blue, 2.0f);

			// stop beem on static collision.
			if (hit.rigidbody == null || hit.rigidbody.isKinematic == false)
				break;
		}

		return lng;
	}
}
