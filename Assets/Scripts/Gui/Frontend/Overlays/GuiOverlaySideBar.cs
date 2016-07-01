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

public interface IGuiOverlayScreen
{
	string OverlayButtonTooltip { get; }
	bool HideOverlayButton { get; }
	bool HighlightOverlayButton { get; }
}

public abstract class GuiOverlaySideBar : GuiOverlay
{
	readonly static float NEXT_UPDATE_TIME = 0.1f;

	public enum E_ShowAnimation
	{
		None,
		Fade,
		SlideLeft,
		SlideRight,
		SlideUp,
		SlideDown
	}

	public enum E_ButtonAction
	{
		None,
		ShowScreen,
		Resume,
		Spawn,
		Spectate,
		Logout,
		Exit,
		DoCommand
	}

	[System.Serializable]
	public class ButtonInfo
	{
		public GUIBase_Button Button;
		[LocalizedTextId] public int TextID = 0;
		public E_ButtonAction Action = GuiOverlaySideBar.E_ButtonAction.None;
		public string ActionArgument;

		[System.NonSerialized] public IGuiOverlayScreen Screen;
		[System.NonSerialized] public GUIBase_Label Tooltip;
	}

	// PRIVATE MEMBERS

	[SerializeField] float m_AnimationTime = 0.75f;
	[SerializeField] E_ShowAnimation m_ShowAnimation = E_ShowAnimation.None;
	// DO NOT access this member from outside, please
	// it's public just because Unity inspector bug
	/*[SerializeField]*/
	public ButtonInfo[] m_Buttons = new ButtonInfo[0];

	Rect m_LayoutRect = new Rect();
	Tween.Tweener m_Tweener = null;
	protected float m_TweenValue = 0.0f;
	bool m_ShouldDisableButtons;
	string m_ActiveScreen = null;
	bool m_InitButtons = true;
	float m_NextUpdateTime = 0.0f;

	//HACK: disable some buttons for beta
	List<string> HACK_disabledForBeta = new List<string>() {"Shop_Button", "LeaderBoards_Button", "Gold", "Money"};

	// GETTERS/SETTERS

	public Tween.Tweener Tweener
	{
		get
		{
			if (m_Tweener == null)
			{
				m_Tweener = new Tween.Tweener();
			}
			return m_Tweener;
		}
	}

	// ABSTRACT INTERFACE

	protected virtual void OnButtonPressed(GUIBase_Button button)
	{
	}

	protected virtual void OnButtonInitialized(ButtonInfo buttonInfo)
	{
	}

	protected virtual bool ShouldDisplayButton(GUIBase_Button button)
	{
		return button.Widget.m_VisibleOnLayoutShow;
	}

	protected virtual bool ShouldDisableButton(GUIBase_Button button)
	{
		return false;
	}

	protected virtual bool ShouldHighlightButton(GUIBase_Button button)
	{
		return false;
	}

	// PUBLIC METHODS

	/*protected void SetButtonDisabled(string name, bool state)
	{
		foreach (var info in m_Buttons)
		{
			if (info.Action == E_ButtonAction.None)
				continue;
			if (info.Button == null)
				continue;
			if (info.Button.name != name)
				continue;

			info.Button.SetDisabled(state);
		}
	}*/

	// GUIOVERLAY INTERFACE

	protected override void OnViewInit()
	{
		base.OnViewInit();

		Transform trans = Layout.transform;
		m_LayoutRect = Layout.GetBBox();
		m_LayoutRect.x = trans.position.x;
		m_LayoutRect.y = trans.position.y;
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		InitButtons();

		m_ShouldDisableButtons = ShouldDisableButtons();

		BindButtons();
		RefreshButtonsState();

		PrepareShowAnimation();
		UpdateTweener();
	}

	protected override void OnViewHide()
	{
		DestroyTweener();

		UnbindButtons();

		base.OnViewHide();
	}

	protected override void OnViewUpdate()
	{
		base.OnViewUpdate();

		m_NextUpdateTime -= Time.deltaTime;
		bool shouldDisable = ShouldDisableButtons();
		if (m_NextUpdateTime <= 0.0f || m_ShouldDisableButtons != shouldDisable)
		{
			m_NextUpdateTime = NEXT_UPDATE_TIME;
			m_ShouldDisableButtons = shouldDisable;

			RefreshButtonsState();
		}

		UpdateTweener();
	}

