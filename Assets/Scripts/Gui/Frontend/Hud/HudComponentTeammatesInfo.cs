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

public class HudComponentTeammatesInfo : HudComponent
{
	// -----
	public class Label
	{
		public GUIBase_Widget Base;
		public GUIBase_Label Name;
		public Transform Transform;
		public float OrigNameScale;
		public float OrigBaseWidth;
	}

	const float HUD_INDICATOR_MAX_SQRT_DIST = 60*60;

	GUIBase_Pivot m_Pivot;
	GUIBase_Layout m_Layout;

	List<MedKit> m_RegisteredMedkits = new List<MedKit>();
	List<AmmoKit> m_RegisteredAmmokits = new List<AmmoKit>();
	List<Label> m_Labels = new List<Label>();
	List<Label> m_Medkits = new List<Label>();
	List<Label> m_Ammokits = new List<Label>();

	// -----
	protected override bool OnInit()
	{
		if (base.OnInit() == false)
			return false;

		m_Pivot = MFGuiManager.Instance.GetPivot("MainHUD");
		m_Layout = m_Pivot.GetLayout("HUD_Layout");
		m_RegisteredMedkits.Clear();
		m_RegisteredAmmokits.Clear();

		for (int i = 0; i < 5; i++)
		{
			Label l = new Label();
			l.Base = m_Layout.GetWidget("FriendName" + i);
			l.Name = GuiBaseUtils.GetChildLabel(l.Base, "Name");
			l.Transform = l.Base.transform;
			l.OrigNameScale = 0.54f; // l.Name.transform.localScale.x;
			l.OrigBaseWidth = l.Base.GetWidth();
			l.Base.Show(false, true);
			l.Base.SetModify(true);
			m_Labels.Add(l);
		}
		for (int i = 0; i < 3; i++)
		{
			Label l = new Label();
			l.Base = m_Layout.GetWidget("Medkit" + i);
			l.Name = null; //GuiBaseUtils.GetChildLabel(l.Base, "Name");
			l.Transform = l.Base.transform;
			l.OrigNameScale = 0.54f;
			l.OrigBaseWidth = l.Base.GetWidth();
			l.Base.Show(false, true);
			l.Base.SetModify(true);
			m_Medkits.Add(l);
		}
		for (int i = 0; i < 3; i++)
		{
			Label l = new Label();
			l.Base = m_Layout.GetWidget("Ammokit" + i);
			l.Name = null; //GuiBaseUtils.GetChildLabel(l.Base, "Name");
			l.Transform = l.Base.transform;
			l.OrigNameScale = 0.54f;
			l.OrigBaseWidth = l.Base.GetWidth();
			l.Base.Show(false, true);
			l.Base.SetModify(true);
			m_Ammokits.Add(l);
		}
		return true;
	}

	// -----
	protected override void OnDestroy()
	{
		m_Labels.Clear();
		m_Labels = null;
		m_Medkits.Clear();
		m_Medkits = null;
		m_Ammokits.Clear();
		m_Ammokits = null;
		m_RegisteredMedkits.Clear();
		m_RegisteredMedkits = null;
		m_RegisteredAmmokits.Clear();
		m_RegisteredAmmokits = null;

		base.OnDestroy();
	}

	// -----
	public void RegisterMedkit(MedKit medkit)
	{
		if (!medkit || !medkit.Owner)
			return;
		if (LocalPlayer == null || !medkit.Owner.IsFriend(LocalPlayer.Owner))
			return;
		m_RegisteredMedkits.Add(medkit);
	}

	// -----
	public void UnregisterMedkit(MedKit medkit)
	{
		m_RegisteredMedkits.Remove(medkit);
	}

	// -----
	public void RegisterAmmokit(AmmoKit ammokit)
	{
		if (!ammokit || !ammokit.Owner)
			return;
		if (LocalPlayer == null || !ammokit.Owner.IsFriend(LocalPlayer.Owner))
			return;
		m_RegisteredAmmokits.Add(ammokit);
	}

	// -----
	public void UnregisterAmmokit(AmmoKit ammokit)
	{
		m_RegisteredAmmokits.Remove(ammokit);
	}

