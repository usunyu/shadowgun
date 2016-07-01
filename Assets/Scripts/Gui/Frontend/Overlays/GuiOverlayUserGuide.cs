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

public enum E_Hint
{
	PremiumBuy,
	PremiumRenew,
	Player,
	Xp,
	Gold,
	Money,
	Chips,
	Max
}

[AddComponentMenu("GUI/Frontend/Overlays/GuiOverlayNewsBar")]
public class GuiOverlayUserGuide : GuiOverlay
{
	[System.Serializable]
	public class HintInfo
	{
		public E_Hint BindHintId = E_Hint.Max;
		public GUIBase_Button HintButton;
		public GUIBase_Button LinkedButton;

		[HideInInspector] public Vector2 Offset = new Vector2(0.0f, 0.0f);
	}

	[System.Serializable]
	public class ScreenInfo
	{
		public GUIBase_Layout HintLayout;
		public string LinkedScreen;
	}

	// PRIVATE MEMBERS

	[SerializeField] float m_InitialHintDelay = 5.0f;
	[SerializeField] float m_NextHintDelay = 5*60.0f;
	[SerializeField] float m_ExternalHintDelay = 0.3f;
	[SerializeField] AudioClip m_HintSound = null;
	[SerializeField] HintInfo[] m_Hints;
	[SerializeField] float m_FirstHelpDelay = 0.25f;
	[SerializeField] float m_FadeInHelpTime = 0.1f;
	[SerializeField] float m_NextHelpDelay = 0.15f;
	[SerializeField] AudioClip m_ShowHelpSound = null;
	[SerializeField] AudioClip m_HideHelpSound = null;
	[SerializeField] ScreenInfo[] m_Screens;

	string m_PrimaryKey = "default";
	Tween.Tweener m_Tweener = null;
	AudioSource m_AudioSource = null;
	E_Hint m_CurrentHint = E_Hint.Max;
	E_Hint m_PreviousHint = E_Hint.Max;
	float m_HintTweenY = 0.0f;
	string m_LastVisibleScreen = null;
	bool m_CanShowHints = true;
	List<string> m_VisitedScreens = new List<string>();

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

	// GUIOVERLAY INTERFACE

	protected override void OnViewInit()
	{
		base.OnViewInit();

		CloudUser.authenticationChanged += OnAuthenticationChanged;
		UserSettings.SettingsLoaded += OnSettingsLoaded;
		UserSettings.SettingsSaving += OnSettingsSaving;
	}

	protected override void OnViewDestroy()
	{
		UserSettings.SettingsLoaded -= OnSettingsLoaded;
		UserSettings.SettingsSaving -= OnSettingsSaving;
		CloudUser.authenticationChanged -= OnAuthenticationChanged;
		OnAuthenticationChanged(false);

		base.OnViewDestroy();
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		BindControls();

		//NOTE: for testing purposes only
		//m_InitialHintDelay = 1.0f;
		//m_NextHintDelay = 2.0f;

		if (GuiOptions.showHints == true)
		{
			SetHintTimer(m_InitialHintDelay);
		}

		// hide hint layouts
		foreach (var info in m_Screens)
		{
			if (info.HintLayout != null)
			{
				info.HintLayout.Show(false);
			}
		}

		if (GuiOptions.showHints == true)
		{
			UpdateTweener();
		}
	}

	protected override void OnViewHide()
	{
		DestroyTweener();

		StopHintTimer();

		m_CurrentHint = E_Hint.Max;
		m_PreviousHint = E_Hint.Max;

		UnbindControls();

		base.OnViewHide();
	}

	protected override void OnViewUpdate()
	{
		base.OnViewUpdate();

		if (GuiOptions.showHints == true)
		{
			ListenInput();

			if (UserGuide.IsActive == true && UserGuide.InitialSequenceFinished == true)
			{
				if (GuiBaseUtils.PendingHint != E_Hint.Max)
				{
					//SetHintVisibility(m_CurrentHint, false);
					ForceShowHint(GuiBaseUtils.PendingHint, m_ExternalHintDelay);
					GuiBaseUtils.PendingHint = E_Hint.Max;
				}
			}

			UpdateTweener();
		}
	}

	protected override void OnActiveScreen(string screenName)
	{
		screenName = screenName ?? "default";

		SetHintVisibility(m_CurrentHint, false);
		SetHelpLayoutVisibility(m_LastVisibleScreen, false);

		if (Ftue.IsActive == true)
		{
			SetHelpLayoutVisibility(screenName, true);
		}

		m_CanShowHints = screenName == "SlotMachine" ? false : true;
	}