	protected override void OnActiveScreen(string screenName)
	{
		m_ActiveScreen = screenName;

		foreach (var info in m_Buttons)
		{
			if (info.Button == null)
				continue;
			if (info.Action != E_ButtonAction.ShowScreen)
				continue;

			bool pressed = info.ActionArgument == screenName;
			info.Button.stayDown = pressed;
			info.Button.ForceDownStatus(pressed);
		}
	}

	// PRIVATE METHODS

	IGuiOverlayScreen FindScreen(string name)
	{
		Component menu = Owner as Component;
		if (menu == null)
			return null;

		int idx = name.IndexOf(":");
		if (idx > 0)
		{
			name = name.Substring(0, idx);
		}

		string[] patterns =
		{
			"GuiScreen{0}",
			"{0}Screen",
			"Gui{0}Menu",
			"GuiPopup{0}"
		};
		foreach (var pattern in patterns)
		{
			string fullname = string.Format(pattern, name);

			GuiScreen[] screens = menu.GetComponentsInChildren<GuiScreen>();
			foreach (var screen in screens)
			{
				if (screen.GetType().Name == fullname)
					return screen as IGuiOverlayScreen;
			}
		}

		return null;
	}

	void InitButtons()
	{
		if (m_InitButtons == false)
			return;

		Component menu = Owner as Component;
		if (menu == null)
			return;

		for (int idx = 0; idx < m_Buttons.Length; ++idx)
		{
			ButtonInfo info = m_Buttons[idx];
			if (info.Button == null)
				continue;
			if (info.Action != E_ButtonAction.ShowScreen)
				continue;

			info.Screen = FindScreen(info.ActionArgument);
			info.Tooltip = GuiBaseUtils.GetChildLabel(info.Button.Widget, "Tooltip_Label", false);

			OnButtonInitialized(info);

			m_Buttons[idx] = info;
		}

		m_InitButtons = false;
	}

	void PrepareShowAnimation()
	{
		if (m_ShowAnimation == E_ShowAnimation.None)
			return;

		float tweenTo = 0.0f;
		if (m_ShowAnimation == E_ShowAnimation.Fade)
		{
			m_TweenValue = 0.0f;
			tweenTo = 1.0f;
		}
		else
		{
			Transform trans = Layout.transform;
			Vector3 position = trans.position;
			switch (m_ShowAnimation)
			{
			case E_ShowAnimation.SlideLeft:
				m_TweenValue = m_LayoutRect.x + m_LayoutRect.width;
				tweenTo = m_LayoutRect.x;
				break;
			case E_ShowAnimation.SlideRight:
				m_TweenValue = m_LayoutRect.x - m_LayoutRect.width;
				tweenTo = m_LayoutRect.x;
				break;
			case E_ShowAnimation.SlideUp:
				m_TweenValue = m_LayoutRect.y + m_LayoutRect.height;
				tweenTo = m_LayoutRect.y;
				break;
			case E_ShowAnimation.SlideDown:
				m_TweenValue = m_LayoutRect.y - m_LayoutRect.height;
				tweenTo = m_LayoutRect.y;
				break;
			}
			trans.position = position;
		}

		Tweener.TweenTo(this,
						"m_TweenValue",
						tweenTo,
						m_AnimationTime,
						Tween.Easing.Sine.EaseOut,
						(tween, finished) =>
						{
							if (m_ShowAnimation == E_ShowAnimation.Fade)
							{
								Layout.FadeAlpha = m_TweenValue;
							}
							else
							{
								Transform trans = Layout.transform;
								Vector3 position = trans.position;
								switch (m_ShowAnimation)
								{
								case E_ShowAnimation.SlideLeft:
									position.x = m_TweenValue;
									break;
								case E_ShowAnimation.SlideRight:
									position.x = m_TweenValue;
									break;
								case E_ShowAnimation.SlideUp:
									position.y = m_TweenValue;
									break;
								case E_ShowAnimation.SlideDown:
									position.y = m_TweenValue;
									break;
								}
								trans.position = position;
							}
						});
	}

	void DestroyTweener()
	{
		Tweener.StopTweens(true);

		Layout.SetModify(true, false);
	}

	void UpdateTweener()
	{
		if (Tweener.IsTweening == false)
			return;

		Tweener.UpdateTweens();

		Layout.SetModify(true, false);
	}

	void BindButtons()
	{
		foreach (var info in m_Buttons)
		{
			if (info.Button == null)
				continue;

			info.Button.RegisterTouchDelegate2(OnButtonPressed);

			if (info.TextID != 0)
			{
				info.Button.SetNewText(info.TextID);
			}
		}
	}

	void UnbindButtons()
	{
		foreach (var info in m_Buttons)
		{
			if (info.Button == null)
				continue;

			info.Button.RegisterTouchDelegate2(null);
		}
	}

