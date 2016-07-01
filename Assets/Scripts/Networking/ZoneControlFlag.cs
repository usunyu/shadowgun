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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using uLink;
using Network = uLink.Network;
using NetworkViewID = uLink.NetworkViewID;
using RPCMode = uLink.RPCMode;
using NetworkView = uLink.NetworkView;

[AddComponentMenu("Multiplayer/ZoneControlFlag")]
public class ZoneControlFlag : uLink.MonoBehaviour
{
	public enum E_ZoneControlEvent
	{
		Defend,
		Attacked,
		FlagNeutral,
		Owned,
	}

	enum E_AnimationState
	{
		None,
		Up,
		Down,
	}

	public E_Team StartBase = E_Team.None;
	public int FlagName = 0;
	public float Range = 10;
	public float FlagRaise = 3;
	public Transform Flag;

	public int ZoneNameIndex;

	[System.NonSerialized] public SpawnPointPlayer[] GoodSpawnPoints;
	[System.NonSerialized] public SpawnPointPlayer[] BadSpawnPoints;

	public MeshRenderer[] ChangeColorOn;

	// special mesh saving us few rendercalls - it use texture offsets
	public MeshRenderer FlagBaseSingleMesh;

	public MeshRenderer FlagIcon;

	public E_Team FlagOwner { get; private set; }
	// is valid only when flag is on the TOP or  going down  (have to be set only in onBottom, onTop
	public E_Team AlmostOwner { get; private set; } // is valid between bottom and top 
	public E_Team AreaOwner { get; private set; } //is valid when only players from one team are there 		

	E_Team m_ClientTargetTeam;
	int m_ClientFlagProgress;
	bool m_CoroutineInProgress;
	bool m_IsChanging;

	bool m_ForceSerialize;

	Material FlagBaseMaterial;

	static int m_BaseFlagSteps = 5;

	NetworkView NetworkView;
	Transform Transform;
	Vector3 Center;

	float Progress;
	float BaseHeight;

	E_AnimationState AnimState = E_AnimationState.None;

	public static Dictionary<E_Team, Color> Colors = new Dictionary<E_Team, Color>(TeamComparer.Instance);
	public static Dictionary<E_Team, Vector2> FlagUV = new Dictionary<E_Team, Vector2>(TeamComparer.Instance);
	public static Dictionary<E_Team, float> FlagBaseUV = new Dictionary<E_Team, float>(TeamComparer.Instance);

	Dictionary<E_Team, int> Temp = new Dictionary<E_Team, int>(TeamComparer.Instance);

	float DelayTimer = 0;
#if !DEADZONE_CLIENT
    float FlagSpeed { get { return 1.0f / Server.Instance.GameInfo.ZoneControlSetup.FlagRaiseTime; } }
#endif

	public Vector3 HudIconPosition
	{
		get { return Transform.position + Vector3.up*2; }
	}

	static ZoneControlFlag()
	{
		Colors.Add(E_Team.None, new Color(1, 1, 1));
		Colors.Add(E_Team.Bad, new Color(1, 2/255.0f, 60/255.0f));
		Colors.Add(E_Team.Good, new Color(46/255.0f, 159/255.0f, 1));

		FlagUV.Add(E_Team.None, new Vector2(0.666f, 0));
		FlagUV.Add(E_Team.Bad, new Vector2(0.0f, 0));
		FlagUV.Add(E_Team.Good, new Vector2(0.333f, 0));

		FlagBaseUV.Add(E_Team.None, 0.0f);
		FlagBaseUV.Add(E_Team.Bad, 0.5f);
		FlagBaseUV.Add(E_Team.Good, 0.0f);
	}

	void Awake()
	{
		Transform = transform;

		if (null != FlagBaseSingleMesh)
		{
			FlagBaseMaterial = FlagBaseSingleMesh.material;
		}

		if (uLink.Network.isServer)
		{
			BadSpawnPoints = gameObject.GetComponentsInChildren<SpawnPointBadGuys>(true);
			GoodSpawnPoints = gameObject.GetComponentsInChildren<SpawnPointGoodGuys>(true);

			Temp.Add(E_Team.Bad, 0);
			Temp.Add(E_Team.Good, 0);
		}

		NetworkView = networkView;

		NetworkView.stateSynchronization = uLink.NetworkStateSynchronization.Reliable;
		NetworkView.observed = this;

		Center = transform.position;

		BaseHeight = (null != Flag) ? Flag.localPosition.z : 1.0f;
		Reset();
	}

	public bool IsFullyControlledBy(E_Team team)
	{
		return (FlagOwner == team) && (Progress > 0.99f);
	}

