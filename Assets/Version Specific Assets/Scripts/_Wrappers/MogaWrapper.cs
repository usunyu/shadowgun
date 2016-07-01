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


/******************************************************************************/

using UnityEngine;
using System;

/******************************************************************************/

// Constants
public class Moga
{
    public const int ACTION_DOWN = 0;
    public const int ACTION_UP = 1;
    public const int ACTION_FALSE = 0;
    public const int ACTION_TRUE = 1;
    public const int ACTION_DISCONNECTED = 0;
    public const int ACTION_CONNECTED = 1;
    public const int ACTION_CONNECTING = 2;
	public const int ACTION_VERSION_MOGA = 0;
	public const int ACTION_VERSION_MOGAPRO = 1;
	

    public const int KEYCODE_DPAD_UP = 19;
    public const int KEYCODE_DPAD_DOWN = 20;
    public const int KEYCODE_DPAD_LEFT = 21;
    public const int KEYCODE_DPAD_RIGHT = 22;
    public const int KEYCODE_BUTTON_A = 96;
    public const int KEYCODE_BUTTON_B = 97;
    public const int KEYCODE_BUTTON_X = 99;
    public const int KEYCODE_BUTTON_Y = 100;
    public const int KEYCODE_BUTTON_L1 = 102;
    public const int KEYCODE_BUTTON_R1 = 103;
    public const int KEYCODE_BUTTON_L2 = 104;
    public const int KEYCODE_BUTTON_R2 = 105;
	public const int KEYCODE_BUTTON_THUMBL = 106;
	public const int KEYCODE_BUTTON_THUMBR = 107;
    public const int KEYCODE_BUTTON_START = 108;
    public const int KEYCODE_BUTTON_SELECT = 109;

    public const int INFO_KNOWN_DEVICE_COUNT = 1;
    public const int INFO_ACTIVE_DEVICE_COUNT = 2;

    public const int AXIS_X = 0;
    public const int AXIS_Y = 1;
    public const int AXIS_Z = 11;
    public const int AXIS_RZ = 14;
	public const int AXIS_LTRIGGER = 17;
	public const int AXIS_RTRIGGER = 18;	

    public const int STATE_CONNECTION = 1;
    public const int STATE_POWER_LOW = 2;
	public const int STATE_SUPPORTED_PRODUCT_VERSION = 3;	// Controller Version
	public const int STATE_CURRENT_PRODUCT_VERSION = 4;	// Service Controller Version
	
}

#if UNITY_ANDROID

/*
 * Bare-bones wrapper class for "com.bda.controller.Controller".
 * Requires error handling.
 */

public class MogaWrapper
{
    public MogaWrapper(AndroidJavaObject controller)
    {
    }

    public static AndroidJavaObject getInstance(AndroidJavaObject activity)
    {
		return null;
    }

    public bool init()
    {
		return false;
    }

    public void exit()
    {
    }

    public float getAxisValue(int axis)
    {
		return 0.0f;
    }

    public int getKeyCode(int keyCode)
    {
		return -1;
    }

    public int getInfo(int info)
	{
		return -1;
    }

    public int getState(int state)
	{
		return -1;
    }

    public void onPause()
    {
    }

    public void onResume()
    {
    }

    public void setListener(AndroidJavaObject listener, AndroidJavaObject handler)
    {
    }
}
#endif 
