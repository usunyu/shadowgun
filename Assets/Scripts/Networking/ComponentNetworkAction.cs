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

using System.Collections;
using UnityEngine;
using uLink;
using Network = uLink.Network;
using NetworkViewID = uLink.NetworkViewID;
using RPCMode = uLink.RPCMode;

public class ComponentNetworkAction : uLink.MonoBehaviour
{
	AgentHuman Owner;

	void Awake()
	{
		Owner = GetComponent<AgentHuman>();
	}

#if !DEADZONE_CLIENT
	void Start()
	{
		ServerAnticheat.ReportAgentSpawned(Owner);
	}
#endif
	
	// This update function deals with the postponed bullets on the server side to guarantee desired fire-rate
#if !DEADZONE_CLIENT

	float LastServerWeaponShotTime = 0.0f;

	//We can't really use weapon.IsBussy logic on the server. The function itself returns always false on server
	//but this is not a problem. The problem is that it is internally set by reloads and other events which 
	//we really do not want to track on server as we are unable to count shots preciselly (due to replication logic)
	bool ServerWeaponIsBusy(WeaponBase weapon)
	{
		//Allow to shoot little bit earlier (and also faster) to:
		//- lower the latency little bit
		//- compensate the RTT fluctuations
		//- smoother gaming experience
		//
		//When changing this number, consider these:
		//- game server usually runs at 60FPS -> ideal frame is 0.0166 second long
		//- fastest weapon can shoot about 30 shells/sec -> 0.033 seconds per shell
		//- the constant 0.007 allows cheaters to shoot a shell each 0.026 second -> 0.033/0.026 =~ 27% faster
		//- the slower firing weapon, the more precise fire-rate accuracy
		const float ServerFrameCompensationTime = 0.007f;

		return LastServerWeaponShotTime + weapon.FireTime - ServerFrameCompensationTime > Time.timeSinceLevelLoad;
	}

	void Update()
	{
		if (PostponedServerAttack.IsValid)
		{
			WeaponBase weapon = Owner.WeaponComponent.GetCurrentWeapon();
			if(weapon != PostponedServerAttack.Weapon)
			{
				//Invalidate when the weapon already swapped or dissapeared (prevent shooting accidentaly from a different weapon)
				PostponedServerAttack.Invalidate();
				return;
			}
		
			if ( !ServerWeaponIsBusy(weapon) )
			{
				ProcessServerAttack(weapon, PostponedServerAttack.FromPos, PostponedServerAttack.AttackDir);
				PostponedServerAttack.Invalidate();
			}
		}
	}
#endif // #if !DEADZONE_CLIENT

	void Activate()
	{
		Owner.BlackBoard.ActionHandler += HandleAction;
	}

	void Deactivate()
	{
		Owner.BlackBoard.ActionHandler -= HandleAction;
	}

