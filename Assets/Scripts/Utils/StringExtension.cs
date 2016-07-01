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
using System.Text;
using System.Globalization;
using System.Linq;
using Regex = System.Text.RegularExpressions.Regex;

public static class StringExtension
{
	public static string CollapseWhitespaces(this string str, bool trim)
	{
		str = Regex.Replace(str, @"\s+", " ");
		return trim ? str.Trim() : str;
	}

	public static string AsciiOnly(this string str, bool includeExtendedAscii)
	{
		int upperLimit = includeExtendedAscii ? 255 : 127;
		char[] asciiChars = str.Where(ch => (int)ch <= upperLimit).ToArray();
		return new string(asciiChars);
	}

	public static string RemoveDiacritics(this string str)
	{
		if (string.IsNullOrEmpty(str) == true)
			return string.Empty;

		string normalized = str.MFNormalize(NormalizationForm.FormD);
		StringBuilder builder = new StringBuilder();

		for (int idx = 0; idx < normalized.Length; ++idx)
		{
			UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(normalized[idx]);
			if (category != UnicodeCategory.NonSpacingMark)
			{
				builder.Append(normalized[idx]);
			}
		}

		return builder.ToString().MFNormalize(NormalizationForm.FormC);
	}

	public static string FilterSwearWords(this string str, bool precise = false)
	{
		return SwearWords.Filter(str, precise);
	}
}