	public void Reset()
	{
		DelayTimer = 0;
		AlmostOwner = FlagOwner = AreaOwner = E_Team.None;
		AnimState = E_AnimationState.None;

		m_ClientFlagProgress = -1;

		FlagOwner = StartBase;
		ClientSetFlag(FlagOwner);

		if (StartBase == E_Team.None)
		{
			Progress = 0;
			SetFlagProgress();
		}
		else
		{
			Progress = 1;
			SetFlagProgress();
		}

		m_ForceSerialize = true;

//        Debug.Log(name + "  Reset - " + FlagOwner);
		//Debug.Log( " ###### Reset() on " + this + " : " + Progress + " " + FlagOwner + " " + " " + AreaOwner + " " + " " + AlmostOwner );
	}

	void Update()
	{
		if (uLink.Network.isServer)
			ServerUpdate();
		else
			ClientUpdate();
	}

	void ServerUpdate()
	{
		if (DelayTimer > Time.timeSinceLevelLoad)
			return;

		E_Team oldAreaOwner = AreaOwner;
		E_Team oldAlmostOwner = AlmostOwner;
		E_Team oldFlagOwner = FlagOwner;

		AreaOwner = ServerGetAreaOwner();

		if (AreaOwner == E_Team.None || (AreaOwner == FlagOwner && Progress == 1.0f))
		{
			AnimState = E_AnimationState.None;
		}
		else if (AreaOwner == AlmostOwner && Progress != 1)
		{
			AnimState = E_AnimationState.Up;
		}
		else if (AreaOwner != E_Team.None && AlmostOwner == E_Team.None && FlagOwner == E_Team.None)
		{
			AnimState = E_AnimationState.Up;
		}
		else
		{
			AnimState = E_AnimationState.Down;
		}

		float oldProgress = Progress;

		if (AnimState == E_AnimationState.Down)
		{
			ServerGoDown();
		}
		else if (AnimState == E_AnimationState.Up)
		{
			ServerGoUp();
		}

		// serializing code is not called ever frame. In case of some change 
		// we should deliver this change to clients
		if (false == m_IsChanging)
		{
			if (m_ForceSerialize)
			{
				m_IsChanging = true;

				m_ForceSerialize = false;
			}
			else
			{
				m_IsChanging = oldProgress != Progress || oldFlagOwner != FlagOwner || oldAlmostOwner != AlmostOwner || oldAreaOwner != AreaOwner;
			}
		}
	}

	void ClientUpdate()
	{
		if (m_ClientFlagProgress > 0 && m_ClientFlagProgress <= m_BaseFlagSteps && false == m_CoroutineInProgress)
		{
			int time = (int)(3*Time.timeSinceLevelLoad);

			if ((time & 1) > 0)
			{
				__ClientFlagWorker(m_ClientFlagProgress);
			}
			else
			{
				__ClientFlagWorker(m_ClientFlagProgress - 1);
			}
		}
	}

	void ServerBufferLastState()
	{
		uLink.Network.RemoveRPCsByName(NetworkView.viewID, "LastState");

		NetworkView.RPC("LastState", RPCMode.Buffered, Progress, FlagOwner, AreaOwner, AlmostOwner);
	}

	void uLink_OnSerializeNetworkView(uLink.BitStream stream, uLink.NetworkMessageInfo info)
	{
		// Check if we should write to the stream, or read from the stream.
		if (stream.isWriting)
		{
			// This is performed on serialization.
			if (m_IsChanging)
			{
				stream.WriteSingle(Progress);
				stream.Write<E_Team>(FlagOwner);
				stream.Write<E_Team>(AreaOwner);
				stream.Write<E_Team>(AlmostOwner);

				ServerBufferLastState();

				m_IsChanging = false;
			}
		}
		else
		{
			//Debug.Log( " ###### Serialize() on " + this );

			Progress = stream.ReadSingle();
			FlagOwner = stream.Read<E_Team>();
			AreaOwner = stream.Read<E_Team>();
			AlmostOwner = stream.Read<E_Team>();

			//SetFlagProgress();

			if (AlmostOwner != E_Team.None)
				ClientSetFlag(AlmostOwner);
			else if (FlagOwner != E_Team.None)
				ClientSetFlag(FlagOwner);
			else
				ClientSetFlag(E_Team.None);

			SetFlagProgress();
		}
	}

	[uSuite.RPC]
	void LastState(float progress, E_Team flagOwner, E_Team areaOwner, E_Team almostOwner)
	{
		Progress = progress;
		FlagOwner = flagOwner;
		AreaOwner = areaOwner;
		AlmostOwner = almostOwner;

		//SetFlagProgress();

		if (AlmostOwner != E_Team.None)
			ClientSetFlag(AlmostOwner);
		else if (FlagOwner != E_Team.None)
			ClientSetFlag(FlagOwner);
		else
			ClientSetFlag(E_Team.None);

		SetFlagProgress();

		//Debug.Log( " ###### LastState() on " + this + " : " + progress + " " + FlagOwner + " " + " " + AreaOwner + " " + " " + AlmostOwner );
	}

