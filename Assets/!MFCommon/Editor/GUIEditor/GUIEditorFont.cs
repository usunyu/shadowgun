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
using UnityEditor;
using System.Collections;
using System.IO;

public class GUIEditorFont
{	
	Hashtable	m_Fonts = new Hashtable();
	
	//
	// ctor
	//
	public GUIEditorFont()
	{
	}
	
	// 
	// OnGUI()
	//
	public void OnGUI()
	{
		EditorGUILayout.BeginHorizontal();
		
		// Find all fonts already loaded to scene
		FindFontsInScene();
		
		// Show Left Panel
		RenderLeftPanel();
		
		// Show Right Panel
		RenderRightPanel();
		
		EditorGUILayout.EndHorizontal();
	}

	//
	// FindFontsInScene
	//
	void FindFontsInScene()
	{
		GUIBase_Font[] fonts = Object.FindObjectsOfType(typeof(GUIBase_Font)) as GUIBase_Font[];
		
		foreach (GUIBase_Font f in fonts)
		{
			if (!m_Fonts.ContainsKey(f.name))
			{
				AddFont(f);
			}
		}
	}

	//
	// Render left panel
	//
	void RenderLeftPanel()
	{
		// Vertical frame with fixed width
		EditorGUILayout.BeginVertical(GUILayout.Width(200));
		
		ShowNewFontButton();
		ShowChangeFontButton();
		ShowRebuildLablesButton();
		
		EditorGUILayout.Separator();
		
		EditorGUILayout.EndVertical();
	}
	
	//
	// Render right panel
	//
	void RenderRightPanel()
	{
	}
	
	//
	// ShowNewFontButton
	//
	void ShowNewFontButton()
	{
		if (GUILayout.Button("Load new font to scene"))
		{
			string fileName; 
			string fontName;
			
			if(!GetFontFilename(out fileName, out fontName))
				return;

			if (fontName != "")
			{
				if (! m_Fonts.ContainsKey(fontName))
				{
					GUIBase_Font.C_FontDscr	fDscr;
					if( !LoadFontDescription(fileName, out fDscr) )
						return;
					
					// Create game object
						
					GameObject		tmpGObj			= new GameObject(fontName);
					GUIBase_Font	fontComponent	= tmpGObj.AddComponent<GUIBase_Font>();
					
					fontComponent.m_FontDscr = fDscr;
					AddFont(fontComponent);
				}
			}
		}
	}
	
	bool GetFontFilename(out string fileName, out string fontName)
	{
		fileName = EditorUtility.OpenFilePanel("Open font table", "", "tab");
		fontName = "";

		if (fileName == null || fileName == "")
			return  false;
		
		// Cut path and extension
		string[]	splitFileName	= fileName.Split('/');
		string		nameWithExt;
			
		if (splitFileName.Length > 0)
		{
			nameWithExt = splitFileName[splitFileName.Length - 1];
				
			splitFileName = nameWithExt.Split('.');
				
			if (splitFileName.Length > 0)
			{
				fontName = splitFileName[0];
				return true;
			}
		}
		return false;
	}
	
	bool LoadFontDescription(string fileName, out GUIBase_Font.C_FontDscr fDscr)
	{
		int						maxWidth	= 0;
		fDscr  = new GUIBase_Font.C_FontDscr();

        using (StreamReader sr = File.OpenText(fileName))
		{
			bool endOfFile = false;

			if (! ReadInt(sr, out maxWidth, out endOfFile))
			{
				Debug.Log("Bad font description format " + fileName); 
				return false;
			}
			
			// Set maximal width of char
			fDscr.SetCharMaxWidth(maxWidth);
		
			while (! endOfFile)
			{
				if (! ReadCharDscr(ref fDscr, sr, out endOfFile))
				{
					if (endOfFile)
					{
						break;
					}
					else
					{
						Debug.Log("Bad font description format " + fileName); 
						return false;
					}
				}
			}
		}
		return true;
	}
	
