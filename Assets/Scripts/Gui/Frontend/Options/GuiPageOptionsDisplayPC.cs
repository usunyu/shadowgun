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

public class GuiPageOptionsDisplayPC : GuiPageOptionsDisplay.IGuiPageOptionsDisplay
{
	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	readonly static string GRAPHDETAILS_ENUM = "GraphDetails_Enum";
	readonly static string DETECTGRAPHIC_BUTTON = "DetectGraphic_Button";
	readonly static string SHOWHINTS_SWITCH = "ShowHints_Switch";
	readonly static string LANGUAGE_ENUM = "Language_Enum";

	readonly static string RESOLUTION_ENUM = "Resolution_Enum";
	readonly static string RESOLUTION_LABEL = "Resolution_Enum_Label";
	readonly static string FULLSCREEN_SWITCH = "Fullscreen_Switch";
	readonly static string APPLY_BUTTON = "ApplyButton";

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	GUIBase_Enum m_GraphicEnum;
	GUIBase_Button m_AutoDetectButton;
	GUIBase_Switch m_ShowHintsSwitch;
	GUIBase_Enum m_LanguageEnum;

	GUIBase_Switch m_FullscreenSwitch;
	GUIBase_Enum m_ResolutionEnum;
	GUIBase_Label[] m_ResolutionLabels = new GUIBase_Label[3];
	GUIBase_Button m_ApplyButton;

	Resolution m_CurrentResolution;
	int m_ResolutionEnumLastValue;
	int m_ResolutionEnumSelection;
	int m_ResolutionMax = 0;

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
		m_LanguageEnum = GuiBaseUtils.GetControl<GUIBase_Enum>(layout, LANGUAGE_ENUM);

		m_FullscreenSwitch = GuiBaseUtils.GetControl<GUIBase_Switch>(layout, FULLSCREEN_SWITCH);
		m_ResolutionEnum = GuiBaseUtils.GetControl<GUIBase_Enum>(layout, RESOLUTION_ENUM);
		m_ResolutionLabels[0] = GuiBaseUtils.GetControl<GUIBase_Label>(layout, RESOLUTION_LABEL + 0);
		m_ResolutionLabels[1] = GuiBaseUtils.GetControl<GUIBase_Label>(layout, RESOLUTION_LABEL + 1);
		m_ResolutionLabels[2] = GuiBaseUtils.GetControl<GUIBase_Label>(layout, RESOLUTION_LABEL + 2);
		m_ApplyButton = GuiBaseUtils.GetControl<GUIBase_Button>(layout, APPLY_BUTTON);