	void ServerGoUp()
	{
		if (uLink.Network.isServer == false)
			throw new uLink.NetworkException(name + " GoUp is called on client");

#if !DEADZONE_CLIENT
		if(Progress == 0)
			ServerOnLeavingBottom();

        Progress = Mathf.Lerp(0, 1, Progress + Time.deltaTime * FlagSpeed  * (1 + (Temp[AreaOwner] - 1) * 0.5f));
		
		SetFlagProgress();
		
        if (Progress == 1)
            ServerOnTop();
#endif
	}

	void ServerGoDown()
	{
		if (uLink.Network.isServer == false)
			throw new uLink.NetworkException(name + " GoDown is called on client");

#if !DEADZONE_CLIENT
		if(Progress == 1)
			ServerOnLeavingTop();

		Progress = Mathf.Lerp(0, 1, Progress - Time.deltaTime * FlagSpeed * (1 + (Temp[AreaOwner] - 1) * 0.5f));
		
		SetFlagProgress();
		
        if (Progress == 0)
            ServerOnBottom();
#endif
	}

	E_Team ServerGetAreaOwner()
	{
		int good = 0;
		int bad = 0;

		foreach (KeyValuePair<uLink.NetworkPlayer, ComponentPlayer> pair in Player.Players)
		{
			AgentHuman a = pair.Value.Owner;

			if (a.IsAlive == false)
			{
				continue;
			}

			if (a.Position.y - Center.y < -1)
				continue;

			if ((Center - a.Position).sqrMagnitude > Range*Range)
			{
				continue;
			}

			if (a.Team == E_Team.Bad)
			{
				bad++;
			}
			else
			{
				good++;
			}
		}

		Temp[E_Team.Good] = good;
		Temp[E_Team.Bad] = bad;

		if ((bad == 0 && good == 0) || (bad > 0 && good > 0)) // none or both teams are near
		{
			return E_Team.None;
		}

		return bad > 0 ? E_Team.Bad : E_Team.Good;
	}

	void SetFlagProgress()
	{
		if (null != Flag)
		{
			Flag.localPosition = new Vector3(0, 0, BaseHeight + FlagRaise*Progress);
		}

		if (true == uLink.Network.isClient)
		{
			if (null != FlagBaseMaterial)
			{
				int index = 0;

				if (Progress > 0)
				{
					index = (int)(m_BaseFlagSteps*Progress) + 1;
				}

				ClientUpdateProgressOnSingleMesh(index);
			}
		}
	}

	void ServerOnLeavingBottom()
	{
		AlmostOwner = AreaOwner;
	}

	void ServerOnLeavingTop()
	{
		AlmostOwner = FlagOwner;
	}

	void ServerOnTop()
	{
		if (uLink.Network.isServer == false)
			throw new uLink.NetworkException(name + " GoDown is called on client");

		if (FlagOwner == E_Team.None)
		{
			FlagOwner = AlmostOwner;
			ServerDistributePoint(E_ZoneControlEvent.Owned, FlagOwner);
		}

		NetworkView.RPC("ClientInfo", uLink.RPCMode.Others, FlagOwner);
		AlmostOwner = E_Team.None;
	}

	void ServerOnBottom()
	{
		if (uLink.Network.isServer == false)
			throw new uLink.NetworkException(name + " GoDown is called on client");

		if (FlagOwner != E_Team.None)
			ServerDistributePoint(E_ZoneControlEvent.FlagNeutral, AreaOwner);

		FlagOwner = E_Team.None;
		AlmostOwner = E_Team.None;

		NetworkView.RPC("ClientInfo", uLink.RPCMode.Others, E_Team.None);

		DelayTimer = Time.timeSinceLevelLoad + 2.5f;
	}

	void ServerDistributePoint(E_ZoneControlEvent action, E_Team winner)
	{
		foreach (KeyValuePair<uLink.NetworkPlayer, ComponentPlayer> pair in Player.Players)
		{
			AgentHuman a = pair.Value.Owner;
			if (a.IsAlive == false)
				continue;

			if (a.Team != winner)
				continue;

			if ((Center - a.Position).sqrMagnitude > Range*Range)
				continue;

			PPIManager.Instance.ServerAddScoreForZoneControl(action, a.networkView.owner);
		}
	}

