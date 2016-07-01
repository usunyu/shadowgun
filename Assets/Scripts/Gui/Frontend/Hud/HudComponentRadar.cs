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

public class HudComponentRadar : HudComponent
{
	// ---
	public class FlagInfo
	{
		// ---
		public FlagInfo(ZoneControlFlag flag)
		{
			Flag = flag;
			transform = Flag.transform;
			UpdateDistance();
			CoroutineActive = false;
		}

		// ---
		public void UpdateDistance()
		{
			Distance = Flag.GetDistanceToLocalPlayer();
		}

		Transform transform;
		public ZoneControlFlag Flag { get; private set; }

		public Vector3 Pos
		{
			get { return transform.position; }
		}

		public float Distance { get; private set; }

		public Color Color
		{
			get { return ZoneControlFlag.Colors[Flag.FlagOwner]; }
		}

		public Color ChangingToColor
		{
			get { return Flag.FlagOwner != E_Team.None ? ZoneControlFlag.Colors[E_Team.None] : ZoneControlFlag.Colors[Flag.AreaOwner]; }
		}

		public bool IsChanging
		{
			get { return Flag.AreaOwner != E_Team.None && Flag.AreaOwner != Flag.FlagOwner; }
		}

		public bool CoroutineActive { get; set; }
		public GUIBase_Sprite RadarFlag { get; set; }
	}

	// ---
	class RadarFriend
	{
		public GUIBase_MultiSprite m_MultiSprite { get; private set; }
		public E_CommandID m_Command { get; private set; }
		public AgentHuman m_Agent { get; private set; }
		public bool m_Used { get; private set; }
		public bool m_Changed { get; private set; }
		//Color						TeamCommandColor = new Color(0, 0.6f, 1.0f, 1.0f);

		// -----
		public RadarFriend(GUIBase_MultiSprite sprite)
		{
			m_MultiSprite = sprite;
			m_Agent = null;
			m_Used = false;
			m_Changed = true;
			m_MultiSprite.Widget.Show(false, false);
		}

		// -----
		public void Refresh(bool forcedVisibility)
		{
			if (((m_Agent == null) || !m_Agent.IsAlive || !m_Agent.IsFriend(Player.LocalInstance.Owner)) && m_Used)
			{
				m_Used = false;
				m_Changed = true;
			}

//			if (m_Changed)
//			{	
			if (!m_Used && (m_MultiSprite.Widget.IsVisible() || forcedVisibility))
				m_MultiSprite.Widget.Show(false, false);
			m_Changed = false;
//			}
		}

		// -----
		public void SetAgent(AgentHuman agent)
		{
			m_Agent = agent;
			m_Used = true;
			m_Changed = true;
			SetCommand(E_CommandID.Max);
		}

		// -----
		public void SetCommand(E_CommandID commandID)
		{
			m_Command = commandID;
			m_Changed = true;
			ApplyTeamCommand();
		}

		// -------
		void ApplyTeamCommand()
		{
			GUIBase_MultiSprite sprite = m_MultiSprite;
			PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(Player.LocalInstance.networkView.owner);
			E_Team playerTeam = (ppi != null) ? ppi.Team : E_Team.None;

			switch (m_Command)
			{
			case E_CommandID.Affirmative:
				sprite.State = "Affirmative";
				sprite.Widget.m_GuiWidgetLayer = 7;
				//sprite.Color = TeamCommandColor;
				//sprite.StartCoroutine( PulseObject(sprite.Widget) );
				break;
			case E_CommandID.Attack:
				sprite.State = "Attack";
				sprite.Widget.m_GuiWidgetLayer = 7;
				//sprite.Color = TeamCommandColor;
				//sprite.StartCoroutine( PulseObject(sprite.Widget) );
				break;
			case E_CommandID.Back:
				sprite.State = "Back";
				sprite.Widget.m_GuiWidgetLayer = 7;
				//sprite.Color = TeamCommandColor;
				//sprite.StartCoroutine( PulseObject(sprite.Widget) );
				break;
			case E_CommandID.CoverMe:
				sprite.State = "CoverMe";
				sprite.Widget.m_GuiWidgetLayer = 7;
				//sprite.Color = TeamCommandColor;
				//sprite.StartCoroutine( PulseObject(sprite.Widget) );
				break;
			case E_CommandID.Help:
				sprite.State = "Help";
				sprite.Widget.m_GuiWidgetLayer = 7;
				//sprite.Widget.Color = TeamCommandColor;
				//sprite.StartCoroutine( PulseObject(sprite.Widget) );
				break;
			case E_CommandID.Medic:
				sprite.State = "Medic";
				sprite.Widget.m_GuiWidgetLayer = 7;
				//sprite.Color = TeamCommandColor;
				//sprite.StartCoroutine( PulseObject(sprite.Widget) );
				break;
			case E_CommandID.Negative:
				sprite.State = "Negative";
				sprite.Widget.m_GuiWidgetLayer = 7;
				//sprite.Color = TeamCommandColor;
				//sprite.StartCoroutine( PulseObject(sprite.Widget) );
				break;
			case E_CommandID.OutOfAmmo:
				sprite.State = "OutOfAmmo";
				sprite.Widget.m_GuiWidgetLayer = 7;
				//sprite.Color = TeamCommandColor;
				//sprite.StartCoroutine( PulseObject(sprite.Widget) );
				break;
			default:
				if (m_Command != E_CommandID.Max)
					Debug.LogWarning("Unknown enum: " + m_Command);
				sprite.State = "Normal";
				sprite.Widget.m_GuiWidgetLayer = 6;
				//sprite.StopAllCoroutines();
				break;
			}
			sprite.Color = ZoneControlFlag.Colors[playerTeam];
			sprite.Widget.SetModify();
		}
	}

