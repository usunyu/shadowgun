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
using DateTime = System.DateTime;
using TimeSpan = System.TimeSpan;

public class GuiPopupDailyRewards : GuiPopup
{
	readonly static string CLOSE_BUTTON = "Close_Button";
	readonly static string SKIPANIM_BUTTON = "SkipAnim_Button";

	enum E_Status
	{
		Idle,
		Missed,
		Gained,
		Current,
	}

	struct DayInfo
	{
		public DateTime Date;
		public E_Status Status;
		public Vector2 Scale;
		public PPIDailyRewards.Day Instant;
		public PPIDailyRewards.Day Conditional;
	}

	abstract class Gadget
	{
		GUIBase_Widget m_BackgroundIdle;
		GUIBase_Widget m_BackgroundCurrent;
		GUIBase_Widget m_BackgroundGained;
		GUIBase_Widget m_BackgroundMissed;

		public bool Highlight
		{
			get { return m_BackgroundCurrent.Visible; }
			set
			{
				ShowWidget(m_BackgroundIdle, !value);
				ShowWidget(m_BackgroundCurrent, value);
			}
		}

		public Gadget(GUIBase_Widget root)
		{
			m_BackgroundIdle = GuiBaseUtils.GetChild<GUIBase_Widget>(root, "BackgroundIdle");
			m_BackgroundCurrent = GuiBaseUtils.GetChild<GUIBase_Widget>(root, "BackgroundCurrent");
			m_BackgroundGained = GuiBaseUtils.GetChild<GUIBase_Widget>(root, "BackgroundGained");
			m_BackgroundMissed = GuiBaseUtils.GetChild<GUIBase_Widget>(root, "BackgroundMissed");
		}

		public virtual void Update(DayInfo info)
		{
			ShowWidget(m_BackgroundIdle, info.Status == E_Status.Idle);
			ShowWidget(m_BackgroundCurrent, info.Status == E_Status.Current);
			ShowWidget(m_BackgroundGained, info.Status == E_Status.Gained);
			ShowWidget(m_BackgroundMissed, info.Status == E_Status.Missed);
		}

		protected void ShowWidget(GUIBase_Widget widget, bool state)
		{
			if (widget.Visible != state)
			{
				widget.ShowImmediate(state, true);
			}
		}
	}

	abstract class Reward : Gadget
	{
		GUIBase_Label m_TextLabel;
		GUIBase_Label m_ItemCountLabel;
		GUIBase_Sprite m_GoldImage;
		GUIBase_Sprite m_ChipsImage;
		GUIBase_Sprite m_ItemImage;
		GUIBase_Sprite m_MagicImage;

		public Reward(GUIBase_Widget root) : base(root)
		{
			m_TextLabel = GuiBaseUtils.GetChild<GUIBase_Label>(root, "Text_Label");
			m_ItemCountLabel = GuiBaseUtils.GetChild<GUIBase_Label>(root, "ItemCount_Label");
			m_GoldImage = GuiBaseUtils.GetChild<GUIBase_Sprite>(root, "Gold_Image");
			m_ChipsImage = GuiBaseUtils.GetChild<GUIBase_Sprite>(root, "Chips_Image");
			m_ItemImage = GuiBaseUtils.GetChild<GUIBase_Sprite>(root, "Item_Image");
			m_MagicImage = GuiBaseUtils.GetChild<GUIBase_Sprite>(root, "Magic_Image");
		}

