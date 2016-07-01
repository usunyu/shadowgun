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

public class GuiScreenPlayerEarnings : GuiScreen
{
	readonly static string PRIMARYEARNINGS_LABEL = "PrimaryEarnings_Label";
	readonly static string SECONDARYEARNINGS_LABEL = "SecondaryEarnings_Label";
	readonly static string KILLS_ENUM = "Kills_Enum";
	readonly static string PRIMARYXP_ENUM = "PrimaryXP_Enum";
	readonly static string SECONDARYXP_ENUM = "SecondaryXP_Enum";
	readonly static string PRIMARYMONEY_ENUM = "PrimaryMoney_Enum";
	readonly static string SECONDARYMONEY_ENUM = "SecondaryMoney_Enum";
	readonly static string PLAYERRANKPIC = "PlayerRankPic";

	// PRIVATE MEMBERS

	[SerializeField] int m_EarnWithPremiumTextId = 502052;
	[SerializeField] int m_EarnWithoutPremiumTextId = 502051;
	[SerializeField] float m_FirstCountDelay = 0.25f;
	[SerializeField] float m_NextCountDelay = 0.0f;
	[SerializeField] float m_CountDownDuration = 0.5f;
	[SerializeField] AudioClip m_CountDownSound;
	[SerializeField] float m_HighlightDuration = 0.1f;
	[SerializeField] float m_HighlightScaleMultiplier = 1.5f;
	[SerializeField] AudioClip m_HighlightSound;

	Vector3 m_ScaleKills;
	Vector3 m_ScaleXP;
	Vector3 m_ScaleMoney;

	// GUIVIEW INTERFACE

	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_ScaleKills = Layout.GetWidget(KILLS_ENUM).transform.localScale;
		m_ScaleXP = Layout.GetWidget(PRIMARYXP_ENUM).transform.localScale;
		m_ScaleMoney = Layout.GetWidget(PRIMARYMONEY_ENUM).transform.localScale;
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		StartCoroutine(ShowEarnings_Coroutine());
	}

	protected override void OnViewHide()
	{
		StopAllCoroutines();

		base.OnViewHide();
	}

	// PRIVATE METHODS

	IEnumerator ShowEarnings_Coroutine()
	{
		PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPlayerPPI();
		bool hasPremium = CloudUser.instance.isPremiumAccountActive;
		float premiumMult = hasPremium ? (1.0f/GameplayRewards.PremiumAccountModificator) : GameplayRewards.PremiumAccountModificator;

		GuiBaseUtils.GetControl<GUIBase_Label>(Layout, PRIMARYEARNINGS_LABEL)
					.SetNewText(hasPremium ? m_EarnWithPremiumTextId : m_EarnWithoutPremiumTextId);
		GuiBaseUtils.GetControl<GUIBase_Label>(Layout, SECONDARYEARNINGS_LABEL)
					.SetNewText(hasPremium ? m_EarnWithoutPremiumTextId : m_EarnWithPremiumTextId);

		var kills = GuiBaseUtils.GetControl<GUIBase_Label>(Layout, KILLS_ENUM);
		var xp1 = GuiBaseUtils.GetControl<GUIBase_Label>(Layout, PRIMARYXP_ENUM);
		var xp2 = GuiBaseUtils.GetControl<GUIBase_Label>(Layout, SECONDARYXP_ENUM);
		var money1 = GuiBaseUtils.GetControl<GUIBase_Label>(Layout, PRIMARYMONEY_ENUM);
		var money2 = GuiBaseUtils.GetControl<GUIBase_Label>(Layout, SECONDARYMONEY_ENUM);
		var rank = GuiBaseUtils.GetControl<GUIBase_MultiSprite>(Layout, PLAYERRANKPIC);

		rank.State = string.Format("Rank_{0}", Mathf.Min(ppi.Rank, rank.Count - 1).ToString("D2"));

		kills.SetNewText("0");
		xp1.SetNewText("0");
		xp2.SetNewText("0");
		money1.SetNewText("0");
		money2.SetNewText("0");

		yield return new WaitForSeconds(m_FirstCountDelay);

		yield return StartCoroutine(CountDown_Coroutine(kills, null, 0, ppi.Score.Kills, premiumMult));
		yield return StartCoroutine(Highlight_Coroutine(kills.Widget, m_ScaleKills));

		yield return new WaitForSeconds(m_NextCountDelay);

		yield return StartCoroutine(CountDown_Coroutine(xp1, xp2, 0, ppi.Score.Experience, premiumMult));
		yield return StartCoroutine(Highlight_Coroutine(xp1.Widget, m_ScaleXP));

		yield return new WaitForSeconds(m_NextCountDelay);

		yield return StartCoroutine(CountDown_Coroutine(money1, money2, 0, ppi.Score.Money, premiumMult));
		yield return StartCoroutine(Highlight_Coroutine(money1.Widget, m_ScaleMoney));
	}

	IEnumerator CountDown_Coroutine(GUIBase_Label label1, GUIBase_Label label2, int source, int target, float premiumMult)
	{
		//Debug.Log(label1.name+", source="+source+", target="+target);

		int delta = target - source;
		float value1 = source;
		float value2 = source;
		do
		{
			float value = delta*(Time.deltaTime/m_CountDownDuration);

			if (label1 != null)
			{
				value1 = Mathf.Min(value1 + value, (float)target);
				label1.SetNewText(Mathf.RoundToInt(value1).ToString());
			}

			if (label2 != null)
			{
				value2 = Mathf.Min(value2 + value*premiumMult, source + delta*premiumMult);
				label2.SetNewText(Mathf.RoundToInt(value2).ToString());
			}

			//Debug.Log(label1.name+", value="+value+", value1="+value1+"("+Mathf.RoundToInt(value1)+"), value2="+value2+"("+Mathf.RoundToInt(value2)+")");

			if (m_CountDownSound != null)
			{
				MFGuiManager.Instance.PlayOneShot(m_CountDownSound);
			}

			yield return new WaitForEndOfFrame();
		} while (value1 < target);

		// just to be sure we have target value displayed
		if (label1 != null)
		{
			label1.SetNewText(target.ToString());
		}

		// just to be sure we have alternative target value displayed
		if (label2 != null)
		{
			label2.SetNewText(Mathf.RoundToInt(source + delta*premiumMult).ToString());
		}
	}

	IEnumerator Highlight_Coroutine(GUIBase_Widget widget, Vector3 current)
	{
		if (m_HighlightSound != null)
		{
			MFGuiManager.Instance.PlayOneShot(m_HighlightSound);
		}

		Vector3 source = current*m_HighlightScaleMultiplier;
		Vector3 target = current;
		Vector3 delta = new Vector3(source.x - target.x, source.y - target.y, 1.0f);
		Vector3 scale = source;
		do
		{
			scale.x -= Mathf.Min(delta.x*(Time.deltaTime/m_HighlightDuration), target.x);
			scale.y -= Mathf.Min(delta.y*(Time.deltaTime/m_HighlightDuration), target.y);

			widget.transform.localScale = scale;
			widget.SetModify(true);

			yield return new WaitForEndOfFrame();
		} while (scale.x > target.x || scale.y > target.y);
	}
}
