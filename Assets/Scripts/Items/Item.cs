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

[System.Serializable]
public class ItemIcons
{
	public GameObject IconBad;
	public GameObject IconGood;

	public void SetTeamIcon(E_Team team, bool sameTeamOnly = true)
	{
		PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(uLink.Network.player);

		if (ppi == null)
			return;

		if (ppi.Team != team && sameTeamOnly)
		{
// show icons only for same team !!!
			team = E_Team.None;
		}

		if (IconBad)
			IconBad.SetActive(team == E_Team.Bad);

		if (IconGood)
			IconGood.SetActive(team == E_Team.Good);
	}
}

public class Item
{
	public ItemSettings Settings { get; private set; }
	protected AgentHuman Owner;

	int _Count;

	public int Count
	{
		get { return _Count; }
		private set { _Count = value; }
	}

	public int OrigCount { get; private set; }
	public float Timer { get; private set; }
	public bool Detected { get; private set; } //was the item detected by Detector gadget?
	int EarnedExperience;

	float CheckDangerousSubjects = 0;
	bool RefreshBufferedRPC = false;

	static Quaternion Temp;

	List<GameObject> LiveItems = new List<GameObject>();

	public bool Active { get; private set; }

	public Item(PlayerPersistantInfo ppi, AgentHuman owner, E_ItemID itemID)
	{
		Owner = owner;
		Settings = ItemSettingsManager.Instance.Get(itemID);

		if (Settings.ItemBehaviour == E_ItemBehaviour.Booster)
			Timer = Settings.BoostTimer;
		else
			Timer = Settings.Timer;
		Detected = false;

		if (Settings.Consumable)
		{
// for cunsumables get real count from inventory !!! 
			PPIItemData data = ppi.InventoryList.Items.Find(item => item.ID == itemID);
			Count = data.Count > Settings.MaxCountInMission ? Settings.MaxCountInMission : data.Count;
		}
		else
		{
			Count = Settings.Count > Settings.MaxCountInMission ? Settings.MaxCountInMission : Settings.Count;

			if (ppi.Upgrades.OwnsUpgrade(E_UpgradeID.ExplosivePouch))
			{
				switch (itemID)
				{
				case E_ItemID.GrenadeEMP:
				case E_ItemID.GrenadeEMPII:
				case E_ItemID.GrenadeFlash:
				case E_ItemID.GrenadeFrag:
				case E_ItemID.GrenadeFragII:
				case E_ItemID.Mine:
				case E_ItemID.MineEMP:
				case E_ItemID.MineEMPII:
					++Count;
					break;
				}
			}
		}
		OrigCount = Count;
	}

	public void Destroy()
	{
		Owner = null;
		Settings = null;
		LiveItems.Clear();
	}

	public bool IsAvailableForUse()
	{
		if ((Game.GetMultiplayerGameType() == E_MPGameType.ZoneControl) && !Settings.AllowedInZoneControl)
		{
			return false;
		}

		if ((Game.GetMultiplayerGameType() == E_MPGameType.DeathMatch) && !Settings.AllowedInDeathmatch)
		{
			return false;
		}

		switch (Settings.ItemBehaviour)
		{
		case E_ItemBehaviour.Booster:
			return Count > 0;
		case E_ItemBehaviour.Throw:
			return Count > 0;
		case E_ItemBehaviour.Place:
			return Timer == Settings.Timer && (Count > 0 || Settings.Replaceable);
		default:
			return true;
		}
	}

	public void BoostUsed()
	{
		Active = true;
		Count--;
	}

