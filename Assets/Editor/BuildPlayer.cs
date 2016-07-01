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


// DEDICATED SERVER DATA STRIPPING
//
// ATTENTION - this is an advanced but DANGEROUS FEATURE. Make sure you have a backup of the whole project before
// using this feature! SVN local copy or Git repository is a sufficient backup.
//
// All the dedicated server builds can perform a heavy maps and resource pre-processing. Basically, all the non-required
// components and assets are stripped off during the build. That results in dozens of modified assets after the build.
//
// There is a mechanism implemented which should re-import them all back but it is currently broken. The best way to workaround
// it is to forcibly kill the Editor process and launch it back again. If you close the Editor application in a regular way,
// it will save all the modified assets and you will need to revert them back.
//
// All the stripping builds modify PlayerSettings, especially the "Scripting Define Symbols" field which is an unwanted side-effect.
// The good way to a workaround of this issue is to keep a backup of the $ProjectDir/ProjectSettings/ProjectSettings.asset file.
//
// Uncomment the following line if you want to allow stripped-data builds
// #define STRIPPED_BUILDS_ALLOWED

using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections.Generic;


// =====================================================================================================================
// =====================================================================================================================
public partial class BuildPlayer
{
	enum E_ApplicationType
	{
		Unknown,
		Matchmaking,
		DedicatedServer,
		AndroidClient,
		IOSClient,
		ServerMonitor,
		CloudInfo,
		WebPlayerClient,
		PCClient,
		OSXClient,
	};
	
	enum E_BuildType
	{
		Universal
	};
	
	static E_ApplicationType    m_ActiveApplicationBuildType = E_ApplicationType.Unknown;
	static List<string>			m_ModifiedAssets = new List<string>();
	static bool					m_ReloadModifiedAssetsAfterBuild = true;
	static bool					m_StripOffData = false;

	static private BuildTargetGroup BuildTargetToGroup(BuildTarget target)
	{
		switch (target)
		{
		case BuildTarget.Android:                  return BuildTargetGroup.Android;
//		case BuildTarget.FlashPlayer:              return BuildTargetGroup.FlashPlayer;
		case BuildTarget.iOS:                   return BuildTargetGroup.iOS;
//		case BuildTarget.MetroPlayerARM:           return BuildTargetGroup.Unknown;
//		case BuildTarget.MetroPlayerX64:           return BuildTargetGroup.Unknown;
//		case BuildTarget.MetroPlayerX86:           return BuildTargetGroup.Unknown;
		case BuildTarget.WSAPlayer:              return BuildTargetGroup.Unknown;
		case BuildTarget.WP8Player:                return BuildTargetGroup.Unknown;
//		case BuildTarget.NaCl:                     return BuildTargetGroup.NaCl;
		case BuildTarget.PS3:                      return BuildTargetGroup.PS3;
		case BuildTarget.StandaloneGLESEmu:        return BuildTargetGroup.Standalone;
		case BuildTarget.StandaloneLinux:          return BuildTargetGroup.Standalone;
		case BuildTarget.StandaloneLinux64:        return BuildTargetGroup.Standalone;
		case BuildTarget.StandaloneLinuxUniversal: return BuildTargetGroup.Standalone;
		case BuildTarget.StandaloneOSXIntel:       return BuildTargetGroup.Standalone;
		case BuildTarget.StandaloneWindows:        return BuildTargetGroup.Standalone;
		case BuildTarget.StandaloneWindows64:      return BuildTargetGroup.Standalone;
		case BuildTarget.WebPlayer:                return BuildTargetGroup.WebPlayer;
		case BuildTarget.WebPlayerStreamed:        return BuildTargetGroup.WebPlayer;
//		case BuildTarget.Wii:                      return BuildTargetGroup.Wii;
		case BuildTarget.XBOX360:                  return BuildTargetGroup.XBOX360;
		default:                                   return BuildTargetGroup.Unknown;
		}
	}

	// =================================================================================================================
	// This is dummy function used by Build pipline for reinitialize projects. refresh new assets, recompile, ...
	static void BuildDummy()
	{
		Debug.Log("BuildDummy DONE.");
	}
	
	
	
	// =================================================================================================================
	// === MAIN build function =========================================================================================
	static private string Build( E_ApplicationType inType, string[] levels, string locationPathName, BuildTarget target, BuildOptions options, string defineSymbols)
	{
		// this list will be filled in PostProcess scene callback, 
		// so we will clear it here for sure
		m_ModifiedAssets.Clear();
		
		// set active application type.
		// PostProcess scene callbacks will now what is actual building
		m_ActiveApplicationBuildType = inType;
		
		// Set define symbols for compiler
		BuildTargetGroup targetGroup = BuildTargetToGroup(target);
		string prevDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
		PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, defineSymbols);
		
