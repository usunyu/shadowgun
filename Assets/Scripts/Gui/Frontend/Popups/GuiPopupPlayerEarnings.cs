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

public class GuiPopupPlayerEarnings : GuiPopup
{
	readonly static string CLOSE_BUTTON = "Close_Button";
	readonly static string SKIPANIM_BUTTON = "SkipAnim_Button";

	class Column
	{
		protected GUIBase_Label m_EarnedLabel;
		protected GUIBase_Label m_MissionBonusLabel;
		protected GUIBase_Label m_NewRankBonusLabel;
		protected GUIBase_Label m_FirstMatchBonusLabel;
		protected GUIBase_Label m_PremiumBonusLabel;
		protected GUIBase_Label m_TotalWithoutPremiumLabel;
		protected GUIBase_Label m_TotalWithPremium_Label;

		protected Color m_ActiveColor;
		protected Color m_InactiveColor;

		public virtual void Init(GUIBase_Widget root)
		{
			m_EarnedLabel = GuiBaseUtils.GetChild<GUIBase_Label>(root, "Earned_Label");
			m_MissionBonusLabel = GuiBaseUtils.GetChild<GUIBase_Label>(root, "MissionBonus_Label");
			m_NewRankBonusLabel = GuiBaseUtils.GetChild<GUIBase_Label>(root, "NewRankBonus_Label");
			m_FirstMatchBonusLabel = GuiBaseUtils.GetChild<GUIBase_Label>(root, "FirstMatchBonus_Label");
			m_PremiumBonusLabel = GuiBaseUtils.GetChild<GUIBase_Label>(root, "PremiumBonus_Label");
			m_TotalWithoutPremiumLabel = GuiBaseUtils.GetChild<GUIBase_Label>(root, "TotalWithoutPremium_Label");
			m_TotalWithPremium_Label = GuiBaseUtils.GetChild<GUIBase_Label>(root, "TotalWithPremium_Label");

			m_ActiveColor = GuiBaseUtils.GetChild<GUIBase_Widget>(root, "TotalWithoutPremium_Label").Color;
			m_InactiveColor = GuiBaseUtils.GetChild<GUIBase_Widget>(root, "TotalWithPremium_Label").Color;
		}

		public virtual void Update(bool hasPremium, bool newRank, bool firstRound, int total, int missionBonus, int intRankBonus)
		{
			m_TotalWithoutPremiumLabel.Widget.Color = hasPremium ? m_InactiveColor : m_ActiveColor;
			m_TotalWithPremium_Label.Widget.SetFadeAlpha(hasPremium ? 1.0f : 0.75f, true);
			//m_TotalWithPremium_Label.Widget.Color   = hasPremium ? m_ActiveColor : m_InactiveColor;
		}

		protected void ShowWidget(GUIBase_Widget widget, bool state, bool fadeOnly = false)
		{
			if (fadeOnly == true)
			{
				if (widget.Visible == false)
				{
					widget.ShowImmediate(true, state);
				}
				widget.SetFadeAlpha(state ? 1.0f : 0.25f, true);
			}
			else if (widget.Visible != state)
			{
				widget.ShowImmediate(state, true);
			}
		}
	}

	class LabelsColumn : Column
	{
		GUIBase_Widget m_ActiveRowBg;
		GUIBase_Widget m_PremiumIcon;
		GUIBase_Widget m_NonPremiumIcon;
		GUIBase_MultiSprite m_PlayerRankPic;

		public override void Init(GUIBase_Widget root)
		{
			base.Init(root);

			m_ActiveRowBg = GuiBaseUtils.GetChild<GUIBase_Widget>(root, "ActiveRowBg");
			m_PremiumIcon = GuiBaseUtils.GetChild<GUIBase_Widget>(root, "PremiumIcon");
			m_NonPremiumIcon = GuiBaseUtils.GetChild<GUIBase_Widget>(root, "NonPremiumIcon");
			m_PlayerRankPic = GuiBaseUtils.GetChild<GUIBase_MultiSprite>(m_NewRankBonusLabel.Widget, "PlayerRankPic");
		}

