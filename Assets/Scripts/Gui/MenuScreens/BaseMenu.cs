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

// =====================================================================================================================
// =====================================================================================================================
public abstract class BaseMenu : MonoBehaviour, IViewOwner
{
	Dictionary<string, GuiScreen> m_Screens = new Dictionary<string, GuiScreen>();
	Stack<GuiScreen> m_ActiveScreens = new Stack<GuiScreen>();
	Dictionary<GuiScreen, string> m_ScreensToNames_TODO = new Dictionary<GuiScreen, string>(); // TODO :: I have to implement it better...

	protected GuiScreen activeScreen
	{
		get { return m_ActiveScreens.Count > 0 ? m_ActiveScreens.Peek() : null; }
	}

	/*
	string											activeScreenName { 
		get { 
			return m_ScreensToNames_TODO.ContainsKey(activeScreen) == true ? 
				      m_ScreensToNames_TODO[activeScreen] 				   : 
				      string.Empty;
		} 
	}*/
	public string ActiveScreenName { get; private set; }

	public int ScreenStackDepth
	{
		get { return m_ActiveScreens.Count; }
	}

	// =================================================================================================================	
	// IScreenOwner interface...
	public abstract void DoCommand(string inCommand);
	public abstract void ShowScreen(string inScreenName, bool inClearStack = false); // TODO I don't like strings...
	public abstract GuiPopup ShowPopup(string inPopupName, string inCaption, string inText, PopupHandler inHandler = null);
	public abstract void Back();
	public abstract void Exit();

	public bool IsTopView(GuiView inView)
	{
		return activeScreen != null && activeScreen == inView ? true : false;
	}

	public bool IsAnyScreenVisible()
	{
		return m_ActiveScreens.Count > 0 && IsAnyPopupVisible() == false ? true : false;
	}

	public bool IsAnyPopupVisible()
	{
		foreach (var screen in m_ActiveScreens)
		{
			if (screen is GuiPopup)
				return true;
			if (screen is GuiScreenModal)
				return true;
		}
		return false;
	}

	// =================================================================================================================
	protected T _CreateScreen<T>(string inScreenName, GameObject inGameObject = null) where T : GuiScreen
	{
		GameObject go = inGameObject ?? gameObject;

		T screen = go.AddComponent<T>();
		_RegisterScreen(inScreenName, screen);

		return screen;
	}

	protected void _RegisterScreen(string inScreenName, GuiScreen inScreen)
	{
		m_Screens[inScreenName] = inScreen;
		m_ScreensToNames_TODO[inScreen] = inScreenName;
	}

	protected void _InitScreens()
	{
		foreach (GuiScreen screen in m_Screens.Values)
		{
			screen.InitView();
		}
	}

	protected void _DestroyScreens()
	{
		foreach (GuiScreen screen in m_Screens.Values)
		{
			if (screen != null)
			{
				screen.DestroyView();
			}
		}
	}

	protected void _UpdateVisibleScreens()
	{
		if (m_ActiveScreens == null || m_ActiveScreens.Count <= 0)
			return;

		List<GuiScreen> screensForUpdateInThisTick = new List<GuiScreen>();

		foreach (GuiScreen screen in m_ActiveScreens)
		{
			// skip null scenes, TODO Is it error ?
			if (screen == null)
				continue;

			if (screen.IsVisible == false)
				break;

			screensForUpdateInThisTick.Add(screen);
		}

		foreach (GuiScreen screen in screensForUpdateInThisTick)
		{
			screen.UpdateView();
		}
	}

	protected void _ClearStack()
	{
		while (m_ActiveScreens.Count > 0)
		{
			GuiScreen activeScreen = m_ActiveScreens.Pop();
			if (activeScreen != null && activeScreen.IsVisible == true)
			{
				activeScreen.HideView(this);
			}
		}

		ActiveScreenName = null;
	}

	protected void _HideTopScreen()
	{
		if (m_ActiveScreens.Count <= 0)
			return;

		GuiScreen activeScreen = m_ActiveScreens.Peek();
		if (activeScreen != null)
		{
			activeScreen.HideView(this);
		}
	}

	protected void _DisableTopScreen()
	{
		if (m_ActiveScreens.Count <= 0)
			return;

		GuiScreen activeScreen = m_ActiveScreens.Peek();
		if (activeScreen != null)
		{
			activeScreen.DisableView();
		}
	}

	/*protected void _EnableTopScreen()
	{
		if(m_ActiveScreens.Count <= 0)
			return;

		GuiScreen activeScreen = m_ActiveScreens.Peek();
		if(activeScreen != null)
		{
			activeScreen.Screen_Enable();
		}
	}*/

	protected void _ShowScreen(string inScreenName)
	{
//		Debug.Log("ShowScreen : " + inScreenName + " active: " + ((m_ActiveScreens.Count > 0) ? m_ScreensToNames_TODO[m_ActiveScreens.Peek()] : "none"));
		GuiScreen newScreen;
		if (m_Screens.TryGetValue(inScreenName, out newScreen) == true)
		{
			ActiveScreenName = inScreenName;
			m_ActiveScreens.Push(newScreen);
			newScreen.ShowView(this);
			newScreen.EnableView();
		}
		else
		{
			Debug.LogError(GetType().Name + "<" + name + ">._ShowScreen('" + inScreenName + "') :: Screen doesn't exist!");
		}
	}

	protected void _Back(bool force = false)
	{
		int minCount = force == true ? 0 : 1;

		// nothing if there is only last screen.
		if (m_ActiveScreens.Count <= minCount)
			return;

		GuiScreen activeScreen = m_ActiveScreens.Pop();
		activeScreen.HideView(this);

		if (m_ActiveScreens.Count > 0)
		{
			activeScreen = m_ActiveScreens.Peek();

			//Debug.Log("back: " + m_ScreensToNames_TODO[activeScreen] + " enabled: " + activeScreen.isEnabled);

			if (activeScreen.IsEnabled == false)
				activeScreen.EnableView();
			else
				activeScreen.ShowView(this);

			ActiveScreenName = m_ScreensToNames_TODO[activeScreen];
		}
		else
		{
			ActiveScreenName = null;
		}
	}
}
