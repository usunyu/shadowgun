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
using System.Collections.Generic;

[AddComponentMenu("GUI/Frontend/Overlays/GuiOverlayStatusBar")]
public class GuiOverlayStatusBar : GuiOverlaySideBar
{
	// CONFIG

	[SerializeField] AudioClip m_CountDownSound;

	// PRIVATE MEMBERS

	GUIBase_Button m_GoldButton;
	GUIBase_Button m_MoneyButton;
	GUIBase_Label m_LevelLabel;
	GUIBase_MultiSprite m_LevelPic;
	GUIBase_Label m_XPLabel;
	GUIBase_Button m_PlayerButton;
	GUIBase_Button m_PremiumButton;
	GUIBase_Button m_PremiumActiveButton;
	GUIBase_Widget m_PremiumBackground;
	GUIBase_Widget m_FreeBackground;
	GUIBase_Widget m_EmptyBar;
	GUIBase_Widget m_FullBar;
	GUIBase_Label m_NextSpawn_Label;
	bool m_Animate = true;
	bool m_AnimateRank = true;
	bool m_AnimateMoney = true;
	bool m_AnimateGold = true;
	static bool m_ForceUpdateControls = true;
	static int m_Rank = -1;
	static int m_Money = -1;
	static int m_Gold = -1;

	// PUBLIC MEMBERS

	public bool Animate
	{
		get { return m_Animate; }
		set
		{
			m_Animate = value;
			if (m_Animate == false)
				FlushAnimations();
		}
	}

	public bool AnimateRank
	{
		get { return Animate && m_Rank >= 0 ? m_AnimateRank : false; }
		set { m_AnimateRank = value; }
	}

	public bool AnimateMoney
	{
		get { return Animate && m_Money >= 0 ? m_AnimateMoney : false; }
		set { m_AnimateMoney = value; }
	}

	public bool AnimateGold
	{
		get { return Animate && m_Gold >= 0 ? m_AnimateGold : false; }
		set { m_AnimateGold = value; }
	}

	// GUIOVERLAYSIDEBAR INTERFACE

	protected override bool ShouldDisplayButton(GUIBase_Button button)
	{
		bool hasPremium = CloudUser.instance.isPremiumAccountActive;
		if (button == m_PremiumButton)
		{
			return m_PremiumActiveButton != null ? !hasPremium : true;
		}
		else if (button == m_PremiumActiveButton)
		{
			return hasPremium;
		}
		else if (button.name == "Rewards_Button")
		{
			if (Ftue.IsActive == true)
				return false;

			var ppi = PPIManager.Instance.GetLocalPPI();
			if (ppi == null)
				return false;

			var rewards = ppi.DailyRewards;
			if (rewards == null)
				return false;
			if (rewards.Instant == null)
				return false;
			if (rewards.Instant.Length != PPIDailyRewards.DAYS_PER_CYCLE)
				return false;
			if (rewards.Instant[0] == null)
				return false;
		}
		return base.ShouldDisplayButton(button);
	}

	// GUIOVERLAY INTERFACE

