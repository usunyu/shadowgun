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

public class ExplosionSting : Explosion
{
	protected override void ServerExplode()
	{
		//base.ServerExplode();

		if (damageRadius > 0)
		{
			ApplyStingDamage();
		}
	}

	void ApplyStingDamage()
	{
		if (uLink.Network.isServer == false)
		{
			return;
		}

		Collider[] colliders = Physics.OverlapSphere(Position, damageRadius);

		foreach (Collider collider in colliders)
		{
			AgentHuman victim = collider.GetComponent<AgentHuman>();

			if (victim == null)
				continue;

			Vector3 dir = Agent.Position - m_Transform.position;

			if (dir.sqrMagnitude > 0.1f*0.1f)
			{
				dir.Normalize();
			}
			else
				dir = Agent.Forward*-1f;

			victim.KnockDown(Agent, E_MeleeType.First, dir);
		}
	}
}
