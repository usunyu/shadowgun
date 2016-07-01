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

public class ScreenComponent_Map2 : ScreenComponent
{
	//==================================================================================================================

	const int TextID_DeathMatch = 0109009;
	const int TextID_Domination = 0109010;

	readonly static Color Color_ZoneVacant = new Color(128.0f/255, 128.0f/255, 128.0f/255);
	readonly static Color Color_ZoneTeamRed = new Color(219.0f/255, 5.0f/255, 30.0f/255);
	readonly static Color Color_ZoneTeamBlue = new Color(0.0f/255, 160.0f/255, 219.0f/255);

	//==================================================================================================================

	class SpawnZoneButton
	{
		public delegate void SetSpawnZoneIndex(int inZoneIndex);

		GUIBase_Button m_Button;
		GUIBase_Label m_Label;
		GUIBase_Sprite m_Sprite;
		ZoneControlFlag m_Zone;
		int m_ZoneIndex;
		SetSpawnZoneIndex m_SetSpawnIndex;

		public ZoneControlFlag Zone
		{
			get { return m_Zone; }
		}

		public int ZoneIndex
		{
			get { return m_ZoneIndex; }
		}

		public string Label
		{
			get { return m_Label.GetText(); }
		}

		//--------------------------------------------------------------------------------------------------------------
		public SpawnZoneButton(GUIBase_Widget inRootWidget, ZoneControlFlag inZoneControl, int inZoneIndex, SetSpawnZoneIndex inDelegate)
		{
			m_Button = inRootWidget.transform.GetChildComponent<GUIBase_Button>("Button");

			if (m_Button != null)
			{
				m_Button.RegisterTouchDelegate2(OnSelect);

				m_Zone = inZoneControl;
				m_ZoneIndex = inZoneIndex;
				m_SetSpawnIndex = inDelegate;

				m_Label = inRootWidget.transform.GetChildComponent<GUIBase_Label>("Label");
				m_Sprite = inRootWidget.transform.GetChildComponent<GUIBase_Sprite>("Sprite_Selected");

				if (m_Sprite != null)
				{
					m_Sprite.Widget.Show(false, true);
				}
			}
		}

		//--------------------------------------------------------------------------------------------------------------
		void OnSelect(GUIBase_Widget inWidget)
		{
			m_SetSpawnIndex(m_ZoneIndex);
		}

		//--------------------------------------------------------------------------------------------------------------
		public void Update(E_Team inMyTeam, int inMySpawnIndex)
		{
			if (m_Zone.FlagOwner != E_Team.None)
			{
				m_Button.Widget.Color = (m_Zone.FlagOwner == E_Team.Good) ? Color_ZoneTeamBlue : Color_ZoneTeamRed;
			}
			else
			{
				m_Button.Widget.Color = Color_ZoneVacant;
			}

			m_Button.SetDisabled(m_Zone.FlagOwner != inMyTeam);

			if (m_Sprite != null)
			{
				m_Sprite.Widget.Show(m_ZoneIndex == inMySpawnIndex, true);
			}
		}
	}

	//==================================================================================================================

	GameObject m_Def = null;
	GUIBase_Widget m_MapParent = null;

	SpawnZoneButton[] m_SpawnButtons = null;

	int m_SpawnZoneIndex = -1;
	GUIBase_Label m_SpawnZoneLabel = null;
	bool m_SpawnZoneAutoselected = true;

	E_Team m_SelectedTeam = E_Team.None;

	public override string ParentName
	{
		get { return "Map"; }
	}

	public override float UpdateInterval
	{
		get { return 0.5f; }
	}

	//==================================================================================================================

	//------------------------------------------------------------------------------------------------------------------
	protected override bool OnInit()
	{
		if (base.OnInit() == false)
			return false;

		SetTeam(GetPreferredTeam());
		SetZone(GetPreferredZone());

		if (AttachMiniMap())
		{
			InitModeAndName();
			InitZoneButtons();
		}

		return true;
	}

	//------------------------------------------------------------------------------------------------------------------
	protected override void OnShow()
	{
		base.OnShow();

		SetTeam(GetPreferredTeam());
		SetZone(GetPreferredZone());

		UpdateZoneButtons();
	}

	//------------------------------------------------------------------------------------------------------------------
	protected override void OnUpdate()
	{
		base.OnUpdate();

		SetTeam(GetPreferredTeam());
		SetZone(GetPreferredZone());

		UpdateZoneButtons();
	}

