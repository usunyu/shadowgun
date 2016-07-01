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

public enum E_PopupResultCode
{
	Ok,
	Cancel,
	Success,
	Failed
}
public delegate void PopupHandler(GuiPopup inPopup, E_PopupResultCode inResult);

public abstract class GuiPopup : GuiScreen
{
	// PRIVATE MEMBERS

	PopupHandler m_ResultHandler;
	Tween.Tweener m_Tweener = new Tween.Tweener();

	// PROTECTED MEMBERS

	protected Tween.Tweener Tweener
	{
		get { return m_Tweener; }
	}

	// ABSTRACT INTERFACE

	public virtual bool CanCloseByEscape
	{
		get
		{
			if (Layout == null)
				return false;

			string[] names = {"Close_Button", "Cancel_Button", "Back_Button"};
			GUIBase_Button button = null;
			foreach (var name in names)
			{
				button = GuiBaseUtils.GetControl<GUIBase_Button>(Layout, name, false);
				if (button != null)
					break;
			}

			return button == null ? false : button.Widget.Visible && !button.IsDisabled;
		}
	}

	public abstract void SetCaption(string inCaption);
	public abstract void SetText(string inText);

	// PUBLIC METHODS

	public void SetHandler(PopupHandler inHandler)
	{
		m_ResultHandler = inHandler;
	}

	public void ForceClose()
	{
		if (IsVisible == true)
		{
			Owner.Back();
			SendResult(E_PopupResultCode.Failed);
		}
	}

	// GUIVIEW INTERFACE

	protected override void OnViewShow()
	{
		base.OnViewShow();

		m_Tweener.TweenFromTo(Layout, "m_FadeAlpha", 0.0f, 1.0f, 0.15f, Tween.Easing.Sine.EaseIn, (tween, finished) => { Layout.SetModify(true); });

		if (Layout.GetComponent<AudioSource>() != null)
		{
			Layout.GetComponent<AudioSource>().Play();
		}
	}

	protected override void OnViewHide()
	{
		m_Tweener.StopTweens(true);

		base.OnViewHide();
	}

	protected override void OnViewUpdate()
	{
		base.OnViewUpdate();

		m_Tweener.UpdateTweens();
	}

	protected override bool OnViewProcessInput(ref IInputEvent evt)
	{
		if (evt.Kind == E_EventKind.Key)
		{
			KeyEvent key = (KeyEvent)evt;
			if (key.Code == KeyCode.Escape)
			{
				if (key.State == E_KeyState.Released && CanCloseByEscape == true)
				{
					ForceClose();
				}
				return true;
			}
		}

		return base.OnViewProcessInput(ref evt);
	}

	// PROTECTED METHODS

	protected void SendResult(E_PopupResultCode inResult)
	{
		if (m_ResultHandler != null)
		{
			m_ResultHandler(this, inResult);
		}
	}
}
