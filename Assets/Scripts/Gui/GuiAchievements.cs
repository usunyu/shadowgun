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
using System.Collections.Generic;

public class GuiAchievements : MonoBehaviour
{
	public static GuiAchievements Instance;

	static GUIBase_Pivot m_PivotAchievements;
	static GUIBase_Layout[] m_LayoutAchievement;
	static string[] s_AchievementsName = new string[]
	{
		"ALayout01",
		"ALayout02",
		"ALayout03",
		"ALayout04",
		"ALayout05",
		"ALayout06",
		"ALayout07",
		"ALayout08",
		"ALayout09",
		"ALayout10_Easy",
		"ALayout11_Medium",
		"ALayout12_Hard",
		"ALayout13_Extras",
	};

	int showingID = -1;
	float hideRealTime = 0;

	void Awake()
	{
		Instance = this;
	}

	void OnDestroy()
	{
		StopAllCoroutines();
		CancelInvoke();
		Instance = null;
	}

	public void InitGui()
	{
		InitAchievements();

		//DEBUG
		//StartCoroutine( DebugShowAllAchievements() );
	}

	void InitAchievements()
	{
		m_LayoutAchievement = new GUIBase_Layout[s_AchievementsName.Length];
		m_PivotAchievements = MFGuiManager.Instance.GetPivot("Achievements");
		if (!m_PivotAchievements)
		{
			Debug.LogError("Pivot 'Achievements' not found! ");
			return;
		}

		for (int i = 0; i < m_LayoutAchievement.Length; i++)
			m_LayoutAchievement[i] = GuiBaseUtils.GetLayout(s_AchievementsName[i], m_PivotAchievements);
	}

	void Update()
	{
		if (showingID >= 0)
		{
			if (Time.realtimeSinceStartup >= hideRealTime)
			{
				_ShowAchievement(showingID, false);
				showingID = -1;
				hideRealTime = 0;
			}
		}
	}

	void _ShowAchievement(int index, bool show)
	{
		if (index < 0 || index >= m_LayoutAchievement.Length)
			return;

		GUIBase_Layout l = m_LayoutAchievement[index];

		if (l)
			MFGuiManager.Instance.ShowLayout(l, show);
	}

	public void ShowAchievement(int id, float time = 3.5f)
	{
		showingID = id;
		hideRealTime = Time.realtimeSinceStartup + time;
		_ShowAchievement(showingID, true);
	}

	IEnumerator DebugShowAllAchievements()
	{
		for (int i = 0; i < 13; i++)
		{
			ShowAchievement(i);
			yield return new WaitForSeconds(4);
		}
	}
};
