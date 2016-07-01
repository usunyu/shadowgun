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

public abstract class GuiMenu : BaseMenu, IViewOwner
{
	enum E_MenuState
	{
		None,
		Idle,
		Menu
	}

	// PRIVATE MEMBERS

	[SerializeField] GUIBase_Pivot m_Pivot;
	[SerializeField] GUIBase_Layout m_Background;
	[SerializeField] bool m_AllowEmptyStack;

	E_MenuState m_MenuState;
	GuiInputMenu m_InputController = new GuiInputMenu();

	Dictionary<string, GuiOverlay> m_Overlays = new Dictionary<string, GuiOverlay>();
	Tween.Tweener m_Tweener = new Tween.Tweener();
	bool m_BackgroundCustomVisibility = true;
	float m_BackgroundAlpha = 0.0f;
	bool m_OverlaysCustomEnabled = true;

	// GETTERS / SETTERS

	public GuiFrontendBase Frontend { get; private set; }

	public bool IsInitialized
	{
		get { return m_MenuState != E_MenuState.None; }
	}

	public bool IsVisible
	{
		get { return m_MenuState == E_MenuState.Menu; }
	}

	// ABSTRACT INTERFACE

	protected abstract void OnMenuInit();

	protected virtual void OnMenuDeinit()
	{
	}

	protected abstract void OnMenuUpdate();

	protected abstract void OnMenuShowMenu();
	protected abstract void OnMenuHideMenu();
	protected abstract void OnMenuRefreshMenu(bool anyPopupVisible);

	protected virtual string FixActiveScreenName(string screenName)
	{
		return screenName;
	}

	protected virtual bool ProcessMenuInput(ref IInputEvent evt)
	{
		return false;
	}

	// PUBLIC METHODS

	public void InitMenu(GuiFrontendBase frontend)
	{
		if (Frontend != null)
			return;

		//Debug.Log("InitMenu: " + name);

		Frontend = frontend;

		// setup input
		m_InputController.Opacity = GuiFrontendMain.IsVisible == true ? E_InputOpacity.SemiTransparent : E_InputOpacity.Opaque;
		m_InputController.OnInputHitTest += OnInputHitTest;
		m_InputController.OnProcessInput += OnProcessInput;
		InputManager.Register(m_InputController);

		LateInit();
	}

	public void DeinitMenu(GuiFrontendBase frontend)
	{
		if (Frontend != frontend)
			return;

		//Debug.Log("Deinit: " + name);

		OnMenuDeinit();

		m_InputController.OnInputHitTest -= OnInputHitTest;
		m_InputController.OnProcessInput -= OnProcessInput;
		InputManager.Unregister(m_InputController);

		Frontend = null;
	}

	public bool ShowMenu()
	{
		if (IsInitialized == false)
			return false;
		if (IsVisible == true)
			return true;

		//HACK: we should force font refresh some better way!
		GuiOptions.language = GuiOptions.language;

		_ClearStack();

		// register this menu to user guide
		UserGuide.RegisterMenu(this);
		Ftue.RegisterMenu(this);

		// try to display overlays
		SetOverlaysVisibleImpl(true);

		// try to show background
		UpdateBackground(IsAnyScreenVisible(), false);

		// go to default state
		SetMenuState(E_MenuState.Menu);

		// inform sub-classes
		OnMenuShowMenu();

		// start capture input
		m_InputController.CaptureInput = true;

		// done
		return true;
	}

	public void HideMenu()
	{
		if (IsVisible == false)
			return;

		// stop capture input
		m_InputController.CaptureInput = false;

		// go to idle state
		SetMenuState(E_MenuState.Idle);

		// hide background
		UpdateBackground(false, true);

		// hide overlays
		SetOverlaysVisibleImpl(false);

		// inform sub-classes
		OnMenuHideMenu();

		// register this menu to user guide
		Ftue.UnregisterMenu(this);
		UserGuide.UnregisterMenu(this);

		_ClearStack();
	}

	public void UpdateMenu()
	{
		if (IsVisible == false)
			return;

		// inform sub-classes
		OnMenuUpdate();

		_UpdateVisibleScreens();

		foreach (var overlay in m_Overlays.Values)
		{
			overlay.UpdateView();
		}

		if (m_Tweener.IsTweening == true)
		{
			m_Tweener.UpdateTweens();
		}
	}

	public void SetOverlaysVisible(bool state)
	{
		bool anyPopupVisible = activeScreen as GuiPopup != null ? true : false;
		SetOverlaysVisibleImpl(state && IsVisible);
		SetOverlaysEnabledImpl(state && !anyPopupVisible && IsVisible);
	}

