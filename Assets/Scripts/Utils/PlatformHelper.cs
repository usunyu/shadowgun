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

public class PlatformHelper
{
	//const string WindowsEditor = "WindowsEditor";
	//const string WindowsPlayer = "WindowsPlayer";

	public static string Serialize(RuntimePlatform platform)
	{
		//serilize using int value for now, hopefully it is reliable enough. If not we will need to
		//come with some names instead.

		/*
		switch( platform )
		{
		case RuntimePlatform.WindowsEditor:
			return WindowsEditor;
			
		case RuntimePlatform.WindowsPlayer:
			return WindowsPlayer;
			
		case RuntimePlatform.WindowsWebPlayer:
			return WindowsWebPlayer;
		}
		
		throw new System.PlatformNotSupportedException( platform.ToString() );
		*/

		int intValue = (int)platform;
		return intValue.ToString();
	}

	public static RuntimePlatform Deserialize(string platformName)
	{
		/*
		switch( platformName )
		{
		case WindowsEditor:
			return RuntimePlatform.WindowsEditor;
			
		case WindowsPlayer:
			return RuntimePlatform.WindowsPlayer;
		}
		
		throw new System.PlatformNotSupportedException( platformName );
		*/

		int intValue = System.Convert.ToInt32(platformName);
		return (RuntimePlatform)intValue;
	}

	public static bool IsDevelop(RuntimePlatform platform)
	{
		switch (platform)
		{
		case RuntimePlatform.OSXEditor:
			return true;

		case RuntimePlatform.WindowsEditor:
			return true;

		default:
			return false;
		}
	}

	public static bool IsMobile(RuntimePlatform platform)
	{
		switch (platform)
		{
		case RuntimePlatform.Android:
			return true;

		case RuntimePlatform.IPhonePlayer:
			return true;
#if !UNITY_4_0
		case RuntimePlatform.WSAPlayerARM:
			return true;
#endif
		default:
			return false;
		}
	}

	public static bool IsOSXPlayerOrOSXEditorRussianOrUkrainian
	{
		get
		{
			if ((Application.platform == RuntimePlatform.OSXPlayer ||
				 Application.platform == RuntimePlatform.OSXEditor) &&
				(Application.systemLanguage == SystemLanguage.Russian ||
				 Application.systemLanguage == SystemLanguage.Ukrainian))
			{
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}