	//------------------------------------------------------------------------------------------------------------------
	bool AttachMiniMap()
	{
		m_Def = GameObject.FindGameObjectWithTag("MapDefinition");

		if (m_Def == null)
		{
			Debug.LogWarning("Game object 'MapDefinition' not found!");
			return false;
		}

		// insert mini-map to corresponing screen-layout...

		m_MapParent = m_Def.transform.GetChildComponent<GUIBase_Widget>("Map");

		if (m_MapParent == null)
		{
			Debug.LogWarning("Widget 'Map' not found in 'MapDefinition'!");
			return false;
		}

		m_MapParent.Relink(Parent.Parent);

		return true;
	}

	//------------------------------------------------------------------------------------------------------------------
	void InitModeAndName()
	{
		// name...

		GUIBase_Label name = m_MapParent.transform.GetChildComponent<GUIBase_Label>("MapName_Label");

		if (name != null)
		{
			if (Client.Instance != null)
			{
				name.SetNewText(Client.Instance.GameState.LevelName);
			}
			else
			{
				name.SetNewText("Unknown");
			}
		}

		// mode...

		GUIBase_Label mode = m_MapParent.transform.GetChildComponent<GUIBase_Label>("MapMode_Label");

		if (mode != null)
		{
			string text = "Unknown";

			if (Client.Instance != null)
			{
				switch (Client.Instance.GameState.GameType)
				{
				case E_MPGameType.DeathMatch:
					text = TextDatabase.instance[TextID_DeathMatch];
					break;
				case E_MPGameType.ZoneControl:
					text = TextDatabase.instance[TextID_Domination];
					break;
				default:
					Debug.LogWarning("Unknown type of game! " + Client.Instance.GameState.GameType);
					break;
				}
			}

			mode.SetNewText(text);
		}
	}

	//------------------------------------------------------------------------------------------------------------------
	void InitZoneButtons()
	{
		// find death-match-map definition...

		GameZoneZoneControl gzc = null;

		if (Mission.Instance != null)
		{
			gzc = Mission.Instance.GameZone as GameZoneZoneControl;
		}

		if ((gzc == null) || (gzc.Zones == null))
			return;

		DominationMapDefinition def = m_Def.GetComponentInChildren<DominationMapDefinition>();

		if ((def == null) || (def.m_SpawnZones == null) || (def.m_SpawnZones.Length == 0))
			return;

		// init on mini-map buttons...

		int bNum = 0;

		m_SpawnButtons = new SpawnZoneButton[def.m_SpawnZones.Length];

		for (int i = 0; i < def.m_SpawnZones.Length; ++i)
		{
			DominationMapDefinition.SpawnZoneData zoneDef = def.m_SpawnZones[i];

			if ((zoneDef.m_SpawnZone == null) || (zoneDef.m_MiniMapButton == null))
			{
				Debug.LogWarning("MiniMapDefinition : Record #" + i + " in 'Spawn Zones' is invalid!");
				continue;
			}

			int zoneIndex = gzc.Zones.FindIndex(sz => sz == zoneDef.m_SpawnZone);

			if (zoneIndex == -1)
			{
				Debug.LogWarning("MiniMapDefinition : Record #" + i + " in 'Spawn Zones' is referencing 'unknown' zone!");
				continue;
			}

			m_SpawnButtons[i] = new SpawnZoneButton(zoneDef.m_MiniMapButton, zoneDef.m_SpawnZone, zoneIndex, OnZoneSelected);

			bNum++;
		}

		//	Debug.LogInfo( "Successfully created " + bNum + " spawn-zone buttons." );

		// init prev/next buttons...

		Transform child = m_MapParent.transform.FindChildByName("SpawnPoint_Enum");

		if (child != null)
		{
			GUIBase_Button prev = child.GetChildComponent<GUIBase_Button>("GUI_enum_left");
			GUIBase_Button next = child.GetChildComponent<GUIBase_Button>("GUI_enum_right");

			if (prev != null)
			{
				prev.RegisterTouchDelegate2(SelectPrevZone);
				prev.SetDisabled(bNum == 0);
			}
			if (next != null)
			{
				next.RegisterTouchDelegate2(SelectNextZone);
				next.SetDisabled(bNum == 0);
			}

			m_SpawnZoneLabel = child.GetChildComponent<GUIBase_Label>("Spawn_Label");

			if (m_SpawnZoneLabel != null)
			{
				m_SpawnZoneLabel.SetNewText(string.Empty);
			}
		}
	}

	//------------------------------------------------------------------------------------------------------------------
	void UpdateZoneButtons()
	{
		int num = m_SpawnButtons != null ? m_SpawnButtons.Length : 0;

		while (num-- > 0)
		{
			SpawnZoneButton btn = m_SpawnButtons[num];

			if (btn != null)
			{
				btn.Update(m_SelectedTeam, m_SpawnZoneIndex);
			}
		}
	}

