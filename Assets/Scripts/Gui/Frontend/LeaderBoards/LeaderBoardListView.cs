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

public class LeaderBoardListView : BaseListView
{
	internal class GuiLine
	{
		Color HIGHLIGHT_COLOR = new Color(170.0f/255, 200.0f/255, 30.0f/255);
		Color DEFAULT_COLOR = Color.white;

		GUIBase_Widget m_Line;

		GUIBase_Label m_Order;
		GUIBase_Label m_RankText;
		GUIBase_MultiSprite m_RankIcon;
		GUIBase_Widget m_FriendIcon;
		GUIBase_Label m_DisplayName;
		GUIBase_Label m_Score;
		GUIBase_Widget m_ScoreBarBg;
		GUIBase_Widget m_ScoreBarFg;
		GUIBase_Widget m_FacebookFriendIcon;

		//public Vector3 spritePos { get { return m_Line.transform.position; } }

		public GuiLine(GUIBase_Widget inLine)
		{
			Transform trans = inLine.transform;

			m_Line = inLine;
			m_Order = trans.GetChildComponent<GUIBase_Label>("Order");
			m_RankText = trans.GetChildComponent<GUIBase_Label>("TextRank");
			m_RankIcon = trans.GetChildComponent<GUIBase_MultiSprite>("PlayerRankPic");
			m_FriendIcon = trans.GetChildComponent<GUIBase_Widget>("FriendIcon");
			m_FacebookFriendIcon = trans.GetChildComponent<GUIBase_Widget>("FacebookFriendIcon");
			m_DisplayName = trans.GetChildComponent<GUIBase_Label>("Nickname");
			m_Score = trans.GetChildComponent<GUIBase_Label>("Score");
			m_ScoreBarBg = trans.GetChildComponent<GUIBase_Widget>("ScoreBarBg");
			m_ScoreBarFg = trans.GetChildComponent<GUIBase_Widget>("ScoreBarFg");
		}

		public void Update(LeaderBoard.Row row, float normalizedScore, bool isFriend, bool inHighlightPlayer, string facebookName = null)
		{
			int xp = row.Experience > 0 ? row.Experience : row.Score;
			int rank = PlayerPersistantInfo.GetPlayerRankFromExperience(xp);

			string displayname = string.IsNullOrEmpty(row.DisplayName) ? row.PrimaryKey : GuiBaseUtils.FixNameForGui(row.DisplayName);
			if (facebookName != null)
				displayname = GuiBaseUtils.FixNickname(facebookName + " (" + displayname + ")", displayname, false);

			m_Order.SetNewText(row.Order < 0 ? "" : row.Order.ToString());
			m_RankText.SetNewText(rank <= 0 ? "" : rank.ToString());
			m_DisplayName.SetNewText(displayname);
			m_Score.SetNewText(row.Score < 0 ? "" : row.Score.ToString("N0"));

			string rankState = string.Format("Rank_{0}", Mathf.Min(rank, m_RankIcon.Count - 1).ToString("D2"));
			m_RankIcon.State = rank <= 0 ? GUIBase_MultiSprite.DefaultState : rankState;

			SetProgress(normalizedScore);

			m_Order.Widget.Color = inHighlightPlayer ? HIGHLIGHT_COLOR : DEFAULT_COLOR;
			m_DisplayName.Widget.Color = inHighlightPlayer ? HIGHLIGHT_COLOR : DEFAULT_COLOR;
			m_Score.Widget.Color = inHighlightPlayer ? HIGHLIGHT_COLOR : DEFAULT_COLOR;

			m_FriendIcon.FadeAlpha = isFriend && (facebookName == null) ? 1.0f : 0.0f;
			m_FacebookFriendIcon.FadeAlpha = facebookName != null ? 1.0f : 0.0f;
		}

		public void Show()
		{
			if (m_Line.Visible == false)
			{
				m_Line.Show(true, true);
			}
		}

		public void Hide()
		{
			m_Line.Show(false, true);
		}

		void SetProgress(float current)
		{
			if (current > 0.0f)
			{
				Transform emptyTrans = m_ScoreBarBg.transform;
				Vector3 pos = emptyTrans.localPosition;
				Vector3 scale = emptyTrans.localScale;
				float width = m_ScoreBarBg.GetWidth();
				float ratio = Mathf.Clamp(0.1f + current*0.9f, 0.1f, 1.0f);

				pos.x += (width - width*ratio)*scale.x*0.5f;
				scale.x *= ratio;

				Transform fullTrans = m_ScoreBarFg.transform;
				fullTrans.localScale = scale;
				fullTrans.localPosition = pos;
				m_ScoreBarFg.SetModify();
			}

			m_ScoreBarBg.Show(current > 0.0f ? true : false, true);
			m_ScoreBarFg.Show(current > 0.0f ? true : false, true);
		}
	};

