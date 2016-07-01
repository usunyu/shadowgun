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

//
// Main GuiEditor (plugin) class
//
// Holds two "subplugins":
//		- Widget plugin
//				- here you define new widgets (their UV in texture)
//		- Layout plugin
//				- here you define where widget lays in the layout (on the screen) and what is its real (on screen) size
//

using UnityEngine;
using UnityEditor;

public class GuiEditor : EditorWindow
{
	int						m_MainButtonsSelGridInt 		= 1;	// by default start with Layout window
    string[]				m_MainButtonsSelStrings 		= new string[] {"Widgets","Layouts","Fonts"};
	
	static GUIEditorWidget	m_WidgetWindow					= new GUIEditorWidget();
	static GUIEditorLayout	m_LayoutWindow					= new GUIEditorLayout();
	static GUIEditorFont	m_FontWindow					= new GUIEditorFont();
	
	static bool				m_MouseLeftDown					= false;
		
	enum E_MainWindowButtonGrid 
	{
		E_MWBG_WIDGETS,
		E_MWBG_LAYOUTS,
		E_MWBG_FONTS
	};
	
	[MenuItem ("Window/GUI Editor")]
	static void Init()
	{
		GuiEditor window = (GuiEditor)EditorWindow.GetWindow(typeof(GuiEditor));
		
		// The window recieves an OnGUI call whenever the mouse is moved over the window
		window.wantsMouseMove = true;
	}
	
	//---------------------------------------------------------------------------------------------
	//
	// OnGui
	//	Render selected "subplugin" (Widget or Layout)
	//
	//---------------------------------------------------------------------------------------------
	void OnGUI()
	{
		//
		// Draw Main buttons
		//
		m_MainButtonsSelGridInt = GUILayout.Toolbar(m_MainButtonsSelGridInt, m_MainButtonsSelStrings);
		
		// switch between subplugins based on selected button (from toolbar)

		switch (m_MainButtonsSelGridInt)
		{
			// Definition of widgets
		case (int)E_MainWindowButtonGrid.E_MWBG_WIDGETS:
			m_WidgetWindow.OnGUI(ref m_MouseLeftDown);
			break;
			
			// Definition of layouts 
		case (int)E_MainWindowButtonGrid.E_MWBG_LAYOUTS:
			m_LayoutWindow.OnGUI(ref m_MouseLeftDown);
			break;
			
			// Definition of fonts
		case (int)E_MainWindowButtonGrid.E_MWBG_FONTS:
			m_FontWindow.OnGUI();
			break;
		}
		
		// Repait everything
		Repaint();
	}
	
	//---------------------------------------------------------------------------------------------	
	
	void UpdateLabels()
	{
		GUIBase_Label[] labels = Object.FindObjectsOfType(typeof(GUIBase_Label)) as GUIBase_Label[];
		
		foreach (GUIBase_Label label in labels)
		{
			if(label.GenerateRunTimeData() == true)
			{
				EditorUtility.SetDirty(label);
			}				
		}
	}
	
	//---------------------------------------------------------------------------------------------	
	//
	// OnLostFocus()
	//
	//---------------------------------------------------------------------------------------------
	void OnLostFocus()
	{
		m_MouseLeftDown		= false;
	}
	
	void OnInspectorUpdate() 
	{
		//some label could have changed
		//UpdateLabels();
	} 	
}


