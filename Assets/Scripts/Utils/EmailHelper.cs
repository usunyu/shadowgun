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

public class EmailHelper
{
	const string SUPPORT_EMAIL = "deadzone@madfingergames.com";

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	// return false if email client has not been opened
	// email client is not configured
	public static bool ShowSupportEmailComposerForIOSOrAndroid()
	{
		// informations
		string userName = CloudUser.instance.userName_TODO != null ? CloudUser.instance.userName_TODO : "";
		string appVersion = BuildInfo.Version.ToString();
		string deviceName = SystemInfo.deviceModel;
		string osWithVersion = SystemInfo.operatingSystem;
		string systemLanguage = Application.systemLanguage.ToString();

		string address = SUPPORT_EMAIL;
		string subject = TextDatabase.instance[0104110];

		string[] bodyLines =
		{
			"\n",
			"========================",
			"Username: " + userName,
			"App Version: " + appVersion,
			"Device: " + deviceName,
			"OS: " + osWithVersion,
			"System language: " + systemLanguage,
			"=========================",
			"\n"
		};

		string body = string.Join("\n", bodyLines);

		// try to open an email client
		bool opened = EtceteraWrapper.ShowEmailComposer(address, subject, body);

		return opened;
	}
}
