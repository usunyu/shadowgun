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

public class ScreenComponent_FinalResult : ScreenComponent
{
	// PRIVATE MEMBERS

	GUIBase_MultiSprite m_ZoneControl;
	GUIBase_Widget[] m_DeathMatch = new GUIBase_Widget[3];

	// SCREENCOMPONENT INTERFACE

	public override string ParentName
	{
		get { return "ResultImage"; }
	}

	// GUICOMPONENT INTERFACE

	protected override bool OnInit()
	{
		if (base.OnInit() == false)
			return false;

		GUIBase_Widget[] children = Parent.GetComponentsInChildren<GUIBase_Widget>();
		foreach (var child in children)
		{
			switch (child.name)
			{
			case "ZoneControl":
				m_ZoneControl = child.GetComponent<GUIBase_MultiSprite>();
				break;
			case "DeathMatch1st":
				m_DeathMatch[0] = child;
				break;
			case "DeathMatch2nd":
				m_DeathMatch[1] = child;
				break;
			case "DeathMatch3rd":
				m_DeathMatch[2] = child;
				break;
			}
		}

		return true;
	}

	protected override void OnShow()
	{
		base.OnShow();

		int bonusXp = 0;
		int bonusMoney = 0;

		GUIBase_Widget widget = null;
		Client.GameInfo gameInfo = Client.Instance.GameState;
		if (gameInfo != null)
		{
			if (gameInfo.GameType == E_MPGameType.ZoneControl)
			{
				PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();
				if (ppi != null)
				{
					E_Team enemy = ppi.Team == E_Team.Good ? E_Team.Bad : E_Team.Good;
					int ourScore = gameInfo.ZCInfo.TeamScore[ppi.Team];
					int enemyScore = gameInfo.ZCInfo.TeamScore[enemy];

					m_ZoneControl.State = ourScore <= enemyScore ? "Lose" : "Win";

					widget = m_ZoneControl.Widget;

					bonusXp = ourScore <= enemyScore ? GameplayRewards.ZC.Lost : GameplayRewards.ZC.Win;
				}
			}
			else
			{
				PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();
				List<PlayerPersistantInfo> list = new List<PlayerPersistantInfo>(PPIManager.Instance.GetPPIList());
				list.Sort(ComparePPIsByScore);

				int max = Mathf.Min(list.Count, m_DeathMatch.Length);
				int idx = 0;
				for (; idx < max; ++idx)
				{
					if (list[idx].Player == ppi.Player)
					{
						widget = m_DeathMatch[idx];
						break;
					}
				}

				switch (idx)
				{
				case 0:
					bonusXp = GameplayRewards.DM.First;
					break;
				case 1:
					bonusXp = GameplayRewards.DM.First;
					break;
				case 2:
					bonusXp = GameplayRewards.DM.First;
					break;
				default:
					break;
				}
			}
		}

		if (CloudUser.instance.isPremiumAccountActive == true)
		{
			bonusXp = Mathf.RoundToInt(bonusXp*GameplayRewards.PremiumAccountModificator);
		}
		bonusMoney = Mathf.RoundToInt(bonusXp*GameplayRewards.MoneyModificator);

		GuiBaseUtils.GetControl<GUIBase_Label>(Owner.Layout, "BonusXp_Label")
					.SetNewText(string.Format(TextDatabase.instance[0502073], bonusXp.ToString()));
		GuiBaseUtils.GetControl<GUIBase_Label>(Owner.Layout, "BonusMoney_Label")
					.SetNewText(string.Format(TextDatabase.instance[0502074], bonusMoney.ToString()));

		if (widget != null)
		{
			widget.ShowImmediate(true, true);
		}
	}

	// PRIVATE METHODS

	int ComparePPIsByScore(PlayerPersistantInfo left, PlayerPersistantInfo right)
	{
		int res = right.Score.Score.CompareTo(left.Score.Score); // 1) descending by score
		if (res == 0)
		{
			res = right.Score.Kills.CompareTo(left.Score.Kills); // 2) descending by kills
			if (res == 0)
			{
				res = left.Score.Deaths.CompareTo(right.Score.Deaths); // 3) increasing by deaths
				if (res == 0)
				{
					res = left.Name.CompareTo(right.Name); // 4) increasing by names
				}
			}
		}
		return res;
	}
}
