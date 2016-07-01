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

public class GuiPagePlayerStatsRecentMatches : GuiScreen, IGuiPagePlayerStats
{
	// IGUIPAGEPLAYERSTATS INTERFACE

	void IGuiPagePlayerStats.Refresh(PlayerPersistantInfo ppi)
	{
		// collected values

		PPIPlayerStats stats = ppi.Statistics;
		PPIPlayerStats.GameData dm = stats.GetGameData(E_MPGameType.DeathMatch);
		PPIPlayerStats.GameData zc = stats.GetGameData(E_MPGameType.ZoneControl);

		// death match

		SetText("DMKills_Enum", dm.Kills.ToString());
		SetText("DMDeaths_Enum", dm.Deaths.ToString());
		SetText("DMGoldEarn_Enum", dm.Golds.ToString());
		SetText("DMMoneyEarn_Enum", dm.Money.ToString());
		SetText("DMXPEarn_Enum", dm.Experience.ToString());
		SetText("DMTotal_Enum", dm.Score.ToString());

		// zone control

		SetText("ZCKills_Enum", zc.Kills.ToString());
		SetText("ZCDeaths_Enum", zc.Deaths.ToString());
		SetText("ZCGoldEarn_Enum", zc.Golds.ToString());
		SetText("ZCMoneyEarn_Enum", zc.Money.ToString());
		SetText("ZCXPEarn_Enum", zc.Experience.ToString());
		SetText("ZCTotal_Enum", zc.Score.ToString());
	}

	// PRIVATE METHODS

	void SetText(string name, string text)
	{
		GUIBase_Label label = GuiBaseUtils.GetControl<GUIBase_Label>(Layout, name);
		label.SetNewText(text);
	}
}
