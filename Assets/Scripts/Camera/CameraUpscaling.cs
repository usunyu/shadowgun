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

public class CameraUpscaling : MonoBehaviour
{
	public bool m_EnableUpscaling = false;
	public int m_ReduceScreenSizeByPercent = 10;

	RenderTexture m_RenderTex;
	//private bool				m_UpscalingEnabled = false;
	GameObject m_GameObj;
	UpscalingAuxCam m_AuxCam;
	int m_MinRTSize = 64;

	void Awake()
	{
		if (GetComponent<Camera>() && m_EnableUpscaling && m_ReduceScreenSizeByPercent > 0)
		{
			m_GameObj = new GameObject("UpscalingAuxGO");
			m_GameObj.AddComponent<Camera>();

			m_AuxCam = m_GameObj.AddComponent<UpscalingAuxCam>() as UpscalingAuxCam;

			m_GameObj.GetComponent<Camera>().clearFlags = CameraClearFlags.Nothing;
			m_GameObj.GetComponent<Camera>().cullingMask = 0;
			m_GameObj.GetComponent<Camera>().depthTextureMode = DepthTextureMode.None;
			m_GameObj.GetComponent<Camera>().nearClipPlane = 0.1f;
			m_GameObj.GetComponent<Camera>().farClipPlane = 100;
			m_GameObj.GetComponent<Camera>().transform.position = new Vector3(9999, 9999, 9999);
			m_GameObj.GetComponent<Camera>().name = "UpscalingAUXCam";

			int screenWidth = Screen.width;
			int screenHeight = Screen.height;

			int dstWidth = (int)(screenWidth*(1.0f - (float)m_ReduceScreenSizeByPercent/100));
			int dstHeight = (int)(screenHeight*(1.0f - (float)m_ReduceScreenSizeByPercent/100));

			if (dstWidth < m_MinRTSize)
			{
				dstWidth = m_MinRTSize;
			}

			if (dstHeight < m_MinRTSize)
			{
				dstHeight = m_MinRTSize;
			}

			//Debug.Log("Using upscaling from " + dstWidth + " x " + dstHeight + " to " + screenWidth + " x " + screenHeight);

			Init(dstWidth, dstHeight);

			GetComponent<Camera>().targetTexture = m_RenderTex;

			//m_UpscalingEnabled = true;
		}
	}

	bool Init(int width, int height)
	{
		m_RenderTex = new RenderTexture(width, height, 16, RenderTextureFormat.RGB565);
		m_RenderTex.name = "DownscaledRT";

		if (!m_RenderTex.Create())
		{
			return false;
		}

		m_AuxCam.m_RenderTex = m_RenderTex;

		return true;
	}
}
