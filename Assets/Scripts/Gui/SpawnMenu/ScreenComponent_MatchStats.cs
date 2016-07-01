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

public class ScreenComponent_MatchStats : ScreenComponent
{
	// =================================================================================================================

	const int TextID_YourTeam = 0502021;
	const int TextID_EnemyTeam = 0502022;
	const int TextID_Winning = 0502023;
	const int TextID_Draw = 0502028;
	const int TextID_Losing = 0502024;

	// =================================================================================================================

	GUIBase_Label m_GoodCourse;
	GUIBase_Label m_GoodSpawns;

	GUIBase_Label m_BadCourse;
	GUIBase_Label m_BadSpawns;

	public override string ParentName
	{
		get { return "MatchStats"; }
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
			if (l.name == "YourCourse_Enum")
				m_GoodCourse = l;
			else if (l.name == "YourSpawns_Enum")
				m_GoodSpawns = l;
			else if (l.name == "EnemyCourse_Enum")
				m_BadCourse = l;
			else if (l.name == "EnemySpawns_Enum")
				m_BadSpawns = l;
		}

		return true;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		PlayerPersistantInfo playerPPI = PPIManager.Instance.GetLocalPlayerPPI();

		if (playerPPI == null)
			return;

		// colect data...

		int blueScore = Client.Instance.GameState.ZCInfo.TeamScore[E_Team.Good];
		int redScore = Client.Instance.GameState.ZCInfo.TeamScore[E_Team.Bad];

		string goodCourse = TextDatabase.instance[playerPPI.Team == E_Team.Good ? TextID_YourTeam : TextID_EnemyTeam] + " - ";
		string badCourse = TextDatabase.instance[playerPPI.Team == E_Team.Bad ? TextID_YourTeam : TextID_EnemyTeam] + " - ";

		if (blueScore > redScore)
		{
			goodCourse += TextDatabase.instance[TextID_Winning];
			badCourse += TextDatabase.instance[TextID_Losing];
		}
		else if (blueScore < redScore)
		{
			goodCourse += TextDatabase.instance[TextID_Losing];
			badCourse += TextDatabase.instance[TextID_Winning];
		}
		else
		{
			goodCourse += TextDatabase.instance[TextID_Draw];
			badCourse += TextDatabase.instance[TextID_Draw];
		}

		// update gui...

		m_GoodCourse.SetNewText(goodCourse);
		m_GoodSpawns.SetNewText(blueScore.ToString("D2"));
		m_BadCourse.SetNewText(badCourse);
		m_BadSpawns.SetNewText(redScore.ToString("D2"));
	}
}