	void HandleAction(AgentAction action)
	{
		if (action is AgentActionAttack)
		{
			if (Owner.IsOwner)
				Owner.NetworkView.RPC("AttackS", uLink.RPCMode.Server, (action as AgentActionAttack).FromPos, (action as AgentActionAttack).AttackDir);
		}
		else if (action is AgentActionInjury)
		{
			var injury = action as AgentActionInjury;
			if (Owner.IsServer)
			{
				uLink.NetworkViewID viewId = (injury.Attacker != null && injury.Attacker.NetworkView != null)
															 ? injury.Attacker.NetworkView.viewID
															 : uLink.NetworkViewID.unassigned;

				Owner.NetworkView.RPC("Injury", RPCMode.Others, viewId, injury.Pos, injury.Impulse, (short)injury.Damage, (short)injury.BodyPart);
			}
		}
		else if (action is AgentActionReload)
		{
			if (Owner.IsOwner)
				Owner.NetworkView.RPC("Reload", uLink.RPCMode.Server);
		}
		else if (action is AgentActionTeamCommand)
		{
			AgentActionTeamCommand a = action as AgentActionTeamCommand;
			if (Owner.IsOwner)
				Owner.NetworkView.RPC("TeamCmd", RPCMode.Server, a.Command);
		}

		else if (action is AgentActionRoll)
		{
			if (Owner.IsOwner)
				Owner.NetworkView.RPC("Roll", uLink.RPCMode.Server, (action as AgentActionRoll).Direction);
		}
		else if (action is AgentActionDeath)
		{
			var death = action as AgentActionDeath;
			if (Owner.IsServer)
			{
				uLink.NetworkViewID viewId = (death.Attacker != null && death.Attacker.NetworkView != null)
															 ? death.Attacker.NetworkView.viewID
															 : uLink.NetworkViewID.unassigned;

				Owner.NetworkView.RPC("Death", RPCMode.Others, viewId, death.Pos, death.Impulse, (short)death.Damage, (short)death.BodyPart);

				if (null != death.Attacker)
				{
					PPIManager.Instance.ServerAddScoreForKill(Owner.NetworkView.owner,
															  death.Attacker.NetworkView.owner,
															  Owner.BlackBoard.AttackersDamageData,
															  death.BodyPart,
															  Owner.GadgetsComponent.GetBoostGoldReward());
				}

				/*if (Server.Instance.GameInfo.GameType == E_MPGameType.ZoneControl)
				{
					// currently not using rebalancing after death
					//PPIManager.Instance.ServerRebalanceTeams();
				}*/
			}
		}
		else if (action is AgentActionCoverEnter)
		{
			if (Owner.IsOwner)
				Owner.NetworkView.RPC("CoverEnter",
									  uLink.RPCMode.Server,
									  Mission.Instance.GameZone.GetCoverIndex(Owner.BlackBoard.Cover),
									  Owner.BlackBoard.Desires.CoverPosition);
		}
		else if (action is AgentActionCoverLeave)
		{
			if (Owner.IsOwner)
				Owner.NetworkView.RPC("CoverLeave", uLink.RPCMode.Server, ((AgentActionCoverLeave)action).TypeOfLeave);
		}
		else if (action is AgentActionCoverFire)
		{
			if (Owner.IsOwner)
			{
				AgentActionCoverFire a = action as AgentActionCoverFire;
				Owner.NetworkView.RPC("CoverFireStart", uLink.RPCMode.Server, a.CoverPose, a.CoverDirection);
			}
		}
		else if (action is AgentActionCoverFireCancel)
		{
			if (Owner.IsOwner)
				Owner.NetworkView.RPC("CoverFireStop", uLink.RPCMode.Server);
		}
		else if (action is AgentActionWeaponChange)
		{
			if (Owner.IsOwner)
				Owner.NetworkView.RPC("ChangeWeapon", uLink.RPCMode.Server, (action as AgentActionWeaponChange).NewWeapon);
		}
		else if (action is AgentActionUseItem)
		{
//			Debug.Log ("ComponentNetworkAction.HandleAction(), time=" + Time.timeSinceLevelLoad + ", BlackBoard.KeepMotion=" + Owner.BlackBoard.KeepMotion + ", Owner.IsOwner=" + Owner.IsOwner);

			if (Owner.IsOwner)
			{
				if (Owner.IsInCover)
					Owner.NetworkView.RPC("UseItemInCover",
										  uLink.RPCMode.Server,
										  Owner.BlackBoard.Desires.Gadget,
										  Owner.BlackBoard.CoverPose,
										  Owner.BlackBoard.CoverPosition);
				else
					Owner.NetworkView.RPC("UseItem", uLink.RPCMode.Server, Owner.BlackBoard.Desires.Gadget, Owner.BlackBoard.KeepMotion);
			}
		}
		else if (action is AgentActionMelee)
		{
			if (Owner.IsOwner)
			{
				AgentActionMelee a = action as AgentActionMelee;

				uLink.NetworkViewID viewId = (a.Target != null && a.Target.NetworkView != null)
															 ? a.Target.NetworkView.viewID
															 : uLink.NetworkViewID.unassigned;

				Owner.NetworkView.RPC("Melee", uLink.RPCMode.Server, a.MeleeType, viewId);
			}
		}
		else if (action is AgentActionKnockdown)
		{
			if (Owner.IsServer)
			{
				AgentActionKnockdown a = action as AgentActionKnockdown;

				uLink.NetworkViewID viewId = (a.Attacker != null && a.Attacker.NetworkView != null)
															 ? a.Attacker.NetworkView.viewID
															 : uLink.NetworkViewID.unassigned;

				Owner.NetworkView.RPC("Knockdown", uLink.RPCMode.Others, a.MeleeType, viewId, a.Direction);
			}
		}
	}

#if !DEADZONE_CLIENT
	struct PostponedAttack
	{
		public Vector3		FromPos;
		public Vector3		AttackDir;
		public WeaponBase	Weapon;
		public bool			IsValid { get {return Weapon != null;} }
		public void			Invalidate() { Weapon = null; }
	}

