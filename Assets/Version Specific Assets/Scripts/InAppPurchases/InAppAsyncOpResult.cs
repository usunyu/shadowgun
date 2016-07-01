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

using System.Collections.Generic;

public enum InAppAsyncOpState
{
	Waiting,
	Finished,
	Failed,
	Cancelled,
	CannotVerify
};

public class InAppAsyncOpResult
{
	public delegate void AsyncOpResDelegate(InAppAsyncOpResult res);

	public InAppAsyncOpState CurrentState { get; internal set; }

	public string ResultDesc
	{
		get
		{
			switch (CurrentState)
			{
			case InAppAsyncOpState.CannotVerify:
				return "cannot verify";
			default:
				return CurrentState.ToString().ToLower();
			}
		}
	}

	internal object UserData { get; set; }
	internal string ProductId { get; set; }
	internal string TransactionId { get; set; }

	public InAppAsyncOpResult()
	{
		CurrentState = InAppAsyncOpState.Waiting;
	}
}
