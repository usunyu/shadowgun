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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MFNotificationService : MonoBehaviour
{
	//******************************************************************//
	// Public API
	//******************************************************************//

	public static void CancelNotification(int id)
	{
		Instance.CancelNotificationInternal(id);
	}

	public static void CancelAll()
	{
		Instance.CancelAllInternal();
	}

	public static void Notify(int id, MFNotification notification)
	{
		Instance.NotifyInternal(id, notification, DateTime.Now, TimeSpan.Zero);
	}

	public static void Notify(int id, MFNotification notification, DateTime when)
	{
		Instance.NotifyInternal(id, notification, when, TimeSpan.Zero);
	}

	public static void Notify(int id, MFNotification notification, DateTime when, TimeSpan period)
	{
		Instance.NotifyInternal(id, notification, when, period);
	}

	public static List<MFNotification> ReceivedNotifications
	{
		get { return Instance.ReceivedNotificationsInternal(); }
	}

	public static void ClearReceivedNotifications()
	{
		Instance.ClearReceivedNotificationsInternal();
	}

	public static void RegisterPushNotifications()
	{
		Instance.RegisterPushNotificationsInternal();
	}

	public static void UnregisterPushNotifications()
	{
		Instance.UnregisterPushNotificationsInternal();
	}

	//******************************************************************//
	// Private API
	//******************************************************************//
	protected abstract void NotifyInternal(int id, MFNotification notification, DateTime when, TimeSpan period);
	protected abstract void CancelNotificationInternal(int id);
	protected abstract void CancelAllInternal();
	protected abstract List<MFNotification> ReceivedNotificationsInternal();
	protected abstract void ClearReceivedNotificationsInternal();
	protected abstract void RegisterPushNotificationsInternal();
	protected abstract void UnregisterPushNotificationsInternal();

	protected static MFNotificationService Instance
	{
		get
		{
			if (ms_Instance == null)
			{
				GameObject go = new GameObject(typeof (MFNotificationService).Name);
				ms_Instance = go.AddComponent<MFNotificationServiceDummy>();
				GameObject.DontDestroyOnLoad(go);
			}

			return ms_Instance;
		}
	}

	static MFNotificationService ms_Instance = null;
}
