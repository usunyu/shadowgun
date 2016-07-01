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
using System;
using System.Collections;
using System.Collections.Generic;

public class ScoreListScreenComponent : MonoBehaviour
{
	internal class ScoreListLine
	{
		GUIBase_Sprite m_LineSprite;

		GUIBase_Label m_RankLabel;
		GUIBase_Label m_OrederLabel;

		GUIBase_Label m_NameLabel;
		GUIBase_Label m_ScoreLabel;
		GUIBase_Label m_KillsLabel;
		GUIBase_Label m_DeathLabel;
		GUIBase_Label m_PingLabel;

		public Vector3 spritePos
		{
			get { return m_LineSprite.transform.position; }
		}

		public ScoreListLine(GUIBase_Sprite inLineSprite)
		{
			m_LineSprite = inLineSprite;
			GUIBase_Label[] labels = m_LineSprite.Widget.GetComponentsInChildren<GUIBase_Label>();
			foreach (GUIBase_Label l in labels)
			{
				if (l.name == "Player_Label")
					m_NameLabel = l;
				else if (l.name == "Kill_Label")
					m_KillsLabel = l;
				else if (l.name == "Death_Label")
					m_DeathLabel = l;
				else if (l.name == "Rank_Label")
					m_RankLabel = l;
				else if (l.name == "Order_Label")
					m_OrederLabel = l;
				else if (l.name == "Score_Label")
					m_ScoreLabel = l;
				else if (l.name == "Ping_Label")
					m_PingLabel = l;
			}
		}

		public void Update(PlayerPersistantInfo inPPI, int inOrder)
		{
			if (m_OrederLabel != null)
				m_OrederLabel.SetNewText(inOrder.ToString());

			if (m_RankLabel != null)
				m_RankLabel.SetNewText(inPPI.Rank.ToString());

			m_NameLabel.SetNewText(inPPI.NameForGui);

			m_ScoreLabel.SetNewText(inPPI.Score.Score.ToString());
			m_KillsLabel.SetNewText(inPPI.Score.Kills.ToString());
			m_DeathLabel.SetNewText(inPPI.Score.Deaths.ToString());
			m_PingLabel.SetNewText(inPPI.Ping.ToString());

			//Debug.Log("show PPI " + inPPI.Name + " " + inPPI.Score.Score.ToString());
		}

		public void Show()
		{
			MFGuiManager.Instance.ShowWidget(m_LineSprite.Widget, true, true);
		}

		public void Hide()
		{
			MFGuiManager.Instance.ShowWidget(m_LineSprite.Widget, false, true);
		}
	};

	const int maxPlayers = 8;

	ScoreListLine[] m_ScoreLines = new ScoreListLine[maxPlayers];
	GUIBase_Sprite m_HighlightSprite = null;
	int m_HighlightIndex = -1;
	E_Team m_TeamFilter = E_Team.None;

	public void InitGuiLines(GameObject inParent, E_Team inTeamFilter)
	{
		m_TeamFilter = inTeamFilter;

		//get labels for players
		for (int i = 0; i < maxPlayers; i++)
		{
			// find sprite line...
			Transform line = inParent.transform.FindChildByName("Score_Sprite_" + i);
			if (line == null)
				continue;

			GUIBase_Sprite lineSprite = line.GetComponent<GUIBase_Sprite>();
			if (lineSprite == null)
				continue;

			m_ScoreLines[i] = new ScoreListLine(lineSprite);
		}

		//get sprite for highlight line
		m_HighlightSprite = inParent.transform.FindChildByName("ScoreSelection_Sprite").GetComponent<GUIBase_Sprite>();
	}

	public void Show()
	{
		UpdateStats();
		InvokeRepeating("UpdateStats", 1.0f, 1.0f);
	}

	public void Hide()
	{
		CancelInvoke();
	}

	void UpdateStats()
	{
		m_HighlightIndex = -1;

		List<PlayerPersistantInfo> ppis = GetPPIList();

		// Debug.Log("ppis.Count " + ppis.Count);

		for (int i = 0; i < m_ScoreLines.Length; i++)
		{
			ScoreListLine line = m_ScoreLines[i];

			if (line == null)
				continue;

			if (i < ppis.Count)
			{
				line.Update(ppis[i], i);
				line.Show();

				if (ppis[i].Player == PPIManager.Instance.GetLocalPPI().Player)
					m_HighlightIndex = i;
			}
			else
			{
				line.Hide();
			}
		}

		//zobraz highligh
		if (m_HighlightIndex != -1)
			ShowHighlight(m_HighlightIndex);
		else
			HideHighlight();
	}

	List<PlayerPersistantInfo> GetPPIList()
	{
		List<PlayerPersistantInfo> originalPPIs = PPIManager.Instance.GetPPIList();
		List<PlayerPersistantInfo> filteredPPIs = new List<PlayerPersistantInfo>();

		// filter PPIS...        
		if (m_TeamFilter != E_Team.None)
		{
			for (int i = 0; i < originalPPIs.Count; i++)
			{
				if (originalPPIs[i] != null && originalPPIs[i].Team == m_TeamFilter)
				{
					filteredPPIs.Add(originalPPIs[i]);
				}
			}
		}
		else
		{
			filteredPPIs = new List<PlayerPersistantInfo>(originalPPIs);
		}

		// and sort PPIs... 
		filteredPPIs.Sort((ps1, ps2) => ps2.Score.Score.CompareTo(ps1.Score.Score));

		return filteredPPIs;
	}

	void ShowHighlight(int index)
	{
		m_HighlightSprite.transform.position = m_ScoreLines[index].spritePos;
		m_HighlightSprite.Widget.Show(true, true);
	}

	void HideHighlight()
	{
		m_HighlightSprite.Widget.Show(false, true);
	}
}
