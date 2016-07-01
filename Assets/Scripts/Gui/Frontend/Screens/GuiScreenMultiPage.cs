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

public abstract class GuiScreenMultiPage : GuiScreen, IViewOwner
{
	enum E_SwitchPage
	{
		Prev = -1,
		Next = +1
	}

	// PRIVATE MEMBERS

	[SerializeField] int m_CurrentPage = 0;
	[SerializeField] GuiScreen[] m_Pages = new GuiScreen[0];

	//HACK: disable some buttons for beta
	List<string> HACK_disabledForBeta = new List<string>() {"00VoiceChat_Layout"};

	// GETTERS / SETTERS

	public int CurrentPageIndex
	{
		get { return m_CurrentPage; }
		set { SwitchPage(value); }
	}

	public GuiScreen CurrentPage
	{
		get { return m_CurrentPage >= 0 && m_CurrentPage < m_Pages.Length ? m_Pages[m_CurrentPage] : null; }
	}

	// ABSTRACT INTERFACE

	protected virtual void OnPageVisible(GuiScreen page)
	{
	}

	protected virtual void OnPageHiding(GuiScreen page)
	{
	}

	// PUBLIC METHODS

	public GuiScreen GetPage(int idx)
	{
		if (idx < 0 || idx >= m_Pages.Length)
			return null;
		return m_Pages[idx];
	}

	public void GotoPreviousPage()
	{
		SwitchPage(E_SwitchPage.Prev);
	}

	public void GotoNextPage()
	{
		SwitchPage(E_SwitchPage.Next);
	}

	public virtual void GotoPage(int pageIdx)
	{
		SwitchPage(pageIdx);
	}

	public void ResetPage()
	{
		GuiScreen page = CurrentPage;
		if (page != null)
		{
			page.ResetView();
		}
	}

	// GUISCREEN INTERFACE

	protected override void OnViewInit()
	{
		base.OnViewInit();

		// init pages
		int pageIndex = 0;
		List<GuiScreen> pages = new List<GuiScreen>();
		foreach (var page in m_Pages)
		{
			if (page == null)
				continue;
			if (BuildInfo.Version.Stage == BuildInfo.Stage.Beta &&
				page.Layout != null &&
				HACK_disabledForBeta.Contains(page.Layout.name) == true)
				continue;

			page.MultiPageIndex = pageIndex;
			page.InitView();
			pages.Add(page);

			pageIndex += 1;
		}
		m_Pages = pages.ToArray();
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		// activate current page
		ShowPage(m_CurrentPage);
		ResetPage();
	}

	protected override void OnViewHide()
	{
		HidePage(m_CurrentPage);

		// call super
		base.OnViewHide();
	}

	protected override void OnViewUpdate()
	{
		GuiScreen page = CurrentPage;
		if (page != null)
		{
			page.UpdateView();
		}

		base.OnViewUpdate();
	}

	protected override void OnViewEnable()
	{
		base.OnViewEnable();

		// enable current page
		EnablePage(m_CurrentPage, IsEnabled);
	}

	protected override void OnViewDisable()
	{
		// disable current page
		EnablePage(m_CurrentPage, false);

		// call super
		base.OnViewDisable();
	}

	protected override void OnViewDestroy()
	{
		foreach (var page in m_Pages)
		{
			if (page != null)
			{
				page.DestroyView();
			}
		}

		// call super
		base.OnViewDestroy();
	}

	protected override GUIBase_Widget OnViewHitTest(ref Vector2 point)
	{
		if (Owner == null)
			return null;

		GUIBase_Widget widget = base.OnViewHitTest(ref point);
		if (widget != null)
			return widget;

		GuiScreen page = CurrentPage;
		return page != null ? page.HitTest(ref point) : null;
	}

	protected override bool OnViewProcessInput(ref IInputEvent evt)
	{
		if (Owner == null)
			return false;

		if (base.OnViewProcessInput(ref evt) == true)
			return true;

		GuiScreen page = CurrentPage;
		return page != null ? page.ProcessInput(ref evt) : false;
	}

	// ISCREENOWNER INTERFACE

	public void DoCommand(string inCommand)
	{
		if (Owner != null)
		{
			Owner.DoCommand(inCommand);
		}
	}

	public void ShowScreen(string inScreenName, bool inClearStack)
	{
		if (Owner != null)
		{
			Owner.ShowScreen(inScreenName, inClearStack);
		}
	}

	public GuiPopup ShowPopup(string inPopupName, string inCaption, string inText, PopupHandler inHandler)
	{
		if (Owner == null)
			return null;

		return Owner.ShowPopup(inPopupName, inCaption, inText, inHandler);
	}

	public void Back()
	{
		if (Owner != null)
		{
			Owner.Back();
		}
	}

	public void Exit()
	{
		if (Owner != null)
		{
			Owner.Exit();
		}
	}

	public bool IsTopView(GuiView inView)
	{
		return Owner != null ? Owner.IsTopView(inView) : false;
	}

	public bool IsAnyScreenVisible()
	{
		return Owner != null ? Owner.IsAnyScreenVisible() : false;
	}

	public bool IsAnyPopupVisible()
	{
		return Owner != null ? Owner.IsAnyPopupVisible() : false;
	}

	// PRIVATE METHODS

	void SwitchPage(E_SwitchPage page)
	{
		SwitchPage(m_CurrentPage + (int)page);
	}

	void SwitchPage(int pageIdx)
	{
		EnablePage(m_CurrentPage, false);
		HidePage(m_CurrentPage);

		m_CurrentPage = pageIdx;
		if (m_CurrentPage < 0)
		{
			m_CurrentPage = Mathf.Max(0, m_Pages.Length - 1);
		}
		else if (m_CurrentPage >= m_Pages.Length)
		{
			m_CurrentPage = 0;
		}

		ShowPage(m_CurrentPage);
		EnablePage(m_CurrentPage, IsEnabled);
		ResetPage();
	}

	void ShowPage(int pageIdx)
	{
		if (pageIdx < 0)
			return;
		if (pageIdx >= m_Pages.Length)
			return;

		GuiScreen page = m_Pages[pageIdx];
		if (page == null)
			return;

		page.ShowView(this);

		OnPageVisible(page);
	}

	void HidePage(int pageIdx)
	{
		if (pageIdx < 0)
			return;
		if (pageIdx >= m_Pages.Length)
			return;

		GuiScreen page = m_Pages[pageIdx];
		if (page == null)
			return;

		OnPageHiding(page);

		page.HideView(this);
	}

	void EnablePage(int pageIdx, bool state)
	{
		if (pageIdx < 0)
			return;
		if (pageIdx >= m_Pages.Length)
			return;

		GuiScreen page = m_Pages[pageIdx];
		if (page == null)
			return;

		if (state == true)
		{
			page.EnableView();
		}
		else
		{
			page.DisableView();
		}
	}
}
