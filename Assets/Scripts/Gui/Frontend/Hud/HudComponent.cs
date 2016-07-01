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

public abstract class HudComponent : GuiComponent<GuiHUD>
{
	public ComponentPlayerLocal LocalPlayer
	{
		get { return Owner.LocalPlayer; }
	}

	public bool SetTextAndAdjustBackground(string text, GUIBase_Label label, GUIBase_Widget background, float BaseSize, float TextScale)
	{
		float CorrectionPerChar = 35*(1 - TextScale); //15f;
		bool modif = false;

		if (label.GetText() != text)
		{
			label.SetNewText(text);
			modif = true;
		}
		int len = text.Length;
		float textSize = label.textSize.x - CorrectionPerChar*(float)len;
		float xScale = 0.2f + textSize/BaseSize;

		if ((Mathf.Abs(background.transform.localScale.x - xScale) > 0.01f) || (Mathf.Abs(label.transform.localScale.y - TextScale) > 0.01f))
		{
			background.transform.localScale = new Vector3(xScale, 1.0f, 1.0f);
			label.transform.localScale = new Vector3(TextScale/xScale, TextScale, TextScale);
			modif = true;
		}

		if (!background.IsVisible() || !label.Widget.IsVisible())
		{
			background.Show(true, true);
			label.Widget.Show(true, true);
		}

		return modif;
	}
}
