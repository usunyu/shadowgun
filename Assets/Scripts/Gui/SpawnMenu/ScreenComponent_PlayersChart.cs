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

public class ScreenComponent_PlayersChart : ScreenComponent
{
	//==================================================================================================================

	class ChartLine
	{
		GUIBase_Widget m_LineWidget;
		GUIBase_Label m_OrderLabel;
		GUIBase_MultiSprite m_RankSprite;
		GUIBase_Label m_RankLabel;
		GUIBase_Label m_NameLabel;
		GUIBase_Label m_ScoreLabel;
		GUIBase_Label m_KillsLabel;
		GUIBase_Label m_DeadsLabel;
		GUIBase_Widget m_FriendIcon;
		GUIBase_MultiSprite m_PlatformSprite;

		public Vector3 Pos
		{
			get { return m_LineWidget.transform.position; }
		}

		//--------------------------------------------------------------------------------------------------------------
		public ChartLine(GUIBase_Widget inLineWidget)
		{
			m_LineWidget = inLineWidget;

			GUIBase_Label[] labels = m_LineWidget.GetComponentsInChildren<GUIBase_Label>();
			GUIBase_MultiSprite[] sprites = m_LineWidget.GetComponentsInChildren<GUIBase_MultiSprite>();

			foreach (GUIBase_Label l in labels)
			{
				if (l.name == "Stand_Enum")
					m_OrderLabel = l;
				else if (l.name == "RankNumber")
					m_RankLabel = l;
				else if (l.name == "Name")
					m_NameLabel = l;
				else if (l.name == "Score_Enum")
					m_ScoreLabel = l;
				else if (l.name == "Kills_Enum")
					m_KillsLabel = l;
				else if (l.name == "Deads_Enum")
					m_DeadsLabel = l;
			}

			foreach (GUIBase_MultiSprite s in sprites)
			{
				if (s.name == "RankPic")
					m_RankSprite = s;
				else if (s.name == "PlatformPic")
					m_PlatformSprite = s;
			}

			m_FriendIcon = GuiBaseUtils.GetChild<GUIBase_Widget>(m_LineWidget, "FriendIcon");
		}

		//--------------------------------------------------------------------------------------------------------------
		public void Update(PlayerPersistantInfo inPPI, int inOrder, Color inBackground)
		{
			if (m_OrderLabel != null)
				m_OrderLabel.SetNewText(inOrder.ToString());

			if (m_RankSprite != null)
				m_RankSprite.State = "Rank_" + Mathf.Min(inPPI.Rank, m_RankSprite.Count - 1).ToString("D2");

			if (m_RankLabel != null)
				m_RankLabel.SetNewText(inPPI.Rank.ToString());

			if (m_NameLabel != null)
				m_NameLabel.SetNewText(inPPI.NameForGui);

			if (m_ScoreLabel != null)
				m_ScoreLabel.SetNewText(inPPI.Score.Score.ToString());

			if (m_KillsLabel != null)
				m_KillsLabel.SetNewText(inPPI.Score.Kills.ToString());

			if (m_DeadsLabel != null)
				m_DeadsLabel.SetNewText(inPPI.Score.Deaths.ToString());

			if (m_FriendIcon != null)
			{
				bool isFriend = GameCloudManager.friendList.friends.FindIndex(obj => obj.PrimaryKey == inPPI.PrimaryKey) != -1;
				if (m_FriendIcon.Visible != isFriend)
				{
					m_FriendIcon.Show(isFriend, true);
				}
			}

			if (m_PlatformSprite != null)
			{
				string platform;
				switch (inPPI.Platform)
				{
				case RuntimePlatform.Android:
					platform = "Plat_Andro";
					break;
				case RuntimePlatform.IPhonePlayer:
					platform = "Plat_Apple";
					break;
				case RuntimePlatform.WindowsPlayer:
					platform = "Plat_Pc";
					break;
				case RuntimePlatform.OSXPlayer:
					platform = "Plat_Mac";
					break;
				case RuntimePlatform.WindowsWebPlayer:
					platform = "Plat_Fb";
					break;
				case RuntimePlatform.OSXWebPlayer:
					platform = "Plat_Fb";
					break;
				default:
					platform = "Plat_Skull";
					break;
				}
				m_PlatformSprite.State = platform;
			}

			if (m_LineWidget != null)
			{
				m_LineWidget.Color = inBackground;
				m_LineWidget.SetModify();
			}
		}

		//--------------------------------------------------------------------------------------------------------------
		public void Show()
		{
			if (m_LineWidget.Visible == false)
			{
				m_LineWidget.ShowImmediate(true, true);
			}
		}

		//--------------------------------------------------------------------------------------------------------------
		public void Hide()
		{
			m_LineWidget.Show(false, true);
		}
	};

	//==================================================================================================================

	ChartLine[] m_Lines;
	E_Team m_FirstTeam;

	int m_PlayerPlace = -1;
	GUIBase_Sprite m_PlayerHighlighting;

	readonly static Color[] TeamColor = new Color[3]
	{
		new Color(30.0f/255, 100.0f/255, 100.0f/255), // E_Team.None	
		new Color(0.0f/255, 170.0f/255, 200.0f/255), // E_Team.Good
		new Color(190.0f/255, 15.0f/255, 30.0f/255)
	}; // E_Team.Bad

	public int PlayerPlace
	{
		get { return m_PlayerPlace; }
	}

	public override string ParentName
	{
		get { return "Table"; }
	}

	public override float UpdateInterval
	{
		get { return 0.5f; }
	}

	//==================================================================================================================

