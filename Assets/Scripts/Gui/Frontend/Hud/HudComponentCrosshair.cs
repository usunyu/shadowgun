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

public class HudComponentCrosshair : HudComponent
{
	enum E_CrosshairType
	{
		None = -1,
		Normal = 0,
		Target,
		TargetMelee,
		Max
	};

	static string s_PivotMainName = "MainHUD";
	static string s_LayoutMainName = "HUD_Layout";
	static string[] s_CrosshairName = new string[(int)E_CrosshairType.Max] {"CrosshairNormal", "CrosshairTarget", "CrosshairTargetMelee"};
	static string s_CrosshairHit = "CrosshairHit";

	GUIBase_Sprite[] m_Crosshair = new GUIBase_Sprite[(int)E_CrosshairType.Max];
	//GUIBase_Widget[]	m_CrosshairTargetChilds;

	GUIBase_Sprite m_CrosshairHit;
	Transform m_CrosshairHitTransform;
	E_CrosshairType m_CrosshairType;
	bool m_MeleeRange = false;

	GUIBase_Widget m_EnemyLabel;
	GUIBase_Label m_EnemyLabelName;
	float m_EnemyLabelNameOrigScale;
	float m_EnemyLabelOrigWidth;

	GUIBase_Widget m_PrepareForFire;
	GUIBase_Widget m_PrepareForFireA;
	GUIBase_Widget m_PrepareForFireB;

	// ---------
	protected override bool OnInit()
	{
		if (base.OnInit() == false)
			return false;

		GUIBase_Pivot pivotMain = MFGuiManager.Instance.GetPivot(s_PivotMainName);
		if (!pivotMain)
		{
			Debug.LogError("'" + s_PivotMainName + "' not found!!! Assert should come now");
			return false;
		}
		GUIBase_Layout layoutMain = pivotMain.GetLayout(s_LayoutMainName);
		if (!layoutMain)
		{
			Debug.LogError("'" + s_LayoutMainName + "' not found!!! Assert should come now");
			return false;
		}

		m_EnemyLabel = layoutMain.GetWidget("EnemyName").GetComponent<GUIBase_Widget>();
		m_EnemyLabelName = layoutMain.GetWidget("EnemyNameLbl").GetComponent<GUIBase_Label>();
		m_EnemyLabelNameOrigScale = 0.7f; //m_EnemyLabelName.transform.localScale.x;
		m_EnemyLabelOrigWidth = m_EnemyLabel.GetWidth();

		m_PrepareForFire = layoutMain.GetWidget("PrepareForFire").GetComponent<GUIBase_Widget>();
		m_PrepareForFireA = layoutMain.GetWidget("PrepareForFireA").GetComponent<GUIBase_Widget>();
		m_PrepareForFireB = layoutMain.GetWidget("PrepareForFireB").GetComponent<GUIBase_Widget>();

		for (int idx = 0; idx < (int)E_CrosshairType.Max; ++idx)
		{
			if (s_CrosshairName.Length <= idx)
			{
				Debug.LogError("Crosshair names mishmash; there is not any name for " + (E_CrosshairType)idx + " crosshair specified !!!");
				break;
			}
			m_Crosshair[idx] = PrepareSprite(layoutMain, s_CrosshairName[idx]);
		}
		//m_CrosshairTargetChilds = m_Crosshair[(int)E_CrosshairType.Target].GetComponentsInChildren<GUIBase_Widget>();

		m_CrosshairHit = PrepareSprite(layoutMain, s_CrosshairHit);
		m_CrosshairHitTransform = m_CrosshairHit.transform;
		m_CrosshairType = E_CrosshairType.None;

		m_MeleeRange = false;

		return true;
	}

	protected override void OnDestroy()
	{
		m_CrosshairType = E_CrosshairType.None;

		base.OnDestroy();
	}