		protected void Update(PPIDailyRewards.Reward reward)
		{
			string text;
			switch (reward.Type)
			{
			case PPIDailyRewards.E_RewardType.Chips:
				int textId = reward.Value == 1 ? 00502095 : 00502076;
				text = string.Format(TextDatabase.instance[textId], reward.Value);
				break;
			case PPIDailyRewards.E_RewardType.Gold:
				text = string.Format(TextDatabase.instance[00502077], reward.Value);
				break;
			case PPIDailyRewards.E_RewardType.Item:
				if (reward.Value >= 0)
				{
					var item = ItemSettingsManager.Instance.FindByGUID(reward.Id);
					text = item != null ? TextDatabase.instance[item.Name] : string.Empty;
					if (item != null)
					{
						m_ItemImage.Widget.CopyMaterialSettings(item.ShopWidget);
						m_ItemCountLabel.SetNewText((reward.Value > 0 ? reward.Value : item.Count).ToString());
					}
				}
				else if (reward.Value < 0)
				{
					text = TextDatabase.instance[00502101];
				}
				else
				{
					text = string.Empty;
				}
				break;
			default:
				text = string.Empty;
				break;
			}

			m_TextLabel.SetNewText(text.ToUpper());

			ShowWidget(m_GoldImage.Widget, reward.Type == PPIDailyRewards.E_RewardType.Gold);
			ShowWidget(m_ChipsImage.Widget, reward.Type == PPIDailyRewards.E_RewardType.Chips);
			ShowWidget(m_ItemImage.Widget, reward.Type == PPIDailyRewards.E_RewardType.Item && reward.Value >= 0);
			ShowWidget(m_MagicImage.Widget, reward.Type == PPIDailyRewards.E_RewardType.Item && reward.Value < 0);
		}
	}

	class InstantReward : Reward
	{
		public InstantReward(GUIBase_Widget root) : base(root)
		{
		}

		public override void Update(DayInfo info)
		{
			info.Status = ModifyStatus(info.Status, info.Date, info.Instant);

			base.Update(info);

			Update(info.Instant.Reward);
		}

		GuiPopupDailyRewards.E_Status ModifyStatus(GuiPopupDailyRewards.E_Status status, DateTime date, PPIDailyRewards.Day day)
		{
			if (status != GuiPopupDailyRewards.E_Status.Current)
				return status;
			if (date != CloudDateTime.UtcNow.Date)
				return status;
			return GuiPopupDailyRewards.E_Status.Gained;
		}
	}

	class ConditionalReward : Reward
	{
		GUIBase_Label m_ConditionLabel;

		public ConditionalReward(GUIBase_Widget root) : base(root)
		{
			m_ConditionLabel = GuiBaseUtils.GetChild<GUIBase_Label>(root, "Condition_Label");
		}

		public override void Update(DayInfo info)
		{
			info.Status = ModifyStatus(info.Status, info.Conditional);

			base.Update(info);

			Update(info.Conditional.Reward);

			int textId;
			switch (info.Conditional.Condition.Type)
			{
			case PPIDailyRewards.E_ConditionType.GainExperience:
				textId = info.Conditional.Condition.Value == 1 ? 00502096 : 00502096;
				break;
			case PPIDailyRewards.E_ConditionType.PlayNumberOfMatches:
				textId = info.Conditional.Condition.Value == 1 ? 00502097 : 00502098;
				break;
			case PPIDailyRewards.E_ConditionType.WinNumberOfMatches:
				textId = info.Conditional.Condition.Value == 1 ? 00502099 : 00502100;
				break;
			default:
				textId = 0;
				break;
			}

			string text = textId > 0 ? string.Format(TextDatabase.instance[textId], info.Conditional.Condition.Value) : string.Empty;
			m_ConditionLabel.SetNewText(text);
		}

		GuiPopupDailyRewards.E_Status ModifyStatus(GuiPopupDailyRewards.E_Status status, PPIDailyRewards.Day day)
		{
			switch (status)
			{
			case GuiPopupDailyRewards.E_Status.Current:
				return day.Received == true ? GuiPopupDailyRewards.E_Status.Gained : GuiPopupDailyRewards.E_Status.Idle;
			case GuiPopupDailyRewards.E_Status.Gained:
				return day.Received == true ? GuiPopupDailyRewards.E_Status.Gained : GuiPopupDailyRewards.E_Status.Missed;
			default:
				return status;
			}
		}
	}

	class Day : Gadget
	{
		GUIBase_Widget m_Root;
		Vector3 m_RootScale;
		GUIBase_Label m_DayLabel;
		Gadget m_DailyReward;
		Gadget m_ConditionalReward;

		public void SetHighlight(bool instant, bool conditional)
		{
			m_DailyReward.Highlight = instant;
			m_ConditionalReward.Highlight = conditional;
		}

		public Day(GUIBase_Widget root) : base(root)
		{
			m_Root = root;
			m_RootScale = root.transform.localScale;
			m_DayLabel = GuiBaseUtils.GetChild<GUIBase_Label>(root, "Day_Label");

			m_DailyReward = new InstantReward(GuiBaseUtils.GetChild<GUIBase_Widget>(root, "DailyReward"));
			m_ConditionalReward = new ConditionalReward(GuiBaseUtils.GetChild<GUIBase_Widget>(root, "ConditionalReward"));
		}

