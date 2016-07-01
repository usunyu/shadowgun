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

public class HudComponentCombatInfo : HudComponent
{
	// -----
	/*class AchievementNotify
	{
		GUIBase_Widget			m_Parent;
		GUIBase_Label			m_Text;		
		
		float m_CurrentAlpha;
		float m_Speed = 1;	
		float m_Progress;
		float TimeToHide;
		
		public bool IsVisible {get; private set;}
		public float Progress {get {return m_Progress;}}
		
		public AchievementNotify(GUIBase_Layout layout)
		{
			m_Parent = layout.GetWidget("Achievements");
			
			m_Text = layout.GetWidget("AchievementsText").GetComponent<GUIBase_Label>();
			
			Hide();
				
		}
		
		// -----
		public void Enable(bool enable)
		{
			if(enable && IsVisible)
				m_Parent.Show(true, true);
			else if(enable == false && IsVisible)
				m_Parent.Show(false, true);
				
		}
	
		// -----
		public void Show(string text)
		{
			m_Parent.Show(true, true);	
			m_Text.SetNewText(text);
			m_Progress  = 0;
			IsVisible = true;
			
			m_Parent.audio.Play();
			
		}
		
		// -----
		public void Update()
		{
			if(IsVisible == false)
				return;
			
			m_Progress = Mathf.Min(m_Progress + TimeManager.Instance.GetRealDeltaTime() * m_Speed, 2);
			
			float alpha = Mathfx.Sinerp(0, 3.5f, m_Progress);
			m_Parent.FadeAlpha = alpha;	
			
			if(alpha <= 0)
				Hide();
		}
		
		// -----
		private void Hide()
		{
			IsVisible = false;
			m_Parent.Show(false, true);
		}
	}
	/**/

	// ------
	class Indicator
	{
		GUIBase_Widget m_Indicator;
		float m_Timer;
		float m_TimeOut;
		bool m_Active;
		bool m_Enabled;

		// ------
		public Indicator(GUIBase_Widget widget, float timeOut)
		{
			m_Indicator = widget;
			m_TimeOut = timeOut;
			m_Active = false;
			m_Enabled = false;
		}

		// ------
		public void Activate()
		{
			if (!m_Active)
			{
				m_Active = true;
				m_Timer = 0;
				m_Indicator.FadeAlpha = 1.0f;
				m_Indicator.StartCoroutine(HighlightObject(m_Indicator));
			}
			UpdateVisibility();
		}

		// -----
		public void Update(float deltaTime)
		{
			if (m_Active)
			{
				m_Timer += deltaTime;

				if (m_Timer > m_TimeOut)
				{
					m_Active = false;
					UpdateVisibility();
					m_Indicator.StopAllCoroutines();
				}
			}
		}

		// -----
		public void Enable(bool enable)
		{
			m_Enabled = enable;
			UpdateVisibility();
		}

		// -----
		void UpdateVisibility()
		{
			if (m_Enabled && m_Active)
			{
				m_Indicator.Show(true, true);
			}
			else
				m_Indicator.Show(false, true);
		}

		// ---------
		IEnumerator HighlightObject(GUIBase_Widget sprite)
		{
			while (true)
			{
				sprite.FadeAlpha = 0.3f;
				yield return new WaitForSeconds(0.1f);
				sprite.FadeAlpha = 1.0f;
				yield return new WaitForSeconds(0.3f);
			}
		}
	}

	Indicator HealIndicator;
	Indicator RechargeAmmoIndicator;
	//AchievementNotify	AchievementInfo;

	string s_PivotMainName = "MainHUD";
	string s_LayoutMainName = "HUD_Layout";
	string s_Parent = "CombatInfo";

	// ---------------------------------------------------------------------------------------------------------------------------------
	// 						P U B L I C      P A R T
	// ---------------------------------------------------------------------------------------------------------------------------------
	// ---------
	protected override bool OnInit()
	{
		if (base.OnInit() == false)
			return false;

		GUIBase_Pivot pivot = MFGuiManager.Instance.GetPivot(s_PivotMainName);
		GUIBase_Layout layout = pivot.GetLayout(s_LayoutMainName);

		layout.GetWidget(s_Parent).GetComponent<GUIBase_Widget>();

		HealIndicator = new Indicator(layout.GetWidget("Heal").GetComponent<GUIBase_Widget>(), 1.0f);
		RechargeAmmoIndicator = new Indicator(layout.GetWidget("RechargeAmmo").GetComponent<GUIBase_Widget>(), 2.0f);
		//AchievementInfo = new AchievementNotify(layout);

		//Game.Instance.PlayerPersistentInfo.OnRankChanged += ShowNewRank;

		return true;
	}

	protected override void OnDestroy()
	{
		HealIndicator = null;
		RechargeAmmoIndicator = null;

		base.OnDestroy();
	}

	// ---------
	protected override void OnLateUpdate()
	{
		base.OnLateUpdate();

		HealIndicator.Update(Time.deltaTime);
		RechargeAmmoIndicator.Update(Time.deltaTime);
		//AchievementInfo.Update();
	}

	// ---------
	protected override void OnShow()
	{
		base.OnShow();

		UpdateVisibility();
	}

	// ---------
	protected override void OnHide()
	{
		UpdateVisibility();

		base.OnHide();
	}

	// ---------
	void UpdateVisibility()
	{
		HealIndicator.Enable(IsVisible);
		RechargeAmmoIndicator.Enable(IsVisible);
	}

	/*public void ShowInfo(E_MessageType message, float speed)
	{
		Messages[message].Show(speed);
		
		List<Message> sorted = new List<Message>();
		
		foreach(KeyValuePair<E_MessageType, Message> m in Messages)
		{
			if(m.Value.IsVisible)
				sorted.Add(m.Value);
		}
		
		sorted.Sort((p1, p2) => p1.Progress.CompareTo(p2.Progress));
		
		int layer = 1;
		foreach(KeyValuePair<E_MessageType, Message> m in Messages)
		{
			m.Value.SetLayer(layer++);
		}
	}
	
	public void ShowAchievement(string text)
	{
		 AchievementInfo.Show(text);
	}

	public void ShowHit()
	{
		HitInfo.AddHit();
	}
	/**/

	// -----
	public void Heal()
	{
		HealIndicator.Activate();
	}

	// -----
	public void RechargeAmmo()
	{
		RechargeAmmoIndicator.Activate();
	}
}
