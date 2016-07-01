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

public class ScreenShotMovie : MonoBehaviour
{
	public string m_FolderName = "ScreenshotFolder";
	public int m_FrameRate = 25;
	public Camera m_CaptureCamera;
	public int m_SuperSize = 0;
	//public float 		m_WaitTime = 5;	

	string m_RealFolder = "";
	bool m_Capture = false;
	Animation[] m_AnimTargets;

	// Use this for initialization
	void Start()
	{
		// Set the playback framerate!
		// (real time doesn't influence time anymore)
		Time.captureFramerate = m_FrameRate;

		// Find a folder that doesn't exist yet by appending numbers!
		m_RealFolder = m_FolderName;
		int count = 1;
		while (System.IO.Directory.Exists(m_RealFolder))
		{
			m_RealFolder = m_FolderName + count;
			count++;
		}
		// Create the folder
		System.IO.Directory.CreateDirectory(m_RealFolder);

		m_AnimTargets = GetComponentsInChildren<Animation>();
		if (m_AnimTargets == null || m_AnimTargets.Length <= 0)
		{
			Debug.LogError("Can't find game objects with animation component for movie capture !!!");
		}
		else
		{
			// remove auto play flag from animation component...
			foreach (Animation a in m_AnimTargets)
			{
				a.playAutomatically = false;
				a.Stop();
			}
		}

		if (m_CaptureCamera == null)
		{
			m_CaptureCamera = GetComponentInChildren<Camera>();
		}

		if (m_CaptureCamera == null)
		{
			Debug.LogWarning("Can't find camera for movie capture !!!");
		}
		else if (m_CaptureCamera.GetComponent<Animation>() == null)
		{
			Debug.LogWarning("movie capture camera dosn't have animation component assigned !!!");
			m_CaptureCamera = null;
		}
		else if (m_CaptureCamera.GetComponent<Animation>().clip == null)
		{
			Debug.LogWarning("movie capture camera animation component doesn't have animClip assigned !!!");
			m_CaptureCamera = null;
		}
		else
		{
			m_CaptureCamera.GetComponent<Animation>().playAutomatically = false;
			m_CaptureCamera.GetComponent<Animation>().Stop();
		}

		//Invoke("StartCapture", m_WaitTime);
	}

	// Update is called once per frame
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			m_Capture = !m_Capture;
			if (m_Capture == true)
			{
				StartCapture();
			}
		}

		if (m_Capture == false)
			return;

		// name is "realFolder/0005 shot.png"
		string name = string.Format("{0}/{1:D04} shot.png", m_RealFolder, Time.frameCount);

		// Capture the screenshot
		Application.CaptureScreenshot(name, m_SuperSize);
	}

	void OnGUI()
	{
		if (m_Capture == true)
			return;

		GUI.Label(new Rect(20, 20, 550, 100), "Press [SPACE] for Start/Stop capture...");
	}

	void StartCapture()
	{
		m_Capture = true;

		// remove auto play flag from animation component...
		foreach (Animation a in m_AnimTargets)
		{
			AnmationPlayTimeModifer mod = a.GetComponent<AnmationPlayTimeModifer>();
			if (mod != null)
			{
				mod.Play();
			}
			else
			{
				a.Play();
			}
		}

		if (m_CaptureCamera != null)
		{
			m_CaptureCamera.GetComponent<Animation>().Play();
		}
	}
}