	// HANDLERS

	void OnAuthenticationChanged(bool state)
	{
		if (state == true)
		{
			m_PrimaryKey = CloudUser.instance.primaryKey;

			CloudUser.premiumAcctChanged += OnUserPremiumAcctChanged;

			OnSettingsLoaded(Game.Settings);
		}
		else
		{
			CloudUser.premiumAcctChanged -= OnUserPremiumAcctChanged;

			OnSettingsSaving(Game.Settings);
		}
	}

	void OnSettingsLoaded(UserSettings settings)
	{
		string json = settings.GetString("ftue.screens", "[]");
		m_VisitedScreens = JsonMapper.ToObject<List<string>>(json) ?? new List<string>();
	}

	void OnSettingsSaving(UserSettings settings)
	{
		string json = JsonMapper.ToJson(m_VisitedScreens);
		settings.SetString("ftue.screens", json);
	}

	void OnUserPremiumAcctChanged(bool state)
	{
		if (state == false)
		{
			ForceShowHint(E_Hint.PremiumBuy, m_CurrentHint == E_Hint.PremiumRenew ? 0.0f : m_InitialHintDelay);
		}
		else if (m_CurrentHint == E_Hint.PremiumBuy)
		{
			SetHintVisibility(m_CurrentHint, false);
		}
	}

	// PRIVATE METHODS

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

	void BindControls()
	{
		for (int idx = 0; idx < m_Hints.Length; ++idx)
		{
			if (m_Hints[idx].HintButton == null)
				continue;
			if (m_Hints[idx].LinkedButton == null)
				continue;

			// store hint id for later use in lambdas
			E_Hint hintId = m_Hints[idx].BindHintId;

			// register delegate to hint button
			GuiBaseUtils.RegisterButtonDelegate(m_Hints[idx].HintButton,
												null,
												(inside) =>
												{
													if (inside == true)
													{
														SetHintVisibility(hintId, false);
													}
												});

			// register delegate to linked button
			GuiBaseUtils.RegisterButtonDelegate3(m_Hints[idx].LinkedButton, (widget, evt) => { SetHintVisibility(m_CurrentHint, false); }, null, null);

			// setup offset
			Vector2 offset = m_Hints[idx].Offset;
			offset = m_Hints[idx].LinkedButton.transform.position;
			offset.x = m_Hints[idx].LinkedButton.Widget.GetOrigPos().x - offset.x;
			offset.y = m_Hints[idx].LinkedButton.Widget.GetOrigPos().y - offset.y;
			m_Hints[idx].Offset = offset;

			// hide hint
			SetHintVisibility(hintId, false);
		}
	}

	void UnbindControls()
	{
		for (int idx = 0; idx < m_Hints.Length; ++idx)
		{
			if (m_Hints[idx].HintButton == null)
				continue;

			SetHintVisibility((E_Hint)idx, false);

			GuiBaseUtils.RegisterButtonDelegate(m_Hints[idx].HintButton, null, null);

			if (m_Hints[idx].LinkedButton != null)
			{
				GuiBaseUtils.RegisterButtonDelegate3(m_Hints[idx].LinkedButton, null, null, null);
			}
		}
	}

	void StopHintTimer()
	{
		CancelInvoke("ShowHint");
	}

	void SetHintTimer(float delay)
	{
		if (BuildInfo.Version.Stage == BuildInfo.Stage.Beta)
			return;

		StopHintTimer();

		if (delay >= 0.0f)
		{
			Invoke("ShowHint", delay);
		}
	}

