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

[AddComponentMenu("Optimizations/Disable Based On Performance")]

// TODO: Is this class still required? It seems it is used in a single map only for testing purposes.
public class DisableBasedOnPerformance : MonoBehaviour
{
	public DeviceInfo.Performance UseForDevicePerformance = DeviceInfo.Performance.Medium;

	// Use this for initialization
	void Start()
	{
		switch (UseForDevicePerformance)
		{
		case DeviceInfo.Performance.Medium:
			if (DeviceInfo.PerformanceGrade == DeviceInfo.Performance.Low)
				gameObject.SetActive(false);
			break;

		case DeviceInfo.Performance.High:
			if ((DeviceInfo.PerformanceGrade == DeviceInfo.Performance.Low) ||
				(DeviceInfo.PerformanceGrade == DeviceInfo.Performance.Medium))
				gameObject.SetActive(false);
			break;
		case DeviceInfo.Performance.UltraHigh:
			if ((DeviceInfo.PerformanceGrade == DeviceInfo.Performance.Low) ||
				(DeviceInfo.PerformanceGrade == DeviceInfo.Performance.Medium) ||
				(DeviceInfo.PerformanceGrade == DeviceInfo.Performance.High))
				gameObject.SetActive(false);
			break;
		}
	}
}
