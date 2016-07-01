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

[AddComponentMenu("Weapons/WeaponSniper")]
public class WeaponSniper : WeaponBase
{
	const float TIME_TO_PREPARE_FOR_FIRE = 0.25f;
	float m_PrepareForFireCounter = 0;
	bool m_PrepareForFire = false;

	// -------
	public override bool PreparedForFire
	{
		get { return m_PrepareForFireCounter >= TIME_TO_PREPARE_FOR_FIRE; }
	}

	public override float PreparedForFireProgress
	{
		get
		{
			if (m_PrepareForFire)
				return Mathf.Clamp(m_PrepareForFireCounter/TIME_TO_PREPARE_FOR_FIRE, 0, 1);
			else
				return -1;
		}
	}

	// -------
	public override void PrepareForFire(bool Prepare)
	{
		if (m_PrepareForFire != Prepare)
		{
			m_PrepareForFire = Prepare;
			m_PrepareForFireCounter = 0;
		}
	}

	// -------
	public override void Fire(Vector3 direction)
	{
		Fire(TransformShot.position, direction);
		PrepareForFire(false);
	}

	// -------
	void Update()
	{
		if (m_PrepareForFire && !PreparedForFire)
		{
			m_PrepareForFireCounter += Time.deltaTime;
		}
	}
}