		public override void Update(bool hasPremium, bool newRank, bool firstRound, int total, int missionBonus, int rankBonus)
		{
			base.Update(hasPremium, newRank, firstRound, total, missionBonus, rankBonus);

			/*Vector3 pos = m_ActiveRowBg.transform.localPosition;
			pos.y = hasPremium ? m_TotalWithPremium_Label.transform.localPosition.y : m_TotalWithoutPremiumLabel.transform.localPosition.y;
			m_ActiveRowBg.transform.localPosition = pos;
			m_ActiveRowBg.SetModify();*/

			ShowWidget(m_ActiveRowBg, !hasPremium);
			ShowWidget(m_PremiumIcon, hasPremium);
			ShowWidget(m_NonPremiumIcon, !hasPremium);
			ShowWidget(m_NewRankBonusLabel.Widget, newRank, true);
			ShowWidget(m_FirstMatchBonusLabel.Widget, firstRound, true);
			ShowWidget(m_PremiumBonusLabel.Widget, hasPremium, true);
		}

		public void ShowRank(int rank)
		{
			string rankState = string.Format("Rank_{0}", Mathf.Min(rank, m_PlayerRankPic.Count - 1).ToString("D2"));
			m_PlayerRankPic.State = rank <= 0 ? GUIBase_MultiSprite.DefaultState : rankState;
		}
	}

	class ValuesColumn : Column
	{
		public float CountDownDuration;
		public AudioClip CountDownSound;

		public override void Init(GUIBase_Widget root)
		{
			base.Init(root);

			m_EarnedLabel.SetNewText("0");
			m_MissionBonusLabel.SetNewText("+0");
			m_NewRankBonusLabel.SetNewText("+0");
			m_FirstMatchBonusLabel.SetNewText("x1");
			m_PremiumBonusLabel.SetNewText("x1");
			m_TotalWithoutPremiumLabel.SetNewText("0");
			m_TotalWithPremium_Label.SetNewText("0");
		}

		public override void Update(bool hasPremium, bool newRank, bool firstRound, int total, int missionBonus, int rankBonus)
		{
			base.Update(hasPremium, newRank, firstRound, total, missionBonus, rankBonus);

			int firstRoundMult = firstRound ? 2 : 1;
			float premiumMult = hasPremium ? GameplayRewards.PremiumAccountModificator : 1;
			float allBonusMult = firstRoundMult*premiumMult;

			int totalNoBonus = Mathf.RoundToInt(total/allBonusMult);
			int totalPremium = Mathf.RoundToInt(totalNoBonus*GameplayRewards.PremiumAccountModificator);
			int mission = Mathf.RoundToInt(missionBonus/premiumMult);
			int earned = totalNoBonus - mission - rankBonus;

//			Debug.Log("hasPremium="+hasPremium+", newRank="+newRank+", firstRound="+firstRound);
//			Debug.Log("total="+total+", missionBonus="+missionBonus+", rankBonus="+rankBonus);
//			Debug.Log("earned="+earned+", mission="+mission+", rankBonus="+rankBonus+", totalNoBonus="+totalNoBonus+", totalNoPremium="+(totalNoBonus*firstRoundMult)+", totalWithPremium="+(totalNoBonus*allBonusMult));

			int rankValue = newRank ? rankBonus : 0;
			int roundValue = firstRound ? firstRoundMult : 0;
			float premiumValue = hasPremium ? premiumMult : 0;

			ShowWidget(m_MissionBonusLabel.Widget, mission > 0);
			ShowWidget(m_NewRankBonusLabel.Widget, rankValue > 0);
			ShowWidget(m_FirstMatchBonusLabel.Widget, roundValue > 0);
			ShowWidget(m_PremiumBonusLabel.Widget, premiumValue > 0);

			AnimateWidget(m_EarnedLabel, 0, earned);
			AnimateWidget(m_MissionBonusLabel, 0, mission, mission > 0 ? "+{0:0}" : "{0}");
			AnimateWidget(m_NewRankBonusLabel, 0, rankValue, rankValue > 0 ? "+{0:0}" : "{0}");
			AnimateWidget(m_FirstMatchBonusLabel, 1, roundValue, roundValue > 1 ? "x{0:0}" : "{0}");
			AnimateWidget(m_PremiumBonusLabel, 1, premiumValue, premiumValue > 1 ? "x{0:0.0}" : "{0}");
			AnimateWidget(m_TotalWithoutPremiumLabel, 0, totalNoBonus*firstRoundMult);
			AnimateWidget(m_TotalWithPremium_Label, 0, totalPremium*firstRoundMult);
		}

