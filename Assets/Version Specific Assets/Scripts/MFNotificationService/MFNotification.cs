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

using LitJson;
using System;
using System.Collections.Generic;

public class MFNotificationConstants
{
	public const int INVALID_ID = -1;
	public const int MAX_NOTIFY_ID = 1 << 8;
}

public class MFNotification
{
	public enum Source
	{
		LOCAL,
		REMOTE
	}

	public MFNotification(string title, string text, string icon, string sound = "", int counter = 0)
	{
		Title = title;
		Text = text;
		Icon = icon;
		Counter = counter;
		Sound = sound;
		Origin = Source.LOCAL;
		Id = MFNotificationConstants.INVALID_ID;
	}

	public MFNotification()
	{
	}

	public string Title { get; set; }
	public string Text { get; set; }
	public string Icon { get; set; }
	public int Counter { get; set; }
	public string Sound { get; set; }
	public Source Origin { get; internal set; }
	public int Id { get; internal set; }
}
