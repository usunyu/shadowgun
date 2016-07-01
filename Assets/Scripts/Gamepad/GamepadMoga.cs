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
using System;

//-------------------------------------------------------
// Gamepad Singleton
//-------------------------------------------------------
public class MogaGamepad
{
#if UNITY_ANDROID
	static MogaWrapper m_MogaController = null;
	static bool IsMogaConnected = false;
#endif
	public delegate void ConnectionDelegate(bool connect);
	public static ConnectionDelegate OnConnectionChange;

#if UNITY_ANDROID
	public delegate void BatteryDelegate(bool low);
	public static BatteryDelegate OnBatteryLowChange;
	static bool BatteryLow = false;
	static float LastTimeBatteryCheck = 0;
	const float remindBatteryTime = 90;
#endif

	public static void Init()
	{
#if UNITY_ANDROID

		Debug.Log("Moga Init");
		AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
		if (jc == null)
		{
			Debug.Log("unity player not found");
			return;
		}

		AndroidJavaObject activity = jc.GetStatic<AndroidJavaObject>("currentActivity");
		if (activity == null)
		{
			Debug.Log("current activity not found");
			return;
		}

		AndroidJavaObject controller = MogaWrapper.getInstance(activity);
		if (controller == null)
		{
			Debug.Log("moga controller not found");
			return;
		}

		m_MogaController = new MogaWrapper(controller);
		m_MogaController.init();
#endif
	}

	public static void Done()
	{
#if UNITY_ANDROID
		Debug.Log("Moga Done");
		m_MogaController.exit();
		m_MogaController = null;
#endif
	}

	public static bool IsConnected()
	{
#if UNITY_ANDROID
		return (m_MogaController != null && IsMogaConnected);
#else
		return false;
#endif
	}

	public static bool IsMogaPro()
	{
#if UNITY_ANDROID
		return (m_MogaController != null && (m_MogaController.getState(Moga.STATE_CURRENT_PRODUCT_VERSION) == Moga.ACTION_VERSION_MOGAPRO));
#else
		return false;
#endif
	}

	public static bool MenuKeyPressed()
	{
#if UNITY_ANDROID
		return (GetKeyCode(Moga.KEYCODE_BUTTON_A) == Moga.ACTION_DOWN || GetKeyCode(Moga.KEYCODE_BUTTON_B) == Moga.ACTION_DOWN
				|| GetKeyCode(Moga.KEYCODE_BUTTON_L1) == Moga.ACTION_DOWN || GetKeyCode(Moga.KEYCODE_BUTTON_R1) == Moga.ACTION_DOWN);
#else
		return false;
#endif
	}

	public static bool MenuMovePressed()
	{
#if UNITY_ANDROID
		return (Mathf.Abs(GetAxis(Moga.AXIS_X)) > 0.4f || Mathf.Abs(GetAxis(Moga.AXIS_Y)) > 0.4f);
#else
		return false;
#endif
	}

	public static void OnFocus(bool focused)
	{
#if UNITY_ANDROID
		//Debug.Log("Moga OnFocus: " + focused + " intance: " + m_MogaController != null);
		if (m_MogaController != null)
		{
			if (focused)
				m_MogaController.onResume();
			else
				m_MogaController.onPause();
		}
#endif
	}

	public static float GetAxis(int axis)
	{
#if UNITY_ANDROID
		return m_MogaController.getAxisValue(axis);
#else
		return 0;
#endif
	}

	public static int GetKeyCode(int keyCode)
	{
#if UNITY_ANDROID
		return m_MogaController.getKeyCode(keyCode);
#else
		return 0;
#endif
	}

	public static void Update()
	{
#if UNITY_ANDROID
		if (m_MogaController != null)
		{
			//connection status
			int connection = m_MogaController.getState(Moga.STATE_CONNECTION);
			bool newMogaCon = (connection == Moga.ACTION_CONNECTED);
			if (newMogaCon != IsMogaConnected)
			{
				if (OnConnectionChange != null)
					OnConnectionChange(newMogaCon);

				IsMogaConnected = newMogaCon;
				Debug.Log("Moga controller " + (newMogaCon ? "connected" : "disconected"));
			}

			//battery status
			bool batteryLow = m_MogaController.getState(Moga.STATE_POWER_LOW) == Moga.ACTION_TRUE;
			if (batteryLow != BatteryLow || (batteryLow && (Time.timeSinceLevelLoad > LastTimeBatteryCheck + remindBatteryTime)))
			{
				if (OnBatteryLowChange != null)
					OnBatteryLowChange(batteryLow);

				BatteryLow = batteryLow;
				LastTimeBatteryCheck = Time.timeSinceLevelLoad;
				Debug.Log("Moga gamepad battery is low");
			}
		}
#endif
	}
}
