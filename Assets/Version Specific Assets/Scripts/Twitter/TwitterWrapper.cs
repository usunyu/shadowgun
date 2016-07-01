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
using RequestCompletionCallback = System.Action<string /* Error */, object /* Result */>;

// Application should use this "wrapper" instead of calling methods directly on 'Prime31' classes!
public class TwitterWrapper : MonoBehaviour
{
	const int MsgLengthLimit = 140;

	static TwitterWrapper m_Instance;
	static GameObject m_InstanceOwner;

	public static TwitterWrapper Instance
	{
		get { return m_Instance; }
	}

	public static int PendingRequests
	{
		get { return 0; }
	}

	//-----------------------------------------------------------------------------------------------------------------
	// Initialization "of" Twitter application.
	//-----------------------------------------------------------------------------------------------------------------
	public static void Init(string CustomerKey, string CustomerSecret)
	{
		if (m_Instance == null)
		{
			// create global instance...

			m_InstanceOwner = new GameObject("_Twitter_");
			m_Instance = m_InstanceOwner.AddComponent<TwitterWrapper>();

			DontDestroyOnLoad(m_Instance);
		}
	}

	//-----------------------------------------------------------------------------------------------------------------
	// Deinitialization.
	//-----------------------------------------------------------------------------------------------------------------
	public static void Done()
	{
		if (m_Instance != null)
		{
			GameObject.Destroy(m_InstanceOwner);

			m_InstanceOwner = null;
			m_Instance = null;
		}
	}

	//-----------------------------------------------------------------------------------------------------------------
	// Logs user in (via dialog).
	//-----------------------------------------------------------------------------------------------------------------
	public static void LogIn()
	{
	}

	//-----------------------------------------------------------------------------------------------------------------
	// Logs user out.
	//-----------------------------------------------------------------------------------------------------------------
	public static void LogOut()
	{
	}

	//-----------------------------------------------------------------------------------------------------------------
	// Is user logged in?
	//-----------------------------------------------------------------------------------------------------------------
	public static bool IsLoggedIn()
	{
		return false;
	}

	//-----------------------------------------------------------------------------------------------------------------
	// Post given message on user's Twitter wall.
	// 
	// Note: Twitter detects "duplicity" with recent tweets and if message is marked as duplicate than is isn't posted
	//       error (via empty message error) is returned.
	//-----------------------------------------------------------------------------------------------------------------
	public static void PostMessage(string Message, RequestCompletionCallback Callback)
	{
	}

	//-----------------------------------------------------------------------------------------------------------------
	// Post message with image on user's Twitter wall.
	//-----------------------------------------------------------------------------------------------------------------
	public static void PostImage(string Message, string PathToImage)
	{
	}

	//-----------------------------------------------------------------------------------------------------------------
	// Send REST request.
	// - Parameter 'Name' should be URL fragment from the API (excluding https://api.twitter.com) and must request .json!
	// - Parameter 'Type' is request's type (get/post).
	// - Parameter 'Params' is dictionary of key/value pairs for given request.
	// Note: Requests are processed sequentially (new one is initiated after previous one is finished)!
	//-----------------------------------------------------------------------------------------------------------------
	public static void SendRequest(string Name, RequestType Type, Dictionary<string, string> Params, RequestCompletionCallback Callback)
	{
	}

	//-----------------------------------------------------------------------------------------------------------------
	// Opens message composer with given text.
	//-----------------------------------------------------------------------------------------------------------------
	public static void ShowMessageComposer(string Message)
	{
	}
}
