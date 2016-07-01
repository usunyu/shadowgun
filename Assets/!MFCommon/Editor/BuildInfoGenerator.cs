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
using UnityEditor;
using UnityEditor.Callbacks;
using System;
using System.IO;
using System.Xml;
using Microsoft.Win32;



public class BuildInfoGenerator
{
	static private bool                  regenerate = true;
	static private BuildInfo.VersionInfo versionInfo;
	static private BuildInfo.DateInfo    dateInfo;
	
	[PostProcessScene]
	public static void OnPostProcessScene()
	{
		// generate data for game info if needed
		if (regenerate == true)
		{
			GenerateVersionInfo();
			GenerateDateTime();
			
			regenerate = false;
		}
		
		// generate game object now
		CreateDummy();
	}

	[PostProcessBuild]
	public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
	{
		// regenerate data next time
		regenerate = true;
	}

	private static void CreateDummy()
	{
		//Debug.Log(">>>> BuildInfoGenerator.Generate() :: regenerate="+regenerate+", currentScene="+EditorApplication.currentScene);
		
		// create dummy object for game info
		GameObject go = new GameObject("BuildInfo");
		go.isStatic = true;
		go.SetActive(true);
		
		// setup build info
		BuildInfo builInfo = go.AddComponent<BuildInfo>();
		builInfo.Init(versionInfo, dateInfo);
		builInfo.enabled = true;
	}
	
	private static void GenerateVersionInfo()
	{
		string[] values = PlayerSettings.bundleVersion.Split('.');
		
		BuildInfo.Stage stage = BuildInfo.Stage.Release;
		if (values.Length > 0)
		{
			string value = values[values.Length - 1];
			for (var idx = BuildInfo.Stage.Beta; idx <= BuildInfo.Stage.Development; ++idx)
			{
				string stageStr = BuildInfo.ToString(idx);
				if (value.EndsWith(stageStr) == true)
				{
					values[values.Length - 1] = value.Substring(0, value.Length - stageStr.Length);
					stage = idx;
					break;
				}
			}
		}

		versionInfo = new BuildInfo.VersionInfo() {
			Major    = values.Length > 0 ? int.Parse(values[0]) : 1,
			Minor    = values.Length > 1 ? int.Parse(values[1]) : 0,
			Build    = values.Length > 2 ? int.Parse(values[2]) : 0,
			Revision = GetCurrentRevision(),
#if UNITY_ANDROID
			Code     = PlayerSettings.Android.bundleVersionCode,
#else
			Code     = 0,
#endif
			Stage    = stage
		};
	}

	private static void GenerateDateTime()
	{
		DateTime dateTime = DateTime.UtcNow;
		
		dateInfo = new BuildInfo.DateInfo() {
			Year   = dateTime.Year,
			Month  = dateTime.Month,
			Day    = dateTime.Day,
			Hour   = dateTime.Hour,
			Minute = dateTime.Minute,
			Second = dateTime.Second
		};
	}
	
	private static string SpawnSytemCommand(string filename, string arguments, string workingDirectory = "")
	{
		string output = null;
		string errors = null;
		
		try
		{
			var startInfo = new System.Diagnostics.ProcessStartInfo();
			
			startInfo.FileName			     = filename;
			startInfo.Arguments			     = arguments;
			startInfo.UseShellExecute	     = false;
			startInfo.CreateNoWindow         = true;
			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardError  = true;
		
			if (workingDirectory != string.Empty)
			{
				startInfo.WorkingDirectory = workingDirectory;
			}
			
			using (var exeProcess = System.Diagnostics.Process.Start(startInfo))
			{
				exeProcess.WaitForExit();
				output = exeProcess.StandardOutput.ReadToEnd();
				errors = exeProcess.StandardError.ReadToEnd();
			}
		}
		catch
		{
			string[] lines = {
				"Error spawning proccess:",
				"  " + filename + " " + arguments,
				errors
			};
			errors = string.Join(System.Environment.NewLine, lines);
		}
		finally
		{
			if (!string.IsNullOrEmpty(errors))
			{
				Debug.LogWarning(errors);
				output = null;
			}
		}
		
		return output;
	}
	
