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

public class FacebookWebplayerIntegration : MonoBehaviour
{
	public float MinAcceptableRatio
	{
		get { return 1.72f; }
		set {}
	}

	public float MaxAcceptableRatio
	{
		get { return 1.82f; }
		set {}
	}

	public float WantedRatio
	{
		get { return 1.777777f; }
		set {}
	}

	static FacebookWebplayerIntegration m_Instance = null;

	public static FacebookWebplayerIntegration Instance
	{
		get
		{
			if (m_Instance == null)
			{
				GameObject go = new GameObject("FacebookWebplayerIntegration");
				m_Instance = go.AddComponent<FacebookWebplayerIntegration>();
				GameObject.DontDestroyOnLoad(m_Instance);
			}
			return m_Instance;
		}
	}

	public void Init()
	{
	}

	public void FixResolution()
	{
	}

	public void ShowScreenShot()
	{
	}

	public void ShowScreenShotDelayed(float time)
	{
	}

	public void HideScreenShot()
	{
	}
}