	protected override void OnViewInit()
	{
		// anchor status bar to the top of the screen
		Transform trans = Layout.transform;
		Vector3 position = trans.position;
		position.y = 0;
		trans.position = position;

		//
		base.OnViewInit();

		// cache controls
		m_GoldButton = GuiBaseUtils.GetControl<GUIBase_Button>(Layout, "Gold", false);
		m_MoneyButton = GuiBaseUtils.GetControl<GUIBase_Button>(Layout, "Money", false);
		m_LevelLabel = GuiBaseUtils.GetControl<GUIBase_Label>(Layout, "TextRank", false);
		m_LevelPic = GuiBaseUtils.GetControl<GUIBase_MultiSprite>(Layout, "PlayerRankPic", false);
		m_XPLabel = GuiBaseUtils.GetControl<GUIBase_Label>(Layout, "TextXP", false);
		m_PlayerButton = GuiBaseUtils.GetControl<GUIBase_Button>(Layout, "Player", false);
		m_EmptyBar = Layout.GetWidget("EmptyBar", false);
		m_FullBar = Layout.GetWidget("FullBar", false);
		m_PremiumButton = GuiBaseUtils.GetControl<GUIBase_Button>(Layout, "Premium", false);
		m_PremiumActiveButton = GuiBaseUtils.GetControl<GUIBase_Button>(Layout, "PremiumActive", false);
		m_PremiumBackground = Layout.GetWidget("Background_Premium");
		m_FreeBackground = Layout.GetWidget("Background_Free");
		m_NextSpawn_Label = GuiBaseUtils.GetControl<GUIBase_Label>(Layout, "NextSpawn", false);

		// set initial visibility depend on build stage
		bool isBeta = BuildInfo.Version.Stage == BuildInfo.Stage.Beta;
		SetWidgetInitialVisibility(Layout.GetWidget("Feedback_Button", false), true, isBeta);
		SetWidgetInitialVisibility(Layout.GetWidget("ShadowGunLogo"), true, isBeta || GuiFrontendIngame.IsVisible);
		if (m_GoldButton != null)
		{
			SetWidgetInitialVisibility(m_GoldButton.Widget, true, !isBeta && GuiFrontendMain.IsVisible);
		}
		if (m_MoneyButton != null)
		{
			SetWidgetInitialVisibility(m_MoneyButton.Widget, true, !isBeta && GuiFrontendMain.IsVisible);
		}
		SetWidgetInitialVisibility(Layout.GetWidget("XP", false), true, !isBeta && GuiFrontendMain.IsVisible);

		CloudUser.authenticationChanged += OnUserAuthenticationChanged;
		PPIManager.localPlayerInfoChanged += OnLocalPlayerInfoChanged;
	}

	protected override void OnViewDestroy()
	{
		CloudUser.authenticationChanged -= OnUserAuthenticationChanged;
		PPIManager.localPlayerInfoChanged -= OnLocalPlayerInfoChanged;

		base.OnViewDestroy();
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		if (m_ForceUpdateControls == true)
		{
			UpdateControls(PPIManager.Instance.GetLocalPPI(), true);
			m_ForceUpdateControls = false;
		}
	}

	protected override void OnViewUpdate()
	{
		base.OnViewUpdate();

		UpdatePremiumBackground();
		UpdatePremiumButton();
		UpdateTimer();
	}

	// HANDLERS

	void OnUserAuthenticationChanged(bool state)
	{
		if (state == true)
		{
			m_ForceUpdateControls = true;
		}
		else
		{
			FlushAnimations();

			m_Rank = -1;
			m_Money = -1;
			m_Gold = -1;
		}
	}

	void OnLocalPlayerInfoChanged(PlayerPersistantInfo ppi)
	{
		UpdateControls(ppi, false);
	}

	// PRIVATE METHODS

	void UpdateControls(PlayerPersistantInfo ppi, bool reset)
	{
		UpdatePlayerName(ppi, reset);
		UpdateXp(ppi, reset);
		UpdateGold(ppi, reset);
		UpdateMoney(ppi, reset);

		UpdatePremiumBackground();
		UpdatePremiumButton();
	}

	void UpdateGold(PlayerPersistantInfo ppi, bool reset)
	{
		if (GuiFrontendMain.IsVisible == false)
			return;
		if (m_GoldButton == null)
			return;

		int gold = ppi != null ? ppi.Gold : 0;
		if (reset == false)
		{
			SetButtonValue(m_GoldButton, m_Gold, gold, AnimateGold);
			m_Gold = gold;
		}
		else
		{
			m_GoldButton.SetNewText(gold.ToString());
		}
	}

