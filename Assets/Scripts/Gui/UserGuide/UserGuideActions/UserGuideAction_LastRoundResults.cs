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

//#define FAKE_FINAL_RESULT

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class UserGuideAction_LastRoundResults<T> : UserGuideAction_SystemDialogs<T>
				where T : GuiPopup
{
	// USERGUIDEACTION INTERFACE

	protected override bool OnExecute()
	{
		if (base.OnExecute() == false)
			return false;
		if (GuiFrontendMain.IsVisible == false)
			return false;
		if (GuideData.LocalPPI == null)
			return false;
#if !FAKE_FINAL_RESULT
		if (GuideData.LastRoundResult == null)
			return false;
#else
		FakeFinalResults();
#endif
		return true;
	}

#if FAKE_FINAL_RESULT
	void FakeFinalResults()
	{
		GuideData.LastRoundResult = new RoundFinalResult();
		for (int idx =0; idx < 12; ++idx)
		{
			GuideData.LastRoundResult.PlayersScore.Add(new RoundFinalResult.PlayerResult() {
				Team = Random.Range(0, 2) == 0 ? E_Team.Good : E_Team.Bad,
				PrimaryKey = idx == 0 ? CloudUser.instance.primaryKey : "user_" + idx,
				NickName = idx == 0 ? CloudUser.instance.nickName : "user_" + idx,
				Score = Random.Range(0, 10000),
				Deaths = Random.Range(0, 20),
				Kills = Random.Range(0, 20),
				Experience = Random.Range(0, 1000000),
				Platform = Random.Range(0, 2) == 0 ? RuntimePlatform.IPhonePlayer : RuntimePlatform.Android
			});
		}
		GuideData.LastRoundResult.GameType = E_MPGameType.ZoneControl;
		GuideData.LastRoundResult.Gold = 500;
		GuideData.LastRoundResult.Prey.UserName   = "user_1";
		GuideData.LastRoundResult.Prey.KilledMe   = Random.Range(0, 5);
		GuideData.LastRoundResult.Prey.KilledByMe = Random.Range(5, 20);
		GuideData.LastRoundResult.Nightmare.UserName   = "user_2";
		GuideData.LastRoundResult.Nightmare.KilledMe   = Random.Range(5, 20);
		GuideData.LastRoundResult.Nightmare.KilledByMe = Random.Range(0, 5);
		GuideData.LastRoundResult.Experience = (short)GuideData.LastRoundResult.PlayersScore[0].Experience;
		GuideData.LastRoundResult.MissionExp = 200;
		GuideData.LastRoundResult.Money = 700;
		GuideData.LastRoundResult.Winner = true;
		GuideData.LastRoundResult.Team = GuideData.LastRoundResult.PlayersScore[0].Team;
		GuideData.LastRoundResult.FirstRound = true;
		GuideData.LastRoundResult.NewRank = true;
		GuideData.LastRoundResult.Place = 0;
		GuideData.LastRoundResult.MapName = "TEST";
	}
#endif
}

public class UserGuideAction_FinalResults : UserGuideAction_LastRoundResults<GuiPopupFinalResults>
{
	// C-TOR

	public UserGuideAction_FinalResults()
	{
		Priority = (int)E_UserGuidePriority.FinalResults;
	}

	// USERGUIDEACTION INTERFACE

	protected override bool OnExecute()
	{
		if (base.OnExecute() == false)
			return false;

		// display popup
		ShowPopup().SetData(GuideData.LastRoundResult);

		// done		
		return true;
	}
}

public class UserGuideAction_PlayerEarnings : UserGuideAction_LastRoundResults<GuiPopupPlayerEarnings>
{
	// C-TOR

	public UserGuideAction_PlayerEarnings()
	{
		Priority = (int)E_UserGuidePriority.PlayerEarnings;
	}

	// USERGUIDEACTION INTERFACE

	protected override bool OnExecute()
	{
		if (base.OnExecute() == false)
			return false;

		var ppi = GuideData.LocalPPI;
		var results = GuideData.LastRoundResult;

		if (results.Experience == 0 && results.MissionExp == 0 && results.Money == 0 && results.MissioMoney == 0)
		{
			// there is nothing to display update local ppi now
			FetchPPIFromCloud();
			return false;
		}

		var playerResult = results.PlayersScore.Find(obj => obj.PrimaryKey == ppi.PrimaryKey) ?? new RoundFinalResult.PlayerResult();

		// display popup
		ShowPopup().SetData(GuideData.LastRoundResult, playerResult);

		// done		
		return true;
	}

	protected override void OnPopupHides(E_PopupResultCode result)
	{
		// user closed popup,
		// we can update local ppi now
		FetchPPIFromCloud();

		base.OnPopupHides(result);
	}

	void FetchPPIFromCloud()
	{
		FetchPlayerPersistantInfo action = new FetchPlayerPersistantInfo(CloudUser.instance.authenticatedUserID);
		GameCloudManager.AddAction(action);
	}
}

public class UserGuideAction_Promotion : UserGuideAction_LastRoundResults<GuiPopupPromote>
{
	// C-TOR

	public UserGuideAction_Promotion()
	{
		Priority = (int)E_UserGuidePriority.Promotion;
	}

	// USERGUIDEACTION INTERFACE

	protected override bool OnExecute()
	{
		if (base.OnExecute() == false)
			return false;

		var results = GuideData.LastRoundResult;

		if (results.NewRank == false)
			return false;

		var ppi = GuideData.LocalPPI;
		int nextRankXp = PlayerPersistantInfo.GetPlayerMinExperienceForRank(Mathf.Clamp(ppi.Rank + 1, 1, PlayerPersistantInfo.MAX_RANK));
		int points = Mathf.Max(0, nextRankXp - ppi.Experience);

		// display popup
		ShowPopup().SetData(ppi.Rank, points);

		// done
		return true;
	}
}

public class UserGuideAction_UnlockedItems : UserGuideAction_LastRoundResults<GuiPopupUnlockedItems>
{
	// C-TOR

	public UserGuideAction_UnlockedItems()
	{
		Priority = (int)E_UserGuidePriority.UnlockedItems;
	}

	// USERGUIDEACTION INTERFACE

	protected override bool OnExecute()
	{
		if (base.OnExecute() == false)
			return false;

		var ppi = GuideData.LocalPPI;
		var results = GuideData.LastRoundResult;

		if (results.NewRank == false)
			return false;

		ResearchItem[] researchItems = ResearchSupport.Instance.GetItems();
		List<ResearchItem> items = new List<ResearchItem>();
		foreach (var item in researchItems)
		{
			// we need to call this first
			// to validate it's internal state
			item.Validate();

			// check minimal rank now
			if (item.GetRequiredRank() == ppi.Rank)
			{
				items.Add(item);
			}
		}

		if (items.Count == 0)
			return false;

		// display popup
		ShowPopup().SetData(items);

		// done
		return true;
	}

	protected override void OnPopupHides(E_PopupResultCode result)
	{
		GuideData.ShowOffers = false;

		base.OnPopupHides(result);
	}
}