	//------------------------------------------------------------------------------------------------------------------
	void SetTeam(E_Team inTeam)
	{
		if ((Client.Instance != null) && (Client.Instance.GameState.GameType != E_MPGameType.ZoneControl))
		{
			return;
		}

		m_SelectedTeam = inTeam;

		if (PPIManager.Instance.GetLocalPPI().Team != m_SelectedTeam)
		{
			PPIManager.Instance.GetLocalPPI().Team = E_Team.None;

			Client.Instance.SendRequestForTeamSwitch(m_SelectedTeam);
		}
	}

	//------------------------------------------------------------------------------------------------------------------
	void SetZone(int inZoneIndex)
	{
		if ((Client.Instance != null) && (Client.Instance.GameState.GameType != E_MPGameType.ZoneControl))
		{
			return;
		}

		m_SpawnZoneIndex = inZoneIndex;

		if (PPIManager.Instance.GetLocalPPI().ZoneIndex != m_SpawnZoneIndex)
		{
			PPIManager.Instance.GetLocalPPI().ZoneIndex = m_SpawnZoneIndex;

			UpdateZoneButtons();

			if (m_SpawnZoneLabel != null)
			{
				string text = m_SpawnZoneIndex != -1 ? GetSpawnZoneButton(m_SpawnZoneIndex).Label : string.Empty;

				m_SpawnZoneLabel.SetNewText(text);
			}

			//	Debug.Log( "SpawnZoneIndex : " + m_SpawnZoneIndex );
		}
	}

	//------------------------------------------------------------------------------------------------------------------
	E_Team GetPreferredTeam()
	{
		if ((Client.Instance != null) && (Client.Instance.GameState.GameType != E_MPGameType.ZoneControl))
		{
			return E_Team.None;
		}

		PlayerPersistantInfo playerPPI = PPIManager.Instance.GetLocalPPI();

		if (playerPPI.Team != E_Team.None)
		{
			return playerPPI.Team;
		}
		else
		{
			int bCounter = 0;
			int gCounter = 0;
			List<PlayerPersistantInfo> ppiList = PPIManager.Instance.GetPPIList();

			foreach (PlayerPersistantInfo ppi in ppiList)
			{
				if (ppi.Team == E_Team.Bad)
				{
					bCounter++;
				}
				else if (ppi.Team == E_Team.Good)
				{
					gCounter++;
				}
			}

			if (bCounter < gCounter)
				return E_Team.Bad;
			if (bCounter > gCounter)
				return E_Team.Good;

			int bScore = Client.Instance.GameState.ZCInfo.TeamScore[E_Team.Bad];
			int gScore = Client.Instance.GameState.ZCInfo.TeamScore[E_Team.Good];

			return (bScore < gScore) ? E_Team.Bad : E_Team.Good;
		}
	}

	//------------------------------------------------------------------------------------------------------------------
	int GetPreferredZone()
	{
		int idx = PPIManager.Instance.GetLocalPPI().ZoneIndex;
		SpawnZoneButton btn = GetSpawnZoneButton(idx);

		if ((m_SpawnZoneAutoselected == true) || (btn == null) || (btn.Zone.FlagOwner != m_SelectedTeam))
		{
			idx = GetNearestZoneToEnemy();

			m_SpawnZoneAutoselected = true;

			//	if ( idx != -1 ) // nearest to previous one
			//	{
			//		idx = GetNearestZone( idx );
			//	}
			//	else             // random if previous one is uknown
			//	{
			//		idx = GetNextZone( Random.Range(0,999), Random.value < 0.5f ? -1 : +1 );
			//	}
		}

		return idx;
	}

	//------------------------------------------------------------------------------------------------------------------
	void OnZoneSelected(int inZoneIndex)
	{
		SetZone(inZoneIndex);
		m_SpawnZoneAutoselected = false;
	}

	//------------------------------------------------------------------------------------------------------------------
	void SelectPrevZone(GUIBase_Widget inWidget)
	{
		SetZone(GetNextZone(m_SpawnZoneIndex, -1));
		m_SpawnZoneAutoselected = false;
	}

	//------------------------------------------------------------------------------------------------------------------
	void SelectNextZone(GUIBase_Widget inWidget)
	{
		SetZone(GetNextZone(m_SpawnZoneIndex, +1));
		m_SpawnZoneAutoselected = false;
	}