	public void Use(Vector3 pos, Vector3 dir, Vector3 targetPos)
	{
#if !DEADZONE_CLIENT
        if (Settings.ItemUse == E_ItemUse.Passive)
            return;

        if (uLink.Network.isServer == false)
            return;

		bool allowed = false;

		switch( Server.Instance.GameInfo.GameType )
		{
		case E_MPGameType.DeathMatch:
			allowed = Settings.AllowedInDeathmatch;
			break;
		case E_MPGameType.ZoneControl:
			allowed = Settings.AllowedInZoneControl;
			break;
		}
		
		if( allowed )
		{
	        Owner.StatisticAddItemUse(Settings.ID);
	
	        switch (Settings.ItemBehaviour)
			{
			case E_ItemBehaviour.Throw:
				allowed = ThrowObject( pos, (targetPos - pos).normalized );
				break;
			case E_ItemBehaviour.Place:
				allowed = PlaceObject(pos, dir);
				break;
			default:
				throw new System.Exception("Item: Unknown behaviour");
			}
		} // no else here
		
		if( !allowed && null != Owner )
		{
			if( Server.Instance != null )
			{
				Server.Instance.PlaySoundDisabledOnClient( Player.GetNetworkPlayer( Owner ) );
			}
		}
#endif
	}

	public void Update()
	{
		//perform server-side Update
		if (uLink.Network.isServer)
		{
			if (Settings.ItemBehaviour == E_ItemBehaviour.Detector)
			{
				DetectDangerousSubjects_Server(); //detect agents, mines, turrets
			}
		}
		else
		{
			DetectDangerousSubjects_Client();
		}

		if (Active)
		{
			if (Settings.ItemBehaviour == E_ItemBehaviour.Booster && Timer > 0)
			{
				Timer -= Time.deltaTime;

				if (Timer <= 0)
				{
					Timer = 0;
					Active = false;
				}
			}
			return;
		}

		if (Timer < Settings.Timer)
			Timer = Mathf.Min(Timer + Settings.RechargeModificator*Time.deltaTime, Settings.Timer);
	}

	//
	void DetectDangerousSubjects_Client()
	{
		if (CheckDangerousSubjects > Time.timeSinceLevelLoad)
			return;

		CheckDangerousSubjects = Time.timeSinceLevelLoad + 0.5f;

		List<AgentHuman> agents = new List<AgentHuman>();

		//
		foreach (KeyValuePair<uLink.NetworkPlayer, ComponentPlayer> pair in Player.Players)
		{
			AgentHuman agent = pair.Value.Owner;

			if (!IsAgentValid(agent))
				continue;

			if (!agent.BlackBoard.IsDetected) //set by RPC from server
				continue;

			agents.Add(agent);
		}

		//update HUD
//		if (agents.Count > 0)
		//GuiHUD.Instance.UpdateDetectedAgents( agents );
	}

	void DetectDangerousSubjects_Server()
	{
		if (CheckDangerousSubjects > Time.timeSinceLevelLoad)
			return;

		CheckDangerousSubjects = Time.timeSinceLevelLoad + 0.5f;

		//
		foreach (KeyValuePair<uLink.NetworkPlayer, ComponentPlayer> pair in Player.Players)
		{
			AgentHuman agent = pair.Value.Owner;

			if (!IsAgentValid(agent))
				continue;

			float dist = (agent.TransformTarget.position - Owner.Position).sqrMagnitude;
			bool hasJammer = agent.GadgetsComponent.IsGadgetAvailableWithBehaviour(E_ItemBehaviour.Jammer);

			//
			if (dist <= Settings.Range*Settings.Range)
				DetectMines(agent);

			//
			if (hasJammer || dist > Settings.Range*Settings.Range) //need to put this value to settings!
			{
				if (agent.BlackBoard.IsDetected) //forget about him
				{
					//change the status 
					AgentDetectedStateChanged(agent, false);
				}

				continue; //too far
			}

//			Debug.Log ("DetectDangerousSubjects(), Owner=" + Owner.name + " detected enemy: " + agent.name/* + ", dist=" + dist*/);

			//
			///			DetectTurrets(agent);

			if (!agent.BlackBoard.IsDetected || RefreshBufferedRPC)
			{
				//change the status 
				AgentDetectedStateChanged(agent, true);
			}
		}

		RefreshBufferedRPC = false;
	}

