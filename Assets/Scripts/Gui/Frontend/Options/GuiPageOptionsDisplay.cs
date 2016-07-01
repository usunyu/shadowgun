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

[AddComponentMenu("GUI/Frontend/OptionPages/GuiPageOptionsDisplay")]
public class GuiPageOptionsDisplay : GuiScreen
{
	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	public interface IGuiPageOptionsDisplay
	{
		void OnInit(GUIBase_Layout layout);
		void OnShow();
		void OnHide();
		void OnReset();
		void OnUpdate();
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	IGuiPageOptionsDisplay m_GuiPageOptionsDisplay;

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------
	// GuiView methods
	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	protected override void OnViewInit()
	{
		base.OnViewInit();

#if UNITY_STANDALONE || UNITY_EDITOR
		m_ScreenLayout = GetLayout("MainOpt", "00Display_Layout_PC");

		if (m_GuiPageOptionsDisplay == null)
			m_GuiPageOptionsDisplay = new GuiPageOptionsDisplayPC();
#else
		if (m_GuiPageOptionsDisplay == null)
			m_GuiPageOptionsDisplay = new GuiPageOptionsDisplayMobile();
#endif

		m_GuiPageOptionsDisplay.OnInit(Layout);
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	protected override void OnViewShow()
	{
		base.OnViewShow();

		m_GuiPageOptionsDisplay.OnShow();
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	protected override void OnViewHide()
	{
		m_GuiPageOptionsDisplay.OnHide();

		base.OnViewHide();
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	protected override void OnViewReset()
	{
		base.OnViewReset();

		m_GuiPageOptionsDisplay.OnReset();
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	protected override void OnViewUpdate()
	{
		base.OnViewUpdate();

		m_GuiPageOptionsDisplay.OnUpdate();
	}
}
