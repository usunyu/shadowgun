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
using DateTime = System.DateTime;

[AddComponentMenu("GUI/Frontend/Overlays/GuiOverlayNewsBar")]
public class GuiOverlayNewsBar : GuiOverlay
{
	// PRIVATE MEMBERS

	GUIBase_Label m_VersionInfo;
	GUIBase_Label m_Label;
	string m_DefaultMessage = "";
	int m_MessageCycling = 0;

	// GUIOVERLAY INTERFACE

	protected override void OnViewInit()
	{
		m_Label = GuiBaseUtils.FindLayoutWidget<GUIBase_Label>(Layout, "GUIBase_Label");
		m_VersionInfo = GuiBaseUtils.FindLayoutWidget<GUIBase_Label>(Layout, "Version_Label");

		m_DefaultMessage = m_Label.GetText();

		// anchor news bar to the bottom of the screen
		Transform trans = Layout.transform;
		Vector3 position = trans.position;
		position.y = Screen.height - Layout.PlatformSize.y*Layout.LayoutScale.y;
		trans.position = position;
	}

	protected override void OnViewShow()
	{
		GameCloudManager.mailbox.FetchMessages();

		if (m_VersionInfo.Widget.Visible == false)
		{
			m_VersionInfo.Widget.Show(true, true);
		}
		m_VersionInfo.SetNewText("v" + BuildInfo.Version);

		Invoke("UpdateMessage", 4.0f);
	}

	protected override void OnViewHide()
	{
		CancelInvoke("UpdateMessage");
	}

	protected override void OnActiveScreen(string screenName)
	{
		switch (screenName)
		{
		case "Shop":
			Layout.Show(false);
			break;
		case "Equip":
			Layout.Show(false);
			break;
		case "PlayerStats":
			Layout.Show(false);
			break;
		default:
			Layout.Show(IsVisible);
			break;
		}
	}

	// PRIVATE METHODS

	void UpdateMessage()
	{
		// choose one message and show it
		if (m_Label != null)
		{
			string message = GetMessageToShow();
			m_Label.SetNewText(message);
		}
	}

	string GetMessageToShow()
	{
		Invoke("UpdateMessage", 4.0f);
		m_MessageCycling = ++m_MessageCycling%3;

		if (uLink.Network.status == uLink.NetworkStatus.Disconnected)
		{
			// cycle between normal messages and match making status message
			// (but only when the player is not connected/playing a game)
			// the match making statistics stays 2 times longer
			if (m_MessageCycling > 0)
			{
				return LobbyClient.MatchMakingStatistics;
			}
		}

		News.HeadLine headLine = GameCloudManager.news.GetNextHeadLine();
		if (headLine != null)
		{
			return headLine.Text;
		}

		return m_DefaultMessage;
	}
}
