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

[AddComponentMenu("GUI/Frontend/Screens/GuiScreenLobbyRandom")]
public class GuiScreenLobbyRandom : GuiScreenLobbyBase
{
	readonly static string PLAYDM_BUTTON = "PlayDM_Button";
	readonly static string PLAYZC_BUTTON = "PlayZC_Button";
	readonly static string PLAYDM_BLURB = "PlayDM_Blurb";
	readonly static string PLAYZC_BLURB = "PlayZC_Blurb";
	readonly static string PLAYZC_LOCK = "PlayZC_Lock";

	struct LockWidget
	{
		GUIBase_Widget m_Root;
		GUIBase_Label m_Text;
		string m_Format;

		public void Init(GUIBase_Widget root)
		{
			m_Root = root;
			m_Text = GuiBaseUtils.GetChild<GUIBase_Label>(m_Root, "LockedText");
			m_Format = m_Text.GetText();
		}

		public void Show(bool state, int rank)
		{
			if (m_Root.Visible != state)
			{
				m_Root.ShowImmediate(state, true);
			}
			if (state == true)
			{
				if (rank > 0)
				{
					m_Text.SetNewText(m_Format.Replace("%d1", rank.ToString()));
				}
				else
				{
					m_Text.SetNewText(TextDatabase.instance[09900006]);
				}
			}
		}
	}

	// PRIVATE MEMBERS

	GUIBase_Button m_ButtonPlayDM;
	GUIBase_Button m_ButtonPlayZC;
	GUIBase_Widget m_PlayDMBlurb;
	GUIBase_Widget m_PlayZCBlurb;
	LockWidget m_PlayZCLock;

	// GUISCREEN INTERFACE

	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_PlayDMBlurb = Layout.GetWidget(PLAYDM_BLURB);
		m_PlayZCBlurb = Layout.GetWidget(PLAYZC_BLURB);

		m_PlayZCLock.Init(Layout.GetWidget(PLAYZC_LOCK));
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		m_ButtonPlayDM = RegisterButtonDelegate(PLAYDM_BUTTON, () => { OnPlayPressed(E_MPGameType.DeathMatch); }, null);
		m_ButtonPlayZC = RegisterButtonDelegate(PLAYZC_BUTTON, () => { OnPlayPressed(E_MPGameType.ZoneControl); }, null);

		UpdateButtonStates();
	}

	protected override void OnViewHide()
	{
		RegisterButtonDelegate(PLAYDM_BUTTON, null, null);
		RegisterButtonDelegate(PLAYZC_BUTTON, null, null);

		base.OnViewHide();
	}

	protected override void OnViewUpdate()
	{
		base.OnViewUpdate();

		UpdateButtonStates();
	}

	// HANDLERS

	void OnPlayPressed(E_MPGameType gameType)
	{
		Play(gameType);
	}

	// PRIVATE METHODS

	void UpdateButtonStates()
	{
		FtueAction.Base action = Ftue.ActiveAction ?? Ftue.PendingAction;
		PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();
		ZoneControlInfo zcInfo = GameInfoSettings.GetGameInfo<ZoneControlInfo>();
		int maxRank = PlayerPersistantInfo.MAX_RANK;
		int rank = ppi != null ? ppi.Rank : 1;
		int minRankZC = zcInfo != null ? Mathf.Clamp(zcInfo.MinimalDesiredRankToPlay, 1, maxRank) : 1;
		bool disabled = !LobbyClient.IsConnected;
		bool highlight = action != null && action.ShouldBeIngame || action is FtueAction.RankUp ? true : false;
		bool disableDM = disabled;
		bool tutorialZC = Ftue.IsActionFinished<FtueAction.ZoneControl>() ? false : true;
		bool unlockedZC = rank >= minRankZC ? true : false;
		if (tutorialZC == true && Ftue.IsActionActive<FtueAction.ZoneControl>() == false)
		{
			unlockedZC = false;
		}

		bool highlightDM = unlockedZC == false ? highlight : false;
		bool disableZC = unlockedZC == true ? disabled : true;
		bool highlightZC = unlockedZC == true ? highlight : false;
		bool showBlurbDM = ppi != null && ppi.IsFirstGameToday(E_MPGameType.DeathMatch) ? true : false;
		bool showBlurbZC = ppi != null && ppi.IsFirstGameToday(E_MPGameType.ZoneControl) ? unlockedZC : false;
		bool showLockZC = zcInfo != null && unlockedZC == false ? true : false;

		m_ButtonPlayDM.IsDisabled = disableDM;
		m_ButtonPlayDM.animate = true;
		m_ButtonPlayDM.isHighlighted = action is FtueAction.DeathMatch || highlightDM ? !disableDM : false;
		ShowWidget(m_PlayDMBlurb, showBlurbDM);

		m_ButtonPlayZC.IsDisabled = disableZC;
		m_ButtonPlayZC.animate = true;
		m_ButtonPlayZC.isHighlighted = action is FtueAction.ZoneControl || highlightZC ? !disableZC : false;
		m_PlayZCLock.Show(showLockZC, tutorialZC && rank >= minRankZC ? -1 : minRankZC);
		ShowWidget(m_PlayZCBlurb, showBlurbZC);
	}

	void ShowWidget(GUIBase_Widget widget, bool state)
	{
		if (widget.Visible != state)
		{
			widget.ShowImmediate(state, true);
		}
	}
}
