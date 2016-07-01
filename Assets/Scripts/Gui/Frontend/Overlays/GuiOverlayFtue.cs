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
using LitJson;

[AddComponentMenu("GUI/Frontend/Overlays/GuiOverlayFtue")]
public class GuiOverlayFtue : GuiOverlay
{
	readonly static string CAPTION_LABEL = "Caption_Label";
	readonly static string TEXT_LABEL = "Text_Label";
	readonly static string SHOWINFO_BUTTON = "ShowInfo_Button";
	readonly static string SKIPTHIS_BUTTON = "SkipThis_Button";
	readonly static string SKIPALL_BUTTON = "SkipAll_Button";
	readonly static string CLOSE_BUTTON = "Close_Button";

	public delegate void ButtonPressedHandler(string buttonName);

	class HelperBase
	{
		protected GUIBase_Widget m_Root { get; private set; }
		protected float m_FadeInTime { get; private set; }
		protected float m_FadeOutTime { get; private set; }

		public float Alpha = 0;
		public ButtonPressedHandler ButtonPressed;

		public virtual void Initialize(GUIBase_Layout layout, string rootName, float fadeInTime, float fadeOutTime)
		{
			m_Root = GuiBaseUtils.FindLayoutWidget<GUIBase_Widget>(layout, rootName);
			m_Root.FadeAlpha = Alpha;

			m_FadeInTime = fadeInTime;
			m_FadeOutTime = fadeOutTime;
		}

		public virtual void Show(Tween.Tweener tweener)
		{
			if (Alpha < 1.0f)
			{
				tweener.TweenTo(this, "Alpha", 1.0f, m_FadeInTime);
			}
		}

		public virtual void Hide(Tween.Tweener tweener)
		{
			if (Alpha > 0.0f)
			{
				tweener.TweenTo(this, "Alpha", 0.0f, m_FadeOutTime);
			}
		}

		public void UpdateVisibility()
		{
			if (m_Root.FadeAlpha == Alpha)
				return;

			m_Root.SetFadeAlpha(Alpha, true);

			if (Alpha > 0.0f)
			{
				if (m_Root.Visible == false)
				{
					m_Root.Show(true, true);
				}
			}
			else
			{
				if (m_Root.Visible == true)
				{
					m_Root.Show(false, true);
				}
			}
		}

		protected void OnButtonPressed(GUIBase_Widget widget)
		{
			if (ButtonPressed != null)
			{
				ButtonPressed(widget.name);
			}
		}
	}

	class ControlsHelper : HelperBase
	{
		GUIBase_Label m_Caption;
		GUIBase_Label m_Text;
		GUIBase_Button m_ShowInfo;

		public override void Initialize(GUIBase_Layout layout, string rootName, float fadeInTime, float fadeOutTime)
		{
			base.Initialize(layout, rootName, fadeInTime, fadeOutTime);

			m_Caption = GuiBaseUtils.GetChild<GUIBase_Label>(m_Root, CAPTION_LABEL);
			m_Text = GuiBaseUtils.GetChild<GUIBase_Label>(m_Root, TEXT_LABEL);
			m_ShowInfo = GuiBaseUtils.GetChild<GUIBase_Button>(m_Root, SHOWINFO_BUTTON);

			m_ShowInfo.RegisterTouchDelegate2(OnButtonPressed);
		}

		public void Update(FtueAction.Base action, int actionsFinished, int actionsTotal)
		{
			if (m_Root.Visible == true)
			{
				m_Caption.SetNewText(string.Format("{0}: {1}/{2}",
												   TextDatabase.instance[9900000],
												   Mathf.Min(actionsFinished + 1, actionsTotal),
												   actionsTotal
													 ));
				string label = action.IsIdle == true ? string.Format(TextDatabase.instance[9900003], action.Label) : action.Label;
				m_Text.SetNewText(label);
			}

			m_ShowInfo.IsDisabled = Alpha < 0.8f ? true : false; //action.IsIdle;
		}
	}

	class InfoBoxHelper : HelperBase
	{
		GUIBase_Label m_Caption;
		GUIBase_TextArea m_Text;
		GUIBase_Button m_Close;
		GUIBase_Button m_SkipThis;
		GUIBase_Button m_SkipAll;
		Vector2 m_DefaultTextScale;

