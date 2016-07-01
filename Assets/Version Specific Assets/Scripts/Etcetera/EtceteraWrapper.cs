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

public static class EtceteraWrapper
{
	public static void Init()
	{
	}
	
	public static void Done()
	{
	}

	public static void ShowDialog( string Title, string Message, string TextOnButton )
	{
	}

	public static void ShowDialog( string Title, string Message, string TextOnButtonA, string TextOnButtonB )
	{
	}

	public static void ShowPrompt( string Title, string Message, string FieldLabel, string FieldText )
	{
	}

	public static void ShowPrompt( string Title, string Message, string FieldLabel, string FieldText, string Field2Label, string Field2Text )
	{
	}

	public static void ShowActivityNotification( string Message )
	{
	}

	public static void HideActivityNotification()
	{
	}

	public static void ShowWeb( string URL )
	{
	}

	public static bool ShowEmailComposer( string Address, string Subject, string Message )
	{
		return false;
	}

	public static void AskForReview( string Title, string Message, string RateIt, string RemindLater, string DontAsk, string AppID )
	{
	}

	public static void AskForReview( string Title, string Message, string RateIt, string RemindLater, string DontAsk, int LauchCount, int HoursBetween )
	{
	}

	public static void AskForReview( string Title, string Message, string RateIt, string RemindLater, string DontAsk, string AppID, int LauchCount, int HoursBetween )
	{
	}

	private static void DialogFinished( string TextOnPressedButton )
	{
	}

	private static void DialogCanceled()
	{
	}

	private static void PromptFinished( string FieldText )
	{
	}

	private static void Prompt2Finished( string FieldText, string Field2Text )
	{
	}

	private static void PromptCanceled()
	{
	}
}

public static class EtceteraManager
{
	public delegate void AlertButtonClicked(string TextOnPressedButton);

#pragma warning disable 0067
	public static event AlertButtonClicked alertButtonClickedEvent;
#pragma warning restore
}

public static class EtceteraBinding
{
	public static void showAlertWithTitleMessageAndButtons(string kind, string text, string[] buttons)
	{
	}
}