		public override void Update(DayInfo info)
		{
			base.Update(info);
			m_DailyReward.Update(info);
			m_ConditionalReward.Update(info);

			m_Root.transform.localScale = new Vector3(m_RootScale.x*info.Scale.x, m_RootScale.y*info.Scale.y, m_RootScale.z);
			m_Root.SetModify(true);

			m_DayLabel.SetNewText(GuiBaseUtils.DateToDaysString(info.Date));
		}
	}

	// CONFIGURATION

	[SerializeField] AudioClip m_GainedAudio;
	[SerializeField] AudioClip m_MissedAudio;
	[SerializeField] AudioClip m_RollbackAudio;

	// PRIVATE MEMBERS

	Day[] m_Days = new Day[PPIDailyRewards.DAYS_PER_CYCLE];
	float m_AnimationSpeed;

	// PUBLIC METHODS

	public void SetData(PPIDailyRewards newRewards, bool instant, bool conditional, PPIDailyRewards oldRewards, DateTime oldRewardsDate)
	{
		StartCoroutine(ShowRewards_Coroutine(newRewards, instant, conditional, oldRewards, oldRewardsDate));
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

		for (int idx = 0; idx < PPIDailyRewards.DAYS_PER_CYCLE; ++idx)
		{
			m_Days[idx] = new Day(Layout.GetWidget("Day_" + idx));
		}

		// run watch dog
		Transform parent = transform.parent;
		while (parent != null)
		{
			GuiMenu menu = parent.GetComponent<GuiMenu>();
			if (menu == null)
			{
				parent = parent.parent;
			}
			else
			{
				menu.gameObject.AddComponent<DailyRewardsWatchDog>();
				break;
			}
		}
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		m_AnimationSpeed = 1.0f;

		RegisterButtonDelegate(CLOSE_BUTTON, () => { Owner.Back(); }, null);
		RegisterButtonDelegate(SKIPANIM_BUTTON, () => { m_AnimationSpeed = 0.05f; }, null);
	}

	protected override void OnViewHide()
	{
		StopAllCoroutines();

		RegisterButtonDelegate(CLOSE_BUTTON, null, null);
		RegisterButtonDelegate(SKIPANIM_BUTTON, null, null);

		base.OnViewHide();
	}

	// PRIVATE METHODS

	IEnumerator ShowRewards_Coroutine(PPIDailyRewards newRewards,
									  bool instant,
									  bool conditional,
									  PPIDailyRewards oldRewards,
									  DateTime oldRewardsDate)
	{
		if (oldRewards != null)
		{
			yield return StartCoroutine(ShowMissedRewards_Coroutine(newRewards, oldRewards, oldRewardsDate));
		}

		yield return StartCoroutine(ShowCurrentRewards_Coroutine(newRewards, instant, conditional));
	}