	public void SetOverlaysEnabled(bool state)
	{
		m_OverlaysCustomEnabled = state;
	}

	public void SetBackgroundVisibility(bool state)
	{
		m_BackgroundCustomVisibility = state;
	}

	public GuiPopupMessageBox ShowMessageBox(int captionID, int textID, PopupHandler handler = null)
	{
		return ShowMessageBox(TextDatabase.instance[captionID], TextDatabase.instance[textID], handler);
	}

	public GuiPopupMessageBox ShowMessageBox(string caption, string text, PopupHandler handler = null)
	{
		return ShowPopup("MessageBox", caption, text, handler) as GuiPopupMessageBox;
	}

	public GuiPopupConfirmDialog ShowConfirmDialog(int captionID, int textID, PopupHandler handler = null)
	{
		return ShowConfirmDialog(TextDatabase.instance[captionID], TextDatabase.instance[textID], handler);
	}

	public GuiPopupConfirmDialog ShowConfirmDialog(string caption, string text, PopupHandler handler = null)
	{
		return ShowPopup("ConfirmDialog", caption, text, handler) as GuiPopupConfirmDialog;
	}

	void AskForFeedback()
	{
		if (IsVisible == false)
			return;

		// ask user for his feedback
		ShowConfirmDialog(0104000, 0104111, OnFeedbackConfirmation);
	}

	public void ShowFeedbackForm()
	{
		if (IsVisible == false)
			return;

#if UNITY_IPHONE || UNITY_ANDROID

		// try to open an email client
		if (EmailHelper.ShowSupportEmailComposerForIOSOrAndroid() == true)
			return;

		// inform user that there is not an email client available
		ShowMessageBox(0104000, 0104010, null);

#else	
		
		Application.OpenURL(Constants.SUPPORT_URL);
		
#endif
	}

	public void BuyPremiumAccount()
	{
		ShowPopup("PremiumAccount", "", "", (popup, result) => { });
	}

	public void QuitApplication()
	{
		StartCoroutine(QuitApplication_Coroutine(0.5f));
	}

	// ISCREENOWNER INTERFACE

	public override void ShowScreen(string screenName, bool clearStack = false)
	{
		if (screenName == "Main")
		{
			ShowMenu();
			return;
		}
		else if (string.IsNullOrEmpty(screenName) == false && screenName.StartsWith("ResearchMain") == true)
		{
			if (PlayerPrefs.GetInt(CloudUser.instance.primaryKey + ".ResearchInfoDisplayed", 0) != 1)
			{
				if (clearStack == true)
				{
					_ClearStack();
				}

				string savedScreenName = screenName;
				ShowPopup("ResearchInfo", null, null, (inPopup, inResult) => { ShowScreen(savedScreenName); });

				PlayerPrefs.SetInt(CloudUser.instance.primaryKey + ".ResearchInfoDisplayed", 1);

				return;
			}
		}

		string name = screenName;
		int page = -1;
		int idx = screenName.IndexOf(':');
		if (idx > 0)
		{
			name = screenName.Substring(0, idx);
			if (int.TryParse(screenName.Substring(idx + 1), out page) == false)
			{
				page = -1;
			}
		}

		if (ActiveScreenName != name)
		{
			InputManager.FlushInput();

			if (clearStack == true)
			{
				_ClearStack();
			}
			else
			{
				_HideTopScreen();
			}

			_ShowScreen(name);
		}

		if (page >= 0)
		{
			GuiScreenMultiPage multiPage = activeScreen as GuiScreenMultiPage;
			if (multiPage != null && multiPage.CurrentPageIndex != page)
			{
				multiPage.GotoPage(page);
			}
		}

		RefreshMenu();
	}

	public override GuiPopup ShowPopup(string popupName, string caption, string text, PopupHandler handler = null)
	{
		if (ActiveScreenName == popupName)
			return activeScreen as GuiPopup;

		InputManager.FlushInput();

		_DisableTopScreen();
		_ShowScreen(popupName);

		GuiPopup popup = activeScreen as GuiPopup;
		if (popup != null && ActiveScreenName == popupName)
		{
			popup.SetCaption(caption);
			popup.SetText(text);
			popup.SetHandler(handler);
		}

		RefreshMenu();

		return ActiveScreenName == popupName ? popup : null;
	}