	// -----
	protected override void OnShow()
	{
		base.OnShow();

/*        foreach (Label l in m_Labels)
        {
            l.Widget.Show(true, true);
            l.SetNewText("Zona 1 - 122 m");
        }*/

		UpdateLabels();
	}

	// -----
	protected override void OnLateUpdate()
	{
		base.OnLateUpdate();

		UpdateLabels();
	}

	// -----
	bool ShowHudIndicator(Camera cam, Label hudIndicator, Transform objToShow, Vector3 origPos)
	{
		return false;
		/*bool   	modified= false;
		Vector3 offset 	= new Vector3(0, 1.0f, 0);
        //Vector3 pos 		= cam.WorldToViewportPoint(objToShow.position + new Vector3(0, 1.0f, 0));
		Vector3 relativePosition = cam.transform.InverseTransformPoint(objToShow.position);
		bool behind = relativePosition.z < 0;			
    	relativePosition.z = Mathf.Max(relativePosition.z, 1.0f);
    	Vector3 pos = cam.WorldToViewportPoint(cam.transform.TransformPoint(relativePosition) + offset);
		
		if (behind && (pos.x > 0) && (pos.x < 1.0f) && (pos.y > 0) && (pos.y < 1.0f))
		{
			if (pos.x > 0.5f)
				pos.x = 1.0f;
			else
				pos.x = 0f;
			if (pos.y > 0.5f)
				pos.y = 1.0f;
			else
				pos.y = 0f;
		}
		
		//Vector3 posX = pos;
		pos.x = Mathf.Clamp( pos.x, 0.05f, 0.95f);
		pos.y = Mathf.Clamp( pos.y, 0.05f, 0.95f);
		//Debug.Log("Pos: "+posX+"   Clamp: " + pos);
		
        pos.z = origPos.z;
        pos.y = (1 - pos.y) * Screen.height;
        pos.x *= Screen.width;

		modified |= hudIndicator.Transform.position != pos;
        hudIndicator.Transform.position = pos;
		if (!hudIndicator.Base.IsVisible())
        	hudIndicator.Base.Show(true, true);
		bool 	insideCrosshairArea = Owner.Crosshair.WidgetInsideCrosshairArea( hudIndicator.Base.GetWidth(), hudIndicator.Base.GetHeight(), hudIndicator.Transform, 3f );
		float 	alpha;
		if (insideCrosshairArea)
			alpha = 0.4f;
		else
			alpha = 1.0f;
		modified |= !Mathf.Approximately(hudIndicator.Base.FadeAlpha, alpha);
		hudIndicator.Base.FadeAlpha 		= alpha;
		if (modified)
			hudIndicator.Base.SetModify(true);
		return true;
		/**/
	}

