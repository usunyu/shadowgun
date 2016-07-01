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

using System;
using System.Collections.Generic;

public class MFNotificationServiceDummy : MFNotificationService
{
	protected override void NotifyInternal(int id, MFNotification notification, DateTime when, TimeSpan period)
	{
	}

	protected override void CancelNotificationInternal(int id)
	{
	}

	protected override void CancelAllInternal()
	{
	}

	protected override List<MFNotification> ReceivedNotificationsInternal()
	{
		return new List<MFNotification>();
	}

	protected override void ClearReceivedNotificationsInternal()
	{
	}

	protected override void RegisterPushNotificationsInternal()
	{
	}

	protected override void UnregisterPushNotificationsInternal()
	{
	}
}