	List<FlagInfo> m_FlagData = new List<FlagInfo>();
	GUIBase_Widget Radar;
	GUIBase_Sprite RadarBkg;
	GUIBase_Sprite RadarCenter;
	GUIBase_Sprite Pulse;
	GUIBase_Sprite[] RadarEnemies;
	RadarFriend[] RadarFriends;
	GUIBase_Sprite[] RadarFlags;
	float PulseTimer;
	float RadarRange;
	bool HasDetector;

	public const float RadarMaxRange = 100.0f;

	const float PulseTime = 0.8f;
	const float PulseFadeTime = 1.0f;
	const float DelayBetweenPulses = 2.0f;
	const float PulseRadarHighlightBlendInStart = PulseTime - 0.2f;
	const float PulseRadarHighlightBlendInStop = PulseRadarHighlightBlendInStart + 0.4f;
	const float PulseRadarHighlightTime = PulseRadarHighlightBlendInStop + 0.1f;
	const float PulseRadarHighlightFade = PulseRadarHighlightTime + 0.4f;

	float RadarScreenRadius;
	float RadarCenterRadius;
	string s_PivotMainName = "MainHUD";
	string s_LayoutMainName = "HUD_Layout";
	string s_RadarName = "Radar";
	string s_RadarBackgroundName = "Background";
	string s_RadarCenterName = "Center";
	string s_PulseName = "Pulse";

	string[] s_RadarEnemyNames = new string[] {"Enemy1", "Enemy2", "Enemy3", "Enemy4", "Enemy5", "Enemy6"};
	string[] s_RadarFriendNames = new string[] {"Friend1", "Friend2", "Friend3", "Friend4", "Friend5", "Friend6"};
	string[] s_RadarFlagNames = new string[] {"Flag1", "Flag2", "Flag3", "Flag4", "Flag5", "Flag6"};

	public override float UpdateInterval
	{
		get { return 0.1f; }
	}

	// ---------------------------------------------------------------------------------------------------------------------------------
	// 						P U B L I C      P A R T
	// ---------------------------------------------------------------------------------------------------------------------------------