	[uSuite.RPC]
	void ClientInfo(E_Team owner)
	{
		//Debug.Log( " ###### ClientInfo() on " + this + " : " + Progress + " " + FlagOwner + " " + " " + AreaOwner + " " + " " + AlmostOwner );

		if (HudComponentConsole.Instance != null)
		{
			switch (owner)
			{
			case E_Team.Good:
				HudComponentConsole.Instance.ShowMessage(
														 TextDatabase.instance[ZoneNameIndex] + " " + TextDatabase.instance[MPMessages.ZoneControlledBy] + " " +
														 TextDatabase.instance[MPMessages.TeamGood],
														 Color.blue);
				break;
			case E_Team.Bad:
				HudComponentConsole.Instance.ShowMessage(
														 TextDatabase.instance[ZoneNameIndex] + " " + TextDatabase.instance[MPMessages.ZoneControlledBy] + " " +
														 TextDatabase.instance[MPMessages.TeamBad],
														 Color.red);
				break;
			default:
				HudComponentConsole.Instance.ShowMessage(
														 TextDatabase.instance[ZoneNameIndex] + " " + TextDatabase.instance[MPMessages.ZoneNeutralized],
														 Color.white);
				break;
			}
		}

		FlagOwner = owner;
		AlmostOwner = E_Team.None;
		AreaOwner = E_Team.None;
		ClientSetFlag(FlagOwner);

		if (owner == E_Team.None)
		{
			Client.Instance.PlaySoundFlagNeutral();
			Progress = 0;
		}
		else
		{
			Progress = 1;
			if (PPIManager.Instance.GetLocalPPI().Team == owner)
				Client.Instance.PlaySoundFlagOwned();
			else
				Client.Instance.PlaySoundFlagLost();
		}

		SetFlagProgress();
	}

	void ClientSetFlag(E_Team owner)
	{
		if (uLink.Network.isServer)
			return;

		foreach (Renderer renderer in ChangeColorOn)
			renderer.material.color = Colors[owner];

		FlagIcon.material.SetTextureOffset("_MainTex", FlagUV[owner]);

		if (owner != m_ClientTargetTeam)
		{
			m_ClientTargetTeam = owner;
			m_ClientFlagProgress = -1;
		}

		//Debug.Log(name + " new flag owner " + owner);

		//Debug.Log( " ###### ClientSetFlag() on " + this + " : " + Progress + " " + FlagOwner + " " + " " + AreaOwner + " " + " " + AlmostOwner );
	}

	public float GetDistanceToLocalPlayer()
	{
		if (Player.LocalInstance == null)
			return 0;

		return Vector3.Magnitude(Player.LocalInstance.Owner.Transform.position - Transform.position);
	}

	// -----
	void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, Range);
		Gizmos.color = Color.red - new Color(0, 0, 0, 0.5f);
		Gizmos.DrawSphere(transform.position, Range);
	}

	void ClientUpdateProgressOnSingleMesh(int progress)
	{
		if (progress == m_ClientFlagProgress)
		{
			return;
		}

		bool up = (progress > m_ClientFlagProgress) ? true : false;

		if (up && m_ClientFlagProgress >= 0)
		{
			__ClientFlagWorker(m_ClientFlagProgress);
		}
		else
		{
			__ClientFlagWorker(progress);
		}

		m_ClientFlagProgress = progress;

		StopAllCoroutines();

		m_CoroutineInProgress = true;

		StartCoroutine(FlashBase((0 == m_ClientFlagProgress) || ((m_BaseFlagSteps + 1) == m_ClientFlagProgress)));
	}

	IEnumerator FlashBase(bool longFlash)
	{
		if (null != FlagBaseMaterial)
		{
			Vector4 tmp = FlagBaseMaterial.GetVector("_IntensityScaleBias");

			float power = 0.0f;

			while (power < 1.0f)
			{
				yield return new WaitForEndOfFrame();
				power = Mathf.Clamp01(power + Time.deltaTime*(longFlash ? 3.0f : 5.0f));
				tmp.x = 1.0f + power*3.0f;
				FlagBaseMaterial.SetVector("_IntensityScaleBias", tmp);
			}

			yield return new WaitForEndOfFrame();

			if (longFlash)
			{
				yield return new WaitForSeconds(0.5f);
			}

			while (power > 0.0f)
			{
				yield return new WaitForEndOfFrame();
				power = Mathf.Clamp01(power - Time.deltaTime*(longFlash ? 1.0f : 2.0f));
				tmp.x = 1.0f + power*3.0f;
				FlagBaseMaterial.SetVector("_IntensityScaleBias", tmp);
			}
		}

		m_CoroutineInProgress = false;
	}

	void __ClientFlagWorker(int progress)
	{
		if (null != FlagBaseMaterial)
		{
			progress = Mathf.Clamp(progress, 0, m_BaseFlagSteps);

			Vector2 offset;
			offset.x = FlagBaseUV[m_ClientTargetTeam];
			offset.y = -progress*0.1f;
			FlagBaseMaterial.SetTextureOffset("_MainTex", offset);
		}
	}
}
