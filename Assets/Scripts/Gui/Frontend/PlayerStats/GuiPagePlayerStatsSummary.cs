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

public class GuiPagePlayerStatsSummary : GuiScreen, IGuiPagePlayerStats
{
	// IGUIPAGEPLAYERSTATS INTERFACE

	void IGuiPagePlayerStats.Refresh(PlayerPersistantInfo ppi)
	{
		// collected values

		int thisRankExperience = PlayerPersistantInfo.GetPlayerMinExperienceForRank(Mathf.Clamp(ppi.Rank, 1, PlayerPersistantInfo.MAX_RANK));
		int nextRankExperience = PlayerPersistantInfo.GetPlayerMinExperienceForRank(Mathf.Clamp(ppi.Rank + 1, 1, PlayerPersistantInfo.MAX_RANK));
		PPIPlayerStats stats = ppi.Statistics;
		float totalTimeSpent = stats.GetTimeSpent();
		float totalShots = stats.GetShots();
		float totalKills = stats.GetKills();
		PPIPlayerStats.GameData dm = stats.GetGameData(E_MPGameType.DeathMatch);
		PPIPlayerStats.GameData zc = stats.GetGameData(E_MPGameType.ZoneControl);

		// computed values

		float computedKillRatio = totalTimeSpent > 0 ? totalKills/totalTimeSpent : 0.0f;
		int computedAccurancy = totalShots > 0 ? Mathf.RoundToInt((stats.GetHits()/(float)totalShots)*100) : 0;
		int dmSpentMinutes = Mathf.RoundToInt((float)System.TimeSpan.FromMinutes(dm.TimeSpent).TotalMinutes);
		int dmSpentHours = Mathf.RoundToInt((float)System.TimeSpan.FromMinutes(dm.TimeSpent).TotalHours);
		int zcSpentMinutes = Mathf.RoundToInt((float)System.TimeSpan.FromMinutes(zc.TimeSpent).TotalMinutes);
		int zcSpentHours = Mathf.RoundToInt((float)System.TimeSpan.FromMinutes(zc.TimeSpent).TotalHours);
		int totalSpentMinutes = dmSpentMinutes + zcSpentMinutes;
		int totalSpentHours = dmSpentHours + zcSpentHours;

		int dmTimeSpentUnitTextId;
		if (dmSpentHours == 1)
			dmTimeSpentUnitTextId = 01160038;
		else if (dmSpentHours > 0)
			dmTimeSpentUnitTextId = 01160019;
		else if (dmSpentMinutes == 1)
			dmTimeSpentUnitTextId = 01160039;
		else
			dmTimeSpentUnitTextId = 01160033;

		int zcTimeSpentUnitTextId;
		if (zcSpentHours == 1)
			zcTimeSpentUnitTextId = 01160038;
		else if (zcSpentHours > 0)
			zcTimeSpentUnitTextId = 01160019;
		else if (zcSpentMinutes == 1)
			zcTimeSpentUnitTextId = 01160039;
		else
			zcTimeSpentUnitTextId = 01160033;

		int timeSpentUnitTextId;
		if (totalSpentHours == 1)
			timeSpentUnitTextId = 01160035;
		else if (totalSpentHours > 0)
			timeSpentUnitTextId = 01160014;
		else if (totalSpentMinutes == 1)
			timeSpentUnitTextId = 01160037;
		else
			timeSpentUnitTextId = 01160036;

		// rank

		GUIBase_MultiSprite rank = GuiBaseUtils.GetControl<GUIBase_MultiSprite>(Layout, "PlayerRankPic");
		rank.State = string.Format("Rank_{0}", Mathf.Min(ppi.Rank, rank.Count - 1).ToString("D2"));

		SetText("Rank_Enum", ppi.Rank.ToString("D2"));

		// experience

		SetText("XPEarned_Enum", ppi.Experience.ToString());
		SetText("XPLeft_Label", string.Format(TextDatabase.instance[1160015], Mathf.Max(0, nextRankExperience - ppi.Experience)));
		SetProgress("XPBarFg", "XPBarBg", ppi.Experience - thisRankExperience, nextRankExperience - thisRankExperience);

		// kills

		SetText("Kills_Enum", stats.GetKills().ToString());
		SetText("KillRatio_Enum", computedKillRatio.ToString("0.0"));

		// deaths

		SetText("Deaths_Enum", stats.GetDeaths().ToString());

		// score

		SetText("TotalScore_Enum", stats.GetScore().ToString());
		SetText("GoldEarned_Enum", stats.GetGolds().ToString());
		SetText("MoneyEarned_Enum", stats.GetMoney().ToString());
		SetText("Accuracy_Enum", computedAccurancy.ToString());
		SetText("Shotsfired_Enum", totalShots.ToString());
		SetText("Headshots_Enum", stats.GetHeadshots().ToString());

		// time spent

		SetText("TotalTime_Enum", totalSpentHours > 0 ? totalSpentHours.ToString() : totalSpentMinutes.ToString());
		SetText("TotalTime_Units", TextDatabase.instance[timeSpentUnitTextId]);
		SetText("DMTime_Enum", string.Format(TextDatabase.instance[dmTimeSpentUnitTextId], dmSpentHours > 0 ? dmSpentHours : dmSpentMinutes));
		SetText("ZCTime_Enum", string.Format(TextDatabase.instance[zcTimeSpentUnitTextId], zcSpentHours > 0 ? zcSpentHours : zcSpentMinutes));
	}

	void SetText(string name, string text)
	{
		GUIBase_Label label = GuiBaseUtils.GetControl<GUIBase_Label>(Layout, name);
		label.SetNewText(text);
	}

	void SetProgress(string nameFg, string nameBg, int current, int max)
	{
		GUIBase_Widget widgetBg = Layout.GetWidget(nameBg);
		GUIBase_Widget widgetFg = Layout.GetWidget(nameFg);

		Transform emptyTrans = widgetBg.transform;
		Vector3 pos = emptyTrans.localPosition;
		Vector3 scale = emptyTrans.localScale;
		float width = widgetBg.GetWidth();
		float ratio = Mathf.Clamp(current/(float)max, 0.01f, 1.0f);

		pos.x -= (width - width*ratio)*scale.x*0.5f;
		scale.x *= ratio;

		Transform fullTrans = widgetFg.transform;
		fullTrans.localScale = scale;
		fullTrans.localPosition = pos;
		widgetFg.SetModify();
	}
}
