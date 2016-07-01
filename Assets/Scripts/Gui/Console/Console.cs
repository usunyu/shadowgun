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

// debug console with log, text input ability (with commands), output filtering etc.
// for console commands see ConsoleCmdBase.cs and ConsoleCmdHelp.cs as example how to create console commands

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

// shortcut for usage of OutputLineType
using LineType = ConsoleWindow.OutputLineType;

public class Console : MonoBehaviour
{
	ConsoleWindow Log = new ConsoleWindow("Log",
										  134682,
										  false,
										  Screen.width*0.01f,
										  Screen.width*0.01f,
										  Screen.width - (2*Screen.width*0.01f),
										  3*Screen.height/4 - (2*Screen.width*0.01f));
	ConsoleWindow Cmd = new ConsoleWindow("Console",
										  135682,
										  true,
										  Screen.width*0.01f,
										  Screen.width*0.01f + 3*Screen.height/4,
										  Screen.width - (2*Screen.width*0.01f),
										  Screen.height/4 - (2*Screen.width*0.01f));

	// true if console is visible
	bool Enabled = false;

	bool bFirstShowDone = false;

	public static CommandsList _CommandObjects = null;

	// list of instances for all known console commands
	public static CommandsList CommandObjects
	{
		get
		{
			if (null == _CommandObjects)
			{
				_CommandObjects = new CommandsList();
			}
			return _CommandObjects;
		}
		private set { }
	}

	void OnEnable()
	{
		Application.logMessageReceived += HandleLog;
	}

	void OnDisable()
	{
		Application.logMessageReceived -= HandleLog;
	}

	void Update()
	{
		if (Enabled)
		{
			if (!bFirstShowDone)
			{
				bFirstShowDone = true;

				// build informations about all classes derived from ConsoleCmdBase
				BuildClassInfo();

				CommandLine("help");
			}

			string Command = Cmd.PickupLineEntered();

			if (Command != String.Empty)
			{
				CommandLine(Command);
			}
		}

		// enable/disable console (key '~')
		if (Input.GetKeyDown(KeyCode.BackQuote))
		{
			Enabled = !Enabled;

			Cmd.OnShow(Enabled);
		}

		Log.OnUpdate();
		Cmd.OnUpdate();
	}

	void OnGUI()
	{
		if (!Enabled)
		{
			return;
		}

		Log.OnGUI();
		Cmd.OnGUI();
	}

	// callback to notify console about new log message
	void HandleLog(string message, string stackTrace, LogType type)
	{
		Log.AddLine(message, stackTrace, type);
	}

	// handle new command from input box
	void CommandLine(string _Command)
	{
		string Result = string.Empty;
		string CmdKeyword = string.Empty;

		string Command = _Command.Trim().ToLower();

		string[] Words = Command.Split(' ');

		// use first word as command identifier
		if (Words.Length > 0)
		{
			CmdKeyword = Words[0];

			ConsoleCmdBase Cmd = CommandObjects.FindByName(CmdKeyword);

			if (null != Cmd)
			{
				Result = Cmd.ProceedCommand(Words);
			}
		}

		// output section

		AddLine(">" + Command, "", LineType.Console);

		if (Result != string.Empty)
		{
			AddLine(Result, "", LineType.Console);
		}
		else
		{
			AddLine("Command '" + CmdKeyword + "' not found.", "", LineType.Console);
		}
	}

	// add new entry line to output windows
	void AddLine(string _Text, string _Stack, LineType _Type)
	{
		Cmd.AddLine(_Text, _Stack, _Type);
		Log.AddLine(_Text, _Stack, _Type);
	}

	// build informations about all classes derived from ConsoleCmdBase
	static void BuildClassInfo()
	{
		Assembly assembly = Assembly.GetExecutingAssembly();

		Type baseType = typeof (ConsoleCmdBase);

		// grab all types
		Type[] types = assembly.GetTypes();

		foreach (var type in types)
		{
			if (!type.IsClass)
			{
				continue;
			}

			if (type.IsSubclassOf(baseType))
			{
				ConsoleCmdBase cmd = CreateCmdObjectByType(type);

				if (null != cmd)
				{
					CommandObjects.Add(cmd);
				}
			}
		}
	}

	// creates instance of given type ( derived from ConsoleCmdBase )
	static ConsoleCmdBase CreateCmdObjectByType(Type T)
	{
		MethodInfo I = T.GetMethod("GetCmdName");

		if (null != I)
		{
			return Activator.CreateInstance(T) as ConsoleCmdBase;
		}

		return null;
	}
}