	// -----
	void UpdateLabels()
	{
		if (!LocalPlayer)
			return;
		Vector3 origPos = m_Labels[0].Transform.position;

		int index = 0;
		int medkitsShown = 0;
		int ammokitsShown = 0;

		Camera Cam;

		Cam = Camera.main ? Camera.main : (Camera.current ? Camera.current : GameCamera.Instance.MainCamera);

		PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(LocalPlayer.networkView.owner);
		E_Team playerTeam = (ppi != null) ? ppi.Team : E_Team.None;

		if (Cam)
		{
			foreach (KeyValuePair<uLink.NetworkPlayer, ComponentPlayer> pair in Player.Players)
			{
				bool modified = false;
				if (pair.Value == LocalPlayer)
					continue;
				AgentHuman a = pair.Value.Owner;
				if (a.IsAlive == false)
					continue;
				if (LocalPlayer != null && !a.IsFriend(LocalPlayer.Owner))
					continue;

				PlayerPersistantInfo ppi2 = PPIManager.Instance.GetPPI(pair.Key);
				if (ppi2 == null)
					continue;
				Vector3 pos = Cam.WorldToViewportPoint(a.Position + new Vector3(0, 2.2f, 0));
				if (pos.z < 0)
					continue;

				pos.z = origPos.z;
				pos.y = (1 - pos.y)*Screen.height;
				pos.x *= Screen.width;

				/**/
				modified |= m_Labels[index].Transform.position != pos;
				m_Labels[index].Transform.position = pos;
				bool modif = SetTextAndAdjustBackground(ppi2.NameForGui,
														m_Labels[index].Name,
														m_Labels[index].Base,
														m_Labels[index].OrigBaseWidth,
														m_Labels[index].OrigNameScale);
				/**/
				modified |= modif;
				/**/
				modified |= m_Labels[index].Base.Color != ZoneControlFlag.Colors[playerTeam];
				m_Labels[index].Base.Color = ZoneControlFlag.Colors[playerTeam];
				if (!m_Labels[index].Base.IsVisible())
					m_Labels[index].Base.Show(true, true);
				bool insideCrosshairArea = Owner.Crosshair.WidgetInsideCrosshairArea(m_Labels[index].Base.GetWidth(),
																					 m_Labels[index].Base.GetHeight(),
																					 m_Labels[index].Transform,
																					 3f);
				float alpha;
				if (insideCrosshairArea)
					alpha = 0.4f;
				else
					alpha = 1.0f;
				/**/
				modified |= !Mathf.Approximately(m_Labels[index].Base.FadeAlpha, alpha);
				m_Labels[index].Base.FadeAlpha = alpha;
				/**/
				modified |= !Mathf.Approximately(m_Labels[index].Name.Widget.FadeAlpha, alpha);
				m_Labels[index].Name.Widget.FadeAlpha = alpha;
				/**/
				modified |= !Mathf.Approximately(m_Labels[index].Name.Widget.FadeAlpha, alpha);
				if (modified)
					m_Labels[index].Base.SetModify(true);
				//else
				//	Debug.Log("Optimization!");
				index++;
				if (index >= m_Labels.Count)
					break;
			}

/*			//------------
			if (!LocalPlayer.Owner.IsFullyHealed)
			{
				// sort list
				Vector3 ownerPos = LocalPlayer.Owner.Position;
				m_RegisteredMedkits.Sort(delegate(MedKit m1, MedKit m2)
								 	 		{
												return (ownerPos - m1.transform.position).sqrMagnitude.CompareTo((ownerPos - m2.transform.position).sqrMagnitude);
											}	
										 );
				
				
				origPos = m_Medkits[0].Transform.position;
				foreach (MedKit medkit in m_RegisteredMedkits)
				{
					if ((ownerPos - medkit.transform.position).sqrMagnitude < HUD_INDICATOR_MAX_SQRT_DIST)
					{
						if (ShowHudIndicator(Cam, m_Medkits[medkitsShown], medkit.transform, origPos))
						{	
							++medkitsShown;
							if (medkitsShown >= m_Medkits.Count)
								break;
						}	
					}
				}
			}

			//------------
			if (true)
			{
				// sort list
				Vector3 ownerPos = LocalPlayer.Owner.Position;
				m_RegisteredAmmokits.Sort(delegate(AmmoKit m1, AmmoKit m2)
								 	 		{
												return (ownerPos - m1.transform.position).sqrMagnitude.CompareTo((ownerPos - m2.transform.position).sqrMagnitude);
											}	
										 );
				
				
				origPos = m_Ammokits[0].Transform.position;
				foreach (AmmoKit ammokit in m_RegisteredAmmokits)
				{
					if ((ownerPos - ammokit.transform.position).sqrMagnitude < HUD_INDICATOR_MAX_SQRT_DIST)
					{
						if (ShowHudIndicator(Cam, m_Ammokits[ammokitsShown], ammokit.transform, origPos))
						{	
							++ammokitsShown;
							if (ammokitsShown >= m_Ammokits.Count)
								break;
						}	
					}
				}
			}
/**/
		}

		// -----
		for (int i = index; i < m_Labels.Count; i++)
		{
			if (m_Labels[i].Base.IsVisible())
				m_Labels[i].Base.Show(false, true);
		}
		// -----
		for (int i = medkitsShown; i < m_Medkits.Count; i++)
		{
			if (m_Medkits[i].Base.IsVisible())
				m_Medkits[i].Base.Show(false, true);
		}
		// -----
		for (int i = ammokitsShown; i < m_Ammokits.Count; i++)
		{
			if (m_Ammokits[i].Base.IsVisible())
				m_Ammokits[i].Base.Show(false, true);
		}
	}
}
