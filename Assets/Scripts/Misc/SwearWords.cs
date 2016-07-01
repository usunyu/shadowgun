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
using Regex = System.Text.RegularExpressions.Regex;
using RegexOptions = System.Text.RegularExpressions.RegexOptions;

public static class SwearWords
{
	// PRIVATE MEMBERS

	static string[] m_Replacements = null;
	static Regex[] m_RegexRules = null;
	static string[] m_Dictionary =
	{
		"bitch",
		"cock",
		"cunt",
		"dick",
		"fuck",
		"penis",
		"piss",
		"porn",
		"sex",
		"shit",
		"suck",
		"tits",
		"vagina"
	};

	// PUBLIC MEMBERS

	public static string[] Dictionary
	{
		get { return m_Dictionary; }
		set { SetDictionary(value); }
	}

	// PUBLIC METHODS

	public static string Filter(string text, bool precise = false)
	{
		if (precise == true)
		{
			return FilterPrecise(text);
		}
		else
		{
			return FilterFast(text);
		}
	}

	// PRIVATE METHODS

	static string FilterFast(string text)
	{
		PrepareData();

		//float xxx = Time.realtimeSinceStartup;

		// filter text
		string filteredText = text;
		for (int idx = 0; idx < m_Dictionary.Length; ++idx)
		{
			filteredText = Replace(filteredText, m_Dictionary[idx], ref m_Replacements[idx], 1, -1, false);
		}

		//Debug.Log(">>> length=" + text.Length + ", " + ((Time.realtimeSinceStartup - xxx) * 1000).ToString("0.00") + "ms");

		// done
		return filteredText;
	}

	static string FilterPrecise(string text)
	{
		PrepareData();

		for (int idx = 0; idx < m_RegexRules.Length; ++idx)
		{
			text = m_RegexRules[idx].Replace(text, m_Replacements[idx]);
		}

		return text;
	}

	static void SetDictionary(string[] dictionary)
	{
		if (dictionary == null)
			return;
		if (dictionary.Length == 0)
			return;

		m_Dictionary = dictionary;
		m_Replacements = null;
		m_RegexRules = null;

		PrepareData();
	}

	static void PrepareData()
	{
		if (m_RegexRules == null || m_Replacements == null)
		{
			m_Replacements = new string[m_Dictionary.Length];
			m_RegexRules = new Regex[m_Dictionary.Length];
			for (int idx = 0; idx < m_Dictionary.Length; ++idx)
			{
				string word = m_Dictionary[idx];
				string regex = "";
				foreach (var ch in word)
				{
					regex += ch + @"+\s?";
				}

				m_Replacements[idx] = new string('#', word.Length);
				m_RegexRules[idx] = new Regex(regex, RegexOptions.IgnoreCase);
			}
		}
	}

	static string Replace(string Text, string Find, ref string Replacement, int Start = 1, int Count = -1, bool CaseSensitive = true)
	{
		//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		// Don't replace this! Creative Common
		// This CODE is part of .NET CMS: http://cmsaspnet.com/ the best .net cms! Download now, is "portable"!
		// SPONSOR: The hi performance CMS+forum software database-less in html5
		//This message is part of the license. You can edit or convert this code without removing the message.
		//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

		if (Start != 1)
		{
			Text = Text.Substring(Start - 1);
		}
		if (Text != null && Find != null)
		{
			if (CaseSensitive)
			{
				int P = 0;
				//P Is base 0, Start is base 1 
				int C = 1;
				do
				{
					P = Text.IndexOf(Find, P);
					if (P != -1)
					{
						Text = Text.Substring(0, P) + Replacement + Text.Substring(P + Find.Length);
						if (Count != -1 && Count == C)
						{
							break;
						}
					}
					else
					{
						break;
					}
					C += 1;
					P += 1;
				} while (true);
				return Text;
			}
			else
			{
				Find = Find.ToLower();
				int difference = Replacement.Length - Find.Length;
				int TotDifference = 0;
				string TextLow = Text.ToLower();
				int P = 0;
				//P Is base 0, Start is base 1 
				int NewP = 0;
				int C = 1;
				do
				{
					P = TextLow.IndexOf(Find, P);
					if (P != -1)
					{
						NewP = P + TotDifference;
						Text = Text.Substring(0, NewP) + Replacement + Text.Substring(NewP + Find.Length);
						TotDifference += difference;
						if (Count != -1 && Count == C)
						{
							break;
						}
					}
					else
					{
						break;
					}
					C += 1;
					P += 1;
				} while (true);
				return Text;
			}
		}

		return null;
	}
}