	void RefreshButtonsState()
	{
		foreach (var info in m_Buttons)
		{
			if (info.Button == null)
				continue;

			bool visible = IsVisible == true ? ShouldDisplayButton(info.Button) : false;
			bool highlighted = ShouldHighlightButton(info.Button);
			if (info.Screen != null && IsVisible == true)
			{
				visible = info.Screen.HideOverlayButton ? false : IsVisible;
				highlighted ^= info.Screen.HighlightOverlayButton;
			}

			if (info.Button.Widget.Visible != visible)
			{
				info.Button.Widget.ShowImmediate(visible, true);
			}

			// update disabled state
			bool disabled = ShouldDisableButton(info.Button);
			if (BuildInfo.Version.Stage != BuildInfo.Stage.Beta)
			{
				disabled ^= m_ShouldDisableButtons;
			}
			else
			{
				disabled ^= m_ShouldDisableButtons || HACK_disabledForBeta.Contains(info.Button.name);
			}
			info.Button.SetDisabled(info.Action == E_ButtonAction.None ? true : disabled || !visible);

			// update highlight state
			info.Button.isHighlighted = info.Button.stayDown == false && info.Button.IsDisabled == false ? highlighted : false;

			// update tooltip
			if (info.Tooltip != null && info.Screen != null)
			{
				string tooltip = info.Screen.OverlayButtonTooltip ?? string.Empty;
				bool showTooltip = IsVisible == true && visible == true && info.Screen != null && !string.IsNullOrEmpty(tooltip);
				info.Tooltip.Widget.ShowImmediate(showTooltip, true);
				if (showTooltip == true)
				{
					info.Tooltip.SetNewText(tooltip);
				}
			}
		}
	}

	bool ShouldDisableButtons()
	{
		if (Layout == null || Layout.Visible == false)
			return true;
		return Game.Instance != null ? Game.Instance.IsLoading : true;
	}

	void OnButtonPressed(GUIBase_Widget widget)
	{
		if (widget == null)
			return;

		GUIBase_Button button = widget.GetComponent<GUIBase_Button>();
		if (button == null)
			return;

		ButtonInfo buttonInfo = new ButtonInfo();
		foreach (var info in m_Buttons)
		{
			if (info.Button == null)
				continue;

			// store button info if matches
			if (info.Button == button)
			{
				buttonInfo = info;
			}

			// reset down status for current button
			info.Button.ForceDownStatus(false);
		}

		// do some action now
		ProcessButtonAction(buttonInfo);

		// let sub-classes do something clever
		OnButtonPressed(button);
	}

	void ProcessButtonAction(ButtonInfo info)
	{
		if (info.Button.stayDown == false)
		{
			switch (info.Action)
			{
			case E_ButtonAction.ShowScreen:
				OnShowScreen(info.ActionArgument);
				break;
			case E_ButtonAction.Resume:
				OnResume();
				break;
			case E_ButtonAction.Spawn:
				OnSpawn();
				break;
			case E_ButtonAction.Spectate:
				OnSpectate();
				break;
			case E_ButtonAction.Logout:
				OnLogout();
				break;
			case E_ButtonAction.Exit:
				OnExit();
				break;
			case E_ButtonAction.DoCommand:
				OnDoCommand(info.ActionArgument);
				break;
			}
		}
		else
		{
			OnBack();
		}
	}

	void OnBack()
	{
		Owner.Back();
	}

	void OnShowScreen(string argument)
	{
		if (string.IsNullOrEmpty(argument) == true)
		{
			Debug.LogError("Specify any screen name for button action 'ShowScreen', please.");
			return;
		}

		int idx = argument.IndexOf(':');
		if (idx < 0 || argument.Substring(0, idx) != m_ActiveScreen)
		{
			Owner.Back();
		}
		Owner.ShowScreen(argument, false);
	}

	void OnResume()
	{
		Owner.DoCommand("ResumeGame");
	}

	void OnSpawn()
	{
		Owner.DoCommand("SpawnPlayer");
	}

	void OnSpectate()
	{
		Owner.DoCommand("Spectate");
	}

	void OnLogout()
	{
		Owner.DoCommand("LogoutUser");
	}

	void OnExit()
	{
		Owner.Exit();
	}

	void OnDoCommand(string argument)
	{
		if (string.IsNullOrEmpty(argument) == true)
		{
			Debug.LogError("Specify any command for button action 'DoCommand', please.");
			return;
		}

		Owner.DoCommand(argument);
	}
}