	//
	void AgentDetectedStateChanged(AgentHuman agent, bool detected)
	{
		//set new state on server
		agent.BlackBoard.IsDetected = detected;

		//inform connected clients
		Owner.NetworkView.RPC("AgentDetected", uLink.RPCMode.Others, Owner.NetworkView.viewID, agent.NetworkView.viewID, detected);

		if (detected)
		{
			//add buffered RPC for newly connected clients
			Owner.NetworkView.RPC("AgentDetected", uLink.RPCMode.Buffered, Owner.NetworkView.viewID, agent.NetworkView.viewID, true);
		}
		else
		{
			//RemoveRPCsByName smazne VSECKY RPC AgentDetected, tedy i pro agenty, kteri zustavaji detected. Nutno refreshnout cele pri nasledujici detekci.
			RefreshBufferedRPC = true;

			//find and remove the previously buffered RPC
			uLink.Network.RemoveRPCsByName(Owner.NetworkView.viewID, "AgentDetected");
		}
	}

	void DetectMines(AgentHuman agent)
	{
		SearchForMine(agent.GadgetsComponent.GetGadget(E_ItemID.Mine)); //Mine
		SearchForMine(agent.GadgetsComponent.GetGadget(E_ItemID.MineEMP)); //MineEMP
		SearchForMine(agent.GadgetsComponent.GetGadget(E_ItemID.MineEMPII)); //MineEMPII
	}

	//
	void SearchForMine(Item item)
	{
		if (item != null && item.IsPlaced())
		{
			item.Detected = true;

			foreach (GameObject g in item.LiveItems)
			{
				float dist = (g.transform.position - Owner.Position).sqrMagnitude;

				if (dist < Settings.Range*Settings.Range)
				{
					Mine mine = g.GetComponent<Mine>();

					if (!mine.IsDetected)
						mine.SetDetected(true);

					//send buffered RPC
//					Owner.NetworkView.RPC("MineDetected", uLink.RPCMode.AllBuffered, mine.networkView.viewID);	//this was sent only if !detected - problem is, that the viewID becomes invalid when a client disconnects
					Owner.NetworkView.RPC("MineDetected", uLink.RPCMode.Others, mine.networkView.viewID);

//					Debug.Log (" DetectMines(), detected Mine, pos=" + g.transform.position + ", dist=" + Mathf.Sqrt(dist) );
				}
			}
		}
	}

	void DetectTurrets(AgentHuman agent)
	{
		Debug.LogWarning(" DetectTurrets() NOT IMPLEMENTED YET. Didn't decide whether we want it or not.");
	}

	bool IsAgentValid(AgentHuman agent)
	{
		return agent != null &&
			   agent.IsAlive &&
			   Owner.IsFriend(agent) == false;
	}

//    void PlaceObject(Vector3 pos, Vector3 dir) // by Mara
//    {
//        if (Count == 0)
//        {
//            if (LiveItems.Count == 0)
//            {
//                Debug.Log("no live items !!!" + Settings.ID);
//                return;
//            }
//
//            uLink.Network.Destroy(LiveItems[0]); 
//        }
//
//        Vector3 start = pos + dir;
//
//        RaycastHit hit;
//
//        if (Physics.Raycast(start, -Vector3.up, out hit, 2.0f) == false)
//            return;
//
//        if (Vector3.Dot(hit.normal, Vector3.up) < 0.6f)
//            return;
//
//        uLink.Network.Instantiate(Owner.NetworkView.owner, Settings.SpawnObject, hit.point, Quaternion.identity, 0, hit.normal, Settings.ID); 
//    }

//	void PlaceObject(Vector3 pos, Vector3 dir) // by Capa ... sphere casted obliquely downward (don't miss "vertical" collisions)
//	{
//		if (Count == 0)
//		{
//			if (LiveItems.Count == 0)
//			{
//				Debug.LogError("no live items !!!" + Settings.ID);
//				return;
//			}
//			
//			uLink.Network.Destroy(LiveItems[0]); 
//		}
//		
//		Vector3        src     = pos;// + 0.2f * dir;
//		Vector3        dst     = pos + dir - 1.5f * Vector3.up;
//		Vector3        castDir = Vector3.Normalize( dst - src );
//		int            mask    = ~( ObjectLayerMask.Ragdoll | ObjectLayerMask.PhysicBody );
//		RaycastHit []  hits    = Physics.SphereCastAll( src, 0.16f, castDir, 2.0f, mask );
//		
//		if ((hits == null) || (hits.Length == 0))
//		return;
//		
//	//	DebugDraw.DepthTest   = true;
//	//	DebugDraw.DisplayTime = 8.0f;
//	//	DebugDraw.Line( Color.green, src, dst );
//	//	foreach (RaycastHit hit in hits)
//	//	{
//	//		DebugDraw.Diamond( Color.red, 0.01f, hit.point );
//	//		DebugDraw.LineOriented( Color.red, hit.point, hit.point+hit.normal*0.25f, 0.05f );
//	//	}
//		
//		if (hits.Length > 1)
//		{
//			System.Array.Sort(hits, CollisionUtils.CompareHits);
//		}
//		
//		foreach (RaycastHit hit in hits)
//		{
//			if (hit.collider.isTrigger == true)
//			continue;
//			
//			if (IsValidForPlacement(hit))
//			{
//				uLink.Network.Instantiate(Owner.NetworkView.owner, Settings.SpawnObject, hit.point,
//				                          Quaternion.identity, 0, hit.normal, Settings.ID);
//			}
//			
//			return;
//		}
//	}

