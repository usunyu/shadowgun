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

// console command 'endround' - This command will force current round to finish
// @see ConsoleCmdBase

using UnityEngine;

public class ConsoleCmdEndRound : ConsoleCmdBase
{
	// @see ConsoleCmdBase.GetCmdName()
	public override string GetCmdName()
	{
		return "EndRound";
	}

	// @see ConsoleCmdBase.ProceedCommand()
	public override string ProceedCommand(string[] CommandLineWords)
	{
#if !DEADZONE_CLIENT
		if( null != Server.Instance )
		{
			Server.Instance.GameInfo.SimulateEndRound();
			
			return "Finishing ... ";
		}
#endif

		return "Server instance does not exist.";
	}

	// @see ConsoleCmdBase.GetHelpString()
	public override string GetHelpString()
	{
		string Result = "This command will force current round to finish. \n\nusage : EndRound";
		return Result;
	}
};
