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

[AddComponentMenu("GUI/Widgets/Popup")]
public class GUIBase_PopUp : GUIBase_Callback
{
	public GUIBase_Button[] m_PopUpButtons = new GUIBase_Button[1];

	public delegate void PopUpDelegate(int i);

	GUIBase_Widget m_Widget;
	PopUpDelegate m_PopUpDelegate;

	bool m_PopUpButtonsVisible = false;

	//---------------------------------------------------------
	public void Start()
	{
		m_Widget = GetComponent<GUIBase_Widget>();

		m_Widget.RegisterUpdateDelegate(UpdatePopUp);
	}

	//---------------------------------------------------------
	public void RegisterPopUpDelegate(PopUpDelegate d)
	{
		m_PopUpDelegate = d;
	}

	//---------------------------------------------------------
	public override void ChildButtonPressed(float v)
	{
		ShowPopUpButtons(true);
	}

	//---------------------------------------------------------
	public override void ChildButtonReleased()
	{
		if (m_PopUpDelegate != null)
		{
			Vector2 eventPos = new Vector2();

			if (Input.touchCount != 0)
			{
				Touch t = Input.touches[0];

				eventPos.x = t.position.x;
				eventPos.y = t.position.y;
			}
			else
			{
				eventPos.x = Input.mousePosition.x;
				eventPos.y = Input.mousePosition.y;
			}

			eventPos.y = Screen.height - eventPos.y;

			// Find out if touch was released over one of popup buttons...
			for (int i = 0; i < m_PopUpButtons.Length; ++i)
			{
				if (m_PopUpButtons[i])
				{
					GUIBase_Widget widget = m_PopUpButtons[i].Widget;

					if (widget)
					{
						if (widget.IsMouseOver(eventPos))
						{
							// Call user's delegate
							m_PopUpDelegate(i);
							return;
						}
					}
				}
			}
		}
	}

	//---------------------------------------------------------
	void ShowPopUpButtons(bool v)
	{
		m_PopUpButtonsVisible = v;

		// show all popup buttons
		for (int i = 0; i < m_PopUpButtons.Length; ++i)
		{
			if (m_PopUpButtons[i])
			{
				GUIBase_Widget widget = m_PopUpButtons[i].Widget;

				if (widget)
				{
					widget.Show(v, true);
				}
			}
		}
	}

	//---------------------------------------------------------
	void UpdatePopUp()
	{
		if (m_PopUpButtonsVisible)
		{
			bool touch = false;

			if (Input.touchCount != 0)
			{
				touch = true;
			}
			else if (Input.GetMouseButton(0))
			{
				touch = true;
			}

			if (!touch)
			{
				ShowPopUpButtons(false);
			}
		}
	}
}