	PostponedAttack PostponedServerAttack;

	void ProcessServerAttack( WeaponBase weapon, Vector3 fromPos, Vector3 attackDir )
	{
		//FIXME: tenhle zpusob volani se musi vyresit - zatim to zpusobuje exceptions ( in progress )
		//Medved: Moc rad bych vedel, co to bylo za problemy... Predpokladam, ze neco na clientovi diky
		//prehozeni poradi garantovanych a negarantovanych packetu v pripade, kdy garantovany zazil re-send.

		/*
		if(weapon.FireTime < 0.15f)
			Owner.NetworkView.UnreliableRPC("AttackC",uLink.RPCMode.OthersExceptOwner, attackDir);
		else 
		*/
		Owner.NetworkView.RPC("AttackC",uLink.RPCMode.OthersExceptOwner, attackDir);
		
		AgentActionAttack action = AgentActionFactory.Create(AgentActionFactory.E_Type.Attack) as AgentActionAttack;
		action.AttackDir = attackDir;
		action.FromPos = fromPos;
		Owner.BlackBoard.ActionAdd(action);

		LastServerWeaponShotTime = Time.timeSinceLevelLoad;
	}

	[uSuite.RPC]
	protected void AttackS(Vector3 fromPos, Vector3 attackDir, uLink.NetworkMessageInfo info)
	{
		if(!Owner.IsServer)
			return;
		
		if(Owner.BlackBoard.DontUpdate)
			return;
		
		// player cannot attack during spawn protection timer
		if(Owner.IsSpawnedRecently)
			return;

		WeaponBase weapon = Owner.WeaponComponent.GetCurrentWeapon();
		if(weapon == null)
			return;

		// Neni penis, neni laska :-)
		if(weapon.ClipAmmo == 0)
			return;

		if (!ServerAnticheat.ReportAndValidateAttack(Owner.NetworkView.owner, weapon, fromPos, attackDir, info))
		{
			//Ignore this attack as it is not valid
			weapon.DecreaseAmmo();
			return;
		}

		if (ServerWeaponIsBusy(weapon))
		{
			PostponedServerAttack.FromPos = fromPos;
			PostponedServerAttack.AttackDir = attackDir;
			PostponedServerAttack.Weapon = weapon;
		}
		else
		{
			ProcessServerAttack(weapon, fromPos, attackDir);
		}
	}
#endif // #if !DEADZONE_CLIENT

	[uSuite.RPC]
	protected void AttackC(Vector3 attackDir)
	{
		// TODO: I'm not sure what causes DontUpdate to be set to true on clients (especially proxies) but I would
		//say fuck it on proxies! The proxy has to do what server says. No way to ignore to spawn a projectile.
		//if (Owner.BlackBoard.DontUpdate)
		//	return;

#if !DEADZONE_CLIENT
		if (Owner.IsServer)
		{
			ServerAnticheat.ReportPotentialCheatAttempt("AttackC", "should never be called on the server side", Owner.NetworkView.owner);
			return;
		}
#endif

		AgentActionAttack action = AgentActionFactory.Create(AgentActionFactory.E_Type.Attack) as AgentActionAttack;
		action.AttackDir = attackDir;
		Owner.BlackBoard.ActionAdd(action);
	}

