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

// base class for console commands

using System.Collections.Generic;

public abstract class ConsoleCmdBase
{
	// @return name of this command - f.e. "help", "load" or "copy"

	public abstract string GetCmdName();

	// called when command should be proceeded
	// @return text for console output, f.e. "All objects was destroyed in 0.01 secs"
	// @param CommandLine contains rest of command line after command keyword
	public abstract string ProceedCommand(string[] CommandLineWords);

	// return help description
	public abstract string GetHelpString();
}

// helper class for list of commands
public class CommandsList : List<ConsoleCmdBase>
{
	public ConsoleCmdBase FindByName(string Name)
	{
		foreach (ConsoleCmdBase Cmd in this)
		{
			if (0 == string.Compare(Cmd.GetCmdName(), Name, true))
			{
				return Cmd;
			}
		}

		return null;
	}
}
