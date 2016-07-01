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

// ProjectileGrenade.
// DSC  :: This class is handled different then other projectiles.. its not cached, because it use network instantiate function
[AddComponentMenu("Items/Projectiles/FlashBangProjectile")]
public class FlashBangProjectile : GrenadeProjectileBase
{
	float MinDistance;
	float MinAngle = 45.0f;

	protected override void Awake()
	{
		base.Awake();

		MinDistance = Radius*0.3f;

		bLocalExplode = false;
	}

	[uSuite.RPC]
	internal new void ExplodeOnClient(Vector3 position)
	{
		Radius = ItemSettingsManager.Instance.Get(ItemID).Range;
		MinDistance = Radius*0.4f;

		_ExplodeWorker(0, Radius, position);

		if (Player.LocalInstance == null)
		{
			return;
		}

		Vector3 pos = Player.LocalInstance.Owner.TransformEye.position;
		Vector3 dir = Player.LocalInstance.Owner.BlackBoard.FireDir;

		Vector3 dirToGrenade = position - pos;

		float dist = dirToGrenade.magnitude;

		if (dist > Radius)
		{
			// too far
			// Debug.Log("distance " + dist);
			return;
		}

		dirToGrenade.Normalize();

		float angle = Vector3.Angle(dir, dirToGrenade);
		//Debug.DrawLine(pos, pos + dirToGrenade * dist, Color.white, 10);

		int mask = ~(ObjectLayerMask.IgnoreRayCast | ObjectLayerMask.Ragdoll | ObjectLayerMask.PhysicBody | ObjectLayerMask.Hat);

		RaycastHit[] hits = Physics.RaycastAll(position, -dirToGrenade, dist - 0.2f, mask);
		//sort by distance
		if (hits.Length > 1)
		{
			System.Array.Sort(hits, CollisionUtils.CompareHits);
		}

		float intensity = 1;
		float duration = 5;

		foreach (RaycastHit hit in hits)
		{
			if (hit.transform == Transform)
				continue;

			if (hit.transform == Player.LocalInstance.Owner.Transform)
				continue;

			if (hit.collider.isTrigger)
				continue;

			if (hit.collider.isTrigger)
				continue;

#if UNITY_MFG && UNITY_EDITOR //we don't have FluidSurface in WebPlayer
			if( hit.collider.GetComponent<FluidSurface>() )
			{
				intensity *= 0.8f; // remove 20%
				continue;
			}
#endif
			if (hit.collider.GetComponent<AgentHuman>())
			{
				continue;
			}

			//Debug.Log("coll " + hit.collider.name);
			return; // hit collision
		}

		if (dist > MinDistance)
		{
			intensity *= 1 - (dist - MinDistance)/(Radius - MinDistance);
			if (intensity < 0.5f)
				intensity = 0.5f;
		}

		if (angle > MinAngle)
		{
			duration *= 1 - (angle - MinAngle)/(180 - MinAngle);

			if (duration < 0.3f)
				duration = 0.3f;
		}

		//Debug.Log("intensity " + intensity + " duration " + duration + " angle " + angle + " dist " + dist);
		MFScreenSpaceVertexGridFX.Instance.SpawnFlashbang(Color.white, intensity, duration);
	}
}