	/// <summary>
	/// Called on the server and proxies when the owner reloads.
	/// </summary>
	[uSuite.RPC]
	protected void Reload(uLink.NetworkMessageInfo info)
	{
		//TODO: What about this condition on the proxies. Again, I would say fuck it
		if (Owner.BlackBoard.DontUpdate)
			return;

#if !DEADZONE_CLIENT
		if (Owner.IsServer)
		{
			ServerAnticheat.ReportReload(Owner.NetworkView.owner, info);
			Owner.NetworkView.RPC("Reload", uLink.RPCMode.OthersExceptOwner);
		}
#endif

		if (Owner.IsInCover)
			Owner.BlackBoard.ActionAdd((AgentActionIdle)AgentActionFactory.Create(AgentActionFactory.E_Type.Idle));

		AgentAction reloadAction = AgentActionFactory.Create(AgentActionFactory.E_Type.Reload) as AgentActionReload;
		Owner.BlackBoard.ActionAdd(reloadAction);
	}

	[uSuite.RPC]
	protected void TeamCmd(E_CommandID id, uLink.NetworkMessageInfo info)
	{
#if !DEADZONE_CLIENT
		if (Owner.IsServer)
		{
			ServerAnticheat.ReportTeamCmd(Owner.NetworkView.owner, id, info);

			Owner.NetworkView.RPC("TeamCmd", uLink.RPCMode.OthersExceptOwner, id);
			return;
		}
#endif

		AgentActionTeamCommand a = AgentActionFactory.Create(AgentActionFactory.E_Type.TeamCommand) as AgentActionTeamCommand;
		a.Command = id;

		Owner.BlackBoard.ActionAdd(a);
	}

	/// <summary>
	/// Called on the owner when an injury is detected on the server.
	/// This makes sure that damage that is computed in the server simulation is also computed on the owner.
	/// Visual damage, like blood, is created locally when bullets hit the player, so this does not need to be networked.
	/// </summary>
	[uSuite.RPC]
	protected void Injury(NetworkViewID attackerNVId, Vector3 pos, Vector3 impuls, short damage, short bodyPart)
	{
#if !DEADZONE_CLIENT
		if (Owner.IsServer)
		{
			ServerAnticheat.ReportPotentialCheatAttempt("Injury", "should never be called on the server side", Owner.NetworkView.owner);
			return;
		}
#endif

		if (Owner.BlackBoard.DontUpdate)
			return;

		uLink.NetworkView View = (attackerNVId != uLink.NetworkViewID.unassigned) ? uLink.NetworkView.Find(attackerNVId) : null;

		Owner.Injure(View ? View.GetComponent<AgentHuman>() : null, (float)damage, pos, impuls, (E_BodyPart)bodyPart);
	}

	/// <summary>
	/// Called on the owner and proxy when the server decides a player has died.
	/// </summary>
	[uSuite.RPC]
	protected void Death(NetworkViewID attackerNVId, Vector3 pos, Vector3 impuls, short damage, short bodyPart)
	{
#if !DEADZONE_CLIENT
		if (Owner.IsServer)
		{
			ServerAnticheat.ReportPotentialCheatAttempt("Death", "should never be called on the server side", Owner.NetworkView.owner);
			return;
		}
#endif

		if (Owner.BlackBoard.DontUpdate)
			return;

		uLink.NetworkView View = (attackerNVId != uLink.NetworkViewID.unassigned) ? uLink.NetworkView.Find(attackerNVId) : null;

		Owner.Die(View ? View.GetComponent<AgentHuman>() : null, pos, impuls, damage, (E_BodyPart)bodyPart);
	}

	[uSuite.RPC]
	protected void CoverEnter(int coverIndex, E_CoverDirection coverDirection, uLink.NetworkMessageInfo info)
	{
		Cover cover = Mission.Instance.GameZone.GetCover(coverIndex);
		if (cover == null)
		{
			Debug.LogWarning("Received CoverEnter RPC but no cover was found at the specified position. This could indicate a position sync problem.");
			return;
		}

#if !DEADZONE_CLIENT
		if (Owner.IsServer)
		{
			//Only server has the right to ignore the cover enter
			if (Owner.BlackBoard.DontUpdate)
				return;

			ServerAnticheat.ReportCoverEnter(Owner.NetworkView.owner, cover, coverDirection, info);
			Owner.NetworkView.RPC("CoverEnter", uLink.RPCMode.OthersExceptOwner, coverIndex, coverDirection);
		}
#endif

		Owner.CoverStart(cover, coverDirection);
	}

