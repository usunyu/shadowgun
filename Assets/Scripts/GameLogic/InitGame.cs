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

public class InitGame : MonoBehaviour
{
	void Awake()
	{
		m_whiteTexture = new Texture2D(1, 1);
		m_whiteTexture.wrapMode = TextureWrapMode.Repeat;
		m_whiteTexture.SetPixel(0, 0, Color.white);
		m_whiteTexture.Apply();
	}

	// Use this for initialization
	void Start()
	{
#if UNITY_IPHONE
		if(UnityEngine.iOS.Device.generation == UnityEngine.iOS.DeviceGeneration.iPad1Gen || UnityEngine.iOS.Device.generation == UnityEngine.iOS.DeviceGeneration.iPodTouch4Gen || UnityEngine.iOS.Device.generation == UnityEngine.iOS.DeviceGeneration.iPhone3GS)
		{
			EtceteraManager.alertButtonClickedEvent += alertButtonClicked;
			string [] buttons = {"QUIT"}; 
			EtceteraBinding.showAlertWithTitleMessageAndButtons("Error", "Your Device Is Not Supported.", buttons); 
			return;
		}
#endif

		int graphicDetail = PlayerPrefs.GetInt("GraphicDetail", (int)DeviceInfo.GetDetectedPerformanceLevel());

		DeviceInfo.Initialize((DeviceInfo.Performance)graphicDetail);

		GameObject.DontDestroyOnLoad(gameObject);
		int count = 0;
		if (null != BundleSettingsManager.Instance)
			count++;
		if (null != FundSettingsManager.Instance)
			count++;
		if (null != HatSettingsManager.Instance)
			count++;
		if (null != ItemSettingsManager.Instance)
			count++;
		if (null != PerkSettingsManager.Instance)
			count++;
		if (null != SkinSettingsManager.Instance)
			count++;
		if (null != TechSettingsManager.Instance)
			count++;
		if (null != UpgradeSettingsManager.Instance)
			count++;
		if (null != WeaponSettingsManager.Instance)
			count++;
		if (null != TicketSettingsManager.Instance)
			count++;
		if (null != AccountSettingsManager.Instance)
			count++;

		//Debug.Log("Settings: " + count);

		//Debug.Log(graphicDetail + " default " + (int)DeviceInfo.GetDetectedPerformanceLevel());

		StartCoroutine(LoadMainMenu());
	}

	IEnumerator LoadMainMenu()
	{
		m_loader = ApplicationDZ.LoadLevelAsync("MainMenu");
		yield return m_loader;
		Destroy(gameObject);
	}

	void alertButtonClicked(string text)
	{
		Application.Quit();
	}

	void OnGUI()
	{
		if (m_loader != null)
		{
			GUI.color = Color.white;
			GUI.DrawTexture(new Rect(0, Screen.height - Screen.height/80, Screen.width*m_loader.progress, Screen.height), m_whiteTexture);
		}
	}

	AsyncOperation m_loader;
	Texture2D m_whiteTexture;
}
