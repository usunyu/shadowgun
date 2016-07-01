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

public class GuiScreenLoadingHint : GuiScreen
{
	// CONFIGURATION

	[SerializeField] int[] m_TextIDs = new int[0];
#pragma warning disable 414
	[SerializeField] int[] m_TextIDsPC = new int[0];
#pragma warning restore 414

	// GUIVIEW INTERFACE

	protected override void OnViewInit()
	{
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();
		StartCoroutine(UpdateHint());
	}

	IEnumerator UpdateHint()
	{
		while (IsVisible)
		{
			int textID = 
#if MADFINGER_KEYBOARD_MOUSE
			m_TextIDsPC.Length > 0 ? m_TextIDsPC[Random.Range(0, m_TextIDsPC.Length)] : 
				(m_TextIDs.Length > 0 ? m_TextIDs[Random.Range(0, m_TextIDs.Length)] : 0);
#else
							m_TextIDs.Length > 0 ? m_TextIDs[Random.Range(0, m_TextIDs.Length)] : 0;
#endif

			GUIBase_TextArea hint = GuiBaseUtils.GetControl<GUIBase_TextArea>(Layout, "Hint_Label");
			hint.SetNewText(textID);

			yield return new WaitForSeconds(10);
		}
	}
}
