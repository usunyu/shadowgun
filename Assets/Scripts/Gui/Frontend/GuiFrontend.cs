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

public abstract class GuiFrontendBase : MonoBehaviour
{
	GuiCache<GuiScreen> m_Screens = new GuiCache<GuiScreen>();

	public GuiScreen RegisterScreen(string name, GuiScreen screen)
	{
		return m_Screens.Register(name, screen);
	}
}

public abstract class GuiFrontend<T> : GuiFrontendBase
				where T : struct, System.IComparable
{
	// PRIVATE MEMBERS

	T m_CurrentState;
	Dictionary<T, GuiMenu> m_Menus = new Dictionary<T, GuiMenu>();

	// GETTERS / SETTERS

	protected T CurrentState
	{
		get { return m_CurrentState; }
	}

	protected GuiMenu CurrentMenu
	{
		get { return GetMenuForState(m_CurrentState); }
	}

	// ABSTRACT INTERFACE

	protected virtual void OnStateChanged()
	{
	}

	// PROTECTED METHODS

	protected bool IsInState(T state)
	{
		return m_CurrentState.Equals(state);
	}

	protected GuiMenu SetState(T state)
	{
		if (IsInState(state) == true)
			return CurrentMenu;

		InputManager.FlushInput();

		// hide previous menu if any
		GuiMenu menu = CurrentMenu;
		if (menu != null)
		{
			menu.HideMenu();
		}

		// set new state
		m_CurrentState = state;

		// inform sub-classes
		OnStateChanged();

		// get current menu
		return CurrentMenu;
	}

	protected N RegisterMenu<N>(T state) where N : GuiMenu
	{
		if (m_Menus.ContainsKey(state) == true)
			return null;

		N menu = GetComponentInChildren<N>();
		if (menu == null)
			return null;

		m_Menus[state] = menu;

		return menu;
	}

	protected N GetMenuForState<N>(T state) where N : GuiMenu
	{
		return GetMenuForState(state) as N;
	}

	protected GuiMenu GetMenuForState(T state)
	{
		if (m_Menus.ContainsKey(state) == false)
			return null;
		return m_Menus[state];
	}

	protected virtual void OnDestroy()
	{
		foreach (var pair in m_Menus)
		{
			if (pair.Value != null)
				pair.Value.DeinitMenu(this);
		}
	}
}
