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

public class ScreenComponent_NextMatchInfo : ScreenComponent
{
	// =================================================================================================================

	GUIBase_Label m_WaitTime;

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

		m_WaitTime = Parent.transform.GetChildComponent<GUIBase_Label>("NextMatchTime_Enum");

		//	GUIBase_Label [] labels = parent.GetComponentsInChildren<GUIBase_Label>();
		//	
		//	foreach(GUIBase_Label l in labels)
		//	{
		//		if (l.name == "NextMatchTime_Enum")
		//			m_WaitTime = l;
		//	}

		return true;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		// TODO: update time left to the next match
		if (m_WaitTime != null)
		{
			//	float  timeLeft  = 
			//	int    minutes   = Mathf.FloorToInt( timeLeft / 60.0f );
			//	int    seconds   = Mathf.CeilToInt( timeLeft - 60.0f * minutes );
			int minutes = 59;
			int seconds = 59;

			m_WaitTime.SetNewText(minutes.ToString("D2") + ":" + seconds.ToString("D2"));
		}
	}
}