	bool PlaceObject(Vector3 pos, Vector3 dir) // by Mira ... ( see lines above ) :-)
	{
		if (Count == 0)
		{
			//The replaceable test fixes the cheating about placeable objects. (you can emulate the cheat by commenting out
			//the "Count--" in the RegisterPlacedItem method) There were several nasty cheaters placing tens of mines!
			//TODO Proc ale kurwa ten cheat nefunguje taky na SentryGun? To je jeste potreba zjistit.
			if (Settings.Replaceable)
			{
				if (LiveItems.Count == 0)
				{
					Debug.LogError("no live items !!!" + Settings.ID);
					return false;
				}

				uLink.Network.Destroy(LiveItems[0]);
			}
			else
			{
				return false;
			}
		}

		bool result = false;
		// place just on top of the static geometry
		int mask = ObjectLayerMask.Default | ObjectLayerMask.PhysicsMetal;

		RaycastHit hit = new RaycastHit();

		//TODO Tu se nekde da ocheckovat ta pokladani min a strilen v okoli vlajek
		if (GetPlacementHit(pos, dir, ref hit, mask))
		{
			if (IsValidForPlacement(hit, Settings.SpawnObject, (null != Owner) ? Owner.Position.y + 0.4f : pos.y))
			{
				uLink.Network.Instantiate(Owner.NetworkView.owner,
										  Settings.SpawnObject,
										  hit.point,
										  Quaternion.identity,
										  0,
										  hit.normal,
										  Settings.ID);
				result = true;
			}
		}

		return result;
	}

	bool GetPlacementHit(Vector3 pos, Vector3 dir, ref RaycastHit hitResult, int mask)
	{
		Vector3 src = pos;
		Vector3 dst = pos + dir - 1.5f*Vector3.up;
		Vector3 castDir = Vector3.Normalize(dst - src);
		float back = 0.75f; // prevents from placing objects into geometry.
		float forward = 2.0f;

		src -= castDir*back;

		RaycastHit[] hits = Physics.SphereCastAll(src, 0.16f, castDir, forward + back, mask);

		if (hits == null || hits.Length == 0)
		{
			return false;
		}

		if (hits.Length > 1)
		{
			System.Array.Sort(hits, CollisionUtils.CompareHits);
		}

/*		DebugDraw.DepthTest   = true;
		DebugDraw.DisplayTime = 8.0f;
		
		foreach (RaycastHit hit in hits)
		{
			DebugDraw.Diamond( Color.red, 0.04f, hit.point );
			DebugDraw.LineOriented( Color.red, hit.point, hit.point+hit.normal*0.25f, 0.05f );
		}
		
		DebugDraw.LineOriented( Color.yellow, src, dst, 0.05f );
*/

		foreach (RaycastHit hit in hits)
		{
			if (hit.collider.isTrigger == true)
			{
				continue;
			}

			hitResult = hit;

			return true;
		}

		return false;
	}

