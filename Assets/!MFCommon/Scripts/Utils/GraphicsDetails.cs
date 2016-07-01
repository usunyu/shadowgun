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

public class GraphicsDetailsUtl
{
	public enum Quality
	{
		Low,
		Medium,
		High
	};

	public static bool IsTegra3()
	{
//		Debug.Log(SystemInfo.graphicsDeviceName);
//		Debug.Log(SystemInfo.graphicsDeviceVendor);
//		Debug.Log(SystemInfo.processorCount);

		string graphicsDeviceName = SystemInfo.graphicsDeviceName;
		string graphicsDeviceVendor = SystemInfo.graphicsDeviceVendor;

//		graphicsDeviceName 		= "NVIDIA Tegra 3";
//		graphicsDeviceVendor	= "NVIDIA Corporation";

		//
		// Lame attempt to detect tegra 3 devices
		//

		if (SystemInfo.processorCount >= 4)
		{
			string vendor = graphicsDeviceVendor.ToUpper();

			if (vendor.IndexOf("NVIDIA") != -1)
			{
				string deviceName = graphicsDeviceName.ToUpper();

				if (deviceName.IndexOf("TEGRA 3") != -1)
				{
					return true;
				}
			}
		}

		return false;
	}

	public static void AutoSetupShaderQuality()
	{
		//	Debug.Log("IsTegra3 : " + IsTegra3());

#if UNITY_IPHONE		
		if (UnityEngine.iOS.Device.generation == UnityEngine.iOS.DeviceGeneration.iPad3Gen || UnityEngine.iOS.Device.generation == UnityEngine.iOS.DeviceGeneration.iPhone4S)
		{
			SetShaderQuality(Quality.High);
		}
		else if(UnityEngine.iOS.Device.generation == UnityEngine.iOS.DeviceGeneration.iPad2Gen)
		{
			SetShaderQuality(Quality.Medium);
		}
		else 
		{
			SetShaderQuality(Quality.Low);
		}
#else
		//
		// TODO: Add GPU performance autodetection for Android
		//

		if (IsTegra3())
		{
			SetShaderQuality(Quality.High);
		}
		else
		{
			SetShaderQuality(Quality.Low);
		}

#endif
	}

	public static void SetShaderQuality(Quality quality)
	{
		DisableShaderKeyword("UNITY_SHADER_DETAIL_LOW");
		DisableShaderKeyword("UNITY_SHADER_DETAIL_MEDIUM");
		DisableShaderKeyword("UNITY_SHADER_DETAIL_HIGH");

		switch (quality)
		{
		case Quality.Low:
			EnableShaderKeyword("UNITY_SHADER_DETAIL_LOW");
			break;

		case Quality.Medium:
			EnableShaderKeyword("UNITY_SHADER_DETAIL_MEDIUM");
			break;

		case Quality.High:
			EnableShaderKeyword("UNITY_SHADER_DETAIL_HIGH");
			break;
		}
	}

	public static void EnableShaderKeyword(string keyword)
	{
		//	Debug.Log("EnableShaderKeyword: " + keyword);
		Shader.EnableKeyword(keyword);
	}

	public static void DisableShaderKeyword(string keyword)
	{
		//	Debug.Log("DisableShaderKeyword: " + keyword);
		Shader.DisableKeyword(keyword);
	}
}