	private static int GetCurrentRevision()
	{
		return Directory.Exists(Path.GetFullPath(Path.Combine(Application.dataPath, "../.svn"))) ? GetSvnRevision() : GetGitSvnRevision();
	}

	// When connected to the repository by "git svn" client
	private static int GetGitSvnRevision()
	{
		// The binary is missing and the code got rotten. All it does, it generates the following warning message:
		// "Error spawning proccess:"
		/*
		string result = SpawnSytemCommand("git", "svn info", Path.GetFullPath(Application.dataPath));
		int revision = 0;
		
		if (result != null)
		{
			System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(result, "Revision:[ \t]*([0-9]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
		
			revision = match.Success && match.Groups.Count > 1 ? int.Parse(match.Groups[1].Value) : 0;
		}
		
		return revision;
		*/
		return 0;
	}
	
	// When connected to the repository by a regular svn client
	public static int GetSvnRevision()
	{
		string output = GetSvnRevisionInfo(false);
		if (output == null)
		{
			output = GetSvnRevisionInfo(true);
		}
		
		return output != null ? ParseSvnRevisionInfo(output) : 0;
	}
	
	private static string GetSvnRevisionInfo(bool useOldClient)
	{
		string clientpath = DeduceSvnClient(useOldClient);
		if (clientpath == null)
			return null;
		
		string[] args = {
			"info",
			// svn requires login when accessing revision HEAD
			// so it causes a little bit a problem with SyncroSVN client
			//UsesSyncroSVN() == true ? "" : "--revision BASE",
			"--xml",
			"\"" + Path.GetFullPath(Path.Combine(Application.dataPath, "..")) + "\""
		};
		
		return SpawnSytemCommand(clientpath, string.Join(" ", args));
	}
	
	private static string DeduceSvnClient(bool useOldClient)
	{
		if (Environment.OSVersion.Platform == PlatformID.Unix)
		{
			return "svn";
		}
		else
		{
			var key = Registry.LocalMachine.OpenSubKey(@"Software\TortoiseSVN");
			if (key != null)
			{
				string dir = key.GetValue("Directory").ToString();
				if (File.Exists(Path.Combine(dir, "bin/svn.exe")) == true)
				{
					return Path.Combine(dir, "bin/svn.exe");
				}
			}
			
			var versionedFile = Path.Combine("Editor/bin", Path.Combine(useOldClient == true ? "svn17" : "svn18", "svn.exe"));
			var mfCommonFile  = Path.Combine(Path.Combine(Application.dataPath, "!MFCommon"), versionedFile);
			if (File.Exists(mfCommonFile) == true)
			{
				return mfCommonFile;
			}
			else
			{
				return Path.Combine(Application.dataPath, versionedFile);
			}
		}
	}
	
	private static int ParseSvnRevisionInfo(string text)
	{
		try
		{
			XmlDocument xml = new XmlDocument();
			xml.LoadXml(text);
				
			XmlNodeList list = xml.GetElementsByTagName("commit");
			if (list.Count > 0)
			{
				XmlNode revision = list[0].Attributes["revision"];
				return revision != null ? int.Parse(revision.Value) : 0;
			}
		}
		catch
		{
			string[] lines = {
				"Error parsing svn info:",
				text
			};
			Debug.LogWarning(string.Join(System.Environment.NewLine, lines));
		}
			
		return 0;
	}
	
//	private static bool UsesSyncroSVN()
//	{
//#if !UNITY_STANDALONE_OSX && !UNITY_IPHONE
//		var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\3000-9469-2694-4689");
//		return key != null ? true : false;
//#else
//		return true;
//#endif
//	}
	
}