		// This function will create the Real player.
		string retv = BuildPipeline.BuildPlayer( levels, locationPathName, target, options);
		
		PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, prevDefineSymbols);
		
		// reset active application type
		m_ActiveApplicationBuildType = E_ApplicationType.Unknown;

		// Reload modified persistant files if needed.
		if(m_ReloadModifiedAssetsAfterBuild == true)
		{
			bool reportProblem = false;
			Debug.Log(" !!! Force reload modified assets !!! " );
			
			foreach(string assetPath in m_ModifiedAssets)
			{
				if(string.IsNullOrEmpty(assetPath))
					continue; 

				try
				{
					System.IO.File.SetLastWriteTime(assetPath, System.DateTime.Now);

				}
				catch (System.Exception e)
				{
					Debug.Log("Unable to restore modified asset: " + e.ToString() + " " + assetPath);
					reportProblem = true;
				}
				//AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
			}
			
			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

			if (reportProblem)
			{
				EditorUtility.DisplayDialog("Asset reimport problem", "Some of the modified assets were not re-imported and thus they stay modified. Check the editor log for more details.", "Ok");
			}
		}
		
		// clear modified assets
		m_ModifiedAssets.Clear();		
		
		// end, return error string if exist.
		return retv;
	}
	


	// =================================================================================================================
	// === build menu items ============================================================================================
	
	static string GetPlayerFileName( string name, BuildTarget target )
	{
		switch (target)
		{
		case BuildTarget.StandaloneWindows: return name += ".exe";
		case BuildTarget.StandaloneWindows64: goto case BuildTarget.StandaloneWindows;
		case BuildTarget.StandaloneOSXIntel: return name += ".app";
		default: return name;
		}
	}
	
	static void BuildDedicatedServerWorker(string outputDirectory, string binaryName, BuildTarget target, BuildOptions options, string defineSymbols, bool stripData)
	{
		string infoPrefix = "==== Dedicated server build ";
		Debug.Log(infoPrefix + "started.");

		// .............................................
		// get scenes for dedicated server...
		List<string> listOfScenes = GetScenesForBuild( E_ApplicationType.DedicatedServer );
		if(listOfScenes.Count <= 0)
		{
			Debug.LogWarning(infoPrefix + "List of scenes is empty");
			return;
		}
		Debug.Log(infoPrefix + ", Building Scenes: " + ConvertStringArrayToString( listOfScenes.ToArray() ) );

		// .............................................
		// prepare output directory...
		System.IO.Directory.CreateDirectory( outputDirectory );
		Debug.Log(infoPrefix + "Output directory: " + outputDirectory );
		
		// .............................................
		// prepare player name...
		string playerName = outputDirectory + GetPlayerFileName(binaryName, target);
		Debug.Log(infoPrefix + "Player name: " + playerName );

		// .............................................
		//AndroidPublishSettings.ApplySecretSettings( outputDirectory + "Settings.json");
		m_StripOffData = stripData;
		Build(E_ApplicationType.DedicatedServer, listOfScenes.ToArray(), playerName, target, options, defineSymbols);
		m_StripOffData = false;

		Debug.Log(infoPrefix + "DONE.");
	}

	[MenuItem("Build/Dedicated Server Win32")]
	static void BuildDedicatedServer()
	{
		BuildDedicatedServerWorker("../_Packages/DedicatedServer/", "dz-server-dev", BuildTarget.StandaloneWindows, BuildOptions.None, "DEADZONE_DEDICATEDSERVER;IAP_USE_MFLIVE", false);
	}

#if STRIPPED_BUILDS_ALLOWED
	[MenuItem("Build/Dedicated Server Win32 - STRIPPED")]
	static void BuildDedicatedServerStripped()
	{
		if( DedicatedServerWarnMessage() )
		{
			BuildDedicatedServerWorker("../_Packages/DedicatedServer/", "dz-server-dev", BuildTarget.StandaloneWindows, BuildOptions.None, "DEADZONE_DEDICATEDSERVER;IAP_USE_MFLIVE", true);
		}
	}
#endif

	[MenuItem("Build/Dedicated Server Linux x86_64")]
	static void BuildDedicatedServerLinux()
	{
		BuildDedicatedServerWorker("../_Packages/DedicatedServer_Linux64/", "dz-server-dev", BuildTarget.StandaloneLinux64, BuildOptions.None, "DEADZONE_DEDICATEDSERVER", false);
	}
	
#if STRIPPED_BUILDS_ALLOWED
	[MenuItem("Build/Dedicated Server Linux x86_64 - STRIPPED")]
	static void BuildDedicatedServerLinuxStripped()
	{
		if( DedicatedServerWarnMessage() )
		{
			BuildDedicatedServerWorker("../_Packages/DedicatedServer_Linux64/", "dz-server-dev", BuildTarget.StandaloneLinux64, BuildOptions.None, "DEADZONE_DEDICATEDSERVER", true);
		}
	}