	[uSuite.RPC]
	protected void CoverLeave(AgentActionCoverLeave.E_Type typeOfLeave, uLink.NetworkMessageInfo info)
	{
		//TODO: How is this? I believe we definitelly cannot afford to ignore the CoverLeave event on both on proxies and I think we
		//should not ignore it on server too. No doubt about proxies - they should just do what they are told to do.
		//About the server - how can we ignore it without fixing a client? If we ignore it, the player stayes in a cover forever
		//which might cause both: visual artefacts and even detecting player as a cheater.
		//I don't think that a potential cheater can gain much of benefit by leaving the cover prematurely.

		//if (Owner.BlackBoard.DontUpdate)
		//	return;

		if (Owner.IsInCover)
		{
#if !DEADZONE_CLIENT
			if (Owner.IsServer)
			{
				ServerAnticheat.ReportCoverLeave(Owner.NetworkView.owner, typeOfLeave, info);
				Owner.NetworkView.RPC("CoverLeave", uLink.RPCMode.OthersExceptOwner, typeOfLeave);
			}
#endif

			AgentActionCoverLeave action = AgentActionFactory.Create(AgentActionFactory.E_Type.CoverLeave) as AgentActionCoverLeave;
			action.TypeOfLeave = typeOfLeave;
			action.FinalViewDirection = Owner.BlackBoard.Cover.Forward;
			action.Cover = Owner.BlackBoard.Cover;

			Owner.BlackBoard.ActionAdd(action);
		}
	}

	[uSuite.RPC]
	protected void CoverFireStart(E_CoverPose pose, E_CoverDirection direction, uLink.NetworkMessageInfo info)
	{
		if (Owner.BlackBoard.DontUpdate)
			return;

#if !DEADZONE_CLIENT
		if (Owner.IsServer)
		{
			ServerAnticheat.ReportCoverFireStart(Owner.NetworkView.owner, pose, direction, info);
			Owner.NetworkView.RPC("CoverFireStart", uLink.RPCMode.OthersExceptOwner, pose, direction);
		}
#endif

		if (Owner.IsInCover)
		{
			AgentActionCoverFire action = AgentActionFactory.Create(AgentActionFactory.E_Type.CoverFire) as AgentActionCoverFire;
			action.CoverPose = pose;
			action.CoverDirection = direction;

			Owner.BlackBoard.ActionAdd(action);
		}
	}

	[uSuite.RPC]
	protected void CoverFireStop(uLink.NetworkMessageInfo info)
	{
		if (Owner.BlackBoard.DontUpdate)
			return;

#if !DEADZONE_CLIENT
		if (Owner.IsServer)
		{
			ServerAnticheat.ReportCoverFireStop(Owner.NetworkView.owner, info);
			Owner.NetworkView.RPC("CoverFireStop", uLink.RPCMode.OthersExceptOwner);
		}
#endif

		if (Owner.IsInCover)
		{
			AgentActionCoverFireCancel action = AgentActionFactory.Create(AgentActionFactory.E_Type.CoverFireCancel) as AgentActionCoverFireCancel;

			Owner.BlackBoard.ActionAdd(action);
		}
	}

	[uSuite.RPC]
	void ChangeWeapon(E_WeaponID weapon, uLink.NetworkMessageInfo info)
	{
		if (Owner.BlackBoard.DontUpdate)
			return;

#if !DEADZONE_CLIENT
		if (Owner.IsServer)
		{
			ServerAnticheat.ReportChangeWeapon(Owner.NetworkView.owner, weapon, info);
		}
#endif

		AgentActionWeaponChange action = AgentActionFactory.Create(AgentActionFactory.E_Type.WeaponChange) as AgentActionWeaponChange;
		action.NewWeapon = weapon;
		Owner.BlackBoard.ActionAdd(action);

		Owner.NetworkView.RPC("ChangeWeaponC", uLink.RPCMode.OthersExceptOwner, weapon);
	}

