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

public class SensorEyesPlayer : SensorBase
{
	public float EyeRange = 6;
	public float FieldOfView = 120;
	public float MaxYDelta = 0.65f;
	public Transform CenterTransform;

	float sqrEyeRange
	{
		get { return EyeRange*EyeRange; }
	}

	// Use this for initialization
	public SensorEyesPlayer(AgentHuman owner) : base(owner)
	{
		CenterTransform = Owner.transform.Find("CameraTargetDir");
	}

	// Update is called once per frame
	public override void Update()
	{
		if (Owner.IsBusy || Owner.IsInCover || Owner.IsEnteringToCover || Owner.IsLeavingToCover || !Owner.IsAlive)
		{
			Owner.BlackBoard.Desires.MeleeTarget = null;
			return;
		}

		Vector3 FromDirection;
		Vector3 FromPosition;

		// new version of melee target (testing)
		if (null != GameCamera.Instance)
		{
			FromDirection = GameCamera.Instance.CameraForward;
			FromPosition = CenterTransform.position;

			RaycastHit[] hits = Physics.RaycastAll(FromPosition, FromDirection, 3.0f);
			//Debug.DrawLine(FromPosition, FromPosition + FromDirection*3.5f, Color.red, 1);

			//sort by distance
			if (hits.Length > 1)
			{
				System.Array.Sort(hits, CollisionUtils.CompareHits);
			}

			foreach (RaycastHit hit in hits)
			{
				//Debug.DrawLine( hit.point, hit.point + hit.normal*0.1f );

				if (hit.transform.IsChildOf(Owner.Transform))
				{
					continue;
				}

				GameObject hitObj = hit.transform.gameObject;

				SentryGun sentryGun = hitObj.GetComponent<SentryGun>();

				if (null != sentryGun)
				{
					if (sentryGun.IsAlive == true)
					{
						Owner.BlackBoard.Desires.MeleeTarget = sentryGun;
						return;
					}
				}

				AgentHuman Human = hitObj.GetComponent<AgentHuman>();

				if (null != Human)
				{
					if (Human.IsAlive == false || Human.IsInCover || Human.IsLeavingToCover || Human.IsEnteringToCover || Human.IsInKnockdown ||
						Owner.IsFriend(Human))
						continue;

					float y_delta = Mathf.Abs(Human.Position.y - Owner.Position.y);

					// TODO : add more sophisticated test (like sweep test against target)
					if (y_delta < MaxYDelta)
					{
						Owner.BlackBoard.Desires.MeleeTarget = Human;

						//				Debug.Log("Find human by direct test" + Human.name);
						return;
					}
				}

				// wall hit etc .. 
				break;
			}

			//second test
			AgentHuman bestP = null;

			float bestD = 3.5f*3.5f;
			float bestA = 30.0f;

			FromPosition = CenterTransform.position;
			FromDirection.y = 0;
			FromDirection.Normalize();

			//	Debug.DrawLine(FromPosition, FromPosition + FromDirection, Color.red, 1);

			int rayCastMask = ~(ObjectLayerMask.IgnoreRayCast | ObjectLayerMask.Ragdoll | ObjectLayerMask.Hat);
			RaycastHit rayCastHit = new RaycastHit();

			foreach (KeyValuePair<uLink.NetworkPlayer, ComponentPlayer> pair in Player.Players)
			{
				AgentHuman agent = pair.Value.Owner;

				if (agent.IsAlive == false || agent.IsInCover || agent.IsLeavingToCover || agent.IsEnteringToCover || agent.IsInKnockdown ||
					Owner.IsFriend(agent))
					continue;

				if (agent == Owner)
					continue;

				Vector3 dirToTarget = agent.ChestPosition - FromPosition;

				if (Mathf.Abs(dirToTarget.y) > MaxYDelta)
					continue; //too big hight diff

				dirToTarget.y = 0;

				float d = dirToTarget.sqrMagnitude;
				if (d > bestD)
					continue; // too far

				dirToTarget.Normalize();
				float a = Vector3.Angle(FromDirection, dirToTarget);

				//		Debug.DrawLine(FromPosition, FromPosition + dirToTarget, Color.white, 1);

				if (a > bestA)
					continue; // not looking at him

				dirToTarget = agent.ChestPosition - FromPosition;
				if (Physics.Raycast(FromPosition, dirToTarget.normalized, out rayCastHit, dirToTarget.magnitude, rayCastMask) == false)
					continue;

				if (agent != rayCastHit.transform.GetComponent<AgentHuman>())
					continue;

				bestP = agent;
				bestD = d;
				bestA = a;
			}

			//		if(bestP != null)
			//			Debug.Log("Find human by coll test" + bestP.name);

			Owner.BlackBoard.Desires.MeleeTarget = bestP;
		}
	}

	public override void Reset()
	{
		Owner.BlackBoard.Desires.MeleeTarget = null;
	}
}