	public override void DoCommand(string inCommand)
	{
		switch (inCommand)
		{
		case "FeedbackForm":
			AskForFeedback();
			break;
		case "PremiumAccount":
			BuyPremiumAccount();
			break;
		default:
			Debug.LogError("Unknown command " + inCommand);
			break;
		}
	}

	public override void Back()
	{
		if (m_AllowEmptyStack == true || ScreenStackDepth > 1)
		{
			_Back(m_AllowEmptyStack);

			RefreshMenu();
		}
	}

	// MONOBEHAVIOUR INTERFACE

	protected virtual void OnDestroy()
	{
		if (IsVisible == true)
		{
			OnMenuHideMenu();
		}

		InputManager.Unregister(m_InputController);
		m_InputController.OnProcessInput -= OnProcessInput;

		// destroy screens
		_DestroyScreens();

		// destroy overlays
		foreach (var overlay in m_Overlays.Values)
		{
			if (overlay != null)
			{
				overlay.DestroyView();
			}
		}
	}

	// PROTECTED METHODS

	protected GuiScreen RegisterScreen<T>() where T : GuiScreen
	{
		T screen = gameObject.AddComponent<T>();
		return RegisterScreen(screen);
	}

	protected void RefreshMenu()
	{
		bool anyPopupVisible = activeScreen is GuiPopup ? true : false;

		UpdateBackground(IsAnyScreenVisible(), false);

		SetOverlaysEnabledImpl(anyPopupVisible == false ? true : false);

		OnMenuRefreshMenu(anyPopupVisible);
	}

	// PRIVATE METHODS

	void SetMenuState(E_MenuState state)
	{
		if (m_MenuState == state)
			return;
		m_MenuState = state;

		switch (m_MenuState)
		{
		case E_MenuState.Idle:
			break;
		case E_MenuState.Menu:
			break;
		default:
			break;
		}
	}

	void LateInit()
	{
		GuiScreen[] screens = GetComponentsInChildren<GuiScreen>();
		foreach (var screen in screens)
		{
			RegisterScreen(screen);
		}

		GuiOverlay[] overlays = GetComponentsInChildren<GuiOverlay>();
		foreach (var overlay in overlays)
		{
			RegisterOverlay(overlay);
		}

		OnMenuInit();

		foreach (var overlayPair in m_Overlays)
		{
			GuiOverlay overlay = overlayPair.Value;
			if (overlay == null)
				continue;

			overlay.InitView();
		}

		_InitScreens();

		if (Player.LocalInstance != null && Player.LocalInstance.Controls != null)
		{
			Player.LocalInstance.Controls.LockCursor(false);
		}

		SetMenuState(E_MenuState.Idle);
	}

	GuiScreen RegisterScreen(GuiScreen screen)
	{
		string name = GetScreenName(screen);

		if (Frontend == null)
		{
			//Debug.Log(GetType().Name + "<" + this.name + ">.RegisterScreen('" + name + "') :: There is not any frontend specified!");
			return screen;
		}

		screen = Frontend.RegisterScreen(name, screen);

		_RegisterScreen(name, screen);

		return screen;
	}

	void RegisterOverlay(GuiOverlay overlay)
	{
		string name = GetScreenName(overlay);
		if (m_Overlays.ContainsKey(name) == true)
			return;

		m_Overlays[name] = overlay;
	}

	void SetOverlaysVisibleImpl(bool state)
	{
		string screenName = FixActiveScreenName(ActiveScreenName);

		foreach (var overlayPair in m_Overlays)
		{
			GuiOverlay overlay = overlayPair.Value;
			if (overlay == null)
				continue;

			if (state == true)
			{
				overlay.ShowView(this);
				overlay.EnableView();
				overlay.SetActiveScreen(screenName);
			}
			else
			{
				overlay.SetActiveScreen(null);
				overlay.DisableView();
				overlay.HideView(this);
			}
		}
	}

	void UpdateBackground(bool anyScreenVisible, bool force)
	{
		if (m_Background == null)
			return;

		bool display = m_BackgroundCustomVisibility && anyScreenVisible;
		float tweenFrom = m_Background.FadeAlpha;
		float tweenTo = 0.0f;

		m_Tweener.StopTweens(false);

		if (display == true)
		{
			tweenTo = 1.0f;

			if (m_Background.Visible == false)
			{
				m_Background.ShowImmediate(true);
			}
		}
		else if (display == false)
		{
			tweenTo = 0.0f;

			if (force == true)
			{
				m_Background.ShowImmediate(false);
			}
		}

		if (force == true)
		{
			m_Background.SetFadeAlpha(tweenTo, true);
		}
		else
		{
			m_BackgroundAlpha = tweenFrom;
			m_Tweener.TweenTo(this,
							  "m_BackgroundAlpha",
							  tweenTo,
							  0.15f,
							  Tween.Easing.Linear.EaseNone,
							  (tween, finished) =>
							  {
								  m_Background.SetFadeAlpha(m_BackgroundAlpha, true);
								  if (finished == true && m_Background.FadeAlpha == 0.0f)
								  {
									  m_Background.ShowImmediate(false);
								  }
							  });
		}
	}

