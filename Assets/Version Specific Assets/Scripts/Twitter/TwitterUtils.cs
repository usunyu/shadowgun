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

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public static class TwitterUtils
{
	public static void LogIn(Action<bool> CompletionCallback)
	{
	}

	//-----------------------------------------------------------------------------------------------------------------
	// Retrieve ID of logged-in user.
	// 
	// Note: (1) User must be already logged !!!
	//       (2) Result should be "cached" by application because underlying Twitter API request is "rate" limited:
	//           max 15 calls per 15 mins !?!
	//       (2) On failure empty string is passed in completion-callback.
	//-----------------------------------------------------------------------------------------------------------------

	public static void GetUserID(Action<string> CompletionCallback)
	{
	}

	//-----------------------------------------------------------------------------------------------------------------
	// Retrieves IDs of all "sites" followed by logged-in user.
	// 
	// Note: (1) User must be already logged !!!
	//       (2) Works in "batches" so given callback will be called for every batch with partial result.
	//           The end of job is "signalized" by 'empty' list passed into the callback or by 'null'
	//           when error/failure occurs.
	//       (3) Result should be "cached" by application because underlying Twitter API request (one per batch)
	//           is "rate" limited: max 15 calls per 15 mins !?!
	//-----------------------------------------------------------------------------------------------------------------

	public static void GetFollowingIDs(Action<List<string>> CompletionCallback)
	{
	}

	//-----------------------------------------------------------------------------------------------------------------
	// Checks if logged-in user "follows" (all) specified profile(s).
	// 
	// - TwitterIDs ... Up to 100 by comma separated IDs (or screen-names)
	// 
	// Note: (1) User must be already logged !!!
	//       (2) Results should be "cached" by application because underlying Twitter API request
	//           is "rate" limited: max 15 calls per 15 mins !?!
	//       (3) On failure 'false' is passed in completion-callback.
	//-----------------------------------------------------------------------------------------------------------------

	public static void DoesUserFollow(string TwitterIDs, Action<bool> CompletionCallback)
	{
	}

	//-----------------------------------------------------------------------------------------------------------------
	// Posts message.
	// 
	// Note: (1) User will be asked to log-in to Twitter if necessary.
	//       (2) On failure 'false' is passed in completion-callback.
	//-----------------------------------------------------------------------------------------------------------------

	public static void PostMessage(string Message, Action<bool> CompletionCallback)
	{
	}

	//-----------------------------------------------------------------------------------------------------------------
	// Captures and posts screenshot (with short message).
	// 
	// Note: (1) User will be asked to log-in to Twitter if necessary.
	//       (2) On failure 'false' is passed in completion-callback.
	//       (3) Don't call too frequently because on devices with high resolution or with slow storage this can take
	//           a few seconds. And because screenshot must be loaded into memory (at some point) it will also consume
	//           a big chunk of it!
	//-----------------------------------------------------------------------------------------------------------------

	public static void PostScreenshot(string Message, Action<bool> CompletionCallback)
	{
	}
}
