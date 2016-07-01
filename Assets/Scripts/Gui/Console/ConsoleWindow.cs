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

// console output window using standart gui
// for console commands see ConsoleCmdBase.cs and ConsoleCmdHelp.cs as example how to create console commands

using UnityEngine;
using System;
using System.Collections.Generic;

class ConsoleWindow
{
	// type of the entry in output window
	public enum OutputLineType
	{
		Error,
		Assert,
		Warning,
		Log,
		Exception,
		Console,
		None
	};

	// describes one entry in output window
	struct OutputLine
	{
		public string Text { get; private set; }
		public OutputLineType Type { get; private set; }
		public string Stack { get; private set; }

		public OutputLine(string _Text, string _Stack, OutputLineType _Type) : this()
		{
			Text = _Text;
			Type = _Type;
			Stack = _Stack;
		}
	};

	// list of all entries in the output window
	List<OutputLine> Lines = new List<OutputLine>();

	// current scroller position
	Vector2 ScrollPosition;

	// current text holded in input text field
	string InputText = string.Empty;

	// used for filtering messages
	string FilterText = string.Empty;

	// holding cleaned text entered via input box
	string LineEntered = string.Empty;

	// prevents from multiple initialization of styles
	bool bStylesInitialized = false;

	// custom gui styles
	GUIStyle LabelStyle;
	GUIStyle InputStyle;
	GUIStyle ScrollStyle;
	GUIStyle ScrollbarVStyle;
	GUIStyle ScrollbarHStyle;

	// internal variable holding window rectangle
	Rect WinRect;

	// internal variable holding window ID (GUI related)
	int WinID;

	// true if this window contains input box also
	bool bInput;

	// keeping current state of Collapse switch
	bool bCollapse = false;

	// output filters
	bool bShowErrors = true;
	bool bShowWarnings = true;
	bool bShowLogs = true;
	bool bShowException = true;
	bool bShowConsole = true;

	// Title of console window
	string Title;

	// true, if input should gain focus during next OnGUI() call
	bool bFocusInput = false;

	bool bHandlingHistory = false;

	// stack of used commands
	List<string> LineStack = new List<string>();
	int LineStackCurrent = -1;

	public ConsoleWindow(string name, int id, bool input, float x, float y, float w, float h)
	{
		bInput = input;

		WinID = id;

		Title = name;

		WinRect = new Rect(x, y, w, h);
	}

	public void OnShow(bool Enable)
	{
		if (Enable)
		{
			bFocusInput = true;
		}
	}

	// adds new line to the output
	public void AddLine(string _Text, string _Stack, OutputLineType _Type)
	{
		Lines.Add(new OutputLine(_Text, _Stack, _Type));

		// autoscroll
		ScrollPosition.y = float.MaxValue;
	}

	// adds new line to the output
	public void AddLine(string _Text, string _Stack, LogType _Type)
	{
		OutputLineType Converted = OutputLineType.None;

		switch (_Type)
		{
		case LogType.Error:
			Converted = OutputLineType.Error;
			break;
		case LogType.Assert:
			Converted = OutputLineType.Assert;
			break;
		case LogType.Warning:
			Converted = OutputLineType.Warning;
			break;
		case LogType.Log:
			Converted = OutputLineType.Log;
			break;
		case LogType.Exception:
			Converted = OutputLineType.Exception;
			break;
		}

		AddLine(_Text, _Stack, Converted);
	}

	public void OnUpdate()
	{
		if (bHandlingHistory)
		{
			bHandlingHistory = false;
		}
	}

	public void OnGUI()
	{
		if (!bStylesInitialized)
		{
			InitStyles();

			bStylesInitialized = true;
		}

		// this window should be placed over all others
		GUI.depth = -64;

		// title color
		GUI.contentColor = Color.yellow;

		WinRect = GUILayout.Window(WinID, WinRect, GuiCallback, Title);
	}

	// returns last text entered using input textfield
	// this will reset keeped value also
	public string PickupLineEntered()
	{
		string Result = LineEntered;

		LineEntered = string.Empty;

		return Result;
	}

	void InitStyles()
	{
		LabelStyle = new GUIStyle(GUI.skin.label);

		LabelStyle.normal.textColor = Color.green;
		LabelStyle.padding.top = LabelStyle.padding.bottom = 0;

		InputStyle = new GUIStyle(GUI.skin.textField);

		InputStyle.normal.textColor = InputStyle.active.textColor = Color.green;
		InputStyle.hover.textColor = InputStyle.focused.textColor = Color.green;

		ScrollStyle = new GUIStyle(GUI.skin.textField);

		ScrollbarVStyle = new GUIStyle(GUI.skin.verticalScrollbar);
		ScrollbarHStyle = new GUIStyle(GUI.skin.horizontalScrollbar);

		ScrollbarVStyle.padding.top = ScrollbarVStyle.padding.bottom = 1;
		ScrollbarHStyle.padding.left = ScrollbarHStyle.padding.right = 1;
	}

