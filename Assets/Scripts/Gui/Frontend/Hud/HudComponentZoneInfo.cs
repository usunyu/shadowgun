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

public class HudComponentZoneInfo : HudComponent
{
	// ---
	public class Label
	{
		public GUIBase_Widget Base;
		public GUIBase_Number Distance;
		public Transform Transform;
		public float alpha;
	}

	// ---
	public class FlagInfo
	{
		// ---
		public FlagInfo(ZoneControlFlag flag)
		{
			Flag = flag;
			UpdateDistance();
		}

		// ---
		public void UpdateDistance()
		{
			Render = false;
			Camera cam = Camera.main;
			if (!cam)
				cam = GameCamera.Instance.MainCamera;
			Vector3 origPos = Pos;

			if (cam)
			{
				Pos = cam.WorldToViewportPoint(Flag.HudIconPosition);
				if (Pos.z >= 0)
					Render = true;
			}
			else
				Pos = Vector3.zero;

			if (Render)
			{
				Pos = new Vector3(Pos.x*Screen.width, (1 - Pos.y)*Screen.height - (float)Screen.height/18f, origPos.z);
				Distance = Mathf.FloorToInt(Flag.GetDistanceToLocalPlayer());
				if ((Distance < 30f)) // && Flag.FlagIcon && Flag.FlagIcon.isVisible)
					Render = false;
			}
		}

		public int Distance { get; private set; }
		public bool Render { get; private set; }
		public Vector3 Pos { get; private set; }
		public ZoneControlFlag Flag { get; private set; }
	}

	GUIBase_Pivot m_Pivot;
	List<Label> m_Labels = new List<Label>();
	List<FlagInfo> m_FlagData = new List<FlagInfo>();

	// ---------------------
	// Use this for initialization
	protected override bool OnInit()
	{
		if (base.OnInit() == false)
			return false;

		GUIBase_Layout layout1;
		GUIBase_Layout layout2;

		m_Labels.Clear();
		m_Pivot = MFGuiManager.Instance.GetPivot("MainHUD_ZoneControlLabels");
		layout1 = m_Pivot.GetLayout("Labels1");
		for (int i = 0; i < 4; i++)
		{
			Label l = new Label();
			l.Base = layout1.GetWidget("ZoneControlFlag" + i);
			l.Distance = GuiBaseUtils.GetChildNumber(l.Base, "Number");
			l.Transform = l.Base.transform;
			l.alpha = 0;
			m_Labels.Add(l);
		}
		layout2 = m_Pivot.GetLayout("Labels2");
		for (int i = 4; i < 7; i++)
		{
			Label l = new Label();
			l.Base = layout2.GetWidget("ZoneControlFlag" + i);
			l.Distance = GuiBaseUtils.GetChildNumber(l.Base, "Number");
			l.Transform = l.Base.transform;
			l.alpha = 0;
			m_Labels.Add(l);
		}

		m_FlagData.Clear();
		GameZoneZoneControl zone = Mission.Instance.GameZone as GameZoneZoneControl;
		for (int i = 0; i < 6; i++)
		{
			if (i < zone.Zones.Count)
				m_FlagData.Add(new FlagInfo(zone.Zones[i]));
		}

		return true;
	}

	// -------
	protected override void OnDestroy()
	{
		m_Labels.Clear();
		m_Labels = null;

		base.OnDestroy();
	}

	// -------
	protected override void OnHide()
	{
		MFGuiManager.Instance.ShowPivot(m_Pivot, false);

		base.OnHide();
	}

	// -------
	protected override void OnShow()
	{
		base.OnShow();

/*        foreach (Label l in m_Labels)
        {
            l.Widget.Show(true, true);
            l.SetNewText("Zona 1 - 122 m");
        }*/
		MFGuiManager.Instance.ShowPivot(m_Pivot, true);

		UpdateLabels();
	}

	// -------
	protected override void OnLateUpdate()
	{
		base.OnLateUpdate();

		UpdateLabels();
	}

	// -----
	void UpdateLabels()
	{
		const float MAX_ALPHA_CHANGE_PER_SEC = 1.0f;

		foreach (FlagInfo info in m_FlagData)
			info.UpdateDistance();

		// sort list
		m_FlagData.Sort(delegate(FlagInfo f1, FlagInfo f2) { return f1.Distance.CompareTo(f2.Distance); }
						);

		for (int i = 0; i < 6; i++)
		{
			if ((m_FlagData.Count <= i) || !m_FlagData[i].Render)
				m_Labels[i].Base.Show(false, true);
			else
			{
				m_Labels[i].Distance.SetNumber(m_FlagData[i].Distance, 999);
				if (m_FlagData[i].Flag.FlagOwner == E_Team.None)
					m_Labels[i].Base.Color = ZoneControlFlag.Colors[E_Team.None]*0.5f;
				else
					m_Labels[i].Base.Color = ZoneControlFlag.Colors[m_FlagData[i].Flag.FlagOwner]*0.8f;
				//m_Labels[i].Distance.Widget.Color 	= ZoneControlFlag.Colors[m_FlagData[i].Flag.FlagOwner] * 2;

				float distanceFactor = (m_FlagData[i].Distance < 40.0f) ? 0 : Mathf.Clamp((m_FlagData[i].Distance - 40.0f)/200f, 0, 1);
				m_Labels[i].Transform.position = m_FlagData[i].Pos;
				m_Labels[i].Transform.localScale = Vector3.one*(1.166f - distanceFactor/3);

				bool insideCrosshairArea = Owner.Crosshair.WidgetInsideCrosshairArea(m_Labels[i].Base.GetWidth(),
																					 m_Labels[i].Base.GetHeight(),
																					 m_Labels[i].Transform,
																					 3f);
				float alpha = 1.0f - distanceFactor;
				alpha -= insideCrosshairArea ? 0.5f : 0f;
				bool visible = alpha > 0.1f;

				if ((m_Labels[i].alpha > alpha) && ((m_Labels[i].alpha - alpha) > Time.deltaTime*MAX_ALPHA_CHANGE_PER_SEC))
					alpha = m_Labels[i].alpha - Time.deltaTime*MAX_ALPHA_CHANGE_PER_SEC;
				else if ((m_Labels[i].alpha < alpha) && ((alpha - m_Labels[i].alpha) > Time.deltaTime*MAX_ALPHA_CHANGE_PER_SEC))
					alpha = m_Labels[i].alpha + Time.deltaTime*MAX_ALPHA_CHANGE_PER_SEC;
				alpha = Mathf.Clamp(alpha, (visible) ? 0.1f : 0f, 1f);

				m_Labels[i].alpha = alpha;
				m_Labels[i].Base.FadeAlpha = alpha;
				m_Labels[i].Distance.Widget.FadeAlpha = alpha;
				m_Labels[i].Base.Show(true, true);
				m_Labels[i].Base.SetModify(true);
			}
		}
	}
}
