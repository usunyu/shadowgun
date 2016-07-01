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
using System.Net.NetworkInformation;

public class SysUtils
{
	public static string GetUniqueDeviceID()
	{
#if UNITY_IPHONE

				//
				// On iOS we use MAC address instead of controversial UDID, because Apple started
				// to reject apps which used it.
				//
		
		string address = GetMacAddress();
		if (address == "020000000000") // iOS7+ default MAC address
			return string.Empty;
		return utils.CryptoUtils.CalcSHA1Hash(address);
#else
		return utils.CryptoUtils.CalcSHA1Hash(SystemInfo.deviceUniqueIdentifier);
#endif
	}

	public static string GetMacAddress()
	{
		NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();

		foreach (NetworkInterface adapter in nics)
		{
			PhysicalAddress address = adapter.GetPhysicalAddress();

			if (address.ToString() != "")
			{
				return address.ToString();
			}
		}

		return "";
	}

	// Backward compatibility with Unity 4.x Screen.lockCursor property logic
	public static bool Screen_lockCursor
	{
		get
		{
			return CursorLockMode.None == Cursor.lockState;
		}
		set
		{
			if (value)
			{
				Cursor.visible = false;
				Cursor.lockState = CursorLockMode.Locked;
			}
			else
			{
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}
		}
	}
};
