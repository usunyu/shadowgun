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

public class GuiPageOptionsDisplayMobile : GuiPageOptionsDisplay.IGuiPageOptionsDisplay
{
	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	readonly static string GRAPHDETAILS_ENUM = "GraphDetails_Enum";
	readonly static string DETECTGRAPHIC_BUTTON = "DetectGraphic_Button";
	readonly static string SHOWHINTS_SWITCH = "ShowHints_Switch";
	readonly static string HINT_LABEL = "Hint_Label";
	readonly static string LANGUAGE_ENUM = "Language_Enum";

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	GUIBase_Enum m_GraphicEnum;
	GUIBase_Button m_AutoDetectButton;
	GUIBase_Switch m_ShowHintsSwitch;
	GUIBase_Label m_HintLabel;
	GUIBase_Enum m_LanguageEnum;

	int m_OriginalGraphicValue = -1;

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------
	// IGuiPageOptionsDisplay methods
	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	public void OnInit(GUIBase_Layout layout)
	{
		m_GraphicEnum = GuiBaseUtils.GetControl<GUIBase_Enum>(layout, GRAPHDETAILS_ENUM);
		m_AutoDetectButton = GuiBaseUtils.GetControl<GUIBase_Button>(layout, DETECTGRAPHIC_BUTTON);
		m_ShowHintsSwitch = GuiBaseUtils.GetControl<GUIBase_Switch>(layout, SHOWHINTS_SWITCH);
		m_HintLabel = GuiBaseUtils.GetControl<GUIBase_Label>(layout, HINT_LABEL);
		m_LanguageEnum = GuiBaseUtils.GetControl<GUIBase_Enum>(layout, LANGUAGE_ENUM);

		if (m_OriginalGraphicValue < 0)
		{
			m_OriginalGraphicValue = GuiOptions.graphicDetail;
		}
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	public void OnShow()
	{
		m_GraphicEnum.RegisterDelegate(OnGraphicChanged);
		m_AutoDetectButton.RegisterReleaseDelegate(OnDetectGraphicButton);
		m_ShowHintsSwitch.RegisterDelegate(OnShowHintChanged);
		m_LanguageEnum.RegisterDelegate(OnLanguageChanged);
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	public void OnHide()
	{
		m_GraphicEnum.RegisterDelegate(null);
		m_AutoDetectButton.RegisterReleaseDelegate(null);
		m_ShowHintsSwitch.RegisterDelegate(null);
		m_LanguageEnum.RegisterDelegate(null);
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	public void OnReset()
	{
		m_GraphicEnum.Selection = (int)GuiOptions.graphicDetail; // this will invoke OnGraphicChanged() delegate
		m_ShowHintsSwitch.SetValue(GuiOptions.showHints);
		m_LanguageEnum.Selection = (int)GuiOptions.language;

		UpdateHint();
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	public void OnUpdate()
	{
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------
	// Handlers
	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	void OnGraphicChanged(int value)
	{
		GuiOptions.graphicDetail = value;
		//ApplyGraphicsOptions();

		UpdateHint();
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	void OnDetectGraphicButton(bool inside)
	{
		if (inside == true)
		{
			GuiOptions.graphicDetail = GuiOptions.GetDefaultGraphics();
			m_GraphicEnum.Selection = GuiOptions.graphicDetail; // this will invoke OnGraphicChanged() delegate

			UpdateHint();
		}
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	void OnShowHintChanged(bool state)
	{
		GuiOptions.showHints = state;
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	void OnLanguageChanged(int val)
	{
		GuiOptions.language = (GuiOptions.E_Language)val;
		//Debug.Log("Language changed to: " + GuiOptions.language);
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------
	// Private methods
	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	void UpdateHint()
	{
		if (GuiFrontendMain.IsVisible == true)
		{
			m_HintLabel.Widget.ShowImmediate(false, true);
		}
		else
		{
			m_HintLabel.Widget.ShowImmediate(m_OriginalGraphicValue != GuiOptions.graphicDetail, true);
		}
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	void ApplyGraphicsOptions()
	{
#if UNITY_EDITOR
		DeviceInfo.Initialize((DeviceInfo.Performance)GuiOptions.graphicDetail);
#endif
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------
}
