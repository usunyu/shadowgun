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
using System.Collections;


//--------------------------------------------------------------------------------------------------
//--------------------------------------------------------------------------------------------------

// If you want to load the scene use this class instead of the Application class.
// If you want to get loadedLevelName use this class.
//
//
// Why you should use this class instead of the Application class?
//
// When you use WWW class to download scenes from the server (*.unity3d file)
// and call Application.LoadLevel after the file is downloaded,
// the Application.loadedLevelName is empty and MonoBehaviour.OnLevelWasLoaded receives -1.
//
// More info at http://forum.unity3d.com/threads/77257-Empty-level-name-and-receives-1-when-MonoBehaviour-OnLevelWasLoaded-called.
//
public class ApplicationDZ
{
	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------
	
	private static string m_LoadedLevelName;
	
	
	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------
	
	public static string loadedLevelName
	{
		get
		{
			if (!System.String.IsNullOrEmpty( Application.loadedLevelName ))
			{
				return Application.loadedLevelName;
			}
			else
			{
				return m_LoadedLevelName;
			}
		}
		
		private set
		{
			m_LoadedLevelName = value;
		}
	}
	
	
	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------
	
	public static void LoadLevel(string name)
	{
		loadedLevelName = name;
		
		Application.LoadLevel( name );
	}
	
	
	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------
	
	public static void LoadLevelAdditive(string name)
	{
		loadedLevelName = name;
		
		Application.LoadLevelAdditive( name );
	}
	
	
	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------
	
	public static AsyncOperation LoadLevelAdditiveAsync(string levelName)
	{
		loadedLevelName = levelName;
		
		return Application.LoadLevelAdditiveAsync( levelName );
	}
	
	
	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------
	
	public static AsyncOperation LoadLevelAsync(string levelName)
	{
		loadedLevelName = levelName;
		
		return Application.LoadLevelAsync( levelName );
	}
	
	
	//--------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------
}