	void UpdateMoney(PlayerPersistantInfo ppi, bool reset)
	{
		if (GuiFrontendMain.IsVisible == false)
			return;
		if (m_MoneyButton == null)
			return;

		int money = ppi != null ? ppi.Money : 0;
		if (reset == false)
		{
			SetButtonValue(m_MoneyButton, m_Money, money, AnimateMoney);
			m_Money = money;
		}
		else
		{
			m_MoneyButton.SetNewText(money.ToString());
		}
	}

	void UpdateXp(PlayerPersistantInfo ppi, bool reset)
	{
		if (GuiFrontendMain.IsVisible == false)
			return;

		int xp = ppi != null ? ppi.Experience : 0;
		int level = PlayerPersistantInfo.GetPlayerRankFromExperience(xp);
		if (reset == false)
		{
			SetLabelValue(m_LevelLabel, m_Rank, level, AnimateRank);
			m_Rank = level;
		}
		else
		{
			m_LevelLabel.SetNewText(level.ToString());
		}

		int minXp = PlayerPersistantInfo.GetPlayerMinExperienceForRank(Mathf.Clamp(level, 1, PlayerPersistantInfo.MAX_RANK));
		int maxXp = PlayerPersistantInfo.GetPlayerMinExperienceForRank(Mathf.Clamp(level + 1, 1, PlayerPersistantInfo.MAX_RANK));

		m_LevelPic.State = string.Format("Rank_{0}", Mathf.Min(level, m_LevelPic.Count - 1).ToString("D2"));
		m_XPLabel.SetNewText(xp.ToString() + "/" + maxXp.ToString());

		Transform emptyTrans = m_EmptyBar.transform;
		Vector3 pos = emptyTrans.localPosition;
		Vector3 scale = emptyTrans.localScale;
		float width = m_EmptyBar.GetWidth();
		float ratio = minXp != maxXp ? Mathf.Clamp((xp - minXp)/(float)(maxXp - minXp), 0.01f, 1.0f) : 0.0f;

		pos.x -= (width - width*ratio)*scale.x*0.5f;
		scale.x *= ratio;

		Transform fullTrans = m_FullBar.transform;
		fullTrans.localScale = scale;
		fullTrans.localPosition = pos;
		m_FullBar.SetModify();
	}

	void UpdatePlayerName(PlayerPersistantInfo ppi, bool reset)
	{
		string playerName = ppi != null ? ppi.Name : "";

		m_PlayerButton.SetNewText(GuiBaseUtils.FixNameForGui(playerName));
	}

	void UpdateTimer()
	{
		if (m_NextSpawn_Label == null)
			return;
		if (m_NextSpawn_Label.Widget.Visible == false)
			return;
		if (Client.Instance == null)
			return;

		if (Client.Instance.GameState.GameType == E_MPGameType.DeathMatch &&
			Client.TimeToRespawn > Client.Instance.GameState.DMInfo.RestTimeSeconds)
		{
			if (m_NextSpawn_Label.Widget.Visible == true)
			{
				m_NextSpawn_Label.Widget.Show(false, true);
			}
		}
		else
		{
			int minutes = Client.TimeToRespawn/60;
			int seconds = Client.TimeToRespawn%60;
			string time = string.Format("{0:00}:{1:00}", minutes, seconds);

			m_NextSpawn_Label.SetNewText(time);
		}
	}

