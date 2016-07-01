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

public class SMFinalResults_Screen : GuiScreen
{
	class Dialog
	{
		GUIBase_Widget m_Root;
		GUIBase_Label m_ResultText;
		GUIBase_Widget m_SkullNormal;
		GUIBase_Widget m_SkullPremium;

		public Dialog(GUIBase_Widget root)
		{
			m_Root = root;
			m_ResultText = GuiBaseUtils.GetChild<GUIBase_Label>(root, "ResultText_Label");
			m_SkullNormal = GuiBaseUtils.GetChild<GUIBase_Widget>(root, "Skull_Normal");
			m_SkullPremium = GuiBaseUtils.GetChild<GUIBase_Widget>(root, "Skull_Premium");
		}

		public void Update(bool visible, string text, bool premium)
		{
			m_Root.Show(visible, true);

			if (visible == true)
			{
				m_SkullNormal.Show(!premium, true);
				m_SkullPremium.Show(premium, true);

				m_ResultText.Text = text;
			}
		}
	}

	// CONFIGURATION

	[SerializeField] int[] m_YouWonTextIds = new int[0];
	[SerializeField] int[] m_YouLoseTextIds = new int[0];

	// PRIVATE MEMBERS

	Dialog m_WinDialog;
	Dialog m_LoseDialog;

	// GUIVIEW INTERFACE

	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_WinDialog = new Dialog(Layout.GetWidget("Win"));
		m_LoseDialog = new Dialog(Layout.GetWidget("Lose"));
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		UpdateResultLabel();
	}

	// PRIVATE METHODS

	void UpdateResultLabel()
	{
		PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPlayerPPI();
		if (ppi == null)
			return;

		Client.GameInfo gameInfo = Client.Instance.GameState;
		if (gameInfo == null)
			return;

		string text = string.Empty;
		bool winner = false;
		if (gameInfo.GameType == E_MPGameType.ZoneControl)
		{
			E_Team enemy = ppi.Team == E_Team.Good ? E_Team.Bad : E_Team.Good;
			int ourScore = gameInfo.ZCInfo.TeamScore[ppi.Team];
			int enemyScore = gameInfo.ZCInfo.TeamScore[enemy];

			winner = ourScore > enemyScore ? true : false;

			int[] textIds = winner ? m_YouWonTextIds : m_YouLoseTextIds;
			int textId = textIds[Random.Range(0, textIds.Length)];

			text = TextDatabase.instance[textId];
		}
		else
		{
			List<PlayerPersistantInfo> players = PPIManager.Instance.GetPPIList();

			// sort players
			players.Sort((x, y) =>
						 {
							 // descending by score
							 int res = y.Score.Score.CompareTo(x.Score.Score);
							 if (res == 0)
							 {
								 // descending by kills
								 res = y.Score.Kills.CompareTo(x.Score.Kills);
								 if (res == 0)
								 {
									 // increasing by deaths
									 res = x.Score.Deaths.CompareTo(y.Score.Deaths);
									 if (res == 0)
									 {
										 // increasing by names
										 res = x.Name.CompareTo(y.Name);
									 }
								 }
							 }
							 return res;
						 });

			int idx = players.FindIndex(obj => obj.PrimaryKey == ppi.PrimaryKey);

			winner = idx == 0 ? true : false;
			text = winner == true ? TextDatabase.instance[00502089] : string.Format(TextDatabase.instance[00502090], PlaceToString(idx + 1));
		}

		m_WinDialog.Update(winner, text, CloudUser.instance.isPremiumAccountActive);
		m_LoseDialog.Update(!winner, text, CloudUser.instance.isPremiumAccountActive);
	}

	string PlaceToString(int place)
	{
		switch (place)
		{
		case 1:
			return string.Format(TextDatabase.instance[00502091], place);
		case 2:
			return string.Format(TextDatabase.instance[00502092], place);
		case 3:
			return string.Format(TextDatabase.instance[00502093], place);
		default:
			return string.Format(TextDatabase.instance[00502094], place);
		}
	}
}