	[uSuite.RPC]
	void ChangeWeaponC(E_WeaponID weapon, uLink.NetworkMessageInfo info)
	{
#if !DEADZONE_CLIENT
		if (Owner.IsServer)
		{
			ServerAnticheat.ReportPotentialCheatAttempt("ChangeWeaponC", "should never be called on the server side", Owner.NetworkView.owner);
			return;
		}
#endif

		if (Owner.BlackBoard.DontUpdate)
			return;

		AgentActionWeaponChange action = AgentActionFactory.Create(AgentActionFactory.E_Type.WeaponChange) as AgentActionWeaponChange;
		action.NewWeapon = weapon;
		Owner.BlackBoard.ActionAdd(action);
	}

	/// <summary>
	/// Called on the server and proxies when the owner roll.
	/// </summary>
	[uSuite.RPC]
	protected void Roll(E_Direction direction, uLink.NetworkMessageInfo info)
	{
#if !DEADZONE_CLIENT
		if (Owner.IsServer)
		{
			ServerAnticheat.ReportRoll(Owner.NetworkView.owner, direction, info);
			Owner.NetworkView.RPC("Roll", uLink.RPCMode.OthersExceptOwner, direction);
		}
#endif

		AgentActionRoll a = AgentActionFactory.Create(AgentActionFactory.E_Type.Roll) as AgentActionRoll;
		a.Direction = direction;

		Owner.BlackBoard.ActionAdd(a);
	}

	[uSuite.RPC]
	protected void UseItemInCover(E_ItemID gadget, E_CoverPose coverPose, E_CoverDirection coverDirection, uLink.NetworkMessageInfo info)
	{
		if (Owner.BlackBoard.DontUpdate)
			return;

#if !DEADZONE_CLIENT
		if (Owner.IsServer)
		{
			ServerAnticheat.ReportUseItemInCover(Owner.NetworkView.owner, gadget, coverPose, coverDirection, info);
			Owner.NetworkView.RPC("UseItemInCover", uLink.RPCMode.OthersExceptOwner, gadget, coverPose, coverDirection);
		}
#endif

		Owner.BlackBoard.KeepMotion = false;
		Owner.BlackBoard.Desires.Gadget = gadget;

		AgentActionUseItem a = AgentActionFactory.Create(AgentActionFactory.E_Type.UseItem) as AgentActionUseItem;

		a.CoverDirection = coverDirection;
		a.CoverPose = coverPose;

		Owner.BlackBoard.ActionAdd(a);

		Owner.WorldState.SetWSProperty(E_PropKey.AtTargetPos, true);
	}

	[uSuite.RPC]
	protected void UseItem(E_ItemID gadget, bool keepMotion, uLink.NetworkMessageInfo info)
	{
//		Debug.Log ("ComponentNetworkAction.UseItem(), time=" + Time.timeSinceLevelLoad + ", BlackBoard.KeepMotion=" + Owner.BlackBoard.KeepMotion + ", Owner.IsOwner=" + Owner.IsOwner + ", Owner.IsServer=" + Owner.IsServer + ", BlackBoard.DontUpdate=" + Owner.BlackBoard.DontUpdate);

		if (Owner.BlackBoard.DontUpdate)
			return;

#if !DEADZONE_CLIENT
		if (Owner.IsServer)
		{
			ServerAnticheat.ReportUseItem(Owner.NetworkView.owner, gadget, keepMotion, info);
			Owner.NetworkView.RPC("UseItem", uLink.RPCMode.OthersExceptOwner, gadget, keepMotion);
		}
#endif

		Owner.BlackBoard.KeepMotion = keepMotion;
		Owner.BlackBoard.Desires.Gadget = gadget;
		Owner.BlackBoard.ActionAdd(AgentActionFactory.Create(AgentActionFactory.E_Type.UseItem));

		Owner.WorldState.SetWSProperty(E_PropKey.AtTargetPos, true);
	}