	void ShowChangeFontButton()
	{
		if (GUILayout.Button("Change selected font"))
		{
			
			Object[] selFont = Selection.GetFiltered(typeof(GUIBase_Font), SelectionMode.TopLevel);
			
			if(selFont == null || selFont.Length < 1)
			{
				Debug.LogWarning("No font selected.");
				return;
			}
			
		
			string fileName; 
			string fontName;
			
			if(!GetFontFilename(out fileName, out fontName))
				return;
			
			GUIBase_Font.C_FontDscr	fDscr;
			if( !LoadFontDescription(fileName, out fDscr) )
				return;
				
			GUIBase_Font font = selFont[0] as  GUIBase_Font;				
			font.m_FontDscr = fDscr;
			if(font.name != fontName)
				font.name = fontName;
		
			RebuildAllLables();
		}
	}
	
	void RebuildAllLables()
	{
		GUIBase_Label[] labels = Object.FindObjectsOfType(typeof(GUIBase_Label)) as GUIBase_Label[];
		
		foreach (GUIBase_Label label in labels)
		{
			label.GenerateRunTimeData();
			EditorUtility.SetDirty(label);
		}
	}
	
	void ShowRebuildLablesButton()
	{
		if (GUILayout.Button("Rebuild lables in selection"))
		{
			Object[] selGOs = Selection.GetFiltered(typeof(GameObject), SelectionMode.Deep);
			Debug.Log("GO found: " + selGOs.Length);
			
			int labelsInSelection = 0;
			foreach (GameObject go in selGOs)
			{
				GUIBase_Label label = go.GetComponent<GUIBase_Label>();
				if(label ==  null)
					continue;
				
				labelsInSelection++;
				
				Debug.Log("Rebuilding label " + label.name + ": " + label.Text);
				
				label.GenerateRunTimeData();
				EditorUtility.SetDirty(label);
			}
			
			if(labelsInSelection > 0)
				Debug.Log("Rebuild " +  labelsInSelection + " labels.");
			else
				Debug.LogWarning("No GUIBase_Label in selection.");
		}
	}
	
	//
	// Read Integer from stream
	//
	bool ReadInt(StreamReader sr, out int intValue, out bool endOfFile)
	{
		bool	res = false;
		string	input;
					
		intValue	= 0;
		endOfFile	= true;
		
		while ((input = sr.ReadLine()) != null)
		{
			endOfFile	= false;
			
			if (int.TryParse(input, out intValue))
			{
				//Debug.Log(intValue);
				res = true;
				break;
			}

			endOfFile	= true;
		}
		
		return res;
	}
	
	//
	// Read Float from stream
	//
	bool ReadFloat(StreamReader sr, out float floatValue, out bool endOfFile)
	{
		bool	res	= false;
		string	input;
		
		floatValue	= 0.0f;
		endOfFile	= true;
		
		while ((input = sr.ReadLine()) != null)
		{
			endOfFile = false;
			
			if (float.TryParse(input, out floatValue))
			{
				//Debug.Log(floatValue);				
				res = true;
				break;
			}
			
			endOfFile	= true;
		}
		
		return res;
	}
	
	//
	// Read char description
	//
	bool ReadCharDscr(ref GUIBase_Font.C_FontDscr dscr, StreamReader sr, out bool endOfFile)
	{
		int		charIdx		= 0;
		float	width		= 0.0f;
		float	cx			= 0.0f;
		float	cy			= 0.0f;
		float	cw			= 0.0f;
		float	ch			= 0.0f;
		
		if (! ReadInt(sr, out charIdx, out endOfFile))
		{
			return false;
		}
						
		if (!ReadFloat(sr, out width, out endOfFile))
		{
			return false;
		}
						
		if (!ReadFloat(sr, out cx, out endOfFile))
		{
			return false;
		}
						
		if (!ReadFloat(sr, out cy, out endOfFile))
		{
			return false;
		}
						
		if (!ReadFloat(sr, out cw, out endOfFile))
		{
			return false;
		}
						
		if (!ReadFloat(sr, out ch, out endOfFile))
		{
			return false;
		}
		
		dscr.AddChar(charIdx, width, cx, cy, cw, ch);
		
		return true;
	}
	
	//
	// Adds font to m_Fonts
	// 
	void AddFont(GUIBase_Font f)
	{
		m_Fonts[f.name] = f.m_FontDscr;
		//Debug.Log("count of chars = " + f.m_FontDscr.m_CharTable.Length);
	}
}