		public override void Initialize(GUIBase_Layout layout, string rootName, float fadeInTime, float fadeOutTime)
		{
			base.Initialize(layout, rootName, fadeInTime, fadeOutTime);

			m_Caption = GuiBaseUtils.GetChild<GUIBase_Label>(m_Root, CAPTION_LABEL);
			m_Text = GuiBaseUtils.GetChild<GUIBase_TextArea>(m_Root, TEXT_LABEL);
			m_Close = GuiBaseUtils.GetChild<GUIBase_Button>(m_Root, CLOSE_BUTTON);
			m_SkipThis = GuiBaseUtils.GetChild<GUIBase_Button>(m_Root, SKIPTHIS_BUTTON);
			m_SkipAll = GuiBaseUtils.GetChild<GUIBase_Button>(m_Root, SKIPALL_BUTTON);

			m_Close.RegisterTouchDelegate2(OnButtonPressed);
			m_SkipThis.RegisterTouchDelegate2(OnButtonPressed);
			m_SkipAll.RegisterTouchDelegate2(OnButtonPressed);

			m_DefaultTextScale = m_Text.textScale;
		}

		public override void Show(Tween.Tweener tweener)
		{
			if (Player.LocalInstance != null && Player.LocalInstance.Controls != null)
				Player.LocalInstance.Controls.EnableLockCursor(false);
			base.Show(tweener);
		}

		public override void Hide(Tween.Tweener tweener)
		{
			if (Player.LocalInstance != null && Player.LocalInstance.Controls != null)
				Player.LocalInstance.Controls.EnableLockCursor(true);
			base.Hide(tweener);

			m_SkipThis.animate = false;
			m_SkipThis.isHighlighted = false;
		}

		public void Update(FtueAction.Base action, int actionsFinished, int actionsTotal)
		{
			if (m_Root.Visible == true)
			{
				string hint = Ftue.NextActionHint;
				string label = action.IsIdle == true ? string.Format(TextDatabase.instance[9900003], action.Label) : action.Label;
				m_Caption.SetNewText(hint != null
													 ? label
													 : string.Format("{0}: {1}",
																	 TextDatabase.instance[9900000],
																	 label
																	   ));
				m_Text.Widget.ShowImmediate(true, true);
				m_Text.SetNewText(hint ?? action.Description);
				m_Text.textScale = string.IsNullOrEmpty(hint)
												   ? new Vector2(m_DefaultTextScale.x*action.DescriptionScale.x, m_DefaultTextScale.y*action.DescriptionScale.y)
												   : m_DefaultTextScale;

				bool isNext = action is FtueAction.Welcome || action is FtueAction.Hud || action is FtueAction.FinalText ? true : false;
				bool highlight = Alpha < 1.0f ? false : action.IsActive;
				m_SkipThis.animate = true;
				m_SkipThis.isHighlighted = isNext ? highlight : false;
				m_SkipThis.SetNewText(isNext ? 9900004 : 9900001);
			}

			m_Close.IsDisabled = Alpha < 0.8f ? true : false;
			m_SkipThis.IsDisabled = Alpha < 0.8f ? true : action.IsIdle;
			m_SkipAll.IsDisabled = Alpha < 0.8f ? true : false;
		}
	}

	// PRIVATE MEMBERS

	ControlsHelper m_Controls = new ControlsHelper();
	InfoBoxHelper m_InfoBox = new InfoBoxHelper();
	List<string> m_DisplayedInfoBoxes = new List<string>();
	bool m_IgnoreDisplayedState = true;
	bool m_AnyScreenVisible = false;
	Tween.Tweener m_Tweener = new Tween.Tweener();

	// GUIOVERLAY INTERFACE

	protected override void OnViewInit()
	{
		m_Controls.Initialize(Layout, "Controls", 0.05f, 0.10f);
		m_InfoBox.Initialize(Layout, "InfoBox", 0.10f, 0.10f);

		m_Controls.ButtonPressed += OnButtonPressed;
		m_InfoBox.ButtonPressed += OnButtonPressed;

		CloudUser.authenticationChanged += OnUserAuthenticationChanged;
		UserSettings.SettingsLoaded += Load;
		UserSettings.SettingsSaving += Save;
	}

