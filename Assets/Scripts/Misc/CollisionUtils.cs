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
using System.Collections.Generic;

public class CollisionUtils
{
	//function for sort
	public static int CompareHits(RaycastHit x, RaycastHit y)
	{
		return x.distance.CompareTo(y.distance);
	}

	// ------
	static bool IsIdenticalOrChild(GameObject parent, GameObject testedObj)
	{
		if (!parent || !testedObj)
			return false;
		else
			return (parent == testedObj) || (testedObj.transform.parent != null) && IsIdenticalOrChild(parent, testedObj.transform.parent.gameObject);
	}

	// ------
	static GameObject GetFirstCollisionOnRay(RaycastHit[] hits, GameObject ignoreGO, out Vector3 hitPoint)
	{
		//sort by distance
		if (hits.Length > 1)
			System.Array.Sort(hits, CollisionUtils.CompareHits);

		//Debug.Log("hits: " + hits.Length);

		foreach (RaycastHit hit in hits)
		{
			GameObject hitObj = hit.transform.gameObject;
			//Debug.Log("Hit: " + hit.transform.name + " layer: " + hitObj.layer);

			if (IsIdenticalOrChild(ignoreGO, hitObj))
				continue;

			// ignore trigers...
			if (hit.collider.isTrigger)
				continue;

			hitPoint = hit.point;
			return hitObj;
		}
		hitPoint = Vector3.zero;
		return null;
	}

	// ------
	public static GameObject FirstCollisionOnRay(Ray ray,
												 float distance,
												 GameObject ignoreGO,
												 out Vector3 hitPoint,
												 int layerMask = Physics.DefaultRaycastLayers)
	{
		//Debug.Log("Testin collision: " + ray + " " + ray.direction.magnitude);
		//Debug.DrawRay(ray.origin, ray.direction*useRayTestDistance, Color.white, 30); //DEBUG draw raycast
		RaycastHit[] hits;
		hits = Physics.RaycastAll(ray, distance, layerMask);

		return GetFirstCollisionOnRay(hits, ignoreGO, out hitPoint);
	}

	// ------
	public static GameObject FirstSphereCollisionOnRay(Ray ray,
													   float radius,
													   float distance,
													   GameObject ignoreGO,
													   out Vector3 hitPoint,
													   int layerMask = Physics.DefaultRaycastLayers)
	{
		//Debug.Log("Testin collision: " + ray + " " + ray.direction.magnitude);
		//Debug.DrawRay(ray.origin, ray.direction*useRayTestDistance, Color.white, 30); //DEBUG draw raycast
		RaycastHit[] hits;
		hits = Physics.SphereCastAll(ray, radius, distance, layerMask);

		return GetFirstCollisionOnRay(hits, ignoreGO, out hitPoint);
	}

	public static Vector3 GetGroundedPos(Vector3 spawnPos)
	{
		RaycastHit hit;
		if (Physics.Raycast(spawnPos + Vector3.up, -Vector3.up, out hit, 5, (int)E_LayerType.CollisionDecal))
		{
			return hit.point;
		}

		return spawnPos;
	}
}
