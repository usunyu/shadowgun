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
using System.Collections.Generic;

public class GuiOverlaySideBarQuickButtons : GuiOverlaySideBar
{
	// PRIVATE MEMBERS

	bool m_AnyScreenVisible = false;
	float m_LayoutAlpha = 0.0f;
	Tween.Tweener m_Tweener = new Tween.Tweener();
	GUIBase_Widget m_Background;
	float m_BackgroundOriginXNormalized;
	GUIBase_Button[] m_OrderedButtons;

	// GUIOVERLAY INTERFACE

	protected override void OnViewInit()
	{
		// anchor news bar to the bottom of the screen
		Transform trans = Layout.transform;
		Vector3 position = trans.position;
		position.y = Screen.height - Layout.PlatformSize.y*Layout.LayoutScale.y;
		trans.position = position;

		//
		base.OnViewInit();

		Layout.FadeAlpha = m_LayoutAlpha;

		m_Background = Layout.GetWidget("Frame_Sprite");
		m_BackgroundOriginXNormalized = m_Background.transform.position.x/Screen.width;

		List<GUIBase_Button> buttons = new List<GUIBase_Button>();
		foreach (var info in m_Buttons)
		{
			if (info.Button == null)
				continue;
			if (info.Button.Widget == null)
				continue;
			if (info.Button.Widget.Parent != m_Background)
				continue;

			buttons.Add(info.Button);
		}

		buttons.Sort((x, y) => { return x.transform.localPosition.x.CompareTo(y.transform.localPosition.x); });
		m_OrderedButtons = buttons.ToArray();
	}

	protected override void OnViewUpdate()
	{
		base.OnViewUpdate();

		float targetAlpha = m_AnyScreenVisible == false ? 1.0f : 0.0f;
		if (m_LayoutAlpha != targetAlpha)
		{
			m_Tweener.TweenTo(this,
							  "m_LayoutAlpha",
							  targetAlpha,
							  0.05f,
							  Tween.Easing.Sine.EaseInOut,
							  (tween, finished) =>
							  {
								  if (Layout.Visible == false && m_LayoutAlpha > 0.0f)
								  {
									  Layout.Show(true);
								  }
								  else if (Layout.Visible == true && m_LayoutAlpha <= 0.0f)
								  {
									  Layout.Show(false);
								  }

								  Layout.SetFadeAlpha(m_LayoutAlpha, false);
								  Layout.SetModify(true, false);
							  });
		}

		if (m_Tweener.IsTweening == true)
		{
			m_Tweener.UpdateTweens();
		}

		UpdateLayout();

		Layout.InputEnabled = Layout.FadeAlpha < 1.0f ? false : true;
	}

	protected override void OnActiveScreen(string screenName)
	{
		m_AnyScreenVisible = string.IsNullOrEmpty(screenName) || screenName == "Lobby" ? false : true;
	}

	protected override bool ShouldDisplayButton(GUIBase_Button button)
	{
		switch (button.name)
		{
		case "Rewards_Button":
			return ShouldDisplayDailyRewardsButton();
		default:
			return true;
		}
	}

	protected override bool ShouldHighlightButton(GUIBase_Button button)
	{
		switch (button.name)
		{
		case "Rewards_Button":
			return ShouldDisplayDailyRewardsButton();
		default:
			return base.ShouldHighlightButton(button);
		}
	}

	// PRIVATE METHODS

	void UpdateLayout()
	{
		if (m_LayoutAlpha <= 0.0f)
			return;

		List<GUIBase_Button> buttons = new List<GUIBase_Button>();
		float width = 0.0f;
		foreach (var button in m_OrderedButtons)
		{
			if (button.Widget.Visible == false)
				continue;

			buttons.Add(button);

			if (width < button.Widget.GetWidth())
			{
				width = button.Widget.GetWidth();
			}
		}

		int half = Mathf.RoundToInt(width*1.2f*0.5f);
		int offset = Mathf.RoundToInt(-half*buttons.Count*0.5f + half*0.5f);
		foreach (var button in buttons)
		{
			Transform trans = button.transform;
			Vector3 pos = trans.localPosition;

			if (Mathf.RoundToInt(pos.x) != offset)
			{
				button.Widget.SetModify(true);
			}

			pos.x = offset;
			trans.localPosition = pos;

			offset += half;
		}

		Transform backgroundTrans = m_Background.transform;
		Vector3 backgroundPos = backgroundTrans.position;
		backgroundPos.x = ShouldDisplayDailyRewardsButton() ? m_BackgroundOriginXNormalized*Screen.width : Screen.width*0.5f;
		if (Mathf.RoundToInt(backgroundPos.x) != Mathf.RoundToInt(backgroundTrans.position.x))
		{
			backgroundTrans.position = backgroundPos;
			m_Background.SetModify(true);
		}

		m_Background.SetSpriteSize(0, half*buttons.Count + half*0.25f, m_Background.GetHeight());
	}

	bool ShouldDisplayDailyRewardsButton()
	{
		var ppi = PPIManager.Instance.GetLocalPPI();
		if (ppi == null)
			return false;
		return ppi.DailyRewards.GetCurrentConditionalDay().Received ? false : true;
	}
}