	// ---------
	protected override bool OnInit()
	{
		if (base.OnInit() == false)
			return false;

		//m_DetectedAgents = null;

		m_FlagData.Clear();
		GUIBase_Pivot pivot = MFGuiManager.Instance.GetPivot(s_PivotMainName);
		if (!pivot)
		{
			Debug.LogError("'" + s_PivotMainName + "' not found!!! Assert should come now");
			return false;
		}
		GUIBase_Layout layout = pivot.GetLayout(s_LayoutMainName);
		if (!layout)
		{
			Debug.LogError("'" + s_LayoutMainName + "' not found!!! Assert should come now");
			return false;
		}

		Radar = layout.GetWidget(s_RadarName).GetComponent<GUIBase_Widget>();
		RadarBkg = layout.GetWidget(s_RadarBackgroundName).GetComponent<GUIBase_Sprite>();
		RadarCenter = layout.GetWidget(s_RadarCenterName).GetComponent<GUIBase_Sprite>();
		Pulse = layout.GetWidget(s_PulseName).GetComponent<GUIBase_Sprite>();
		Pulse.transform.localScale = Vector3.zero;
		Pulse.Widget.SetModify();
		PulseTimer = 0;

		RadarEnemies = new GUIBase_Sprite[s_RadarEnemyNames.Length];
		int index = 0;
		foreach (string name in s_RadarEnemyNames)
		{
			RadarEnemies[index++] = layout.GetWidget(name).GetComponent<GUIBase_Sprite>();
		}

		PlayerPersistantInfo ppi = (Player.LocalInstance) ? PPIManager.Instance.GetPPI(Player.LocalInstance.networkView.owner) : null;
		//E_Team 					playerTeam	= (ppi != null) ? ppi.Team : E_Team.None;	
		RadarFriends = new RadarFriend[s_RadarFriendNames.Length];
		index = 0;
		foreach (string name in s_RadarFriendNames)
		{
			RadarFriends[index] = new RadarFriend(layout.GetWidget(name).GetComponent<GUIBase_MultiSprite>());
			++index;
		}

		// ------
		GameZoneZoneControl zone = Mission.Instance.GameZone as GameZoneZoneControl;
		if (zone != null)
		{
			foreach (ZoneControlFlag flag in zone.Zones)
			{
				m_FlagData.Add(new FlagInfo(flag));
			}
		}
		foreach (FlagInfo flagInfo in m_FlagData)
		{
			index = flagInfo.Flag.ZoneNameIndex - 0500480;
			if ((index < 0) || (index > s_RadarFlagNames.Length))
			{
				Debug.LogWarning("Can't translate Flag.ZoneNameIndex into index for radar!");
				m_FlagData.Clear();
				break;
			}
			else
			{
				flagInfo.RadarFlag = layout.GetWidget(s_RadarFlagNames[index]).GetComponent<GUIBase_Sprite>();
			}
		}
		RadarFlags = new GUIBase_Sprite[s_RadarFlagNames.Length];
		index = 0;
		foreach (string name in s_RadarFlagNames)
		{
			RadarFlags[index] = layout.GetWidget(name).GetComponent<GUIBase_Sprite>();
			++index;
		}
		// ------

		Transform radarTrans = Radar.transform;
		Vector3 lossyScale = radarTrans.lossyScale;

		//Debug.Log("Radar.Widget.m_Width: "+Radar.Widget.m_Width);
		Vector2 size = new Vector2(RadarBkg.Widget.GetWidth() - 60, RadarBkg.Widget.GetWidth() - 60);
						// layout.LayoutSpaceDeltaToScreen(new Vector2(RadarBkg.Widget.m_Width - 16, RadarBkg.Widget.m_Width - 16));
		size.x = size.x*lossyScale.x;
		size.y = size.y*lossyScale.y;
		RadarScreenRadius = size.x/2.0f;
		size = new Vector2(RadarCenter.Widget.GetWidth(), RadarCenter.Widget.GetWidth());
						//layout.LayoutSpaceDeltaToScreen(new Vector2(RadarCenter.Widget.m_Width, RadarCenter.Widget.m_Width));
		size.x *= lossyScale.x;
		size.y *= lossyScale.y;
		RadarCenterRadius = size.x/2.0f;
		RadarScreenRadius -= RadarCenterRadius;

		RadarRange = RadarMaxRange;
		HasDetector = false;
		// -----
		if (Player.LocalInstance)
		{
			ppi = PPIManager.Instance.GetPPI(Player.LocalInstance.Owner.NetworkView.owner);
			foreach (PPIItemData d in ppi.EquipList.Items)
			{
				ItemSettings item = ItemSettingsManager.Instance.Get(d.ID);
				if (item.ItemBehaviour == E_ItemBehaviour.Detector)
				{
					HasDetector = true;
					break;
				}
			}
		}

		return true;
	}

	// ---------
	protected override void OnLateUpdate()
	{
		base.OnLateUpdate();

		UpdatePulse(Time.deltaTime);
	}

	// ---------
	protected override void OnShow()
	{
		base.OnShow();

		int index = 0;
		foreach (FlagInfo info in m_FlagData)
		{
			info.CoroutineActive = false;
			info.RadarFlag.Widget.StopAllCoroutines();
			info.RadarFlag.Widget.FadeAlpha = 1.0f;
			info.RadarFlag.Widget.Color = info.Color;
			++index;
		}
		ShowWidgets();

		foreach (GUIBase_Sprite sprite in RadarEnemies)
		{
			sprite.Widget.StartCoroutine(PulseObject(sprite.Widget));
		}
	}

	// ---------
	protected override void OnHide()
	{
		ShowWidgets();
		base.OnHide();

		foreach (GUIBase_Sprite sprite in RadarEnemies)
		{
			sprite.StopAllCoroutines();
		}
	}

	// ---------
	protected override void OnUpdate()
	{
		base.OnUpdate();

		UpdateRadarInternal();
	}