		void AnimateWidget(GUIBase_Label label, float source, float target, string format = null)
		{
			var animation = MFGuiManager.AnimateWidget(label, source, target);
			if (animation != null)
			{
				animation.Format = format ?? animation.Format;
				animation.Duration = CountDownDuration;
				animation.AudioClip = CountDownSound;
			}
		}
	}

	// PRIVATE MEMBERS

	[SerializeField] float m_FirstCountDelay = 0.25f;
	[SerializeField] float m_CountDownDuration = 0.5f;
	[SerializeField] AudioClip m_CountDownSoundXp;
	[SerializeField] AudioClip m_CountDownSoundMoney;
	[SerializeField] AudioClip m_CountDownSoundGold;

	LabelsColumn m_Labels = new LabelsColumn();
	ValuesColumn m_Xp = new ValuesColumn();
	ValuesColumn m_Money = new ValuesColumn();
	ValuesColumn m_Gold = new ValuesColumn();
	UserGuideAction m_UserGuideAction = new UserGuideAction_PlayerEarnings();
	bool m_IsPremiumAccountActive;

	// PUBLIC METHODS

	public void SetData(RoundFinalResult finalResult, RoundFinalResult.PlayerResult playerResult)
	{
		StartCoroutine(ShowEarnings_Coroutine(finalResult, playerResult));
	}

	// GUIPOPUP INTERFACE

	public override void SetCaption(string inCaption)
	{
	}

	public override void SetText(string inText)
	{
	}

	// GUIVIEW INTERFACE

	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_Labels.Init(Layout.GetWidget("Labels_Column"));
		m_Xp.Init(Layout.GetWidget("Xp_Column"));
		m_Money.Init(Layout.GetWidget("Money_Column"));
		m_Gold.Init(Layout.GetWidget("Gold_Column"));

		m_Xp.CountDownDuration = m_CountDownDuration;
		m_Xp.CountDownSound = m_CountDownSoundXp;

		m_Money.CountDownDuration = m_CountDownDuration;
		m_Money.CountDownSound = m_CountDownSoundMoney;

		m_Gold.CountDownDuration = m_CountDownDuration;
		m_Gold.CountDownSound = m_CountDownSoundGold;

		UserGuide.RegisterAction(m_UserGuideAction);
	}

	protected override void OnViewDestroy()
	{
		UserGuide.UnregisterAction(m_UserGuideAction);

		StopAllCoroutines();

		base.OnViewDestroy();
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		RegisterButtonDelegate(CLOSE_BUTTON, () => { ForceClose(); }, null);
		RegisterButtonDelegate(SKIPANIM_BUTTON, () => { MFGuiManager.FlushAnimations(); }, null);

		m_IsPremiumAccountActive = CloudUser.instance.isPremiumAccountActive;
	}

	protected override void OnViewHide()
	{
		StopAllCoroutines();

		RegisterButtonDelegate(CLOSE_BUTTON, null, null);
		RegisterButtonDelegate(SKIPANIM_BUTTON, null, null);

		base.OnViewHide();
	}

	// PRIVATE METHODS

	IEnumerator ShowEarnings_Coroutine(RoundFinalResult finalResult, RoundFinalResult.PlayerResult playerResult)
	{
		var ppi = PPIManager.Instance.GetLocalPPI();
		int rank = PlayerPersistantInfo.GetPlayerRankFromExperience(ppi.Experience + finalResult.Experience);
		int rankBonusMoney = finalResult.NewRank ? (int)GameplayRewards.MoneyRank : 0;

		m_Labels.Update(m_IsPremiumAccountActive, finalResult.NewRank, finalResult.FirstRound, 0, 0, rankBonusMoney);
		m_Labels.ShowRank(rank);

		yield return new WaitForSeconds(m_FirstCountDelay);

		m_Xp.Update(m_IsPremiumAccountActive, finalResult.NewRank, finalResult.FirstRound, finalResult.Experience, finalResult.MissionExp, 0);
		ppi.Experience += finalResult.Experience;

		m_Money.Update(m_IsPremiumAccountActive,
					   finalResult.NewRank,
					   finalResult.FirstRound,
					   finalResult.Money,
					   finalResult.MissioMoney,
					   rankBonusMoney);
		ppi.Money += finalResult.Money;

		m_Gold.Update(m_IsPremiumAccountActive, false, false, finalResult.Gold, 0, 0);
		ppi.Gold += finalResult.Gold;

		yield break;
	}
}
