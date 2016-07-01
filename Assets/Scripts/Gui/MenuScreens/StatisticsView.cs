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

// =============================================================================================================================
// =============================================================================================================================
public class StatisticsView : BaseListView
{
	internal class StatisticsLine
	{
		Color HIGHLIGHT_COLOR = new Color(170.0f/255, 200.0f/255, 30.0f/255);
		Color DEFAULT_COLOR = Color.white;

		GUIBase_Widget m_Line;

		GUIBase_Label m_Name;
		GUIBase_Label m_PlayerValue;
		GUIBase_Label m_FriendValue;
		GUIBase_Label m_FriendName;

		public Vector3 spritePos
		{
			get { return m_Line.transform.position; }
		}

		public StatisticsLine(GUIBase_Widget inLine)
		{
			m_Line = inLine;
			m_Name = inLine.transform.GetChildComponent<GUIBase_Label>("01_name");
			m_PlayerValue = inLine.transform.GetChildComponent<GUIBase_Label>("02_player_value");
			m_FriendValue = inLine.transform.GetChildComponent<GUIBase_Label>("03_friend_value");
			m_FriendName = inLine.transform.GetChildComponent<GUIBase_Label>("04_friend_name");
		}

		public void Update(Statistics.Item inItem, Statistics.E_Mode inStatisticMode)
		{
			bool friend = inStatisticMode == Statistics.E_Mode.CompareWithFriend;
			bool best = inStatisticMode == Statistics.E_Mode.CompareWithBest;
			m_FriendName.Widget.Show(best, true);
			m_FriendValue.Widget.Show(friend || best, true);

			if (inItem is Statistics.IntItem)
			{
				Update(inItem as Statistics.IntItem, inStatisticMode);
			}
			else if (inItem is Statistics.FloatItem)
			{
				Update(inItem as Statistics.FloatItem, inStatisticMode);
			}
			else if (inItem is Statistics.StringItem)
			{
				Update(inItem as Statistics.StringItem, inStatisticMode);
			}
			else
			{
				Debug.LogWarning("Unknown Statistics item type" + inItem.GetType().Name);
			}
		}

		public void Show()
		{
			m_Line.Show(true, true);
		}

		public void Hide()
		{
			m_Line.Show(false, true);
		}

		// ---------------------------------------------------------------------------------------------------------------------		
		void Update(Statistics.IntItem inItem, Statistics.E_Mode inStatisticMode)
		{
			if (inItem.m_NameIndex == 0 && string.IsNullOrEmpty(inItem.m_NameText) == false)
			{
				UpdateLine(inItem.m_NameText,
						   inItem.m_PlayerValue.ToString(),
						   inItem.m_SecondValue.ToString(),
						   inItem.m_SecondValueFriendName,
						   inItem.m_HighlightPlayer,
						   inItem.m_HighlightFriend);
			}
			else
			{
				UpdateLine(inItem.m_NameIndex,
						   inItem.m_PlayerValue.ToString(),
						   inItem.m_SecondValue.ToString(),
						   inItem.m_SecondValueFriendName,
						   inItem.m_HighlightPlayer,
						   inItem.m_HighlightFriend);
			}
		}

		void Update(Statistics.FloatItem inItem, Statistics.E_Mode inStatisticMode)
		{
			if (inItem.m_NameIndex == 0 && string.IsNullOrEmpty(inItem.m_NameText) == false)
			{
				UpdateLine(inItem.m_NameText,
						   inItem.m_PlayerValue.ToString(),
						   inItem.m_SecondValue.ToString(),
						   inItem.m_SecondValueFriendName,
						   inItem.m_HighlightPlayer,
						   inItem.m_HighlightFriend);
			}
			else
			{
				UpdateLine(inItem.m_NameIndex,
						   inItem.m_PlayerValue.ToString(),
						   inItem.m_SecondValue.ToString(),
						   inItem.m_SecondValueFriendName,
						   inItem.m_HighlightPlayer,
						   inItem.m_HighlightFriend);
			}
		}

		void Update(Statistics.StringItem inItem, Statistics.E_Mode inStatisticMode)
		{
			if (inItem.m_NameIndex == 0 && string.IsNullOrEmpty(inItem.m_NameText) == false)
			{
				UpdateLine(inItem.m_NameText,
						   inItem.m_PlayerValue,
						   inItem.m_SecondValue,
						   inItem.m_SecondValueFriendName,
						   inItem.m_HighlightPlayer,
						   inItem.m_HighlightFriend);
			}
			else
			{
				UpdateLine(inItem.m_NameIndex,
						   inItem.m_PlayerValue,
						   inItem.m_SecondValue,
						   inItem.m_SecondValueFriendName,
						   inItem.m_HighlightPlayer,
						   inItem.m_HighlightFriend);
			}
		}

