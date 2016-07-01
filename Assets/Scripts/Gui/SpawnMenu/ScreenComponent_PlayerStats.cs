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

public class ScreenComponent_PlayerStats : ScreenComponent
{
	// =================================================================================================================

	GUIBase_Label m_KillsLabel;
	GUIBase_Label m_DeadsLabel;
	GUIBase_Label m_MoneyLabel;
	GUIBase_Label m_ScoreLabel;

	public override string ParentName
	{
		get { return "YourStats"; }
	}

	public override float UpdateInterval
	{
		get { return 0.5f; }
	}

	// =================================================================================================================

	protected override bool OnInit()
	{
		if (base.OnInit() == false)
			return false;

		GUIBase_Label[] labels = Parent.GetComponentsInChildren<GUIBase_Label>();

		foreach (GUIBase_Label l in labels)
		{
			if (l.name == "Score_Enum")
				m_ScoreLabel = l;
			else if (l.name == "Income_Enum")
				m_MoneyLabel = l;
			else if (l.name == "Kill_Enum")
				m_KillsLabel = l;
			else if (l.name == "Dead_Enum")
				m_DeadsLabel = l;
		}

		return true;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		PlayerPersistantInfo player = PPIManager.Instance.GetLocalPlayerPPI();

		if (player != null)
		{
			if (m_ScoreLabel != null)
				m_ScoreLabel.SetNewText(player.Score.Score.ToString());

			if (m_KillsLabel != null)
				m_KillsLabel.SetNewText(player.Score.Kills.ToString());

			if (m_DeadsLabel != null)
				m_DeadsLabel.SetNewText(player.Score.Deaths.ToString());

			// TODO: earned money
			int money = -1; // player.Score.Money;

			if (m_MoneyLabel != null)
				m_MoneyLabel.SetNewText(money.ToString() + "$");
		}
	}
}
