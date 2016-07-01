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

public static class DeviceInfo
{
	public enum Performance
	{
		Low,
		Medium,
		High,
		UltraHigh,
		Auto
	}

	static string m_UnityQualityProfileName_UltraLow = "Fastest";
	static string m_UnityQualityProfileName_Low = "Fast";
	static string m_UnityQualityProfileName_Medium = "Good";
	static string m_UnityQualityProfileName_High = "Beautiful";
	static string m_UnityQualityProfileName_UltraHigh = "Fantastic";

	// -------
	static Performance m_Performance = Performance.Auto;
	static bool m_Initialized = true;

	// -------
	public static Performance PerformanceGrade
	{
		get { return m_Performance; }
	}

	// -------
	public static void Initialize(Performance suggestion)
	{
		// verify that Unity Quality settings contains all settings which we are using...
		string[] names = QualitySettings.names;

		//Debug.Log("DeviceInfo.Initialize");
		MFDebugUtils.Assert(System.Array.IndexOf(names, m_UnityQualityProfileName_UltraLow) >= 0);
		MFDebugUtils.Assert(System.Array.IndexOf(names, m_UnityQualityProfileName_Low) >= 0);
		MFDebugUtils.Assert(System.Array.IndexOf(names, m_UnityQualityProfileName_Medium) >= 0);
		MFDebugUtils.Assert(System.Array.IndexOf(names, m_UnityQualityProfileName_High) >= 0);
		MFDebugUtils.Assert(System.Array.IndexOf(names, m_UnityQualityProfileName_UltraHigh) >= 0);

		if (suggestion != Performance.Auto)
		{
			m_Performance = suggestion;

			SetPerformanceLevel(m_Performance);
		}
		else
		{
			Check();
		}
	}

	// -------
	static void Check()
	{
		if (!m_Initialized)
			return;
		m_Initialized = false;

		m_Performance = GetDetectedPerformanceLevel();

		UpdatePerformanceSettings();
	}

	public static Performance GetDetectedPerformanceLevel()
	{
#if UNITY_EDITOR

		return DeviceInfo.Performance.High; // high details for EDITOR

#elif UNITY_IPHONE
		if (UnityEngine.iOS.Device.generation == UnityEngine.iOS.DeviceGeneration.iPhone5 || UnityEngine.iOS.Device.generation == UnityEngine.iOS.DeviceGeneration.iPhone5C || UnityEngine.iOS.Device.generation == UnityEngine.iOS.DeviceGeneration.iPad4Gen)
		{
			return DeviceInfo.Performance.UltraHigh;
		}
		else if (UnityEngine.iOS.Device.generation == UnityEngine.iOS.DeviceGeneration.iPad3Gen || 
				UnityEngine.iOS.Device.generation == UnityEngine.iOS.DeviceGeneration.iPhone4S ||
				UnityEngine.iOS.Device.generation == UnityEngine.iOS.DeviceGeneration.iPad2Gen ||
				UnityEngine.iOS.Device.generation == UnityEngine.iOS.DeviceGeneration.iPadMini1Gen ||
				UnityEngine.iOS.Device.generation == UnityEngine.iOS.DeviceGeneration.iPodTouch5Gen	)
		{
			return DeviceInfo.Performance.Medium;
		}
		else if (UnityEngine.iOS.Device.generation == UnityEngine.iOS.DeviceGeneration.iPhone4 || 
				UnityEngine.iOS.Device.generation == UnityEngine.iOS.DeviceGeneration.iPodTouch4Gen ||
				UnityEngine.iOS.Device.generation == UnityEngine.iOS.DeviceGeneration.iPad1Gen)
		{
			return DeviceInfo.Performance.Low;	
		}
		//5S, iPadAir, iPadmini2
		return DeviceInfo.Performance.UltraHigh;		//all unknown devices (probably any new ones)
#elif UNITY_ANDROID

		if ( GraphicsDetailsUtl.IsTegra3() )
		{
			return DeviceInfo.Performance.High;			// high details for TEGRA 3
		}
		else
		{
			return DeviceInfo.Performance.Low;			//TODO: choose proper details based on actual device performance
		}
#elif UNITY_STANDALONE
		if (SystemInfo.graphicsShaderLevel >= 30 && SystemInfo.processorCount >= 2 &&
			SystemInfo.graphicsMemorySize >= 512 && SystemInfo.systemMemorySize >= 4096)
			return DeviceInfo.Performance.UltraHigh;
		else if (SystemInfo.graphicsMemorySize >= 256 && SystemInfo.systemMemorySize >= 2048)
			return DeviceInfo.Performance.High;
		else if (SystemInfo.graphicsMemorySize >= 128 && SystemInfo.systemMemorySize >= 1024)
			return DeviceInfo.Performance.Medium;		
		return DeviceInfo.Performance.Low;
#else
		return DeviceInfo.Performance.Low;
#endif
	}

	static void SetPerformanceLevel(Performance perfLevel)
	{
		//Application.targetFrameRate = 30;
		Screen.sleepTimeout = SleepTimeout.NeverSleep;

		//Debug.Log("Set Quality Settings - " + perfLevel);

		switch (perfLevel)
		{
		case Performance.UltraHigh:

			//RenderSettings.fog = true;

			GraphicsDetailsUtl.SetShaderQuality(GraphicsDetailsUtl.Quality.High);

			QualitySettings.SetQualityLevel(System.Array.IndexOf(QualitySettings.names, m_UnityQualityProfileName_UltraHigh), true);

			QualitySettings.masterTextureLimit = 0;

			break;

		case Performance.High:

			//RenderSettings.fog = true;

			GraphicsDetailsUtl.SetShaderQuality(GraphicsDetailsUtl.Quality.High);

			QualitySettings.SetQualityLevel(System.Array.IndexOf(QualitySettings.names, m_UnityQualityProfileName_High), true);

			QualitySettings.masterTextureLimit = 0;
			break;

		case Performance.Medium:

			//RenderSettings.fog = false;

			GraphicsDetailsUtl.SetShaderQuality(GraphicsDetailsUtl.Quality.Medium);

			QualitySettings.SetQualityLevel(System.Array.IndexOf(QualitySettings.names, m_UnityQualityProfileName_Medium), true);

			QualitySettings.masterTextureLimit = 0;
			break;

		default: // Performance.Low:

			//RenderSettings.fog = false;

			GraphicsDetailsUtl.SetShaderQuality(GraphicsDetailsUtl.Quality.Low);

			QualitySettings.SetQualityLevel(System.Array.IndexOf(QualitySettings.names, m_UnityQualityProfileName_Low), true);

			QualitySettings.masterTextureLimit = 1;

			break;
		}
	}

	public static void UpdatePerformanceSettings()
	{
		SetPerformanceLevel(DeviceInfo.PerformanceGrade);
	}
}