#endif

	[MenuItem("Build/Dedicated Server Mac OSX")]
	static void BuildDedicatedServerMacOSX()
	{
		BuildDedicatedServerWorker("../_Packages/DedicatedServer_MacOSX/", "dz-server-dev", BuildTarget.StandaloneOSXIntel, BuildOptions.None, "DEADZONE_DEDICATEDSERVER", false);
	}

#if STRIPPED_BUILDS_ALLOWED
	[MenuItem("Build/Dedicated Server Mac OSX - STRIPPED")]
	static void BuildDedicatedServerMacOSXStripped()
	{
		if( DedicatedServerWarnMessage() )
		{
			BuildDedicatedServerWorker("../_Packages/DedicatedServer_MacOSX/", "dz-server-dev", BuildTarget.StandaloneOSXIntel, BuildOptions.None, "DEADZONE_DEDICATEDSERVER", true );
		}
	}
#endif

#if STRIPPED_BUILDS_ALLOWED
	[ MenuItem( "Build/Dedicated server stripping preview" ) ]
	static void DedicatedServerScenePreview()
	{
		if( DedicatedServerWarnMessage() )
		{
			StripOffDataForDedicatedServer();
		}
	}
#endif
	
	// function with name BuildClient is used in our build utils...
	static void BuildClient()	
	{
		BuildAndroidClientDefault();
	}
	
	[MenuItem("Build/Android Client")]
	static void BuildAndroidClientDefault()
	{
		BuildAndroidClient("DEADZONE_CLIENT");
	}

	static void BuildAndroidClient(string defineSymbols = "", E_BuildType buildType = E_BuildType.Universal)
	{
		if(EditorUserBuildSettings.selectedBuildTargetGroup != BuildTargetGroup.Android)
		{
			Debug.LogError("Invalid build target group (" + EditorUserBuildSettings.selectedBuildTargetGroup + ") \n."+
						   "In this moment we are able to build only android clients...");
			return;
		}

		/*if(EditorUserBuildSettings.androidBuildSubtarget != AndroidBuildSubtarget.DXT)
		{
			Debug.LogError("Invalid android sub target (" + EditorUserBuildSettings.androidBuildSubtarget + ") \n."+
						   "In this moment we support only DXT android clients...");
			return;
		}*/

		// .............................................
		// Start to build player...
		string infoPrefix = "==== Client build ";
		Debug.Log(infoPrefix + "started.");


		// .............................................
		// get scenes for dedicated server...
		List<string> listOfScenes = GetScenesForBuild( E_ApplicationType.AndroidClient, buildType );
		if(listOfScenes.Count <= 0)
		{
			Debug.LogWarning(infoPrefix + "List of scenes is empty");
			return;
		}
		Debug.Log(infoPrefix + ", Building Scenes: " + ConvertStringArrayToString( listOfScenes.ToArray() ) );


		// .............................................
		// prepare output directory...
		MobileTextureSubtarget androidSubTarget = EditorUserBuildSettings.androidBuildSubtarget;
		string subTarget  = androidSubTarget.ToString().ToLower();
		string buildName = buildType != E_BuildType.Universal ? ("_" + buildType.ToString()) : string.Empty;
			
		string outputDirectory  = "../_Packages/AndroidPlayer_" + subTarget + buildName + "/";
		System.IO.Directory.CreateDirectory( outputDirectory );

		Debug.Log(infoPrefix + "Output directory: " + outputDirectory );


		// .............................................
		// prepare player name...
		string playerName = outputDirectory + "DeadZone_" + subTarget + buildName + ".apk";
		Debug.Log(infoPrefix + "Player name: " + playerName );


		// .............................................
		// apply settings from
		ApplyAndroidSecretSettings(outputDirectory + "Settings.json");

		// if we are using split apk add loader level
		if( PlayerSettings.Android.useAPKExpansionFiles == true )
		{
			listOfScenes.Insert(0, "MadFinger Assets/Levels/Loader.unity");
		}

		Build(E_ApplicationType.AndroidClient, listOfScenes.ToArray(), playerName, BuildTarget.Android, BuildOptions.None, defineSymbols);

		Debug.Log(infoPrefix + "DONE." );		
	}

	[MenuItem("Build/iOS Client")]
	static void BuildIOSClientDefault()
	{
		// Currently we need to swap uLobby.dll with it's client part
		string uLobbyClientFilePath = Application.dataPath + "/Plugins/uLobby/Extras/ClientDLL/uLobby";
		string uLobbyFilePath = Application.dataPath + "/Plugins/uLobby/Assembly/uLobby.dll";
		
		string[] disabledDlls = {
			uLobbyFilePath,
			Application.dataPath + "/Jboy/Library/Jboy.Core.dll",
			Application.dataPath + "/Jboy/Library/Jboy.dll"
		};

		foreach (var dll in disabledDlls)
		{
			System.IO.File.Move(dll, dll + ".orig");
			System.IO.File.Move(dll + ".meta", dll + ".orig.meta");
		}
		
		System.IO.File.Move(uLobbyClientFilePath + ".meta", uLobbyFilePath + ".meta");
		System.IO.File.Move(uLobbyClientFilePath,           uLobbyFilePath);
		
		// Build client with proper defines in order not to compile server source files
		BuildIOSClient("DEADZONE_CLIENT");
		
		// And swap back
		System.IO.File.Move(uLobbyFilePath + ".meta", uLobbyClientFilePath + ".meta");
		System.IO.File.Move(uLobbyFilePath,           uLobbyClientFilePath);
		
		foreach (var dll in disabledDlls)
		{
			System.IO.File.Move(dll + ".orig", dll);
			System.IO.File.Move(dll + ".orig.meta", dll + ".meta");
		}
	}
	
	static void BuildIOSClient(string defineSymbols = "", string buildName = "")
	{
		if(EditorUserBuildSettings.selectedBuildTargetGroup != BuildTargetGroup.iOS)
		{
			Debug.LogError("Invalid build target group (" + EditorUserBuildSettings.selectedBuildTargetGroup + ") \n.");
			return;
		}

		// .............................................
		// Start to build player...
		string infoPrefix = "==== Client build ";
		Debug.Log(infoPrefix + "started.");


		// .............................................
		// get scenes for dedicated server...
		List<string> listOfScenes = GetScenesForBuild( E_ApplicationType.IOSClient );
		if(listOfScenes.Count <= 0)
		{
			Debug.LogWarning(infoPrefix + "List of scenes is empty");
			return;
		}
		Debug.Log(infoPrefix + ", Building Scenes: " + ConvertStringArrayToString( listOfScenes.ToArray() ) );


		// .............................................
		// prepare output directory...
		if (buildName.Length > 0)
		{
			buildName = "_" + buildName;
		}
			
		string outputDirectory  = "../_Packages/iOSPlayer" + buildName + "/";
		System.IO.Directory.CreateDirectory( outputDirectory );

		Debug.Log(infoPrefix + "Output directory: " + outputDirectory );
		
		// .............................................
		// prepare player name...
		string playerName = outputDirectory + "DeadZone" + buildName;
		Debug.Log(infoPrefix + "Player name: " + playerName );

		Build(E_ApplicationType.IOSClient, listOfScenes.ToArray(), playerName, BuildTarget.iOS, BuildOptions.None, defineSymbols);

		Debug.Log(infoPrefix + "DONE." );	
	}
	
	public static readonly string OSX_CLIENT__CFBundleName = 			"SHADOWGUN: DeadZone";
	public static readonly string OSX_CLIENT__CFBundleDisplayName = 	OSX_CLIENT__CFBundleName;
	public static readonly string OSX_CLIENT__ProductName = 			"SHADOWGUN DeadZone";
	public static readonly string OSX_CLIENT__BundleVersion = 			"2.2.2";
	public static readonly string OSX_CLIENT__LSMinimumSystemVersion = 	"10.7";
	
	
	[MenuItem("Build/OS X Client")]
	static void BuildOSXClient()
	{
		if(EditorUserBuildSettings.selectedBuildTargetGroup != BuildTargetGroup.Standalone)
		{
			Debug.LogError("Invalid build target group (" + EditorUserBuildSettings.selectedBuildTargetGroup + ") \n.");
			return;
		}

		Debug.Log("==== OS X Client build started.");
		
		// .............................................
		// get scenes for OS X Client...
		List<string> listOfScenes = GetScenesForBuild( E_ApplicationType.OSXClient );
		if(listOfScenes.Count <= 0)
		{
			Debug.LogWarning("==== OS X Client build List of scenes is empty");
			return;
		}


		// .............................................
		// prepare output directory...
		string outputDirectory = System.Environment.GetFolderPath( System.Environment.SpecialFolder.Desktop ); //"../_Packages/OSXClient/";
		System.IO.Directory.CreateDirectory( outputDirectory );
		
		
		// .............................................
		// prepare player name...
		string playerName = System.IO.Path.Combine( outputDirectory, OSX_CLIENT__ProductName + ".app" );
		
		
		// .............................................
		// defines...
		string defineSymbols = "MADFINGER_KEYBOARD_MOUSE";
		
		
		// backup and product name
		string prevProductName = PlayerSettings.productName;
		PlayerSettings.productName = OSX_CLIENT__ProductName;
		
		// backup and set bundle version
		string prevBundleVersion = PlayerSettings.bundleVersion;
		PlayerSettings.bundleVersion = OSX_CLIENT__BundleVersion;

		// backup and set AppStore validation
		bool prevMacAppStoreValidation = PlayerSettings.useMacAppStoreValidation;
		PlayerSettings.useMacAppStoreValidation = true;

		Build(E_ApplicationType.OSXClient, listOfScenes.ToArray(), playerName, BuildTarget.StandaloneOSXIntel, BuildOptions.None, defineSymbols);

		// restore previously backed-up player settings
		PlayerSettings.bundleVersion = prevBundleVersion;
		PlayerSettings.productName = prevProductName;
		PlayerSettings.useMacAppStoreValidation = prevMacAppStoreValidation;

		Debug.Log("==== OS X Client build DONE.");
	}
	

	[MenuItem("Build/Win32 Client")]
	static void BuildPCClient()
	{
		if (EditorUserBuildSettings.selectedBuildTargetGroup != BuildTargetGroup.Standalone)
		{
			Debug.LogError("Invalid build target group (" + EditorUserBuildSettings.selectedBuildTargetGroup + ") \n.");
			return;
		}

		Debug.Log("==== Win32 Client build started.");

		//backup various settings which use to be changed during the process
		string prevScriptingDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);

		// get scenes for PC Client...
		List<string> listOfScenes = GetScenesForBuild(E_ApplicationType.PCClient);
		if(listOfScenes.Count <= 0)
		{
			Debug.LogWarning("==== Windows Client build List of scenes is empty");
			return;
		}

		// prepare output directory...
		string outputDirectory  = "../_Packages/Win32Client/";
		System.IO.Directory.CreateDirectory(outputDirectory);

		// prepare player name...
		string playerName = System.IO.Path.Combine(outputDirectory, "DeadZone.exe");

		// defines...
		string defineSymbols = "DEADZONE_CLIENT;MADFINGER_KEYBOARD_MOUSE;IAP_USE_MFLIVE";	

		Build(E_ApplicationType.PCClient, listOfScenes.ToArray(), playerName, BuildTarget.StandaloneWindows, BuildOptions.None, defineSymbols);

		//restore previously backed-up settings
		PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, prevScriptingDefineSymbols);

		Debug.Log("==== Win32 Client build DONE.");
	}


	// =================================================================================================================
	// === PostProcess scene function for dedicated server =============================================================
	[PostProcessScene]
	public static void OnPostprocessScene()
	{
		if (m_StripOffData)
		{
			StripOffDataForDedicatedServer();
		}
	}

	static void StripOffDataForDedicatedServer()
	{
		// ...................................................
		// destroy scene objects
		DestroyObjectsForDedicatedServer();
		
		// ...................................................
		// disable ALL Renderers - this parameter should be false
		// until Unity guys will fix issue with Renderer leaks.
		// Now it will modify most of prefabs on disk
		DisableRenderers(false);
		
		// testing - for real build DontModifyPersistentAssets should be true
		bool DontModifyPersistentAssets = true;
		
		// ...................................................
		// destroy loaded, non-persistent Renderers
		DestroyComponents<Renderer>(DontModifyPersistentAssets);
		
		// ...................................................
		// destroy loaded, non-persistent MeshFilters
		DestroyComponents<MeshFilter>(true);
		
		// ...................................................
		// destroy loaded, non-persistent Animatios
		DestroyComponents<Animation>(true);
		
		// ...................................................
		// destroy loaded, non-persistent OcclusionAreas
		DestroyComponents<OcclusionArea>(DontModifyPersistentAssets);
		
		// ...................................................
		// destroy loaded, non-persistent ParticleSystem
		DestroyComponents<ParticleSystem>(DontModifyPersistentAssets);
		
		// ...................................................
		// destroy loaded, non-persistent Skyboxes
		DestroyComponents<Skybox>(DontModifyPersistentAssets);
		
		#if UNITY_MFG
		DestroyComponents<FluidSurface>( DontModifyPersistentAssets );
		#endif
		
		// optional 
		
		// todo : solve warning - Can't remove AudioSource because Mission (Script) depends on it UnityEngine.Object:DestroyImmediate(Object)
		// DestroyComponents<AudioSource>( true ); 
		// DestroyComponents<OcclusionPortal>( true );
		// DestroyComponents<FluidSurface>( true );
	}

	// =================================================================================================================
	// === PostProcess scene function for client =======================================================================
    [PostProcessScene]
    public static void OnPostprocessScene_client()
    {
		if(m_ActiveApplicationBuildType != E_ApplicationType.AndroidClient)
			return;
    	
		Renderer[] allLoadedComponents = Resources.FindObjectsOfTypeAll( typeof( Renderer ) ) as Renderer[];
		
		int skipped  = 0;
		int modified = 0;
		
		// ...................................................
		// disable shadow on loaded meshes
		foreach( Renderer r in allLoadedComponents )
		{            
			if( r != null )
			{
				// skip persistant renderers (which are not in scene
				if( true == EditorUtility.IsPersistent( r.gameObject ))
				{
					skipped++;
					continue;
				}
			
				if(r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off || r.receiveShadows == true)
				{
					r.shadowCastingMode    = UnityEngine.Rendering.ShadowCastingMode.Off;
					r.receiveShadows = false;
					//Debug.Log("modify " + r.name);
					modified++;
				}				
			}
		}
		
		Debug.Log("OnPostprocessScene " + EditorApplication.currentScene + " - Modify " + typeof( Renderer ) + " : " + ", " + allLoadedComponents.Length + " all, " + skipped + " skipped, " + modified + " modified.");
	}

	// =================================================================================================================
	// === PostProcess scene function for GUI ==========================================================================
	[PostProcessScene]
	static void OnPostprocessScene_GUI()
	{
		// ...................................................
		// dedicated server doesn't use GUI...
		if(m_ActiveApplicationBuildType == E_ApplicationType.DedicatedServer)
			return;
		
#if false // this code causes inconsistent button states
		// ...................................................
		// prepare button's idle state
		{
			int  skipped = 0;
			int modified = 0;
			
			GUIBase_Button[] controls = Resources.FindObjectsOfTypeAll( typeof( GUIBase_Button ) ) as GUIBase_Button[];
			foreach( var control in controls )
			{
				if( EditorUtility.IsPersistent( control.gameObject ) == true || control.idleSprite != null )
				{
					++skipped;
					continue;
				}

				control.CreateIdleSpriteIfNeeded();
				++modified;
			}
	
			/*if( EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android || EditorUserBuildSettings.activeBuildTarget == BuildTarget.iPhone )
			{
				string[] parts = {
					controls.Length + " checked",
					skipped         + " skipped",
					modified        + " modified"
				};
				Debug.Log("OnPostprocessScene " + EditorApplication.currentScene + " - Modify " + typeof( GUIBase_Button ) + " : " + string.Join(", ", parts) + ".");
			}*/
		}
#endif

		// ...................................................
		// prepare multisprite's states
		{
			int modified = 0;
			
			GUIBase_MultiSprite[] controls = Resources.FindObjectsOfTypeAll( typeof( GUIBase_MultiSprite ) ) as GUIBase_MultiSprite[];
			foreach( var control in controls )
			{
				control.PrepareStates();
				++modified;
			}
	
			/*if( EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android || EditorUserBuildSettings.activeBuildTarget == BuildTarget.iPhone )
			{
				string[] parts = {
					controls.Length + " checked",
					modified        + " modified"
				};
				Debug.Log("OnPostprocessScene " + EditorApplication.currentScene + " - Modify " + typeof( GUIBase_MultiSprite ) + " : " + string.Join(", ", parts) + ".");
			}*/
		}

		// ...................................................
		// remove obsolete components
		{
			int elementsSkipped    = 0;
			int audioSourceRemoved = 0;
			int animationRemoved   = 0;
			
			GUIBase_Element[] elements = Resources.FindObjectsOfTypeAll( typeof( GUIBase_Element ) ) as GUIBase_Element[];
			foreach( var element in elements )
			{
				if ( EditorUtility.IsPersistent( element.gameObject ) == true )
				{
					++elementsSkipped;
					continue;
				}
				
				AudioSource tempAudio = element.GetComponent<AudioSource>();
				if( tempAudio != null )
				{
					if (tempAudio.clip == null)
					{
						Object.DestroyImmediate( tempAudio, true );
						++audioSourceRemoved;
					}
				}
				
				Animation tempAnim = element.GetComponent<Animation>();
				if( tempAnim != null )
				{
					if (tempAnim.clip == null && tempAnim.GetClipCount() == 0)
					{
						Object.DestroyImmediate( tempAnim );
						++animationRemoved;
					}
				}
			}
			
			/*if( EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android || EditorUserBuildSettings.activeBuildTarget == BuildTarget.iPhone )
			{
				string[] parts = {
					elements.Length    + " elements checked",
					elementsSkipped    + " elements skipped",
					audioSourceRemoved + " audio sources removed",
					animationRemoved   + " animations removed"
				};
				Debug.Log("OnPostprocessScene " + EditorApplication.currentScene + " - Clean " + typeof( GUIBase_Element ) + " : " + string.Join(", ", parts) + ".");
			}*/

			if(Application.isPlaying == false)
			{
				MFFontManager[] fontManagers = Resources.FindObjectsOfTypeAll ( typeof( MFFontManager ) ) as MFFontManager[];
				foreach (MFFontManager fm in fontManagers)
				{
					if(false == EditorUtility.IsPersistent( fm.gameObject ))
					{
						//Debug.Log("Asset path: " + AssetDatabase.GetAssetOrScenePath(fm));
						fm.EDITOR_PostProcessScene();
					}
				}
			}
		}
	}

	private static void RecordModifiedAsset(GameObject gameObject)
	{
		string assetPath = AssetDatabase.GetAssetPath(gameObject);
		if (string.IsNullOrEmpty(assetPath))
			return;

		if (m_ModifiedAssets.Contains(assetPath))
			return;
		
		// TODO: Since an unknown 4.x Unity version, we do have game objects which refers somewhere into Library folder
		// to invalid filenames like "Library/unity editor resources"
		if (!System.IO.File.Exists(assetPath))
			return;
		
		m_ModifiedAssets.Add(assetPath);
	}
	
	// =================================================================================================================
	// === disable loaded, non-persistent Renderers ====================================================================
	private static void DisableRenderers( bool DontModifyPersistentAssets )
	{
		// ...................................................
		// get all loaded renderers...
		Object[] allLoadedRenderers = Resources.FindObjectsOfTypeAll(typeof(Renderer));
		//Debug.Log("OnPostprocessScene: " + EditorApplication.currentScene + ", Renderers: " + allLoadedRenderers.Length);


//		// ...................................................
//		// print name and game object full name of all renderers
//		// which will be disabled...
//		List<string> rendererNames = new List<string>();
//      foreach( Object obj in allLoadedRenderers)
//      {
//          Renderer renderer =  obj as Renderer;
//          rendererNames.Add(renderer.name + ", " + renderer.gameObject.GetFullName());
//      }
//		Debug.Log("OnPostprocessScene: " + "Renderers to deactivate: " + ConvertStringArrayToString( rendererNames.ToArray() ) );

		int skipped = 0;
		// ...................................................
		// disable all loaded renderers
        foreach( Object obj in allLoadedRenderers )
        {
            Renderer renderer = obj as Renderer;
            
			if(true == EditorUtility.IsPersistent( renderer.gameObject ))
			{
				if( DontModifyPersistentAssets )
				{
					skipped++;
					continue;
				}
				else
				{
					RecordModifiedAsset(renderer.gameObject);
				}
			}
			
            renderer.enabled = false;
        }
        
		Debug.Log("OnPostprocessScene: " + EditorApplication.currentScene + ", " + allLoadedRenderers.Length + " Renderers, " + skipped + " skipped.");
	}
	
	// =================================================================================================================
	// === Delete scene objects based for dedicated server  ============================================================
	private static void DestroyObjectsForDedicatedServer()
	{
		int destroyedObjects = 0;
		int pass = 0;
		do
		{
			int destroyed = DestroyObjectsForDedicatedServerWorker();
			
			if( destroyed > 0 )
			{
				pass++;
				destroyedObjects += destroyed;
			}
			else
			{
				break;
			}
		}
		while( true );
		
		Debug.Log("OnPostprocessScene: " + destroyedObjects + " objects was destroyed in " + pass + " passes");
	}
	
	// =================================================================================================================
	// === Delete scene objects based for dedicated server  ============================================================
	private static int DestroyObjectsForDedicatedServerWorker( )
	{
		System.Type [] typesToDestroy = { 
			typeof( MeshRenderer ),				typeof( MeshFilter ),		typeof( MeshRenderer ),		typeof( MeshFilter ),		
			typeof( Transform ),				typeof( LensFlare ), 		typeof( Light ),			typeof( AudioSource ),
			typeof( OcclusionArea ),			typeof( Animation ),		typeof( Camera ),			typeof( ParticleSystem ),
			typeof( ParticleSystemRenderer ),	typeof( LightProbeGroup ),	typeof( SpectatorCamera ),	typeof( DisableBasedOnPerformance ),
			typeof( GUIBase_Widget ),			typeof( GUIBase_Label ),	typeof( GUIBase_Button ),	typeof( GUIBase_Sprite ),
			typeof( GUIBase_Enum ),				typeof( GUIBase_Layout ),	typeof( GUIBase_Platform ),	typeof( DominationMapDefinition ),
			typeof( RotateObject ),				typeof( SpectatorCamera ),	typeof( AudioListener )
		};
				
		Object[] objects = Resources.FindObjectsOfTypeAll( typeof( GameObject ) );
		
		List<Object> objectList = new List<Object>();
		
		for( int i = 0; i < objects.Length; i++ )
		{
			GameObject o = objects[i] as GameObject;
			
			if( null == o || o.transform.childCount > 0 || true == EditorUtility.IsPersistent( o ) )
			{
				continue;
			}
			
			Component [] components = o.GetComponents<Component>();
			
			if( null != components )
			{
				bool ok = true;
				
				foreach ( Component c in components )
				{
					System.Type t = c.GetType();
					
					bool typeOK = false;
					
					foreach( System.Type type in typesToDestroy )
					{
						if( t == type )
						{
							typeOK = true;
							break;
						}
					}
					
					if( !typeOK )
					{
						ok = false;
						break;
					}
					
					if( t == typeof( Light ) )
					{
						Light l = c as Light;
						
						if( null != l )
						{
							if( l.type == LightType.Directional )
							{
								ok = false;
								break;
							}
						}
					}
				}
				
				if( ok )
				{
					objectList.Add( o );
				}
			}
		}
		
		foreach( GameObject o in objectList )
		{
			Object.DestroyImmediate( o, false );
		}
		
		return objectList.Count;
	}
		
	
	
	
	// =================================================================================================================
	// === disable loaded, non-persistent components of type T =========================================================
	private static void DestroyComponents<T>( bool DontModifyPersistentAssets )
	{
		T[] allLoadedComponents = Resources.FindObjectsOfTypeAll( typeof( T ) ) as T[];
		//Debug.Log( "OnPostprocessScene: " + EditorApplication.currentScene + ", Meshes: " + allLoadedMeshes.Length );
		
		int skipped = 0;
		int destroyed = 0;
		
		// ...................................................
		// disable loaded meshes
		foreach( T obj in allLoadedComponents )
		{            
			Component C = obj as Component;
			
			if( C != null && true == EditorUtility.IsPersistent( C.gameObject ))
			{
				if( DontModifyPersistentAssets )
				{
					skipped++;
					continue;
				}
				else
				{
					RecordModifiedAsset(C.gameObject);
				}
			}
			
			if( C != null )
			{
				Object.DestroyImmediate( C, true );
				destroyed++;
			}
		}
		
		Debug.Log("OnPostprocessScene " + EditorApplication.currentScene + " - destroy " + typeof( T ) + " : " + ", " + allLoadedComponents.Length + " all, " + skipped + " skipped, " + destroyed + " destroyed.");
	}

	// =================================================================================================================
	// === utility functions ===========================================================================================
	static List<string> GetScenesForBuild(E_ApplicationType inTarget, E_BuildType buildType = E_BuildType.Universal)
	{
		// ...................................................
		// get scenes from editor build settings,
		// exclude disabled ones and scenes which are not levels...
		List<string> listOfScenes = new List<string>();

		// ...................................................
		// add some global scenes...
		listOfScenes.Add("assets/Levels/Multiplayer/mp_core.unity");
		listOfScenes.Add("assets/Levels/Multiplayer/mp_vortex.unity");
		listOfScenes.Add("Assets/Levels/empty.unity");

		// ...................................................
		// add per target scenes...
		switch(inTarget)
		{
		case E_ApplicationType.DedicatedServer:
			listOfScenes.Insert(0, "Assets/Levels/DedicatedServer.unity");
			break;
		case E_ApplicationType.WebPlayerClient:
//			listOfScenes.Add("Assets/Levels/InitGame.unity");
			goto case E_ApplicationType.AndroidClient;
			
		case E_ApplicationType.OSXClient:
			goto case E_ApplicationType.AndroidClient;
			
		case E_ApplicationType.PCClient:
			goto case E_ApplicationType.AndroidClient;
			
		case E_ApplicationType.AndroidClient:
			listOfScenes.Add("Assets/Levels/Gui_16_9.unity");
			//listOfScenes.Add("assets/Levels/Multiplayer/loading.unity");
			//listOfScenes.Add("assets/Levels/Multiplayer/loadingcustomize.unity");
			//listOfScenes.Add("assets/Levels/Multiplayer/loadinggetpremium.unity");
			listOfScenes.Add("assets/Levels/LoadingToGame.unity");
			listOfScenes.Add("assets/Levels/LoadingFromGame.unity");
			listOfScenes.Add("Assets/Levels/MainMenu.unity");
			listOfScenes.Insert(0, "Assets/Levels/InitGame.unity");
			break;
		case E_ApplicationType.IOSClient:
			goto case E_ApplicationType.AndroidClient;
		default:
			Debug.LogError("Unknown target. Abort !!!");
			return null;
		}

		return listOfScenes;
	}

	static string ConvertStringArrayToString(string[] array)
    {
		// Concatenate all the elements into a StringBuilder.
		//
		System.Text.StringBuilder builder = new System.Text.StringBuilder();
		builder.Append("\n");
		foreach (string value in array)
		{
		    builder.Append(value);
		    builder.Append(",\n");
		}

		return builder.ToString();
    }
	
	// =================================================================================================================
	// === helper method to prevent of modifying scene and prefabs accidentally ========================================
	static bool DedicatedServerWarnMessage()
	{
		string text = "This action will modify your current scene (without possibility of Undo) and also most of your prefabs.";
		
		return UnityEditor.EditorUtility.DisplayDialog( "Kindly warning", text, "OK, let's do it", "Cancel" );
	}
}
