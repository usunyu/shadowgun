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


#if !DEADZONE_CLIENT

using UnityEngine;

public class ServerAnticheat : MonoBehaviour
{
	public static bool IsTurnedOn
	{
		get { return false; }
	}
	
	public static bool GlobalEventLoggingEnabled;

	public bool DevelopMode = false;

	internal static void ReportPotentialCheatAttempt(string id, string description, uLink.NetworkPlayer player)
	{
	}

#region The real anticheat interface
	
	public static bool ReportAndValidateAttack(uLink.NetworkPlayer player, WeaponBase weapon, Vector3 fromPos, Vector3 dir, uLink.NetworkMessageInfo info)
	{
		return true;
	}
	
	public static void ReportReload(uLink.NetworkPlayer player, uLink.NetworkMessageInfo info)
	{
	}
	
	public static void ReportTeamCmd(uLink.NetworkPlayer player, E_CommandID id, uLink.NetworkMessageInfo info)
	{
	}
	
	public static void ReportCoverEnter(uLink.NetworkPlayer player, Cover cover, E_CoverDirection coverDirection, uLink.NetworkMessageInfo info)
	{
	}
	
	public static void ReportCoverLeave(uLink.NetworkPlayer player, AgentActionCoverLeave.E_Type typeOfLeave, uLink.NetworkMessageInfo info)
	{
	}
	
	public static void ReportCoverFireStart(uLink.NetworkPlayer player, E_CoverPose pose, E_CoverDirection direction, uLink.NetworkMessageInfo info)
	{
	}
	
	public static void ReportCoverFireStop(uLink.NetworkPlayer player, uLink.NetworkMessageInfo info)
	{
	}
	
	public static void ReportChangeWeapon(uLink.NetworkPlayer player, E_WeaponID weapon, uLink.NetworkMessageInfo info)
	{
	}
	
	public static void ReportRoll(uLink.NetworkPlayer player, E_Direction direction, uLink.NetworkMessageInfo info)
	{
	}
	
	public static void ReportUseItemInCover(uLink.NetworkPlayer player, E_ItemID gadget, E_CoverPose coverPose, E_CoverDirection coverDirection, uLink.NetworkMessageInfo info)
	{
	}
	
	public static void ReportUseItem(uLink.NetworkPlayer player, E_ItemID gadget, bool keepMotion, uLink.NetworkMessageInfo info)
	{
	}
	
	public static bool ReportAndValidateMelee(uLink.NetworkPlayer player, AgentHuman attacker, Agent target, uLink.NetworkMessageInfo info)
	{
		return true;
	}
	
	public static void ReportAgentDetected(uLink.NetworkPlayer player, AgentHuman theAgent, AgentHuman sender, uLink.NetworkMessageInfo info)
	{
	}
	
	public static void ReportMineDetected(uLink.NetworkPlayer player, Mine mine, uLink.NetworkMessageInfo info)
	{
	}
	
	public static void ReportMove(uLink.NetworkPlayer player, Vector3 newPos, Vector3 newVelocity, uLink.NetworkMessageInfo info)
	{
	}
	
#endregion

	public static void ReportAgentSpawned(AgentHuman playerAgent)
	{
	}

	public static bool ValidateHit(AgentHuman playerAgent, Projectile projectile, RaycastHit hit)
	{
		return true;
	}
}

#endif
