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

public class GuiPopupResearchWait : GuiPopupAnimatedBase
{
	public enum E_AsyncOpStatus
	{
		Waiting,
		Failed,
		Finished
	}

	public delegate E_AsyncOpStatus ActionStatus();

	GUIBase_Label m_StatusLabel;
	GUIBase_Label m_CaptionLabel;
	ActionStatus ActionStatusDlgt = null;

	// ------
	public override void SetCaption(string inCaption)
	{
		m_CaptionLabel.SetNewText(inCaption);
	}

	// ------
	public override void SetText(string inText)
	{
		m_StatusLabel.SetNewText(inText);
	}

	// ------
	public void SetActionStatusDelegate(ActionStatus status)
	{
		ActionStatusDlgt = status;
	}

	// ------
	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_StatusLabel = PrepareLabel(Layout, "Text_Label");
		m_CaptionLabel = PrepareLabel(Layout, "Caption_Label");
	}

	// ------
	protected override void OnViewUpdate()
	{
		if (IsVisible)
		{
			if (ActionStatusDlgt == null)
			{
				CloseDialog();
				SendResult(E_PopupResultCode.Failed);
				return;
			}

			if (ActionStatusDlgt() == E_AsyncOpStatus.Finished)
			{
				CloseDialog();
				SendResult(E_PopupResultCode.Success);
			}
			else if (ActionStatusDlgt() == E_AsyncOpStatus.Failed)
			{
				CloseDialog();
				SendResult(E_PopupResultCode.Failed);
			}
		}

		base.OnViewUpdate();
	}

	// ------
	void CloseDialog()
	{
		ActionStatusDlgt = null;
		Owner.Back();
	}
}
