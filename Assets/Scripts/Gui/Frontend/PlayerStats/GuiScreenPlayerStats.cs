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

interface IGuiPagePlayerStats
{
	void Refresh(PlayerPersistantInfo ppi);
}

[AddComponentMenu("GUI/Frontend/Popups/GuiScreenPlayerStats")]
public class GuiScreenPlayerStats : GuiScreenModal, IGuiOverlayScreen
{
	readonly static string[] BUTTONS = {"YourSum_Button", "RecentMatch_Button", "FavArsenal_Button"};
	readonly static string USERNAME = "Username";
	readonly static string USERNAMEPREV = "UsernamePrev_Button";
	readonly static string USERNAMENEXT = "UsernameNext_Button";

	// PRIVATE MEMBERS

	GUIBase_Button[] m_Buttons = new GUIBase_Button[BUTTONS.Length];

	// PUBLIC MEMBERS

	public static PlayerPersistantInfo[] UserPPIs;
	public static PlayerPersistantInfo UserPPI;

	// IGUIOVERLAYSCREEN INTERFACE

	string IGuiOverlayScreen.OverlayButtonTooltip
	{
		get { return null; }
	}

	bool IGuiOverlayScreen.HideOverlayButton
	{
		get { return Ftue.IsActionIdle<FtueAction.Stats>(); }
	}

	bool IGuiOverlayScreen.HighlightOverlayButton
	{
		get { return Ftue.IsActionActive<FtueAction.Stats>(); }
	}

	// GUISCREENMULTIPAGE INTERFACE

	protected override void OnPageVisible(GuiScreen page)
	{
		if (CurrentPageIndex < 0 || CurrentPageIndex >= m_Buttons.Length)
			return;

		IGuiPagePlayerStats statsPage = CurrentPage as IGuiPagePlayerStats;
		if (statsPage != null)
		{
			statsPage.Refresh(UserPPI);
		}

		m_Buttons[CurrentPageIndex].stayDown = true;
		m_Buttons[CurrentPageIndex].ForceDownStatus(true);
	}

	protected override void OnPageHiding(GuiScreen page)
	{
		if (CurrentPageIndex < 0 || CurrentPageIndex >= m_Buttons.Length)
			return;

		m_Buttons[CurrentPageIndex].stayDown = false;
		m_Buttons[CurrentPageIndex].ForceDownStatus(false);
	}

	// GUISCREEN INTERFACE

	protected override void OnViewInit()
	{
		base.OnViewInit();

		for (int idx = 0; idx < m_Buttons.Length; ++idx)
		{
			m_Buttons[idx] = GuiBaseUtils.GetControl<GUIBase_Button>(Layout, BUTTONS[idx]);
		}
	}

	protected override void OnViewShow()
	{
		UserPPI = UserPPI ?? PPIManager.Instance.GetLocalPPI();

		base.OnViewShow();

		// bind buttons
		for (int idx = 0; idx < m_Buttons.Length; ++idx)
		{
			int pageId = idx;
			GuiBaseUtils.RegisterButtonDelegate(m_Buttons[idx],
												null,
												(inside) =>
												{
													if (inside == true)
													{
														GotoPage(pageId);
													}
												});
		}

		GUIBase_Button prevButton = RegisterButtonDelegate(USERNAMEPREV, () => { SwitchPPI(-1); }, null);
		prevButton.Widget.Show(UserPPIs != null && UserPPIs.Length > 1 ? true : false, true);

		GUIBase_Button nextButton = RegisterButtonDelegate(USERNAMENEXT, () => { SwitchPPI(+1); }, null);
		nextButton.Widget.Show(UserPPIs != null && UserPPIs.Length > 1 ? true : false, true);

		Refresh();
	}

	protected override void OnViewHide()
	{
		UserPPI = null;
		UserPPIs = null;

		// unbind buttons
		for (int idx = 0; idx < m_Buttons.Length; ++idx)
		{
			GuiBaseUtils.RegisterButtonDelegate(m_Buttons[idx], null, null);
		}
		RegisterButtonDelegate(USERNAMEPREV, null, null);
		RegisterButtonDelegate(USERNAMENEXT, null, null);

		// call super
		base.OnViewHide();
	}

	protected override bool OnViewProcessInput(ref IInputEvent evt)
	{
		if (evt.Kind == E_EventKind.Key)
		{
			KeyEvent key = (KeyEvent)evt;
			if (key.Code == KeyCode.Escape)
			{
				if (key.State == E_KeyState.Released)
				{
					Owner.Back();
				}
				return true;
			}
		}

		return base.OnViewProcessInput(ref evt);
	}

	// PRIVATE METHODS

	void Refresh()
	{
		IGuiPagePlayerStats statsPage = CurrentPage as IGuiPagePlayerStats;
		if (statsPage != null)
		{
			statsPage.Refresh(UserPPI);
		}

		GuiBaseUtils.GetControl<GUIBase_Label>(Layout, USERNAME).SetNewText(UserPPI.NameForGui);
	}

	void SwitchPPI(int dir)
	{
		if (UserPPI == null)
			return;
		if (UserPPIs == null)
			return;
		if (UserPPIs.Length <= 1)
			return;

		int idx = System.Array.FindIndex(UserPPIs, obj => obj.PrimaryKey == UserPPI.PrimaryKey) + dir;
		int last = UserPPIs.Length - 1;
		if (idx < 0)
		{
			idx = last;
		}
		else if (idx > last)
		{
			idx = 0;
		}

		UserPPI = UserPPIs[idx];

		Refresh();
	}
}
