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

public abstract class UserGuideActionWithPopup<T> : UserGuideAction
				where T : GuiPopup
{
	// PROTECTED MEMBERS

	protected T Popup { get; private set; }

	// ABSTRACT INTERFACE

	protected virtual void OnPopupHides(E_PopupResultCode result)
	{
	}

	// PROTECTED METHODS

	protected T ShowPopup()
	{
		return ShowPopup(null, null);
	}

	protected T ShowPopup(string popupName)
	{
		return ShowPopup(popupName, null, null);
	}

	protected T ShowPopup(string caption, string text)
	{
		string popupName = GuiMenu.GetScreenName(typeof (T).Name);
		return ShowPopup(popupName, caption, text);
	}

	protected T ShowPopup(string popupName, string caption, string text)
	{
		if (Popup != null)
			return Popup;
		if (GuideData == null)
			return null;

		Popup = (T)GuideData.Menu.ShowPopup(popupName,
											caption,
											text,
											(inPopup, inResult) =>
											{
												OnPopupHides(inResult);

												Terminate();
											});

		return Popup;
	}

	protected void HidePopup()
	{
		if (Popup != null)
		{
			Popup.ForceClose();
		}
		Popup = null;
	}

	// USERGUIDEACTION INTERFACE

	protected override bool OnUpdate()
	{
		if (base.OnUpdate() == false)
			return false;

		if (Popup != null && Popup.IsVisible == false)
		{
			OnPopupHides(E_PopupResultCode.Failed);

			Terminate();
		}

		return true;
	}

	protected override void OnTerminate()
	{
		HidePopup();

		base.OnTerminate();
	}
}