	// -------
	public void StartTeamCommand(AgentHuman agent, E_CommandID teamCommand)
	{
		foreach (RadarFriend friend in RadarFriends)
		{
			if (friend.m_Agent == agent)
			{
				friend.SetCommand(teamCommand);
				break;
			}
		}
		//Debug.Log("Cmd start "+agent+"   "+teamCommand);
	}

	// -------
	public void StopTeamCommand(AgentHuman agent)
	{
		foreach (RadarFriend friend in RadarFriends)
		{
			if (friend.m_Agent == agent)
			{
				friend.SetCommand(E_CommandID.Max);
				break;
			}
		}
		//Debug.Log("Cmd stop "+agent);
	}

	// ---------------------------------------------------------------------------------------------------------------------------------
	// 						P R I V A T E     P A R T
	// ---------------------------------------------------------------------------------------------------------------------------------

	// ---------
	void ShowWidgets()
	{
		Radar.Show(IsVisible, true);
		foreach (GUIBase_Sprite sprite in RadarFlags)
			sprite.Widget.Show(false, true);
		if (IsVisible)
			UpdateRadarInternal(true);
	}

	// ---------
	IEnumerator HighlightObject(GUIBase_Widget sprite, FlagInfo info)
	{
		while (true)
		{
			if (info.Color != info.ChangingToColor)
			{
				sprite.Color = info.ChangingToColor;
				yield return new WaitForSeconds(0.1f);
				sprite.Color = info.Color;
				yield return new WaitForSeconds(0.5f);
			}
			else
				yield return new WaitForSeconds(0.4f);

			if (!info.CoroutineActive)
			{
				//Debug.LogWarning( " ### HighLight out unusual ! ### ");
				break;
			}
		}
	}

	// ---------
	IEnumerator PulseObject(GUIBase_Widget widget)
	{
		yield return new WaitForSeconds(2.0f);
		while (true)
		{
			widget.FadeAlpha = 0;
			yield return new WaitForSeconds(0.1f);
			widget.FadeAlpha = 1.0f;
			yield return new WaitForSeconds(0.5f);
		}
	}

	// ---------
	T GetChildByName<T>(GameObject obj, string name) where T : Component
	{
		Transform t = obj.transform.Find(name);

		return (t != null) ? t.GetComponent<T>() : null;
	}

	// ---------
	void UpdatePulse(float deltaTime)
	{
		if (!HasDetector)
			return;

		PulseTimer += deltaTime;

		if (PulseTimer > DelayBetweenPulses)
		{
			PulseTimer -= DelayBetweenPulses;
			Pulse.Widget.Show(true, false);
			Pulse.Widget.FadeAlpha = 0.5f;
		}

		if (PulseTimer <= PulseTime)
		{
			float scale = 1 - Mathf.Cos((PulseTimer/PulseTime)*Mathf.PI/2);
			Pulse.transform.localScale = new Vector3(scale, scale, scale);
			Pulse.Widget.SetModify();
		}
		else if (PulseTimer <= PulseFadeTime)
		{
			Pulse.Widget.FadeAlpha = 0.5f*(1 - (PulseTimer - PulseTime)/(PulseFadeTime - PulseTime));
		}
		else if (PulseTimer <= DelayBetweenPulses)
		{
			Pulse.Widget.Show(false, false);
		}
	}

	// ---------
	float GetPulseModificator()
	{
		if (PulseTimer < PulseRadarHighlightBlendInStart)
			return 0.5f;
		if (PulseTimer < PulseRadarHighlightBlendInStop)
		{
			return 0.5f + ((PulseTimer - PulseRadarHighlightBlendInStart)/(PulseRadarHighlightBlendInStop - PulseRadarHighlightBlendInStart))*0.5f;
		}
		else if (PulseTimer <= PulseRadarHighlightTime)
			return 1.0f;
		else if (PulseTimer <= PulseRadarHighlightFade)
			return 0.5f + (1 - (PulseTimer - PulseRadarHighlightTime)/(PulseRadarHighlightFade - PulseRadarHighlightTime))*0.5f;
		else
			return 0.5f;
	}

