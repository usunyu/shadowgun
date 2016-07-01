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

public abstract class GuiPopupAnimatedBase : GuiPopup
{
	// PRIVATE MEMBERS

	float m_OriginalPositionY;
	float m_PositionY = 0.0f;

	// GUIVIEW INTERFACE

	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_OriginalPositionY = Layout.transform.position.y;
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		Tweener.TweenFromTo(this,
							"m_PositionY",
							m_OriginalPositionY - Screen.height*0.1f,
							m_OriginalPositionY,
							0.15f,
							Tween.Easing.Sine.EaseOut,
							(tween, finished) =>
							{
								Transform trans = Layout.transform;
								Vector3 pos = trans.position;
								pos.y = m_PositionY;
								trans.position = pos;
							});
	}
}
