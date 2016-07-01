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

public class SpawnZoneButtonEx
{
	readonly string[] ButtonLabels = new string[6] {"A", "B", "C", "D", "E", "F"};

	public delegate void SetSpawnZoneIndexDelegate(int inZoneIndex);

	GUIBase_Button m_Button;
	ZoneControlFlag m_Zone;
	int m_ZoneIndex;
	SetSpawnZoneIndexDelegate m_SetSpawnIndex;
//	private string 						dbgName;

	public SpawnZoneButtonEx(GUIBase_Button inButton,
							 string inZoneName,
							 int inZoneIndex,
							 ZoneControlFlag inZoneControl,
							 SetSpawnZoneIndexDelegate inDelegate)
	{
		m_Button = inButton;
		m_Zone = inZoneControl;
		m_ZoneIndex = inZoneIndex;
		m_SetSpawnIndex = inDelegate;
//		dbgName 		= inZoneName;

		if (m_Button != null)
		{
			m_Button.RegisterTouchDelegate2(OnSelect);

			// Capa: temporary ?!? solution
			GUIBase_Label label = m_Button.transform.GetChildComponent<GUIBase_Label>("Spawn_Label");
			if (label != null)
				label.SetNewText(ButtonLabels[m_ZoneIndex]);
		}
	}

	public void OnSelect(GUIBase_Widget w)
	{
//		Debug.Log("Select zone: " + dbgName);
		m_SetSpawnIndex(m_ZoneIndex);
	}

	public void Update(E_Team inMyTeam, int inSpawnIndex)
	{
		if (m_Button != null)
		{
			if (m_Zone == null)
			{
				m_Button.Widget.Show(false, true);
			}
			else if (m_Zone.FlagOwner == E_Team.None)
			{
				m_Button.Widget.Color = Color.black;
				m_Button.SetDisabled(true);
			}
			else if (inSpawnIndex == m_ZoneIndex)
			{
				m_Button.Widget.Color = Color.yellow;
				m_Button.SetDisabled(true);
			}
			else
			{
				m_Button.Widget.Color = m_Zone.FlagOwner == E_Team.Good ? Color.blue : Color.red;
				m_Button.SetDisabled(m_Zone.FlagOwner != inMyTeam);
			}
		}
	}
}

public class ScreenComponent_Map : ScreenComponent
{
	// -----------------------------------------------------------------------------------------------------------------
	// TODO :: Add this strings into TextDatabase...
	//private static string TEXT_GAME_TYPE_DOMINATION = "DOMINATION";
	//private static string TEXT_GAME_TYPE_DEADMATCH  = "DEADMATCH";

	// -----------------------------------------------------------------------------------------------------------------
	GUIBase_Label m_MapName;
	//private GUIBase_Label						m_GameMode;
	SpawnZoneButtonEx[] m_SpawnButtons = new SpawnZoneButtonEx[6];

	// -----------------------------------------------------------------------------------------------------------------
	E_Team m_SelectedTeam = E_Team.None;
	int m_SpawnZoneIndex = -1;

	public override string ParentName
	{
		get { return "Map"; }
	}

	public override float UpdateInterval
	{
		get { return 0.5f; }
	}

	// -----------------------------------------------------------------------------------------------------------------	
	// internal interface...
	protected override bool OnInit()
	{
		if (base.OnInit() == false)
			return false;

		m_MapName = Parent.transform.GetChildComponent<GUIBase_Label>("MapName_Label");
		UpdateMapName();

		//m_GameMode = parent.transform.GetChildComponent<GUIBase_Label>("MapMode_Label");
		UpdateGameTypeName();

		SetTeam(GetPreferredTeam());
		UpdateTeamButtons();

		m_SpawnZoneIndex = GetPreferredZone();
		InitZoneButtons();

		// TODO :: 
		return true;
	}

	protected override void OnShow()
	{
		base.OnShow();

		// TODO ...
		SetTeam(GetPreferredTeam());
		m_SpawnZoneIndex = GetPreferredZone();

		UpdateTeamButtons();
		ValidateSpawnZone();

		UpdateZoneButtons(m_SelectedTeam, m_SpawnZoneIndex);
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		E_Team myTeam = PPIManager.Instance.GetLocalPPI().Team;
		if (myTeam != E_Team.None && myTeam != m_SelectedTeam)
		{
			m_SelectedTeam = myTeam;
		}

		UpdateTeamButtons();
		ValidateSpawnZone();

		UpdateZoneButtons(m_SelectedTeam, m_SpawnZoneIndex);
	}

	// =================================================================================================================
	// === internal ====================================================================================================
	int GetPreferredZone()
	{
		if (Mission.Instance == null || (Mission.Instance.GameZone as GameZoneZoneControl) == null)
			return -1;

		List<ZoneControlFlag> spawnZones = (Mission.Instance.GameZone as GameZoneZoneControl).Zones;
		if (spawnZones == null)
			return -1;

		int index = PPIManager.Instance.GetLocalPPI().ZoneIndex;
		if (index < 0 || index >= spawnZones.Count)
		{
			return -1;
		}

		return index;
	}

	E_Team GetPreferredTeam()
	{
		if (PPIManager.Instance.GetLocalPPI().Team != E_Team.None)
			return PPIManager.Instance.GetLocalPPI().Team;
		else
		{
			int bad = 0;
			int good = 0;
			List<PlayerPersistantInfo> netPlayers = PPIManager.Instance.GetPPIList();
			foreach (PlayerPersistantInfo ppi in netPlayers)
			{
				if (ppi.Team == E_Team.Bad)
					bad++;
				if (ppi.Team == E_Team.Good)
					good++;
			}

			E_Team preferedTeam = bad < good ? E_Team.Bad : E_Team.Good;
			return preferedTeam;
		}
	}