		void UpdateLine(string inText, string inVal1, string inVal2, string inVal3, bool inHighlightPlayer, bool inHighlightFriend)
		{
			m_Name.SetNewText(inText);
			m_PlayerValue.SetNewText(inVal1);
			m_FriendValue.SetNewText(inVal2);
			m_FriendName.SetNewText(inVal3);

			m_PlayerValue.Widget.Color = inHighlightPlayer ? HIGHLIGHT_COLOR : DEFAULT_COLOR;
			m_FriendValue.Widget.Color = inHighlightFriend ? HIGHLIGHT_COLOR : DEFAULT_COLOR;
		}

		void UpdateLine(int inTextID, string inVal1, string inVal2, string inVal3, bool inHighlightPlayer, bool inHighlightFriend)
		{
			m_Name.SetNewText(inTextID);
			m_PlayerValue.SetNewText(inVal1);
			m_FriendValue.SetNewText(inVal2);
			m_FriendName.SetNewText(inVal3);

			m_PlayerValue.Widget.Color = inHighlightPlayer ? HIGHLIGHT_COLOR : DEFAULT_COLOR;
			m_FriendValue.Widget.Color = inHighlightFriend ? HIGHLIGHT_COLOR : DEFAULT_COLOR;
		}
	};

	// -------------------------------------------------------------------------------------------------------------------------
	StatisticsLine[] m_GuiLines;
	GUIBase_Button m_PrevButton;
	GUIBase_Button m_NextButton;
	GUIBase_Layout m_View;

	Statistics m_Statistics = new Statistics();
	Statistics.E_Mode m_Mode = Statistics.E_Mode.Player;
	string m_FriendName = string.Empty;

	int m_FirstVisibleIndex = 0;
	bool isUpdateNeccesary { get; set; }

	// =========================================================================================================================
	// === public interface ====================================================================================================
	public void GUIView_Init(GUIBase_Layout inView, GUIBase_List inList, GUIBase_Button inPrev, GUIBase_Button inNext)
	{
		m_View = inView;
		m_PrevButton = inPrev;
		m_NextButton = inNext;

		InitGuiLines(inList);
	}

	public override void GUIView_Show()
	{
		m_Statistics.Clear();

		isUpdateNeccesary = true;

		// set delegates to prev and next buttons, 
		// we have to do it in every show event because 
		// this two buttons are shared with different views... 
		m_PrevButton.RegisterReleaseDelegate2(Delegate_Prev);
		m_NextButton.RegisterReleaseDelegate2(Delegate_Next);

		MFGuiManager.Instance.ShowLayout(m_View, true);
	}

	public override void GUIView_Hide()
	{
		MFGuiManager.Instance.ShowLayout(m_View, false);
	}

	public override void GUIView_Update()
	{
		if (isUpdateNeccesary == false)
			return;

		UpdateView();
		isUpdateNeccesary = false;
	}

	public void SetStatisticsMode(Statistics.E_Mode inMode, string inFriendName)
	{
		isUpdateNeccesary |= inMode != m_Mode;
		isUpdateNeccesary |= inFriendName != m_FriendName;

		m_Mode = inMode;
		m_FriendName = inFriendName;
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

		m_GuiLines = new StatisticsLine[inParent.numOfLines];
		for (int i = 0; i < inParent.numOfLines; i++)
		{
			m_GuiLines[i] = new StatisticsLine(inParent.GetWidgetOnLine(i));
		}
	}

	void UpdateView()
	{
		m_Statistics.PrepareFor(m_Mode, m_FriendName);

		for (int i = 0; i < m_GuiLines.Length; i++)
		{
			int sIndex = m_FirstVisibleIndex + i;

			if (sIndex < m_Statistics.Count)
			{
				m_GuiLines[i].Show();
				//m_GuiLines[i].Update(m_Statistics[sIndex]);
				m_GuiLines[i].Update(m_Statistics.GetItem(sIndex), m_Statistics.Mode);
			}
			else
			{
				m_GuiLines[i].Hide();
			}
		}

		m_PrevButton.SetDisabled(m_FirstVisibleIndex == 0);
		m_NextButton.SetDisabled((m_FirstVisibleIndex + m_GuiLines.Length) >= m_Statistics.Count);
	}

	void OnFriendListChanged(object sender, System.EventArgs e)
	{
		isUpdateNeccesary = true;
	}

	// =========================================================================================================================
	// === internal gui delegates ==============================================================================================
	void Delegate_Prev(GUIBase_Widget inInstigator)
	{
		m_FirstVisibleIndex -= m_GuiLines.Length;
		isUpdateNeccesary = true;

		// debug code...
		if (m_FirstVisibleIndex < 0)
		{
			m_FirstVisibleIndex = 0;
			Debug.LogError("Internal error, inform alex");
		}
	}

	void Delegate_Next(GUIBase_Widget inInstigator)
	{
		m_FirstVisibleIndex += m_GuiLines.Length;
		isUpdateNeccesary = true;

		// debug code...		
		if ((m_FirstVisibleIndex%m_GuiLines.Length) != 0 || m_FirstVisibleIndex >= m_Statistics.Count)
		{
			m_FirstVisibleIndex = m_GuiLines.Length*((int)(m_Statistics.Count/m_GuiLines.Length));
			Debug.LogError("Internal error, inform alex");
		}
	}
}