	[uSuite.RPC]
	protected void Melee(E_MeleeType meleeType, uLink.NetworkViewID viewID, uLink.NetworkMessageInfo info)
	{
		if (Owner.BlackBoard.DontUpdate)
			return;

		uLink.NetworkView view = uLink.NetworkView.Find(viewID);

		if (null == view)
		{
			// target was just destroyed
			return;
		}

		Agent targetAgent = view.GetComponent<Agent>();
		if (targetAgent == null)
			return; // wtf ?

#if !DEADZONE_CLIENT
		if (Owner.IsServer)
		{
			if (!ServerAnticheat.ReportAndValidateMelee(Owner.NetworkView.owner, Owner, targetAgent, info))
			{
				// Ignore the action when it is not valid. This may happen even in a regular/fair game when
				// an attacking playerexperiences a lag.
				return;
			}
		}
#endif

		AgentActionMelee a = AgentActionFactory.Create(AgentActionFactory.E_Type.Melee) as AgentActionMelee;
		a.Target = targetAgent;
		a.MeleeType = meleeType;
		Owner.BlackBoard.ActionAdd(a);

		if (a.IsFailed())
			return; // wtf ?

		if (Owner.IsServer)
		{
			//send to proxies
			Owner.NetworkView.RPC("Melee", uLink.RPCMode.OthersExceptOwner, meleeType, viewID);

			//knockdown target immediatly, its player, we dont have time wait ....
			Vector3 direction = (a.Target.Position - Owner.Position).normalized;
			a.Target.KnockDown(Owner, meleeType, direction);
		}

		//Debug.Log(Time.timeSinceLevelLoad + " " + "Melee " + a.MeleeType + " " + a.Target.name);
	}

	[uSuite.RPC]
	protected void Knockdown(E_MeleeType meleeType, uLink.NetworkViewID viewId, Vector3 direction)
	{
#if !DEADZONE_CLIENT
		if (Owner.IsServer)
		{
			ServerAnticheat.ReportPotentialCheatAttempt("Knockdown", "should never be called on the server side", Owner.NetworkView.owner);
			return;
		}
#endif

		if (Owner.BlackBoard.DontUpdate)
			return;

		uLink.NetworkView View = (viewId != uLink.NetworkViewID.unassigned) ? uLink.NetworkView.Find(viewId) : null;
		AgentHuman attacker = View ? View.GetComponent<AgentHuman>() : null;

		Owner.KnockDown(attacker, meleeType, direction);
	}

	[uSuite.RPC]
	protected void AgentDetected(NetworkViewID senderID, NetworkViewID agentID, bool detected, uLink.NetworkMessageInfo info)
	{
		uLink.NetworkView agentView = uLink.NetworkView.Find(agentID);
		if (!agentView)
			return;

		uLink.NetworkView senderView = uLink.NetworkView.Find(senderID);
		if (!senderView)
			return;

		AgentHuman agent = agentView.GetComponent<AgentHuman>();
		AgentHuman sender = senderView.GetComponent<AgentHuman>();

#if !DEADZONE_CLIENT
		if (Owner.IsServer)
		{
			ServerAnticheat.ReportAgentDetected(Owner.NetworkView.owner, agent, sender, info);
		}
#endif

		if (sender && sender.IsAlive && agent && agent.IsAlive && sender.IsFriend(agent) == false)
		{
			agent.BlackBoard.IsDetected = detected;

//			Debug.Log ("AgentDetected(), detected=" + detected + ", Owner=" + Owner.name + ", sender=" + sender.name + ", agent=" + agent.name);
		}
	}

	[uSuite.RPC]
	protected void MineDetected(NetworkViewID mineID, uLink.NetworkMessageInfo info)
	{
		uLink.NetworkView view = uLink.NetworkView.Find(mineID);
		if (view == null)
			return;

		Mine mine = view.GetComponent<Mine>();
		if (mine == null)
			return;

#if !DEADZONE_CLIENT
		if (Owner.IsServer)
		{
			ServerAnticheat.ReportMineDetected(Owner.NetworkView.owner, mine, info);
		}
#endif

		if (!mine.IsDetected)
			mine.SetDetected(true);

//		Debug.Log ("MineDetected(), Owner=" + Owner.name + ", mine=" + mine.name);
	}
}