	void ValidateSpawnZone()
	{
		if (Mission.Instance == null)
			return;

		GameZoneZoneControl gz = Mission.Instance.GameZone as GameZoneZoneControl;
		if (gz == null || gz.Zones == null || gz.Zones.Count <= 0)
			return;

		List<ZoneControlFlag> spawnZones = gz.Zones;

		if (m_SpawnZoneIndex < 0 || spawnZones.Count <= m_SpawnZoneIndex)
			m_SpawnZoneIndex = -1;
		else if (spawnZones[m_SpawnZoneIndex] == null || spawnZones[m_SpawnZoneIndex].FlagOwner != m_SelectedTeam)
			m_SpawnZoneIndex = -1;

		if (m_SpawnZoneIndex < 0)
		{
			if (m_SelectedTeam == E_Team.Good)
			{
				// get first availible zone, forward...

				for (int i = 0; i < spawnZones.Count; i++)
				{
					if (spawnZones[i] == null || spawnZones[i].FlagOwner != m_SelectedTeam)
						continue;

					m_SpawnZoneIndex = i;
					break;
				}
			}
			else if (m_SelectedTeam == E_Team.Bad)
			{
				// get first availible zone, backward...

				for (int i = spawnZones.Count - 1; i >= 0; i--)
				{
					if (spawnZones[i] == null || spawnZones[i].FlagOwner != m_SelectedTeam)
						continue;

					m_SpawnZoneIndex = i;
					break;
				}
			}
		}
	}

	// -----------------------------------------------------------------------------------------------------------------
	// gui...
	void UpdateMapName()
	{
		// set map name ...
		if (Client.Instance != null)
		{
			m_MapName.SetNewText(Client.Instance.GameState.LevelName);
		}
		else
		{
			m_MapName.SetNewText("Unknown");
		}
	}

	void UpdateGameTypeName()
	{
		// THIS IS NOT NECESSARY...

		// Set Map Mode name ...
		/*if(Client.Instance != null)
		{
			switch(Client.Instance.GameState.GameType)
			{
				case E_MPGameType.DeathMatch:
					m_GameMode.SetNewText(TEXT_GAME_TYPE_DEADMATCH);
					break;
				case E_MPGameType.ZoneControl:	
					m_GameMode.SetNewText(TEXT_GAME_TYPE_DOMINATION);
					break;
				default:
					Debug.LogError("Unknown Game Type ");				
					m_GameMode.SetNewText("Unknown");					
					break;
			}
		}
		else
		{
			m_GameMode.SetNewText("Unknown");
		}*/
	}

	void UpdateTeamButtons()
	{
		//m_SelectTeamA.Widget.Color = m_SelectedTeam == E_Team.Good ? Color.yellow : Color.gray;
		//m_SelectTeamB.Widget.Color = m_SelectedTeam == E_Team.Bad  ? Color.yellow : Color.gray;
	}

	void InitZoneButtons()
	{
		GameZoneZoneControl gz = null;

		if (Mission.Instance != null)
		{
			gz = Mission.Instance.GameZone as GameZoneZoneControl;
		}

		// initialize spawn zone buttons...
		List<ZoneControlFlag> spawnZones = (gz != null) ? gz.Zones : null;
		for (int i = 0; i < m_SpawnButtons.Length; i++)
		{
			int zoneIndex = i;
			string buttonName = "MapSpawn_" + (i + 1) + "_Button";
			GUIBase_Button button = Parent.transform.GetChildComponent<GUIBase_Button>(buttonName);
			ZoneControlFlag zone = (spawnZones != null && i < spawnZones.Count && spawnZones[i] != null) ? spawnZones[i] : null;

			m_SpawnButtons[i] = new SpawnZoneButtonEx(button, buttonName, zoneIndex, zone, SetSpawnIndex);
		}

		UpdateZoneButtons(m_SelectedTeam, m_SpawnZoneIndex);
	}

	void UpdateZoneButtons(E_Team inTeam, int inSpawnIndex)
	{
		foreach (SpawnZoneButtonEx b in m_SpawnButtons)
		{
			if (b != null)
			{
				b.Update(inTeam, inSpawnIndex);
			}
		}
	}

	// -----------------------------------------------------------------------------------------------------------------
	// delegates...	
	public void SetSpawnIndex(int inSpawnIndex)
	{
		m_SpawnZoneIndex = inSpawnIndex;
		// TODO ...
		PPIManager.Instance.GetLocalPPI().ZoneIndex = m_SpawnZoneIndex;

		UpdateZoneButtons(m_SelectedTeam, m_SpawnZoneIndex);
	}

	void OnSelectTeam_A(GUIBase_Widget inInstigator)
	{
		SetTeam(E_Team.Good);
	}

	void OnSelectTeam_B(GUIBase_Widget inInstigator)
	{
		SetTeam(E_Team.Bad);
	}

	void SetTeam(E_Team inNewTeam)
	{
		m_SelectedTeam = inNewTeam;

		if (PPIManager.Instance.GetLocalPPI().Team != inNewTeam)
		{
			PPIManager.Instance.GetLocalPPI().Team = E_Team.None;
			Client.Instance.SendRequestForTeamSwitch(inNewTeam);
		}
	}
}
