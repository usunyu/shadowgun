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

using System;
using System.Globalization;
using System.Text;
using System.Runtime.CompilerServices;
using NUtil = Madfinger.Mono.Globalization.Unicode.NormalizationTableUtil;

namespace Madfinger.Mono.Globalization.Unicode
{
	public enum NormalizationCheck
	{
		Yes,
		No,
		Maybe
	}

	public class Normalization
	{
		public const int NoNfd = 1;
		public const int NoNfkd = 2;
		public const int NoNfc = 4;
		public const int MaybeNfc = 8;
		public const int NoNfkc = 16;
		public const int MaybeNfkc = 32;
		public const int FullCompositionExclusion = 64;
		public const int IsUnsafe = 128;
//		public const int ExpandOnNfd = 256;
//		public const int ExpandOnNfc = 512;
//		public const int ExpandOnNfkd = 1024;
//		public const int ExpandOnNfkc = 2048;

		static uint PropValue(int cp)
		{
			return props[NUtil.PropIdx(cp)];
		}

		static int CharMapIdx(int cp)
		{
			return charMapIndex[NUtil.MapIdx(cp)];
		}

		static byte GetCombiningClass(int c)
		{
			return combiningClass[NUtil.Combining.ToIndex(c)];
		}

		static int GetPrimaryCompositeFromMapIndex(int src)
		{
			return mapIdxToComposite[NUtil.Composite.ToIndex(src)];
		}

		static int GetPrimaryCompositeHelperIndex(int cp)
		{
			return helperIndex[NUtil.Helper.ToIndex(cp)];
		}

		static string Compose(string source, int checkType)
		{
			StringBuilder sb = null;
			// Decompose to NFD or NKFD depending on our target
			Decompose(source, ref sb, checkType == 2 ? 3 : 1);
			if (sb == null)
				sb = Combine(source, 0, checkType);
			else
				Combine(sb, 0, checkType);

			return sb != null ? sb.ToString() : source;
		}

		static StringBuilder Combine(string source, int start, int checkType)
		{
			for (int i = 0; i < source.Length; i++)
			{
				if (QuickCheck(source[i], checkType) == NormalizationCheck.Yes)
					continue;
				StringBuilder sb = new StringBuilder(source.Length + source.Length/10);
				sb.Append(source);
				Combine(sb, i, checkType);
				return sb;
			}
			return null;
		}

/*
		private static bool CanBePrimaryComposite (int i)
		{
			if (i >= 0x3400 && i <= 0x9FBB)
				return GetPrimaryCompositeHelperIndex (i) != 0;
			return (PropValue (i) & IsUnsafe) != 0;
		}
*/

		static void Combine(StringBuilder sb, int i, int checkType)
		{
			// Back off one character as we may be looking at a V or T jamo.
			CombineHangul(sb, null, i > 0 ? i - 1 : i);

			while (i < sb.Length)
			{
				if (QuickCheck(sb[i], checkType) == NormalizationCheck.Yes)
				{
					i++;
					continue;
				}

				i = TryComposeWithPreviousStarter(sb, null, i);
			}
		}

		static int CombineHangul(StringBuilder sb, string s, int current)
		{
			int length = sb != null ? sb.Length : s.Length;
			int last = Fetch(sb, s, current);

			for (int i = current + 1; i < length; ++i)
			{
				int ch = Fetch(sb, s, i);

				// 1. check to see if two current characters are L and V

				int LIndex = last - HangulLBase;
				if (0 <= LIndex && LIndex < HangulLCount)
				{
					int VIndex = ch - HangulVBase;
					if (0 <= VIndex && VIndex < HangulVCount)
					{
						if (sb == null)
							return -1;

						// make syllable of form LV

						last = HangulSBase + (LIndex*HangulVCount + VIndex)*HangulTCount;

						sb[i - 1] = (char)last; // reset last
						sb.Remove(i, 1);
						i--;
						length--;
						continue; // discard ch
					}
				}

				// 2. check to see if two current characters are LV and T

				int SIndex = last - HangulSBase;
				if (0 <= SIndex && SIndex < HangulSCount && (SIndex%HangulTCount) == 0)
				{
					int TIndex = ch - HangulTBase;
					if (0 < TIndex && TIndex < HangulTCount)
					{
						if (sb == null)
							return -1;

						// make syllable of form LVT

						last += TIndex;

						sb[i - 1] = (char)last; // reset last
						sb.Remove(i, 1);
						i--;
						length--;
						continue; // discard ch
					}
				}
				// if neither case was true, just add the character
				last = ch;
			}

			return length;
		}

		static int Fetch(StringBuilder sb, string s, int i)
		{
			return (int)(sb != null ? sb[i] : s[i]);
		}

		// Cf. figure 7, section 1.3 of http://unicode.org/reports/tr15/.
		static int TryComposeWithPreviousStarter(StringBuilder sb, string s, int current)
		{
			// Backtrack to previous starter.
			int i = current - 1;
			if (GetCombiningClass(Fetch(sb, s, current)) == 0)
			{
				if (i < 0 || GetCombiningClass(Fetch(sb, s, i)) != 0)
					return current + 1;
			}
			else
			{
				while (i >= 0 && GetCombiningClass(Fetch(sb, s, i)) != 0)
					i--;
				if (i < 0)
					return current + 1;
			}

			int starter = Fetch(sb, s, i);

			// The various decompositions involving starter follow this index.
			int comp_idx = GetPrimaryCompositeHelperIndex(starter);
			if (comp_idx == 0)
				return current + 1;

			int length = (sb != null ? sb.Length : s.Length);
			int prevCombiningClass = -1;
			for (int j = i + 1; j < length; j++)
			{
				int candidate = Fetch(sb, s, j);

				int combiningClass = GetCombiningClass(candidate);
				if (combiningClass == prevCombiningClass)
								// We skipped over a guy with the same class, without
								// combining.  Skip this one, too.
					continue;

				int composed = TryCompose(comp_idx, starter, candidate);
				if (composed != 0)
				{
					if (sb == null)
									// Not normalized, and we are only checking.
						return -1;

					// Full Unicode warning: This will break when the underlying
					// tables are extended.
					sb[i] = (char)composed;
					sb.Remove(j, 1);

					return current;
				}

				// Gray box.  We're done.
				if (combiningClass == 0)
					return j + 1;

				prevCombiningClass = combiningClass;
			}

			return length;
		}

		static int TryCompose(int i, int starter, int candidate)
		{
			while (mappedChars[i] == starter)
			{
				if (mappedChars[i + 1] == candidate &&
					mappedChars[i + 2] == 0)
				{
					int composed = GetPrimaryCompositeFromMapIndex(i);

					if ((PropValue(composed) & FullCompositionExclusion) == 0)
						return composed;
				}

				// Skip this entry.
				while (mappedChars[i] != 0)
					i++;
				i++;
			}

			return 0;
		}

		static string Decompose(string source, int checkType)
		{
			StringBuilder sb = null;
			Decompose(source, ref sb, checkType);
			return sb != null ? sb.ToString() : source;
		}

		static void Decompose(string source,
							  ref StringBuilder sb,
							  int checkType)
		{
			int[] buf = null;
			int start = 0;
			for (int i = 0; i < source.Length; i++)
				if (QuickCheck(source[i], checkType) == NormalizationCheck.No)
					DecomposeChar(ref sb,
								  ref buf,
								  source,
								  i,
								  checkType,
								  ref start);
			if (sb != null)
				sb.Append(source, start, source.Length - start);
			ReorderCanonical(source, ref sb, 1);
		}

		static void ReorderCanonical(string src, ref StringBuilder sb, int start)
		{
			if (sb == null)
			{
				// check only with src.
				for (int i = 1; i < src.Length; i++)
				{
					int level = GetCombiningClass(src[i]);
					if (level == 0)
						continue;
					if (GetCombiningClass(src[i - 1]) > level)
					{
						sb = new StringBuilder(src.Length);
						sb.Append(src, 0, src.Length);
						ReorderCanonical(src, ref sb, i);
						return;
					}
				}
				return;
			}
			// check only with sb
			for (int i = start; i < sb.Length;)
			{
				int level = GetCombiningClass(sb[i]);
				if (level == 0 || GetCombiningClass(sb[i - 1]) <= level)
				{
					i++;
					continue;
				}

				char c = sb[i - 1];
				sb[i - 1] = sb[i];
				sb[i] = c;
				// Apply recursively.
				if (i > 1)
					i--;
			}
		}

		static void DecomposeChar(ref StringBuilder sb,
								  ref int[] buf,
								  string s,
								  int i,
								  int checkType,
								  ref int start)
		{
			if (sb == null)
				sb = new StringBuilder(s.Length + 100);
			sb.Append(s, start, i - start);
			if (buf == null)
				buf = new int[19];
			int n = GetCanonical(s[i], buf, 0, checkType);
			for (int x = 0; x < n; x++)
			{
				if (buf[x] < char.MaxValue)
					sb.Append((char)buf[x]);
				else
				{
					// surrogate
					sb.Append((char)(buf[x] >> 10 + 0xD800));
					sb.Append((char)((buf[x] & 0x0FFF) + 0xDC00));
				}
			}
			start = i + 1;
		}

		public static NormalizationCheck QuickCheck(char c, int type)
		{
			uint v;
			switch (type)
			{
			default: // NFC
				v = PropValue((int)c);
				return (v & NoNfc) == 0
									   ? (v & MaybeNfc) == 0
														 ? NormalizationCheck.Yes
														 : NormalizationCheck.Maybe
									   : NormalizationCheck.No;
			case 1: // NFD
				if ('\uAC00' <= c && c <= '\uD7A3')
					return NormalizationCheck.No;
				return (PropValue((int)c) & NoNfd) != 0
									   ? NormalizationCheck.No
									   : NormalizationCheck.Yes;
			case 2: // NFKC
				v = PropValue((int)c);
				return (v & NoNfkc) != 0
									   ? NormalizationCheck.No
									   : (v & MaybeNfkc) != 0
														 ? NormalizationCheck.Maybe
														 : NormalizationCheck.Yes;
			case 3: // NFKD
				if ('\uAC00' <= c && c <= '\uD7A3')
					return NormalizationCheck.No;
				return (PropValue((int)c) & NoNfkd) != 0
									   ? NormalizationCheck.No
									   : NormalizationCheck.Yes;
			}
		}

		/* for now we don't use FC_NFKC closure
		public static bool IsMultiForm (char c)
		{
			return (PropValue ((int) c) & 0xF0000000) != 0;
		}

		public static char SingleForm (char c)
		{
			uint v = PropValue ((int) c);
			int idx = (int) ((v & 0x7FFF0000) >> 16);
			return (char) singleNorm [idx];
		}

		public static void MultiForm (char c, char [] buf, int index)
		{
			// FIXME: handle surrogate
			uint v = PropValue ((int) c);
			int midx = (int) ((v & 0x7FFF0000) >> 16);
			buf [index] = (char) multiNorm [midx];
			buf [index + 1] = (char) multiNorm [midx + 1];
			buf [index + 2] = (char) multiNorm [midx + 2];
			buf [index + 3] = (char) multiNorm [midx + 3];
			if (buf [index + 3] != 0)
				buf [index + 4] = (char) 0; // zero termination
		}
		*/

		const int HangulSBase = 0xAC00,
				  HangulLBase = 0x1100,
				  HangulVBase = 0x1161,
				  HangulTBase = 0x11A7,
				  HangulLCount = 19,
				  HangulVCount = 21,
				  HangulTCount = 28,
				  HangulNCount = HangulVCount*HangulTCount,
				  // 588
				  HangulSCount = HangulLCount*HangulNCount; // 11172

		static int GetCanonicalHangul(int s, int[] buf, int bufIdx)
		{
			int idx = s - HangulSBase;
			if (idx < 0 || idx >= HangulSCount)
			{
				return bufIdx;
			}

			int L = HangulLBase + idx/HangulNCount;
			int V = HangulVBase + (idx%HangulNCount)/HangulTCount;
			int T = HangulTBase + idx%HangulTCount;

			buf[bufIdx++] = L;
			buf[bufIdx++] = V;
			if (T != HangulTBase)
			{
				buf[bufIdx++] = T;
			}
			buf[bufIdx] = (char)0;
			return bufIdx;
		}

		static int GetCanonical(int c, int[] buf, int bufIdx, int checkType)
		{
			int newBufIdx = GetCanonicalHangul(c, buf, bufIdx);
			if (newBufIdx > bufIdx)
				return newBufIdx;

			int i = CharMapIdx(c);
			if (i == 0 || mappedChars[i] == c)
				buf[bufIdx++] = c;
			else
			{
				// Character c maps to one or more decomposed chars.
				for (; mappedChars[i] != 0; i++)
				{
					int nth = mappedChars[i];

					// http://www.unicode.org/reports/tr15/tr15-31.html, 1.3:
					// Full decomposition involves recursive application of the
					// Decomposition_Mapping values.  Note that QuickCheck does
					// not currently support astral plane codepoints.
					if (nth <= 0xffff && QuickCheck((char)nth, checkType) == NormalizationCheck.Yes)
						buf[bufIdx++] = nth;
					else
						bufIdx = GetCanonical(nth, buf, bufIdx, checkType);
				}
			}

			return bufIdx;
		}

		public static bool IsNormalized(string source, int type)
		{
			int prevCC = -1;
			for (int i = 0; i < source.Length;)
			{
				int cc = GetCombiningClass(source[i]);
				if (cc != 0 && cc < prevCC)
					return false;
				prevCC = cc;

				switch (QuickCheck(source[i], type))
				{
				case NormalizationCheck.Yes:
					i++;
					break;
				case NormalizationCheck.No:
					return false;
				case NormalizationCheck.Maybe:
					// for those forms with composition, it cannot be checked here
					switch (type)
					{
					case 0: // NFC
					case 2: // NFKC
						return source == Normalize(source, type);
					}
					// go on...

					i = CombineHangul(null, source, i > 0 ? i - 1 : i);
					if (i < 0)
						return false;

					i = TryComposeWithPreviousStarter(null, source, i);
					if (i < 0)
						return false;
					break;
				}
			}
			return true;
		}

		public static string Normalize(string source, int type)
		{
			switch (type)
			{
			default:
			case 2:
				return Compose(source, type);
			case 1:
			case 3:
				return Decompose(source, type);
			}
		}

		static byte[] props;
		static int[] mappedChars;
		static short[] charMapIndex;
		static short[] helperIndex;
		static ushort[] mapIdxToComposite;
		static byte[] combiningClass;

		public readonly static bool IsReady = true; // always

		static Normalization()
		{
			props = propsArr;
			mappedChars = mappedCharsArr;
			charMapIndex = charMapIndexArr;
			helperIndex = helperIndexArr;
			mapIdxToComposite = mapIdxToCompositeArr;
			combiningClass = combiningClassArr;
		}

		//
		// autogenerated code or icall to fill array runs here
		//

		readonly static byte[] propsArr = new byte[]
		{
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0000-000F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0010-001F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0020-002F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x80, 0x80, 0x80, 0x80, 0, // 0030-003F
			0, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, // 0040-004F
			0x80, 0, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0, 0, 0, 0, 0, // 0050-005F
			0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, // 0060-006F
			0x80, 0, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0, 0, 0, 0, 0, // 0070-007F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0080-008F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0090-009F
			0x12, 0, 0, 0, 0, 0, 0, 0, 0x92, 0, 0x12, 0, 0, 0, 0, 0x12, // 00A0-00AF
			0, 0, 0x12, 0x12, 0x92, 0x12, 0, 0x80, 0x12, 0x12, 0x12, 0, 0x12, 0x12, 0x12, 0, // 00B0-00BF
			3, 3, 0x83, 3, 0x83, 0x83, 0x80, 0x83, 3, 3, 0x83, 3, 3, 3, 3, 0x83, // 00C0-00CF
			0, 3, 3, 3, 0x83, 0x83, 0x83, 0, 0x80, 3, 3, 3, 0x83, 3, 0, 0, // 00D0-00DF
			3, 3, 0x83, 3, 0x83, 0x83, 0x80, 0x83, 3, 3, 0x83, 3, 3, 3, 3, 0x83, // 00E0-00EF
			0, 3, 3, 3, 0x83, 0x83, 0x83, 0, 0x80, 3, 3, 3, 0x83, 3, 0, 3, // 00F0-00FF
			3, 3, 0x83, 0x83, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, // 0100-010F
			0, 0, 0x83, 0x83, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, // 0110-011F
			3, 3, 3, 3, 3, 3, 0, 0, 3, 3, 3, 3, 3, 3, 3, 3, // 0120-012F
			3, 0, 0x12, 0x12, 3, 3, 3, 3, 0, 3, 3, 3, 3, 3, 3, 0x12, // 0130-013F
			0x12, 0, 0, 3, 3, 3, 3, 3, 3, 0x12, 0, 0, 0x83, 0x83, 3, 3, // 0140-014F
			3, 3, 0, 0, 3, 3, 3, 3, 3, 3, 0x83, 0x83, 3, 3, 3, 3, // 0150-015F
			0x83, 0x83, 3, 3, 3, 3, 0, 0, 0x83, 0x83, 0x83, 0x83, 3, 3, 3, 3, // 0160-016F
			3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 0x92, // 0170-017F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0180-018F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0190-019F
			0x83, 0x83, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x83, // 01A0-01AF
			0x83, 0, 0, 0, 0, 0, 0, 0x80, 0, 0, 0, 0, 0, 0, 0, 0, // 01B0-01BF
			0, 0, 0, 0, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 3, 3, 3, // 01C0-01CF
			3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 0, 3, 3, // 01D0-01DF
			3, 3, 3, 3, 0, 0, 3, 3, 3, 3, 0x83, 0x83, 3, 3, 3, 3, // 01E0-01EF
			3, 0x12, 0x12, 0x12, 3, 3, 0, 0, 3, 3, 3, 3, 3, 3, 3, 3, // 01F0-01FF
			3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, // 0200-020F
			3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 0, 0, 3, 3, // 0210-021F
			0, 0, 0, 0, 0, 0, 0x83, 0x83, 0x83, 0x83, 3, 3, 3, 3, 0x83, 0x83, // 0220-022F
			3, 3, 3, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0230-023F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0240-024F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0250-025F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0260-026F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0270-027F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0280-028F
			0, 0, 0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0290-029F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 02A0-02AF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x80, 0, 0, 0, 0, 0, 0, // 02B0-02BF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 02C0-02CF
			0, 0, 0, 0, 0, 0, 0, 0, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0, 0, // 02D0-02DF
			0x12, 0x12, 0x12, 0x12, 0x12, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 02E0-02EF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 02F0-02FF
			0xA8, 0xA8, 0xA8, 0xA8, 0xA8, 0, 0xA8, 0xA8, 0xA8, 0xA8, 0xA8, 0xA8, 0xA8, 0, 0, 0xA8, // 0300-030F
			0, 0xA8, 0, 0xA8, 0xA8, 0, 0, 0, 0, 0, 0, 0xA8, 0, 0, 0, 0, // 0310-031F
			0, 0, 0, 0xA8, 0xA8, 0xA8, 0xA8, 0xA8, 0xA8, 0, 0, 0, 0, 0xA8, 0xA8, 0, // 0320-032F
			0xA8, 0xA8, 0, 0, 0, 0, 0, 0, 0xA8, 0, 0, 0, 0, 0, 0, 0, // 0330-033F
			0x57, 0x57, 0xA8, 0x57, 0x57, 0xA8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0340-034F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0350-035F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0360-036F
			0, 0, 0, 0, 0x57, 0, 0, 0, 0, 0, 0x12, 0, 0, 0, 0x57, 0, // 0370-037F
			0, 0, 0, 0, 0x12, 0x93, 0x83, 0x57, 0x83, 0x83, 0x83, 0, 0x83, 0, 0x83, 0x83, // 0380-038F
			0x83, 0x80, 0, 0, 0, 0x80, 0, 0x80, 0, 0x80, 0, 0, 0, 0, 0, 0x80, // 0390-039F
			0, 0x80, 0, 0, 0, 0x80, 0, 0, 0, 0x80, 3, 3, 0x83, 0x83, 0x83, 0x83, // 03A0-03AF
			0x83, 0x80, 0, 0, 0, 0x80, 0, 0x80, 0, 0x80, 0, 0, 0, 0, 0, 0x80, // 03B0-03BF
			0, 0x80, 0, 0, 0, 0x80, 0, 0, 0, 0x80, 0x83, 0x83, 0x83, 0x83, 0x83, 0, // 03C0-03CF
			0x12, 0x12, 0x92, 0x13, 0x13, 0x12, 0x12, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 03D0-03DF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 03E0-03EF
			0x12, 0x12, 0x12, 0, 0x12, 0x12, 0, 0, 0, 0x12, 0, 0, 0, 0, 0, 0, // 03F0-03FF
			3, 3, 0, 3, 0, 0, 0x80, 3, 0, 0, 0, 0, 3, 3, 3, 0, // 0400-040F
			0x80, 0, 0, 0x80, 0, 0x80, 0x80, 0x80, 0x80, 3, 0x80, 0, 0, 0, 0x80, 0, // 0410-041F
			0, 0, 0, 0x80, 0, 0, 0, 0x80, 0, 0, 0, 0x80, 0, 0x80, 0, 0, // 0420-042F
			0x80, 0, 0, 0x80, 0, 0x80, 0x80, 0x80, 0x80, 3, 0x80, 0, 0, 0, 0x80, 0, // 0430-043F
			0, 0, 0, 0x80, 0, 0, 0, 0x80, 0, 0, 0, 0x80, 0, 0x80, 0, 0, // 0440-044F
			3, 3, 0, 3, 0, 0, 0x80, 3, 0, 0, 0, 0, 3, 3, 3, 0, // 0450-045F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0460-046F
			0, 0, 0, 0, 0x80, 0x80, 3, 3, 0, 0, 0, 0, 0, 0, 0, 0, // 0470-047F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0480-048F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0490-049F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 04A0-04AF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 04B0-04BF
			0, 3, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 04C0-04CF
			3, 3, 3, 3, 0, 0, 3, 3, 0x80, 0x80, 3, 3, 3, 3, 3, 3, // 04D0-04DF
			0, 0, 3, 3, 3, 3, 3, 3, 0x80, 0x80, 3, 3, 3, 3, 3, 3, // 04E0-04EF
			3, 3, 3, 3, 3, 3, 0, 0, 3, 3, 0, 0, 0, 0, 0, 0, // 04F0-04FF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0500-050F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0510-051F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0520-052F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0530-053F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0540-054F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0550-055F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0560-056F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0570-057F
			0, 0, 0, 0, 0, 0, 0, 0x12, 0, 0, 0, 0, 0, 0, 0, 0, // 0580-058F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0590-059F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 05A0-05AF
			0, 0, 0, 0, 0x80, 0, 0, 0x80, 0x80, 0x80, 0, 0, 0x80, 0, 0, 0x80, // 05B0-05BF
			0, 0x80, 0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 05C0-05CF
			0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0, 0x80, 0x80, 0x80, 0x80, 0x80, 0, 0x80, 0, // 05D0-05DF
			0x80, 0x80, 0, 0x80, 0x80, 0, 0x80, 0x80, 0x80, 0x80, 0x80, 0, 0, 0, 0, 0, // 05E0-05EF
			0, 0, 0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 05F0-05FF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0600-060F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0610-061F
			0, 0, 3, 3, 3, 3, 3, 0x80, 0, 0, 0, 0, 0, 0, 0, 0, // 0620-062F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0630-063F
			0, 0, 0, 0, 0, 0, 0, 0, 0x80, 0, 0x80, 0, 0, 0, 0, 0, // 0640-064F
			0, 0, 0, 0xA8, 0xA8, 0xA8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0650-065F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0660-066F
			0, 0, 0, 0, 0, 0x12, 0x12, 0x12, 0x12, 0, 0, 0, 0, 0, 0, 0, // 0670-067F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0680-068F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0690-069F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 06A0-06AF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 06B0-06BF
			3, 0x80, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 06C0-06CF
			0, 0, 0x80, 3, 0, 0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 06D0-06DF
			0, 0, 0, 0, 0, 0x80, 0x80, 0x80, 0, 0, 0, 0, 0x80, 0, 0, 0, // 0910-091F
			0, 0x80, 0x80, 0, 0, 0, 0, 0, 0x80, 3, 0, 0x80, 0, 0, 0, 0x80, // 0920-092F
			0x80, 3, 0, 0x80, 3, 0, 0, 0, 0, 0, 0, 0, 0xA8, 0, 0, 0, // 0930-093F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0940-094F
			0, 0, 0, 0, 0, 0, 0, 0, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, // 0950-095F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0960-096F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0970-097F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0980-098F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0990-099F
			0, 0x80, 0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x80, // 09A0-09AF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x80, 0, 0xA8, 0, // 09B0-09BF
			0, 0, 0, 0, 0, 0, 0, 0x80, 0, 0, 0, 3, 3, 0, 0, 0, // 09C0-09CF
			0, 0, 0, 0, 0, 0, 0, 0xA8, 0, 0, 0, 0, 0x57, 0x57, 0, 0x57, // 09D0-09DF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 09E0-09EF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 09F0-09FF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0A00-0A0F
			0, 0, 0, 0, 0, 0, 0x80, 0x80, 0, 0, 0, 0, 0x80, 0, 0, 0, // 0A10-0A1F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x80, 0, 0, 0, 0, // 0A20-0A2F
			0, 0, 0x80, 0x57, 0, 0, 0x57, 0, 0x80, 0, 0, 0, 0x80, 0, 0, 0, // 0A30-0A3F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0A40-0A4F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x57, 0x57, 0x57, 0, 0, 0x57, 0, // 0A50-0A5F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0A60-0A6F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0A70-0A7F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0A80-0A8F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0A90-0A9F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0AA0-0AAF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0AB0-0ABF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0AC0-0ACF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0AD0-0ADF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0AE0-0AEF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0AF0-0AFF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0B00-0B0F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0B10-0B1F
			0, 0x80, 0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0B20-0B2F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x80, 0, 0xA8, 0, // 0B30-0B3F
			0, 0, 0, 0, 0, 0, 0, 0x80, 3, 0, 0, 3, 3, 0, 0, 0, // 0B40-0B4F
			0, 0, 0, 0, 0, 0, 0xA8, 0xA8, 0, 0, 0, 0, 0x57, 0x57, 0, 0, // 0B50-0B5F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0B60-0B6F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0B70-0B7F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0B80-0B8F
			0, 0, 0x80, 0, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0B90-0B9F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0BA0-0BAF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xA8, 0, // 0BB0-0BBF
			0, 0, 0, 0, 0, 0, 0x80, 0x80, 0, 0, 3, 3, 3, 0, 0, 0, // 0BC0-0BCF
			0, 0, 0, 0, 0, 0, 0, 0xA8, 0, 0, 0, 0, 0, 0, 0, 0, // 0BD0-0BDF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0BE0-0BEF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0BF0-0BFF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C00-0C0F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C10-0C1F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C20-0C2F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C30-0C3F
			0, 0, 0, 0, 0, 0, 0x80, 0, 3, 0, 0, 0, 0, 0, 0, 0, // 0C40-0C4F
			0, 0, 0, 0, 0, 0, 0xA8, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C50-0C5F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C60-0C6F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C70-0C7F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C80-0C8F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C90-0C9F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0CA0-0CAF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x80, // 0CB0-0CBF
			3, 0, 0xA8, 0, 0, 0, 0x80, 3, 3, 0, 0x83, 3, 0, 0, 0, 0, // 0CC0-0CCF
			0, 0, 0, 0, 0, 0xA8, 0xA8, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0CD0-0CDF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0CE0-0CEF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0CF0-0CFF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D00-0D0F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D10-0D1F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D20-0D2F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xA8, 0, // 0D30-0D3F
			0, 0, 0, 0, 0, 0, 0x80, 0x80, 0, 0, 3, 3, 3, 0, 0, 0, // 0D40-0D4F
			0, 0, 0, 0, 0, 0, 0, 0xA8, 0, 0, 0, 0, 0, 0, 0, 0, // 0D50-0D5F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D60-0D6F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D70-0D7F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D80-0D8F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D90-0D9F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0DA0-0DAF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0DB0-0DBF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xA8, 0, 0, 0, 0, 0xA8, // 0DC0-0DCF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x80, 3, 0, 0x83, 3, 3, 0xA8, // 0DD0-0DDF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0DE0-0DEF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0DF0-0DFF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E00-0E0F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E10-0E1F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E20-0E2F
			0, 0, 0, 0x12, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E30-0E3F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E40-0E4F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E50-0E5F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E60-0E6F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E70-0E7F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E80-0E8F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E90-0E9F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0EA0-0EAF
			0, 0, 0, 0x12, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0EB0-0EBF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0EC0-0ECF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x12, 0x12, 0, 0, // 0ED0-0EDF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0EE0-0EEF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0EF0-0EFF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x12, 0, 0, 0, // 0F00-0F0F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0F10-0F1F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0F20-0F2F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0F30-0F3F
			0x80, 0, 0x80, 0x57, 0, 0, 0, 0, 0, 0, 0, 0, 0x80, 0x57, 0, 0, // 0F40-0F4F
			0, 0x80, 0x57, 0, 0, 0, 0x80, 0x57, 0, 0, 0, 0x80, 0x57, 0, 0, 0, // 0F50-0F5F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x57, 0, 0, 0, 0, 0, 0, // 0F60-0F6F
			0, 0x80, 0x80, 0x57, 0x80, 0x57, 0x57, 0x12, 0x57, 0x12, 0, 0, 0, 0, 0, 0, // 0F70-0F7F
			0x80, 0x57, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0F80-0F8F
			0x80, 0, 0x80, 0x57, 0, 0, 0, 0, 0, 0, 0, 0, 0x80, 0x57, 0, 0, // 0F90-0F9F
			0, 0x80, 0x57, 0, 0, 0, 0x80, 0x57, 0, 0, 0, 0x80, 0x57, 0, 0, 0, // 0FA0-0FAF
			0, 0, 0x80, 0x80, 0, 0x80, 0, 0x80, 0, 0x57, 0, 0, 0, 0, 0, 0, // 0FB0-0FBF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0FC0-0FCF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0FD0-0FDF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0FE0-0FEF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0FF0-0FFF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1000-100F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1010-101F
			0, 0, 0, 0, 0, 0x80, 3, 0, 0, 0, 0, 0, 0, 0, 0xA8, 0, // 1020-102F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1030-103F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1040-104F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1050-105F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1060-106F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1070-107F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1080-108F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1090-109F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 10A0-10AF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 10B0-10BF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 10C0-10CF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 10D0-10DF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 10E0-10EF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x12, 0, 0, 0, // 10F0-10FF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1100-110F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1110-111F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1120-112F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1130-113F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1140-114F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1150-115F
			0, 0x28, 0x28, 0x28, 0x28, 0x28, 0x28, 0x28, 0x28, 0x28, 0x28, 0x28, 0x28, 0x28, 0x28, 0x28, // 1160-116F
			0x28, 0x28, 0x28, 0x28, 0x28, 0x28, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1170-117F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1180-118F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1190-119F
			0, 0, 0, 0, 0, 0, 0, 0, 0x28, 0x28, 0x28, 0x28, 0x28, 0x28, 0x28, 0x28, // 11A0-11AF
			0x28, 0x28, 0x28, 0x28, 0x28, 0x28, 0x28, 0x28, 0x28, 0x28, 0x28, 0x28, 0x28, 0x28, 0x28, 0x28, // 11B0-11BF
			0x28, 0x28, 0x28, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 11C0-11CF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 11D0-11DF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 11E0-11EF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 11F0-11FF
			0, 0, 0, 0, 0, 0x80, 3, 0x80, 3, 0x80, 3, 0x80, 3, 0x80, 3, 0, // 1B00-1B0F
			0, 0x80, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1B10-1B1F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1B20-1B2F
			0, 0, 0, 0, 0, 0xA8, 0, 0, 0, 0, 0x80, 3, 0x80, 3, 0x80, 0x80, // 1B30-1B3F
			3, 3, 0x80, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1B40-1B4F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1B50-1B5F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1B60-1B6F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1B70-1B7F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1B80-1B8F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1B90-1B9F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1BA0-1BAF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1BB0-1BBF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1BC0-1BCF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1BD0-1BDF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1BE0-1BEF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1BF0-1BFF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1C00-1C0F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1C10-1C1F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1C20-1C2F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1C30-1C3F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1C40-1C4F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1C50-1C5F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1C60-1C6F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1C70-1C7F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1C80-1C8F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1C90-1C9F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1CA0-1CAF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1CB0-1CBF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1CC0-1CCF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1CD0-1CDF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1CE0-1CEF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1CF0-1CFF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1D00-1D0F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1D10-1D1F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x12, 0x12, 0x12, 0, // 1D20-1D2F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0, 0x12, 0x12, 0x12, 0x12, // 1D30-1D3F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0, 0x12, // 1D40-1D4F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 1D50-1D5F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0, 0, 0, 0, 0, // 1D60-1D6F
			0, 0, 0, 0, 0, 0, 0, 0, 0x12, 0, 0, 0, 0, 0, 0, 0, // 1D70-1D7F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1D80-1D8F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x12, 0x12, 0x12, 0x12, 0x12, // 1D90-1D9F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 1DA0-1DAF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 1DB0-1DBF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1DC0-1DCF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1DD0-1DDF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1DE0-1DEF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1DF0-1DFF
			3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, // 1E00-1E0F
			3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, // 1E10-1E1F
			3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, // 1E20-1E2F
			3, 3, 3, 3, 3, 3, 0x83, 0x83, 3, 3, 3, 3, 3, 3, 3, 3, // 1E30-1E3F
			3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, // 1E40-1E4F
			3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 0x83, 0x83, 3, 3, 3, 3, // 1E50-1E5F
			3, 3, 0x83, 0x83, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, // 1E60-1E6F
			3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, // 1E70-1E7F
			3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, // 1E80-1E8F
			3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 0x12, 0x13, 0, 0, 0, 0, // 1E90-1E9F
			0x83, 0x83, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, // 1EA0-1EAF
			3, 3, 3, 3, 3, 3, 3, 3, 0x83, 0x83, 3, 3, 3, 3, 3, 3, // 1EB0-1EBF
			3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 0x83, 0x83, 3, 3, // 1EC0-1ECF
			3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, // 1ED0-1EDF
			3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, // 1EE0-1EEF
			3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 0, 0, 0, 0, 0, 0, // 1EF0-1EFF
			0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, // 1F00-1F0F
			0x83, 0x83, 3, 3, 3, 3, 0, 0, 0x83, 0x83, 3, 3, 3, 3, 0, 0, // 1F10-1F1F
			0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, // 1F20-1F2F
			0x83, 0x83, 3, 3, 3, 3, 3, 3, 0x83, 0x83, 3, 3, 3, 3, 3, 3, // 1F30-1F3F
			0x83, 0x83, 3, 3, 3, 3, 0, 0, 0x83, 0x83, 3, 3, 3, 3, 0, 0, // 1F40-1F4F
			0x83, 0x83, 3, 3, 3, 3, 3, 3, 0, 0x83, 0, 3, 0, 3, 0, 3, // 1F50-1F5F
			0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, 0x83, // 1F60-1F6F
			0x83, 0x57, 3, 0x57, 0x83, 0x57, 3, 0x57, 3, 0x57, 3, 0x57, 0x83, 0x57, 0, 0, // 1F70-1F7F
			3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, // 1F80-1F8F
			3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, // 1F90-1F9F
			3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, // 1FA0-1FAF
			3, 3, 3, 3, 3, 0, 0x83, 3, 3, 3, 3, 0x57, 3, 0x12, 0x57, 0x92, // 1FB0-1FBF
			0x12, 0x13, 3, 3, 3, 0, 0x83, 3, 3, 0x57, 3, 0x57, 3, 0x13, 0x13, 0x13, // 1FC0-1FCF
			3, 3, 3, 0x57, 0, 0, 3, 3, 3, 3, 3, 0x57, 0, 0x13, 0x13, 0x13, // 1FD0-1FDF
			3, 3, 3, 0x57, 3, 3, 3, 3, 3, 3, 3, 0x57, 3, 0x13, 0x57, 0x57, // 1FE0-1FEF
			0, 0, 3, 3, 3, 0, 0x83, 3, 3, 0x57, 3, 0x57, 3, 0x57, 0x92, 0, // 1FF0-1FFF
			0x57, 0x57, 0x92, 0x92, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0, 0, 0, 0, 0, // 2000-200F
			0, 0x12, 0, 0, 0, 0, 0, 0x12, 0, 0, 0, 0, 0, 0, 0, 0, // 2010-201F
			0, 0, 0, 0, 0x12, 0x12, 0x12, 0, 0, 0, 0, 0, 0, 0, 0, 0x12, // 2020-202F
			0, 0, 0, 0x12, 0x12, 0, 0x12, 0x12, 0, 0, 0, 0, 0x12, 0, 0x12, 0, // 2030-203F
			0, 0, 0, 0, 0, 0, 0, 0x12, 0x12, 0x12, 0, 0, 0, 0, 0, 0, // 2040-204F
			0, 0, 0, 0, 0, 0, 0, 0x12, 0, 0, 0, 0, 0, 0, 0, 0x12, // 2050-205F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2060-206F
			0x12, 0x12, 0, 0, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 2070-207F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0, // 2080-208F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0, 0, 0, // 2090-209F
			0, 0, 0, 0, 0, 0, 0, 0, 0x12, 0, 0, 0, 0, 0, 0, 0, // 20A0-20AF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 20B0-20BF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 20C0-20CF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 20D0-20DF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 20E0-20EF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 20F0-20FF
			0x12, 0x12, 0x12, 0x12, 0, 0x12, 0x12, 0x12, 0, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 2100-210F
			0x12, 0x12, 0x12, 0x12, 0, 0x12, 0x12, 0, 0, 0x12, 0x12, 0x12, 0x12, 0x12, 0, 0, // 2110-211F
			0x12, 0x12, 0x12, 0, 0x12, 0, 0x57, 0, 0x12, 0, 0x57, 0x57, 0x12, 0x12, 0, 0x12, // 2120-212F
			0x12, 0x12, 0, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0, 0x12, 0x12, 0x12, 0x12, 0x12, // 2130-213F
			0x12, 0, 0, 0, 0, 0x12, 0x12, 0x12, 0x12, 0x12, 0, 0, 0, 0, 0, 0, // 2140-214F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 2150-215F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 2160-216F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 2170-217F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x12, 0, 0, 0, 0, 0, 0, // 2180-218F
			0x80, 0, 0x80, 0, 0x80, 0, 0, 0, 0, 0, 3, 3, 0, 0, 0, 0, // 2190-219F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0, // 21A0-21AF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 21B0-21BF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 3, 3, // 21C0-21CF
			0x80, 0, 0x80, 0, 0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 21D0-21DF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 21E0-21EF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 21F0-21FF
			0, 0, 0, 0x80, 3, 0, 0, 0, 0x80, 3, 0, 0x80, 3, 0, 0, 0, // 2200-220F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2210-221F
			0, 0, 0, 0x80, 3, 0x80, 3, 0, 0, 0, 0, 0, 0x12, 0x12, 0, 0x12, // 2220-222F
			0x12, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x80, 0, 0, 0, // 2230-223F
			0, 3, 0, 0x80, 3, 0x80, 0, 3, 0x80, 3, 0, 0, 0, 0x80, 0, 0, // 2240-224F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2250-225F
			3, 0x80, 3, 0, 0x80, 0x80, 0, 0, 0, 0, 0, 0, 0, 3, 3, 3, // 2260-226F
			3, 3, 0x80, 0x80, 3, 3, 0x80, 0x80, 3, 3, 0x80, 0x80, 0x80, 0x80, 0, 0, // 2270-227F
			3, 3, 0x80, 0x80, 3, 3, 0x80, 0x80, 3, 3, 0, 0, 0, 0, 0, 0, // 2280-228F
			0, 0x80, 0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2290-229F
			0, 0, 0x80, 0, 0, 0, 0, 0, 0x80, 0x80, 0, 0x80, 3, 3, 3, 3, // 22A0-22AF
			0, 0, 0x80, 0x80, 0x80, 0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 22B0-22BF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 22C0-22CF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 22D0-22DF
			3, 3, 3, 3, 0, 0, 0, 0, 0, 0, 3, 3, 3, 3, 0, 0, // 22E0-22EF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 22F0-22FF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2300-230F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2310-231F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x57, 0x57, 0, 0, 0, 0, 0, // 2320-232F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 2460-246F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 2470-247F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 2480-248F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 2490-249F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 24A0-24AF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 24B0-24BF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 24C0-24CF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 24D0-24DF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0, 0, 0, 0, 0, // 24E0-24EF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 24F0-24FF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2500-250F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2510-251F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2520-252F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2530-253F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2540-254F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2550-255F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2560-256F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2570-257F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2580-258F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2590-259F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 25A0-25AF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 25B0-25BF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 25C0-25CF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 25D0-25DF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 25E0-25EF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 25F0-25FF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2980-298F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2990-299F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 29A0-29AF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 29B0-29BF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 29C0-29CF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 29D0-29DF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 29E0-29EF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 29F0-29FF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x12, 0, 0, 0, // 2A00-2A0F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2A10-2A1F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2A20-2A2F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2A30-2A3F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2A40-2A4F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2A50-2A5F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2A60-2A6F
			0, 0, 0, 0, 0x12, 0x12, 0x12, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2A70-2A7F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2A80-2A8F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2A90-2A9F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2AA0-2AAF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2AB0-2ABF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2AC0-2ACF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x57, 0x80, 0, 0, // 2AD0-2ADF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x12, 0x12, 0, 0, // 2C70-2C7F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x12, // 2D60-2D6F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x12, // 2E90-2E9F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2EA0-2EAF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2EB0-2EBF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2EC0-2ECF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2ED0-2EDF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2EE0-2EEF
			0, 0, 0, 0x12, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2EF0-2EFF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 2F00-2F0F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 2F10-2F1F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 2F20-2F2F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 2F30-2F3F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 2F40-2F4F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 2F50-2F5F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 2F60-2F6F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 2F70-2F7F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 2F80-2F8F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 2F90-2F9F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 2FA0-2FAF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 2FB0-2FBF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 2FC0-2FCF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2FD0-2FDF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2FE0-2FEF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2FF0-2FFF
			0x12, 0, 0, 0, 0, 0, 0, 0, 0x80, 0x80, 0, 0, 0, 0, 0, 0, // 3000-300F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 3010-301F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 3020-302F
			0, 0, 0, 0, 0, 0, 0x12, 0, 0x12, 0x12, 0x12, 0, 0, 0, 0, 0, // 3030-303F
			0, 0, 0, 0, 0, 0, 0x80, 0, 0, 0, 0, 0x80, 3, 0x80, 3, 0x80, // 3040-304F
			3, 0x80, 3, 0x80, 3, 0x80, 3, 0x80, 3, 0x80, 3, 0x80, 3, 0x80, 3, 0x80, // 3050-305F
			3, 0x80, 3, 0, 0x80, 3, 0x80, 3, 0x80, 3, 0, 0, 0, 0, 0, 0x80, // 3060-306F
			3, 3, 0x80, 3, 3, 0x80, 3, 3, 0x80, 3, 3, 0x80, 3, 3, 0, 0, // 3070-307F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 3080-308F
			0, 0, 0, 0, 3, 0, 0, 0, 0, 0xA8, 0xA8, 0x12, 0x12, 0x80, 3, 0x12, // 3090-309F
			0, 0, 0, 0, 0, 0, 0x80, 0, 0, 0, 0, 0x80, 3, 0x80, 3, 0x80, // 30A0-30AF
			3, 0x80, 3, 0x80, 3, 0x80, 3, 0x80, 3, 0x80, 3, 0x80, 3, 0x80, 3, 0x80, // 30B0-30BF
			3, 0x80, 3, 0, 0x80, 3, 0x80, 3, 0x80, 3, 0, 0, 0, 0, 0, 0x80, // 30C0-30CF
			3, 3, 0x80, 3, 3, 0x80, 3, 3, 0x80, 3, 3, 0x80, 3, 3, 0, 0, // 30D0-30DF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x80, // 30E0-30EF
			0x80, 0x80, 0x80, 0, 3, 0, 0, 3, 3, 3, 3, 0, 0, 0x80, 3, 0x12, // 30F0-30FF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 3100-310F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 3110-311F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 3120-312F
			0, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 3130-313F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 3140-314F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 3150-315F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 3160-316F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 3170-317F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0, // 3180-318F
			0, 0, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 3190-319F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 31A0-31AF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 31B0-31BF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 31C0-31CF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 31D0-31DF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 31E0-31EF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 31F0-31FF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 3200-320F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0, // 3210-321F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 3220-322F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 3230-323F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0, 0, 0, 0, 0, 0, 0, 0, // 3240-324F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 3250-325F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 3260-326F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0, // 3270-327F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 3280-328F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 3290-329F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 32A0-32AF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 32B0-32BF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 32C0-32CF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 32D0-32DF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 32E0-32EF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0, // 32F0-32FF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 3300-330F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 3310-331F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 3320-332F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 3330-333F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 3340-334F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 3350-335F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 3360-336F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 3370-337F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 3380-338F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 3390-339F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 33A0-33AF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 33B0-33BF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 33C0-33CF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 33D0-33DF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 33E0-33EF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // 33F0-33FF
			0x12, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // A770-A77F
			0, 0, 0, 0, 0, 0, 0, 0, 0x12, 0x12, 0, 0, 0, 0, 0, 0, // A7F0-A7FF
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, // F900-F90F
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, // F910-F91F
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, // F920-F92F
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, // F930-F93F
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, // F940-F94F
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, // F950-F95F
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, // F960-F96F
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, // F970-F97F
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, // F980-F98F
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, // F990-F99F
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, // F9A0-F9AF
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, // F9B0-F9BF
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, // F9C0-F9CF
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, // F9D0-F9DF
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, // F9E0-F9EF
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, // F9F0-F9FF
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0, 0, // FA00-FA0F
			0x57, 0, 0x57, 0, 0, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0, // FA10-FA1F
			0x57, 0, 0x57, 0, 0, 0x57, 0x57, 0, 0, 0, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, // FA20-FA2F
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, // FA30-FA3F
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, // FA40-FA4F
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, // FA50-FA5F
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0, 0, // FA60-FA6F
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, // FA70-FA7F
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, // FA80-FA8F
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, // FA90-FA9F
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, // FAA0-FAAF
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, // FAB0-FABF
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, // FAC0-FACF
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0, 0, 0, 0, 0, 0, // FAD0-FADF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // FAE0-FAEF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // FAF0-FAFF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0, 0, 0, 0, 0, 0, 0, 0, 0, // FB00-FB0F
			0, 0, 0, 0x12, 0x12, 0x12, 0x12, 0x12, 0, 0, 0, 0, 0, 0x57, 0, 0x57, // FB10-FB1F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, // FB20-FB2F
			0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0x57, 0, 0x57, 0x57, 0x57, 0x57, 0x57, 0, 0x57, 0, // FB30-FB3F
			0x57, 0x57, 0, 0x57, 0x57, 0, 0x57, 0x57, 0x57, 0xD7, 0x57, 0x57, 0x57, 0x57, 0x57, 0x12, // FB40-FB4F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FB50-FB5F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FB60-FB6F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FB70-FB7F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FB80-FB8F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FB90-FB9F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FBA0-FBAF
			0x12, 0x12, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // FBB0-FBBF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // FBC0-FBCF
			0, 0, 0, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FBD0-FBDF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FBE0-FBEF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FBF0-FBFF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FC00-FC0F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FC10-FC1F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FC20-FC2F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FC30-FC3F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FC40-FC4F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FC50-FC5F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FC60-FC6F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FC70-FC7F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FC80-FC8F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FC90-FC9F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FCA0-FCAF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FCB0-FCBF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FCC0-FCCF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FCD0-FCDF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FCE0-FCEF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FCF0-FCFF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FD00-FD0F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FD10-FD1F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FD20-FD2F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0, 0, // FD30-FD3F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // FD40-FD4F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FD50-FD5F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FD60-FD6F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FD70-FD7F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FD80-FD8F
			0, 0, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FD90-FD9F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FDA0-FDAF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FDB0-FDBF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0, 0, 0, 0, 0, 0, 0, 0, // FDC0-FDCF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // FDD0-FDDF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // FDE0-FDEF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0, 0, 0, // FDF0-FDFF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // FE00-FE0F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0, 0, 0, 0, 0, 0, // FE10-FE1F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // FE20-FE2F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FE30-FE3F
			0x12, 0x12, 0x12, 0x12, 0x12, 0, 0, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FE40-FE4F
			0x12, 0x12, 0x12, 0, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FE50-FE5F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0, 0x12, 0x12, 0x12, 0x12, 0, 0, 0, 0, // FE60-FE6F
			0x12, 0x12, 0x12, 0, 0x12, 0, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FE70-FE7F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FE80-FE8F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FE90-FE9F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FEA0-FEAF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FEB0-FEBF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FEC0-FECF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FED0-FEDF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FEE0-FEEF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0, 0, 0, // FEF0-FEFF
			0, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FF00-FF0F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FF10-FF1F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FF20-FF2F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FF30-FF3F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FF40-FF4F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FF50-FF5F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FF60-FF6F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FF70-FF7F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FF80-FF8F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FF90-FF9F
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FFA0-FFAF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0, // FFB0-FFBF
			0, 0, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0, 0, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, // FFC0-FFCF
			0, 0, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0, 0, 0x12, 0x12, 0x12, 0, 0, 0, // FFD0-FFDF
			0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0, // FFE0-FFEF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // FFF0-FFFF
		};
		readonly static int[] mappedCharsArr = new int[]
		{
			0, 0x20, 0, 0x20, 0x301, 0, 0x20, 0x303, 0, 0x20, 0x304, 0, 0x20, 0x305, 0, 0x20, // 0000-000F
			0x306, 0, 0x20, 0x307, 0, 0x20, 0x308, 0, 0x20, 0x30A, 0, 0x20, 0x30B, 0, 0x20, 0x313, // 0010-001F
			0, 0x20, 0x314, 0, 0x20, 0x327, 0, 0x20, 0x328, 0, 0x20, 0x333, 0, 0x20, 0x342, 0, // 0020-002F
			0x20, 0x345, 0, 0x20, 0x64B, 0, 0x20, 0x64C, 0, 0x20, 0x64C, 0x651, 0, 0x20, 0x64D, 0, // 0030-003F
			0x20, 0x64D, 0x651, 0, 0x20, 0x64E, 0, 0x20, 0x64E, 0x651, 0, 0x20, 0x64F, 0, 0x20, 0x64F, // 0040-004F
			0x651, 0, 0x20, 0x650, 0, 0x20, 0x650, 0x651, 0, 0x20, 0x651, 0, 0x20, 0x651, 0x670, 0, // 0050-005F
			0x20, 0x652, 0, 0x20, 0x3099, 0, 0x20, 0x309A, 0, 0x21, 0, 0x21, 0x21, 0, 0x21, 0x3F, // 0060-006F
			0, 0x22, 0, 0x23, 0, 0x24, 0, 0x25, 0, 0x26, 0, 0x27, 0, 0x28, 0, 0x28, // 0070-007F
			0x31, 0x29, 0, 0x28, 0x31, 0x30, 0x29, 0, 0x28, 0x31, 0x31, 0x29, 0, 0x28, 0x31, 0x32, // 0080-008F
			0x29, 0, 0x28, 0x31, 0x33, 0x29, 0, 0x28, 0x31, 0x34, 0x29, 0, 0x28, 0x31, 0x35, 0x29, // 0090-009F
			0, 0x28, 0x31, 0x36, 0x29, 0, 0x28, 0x31, 0x37, 0x29, 0, 0x28, 0x31, 0x38, 0x29, 0, // 00A0-00AF
			0x28, 0x31, 0x39, 0x29, 0, 0x28, 0x32, 0x29, 0, 0x28, 0x32, 0x30, 0x29, 0, 0x28, 0x33, // 00B0-00BF
			0x29, 0, 0x28, 0x34, 0x29, 0, 0x28, 0x35, 0x29, 0, 0x28, 0x36, 0x29, 0, 0x28, 0x37, // 00C0-00CF
			0x29, 0, 0x28, 0x38, 0x29, 0, 0x28, 0x39, 0x29, 0, 0x28, 0x61, 0x29, 0, 0x28, 0x62, // 00D0-00DF
			0x29, 0, 0x28, 0x63, 0x29, 0, 0x28, 0x64, 0x29, 0, 0x28, 0x65, 0x29, 0, 0x28, 0x66, // 00E0-00EF
			0x29, 0, 0x28, 0x67, 0x29, 0, 0x28, 0x68, 0x29, 0, 0x28, 0x69, 0x29, 0, 0x28, 0x6A, // 00F0-00FF
			0x29, 0, 0x28, 0x6B, 0x29, 0, 0x28, 0x6C, 0x29, 0, 0x28, 0x6D, 0x29, 0, 0x28, 0x6E, // 0100-010F
			0x29, 0, 0x28, 0x6F, 0x29, 0, 0x28, 0x70, 0x29, 0, 0x28, 0x71, 0x29, 0, 0x28, 0x72, // 0110-011F
			0x29, 0, 0x28, 0x73, 0x29, 0, 0x28, 0x74, 0x29, 0, 0x28, 0x75, 0x29, 0, 0x28, 0x76, // 0120-012F
			0x29, 0, 0x28, 0x77, 0x29, 0, 0x28, 0x78, 0x29, 0, 0x28, 0x79, 0x29, 0, 0x28, 0x7A, // 0130-013F
			0x29, 0, 0x28, 0x1100, 0x29, 0, 0x28, 0x1100, 0x1161, 0x29, 0, 0x28, 0x1102, 0x29, 0, 0x28, // 0140-014F
			0x1102, 0x1161, 0x29, 0, 0x28, 0x1103, 0x29, 0, 0x28, 0x1103, 0x1161, 0x29, 0, 0x28, 0x1105, 0x29, // 0150-015F
			0, 0x28, 0x1105, 0x1161, 0x29, 0, 0x28, 0x1106, 0x29, 0, 0x28, 0x1106, 0x1161, 0x29, 0, 0x28, // 0160-016F
			0x1107, 0x29, 0, 0x28, 0x1107, 0x1161, 0x29, 0, 0x28, 0x1109, 0x29, 0, 0x28, 0x1109, 0x1161, 0x29, // 0170-017F
			0, 0x28, 0x110B, 0x29, 0, 0x28, 0x110B, 0x1161, 0x29, 0, 0x28, 0x110B, 0x1169, 0x110C, 0x1165, 0x11AB, // 0180-018F
			0x29, 0, 0x28, 0x110B, 0x1169, 0x1112, 0x116E, 0x29, 0, 0x28, 0x110C, 0x29, 0, 0x28, 0x110C, 0x1161, // 0190-019F
			0x29, 0, 0x28, 0x110C, 0x116E, 0x29, 0, 0x28, 0x110E, 0x29, 0, 0x28, 0x110E, 0x1161, 0x29, 0, // 01A0-01AF
			0x28, 0x110F, 0x29, 0, 0x28, 0x110F, 0x1161, 0x29, 0, 0x28, 0x1110, 0x29, 0, 0x28, 0x1110, 0x1161, // 01B0-01BF
			0x29, 0, 0x28, 0x1111, 0x29, 0, 0x28, 0x1111, 0x1161, 0x29, 0, 0x28, 0x1112, 0x29, 0, 0x28, // 01C0-01CF
			0x1112, 0x1161, 0x29, 0, 0x28, 0x4E00, 0x29, 0, 0x28, 0x4E03, 0x29, 0, 0x28, 0x4E09, 0x29, 0, // 01D0-01DF
			0x28, 0x4E5D, 0x29, 0, 0x28, 0x4E8C, 0x29, 0, 0x28, 0x4E94, 0x29, 0, 0x28, 0x4EE3, 0x29, 0, // 01E0-01EF
			0x28, 0x4F01, 0x29, 0, 0x28, 0x4F11, 0x29, 0, 0x28, 0x516B, 0x29, 0, 0x28, 0x516D, 0x29, 0, // 01F0-01FF
			0x28, 0x52B4, 0x29, 0, 0x28, 0x5341, 0x29, 0, 0x28, 0x5354, 0x29, 0, 0x28, 0x540D, 0x29, 0, // 0200-020F
			0x28, 0x547C, 0x29, 0, 0x28, 0x56DB, 0x29, 0, 0x28, 0x571F, 0x29, 0, 0x28, 0x5B66, 0x29, 0, // 0210-021F
			0x28, 0x65E5, 0x29, 0, 0x28, 0x6708, 0x29, 0, 0x28, 0x6709, 0x29, 0, 0x28, 0x6728, 0x29, 0, // 0220-022F
			0x28, 0x682A, 0x29, 0, 0x28, 0x6C34, 0x29, 0, 0x28, 0x706B, 0x29, 0, 0x28, 0x7279, 0x29, 0, // 0230-023F
			0x28, 0x76E3, 0x29, 0, 0x28, 0x793E, 0x29, 0, 0x28, 0x795D, 0x29, 0, 0x28, 0x796D, 0x29, 0, // 0240-024F
			0x28, 0x81EA, 0x29, 0, 0x28, 0x81F3, 0x29, 0, 0x28, 0x8CA1, 0x29, 0, 0x28, 0x8CC7, 0x29, 0, // 0250-025F
			0x28, 0x91D1, 0x29, 0, 0x29, 0, 0x2A, 0, 0x2B, 0, 0x2C, 0, 0x2D, 0, 0x2E, 0, // 0260-026F
			0x2E, 0x2E, 0, 0x2E, 0x2E, 0x2E, 0, 0x2F, 0, 0x30, 0, 0x30, 0x2044, 0x33, 0, 0x30, // 0270-027F
			0x70B9, 0, 0x31, 0, 0x31, 0x2E, 0, 0x31, 0x30, 0, 0x31, 0x30, 0x2E, 0, 0x31, 0x30, // 0280-028F
			0x65E5, 0, 0x31, 0x30, 0x6708, 0, 0x31, 0x30, 0x70B9, 0, 0x31, 0x31, 0, 0x31, 0x31, 0x2E, // 0290-029F
			0, 0x31, 0x31, 0x65E5, 0, 0x31, 0x31, 0x6708, 0, 0x31, 0x31, 0x70B9, 0, 0x31, 0x32, 0, // 02A0-02AF
			0x31, 0x32, 0x2E, 0, 0x31, 0x32, 0x65E5, 0, 0x31, 0x32, 0x6708, 0, 0x31, 0x32, 0x70B9, 0, // 02B0-02BF
			0x31, 0x33, 0, 0x31, 0x33, 0x2E, 0, 0x31, 0x33, 0x65E5, 0, 0x31, 0x33, 0x70B9, 0, 0x31, // 02C0-02CF
			0x34, 0, 0x31, 0x34, 0x2E, 0, 0x31, 0x34, 0x65E5, 0, 0x31, 0x34, 0x70B9, 0, 0x31, 0x35, // 02D0-02DF
			0, 0x31, 0x35, 0x2E, 0, 0x31, 0x35, 0x65E5, 0, 0x31, 0x35, 0x70B9, 0, 0x31, 0x36, 0, // 02E0-02EF
			0x31, 0x36, 0x2E, 0, 0x31, 0x36, 0x65E5, 0, 0x31, 0x36, 0x70B9, 0, 0x31, 0x37, 0, 0x31, // 02F0-02FF
			0x37, 0x2E, 0, 0x31, 0x37, 0x65E5, 0, 0x31, 0x37, 0x70B9, 0, 0x31, 0x38, 0, 0x31, 0x38, // 0300-030F
			0x2E, 0, 0x31, 0x38, 0x65E5, 0, 0x31, 0x38, 0x70B9, 0, 0x31, 0x39, 0, 0x31, 0x39, 0x2E, // 0310-031F
			0, 0x31, 0x39, 0x65E5, 0, 0x31, 0x39, 0x70B9, 0, 0x31, 0x2044, 0, 0x31, 0x2044, 0x31, 0x30, // 0320-032F
			0, 0x31, 0x2044, 0x32, 0, 0x31, 0x2044, 0x33, 0, 0x31, 0x2044, 0x34, 0, 0x31, 0x2044, 0x35, // 0330-033F
			0, 0x31, 0x2044, 0x36, 0, 0x31, 0x2044, 0x37, 0, 0x31, 0x2044, 0x38, 0, 0x31, 0x2044, 0x39, // 0340-034F
			0, 0x31, 0x65E5, 0, 0x31, 0x6708, 0, 0x31, 0x70B9, 0, 0x32, 0, 0x32, 0x2E, 0, 0x32, // 0350-035F
			0x30, 0, 0x32, 0x30, 0x2E, 0, 0x32, 0x30, 0x65E5, 0, 0x32, 0x30, 0x70B9, 0, 0x32, 0x31, // 0360-036F
			0, 0x32, 0x31, 0x65E5, 0, 0x32, 0x31, 0x70B9, 0, 0x32, 0x32, 0, 0x32, 0x32, 0x65E5, 0, // 0370-037F
			0x32, 0x32, 0x70B9, 0, 0x32, 0x33, 0, 0x32, 0x33, 0x65E5, 0, 0x32, 0x33, 0x70B9, 0, 0x32, // 0380-038F
			0x34, 0, 0x32, 0x34, 0x65E5, 0, 0x32, 0x34, 0x70B9, 0, 0x32, 0x35, 0, 0x32, 0x35, 0x65E5, // 0390-039F
			0, 0x32, 0x36, 0, 0x32, 0x36, 0x65E5, 0, 0x32, 0x37, 0, 0x32, 0x37, 0x65E5, 0, 0x32, // 03A0-03AF
			0x38, 0, 0x32, 0x38, 0x65E5, 0, 0x32, 0x39, 0, 0x32, 0x39, 0x65E5, 0, 0x32, 0x2044, 0x33, // 03B0-03BF
			0, 0x32, 0x2044, 0x35, 0, 0x32, 0x65E5, 0, 0x32, 0x6708, 0, 0x32, 0x70B9, 0, 0x33, 0, // 03C0-03CF
			0x33, 0x2E, 0, 0x33, 0x30, 0, 0x33, 0x30, 0x65E5, 0, 0x33, 0x31, 0, 0x33, 0x31, 0x65E5, // 03D0-03DF
			0, 0x33, 0x32, 0, 0x33, 0x33, 0, 0x33, 0x34, 0, 0x33, 0x35, 0, 0x33, 0x36, 0, // 03E0-03EF
			0x33, 0x37, 0, 0x33, 0x38, 0, 0x33, 0x39, 0, 0x33, 0x2044, 0x34, 0, 0x33, 0x2044, 0x35, // 03F0-03FF
			0, 0x33, 0x2044, 0x38, 0, 0x33, 0x65E5, 0, 0x33, 0x6708, 0, 0x33, 0x70B9, 0, 0x34, 0, // 0400-040F
			0x34, 0x2E, 0, 0x34, 0x30, 0, 0x34, 0x31, 0, 0x34, 0x32, 0, 0x34, 0x33, 0, 0x34, // 0410-041F
			0x34, 0, 0x34, 0x35, 0, 0x34, 0x36, 0, 0x34, 0x37, 0, 0x34, 0x38, 0, 0x34, 0x39, // 0420-042F
			0, 0x34, 0x2044, 0x35, 0, 0x34, 0x65E5, 0, 0x34, 0x6708, 0, 0x34, 0x70B9, 0, 0x35, 0, // 0430-043F
			0x35, 0x2E, 0, 0x35, 0x30, 0, 0x35, 0x2044, 0x36, 0, 0x35, 0x2044, 0x38, 0, 0x35, 0x65E5, // 0440-044F
			0, 0x35, 0x6708, 0, 0x35, 0x70B9, 0, 0x36, 0, 0x36, 0x2E, 0, 0x36, 0x65E5, 0, 0x36, // 0450-045F
			0x6708, 0, 0x36, 0x70B9, 0, 0x37, 0, 0x37, 0x2E, 0, 0x37, 0x2044, 0x38, 0, 0x37, 0x65E5, // 0460-046F
			0, 0x37, 0x6708, 0, 0x37, 0x70B9, 0, 0x38, 0, 0x38, 0x2E, 0, 0x38, 0x65E5, 0, 0x38, // 0470-047F
			0x6708, 0, 0x38, 0x70B9, 0, 0x39, 0, 0x39, 0x2E, 0, 0x39, 0x65E5, 0, 0x39, 0x6708, 0, // 0480-048F
			0x39, 0x70B9, 0, 0x3A, 0, 0x3A, 0x3A, 0x3D, 0, 0x3B, 0, 0x3C, 0, 0x3C, 0x338, 0, // 0490-049F
			0x3D, 0, 0x3D, 0x3D, 0, 0x3D, 0x3D, 0x3D, 0, 0x3D, 0x338, 0, 0x3E, 0, 0x3E, 0x338, // 04A0-04AF
			0, 0x3F, 0, 0x3F, 0x21, 0, 0x3F, 0x3F, 0, 0x40, 0, 0x41, 0, 0x41, 0x55, 0, // 04B0-04BF
			0x41, 0x300, 0, 0x41, 0x301, 0, 0x41, 0x302, 0, 0x41, 0x303, 0, 0x41, 0x304, 0, 0x41, // 04C0-04CF
			0x306, 0, 0x41, 0x307, 0, 0x41, 0x308, 0, 0x41, 0x309, 0, 0x41, 0x30A, 0, 0x41, 0x30C, // 04D0-04DF
			0, 0x41, 0x30F, 0, 0x41, 0x311, 0, 0x41, 0x323, 0, 0x41, 0x325, 0, 0x41, 0x328, 0, // 04E0-04EF
			0x41, 0x2215, 0x6D, 0, 0x42, 0, 0x42, 0x71, 0, 0x42, 0x307, 0, 0x42, 0x323, 0, 0x42, // 04F0-04FF
			0x331, 0, 0x43, 0, 0x43, 0x6F, 0x2E, 0, 0x43, 0x301, 0, 0x43, 0x302, 0, 0x43, 0x307, // 0500-050F
			0, 0x43, 0x30C, 0, 0x43, 0x327, 0, 0x43, 0x2215, 0x6B, 0x67, 0, 0x44, 0, 0x44, 0x5A, // 0510-051F
			0, 0x44, 0x7A, 0, 0x44, 0x17D, 0, 0x44, 0x17E, 0, 0x44, 0x307, 0, 0x44, 0x30C, 0, // 0520-052F
			0x44, 0x323, 0, 0x44, 0x327, 0, 0x44, 0x32D, 0, 0x44, 0x331, 0, 0x45, 0, 0x45, 0x300, // 0530-053F
			0, 0x45, 0x301, 0, 0x45, 0x302, 0, 0x45, 0x303, 0, 0x45, 0x304, 0, 0x45, 0x306, 0, // 0540-054F
			0x45, 0x307, 0, 0x45, 0x308, 0, 0x45, 0x309, 0, 0x45, 0x30C, 0, 0x45, 0x30F, 0, 0x45, // 0550-055F
			0x311, 0, 0x45, 0x323, 0, 0x45, 0x327, 0, 0x45, 0x328, 0, 0x45, 0x32D, 0, 0x45, 0x330, // 0560-056F
			0, 0x46, 0, 0x46, 0x41, 0x58, 0, 0x46, 0x307, 0, 0x47, 0, 0x47, 0x42, 0, 0x47, // 0570-057F
			0x48, 0x7A, 0, 0x47, 0x50, 0x61, 0, 0x47, 0x79, 0, 0x47, 0x301, 0, 0x47, 0x302, 0, // 0580-058F
			0x47, 0x304, 0, 0x47, 0x306, 0, 0x47, 0x307, 0, 0x47, 0x30C, 0, 0x47, 0x327, 0, 0x48, // 0590-059F
			0, 0x48, 0x50, 0, 0x48, 0x67, 0, 0x48, 0x7A, 0, 0x48, 0x302, 0, 0x48, 0x307, 0, // 05A0-05AF
			0x48, 0x308, 0, 0x48, 0x30C, 0, 0x48, 0x323, 0, 0x48, 0x327, 0, 0x48, 0x32E, 0, 0x49, // 05B0-05BF
			0, 0x49, 0x49, 0, 0x49, 0x49, 0x49, 0, 0x49, 0x4A, 0, 0x49, 0x55, 0, 0x49, 0x56, // 05C0-05CF
			0, 0x49, 0x58, 0, 0x49, 0x300, 0, 0x49, 0x301, 0, 0x49, 0x302, 0, 0x49, 0x303, 0, // 05D0-05DF
			0x49, 0x304, 0, 0x49, 0x306, 0, 0x49, 0x307, 0, 0x49, 0x308, 0, 0x49, 0x309, 0, 0x49, // 05E0-05EF
			0x30C, 0, 0x49, 0x30F, 0, 0x49, 0x311, 0, 0x49, 0x323, 0, 0x49, 0x328, 0, 0x49, 0x330, // 05F0-05FF
			0, 0x4A, 0, 0x4A, 0x302, 0, 0x4B, 0, 0x4B, 0x42, 0, 0x4B, 0x4B, 0, 0x4B, 0x4D, // 0600-060F
			0, 0x4B, 0x301, 0, 0x4B, 0x30C, 0, 0x4B, 0x323, 0, 0x4B, 0x327, 0, 0x4B, 0x331, 0, // 0610-061F
			0x4C, 0, 0x4C, 0x4A, 0, 0x4C, 0x54, 0x44, 0, 0x4C, 0x6A, 0, 0x4C, 0xB7, 0, 0x4C, // 0620-062F
			0x301, 0, 0x4C, 0x30C, 0, 0x4C, 0x323, 0, 0x4C, 0x327, 0, 0x4C, 0x32D, 0, 0x4C, 0x331, // 0630-063F
			0, 0x4D, 0, 0x4D, 0x42, 0, 0x4D, 0x48, 0x7A, 0, 0x4D, 0x50, 0x61, 0, 0x4D, 0x56, // 0640-064F
			0, 0x4D, 0x57, 0, 0x4D, 0x301, 0, 0x4D, 0x307, 0, 0x4D, 0x323, 0, 0x4D, 0x3A9, 0, // 0650-065F
			0x4E, 0, 0x4E, 0x4A, 0, 0x4E, 0x6A, 0, 0x4E, 0x6F, 0, 0x4E, 0x300, 0, 0x4E, 0x301, // 0660-066F
			0, 0x4E, 0x303, 0, 0x4E, 0x307, 0, 0x4E, 0x30C, 0, 0x4E, 0x323, 0, 0x4E, 0x327, 0, // 0670-067F
			0x4E, 0x32D, 0, 0x4E, 0x331, 0, 0x4F, 0, 0x4F, 0x300, 0, 0x4F, 0x301, 0, 0x4F, 0x302, // 0680-068F
			0, 0x4F, 0x303, 0, 0x4F, 0x304, 0, 0x4F, 0x306, 0, 0x4F, 0x307, 0, 0x4F, 0x308, 0, // 0690-069F
			0x4F, 0x309, 0, 0x4F, 0x30B, 0, 0x4F, 0x30C, 0, 0x4F, 0x30F, 0, 0x4F, 0x311, 0, 0x4F, // 06A0-06AF
			0x31B, 0, 0x4F, 0x323, 0, 0x4F, 0x328, 0, 0x50, 0, 0x50, 0x48, 0, 0x50, 0x50, 0x4D, // 06B0-06BF
			0, 0x50, 0x52, 0, 0x50, 0x54, 0x45, 0, 0x50, 0x61, 0, 0x50, 0x301, 0, 0x50, 0x307, // 06C0-06CF
			0, 0x51, 0, 0x52, 0, 0x52, 0x73, 0, 0x52, 0x301, 0, 0x52, 0x307, 0, 0x52, 0x30C, // 06D0-06DF
			0, 0x52, 0x30F, 0, 0x52, 0x311, 0, 0x52, 0x323, 0, 0x52, 0x327, 0, 0x52, 0x331, 0, // 06E0-06EF
			0x53, 0, 0x53, 0x4D, 0, 0x53, 0x76, 0, 0x53, 0x301, 0, 0x53, 0x302, 0, 0x53, 0x307, // 06F0-06FF
			0, 0x53, 0x30C, 0, 0x53, 0x323, 0, 0x53, 0x326, 0, 0x53, 0x327, 0, 0x54, 0, 0x54, // 0700-070F
			0x45, 0x4C, 0, 0x54, 0x48, 0x7A, 0, 0x54, 0x4D, 0, 0x54, 0x307, 0, 0x54, 0x30C, 0, // 0710-071F
			0x54, 0x323, 0, 0x54, 0x326, 0, 0x54, 0x327, 0, 0x54, 0x32D, 0, 0x54, 0x331, 0, 0x55, // 0720-072F
			0, 0x55, 0x300, 0, 0x55, 0x301, 0, 0x55, 0x302, 0, 0x55, 0x303, 0, 0x55, 0x304, 0, // 0730-073F
			0x55, 0x306, 0, 0x55, 0x308, 0, 0x55, 0x309, 0, 0x55, 0x30A, 0, 0x55, 0x30B, 0, 0x55, // 0740-074F
			0x30C, 0, 0x55, 0x30F, 0, 0x55, 0x311, 0, 0x55, 0x31B, 0, 0x55, 0x323, 0, 0x55, 0x324, // 0750-075F
			0, 0x55, 0x328, 0, 0x55, 0x32D, 0, 0x55, 0x330, 0, 0x56, 0, 0x56, 0x49, 0, 0x56, // 0760-076F
			0x49, 0x49, 0, 0x56, 0x49, 0x49, 0x49, 0, 0x56, 0x303, 0, 0x56, 0x323, 0, 0x56, 0x2215, // 0770-077F
			0x6D, 0, 0x57, 0, 0x57, 0x62, 0, 0x57, 0x300, 0, 0x57, 0x301, 0, 0x57, 0x302, 0, // 0780-078F
			0x57, 0x307, 0, 0x57, 0x308, 0, 0x57, 0x323, 0, 0x58, 0, 0x58, 0x49, 0, 0x58, 0x49, // 0790-079F
			0x49, 0, 0x58, 0x307, 0, 0x58, 0x308, 0, 0x59, 0, 0x59, 0x300, 0, 0x59, 0x301, 0, // 07A0-07AF
			0x59, 0x302, 0, 0x59, 0x303, 0, 0x59, 0x304, 0, 0x59, 0x307, 0, 0x59, 0x308, 0, 0x59, // 07B0-07BF
			0x309, 0, 0x59, 0x323, 0, 0x5A, 0, 0x5A, 0x301, 0, 0x5A, 0x302, 0, 0x5A, 0x307, 0, // 07C0-07CF
			0x5A, 0x30C, 0, 0x5A, 0x323, 0, 0x5A, 0x331, 0, 0x5B, 0, 0x5C, 0, 0x5D, 0, 0x5E, // 07D0-07DF
			0, 0x5F, 0, 0x60, 0, 0x61, 0, 0x61, 0x2E, 0x6D, 0x2E, 0, 0x61, 0x2F, 0x63, 0, // 07E0-07EF
			0x61, 0x2F, 0x73, 0, 0x61, 0x2BE, 0, 0x61, 0x300, 0, 0x61, 0x301, 0, 0x61, 0x302, 0, // 07F0-07FF
			0x61, 0x303, 0, 0x61, 0x304, 0, 0x61, 0x306, 0, 0x61, 0x307, 0, 0x61, 0x308, 0, 0x61, // 0800-080F
			0x309, 0, 0x61, 0x30A, 0, 0x61, 0x30C, 0, 0x61, 0x30F, 0, 0x61, 0x311, 0, 0x61, 0x323, // 0810-081F
			0, 0x61, 0x325, 0, 0x61, 0x328, 0, 0x62, 0, 0x62, 0x61, 0x72, 0, 0x62, 0x307, 0, // 0820-082F
			0x62, 0x323, 0, 0x62, 0x331, 0, 0x63, 0, 0x63, 0x2F, 0x6F, 0, 0x63, 0x2F, 0x75, 0, // 0830-083F
			0x63, 0x61, 0x6C, 0, 0x63, 0x63, 0, 0x63, 0x64, 0, 0x63, 0x6D, 0, 0x63, 0x6D, 0xB2, // 0840-084F
			0, 0x63, 0x6D, 0xB3, 0, 0x63, 0x301, 0, 0x63, 0x302, 0, 0x63, 0x307, 0, 0x63, 0x30C, // 0850-085F
			0, 0x63, 0x327, 0, 0x64, 0, 0x64, 0x42, 0, 0x64, 0x61, 0, 0x64, 0x6D, 0, 0x64, // 0860-086F
			0x6D, 0xB2, 0, 0x64, 0x6D, 0xB3, 0, 0x64, 0x7A, 0, 0x64, 0x17E, 0, 0x64, 0x307, 0, // 0870-087F
			0x64, 0x30C, 0, 0x64, 0x323, 0, 0x64, 0x327, 0, 0x64, 0x32D, 0, 0x64, 0x331, 0, 0x64, // 0880-088F
			0x2113, 0, 0x65, 0, 0x65, 0x56, 0, 0x65, 0x72, 0x67, 0, 0x65, 0x300, 0, 0x65, 0x301, // 0890-089F
			0, 0x65, 0x302, 0, 0x65, 0x303, 0, 0x65, 0x304, 0, 0x65, 0x306, 0, 0x65, 0x307, 0, // 08A0-08AF
			0x65, 0x308, 0, 0x65, 0x309, 0, 0x65, 0x30C, 0, 0x65, 0x30F, 0, 0x65, 0x311, 0, 0x65, // 08B0-08BF
			0x323, 0, 0x65, 0x327, 0, 0x65, 0x328, 0, 0x65, 0x32D, 0, 0x65, 0x330, 0, 0x66, 0, // 08C0-08CF
			0x66, 0x66, 0, 0x66, 0x66, 0x69, 0, 0x66, 0x66, 0x6C, 0, 0x66, 0x69, 0, 0x66, 0x6C, // 08D0-08DF
			0, 0x66, 0x6D, 0, 0x66, 0x307, 0, 0x67, 0, 0x67, 0x61, 0x6C, 0, 0x67, 0x301, 0, // 08E0-08EF
			0x67, 0x302, 0, 0x67, 0x304, 0, 0x67, 0x306, 0, 0x67, 0x307, 0, 0x67, 0x30C, 0, 0x67, // 08F0-08FF
			0x327, 0, 0x68, 0, 0x68, 0x50, 0x61, 0, 0x68, 0x61, 0, 0x68, 0x302, 0, 0x68, 0x307, // 0900-090F
			0, 0x68, 0x308, 0, 0x68, 0x30C, 0, 0x68, 0x323, 0, 0x68, 0x327, 0, 0x68, 0x32E, 0, // 0910-091F
			0x68, 0x331, 0, 0x69, 0, 0x69, 0x69, 0, 0x69, 0x69, 0x69, 0, 0x69, 0x6A, 0, 0x69, // 0920-092F
			0x6E, 0, 0x69, 0x76, 0, 0x69, 0x78, 0, 0x69, 0x300, 0, 0x69, 0x301, 0, 0x69, 0x302, // 0930-093F
			0, 0x69, 0x303, 0, 0x69, 0x304, 0, 0x69, 0x306, 0, 0x69, 0x308, 0, 0x69, 0x309, 0, // 0940-094F
			0x69, 0x30C, 0, 0x69, 0x30F, 0, 0x69, 0x311, 0, 0x69, 0x323, 0, 0x69, 0x328, 0, 0x69, // 0950-095F
			0x330, 0, 0x6A, 0, 0x6A, 0x302, 0, 0x6A, 0x30C, 0, 0x6B, 0, 0x6B, 0x41, 0, 0x6B, // 0960-096F
			0x48, 0x7A, 0, 0x6B, 0x50, 0x61, 0, 0x6B, 0x56, 0, 0x6B, 0x57, 0, 0x6B, 0x63, 0x61, // 0970-097F
			0x6C, 0, 0x6B, 0x67, 0, 0x6B, 0x6D, 0, 0x6B, 0x6D, 0xB2, 0, 0x6B, 0x6D, 0xB3, 0, // 0980-098F
			0x6B, 0x74, 0, 0x6B, 0x301, 0, 0x6B, 0x30C, 0, 0x6B, 0x323, 0, 0x6B, 0x327, 0, 0x6B, // 0990-099F
			0x331, 0, 0x6B, 0x3A9, 0, 0x6B, 0x2113, 0, 0x6C, 0, 0x6C, 0x6A, 0, 0x6C, 0x6D, 0, // 09A0-09AF
			0x6C, 0x6E, 0, 0x6C, 0x6F, 0x67, 0, 0x6C, 0x78, 0, 0x6C, 0xB7, 0, 0x6C, 0x301, 0, // 09B0-09BF
			0x6C, 0x30C, 0, 0x6C, 0x323, 0, 0x6C, 0x327, 0, 0x6C, 0x32D, 0, 0x6C, 0x331, 0, 0x6D, // 09C0-09CF
			0, 0x6D, 0x41, 0, 0x6D, 0x56, 0, 0x6D, 0x57, 0, 0x6D, 0x62, 0, 0x6D, 0x67, 0, // 09D0-09DF
			0x6D, 0x69, 0x6C, 0, 0x6D, 0x6D, 0, 0x6D, 0x6D, 0xB2, 0, 0x6D, 0x6D, 0xB3, 0, 0x6D, // 09E0-09EF
			0x6F, 0x6C, 0, 0x6D, 0x73, 0, 0x6D, 0xB2, 0, 0x6D, 0xB3, 0, 0x6D, 0x301, 0, 0x6D, // 09F0-09FF
			0x307, 0, 0x6D, 0x323, 0, 0x6D, 0x2113, 0, 0x6D, 0x2215, 0x73, 0, 0x6D, 0x2215, 0x73, 0xB2, // 0A00-0A0F
			0, 0x6E, 0, 0x6E, 0x41, 0, 0x6E, 0x46, 0, 0x6E, 0x56, 0, 0x6E, 0x57, 0, 0x6E, // 0A10-0A1F
			0x6A, 0, 0x6E, 0x6D, 0, 0x6E, 0x73, 0, 0x6E, 0x300, 0, 0x6E, 0x301, 0, 0x6E, 0x303, // 0A20-0A2F
			0, 0x6E, 0x307, 0, 0x6E, 0x30C, 0, 0x6E, 0x323, 0, 0x6E, 0x327, 0, 0x6E, 0x32D, 0, // 0A30-0A3F
			0x6E, 0x331, 0, 0x6F, 0, 0x6F, 0x56, 0, 0x6F, 0x300, 0, 0x6F, 0x301, 0, 0x6F, 0x302, // 0A40-0A4F
			0, 0x6F, 0x303, 0, 0x6F, 0x304, 0, 0x6F, 0x306, 0, 0x6F, 0x307, 0, 0x6F, 0x308, 0, // 0A50-0A5F
			0x6F, 0x309, 0, 0x6F, 0x30B, 0, 0x6F, 0x30C, 0, 0x6F, 0x30F, 0, 0x6F, 0x311, 0, 0x6F, // 0A60-0A6F
			0x31B, 0, 0x6F, 0x323, 0, 0x6F, 0x328, 0, 0x70, 0, 0x70, 0x2E, 0x6D, 0x2E, 0, 0x70, // 0A70-0A7F
			0x41, 0, 0x70, 0x46, 0, 0x70, 0x56, 0, 0x70, 0x57, 0, 0x70, 0x63, 0, 0x70, 0x73, // 0A80-0A8F
			0, 0x70, 0x301, 0, 0x70, 0x307, 0, 0x71, 0, 0x72, 0, 0x72, 0x61, 0x64, 0, 0x72, // 0A90-0A9F
			0x61, 0x64, 0x2215, 0x73, 0, 0x72, 0x61, 0x64, 0x2215, 0x73, 0xB2, 0, 0x72, 0x301, 0, 0x72, // 0AA0-0AAF
			0x307, 0, 0x72, 0x30C, 0, 0x72, 0x30F, 0, 0x72, 0x311, 0, 0x72, 0x323, 0, 0x72, 0x327, // 0AB0-0ABF
			0, 0x72, 0x331, 0, 0x73, 0, 0x73, 0x72, 0, 0x73, 0x74, 0, 0x73, 0x301, 0, 0x73, // 0AC0-0ACF
			0x302, 0, 0x73, 0x307, 0, 0x73, 0x30C, 0, 0x73, 0x323, 0, 0x73, 0x326, 0, 0x73, 0x327, // 0AD0-0ADF
			0, 0x74, 0, 0x74, 0x307, 0, 0x74, 0x308, 0, 0x74, 0x30C, 0, 0x74, 0x323, 0, 0x74, // 0AE0-0AEF
			0x326, 0, 0x74, 0x327, 0, 0x74, 0x32D, 0, 0x74, 0x331, 0, 0x75, 0, 0x75, 0x300, 0, // 0AF0-0AFF
			0x75, 0x301, 0, 0x75, 0x302, 0, 0x75, 0x303, 0, 0x75, 0x304, 0, 0x75, 0x306, 0, 0x75, // 0B00-0B0F
			0x308, 0, 0x75, 0x309, 0, 0x75, 0x30A, 0, 0x75, 0x30B, 0, 0x75, 0x30C, 0, 0x75, 0x30F, // 0B10-0B1F
			0, 0x75, 0x311, 0, 0x75, 0x31B, 0, 0x75, 0x323, 0, 0x75, 0x324, 0, 0x75, 0x328, 0, // 0B20-0B2F
			0x75, 0x32D, 0, 0x75, 0x330, 0, 0x76, 0, 0x76, 0x69, 0, 0x76, 0x69, 0x69, 0, 0x76, // 0B30-0B3F
			0x69, 0x69, 0x69, 0, 0x76, 0x303, 0, 0x76, 0x323, 0, 0x77, 0, 0x77, 0x300, 0, 0x77, // 0B40-0B4F
			0x301, 0, 0x77, 0x302, 0, 0x77, 0x307, 0, 0x77, 0x308, 0, 0x77, 0x30A, 0, 0x77, 0x323, // 0B50-0B5F
			0, 0x78, 0, 0x78, 0x69, 0, 0x78, 0x69, 0x69, 0, 0x78, 0x307, 0, 0x78, 0x308, 0, // 0B60-0B6F
			0x79, 0, 0x79, 0x300, 0, 0x79, 0x301, 0, 0x79, 0x302, 0, 0x79, 0x303, 0, 0x79, 0x304, // 0B70-0B7F
			0, 0x79, 0x307, 0, 0x79, 0x308, 0, 0x79, 0x309, 0, 0x79, 0x30A, 0, 0x79, 0x323, 0, // 0B80-0B8F
			0x7A, 0, 0x7A, 0x301, 0, 0x7A, 0x302, 0, 0x7A, 0x307, 0, 0x7A, 0x30C, 0, 0x7A, 0x323, // 0B90-0B9F
			0, 0x7A, 0x331, 0, 0x7B, 0, 0x7C, 0, 0x7D, 0, 0x7E, 0, 0xA2, 0, 0xA3, 0, // 0BA0-0BAF
			0xA5, 0, 0xA6, 0, 0xA8, 0x300, 0, 0xA8, 0x301, 0, 0xA8, 0x342, 0, 0xAC, 0, 0xAF, // 0BB0-0BBF
			0, 0xB0, 0x43, 0, 0xB0, 0x46, 0, 0xB4, 0, 0xB7, 0, 0xC2, 0x300, 0, 0xC2, 0x301, // 0BC0-0BCF
			0, 0xC2, 0x303, 0, 0xC2, 0x309, 0, 0xC4, 0x304, 0, 0xC5, 0, 0xC5, 0x301, 0, 0xC6, // 0BD0-0BDF
			0, 0xC6, 0x301, 0, 0xC6, 0x304, 0, 0xC7, 0x301, 0, 0xCA, 0x300, 0, 0xCA, 0x301, 0, // 0BE0-0BEF
			0xCA, 0x303, 0, 0xCA, 0x309, 0, 0xCF, 0x301, 0, 0xD4, 0x300, 0, 0xD4, 0x301, 0, 0xD4, // 0BF0-0BFF
			0x303, 0, 0xD4, 0x309, 0, 0xD5, 0x301, 0, 0xD5, 0x304, 0, 0xD5, 0x308, 0, 0xD6, 0x304, // 0C00-0C0F
			0, 0xD8, 0x301, 0, 0xDC, 0x300, 0, 0xDC, 0x301, 0, 0xDC, 0x304, 0, 0xDC, 0x30C, 0, // 0C10-0C1F
			0xE2, 0x300, 0, 0xE2, 0x301, 0, 0xE2, 0x303, 0, 0xE2, 0x309, 0, 0xE4, 0x304, 0, 0xE5, // 0C20-0C2F
			0x301, 0, 0xE6, 0x301, 0, 0xE6, 0x304, 0, 0xE7, 0x301, 0, 0xEA, 0x300, 0, 0xEA, 0x301, // 0C30-0C3F
			0, 0xEA, 0x303, 0, 0xEA, 0x309, 0, 0xEF, 0x301, 0, 0xF0, 0, 0xF4, 0x300, 0, 0xF4, // 0C40-0C4F
			0x301, 0, 0xF4, 0x303, 0, 0xF4, 0x309, 0, 0xF5, 0x301, 0, 0xF5, 0x304, 0, 0xF5, 0x308, // 0C50-0C5F
			0, 0xF6, 0x304, 0, 0xF8, 0x301, 0, 0xFC, 0x300, 0, 0xFC, 0x301, 0, 0xFC, 0x304, 0, // 0C60-0C6F
			0xFC, 0x30C, 0, 0x102, 0x300, 0, 0x102, 0x301, 0, 0x102, 0x303, 0, 0x102, 0x309, 0, 0x103, // 0C70-0C7F
			0x300, 0, 0x103, 0x301, 0, 0x103, 0x303, 0, 0x103, 0x309, 0, 0x112, 0x300, 0, 0x112, 0x301, // 0C80-0C8F
			0, 0x113, 0x300, 0, 0x113, 0x301, 0, 0x126, 0, 0x127, 0, 0x14B, 0, 0x14C, 0x300, 0, // 0C90-0C9F
			0x14C, 0x301, 0, 0x14D, 0x300, 0, 0x14D, 0x301, 0, 0x153, 0, 0x15A, 0x307, 0, 0x15B, 0x307, // 0CA0-0CAF
			0, 0x160, 0x307, 0, 0x161, 0x307, 0, 0x168, 0x301, 0, 0x169, 0x301, 0, 0x16A, 0x308, 0, // 0CB0-0CBF
			0x16B, 0x308, 0, 0x17F, 0x74, 0, 0x17F, 0x307, 0, 0x18E, 0, 0x190, 0, 0x1A0, 0x300, 0, // 0CC0-0CCF
			0x1A0, 0x301, 0, 0x1A0, 0x303, 0, 0x1A0, 0x309, 0, 0x1A0, 0x323, 0, 0x1A1, 0x300, 0, 0x1A1, // 0CD0-0CDF
			0x301, 0, 0x1A1, 0x303, 0, 0x1A1, 0x309, 0, 0x1A1, 0x323, 0, 0x1AB, 0, 0x1AF, 0x300, 0, // 0CE0-0CEF
			0x1AF, 0x301, 0, 0x1AF, 0x303, 0, 0x1AF, 0x309, 0, 0x1AF, 0x323, 0, 0x1B0, 0x300, 0, 0x1B0, // 0CF0-0CFF
			0x301, 0, 0x1B0, 0x303, 0, 0x1B0, 0x309, 0, 0x1B0, 0x323, 0, 0x1B7, 0x30C, 0, 0x1EA, 0x304, // 0D00-0D0F
			0, 0x1EB, 0x304, 0, 0x222, 0, 0x226, 0x304, 0, 0x227, 0x304, 0, 0x228, 0x306, 0, 0x229, // 0D10-0D1F
			0x306, 0, 0x22E, 0x304, 0, 0x22F, 0x304, 0, 0x250, 0, 0x251, 0, 0x252, 0, 0x254, 0, // 0D20-0D2F
			0x255, 0, 0x259, 0, 0x25B, 0, 0x25C, 0, 0x25F, 0, 0x261, 0, 0x263, 0, 0x265, 0, // 0D30-0D3F
			0x266, 0, 0x268, 0, 0x269, 0, 0x26A, 0, 0x26D, 0, 0x26F, 0, 0x270, 0, 0x271, 0, // 0D40-0D4F
			0x272, 0, 0x273, 0, 0x274, 0, 0x275, 0, 0x278, 0, 0x279, 0, 0x27B, 0, 0x281, 0, // 0D50-0D5F
			0x282, 0, 0x283, 0, 0x289, 0, 0x28A, 0, 0x28B, 0, 0x28C, 0, 0x290, 0, 0x291, 0, // 0D60-0D6F
			0x292, 0, 0x292, 0x30C, 0, 0x295, 0, 0x29D, 0, 0x29F, 0, 0x2B9, 0, 0x2BC, 0x6E, 0, // 0D70-0D7F
			0x300, 0, 0x301, 0, 0x308, 0x301, 0, 0x313, 0, 0x385, 0, 0x386, 0, 0x388, 0, 0x389, // 0D80-0D8F
			0, 0x38A, 0, 0x38C, 0, 0x38E, 0, 0x38F, 0, 0x390, 0, 0x391, 0x300, 0, 0x391, 0x301, // 0D90-0D9F
			0, 0x391, 0x304, 0, 0x391, 0x306, 0, 0x391, 0x313, 0, 0x391, 0x314, 0, 0x391, 0x345, 0, // 0DA0-0DAF
			0x393, 0, 0x395, 0x300, 0, 0x395, 0x301, 0, 0x395, 0x313, 0, 0x395, 0x314, 0, 0x397, 0x300, // 0DB0-0DBF
			0, 0x397, 0x301, 0, 0x397, 0x313, 0, 0x397, 0x314, 0, 0x397, 0x345, 0, 0x398, 0, 0x399, // 0DC0-0DCF
			0x300, 0, 0x399, 0x301, 0, 0x399, 0x304, 0, 0x399, 0x306, 0, 0x399, 0x308, 0, 0x399, 0x313, // 0DD0-0DDF
			0, 0x399, 0x314, 0, 0x39F, 0x300, 0, 0x39F, 0x301, 0, 0x39F, 0x313, 0, 0x39F, 0x314, 0, // 0DE0-0DEF
			0x3A0, 0, 0x3A1, 0x314, 0, 0x3A3, 0, 0x3A5, 0, 0x3A5, 0x300, 0, 0x3A5, 0x301, 0, 0x3A5, // 0DF0-0DFF
			0x304, 0, 0x3A5, 0x306, 0, 0x3A5, 0x308, 0, 0x3A5, 0x314, 0, 0x3A9, 0, 0x3A9, 0x300, 0, // 0E00-0E0F
			0x3A9, 0x301, 0, 0x3A9, 0x313, 0, 0x3A9, 0x314, 0, 0x3A9, 0x345, 0, 0x3AC, 0, 0x3AC, 0x345, // 0E10-0E1F
			0, 0x3AD, 0, 0x3AE, 0, 0x3AE, 0x345, 0, 0x3AF, 0, 0x3B0, 0, 0x3B1, 0x300, 0, 0x3B1, // 0E20-0E2F
			0x301, 0, 0x3B1, 0x304, 0, 0x3B1, 0x306, 0, 0x3B1, 0x313, 0, 0x3B1, 0x314, 0, 0x3B1, 0x342, // 0E30-0E3F
			0, 0x3B1, 0x345, 0, 0x3B2, 0, 0x3B3, 0, 0x3B4, 0, 0x3B5, 0, 0x3B5, 0x300, 0, 0x3B5, // 0E40-0E4F
			0x301, 0, 0x3B5, 0x313, 0, 0x3B5, 0x314, 0, 0x3B7, 0x300, 0, 0x3B7, 0x301, 0, 0x3B7, 0x313, // 0E50-0E5F
			0, 0x3B7, 0x314, 0, 0x3B7, 0x342, 0, 0x3B7, 0x345, 0, 0x3B8, 0, 0x3B9, 0, 0x3B9, 0x300, // 0E60-0E6F
			0, 0x3B9, 0x301, 0, 0x3B9, 0x304, 0, 0x3B9, 0x306, 0, 0x3B9, 0x308, 0, 0x3B9, 0x313, 0, // 0E70-0E7F
			0x3B9, 0x314, 0, 0x3B9, 0x342, 0, 0x3BA, 0, 0x3BC, 0, 0x3BC, 0x41, 0, 0x3BC, 0x46, 0, // 0E80-0E8F
			0x3BC, 0x56, 0, 0x3BC, 0x57, 0, 0x3BC, 0x67, 0, 0x3BC, 0x6D, 0, 0x3BC, 0x73, 0, 0x3BC, // 0E90-0E9F
			0x2113, 0, 0x3BF, 0x300, 0, 0x3BF, 0x301, 0, 0x3BF, 0x313, 0, 0x3BF, 0x314, 0, 0x3C0, 0, // 0EA0-0EAF
			0x3C1, 0, 0x3C1, 0x313, 0, 0x3C1, 0x314, 0, 0x3C2, 0, 0x3C5, 0x300, 0, 0x3C5, 0x301, 0, // 0EB0-0EBF
			0x3C5, 0x304, 0, 0x3C5, 0x306, 0, 0x3C5, 0x308, 0, 0x3C5, 0x313, 0, 0x3C5, 0x314, 0, 0x3C5, // 0EC0-0ECF
			0x342, 0, 0x3C6, 0, 0x3C7, 0, 0x3C9, 0x300, 0, 0x3C9, 0x301, 0, 0x3C9, 0x313, 0, 0x3C9, // 0ED0-0EDF
			0x314, 0, 0x3C9, 0x342, 0, 0x3C9, 0x345, 0, 0x3CA, 0x300, 0, 0x3CA, 0x301, 0, 0x3CA, 0x342, // 0EE0-0EEF
			0, 0x3CB, 0x300, 0, 0x3CB, 0x301, 0, 0x3CB, 0x342, 0, 0x3CC, 0, 0x3CD, 0, 0x3CE, 0, // 0EF0-0EFF
			0x3CE, 0x345, 0, 0x3D2, 0x301, 0, 0x3D2, 0x308, 0, 0x406, 0x308, 0, 0x410, 0x306, 0, 0x410, // 0F00-0F0F
			0x308, 0, 0x413, 0x301, 0, 0x415, 0x300, 0, 0x415, 0x306, 0, 0x415, 0x308, 0, 0x416, 0x306, // 0F10-0F1F
			0, 0x416, 0x308, 0, 0x417, 0x308, 0, 0x418, 0x300, 0, 0x418, 0x304, 0, 0x418, 0x306, 0, // 0F20-0F2F
			0x418, 0x308, 0, 0x41A, 0x301, 0, 0x41E, 0x308, 0, 0x423, 0x304, 0, 0x423, 0x306, 0, 0x423, // 0F30-0F3F
			0x308, 0, 0x423, 0x30B, 0, 0x427, 0x308, 0, 0x42B, 0x308, 0, 0x42D, 0x308, 0, 0x430, 0x306, // 0F40-0F4F
			0, 0x430, 0x308, 0, 0x433, 0x301, 0, 0x435, 0x300, 0, 0x435, 0x306, 0, 0x435, 0x308, 0, // 0F50-0F5F
			0x436, 0x306, 0, 0x436, 0x308, 0, 0x437, 0x308, 0, 0x438, 0x300, 0, 0x438, 0x304, 0, 0x438, // 0F60-0F6F
			0x306, 0, 0x438, 0x308, 0, 0x43A, 0x301, 0, 0x43D, 0, 0x43E, 0x308, 0, 0x443, 0x304, 0, // 0F70-0F7F
			0x443, 0x306, 0, 0x443, 0x308, 0, 0x443, 0x30B, 0, 0x447, 0x308, 0, 0x44B, 0x308, 0, 0x44D, // 0F80-0F8F
			0x308, 0, 0x456, 0x308, 0, 0x474, 0x30F, 0, 0x475, 0x30F, 0, 0x4D8, 0x308, 0, 0x4D9, 0x308, // 0F90-0F9F
			0, 0x4E8, 0x308, 0, 0x4E9, 0x308, 0, 0x565, 0x582, 0, 0x574, 0x565, 0, 0x574, 0x56B, 0, // 0FA0-0FAF
			0x574, 0x56D, 0, 0x574, 0x576, 0, 0x57E, 0x576, 0, 0x5D0, 0, 0x5D0, 0x5B7, 0, 0x5D0, 0x5B8, // 0FB0-0FBF
			0, 0x5D0, 0x5BC, 0, 0x5D0, 0x5DC, 0, 0x5D1, 0, 0x5D1, 0x5BC, 0, 0x5D1, 0x5BF, 0, 0x5D2, // 0FC0-0FCF
			0, 0x5D2, 0x5BC, 0, 0x5D3, 0, 0x5D3, 0x5BC, 0, 0x5D4, 0, 0x5D4, 0x5BC, 0, 0x5D5, 0x5B9, // 0FD0-0FDF
			0, 0x5D5, 0x5BC, 0, 0x5D6, 0x5BC, 0, 0x5D8, 0x5BC, 0, 0x5D9, 0x5B4, 0, 0x5D9, 0x5BC, 0, // 0FE0-0FEF
			0x5DA, 0x5BC, 0, 0x5DB, 0, 0x5DB, 0x5BC, 0, 0x5DB, 0x5BF, 0, 0x5DC, 0, 0x5DC, 0x5BC, 0, // 0FF0-0FFF
			0x5DD, 0, 0x5DE, 0x5BC, 0, 0x5E0, 0x5BC, 0, 0x5E1, 0x5BC, 0, 0x5E2, 0, 0x5E3, 0x5BC, 0, // 1000-100F
			0x5E4, 0x5BC, 0, 0x5E4, 0x5BF, 0, 0x5E6, 0x5BC, 0, 0x5E7, 0x5BC, 0, 0x5E8, 0, 0x5E8, 0x5BC, // 1010-101F
			0, 0x5E9, 0x5BC, 0, 0x5E9, 0x5C1, 0, 0x5E9, 0x5C2, 0, 0x5EA, 0, 0x5EA, 0x5BC, 0, 0x5F2, // 1020-102F
			0x5B7, 0, 0x621, 0, 0x622, 0, 0x623, 0, 0x624, 0, 0x625, 0, 0x626, 0, 0x626, 0x627, // 1030-103F
			0, 0x626, 0x62C, 0, 0x626, 0x62D, 0, 0x626, 0x62E, 0, 0x626, 0x631, 0, 0x626, 0x632, 0, // 1040-104F
			0x626, 0x645, 0, 0x626, 0x646, 0, 0x626, 0x647, 0, 0x626, 0x648, 0, 0x626, 0x649, 0, 0x626, // 1050-105F
			0x64A, 0, 0x626, 0x6C6, 0, 0x626, 0x6C7, 0, 0x626, 0x6C8, 0, 0x626, 0x6D0, 0, 0x626, 0x6D5, // 1060-106F
			0, 0x627, 0, 0x627, 0x643, 0x628, 0x631, 0, 0x627, 0x644, 0x644, 0x647, 0, 0x627, 0x64B, 0, // 1070-107F
			0x627, 0x653, 0, 0x627, 0x654, 0, 0x627, 0x655, 0, 0x627, 0x674, 0, 0x628, 0, 0x628, 0x62C, // 1080-108F
			0, 0x628, 0x62D, 0, 0x628, 0x62D, 0x64A, 0, 0x628, 0x62E, 0, 0x628, 0x62E, 0x64A, 0, 0x628, // 1090-109F
			0x631, 0, 0x628, 0x632, 0, 0x628, 0x645, 0, 0x628, 0x646, 0, 0x628, 0x647, 0, 0x628, 0x649, // 10A0-10AF
			0, 0x628, 0x64A, 0, 0x629, 0, 0x62A, 0, 0x62A, 0x62C, 0, 0x62A, 0x62C, 0x645, 0, 0x62A, // 10B0-10BF
			0x62C, 0x649, 0, 0x62A, 0x62C, 0x64A, 0, 0x62A, 0x62D, 0, 0x62A, 0x62D, 0x62C, 0, 0x62A, 0x62D, // 10C0-10CF
			0x645, 0, 0x62A, 0x62E, 0, 0x62A, 0x62E, 0x645, 0, 0x62A, 0x62E, 0x649, 0, 0x62A, 0x62E, 0x64A, // 10D0-10DF
			0, 0x62A, 0x631, 0, 0x62A, 0x632, 0, 0x62A, 0x645, 0, 0x62A, 0x645, 0x62C, 0, 0x62A, 0x645, // 10E0-10EF
			0x62D, 0, 0x62A, 0x645, 0x62E, 0, 0x62A, 0x645, 0x649, 0, 0x62A, 0x645, 0x64A, 0, 0x62A, 0x646, // 10F0-10FF
			0, 0x62A, 0x647, 0, 0x62A, 0x649, 0, 0x62A, 0x64A, 0, 0x62B, 0, 0x62B, 0x62C, 0, 0x62B, // 1100-110F
			0x631, 0, 0x62B, 0x632, 0, 0x62B, 0x645, 0, 0x62B, 0x646, 0, 0x62B, 0x647, 0, 0x62B, 0x649, // 1110-111F
			0, 0x62B, 0x64A, 0, 0x62C, 0, 0x62C, 0x62D, 0, 0x62C, 0x62D, 0x649, 0, 0x62C, 0x62D, 0x64A, // 1120-112F
			0, 0x62C, 0x644, 0x20, 0x62C, 0x644, 0x627, 0x644, 0x647, 0, 0x62C, 0x645, 0, 0x62C, 0x645, 0x62D, // 1130-113F
			0, 0x62C, 0x645, 0x649, 0, 0x62C, 0x645, 0x64A, 0, 0x62C, 0x649, 0, 0x62C, 0x64A, 0, 0x62D, // 1140-114F
			0, 0x62D, 0x62C, 0, 0x62D, 0x62C, 0x64A, 0, 0x62D, 0x645, 0, 0x62D, 0x645, 0x649, 0, 0x62D, // 1150-115F
			0x645, 0x64A, 0, 0x62D, 0x649, 0, 0x62D, 0x64A, 0, 0x62E, 0, 0x62E, 0x62C, 0, 0x62E, 0x62D, // 1160-116F
			0, 0x62E, 0x645, 0, 0x62E, 0x649, 0, 0x62E, 0x64A, 0, 0x62F, 0, 0x630, 0, 0x630, 0x670, // 1170-117F
			0, 0x631, 0, 0x631, 0x633, 0x648, 0x644, 0, 0x631, 0x670, 0, 0x631, 0x6CC, 0x627, 0x644, 0, // 1180-118F
			0x632, 0, 0x633, 0, 0x633, 0x62C, 0, 0x633, 0x62C, 0x62D, 0, 0x633, 0x62C, 0x649, 0, 0x633, // 1190-119F
			0x62D, 0, 0x633, 0x62D, 0x62C, 0, 0x633, 0x62E, 0, 0x633, 0x62E, 0x649, 0, 0x633, 0x62E, 0x64A, // 11A0-11AF
			0, 0x633, 0x631, 0, 0x633, 0x645, 0, 0x633, 0x645, 0x62C, 0, 0x633, 0x645, 0x62D, 0, 0x633, // 11B0-11BF
			0x645, 0x645, 0, 0x633, 0x647, 0, 0x633, 0x649, 0, 0x633, 0x64A, 0, 0x634, 0, 0x634, 0x62C, // 11C0-11CF
			0, 0x634, 0x62C, 0x64A, 0, 0x634, 0x62D, 0, 0x634, 0x62D, 0x645, 0, 0x634, 0x62D, 0x64A, 0, // 11D0-11DF
			0x634, 0x62E, 0, 0x634, 0x631, 0, 0x634, 0x645, 0, 0x634, 0x645, 0x62E, 0, 0x634, 0x645, 0x645, // 11E0-11EF
			0, 0x634, 0x647, 0, 0x634, 0x649, 0, 0x634, 0x64A, 0, 0x635, 0, 0x635, 0x62D, 0, 0x635, // 11F0-11FF
			0x62D, 0x62D, 0, 0x635, 0x62D, 0x64A, 0, 0x635, 0x62E, 0, 0x635, 0x631, 0, 0x635, 0x644, 0x639, // 1200-120F
			0x645, 0, 0x635, 0x644, 0x649, 0, 0x635, 0x644, 0x649, 0x20, 0x627, 0x644, 0x644, 0x647, 0x20, 0x639, // 1210-121F
			0x644, 0x64A, 0x647, 0x20, 0x648, 0x633, 0x644, 0x645, 0, 0x635, 0x644, 0x6D2, 0, 0x635, 0x645, 0, // 1220-122F
			0x635, 0x645, 0x645, 0, 0x635, 0x649, 0, 0x635, 0x64A, 0, 0x636, 0, 0x636, 0x62C, 0, 0x636, // 1230-123F
			0x62D, 0, 0x636, 0x62D, 0x649, 0, 0x636, 0x62D, 0x64A, 0, 0x636, 0x62E, 0, 0x636, 0x62E, 0x645, // 1240-124F
			0, 0x636, 0x631, 0, 0x636, 0x645, 0, 0x636, 0x649, 0, 0x636, 0x64A, 0, 0x637, 0, 0x637, // 1250-125F
			0x62D, 0, 0x637, 0x645, 0, 0x637, 0x645, 0x62D, 0, 0x637, 0x645, 0x645, 0, 0x637, 0x645, 0x64A, // 1260-126F
			0, 0x637, 0x649, 0, 0x637, 0x64A, 0, 0x638, 0, 0x638, 0x645, 0, 0x639, 0, 0x639, 0x62C, // 1270-127F
			0, 0x639, 0x62C, 0x645, 0, 0x639, 0x644, 0x64A, 0x647, 0, 0x639, 0x645, 0, 0x639, 0x645, 0x645, // 1280-128F
			0, 0x639, 0x645, 0x649, 0, 0x639, 0x645, 0x64A, 0, 0x639, 0x649, 0, 0x639, 0x64A, 0, 0x63A, // 1290-129F
			0, 0x63A, 0x62C, 0, 0x63A, 0x645, 0, 0x63A, 0x645, 0x645, 0, 0x63A, 0x645, 0x649, 0, 0x63A, // 12A0-12AF
			0x645, 0x64A, 0, 0x63A, 0x649, 0, 0x63A, 0x64A, 0, 0x640, 0x64B, 0, 0x640, 0x64E, 0, 0x640, // 12B0-12BF
			0x64E, 0x651, 0, 0x640, 0x64F, 0, 0x640, 0x64F, 0x651, 0, 0x640, 0x650, 0, 0x640, 0x650, 0x651, // 12C0-12CF
			0, 0x640, 0x651, 0, 0x640, 0x652, 0, 0x641, 0, 0x641, 0x62C, 0, 0x641, 0x62D, 0, 0x641, // 12D0-12DF
			0x62E, 0, 0x641, 0x62E, 0x645, 0, 0x641, 0x645, 0, 0x641, 0x645, 0x64A, 0, 0x641, 0x649, 0, // 12E0-12EF
			0x641, 0x64A, 0, 0x642, 0, 0x642, 0x62D, 0, 0x642, 0x644, 0x6D2, 0, 0x642, 0x645, 0, 0x642, // 12F0-12FF
			0x645, 0x62D, 0, 0x642, 0x645, 0x645, 0, 0x642, 0x645, 0x64A, 0, 0x642, 0x649, 0, 0x642, 0x64A, // 1300-130F
			0, 0x643, 0, 0x643, 0x627, 0, 0x643, 0x62C, 0, 0x643, 0x62D, 0, 0x643, 0x62E, 0, 0x643, // 1310-131F
			0x644, 0, 0x643, 0x645, 0, 0x643, 0x645, 0x645, 0, 0x643, 0x645, 0x64A, 0, 0x643, 0x649, 0, // 1320-132F
			0x643, 0x64A, 0, 0x644, 0, 0x644, 0x622, 0, 0x644, 0x623, 0, 0x644, 0x625, 0, 0x644, 0x627, // 1330-133F
			0, 0x644, 0x62C, 0, 0x644, 0x62C, 0x62C, 0, 0x644, 0x62C, 0x645, 0, 0x644, 0x62C, 0x64A, 0, // 1340-134F
			0x644, 0x62D, 0, 0x644, 0x62D, 0x645, 0, 0x644, 0x62D, 0x649, 0, 0x644, 0x62D, 0x64A, 0, 0x644, // 1350-135F
			0x62E, 0, 0x644, 0x62E, 0x645, 0, 0x644, 0x645, 0, 0x644, 0x645, 0x62D, 0, 0x644, 0x645, 0x64A, // 1360-136F
			0, 0x644, 0x647, 0, 0x644, 0x649, 0, 0x644, 0x64A, 0, 0x645, 0, 0x645, 0x627, 0, 0x645, // 1370-137F
			0x62C, 0, 0x645, 0x62C, 0x62D, 0, 0x645, 0x62C, 0x62E, 0, 0x645, 0x62C, 0x645, 0, 0x645, 0x62C, // 1380-138F
			0x64A, 0, 0x645, 0x62D, 0, 0x645, 0x62D, 0x62C, 0, 0x645, 0x62D, 0x645, 0, 0x645, 0x62D, 0x645, // 1390-139F
			0x62F, 0, 0x645, 0x62D, 0x64A, 0, 0x645, 0x62E, 0, 0x645, 0x62E, 0x62C, 0, 0x645, 0x62E, 0x645, // 13A0-13AF
			0, 0x645, 0x62E, 0x64A, 0, 0x645, 0x645, 0, 0x645, 0x645, 0x64A, 0, 0x645, 0x649, 0, 0x645, // 13B0-13BF
			0x64A, 0, 0x646, 0, 0x646, 0x62C, 0, 0x646, 0x62C, 0x62D, 0, 0x646, 0x62C, 0x645, 0, 0x646, // 13C0-13CF
			0x62C, 0x649, 0, 0x646, 0x62C, 0x64A, 0, 0x646, 0x62D, 0, 0x646, 0x62D, 0x645, 0, 0x646, 0x62D, // 13D0-13DF
			0x649, 0, 0x646, 0x62D, 0x64A, 0, 0x646, 0x62E, 0, 0x646, 0x631, 0, 0x646, 0x632, 0, 0x646, // 13E0-13EF
			0x645, 0, 0x646, 0x645, 0x649, 0, 0x646, 0x645, 0x64A, 0, 0x646, 0x646, 0, 0x646, 0x647, 0, // 13F0-13FF
			0x646, 0x649, 0, 0x646, 0x64A, 0, 0x647, 0, 0x647, 0x62C, 0, 0x647, 0x645, 0, 0x647, 0x645, // 1400-140F
			0x62C, 0, 0x647, 0x645, 0x645, 0, 0x647, 0x649, 0, 0x647, 0x64A, 0, 0x647, 0x670, 0, 0x648, // 1410-141F
			0, 0x648, 0x633, 0x644, 0x645, 0, 0x648, 0x654, 0, 0x648, 0x674, 0, 0x649, 0, 0x649, 0x670, // 1420-142F
			0, 0x64A, 0, 0x64A, 0x62C, 0, 0x64A, 0x62C, 0x64A, 0, 0x64A, 0x62D, 0, 0x64A, 0x62D, 0x64A, // 1430-143F
			0, 0x64A, 0x62E, 0, 0x64A, 0x631, 0, 0x64A, 0x632, 0, 0x64A, 0x645, 0, 0x64A, 0x645, 0x645, // 1440-144F
			0, 0x64A, 0x645, 0x64A, 0, 0x64A, 0x646, 0, 0x64A, 0x647, 0, 0x64A, 0x649, 0, 0x64A, 0x64A, // 1450-145F
			0, 0x64A, 0x654, 0, 0x64A, 0x674, 0, 0x671, 0, 0x677, 0, 0x679, 0, 0x67A, 0, 0x67B, // 1460-146F
			0, 0x67E, 0, 0x67F, 0, 0x680, 0, 0x683, 0, 0x684, 0, 0x686, 0, 0x687, 0, 0x688, // 1470-147F
			0, 0x68C, 0, 0x68D, 0, 0x68E, 0, 0x691, 0, 0x698, 0, 0x6A4, 0, 0x6A6, 0, 0x6A9, // 1480-148F
			0, 0x6AD, 0, 0x6AF, 0, 0x6B1, 0, 0x6B3, 0, 0x6BA, 0, 0x6BB, 0, 0x6BE, 0, 0x6C0, // 1490-149F
			0, 0x6C1, 0, 0x6C1, 0x654, 0, 0x6C5, 0, 0x6C6, 0, 0x6C7, 0, 0x6C7, 0x674, 0, 0x6C8, // 14A0-14AF
			0, 0x6C9, 0, 0x6CB, 0, 0x6CC, 0, 0x6D0, 0, 0x6D2, 0, 0x6D2, 0x654, 0, 0x6D3, 0, // 14B0-14BF
			0x6D5, 0x654, 0, 0x915, 0x93C, 0, 0x916, 0x93C, 0, 0x917, 0x93C, 0, 0x91C, 0x93C, 0, 0x921, // 14C0-14CF
			0x93C, 0, 0x922, 0x93C, 0, 0x928, 0x93C, 0, 0x92B, 0x93C, 0, 0x92F, 0x93C, 0, 0x930, 0x93C, // 14D0-14DF
			0, 0x933, 0x93C, 0, 0x9A1, 0x9BC, 0, 0x9A2, 0x9BC, 0, 0x9AF, 0x9BC, 0, 0x9C7, 0x9BE, 0, // 14E0-14EF
			0x9C7, 0x9D7, 0, 0xA16, 0xA3C, 0, 0xA17, 0xA3C, 0, 0xA1C, 0xA3C, 0, 0xA2B, 0xA3C, 0, 0xA32, // 14F0-14FF
			0xA3C, 0, 0xA38, 0xA3C, 0, 0xB21, 0xB3C, 0, 0xB22, 0xB3C, 0, 0xB47, 0xB3E, 0, 0xB47, 0xB56, // 1500-150F
			0, 0xB47, 0xB57, 0, 0xB92, 0xBD7, 0, 0xBC6, 0xBBE, 0, 0xBC6, 0xBD7, 0, 0xBC7, 0xBBE, 0, // 1510-151F
			0xC46, 0xC56, 0, 0xCBF, 0xCD5, 0, 0xCC6, 0xCC2, 0, 0xCC6, 0xCD5, 0, 0xCC6, 0xCD6, 0, 0xCCA, // 1520-152F
			0xCD5, 0, 0xD46, 0xD3E, 0, 0xD46, 0xD57, 0, 0xD47, 0xD3E, 0, 0xDD9, 0xDCA, 0, 0xDD9, 0xDCF, // 1530-153F
			0, 0xDD9, 0xDDF, 0, 0xDDC, 0xDCA, 0, 0xE4D, 0xE32, 0, 0xEAB, 0xE99, 0, 0xEAB, 0xEA1, 0, // 1540-154F
			0xECD, 0xEB2, 0, 0xF0B, 0, 0xF40, 0xFB5, 0, 0xF42, 0xFB7, 0, 0xF4C, 0xFB7, 0, 0xF51, 0xFB7, // 1550-155F
			0, 0xF56, 0xFB7, 0, 0xF5B, 0xFB7, 0, 0xF71, 0xF72, 0, 0xF71, 0xF74, 0, 0xF71, 0xF80, 0, // 1560-156F
			0xF90, 0xFB5, 0, 0xF92, 0xFB7, 0, 0xF9C, 0xFB7, 0, 0xFA1, 0xFB7, 0, 0xFA6, 0xFB7, 0, 0xFAB, // 1570-157F
			0xFB7, 0, 0xFB2, 0xF80, 0, 0xFB2, 0xF81, 0, 0xFB3, 0xF80, 0, 0xFB3, 0xF81, 0, 0x1025, 0x102E, // 1580-158F
			0, 0x10DC, 0, 0x1100, 0, 0x1100, 0x1161, 0, 0x1101, 0, 0x1102, 0, 0x1102, 0x1161, 0, 0x1103, // 1590-159F
			0, 0x1103, 0x1161, 0, 0x1104, 0, 0x1105, 0, 0x1105, 0x1161, 0, 0x1106, 0, 0x1106, 0x1161, 0, // 15A0-15AF
			0x1107, 0, 0x1107, 0x1161, 0, 0x1108, 0, 0x1109, 0, 0x1109, 0x1161, 0, 0x110A, 0, 0x110B, 0, // 15B0-15BF
			0x110B, 0x1161, 0, 0x110B, 0x116E, 0, 0x110C, 0, 0x110C, 0x1161, 0, 0x110C, 0x116E, 0x110B, 0x1174, 0, // 15C0-15CF
			0x110D, 0, 0x110E, 0, 0x110E, 0x1161, 0, 0x110E, 0x1161, 0x11B7, 0x1100, 0x1169, 0, 0x110F, 0, 0x110F, // 15D0-15DF
			0x1161, 0, 0x1110, 0, 0x1110, 0x1161, 0, 0x1111, 0, 0x1111, 0x1161, 0, 0x1112, 0, 0x1112, 0x1161, // 15E0-15EF
			0, 0x1114, 0, 0x1115, 0, 0x111A, 0, 0x111C, 0, 0x111D, 0, 0x111E, 0, 0x1120, 0, 0x1121, // 15F0-15FF
			0, 0x1122, 0, 0x1123, 0, 0x1127, 0, 0x1129, 0, 0x112B, 0, 0x112C, 0, 0x112D, 0, 0x112E, // 1600-160F
			0, 0x112F, 0, 0x1132, 0, 0x1136, 0, 0x1140, 0, 0x1147, 0, 0x114C, 0, 0x1157, 0, 0x1158, // 1610-161F
			0, 0x1159, 0, 0x1160, 0, 0x1161, 0, 0x1162, 0, 0x1163, 0, 0x1164, 0, 0x1165, 0, 0x1166, // 1620-162F
			0, 0x1167, 0, 0x1168, 0, 0x1169, 0, 0x116A, 0, 0x116B, 0, 0x116C, 0, 0x116D, 0, 0x116E, // 1630-163F
			0, 0x116F, 0, 0x1170, 0, 0x1171, 0, 0x1172, 0, 0x1173, 0, 0x1174, 0, 0x1175, 0, 0x1184, // 1640-164F
			0, 0x1185, 0, 0x1188, 0, 0x1191, 0, 0x1192, 0, 0x1194, 0, 0x119E, 0, 0x11A1, 0, 0x11AA, // 1650-165F
			0, 0x11AC, 0, 0x11AD, 0, 0x11B0, 0, 0x11B1, 0, 0x11B2, 0, 0x11B3, 0, 0x11B4, 0, 0x11B5, // 1660-166F
			0, 0x11C7, 0, 0x11C8, 0, 0x11CC, 0, 0x11CE, 0, 0x11D3, 0, 0x11D7, 0, 0x11D9, 0, 0x11DD, // 1670-167F
			0, 0x11DF, 0, 0x11F1, 0, 0x11F2, 0, 0x1B05, 0x1B35, 0, 0x1B07, 0x1B35, 0, 0x1B09, 0x1B35, 0, // 1680-168F
			0x1B0B, 0x1B35, 0, 0x1B0D, 0x1B35, 0, 0x1B11, 0x1B35, 0, 0x1B3A, 0x1B35, 0, 0x1B3C, 0x1B35, 0, 0x1B3E, // 1690-169F
			0x1B35, 0, 0x1B3F, 0x1B35, 0, 0x1B42, 0x1B35, 0, 0x1D02, 0, 0x1D16, 0, 0x1D17, 0, 0x1D1C, 0, // 16A0-16AF
			0x1D1D, 0, 0x1D25, 0, 0x1D7B, 0, 0x1D85, 0, 0x1E36, 0x304, 0, 0x1E37, 0x304, 0, 0x1E5A, 0x304, // 16B0-16BF
			0, 0x1E5B, 0x304, 0, 0x1E62, 0x307, 0, 0x1E63, 0x307, 0, 0x1EA0, 0x302, 0, 0x1EA0, 0x306, 0, // 16C0-16CF
			0x1EA1, 0x302, 0, 0x1EA1, 0x306, 0, 0x1EB8, 0x302, 0, 0x1EB9, 0x302, 0, 0x1ECC, 0x302, 0, 0x1ECD, // 16D0-16DF
			0x302, 0, 0x1F00, 0x300, 0, 0x1F00, 0x301, 0, 0x1F00, 0x342, 0, 0x1F00, 0x345, 0, 0x1F01, 0x300, // 16E0-16EF
			0, 0x1F01, 0x301, 0, 0x1F01, 0x342, 0, 0x1F01, 0x345, 0, 0x1F02, 0x345, 0, 0x1F03, 0x345, 0, // 16F0-16FF
			0x1F04, 0x345, 0, 0x1F05, 0x345, 0, 0x1F06, 0x345, 0, 0x1F07, 0x345, 0, 0x1F08, 0x300, 0, 0x1F08, // 1700-170F
			0x301, 0, 0x1F08, 0x342, 0, 0x1F08, 0x345, 0, 0x1F09, 0x300, 0, 0x1F09, 0x301, 0, 0x1F09, 0x342, // 1710-171F
			0, 0x1F09, 0x345, 0, 0x1F0A, 0x345, 0, 0x1F0B, 0x345, 0, 0x1F0C, 0x345, 0, 0x1F0D, 0x345, 0, // 1720-172F
			0x1F0E, 0x345, 0, 0x1F0F, 0x345, 0, 0x1F10, 0x300, 0, 0x1F10, 0x301, 0, 0x1F11, 0x300, 0, 0x1F11, // 1730-173F
			0x301, 0, 0x1F18, 0x300, 0, 0x1F18, 0x301, 0, 0x1F19, 0x300, 0, 0x1F19, 0x301, 0, 0x1F20, 0x300, // 1740-174F
			0, 0x1F20, 0x301, 0, 0x1F20, 0x342, 0, 0x1F20, 0x345, 0, 0x1F21, 0x300, 0, 0x1F21, 0x301, 0, // 1750-175F
			0x1F21, 0x342, 0, 0x1F21, 0x345, 0, 0x1F22, 0x345, 0, 0x1F23, 0x345, 0, 0x1F24, 0x345, 0, 0x1F25, // 1760-176F
			0x345, 0, 0x1F26, 0x345, 0, 0x1F27, 0x345, 0, 0x1F28, 0x300, 0, 0x1F28, 0x301, 0, 0x1F28, 0x342, // 1770-177F
			0, 0x1F28, 0x345, 0, 0x1F29, 0x300, 0, 0x1F29, 0x301, 0, 0x1F29, 0x342, 0, 0x1F29, 0x345, 0, // 1780-178F
			0x1F2A, 0x345, 0, 0x1F2B, 0x345, 0, 0x1F2C, 0x345, 0, 0x1F2D, 0x345, 0, 0x1F2E, 0x345, 0, 0x1F2F, // 1790-179F
			0x345, 0, 0x1F30, 0x300, 0, 0x1F30, 0x301, 0, 0x1F30, 0x342, 0, 0x1F31, 0x300, 0, 0x1F31, 0x301, // 17A0-17AF
			0, 0x1F31, 0x342, 0, 0x1F38, 0x300, 0, 0x1F38, 0x301, 0, 0x1F38, 0x342, 0, 0x1F39, 0x300, 0, // 17B0-17BF
			0x1F39, 0x301, 0, 0x1F39, 0x342, 0, 0x1F40, 0x300, 0, 0x1F40, 0x301, 0, 0x1F41, 0x300, 0, 0x1F41, // 17C0-17CF
			0x301, 0, 0x1F48, 0x300, 0, 0x1F48, 0x301, 0, 0x1F49, 0x300, 0, 0x1F49, 0x301, 0, 0x1F50, 0x300, // 17D0-17DF
			0, 0x1F50, 0x301, 0, 0x1F50, 0x342, 0, 0x1F51, 0x300, 0, 0x1F51, 0x301, 0, 0x1F51, 0x342, 0, // 17E0-17EF
			0x1F59, 0x300, 0, 0x1F59, 0x301, 0, 0x1F59, 0x342, 0, 0x1F60, 0x300, 0, 0x1F60, 0x301, 0, 0x1F60, // 17F0-17FF
			0x342, 0, 0x1F60, 0x345, 0, 0x1F61, 0x300, 0, 0x1F61, 0x301, 0, 0x1F61, 0x342, 0, 0x1F61, 0x345, // 1800-180F
			0, 0x1F62, 0x345, 0, 0x1F63, 0x345, 0, 0x1F64, 0x345, 0, 0x1F65, 0x345, 0, 0x1F66, 0x345, 0, // 1810-181F
			0x1F67, 0x345, 0, 0x1F68, 0x300, 0, 0x1F68, 0x301, 0, 0x1F68, 0x342, 0, 0x1F68, 0x345, 0, 0x1F69, // 1820-182F
			0x300, 0, 0x1F69, 0x301, 0, 0x1F69, 0x342, 0, 0x1F69, 0x345, 0, 0x1F6A, 0x345, 0, 0x1F6B, 0x345, // 1830-183F
			0, 0x1F6C, 0x345, 0, 0x1F6D, 0x345, 0, 0x1F6E, 0x345, 0, 0x1F6F, 0x345, 0, 0x1F70, 0x345, 0, // 1840-184F
			0x1F74, 0x345, 0, 0x1F7C, 0x345, 0, 0x1FB6, 0x345, 0, 0x1FBF, 0x300, 0, 0x1FBF, 0x301, 0, 0x1FBF, // 1850-185F
			0x342, 0, 0x1FC6, 0x345, 0, 0x1FF6, 0x345, 0, 0x1FFE, 0x300, 0, 0x1FFE, 0x301, 0, 0x1FFE, 0x342, // 1860-186F
			0, 0x2002, 0, 0x2003, 0, 0x2010, 0, 0x2013, 0, 0x2014, 0, 0x2025, 0, 0x2026, 0, 0x2032, // 1870-187F
			0x2032, 0, 0x2032, 0x2032, 0x2032, 0, 0x2032, 0x2032, 0x2032, 0x2032, 0, 0x2035, 0x2035, 0, 0x2035, 0x2035, // 1880-188F
			0x2035, 0, 0x203E, 0, 0x20A9, 0, 0x2190, 0, 0x2190, 0x338, 0, 0x2191, 0, 0x2192, 0, 0x2192, // 1890-189F
			0x338, 0, 0x2193, 0, 0x2194, 0x338, 0, 0x21D0, 0x338, 0, 0x21D2, 0x338, 0, 0x21D4, 0x338, 0, // 18A0-18AF
			0x2203, 0x338, 0, 0x2208, 0x338, 0, 0x220B, 0x338, 0, 0x2211, 0, 0x2212, 0, 0x2223, 0x338, 0, // 18B0-18BF
			0x2225, 0x338, 0, 0x222B, 0x222B, 0, 0x222B, 0x222B, 0x222B, 0, 0x222B, 0x222B, 0x222B, 0x222B, 0, 0x222E, // 18C0-18CF
			0x222E, 0, 0x222E, 0x222E, 0x222E, 0, 0x223C, 0x338, 0, 0x2243, 0x338, 0, 0x2245, 0x338, 0, 0x2248, // 18D0-18DF
			0x338, 0, 0x224D, 0x338, 0, 0x2261, 0x338, 0, 0x2264, 0x338, 0, 0x2265, 0x338, 0, 0x2272, 0x338, // 18E0-18EF
			0, 0x2273, 0x338, 0, 0x2276, 0x338, 0, 0x2277, 0x338, 0, 0x227A, 0x338, 0, 0x227B, 0x338, 0, // 18F0-18FF
			0x227C, 0x338, 0, 0x227D, 0x338, 0, 0x2282, 0x338, 0, 0x2283, 0x338, 0, 0x2286, 0x338, 0, 0x2287, // 1900-190F
			0x338, 0, 0x2291, 0x338, 0, 0x2292, 0x338, 0, 0x22A2, 0x338, 0, 0x22A8, 0x338, 0, 0x22A9, 0x338, // 1910-191F
			0, 0x22AB, 0x338, 0, 0x22B2, 0x338, 0, 0x22B3, 0x338, 0, 0x22B4, 0x338, 0, 0x22B5, 0x338, 0, // 1920-192F
			0x2502, 0, 0x25A0, 0, 0x25CB, 0, 0x2985, 0, 0x2986, 0, 0x2ADD, 0x338, 0, 0x2D61, 0, 0x3001, // 1930-193F
			0, 0x3002, 0, 0x3008, 0, 0x3009, 0, 0x300A, 0, 0x300B, 0, 0x300C, 0, 0x300D, 0, 0x300E, // 1940-194F
			0, 0x300F, 0, 0x3010, 0, 0x3011, 0, 0x3012, 0, 0x3014, 0, 0x3015, 0, 0x3016, 0, 0x3017, // 1950-195F
			0, 0x3046, 0x3099, 0, 0x304B, 0x3099, 0, 0x304D, 0x3099, 0, 0x304F, 0x3099, 0, 0x3051, 0x3099, 0, // 1960-196F
			0x3053, 0x3099, 0, 0x3055, 0x3099, 0, 0x3057, 0x3099, 0, 0x3059, 0x3099, 0, 0x305B, 0x3099, 0, 0x305D, // 1970-197F
			0x3099, 0, 0x305F, 0x3099, 0, 0x3061, 0x3099, 0, 0x3064, 0x3099, 0, 0x3066, 0x3099, 0, 0x3068, 0x3099, // 1980-198F
			0, 0x306F, 0x3099, 0, 0x306F, 0x309A, 0, 0x3072, 0x3099, 0, 0x3072, 0x309A, 0, 0x3075, 0x3099, 0, // 1990-199F
			0x3075, 0x309A, 0, 0x3078, 0x3099, 0, 0x3078, 0x309A, 0, 0x307B, 0x3099, 0, 0x307B, 0x309A, 0, 0x3088, // 19A0-19AF
			0x308A, 0, 0x3099, 0, 0x309A, 0, 0x309D, 0x3099, 0, 0x30A1, 0, 0x30A2, 0, 0x30A2, 0x30D1, 0x30FC, // 19B0-19BF
			0x30C8, 0, 0x30A2, 0x30EB, 0x30D5, 0x30A1, 0, 0x30A2, 0x30F3, 0x30DA, 0x30A2, 0, 0x30A2, 0x30FC, 0x30EB, 0, // 19C0-19CF
			0x30A3, 0, 0x30A4, 0, 0x30A4, 0x30CB, 0x30F3, 0x30B0, 0, 0x30A4, 0x30F3, 0x30C1, 0, 0x30A5, 0, 0x30A6, // 19D0-19DF
			0, 0x30A6, 0x3099, 0, 0x30A6, 0x30A9, 0x30F3, 0, 0x30A7, 0, 0x30A8, 0, 0x30A8, 0x30B9, 0x30AF, 0x30FC, // 19E0-19EF
			0x30C9, 0, 0x30A8, 0x30FC, 0x30AB, 0x30FC, 0, 0x30A9, 0, 0x30AA, 0, 0x30AA, 0x30F3, 0x30B9, 0, 0x30AA, // 19F0-19FF
			0x30FC, 0x30E0, 0, 0x30AB, 0, 0x30AB, 0x3099, 0, 0x30AB, 0x30A4, 0x30EA, 0, 0x30AB, 0x30E9, 0x30C3, 0x30C8, // 1A00-1A0F
			0, 0x30AB, 0x30ED, 0x30EA, 0x30FC, 0, 0x30AC, 0x30ED, 0x30F3, 0, 0x30AC, 0x30F3, 0x30DE, 0, 0x30AD, 0, // 1A10-1A1F
			0x30AD, 0x3099, 0, 0x30AD, 0x30E5, 0x30EA, 0x30FC, 0, 0x30AD, 0x30ED, 0, 0x30AD, 0x30ED, 0x30B0, 0x30E9, 0x30E0, // 1A20-1A2F
			0, 0x30AD, 0x30ED, 0x30E1, 0x30FC, 0x30C8, 0x30EB, 0, 0x30AD, 0x30ED, 0x30EF, 0x30C3, 0x30C8, 0, 0x30AE, 0x30AC, // 1A30-1A3F
			0, 0x30AE, 0x30CB, 0x30FC, 0, 0x30AE, 0x30EB, 0x30C0, 0x30FC, 0, 0x30AF, 0, 0x30AF, 0x3099, 0, 0x30AF, // 1A40-1A4F
			0x30EB, 0x30BC, 0x30A4, 0x30ED, 0, 0x30AF, 0x30ED, 0x30FC, 0x30CD, 0, 0x30B0, 0x30E9, 0x30E0, 0, 0x30B0, 0x30E9, // 1A50-1A5F
			0x30E0, 0x30C8, 0x30F3, 0, 0x30B1, 0, 0x30B1, 0x3099, 0, 0x30B1, 0x30FC, 0x30B9, 0, 0x30B3, 0, 0x30B3, // 1A60-1A6F
			0x3099, 0, 0x30B3, 0x30C8, 0, 0x30B3, 0x30EB, 0x30CA, 0, 0x30B3, 0x30FC, 0x30DD, 0, 0x30B5, 0, 0x30B5, // 1A70-1A7F
			0x3099, 0, 0x30B5, 0x30A4, 0x30AF, 0x30EB, 0, 0x30B5, 0x30F3, 0x30C1, 0x30FC, 0x30E0, 0, 0x30B7, 0, 0x30B7, // 1A80-1A8F
			0x3099, 0, 0x30B7, 0x30EA, 0x30F3, 0x30B0, 0, 0x30B9, 0, 0x30B9, 0x3099, 0, 0x30BB, 0, 0x30BB, 0x3099, // 1A90-1A9F
			0, 0x30BB, 0x30F3, 0x30C1, 0, 0x30BB, 0x30F3, 0x30C8, 0, 0x30BD, 0, 0x30BD, 0x3099, 0, 0x30BF, 0, // 1AA0-1AAF
			0x30BF, 0x3099, 0, 0x30C0, 0x30FC, 0x30B9, 0, 0x30C1, 0, 0x30C1, 0x3099, 0, 0x30C3, 0, 0x30C4, 0, // 1AB0-1ABF
			0x30C4, 0x3099, 0, 0x30C6, 0, 0x30C6, 0x3099, 0, 0x30C7, 0x30B7, 0, 0x30C8, 0, 0x30C8, 0x3099, 0, // 1AC0-1ACF
			0x30C8, 0x30F3, 0, 0x30C9, 0x30EB, 0, 0x30CA, 0, 0x30CA, 0x30CE, 0, 0x30CB, 0, 0x30CC, 0, 0x30CD, // 1AD0-1ADF
			0, 0x30CE, 0, 0x30CE, 0x30C3, 0x30C8, 0, 0x30CF, 0, 0x30CF, 0x3099, 0, 0x30CF, 0x309A, 0, 0x30CF, // 1AE0-1AEF
			0x30A4, 0x30C4, 0, 0x30D0, 0x30FC, 0x30EC, 0x30EB, 0, 0x30D1, 0x30FC, 0x30BB, 0x30F3, 0x30C8, 0, 0x30D1, 0x30FC, // 1AF0-1AFF
			0x30C4, 0, 0x30D2, 0, 0x30D2, 0x3099, 0, 0x30D2, 0x309A, 0, 0x30D3, 0x30EB, 0, 0x30D4, 0x30A2, 0x30B9, // 1B00-1B0F
			0x30C8, 0x30EB, 0, 0x30D4, 0x30AF, 0x30EB, 0, 0x30D4, 0x30B3, 0, 0x30D5, 0, 0x30D5, 0x3099, 0, 0x30D5, // 1B10-1B1F
			0x309A, 0, 0x30D5, 0x30A1, 0x30E9, 0x30C3, 0x30C9, 0, 0x30D5, 0x30A3, 0x30FC, 0x30C8, 0, 0x30D5, 0x30E9, 0x30F3, // 1B20-1B2F
			0, 0x30D6, 0x30C3, 0x30B7, 0x30A7, 0x30EB, 0, 0x30D8, 0, 0x30D8, 0x3099, 0, 0x30D8, 0x309A, 0, 0x30D8, // 1B30-1B3F
			0x30AF, 0x30BF, 0x30FC, 0x30EB, 0, 0x30D8, 0x30EB, 0x30C4, 0, 0x30D9, 0x30FC, 0x30BF, 0, 0x30DA, 0x30BD, 0, // 1B40-1B4F
			0x30DA, 0x30CB, 0x30D2, 0, 0x30DA, 0x30F3, 0x30B9, 0, 0x30DA, 0x30FC, 0x30B8, 0, 0x30DB, 0, 0x30DB, 0x3099, // 1B50-1B5F
			0, 0x30DB, 0x309A, 0, 0x30DB, 0x30F3, 0, 0x30DB, 0x30FC, 0x30EB, 0, 0x30DB, 0x30FC, 0x30F3, 0, 0x30DC, // 1B60-1B6F
			0x30EB, 0x30C8, 0, 0x30DD, 0x30A4, 0x30F3, 0x30C8, 0, 0x30DD, 0x30F3, 0x30C9, 0, 0x30DE, 0, 0x30DE, 0x30A4, // 1B70-1B7F
			0x30AF, 0x30ED, 0, 0x30DE, 0x30A4, 0x30EB, 0, 0x30DE, 0x30C3, 0x30CF, 0, 0x30DE, 0x30EB, 0x30AF, 0, 0x30DE, // 1B80-1B8F
			0x30F3, 0x30B7, 0x30E7, 0x30F3, 0, 0x30DF, 0, 0x30DF, 0x30AF, 0x30ED, 0x30F3, 0, 0x30DF, 0x30EA, 0, 0x30DF, // 1B90-1B9F
			0x30EA, 0x30D0, 0x30FC, 0x30EB, 0, 0x30E0, 0, 0x30E1, 0, 0x30E1, 0x30AC, 0, 0x30E1, 0x30AC, 0x30C8, 0x30F3, // 1BA0-1BAF
			0, 0x30E1, 0x30FC, 0x30C8, 0x30EB, 0, 0x30E2, 0, 0x30E3, 0, 0x30E4, 0, 0x30E4, 0x30FC, 0x30C9, 0, // 1BB0-1BBF
			0x30E4, 0x30FC, 0x30EB, 0, 0x30E5, 0, 0x30E6, 0, 0x30E6, 0x30A2, 0x30F3, 0, 0x30E7, 0, 0x30E8, 0, // 1BC0-1BCF
			0x30E9, 0, 0x30EA, 0, 0x30EA, 0x30C3, 0x30C8, 0x30EB, 0, 0x30EA, 0x30E9, 0, 0x30EB, 0, 0x30EB, 0x30D4, // 1BD0-1BDF
			0x30FC, 0, 0x30EB, 0x30FC, 0x30D6, 0x30EB, 0, 0x30EC, 0, 0x30EC, 0x30E0, 0, 0x30EC, 0x30F3, 0x30C8, 0x30B2, // 1BE0-1BEF
			0x30F3, 0, 0x30ED, 0, 0x30EF, 0, 0x30EF, 0x3099, 0, 0x30EF, 0x30C3, 0x30C8, 0, 0x30F0, 0, 0x30F0, // 1BF0-1BFF
			0x3099, 0, 0x30F1, 0, 0x30F1, 0x3099, 0, 0x30F2, 0, 0x30F2, 0x3099, 0, 0x30F3, 0, 0x30FB, 0, // 1C00-1C0F
			0x30FC, 0, 0x30FD, 0x3099, 0, 0x3131, 0, 0x3132, 0, 0x3133, 0, 0x3134, 0, 0x3135, 0, 0x3136, // 1C10-1C1F
			0, 0x3137, 0, 0x3138, 0, 0x3139, 0, 0x313A, 0, 0x313B, 0, 0x313C, 0, 0x313D, 0, 0x313E, // 1C20-1C2F
			0, 0x313F, 0, 0x3140, 0, 0x3141, 0, 0x3142, 0, 0x3143, 0, 0x3144, 0, 0x3145, 0, 0x3146, // 1C30-1C3F
			0, 0x3147, 0, 0x3148, 0, 0x3149, 0, 0x314A, 0, 0x314B, 0, 0x314C, 0, 0x314D, 0, 0x314E, // 1C40-1C4F
			0, 0x314F, 0, 0x3150, 0, 0x3151, 0, 0x3152, 0, 0x3153, 0, 0x3154, 0, 0x3155, 0, 0x3156, // 1C50-1C5F
			0, 0x3157, 0, 0x3158, 0, 0x3159, 0, 0x315A, 0, 0x315B, 0, 0x315C, 0, 0x315D, 0, 0x315E, // 1C60-1C6F
			0, 0x315F, 0, 0x3160, 0, 0x3161, 0, 0x3162, 0, 0x3163, 0, 0x3164, 0, 0x3B9D, 0, 0x4018, // 1C70-1C7F
			0, 0x4039, 0, 0x4E00, 0, 0x4E01, 0, 0x4E03, 0, 0x4E09, 0, 0x4E0A, 0, 0x4E0B, 0, 0x4E0D, // 1C80-1C8F
			0, 0x4E19, 0, 0x4E26, 0, 0x4E28, 0, 0x4E2D, 0, 0x4E32, 0, 0x4E36, 0, 0x4E39, 0, 0x4E3F, // 1C90-1C9F
			0, 0x4E59, 0, 0x4E5D, 0, 0x4E82, 0, 0x4E85, 0, 0x4E86, 0, 0x4E8C, 0, 0x4E94, 0, 0x4EA0, // 1CA0-1CAF
			0, 0x4EAE, 0, 0x4EBA, 0, 0x4EC0, 0, 0x4EE4, 0, 0x4F01, 0, 0x4F11, 0, 0x4F80, 0, 0x4F86, // 1CB0-1CBF
			0, 0x4F8B, 0, 0x4FAE, 0, 0x4FBF, 0, 0x502B, 0, 0x50DA, 0, 0x50E7, 0, 0x512A, 0, 0x513F, // 1CC0-1CCF
			0, 0x5140, 0, 0x5145, 0, 0x514D, 0, 0x5165, 0, 0x5168, 0, 0x5169, 0, 0x516B, 0, 0x516D, // 1CD0-1CDF
			0, 0x5180, 0, 0x5182, 0, 0x5196, 0, 0x5199, 0, 0x51AB, 0, 0x51B5, 0, 0x51B7, 0, 0x51C9, // 1CE0-1CEF
			0, 0x51CC, 0, 0x51DC, 0, 0x51DE, 0, 0x51E0, 0, 0x51F5, 0, 0x5200, 0, 0x5207, 0, 0x5217, // 1CF0-1CFF
			0, 0x5229, 0, 0x523A, 0, 0x5289, 0, 0x529B, 0, 0x52A3, 0, 0x52B4, 0, 0x52C7, 0, 0x52C9, // 1D00-1D0F
			0, 0x52D2, 0, 0x52DE, 0, 0x52E4, 0, 0x52F5, 0, 0x52F9, 0, 0x52FA, 0, 0x5315, 0, 0x5317, // 1D10-1D1F
			0, 0x531A, 0, 0x5338, 0, 0x533B, 0, 0x533F, 0, 0x5341, 0, 0x5344, 0, 0x5345, 0, 0x5351, // 1D20-1D2F
			0, 0x5354, 0, 0x535C, 0, 0x5369, 0, 0x5370, 0, 0x5375, 0, 0x5382, 0, 0x53B6, 0, 0x53C3, // 1D30-1D3F
			0, 0x53C8, 0, 0x53E3, 0, 0x53E5, 0, 0x53F3, 0, 0x540D, 0, 0x540F, 0, 0x541D, 0, 0x5442, // 1D40-1D4F
			0, 0x54BD, 0, 0x554F, 0, 0x5555, 0, 0x5587, 0, 0x5599, 0, 0x559D, 0, 0x55C0, 0, 0x55E2, // 1D50-1D5F
			0, 0x5606, 0, 0x5668, 0, 0x56D7, 0, 0x56DB, 0, 0x56F9, 0, 0x571F, 0, 0x5730, 0, 0x5840, // 1D60-1D6F
			0, 0x585A, 0, 0x585E, 0, 0x58A8, 0, 0x58B3, 0, 0x58D8, 0, 0x58DF, 0, 0x58EB, 0, 0x5902, // 1D70-1D7F
			0, 0x590A, 0, 0x5915, 0, 0x591C, 0, 0x5927, 0, 0x5927, 0x6B63, 0, 0x5929, 0, 0x5944, 0, // 1D80-1D8F
			0x5948, 0, 0x5951, 0, 0x5954, 0, 0x5973, 0, 0x5A62, 0, 0x5B28, 0, 0x5B50, 0, 0x5B66, 0, // 1D90-1D9F
			0x5B80, 0, 0x5B85, 0, 0x5B97, 0, 0x5BE7, 0, 0x5BEE, 0, 0x5BF8, 0, 0x5C0F, 0, 0x5C22, 0, // 1DA0-1DAF
			0x5C38, 0, 0x5C3F, 0, 0x5C62, 0, 0x5C64, 0, 0x5C65, 0, 0x5C6E, 0, 0x5C71, 0, 0x5D19, 0, // 1DB0-1DBF
			0x5D50, 0, 0x5DBA, 0, 0x5DDB, 0, 0x5DE5, 0, 0x5DE6, 0, 0x5DF1, 0, 0x5DFE, 0, 0x5E72, 0, // 1DC0-1DCF
			0x5E73, 0x6210, 0, 0x5E74, 0, 0x5E7A, 0, 0x5E7C, 0, 0x5E7F, 0, 0x5EA6, 0, 0x5EC9, 0, 0x5ECA, // 1DD0-1DDF
			0, 0x5ED2, 0, 0x5ED3, 0, 0x5ED9, 0, 0x5EEC, 0, 0x5EF4, 0, 0x5EFE, 0, 0x5F04, 0, 0x5F0B, // 1DE0-1DEF
			0, 0x5F13, 0, 0x5F50, 0, 0x5F61, 0, 0x5F69, 0, 0x5F73, 0, 0x5F8B, 0, 0x5FA9, 0, 0x5FAD, // 1DF0-1DFF
			0, 0x5FC3, 0, 0x5FF5, 0, 0x6012, 0, 0x601C, 0, 0x6075, 0, 0x6094, 0, 0x60D8, 0, 0x60E1, // 1E00-1E0F
			0, 0x6108, 0, 0x6144, 0, 0x614E, 0, 0x6160, 0, 0x6168, 0, 0x618E, 0, 0x6190, 0, 0x61F2, // 1E10-1E1F
			0, 0x61F6, 0, 0x6200, 0, 0x6208, 0, 0x622E, 0, 0x6234, 0, 0x6236, 0, 0x624B, 0, 0x62C9, // 1E20-1E2F
			0, 0x62CF, 0, 0x62D3, 0, 0x62FE, 0, 0x637B, 0, 0x63A0, 0, 0x63C4, 0, 0x641C, 0, 0x6452, // 1E30-1E3F
			0, 0x649A, 0, 0x64C4, 0, 0x652F, 0, 0x6534, 0, 0x654F, 0, 0x6556, 0, 0x6578, 0, 0x6587, // 1E40-1E4F
			0, 0x6597, 0, 0x6599, 0, 0x65A4, 0, 0x65B9, 0, 0x65C5, 0, 0x65E0, 0, 0x65E2, 0, 0x65E5, // 1E50-1E5F
			0, 0x660E, 0x6CBB, 0, 0x6613, 0, 0x662D, 0x548C, 0, 0x6674, 0, 0x6688, 0, 0x6691, 0, 0x66B4, // 1E60-1E6F
			0, 0x66C6, 0, 0x66F0, 0, 0x66F4, 0, 0x6708, 0, 0x6709, 0, 0x6717, 0, 0x671B, 0, 0x6728, // 1E70-1E7F
			0, 0x674E, 0, 0x6756, 0, 0x677B, 0, 0x6797, 0, 0x67F3, 0, 0x6817, 0, 0x682A, 0, 0x682A, // 1E80-1E8F
			0x5F0F, 0x4F1A, 0x793E, 0, 0x6881, 0, 0x6885, 0, 0x68A8, 0, 0x6A02, 0, 0x6A13, 0, 0x6AD3, 0, // 1E90-1E9F
			0x6B04, 0, 0x6B20, 0, 0x6B62, 0, 0x6B63, 0, 0x6B77, 0, 0x6B79, 0, 0x6BAE, 0, 0x6BB3, 0, // 1EA0-1EAF
			0x6BBA, 0, 0x6BCB, 0, 0x6BCD, 0, 0x6BD4, 0, 0x6BDB, 0, 0x6C0F, 0, 0x6C14, 0, 0x6C34, 0, // 1EB0-1EBF
			0x6C88, 0, 0x6CCC, 0, 0x6CE5, 0, 0x6CE8, 0, 0x6D1B, 0, 0x6D1E, 0, 0x6D41, 0, 0x6D6A, 0, // 1EC0-1ECF
			0x6D77, 0, 0x6DCB, 0, 0x6DDA, 0, 0x6DEA, 0, 0x6E1A, 0, 0x6E9C, 0, 0x6EBA, 0, 0x6ECB, 0, // 1ED0-1EDF
			0x6ED1, 0, 0x6EDB, 0, 0x6F0F, 0, 0x6F22, 0, 0x6F23, 0, 0x6FEB, 0, 0x6FFE, 0, 0x701E, 0, // 1EE0-1EEF
			0x706B, 0, 0x7099, 0, 0x70C8, 0, 0x70D9, 0, 0x7149, 0, 0x716E, 0, 0x71CE, 0, 0x71D0, 0, // 1EF0-1EFF
			0x7210, 0, 0x721B, 0, 0x722A, 0, 0x722B, 0, 0x7235, 0, 0x7236, 0, 0x723B, 0, 0x723F, 0, // 1F00-1F0F
			0x7247, 0, 0x7259, 0, 0x725B, 0, 0x7262, 0, 0x7279, 0, 0x72AC, 0, 0x72AF, 0, 0x72C0, 0, // 1F10-1F1F
			0x72FC, 0, 0x732A, 0, 0x7375, 0, 0x7384, 0, 0x7387, 0, 0x7389, 0, 0x73B2, 0, 0x73DE, 0, // 1F20-1F2F
			0x7406, 0, 0x7409, 0, 0x7422, 0, 0x7469, 0, 0x7471, 0, 0x7489, 0, 0x7498, 0, 0x74DC, 0, // 1F30-1F3F
			0x74E6, 0, 0x7506, 0, 0x7518, 0, 0x751F, 0, 0x7528, 0, 0x7530, 0, 0x7532, 0, 0x7537, 0, // 1F40-1F4F
			0x753B, 0, 0x7559, 0, 0x7565, 0, 0x7570, 0, 0x758B, 0, 0x7592, 0, 0x75E2, 0, 0x761D, 0, // 1F50-1F5F
			0x761F, 0, 0x7642, 0, 0x7669, 0, 0x7676, 0, 0x767D, 0, 0x76AE, 0, 0x76BF, 0, 0x76CA, 0, // 1F60-1F6F
			0x76DB, 0, 0x76E3, 0, 0x76E7, 0, 0x76EE, 0, 0x76F4, 0, 0x7701, 0, 0x7740, 0, 0x774A, 0, // 1F70-1F7F
			0x77A7, 0, 0x77DB, 0, 0x77E2, 0, 0x77F3, 0, 0x786B, 0, 0x788C, 0, 0x7891, 0, 0x78CA, 0, // 1F80-1F8F
			0x78CC, 0, 0x78FB, 0, 0x792A, 0, 0x793A, 0, 0x793C, 0, 0x793E, 0, 0x7948, 0, 0x7949, 0, // 1F90-1F9F
			0x7950, 0, 0x7956, 0, 0x795D, 0, 0x795E, 0, 0x7965, 0, 0x797F, 0, 0x798D, 0, 0x798E, 0, // 1FA0-1FAF
			0x798F, 0, 0x79AE, 0, 0x79B8, 0, 0x79BE, 0, 0x79CA, 0, 0x79D8, 0, 0x7A1C, 0, 0x7A40, 0, // 1FB0-1FBF
			0x7A74, 0, 0x7A81, 0, 0x7AB1, 0, 0x7ACB, 0, 0x7AF9, 0, 0x7B20, 0, 0x7B8F, 0, 0x7BC0, 0, // 1FC0-1FCF
			0x7C3E, 0, 0x7C60, 0, 0x7C73, 0, 0x7C7B, 0, 0x7C92, 0, 0x7CBE, 0, 0x7CD6, 0, 0x7CE7, 0, // 1FD0-1FDF
			0x7CF8, 0, 0x7D10, 0, 0x7D22, 0, 0x7D2F, 0, 0x7D5B, 0, 0x7DA0, 0, 0x7DBE, 0, 0x7DF4, 0, // 1FE0-1FEF
			0x7E09, 0, 0x7E37, 0, 0x7E41, 0, 0x7F36, 0, 0x7F3E, 0, 0x7F51, 0, 0x7F72, 0, 0x7F79, 0, // 1FF0-1FFF
			0x7F85, 0, 0x7F8A, 0, 0x7F9A, 0, 0x7FBD, 0, 0x8001, 0, 0x8005, 0, 0x800C, 0, 0x8012, 0, // 2000-200F
			0x8033, 0, 0x8046, 0, 0x806F, 0, 0x807E, 0, 0x807F, 0, 0x8089, 0, 0x808B, 0, 0x81D8, 0, // 2010-201F
			0x81E3, 0, 0x81E8, 0, 0x81EA, 0, 0x81ED, 0, 0x81F3, 0, 0x81FC, 0, 0x820C, 0, 0x8218, 0, // 2020-202F
			0x821B, 0, 0x821F, 0, 0x826E, 0, 0x826F, 0, 0x8272, 0, 0x8278, 0, 0x8279, 0, 0x82E5, 0, // 2030-203F
			0x8336, 0, 0x8352, 0, 0x83C9, 0, 0x83EF, 0, 0x83F1, 0, 0x843D, 0, 0x8449, 0, 0x8457, 0, // 2040-204F
			0x84EE, 0, 0x84FC, 0, 0x85CD, 0, 0x85FA, 0, 0x8606, 0, 0x8612, 0, 0x862D, 0, 0x863F, 0, // 2050-205F
			0x864D, 0, 0x865C, 0, 0x866B, 0, 0x8779, 0, 0x87BA, 0, 0x881F, 0, 0x8840, 0, 0x884C, 0, // 2060-206F
			0x8863, 0, 0x88C2, 0, 0x88CF, 0, 0x88E1, 0, 0x88F8, 0, 0x8910, 0, 0x8941, 0, 0x8964, 0, // 2070-207F
			0x897E, 0, 0x8986, 0, 0x898B, 0, 0x8996, 0, 0x89D2, 0, 0x8A00, 0, 0x8AAA, 0, 0x8ABF, 0, // 2080-208F
			0x8ACB, 0, 0x8AD2, 0, 0x8AD6, 0, 0x8AED, 0, 0x8AF8, 0, 0x8AFE, 0, 0x8B01, 0, 0x8B39, 0, // 2090-209F
			0x8B58, 0, 0x8B80, 0, 0x8B8A, 0, 0x8C37, 0, 0x8C46, 0, 0x8C48, 0, 0x8C55, 0, 0x8C78, 0, // 20A0-20AF
			0x8C9D, 0, 0x8CA1, 0, 0x8CC2, 0, 0x8CC7, 0, 0x8CC8, 0, 0x8CD3, 0, 0x8D08, 0, 0x8D64, 0, // 20B0-20BF
			0x8D70, 0, 0x8DB3, 0, 0x8DEF, 0, 0x8EAB, 0, 0x8ECA, 0, 0x8F26, 0, 0x8F2A, 0, 0x8F38, 0, // 20C0-20CF
			0x8F3B, 0, 0x8F62, 0, 0x8F9B, 0, 0x8FB0, 0, 0x8FB5, 0, 0x8FB6, 0, 0x9023, 0, 0x9038, 0, // 20D0-20DF
			0x9069, 0, 0x9072, 0, 0x907C, 0, 0x908F, 0, 0x9091, 0, 0x90CE, 0, 0x90DE, 0, 0x90FD, 0, // 20E0-20EF
			0x9149, 0, 0x916A, 0, 0x9199, 0, 0x91B4, 0, 0x91C6, 0, 0x91CC, 0, 0x91CF, 0, 0x91D1, 0, // 20F0-20FF
			0x9234, 0, 0x9276, 0, 0x9304, 0, 0x934A, 0, 0x9577, 0, 0x9580, 0, 0x95AD, 0, 0x961C, 0, // 2100-210F
			0x962E, 0, 0x964B, 0, 0x964D, 0, 0x9675, 0, 0x9678, 0, 0x967C, 0, 0x9686, 0, 0x96A3, 0, // 2110-211F
			0x96B6, 0, 0x96B7, 0, 0x96B8, 0, 0x96B9, 0, 0x96E2, 0, 0x96E3, 0, 0x96E8, 0, 0x96F6, 0, // 2120-212F
			0x96F7, 0, 0x9732, 0, 0x9748, 0, 0x9751, 0, 0x9756, 0, 0x975E, 0, 0x9762, 0, 0x9769, 0, // 2130-213F
			0x97CB, 0, 0x97DB, 0, 0x97ED, 0, 0x97F3, 0, 0x97FF, 0, 0x9801, 0, 0x9805, 0, 0x980B, 0, // 2140-214F
			0x9818, 0, 0x983B, 0, 0x985E, 0, 0x98A8, 0, 0x98DB, 0, 0x98DF, 0, 0x98EF, 0, 0x98FC, 0, // 2150-215F
			0x9928, 0, 0x9996, 0, 0x9999, 0, 0x99AC, 0, 0x99F1, 0, 0x9A6A, 0, 0x9AA8, 0, 0x9AD8, 0, // 2160-216F
			0x9ADF, 0, 0x9B12, 0, 0x9B25, 0, 0x9B2F, 0, 0x9B32, 0, 0x9B3C, 0, 0x9B5A, 0, 0x9B6F, 0, // 2170-217F
			0x9C57, 0, 0x9CE5, 0, 0x9DB4, 0, 0x9DFA, 0, 0x9E1E, 0, 0x9E75, 0, 0x9E7F, 0, 0x9E97, 0, // 2180-218F
			0x9E9F, 0, 0x9EA5, 0, 0x9EBB, 0, 0x9EC3, 0, 0x9ECD, 0, 0x9ECE, 0, 0x9ED1, 0, 0x9EF9, 0, // 2190-219F
			0x9EFD, 0, 0x9F0E, 0, 0x9F13, 0, 0x9F20, 0, 0x9F3B, 0, 0x9F43, 0, 0x9F4A, 0, 0x9F52, 0, // 21A0-21AF
			0x9F8D, 0, 0x9F8E, 0, 0x9F9C, 0, 0x9F9F, 0, 0x9FA0, 0, 0xA76F, 0, 0xFB49, 0x5C1, 0, 0xFB49, // 21B0-21BF
			0x5C2, 0, 0x22844, 0, 0x2284A, 0, 0x233D5, 0, 0x242EE, 0, 0x25249, 0, 0x25CD0, 0, 0x27ED3, 0, // 21C0-21CF
		};
		readonly static short[] charMapIndexArr = new short[]
		{
			0x21BA, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0090-009F
			1, 0, 0, 0, 0, 0, 0, 0, 0x15, 0, 0x7E5, 0, 0, 0, 0, 9, // 00A0-00AF
			0, 0, 0x35A, 0x3CE, 3, 0xE88, 0, 0, 0x24, 0x282, 0xA43, 0, 0x339, 0x331, 0x3F9, 0, // 00B0-00BF
			0x4C0, 0x4C3, 0x4C6, 0x4C9, 0x4D5, 0x4DB, 0, 0x514, 0x53E, 0x541, 0x544, 0x553, 0x5D4, 0x5D7, 0x5DA, 0x5E9, // 00C0-00CF
			0, 0x671, 0x688, 0x68B, 0x68E, 0x691, 0x69D, 0, 0, 0x731, 0x734, 0x737, 0x743, 0x7AD, 0, 0, // 00D0-00DF
			0x7F7, 0x7FA, 0x7FD, 0x800, 0x80C, 0x812, 0, 0x861, 0x89B, 0x89E, 0x8A1, 0x8B0, 0x938, 0x93B, 0x93E, 0x94A, // 00E0-00EF
			0, 0xA2E, 0xA48, 0xA4B, 0xA4E, 0xA51, 0xA5D, 0, 0, 0xAFD, 0xB00, 0xB03, 0xB0F, 0xB75, 0, 0xB84, // 00F0-00FF
			0x4CC, 0x803, 0x4CF, 0x806, 0x4ED, 0x824, 0x508, 0x855, 0x50B, 0x858, 0x50E, 0x85B, 0x511, 0x85E, 0x52D, 0x880, // 0100-010F
			0, 0, 0x54A, 0x8A7, 0x54D, 0x8AA, 0x550, 0x8AD, 0x568, 0x8C5, 0x559, 0x8B6, 0x58D, 0x8F0, 0x593, 0x8F6, // 0110-011F
			0x596, 0x8F9, 0x59C, 0x8FF, 0x5AA, 0x90B, 0, 0, 0x5DD, 0x941, 0x5E0, 0x944, 0x5E3, 0x947, 0x5FB, 0x95C, // 0120-012F
			0x5E6, 0, 0x5C8, 0x92C, 0x603, 0x964, 0x61A, 0x99C, 0, 0x62F, 0x9BD, 0x638, 0x9C6, 0x632, 0x9C0, 0x62C, // 0130-013F
			0x9BA, 0, 0, 0x66E, 0xA2B, 0x67D, 0xA3A, 0x677, 0xA34, 0xD7D, 0, 0, 0x694, 0xA54, 0x697, 0xA57, // 0140-014F
			0x6A3, 0xA63, 0, 0, 0x6D8, 0xAAC, 0x6EA, 0xABE, 0x6DE, 0xAB2, 0x6F8, 0xACC, 0x6FB, 0xACF, 0x70A, 0xADE, // 0150-015F
			0x701, 0xAD5, 0x726, 0xAF2, 0x71D, 0xAE9, 0, 0, 0x73A, 0xB06, 0x73D, 0xB09, 0x740, 0xB0C, 0x749, 0xB15, // 0160-016F
			0x74C, 0xB18, 0x761, 0xB2D, 0x78D, 0xB52, 0x7B0, 0xB78, 0x7BC, 0x7C7, 0xB92, 0x7CD, 0xB98, 0x7D0, 0xB9B, 0xAC4, // 0170-017F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0180-018F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0190-019F
			0x6AF, 0xA6F, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x758, // 01A0-01AF
			0xB24, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 01B0-01BF
			0, 0, 0, 0, 0x524, 0x527, 0x87A, 0x622, 0x629, 0x9AA, 0x662, 0x665, 0xA1F, 0x4DE, 0x815, 0x5EF, // 01C0-01CF
			0x950, 0x6A6, 0xA66, 0x74F, 0xB1B, 0xC1A, 0xC6D, 0xC17, 0xC6A, 0xC1D, 0xC70, 0xC14, 0xC67, 0, 0xBD7, 0xC2C, // 01D0-01DF
			0xD16, 0xD19, 0xBE4, 0xC35, 0, 0, 0x599, 0x8FC, 0x614, 0x996, 0x6B5, 0xA75, 0xD0E, 0xD11, 0xD0B, 0xD72, // 01E0-01EF
			0x967, 0x51E, 0x521, 0x877, 0x58A, 0x8ED, 0, 0, 0x66B, 0xA28, 0xBDC, 0xC2F, 0xBE1, 0xC32, 0xC11, 0xC64, // 01F0-01FF
			0x4E1, 0x818, 0x4E4, 0x81B, 0x55C, 0x8B9, 0x55F, 0x8BC, 0x5F2, 0x953, 0x5F5, 0x956, 0x6A9, 0xA69, 0x6AC, 0xA6C, // 0200-020F
			0x6E1, 0xAB5, 0x6E4, 0xAB8, 0x752, 0xB1E, 0x755, 0xB21, 0x707, 0xADB, 0x723, 0xAEF, 0, 0, 0x5B3, 0x914, // 0210-021F
			0, 0, 0, 0, 0, 0, 0x4D2, 0x809, 0x565, 0x8C2, 0xC0E, 0xC61, 0xC08, 0xC5B, 0x69A, 0xA5A, // 0220-022F
			0xD22, 0xD25, 0x7B6, 0xB7E, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0230-023F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0240-024F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0250-025F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0260-026F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0270-027F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0280-028F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0290-029F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 02A0-02AF
			0x902, 0xD40, 0x962, 0xA99, 0xD5A, 0xD5C, 0xD5E, 0xB4A, 0xB70, 0, 0, 0, 0, 0, 0, 0, // 02B0-02BF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 02C0-02CF
			0, 0, 0, 0, 0, 0, 0, 0, 0xF, 0x12, 0x18, 0x27, 6, 0x1B, 0, 0, // 02D0-02DF
			0xD3C, 0x9A8, 0xAC4, 0xB61, 0xD75, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 02E0-02EF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 02F0-02FF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0300-030F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0310-031F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0320-032F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0330-033F
			0xD80, 0xD82, 0, 0xD87, 0xD84, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0340-034F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0350-035F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0360-036F
			0, 0, 0, 0, 0xD7B, 0, 0, 0, 0, 0, 0x30, 0, 0, 0, 0x499, 0, // 0370-037F
			0, 0, 0, 0, 3, 0xBB7, 0xD9E, 0xBC9, 0xDB5, 0xDC1, 0xDD2, 0, 0xDE7, 0, 0xDFC, 0xE10, // 0380-038F
			0xEEB, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0390-039F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xDDB, 0xE05, 0xE2F, 0xE4F, 0xE5B, 0xE71, // 03A0-03AF
			0xEF4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 03B0-03BF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xE7A, 0xEC6, 0xEA5, 0xEBD, 0xED9, 0, // 03C0-03CF
			0xE44, 0xE6A, 0xDF7, 0xF03, 0xF06, 0xED2, 0xEAE, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 03D0-03DF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 03E0-03EF
			0xE86, 0xEB0, 0xEB8, 0, 0xDCD, 0xE4A, 0, 0, 0, 0xDF5, 0, 0, 0, 0, 0, 0, // 03F0-03FF
			0xF15, 0xF1B, 0, 0xF12, 0, 0, 0, 0xF09, 0, 0, 0, 0, 0xF33, 0xF27, 0xF3C, 0, // 0400-040F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0xF2D, 0, 0, 0, 0, 0, 0, // 0410-041F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0420-042F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0xF6F, 0, 0, 0, 0, 0, 0, // 0430-043F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0440-044F
			0xF57, 0xF5D, 0, 0xF54, 0, 0, 0, 0xF92, 0, 0, 0, 0, 0xF75, 0xF69, 0xF80, 0, // 0450-045F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0460-046F
			0, 0, 0, 0, 0, 0, 0xF95, 0xF98, 0, 0, 0, 0, 0, 0, 0, 0, // 0470-047F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0480-048F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0490-049F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 04A0-04AF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 04B0-04BF
			0, 0xF1E, 0xF60, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 04C0-04CF
			0xF0C, 0xF4E, 0xF0F, 0xF51, 0, 0, 0xF18, 0xF5A, 0, 0, 0xF9B, 0xF9E, 0xF21, 0xF63, 0xF24, 0xF66, // 04D0-04DF
			0, 0, 0xF2A, 0xF6C, 0xF30, 0xF72, 0xF36, 0xF7A, 0, 0, 0xFA1, 0xFA4, 0xF4B, 0xF8F, 0xF39, 0xF7D, // 04E0-04EF
			0xF3F, 0xF83, 0xF42, 0xF86, 0xF45, 0xF89, 0, 0, 0xF48, 0xF8C, 0, 0, 0, 0, 0, 0, // 04F0-04FF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0500-050F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0510-051F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0520-052F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0530-053F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0540-054F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0550-055F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0560-056F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0570-057F
			0, 0, 0, 0, 0, 0, 0, 0xFA7, 0, 0, 0, 0, 0, 0, 0, 0, // 0580-058F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0590-059F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 05A0-05AF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 05B0-05BF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 05C0-05CF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 05D0-05DF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 05E0-05EF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 05F0-05FF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0600-060F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0610-061F
			0, 0, 0x1080, 0x1083, 0x1426, 0x1086, 0x1461, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0620-062F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0630-063F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0640-064F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0650-065F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0660-066F
			0, 0, 0, 0, 0, 0x1089, 0x1429, 0x14AC, 0x1464, 0, 0, 0, 0, 0, 0, 0, // 0670-067F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0680-068F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0690-069F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 06A0-06AF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 06B0-06BF
			0x14C0, 0, 0x14A3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 06C0-06CF
			0, 0, 0, 0x14BB, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 06D0-06DF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x14D5, 0, 0, 0, 0, 0, 0, // 0920-092F
			0, 0x14DE, 0, 0, 0x14E1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0930-093F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0940-094F
			0, 0, 0, 0, 0, 0, 0, 0, 0x14C3, 0x14C6, 0x14C9, 0x14CC, 0x14CF, 0x14D2, 0x14D8, 0x14DB, // 0950-095F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0960-096F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0970-097F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0980-098F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0990-099F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 09A0-09AF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 09B0-09BF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x14ED, 0x14F0, 0, 0, 0, // 09C0-09CF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x14E4, 0x14E7, 0, 0x14EA, // 09D0-09DF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 09E0-09EF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 09F0-09FF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0A00-0A0F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0A10-0A1F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0A20-0A2F
			0, 0, 0, 0x14FF, 0, 0, 0x1502, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0A30-0A3F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0A40-0A4F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x14F3, 0x14F6, 0x14F9, 0, 0, 0x14FC, 0, // 0A50-0A5F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0A60-0A6F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0A70-0A7F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0A80-0A8F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0A90-0A9F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0AA0-0AAF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0AB0-0ABF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0AC0-0ACF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0AD0-0ADF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0AE0-0AEF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0AF0-0AFF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0B00-0B0F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0B10-0B1F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0B20-0B2F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0B30-0B3F
			0, 0, 0, 0, 0, 0, 0, 0, 0x150E, 0, 0, 0x150B, 0x1511, 0, 0, 0, // 0B40-0B4F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1505, 0x1508, 0, 0, // 0B50-0B5F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0B60-0B6F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0B70-0B7F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0B80-0B8F
			0, 0, 0, 0, 0x1514, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0B90-0B9F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0BA0-0BAF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0BB0-0BBF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1517, 0x151D, 0x151A, 0, 0, 0, // 0BC0-0BCF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0BD0-0BDF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0BE0-0BEF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0BF0-0BFF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C00-0C0F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C10-0C1F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C20-0C2F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C30-0C3F
			0, 0, 0, 0, 0, 0, 0, 0, 0x1520, 0, 0, 0, 0, 0, 0, 0, // 0C40-0C4F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C50-0C5F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C60-0C6F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C70-0C7F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C80-0C8F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C90-0C9F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0CA0-0CAF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0CB0-0CBF
			0x1523, 0, 0, 0, 0, 0, 0, 0x1529, 0x152C, 0, 0x1526, 0x152F, 0, 0, 0, 0, // 0CC0-0CCF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0CD0-0CDF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0CE0-0CEF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0CF0-0CFF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D00-0D0F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D10-0D1F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D20-0D2F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D30-0D3F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1532, 0x1538, 0x1535, 0, 0, 0, // 0D40-0D4F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D50-0D5F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D60-0D6F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D70-0D7F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D80-0D8F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D90-0D9F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0DA0-0DAF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0DB0-0DBF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0DC0-0DCF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x153B, 0, 0x153E, 0x1544, 0x1541, 0, // 0DD0-0DDF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0DE0-0DEF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0DF0-0DFF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E00-0E0F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E10-0E1F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E20-0E2F
			0, 0, 0, 0x1547, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E30-0E3F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E40-0E4F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E50-0E5F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E60-0E6F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E70-0E7F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E80-0E8F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E90-0E9F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0EA0-0EAF
			0, 0, 0, 0x1550, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0EB0-0EBF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0EC0-0ECF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x154A, 0x154D, 0, 0, // 0ED0-0EDF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0EE0-0EEF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0EF0-0EFF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1553, 0, 0, 0, // 0F00-0F0F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0F10-0F1F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0F20-0F2F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0F30-0F3F
			0, 0, 0, 0x1558, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x155B, 0, 0, // 0F40-0F4F
			0, 0, 0x155E, 0, 0, 0, 0, 0x1561, 0, 0, 0, 0, 0x1564, 0, 0, 0, // 0F50-0F5F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1555, 0, 0, 0, 0, 0, 0, // 0F60-0F6F
			0, 0, 0, 0x1567, 0, 0x156A, 0x1582, 0x1585, 0x1588, 0x158B, 0, 0, 0, 0, 0, 0, // 0F70-0F7F
			0, 0x156D, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0F80-0F8F
			0, 0, 0, 0x1573, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1576, 0, 0, // 0F90-0F9F
			0, 0, 0x1579, 0, 0, 0, 0, 0x157C, 0, 0, 0, 0, 0x157F, 0, 0, 0, // 0FA0-0FAF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1570, 0, 0, 0, 0, 0, 0, // 0FB0-0FBF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0FC0-0FCF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0FD0-0FDF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0FE0-0FEF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0FF0-0FFF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1000-100F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1010-101F
			0, 0, 0, 0, 0, 0, 0x158E, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1020-102F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1030-103F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1040-104F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1050-105F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1060-106F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1070-107F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1080-108F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1090-109F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 10A0-10AF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 10B0-10BF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 10C0-10CF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 10D0-10DF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 10E0-10EF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1591, 0, 0, 0, // 10F0-10FF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x4BB, 0xBDF, 0x4F4, 0, // 1D20-1D2F
			0x51C, 0x53C, 0xCC9, 0x57A, 0x59F, 0x5BF, 0x601, 0x606, 0x620, 0x641, 0x660, 0, 0x686, 0xD14, 0x6B8, 0x6D3, // 1D30-1D3F
			0x70D, 0x72F, 0x782, 0x7E5, 0xD28, 0xD2A, 0x16A8, 0x827, 0x864, 0x892, 0xD32, 0xD34, 0xD36, 0x8E7, 0, 0x96A, // 1D40-1D4F
			0x9CF, 0xC9B, 0xA43, 0xD2E, 0x16AA, 0x16AC, 0xA78, 0xAE1, 0xAFB, 0x16B0, 0xD4A, 0xB36, 0x16B2, 0xE44, 0xE46, 0xE48, // 1D50-1D5F
			0xED2, 0xED4, 0x923, 0xA99, 0xAFB, 0xB36, 0xE44, 0xE46, 0xEB0, 0xED2, 0xED4, 0, 0, 0, 0, 0, // 1D60-1D6F
			0, 0, 0, 0, 0, 0, 0, 0, 0xF78, 0, 0, 0, 0, 0, 0, 0, // 1D70-1D7F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1D80-1D8F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xD2C, 0x836, 0xD30, 0xC4A, 0xD36, // 1D90-1D9F
			0x8CE, 0xD38, 0xD3A, 0xD3E, 0xD42, 0xD44, 0xD46, 0x16B4, 0xD77, 0xD48, 0x16B6, 0xD79, 0xD4E, 0xD4C, 0xD50, 0xD52, // 1DA0-1DAF
			0xD54, 0xD56, 0xD58, 0xD60, 0xD62, 0xCEB, 0xD64, 0xD66, 0x16AE, 0xD68, 0xD6A, 0xB90, 0xD6C, 0xD6E, 0xD70, 0xE6A, // 1DB0-1DBF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1DC0-1DCF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1DD0-1DDF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1DE0-1DEF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1DF0-1DFF
			0x4EA, 0x821, 0x4F9, 0x82D, 0x4FC, 0x830, 0x4FF, 0x833, 0xBE7, 0xC38, 0x52A, 0x87D, 0x530, 0x883, 0x539, 0x88C, // 1E00-1E0F
			0x533, 0x886, 0x536, 0x889, 0xC8B, 0xC91, 0xC8E, 0xC94, 0x56B, 0x8C8, 0x56E, 0x8CB, 0xD1C, 0xD1F, 0x577, 0x8E4, // 1E10-1E1F
			0x590, 0x8F3, 0x5AD, 0x90E, 0x5B6, 0x917, 0x5B0, 0x911, 0x5B9, 0x91A, 0x5BC, 0x91D, 0x5FE, 0x95F, 0xBF6, 0xC47, // 1E20-1E2F
			0x611, 0x993, 0x617, 0x999, 0x61D, 0x99F, 0x635, 0x9C3, 0x16B8, 0x16BB, 0x63E, 0x9CC, 0x63B, 0x9C9, 0x654, 0x9FC, // 1E30-1E3F
			0x657, 0x9FF, 0x65A, 0xA02, 0x674, 0xA31, 0x67A, 0xA37, 0x683, 0xA40, 0x680, 0xA3D, 0xC05, 0xC58, 0xC0B, 0xC5E, // 1E40-1E4F
			0xC9D, 0xCA3, 0xCA0, 0xCA6, 0x6CB, 0xA91, 0x6CE, 0xA94, 0x6DB, 0xAAF, 0x6E7, 0xABB, 0x16BE, 0x16C1, 0x6ED, 0xAC1, // 1E50-1E5F
			0x6FE, 0xAD2, 0x704, 0xAD8, 0xCAB, 0xCAE, 0xCB1, 0xCB4, 0x16C4, 0x16C7, 0x71A, 0xAE3, 0x720, 0xAEC, 0x72C, 0xAF8, // 1E60-1E6F
			0x729, 0xAF5, 0x75E, 0xB2A, 0x767, 0xB33, 0x764, 0xB30, 0xCB7, 0xCBA, 0xCBD, 0xCC0, 0x778, 0xB44, 0x77B, 0xB47, // 1E70-1E7F
			0x787, 0xB4C, 0x78A, 0xB4F, 0x793, 0xB58, 0x790, 0xB55, 0x796, 0xB5E, 0x7A2, 0xB6A, 0x7A5, 0xB6D, 0x7B9, 0xB81, // 1E80-1E8F
			0x7CA, 0xB95, 0x7D3, 0xB9E, 0x7D6, 0xBA1, 0x920, 0xAE6, 0xB5B, 0xB8A, 0x7F4, 0xCC6, 0, 0, 0, 0, // 1E90-1E9F
			0x4E7, 0x81E, 0x4D8, 0x80F, 0xBCE, 0xC23, 0xBCB, 0xC20, 0xBD4, 0xC29, 0xBD1, 0xC26, 0x16CA, 0x16D0, 0xC76, 0xC82, // 1EA0-1EAF
			0xC73, 0xC7F, 0xC7C, 0xC88, 0xC79, 0xC85, 0x16CD, 0x16D3, 0x562, 0x8BF, 0x556, 0x8B3, 0x547, 0x8A4, 0xBED, 0xC3E, // 1EB0-1EBF
			0xBEA, 0xC3B, 0xBF3, 0xC44, 0xBF0, 0xC41, 0x16D6, 0x16D9, 0x5EC, 0x94D, 0x5F8, 0x959, 0x6B2, 0xA72, 0x6A0, 0xA60, // 1EC0-1ECF
			0xBFC, 0xC4F, 0xBF9, 0xC4C, 0xC02, 0xC55, 0xBFF, 0xC52, 0x16DC, 0x16DF, 0xCD0, 0xCDF, 0xCCD, 0xCDC, 0xCD6, 0xCE5, // 1ED0-1EDF
			0xCD3, 0xCE2, 0xCD9, 0xCE8, 0x75B, 0xB27, 0x746, 0xB12, 0xCF0, 0xCFF, 0xCED, 0xCFC, 0xCF6, 0xD05, 0xCF3, 0xD02, // 1EE0-1EEF
			0xCF9, 0xD08, 0x7AA, 0xB72, 0x7C2, 0xB8D, 0x7BF, 0xB87, 0x7B3, 0xB7B, 0, 0, 0, 0, 0, 0, // 1EF0-1EFF
			0xE38, 0xE3B, 0x16E2, 0x16EE, 0x16E5, 0x16F1, 0x16E8, 0x16F4, 0xDA7, 0xDAA, 0x170C, 0x1718, 0x170F, 0x171B, 0x1712, 0x171E, // 1F00-1F0F
			0xE52, 0xE55, 0x1736, 0x173C, 0x1739, 0x173F, 0, 0, 0xDB8, 0xDBB, 0x1742, 0x1748, 0x1745, 0x174B, 0, 0, // 1F10-1F1F
			0xE5E, 0xE61, 0x174E, 0x175A, 0x1751, 0x175D, 0x1754, 0x1760, 0xDC4, 0xDC7, 0x1778, 0x1784, 0x177B, 0x1787, 0x177E, 0x178A, // 1F20-1F2F
			0xE7D, 0xE80, 0x17A2, 0x17AB, 0x17A5, 0x17AE, 0x17A8, 0x17B1, 0xDDE, 0xDE1, 0x17B4, 0x17BD, 0x17B7, 0x17C0, 0x17BA, 0x17C3, // 1F30-1F3F
			0xEA8, 0xEAB, 0x17C6, 0x17CC, 0x17C9, 0x17CF, 0, 0, 0xDEA, 0xDED, 0x17D2, 0x17D8, 0x17D5, 0x17DB, 0, 0, // 1F40-1F4F
			0xEC9, 0xECC, 0x17DE, 0x17E7, 0x17E1, 0x17EA, 0x17E4, 0x17ED, 0, 0xE08, 0, 0x17F0, 0, 0x17F3, 0, 0x17F6, // 1F50-1F5F
			0xEDC, 0xEDF, 0x17F9, 0x1805, 0x17FC, 0x1808, 0x17FF, 0x180B, 0xE13, 0xE16, 0x1823, 0x182F, 0x1826, 0x1832, 0x1829, 0x1835, // 1F60-1F6F
			0xE2C, 0xE1C, 0xE4C, 0xE21, 0xE58, 0xE23, 0xE6E, 0xE28, 0xEA2, 0xEFA, 0xEBA, 0xEFC, 0xED6, 0xEFE, 0, 0, // 1F70-1F7F
			0x16EB, 0x16F7, 0x16FA, 0x16FD, 0x1700, 0x1703, 0x1706, 0x1709, 0x1715, 0x1721, 0x1724, 0x1727, 0x172A, 0x172D, 0x1730, 0x1733,
			// 1F80-1F8F
			0x1757, 0x1763, 0x1766, 0x1769, 0x176C, 0x176F, 0x1772, 0x1775, 0x1781, 0x178D, 0x1790, 0x1793, 0x1796, 0x1799, 0x179C, 0x179F,
			// 1F90-1F9F
			0x1802, 0x180E, 0x1811, 0x1814, 0x1817, 0x181A, 0x181D, 0x1820, 0x182C, 0x1838, 0x183B, 0x183E, 0x1841, 0x1844, 0x1847, 0x184A,
			// 1FA0-1FAF
			0xE35, 0xE32, 0x184D, 0xE41, 0xE1E, 0, 0xE3E, 0x1856, 0xDA4, 0xDA1, 0xD9B, 0xD8B, 0xDAD, 0x1E, 0xE6C, 0x1E, // 1FB0-1FBF
			0x2D, 0xBBA, 0x1850, 0xE67, 0xE25, 0, 0xE64, 0x1862, 0xDB2, 0xD8D, 0xDBE, 0xD8F, 0xDCA, 0x1859, 0x185C, 0x185F, // 1FC0-1FCF
			0xE77, 0xE74, 0xEE8, 0xD99, 0, 0, 0xE83, 0xEEE, 0xDD8, 0xDD5, 0xDCF, 0xD91, 0, 0x1868, 0x186B, 0x186E, // 1FD0-1FDF
			0xEC3, 0xEC0, 0xEF1, 0xE2A, 0xEB2, 0xEB5, 0xECF, 0xEF7, 0xE02, 0xDFF, 0xDF9, 0xD95, 0xDF2, 0xBB4, 0xD89, 0x7E3, // 1FE0-1FEF
			0, 0, 0x1853, 0xEE5, 0xF00, 0, 0xEE2, 0x1865, 0xDE4, 0xD93, 0xE0D, 0xD97, 0xE19, 0xBC7, 0x21, 0, // 1FF0-1FFF
			0x1871, 0x1873, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, // 2000-200F
			0, 0x1875, 0, 0, 0, 0, 0, 0x2A, 0, 0, 0, 0, 0, 0, 0, 0, // 2010-201F
			0, 0, 0, 0, 0x26E, 0x270, 0x273, 0, 0, 0, 0, 0, 0, 0, 0, 1, // 2020-202F
			0, 0, 0, 0x187F, 0x1882, 0, 0x188B, 0x188E, 0, 0, 0, 0, 0x6B, 0, 0xC, 0, // 2030-203F
			0, 0, 0, 0, 0, 0, 0, 0x4B6, 0x4B3, 0x6E, 0, 0, 0, 0, 0, 0, // 2040-204F
			0, 0, 0, 0, 0, 0, 0, 0x1886, 0, 0, 0, 0, 0, 0, 0, 1, // 2050-205F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2060-206F
			0x279, 0x923, 0, 0, 0x40E, 0x43E, 0x457, 0x465, 0x477, 0x485, 0x268, 0x18BB, 0x4A0, 0x7D, 0x264, 0xA11, // 2070-207F
			0x279, 0x282, 0x35A, 0x3CE, 0x40E, 0x43E, 0x457, 0x465, 0x477, 0x485, 0x268, 0x18BB, 0x4A0, 0x7D, 0x264, 0, // 2080-208F
			0x7E5, 0x892, 0xA43, 0xB61, 0xD32, 0x902, 0x96A, 0x9A8, 0x9CF, 0xA11, 0xA78, 0xAC4, 0xAE1, 0, 0, 0, // 2090-209F
			0, 0, 0, 0, 0, 0, 0, 0, 0x6D5, 0, 0, 0, 0, 0, 0, 0, // 20A0-20AF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 20B0-20BF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 20C0-20CF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 20D0-20DF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 20E0-20EF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 20F0-20FF
			0x7EC, 0x7F0, 0x502, 0xBC1, 0, 0x838, 0x83C, 0xCCB, 0, 0xBC4, 0x8E7, 0x59F, 0x59F, 0x59F, 0x902, 0xC99, // 2100-210F
			0x5BF, 0x5BF, 0x620, 0x9A8, 0, 0x660, 0x668, 0, 0, 0x6B8, 0x6D1, 0x6D3, 0x6D3, 0x6D3, 0, 0, // 2110-211F
			0x6F2, 0x70F, 0x717, 0, 0x7C5, 0, 0xE0B, 0, 0x7C5, 0, 0x606, 0xBDA, 0x4F4, 0x502, 0, 0x892, // 2120-212F
			0x53C, 0x571, 0, 0x641, 0xA43, 0xFB9, 0xFC7, 0xFCF, 0xFD4, 0x923, 0, 0x573, 0xEAE, 0xE46, 0xDB0, 0xDF0, // 2130-213F
			0x18B9, 0, 0, 0, 0, 0x51C, 0x864, 0x892, 0x923, 0x962, 0, 0, 0, 0, 0, 0, // 2140-214F
			0x345, 0x34D, 0x32C, 0x335, 0x3BD, 0x33D, 0x3C1, 0x3FD, 0x431, 0x341, 0x446, 0x349, 0x401, 0x44A, 0x46A, 0x329, // 2150-215F
			0x5BF, 0x5C1, 0x5C4, 0x5CE, 0x76A, 0x76C, 0x76F, 0x773, 0x5D1, 0x799, 0x79B, 0x79E, 0x620, 0x502, 0x51C, 0x641, // 2160-216F
			0x923, 0x925, 0x928, 0x932, 0xB36, 0xB38, 0xB3B, 0xB3F, 0x935, 0xB61, 0xB63, 0xB66, 0x9A8, 0x836, 0x864, 0x9CF, // 2170-217F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x27B, 0, 0, 0, 0, 0, 0, // 2180-218F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1898, 0x189F, 0, 0, 0, 0, // 2190-219F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x18A4, 0, // 21A0-21AF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 21B0-21BF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x18A7, 0x18AD, 0x18AA, // 21C0-21CF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 21D0-21DF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 21E0-21EF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 21F0-21FF
			0, 0, 0, 0, 0x18B0, 0, 0, 0, 0, 0x18B3, 0, 0, 0x18B6, 0, 0, 0, // 2200-220F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2210-221F
			0, 0, 0, 0, 0x18BD, 0, 0x18C0, 0, 0, 0, 0, 0, 0x18C3, 0x18C6, 0, 0x18CF, // 2220-222F
			0x18D2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2230-223F
			0, 0x18D6, 0, 0, 0x18D9, 0, 0, 0x18DC, 0, 0x18DF, 0, 0, 0, 0, 0, 0, // 2240-224F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2250-225F
			0x4A9, 0, 0x18E5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x18E2, 0x49D, 0x4AE, // 2260-226F
			0x18E8, 0x18EB, 0, 0, 0x18EE, 0x18F1, 0, 0, 0x18F4, 0x18F7, 0, 0, 0, 0, 0, 0, // 2270-227F
			0x18FA, 0x18FD, 0, 0, 0x1906, 0x1909, 0, 0, 0x190C, 0x190F, 0, 0, 0, 0, 0, 0, // 2280-228F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2290-229F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1918, 0x191B, 0x191E, 0x1921, // 22A0-22AF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 22B0-22BF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 22C0-22CF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 22D0-22DF
			0x1900, 0x1903, 0x1912, 0x1915, 0, 0, 0, 0, 0, 0, 0x1924, 0x1927, 0x192A, 0x192D, 0, 0, // 22E0-22EF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 22F0-22FF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2300-230F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2310-231F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1943, 0x1945, 0, 0, 0, 0, 0, // 2320-232F
			0x282, 0x35A, 0x3CE, 0x40E, 0x43E, 0x457, 0x465, 0x477, 0x485, 0x287, 0x29A, 0x2AD, 0x2C0, 0x2CF, 0x2DE, 0x2ED, // 2460-246F
			0x2FC, 0x30B, 0x31A, 0x35F, 0x7F, 0xB5, 0xBE, 0xC2, 0xC6, 0xCA, 0xCE, 0xD2, 0xD6, 0x83, 0x88, 0x8D, // 2470-247F
			0x92, 0x97, 0x9C, 0xA1, 0xA6, 0xAB, 0xB0, 0xB9, 0x284, 0x35C, 0x3D0, 0x410, 0x440, 0x459, 0x467, 0x479, // 2480-248F
			0x487, 0x28A, 0x29D, 0x2B0, 0x2C3, 0x2D2, 0x2E1, 0x2F0, 0x2FF, 0x30E, 0x31D, 0x362, 0xDA, 0xDE, 0xE2, 0xE6, // 2490-249F
			0xEA, 0xEE, 0xF2, 0xF6, 0xFA, 0xFE, 0x102, 0x106, 0x10A, 0x10E, 0x112, 0x116, 0x11A, 0x11E, 0x122, 0x126, // 24A0-24AF
			0x12A, 0x12E, 0x132, 0x136, 0x13A, 0x13E, 0x4BB, 0x4F4, 0x502, 0x51C, 0x53C, 0x571, 0x57A, 0x59F, 0x5BF, 0x601, // 24B0-24BF
			0x606, 0x620, 0x641, 0x660, 0x686, 0x6B8, 0x6D1, 0x6D3, 0x6F0, 0x70D, 0x72F, 0x76A, 0x782, 0x799, 0x7A8, 0x7C5, // 24C0-24CF
			0x7E5, 0x827, 0x836, 0x864, 0x892, 0x8CE, 0x8E7, 0x902, 0x923, 0x962, 0x96A, 0x9A8, 0x9CF, 0xA11, 0xA43, 0xA78, // 24D0-24DF
			0xA97, 0xA99, 0xAC4, 0xAE1, 0xAFB, 0xB36, 0xB4A, 0xB61, 0xB70, 0xB90, 0x279, 0, 0, 0, 0, 0, // 24E0-24EF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x18CA, 0, 0, 0, // 2A00-2A0F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2A10-2A1F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2A20-2A2F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2A30-2A3F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2A40-2A4F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2A50-2A5F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2A60-2A6F
			0, 0, 0, 0, 0x495, 0x4A2, 0x4A5, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2A70-2A7F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2A80-2A8F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2A90-2A9F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2AA0-2AAF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2AB0-2ABF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2AC0-2ACF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x193A, 0, 0, 0, // 2AD0-2ADF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x193D, // 2D60-2D6F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1EB4, // 2E90-2E9F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2EA0-2EAF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2EB0-2EBF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2EC0-2ECF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2ED0-2EDF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2EE0-2EEF
			0, 0, 0, 0x21B6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2EF0-2EFF
			0x1C83, 0x1C95, 0x1C9B, 0x1C9F, 0x1CA1, 0x1CA7, 0x1CAB, 0x1CAF, 0x1CB3, 0x1CCF, 0x1CD7, 0x1CDD, 0x1CE3, 0x1CE5, 0x1CE9, 0x1CF7,
			// 2F00-2F0F
			0x1CF9, 0x1CFB, 0x1D07, 0x1D19, 0x1D1D, 0x1D21, 0x1D23, 0x1D29, 0x1D33, 0x1D35, 0x1D3B, 0x1D3D, 0x1D41, 0x1D43, 0x1D65, 0x1D6B,
			// 2F10-2F1F
			0x1D7D, 0x1D7F, 0x1D81, 0x1D83, 0x1D87, 0x1D96, 0x1D9C, 0x1DA0, 0x1DAA, 0x1DAC, 0x1DAE, 0x1DB0, 0x1DBA, 0x1DBC, 0x1DC4, 0x1DC6,
			// 2F20-2F2F
			0x1DCA, 0x1DCC, 0x1DCE, 0x1DD5, 0x1DD9, 0x1DE9, 0x1DEB, 0x1DEF, 0x1DF1, 0x1DF3, 0x1DF5, 0x1DF9, 0x1E01, 0x1E25, 0x1E2B, 0x1E2D,
			// 2F30-2F3F
			0x1E45, 0x1E47, 0x1E4F, 0x1E51, 0x1E55, 0x1E57, 0x1E5B, 0x1E5F, 0x1E73, 0x1E77, 0x1E7F, 0x1EA2, 0x1EA4, 0x1EAA, 0x1EAE, 0x1EB2,
			// 2F40-2F4F
			0x1EB6, 0x1EB8, 0x1EBA, 0x1EBC, 0x1EBE, 0x1EF0, 0x1F04, 0x1F0A, 0x1F0C, 0x1F0E, 0x1F10, 0x1F12, 0x1F14, 0x1F1A, 0x1F26, 0x1F2A,
			// 2F50-2F5F
			0x1F3E, 0x1F40, 0x1F44, 0x1F46, 0x1F48, 0x1F4A, 0x1F58, 0x1F5A, 0x1F66, 0x1F68, 0x1F6A, 0x1F6C, 0x1F76, 0x1F82, 0x1F84, 0x1F86,
			// 2F60-2F6F
			0x1F96, 0x1FB4, 0x1FB6, 0x1FC0, 0x1FC6, 0x1FC8, 0x1FD4, 0x1FE0, 0x1FF6, 0x1FFA, 0x2002, 0x2006, 0x2008, 0x200C, 0x200E, 0x2010,
			// 2F70-2F7F
			0x2018, 0x201A, 0x2020, 0x2024, 0x2028, 0x202A, 0x202C, 0x2030, 0x2032, 0x2034, 0x2038, 0x203A, 0x2060, 0x2064, 0x206C, 0x206E,
			// 2F80-2F8F
			0x2070, 0x2080, 0x2084, 0x2088, 0x208A, 0x20A6, 0x20A8, 0x20AC, 0x20AE, 0x20B0, 0x20BE, 0x20C0, 0x20C2, 0x20C6, 0x20C8, 0x20D4,
			// 2F90-2F9F
			0x20D6, 0x20D8, 0x20E8, 0x20F0, 0x20F8, 0x20FA, 0x20FE, 0x2108, 0x210A, 0x210E, 0x2120, 0x2126, 0x212C, 0x2136, 0x213A, 0x213C,
			// 2FA0-2FAF
			0x213E, 0x2140, 0x2144, 0x2146, 0x214A, 0x2156, 0x2158, 0x215A, 0x2162, 0x2164, 0x2166, 0x216C, 0x216E, 0x2170, 0x2174, 0x2176,
			// 2FB0-2FBF
			0x2178, 0x217A, 0x217C, 0x2182, 0x218A, 0x218C, 0x2192, 0x2194, 0x2196, 0x2198, 0x219C, 0x219E, 0x21A0, 0x21A2, 0x21A4, 0x21A6,
			// 2FC0-2FCF
			0x21A8, 0x21AC, 0x21AE, 0x21B0, 0x21B4, 0x21B8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2FD0-2FDF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2FE0-2FEF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2FF0-2FFF
			1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 3000-300F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 3010-301F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 3020-302F
			0, 0, 0, 0, 0, 0, 0x1957, 0, 0x1D29, 0x1D2B, 0x1D2D, 0, 0, 0, 0, 0, // 3030-303F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1964, 0, 0x1967, 0, // 3040-304F
			0x196A, 0, 0x196D, 0, 0x1970, 0, 0x1973, 0, 0x1976, 0, 0x1979, 0, 0x197C, 0, 0x197F, 0, // 3050-305F
			0x1982, 0, 0x1985, 0, 0, 0x1988, 0, 0x198B, 0, 0x198E, 0, 0, 0, 0, 0, 0, // 3060-306F
			0x1991, 0x1994, 0, 0x1997, 0x199A, 0, 0x199D, 0x19A0, 0, 0x19A3, 0x19A6, 0, 0x19A9, 0x19AC, 0, 0, // 3070-307F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 3080-308F
			0, 0, 0, 0, 0x1961, 0, 0, 0, 0, 0, 0, 0x63, 0x66, 0, 0x19B6, 0x19AF, // 3090-309F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1A05, 0, 0x1A20, 0, // 30A0-30AF
			0x1A4C, 0, 0x1A66, 0, 0x1A6F, 0, 0x1A7F, 0, 0x1A8F, 0, 0x1A99, 0, 0x1A9E, 0, 0x1AAB, 0, // 30B0-30BF
			0x1AB0, 0, 0x1AB9, 0, 0, 0x1AC0, 0, 0x1AC5, 0, 0x1ACD, 0, 0, 0, 0, 0, 0, // 30C0-30CF
			0x1AE9, 0x1AEC, 0, 0x1B04, 0x1B07, 0, 0x1B1C, 0x1B1F, 0, 0x1B39, 0x1B3C, 0, 0x1B5E, 0x1B61, 0, 0, // 30D0-30DF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 30E0-30EF
			0, 0, 0, 0, 0x19E1, 0, 0, 0x1BF6, 0x1BFF, 0x1C04, 0x1C09, 0, 0, 0, 0x1C12, 0x1A72, // 30F0-30FF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 3100-310F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 3110-311F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 3120-312F
			0, 0x1593, 0x1598, 0x165F, 0x159A, 0x1661, 0x1663, 0x159F, 0x15A4, 0x15A6, 0x1665, 0x1667, 0x1669, 0x166B, 0x166D, 0x166F, // 3130-313F
			0x15F5, 0x15AB, 0x15B0, 0x15B5, 0x15FF, 0x15B7, 0x15BC, 0x15BE, 0x15C6, 0x15D0, 0x15D2, 0x15DD, 0x15E2, 0x15E7, 0x15EC, 0x1625,
			// 3140-314F
			0x1627, 0x1629, 0x162B, 0x162D, 0x162F, 0x1631, 0x1633, 0x1635, 0x1637, 0x1639, 0x163B, 0x163D, 0x163F, 0x1641, 0x1643, 0x1645,
			// 3150-315F
			0x1647, 0x1649, 0x164B, 0x164D, 0x1623, 0x15F1, 0x15F3, 0x1671, 0x1673, 0x1675, 0x1677, 0x1679, 0x167B, 0x167D, 0x15F7, 0x167F,
			// 3160-316F
			0x1681, 0x15F9, 0x15FB, 0x15FD, 0x1601, 0x1603, 0x1605, 0x1607, 0x1609, 0x160B, 0x160D, 0x160F, 0x1611, 0x1613, 0x1615, 0x1617,
			// 3170-317F
			0x1619, 0x161B, 0x1683, 0x1685, 0x161D, 0x161F, 0x1621, 0x164F, 0x1651, 0x1653, 0x1655, 0x1657, 0x1659, 0x165B, 0x165D, 0, // 3180-318F
			0, 0, 0x1C83, 0x1CAB, 0x1C89, 0x1D67, 0x1C8B, 0x1C97, 0x1C8D, 0x1F4C, 0x1CA1, 0x1C91, 0x1C85, 0x1D8C, 0x1D6D, 0x1CB3, // 3190-319F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 31A0-31AF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 31B0-31BF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 31C0-31CF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 31D0-31DF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 31E0-31EF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 31F0-31FF
			0x142, 0x14B, 0x154, 0x15D, 0x166, 0x16F, 0x178, 0x181, 0x199, 0x1A7, 0x1B0, 0x1B9, 0x1C2, 0x1CB, 0x146, 0x14F, // 3200-320F
			0x158, 0x161, 0x16A, 0x173, 0x17C, 0x185, 0x19D, 0x1AB, 0x1B4, 0x1BD, 0x1C6, 0x1CF, 0x1A2, 0x18A, 0x192, 0, // 3210-321F
			0x1D4, 0x1E4, 0x1DC, 0x214, 0x1E8, 0x1FC, 0x1D8, 0x1F8, 0x1E0, 0x204, 0x224, 0x238, 0x234, 0x22C, 0x260, 0x218, // 3220-322F
			0x220, 0x230, 0x228, 0x244, 0x20C, 0x23C, 0x258, 0x248, 0x200, 0x1EC, 0x210, 0x21C, 0x240, 0x1F0, 0x25C, 0x208, // 3230-323F
			0x24C, 0x1F4, 0x250, 0x254, 0x1D53, 0x1DD7, 0x1E4F, 0x1FCC, 0, 0, 0, 0, 0, 0, 0, 0, // 3240-324F
			0x6C4, 0x36E, 0x379, 0x384, 0x38F, 0x39A, 0x3A1, 0x3A8, 0x3AF, 0x3B6, 0x3D3, 0x3DA, 0x3E1, 0x3E4, 0x3E7, 0x3EA, // 3250-325F
			0x1593, 0x159A, 0x159F, 0x15A6, 0x15AB, 0x15B0, 0x15B7, 0x15BE, 0x15C6, 0x15D2, 0x15DD, 0x15E2, 0x15E7, 0x15EC, 0x1595, 0x159C,
			// 3260-326F
			0x15A1, 0x15A8, 0x15AD, 0x15B2, 0x15B9, 0x15C0, 0x15C8, 0x15D4, 0x15DF, 0x15E4, 0x15E9, 0x15EE, 0x15D7, 0x15CB, 0x15C3, 0, // 3270-327F
			0x1C83, 0x1CAB, 0x1C89, 0x1D67, 0x1CAD, 0x1CDF, 0x1C87, 0x1CDD, 0x1CA3, 0x1D29, 0x1E77, 0x1EF0, 0x1EBE, 0x1E7F, 0x20FE, 0x1D6B,
			// 3280-328F
			0x1E5F, 0x1E8D, 0x1E79, 0x1F9A, 0x1D49, 0x1F18, 0x20B2, 0x1FA4, 0x1D0B, 0x1FBA, 0x1F4E, 0x1D96, 0x20E0, 0x1CCD, 0x1D37, 0x1EC6,
			// 3290-329F
			0x214C, 0x1CBB, 0x1CE7, 0x1EA6, 0x1C8B, 0x1C97, 0x1C8D, 0x1DC8, 0x1D47, 0x1D25, 0x1DA4, 0x1D9E, 0x1F72, 0x1CB9, 0x20B6, 0x1D31,
			// 32A0-32AF
			0x1D85, 0x3ED, 0x3F0, 0x3F3, 0x3F6, 0x413, 0x416, 0x419, 0x41C, 0x41F, 0x422, 0x425, 0x428, 0x42B, 0x42E, 0x443, // 32B0-32BF
			0x354, 0x3C8, 0x408, 0x438, 0x451, 0x45F, 0x471, 0x47F, 0x48D, 0x292, 0x2A5, 0x2B8, 0x5A4, 0x897, 0x894, 0x625, // 32C0-32CF
			0x19BB, 0x19D2, 0x19DF, 0x19EA, 0x19F9, 0x1A03, 0x1A1E, 0x1A4A, 0x1A64, 0x1A6D, 0x1A7D, 0x1A8D, 0x1A97, 0x1A9C, 0x1AA9, 0x1AAE,
			// 32D0-32DF
			0x1AB7, 0x1ABE, 0x1AC3, 0x1ACB, 0x1AD6, 0x1ADB, 0x1ADD, 0x1ADF, 0x1AE1, 0x1AE7, 0x1B02, 0x1B1A, 0x1B37, 0x1B5C, 0x1B7C, 0x1B95,
			// 32E0-32EF
			0x1BA5, 0x1BA7, 0x1BB6, 0x1BBA, 0x1BC6, 0x1BCE, 0x1BD0, 0x1BD2, 0x1BDC, 0x1BE7, 0x1BF2, 0x1BF4, 0x1BFD, 0x1C02, 0x1C07, 0, // 32F0-32FF
			0x19BD, 0x19C2, 0x19C7, 0x19CC, 0x19D4, 0x19D9, 0x19E4, 0x19EC, 0x19F2, 0x19FB, 0x19FF, 0x1A08, 0x1A0C, 0x1A11, 0x1A16, 0x1A1A,
			// 3300-330F
			0x1A3E, 0x1A41, 0x1A23, 0x1A45, 0x1A28, 0x1A2B, 0x1A31, 0x1A38, 0x1A5A, 0x1A5E, 0x1A4F, 0x1A55, 0x1A69, 0x1A75, 0x1A79, 0x1A82,
			// 3310-331F
			0x1A87, 0x1A92, 0x1AA1, 0x1AA5, 0x1AB3, 0x1AC8, 0x1AD3, 0x1AD0, 0x1AD8, 0x1AE3, 0x1AEF, 0x1AF8, 0x1AFE, 0x1AF3, 0x1B0D, 0x1B13,
			// 3320-332F
			0x1B17, 0x1B0A, 0x1B22, 0x1B28, 0x1B31, 0x1B2D, 0x1B3F, 0x1B4D, 0x1B50, 0x1B45, 0x1B54, 0x1B58, 0x1B49, 0x1B73, 0x1B6F, 0x1B64,
			// 3330-333F
			0x1B78, 0x1B67, 0x1B6B, 0x1B7E, 0x1B83, 0x1B87, 0x1B8B, 0x1B8F, 0x1B97, 0x1B9C, 0x1B9F, 0x1BA9, 0x1BAC, 0x1BB1, 0x1BBC, 0x1BC0,
			// 3340-334F
			0x1BC8, 0x1BD4, 0x1BD9, 0x1BDE, 0x1BE2, 0x1BE9, 0x1BEC, 0x1BF9, 0x27F, 0x357, 0x3CB, 0x40B, 0x43B, 0x454, 0x462, 0x474, // 3350-335F
			0x482, 0x490, 0x296, 0x2A9, 0x2BC, 0x2CB, 0x2DA, 0x2E9, 0x2F8, 0x307, 0x316, 0x325, 0x36A, 0x375, 0x380, 0x38B, // 3360-336F
			0x396, 0x904, 0x869, 0x4BD, 0x829, 0xA45, 0xA8B, 0x86C, 0x86F, 0x873, 0x5CB, 0x1DD0, 0x1E66, 0x1D89, 0x1E61, 0x1E8F, // 3370-337F
			0xA7F, 0xA13, 0xE8A, 0x9D1, 0x96C, 0x608, 0x643, 0x57C, 0x840, 0x97D, 0xA82, 0xA16, 0xE8D, 0xE96, 0x9DD, 0x982, // 3380-338F
			0x5A7, 0x96F, 0x646, 0x57F, 0x713, 0xE9F, 0xA05, 0x88F, 0x9A5, 0x8E1, 0xA22, 0xE99, 0x9E4, 0x84A, 0x985, 0x9E7, // 3390-339F
			0x84D, 0x9F6, 0x988, 0x9EB, 0x851, 0x9F9, 0x98C, 0xA08, 0xA0C, 0x6C8, 0x973, 0x64A, 0x583, 0xA9B, 0xA9F, 0xAA5, // 33A0-33AF
			0xA8E, 0xA25, 0xE9C, 0x9F3, 0xA85, 0xA19, 0xE90, 0x9D4, 0x977, 0x64E, 0xA88, 0xA1C, 0xE93, 0x9D7, 0x97A, 0x651, // 33B0-33BF
			0x9A2, 0x65D, 0x7E7, 0x4F6, 0x844, 0x847, 0x517, 0x504, 0x866, 0x587, 0x908, 0x5A1, 0x92F, 0x60B, 0x60E, 0x990, // 33C0-33CF
			0x9AD, 0x9B0, 0x9B3, 0x9B7, 0x9DA, 0x9E0, 0x9EF, 0x6BA, 0xA7A, 0x6BD, 0x6C1, 0xAC6, 0x6F5, 0x784, 0x77E, 0x4F0, // 33D0-33DF
			0x351, 0x3C5, 0x405, 0x435, 0x44E, 0x45C, 0x46E, 0x47C, 0x48A, 0x28E, 0x2A1, 0x2B4, 0x2C7, 0x2D6, 0x2E5, 0x2F4, // 33E0-33EF
			0x303, 0x312, 0x321, 0x366, 0x371, 0x37C, 0x387, 0x392, 0x39D, 0x3A4, 0x3AB, 0x3B2, 0x3B9, 0x3D6, 0x3DD, 0x8E9, // 33F0-33FF
			0x20AA, 0x1E75, 0x20C8, 0x20B8, 0x1EE0, 0x1C99, 0x1D45, 0x21B4, 0x21B4, 0x1D92, 0x20FE, 0x1D57, 0x1D90, 0x1E21, 0x1F64, 0x2000,
			// F900-F90F
			0x205E, 0x2068, 0x2078, 0x20E6, 0x1E9A, 0x1EC8, 0x1EF6, 0x1F2E, 0x204A, 0x20F2, 0x2168, 0x1CA5, 0x1D39, 0x1EA0, 0x1F02, 0x205C,
			// F910-F91F
			0x2188, 0x1DC0, 0x1EEA, 0x2054, 0x207E, 0x1E2F, 0x201E, 0x206A, 0x1DDF, 0x1E7B, 0x1ECE, 0x1F20, 0x20EA, 0x1CBF, 0x1CED, 0x1D13,
			// F920-F92F
			0x1E43, 0x1E9E, 0x1F00, 0x1F74, 0x2008, 0x2058, 0x2062, 0x20C4, 0x2132, 0x217E, 0x2186, 0x1F8A, 0x1FAA, 0x1FEA, 0x2044, 0x2104,
			// F930-F93F
			0x218C, 0x2094, 0x1D7B, 0x1DED, 0x1FD2, 0x2016, 0x1F16, 0x1F8E, 0x20B4, 0x2130, 0x1D79, 0x1DB4, 0x1E9C, 0x1ED4, 0x1EE4, 0x1FE6,
			// F940-F94F
			0x1FF2, 0x2112, 0x1D11, 0x201C, 0x1CF3, 0x1CF1, 0x1FBC, 0x1FEC, 0x2048, 0x2116, 0x20A2, 0x1E31, 0x1E9A, 0x209A, 0x1C9D, 0x1DA6,
			// F950-F95F
			0x1E05, 0x1F28, 0x1F56, 0x1D1F, 0x1F92, 0x1CC5, 0x1DFD, 0x1C8F, 0x1EC2, 0x1E4D, 0x1FE4, 0x1D3F, 0x1D73, 0x1F7A, 0x204C, 0x208C,
			// F960-F96F
			0x1EB0, 0x20D6, 0x1EC0, 0x1E35, 0x203E, 0x1E39, 0x1F54, 0x1CB1, 0x1CDB, 0x1CEF, 0x1E94, 0x1FDE, 0x2036, 0x2092, 0x20FC, 0x1D17,
			// F970-F97F
			0x1D4F, 0x1D96, 0x1DE7, 0x1E59, 0x1EEC, 0x1F94, 0x210C, 0x216A, 0x218E, 0x219A, 0x1D07, 0x1E71, 0x1EA8, 0x20D2, 0x1DD3, 0x1E1D,
			// F980-F98F
			0x1E23, 0x1E41, 0x1EE8, 0x1EF8, 0x1F3A, 0x1FB8, 0x1FEE, 0x2014, 0x20CA, 0x2050, 0x20DC, 0x2106, 0x1CFF, 0x1D09, 0x1D51, 0x1EF4,
			// F990-F99F
			0x2072, 0x208C, 0x1DDD, 0x1E03, 0x1E37, 0x1EAC, 0x1FD0, 0x1F24, 0x1CB7, 0x1D69, 0x1DA6, 0x1DC2, 0x1E07, 0x1F2C, 0x1F36, 0x2004,
			// F9A0-F9AF
			0x2012, 0x2100, 0x212E, 0x2134, 0x2150, 0x1CC1, 0x1FB2, 0x20F6, 0x2124, 0x1E0F, 0x1CA9, 0x1CC9, 0x1DA8, 0x1DB2, 0x1E53, 0x1E9A,
			// F9B0-F9BF
			0x1EFC, 0x1F62, 0x2052, 0x20E4, 0x21B0, 0x1E6B, 0x2110, 0x1D05, 0x1E85, 0x1E89, 0x1ECC, 0x1EDA, 0x1F32, 0x1F52, 0x1F88, 0x1FE2,
			// F9C0-F9CF
			0x2154, 0x1CDF, 0x1E27, 0x2118, 0x1CC7, 0x1DBE, 0x1ED6, 0x20CC, 0x1DFB, 0x1E13, 0x1E8B, 0x1F28, 0x211C, 0x1D01, 0x1D4B, 0x1DB8,
			// F9D0-F9DF
			0x1E64, 0x1E81, 0x1E98, 0x1EC4, 0x1F30, 0x1F5C, 0x1FFE, 0x2074, 0x2076, 0x20FA, 0x2128, 0x1D27, 0x1EDC, 0x1D4D, 0x1EFE, 0x1F3C,
			// F9E0-F9EF
			0x2056, 0x211E, 0x2180, 0x2190, 0x1E87, 0x1ED2, 0x2022, 0x1FC6, 0x1FCA, 0x1FD8, 0x1F1E, 0x1EF2, 0x20A0, 0x1CB5, 0x2040, 0x1D03,
			// F9F0-F9FF
			0x1CFD, 0x1DDB, 0x1E33, 0x1FDC, 0x1DA2, 0x1ECA, 0x1E6F, 0x20D0, 0x206E, 0x2114, 0x2084, 0x1DE3, 0x1CD1, 0x1D5D, 0, 0, // FA00-FA0F
			0x1D71, 0, 0x1E69, 0, 0, 0x1CF5, 0x1F22, 0x1F6E, 0x1F98, 0x1FA6, 0x1FA8, 0x1FB0, 0x2138, 0x1FDA, 0x2006, 0, // FA10-FA1F
			0x205A, 0, 0x2098, 0, 0, 0x20DE, 0x20EE, 0, 0, 0, 0x215C, 0x215E, 0x2160, 0x2184, 0x20EC, 0x2122, // FA20-FA2F
			0x1CC3, 0x1CCB, 0x1CD5, 0x1D0F, 0x1D15, 0x1D2F, 0x1D5B, 0x1D61, 0x1D63, 0x1D6F, 0x1D75, 0x1DB6, 0x1DBA, 0x1E0B, 0x1E19, 0x1E1B,
			// FA30-FA3F
			0x1E1F, 0x1E49, 0x1E5D, 0x1E6D, 0x1E96, 0x1ED0, 0x1ED8, 0x1EE6, 0x1EFA, 0x1F06, 0x1F34, 0x1F8C, 0x1F9A, 0x1F9E, 0x1F9C, 0x1FA0,
			// FA40-FA4F
			0x1FA2, 0x1FA4, 0x1FAC, 0x1FAE, 0x1FBE, 0x1FC2, 0x1FCE, 0x1FEE, 0x1FF0, 0x1FF4, 0x1FFC, 0x200A, 0x2026, 0x203C, 0x203C, 0x204E,
			// FA50-FA5F
			0x207A, 0x2086, 0x209C, 0x209E, 0x20BA, 0x20BC, 0x20DA, 0x20DE, 0x212A, 0x2148, 0x2152, 0x1E09, 0x21C8, 0x202E, 0, 0, // FA60-FA6F
			0x1C93, 0x1CEB, 0x1CD9, 0x1CBD, 0x1CD3, 0x1CE1, 0x1D0D, 0x1D1B, 0x1D5B, 0x1D55, 0x1D59, 0x1D5F, 0x1D71, 0x1D77, 0x1D8E, 0x1D94,
			// FA70-FA7F
			0x1D98, 0x1D9A, 0x1DE1, 0x1DE5, 0x1DF7, 0x1DFF, 0x1E0D, 0x1E15, 0x1E11, 0x1E1B, 0x1E17, 0x1E1F, 0x1E29, 0x1E3B, 0x1E3D, 0x1E3F,
			// FA80-FA8F
			0x1E4B, 0x1E69, 0x1E7B, 0x1E7D, 0x1E83, 0x1EAA, 0x1EB0, 0x1ECC, 0x1EE2, 0x1EDE, 0x1EE6, 0x1EEE, 0x1EFA, 0x1F80, 0x1F08, 0x1F1C,
			// FA90-FA9F
			0x1F22, 0x1F38, 0x1F42, 0x1F50, 0x1F5E, 0x1F60, 0x1F6E, 0x1F70, 0x1F78, 0x1F7E, 0x1F7C, 0x1F90, 0x1FC4, 0x1FCE, 0x1FD6, 0x1FE8,
			// FAA0-FAAF
			0x1FEE, 0x1FF8, 0x200A, 0x2042, 0x2046, 0x2066, 0x207C, 0x2082, 0x2086, 0x208E, 0x2098, 0x2090, 0x209C, 0x209A, 0x2096, 0x209E,
			// FAB0-FABF
			0x20A4, 0x20BC, 0x20CE, 0x20E2, 0x20F4, 0x2102, 0x211A, 0x212A, 0x2138, 0x2142, 0x2148, 0x214E, 0x2152, 0x2172, 0x21B4, 0x21C4,
			// FAC0-FACF
			0x21C2, 0x21C6, 0x1C7D, 0x1C7F, 0x1C81, 0x21CA, 0x21CC, 0x21CE, 0x21AA, 0x21B2, 0, 0, 0, 0, 0, 0, // FAD0-FADF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // FAE0-FAEF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // FAF0-FAFF
			0x8D0, 0x8DB, 0x8DE, 0x8D3, 0x8D7, 0xCC3, 0xAC9, 0, 0, 0, 0, 0, 0, 0, 0, 0, // FB00-FB0F
			0, 0, 0, 0xFB3, 0xFAA, 0xFAD, 0xFB6, 0xFB0, 0, 0, 0, 0, 0, 0xFEA, 0, 0x102F, // FB10-FB1F
			0x100B, 0xFB9, 0xFD4, 0xFD9, 0xFF3, 0xFFB, 0x1000, 0x101C, 0x102A, 0x268, 0x1024, 0x1027, 0x21BC, 0x21BF, 0xFBB, 0xFBE, // FB20-FB2F
			0xFC1, 0xFC9, 0xFD1, 0xFD6, 0xFDB, 0xFE1, 0xFE4, 0, 0xFE7, 0xFED, 0xFF0, 0xFF5, 0xFFD, 0, 0x1002, 0, // FB30-FB3F
			0x1005, 0x1008, 0, 0x100D, 0x1010, 0, 0x1016, 0x1019, 0x101E, 0x1021, 0x102C, 0xFDE, 0xFCC, 0xFF8, 0x1013, 0xFC4, // FB40-FB4F
			0x1467, 0x1467, 0x146F, 0x146F, 0x146F, 0x146F, 0x1471, 0x1471, 0x1471, 0x1471, 0x1475, 0x1475, 0x1475, 0x1475, 0x146D, 0x146D,
			// FB50-FB5F
			0x146D, 0x146D, 0x1473, 0x1473, 0x1473, 0x1473, 0x146B, 0x146B, 0x146B, 0x146B, 0x148B, 0x148B, 0x148B, 0x148B, 0x148D, 0x148D,
			// FB60-FB6F
			0x148D, 0x148D, 0x1479, 0x1479, 0x1479, 0x1479, 0x1477, 0x1477, 0x1477, 0x1477, 0x147B, 0x147B, 0x147B, 0x147B, 0x147D, 0x147D,
			// FB70-FB7F
			0x147D, 0x147D, 0x1483, 0x1483, 0x1481, 0x1481, 0x1485, 0x1485, 0x147F, 0x147F, 0x1489, 0x1489, 0x1487, 0x1487, 0x148F, 0x148F,
			// FB80-FB8F
			0x148F, 0x148F, 0x1493, 0x1493, 0x1493, 0x1493, 0x1497, 0x1497, 0x1497, 0x1497, 0x1495, 0x1495, 0x1495, 0x1495, 0x1499, 0x1499,
			// FB90-FB9F
			0x149B, 0x149B, 0x149B, 0x149B, 0x149F, 0x149F, 0x14A1, 0x14A1, 0x14A1, 0x14A1, 0x149D, 0x149D, 0x149D, 0x149D, 0x14B9, 0x14B9,
			// FBA0-FBAF
			0x14BE, 0x14BE, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // FBB0-FBBF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // FBC0-FBCF
			0, 0, 0, 0x1491, 0x1491, 0x1491, 0x1491, 0x14AA, 0x14AA, 0x14A8, 0x14A8, 0x14AF, 0x14AF, 0x1469, 0x14B3, 0x14B3, // FBD0-FBDF
			0x14A6, 0x14A6, 0x14B1, 0x14B1, 0x14B7, 0x14B7, 0x14B7, 0x14B7, 0x142C, 0x142C, 0x103E, 0x103E, 0x106E, 0x106E, 0x1059, 0x1059,
			// FBE0-FBEF
			0x1065, 0x1065, 0x1062, 0x1062, 0x1068, 0x1068, 0x106B, 0x106B, 0x106B, 0x105C, 0x105C, 0x105C, 0x14B5, 0x14B5, 0x14B5, 0x14B5,
			// FBF0-FBFF
			0x1041, 0x1044, 0x1050, 0x105C, 0x105F, 0x108E, 0x1091, 0x1098, 0x10A5, 0x10AE, 0x10B1, 0x10B8, 0x10C7, 0x10D2, 0x10E7, 0x1104,
			// FC00-FC0F
			0x1107, 0x110C, 0x1115, 0x111E, 0x1121, 0x1126, 0x113A, 0x1151, 0x1158, 0x116B, 0x116E, 0x1171, 0x1194, 0x119F, 0x11A6, 0x11B4,
			// FC10-FC1F
			0x11FC, 0x122D, 0x123C, 0x123F, 0x124A, 0x1254, 0x125F, 0x1262, 0x1279, 0x127E, 0x128A, 0x12A1, 0x12A4, 0x12D9, 0x12DC, 0x12DF,
			// FC20-FC2F
			0x12E6, 0x12ED, 0x12F0, 0x12F5, 0x12FC, 0x130B, 0x130E, 0x1313, 0x1316, 0x1319, 0x131C, 0x131F, 0x1322, 0x132D, 0x1330, 0x1341,
			// FC30-FC3F
			0x1350, 0x135F, 0x1366, 0x1374, 0x1377, 0x137F, 0x1392, 0x13A6, 0x13B5, 0x13BC, 0x13BF, 0x13C4, 0x13D7, 0x13E6, 0x13EF, 0x1400,
			// FC40-FC4F
			0x1403, 0x1408, 0x140B, 0x1416, 0x1419, 0x1433, 0x143A, 0x1441, 0x144A, 0x145B, 0x145E, 0x117E, 0x1188, 0x142E, 0x39, 0x40, // FC50-FC5F
			0x47, 0x4E, 0x55, 0x5C, 0x104A, 0x104D, 0x1050, 0x1053, 0x105C, 0x105F, 0x109F, 0x10A2, 0x10A5, 0x10A8, 0x10AE, 0x10B1, // FC60-FC6F
			0x10E1, 0x10E4, 0x10E7, 0x10FE, 0x1104, 0x1107, 0x110F, 0x1112, 0x1115, 0x1118, 0x111E, 0x1121, 0x12ED, 0x12F0, 0x130B, 0x130E,
			// FC70-FC7F
			0x1313, 0x131F, 0x1322, 0x132D, 0x1330, 0x1366, 0x1374, 0x1377, 0x137C, 0x13B5, 0x13E9, 0x13EC, 0x13EF, 0x13FA, 0x1400, 0x1403,
			// FC80-FC8F
			0x142E, 0x1444, 0x1447, 0x144A, 0x1455, 0x145B, 0x145E, 0x1041, 0x1044, 0x1047, 0x1050, 0x1056, 0x108E, 0x1091, 0x1098, 0x10A5,
			// FC90-FC9F
			0x10AB, 0x10B8, 0x10C7, 0x10D2, 0x10E7, 0x1101, 0x1115, 0x1126, 0x113A, 0x1151, 0x1158, 0x116B, 0x1171, 0x1194, 0x119F, 0x11A6,
			// FCA0-FCAF
			0x11B4, 0x11FC, 0x1207, 0x122D, 0x123C, 0x123F, 0x124A, 0x1254, 0x125F, 0x1279, 0x127E, 0x128A, 0x12A1, 0x12A4, 0x12D9, 0x12DC,
			// FCB0-FCBF
			0x12DF, 0x12E6, 0x12F5, 0x12FC, 0x1316, 0x1319, 0x131C, 0x131F, 0x1322, 0x1341, 0x1350, 0x135F, 0x1366, 0x1371, 0x137F, 0x1392,
			// FCC0-FCCF
			0x13A6, 0x13B5, 0x13C4, 0x13D7, 0x13E6, 0x13EF, 0x13FD, 0x1408, 0x140B, 0x141C, 0x1433, 0x143A, 0x1441, 0x144A, 0x1458, 0x1050,
			// FCD0-FCDF
			0x1056, 0x10A5, 0x10AB, 0x10E7, 0x1101, 0x1115, 0x111B, 0x11B4, 0x11C3, 0x11E6, 0x11F1, 0x131F, 0x1322, 0x1366, 0x13EF, 0x13FD,
			// FCE0-FCEF
			0x144A, 0x1458, 0x12BF, 0x12C6, 0x12CD, 0x1271, 0x1274, 0x1299, 0x129C, 0x12B3, 0x12B6, 0x11C6, 0x11C9, 0x11F4, 0x11F7, 0x1163,
			// FCF0-FCFF
			0x1166, 0x1149, 0x114C, 0x1174, 0x1177, 0x1234, 0x1237, 0x1257, 0x125A, 0x11CE, 0x11D5, 0x11E0, 0x11E6, 0x11E3, 0x11B1, 0x120A,
			// FD00-FD0F
			0x1251, 0x1271, 0x1274, 0x1299, 0x129C, 0x12B3, 0x12B6, 0x11C6, 0x11C9, 0x11F4, 0x11F7, 0x1163, 0x1166, 0x1149, 0x114C, 0x1174,
			// FD10-FD1F
			0x1177, 0x1234, 0x1237, 0x1257, 0x125A, 0x11CE, 0x11D5, 0x11E0, 0x11E6, 0x11E3, 0x11B1, 0x120A, 0x1251, 0x11CE, 0x11D5, 0x11E0,
			// FD20-FD2F
			0x11E6, 0x11C3, 0x11F1, 0x1262, 0x1194, 0x119F, 0x11A6, 0x11CE, 0x11D5, 0x11E0, 0x1262, 0x1279, 0x107D, 0x107D, 0, 0, // FD30-FD3F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // FD40-FD4F
			0x10BB, 0x10CA, 0x10CA, 0x10CE, 0x10D5, 0x10EA, 0x10EE, 0x10F2, 0x113D, 0x113D, 0x115F, 0x115B, 0x11A2, 0x1197, 0x119B, 0x11BB,
			// FD50-FD5F
			0x11BB, 0x11B7, 0x11BF, 0x11BF, 0x11FF, 0x11FF, 0x1230, 0x11D8, 0x11D8, 0x11D1, 0x11E9, 0x11E9, 0x11ED, 0x11ED, 0x1242, 0x124D,
			// FD60-FD6F
			0x124D, 0x1265, 0x1265, 0x1269, 0x126D, 0x1281, 0x128D, 0x128D, 0x1291, 0x12A7, 0x12AF, 0x12AB, 0x12E2, 0x12E2, 0x12FF, 0x1303,
			// FD70-FD7F
			0x1353, 0x135B, 0x1357, 0x1344, 0x1344, 0x1362, 0x1362, 0x1369, 0x1369, 0x1395, 0x1399, 0x13A2, 0x1382, 0x138A, 0x13A9, 0x13AD,
			// FD80-FD8F
			0, 0, 0x1386, 0x140E, 0x1412, 0x13DA, 0x13DE, 0x13CB, 0x13CB, 0x13CF, 0x13F6, 0x13F2, 0x144D, 0x144D, 0x109B, 0x10C3, // FD90-FD9F
			0x10BF, 0x10DD, 0x10D9, 0x10FA, 0x10F6, 0x1145, 0x1129, 0x1141, 0x11A9, 0x1203, 0x11DC, 0x1246, 0x134C, 0x136D, 0x143D, 0x1436,
			// FDA0-FDAF
			0x1451, 0x13B8, 0x1307, 0x13E2, 0x12FF, 0x1353, 0x1295, 0x1329, 0x13C7, 0x13B1, 0x1348, 0x1325, 0x1348, 0x13C7, 0x112D, 0x1154,
			// FDB0-FDBF
			0x138E, 0x12E9, 0x1094, 0x1325, 0x1281, 0x1230, 0x11AD, 0x13D3, 0, 0, 0, 0, 0, 0, 0, 0, // FDC0-FDCF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // FDD0-FDDF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // FDE0-FDEF
			0x1229, 0x12F8, 0x1078, 0x1073, 0x139D, 0x120D, 0x1183, 0x1285, 0x1421, 0x1212, 0x1216, 0x1131, 0x118B, 0, 0, 0, // FDF0-FDFF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // FE00-FE0F
			0x26A, 0x193F, 0x1941, 0x493, 0x499, 0x69, 0x4B1, 0x195D, 0x195F, 0x187D, 0, 0, 0, 0, 0, 0, // FE10-FE1F
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // FE20-FE2F
			0x187B, 0x1879, 0x1877, 0x7E1, 0x7E1, 0x7D, 0x264, 0xBA4, 0xBA8, 0x1959, 0x195B, 0x1953, 0x1955, 0x1947, 0x1949, 0x1943, // FE30-FE3F
			0x1945, 0x194B, 0x194D, 0x194F, 0x1951, 0, 0, 0x7D9, 0x7DD, 0x1892, 0x1892, 0x1892, 0x1892, 0x7E1, 0x7E1, 0x7E1, // FE40-FE4F
			0x26A, 0x193F, 0x26E, 0, 0x499, 0x493, 0x4B1, 0x69, 0x1879, 0x7D, 0x264, 0xBA4, 0xBA8, 0x1959, 0x195B, 0x73, // FE50-FE5F
			0x79, 0x266, 0x268, 0x26C, 0x49B, 0x4AC, 0x4A0, 0, 0x7DB, 0x75, 0x77, 0x4B9, 0, 0, 0, 0, // FE60-FE6F
			0x33, 0x12B9, 0x36, 0, 0x3D, 0, 0x44, 0x12BC, 0x4B, 0x12C3, 0x52, 0x12CA, 0x59, 0x12D1, 0x60, 0x12D4, // FE70-FE7F
			0x1032, 0x1034, 0x1034, 0x1036, 0x1036, 0x1038, 0x1038, 0x103A, 0x103A, 0x103C, 0x103C, 0x103C, 0x103C, 0x1071, 0x1071, 0x108C,
			// FE80-FE8F
			0x108C, 0x108C, 0x108C, 0x10B4, 0x10B4, 0x10B6, 0x10B6, 0x10B6, 0x10B6, 0x110A, 0x110A, 0x110A, 0x110A, 0x1124, 0x1124, 0x1124,
			// FE90-FE9F
			0x1124, 0x114F, 0x114F, 0x114F, 0x114F, 0x1169, 0x1169, 0x1169, 0x1169, 0x117A, 0x117A, 0x117C, 0x117C, 0x1181, 0x1181, 0x1190,
			// FEA0-FEAF
			0x1190, 0x1192, 0x1192, 0x1192, 0x1192, 0x11CC, 0x11CC, 0x11CC, 0x11CC, 0x11FA, 0x11FA, 0x11FA, 0x11FA, 0x123A, 0x123A, 0x123A,
			// FEB0-FEBF
			0x123A, 0x125D, 0x125D, 0x125D, 0x125D, 0x1277, 0x1277, 0x1277, 0x1277, 0x127C, 0x127C, 0x127C, 0x127C, 0x129F, 0x129F, 0x129F,
			// FEC0-FECF
			0x129F, 0x12D7, 0x12D7, 0x12D7, 0x12D7, 0x12F3, 0x12F3, 0x12F3, 0x12F3, 0x1311, 0x1311, 0x1311, 0x1311, 0x1333, 0x1333, 0x1333,
			// FED0-FEDF
			0x1333, 0x137A, 0x137A, 0x137A, 0x137A, 0x13C2, 0x13C2, 0x13C2, 0x13C2, 0x1406, 0x1406, 0x1406, 0x1406, 0x141F, 0x141F, 0x142C,
			// FEE0-FEEF
			0x142C, 0x1431, 0x1431, 0x1431, 0x1431, 0x1335, 0x1335, 0x1338, 0x1338, 0x133B, 0x133B, 0x133E, 0x133E, 0, 0, 0, // FEF0-FEFF
			0, 0x69, 0x71, 0x73, 0x75, 0x77, 0x79, 0x7B, 0x7D, 0x264, 0x266, 0x268, 0x26A, 0x26C, 0x26E, 0x277, // FF00-FF0F
			0x279, 0x282, 0x35A, 0x3CE, 0x40E, 0x43E, 0x457, 0x465, 0x477, 0x485, 0x493, 0x499, 0x49B, 0x4A0, 0x4AC, 0x4B1, // FF10-FF1F
			0x4B9, 0x4BB, 0x4F4, 0x502, 0x51C, 0x53C, 0x571, 0x57A, 0x59F, 0x5BF, 0x601, 0x606, 0x620, 0x641, 0x660, 0x686, // FF20-FF2F
			0x6B8, 0x6D1, 0x6D3, 0x6F0, 0x70D, 0x72F, 0x76A, 0x782, 0x799, 0x7A8, 0x7C5, 0x7D9, 0x7DB, 0x7DD, 0x7DF, 0x7E1, // FF30-FF3F
			0x7E3, 0x7E5, 0x827, 0x836, 0x864, 0x892, 0x8CE, 0x8E7, 0x902, 0x923, 0x962, 0x96A, 0x9A8, 0x9CF, 0xA11, 0xA43, // FF40-FF4F
			0xA78, 0xA97, 0xA99, 0xAC4, 0xAE1, 0xAFB, 0xB36, 0xB4A, 0xB61, 0xB70, 0xB90, 0xBA4, 0xBA6, 0xBA8, 0xBAA, 0x1936, // FF50-FF5F
			0x1938, 0x1941, 0x194B, 0x194D, 0x193F, 0x1C0E, 0x1C07, 0x19B9, 0x19D0, 0x19DD, 0x19E8, 0x19F7, 0x1BB8, 0x1BC4, 0x1BCC, 0x1ABC,
			// FF60-FF6F
			0x1C10, 0x19BB, 0x19D2, 0x19DF, 0x19EA, 0x19F9, 0x1A03, 0x1A1E, 0x1A4A, 0x1A64, 0x1A6D, 0x1A7D, 0x1A8D, 0x1A97, 0x1A9C, 0x1AA9,
			// FF70-FF7F
			0x1AAE, 0x1AB7, 0x1ABE, 0x1AC3, 0x1ACB, 0x1AD6, 0x1ADB, 0x1ADD, 0x1ADF, 0x1AE1, 0x1AE7, 0x1B02, 0x1B1A, 0x1B37, 0x1B5C, 0x1B7C,
			// FF80-FF8F
			0x1B95, 0x1BA5, 0x1BA7, 0x1BB6, 0x1BBA, 0x1BC6, 0x1BCE, 0x1BD0, 0x1BD2, 0x1BDC, 0x1BE7, 0x1BF2, 0x1BF4, 0x1C0C, 0x19B2, 0x19B4,
			// FF90-FF9F
			0x1C7B, 0x1C15, 0x1C17, 0x1C19, 0x1C1B, 0x1C1D, 0x1C1F, 0x1C21, 0x1C23, 0x1C25, 0x1C27, 0x1C29, 0x1C2B, 0x1C2D, 0x1C2F, 0x1C31,
			// FFA0-FFAF
			0x1C33, 0x1C35, 0x1C37, 0x1C39, 0x1C3B, 0x1C3D, 0x1C3F, 0x1C41, 0x1C43, 0x1C45, 0x1C47, 0x1C49, 0x1C4B, 0x1C4D, 0x1C4F, 0, // FFB0-FFBF
			0, 0, 0x1C51, 0x1C53, 0x1C55, 0x1C57, 0x1C59, 0x1C5B, 0, 0, 0x1C5D, 0x1C5F, 0x1C61, 0x1C63, 0x1C65, 0x1C67, // FFC0-FFCF
			0, 0, 0x1C69, 0x1C6B, 0x1C6D, 0x1C6F, 0x1C71, 0x1C73, 0, 0, 0x1C75, 0x1C77, 0x1C79, 0, 0, 0, // FFD0-FFDF
			0xBAC, 0xBAE, 0xBBD, 0xBBF, 0xBB2, 0xBB0, 0x1894, 0, 0x1930, 0x1896, 0x189B, 0x189D, 0x18A2, 0x1932, 0x1934, 0, // FFE0-FFEF
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // FFF0-FFFF
		};
		static short[] helperIndexArr = new short[]
		{
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0000
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0010
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0020
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x0499, 0x049D, 0x04A9, 0x04AE, 0, // 0030
			0, 0x04C0, 0x04F9, 0x0508, 0x052A, 0x053E, 0x0577, 0x058A, 0x05AA, 0x05D4, 0x0603, 0x0606, 0x062F, 0x0654, 0x066B, 0x0688, // 0040
			0x06CB, 0, 0x06D8, 0x06F8, 0x071A, 0x0731, 0x0778, 0x0787, 0x07A2, 0x07AA, 0x07C7, 0, 0, 0, 0, 0, // 0050
			0x07E3, 0x07F7, 0x082D, 0x0855, 0x087D, 0x089B, 0x08E4, 0x08ED, 0x090B, 0x0938, 0x0964, 0x0993, 0x09BD, 0x09FC, 0x0A28, 0x0A48, // 0060
			0x0A91, 0, 0x0AAC, 0x0ACC, 0x0AE3, 0x0AFD, 0x0B44, 0x0B4C, 0x0B6A, 0x0B72, 0x0B92, 0, 0, 0, 0, 0, // 0070
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0080
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0090
			0, 0, 0, 0, 0, 0, 0, 0, 0x0BB4, 0, 0, 0, 0, 0, 0, 0, // 00A0
			0, 0, 0, 0, 0x0BC7, 0, 0, 0x0BC9, 0, 0, 0, 0, 0, 0, 0, 0, // 00B0
			0, 0, 0x0BCB, 0, 0x0BD7, 0x0BDA, 0x0BE1, 0x0BE7, 0, 0, 0x0BEA, 0, 0, 0, 0, 0x0BF6, // 00C0
			0, 0, 0, 0, 0x0BF9, 0x0C05, 0x0C0E, 0, 0x0C11, 0, 0, 0, 0x0C14, 0, 0, 0, // 00D0
			0, 0, 0x0C20, 0, 0x0C2C, 0x0C2F, 0x0C32, 0x0C38, 0, 0, 0x0C3B, 0, 0, 0, 0, 0x0C47, // 00E0
			0, 0, 0, 0, 0x0C4C, 0x0C58, 0x0C61, 0, 0x0C64, 0, 0, 0, 0x0C67, 0, 0, 0, // 00F0
			0, 0, 0x0C73, 0x0C7F, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0100
			0, 0, 0x0C8B, 0x0C91, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0110
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0120
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0130
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x0C9D, 0x0CA3, 0, 0, // 0140
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x0CAB, 0x0CAE, 0, 0, 0, 0, // 0150
			0x0CB1, 0x0CB4, 0, 0, 0, 0, 0, 0, 0x0CB7, 0x0CBA, 0x0CBD, 0x0CC0, 0, 0, 0, 0, // 0160
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x0CC6, // 0170
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0180
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0190
			0x0CCD, 0x0CDC, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x0CED, // 01A0
			0x0CFC, 0, 0, 0, 0, 0, 0, 0x0D0B, 0, 0, 0, 0, 0, 0, 0, 0, // 01B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 01C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 01D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x0D0E, 0x0D11, 0, 0, 0, 0, // 01E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 01F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0200
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0210
			0, 0, 0, 0, 0, 0, 0x0D16, 0x0D19, 0x0D1C, 0x0D1F, 0, 0, 0, 0, 0x0D22, 0x0D25, // 0220
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0230
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0240
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0250
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0260
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0270
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0280
			0, 0, 0x0D72, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0290
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 02A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x0D7B, 0, 0, 0, 0, 0, 0, // 02B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 02C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 02D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 02E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 02F0
			0x0D80, 0x0D82, 0, 0, 0, 0, 0, 0, 0x0D84, 0, 0, 0, 0, 0, 0, 0, // 0300
			0, 0, 0, 0x0D87, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0310
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0320
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0330
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0340
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0350
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0360
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0370
			0, 0, 0, 0, 0, 0x0D89, 0x0D8B, 0, 0x0D8D, 0x0D8F, 0x0D91, 0, 0x0D93, 0, 0x0D95, 0x0D97, // 0380
			0x0D99, 0x0D9B, 0, 0, 0, 0x0DB2, 0, 0x0DBE, 0, 0x0DCF, 0, 0, 0, 0, 0, 0x0DE4, // 0390
			0, 0x0DF2, 0, 0, 0, 0x0DF9, 0, 0, 0, 0x0E0B, 0, 0, 0x0E1C, 0x0E21, 0x0E23, 0x0E28, // 03A0
			0x0E2A, 0x0E2C, 0, 0, 0, 0x0E4C, 0, 0x0E58, 0, 0x0E6C, 0, 0, 0, 0, 0, 0x0EA2, // 03B0
			0, 0x0EB2, 0, 0, 0, 0x0EBA, 0, 0, 0, 0x0ED6, 0x0EE8, 0x0EF1, 0x0EFA, 0x0EFC, 0x0EFE, 0, // 03C0
			0, 0, 0x0F03, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 03D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 03E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 03F0
			0, 0, 0, 0, 0, 0, 0x0F09, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0400
			0x0F0C, 0, 0, 0x0F12, 0, 0x0F15, 0x0F1E, 0x0F24, 0x0F27, 0, 0x0F33, 0, 0, 0, 0x0F36, 0, // 0410
			0, 0, 0, 0x0F39, 0, 0, 0, 0x0F45, 0, 0, 0, 0x0F48, 0, 0x0F4B, 0, 0, // 0420
			0x0F4E, 0, 0, 0x0F54, 0, 0x0F57, 0x0F60, 0x0F66, 0x0F69, 0, 0x0F75, 0, 0, 0, 0x0F7A, 0, // 0430
			0, 0, 0, 0x0F7D, 0, 0, 0, 0x0F89, 0, 0, 0, 0x0F8C, 0, 0x0F8F, 0, 0, // 0440
			0, 0, 0, 0, 0, 0, 0x0F92, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0450
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0460
			0, 0, 0, 0, 0x0F95, 0x0F98, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0470
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0480
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0490
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 04A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 04B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 04C0
			0, 0, 0, 0, 0, 0, 0, 0, 0x0F9B, 0x0F9E, 0, 0, 0, 0, 0, 0, // 04D0
			0, 0, 0, 0, 0, 0, 0, 0, 0x0FA1, 0x0FA4, 0, 0, 0, 0, 0, 0, // 04E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 04F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0500
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0510
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0520
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0530
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0540
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0550
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0560
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0570
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0580
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0590
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 05A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 05B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 05C0
			0x0FBB, 0x0FC9, 0x0FD1, 0x0FD6, 0x0FDB, 0x0FDE, 0x0FE4, 0, 0x0FE7, 0x0FEA, 0x0FF0, 0x0FF5, 0x0FFD, 0, 0x1002, 0, // 05D0
			0x1005, 0x1008, 0, 0x100D, 0x1010, 0, 0x1016, 0x1019, 0x101E, 0x1021, 0x102C, 0, 0, 0, 0, 0, // 05E0
			0, 0, 0x102F, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 05F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0600
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0610
			0, 0, 0, 0, 0, 0, 0, 0x1080, 0, 0, 0, 0, 0, 0, 0, 0, // 0620
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0630
			0, 0, 0, 0, 0, 0, 0, 0, 0x1426, 0, 0x1461, 0, 0, 0, 0, 0, // 0640
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0650
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0660
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0670
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0680
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0690
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 06A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 06B0
			0, 0x14A3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 06C0
			0, 0, 0x14BB, 0, 0, 0x14C0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 06D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 06E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 06F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0900
			0, 0, 0, 0, 0, 0x14C3, 0x14C6, 0x14C9, 0, 0, 0, 0, 0x14CC, 0, 0, 0, // 0910
			0, 0x14CF, 0x14D2, 0, 0, 0, 0, 0, 0x14D5, 0, 0, 0x14D8, 0, 0, 0, 0x14DB, // 0920
			0x14DE, 0, 0, 0x14E1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0930
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0940
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0950
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0960
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0970
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0980
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0990
			0, 0x14E4, 0x14E7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x14EA, // 09A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 09B0
			0, 0, 0, 0, 0, 0, 0, 0x14ED, 0, 0, 0, 0, 0, 0, 0, 0, // 09C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 09D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 09E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 09F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0A00
			0, 0, 0, 0, 0, 0, 0x14F3, 0x14F6, 0, 0, 0, 0, 0x14F9, 0, 0, 0, // 0A10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x14FC, 0, 0, 0, 0, // 0A20
			0, 0, 0x14FF, 0, 0, 0, 0, 0, 0x1502, 0, 0, 0, 0, 0, 0, 0, // 0A30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0A40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0A50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0A60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0A70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0A80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0A90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0AA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0AB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0AC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0AD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0AE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0AF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0B00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0B10
			0, 0x1505, 0x1508, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0B20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0B30
			0, 0, 0, 0, 0, 0, 0, 0x150B, 0, 0, 0, 0, 0, 0, 0, 0, // 0B40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0B50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0B60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0B70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0B80
			0, 0, 0x1514, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0B90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0BA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0BB0
			0, 0, 0, 0, 0, 0, 0x1517, 0x151D, 0, 0, 0, 0, 0, 0, 0, 0, // 0BC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0BD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0BE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0BF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C30
			0, 0, 0, 0, 0, 0, 0x1520, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0CA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1523, // 0CB0
			0, 0, 0, 0, 0, 0, 0x1526, 0, 0, 0, 0x152F, 0, 0, 0, 0, 0, // 0CC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0CD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0CE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0CF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D30
			0, 0, 0, 0, 0, 0, 0x1532, 0x1538, 0, 0, 0, 0, 0, 0, 0, 0, // 0D40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0DA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0DB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0DC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x153B, 0, 0, 0x1544, 0, 0, 0, // 0DD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0DE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0DF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0EA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0EB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0EC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0ED0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0EE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0EF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0F00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0F10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0F20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0F30
			0x1555, 0, 0x1558, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x155B, 0, 0, 0, // 0F40
			0, 0x155E, 0, 0, 0, 0, 0x1561, 0, 0, 0, 0, 0x1564, 0, 0, 0, 0, // 0F50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0F60
			0, 0x1567, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0F70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0F80
			0x1570, 0, 0x1573, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1576, 0, 0, 0, // 0F90
			0, 0x1579, 0, 0, 0, 0, 0x157C, 0, 0, 0, 0, 0x157F, 0, 0, 0, 0, // 0FA0
			0, 0, 0x1582, 0x1588, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0FB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0FC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0FD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0FE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0FF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1000
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1010
			0, 0, 0, 0, 0, 0x158E, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1020
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1030
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1040
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1050
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1060
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1070
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1080
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1090
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 10A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 10B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 10C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 10D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 10E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 10F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1100
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1110
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1120
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1130
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1140
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1150
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1160
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1170
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1180
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1190
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 11A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 11B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 11C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 11D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 11E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 11F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1D00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1D10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1D20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1D30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1D40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1D50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1D60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1D70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1D80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1D90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1DA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1DB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1DC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1DD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1DE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1DF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1E00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1E10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1E20
			0, 0, 0, 0, 0, 0, 0x16B8, 0x16BB, 0, 0, 0, 0, 0, 0, 0, 0, // 1E30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1E40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x16BE, 0x16C1, 0, 0, 0, 0, // 1E50
			0, 0, 0x16C4, 0x16C7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1E60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1E70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1E80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1E90
			0x16CA, 0x16D0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1EA0
			0, 0, 0, 0, 0, 0, 0, 0, 0x16D6, 0x16D9, 0, 0, 0, 0, 0, 0, // 1EB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x16DC, 0x16DF, 0, 0, // 1EC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1ED0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1EE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1EF0
			0x16E2, 0x16EE, 0x16FA, 0x16FD, 0x1700, 0x1703, 0x1706, 0x1709, 0x170C, 0x1718, 0x1724, 0x1727, 0x172A, 0x172D, 0x1730, 0x1733, // 1F00
			0x1736, 0x173C, 0, 0, 0, 0, 0, 0, 0x1742, 0x1748, 0, 0, 0, 0, 0, 0, // 1F10
			0x174E, 0x175A, 0x1766, 0x1769, 0x176C, 0x176F, 0x1772, 0x1775, 0x1778, 0x1784, 0x1790, 0x1793, 0x1796, 0x1799, 0x179C, 0x179F, // 1F20
			0x17A2, 0x17AB, 0, 0, 0, 0, 0, 0, 0x17B4, 0x17BD, 0, 0, 0, 0, 0, 0, // 1F30
			0x17C6, 0x17CC, 0, 0, 0, 0, 0, 0, 0x17D2, 0x17D8, 0, 0, 0, 0, 0, 0, // 1F40
			0x17DE, 0x17E7, 0, 0, 0, 0, 0, 0, 0, 0x17F0, 0, 0, 0, 0, 0, 0, // 1F50
			0x17F9, 0x1805, 0x1811, 0x1814, 0x1817, 0x181A, 0x181D, 0x1820, 0x1823, 0x182F, 0x183B, 0x183E, 0x1841, 0x1844, 0x1847, 0x184A, // 1F60
			0x184D, 0, 0, 0, 0x1850, 0, 0, 0, 0, 0, 0, 0, 0x1853, 0, 0, 0, // 1F70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1F80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1F90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1FA0
			0, 0, 0, 0, 0, 0, 0x1856, 0, 0, 0, 0, 0, 0, 0, 0, 0x1859, // 1FB0
			0, 0, 0, 0, 0, 0, 0x1862, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1FC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1FD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1FE0
			0, 0, 0, 0, 0, 0, 0x1865, 0, 0, 0, 0, 0, 0, 0, 0x1868, 0, // 1FF0
			0, 0, 0x1871, 0x1873, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2000
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2010
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2020
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2030
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2040
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2050
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2060
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2070
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2080
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2090
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 20A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 20B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 20C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 20D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 20E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 20F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2100
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2110
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2120
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2130
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2140
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2150
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2160
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2170
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2180
			0x1898, 0, 0x189F, 0, 0x18A4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2190
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 21A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 21B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 21C0
			0x18A7, 0, 0x18AA, 0, 0x18AD, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 21D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 21E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 21F0
			0, 0, 0, 0x18B0, 0, 0, 0, 0, 0x18B3, 0, 0, 0x18B6, 0, 0, 0, 0, // 2200
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2210
			0, 0, 0, 0x18BD, 0, 0x18C0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2220
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x18D6, 0, 0, 0, // 2230
			0, 0, 0, 0x18D9, 0, 0x18DC, 0, 0, 0x18DF, 0, 0, 0, 0, 0x18E2, 0, 0, // 2240
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2250
			0, 0x18E5, 0, 0, 0x18E8, 0x18EB, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2260
			0, 0, 0x18EE, 0x18F1, 0, 0, 0x18F4, 0x18F7, 0, 0, 0x18FA, 0x18FD, 0x1900, 0x1903, 0, 0, // 2270
			0, 0, 0x1906, 0x1909, 0, 0, 0x190C, 0x190F, 0, 0, 0, 0, 0, 0, 0, 0, // 2280
			0, 0x1912, 0x1915, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2290
			0, 0, 0x1918, 0, 0, 0, 0, 0, 0x191B, 0x191E, 0, 0x1921, 0, 0, 0, 0, // 22A0
			0, 0, 0x1924, 0x1927, 0x192A, 0x192D, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 22B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 22C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 22D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 22E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 22F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2500
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2510
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2520
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2530
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2540
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2550
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2560
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2570
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2580
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 2590
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 25A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 25B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 25C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 25D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 25E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 25F0
			0, 0, 0, 0, 0, 0, 0, 0, 0x1943, 0x1945, 0, 0, 0, 0, 0, 0, // 3000
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 3010
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 3020
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 3030
			0, 0, 0, 0, 0, 0, 0x1961, 0, 0, 0, 0, 0x1964, 0, 0x1967, 0, 0x196A, // 3040
			0, 0x196D, 0, 0x1970, 0, 0x1973, 0, 0x1976, 0, 0x1979, 0, 0x197C, 0, 0x197F, 0, 0x1982, // 3050
			0, 0x1985, 0, 0, 0x1988, 0, 0x198B, 0, 0x198E, 0, 0, 0, 0, 0, 0, 0x1991, // 3060
			0, 0, 0x1997, 0, 0, 0x199D, 0, 0, 0x19A3, 0, 0, 0x19A9, 0, 0, 0, 0, // 3070
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 3080
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x19B6, 0, 0, // 3090
			0, 0, 0, 0, 0, 0, 0x19E1, 0, 0, 0, 0, 0x1A05, 0, 0x1A20, 0, 0x1A4C, // 30A0
			0, 0x1A66, 0, 0x1A6F, 0, 0x1A7F, 0, 0x1A8F, 0, 0x1A99, 0, 0x1A9E, 0, 0x1AAB, 0, 0x1AB0, // 30B0
			0, 0x1AB9, 0, 0, 0x1AC0, 0, 0x1AC5, 0, 0x1ACD, 0, 0, 0, 0, 0, 0, 0x1AE9, // 30C0
			0, 0, 0x1B04, 0, 0, 0x1B1C, 0, 0, 0x1B39, 0, 0, 0x1B5E, 0, 0, 0, 0, // 30D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1BF6, // 30E0
			0x1BFF, 0x1C04, 0x1C09, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1C12, 0, 0, // 30F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 3100
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 3110
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 3120
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 3130
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 3140
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 3150
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1C7D, 0, 0, // 3B90
			0, 0, 0, 0, 0, 0, 0, 0, 0x1C7F, 0, 0, 0, 0, 0, 0, 0, // 4010
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 4020
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1C8F, 0, 0, // 4E00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 4E10
			0, 0, 0, 0, 0, 0, 0x1C93, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 4E20
			0, 0, 0x1C99, 0, 0, 0, 0, 0, 0, 0x1C9D, 0, 0, 0, 0, 0, 0, // 4E30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 4E40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 4E50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 4E60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 4E70
			0, 0, 0x1CA5, 0, 0, 0, 0x1CA9, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 4E80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 4E90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1CB1, 0, // 4EA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 4EB0
			0x1CB5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 4EC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 4ED0
			0, 0, 0, 0, 0x1CB7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 4EE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 4EF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 4F00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 4F10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 4F20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 4F30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 4F40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 4F50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 4F60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 4F70
			0x1CBD, 0, 0, 0, 0, 0, 0x1CBF, 0, 0, 0, 0, 0x1CC1, 0, 0, 0, 0, // 4F80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 4F90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1CC3, 0, // 4FA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1CC5, // 4FB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 4FC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 4FD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 4FE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 4FF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5000
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5010
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1CC7, 0, 0, 0, 0, // 5020
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5030
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5040
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5050
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5060
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5070
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5080
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5090
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 50A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 50B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 50C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1CC9, 0, 0, 0, 0, 0, // 50D0
			0, 0, 0, 0, 0, 0, 0, 0x1CCB, 0, 0, 0, 0, 0, 0, 0, 0, // 50E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 50F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5100
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5110
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5120
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5130
			0x1CD1, 0, 0, 0, 0, 0x1CD3, 0, 0, 0, 0, 0, 0, 0, 0x1CD5, 0, 0, // 5140
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5150
			0, 0, 0, 0, 0, 0, 0, 0, 0x1CD9, 0x1CDB, 0, 0, 0, 0x1CDF, 0, 0, // 5160
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5170
			0x1CE1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5180
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5190
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 51A0
			0, 0, 0, 0, 0, 0x1CEB, 0, 0x1CED, 0, 0, 0, 0, 0, 0, 0, 0, // 51B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1CEF, 0, 0, 0x1CF1, 0, 0, 0, // 51C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1CF3, 0, 0x1CF5, 0, // 51D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 51E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 51F0
			0, 0, 0, 0, 0, 0, 0, 0x1CFD, 0, 0, 0, 0, 0, 0, 0, 0, // 5200
			0, 0, 0, 0, 0, 0, 0, 0x1CFF, 0, 0, 0, 0, 0, 0, 0, 0, // 5210
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1D01, 0, 0, 0, 0, 0, 0, // 5220
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1D03, 0, 0, 0, 0, 0, // 5230
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5240
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5250
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5260
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5270
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1D05, 0, 0, 0, 0, 0, 0, // 5280
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1D07, 0, 0, 0, 0, // 5290
			0, 0, 0, 0x1D09, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 52A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 52B0
			0, 0, 0, 0, 0, 0, 0, 0x1D0D, 0, 0x1D0F, 0, 0, 0, 0, 0, 0, // 52C0
			0, 0, 0x1D11, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1D13, 0, // 52D0
			0, 0, 0, 0, 0x1D15, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 52E0
			0, 0, 0, 0, 0, 0x1D17, 0, 0, 0, 0, 0x1D1B, 0, 0, 0, 0, 0, // 52F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5300
			0, 0, 0, 0, 0, 0, 0, 0x1D1F, 0, 0, 0, 0, 0, 0, 0, 0, // 5310
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5320
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1D27, // 5330
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5340
			0, 0x1D2F, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5350
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5360
			0, 0, 0, 0, 0, 0x1D39, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5370
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5380
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5390
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 53A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 53B0
			0, 0, 0, 0x1D3F, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 53C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 53D0
			0, 0, 0, 0, 0, 0x1D45, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 53E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 53F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1D4B, // 5400
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1D4D, 0, 0, // 5410
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5420
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5430
			0, 0, 0x1D4F, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5440
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5450
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5460
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5470
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5480
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5490
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 54A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1D51, 0, 0, // 54B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 54C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 54D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 54E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 54F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5500
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5510
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5520
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5530
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5540
			0, 0, 0, 0, 0, 0x1D55, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5550
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5560
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5570
			0, 0, 0, 0, 0, 0, 0, 0x1D57, 0, 0, 0, 0, 0, 0, 0, 0, // 5580
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1D59, 0, 0, 0, 0x1D5B, 0, 0, // 5590
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 55A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 55B0
			0x1D5D, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 55C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 55D0
			0, 0, 0x1D5F, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 55E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 55F0
			0, 0, 0, 0, 0, 0, 0x1D61, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5600
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5610
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5620
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5630
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5640
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5650
			0, 0, 0, 0, 0, 0, 0, 0, 0x1D63, 0, 0, 0, 0, 0, 0, 0, // 5660
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5670
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5680
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5690
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 56A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 56B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 56C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 56D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 56E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1D69, 0, 0, 0, 0, 0, 0, // 56F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5700
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5710
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5720
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5730
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5740
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5750
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5760
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5770
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5780
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5790
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 57A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 57B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 57C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 57D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 57E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 57F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5800
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5810
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5820
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5830
			0x1D6F, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5840
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1D71, 0, 0, 0, 0x1D73, 0, // 5850
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5860
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5870
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5880
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5890
			0, 0, 0, 0, 0, 0, 0, 0, 0x1D75, 0, 0, 0, 0, 0, 0, 0, // 58A0
			0, 0, 0, 0x1D77, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 58B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 58C0
			0, 0, 0, 0, 0, 0, 0, 0, 0x1D79, 0, 0, 0, 0, 0, 0, 0x1D7B, // 58D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 58E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 58F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5900
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5910
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5920
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5930
			0, 0, 0, 0, 0x1D8E, 0, 0, 0, 0x1D90, 0, 0, 0, 0, 0, 0, 0, // 5940
			0, 0x1D92, 0, 0, 0x1D94, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5950
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5960
			0, 0, 0, 0x1D96, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5970
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5980
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5990
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 59A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 59B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 59C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 59D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 59E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 59F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5A00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5A10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5A20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5A30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5A40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5A50
			0, 0, 0x1D98, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5A60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5A70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5A80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5A90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5AA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5AB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5AC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5AD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5AE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5AF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5B00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5B10
			0, 0, 0, 0, 0, 0, 0, 0, 0x1D9A, 0, 0, 0, 0, 0, 0, 0, // 5B20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5B30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5B40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5B50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5B60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5B70
			0, 0, 0, 0, 0, 0x1DA2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5B80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5B90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5BA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5BB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5BC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5BD0
			0, 0, 0, 0, 0, 0, 0, 0x1DA6, 0, 0, 0, 0, 0, 0, 0x1DA8, 0, // 5BE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5BF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5C00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5C10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5C20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1DB2, // 5C30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5C40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5C50
			0, 0, 0x1DB4, 0, 0x1DB6, 0x1DB8, 0, 0, 0, 0, 0, 0, 0, 0, 0x1DBA, 0, // 5C60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5C70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5C80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5C90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5CA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5CB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5CC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5CD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5CE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5CF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5D00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1DBE, 0, 0, 0, 0, 0, 0, // 5D10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5D20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5D30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5D40
			0x1DC0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5D50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5D60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5D70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5D80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5D90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5DA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1DC2, 0, 0, 0, 0, 0, // 5DB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5DC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5DD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5DE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5DF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5E00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5E10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5E20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5E30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5E40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5E50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5E60
			0, 0, 0, 0, 0x1DD3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5E70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5E80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5E90
			0, 0, 0, 0, 0, 0, 0x1DDB, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5EA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5EB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1DDD, 0x1DDF, 0, 0, 0, 0, 0, // 5EC0
			0, 0, 0x1DE1, 0x1DE3, 0, 0, 0, 0, 0, 0x1DE5, 0, 0, 0, 0, 0, 0, // 5ED0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1DE7, 0, 0, 0, // 5EE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5EF0
			0, 0, 0, 0, 0x1DED, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5F00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5F10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5F20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5F30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5F40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5F50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1DF7, 0, 0, 0, 0, 0, 0, // 5F60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5F70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1DFB, 0, 0, 0, 0, // 5F80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5F90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1DFD, 0, 0, 0, 0x1DFF, 0, 0, // 5FA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5FB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5FC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5FD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5FE0
			0, 0, 0, 0, 0, 0x1E03, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 5FF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6000
			0, 0, 0x1E05, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1E07, 0, 0, 0, // 6010
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6020
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6030
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6040
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6050
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6060
			0, 0, 0, 0, 0, 0x1E09, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6070
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6080
			0, 0, 0, 0, 0x1E0B, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6090
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 60A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 60B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 60C0
			0, 0, 0, 0, 0, 0, 0, 0, 0x1E0D, 0, 0, 0, 0, 0, 0, 0, // 60D0
			0, 0x1E0F, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 60E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 60F0
			0, 0, 0, 0, 0, 0, 0, 0, 0x1E11, 0, 0, 0, 0, 0, 0, 0, // 6100
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6110
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6120
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6130
			0, 0, 0, 0, 0x1E13, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1E15, 0, // 6140
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6150
			0x1E17, 0, 0, 0, 0, 0, 0, 0, 0x1E19, 0, 0, 0, 0, 0, 0, 0, // 6160
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6170
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1E1B, 0, // 6180
			0x1E1D, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6190
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 61A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 61B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 61C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 61D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 61E0
			0, 0, 0x1E1F, 0, 0, 0, 0x1E21, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 61F0
			0x1E23, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6200
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6210
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1E27, 0, // 6220
			0, 0, 0, 0, 0x1E29, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6230
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6240
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6250
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6260
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6270
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6280
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6290
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 62A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 62B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1E2F, 0, 0, 0, 0, 0, 0x1E31, // 62C0
			0, 0, 0, 0x1E33, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 62D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 62E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1E35, 0, // 62F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6300
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6310
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6320
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6330
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6340
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6350
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6360
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1E37, 0, 0, 0, 0, // 6370
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6380
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6390
			0x1E39, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 63A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 63B0
			0, 0, 0, 0, 0x1E3B, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 63C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 63D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 63E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 63F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6400
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1E3D, 0, 0, 0, // 6410
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6420
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6430
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6440
			0, 0, 0x1E3F, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6450
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6460
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6470
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6480
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1E41, 0, 0, 0, 0, 0, // 6490
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 64A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 64B0
			0, 0, 0, 0, 0x1E43, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 64C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 64D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 64E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 64F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6500
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6510
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6520
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6530
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1E49, // 6540
			0, 0, 0, 0, 0, 0, 0x1E4B, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6550
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6560
			0, 0, 0, 0, 0, 0, 0, 0, 0x1E4D, 0, 0, 0, 0, 0, 0, 0, // 6570
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6580
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1E53, 0, 0, 0, 0, 0, 0, // 6590
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 65A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 65B0
			0, 0, 0, 0, 0, 0x1E59, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 65C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 65D0
			0, 0, 0x1E5D, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 65E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 65F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6600
			0, 0, 0, 0x1E64, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6610
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6620
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6630
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6640
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6650
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6660
			0, 0, 0, 0, 0x1E69, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6670
			0, 0, 0, 0, 0, 0, 0, 0, 0x1E6B, 0, 0, 0, 0, 0, 0, 0, // 6680
			0, 0x1E6D, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6690
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 66A0
			0, 0, 0, 0, 0x1E6F, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 66B0
			0, 0, 0, 0, 0, 0, 0x1E71, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 66C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 66D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 66E0
			0, 0, 0, 0, 0x1E75, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 66F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6700
			0, 0, 0, 0, 0, 0, 0, 0x1E7B, 0, 0, 0, 0x1E7D, 0, 0, 0, 0, // 6710
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6720
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6730
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1E81, 0, // 6740
			0, 0, 0, 0, 0, 0, 0x1E83, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6750
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6760
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1E85, 0, 0, 0, 0, // 6770
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6780
			0, 0, 0, 0, 0, 0, 0, 0x1E87, 0, 0, 0, 0, 0, 0, 0, 0, // 6790
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 67A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 67B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 67C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 67D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 67E0
			0, 0, 0, 0x1E89, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 67F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6800
			0, 0, 0, 0, 0, 0, 0, 0x1E8B, 0, 0, 0, 0, 0, 0, 0, 0, // 6810
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6820
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6830
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6840
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6850
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6860
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6870
			0, 0x1E94, 0, 0, 0, 0x1E96, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6880
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6890
			0, 0, 0, 0, 0, 0, 0, 0, 0x1E98, 0, 0, 0, 0, 0, 0, 0, // 68A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 68B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 68C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 68D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 68E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 68F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6900
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6910
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6920
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6930
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6940
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6950
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6960
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6970
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6980
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6990
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 69A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 69B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 69C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 69D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 69E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 69F0
			0, 0, 0x1E9A, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6A00
			0, 0, 0, 0x1E9C, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6A10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6A20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6A30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6A40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6A50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6A60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6A70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6A80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6A90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6AA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6AB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6AC0
			0, 0, 0, 0x1E9E, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6AD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6AE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6AF0
			0, 0, 0, 0, 0x1EA0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6B00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6B10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6B20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6B30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6B40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6B50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6B60
			0, 0, 0, 0, 0, 0, 0, 0x1EA8, 0, 0x1EAA, 0, 0, 0, 0, 0, 0, // 6B70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6B80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6B90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1EAC, 0, // 6BA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1EB0, 0, 0, 0, 0, 0, // 6BB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6BC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6BD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6BE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6BF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6C00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6C10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6C20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6C30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6C40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6C50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6C60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6C70
			0, 0, 0, 0, 0, 0, 0, 0, 0x1EC0, 0, 0, 0, 0, 0, 0, 0, // 6C80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6C90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6CA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6CB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1EC2, 0, 0, 0, // 6CC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6CD0
			0, 0, 0, 0, 0, 0x1EC4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6CE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6CF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6D00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1EC8, 0, 0, 0x1ECA, 0, // 6D10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6D20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6D30
			0, 0x1ECC, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6D40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6D50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1ECE, 0, 0, 0, 0, 0, // 6D60
			0, 0, 0, 0, 0, 0, 0, 0x1ED0, 0, 0, 0, 0, 0, 0, 0, 0, // 6D70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6D80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6D90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6DA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6DB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1ED2, 0, 0, 0, 0, // 6DC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1ED4, 0, 0, 0, 0, 0, // 6DD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1ED6, 0, 0, 0, 0, 0, // 6DE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6DF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6E00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1ED8, 0, 0, 0, 0, 0, // 6E10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6E20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6E30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6E40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6E50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6E60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6E70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6E80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1EDA, 0, 0, 0, // 6E90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6EA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1EDC, 0, 0, 0, 0, 0, // 6EB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1EDE, 0, 0, 0, 0, // 6EC0
			0, 0x1EE0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1EE2, 0, 0, 0, 0, // 6ED0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6EE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6EF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1EE4, // 6F00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6F10
			0, 0, 0x1EE6, 0x1EE8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6F20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6F30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6F40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6F50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6F60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6F70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6F80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6F90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6FA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6FB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6FC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 6FD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1EEA, 0, 0, 0, 0, // 6FE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1EEC, 0, // 6FF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7000
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1EEE, 0, // 7010
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7020
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7030
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7040
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7050
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7060
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7070
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7080
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1EF2, 0, 0, 0, 0, 0, 0, // 7090
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 70A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 70B0
			0, 0, 0, 0, 0, 0, 0, 0, 0x1EF4, 0, 0, 0, 0, 0, 0, 0, // 70C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1EF6, 0, 0, 0, 0, 0, 0, // 70D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 70E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 70F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7100
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7110
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7120
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7130
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1EF8, 0, 0, 0, 0, 0, 0, // 7140
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7150
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1EFA, 0, // 7160
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7170
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7180
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7190
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 71A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 71B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1EFC, 0, // 71C0
			0x1EFE, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 71D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 71E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 71F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7200
			0x1F00, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1F02, 0, 0, 0, 0, // 7210
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1F06, 0, 0, 0, 0, // 7220
			0, 0, 0, 0, 0, 0x1F08, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7230
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7240
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7250
			0, 0, 0x1F16, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7260
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7270
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7280
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7290
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1F1C, // 72A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 72B0
			0x1F1E, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 72C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 72D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 72E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1F20, 0, 0, 0, // 72F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7300
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7310
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1F22, 0, 0, 0, 0, 0, // 7320
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7330
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7340
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7350
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7360
			0, 0, 0, 0, 0, 0x1F24, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7370
			0, 0, 0, 0, 0, 0, 0, 0x1F28, 0, 0, 0, 0, 0, 0, 0, 0, // 7380
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7390
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 73A0
			0, 0, 0x1F2C, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 73B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 73C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1F2E, 0, // 73D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 73E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 73F0
			0, 0, 0, 0, 0, 0, 0x1F30, 0, 0, 0x1F32, 0, 0, 0, 0, 0, 0, // 7400
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7410
			0, 0, 0x1F34, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7420
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7430
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7440
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7450
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1F36, 0, 0, 0, 0, 0, 0, // 7460
			0, 0x1F38, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7470
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1F3A, 0, 0, 0, 0, 0, 0, // 7480
			0, 0, 0, 0, 0, 0, 0, 0, 0x1F3C, 0, 0, 0, 0, 0, 0, 0, // 7490
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 74A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 74B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 74C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 74D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 74E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 74F0
			0, 0, 0, 0, 0, 0, 0x1F42, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7500
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7510
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7520
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1F50, 0, 0, 0, 0, // 7530
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7540
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1F52, 0, 0, 0, 0, 0, 0, // 7550
			0, 0, 0, 0, 0, 0x1F54, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7560
			0x1F56, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7570
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7580
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7590
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 75A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 75B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 75C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 75D0
			0, 0, 0x1F5C, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 75E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 75F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7600
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1F5E, 0, 0x1F60, // 7610
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7620
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7630
			0, 0, 0x1F62, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7640
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7650
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1F64, 0, 0, 0, 0, 0, 0, // 7660
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7670
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7680
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7690
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 76A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 76B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1F6E, 0, 0, 0, 0, 0, // 76C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1F70, 0, 0, 0, 0, // 76D0
			0, 0, 0, 0, 0, 0, 0, 0x1F74, 0, 0, 0, 0, 0, 0, 0, 0, // 76E0
			0, 0, 0, 0, 0x1F78, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 76F0
			0, 0x1F7A, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7700
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7710
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7720
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7730
			0x1F7C, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1F7E, 0, 0, 0, 0, 0, // 7740
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7750
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7760
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7770
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7780
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7790
			0, 0, 0, 0, 0, 0, 0, 0x1F80, 0, 0, 0, 0, 0, 0, 0, 0, // 77A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 77B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 77C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 77D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 77E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 77F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7800
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7810
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7820
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7830
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7840
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7850
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1F88, 0, 0, 0, 0, // 7860
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7870
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1F8A, 0, 0, 0, // 7880
			0, 0x1F8C, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7890
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 78A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 78B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1F8E, 0, 0x1F90, 0, 0, 0, // 78C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 78D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 78E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1F92, 0, 0, 0, 0, // 78F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7900
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7910
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1F94, 0, 0, 0, 0, 0, // 7920
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1F98, 0, 0x1F9A, 0, // 7930
			0, 0, 0, 0, 0, 0, 0, 0, 0x1F9C, 0x1F9E, 0, 0, 0, 0, 0, 0, // 7940
			0x1FA0, 0, 0, 0, 0, 0, 0x1FA2, 0, 0, 0, 0, 0, 0, 0x1FA4, 0x1FA6, 0, // 7950
			0, 0, 0, 0, 0, 0x1FA8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7960
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1FAA, // 7970
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1FAC, 0x1FAE, 0x1FB0, // 7980
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7990
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1FB2, 0, // 79A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 79B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1FB8, 0, 0, 0, 0, 0, // 79C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 79D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 79E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 79F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7A00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1FBC, 0, 0, 0, // 7A10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7A20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7A30
			0x1FBE, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7A40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7A50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7A60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7A70
			0, 0x1FC2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7A80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7A90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7AA0
			0, 0x1FC4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7AB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1FC6, 0, 0, 0, 0, // 7AC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7AD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7AE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7AF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7B00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7B10
			0x1FCA, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7B20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7B30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7B40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7B50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7B60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7B70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7B80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7B90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7BA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7BB0
			0x1FCE, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7BC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7BD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7BE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7BF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7C00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7C10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7C20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1FD0, 0, // 7C30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7C40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7C50
			0x1FD2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7C60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1FD6, 0, 0, 0, 0, // 7C70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7C80
			0, 0, 0x1FD8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7C90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7CA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1FDA, 0, // 7CB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7CC0
			0, 0, 0, 0, 0, 0, 0x1FDC, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7CD0
			0, 0, 0, 0, 0, 0, 0, 0x1FDE, 0, 0, 0, 0, 0, 0, 0, 0, // 7CE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7CF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7D00
			0x1FE2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7D10
			0, 0, 0x1FE4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1FE6, // 7D20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7D30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7D40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1FE8, 0, 0, 0, 0, // 7D50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7D60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7D70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7D80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7D90
			0x1FEA, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7DA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1FEC, 0, // 7DB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7DC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7DD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7DE0
			0, 0, 0, 0, 0x1FEE, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7DF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1FF0, 0, 0, 0, 0, 0, 0, // 7E00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7E10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7E20
			0, 0, 0, 0, 0, 0, 0, 0x1FF2, 0, 0, 0, 0, 0, 0, 0, 0, // 7E30
			0, 0x1FF4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7E40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7E50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7E60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7E70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7E80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7E90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7EA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7EB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7EC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7ED0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7EE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7EF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7F00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7F10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7F20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1FF8, 0, // 7F30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7F40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7F50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7F60
			0, 0, 0x1FFC, 0, 0, 0, 0, 0, 0, 0x1FFE, 0, 0, 0, 0, 0, 0, // 7F70
			0, 0, 0, 0, 0, 0x2000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7F80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2004, 0, 0, 0, 0, 0, // 7F90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7FA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2006, 0, 0, // 7FB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7FC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7FD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7FE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 7FF0
			0, 0x2008, 0, 0, 0, 0x200A, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8000
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8010
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8020
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8030
			0, 0, 0, 0, 0, 0, 0x2012, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8040
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8050
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2014, // 8060
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2016, 0, // 8070
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x201C, 0, 0, 0, 0, // 8080
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8090
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 80A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 80B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 80C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 80D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 80E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 80F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8100
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8110
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8120
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8130
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8140
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8150
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8160
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8170
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8180
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8190
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 81A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 81B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 81C0
			0, 0, 0, 0, 0, 0, 0, 0, 0x201E, 0, 0, 0, 0, 0, 0, 0, // 81D0
			0, 0, 0, 0, 0, 0, 0, 0, 0x2022, 0, 0, 0, 0, 0x2026, 0, 0, // 81E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 81F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8200
			0, 0, 0, 0, 0, 0, 0, 0, 0x202E, 0, 0, 0, 0, 0, 0, 0, // 8210
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8220
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8230
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8240
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8250
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2036, // 8260
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x203C, 0, 0, 0, 0, 0, 0, // 8270
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8280
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8290
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 82A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 82B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 82C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 82D0
			0, 0, 0, 0, 0, 0x203E, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 82E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 82F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8300
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8310
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8320
			0, 0, 0, 0, 0, 0, 0x2040, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8330
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8340
			0, 0, 0x2042, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8350
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8360
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8370
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8380
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8390
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 83A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 83B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2044, 0, 0, 0, 0, 0, 0, // 83C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 83D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2046, // 83E0
			0, 0x2048, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 83F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8400
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8410
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8420
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x204A, 0, 0, // 8430
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x204C, 0, 0, 0, 0, 0, 0, // 8440
			0, 0, 0, 0, 0, 0, 0, 0x204E, 0, 0, 0, 0, 0, 0, 0, 0, // 8450
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8460
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8470
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8480
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8490
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 84A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 84B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 84C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 84D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2050, 0, // 84E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2052, 0, 0, 0, // 84F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8500
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8510
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8520
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8530
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8540
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8550
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8560
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8570
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8580
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8590
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 85A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 85B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2054, 0, 0, // 85C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 85D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 85E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2056, 0, 0, 0, 0, 0, // 85F0
			0, 0, 0, 0, 0, 0, 0x2058, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8600
			0, 0, 0x205A, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8610
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x205C, 0, 0, // 8620
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x205E, // 8630
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8640
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2062, 0, 0, 0, // 8650
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8660
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8670
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8680
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8690
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 86A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 86B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 86C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 86D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 86E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 86F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8700
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8710
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8720
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8730
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8740
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8750
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8760
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2066, 0, 0, 0, 0, 0, 0, // 8770
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8780
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8790
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 87A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2068, 0, 0, 0, 0, 0, // 87B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 87C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 87D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 87E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 87F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8800
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x206A, // 8810
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8820
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8830
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x206E, 0, 0, 0, // 8840
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8850
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8860
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8870
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8880
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8890
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 88A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 88B0
			0, 0, 0x2072, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2074, // 88C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 88D0
			0, 0x2076, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 88E0
			0, 0, 0, 0, 0, 0, 0, 0, 0x2078, 0, 0, 0, 0, 0, 0, 0, // 88F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8900
			0x207A, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8910
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8920
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8930
			0, 0x207C, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8940
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8950
			0, 0, 0, 0, 0x207E, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8960
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8970
			0, 0, 0, 0, 0, 0, 0x2082, 0, 0, 0, 0, 0x2084, 0, 0, 0, 0, // 8980
			0, 0, 0, 0, 0, 0, 0x2086, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8990
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 89A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 89B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 89C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 89D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 89E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 89F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8A00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8A10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8A20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8A30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8A40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8A50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8A60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8A70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8A80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8A90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x208C, 0, 0, 0, 0, 0, // 8AA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x208E, // 8AB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2090, 0, 0, 0, 0, // 8AC0
			0, 0, 0x2092, 0, 0, 0, 0x2094, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8AD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2096, 0, 0, // 8AE0
			0, 0, 0, 0, 0, 0, 0, 0, 0x2098, 0, 0, 0, 0, 0, 0x209A, 0, // 8AF0
			0, 0x209C, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8B00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8B10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8B20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x209E, 0, 0, 0, 0, 0, 0, // 8B30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8B40
			0, 0, 0, 0, 0, 0, 0, 0, 0x20A0, 0, 0, 0, 0, 0, 0, 0, // 8B50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8B60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8B70
			0x20A2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x20A4, 0, 0, 0, 0, 0, // 8B80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8B90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8BA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8BB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8BC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8BD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8BE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8BF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8C00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8C10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8C20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8C30
			0, 0, 0, 0, 0, 0, 0, 0, 0x20AA, 0, 0, 0, 0, 0, 0, 0, // 8C40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8C50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8C60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8C70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8C80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8C90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8CA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8CB0
			0, 0, 0x20B4, 0, 0, 0, 0, 0, 0x20B8, 0, 0, 0, 0, 0, 0, 0, // 8CC0
			0, 0, 0, 0x20BA, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8CD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8CE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8CF0
			0, 0, 0, 0, 0, 0, 0, 0, 0x20BC, 0, 0, 0, 0, 0, 0, 0, // 8D00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8D10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8D20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8D30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8D40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8D50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8D60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8D70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8D80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8D90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8DA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8DB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8DC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8DD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x20C4, // 8DE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8DF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8E00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8E10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8E20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8E30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8E40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8E50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8E60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8E70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8E80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8E90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8EA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8EB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x20C8, 0, 0, 0, 0, 0, // 8EC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8ED0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8EE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8EF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8F00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8F10
			0, 0, 0, 0, 0, 0, 0x20CA, 0, 0, 0, 0x20CC, 0, 0, 0, 0, 0, // 8F20
			0, 0, 0, 0, 0, 0, 0, 0, 0x20CE, 0, 0, 0x20D0, 0, 0, 0, 0, // 8F30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8F40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8F50
			0, 0, 0x20D2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8F60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8F70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8F80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8F90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8FA0
			0x20D6, 0, 0, 0, 0, 0, 0x20DA, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8FB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8FC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8FD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8FE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 8FF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9000
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9010
			0, 0, 0, 0x20DC, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9020
			0, 0, 0, 0, 0, 0, 0, 0, 0x20DE, 0, 0, 0, 0, 0, 0, 0, // 9030
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9040
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9050
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9060
			0, 0, 0x20E2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x20E4, 0, 0, 0, // 9070
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x20E6, // 9080
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9090
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 90A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 90B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x20EA, 0, // 90C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x20EC, 0, // 90D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 90E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x20EE, 0, 0, // 90F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9100
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9110
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9120
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9130
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9140
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9150
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x20F2, 0, 0, 0, 0, 0, // 9160
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9170
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9180
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x20F4, 0, 0, 0, 0, 0, 0, // 9190
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 91A0
			0, 0, 0, 0, 0x20F6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 91B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x20FA, 0, 0, 0x20FC, // 91C0
			0, 0x20FE, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 91D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 91E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 91F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9200
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9210
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9220
			0, 0, 0, 0, 0x2100, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9230
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9240
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9250
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9260
			0, 0, 0, 0, 0, 0, 0x2102, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9270
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9280
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9290
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 92A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 92B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 92C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 92D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 92E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 92F0
			0, 0, 0, 0, 0x2104, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9300
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9310
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9320
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9330
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2106, 0, 0, 0, 0, 0, // 9340
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9350
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9360
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9370
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9380
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9390
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 93A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 93B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 93C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 93D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 93E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 93F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9400
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9410
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9420
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9430
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9440
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9450
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9460
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9470
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9480
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9490
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 94A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 94B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 94C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 94D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 94E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 94F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9500
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9510
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9520
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9530
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9540
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9550
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9560
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9570
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9580
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9590
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x210C, 0, 0, // 95A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 95B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 95C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 95D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 95E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 95F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9600
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9610
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2110, 0, // 9620
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9630
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2112, 0, 0x2114, 0, 0, // 9640
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9650
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9660
			0, 0, 0, 0, 0, 0x2116, 0, 0, 0x2118, 0, 0, 0, 0x211A, 0, 0, 0, // 9670
			0, 0, 0, 0, 0, 0, 0x211C, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9680
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9690
			0, 0, 0, 0x211E, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 96A0
			0, 0, 0, 0, 0, 0, 0, 0x2122, 0x2124, 0, 0, 0, 0, 0, 0, 0, // 96B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 96C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 96D0
			0, 0, 0x2128, 0x212A, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 96E0
			0, 0, 0, 0, 0, 0, 0x212E, 0x2130, 0, 0, 0, 0, 0, 0, 0, 0, // 96F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9700
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9710
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9720
			0, 0, 0x2132, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9730
			0, 0, 0, 0, 0, 0, 0, 0, 0x2134, 0, 0, 0, 0, 0, 0, 0, // 9740
			0, 0, 0, 0, 0, 0, 0x2138, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9750
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9760
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9770
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9780
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9790
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 97A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 97B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 97C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2142, 0, 0, 0, 0, // 97D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 97E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2148, // 97F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x214E, 0, 0, 0, 0, // 9800
			0, 0, 0, 0, 0, 0, 0, 0, 0x2150, 0, 0, 0, 0, 0, 0, 0, // 9810
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9820
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2152, 0, 0, 0, 0, // 9830
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9840
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2154, 0, // 9850
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9860
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9870
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9880
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9890
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 98A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 98B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 98C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 98D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x215C, // 98E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x215E, 0, 0, 0, // 98F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9900
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9910
			0, 0, 0, 0, 0, 0, 0, 0, 0x2160, 0, 0, 0, 0, 0, 0, 0, // 9920
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9930
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9940
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9950
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9960
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9970
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9980
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9990
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 99A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 99B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 99C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 99D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 99E0
			0, 0x2168, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 99F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9A00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9A10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9A20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9A30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9A40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9A50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x216A, 0, 0, 0, 0, 0, // 9A60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9A70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9A80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9A90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9AA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9AB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9AC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9AD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9AE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9AF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9B00
			0, 0, 0x2172, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9B10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9B20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9B30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9B40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9B50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x217E, // 9B60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9B70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9B80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9B90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9BA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9BB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9BC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9BD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9BE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9BF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9C00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9C10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9C20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9C30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9C40
			0, 0, 0, 0, 0, 0, 0, 0x2180, 0, 0, 0, 0, 0, 0, 0, 0, // 9C50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9C60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9C70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9C80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9C90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9CA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9CB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9CC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9CD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9CE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9CF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9D00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9D10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9D20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9D30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9D40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9D50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9D60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9D70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9D80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9D90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9DA0
			0, 0, 0, 0, 0x2184, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9DB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9DC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9DD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9DE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2186, 0, 0, 0, 0, 0, // 9DF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9E00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2188, 0, // 9E10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9E20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9E30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9E40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9E50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9E60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x218C, // 9E70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9E80
			0, 0, 0, 0, 0, 0, 0, 0x218E, 0, 0, 0, 0, 0, 0, 0, 0x2190, // 9E90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9EA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9EB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x219A, 0, // 9EC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9ED0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9EE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9EF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9F00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9F10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9F20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9F30
			0, 0, 0, 0x21AA, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9F40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9F50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9F60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9F70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x21B0, 0x21B2, 0, // 9F80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x21B4, 0, 0, 0, // 9F90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9FA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9FB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9FC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9FD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9FE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 9FF0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x21BC, 0, 0, 0, 0, 0, 0, // FB40
		};
		static ushort[] mapIdxToCompositeArr = new ushort[]
		{
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0480
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x037E, 0, 0, 0, 0x226E, 0, 0, // 0490
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2260, 0, 0, 0, 0, 0x226F, 0, // 04A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 04B0
			0x00C0, 0, 0, 0x00C1, 0, 0, 0x00C2, 0, 0, 0x00C3, 0, 0, 0x0100, 0, 0, 0x0102, // 04C0
			0, 0, 0x0226, 0, 0, 0x00C4, 0, 0, 0x1EA2, 0, 0, 0x00C5, 0, 0, 0x01CD, 0, // 04D0
			0, 0x0200, 0, 0, 0x0202, 0, 0, 0x1EA0, 0, 0, 0x1E00, 0, 0, 0x0104, 0, 0, // 04E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1E02, 0, 0, 0x1E04, 0, 0, 0x1E06, // 04F0
			0, 0, 0, 0, 0, 0, 0, 0, 0x0106, 0, 0, 0x0108, 0, 0, 0x010A, 0, // 0500
			0, 0x010C, 0, 0, 0x00C7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0510
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1E0A, 0, 0, 0x010E, 0, 0, // 0520
			0x1E0C, 0, 0, 0x1E10, 0, 0, 0x1E12, 0, 0, 0x1E0E, 0, 0, 0, 0, 0x00C8, 0, // 0530
			0, 0x00C9, 0, 0, 0x00CA, 0, 0, 0x1EBC, 0, 0, 0x0112, 0, 0, 0x0114, 0, 0, // 0540
			0x0116, 0, 0, 0x00CB, 0, 0, 0x1EBA, 0, 0, 0x011A, 0, 0, 0x0204, 0, 0, 0x0206, // 0550
			0, 0, 0x1EB8, 0, 0, 0x0228, 0, 0, 0x0118, 0, 0, 0x1E18, 0, 0, 0x1E1A, 0, // 0560
			0, 0, 0, 0, 0, 0, 0, 0x1E1E, 0, 0, 0, 0, 0, 0, 0, 0, // 0570
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x01F4, 0, 0, 0x011C, 0, 0, // 0580
			0x1E20, 0, 0, 0x011E, 0, 0, 0x0120, 0, 0, 0x01E6, 0, 0, 0x0122, 0, 0, 0, // 0590
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x0124, 0, 0, 0x1E22, 0, 0, // 05A0
			0x1E26, 0, 0, 0x021E, 0, 0, 0x1E24, 0, 0, 0x1E28, 0, 0, 0x1E2A, 0, 0, 0, // 05B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 05C0
			0, 0, 0, 0, 0x00CC, 0, 0, 0x00CD, 0, 0, 0x00CE, 0, 0, 0x0128, 0, 0, // 05D0
			0x012A, 0, 0, 0x012C, 0, 0, 0x0130, 0, 0, 0x00CF, 0, 0, 0x1EC8, 0, 0, 0x01CF, // 05E0
			0, 0, 0x0208, 0, 0, 0x020A, 0, 0, 0x1ECA, 0, 0, 0x012E, 0, 0, 0x1E2C, 0, // 05F0
			0, 0, 0, 0x0134, 0, 0, 0x212A, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0600
			0, 0x1E30, 0, 0, 0x01E8, 0, 0, 0x1E32, 0, 0, 0x0136, 0, 0, 0x1E34, 0, 0, // 0610
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x0139, // 0620
			0, 0, 0x013D, 0, 0, 0x1E36, 0, 0, 0x013B, 0, 0, 0x1E3C, 0, 0, 0x1E3A, 0, // 0630
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0640
			0, 0, 0, 0, 0x1E3E, 0, 0, 0x1E40, 0, 0, 0x1E42, 0, 0, 0, 0, 0, // 0650
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x01F8, 0, 0, 0x0143, 0, // 0660
			0, 0x00D1, 0, 0, 0x1E44, 0, 0, 0x0147, 0, 0, 0x1E46, 0, 0, 0x0145, 0, 0, // 0670
			0x1E4A, 0, 0, 0x1E48, 0, 0, 0, 0, 0x00D2, 0, 0, 0x00D3, 0, 0, 0x00D4, 0, // 0680
			0, 0x00D5, 0, 0, 0x014C, 0, 0, 0x014E, 0, 0, 0x022E, 0, 0, 0x00D6, 0, 0, // 0690
			0x1ECE, 0, 0, 0x0150, 0, 0, 0x01D1, 0, 0, 0x020C, 0, 0, 0x020E, 0, 0, 0x01A0, // 06A0
			0, 0, 0x1ECC, 0, 0, 0x01EA, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 06B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1E54, 0, 0, 0x1E56, 0, // 06C0
			0, 0, 0, 0, 0, 0, 0, 0, 0x0154, 0, 0, 0x1E58, 0, 0, 0x0158, 0, // 06D0
			0, 0x0210, 0, 0, 0x0212, 0, 0, 0x1E5A, 0, 0, 0x0156, 0, 0, 0x1E5E, 0, 0, // 06E0
			0, 0, 0, 0, 0, 0, 0, 0, 0x015A, 0, 0, 0x015C, 0, 0, 0x1E60, 0, // 06F0
			0, 0x0160, 0, 0, 0x1E62, 0, 0, 0x0218, 0, 0, 0x015E, 0, 0, 0, 0, 0, // 0700
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1E6A, 0, 0, 0x0164, 0, 0, // 0710
			0x1E6C, 0, 0, 0x021A, 0, 0, 0x0162, 0, 0, 0x1E70, 0, 0, 0x1E6E, 0, 0, 0, // 0720
			0, 0x00D9, 0, 0, 0x00DA, 0, 0, 0x00DB, 0, 0, 0x0168, 0, 0, 0x016A, 0, 0, // 0730
			0x016C, 0, 0, 0x00DC, 0, 0, 0x1EE6, 0, 0, 0x016E, 0, 0, 0x0170, 0, 0, 0x01D3, // 0740
			0, 0, 0x0214, 0, 0, 0x0216, 0, 0, 0x01AF, 0, 0, 0x1EE4, 0, 0, 0x1E72, 0, // 0750
			0, 0x0172, 0, 0, 0x1E76, 0, 0, 0x1E74, 0, 0, 0, 0, 0, 0, 0, 0, // 0760
			0, 0, 0, 0, 0, 0, 0, 0, 0x1E7C, 0, 0, 0x1E7E, 0, 0, 0, 0, // 0770
			0, 0, 0, 0, 0, 0, 0, 0x1E80, 0, 0, 0x1E82, 0, 0, 0x0174, 0, 0, // 0780
			0x1E86, 0, 0, 0x1E84, 0, 0, 0x1E88, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0790
			0, 0, 0x1E8A, 0, 0, 0x1E8C, 0, 0, 0, 0, 0x1EF2, 0, 0, 0x00DD, 0, 0, // 07A0
			0x0176, 0, 0, 0x1EF8, 0, 0, 0x0232, 0, 0, 0x1E8E, 0, 0, 0x0178, 0, 0, 0x1EF6, // 07B0
			0, 0, 0x1EF4, 0, 0, 0, 0, 0x0179, 0, 0, 0x1E90, 0, 0, 0x017B, 0, 0, // 07C0
			0x017D, 0, 0, 0x1E92, 0, 0, 0x1E94, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 07D0
			0, 0, 0, 0x1FEF, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 07E0
			0, 0, 0, 0, 0, 0, 0, 0x00E0, 0, 0, 0x00E1, 0, 0, 0x00E2, 0, 0, // 07F0
			0x00E3, 0, 0, 0x0101, 0, 0, 0x0103, 0, 0, 0x0227, 0, 0, 0x00E4, 0, 0, 0x1EA3, // 0800
			0, 0, 0x00E5, 0, 0, 0x01CE, 0, 0, 0x0201, 0, 0, 0x0203, 0, 0, 0x1EA1, 0, // 0810
			0, 0x1E01, 0, 0, 0x0105, 0, 0, 0, 0, 0, 0, 0, 0, 0x1E03, 0, 0, // 0820
			0x1E05, 0, 0, 0x1E07, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0830
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0840
			0, 0, 0, 0, 0, 0x0107, 0, 0, 0x0109, 0, 0, 0x010B, 0, 0, 0x010D, 0, // 0850
			0, 0x00E7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0860
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1E0B, 0, 0, // 0870
			0x010F, 0, 0, 0x1E0D, 0, 0, 0x1E11, 0, 0, 0x1E13, 0, 0, 0x1E0F, 0, 0, 0, // 0880
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x00E8, 0, 0, 0x00E9, 0, // 0890
			0, 0x00EA, 0, 0, 0x1EBD, 0, 0, 0x0113, 0, 0, 0x0115, 0, 0, 0x0117, 0, 0, // 08A0
			0x00EB, 0, 0, 0x1EBB, 0, 0, 0x011B, 0, 0, 0x0205, 0, 0, 0x0207, 0, 0, 0x1EB9, // 08B0
			0, 0, 0x0229, 0, 0, 0x0119, 0, 0, 0x1E19, 0, 0, 0x1E1B, 0, 0, 0, 0, // 08C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 08D0
			0, 0, 0, 0, 0x1E1F, 0, 0, 0, 0, 0, 0, 0, 0, 0x01F5, 0, 0, // 08E0
			0x011D, 0, 0, 0x1E21, 0, 0, 0x011F, 0, 0, 0x0121, 0, 0, 0x01E7, 0, 0, 0x0123, // 08F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x0125, 0, 0, 0x1E23, 0, // 0900
			0, 0x1E27, 0, 0, 0x021F, 0, 0, 0x1E25, 0, 0, 0x1E29, 0, 0, 0x1E2B, 0, 0, // 0910
			0x1E96, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0920
			0, 0, 0, 0, 0, 0, 0, 0, 0x00EC, 0, 0, 0x00ED, 0, 0, 0x00EE, 0, // 0930
			0, 0x0129, 0, 0, 0x012B, 0, 0, 0x012D, 0, 0, 0x00EF, 0, 0, 0x1EC9, 0, 0, // 0940
			0x01D0, 0, 0, 0x0209, 0, 0, 0x020B, 0, 0, 0x1ECB, 0, 0, 0x012F, 0, 0, 0x1E2D, // 0950
			0, 0, 0, 0, 0x0135, 0, 0, 0x01F0, 0, 0, 0, 0, 0, 0, 0, 0, // 0960
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0970
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0980
			0, 0, 0, 0x1E31, 0, 0, 0x01E9, 0, 0, 0x1E33, 0, 0, 0x0137, 0, 0, 0x1E35, // 0990
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 09A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x013A, 0, 0, // 09B0
			0x013E, 0, 0, 0x1E37, 0, 0, 0x013C, 0, 0, 0x1E3D, 0, 0, 0x1E3B, 0, 0, 0, // 09C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 09D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 09E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1E3F, 0, 0, 0x1E41, // 09F0
			0, 0, 0x1E43, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0A00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0A10
			0, 0, 0, 0, 0, 0, 0, 0, 0x01F9, 0, 0, 0x0144, 0, 0, 0x00F1, 0, // 0A20
			0, 0x1E45, 0, 0, 0x0148, 0, 0, 0x1E47, 0, 0, 0x0146, 0, 0, 0x1E4B, 0, 0, // 0A30
			0x1E49, 0, 0, 0, 0, 0, 0, 0, 0x00F2, 0, 0, 0x00F3, 0, 0, 0x00F4, 0, // 0A40
			0, 0x00F5, 0, 0, 0x014D, 0, 0, 0x014F, 0, 0, 0x022F, 0, 0, 0x00F6, 0, 0, // 0A50
			0x1ECF, 0, 0, 0x0151, 0, 0, 0x01D2, 0, 0, 0x020D, 0, 0, 0x020F, 0, 0, 0x01A1, // 0A60
			0, 0, 0x1ECD, 0, 0, 0x01EB, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0A70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0A80
			0, 0x1E55, 0, 0, 0x1E57, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0A90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x0155, 0, 0, 0x1E59, // 0AA0
			0, 0, 0x0159, 0, 0, 0x0211, 0, 0, 0x0213, 0, 0, 0x1E5B, 0, 0, 0x0157, 0, // 0AB0
			0, 0x1E5F, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x015B, 0, 0, 0x015D, // 0AC0
			0, 0, 0x1E61, 0, 0, 0x0161, 0, 0, 0x1E63, 0, 0, 0x0219, 0, 0, 0x015F, 0, // 0AD0
			0, 0, 0, 0x1E6B, 0, 0, 0x1E97, 0, 0, 0x0165, 0, 0, 0x1E6D, 0, 0, 0x021B, // 0AE0
			0, 0, 0x0163, 0, 0, 0x1E71, 0, 0, 0x1E6F, 0, 0, 0, 0, 0x00F9, 0, 0, // 0AF0
			0x00FA, 0, 0, 0x00FB, 0, 0, 0x0169, 0, 0, 0x016B, 0, 0, 0x016D, 0, 0, 0x00FC, // 0B00
			0, 0, 0x1EE7, 0, 0, 0x016F, 0, 0, 0x0171, 0, 0, 0x01D4, 0, 0, 0x0215, 0, // 0B10
			0, 0x0217, 0, 0, 0x01B0, 0, 0, 0x1EE5, 0, 0, 0x1E73, 0, 0, 0x0173, 0, 0, // 0B20
			0x1E77, 0, 0, 0x1E75, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0B30
			0, 0, 0, 0, 0x1E7D, 0, 0, 0x1E7F, 0, 0, 0, 0, 0x1E81, 0, 0, 0x1E83, // 0B40
			0, 0, 0x0175, 0, 0, 0x1E87, 0, 0, 0x1E85, 0, 0, 0x1E98, 0, 0, 0x1E89, 0, // 0B50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1E8B, 0, 0, 0x1E8D, 0, 0, // 0B60
			0, 0, 0x1EF3, 0, 0, 0x00FD, 0, 0, 0x0177, 0, 0, 0x1EF9, 0, 0, 0x0233, 0, // 0B70
			0, 0x1E8F, 0, 0, 0x00FF, 0, 0, 0x1EF7, 0, 0, 0x1E99, 0, 0, 0x1EF5, 0, 0, // 0B80
			0, 0, 0x017A, 0, 0, 0x1E91, 0, 0, 0x017C, 0, 0, 0x017E, 0, 0, 0x1E93, 0, // 0B90
			0, 0x1E95, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0BA0
			0, 0, 0, 0, 0x1FED, 0, 0, 0x0385, 0, 0, 0x1FC1, 0, 0, 0, 0, 0, // 0BB0
			0, 0, 0, 0, 0, 0, 0, 0x1FFD, 0, 0x0387, 0, 0x1EA6, 0, 0, 0x1EA4, 0, // 0BC0
			0, 0x1EAA, 0, 0, 0x1EA8, 0, 0, 0x01DE, 0, 0, 0x212B, 0, 0x01FA, 0, 0, 0, // 0BD0
			0, 0x01FC, 0, 0, 0x01E2, 0, 0, 0x1E08, 0, 0, 0x1EC0, 0, 0, 0x1EBE, 0, 0, // 0BE0
			0x1EC4, 0, 0, 0x1EC2, 0, 0, 0x1E2E, 0, 0, 0x1ED2, 0, 0, 0x1ED0, 0, 0, 0x1ED6, // 0BF0
			0, 0, 0x1ED4, 0, 0, 0x1E4C, 0, 0, 0x022C, 0, 0, 0x1E4E, 0, 0, 0x022A, 0, // 0C00
			0, 0x01FE, 0, 0, 0x01DB, 0, 0, 0x01D7, 0, 0, 0x01D5, 0, 0, 0x01D9, 0, 0, // 0C10
			0x1EA7, 0, 0, 0x1EA5, 0, 0, 0x1EAB, 0, 0, 0x1EA9, 0, 0, 0x01DF, 0, 0, 0x01FB, // 0C20
			0, 0, 0x01FD, 0, 0, 0x01E3, 0, 0, 0x1E09, 0, 0, 0x1EC1, 0, 0, 0x1EBF, 0, // 0C30
			0, 0x1EC5, 0, 0, 0x1EC3, 0, 0, 0x1E2F, 0, 0, 0, 0, 0x1ED3, 0, 0, 0x1ED1, // 0C40
			0, 0, 0x1ED7, 0, 0, 0x1ED5, 0, 0, 0x1E4D, 0, 0, 0x022D, 0, 0, 0x1E4F, 0, // 0C50
			0, 0x022B, 0, 0, 0x01FF, 0, 0, 0x01DC, 0, 0, 0x01D8, 0, 0, 0x01D6, 0, 0, // 0C60
			0x01DA, 0, 0, 0x1EB0, 0, 0, 0x1EAE, 0, 0, 0x1EB4, 0, 0, 0x1EB2, 0, 0, 0x1EB1, // 0C70
			0, 0, 0x1EAF, 0, 0, 0x1EB5, 0, 0, 0x1EB3, 0, 0, 0x1E14, 0, 0, 0x1E16, 0, // 0C80
			0, 0x1E15, 0, 0, 0x1E17, 0, 0, 0, 0, 0, 0, 0, 0, 0x1E50, 0, 0, // 0C90
			0x1E52, 0, 0, 0x1E51, 0, 0, 0x1E53, 0, 0, 0, 0, 0x1E64, 0, 0, 0x1E65, 0, // 0CA0
			0, 0x1E66, 0, 0, 0x1E67, 0, 0, 0x1E78, 0, 0, 0x1E79, 0, 0, 0x1E7A, 0, 0, // 0CB0
			0x1E7B, 0, 0, 0, 0, 0, 0x1E9B, 0, 0, 0, 0, 0, 0, 0x1EDC, 0, 0, // 0CC0
			0x1EDA, 0, 0, 0x1EE0, 0, 0, 0x1EDE, 0, 0, 0x1EE2, 0, 0, 0x1EDD, 0, 0, 0x1EDB, // 0CD0
			0, 0, 0x1EE1, 0, 0, 0x1EDF, 0, 0, 0x1EE3, 0, 0, 0, 0, 0x1EEA, 0, 0, // 0CE0
			0x1EE8, 0, 0, 0x1EEE, 0, 0, 0x1EEC, 0, 0, 0x1EF0, 0, 0, 0x1EEB, 0, 0, 0x1EE9, // 0CF0
			0, 0, 0x1EEF, 0, 0, 0x1EED, 0, 0, 0x1EF1, 0, 0, 0x01EE, 0, 0, 0x01EC, 0, // 0D00
			0, 0x01ED, 0, 0, 0, 0, 0x01E0, 0, 0, 0x01E1, 0, 0, 0x1E1C, 0, 0, 0x1E1D, // 0D10
			0, 0, 0x0230, 0, 0, 0x0231, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0D60
			0, 0, 0x01EF, 0, 0, 0, 0, 0, 0, 0, 0, 0x0374, 0, 0, 0, 0, // 0D70
			0x0340, 0, 0x0341, 0, 0x0344, 0, 0, 0x0343, 0, 0x1FEE, 0, 0x1FBB, 0, 0x1FC9, 0, 0x1FCB, // 0D80
			0, 0x1FDB, 0, 0x1FF9, 0, 0x1FEB, 0, 0x1FFB, 0, 0x1FD3, 0, 0x1FBA, 0, 0, 0x0386, 0, // 0D90
			0, 0x1FB9, 0, 0, 0x1FB8, 0, 0, 0x1F08, 0, 0, 0x1F09, 0, 0, 0x1FBC, 0, 0, // 0DA0
			0, 0, 0x1FC8, 0, 0, 0x0388, 0, 0, 0x1F18, 0, 0, 0x1F19, 0, 0, 0x1FCA, 0, // 0DB0
			0, 0x0389, 0, 0, 0x1F28, 0, 0, 0x1F29, 0, 0, 0x1FCC, 0, 0, 0, 0, 0x1FDA, // 0DC0
			0, 0, 0x038A, 0, 0, 0x1FD9, 0, 0, 0x1FD8, 0, 0, 0x03AA, 0, 0, 0x1F38, 0, // 0DD0
			0, 0x1F39, 0, 0, 0x1FF8, 0, 0, 0x038C, 0, 0, 0x1F48, 0, 0, 0x1F49, 0, 0, // 0DE0
			0, 0, 0x1FEC, 0, 0, 0, 0, 0, 0, 0x1FEA, 0, 0, 0x038E, 0, 0, 0x1FE9, // 0DF0
			0, 0, 0x1FE8, 0, 0, 0x03AB, 0, 0, 0x1F59, 0, 0, 0x2126, 0, 0x1FFA, 0, 0, // 0E00
			0x038F, 0, 0, 0x1F68, 0, 0, 0x1F69, 0, 0, 0x1FFC, 0, 0, 0x1F71, 0, 0x1FB4, 0, // 0E10
			0, 0x1F73, 0, 0x1F75, 0, 0x1FC4, 0, 0, 0x1F77, 0, 0x1FE3, 0, 0x1F70, 0, 0, 0x03AC, // 0E20
			0, 0, 0x1FB1, 0, 0, 0x1FB0, 0, 0, 0x1F00, 0, 0, 0x1F01, 0, 0, 0x1FB6, 0, // 0E30
			0, 0x1FB3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1F72, 0, 0, 0x03AD, // 0E40
			0, 0, 0x1F10, 0, 0, 0x1F11, 0, 0, 0x1F74, 0, 0, 0x03AE, 0, 0, 0x1F20, 0, // 0E50
			0, 0x1F21, 0, 0, 0x1FC6, 0, 0, 0x1FC3, 0, 0, 0, 0, 0x1FBE, 0, 0x1F76, 0, // 0E60
			0, 0x03AF, 0, 0, 0x1FD1, 0, 0, 0x1FD0, 0, 0, 0x03CA, 0, 0, 0x1F30, 0, 0, // 0E70
			0x1F31, 0, 0, 0x1FD6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0E90
			0, 0, 0x1F78, 0, 0, 0x03CC, 0, 0, 0x1F40, 0, 0, 0x1F41, 0, 0, 0, 0, // 0EA0
			0, 0, 0x1FE4, 0, 0, 0x1FE5, 0, 0, 0, 0, 0x1F7A, 0, 0, 0x03CD, 0, 0, // 0EB0
			0x1FE1, 0, 0, 0x1FE0, 0, 0, 0x03CB, 0, 0, 0x1F50, 0, 0, 0x1F51, 0, 0, 0x1FE6, // 0EC0
			0, 0, 0, 0, 0, 0, 0x1F7C, 0, 0, 0x03CE, 0, 0, 0x1F60, 0, 0, 0x1F61, // 0ED0
			0, 0, 0x1FF6, 0, 0, 0x1FF3, 0, 0, 0x1FD2, 0, 0, 0x0390, 0, 0, 0x1FD7, 0, // 0EE0
			0, 0x1FE2, 0, 0, 0x03B0, 0, 0, 0x1FE7, 0, 0, 0x1F79, 0, 0x1F7B, 0, 0x1F7D, 0, // 0EF0
			0x1FF4, 0, 0, 0x03D3, 0, 0, 0x03D4, 0, 0, 0x0407, 0, 0, 0x04D0, 0, 0, 0x04D2, // 0F00
			0, 0, 0x0403, 0, 0, 0x0400, 0, 0, 0x04D6, 0, 0, 0x0401, 0, 0, 0x04C1, 0, // 0F10
			0, 0x04DC, 0, 0, 0x04DE, 0, 0, 0x040D, 0, 0, 0x04E2, 0, 0, 0x0419, 0, 0, // 0F20
			0x04E4, 0, 0, 0x040C, 0, 0, 0x04E6, 0, 0, 0x04EE, 0, 0, 0x040E, 0, 0, 0x04F0, // 0F30
			0, 0, 0x04F2, 0, 0, 0x04F4, 0, 0, 0x04F8, 0, 0, 0x04EC, 0, 0, 0x04D1, 0, // 0F40
			0, 0x04D3, 0, 0, 0x0453, 0, 0, 0x0450, 0, 0, 0x04D7, 0, 0, 0x0451, 0, 0, // 0F50
			0x04C2, 0, 0, 0x04DD, 0, 0, 0x04DF, 0, 0, 0x045D, 0, 0, 0x04E3, 0, 0, 0x0439, // 0F60
			0, 0, 0x04E5, 0, 0, 0x045C, 0, 0, 0, 0, 0x04E7, 0, 0, 0x04EF, 0, 0, // 0F70
			0x045E, 0, 0, 0x04F1, 0, 0, 0x04F3, 0, 0, 0x04F5, 0, 0, 0x04F9, 0, 0, 0x04ED, // 0F80
			0, 0, 0x0457, 0, 0, 0x0476, 0, 0, 0x0477, 0, 0, 0x04DA, 0, 0, 0x04DB, 0, // 0F90
			0, 0x04EA, 0, 0, 0x04EB, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0FA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xFB2E, 0, 0, 0xFB2F, 0, // 0FB0
			0, 0xFB30, 0, 0, 0, 0, 0, 0, 0, 0xFB31, 0, 0, 0xFB4C, 0, 0, 0, // 0FC0
			0, 0xFB32, 0, 0, 0, 0, 0xFB33, 0, 0, 0, 0, 0xFB34, 0, 0, 0xFB4B, 0, // 0FD0
			0, 0xFB35, 0, 0, 0xFB36, 0, 0, 0xFB38, 0, 0, 0xFB1D, 0, 0, 0xFB39, 0, 0, // 0FE0
			0xFB3A, 0, 0, 0, 0, 0xFB3B, 0, 0, 0xFB4D, 0, 0, 0, 0, 0xFB3C, 0, 0, // 0FF0
			0, 0, 0xFB3E, 0, 0, 0xFB40, 0, 0, 0xFB41, 0, 0, 0, 0, 0xFB43, 0, 0, // 1000
			0xFB44, 0, 0, 0xFB4E, 0, 0, 0xFB46, 0, 0, 0xFB47, 0, 0, 0, 0, 0xFB48, 0, // 1010
			0, 0xFB49, 0, 0, 0xFB2A, 0, 0, 0xFB2B, 0, 0, 0, 0, 0xFB4A, 0, 0, 0xFB1F, // 1020
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1030
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1040
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1050
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1060
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1070
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1410
			0, 0, 0, 0, 0, 0, 0x0624, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1420
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1430
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1440
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1450
			0, 0x0626, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1460
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1470
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1480
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1490
			0, 0, 0, 0x06C2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 14A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x06D3, 0, 0, 0, 0, // 14B0
			0x06C0, 0, 0, 0x0958, 0, 0, 0x0959, 0, 0, 0x095A, 0, 0, 0x095B, 0, 0, 0x095C, // 14C0
			0, 0, 0x095D, 0, 0, 0x0929, 0, 0, 0x095E, 0, 0, 0x095F, 0, 0, 0x0931, 0, // 14D0
			0, 0x0934, 0, 0, 0x09DC, 0, 0, 0x09DD, 0, 0, 0x09DF, 0, 0, 0x09CB, 0, 0, // 14E0
			0x09CC, 0, 0, 0x0A59, 0, 0, 0x0A5A, 0, 0, 0x0A5B, 0, 0, 0x0A5E, 0, 0, 0x0A33, // 14F0
			0, 0, 0x0A36, 0, 0, 0x0B5C, 0, 0, 0x0B5D, 0, 0, 0x0B4B, 0, 0, 0x0B48, 0, // 1500
			0, 0x0B4C, 0, 0, 0x0B94, 0, 0, 0x0BCA, 0, 0, 0x0BCC, 0, 0, 0x0BCB, 0, 0, // 1510
			0x0C48, 0, 0, 0x0CC0, 0, 0, 0x0CCA, 0, 0, 0x0CC7, 0, 0, 0x0CC8, 0, 0, 0x0CCB, // 1520
			0, 0, 0x0D4A, 0, 0, 0x0D4C, 0, 0, 0x0D4B, 0, 0, 0x0DDA, 0, 0, 0x0DDC, 0, // 1530
			0, 0x0DDE, 0, 0, 0x0DDD, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1540
			0, 0, 0, 0, 0, 0x0F69, 0, 0, 0x0F43, 0, 0, 0x0F4D, 0, 0, 0x0F52, 0, // 1550
			0, 0x0F57, 0, 0, 0x0F5C, 0, 0, 0x0F73, 0, 0, 0x0F75, 0, 0, 0x0F81, 0, 0, // 1560
			0x0FB9, 0, 0, 0x0F93, 0, 0, 0x0F9D, 0, 0, 0x0FA2, 0, 0, 0x0FA7, 0, 0, 0x0FAC, // 1570
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1670
			0, 0, 0, 0, 0, 0, 0, 0x1B06, 0, 0, 0x1B08, 0, 0, 0x1B0A, 0, 0, // 1680
			0x1B0C, 0, 0, 0x1B0E, 0, 0, 0x1B12, 0, 0, 0x1B3B, 0, 0, 0x1B3D, 0, 0, 0x1B40, // 1690
			0, 0, 0x1B41, 0, 0, 0x1B43, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 16A0
			0, 0, 0, 0, 0, 0, 0, 0, 0x1E38, 0, 0, 0x1E39, 0, 0, 0x1E5C, 0, // 16B0
			0, 0x1E5D, 0, 0, 0x1E68, 0, 0, 0x1E69, 0, 0, 0x1EAC, 0, 0, 0x1EB6, 0, 0, // 16C0
			0x1EAD, 0, 0, 0x1EB7, 0, 0, 0x1EC6, 0, 0, 0x1EC7, 0, 0, 0x1ED8, 0, 0, 0x1ED9, // 16D0
			0, 0, 0x1F02, 0, 0, 0x1F04, 0, 0, 0x1F06, 0, 0, 0x1F80, 0, 0, 0x1F03, 0, // 16E0
			0, 0x1F05, 0, 0, 0x1F07, 0, 0, 0x1F81, 0, 0, 0x1F82, 0, 0, 0x1F83, 0, 0, // 16F0
			0x1F84, 0, 0, 0x1F85, 0, 0, 0x1F86, 0, 0, 0x1F87, 0, 0, 0x1F0A, 0, 0, 0x1F0C, // 1700
			0, 0, 0x1F0E, 0, 0, 0x1F88, 0, 0, 0x1F0B, 0, 0, 0x1F0D, 0, 0, 0x1F0F, 0, // 1710
			0, 0x1F89, 0, 0, 0x1F8A, 0, 0, 0x1F8B, 0, 0, 0x1F8C, 0, 0, 0x1F8D, 0, 0, // 1720
			0x1F8E, 0, 0, 0x1F8F, 0, 0, 0x1F12, 0, 0, 0x1F14, 0, 0, 0x1F13, 0, 0, 0x1F15, // 1730
			0, 0, 0x1F1A, 0, 0, 0x1F1C, 0, 0, 0x1F1B, 0, 0, 0x1F1D, 0, 0, 0x1F22, 0, // 1740
			0, 0x1F24, 0, 0, 0x1F26, 0, 0, 0x1F90, 0, 0, 0x1F23, 0, 0, 0x1F25, 0, 0, // 1750
			0x1F27, 0, 0, 0x1F91, 0, 0, 0x1F92, 0, 0, 0x1F93, 0, 0, 0x1F94, 0, 0, 0x1F95, // 1760
			0, 0, 0x1F96, 0, 0, 0x1F97, 0, 0, 0x1F2A, 0, 0, 0x1F2C, 0, 0, 0x1F2E, 0, // 1770
			0, 0x1F98, 0, 0, 0x1F2B, 0, 0, 0x1F2D, 0, 0, 0x1F2F, 0, 0, 0x1F99, 0, 0, // 1780
			0x1F9A, 0, 0, 0x1F9B, 0, 0, 0x1F9C, 0, 0, 0x1F9D, 0, 0, 0x1F9E, 0, 0, 0x1F9F, // 1790
			0, 0, 0x1F32, 0, 0, 0x1F34, 0, 0, 0x1F36, 0, 0, 0x1F33, 0, 0, 0x1F35, 0, // 17A0
			0, 0x1F37, 0, 0, 0x1F3A, 0, 0, 0x1F3C, 0, 0, 0x1F3E, 0, 0, 0x1F3B, 0, 0, // 17B0
			0x1F3D, 0, 0, 0x1F3F, 0, 0, 0x1F42, 0, 0, 0x1F44, 0, 0, 0x1F43, 0, 0, 0x1F45, // 17C0
			0, 0, 0x1F4A, 0, 0, 0x1F4C, 0, 0, 0x1F4B, 0, 0, 0x1F4D, 0, 0, 0x1F52, 0, // 17D0
			0, 0x1F54, 0, 0, 0x1F56, 0, 0, 0x1F53, 0, 0, 0x1F55, 0, 0, 0x1F57, 0, 0, // 17E0
			0x1F5B, 0, 0, 0x1F5D, 0, 0, 0x1F5F, 0, 0, 0x1F62, 0, 0, 0x1F64, 0, 0, 0x1F66, // 17F0
			0, 0, 0x1FA0, 0, 0, 0x1F63, 0, 0, 0x1F65, 0, 0, 0x1F67, 0, 0, 0x1FA1, 0, // 1800
			0, 0x1FA2, 0, 0, 0x1FA3, 0, 0, 0x1FA4, 0, 0, 0x1FA5, 0, 0, 0x1FA6, 0, 0, // 1810
			0x1FA7, 0, 0, 0x1F6A, 0, 0, 0x1F6C, 0, 0, 0x1F6E, 0, 0, 0x1FA8, 0, 0, 0x1F6B, // 1820
			0, 0, 0x1F6D, 0, 0, 0x1F6F, 0, 0, 0x1FA9, 0, 0, 0x1FAA, 0, 0, 0x1FAB, 0, // 1830
			0, 0x1FAC, 0, 0, 0x1FAD, 0, 0, 0x1FAE, 0, 0, 0x1FAF, 0, 0, 0x1FB2, 0, 0, // 1840
			0x1FC2, 0, 0, 0x1FF2, 0, 0, 0x1FB7, 0, 0, 0x1FCD, 0, 0, 0x1FCE, 0, 0, 0x1FCF, // 1850
			0, 0, 0x1FC7, 0, 0, 0x1FF7, 0, 0, 0x1FDD, 0, 0, 0x1FDE, 0, 0, 0x1FDF, 0, // 1860
			0, 0x2000, 0, 0x2001, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1870
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1880
			0, 0, 0, 0, 0, 0, 0, 0, 0x219A, 0, 0, 0, 0, 0, 0, 0x219B, // 1890
			0, 0, 0, 0, 0x21AE, 0, 0, 0x21CD, 0, 0, 0x21CF, 0, 0, 0x21CE, 0, 0, // 18A0
			0x2204, 0, 0, 0x2209, 0, 0, 0x220C, 0, 0, 0, 0, 0, 0, 0x2224, 0, 0, // 18B0
			0x2226, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 18C0
			0, 0, 0, 0, 0, 0, 0x2241, 0, 0, 0x2244, 0, 0, 0x2247, 0, 0, 0x2249, // 18D0
			0, 0, 0x226D, 0, 0, 0x2262, 0, 0, 0x2270, 0, 0, 0x2271, 0, 0, 0x2274, 0, // 18E0
			0, 0x2275, 0, 0, 0x2278, 0, 0, 0x2279, 0, 0, 0x2280, 0, 0, 0x2281, 0, 0, // 18F0
			0x22E0, 0, 0, 0x22E1, 0, 0, 0x2284, 0, 0, 0x2285, 0, 0, 0x2288, 0, 0, 0x2289, // 1900
			0, 0, 0x22E2, 0, 0, 0x22E3, 0, 0, 0x22AC, 0, 0, 0x22AD, 0, 0, 0x22AE, 0, // 1910
			0, 0x22AF, 0, 0, 0x22EA, 0, 0, 0x22EB, 0, 0, 0x22EC, 0, 0, 0x22ED, 0, 0, // 1920
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2ADC, 0, 0, 0, 0, 0, // 1930
			0, 0, 0, 0x2329, 0, 0x232A, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1940
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1950
			0, 0x3094, 0, 0, 0x304C, 0, 0, 0x304E, 0, 0, 0x3050, 0, 0, 0x3052, 0, 0, // 1960
			0x3054, 0, 0, 0x3056, 0, 0, 0x3058, 0, 0, 0x305A, 0, 0, 0x305C, 0, 0, 0x305E, // 1970
			0, 0, 0x3060, 0, 0, 0x3062, 0, 0, 0x3065, 0, 0, 0x3067, 0, 0, 0x3069, 0, // 1980
			0, 0x3070, 0, 0, 0x3071, 0, 0, 0x3073, 0, 0, 0x3074, 0, 0, 0x3076, 0, 0, // 1990
			0x3077, 0, 0, 0x3079, 0, 0, 0x307A, 0, 0, 0x307C, 0, 0, 0x307D, 0, 0, 0, // 19A0
			0, 0, 0, 0, 0, 0, 0x309E, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 19B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 19C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 19D0
			0, 0x30F4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 19E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 19F0
			0, 0, 0, 0, 0, 0x30AC, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1A00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1A10
			0x30AE, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1A20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1A30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x30B0, 0, 0, 0, // 1A40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1A50
			0, 0, 0, 0, 0, 0, 0x30B2, 0, 0, 0, 0, 0, 0, 0, 0, 0x30B4, // 1A60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x30B6, // 1A70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x30B8, // 1A80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x30BA, 0, 0, 0, 0, 0x30BC, 0, // 1A90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x30BE, 0, 0, 0, 0, // 1AA0
			0x30C0, 0, 0, 0, 0, 0, 0, 0, 0, 0x30C2, 0, 0, 0, 0, 0, 0, // 1AB0
			0x30C5, 0, 0, 0, 0, 0x30C7, 0, 0, 0, 0, 0, 0, 0, 0x30C9, 0, 0, // 1AC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1AD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x30D0, 0, 0, 0x30D1, 0, 0, 0, // 1AE0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1AF0
			0, 0, 0, 0, 0x30D3, 0, 0, 0x30D4, 0, 0, 0, 0, 0, 0, 0, 0, // 1B00
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x30D6, 0, 0, 0x30D7, // 1B10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1B20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0x30D9, 0, 0, 0x30DA, 0, 0, 0, // 1B30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1B40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x30DC, 0, // 1B50
			0, 0x30DD, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1B60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1B70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1B80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1B90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1BA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1BB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1BC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1BD0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1BE0
			0, 0, 0, 0, 0, 0, 0x30F7, 0, 0, 0, 0, 0, 0, 0, 0, 0x30F8, // 1BF0
			0, 0, 0, 0, 0x30F9, 0, 0, 0, 0, 0x30FA, 0, 0, 0, 0, 0, 0, // 1C00
			0, 0, 0x30FE, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1C10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1C20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1C30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1C40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1C50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1C60
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xFAD2, 0, 0xFAD3, // 1C70
			0, 0xFAD4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xF967, // 1C80
			0, 0, 0, 0xFA70, 0, 0, 0, 0, 0, 0xF905, 0, 0, 0, 0xF95E, 0, 0, // 1C90
			0, 0, 0, 0, 0, 0xF91B, 0, 0, 0, 0xF9BA, 0, 0, 0, 0, 0, 0, // 1CA0
			0, 0xF977, 0, 0, 0, 0xF9FD, 0, 0xF9A8, 0, 0, 0, 0, 0, 0xFA73, 0, 0xF92D, // 1CB0
			0, 0xF9B5, 0, 0xFA30, 0, 0xF965, 0, 0xF9D4, 0, 0xF9BB, 0, 0xFA31, 0, 0, 0, 0, // 1CC0
			0, 0xFA0C, 0, 0xFA74, 0, 0xFA32, 0, 0, 0, 0xFA72, 0, 0xF978, 0, 0, 0, 0xF9D1, // 1CD0
			0, 0xFA75, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xFA71, 0, 0xF92E, 0, 0xF979, // 1CE0
			0, 0xF955, 0, 0xF954, 0, 0xFA15, 0, 0, 0, 0, 0, 0, 0, 0xFA00, 0, 0xF99C, // 1CF0
			0, 0xF9DD, 0, 0xF9FF, 0, 0xF9C7, 0, 0xF98A, 0, 0xF99D, 0, 0, 0, 0xFA76, 0, 0xFA33, // 1D00
			0, 0xF952, 0, 0xF92F, 0, 0xFA34, 0, 0xF97F, 0, 0, 0, 0xFA77, 0, 0, 0, 0xF963, // 1D10
			0, 0, 0, 0, 0, 0, 0, 0xF9EB, 0, 0, 0, 0, 0, 0, 0, 0xFA35, // 1D20
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0xF91C, 0, 0, 0, 0, 0, 0xF96B, // 1D30
			0, 0, 0, 0, 0, 0xF906, 0, 0, 0, 0, 0, 0xF9DE, 0, 0xF9ED, 0, 0xF980, // 1D40
			0, 0xF99E, 0, 0, 0, 0xFA79, 0, 0xF90B, 0, 0xFA7A, 0, 0xFA78, 0, 0xFA0D, 0, 0xFA7B, // 1D50
			0, 0xFA37, 0, 0xFA38, 0, 0, 0, 0, 0, 0xF9A9, 0, 0, 0, 0, 0, 0xFA39, // 1D60
			0, 0xFA10, 0, 0xF96C, 0, 0xFA3A, 0, 0xFA7D, 0, 0xF94A, 0, 0xF942, 0, 0, 0, 0, // 1D70
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xFA7E, 0, // 1D80
			0xF90C, 0, 0xF909, 0, 0xFA7F, 0, 0xF981, 0, 0xFA80, 0, 0xFA81, 0, 0, 0, 0, 0, // 1D90
			0, 0, 0xFA04, 0, 0, 0, 0xF95F, 0, 0xF9BC, 0, 0, 0, 0, 0, 0, 0, // 1DA0
			0, 0, 0xF9BD, 0, 0xF94B, 0, 0xFA3B, 0, 0xF9DF, 0, 0xFA3C, 0, 0, 0, 0xF9D5, 0, // 1DB0
			0xF921, 0, 0xF9AB, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1DC0
			0, 0, 0, 0xF98E, 0, 0, 0, 0, 0, 0, 0, 0xFA01, 0, 0xF9A2, 0, 0xF928, // 1DD0
			0, 0xFA82, 0, 0xFA0B, 0, 0xFA83, 0, 0xF982, 0, 0, 0, 0, 0, 0xF943, 0, 0, // 1DE0
			0, 0, 0, 0, 0, 0, 0, 0xFA84, 0, 0, 0, 0xF9D8, 0, 0xF966, 0, 0xFA85, // 1DF0
			0, 0, 0, 0xF9A3, 0, 0xF960, 0, 0xF9AC, 0, 0xFA6B, 0, 0xFA3D, 0, 0xFA86, 0, 0xF9B9, // 1E00
			0, 0xFA88, 0, 0xF9D9, 0, 0xFA87, 0, 0xFA8A, 0, 0xFA3E, 0, 0xFA3F, 0, 0xF98F, 0, 0xFA8B, // 1E10
			0, 0xF90D, 0, 0xF990, 0, 0, 0, 0xF9D2, 0, 0xFA8C, 0, 0, 0, 0, 0, 0xF925, // 1E20
			0, 0xF95B, 0, 0xFA02, 0, 0xF973, 0, 0xF9A4, 0, 0xF975, 0, 0xFA8D, 0, 0xFA8E, 0, 0xFA8F, // 1E30
			0, 0xF991, 0, 0xF930, 0, 0, 0, 0, 0, 0xFA41, 0, 0xFA90, 0, 0xF969, 0, 0, // 1E40
			0, 0, 0, 0xF9BE, 0, 0, 0, 0, 0, 0xF983, 0, 0, 0, 0xFA42, 0, 0, // 1E50
			0, 0, 0, 0, 0xF9E0, 0, 0, 0, 0, 0xFA12, 0, 0xF9C5, 0, 0xFA43, 0, 0xFA06, // 1E60
			0, 0xF98B, 0, 0, 0, 0xF901, 0, 0, 0, 0, 0, 0xF929, 0, 0xFA93, 0, 0, // 1E70
			0, 0xF9E1, 0, 0xFA94, 0, 0xF9C8, 0, 0xF9F4, 0, 0xF9C9, 0, 0xF9DA, 0, 0, 0, 0, // 1E80
			0, 0, 0, 0, 0xF97A, 0, 0xFA44, 0, 0xF9E2, 0, 0xF914, 0, 0xF94C, 0, 0xF931, 0, // 1E90
			0xF91D, 0, 0, 0, 0, 0, 0, 0, 0xF98C, 0, 0xFA95, 0, 0xF9A5, 0, 0, 0, // 1EA0
			0xF970, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1EB0
			0xF972, 0, 0xF968, 0, 0xF9E3, 0, 0, 0, 0xF915, 0, 0xFA05, 0, 0xFA97, 0, 0xF92A, 0, // 1EC0
			0xFA45, 0, 0xF9F5, 0, 0xF94D, 0, 0xF9D6, 0, 0xFA46, 0, 0xF9CB, 0, 0xF9EC, 0, 0xFA99, 0, // 1ED0
			0xF904, 0, 0xFA98, 0, 0xF94E, 0, 0xFA9A, 0, 0xF992, 0, 0xF922, 0, 0xF984, 0, 0xFA9B, 0, // 1EE0
			0, 0, 0xF9FB, 0, 0xF99F, 0, 0xF916, 0, 0xF993, 0, 0xFA9C, 0, 0xF9C0, 0, 0xF9EE, 0, // 1EF0
			0xF932, 0, 0xF91E, 0, 0, 0, 0xFA49, 0, 0xFA9E, 0, 0, 0, 0, 0, 0, 0, // 1F00
			0, 0, 0, 0, 0, 0, 0xF946, 0, 0, 0, 0, 0, 0xFA9F, 0, 0xF9FA, 0, // 1F10
			0xF92B, 0, 0xFA16, 0, 0xF9A7, 0, 0, 0, 0xF961, 0, 0, 0, 0xF9AD, 0, 0xF917, 0, // 1F20
			0xF9E4, 0, 0xF9CC, 0, 0xFA4A, 0, 0xF9AE, 0, 0xFAA1, 0, 0xF994, 0, 0xF9EF, 0, 0, 0, // 1F30
			0, 0, 0xFAA2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1F40
			0xFAA3, 0, 0xF9CD, 0, 0xF976, 0, 0xF962, 0, 0, 0, 0, 0, 0xF9E5, 0, 0xFAA4, 0, // 1F50
			0xFAA5, 0, 0xF9C1, 0, 0xF90E, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xFA17, 0, // 1F60
			0xFAA7, 0, 0, 0, 0xF933, 0, 0, 0, 0xFAA8, 0, 0xF96D, 0, 0xFAAA, 0, 0xFAA9, 0, // 1F70
			0xFA9D, 0, 0, 0, 0, 0, 0, 0, 0xF9CE, 0, 0xF93B, 0, 0xFA4B, 0, 0xF947, 0, // 1F80
			0xFAAB, 0, 0xF964, 0, 0xF985, 0, 0, 0, 0xFA18, 0, 0xFA4C, 0, 0xFA4E, 0, 0xFA4D, 0, // 1F90
			0xFA4F, 0, 0xFA50, 0, 0xFA51, 0, 0xFA19, 0, 0xFA1A, 0, 0xF93C, 0, 0xFA52, 0, 0xFA53, 0, // 1FA0
			0xFA1B, 0, 0xF9B6, 0, 0, 0, 0, 0, 0xF995, 0, 0, 0, 0xF956, 0, 0xFA54, 0, // 1FB0
			0, 0, 0xFA55, 0, 0xFAAC, 0, 0xF9F7, 0, 0, 0, 0xF9F8, 0, 0, 0, 0xFA56, 0, // 1FC0
			0xF9A6, 0, 0xF944, 0, 0, 0, 0xFAAE, 0, 0xF9F9, 0, 0xFA1D, 0, 0xFA03, 0, 0xF97B, 0, // 1FD0
			0, 0, 0xF9CF, 0, 0xF96A, 0, 0xF94F, 0, 0xFAAF, 0, 0xF93D, 0, 0xF957, 0, 0xFAB0, 0, // 1FE0
			0xFA58, 0, 0xF950, 0, 0xFA59, 0, 0, 0, 0xFAB1, 0, 0, 0, 0xFA5A, 0, 0xF9E6, 0, // 1FF0
			0xF90F, 0, 0, 0, 0xF9AF, 0, 0xFA1E, 0, 0xF934, 0, 0xFAB2, 0, 0, 0, 0, 0, // 2000
			0, 0, 0xF9B0, 0, 0xF997, 0, 0xF945, 0, 0, 0, 0, 0, 0xF953, 0, 0xF926, 0, // 2010
			0, 0, 0xF9F6, 0, 0, 0, 0xFA5C, 0, 0, 0, 0, 0, 0, 0, 0xFA6D, 0, // 2020
			0, 0, 0, 0, 0, 0, 0xF97C, 0, 0, 0, 0, 0, 0xFA5D, 0, 0xF974, 0, // 2030
			0xF9FE, 0, 0xFAB3, 0, 0xF93E, 0, 0xFAB4, 0, 0xF958, 0, 0xF918, 0, 0xF96E, 0, 0xFA5F, 0, // 2040
			0xF999, 0, 0xF9C2, 0, 0xF923, 0, 0xF9F0, 0, 0xF935, 0, 0xFA20, 0, 0xF91F, 0, 0xF910, 0, // 2050
			0, 0, 0xF936, 0, 0, 0, 0xFAB5, 0, 0xF911, 0, 0xF927, 0, 0, 0, 0xFA08, 0, // 2060
			0, 0, 0xF9A0, 0, 0xF9E7, 0, 0xF9E8, 0, 0xF912, 0, 0xFA60, 0, 0xFAB6, 0, 0xF924, 0, // 2070
			0, 0, 0xFAB7, 0, 0xFA0A, 0, 0xFA61, 0, 0, 0, 0, 0, 0xF96F, 0, 0xFAB9, 0, // 2080
			0xFABB, 0, 0xF97D, 0, 0xF941, 0, 0xFABE, 0, 0xFA22, 0, 0xFABD, 0, 0xFA62, 0, 0xFABF, 0, // 2090
			0xF9FC, 0, 0xF95A, 0, 0xFAC0, 0, 0, 0, 0, 0, 0xF900, 0, 0, 0, 0, 0, // 20A0
			0, 0, 0, 0, 0xF948, 0, 0, 0, 0xF903, 0, 0xFA64, 0, 0xFA65, 0, 0, 0, // 20B0
			0, 0, 0, 0, 0xF937, 0, 0, 0, 0xF902, 0, 0xF998, 0, 0xF9D7, 0, 0xFAC2, 0, // 20C0
			0xFA07, 0, 0xF98D, 0, 0, 0, 0xF971, 0, 0, 0, 0xFA66, 0, 0xF99A, 0, 0xFA67, 0, // 20D0
			0, 0, 0xFAC3, 0, 0xF9C3, 0, 0xF913, 0, 0, 0, 0xF92C, 0, 0xFA2E, 0, 0xFA26, 0, // 20E0
			0, 0, 0xF919, 0, 0xFAC4, 0, 0xF9B7, 0, 0, 0, 0xF9E9, 0, 0xF97E, 0, 0xF90A, 0, // 20F0
			0xF9B1, 0, 0xFAC5, 0, 0xF93F, 0, 0xF99B, 0, 0, 0, 0, 0, 0xF986, 0, 0, 0, // 2100
			0xF9C6, 0, 0xF951, 0, 0xFA09, 0, 0xF959, 0, 0xF9D3, 0, 0xFAC6, 0, 0xF9DC, 0, 0xF9F1, 0, // 2110
			0, 0, 0xFA2F, 0, 0xF9B8, 0, 0, 0, 0xF9EA, 0, 0xFAC7, 0, 0, 0, 0xF9B2, 0, // 2120
			0xF949, 0, 0xF938, 0, 0xF9B3, 0, 0, 0, 0xFA1C, 0, 0, 0, 0, 0, 0, 0, // 2130
			0, 0, 0xFAC9, 0, 0, 0, 0, 0, 0xFACA, 0, 0, 0, 0, 0, 0xFACB, 0, // 2140
			0xF9B4, 0, 0xFA6A, 0, 0xF9D0, 0, 0, 0, 0, 0, 0, 0, 0xFA2A, 0, 0xFA2B, 0, // 2150
			0xFA2C, 0, 0, 0, 0, 0, 0, 0, 0xF91A, 0, 0xF987, 0, 0, 0, 0, 0, // 2160
			0, 0, 0xFACD, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xF939, 0, // 2170
			0xF9F2, 0, 0, 0, 0xFA2D, 0, 0xF93A, 0, 0xF920, 0, 0, 0, 0xF940, 0, 0xF988, 0, // 2180
			0xF9F3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xF989, 0, 0, 0, 0, 0, // 2190
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xFAD8, 0, 0, 0, 0, 0, // 21A0
		};
		public static byte[] combiningClassArr = new byte[]
		{
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 02F0
			0xE6, 0xE6, 0xE6, 0xE6, 0xE6, 0xE6, 0xE6, 0xE6, 0xE6, 0xE6, 0xE6, 0xE6, 0xE6, 0xE6, 0xE6, 0xE6, // 0300
			0xE6, 0xE6, 0xE6, 0xE6, 0xE6, 0xE8, 0xDC, 0xDC, 0xDC, 0xDC, 0xE8, 0xD8, 0xDC, 0xDC, 0xDC, 0xDC, // 0310
			0xDC, 0xCA, 0xCA, 0xDC, 0xDC, 0xDC, 0xDC, 0xCA, 0xCA, 0xDC, 0xDC, 0xDC, 0xDC, 0xDC, 0xDC, 0xDC, // 0320
			0xDC, 0xDC, 0xDC, 0xDC, 1, 1, 1, 1, 1, 0xDC, 0xDC, 0xDC, 0xDC, 0xE6, 0xE6, 0xE6, // 0330
			0xE6, 0xE6, 0xE6, 0xE6, 0xE6, 0xF0, 0xE6, 0xDC, 0xDC, 0xDC, 0xE6, 0xE6, 0xE6, 0xDC, 0xDC, 0, // 0340
			0xE6, 0xE6, 0xE6, 0xDC, 0xDC, 0xDC, 0xDC, 0xE6, 0xE8, 0xDC, 0xDC, 0xE6, 0xE9, 0xEA, 0xEA, 0xE9, // 0350
			0, 0, 0, 0xE6, 0xE6, 0xE6, 0xE6, 0xE6, 0, 0, 0, 0, 0, 0, 0, 0, // 0480
			0, 0xDC, 0xE6, 0xE6, 0xE6, 0xE6, 0xDC, 0xE6, 0xE6, 0xE6, 0xDE, 0xDC, 0xE6, 0xE6, 0xE6, 0xE6, // 0590
			0xE6, 0xE6, 0xDC, 0xDC, 0xDC, 0xDC, 0xDC, 0xDC, 0xE6, 0xE6, 0xDC, 0xE6, 0xE6, 0xDE, 0xE4, 0xE6, // 05A0
			0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13, 0x13, 0x14, 0x15, 0x16, 0, 0x17, // 05B0
			0, 0x18, 0x19, 0, 0xE6, 0xDC, 0, 0x12, 0, 0, 0, 0, 0, 0, 0, 0, // 05C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 05D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 05E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 05F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0600
			0xE6, 0xE6, 0xE6, 0xE6, 0xE6, 0xE6, 0xE6, 0xE6, 0x1E, 0x1F, 0x20, 0, 0, 0, 0, 0, // 0610
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0620
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0630
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, // 0640
			0x20, 0x21, 0x22, 0xE6, 0xE6, 0xDC, 0xDC, 0xE6, 0xE6, 0xE6, 0xE6, 0xE6, 0xDC, 0xE6, 0xE6, 0xDC, // 0650
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0660
			0x23, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0670
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0680
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0690
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 06A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 06B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 06C0
			0, 0, 0, 0, 0, 0, 0xE6, 0xE6, 0xE6, 0xE6, 0xE6, 0xE6, 0xE6, 0, 0, 0xE6, // 06D0
			0xE6, 0xE6, 0xE6, 0xDC, 0xE6, 0, 0, 0xE6, 0xE6, 0, 0xDC, 0xE6, 0xE6, 0xDC, 0, 0, // 06E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 06F0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0700
			0, 0x24, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0710
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0720
			0xE6, 0xDC, 0xE6, 0xE6, 0xDC, 0xE6, 0xE6, 0xDC, 0xDC, 0xDC, 0xE6, 0xDC, 0xDC, 0xE6, 0xDC, 0xE6, // 0730
			0xE6, 0xE6, 0xDC, 0xE6, 0xDC, 0xE6, 0xDC, 0xE6, 0xDC, 0xE6, 0xE6, 0, 0, 0, 0, 0, // 0740
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 7, 0, 0, 0, // 0930
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 9, 0, 0, // 0940
			0, 0xE6, 0xDC, 0xE6, 0xE6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0950
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 7, 0, 0, 0, // 09B0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 9, 0, 0, // 09C0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 7, 0, 0, 0, // 0A30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 9, 0, 0, // 0A40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 7, 0, 0, 0, // 0AB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 9, 0, 0, // 0AC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 7, 0, 0, 0, // 0B30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 9, 0, 0, // 0B40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 9, 0, 0, // 0BC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 9, 0, 0, // 0C40
			0, 0, 0, 0, 0, 0x54, 0x5B, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0C50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 7, 0, 0, 0, // 0CB0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 9, 0, 0, // 0CC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 9, 0, 0, // 0D40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 9, 0, 0, 0, 0, 0, // 0DC0
			0, 0, 0, 0, 0, 0, 0, 0, 0x67, 0x67, 9, 0, 0, 0, 0, 0, // 0E30
			0, 0, 0, 0, 0, 0, 0, 0, 0x6B, 0x6B, 0x6B, 0x6B, 0, 0, 0, 0, // 0E40
			0, 0, 0, 0, 0, 0, 0, 0, 0x76, 0x76, 0, 0, 0, 0, 0, 0, // 0EB0
			0, 0, 0, 0, 0, 0, 0, 0, 0x7A, 0x7A, 0x7A, 0x7A, 0, 0, 0, 0, // 0EC0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0F00
			0, 0, 0, 0, 0, 0, 0, 0, 0xDC, 0xDC, 0, 0, 0, 0, 0, 0, // 0F10
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0F20
			0, 0, 0, 0, 0, 0xDC, 0, 0xDC, 0, 0xD8, 0, 0, 0, 0, 0, 0, // 0F30
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0F40
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0F50
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0F60
			0, 0x81, 0x82, 0, 0x84, 0, 0, 0, 0, 0, 0x82, 0x82, 0x82, 0x82, 0, 0, // 0F70
			0x82, 0, 0xE6, 0xE6, 9, 0, 0xE6, 0xE6, 0, 0, 0, 0, 0, 0, 0, 0, // 0F80
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0F90
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0FA0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0FB0
			0, 0, 0, 0, 0, 0, 0xDC, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0FC0
			0, 0, 0, 0, 0, 0, 0, 7, 0, 9, 9, 0, 0, 0, 0, 0, // 1030
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xE6, 0xE6, 0xE6, // 1350
			0, 0, 0, 0, 9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1710
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1720
			0, 0, 0, 0, 9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 1730
			0, 0, 9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xE6, 0, 0, // 17D0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0xE4, 0, 0, 0, 0, 0, 0, // 18A0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0xDE, 0xE6, 0xDC, 0, 0, 0, 0, // 1930
			0, 0, 0, 0, 0, 0, 0, 0xE6, 0xDC, 0, 0, 0, 0, 0, 0, 0, // 1A10
			0xE6, 0xE6, 0xDC, 0xE6, 0xE6, 0xE6, 0xE6, 0xE6, 0xE6, 0xE6, 0xDC, 0xE6, 0xE6, 0xEA, 0xD6, 0xDC, // 1DC0
			0xE6, 0xE6, 1, 1, 0xE6, 0xE6, 0xE6, 0xE6, 1, 1, 1, 0xE6, 0xE6, 0, 0, 0, // 20D0
			0, 0xE6, 0, 0, 0, 1, 1, 0xE6, 0xDC, 0xE6, 1, 1, 0xDC, 0xDC, 0xDC, 0xDC, // 20E0
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xDA, 0xE4, 0xE8, 0xDE, 0xE0, 0xE0, // 3020
			0, 0, 0, 0, 0, 0, 0, 0, 0, 8, 8, 0, 0, 0, 0, 0, // 3090
			0, 0, 0, 0, 0, 0, 9, 0, 0, 0, 0, 0, 0, 0, 0, 0, // A800
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1A, 0, // FB10
			0xE6, 0xE6, 0xE6, 0xE6, 0xE6, 0xE6, 0xE6, 0, 0, 0, 0, 0, 0, 0, 0, 0, // FE20
		};
	}
}
