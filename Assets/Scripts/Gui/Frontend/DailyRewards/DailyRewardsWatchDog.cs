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

//#define FAKE_DAILY_REWARDS

using UnityEngine;
using System.Collections;
using DateTime = System.DateTime;
using TimeSpan = System.TimeSpan;

public class DailyRewardsWatchDog : MonoBehaviour
{
	// PRIVATE MEMBERS

	IViewOwner m_Menu;
	UnigueUserID m_UserId;
	UserGuideAction_DailyRewards m_UserGuideAction = new UserGuideAction_DailyRewards();

	// MONOBEHAVIOUR INTERFACE

	void Awake()
	{
		m_Menu = GetComponent<GuiMenu>();

		CloudUser.authenticationChanged += OnUserAuthenticationChanged;
	}

	void OnDestroy()
	{
		CloudUser.authenticationChanged -= OnUserAuthenticationChanged;
		OnUserAuthenticationChanged(false);
	}

	// HANDLERS

	void OnUserAuthenticationChanged(bool state)
	{
		if (state == true)
		{
			m_UserId = CloudUser.instance.authenticatedUserID;
			UserGuide.RegisterAction(m_UserGuideAction);
			StartCoroutine(CheckDailyRewards_Coroutine());
		}
		else
		{
			StopAllCoroutines();
			UserGuide.UnregisterAction(m_UserGuideAction);
			m_UserId = null;
		}
	}

	// PRIVATE METHODS

	public IEnumerator CheckDailyRewards_Coroutine()
	{
		yield return new WaitForSeconds(1.0f);

		while (m_UserId != null)
		{
			if (Ftue.IsActive == true)
			{
				yield return new WaitForSeconds(0.5f);
			}
			else if (m_Menu == null || m_Menu.IsAnyPopupVisible() == true)
			{
				yield return new WaitForSeconds(1.0f);
			}
			else
			{
				GetDailyRewards rewards = new GetDailyRewards(m_UserId);
				GameCloudManager.AddAction(rewards);

				while (rewards.isDone == false)
				{
					yield return new WaitForEndOfFrame();
				}

#if !FAKE_DAILY_REWARDS
				if (rewards.HasReward == true)
#endif
				{
					FetchPlayerPersistantInfo ppi = new FetchPlayerPersistantInfo(m_UserId);
					GameCloudManager.AddAction(ppi);

					while (ppi.isDone == false)
					{
						yield return new WaitForEndOfFrame();
					}

#if !FAKE_DAILY_REWARDS
					UserGuideAction_DailyRewards.HasInstant |= rewards.HasInstant;
					UserGuideAction_DailyRewards.HasConditional |= rewards.HasConditional;
#else
					UserGuideAction_DailyRewards.HasInstant     |= true;
					UserGuideAction_DailyRewards.HasConditional |= true;
#endif
				}

				yield return new WaitForSeconds(60.0f*60); // wait one hour
			}
		}
	}
}

public class UserGuideAction_DailyRewards : UserGuideAction_SystemDialogs<GuiPopupDailyRewards>
{
	// PRIVATE MEMBERS

	public static bool HasInstant;
	public static bool HasConditional;

	public static bool HasReward
	{
		get { return HasInstant || HasConditional; }
	}

	// C-TOR

	public UserGuideAction_DailyRewards()
	{
		Priority = (int)E_UserGuidePriority.DailyRewards;
		AllowRepeatedExecution = true;
	}

	// PUBLIC METHODS

	// USERGUIDEACTION INTERFACE

	protected override bool OnExecute()
	{
		if (base.OnExecute() == false)
			return false;

		if (HasReward == false)
			return false;

		// prepare data
#if !FAKE_DAILY_REWARDS
		DateTime oldDate = CloudDateTime.UtcNow.Date;
		var oldRewards = PPIDailyRewardsExtension.Restore(GuideData.PrimaryKey, ref oldDate);
		var newRewards = PPIManager.Instance.GetLocalPPI().DailyRewards;
#else
		DateTime   oldDate = CloudDateTime.UtcNow.Date;
		var     oldRewards = PPIDailyRewardsExtension.Restore(GuideData.PrimaryKey, ref oldDate, "rewards_fake_old");
		DateTime   newDate = CloudDateTime.UtcNow.Date;
		var     newRewards = PPIDailyRewardsExtension.Restore(GuideData.PrimaryKey, ref newDate, "rewards_fake");
#endif
		DateTime today = CloudDateTime.UtcNow.Date;
		DateTime yesterday = today - TimeSpan.FromDays(1);

		if (oldDate == today || oldDate == yesterday)
		{
			oldRewards = null;
		}

		// store rewards locally
		newRewards.Store(GuideData.PrimaryKey);

		// inform user now
		ShowPopup().SetData(newRewards, HasInstant, HasConditional, oldRewards, oldDate);

		// done
		return true;
	}

	protected override void OnPopupHides(E_PopupResultCode result)
	{
		HasInstant = false;
		HasConditional = false;
		GuideData.ShowOffers = false;

		base.OnPopupHides(result);
	}
}
