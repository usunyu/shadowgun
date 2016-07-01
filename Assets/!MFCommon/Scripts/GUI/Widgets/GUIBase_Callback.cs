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

public abstract class GUIBase_Callback : MonoBehaviour
{
	public enum E_CallbackType
	{
		E_CT_NONE = 0,
		E_CT_INIT = 1,
		E_CT_SHOW = 2,
		E_CT_HIDE = 4,
		E_CT_ON_TOUCH_BEGIN = 8,
		E_CT_ON_TOUCH_UPDATE = 16,
		E_CT_ON_TOUCH_END = 32,
		E_CT_ON_TOUCH_END_OUTSIDE = 64,
		E_CT_ON_TOUCH_END_KEYBOARD = 128,
		E_CT_ON_MOUSEOVER_BEGIN = 256,
		E_CT_ON_MOUSEOVER_END = 512,
		E_CT_COLOR = 1024,
	};

	int m_Flags = 0;

	//---------------------------------------------------------
	public virtual bool Callback(E_CallbackType type, object evt)
	{
		Debug.LogError(GetType().Name + "<" + this.GetFullName('.') + ">.Callback() :: This method should be overriden !!!");

		// if callback handles desired callback type - it should return 'true'
		return false;
	}

	//---------------------------------------------------------
	public virtual void GetTouchAreaScale(out float scaleWidth, out float scaleHeight)
	{
		scaleWidth = 1.0f;
		scaleHeight = 1.0f;
	}

	//---------------------------------------------------------
	public virtual void ChildButtonPressed(float v)
	{
		Debug.LogError(GetType().Name + "<" + this.GetFullName('.') + ">.ChildButtonPressed() :: This method should be overriden !!!");
	}

	//---------------------------------------------------------
	public virtual void ChildButtonReleased()
	{
		Debug.LogError(GetType().Name + "<" + this.GetFullName('.') + ">.ChildButtonReleased() :: This method should be overriden !!!");
	}

	//---------------------------------------------------------
	public void RegisterCallbackType(int clbkTypes)
	{
		m_Flags = m_Flags | clbkTypes;
	}

	//---------------------------------------------------------
	public bool TestFlag(int flagMask)
	{
		return (m_Flags & flagMask) != 0;
	}
}
