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

// =====================================================================================================================
public class News : MonoBehaviour
{
	// .................................................................................................................
	public class HeadLine
	{
		public enum E_Importence
		{
			Low,
			Medium,
			High
		}

		public string Name { get; private set; }
		public string Text { get; private set; }
		public E_Importence Importence { get; private set; }

		public HeadLine(string inHeadLineName, string inHeadLineText, E_Importence inImportence)
		{
			Name = inHeadLineName;
			Text = inHeadLineText;
			Importence = inImportence;
		}
	}

	// .................................................................................................................
	public struct Message
	{
		// TODO ...
	}

	// .................................................................................................................
	// private membesrs
	List<HeadLine> m_HeadLines = new List<HeadLine>();
	int m_CurrentHeadLine = 0;

	// =================================================================================================================
	// === public interface ============================================================================================
	public HeadLine GetNextHeadLine()
	{
		m_CurrentHeadLine++;

		if (m_CurrentHeadLine >= m_HeadLines.Count)
		{
			m_CurrentHeadLine = -1;
			return null;
		}

		return m_HeadLines[m_CurrentHeadLine];
	}

	public void SetHeadLineText(string inHeadLineName, string inHeadLineText, HeadLine.E_Importence inImportence)
	{
		HeadLine headLine = new HeadLine(inHeadLineName, inHeadLineText, inImportence);

		if (string.IsNullOrEmpty(inHeadLineName) == false)
		{
			int index = m_HeadLines.FindIndex(x => x.Name == inHeadLineName);
			if (index >= 0)
			{
				m_HeadLines[index] = headLine;
				return;
			}
		}

		m_HeadLines.Add(headLine);
	}

	public void ProcessMessage(CloudMailbox.NewsMessage inMessage)
	{
		if (string.IsNullOrEmpty(inMessage.m_HeadLine) == true)
			return;

		if (inMessage.m_ExpirationTime < CloudDateTime.UtcNow)
			return;

		SetHeadLineText(null, inMessage.m_HeadLine, HeadLine.E_Importence.Medium);
	}
}
