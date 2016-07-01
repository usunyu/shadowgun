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

public class GadgetDeathMatchState
{
	readonly static Color BLUE = new Color(12/255.0f, 154/255.0f, 222/255.0f);
	readonly static Color RED = new Color(234/255.0f, 12/255.0f, 31/255.0f);

	// PRIVATE MEMBERS

	GUIBase_Widget m_Root;
	GUIBase_Label m_Timer;
	AudioSource m_Audio;

	// PUBLIC MEMBERS

	public bool IsVisible
	{
		get { return m_Root.Visible; }
		set { m_Root.Show(value, true); }
	}

	// C-TOR

	public GadgetDeathMatchState(GUIBase_Widget root)
	{
		m_Root = root;
		m_Timer = GuiBaseUtils.GetChild<GUIBase_Label>(m_Root, "time");
		m_Audio = root.GetComponent<AudioSource>();
	}

	// PUBLIC METHOD

	public void Update()
	{
		if (m_Timer != null && Client.Instance != null)
		{
			int timeLeft = Client.Instance.GameState.DMInfo.RestTimeSeconds;
			int minutes = timeLeft/60;
			int seconds = timeLeft - 60*minutes;

			m_Timer.SetNewText(string.Format("{0:00}:{1:00}", minutes, seconds));
			m_Timer.Widget.Color = timeLeft <= 5 && seconds%2 == 1 ? RED : BLUE;

			if (timeLeft <= 5)
			{
				if (m_Audio != null && m_Audio.isPlaying == false)
				{
					m_Audio.Play();
				}
			}
		}
	}
}

public class HudComponentDeathMatchState : HudComponent
{
	readonly static string LAYOUT = "HUD_Layout";
	readonly static string ROOT = "DeathMatch_State";

	// PRIVATE MEMBERS

	GadgetDeathMatchState m_Gadget;

	// GUICOMPONENT INTERFACE

	public override float UpdateInterval
	{
		get { return 1.0f; }
	}

	protected override bool OnInit()
	{
		if (base.OnInit() == false)
			return false;

		GUIBase_Layout layout = MFGuiManager.Instance.GetLayout(LAYOUT);

		m_Gadget = new GadgetDeathMatchState(layout.GetWidget(ROOT));

		return true;
	}

	protected override void OnShow()
	{
		base.OnShow();

		m_Gadget.IsVisible = true;
		m_Gadget.Update();
	}

	protected override void OnHide()
	{
		m_Gadget.IsVisible = false;

		base.OnHide();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		m_Gadget.Update();
	}
}