	//------------------------------------------------------------------------------------------------------------------
	protected override bool OnInit()
	{
		if (base.OnInit() == false)
		{
			return false;
		}

		InitLines();

		return true;
	}

	//------------------------------------------------------------------------------------------------------------------
	protected override void OnShow()
	{
		base.OnShow();

		m_PlayerPlace = -1;

		UpdateLines();
	}

	//------------------------------------------------------------------------------------------------------------------
	protected override void OnUpdate()
	{
		base.OnUpdate();

		UpdateLines();
	}

	//------------------------------------------------------------------------------------------------------------------
	protected override void OnHide()
	{
		base.OnHide();

		m_PlayerPlace = -1;
	}

	//------------------------------------------------------------------------------------------------------------------
	void InitLines()
	{
		GUIBase_List list = Parent.transform.GetComponent<GUIBase_List>() as GUIBase_List;

		if (list == null)
		{
			Debug.LogError("'GUIBase_List' in '" + Parent.name + "' not found!");
			return;
		}

		if (list.numOfLines <= 0)
		{
			Debug.LogError("Wrong number of lines in '" + Parent.name + "'!");
			return;
		}

		m_Lines = new ChartLine[list.numOfLines];

		for (int i = 0; i < list.numOfLines; ++i)
		{
			m_Lines[i] = new ChartLine(list.GetWidgetOnLine(i));
		}

		m_PlayerHighlighting = Parent.transform.FindChildByName("MyPlayer").GetComponent<GUIBase_Sprite>();

		UpdateHighlighiting(-1);
	}

	//------------------------------------------------------------------------------------------------------------------
	void UpdateLines()
	{
		if ((m_Lines == null) || (m_Lines.Length == 0))
			return;

		int highlight = -1;
		PlayerPersistantInfo local = PPIManager.Instance.GetLocalPlayerPPI();
		List<PlayerPersistantInfo> original = PPIManager.Instance.GetPPIList();
		List<PlayerPersistantInfo> sortedByTeam = new List<PlayerPersistantInfo>(original);
		List<PlayerPersistantInfo> sortedByScore = new List<PlayerPersistantInfo>(original);

		m_FirstTeam = local.Team;

		sortedByTeam.Sort(ComparePPIsByTeam);
		sortedByScore.Sort(ComparePPIsByScore);

		for (int i = 0; i < m_Lines.Length; ++i)
		{
			ChartLine line = m_Lines[i];

			if (i < sortedByTeam.Count)
			{
				PlayerPersistantInfo ppi = sortedByTeam[i];
				Color col = GetColor(ppi);
				int idx = GetIndex(ppi, sortedByScore) + 1;

				line.Show();
				line.Update(ppi, idx, col);

				if (ppi.Player == local.Player)
				{
					m_PlayerPlace = idx;
					highlight = i;
				}
			}
			else
			{
				line.Hide();
			}
		}

		UpdateHighlighiting(highlight);
	}

	//------------------------------------------------------------------------------------------------------------------
	Color GetColor(PlayerPersistantInfo inPPI)
	{
		Color col = TeamColor[(int)inPPI.Team];

		if (IsAlive(inPPI) == false)
		{
			col *= 0.5f;
		}

		return col;
	}

	//------------------------------------------------------------------------------------------------------------------
	int GetIndex(PlayerPersistantInfo inPPI, List<PlayerPersistantInfo> inPPIList)
	{
		for (int i = 0; i < inPPIList.Count; ++i)
		{
			if (inPPI.Player == inPPIList[i].Player)
				return i;
		}

		return -1;
	}

	//------------------------------------------------------------------------------------------------------------------
	int ComparePPIsByScore(PlayerPersistantInfo inA, PlayerPersistantInfo inB)
	{
		int res = inB.Score.Score.CompareTo(inA.Score.Score); // 1) descending by score

		if (res == 0)
		{
			res = inB.Score.Kills.CompareTo(inA.Score.Kills); // 2) descending by kills

			if (res == 0)
			{
				res = inA.Score.Deaths.CompareTo(inB.Score.Deaths); // 3) increasing by deaths

				if (res == 0)
				{
					res = inA.Name.CompareTo(inB.Name); // 4) increasing by names
				}
			}
		}

		return res;
	}

	//------------------------------------------------------------------------------------------------------------------
	int ComparePPIsByTeam(PlayerPersistantInfo inA, PlayerPersistantInfo inB)
	{
		int res = inA.Team.CompareTo(inB.Team);

		if (res == 0)
		{
			res = ComparePPIsByScore(inA, inB);
		}
		else
		{
			res = inA.Team == m_FirstTeam ? -1 : +1;
		}

		return res;
	}

	//------------------------------------------------------------------------------------------------------------------
	void UpdateHighlighiting(int inIndex)
	{
		if (m_PlayerHighlighting != null)
		{
			if (inIndex != -1)
			{
				m_PlayerHighlighting.transform.position = m_Lines[inIndex].Pos;
				m_PlayerHighlighting.Widget.SetModify();
				m_PlayerHighlighting.Widget.Show(true, true);
			}
			else
			{
				m_PlayerHighlighting.Widget.Show(false, true);
			}
		}
	}

	//------------------------------------------------------------------------------------------------------------------
	static bool IsAlive(PlayerPersistantInfo inPPI)
	{
		ComponentPlayer comp;

		if (Player.Players.TryGetValue(inPPI.Player, out comp) == true)
		{
			if ((comp != null) && (comp.Owner != null))
			{
				return comp.Owner.IsAlive;
			}
		}

		return false;
	}
}
