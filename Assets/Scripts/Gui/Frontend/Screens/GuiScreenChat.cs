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

interface IGuiPageChat
{
	string CaptionText { get; }
	int UnreadMessages { get; }
	bool NotifyUser { get; }
}

[AddComponentMenu("GUI/Frontend/Screens/GuiScreenChat")]
public class GuiScreenChat : GuiScreenModal, IGuiOverlayScreen
{
	readonly static string[] BUTTONS = {"Lobby_Button", "Friends_Button"};

	// PRIVATE MEMBERS

	GUIBase_Button[] m_Buttons = new GUIBase_Button[BUTTONS.Length];
	float m_NextUpdateTime = 0.0f;

	// IGUIOVERLAYSCREEN INTERFACE

	string IGuiOverlayScreen.OverlayButtonTooltip
	{
		get
		{
			if (IsVisible == true)
				return null;
			int messages = 0;
			for (int idx = 0; idx < m_Buttons.Length; ++idx)
			{
				IGuiPageChat page = GetPage(idx) as IGuiPageChat;
				if (page != null && page.NotifyUser == true)
				{
					messages += page.UnreadMessages;
				}
			}
			if (messages == 0)
				return null;
			return messages <= 99 ? messages.ToString() : "99+";
		}
	}

	bool IGuiOverlayScreen.HideOverlayButton
	{
		get { return Ftue.IsActionIdle<FtueAction.Chat>(); }
	}

	bool IGuiOverlayScreen.HighlightOverlayButton
	{
		get
		{
			if (Ftue.IsActionActive<FtueAction.Chat>() == true)
				return true;
			if (IsVisible == false)
			{
				for (int idx = 0; idx < m_Buttons.Length; ++idx)
				{
					IGuiPageChat page = GetPage(idx) as IGuiPageChat;
					if (page != null && page.NotifyUser == true)
						return true;
				}
			}
			return false;
		}
	}

	// GUISCREENMULTIPAGE INTERFACE

	protected override void OnPageVisible(GuiScreen page)
	{
		if (CurrentPageIndex < 0 || CurrentPageIndex >= m_Buttons.Length)
			return;

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

	// GUIOVERLAY INTERFACE

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
		base.OnViewShow();

		// bind buttons
		for (int idx = 0; idx < m_Buttons.Length; ++idx)
		{
			int pageId = idx;
			GuiBaseUtils.RegisterButtonDelegate(m_Buttons[idx],
												null,
												(inside) =>
												{
													if (inside == true && CurrentPageIndex != pageId)
													{
														GotoPage(pageId);
													}
												});
		}
	}

	protected override void OnViewHide()
	{
		// unbind buttons
		for (int idx = 0; idx < m_Buttons.Length; ++idx)
		{
			GuiBaseUtils.RegisterButtonDelegate(m_Buttons[idx], null, null);
		}

		// call super
		base.OnViewHide();
	}

	protected override void OnViewUpdate()
	{
		base.OnViewUpdate();

		m_NextUpdateTime -= Time.deltaTime;
		if (m_NextUpdateTime > 0.0f)
			return;
		m_NextUpdateTime = 0.5f;

		for (int idx = 0; idx < m_Buttons.Length; ++idx)
		{
			IGuiPageChat page = GetPage(idx) as IGuiPageChat;
			if (page != null)
			{
				GUIBase_Button button = m_Buttons[idx];
				button.SetNewText(page.CaptionText);
				button.isHighlighted = idx != CurrentPageIndex ? page.NotifyUser : false;
			}
		}
	}
}
