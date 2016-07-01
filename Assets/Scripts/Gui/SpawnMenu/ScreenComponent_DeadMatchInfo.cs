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

public class ScreenComponent_DeadMatchInfo : ScreenComponent
{
	// =================================================================================================================

	GUIBase_Label m_RemainingTime;

	public override string ParentName
	{
		get { return "MatchInfo"; }
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

		m_RemainingTime = Parent.transform.GetChildComponent<GUIBase_Label>("DeadMatchTime_Enum");

		return true;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ((m_RemainingTime != null) && (Client.Instance != null))
		{
			int timeLeft = Client.Instance.GameState.DMInfo.RestTimeSeconds;
			int minutes = timeLeft/60;
			int seconds = timeLeft - 60*minutes;

			m_RemainingTime.SetNewText(string.Format("{0:00}:{1:00}", minutes, seconds));
		}
	}
}