	void SetOverlaysEnabledImpl(bool state)
	{
		string screenName = FixActiveScreenName(ActiveScreenName);

		foreach (var overlayPair in m_Overlays)
		{
			GuiOverlay overlay = overlayPair.Value;
			if (overlay == null)
				continue;

			if (state == true && m_OverlaysCustomEnabled == true)
			{
				overlay.EnableView();
			}
			else
			{
				overlay.DisableView();
			}
			overlay.SetActiveScreen(screenName);
		}
	}

	public static string GetScreenName<T>(T screen) where T : Component
	{
		return GetScreenName(screen != null ? screen.GetType().Name : string.Empty);
	}

	public static string GetScreenName(string name)
	{
		// remove pre-fixes
		if (name.StartsWith("GuiScreen") == true)
		{
			name = name.Substring("GuiScreen".Length);
		}
		if (name.StartsWith("GuiPopup") == true)
		{
			name = name.Substring("GuiPopup".Length);
		}
		if (name.StartsWith("GuiOverlay") == true)
		{
			name = name.Substring("GuiOverlay".Length);
		}
		if (name.StartsWith("Gui") == true)
		{
			name = name.Substring("Gui".Length);
		}
		if (name.StartsWith("SM") == true)
		{
			name = name.Substring("SM".Length);
		}

		// remove post-fixes
		if (name.EndsWith("_Screen") == true)
		{
			name = name.Substring(0, name.Length - "_Screen".Length);
		}
		if (name.EndsWith("Screen") == true)
		{
			name = name.Substring(0, name.Length - "Screen".Length);
		}
		if (name.EndsWith("Menu") == true)
		{
			name = name.Substring(0, name.Length - "Menu".Length);
		}

		// done
		return name;
	}

	// HANDLERS

	GUIBase_Widget OnInputHitTest(ref Vector2 point)
	{
		foreach (var overlayPair in m_Overlays)
		{
			GUIBase_Widget widget = HitTest(overlayPair.Value, ref point);
			if (widget != null)
				return widget;
		}

		return HitTest(activeScreen, ref point);
	}

	GUIBase_Widget HitTest(GuiView view, ref Vector2 point)
	{
		if (view == null)
			return null;
		if (view.IsInitialized == false)
			return null;
		if (view.IsVisible == false)
			return null;
		if (view.IsEnabled == false)
			return null;

		return view.HitTest(ref point);
	}

	bool OnProcessInput(ref IInputEvent evt)
	{
		foreach (var overlayPair in m_Overlays)
		{
			GuiOverlay overlay = overlayPair.Value;
			if (overlay == null)
				continue;

			if (overlay.ProcessInput(ref evt) == true)
				return true;
		}

		GuiScreen screen = activeScreen;
		if (screen != null)
		{
			if (screen.ProcessInput(ref evt) == true)
				return true;

			if (evt.Kind == E_EventKind.Key)
			{
				KeyEvent key = (KeyEvent)evt;
				if (key.Code == KeyCode.Escape)
				{
					if (key.State == E_KeyState.Released)
					{
						if (m_AllowEmptyStack == true || ScreenStackDepth > 1)
						{
							Back();
							return true;
						}
					}
				}
			}
		}

		return ProcessMenuInput(ref evt);
	}

	void OnFeedbackConfirmation(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		if (inResult != E_PopupResultCode.Ok)
			return;

		ShowFeedbackForm();
	}

	IEnumerator QuitApplication_Coroutine(float delay)
	{
		// disable input
		InputManager.IsEnabled = false;

		// start fade-in
		MFGuiFader.FadeIn(delay);

		// wait for a while
		yield return new WaitForSeconds(delay*0.9f);

		// disable all cameras now
		foreach (Camera camera in Camera.allCameras)
		{
			camera.enabled = false;
			camera.gameObject.SetActive(false);
		}

		// finish fade-in
		yield return new WaitForSeconds(delay*0.1f);

		// wait a little longer
		yield return new WaitForSeconds(0.1f);

		// it's safe to quit now
		Application.Quit();
	}
}
