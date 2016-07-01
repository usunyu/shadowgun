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

public static class MFString
{
	public static string MFNormalize(this string str, System.Text.NormalizationForm form = System.Text.NormalizationForm.FormC)
	{
		switch (form)
		{
		default:
			return Madfinger.Mono.Globalization.Unicode.Normalization.Normalize(str, 0);
		case System.Text.NormalizationForm.FormD:
			return Madfinger.Mono.Globalization.Unicode.Normalization.Normalize(str, 1);
		case System.Text.NormalizationForm.FormKC:
			return Madfinger.Mono.Globalization.Unicode.Normalization.Normalize(str, 2);
		case System.Text.NormalizationForm.FormKD:
			return Madfinger.Mono.Globalization.Unicode.Normalization.Normalize(str, 3);
		}
	}

	public static bool MFIsNormalized(this string str, System.Text.NormalizationForm form = System.Text.NormalizationForm.FormC)
	{
		switch (form)
		{
		default:
			return Madfinger.Mono.Globalization.Unicode.Normalization.IsNormalized(str, 0);
		case System.Text.NormalizationForm.FormD:
			return Madfinger.Mono.Globalization.Unicode.Normalization.IsNormalized(str, 1);
		case System.Text.NormalizationForm.FormKC:
			return Madfinger.Mono.Globalization.Unicode.Normalization.IsNormalized(str, 2);
		case System.Text.NormalizationForm.FormKD:
			return Madfinger.Mono.Globalization.Unicode.Normalization.IsNormalized(str, 3);
		}
	}
}
