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

public class GuiPopupSocNetsDialog : GuiPopup
{
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public enum E_Event
	{
		PressedFB,
		PressedTW,
		Closed
	};

	public delegate void EventListener(GuiPopupSocNetsDialog inDialog, E_Event inEvent);

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	GUIBase_Label m_Caption;
	GUIBase_Label m_Text;

	GUIBase_Button m_ButtonFB;
	GUIBase_Button m_ButtonTW;
	GUIBase_Button m_ButtonClose;

	EventListener m_Listener;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	//-----------------------------------------------------------------------------------------------------------------
	public override void SetCaption(string inCaption)
	{
		m_Caption.SetNewText(inCaption);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public override void SetText(string inText)
	{
		m_Text.SetNewText(inText);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public void EnableFacebookButton(bool inEnable)
	{
		m_ButtonFB.SetDisabled(!inEnable);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public void EnableTwitterButton(bool inEnable)
	{
		m_ButtonTW.SetDisabled(!inEnable);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public void EnableCloseButton(bool inEnable)
	{
		m_ButtonClose.SetDisabled(!inEnable);
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	//-----------------------------------------------------------------------------------------------------------------
	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_Caption = PrepareLabel(m_ScreenLayout, "Caption_Label");
		m_Text = PrepareLabel(m_ScreenLayout, "Text_Label");
		m_ButtonFB = PrepareButton(m_ScreenLayout, "Facebook_Button", null, OnFacebookButton);
		m_ButtonTW = PrepareButton(m_ScreenLayout, "Twitter_Button", null, OnTwitterButton);
		m_ButtonClose = PrepareButton(m_ScreenLayout, "Close_Button", null, OnCloseButton);

		SetHandler(this.InternalPopupHandler);
	}

	//-----------------------------------------------------------------------------------------------------------------
	void OnCloseButton(GUIBase_Widget inWidget)
	{
		Owner.Back();
		SendEvent(E_Event.Closed);
	}

	//-----------------------------------------------------------------------------------------------------------------
	void OnFacebookButton(GUIBase_Widget inWidget)
	{
		SendEvent(E_Event.PressedFB);
	}

	//-----------------------------------------------------------------------------------------------------------------
	void OnTwitterButton(GUIBase_Widget inWidget)
	{
		SendEvent(E_Event.PressedTW);
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	//-----------------------------------------------------------------------------------------------------------------
	public void SetHandler(EventListener inListener)
	{
		m_Listener = inListener;
	}

	//-----------------------------------------------------------------------------------------------------------------
	new void SetHandler(PopupHandler inHandler)
	{
		base.SetHandler(inHandler);
	}

	//-----------------------------------------------------------------------------------------------------------------
	void InternalPopupHandler(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		switch (inResult)
		{
		// forcefully closed
		case E_PopupResultCode.Failed:
			SendEvent(E_Event.Closed);
			return;
		// unhandled cases
		default:
			break;
		}

		Debug.Log("Unhandled popup result: " + inResult);
	}

	//-----------------------------------------------------------------------------------------------------------------
	void SendEvent(E_Event inEvent)
	{
		if (m_Listener != null)
		{
			m_Listener(this, inEvent);
		}
	}
}