	void ShowHint()
	{
		if (UserGuide.InitialSequenceFinished == false)
		{
			// wait until all initial dialogs hide
			SetHintTimer(m_InitialHintDelay);
			return;
		}

		if (UserGuideAction_ResetResearch.NotifyUser == true)
		{
			SetHintTimer(m_InitialHintDelay);
			return;
		}

		if (m_CanShowHints == false || string.IsNullOrEmpty(m_LastVisibleScreen) == false)
		{
			// do not display hint while help is displayed
			SetHintTimer(m_PreviousHint == E_Hint.Max ? m_InitialHintDelay : m_NextHintDelay);
			return;
		}

		bool isMainMenu = GuiFrontendMain.IsVisible;

		// deduce next hint
		E_Hint hintId = m_CurrentHint;
		bool isRandom = false;
		if (hintId == E_Hint.Max)
		{
			if (isMainMenu == true && m_PreviousHint == E_Hint.Max && m_CurrentHint == E_Hint.Max)
			{
				// we want to display premium hint as the first hint ever
				hintId = E_Hint.PremiumBuy;
			}
			else
			{
				// randomly select one of hints
				isRandom = true;
				do
				{
					hintId = (E_Hint)Random.Range(0, (int)E_Hint.Max);
				} while (hintId == m_PreviousHint);
			}
		}

		switch (hintId)
		{
		case E_Hint.PremiumBuy:
			if (isMainMenu == true)
			{
				if (CloudUser.instance.isPremiumAccountActive == true)
				{
					System.TimeSpan time = CloudUser.instance.GetPremiumAccountEndDateTime() - CloudDateTime.UtcNow;
					hintId = time.TotalDays < 1 ? E_Hint.PremiumRenew : E_Hint.Max;
				}
			}
			else
			{
				hintId = E_Hint.Max;
			}
			break;
		case E_Hint.PremiumRenew:
			if (isMainMenu == true)
			{
				if (CloudUser.instance.isPremiumAccountActive == false)
				{
					hintId = E_Hint.PremiumBuy;
				}
				else
				{
					System.TimeSpan time = CloudUser.instance.GetPremiumAccountEndDateTime() - CloudDateTime.UtcNow;
					if (time.TotalDays > 1)
					{
						hintId = E_Hint.Max;
					}
				}
			}
			else
			{
				hintId = E_Hint.Max;
			}
			break;
		case E_Hint.Player:
			if (isMainMenu == false)
			{
				hintId = E_Hint.Max;
			}
			break;
		case E_Hint.Xp:
			//if (... some clever condition here ...)
		{
			hintId = E_Hint.Max;
		}
			break;
		case E_Hint.Gold:
			if (isRandom == true && ShopDataBridge.Instance.PlayerGold > 0)
			{
				hintId = E_Hint.Max;
			}
			break;
		case E_Hint.Money:
			if (isRandom == true && ShopDataBridge.Instance.PlayerMoney > 0)
			{
				hintId = E_Hint.Max;
			}
			break;
		case E_Hint.Chips:
			//if (... some clever condition here ...)
		{
			hintId = E_Hint.Max;
		}
			break;
		default:
			break;
		}

		// hide current hint if needed
		if (m_CurrentHint != hintId)
		{
			SetHintVisibility(m_CurrentHint, false);
		}

		// show new hint
		SetHintVisibility(hintId, true);

		// set new timer
		SetHintTimer(m_NextHintDelay);
	}

	void ForceShowHint(E_Hint hintId, float delay = -1.0f)
	{
		if (m_CurrentHint == hintId)
			return;

		SetHintVisibility(m_CurrentHint, false);

		m_CurrentHint = hintId;

		SetHintTimer(delay < 0.0f ? m_InitialHintDelay : delay);
	}

	void SetHintVisibility(E_Hint hintId, bool state)
	{
		if (state == true)
		{
			if (GuiOptions.showHints == false)
				return;
			if (BuildInfo.Version.Stage == BuildInfo.Stage.Beta)
				return;
		}

		HintInfo info = GetHintInfo(hintId);
		if (info == null)
			return;
		if (info.HintButton == null)
			return;

		info.HintButton.Widget.Show(state, true);

		if (state == true)
		{
			if (m_HintSound != null)
			{
				MFGuiManager.Instance.PlayOneShot(m_HintSound);
			}

			m_CurrentHint = hintId;

			GUIBase_Widget widget = info.HintButton.Widget;
			Transform trans = widget.transform;
			Vector3 origin = widget.GetOrigPos();

			origin.y -= info.Offset.y;

			// animate transparency
			Tweener.TweenFromTo(widget,
								"m_FadeAlpha",
								0.0f,
								1.0f,
								0.2f,
								Tween.Easing.Quad.EaseIn,
								(tween, finished) =>
								{
									GUIBase_Widget[] children = widget.GetComponentsInChildren<GUIBase_Widget>();
									foreach (var child in children)
									{
										child.FadeAlpha = widget.FadeAlpha;
									}
								});

			// animate position
			Tweener.TweenFromTo(this,
								"m_HintTweenY",
								origin.y + 25.0f,
								origin.y,
								0.15f,
								Tween.Easing.Sine.EaseInOut,
								(tween, finished) =>
								{
									trans.position = new Vector3(trans.position.x, m_HintTweenY, trans.position.z);

									if (finished == true)
									{
										Tweener.TweenFromTo(this,
															"m_HintTweenY",
															origin.y + 5.0f,
															origin.y,
															0.15f,
															Tween.Easing.Sine.EaseInOut,
															(tween1, finished1) => { trans.position = new Vector3(trans.position.x, m_HintTweenY, trans.position.z); });
									}
								});
		}
		else if (m_CurrentHint == hintId)
		{
			m_PreviousHint = m_CurrentHint;
			m_CurrentHint = E_Hint.Max;

			Tweener.StopTweens(true);
		}
	}