	// called during OnGUI()
	void GuiCallback(int windowID)
	{
		//	HandleHistory();

		GUI.contentColor = Color.white;

		ScrollPosition = GUILayout.BeginScrollView(ScrollPosition, false, true, ScrollbarHStyle, ScrollbarVStyle, ScrollStyle);

		// proceed all line entries
		for (int i = 0; i < Lines.Count; i++)
		{
			OutputLine Line = Lines[i];

			// if desired, this will skip repeating messages
			if (bCollapse && i > 0 && Line.Text == Lines[i - 1].Text)
			{
				continue;
			}

			bool bLineEnabled = false;

			// choose color of entry based on type
			switch (Line.Type)
			{
			case OutputLineType.Error:
				LabelStyle.normal.textColor = Color.red;
				bLineEnabled = bShowErrors;
				break;

			case OutputLineType.Exception:
				LabelStyle.normal.textColor = Color.magenta;
				bLineEnabled = bShowException;
				break;

			case OutputLineType.Warning:
				LabelStyle.normal.textColor = Color.yellow;
				bLineEnabled = bShowWarnings;
				break;

			case OutputLineType.Console:
				LabelStyle.normal.textColor = Color.cyan;
				bLineEnabled = bShowConsole;
				break;

			default:
				LabelStyle.normal.textColor = Color.green;
				bLineEnabled = bShowLogs;
				break;
			}

			if (bLineEnabled)
			{
				if (FilterText != string.Empty && FilterText.Length > 0)
				{
					string[] words = FilterText.ToLower().Split(' ');

					foreach (string word in words)
					{
						if (!Line.Text.ToLower().Contains(word))
						{
							bLineEnabled = false;
							break;
						}
					}
				}

				if (bLineEnabled)
				{
					GUILayout.Label(Line.Text, LabelStyle);
				}
			}
		}

		GUILayout.EndScrollView();

		// bottom panel
		GUILayout.BeginHorizontal();

		if (bInput)
		{
			// TODO : eatKeyPressOnTextFieldFocus is obsolete
			//Input.eatKeyPressOnTextFieldFocus = false;

			GUI.SetNextControlName("Input");

			InputText = GUILayout.TextField(InputText, -1, InputStyle, GUILayout.Width(Screen.width*3/4), GUILayout.MaxHeight(20));

			if (GUI.changed == true)
			{
				if (InputText.Contains("\n"))
				{
					LineEntered = InputText.Replace("\n", "");

					InputText = string.Empty;

					if (0 == LineStack.Count || LineEntered != LineStack[LineStack.Count - 1])
					{
						if (LineEntered.Length > 0)
						{
							if (LineEntered.Replace(" ", "").Length > 0)
							{
								LineStack.Add(LineEntered);
							}
						}
					}

					LineStackCurrent = -1;
				}

				InputText = InputText.Replace("`", "");
			}
		}
		else
		{
			// filters

			LabelStyle.normal.textColor = Color.white;

			GUILayout.Label("Filters", LabelStyle);

			FilterText = GUILayout.TextField(FilterText, -1, InputStyle, GUILayout.Width(Screen.width/8), GUILayout.MaxHeight(20));

			if (GUI.changed == true)
			{
				FilterText = FilterText.Replace("`", "");
			}

			bShowErrors = OnGuiTogleColored(bShowErrors, Color.red, "Error");
			bShowWarnings = OnGuiTogleColored(bShowWarnings, Color.yellow, "Warning");
			bShowLogs = OnGuiTogleColored(bShowLogs, Color.green, "Log");
			bShowException = OnGuiTogleColored(bShowException, Color.magenta, "Exception");
			bShowConsole = OnGuiTogleColored(bShowConsole, Color.cyan, "Console commands");
		}

		GUILayout.FlexibleSpace();

		bCollapse = OnGuiTogleColored(bCollapse, Color.white, "Collapse");

		if (GUILayout.Button("Clear"))
		{
			Lines.Clear();
		}

		GUILayout.EndHorizontal();

		if (bFocusInput)
		{
			GUI.FocusControl("Input");

			bFocusInput = false;
		}

		if (GUI.GetNameOfFocusedControl() == "Input")
		{
			HandleHistory();
			HandleEscape();
		}
	}

	void HandleHistory()
	{
		// this window has command input box
		if (bInput && !bHandlingHistory)
		{
			bool bChangeText = false;

			// handling Up key ( previous item in history )
			if (Input.GetKeyDown(KeyCode.UpArrow) && LineStack.Count > 0)
			{
				if (LineStackCurrent < 0)
				{
					LineStackCurrent = LineStack.Count - 1;
					bChangeText = true;
				}
				else
				{
					if (LineStackCurrent > 0)
					{
						LineStackCurrent--;
						bChangeText = true;
					}
				}
			}

			// handling Down key ( next item in history )
			if (Input.GetKeyDown(KeyCode.DownArrow))
			{
				if (LineStackCurrent >= 0)
				{
					if (LineStackCurrent < (LineStack.Count - 1))
					{
						LineStackCurrent++;
						bChangeText = true;
					}
				}
			}

			if (bChangeText)
			{
				if (LineStackCurrent >= 0 && LineStackCurrent < LineStack.Count)
				{
					InputText = LineStack[LineStackCurrent];

					bHandlingHistory = true;
				}
			}
		}
	}

	void HandleEscape()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			InputText = "";
		}
	}

	// helper method called during OnGUI() for manipulating togle 
	bool OnGuiTogleColored(bool bCurrentState, Color ColorSelected, string Text)
	{
		Color OldColor = GUI.contentColor;

		GUI.contentColor = bCurrentState ? ColorSelected : Color.grey;

		bool Result = GUILayout.Toggle(bCurrentState, Text, GUILayout.ExpandWidth(false));

		GUI.contentColor = OldColor;

		return Result;
	}
};