		m_ShowHintsSwitch.SetValue(GuiOptions.showHints);
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	public void OnShow()
	{
		m_GraphicEnum.RegisterDelegate(OnGraphicChanged);
		m_AutoDetectButton.RegisterReleaseDelegate(OnDetectGraphicButton);
		m_ShowHintsSwitch.RegisterDelegate(OnShowHintChanged);
		m_LanguageEnum.RegisterDelegate(OnLanguageChanged);

		m_FullscreenSwitch.RegisterDelegate(OnFullscreenToggled);
		m_ResolutionEnum.RegisterDelegate(OnResolutionChanged);
		m_ApplyButton.RegisterReleaseDelegate(OnApplyButton);

		if (GuiOptions.fullScreenResolution.width <= 0 || GuiOptions.fullScreenResolution.height <= 0)
		{
			GuiOptions.fullScreenResolution = Screen.resolutions[Screen.resolutions.Length - 1];
		}

		m_FullscreenSwitch.SetValue(Screen.fullScreen);

		m_ResolutionEnumSelection = -1;
		m_ResolutionMax = Screen.resolutions.Length - 1;

		m_CurrentResolution.width = Screen.width;
		m_CurrentResolution.height = Screen.height;

		m_ResolutionLabels[m_ResolutionEnum.Selection].SetNewText(m_CurrentResolution.width + "x" + m_CurrentResolution.height);

		for (int i = 0; i < Screen.resolutions.Length; i++)
		{
			if (Screen.resolutions[i].width == m_CurrentResolution.width && Screen.resolutions[i].height == m_CurrentResolution.height)
			{
				m_ResolutionEnumSelection = i;
				break;
			}
		}

		SetApplyButton();
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	public void OnHide()
	{
		m_GraphicEnum.RegisterDelegate(null);
		m_AutoDetectButton.RegisterReleaseDelegate(null);
		m_ShowHintsSwitch.RegisterDelegate(null);
		m_LanguageEnum.RegisterDelegate(null);
		m_ApplyButton.RegisterReleaseDelegate(null);

		m_FullscreenSwitch.RegisterDelegate(null);
		m_ResolutionEnum.RegisterDelegate(null);
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	public void OnReset()
	{
		m_GraphicEnum.Selection = (int)GuiOptions.graphicDetail; // this will invoke OnGraphicChanged() delegate
		m_ShowHintsSwitch.SetValue(GuiOptions.showHints);
		m_LanguageEnum.Selection = (int)GuiOptions.language;
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	public void OnUpdate()
	{
		//in cases when user exits fullscreen in other way (escape, alt+enter)
		SetApplyButton();
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------
	// Handlers
	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	void OnGraphicChanged(int value)
	{
		SetApplyButton();
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	void OnDetectGraphicButton(bool inside)
	{
		if (inside == true)
		{
			m_GraphicEnum.Selection = GuiOptions.GetDefaultGraphics(); // this will invoke OnGraphicChanged() delegate
		}
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	void OnShowHintChanged(bool state)
	{
		SetApplyButton();
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	void OnLanguageChanged(int val)
	{
		SetApplyButton();
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	void OnFullscreenToggled(bool state)
	{
		SetApplyButton();
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	void OnResolutionChanged(int val)
	{
		//using three-values enum to simulate dynamic-sized enum
		if (m_ResolutionEnumLastValue == val)
			return;

		if (m_ResolutionEnumLastValue == 2 && val == 0)
			m_ResolutionEnumSelection++;

		else if (m_ResolutionEnumLastValue == 0 && val == 2)
			m_ResolutionEnumSelection--;

		else if (m_ResolutionEnumLastValue < val)
			m_ResolutionEnumSelection++;

		else
			m_ResolutionEnumSelection--;
		m_ResolutionEnumLastValue = val;

		if (m_ResolutionEnumSelection < 0)
			m_ResolutionEnumSelection = m_ResolutionMax;

		else if (m_ResolutionEnumSelection > m_ResolutionMax)
			m_ResolutionEnumSelection = 0;

		Resolution res = Screen.resolutions[m_ResolutionEnumSelection];
		m_ResolutionLabels[m_ResolutionEnum.Selection].SetNewText(res.width + "x" + res.height);

		SetApplyButton();
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	void OnApplyButton(bool inside)
	{
		if (!inside)
			return;

		GuiOptions.graphicDetail = m_GraphicEnum.Selection;
		//Debug.Log("Setting gr detail: " + GuiOptions.graphicDetail);
		ApplyGraphicsOptions();

		GuiOptions.showHints = m_ShowHintsSwitch.GetValue();

		GuiOptions.language = (GuiOptions.E_Language)m_LanguageEnum.Selection;
		//Debug.Log("Language changed to: " + GuiOptions.language);

		if (m_ResolutionEnumSelection >= 0 &&
			(Screen.resolutions[m_ResolutionEnumSelection].width != Screen.width ||
			 Screen.resolutions[m_ResolutionEnumSelection].height != Screen.height))
		{
			m_CurrentResolution = Screen.resolutions[m_ResolutionEnumSelection];
			Screen.SetResolution(m_CurrentResolution.width, m_CurrentResolution.height, m_FullscreenSwitch.GetValue());
			GuiOptions.fullScreenResolution = m_CurrentResolution;
		}
		else if (m_FullscreenSwitch.GetValue() != Screen.fullScreen)
		{
			Screen.fullScreen = m_FullscreenSwitch.GetValue();
		}

		m_ApplyButton.SetDisabled(true);
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------
	// Private methods
	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	void SetApplyButton()
	{
		bool disabled = true;

		if (GuiOptions.graphicDetail != m_GraphicEnum.Selection ||
			GuiOptions.showHints != m_ShowHintsSwitch.GetValue() ||
			GuiOptions.language != (GuiOptions.E_Language)m_LanguageEnum.Selection)
		{
			disabled = false;
		}

//#if !UNITY_EDITOR
		if ((m_ResolutionEnumSelection >= 0 &&
			 (Screen.resolutions[m_ResolutionEnumSelection].width != m_CurrentResolution.width ||
			  Screen.resolutions[m_ResolutionEnumSelection].height != m_CurrentResolution.height))
			|| m_FullscreenSwitch.GetValue() != Screen.fullScreen)
		{
			disabled = false;
		}
//#endif		
		if (m_ApplyButton.IsDisabled != disabled)
			m_ApplyButton.SetDisabled(disabled);
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------

	void ApplyGraphicsOptions()
	{
		DeviceInfo.Initialize((DeviceInfo.Performance)GuiOptions.graphicDetail);
	}

	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------
}
