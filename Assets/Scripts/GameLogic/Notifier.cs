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
using DateTime = System.DateTime;

[RequireComponent(typeof (Game))]
public class Notifier : MonoBehaviour
{
	enum NotifyId
	{
		None,
		InstantReward,
		ConditionalReward,
		DeathMatch,
		ZoneControl,
		DeathMatchFriends,
		ZoneControlFriends,
		DeathMatch2xXP,
		ZoneControl2xXP
	}

	// MONOBEHAVIOUR INTERFACE

	void OnApplicationFocus(bool focus)
	{
		// When the application is switched to the fullscreen mode (web player on Mac OS X)
		// this method is called with focus == false.
		// Focus must be set to true.
		if (Application.platform == RuntimePlatform.OSXWebPlayer)
		{
			if (Screen.fullScreen)
			{
				focus = true;
			}
		}

		if (focus == false)
		{
			CancelScheduledNotifications();
			ScheduleNotifications();
		}

		// Unpausing could mean that user returns to the game by clicking a notification
		enabled = focus;
	}

	void Update()
	{
		if (CloudUser.instance.isUserAuthenticated == false)
			return;

		PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();
		if (ppi == null)
			return;

		ListReceivedNotifications(ppi);
		CancelScheduledNotifications();

		enabled = false;
	}

	// PRIVATE METHODS

	void ListReceivedNotifications(PlayerPersistantInfo ppi)
	{
		// On iOS currently if the app is running on foreground and the notification appears in
		// the notification area it's added to receivedNotifications. This means that we can't
		// find out that it was not delivered due to user tapping it.
		var receivedNotifications = MFNotificationService.ReceivedNotifications;

		foreach (var notification in receivedNotifications)
		{
			MFNotificationService.CancelNotification(notification.Id);
		}

		MFNotificationService.ClearReceivedNotifications();
		MFNativeUtils.AppIconBadgeNumber = 0;
	}

	void ScheduleNotifications()
	{
		if (CloudUser.instance.isUserAuthenticated == false)
			return;

		DateTime now = CloudDateTime.UtcNow;
		DateTime today = now.Date;

		// 2xXP bonus or conditional reward notification
		DateTime date = now.AddHours(1);
		E_MPGameType gameType = GetGameWithXpBonus();
		switch (gameType)
		{
		case E_MPGameType.DeathMatch:
			SheduleNotification(NotifyId.DeathMatch2xXP, TextDatabase.instance[00507000], date);
			break;
		case E_MPGameType.ZoneControl:
			SheduleNotification(NotifyId.ZoneControl2xXP, TextDatabase.instance[00507001], date);
			break;
		default:
			string text;
			if (GetConditionalRewardText(now, out text) == true)
			{
				SheduleNotification(NotifyId.ConditionalReward, text, date);
			}
			break;
		}

		// daily reward notification
		date = today.AddDays(1);
		SheduleNotification(NotifyId.InstantReward, TextDatabase.instance[00507002], date);

		// level up notification
		date = today.AddDays(2);
		if (Random.Range(0, 100) > 50)
		{
			SheduleNotification(NotifyId.DeathMatch, TextDatabase.instance[00507003], date);
		}
		else
		{
			SheduleNotification(NotifyId.ZoneControl, TextDatabase.instance[00507004], date);
		}

		// play with friends notification
		if (GameCloudManager.friendList.friends.Count > 0)
		{
			date = today.AddDays(4);
			if (Random.Range(0, 100) > 50)
			{
				SheduleNotification(NotifyId.DeathMatchFriends, TextDatabase.instance[00507005], date);
			}
			else
			{
				SheduleNotification(NotifyId.ZoneControlFriends, TextDatabase.instance[00507006], date);
			}
		}
	}

	void SheduleNotification(NotifyId id, string text, DateTime date)
	{
		MFNotificationService.Notify((int)id, new MFNotification("SHADOWGUN: DEADZONE", text, "app_icon", "", 1), date);
	}

	void CancelScheduledNotifications()
	{
		MFNotificationService.CancelAll();
	}

	E_MPGameType GetGameWithXpBonus()
	{
		PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();
		if (ppi == null)
			return E_MPGameType.None;

		int rank = ppi.Rank;
		for (E_MPGameType gameType = E_MPGameType.DeathMatch; gameType < E_MPGameType.None; ++gameType)
		{
			GameTypeInfo gameInfo = GameInfoSettings.GetGameInfo(gameType);
			if (gameInfo == null)
				continue;
			if (gameInfo.MinimalDesiredRankToPlay > rank)
				continue;
			if (ppi.IsFirstGameToday(gameType) == false)
				continue;

			return gameType;
		}

		return E_MPGameType.None;
	}

	bool GetConditionalRewardText(DateTime date, out string text)
	{
		text = string.Empty;

		// do not display conditional reward
		// if there is not enough time to accomplish it
		DateTime today = date.Date;
		if (date.AddHours(1.5).Date != today)
			return false;

		PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();
		if (ppi == null)
			return false;

		var rewards = PPIManager.Instance.GetLocalPPI().DailyRewards;
		var day = rewards.GetCurrentConditionalDay();
		if (day.Received == true)
			return false;

		// deduce real condition value
		int gained = 0;
		switch (day.Condition.Type)
		{
		case PPIDailyRewards.E_ConditionType.GainExperience:
			gained = ppi.Statistics.Today.Experience;
			break;
		case PPIDailyRewards.E_ConditionType.PlayNumberOfMatches:
			gained = ppi.Statistics.Today.GamesFinished;
			break;
		case PPIDailyRewards.E_ConditionType.WinNumberOfMatches:
			gained = ppi.Statistics.Today.GamesWon;
			break;
		default:
			return false;
		}

		// deduce estimated condition value
		int condition = ppi.Statistics.Today.Date != today ? day.Condition.Value : day.Condition.Value - gained;
		if (condition <= 0)
			return false;

		// get text id for condition
		int textId = 0;
		switch (day.Condition.Type)
		{
		case PPIDailyRewards.E_ConditionType.GainExperience:
			textId = condition == 1 ? 00507007 : 00507007;
			break;
		case PPIDailyRewards.E_ConditionType.PlayNumberOfMatches:
			textId = condition == 1 ? 00507008 : 00507009;
			break;
		case PPIDailyRewards.E_ConditionType.WinNumberOfMatches:
			textId = condition == 1 ? 00507010 : 00507011;
			break;
		default:
			return false;
		}

		// get reward label
		string label = string.Empty;
		switch (day.Reward.Type)
		{
		case PPIDailyRewards.E_RewardType.Chips:
			int labelId = day.Reward.Value == 1 ? 00507012 : 00507013;
			label = string.Format(TextDatabase.instance[labelId], day.Reward.Value);
			break;
		case PPIDailyRewards.E_RewardType.Gold:
			label = string.Format(TextDatabase.instance[00507014], day.Reward.Value);
			break;
		case PPIDailyRewards.E_RewardType.Item:
			if (day.Reward.Value > 0 && rewards.CurrentDay < (PPIDailyRewards.DAYS_PER_CYCLE - 1))
			{
				ItemSettings item = ItemSettingsManager.Instance.FindByGUID(day.Reward.Id);
				label = item != null ? TextDatabase.instance[item.Name].ToUpper() : string.Empty;
			}
			else
			{
				label = TextDatabase.instance[00507015];
			}
			break;
		default:
			return false;
		}

		// format final text
		text = string.Format(TextDatabase.instance[textId], condition, label);

		// done
		return string.IsNullOrEmpty(text) ? false : true;
	}
}