	//------------------------------------------------------------------------------------------------------------------
	int GetNextZone(int inStartIndex, int inDirection)
	{
		GameZoneZoneControl gzc = Mission.Instance.GameZone as GameZoneZoneControl;

		if (gzc != null)
		{
			SpawnZoneButton btn;
			int num = gzc.Zones.Count;
			int idx = inStartIndex;

			for (int i = num; i > 0; --i)
			{
				idx = (idx + inDirection + num)%num;
				btn = GetSpawnZoneButton(gzc.Zones[idx]);

				if ((btn != null) && (btn.Zone.FlagOwner == m_SelectedTeam))
				{
					return btn.ZoneIndex;
				}
			}
		}

		return -1;
	}

	//------------------------------------------------------------------------------------------------------------------
	int GetNearestZoneToEnemy()
	{
		int bestZone = -1;
		float bestDist = float.MaxValue;
		GameZoneZoneControl gzc = Mission.Instance.GameZone as GameZoneZoneControl;

		if ((m_SelectedTeam != E_Team.None) && (gzc != null) && (m_SpawnButtons != null))
		{
			for (int i = 0; i < m_SpawnButtons.Length; ++i)
			{
				SpawnZoneButton iBtn = m_SpawnButtons[i];

				if ((iBtn == null) || (iBtn.Zone == null) || (iBtn.Zone.FlagOwner != m_SelectedTeam))
					continue;

				if (bestZone == -1)
				{
					bestZone = iBtn.ZoneIndex; // there is at least one available zone
				}

				for (int j = 0; j < m_SpawnButtons.Length; ++j)
				{
					SpawnZoneButton jBtn = m_SpawnButtons[j];

					if ((jBtn == null) || (jBtn.Zone.FlagOwner == m_SelectedTeam))
						continue;

					float dist = (iBtn.Zone.gameObject.transform.position -
								  jBtn.Zone.gameObject.transform.position).sqrMagnitude;

					dist -= 1.0e6f*(int)jBtn.Zone.FlagOwner; // prefer zones taken by enemy over the unoccupied ones

					if (dist < bestDist)
					{
						bestDist = dist;
						bestZone = iBtn.ZoneIndex;
					}
				}
			}
		}

		return bestZone;
	}

	//------------------------------------------------------------------------------------------------------------------
	int GetNearestZone(int inStartIndex)
	{
		GameZoneZoneControl gzc = Mission.Instance.GameZone as GameZoneZoneControl;

		if (gzc != null)
		{
			SpawnZoneButton btn;
			int num = gzc.Zones.Count;
			int idx0 = inStartIndex;
			int dir0 = GetTeamDefensiveMarchDir();
			int idx1 = inStartIndex;
			int dir1 = GetTeamOffensiveMarchDir();

			for (int i = num; i > 0; --i) // prefer defense
			{
				idx0 = (idx0 + dir0 + num)%num;
				btn = GetSpawnZoneButton(gzc.Zones[idx0]);

				if ((btn != null) && (btn.Zone.FlagOwner == m_SelectedTeam))
				{
					return btn.ZoneIndex;
				}

				idx1 = (idx1 + dir1 + num)%num;
				btn = GetSpawnZoneButton(gzc.Zones[idx1]);

				if ((btn != null) && (btn.Zone.FlagOwner == m_SelectedTeam))
				{
					return btn.ZoneIndex;
				}
			}
		}

		return -1;
	}

	//------------------------------------------------------------------------------------------------------------------
	int GetTeamDefensiveMarchDir()
	{
		return -GetTeamOffensiveMarchDir();
	}

	//------------------------------------------------------------------------------------------------------------------
	int GetTeamOffensiveMarchDir()
	{
		// TODO: we should also consider "side" on map where our team started... if they are switching
		return m_SelectedTeam == E_Team.Good ? -1 : +1;
	}

	//------------------------------------------------------------------------------------------------------------------
	SpawnZoneButton GetSpawnZoneButton(ZoneControlFlag inSpawnZone)
	{
		int num = m_SpawnButtons != null ? m_SpawnButtons.Length : 0;

		while (num-- > 0)
		{
			SpawnZoneButton btn = m_SpawnButtons[num];

			if ((btn != null) && (btn.Zone == inSpawnZone))
			{
				return btn;
			}
		}

		return null;
	}

	//------------------------------------------------------------------------------------------------------------------
	SpawnZoneButton GetSpawnZoneButton(int inSpawnZoneIndex)
	{
		int num = m_SpawnButtons != null ? m_SpawnButtons.Length : 0;

		while (num-- > 0)
		{
			SpawnZoneButton btn = m_SpawnButtons[num];

			if ((btn != null) && (btn.ZoneIndex == inSpawnZoneIndex))
			{
				return btn;
			}
		}

		return null;
	}
}
