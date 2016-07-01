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

// console command 'help'
// @see ConsoleCmdBase

using UnityEngine;

public class ConsoleCmdHelp : ConsoleCmdBase
{
	// @see ConsoleCmdBase.GetCmdName()
	public override string GetCmdName()
	{
		return "help";
	}

	// @see ConsoleCmdBase.ProceedCommand()
	public override string ProceedCommand(string[] CommandLineWords)
	{
		if (null != CommandLineWords && CommandLineWords.Length > 1)
		{
			ConsoleCmdBase Cmd = Console.CommandObjects.FindByName(CommandLineWords[1]);

			if (null != Cmd)
			{
				return Cmd.GetHelpString();
			}

			return "Command '" + CommandLineWords[1] + "' does not exist.";
		}

		return GetHelpString();
	}

	// @see ConsoleCmdBase.GetHelpString()
	public override string GetHelpString()
	{
		string Result = "usage : help <command> \n\n Available commands : ";

		bool bFirst = true;

		foreach (ConsoleCmdBase Base in Console.CommandObjects)
		{
			if (bFirst)
			{
				bFirst = false;

				Result += Base.GetCmdName();
			}
			else
			{
				Result += ", " + Base.GetCmdName();
			}
		}

		return Result;
	}
};