	bool IsValidForPlacement(RaycastHit hit, GameObject isValidFor, float heightPosFrom)
	{
		if (hit.rigidbody != null)
		{
			return false; // physicaly simulated object
		}

		/*DebugDraw.DepthTest   = true;
		DebugDraw.DisplayTime = 16.0f;
		DebugDraw.Diamond( Color.red, 0.04f, hit.point );		
		Vector3 to = hit.point;
		to.y = heightPosFrom;		
		DebugDraw.Diamond( Color.yellow, 0.04f, to );*/

//		float dot = Vector3.Dot( hit.normal, Vector3.up );
//		Debug.Log( "PLACE HIT: " + "dot=" + dot.ToString("F4") + ", hit.point.y=" + hit.point.y + ", heightPosFrom=" + heightPosFrom + ", isValidFor=" + isValidFor.name );

		if (hit.point.y > heightPosFrom)
						// prevent from placing objects on the top of boxes, covers, etc. - place should be lower than position of the hand
		{
			return false;
		}

		if (Vector3.Dot(hit.normal, Vector3.up) < 0.9f)
		{
			return false; // plane tilted by more than 25 degrees
		}

		if (null != isValidFor)
		{
			float radius = 0.25f; //*(isValidFor.collider.size.x + isValidFor.collider.size.z);

			// todo : eventually add other colider types
			if (null != isValidFor.GetComponent<Collider>())
			{
				BoxCollider box = isValidFor.GetComponent<Collider>() as BoxCollider;

				if (null != box)
				{
					radius = 0.5f*(box.size.x + box.size.z);
				}
			}

			int mask = ~(ObjectLayerMask.Default | ObjectLayerMask.PhysicsMetal);

			//radius = 2*radius;
			Vector3 center = hit.point + Vector3.up*(radius + 0.1f);
			Collider[] hits = Physics.OverlapSphere(center, radius, mask);

			foreach (Collider C in hits)
			{
				if (C.isTrigger)
				{
					continue;
				}

				if (!C.transform.IsChildOf(Owner.Transform))
				{
					/*DebugDraw.DepthTest   = true;
					DebugDraw.DisplayTime = 160.0f;
					DebugDraw.Sphere(Color.red, radius, center);*/
					return false;
				}
			}
			/*DebugDraw.DepthTest   = true;
			DebugDraw.DisplayTime = 160.0f;
			DebugDraw.Sphere(Color.green, radius+0.1f, center);*/
		}

		// TODO: check if give hit corresponds to object that is "valid" for gadget placement (can't be destroyed or moved, etc.)

		return true;
	}

	bool ThrowObject(Vector3 pos, Vector3 dir)
	{
		if (Count == 0)
			return false;

		Temp.SetLookRotation(dir);

		Temp.eulerAngles = new Vector3(Temp.eulerAngles.x - 15, Temp.eulerAngles.y, 0); // throw higher

		dir = Temp*Vector3.forward;

		uLink.Network.Instantiate(Owner.NetworkView.owner, Settings.SpawnObject, pos + dir*0.0f, Quaternion.identity, 0, dir, Settings.ID);

		return true;
	}

	public void RegisterUsedItem()
	{
		if (PlayerControlsDrone.Enabled)
			return;

		Count--;
		Timer = 0;

		if (Settings.Consumable) // if consumable, then 
		{
			PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(Owner.NetworkView.owner);
			if (ppi != null)
				ppi.ConsumableItemUsed(Settings.ID);
		}
	}

	public void RegisterPlacedItem(GameObject g)
	{
		Count--;
		Timer = 0;

		if (Settings.Consumable) // if consumable, then 
		{
			PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(Owner.NetworkView.owner);
			if (ppi != null)
				ppi.ConsumableItemUsed(Settings.ID);
		}

		LiveItems.Add(g);

		if (Game.Instance.GameLog)
			Debug.Log("Register Item " + Settings.ID + " count " + Count + " " + g.name);
	}

	public void UnRegisterPlacedObject(GameObject g)
	{
		if (Settings.Replaceable)
			Count++;

		LiveItems.Remove(g);

		if (Game.Instance.GameLog)
			Debug.Log("UnRegister Item " + Settings.ID + " count " + Count + " " + g.name);
	}

	public bool IsPlaced()
	{
		return LiveItems.Count > 0;
	}
}