	// --------
	void ShowRadarPos(GUIBase_Widget targetWidget, Vector3 targetPos, Vector3 centerPos, Vector3 centerDir)
	{
		Vector3 dirToEnemy = targetPos - centerPos;
		dirToEnemy.y = 0;
		float distance = dirToEnemy.magnitude;
		Vector3 finalPos;

		distance /= RadarRange;
		if (distance > 1.0f)
			distance = Mathf.Clamp(1.0f + (distance - 1.0f)/21f, 0, 1.1f);

		dirToEnemy.Normalize();
		float angle = Mathf.Atan2(-dirToEnemy.z, dirToEnemy.x) - Mathf.Atan2(-centerDir.z, centerDir.x);

		finalPos = new Vector3(Mathf.Sin(angle), -Mathf.Cos(angle), 0);
		finalPos *= distance*RadarScreenRadius + RadarCenterRadius;
		finalPos += RadarCenter.transform.position;

		Transform targetTrans = targetWidget.transform;
		if (targetTrans.position != finalPos)
		{
			targetTrans.position = finalPos;
			targetWidget.SetModify(true);
		}

		if (!targetWidget.IsVisible())
			targetWidget.Show(true, true);
	}

	// ---------
	void UpdateRadarInternal(bool forced = false)
	{
		if (!Camera.main || !Player.LocalInstance)
			return;

		foreach (FlagInfo info in m_FlagData)
			info.UpdateDistance();

		Transform playerTrans = Player.LocalInstance.transform;
		Vector3 playerPos = playerTrans.position;
		Vector3 playerDir = playerTrans.forward;
		playerDir.y = 0;

		// ----------
		foreach (FlagInfo info in m_FlagData)
		{
			ShowRadarPos(info.RadarFlag.Widget, info.Pos, playerPos, playerDir);
			if (info.IsChanging)
			{
				if (!info.CoroutineActive)
				{
					info.RadarFlag.Widget.StopAllCoroutines();
					info.CoroutineActive = true;
					info.RadarFlag.Widget.StartCoroutine(HighlightObject(info.RadarFlag.Widget, info));
				}
			}
			else
			{
				if (info.CoroutineActive)
				{
					info.RadarFlag.Widget.StopAllCoroutines();
					info.CoroutineActive = false;
				}
				info.RadarFlag.Widget.FadeAlpha = 1.0f;
				info.RadarFlag.Widget.Color = info.Color;
			}
		}

		PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(Player.LocalInstance.networkView.owner);
		E_Team playerTeam = (ppi != null) ? ppi.Team : E_Team.None;

		// ----------
		//int indexFriend = 0;
		int indexEnemy = 0;
		foreach (KeyValuePair<uLink.NetworkPlayer, ComponentPlayer> pair in Player.Players)
		{
			if (pair.Value == Player.LocalInstance)
				continue;
			AgentHuman a = pair.Value.Owner;
			if (a.IsAlive == false)
				continue;

			PlayerPersistantInfo ppi2 = PPIManager.Instance.GetPPI(pair.Key);
			if (ppi2 == null)
				continue;

			if (a.IsFriend(Player.LocalInstance.Owner))
			{
				// ----------
				RadarFriend free = null;
				bool found = false;
				foreach (RadarFriend friend in RadarFriends)
				{
					if (friend.m_Used && (friend.m_Agent == a))
					{
						found = true;
						break;
					}
					else if (!friend.m_Used)
						free = friend;
				}
				if (!found && (free != null))
					free.SetAgent(a);
				else
				{
					if (!found && (free == null))
						Debug.LogWarning("Free sprite for radar - friend not found!");
				}
			}
			else if (a.BlackBoard.IsDetected)
			{
				if (a.IsAlive == false)
					continue;

				if (a.GadgetsComponent.IsBoostActive(E_ItemBoosterBehaviour.Invisible) &&
					Player.LocalInstance.Owner.GadgetsComponent.GetGadget(E_ItemID.EnemyDetectorII) == null)
					continue;

				if (!a.IsFriend(Player.LocalInstance.Owner))
				{
					if (indexEnemy < RadarEnemies.Length)
					{
						E_Team enemyTeam = (playerTeam == E_Team.Bad) ? E_Team.Good : E_Team.Bad;
						ShowRadarPos(RadarEnemies[indexEnemy].Widget, a.Position, playerPos, playerDir);
						RadarEnemies[indexEnemy].Widget.Color = ZoneControlFlag.Colors[enemyTeam];
						++indexEnemy;
					}
				}
			}
		}

		// ----------
		foreach (RadarFriend friend in RadarFriends)
		{
			friend.Refresh(forced);
			if (friend.m_Used)
				ShowRadarPos(friend.m_MultiSprite.Widget, friend.m_Agent.Position, playerPos, playerDir);
		}

		// ----------
		for (; indexEnemy < RadarEnemies.Length; indexEnemy++)
		{
			if (RadarEnemies[indexEnemy].Widget.IsVisible() || forced)
				RadarEnemies[indexEnemy].Widget.Show(false, false);
		}
	}
}