	// ------
	public bool WidgetInsideCrosshairArea(float width, float height, Transform widgetTransform, float crosshairAreaScale)
	{
		float crossWidthH = m_CrosshairHit.Widget.GetWidth()*m_CrosshairHitTransform.lossyScale.x*0.5f*crosshairAreaScale;
		float crossHeightH = m_CrosshairHit.Widget.GetHeight()*m_CrosshairHitTransform.lossyScale.y*0.5f*crosshairAreaScale;
		Vector3 crossPos = m_CrosshairHit.transform.position;
		//Debug.Log( "pos:" + pos + "  width:" + width +"  height:" + height );

		float widgetWidthH = width*widgetTransform.lossyScale.x*0.5f;
		float widgetHeightH = height*widgetTransform.lossyScale.y*0.5f;
		Vector3 widgetPos = widgetTransform.position;

		if (((widgetPos.x + widgetWidthH) >= (crossPos.x - crossWidthH)) && ((widgetPos.x - widgetWidthH) <= (crossPos.x + crossWidthH)) &&
			((widgetPos.y + widgetHeightH) >= (crossPos.y - crossHeightH)) && ((widgetPos.y - widgetHeightH) <= (crossPos.y + crossHeightH)))
			return true;
		else
			return false;
	}

	protected override void OnHide()
	{
		foreach (var crosshair in m_Crosshair)
		{
			if (crosshair != null)
			{
				crosshair.Widget.Show(false, true);
			}
		}

		m_CrosshairHit.Widget.Show(false, false);
		m_EnemyLabel.Show(false, true);

		if (m_PrepareForFire.IsVisible())
		{
			m_PrepareForFire.Show(false, true);
		}

		base.OnHide();
	}

	protected override void OnShow()
	{
		base.OnShow();

		ShowCrosshair(m_CrosshairType, true);
	}

	// ---------
	void ShowCrosshair(E_CrosshairType type, bool forceUpdate = false)
	{
		if ((type == E_CrosshairType.Target) && m_MeleeRange)
			type = E_CrosshairType.TargetMelee;
		else
			type = E_CrosshairType.Normal; // nechceme zacervenany kurzor 

		if ((m_CrosshairType == type) && !forceUpdate)
			return;
		//hide current type
		if (m_CrosshairType != E_CrosshairType.None && m_CrosshairType != type)
		{
			m_Crosshair[(int)m_CrosshairType].Widget.Show(false, true);
			// m_CrosshairHit.Widget.Show(false, false);
		}

		//show new
		if (type != E_CrosshairType.None)
		{
			m_Crosshair[(int)type].Widget.Show(true, true);
		}
		m_CrosshairType = type;
	}

	// ---------
	public void ShowMeleeCrosshair(bool on)
	{
		m_MeleeRange = on;
	}

	// ---------
	public override float UpdateInterval
	{
		get { return 0.06f; }
	}

