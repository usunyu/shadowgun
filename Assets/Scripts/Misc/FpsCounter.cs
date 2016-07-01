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

public class FpsCounter : MonoBehaviour
{
	static string s_PivotOtherName = "Other";
	static string s_LayoutOtherName = "Other_Layout";
	static string s_FPSName = "FPS";

	GUIBase_Pivot m_PivotOther;
	GUIBase_Layout m_LayoutOther;
	GUIBase_Number m_FPS;
	bool Initialised;

	// Attach this to a GUIText to make a frames/second indicator.
	//
	// It calculates frames/second over each updateInterval,
	// so the display does not keep changing wildly.
	//
	// It is also fairly accurate at very low FPS counts (<10).
	// We do this not by simply counting frames per interval, but
	// by accumulating FPS for each frame. This way we end up with
	// correct overall FPS even if the interval renders something like
	// 5.5 frames.

	public float updateInterval = 0.5F;

	float accum; // FPS accumulated over the interval
	int frames; // Frames drawn over the interval
	float timeleft; // Left time for current interval

	void Start()
	{
//	    timeleft = updateInterval;  
		Initialised = false;

#if DEADZONE_DEDICATEDSERVER
		
			enabled = false;

#endif // DEADZONE_DEDICATEDSERVER
	}

	void GuiInit()
	{
		if (BuildInfo.Version.Stage == BuildInfo.Stage.Release)
			return;

		if (!MFGuiManager.Instance)
			return;

		GUIBase_Platform p = MFGuiManager.Instance.FindPlatform("Gui_16_9");
		if (!p)
			return;

		m_PivotOther = MFGuiManager.Instance.GetPivot(s_PivotOtherName);

		if (!m_PivotOther)
		{
			//Debug.LogError("'" + s_PivotOtherName + "' not found!!! Assert should come now");
			return;
		}

		m_LayoutOther = m_PivotOther.GetLayout(s_LayoutOtherName);

		if (!m_LayoutOther)
		{
			//Debug.LogError("'" + s_LayoutOtherName + "' not found!!! Assert should come now");
			return;
		}

		// DIRTY fix
		if (m_LayoutOther.GetWidget(s_FPSName, false) == null)
			return;

		m_FPS = GuiBaseUtils.PrepareNumber(m_LayoutOther, s_FPSName);
		if (!m_FPS)
			return;

		Show(true);
		Initialised = true;
	}

	void Show(bool show)
	{
		MFGuiManager.Instance.ShowPivot(m_PivotOther, show);
	}

	void SetFPS(int fps)
	{
		if (m_FPS)
		{
			m_FPS.SetNumber(fps, 99);
		}
	}

	void Update()
	{
		if (Initialised == false)
		{
			GuiInit();

			if (Initialised == false)
				return;
		}

		//unccomment for FPS counter

		if (Time.timeScale <= Mathf.Epsilon || Time.deltaTime <= Mathf.Epsilon)
			return;

		timeleft -= Time.deltaTime;
		accum += Time.timeScale/Time.deltaTime;
		++frames;

		// Interval ended - update GUI text and start new interval
		if (timeleft <= 0.0)
		{
			// display two fractional digits (f2 format)
			float fps = accum/frames;

			SetFPS((int)fps);

			timeleft = updateInterval;
			accum = 0.0F;
			frames = 0;
		}
	}

	/**/
}