	void SetHelpLayoutVisibility(string screenName, bool state)
	{
		if (state == true)
		{
			if (GuiOptions.showHints == false)
				return;
			if (BuildInfo.Version.Stage == BuildInfo.Stage.Beta)
				return;
		}

		if (state == true && m_VisitedScreens.Contains(screenName) == true)
			return;

		ScreenInfo info = GetScreenInfo(screenName);
		if (info != null && info.HintLayout != null)
		{
			if (state == true)
			{
				StartCoroutine(ShowHelp_Coroutine(info.HintLayout));
			}
			else if (info.HintLayout.Visible == true && m_HideHelpSound != null)
			{
				MFGuiManager.Instance.PlayOneShot(m_HideHelpSound);
			}

			info.HintLayout.Show(state);

			m_LastVisibleScreen = state ? screenName : null;

			if (state == true)
			{
				m_VisitedScreens.Add(screenName);
			}
		}
		else
		{
			m_LastVisibleScreen = null;
		}
	}

	IEnumerator ShowHelp_Coroutine(GUIBase_Layout layout)
	{
		GUIBase_Button[] buttons = layout.GetComponentsInChildren<GUIBase_Button>();
		System.Array.Sort(buttons, (x, y) => { return string.Compare(x.name, y.name); });

		foreach (var button in buttons)
		{
			button.Widget.SetFadeAlpha(0.0f, true);
		}

		yield return new WaitForSeconds(m_FirstHelpDelay);

		foreach (var button in buttons)
		{
			if (button.Widget.Visible == false)
				continue;

			if (m_ShowHelpSound != null)
			{
				PlayOneShotWithFade(m_ShowHelpSound, 0.05f);
			}

			while (button.Widget.FadeAlpha < 1.0f)
			{
				button.Widget.SetFadeAlpha(button.Widget.FadeAlpha + Time.deltaTime/m_FadeInHelpTime, true);

				yield return new WaitForEndOfFrame();
			}

			yield return new WaitForSeconds(m_NextHelpDelay);
		}
	}

	void ListenInput()
	{
		if (string.IsNullOrEmpty(m_LastVisibleScreen) == true)
			return;

		bool touched = false;
		foreach (var touch in Input.touches)
		{
			if (touch.phase == TouchPhase.Began)
			{
				touched = true;
				break;
			}
		}

		if (touched == true || Input.GetMouseButtonDown(0) == true)
		{
			SetHelpLayoutVisibility(m_LastVisibleScreen, false);
		}
	}

	HintInfo GetHintInfo(E_Hint hintId)
	{
		return System.Array.Find(m_Hints, (obj) => { return obj.BindHintId == hintId ? true : false; });
	}

	ScreenInfo GetScreenInfo(string screenName)
	{
		return System.Array.Find(m_Screens, (obj) => { return obj.LinkedScreen == screenName ? true : false; });
	}

	public void PlayOneShotWithFade(AudioClip clip, float duration)
	{
		if (m_AudioSource == null)
		{
			m_AudioSource = GetComponent<AudioSource>();
			if (m_AudioSource == null)
			{
				m_AudioSource = gameObject.AddComponent<AudioSource>();
			}
		}

		StartCoroutine(PlayOneShotWithFade_Coroutine(clip, duration));
	}

	IEnumerator PlayOneShotWithFade_Coroutine(AudioClip clip, float duration)
	{
		while (m_AudioSource.volume > 0.0f)
		{
			m_AudioSource.volume -= Time.deltaTime/duration;

			yield return new WaitForEndOfFrame();
		}

		m_AudioSource.Stop();
		m_AudioSource.clip = clip;
		m_AudioSource.Play();

		while (m_AudioSource.volume < 1.0f)
		{
			m_AudioSource.volume += Time.deltaTime/duration;

			yield return new WaitForEndOfFrame();
		}
	}

	string ConstructKey(string key)
	{
		return string.Format("{0}.{1}.Help.{2}", m_PrimaryKey, GetType().Name, key);
	}
}