	// ------
	protected override void OnUpdate()
	{
		base.OnUpdate();

		if (Camera.main == null)
			return;

		AgentHuman player = Owner.LocalPlayer.Owner;
		BlackBoard blackBoard = player.BlackBoard;

		if (player.LastHitTime + 0.15f > Time.timeSinceLevelLoad)
		{
			m_CrosshairHit.Widget.Show(true, true);
		}
		else
		{
			m_CrosshairHit.Widget.Show(false, true);
		}

		WeaponBase weapon = player.WeaponComponent.GetCurrentWeapon();

		if (!weapon || weapon.PreparedForFireProgress < 0 || !player.IsAlive)
		{
			if (m_PrepareForFire.IsVisible())
			{
				m_PrepareForFire.Show(false, true);
			}
		}
		else
		{
			if (!m_PrepareForFire.IsVisible())
				m_PrepareForFire.Show(true, true);
			float progress = weapon.PreparedForFireProgress;
			Color color = Color.white*(1 - progress) + Color.green*progress;
			Vector3 offset = new Vector3(75 + 70*(1 - progress), 0, 0);
			Vector3 basePos = m_PrepareForFire.transform.localPosition;

			m_PrepareForFireA.Color = color;
			m_PrepareForFireB.Color = color;
			m_PrepareForFireA.transform.localPosition = basePos - offset;
			m_PrepareForFireB.transform.localPosition = basePos + offset;
			m_PrepareForFire.SetModify(true);
		}

		const float crosshairDistance = 200.0f;
						//do jake vzdalenosti testujeme kolizi zda mirime na enemy (todo: mozna pouzit konstantu z UpdateIdealFireDir)
		bool aimingHit = false;
		bool showEnemyLabel = false;
		//test collision in aiming sight
		Ray ray = Camera.main.ScreenPointToRay(new Vector2(Screen.width/2, Screen.height/2));
		LayerMask mask = (ObjectLayerMask.Default | ObjectLayerMask.PhysicsMetal | ObjectLayerMask.Ragdoll);
		Vector3 hitPoint;
		GameObject go = CollisionUtils.FirstCollisionOnRay(ray, crosshairDistance, player.GameObject, out hitPoint, mask);
		Agent agent = null;
		if (go)
		{
			agent = GameObjectUtils.GetFirstComponentUpward<Agent>(go);
			if (!agent || (agent == Player.LocalInstance.Owner) || !agent.IsAlive || agent.IsFriend(player))
				agent = null;
		}
		if (!agent)
		{
			mask = ObjectLayerMask.Ragdoll;
			go = CollisionUtils.FirstSphereCollisionOnRay(ray, 0.6f, crosshairDistance, player.GameObject, out hitPoint, mask);
			if (go)
			{
				mask = ObjectLayerMask.Default | ObjectLayerMask.PhysicsMetal | ObjectLayerMask.Ragdoll;
				ray.direction = hitPoint - ray.origin;
				GameObject go2 = CollisionUtils.FirstCollisionOnRay(ray, crosshairDistance, player.GameObject, out hitPoint, mask); // to be accurate
				if (go2)
				{
					agent = GameObjectUtils.GetFirstComponentUpward<Agent>(go2);
					if (!agent || (agent == Player.LocalInstance.Owner) || !agent.IsAlive || agent.IsFriend(player))
						agent = null;
				}
			}
		}

		if (agent)
		{
			//is it enemy?
			//Sem pridavat dalsi typy enemacu co nejsou podedene z agenta
			Agent a = GameObjectUtils.GetFirstComponentUpward<Agent>(go);
			if (a && (a != Player.LocalInstance.Owner) && a.IsAlive && !a.IsFriend(player))
			{
				aimingHit = true;
				PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(a.networkView.owner);
				if (ppi != null)
				{
					SetTextAndAdjustBackground(ppi.NameForGui, m_EnemyLabelName, m_EnemyLabel, m_EnemyLabelOrigWidth, m_EnemyLabelNameOrigScale);
					showEnemyLabel = true;
				}
				E_Team enemyTeam = (ppi != null) ? ppi.Team : E_Team.None;
				if (Client.Instance.GameState.GameType == E_MPGameType.DeathMatch)
					enemyTeam = E_Team.Bad;
				m_EnemyLabel.Color = ZoneControlFlag.Colors[enemyTeam];
				m_CrosshairHit.Widget.Color = ZoneControlFlag.Colors[E_Team.Bad];
				//m_Crosshair[(int)E_CrosshairType.Target].Widget.Color = ZoneControlFlag.Colors[E_Team.Bad];
				//foreach (GUIBase_Widget widget in m_CrosshairTargetChilds)
				//	widget.Color = ZoneControlFlag.Colors[E_Team.Bad];
			}
		}
		m_EnemyLabel.Show(showEnemyLabel, true);

		if (aimingHit || blackBoard.Desires.MeleeTarget && player.CanMelee())
		{
			ShowCrosshair(E_CrosshairType.Target);
		}
		else
		{
			ShowCrosshair(E_CrosshairType.Normal);
		}
	}

	// ------
	GUIBase_Sprite PrepareSprite(GUIBase_Layout layout, string name)
	{
		GUIBase_Sprite sprite = null;
		GUIBase_Widget tmpWidget = layout.GetWidget(name);

		if (tmpWidget)
		{
			sprite = tmpWidget.GetComponent<GUIBase_Sprite>();
		}

		return sprite;
	}
}