	protected override void OnViewDestroy()
	{
		CloudUser.authenticationChanged -= OnUserAuthenticationChanged;
		UserSettings.SettingsLoaded -= Load;
		UserSettings.SettingsSaving -= Save;

		m_Controls.ButtonPressed -= OnButtonPressed;
		m_InfoBox.ButtonPressed -= OnButtonPressed;

		Save(Game.Settings);
	}

	protected override void OnViewHide()
	{
		m_Controls.Hide(m_Tweener);
		m_InfoBox.Hide(m_Tweener);

		base.OnViewHide();
	}

	protected override void OnViewUpdate()
	{
		FtueAction.Base action = Ftue.ActiveAction ?? Ftue.PendingAction;
		int actionsFinished = Ftue.ActionsSkipped + Ftue.ActionsFinished;
		int actionsTotal = Ftue.ActionsTotal;

		if (action != null && m_AnyScreenVisible == false && UserGuide.ActiveAction is FtueAction.Base)
		{
			m_Controls.Show(m_Tweener);
			m_Controls.Update(action, actionsFinished, actionsTotal);

			if (action.IsActive == true && action.IsExecuted == true)
			{
				string key = action.UniqueId;
				bool hasKey = m_DisplayedInfoBoxes.Contains(key);
				if (hasKey == false || m_IgnoreDisplayedState == true)
				{
					if (hasKey == false)
					{
						m_DisplayedInfoBoxes.Add(key);
					}

					m_InfoBox.Show(m_Tweener);

					m_IgnoreDisplayedState = false;
				}
			}
			else
			{
				//m_InfoBox.Hide(m_Tweener);
			}

			m_InfoBox.Update(action, actionsFinished, actionsTotal);
		}
		else
		{
			m_Controls.Hide(m_Tweener);
			m_InfoBox.Hide(m_Tweener);
		}

		UpdateTweener();
	}

	protected override void OnActiveScreen(string screenName)
	{
		bool anyScreenVisible = string.IsNullOrEmpty(screenName) == false ? true : false;
		if (anyScreenVisible == true)
		{
			m_InfoBox.Hide(m_Tweener);
		}

		m_AnyScreenVisible = anyScreenVisible && GuiFrontendMain.IsVisible;
	}

	// HANDLERS

	void OnUserAuthenticationChanged(bool state)
	{
		if (state == true)
		{
			Load(Game.Settings);

			m_IgnoreDisplayedState = true;
		}
		else
		{
			Save(Game.Settings);
		}
	}

	void OnButtonPressed(string buttonName)
	{
		if (buttonName == SHOWINFO_BUTTON)
		{
			if (m_InfoBox.Alpha < 1)
			{
				m_InfoBox.Show(m_Tweener);
			}
			else
			{
				m_InfoBox.Hide(m_Tweener);
			}
		}
		else if (buttonName == CLOSE_BUTTON)
		{
			m_InfoBox.Hide(m_Tweener);
		}
		else if (buttonName == SKIPTHIS_BUTTON)
		{
			if (Ftue.ActiveAction != null)
			{
				Ftue.ActiveAction.Skip();
			}
		}
		else if (buttonName == SKIPALL_BUTTON)
		{
			Ftue.SkipAll();
		}
	}

	// PRIVATE METHODS

	void Load(UserSettings settings)
	{
		string json = settings.GetString("ftue.overlay", "");
		JsonData data = JsonMapper.ToObject(json);

		m_DisplayedInfoBoxes.Clear();
		if (data.IsArray == false)
			return;

		for (int idx = 0; idx < data.Count; ++idx)
		{
			m_DisplayedInfoBoxes.Add((string)data[idx]);
		}
	}

	void Save(UserSettings settings)
	{
		string json = JsonMapper.ToJson(m_DisplayedInfoBoxes.ToArray());
		settings.SetString("ftue.overlay", json);
	}

	void UpdateTweener()
	{
		if (m_Tweener.IsTweening == false)
			return;

		m_Tweener.UpdateTweens();

		m_Controls.UpdateVisibility();
		m_InfoBox.UpdateVisibility();

		Layout.SetModify(true, false);
	}
}