	IEnumerator ShowMissedRewards_Coroutine(PPIDailyRewards newRewards, PPIDailyRewards oldRewards, DateTime oldRewardsDate)
	{
		int currDay = oldRewards.CurrentDay;
		int lastDay = PPIDailyRewards.DAYS_PER_CYCLE - 1;
		DateTime date = oldRewardsDate - TimeSpan.FromDays(currDay);

		DayInfo[] infos = new DayInfo[PPIDailyRewards.DAYS_PER_CYCLE];
		for (int idx = 0; idx < PPIDailyRewards.DAYS_PER_CYCLE; ++idx)
		{
			infos[idx] = new DayInfo()
			{
				Date = date + TimeSpan.FromDays(idx),
				Status = idx <= currDay ? E_Status.Gained : E_Status.Idle,
				Scale = Vector2.one,
				Instant = oldRewards.Instant[idx].DeepCopy(),
				Conditional = oldRewards.Conditional[idx].DeepCopy()
			};
		}

		// display current status
		UpdateDays(infos);

		yield return new WaitForSeconds(0.5f);

		// show missed days
		int day = currDay;
		while (day < lastDay)
		{
			yield return new WaitForSeconds(0.25f*m_AnimationSpeed);

			day += 1;

			DayInfo info = infos[day];
			info.Status = E_Status.Missed;
			infos[day] = info;

			if (MFGuiManager.Instance != null)
			{
				MFGuiManager.Instance.PlayOneShot(m_MissedAudio);
			}

			UpdateDays(infos);
		}

		yield return new WaitForSeconds(0.5f*m_AnimationSpeed);

		date = CloudDateTime.UtcNow.Date - TimeSpan.FromDays(newRewards.CurrentDay);
		day = Mathf.Min(day, lastDay);

		// rollback to the first day
		while (true)
		{
			if (MFGuiManager.Instance != null)
			{
				MFGuiManager.Instance.PlayOneShot(m_RollbackAudio);
			}

			int frames = 3;
			for (int frame = 0; frame < frames; ++frame)
			{
				DayInfo info = infos[day];
				info.Scale.x = 1.0f - 1.0f*(frame/(float)(frames - 1));
				infos[day] = info;

				UpdateDays(infos);

				yield return new WaitForEndOfFrame();
			}

			infos[day] = new DayInfo()
			{
				Date = date + TimeSpan.FromDays(day),
				Status = E_Status.Idle,
				Scale = Vector2.one,
				Instant = newRewards.Instant[day].DeepCopy(),
				Conditional = newRewards.Conditional[day].DeepCopy()
			};

			for (int frame = 0; frame < frames; ++frame)
			{
				DayInfo info = infos[day];
				info.Scale.x = 1.0f*(frame/(float)(frames - 1));
				infos[day] = info;

				UpdateDays(infos);

				yield return new WaitForEndOfFrame();
			}

			day -= 1;
			if (day < 0)
				break;

			yield return new WaitForSeconds(0.1f*m_AnimationSpeed);
		}

		yield return new WaitForSeconds(0.5f*m_AnimationSpeed);
	}

	IEnumerator ShowCurrentRewards_Coroutine(PPIDailyRewards rewards, bool instant, bool conditional)
	{
		int currDay = rewards.CurrentDay;
		DateTime date = CloudDateTime.UtcNow.Date - TimeSpan.FromDays(currDay);

		DayInfo[] infos = new DayInfo[PPIDailyRewards.DAYS_PER_CYCLE];
		for (int idx = 0; idx < PPIDailyRewards.DAYS_PER_CYCLE; ++idx)
		{
			E_Status status = E_Status.Idle;
			if (idx < currDay)
				status = E_Status.Gained;
			else if (idx == currDay)
				status = E_Status.Current;

			infos[idx] = new DayInfo()
			{
				Date = date + TimeSpan.FromDays(idx),
				Status = status,
				Scale = Vector2.one,
				Instant = rewards.Instant[idx].DeepCopy(),
				Conditional = rewards.Conditional[idx].DeepCopy()
			};
			infos[idx].Instant.Received = idx <= currDay ? true : false;
		}

		UpdateDays(infos);

		yield return new WaitForSeconds(0.5f);

		if (MFGuiManager.Instance != null)
		{
			MFGuiManager.Instance.PlayOneShot(m_GainedAudio);
		}

		// highlight current reward
		if (instant == true || conditional == true)
		{
			bool blinkInstant = instant;
			bool blinkConditional = conditional;

			for (int idx = 0; idx < 6; ++idx)
			{
				yield return new WaitForSeconds(0.15f);

				m_Days[currDay].SetHighlight(blinkInstant, blinkConditional);

				blinkInstant = instant ? !blinkInstant : false;
				blinkConditional = conditional ? !blinkConditional : false;
			}

			m_Days[currDay].SetHighlight(instant, conditional);
		}
	}

	void UpdateDays(DayInfo[] infos)
	{
		int lastDay = PPIDailyRewards.DAYS_PER_CYCLE - 1;

		for (int idx = 0, last = PPIDailyRewards.DAYS_PER_CYCLE - 1; idx <= last; ++idx)
		{
			DayInfo info = infos[idx];

			// should we display magic box?
			if (idx == lastDay)
			{
				if (info.Status != E_Status.Current && info.Status != E_Status.Gained)
				{
					info.Instant.Reward.Value = -1;
				}
				if (info.Conditional.Received == false)
				{
					info.Conditional.Reward.Value = -1;
				}
			}

			// update day
			m_Days[idx].Update(info);
		}
	}
}