	void UpdatePremiumButton()
	{
		bool hasPremium = CloudUser.instance.isPremiumAccountActive;

		string text = "";
		if (hasPremium == true)
		{
			System.TimeSpan time = CloudUser.instance.GetPremiumAccountEndDateTime() - CloudDateTime.UtcNow;
			//System.TimeSpan time = (CloudDateTime.UtcNow + new System.TimeSpan(0, 0, 119, 0, 0)) - CloudDateTime.UtcNow;

			int seconds = Mathf.CeilToInt((float)time.TotalSeconds);
			int minutes = Mathf.CeilToInt((float)time.TotalMinutes);
			int hours = Mathf.RoundToInt((float)time.TotalHours);
			int days = Mathf.RoundToInt((float)time.TotalDays);
			//int weeks   = Mathf.RoundToInt(days / 7.0f);
			//int months  = Mathf.RoundToInt(weeks / 4.0f);

			text = TextDatabase.instance[0105008];

			int value, unit;
			if (seconds <= 60)
			{
				value = seconds;
				unit = 0105019;
			}
			else if (minutes < 60)
			{
				value = minutes;
				unit = 0105009;
			}
			else if (hours < 48)
			{
				value = hours;
				unit = value > 1 ? 0105012 : 0105011;
			}
			else
			/*if (days < 14)*/
			{
				value = days;
				unit = value > 1 ? 0105014 : 0105013;
			}
			/*else
			if (weeks < 4)     { value = weeks;   unit = value > 1 ? 0105016 : 0105015; }
			else               { value = months;  unit = value > 1 ? 0105018 : 0105017; }*/

			text = string.Format(text, value, TextDatabase.instance[unit]);

			/*if (minutes < 30)
			{
				if (m_CurrentHint != E_Hint.PremiumRenew && m_PreviousHint != E_Hint.PremiumRenew)
				{
					ForceShowHint(E_Hint.PremiumRenew);
				}
			}*/

			if (minutes < 5)
			{
				GUIBase_Button premium = GuiBaseUtils.GetButton(Layout, "Premium");
				premium.ForceHighlight(time.TotalSeconds > 5 ? !premium.isHighlighted : false);
			}
		}
		else if (GuiFrontendMain.IsVisible == true)
		{
			text = TextDatabase.instance[0105007];
		}
		else
		{
			text = TextDatabase.instance[0105020];
		}

		GUIBase_Button button = hasPremium == true && m_PremiumActiveButton != null ? m_PremiumActiveButton : m_PremiumButton;
		button.SetNewText(text);
	}

	void UpdatePremiumBackground()
	{
		bool hasPremium = CloudUser.instance.isPremiumAccountActive;

		SetWidgetVisibility(m_PremiumBackground, hasPremium);
		SetWidgetVisibility(m_FreeBackground, !hasPremium);
	}

	void SetWidgetVisibility(GUIBase_Button button, bool state)
	{
		if (button == null)
			return;

		SetWidgetVisibility(button.Widget, state);
	}

	void SetWidgetVisibility(GUIBase_Widget widget, bool state)
	{
		if (widget == null)
			return;
		if (widget.Visible == state)
			return;

		widget.ShowImmediate(state, true);
	}

	void SetWidgetInitialVisibility(GUIBase_Widget widget, bool recursive, bool state)
	{
		if (widget == null)
			return;

		widget.m_VisibleOnLayoutShow = state;

		if (recursive == false)
			return;

		GUIBase_Widget[] children = widget.GetComponentsInChildren<GUIBase_Widget>();
		foreach (var child in children)
		{
			child.m_VisibleOnLayoutShow = state;
		}
	}

	void SetLabelValue(GUIBase_Label label, int oldValue, int newValue, bool animate)
	{
		if (oldValue == newValue)
			return;

		if (animate == false)
		{
			label.SetNewText(newValue.ToString());
		}
		else
		{
			var animation = MFGuiManager.AnimateWidget(label, oldValue, newValue);
			if (animation != null)
			{
				animation.AudioClip = m_CountDownSound;
			}
		}
	}

	void SetButtonValue(GUIBase_Button button, int oldValue, int newValue, bool animate)
	{
		if (oldValue == newValue)
			return;

		if (animate == false)
		{
			button.SetNewText(newValue.ToString());
		}
		else
		{
			var animation = MFGuiManager.AnimateWidget(button, oldValue, newValue);
			if (animation != null)
			{
				animation.AudioClip = m_CountDownSound;
			}
		}
	}

	void FlushAnimations()
	{
		MFGuiManager.FlushAnimations();
	}
}