	// -------------------------------------------------------------------------------------------------------------------------
	GuiLine[] m_GuiLines;
	GUIBase_Layout m_View;
	LeaderBoard m_ActiveLeaderBoard;
	GUIBase_List m_Table;

	bool isUpdateNeccesary { get; set; }
	int[] m_RowMapping;

	// =========================================================================================================================
	// === public interface ====================================================================================================
	public void GUIView_Init(GUIBase_Layout inView, GUIBase_List inList)
	{
		m_View = inView;
		m_Table = GuiBaseUtils.GetControl<GUIBase_List>(m_View, "Table");
		InitGuiLines(inList);
	}

	public override void GUIView_Show()
	{
		SetActiveLeaderBoard(m_ActiveLeaderBoard, true);

		m_Table.OnUpdateRow += OnUpdateTableRow;

		MFGuiManager.Instance.ShowLayout(m_View, true);
	}

	public override void GUIView_Hide()
	{
		MFGuiManager.Instance.ShowLayout(m_View, false);

		m_Table.OnUpdateRow -= OnUpdateTableRow;

		m_ActiveLeaderBoard = null;
	}

	public override void GUIView_Update()
	{
		if (isUpdateNeccesary == false || m_ActiveLeaderBoard == null)
			return;

		UpdateView();
		isUpdateNeccesary = false;
	}

	public void SetActiveLeaderBoard(LeaderBoard inActiveLeaderBoard, bool force)
	{
		if (force == false && inActiveLeaderBoard == m_ActiveLeaderBoard)
			return;

		m_ActiveLeaderBoard = inActiveLeaderBoard;
		m_ActiveLeaderBoard.FetchAndUpdate(CloudUser.instance.primaryKey, this, LeaderBaordFetchFinished);
		isUpdateNeccesary = true;
	}

	// =========================================================================================================================
	// === MonoBehaviour interface =============================================================================================

	// !!!!!! DON"T USE Standard MonoBehaviour Update functions. This view is controled from parrent object...

	// =========================================================================================================================
	// === private part ========================================================================================================
	void InitGuiLines(GUIBase_List inParent)
	{
		if (inParent.numOfLines <= 0)
		{
			Debug.LogError("inParent.numOfLines = " + inParent.numOfLines);
			return;
		}

		m_GuiLines = new GuiLine[inParent.numOfLines];
		m_RowMapping = new int[inParent.numOfLines];
		for (int i = 0; i < inParent.numOfLines; i++)
		{
			m_GuiLines[i] = new GuiLine(inParent.GetWidgetOnLine(i));
			m_RowMapping[i] = i;
		}
	}

	void UpdateView()
	{
		LeaderBoard.Row maxScoreRow = m_ActiveLeaderBoard[0];
		float maxScore = (maxScoreRow != null) && (maxScoreRow.Score > 0) ? Mathf.Exp(Mathf.Log10((float)maxScoreRow.Score)) : 1.0f;

		var friends = GameCloudManager.friendList.friends;
		for (uint i = 0; i < m_GuiLines.Length; i++)
		{
			uint index = i;
			if (m_RowMapping != null)
				index = (uint)m_RowMapping[i];

			LeaderBoard.Row row = m_ActiveLeaderBoard[index];
			if (row != null)
			{
				float score = row.Score > 0 ? Mathf.Exp(Mathf.Log10((float)row.Score)) : 0.0f;

				bool isFriend = friends.Find(obj => obj.PrimaryKey == row.PrimaryKey) != null;
				string facebookName = null;

				m_GuiLines[i].Show();
				m_GuiLines[i].Update(row, score/maxScore, isFriend, row.LocalUser, facebookName);
			}
			else
			{
				m_GuiLines[i].Hide();
			}
		}

		m_Table.MaxItems = m_ActiveLeaderBoard.RowCount();
		m_Table.Widget.SetModify();
	}

	void LeaderBaordFetchFinished()
	{
		isUpdateNeccesary = true;
	}

	void OnUpdateTableRow(GUIBase_Widget widget, int rowIndex, int itemIndex)
	{
		m_RowMapping[rowIndex] = itemIndex;
		UpdateView();
	}
}
