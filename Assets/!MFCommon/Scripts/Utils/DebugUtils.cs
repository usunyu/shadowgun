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

#define DEBUG

using UnityEngine;
using System;
using System.Diagnostics;
using System.Collections.Generic;

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public class MFDebugUtils
{
	[Conditional("DEBUG")]
	public static void Assert(bool condition)
	{
		if (!condition)
		{
			var stackTrace = new StackTrace(true);

			foreach (var r in stackTrace.GetFrames())
			{
				UnityEngine.Debug.LogError("File :" + r.GetFileName() + ", " + r.GetMethod() + " " + r.GetFileLineNumber());
			}

			throw new Exception();
		}
	}

	public static String GetFullName(GameObject inObject, char separator = '/')
	{
		if (inObject)
		{
			if (inObject.transform.parent)
				return GetFullName(inObject.transform.parent.gameObject) + separator + inObject.name;

			return inObject.name;
		}

		return "";
	}

	public const string alpha_big_chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
	public const string alpha_small_chars = "abcdefghijklmnopqrstuvwxyz";
	public const string numbers_chars = "0123456789";
	public const string alpha_num_chars = alpha_big_chars + alpha_small_chars + numbers_chars;

	public static string GetRandomString(int inSize, string inAvailibleChars = alpha_num_chars)
	{
		char[] buffer = new char[inSize];
		for (int i = 0; i < inSize; i++)
		{
			buffer[i] = inAvailibleChars[UnityEngine.Random.Range(0, inAvailibleChars.Length)];
		}
		return new string(buffer);
	}
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// list of "recognized" groups of messages (per user, game-sub-system, etc...)
public enum E_LogGroup
{
	Capa,
	Tomas,
	CapaDebug,
}

public static class Log
{
	#region fields ////////////////////////////////////////////////////////////////////////////////////////////////////

	// allowed "groups" of messages
	static HashSet<E_LogGroup> m_Hash = new HashSet<E_LogGroup>();

	// don't filter errors ?
	static bool m_DontFilterErrors = true;

	#endregion

	#region methods ///////////////////////////////////////////////////////////////////////////////////////////////////

	static Log()
	{
		// list of allowed message groups...
		//m_Hash.Add( E_LogGroup.Capa      );
		//m_Hash.Add( E_LogGroup.CapaDebug );
	}

	public static void Info(E_LogGroup Group, object Msg)
	{
		if (IsAllowed(Group))
		{
			UnityEngine.Debug.Log(Time.timeSinceLevelLoad + " " + Msg);
		}
	}

	public static void Warning(E_LogGroup Group, object Msg)
	{
		if (IsAllowed(Group))
		{
			UnityEngine.Debug.LogWarning(Msg);
		}
	}

	public static void Error(E_LogGroup Group, object Msg)
	{
		if ((m_DontFilterErrors == true) || (IsAllowed(Group) == true))
		{
			UnityEngine.Debug.LogError(Msg);
			//	UnityEngine.Debug.LogWarning( Msg );
		}
	}

	static bool IsAllowed(E_LogGroup Group)
	{
		return m_Hash.Contains(Group);
	}

	#endregion
}
