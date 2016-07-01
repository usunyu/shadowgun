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

// #define DEBUG 

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net;

public class Game : MonoBehaviour
{
	public const string PrimaryProductID = "ShadowgunMP";

	public enum E_AppType
	{
		DedicatedServer,
		Game,
	}

	public E_AppType AppType = E_AppType.Game;

	public int ManualViewIDWorkaround;

	public Client Client;
	//public Server Server;

	string _CurrentLevel;

	int _Score = 0;
	float TimeToClearHits;

	E_GameState _GameState = E_GameState.Game; // for editor
	E_GameType _GameType = E_GameType.Multiplayer; //for editor game play

	static Game _Instance = null;

	//player progress
	public int Score
	{
		get { return _Score; }
		set { _Score = value; }
	}

	public int LeaderBoardID;

	public string CurrentLevel
	{
		get { return _CurrentLevel; }
		set { _CurrentLevel = value; }
	}

	public E_GameState GameState
	{
		get { return _GameState; }
		set { _GameState = value; }
	}

	public E_GameType GameType
	{
		get { return _GameType; }
	}

	public float DontHealTime
	{
		get { return 10; }
	}

	public float HealingModificator
	{
		get { return 10; }
	}

	public static Game Instance
	{
		get { return _Instance; }
	}

	public bool GamepadConnected { get; private set; }

	public float LastTouchControlTime { get; private set; }

	public static string CachedJoystickName { get; private set; }

	public bool GameLog = false;

	public static bool canAskForReview = false;

#if UNITY_ANDROID && !UNITY_EDITOR
	private bool m_IsFocused = true;
#endif

	public static UserSettings Settings = UserSettings.Empty;

	public RoundFinalResult RoundResults = new RoundFinalResult();

	[System.Serializable]
	class LoadingScreens
	{
		public string[] ForMenu = new string[0];
		public string[] ForMatch = new string[0];
	}
	[SerializeField] LoadingScreens loadingScreens = new LoadingScreens();

	public string LoadingScreen
	{
		get
		{
			string[] screens = ApplicationDZ.loadedLevelName == MainMenuLevelName ? loadingScreens.ForMatch : loadingScreens.ForMenu;
			return screens != null && screens.Length > 0 ? screens[(int)Random.Range(0, screens.Length)] : "empty";
		}
	}

	public bool IsLoading { get; private set; }

	public string DownloadingLevel { get; private set; }

	public RoundFinalResult LastRoundResult { get; set; }

	string GetName()
	{
		return transform.parent ? (transform.parent.name + name) : name;
	}

	void OnEnable()
	{
	}

	void OnLevelWasLoaded(int level)
	{
		Debug.Log("Loaded level " + level);

		if (Instance != null && Instance != this)
		{
			DestroyImmediate(gameObject);
			return;
		}

		Debug.Log("OnLevelWasLoaded ApplicationDZ.loadedLevelName " + ApplicationDZ.loadedLevelName);

		// Don't process new purchases on the background during gameplay. This reduces number of Cloud
		// verification requests and also prevents updating a PPI when not in the main menu.
		InAppPurchaseMgr.Instance.enabled = ApplicationDZ.loadedLevelName.Equals(MainMenuLevelName);
	}

	void Awake()
	{
		BuildInfo.DrawVersionInfo = false;

		// 1366x720=983520
		// 1024x720=737280
		// 900x640=614400
		//Screen.SetResolution (1024, 540, true);

		//new Texture().SetAnisotropicFilteringLimits(1,1);//2,2);

		//
		// warmup post-fx shaders
		//
		/*
        if (iPhoneSettings.generation == iPhoneGeneration.iPodTouch3Gen || iPhoneSettings.generation == iPhoneGeneration.iPodTouch4Gen || iPhoneSettings.generation == iPhoneGeneration.iPhone3GS || iPhoneSettings.generation == iPhoneGeneration.iPad1Gen)
            QualitySettings.masterTextureLimit = 1;

        if (iPhoneSettings.generation == iPhoneGeneration.iPad2Gen)
            QualitySettings.antiAliasing = 4;
        */

		//CamExplosionFXMgr.PreloadResources();

		GraphicsDetailsUtl.DisableShaderKeyword("UNITY_IPHONE");

#if UNITY_IPHONE
		GraphicsDetailsUtl.EnableShaderKeyword("UNITY_IPHONE");
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
		try
		{
			MogaGamepad.Init();
			MogaGamepad.OnConnectionChange += OnMogaConnectionChange;
			MogaGamepad.OnBatteryLowChange = OnMogaBatteryLowChange;
			
			if(m_IsFocused)
			{
				//Debug.Log("Moga: Application focused");
				MogaGamepad.OnFocus(m_IsFocused);
			}
		}
		catch(System.Exception) {}
#endif

		QualitySettings.masterTextureLimit = 0;

		if (_Instance)
		{
			if (_Instance.Client == null)
				_Instance.Client = Client;
			Destroy(this.gameObject);
			return;
		}
		else
		{
			_Instance = this;
			uLink.NetworkView nw = GetComponent<uLink.NetworkView>();
			if (nw != null)
			{
				// This workaround fixes the warning about alredy registered manual ID
				// Now the NetworkView is registered only once (using given ManualViewIDWorkaround).
				nw.SetManualViewID(ManualViewIDWorkaround);
			}
		}

		CloudUser.authenticationChanged += OnUserAuthenticationChanged;

		GamepadInputManager.CreateInstance();

		DontDestroyOnLoad(this);
		this.transform.parent = null;

		InputManager.Initialize();

		if (AppType == E_AppType.DedicatedServer)
			Application.targetFrameRate = 60;
		else
			Application.targetFrameRate = 30;

		// test server optimization
		// not really great optimization I would say... (our servers were definitelly more laggy)
		// Changing back to 60FPS on the server
		//Application.targetFrameRate = 30;

		uLink.BitStreamCodec.Add<PlayerPersistantInfo>(PlayerPersistantInfo.Deserialize, PlayerPersistantInfo.Serialize);
		uLink.BitStreamCodec.Add<PPIInventoryList>(PPIInventoryList.Deserialize, PPIInventoryList.Serialize);
		uLink.BitStreamCodec.Add<PPIEquipList>(PPIEquipList.Deserialize, PPIEquipList.Serialize);
		uLink.BitStreamCodec.Add<PPIRoundScore>(PPIRoundScore.Deserialize, PPIRoundScore.Serialize);
		uLink.BitStreamCodec.Add<PPIUpgradeList>(PPIUpgradeList.Deserialize, PPIUpgradeList.Serialize);
		uLink.BitStreamCodec.Add<PPIOutfits>(PPIOutfits.Deserialize, PPIOutfits.Serialize);
		uLink.BitStreamCodec.Add<RoundFinalResult>(RoundFinalResult.Deserialize, RoundFinalResult.Serialize);
		uLink.BitStreamCodec.AddAndMakeArray<RoundFinalResult.PlayerResult>(RoundFinalResult.PlayerResult.Deserialize,
																			RoundFinalResult.PlayerResult.Serialize);

#if UNITY_IPHONE || UNITY_ANDROID
		Screen.autorotateToPortrait = false;
		Screen.autorotateToPortraitUpsideDown = false;
#endif

		Screen.sleepTimeout = 120;

		//
		// experimental networking stuff
		//

		if (AppType == E_AppType.Game && Application.isEditor == false)
		{
			// initialize plugins
			EtceteraWrapper.Init();

			if (TwitterConfiguration.IsAvailable)
			{
				TwitterWrapper.Init(TwitterConfiguration.CustomerKey, TwitterConfiguration.CustomerSecret);
			}

			if (ChartBoostConfiguration.IsAvailable)
			{
				ChartBoost.init(ChartBoostConfiguration.AppId, ChartBoostConfiguration.AppSignature);
			}

			if (TapJoyConfiguration.IsAvailable)
			{
				TapJoy.Init(TapJoyConfiguration.AppId, TapJoyConfiguration.SecurityKey);
			}
		}

		// Synchronize item settings managers with cloud
		SettingsCloudSync.GetInstance().UpdateSettingsManagersFromCloud();

		LastTouchControlTime = 0;
	}

	void OnDestroy()
	{
		if (_Instance == this)
		{
			// shutdown plugins
			EtceteraWrapper.Done();

			TapJoy.Done();
			TwitterWrapper.Done();

#if UNITY_ANDROID && !UNITY_EDITOR		
			MogaGamepad.Done();
			MogaGamepad.OnConnectionChange -= OnMogaConnectionChange;
			MogaGamepad.OnBatteryLowChange = null;
#endif

			GamepadInputManager.DestroyInstance();

			CloudUser.authenticationChanged -= OnUserAuthenticationChanged;
			OnUserAuthenticationChanged(false);

			//FacebookPlugin.Instance.OnApplicationFocus -= OnApplicationPause;  kua ja to nemuzu delat ;)
		}
	}

	void OnUserAuthenticationChanged(bool state)
	{
		if (state == true)
		{
			Settings = UserSettings.Load(CloudUser.instance.primaryKey);
			GuiOptions.Load();

			PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();
			ppi.PrimaryKey = CloudUser.instance.primaryKey;
			ppi.PrimaryKeyHash = CloudServices.CalcHash64(ppi.PrimaryKey);

			if (ppi.Experience == 0 && PlayerPrefs.HasKey("firstrun") == false)
			{
				ppi.IsFirstRun = true;
				PlayerPrefs.SetInt("firstrun", 0);
			}

			TapJoy.ConnectUser(ppi.PrimaryKey);
		}
		else
		{
			SaveSettings();
			TapJoy.DisconnectUser();
		}
	}

	public static bool IsSmallDevice()
	{
		//Debug.Log("w:" + Screen.width + "h:" + Screen.height);
		return (Mathf.Max(Screen.width, Screen.height) < 1024);
	}

	public void Start()
	{
		//Debug.Log(Time.realtimeSinceStartup + "Game::Start,  sleepTimeout " + Screen.sleepTimeout);

		//na tebletech spust corutinu ktera bude testovat pripojeni gamepadu. V editoru spoustej test vzdycky
#if !UNITY_EDITOR
		if(!IsSmallDevice())
#endif
		StartCoroutine(CheckGamepadConnected());

#if !DEADZONE_DEDICATEDSERVER
		StartCoroutine(InitInAppPurchases());
#endif //!DEADZONE_DEDICATEDSERVER

		FacebookPlugin.Instance.OnHideUnityEvent += OnHideUnity;
		FacebookPlugin.Instance.OnHideUnityEvent += OnApplicationPause;
	}

	public void OnHideUnity(bool isGameShown)
	{
	}

	IEnumerator InitInAppPurchases()
	{
		while (!CloudUser.instance.isUserAuthenticated)
		{
			yield return new WaitForEndOfFrame();
		}

		while (!SettingsCloudSync.GetInstance().isDone)
		{
			yield return new WaitForEndOfFrame();
		}

		List<InAppInitRequest> iapList = new List<InAppInitRequest>();
		FundSettings[] items = FundSettingsManager.Instance.GetAll();

		foreach (FundSettings s in items)
		{
			if (s.DISABLED)
				continue;

			if (s.AddGold > 0 || s.AddMoney > 0)
			{
				iapList.Add(new InAppInitRequest
				{
					ProductId = s.GUID.ToString(),
					ProductName = ShopDataBridge.GetFundName(s),
					ProductType = InAppProductType.Consumable
				});
			}
		}

		InAppPurchaseMgr.Instance.Init(iapList.ToArray());
	}

	public static bool IsHDResolution()
	{
		int w = Mathf.Max(Screen.width, Screen.height);
		int h = Mathf.Min(Screen.width, Screen.height);
		//Debug.Log("Resolution " + w + " x " + h);
		if (w > 960 || h > 640)
			return true;
		else
			return false;
	}

	public void Save_Save()
	{
	}

	public void Save_Clear()
	{
	}

	public void Save_Load()
	{
	}

	public void LoadMainMenu()
	{
		if (GameState == E_GameState.MainMenu)
			return;

		Time.timeScale = 1.0f;

		StartCoroutine(LoadMainMenuScene());
	}

	IEnumerator LoadMainMenuScene()
	{
		if (ApplicationDZ.loadedLevelName != MainMenuLevelName)
		{
			if (GuiFrontendIngame.IsVisible == true || GuiFrontendIngame.IsHudVisible == true)
			{
				GuiFrontendIngame.HideAll();
			}

			StartCoroutine(LoadScene(MainMenuLevelName, false));
		}

		while (GuiFrontendMain.IsInitialized == false)
		{
			yield return new WaitForEndOfFrame();
		}

		GameState = E_GameState.MainMenu;
	}

	public static string MainMenuLevelName
	{
		get { return "MainMenu"; }
	}

	public void StartNewMultiplayerGame(uLink.HostData hostData,
										int joinRequestId,
										Client.ConnectToServerFailedDelegate onConnectToServerFailed = null)
	{
		_GameType = E_GameType.Multiplayer;

		Instantiate(Client);

		Client.Instance.ConnectToServer(hostData, joinRequestId, onConnectToServerFailed);
	}

	public void StartNewMultiplayerGame(IPEndPoint EndPoint,
										int joinRequestId,
										Client.ConnectToServerFailedDelegate onConnectToServerFailed = null)
	{
		_GameType = E_GameType.Multiplayer;

		Instantiate(Client);

		Client.Instance.ConnectToServer(EndPoint, joinRequestId, onConnectToServerFailed);
	}

	public void StartTutorial()
	{
		_GameType = E_GameType.Tutorial;
		_GameState = E_GameState.Tutorial;

		CurrentLevel = "tutorial";

		StartCoroutine(LoadScene(CurrentLevel, false));
	}

	public void StartSaleScreens()
	{
		return;
	}

	public void LoadLevel(string nextLevel)
	{
		//PlayerPersistantInfo.SynchronizeDataFromPlayer(Player.LocalInstance);

		CurrentLevel = nextLevel;
		StartCoroutine(LoadScene(CurrentLevel, true));
	}

	IEnumerator LoadScene(string scene, bool mission)
	{
		IsLoading = true;

		string sceneLower = scene.ToLower();
		if (sceneLower.StartsWith("mp_"))
			scene = sceneLower;

		// save user settings
		SaveSettings();

		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame(); // waiting for 2 frames is necessary here!!!

		// display loading screen
		{
			// fade to black
			MFGuiFader.FadeIn(MFGuiFader.NORMAL);
			while (MFGuiFader.Fading == true)
				yield return new WaitForEndOfFrame();

			// do some cleaning and display loading screen
			ClearInstances();
			if (AppType == E_AppType.DedicatedServer)
				ApplicationDZ.LoadLevel("Empty");
			else
				ApplicationDZ.LoadLevel(LoadingScreen);

			// prepare fade-out
			MFGuiFader.FadeOut(MFGuiFader.NORMAL);
			MFGuiFader.Paused = true;

			// wait two frame
			// this causes much nicer initial delta time for fading
			yield return new WaitForEndOfFrame();
			yield return new WaitForEndOfFrame();

			// finish fade-out
			MFGuiFader.Paused = false;
			while (MFGuiFader.Fading == true)
				yield return new WaitForEndOfFrame();
		}

		// apply graphic settings
		if (DeviceInfo.PerformanceGrade != (DeviceInfo.Performance)GuiOptions.graphicDetail)
		{
			DeviceInfo.Initialize((DeviceInfo.Performance)GuiOptions.graphicDetail);
		}

		// load scene
		{
			// do another cleaning and load scene
			ClearInstances();
			yield return new WaitForEndOfFrame();
			yield return new WaitForEndOfFrame(); // waiting for 2 frames is necessary here!!!
			yield return Resources.UnloadUnusedAssets();
			yield return new WaitForEndOfFrame();
			ApplicationDZ.LoadLevel(scene);

			// prepare fade-out
			MFGuiFader.FadeOut(MFGuiFader.SLOW);
			MFGuiFader.Paused = true;

			// waint two frames so game has time to initialize
			yield return new WaitForEndOfFrame();
			yield return new WaitForEndOfFrame();
			//yield return Resources.UnloadUnusedAssets();
		}

		if (mission)
		{
			if (Client.Instance)
				Client.Instance.OnMissionLoaded(scene);
#if !DEADZONE_CLIENT
            if (Server.Instance)
                Server.Instance.OnMissionLoaded(scene);
#endif
		}
		System.GC.Collect();

		// clear input buffers
		InputManager.FlushInput();

		// loading finished
		IsLoading = false;
		DownloadingLevel = null;

		// wait one frame
		// this causes much nicer initial delta time for fading
		yield return new WaitForEndOfFrame();

		// finish fade-out
		MFGuiFader.Paused = false;
		while (MFGuiFader.Fading == true)
			yield return new WaitForEndOfFrame();
	}

	public void TryToCleanSomeMemory()
	{
		System.GC.Collect();
//        Resources.UnloadUnusedAssets();
	}

/*
    private IEnumerator LogLeakedObjects()
    {
        yield return new WaitForSeconds(3);

		Names = null;
		LogLeaked();

        yield return new WaitForEndOfFrame();

		yield return StartCoroutine(LoadScene("level_01"));
			yield return new WaitForSeconds(5);
//		yield return StartCoroutine(LoadScene("level_02"));
//			yield return new WaitForSeconds(5);
//		yield return StartCoroutine(LoadScene("level_03"));
//			yield return new WaitForSeconds(5);
//		yield return StartCoroutine(LoadScene("level_04"));
//			yield return new WaitForSeconds(5);
//		yield return StartCoroutine(LoadScene("level_05"));
//			yield return new WaitForSeconds(5);
//		yield return StartCoroutine(LoadScene("level_06"));
//			yield return new WaitForSeconds(5);
//		yield return StartCoroutine(LoadScene("level_07"));
//			yield return new WaitForSeconds(5);
//		yield return StartCoroutine(LoadScene("level_08"));
//			yield return new WaitForSeconds(5);
//		yield return StartCoroutine(LoadScene("level_09"));
//			yield return new WaitForSeconds(5);
//		yield return StartCoroutine(LoadScene("level_10"));
//			yield return new WaitForSeconds(5);

		yield return StartCoroutine(LoadScene("Empty"));

		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();
		yield return Resources.UnloadUnusedAssets();

		LogLeaked();
    }
*/

	void ClearInstances()
	{
		InputManager.FlushInput();

		AgentActionFactory.Clear();
		Player.Clear();
		GuiFrontendIngame.Clear();

		if (GameCamera.Instance)
		{
			GameCamera.Instance.StopAllCoroutines();
			GameCamera.Instance.CancelInvoke();
			GameCamera.Instance = null;
		}
		if (CombatEffectsManager.Instance)
		{
			CombatEffectsManager.Instance.StopAllCoroutines();
			CombatEffectsManager.Instance.CancelInvoke();
			CombatEffectsManager.Instance = null;
		}
		if (ProjectileManager.Instance)
		{
			ProjectileManager.Instance.StopAllCoroutines();
			ProjectileManager.Instance.CancelInvoke();
			ProjectileManager.Instance = null;
		}
		if (Mission.Instance)
		{
			Mission.Instance.StopAllCoroutines();
			Mission.Instance.CancelInvoke();
			Mission.Instance = null;
		}
		if (BloodFXManager.Instance)
		{
			BloodFXManager.Instance.StopAllCoroutines();
			BloodFXManager.Instance.CancelInvoke();
			BloodFXManager.Instance = null;
		}
		if (CamExplosionFXMgr.Instance)
		{
			CamExplosionFXMgr.Instance.StopAllCoroutines();
			CamExplosionFXMgr.Instance.CancelInvoke();
			CamExplosionFXMgr.Instance = null;
		}
		if (WeaponManager.Instance)
		{
			WeaponManager.Instance.StopAllCoroutines();
			WeaponManager.Instance.CancelInvoke();
			WeaponManager.Instance = null;
		}

		if (GuiAchievements.Instance)
		{
			GuiAchievements.Instance.StopAllCoroutines();
			GuiAchievements.Instance.CancelInvoke();
			GuiAchievements.Instance = null;
		}
		//if (GuiCustomizeControls.Instance) { GuiCustomizeControls.Instance.StopAllCoroutines(); GuiCustomizeControls.Instance.CancelInvoke(); GuiCustomizeControls.Instance = null; }
		if (GuiEquipMenu.Instance)
		{
			GuiEquipMenu.Instance.StopAllCoroutines();
			GuiEquipMenu.Instance.CancelInvoke();
			GuiEquipMenu.Instance = null;
		}
		if (GuiSubtitlesRenderer.Instance)
		{
			GuiSubtitlesRenderer.Instance.StopAllCoroutines();
			GuiSubtitlesRenderer.Instance.CancelInvoke();
			GuiSubtitlesRenderer.Instance = null;
		}

		MFFontManager.Release();
		if (MFGuiManager.Instance)
		{
			MFGuiManager.Instance.StopAllCoroutines();
			MFGuiManager.Instance.CancelInvoke();
			MFGuiManager.Instance = null;
		}
	}

	void Update()
	{
		if (IsLoading == true)
			return;

		InputManager.Update();
		GamepadInputManager.Update();

#if UNITY_ANDROID && !UNITY_EDITOR		
		MogaGamepad.Update();		
#endif

		if (Input.touchCount != 0)
		{
			LastTouchControlTime = Time.timeSinceLevelLoad;
		}
	}

	void LateUpdate()
	{
		// Workaround for Unity shader system _Time not working in certain cases		
		Shader.SetGlobalFloat("_GlobalTime", Time.time);

		/*if (Time.frameCount % 30 == 0)
		{// TESTING !!!!
			System.GC.Collect();
		}*/
	}

	void FixedUpdate()
	{
		//////// screen orientation
		if ((Input.deviceOrientation == DeviceOrientation.LandscapeLeft) && (Screen.orientation != ScreenOrientation.LandscapeLeft))
			Screen.orientation = ScreenOrientation.LandscapeLeft;
		else if ((Input.deviceOrientation == DeviceOrientation.LandscapeRight) && (Screen.orientation != ScreenOrientation.LandscapeRight))
			Screen.orientation = ScreenOrientation.LandscapeRight;
	}

	void OnApplicationQuit()
	{
		SaveSettings();

		ClearInstances();

		InputManager.Deinitialize();
	}

	public void OnApplicationFocus(bool focus)
	{
		//Debug.Log( "Game OnApplicationFocus focus:" + focus );

		// When the application is switched to the fullscreen mode (web player on Mac OS X)
		// this method is called with focus == false.
		// Focus must be set to true otherwise InputManager is disabled
		// and mouse contol is not working.
		if (Application.platform == RuntimePlatform.OSXWebPlayer)
		{
			if (Screen.fullScreen)
			{
				focus = true;
			}
		}

#if UNITY_ANDROID && !UNITY_EDITOR
		m_IsFocused = focus;
		Debug.Log("Moga: Application focused " + focus);
		MogaGamepad.OnFocus(focus);
#endif

		if (focus == false)
		{
			InputManager.FlushInput();
		}

#if !UNITY_EDITOR
		InputManager.IsEnabled = focus;
#endif
	}

	void OnApplicationPause(bool pause)
	{
		if (pause == true)
		{
			InputManager.FlushInput();

			SaveSettings();
		}

		if (pause == false)
		{
			//Pro Tapjoy a SponsorPay: kdyz se vracime do applikace a je zobrazene menu, updatuj ppi (aby hrac videl pridane goldy). 
			if (GuiFrontendMain.IsVisible || GuiFrontendIngame.IsVisible)
			{
				if (CloudUser.instance != null && CloudUser.instance.isUserAuthenticated)
				{
					//Debug.Log("On application pause false, fetching ppi");
					FetchPlayerPersistantInfo action = new FetchPlayerPersistantInfo(CloudUser.instance.authenticatedUserID);
					GameCloudManager.AddAction(action);
				}
			}
		}
	}

	void SaveSettings()
	{
		GuiOptions.Save();
		Settings.Save();
	}

	IEnumerator CheckGamepadConnected()
	{
		while (true)
		{
			if (GamepadInputManager.Instance != null)
			{
				bool newConnection = DetectGpadConnection();

				//Debug.Log("Gamepad connected: " + GamepadConnected + " new: " + newConnection );
				if (GamepadConnected != newConnection)
				{
					//Debug.Log("Gamepad connectin changed: " + GamepadConnected + " new: " + newConnection);
					GamepadConnected = newConnection;
					CachedJoystickName = CurrentJoystickName();

#if UNITY_EDITOR
					//OnMogaConnectionChange(newConnection); //for Moga gui testing
#endif

					GamepadInputManager.Instance.SetConfig(CachedJoystickName);
				}
			}
			yield return new WaitForSeconds(2.5F);
		}
	}

	public static bool DetectGpadConnection()
	{
		string[] js = Input.GetJoystickNames();
		return (js != null && js.Length > 0);
	}

	public static string CurrentJoystickName()
	{
		string[] js = Input.GetJoystickNames();
		if (js != null && js.Length > 0)
		{
			if (js.Length > 1)
			{
				Debug.LogWarning("More then 1 gamepad connected, getting name of first.");
			}

			return js[0];
		}
		else
			return null;
	}

	public static string GetPlayerName(uLink.NetworkPlayer player)
	{
		PlayerPersistantInfo p = PPIManager.Instance.GetPPI(player);

		if (p != null)
			return p.Name;

		return "Unknown";
	}

	public static void AskForReview()
	{
		if (canAskForReview == true)
		{
			EtceteraWrapper.AskForReview("DEAD ZONE",
										 TextDatabase.instance[00100023],
										 TextDatabase.instance[00100024],
										 TextDatabase.instance[00100025],
										 TextDatabase.instance[00100026],
										 2,
										 5);
		}
	}

#if UNITY_ANDROID // && !UNITY_EDITOR		

	public void OnMogaConnectionChange(bool connected)
	{
		if (GuiMogaPopup.Instance != null)
		{
			const int strMogaConnected = 02900030;
			const int strMogaDisconnected = 02900031;

			if (connected)
			{
				if (GuiOptions.showMogaHelp)
				{
					bool showSwitch = (CloudUser.instance != null && CloudUser.instance.isUserAuthenticated);
					GuiMogaPopup.Instance.ShowHelp(showSwitch);
				}
				else
					GuiMogaPopup.Instance.Show(strMogaConnected, 2.0f);
			}
			else
			{
				GuiMogaPopup.Instance.HideHelp();
				GuiMogaPopup.Instance.Show(strMogaDisconnected, 2.0f);
			}

			if (GuiHUD.Instance)
				GuiHUD.Instance.UpdateGadgetSelection();
		}
		else
			Debug.Log("GuiMogaPopup instance is not initialised");
	}

	public void OnMogaBatteryLowChange(bool batteryLow)
	{
		if (GuiMogaPopup.Instance != null)
		{
			const int strMogaLowBatttery = 02900032;
			if (batteryLow)
				GuiMogaPopup.Instance.Show(strMogaLowBatttery, 8.0f);
			else
				GuiMogaPopup.Instance.Hide();
		}
	}
#endif

	public static E_MPGameType GetMultiplayerGameType()
	{
		if (uLink.Network.isServer)
		{
#if !DEADZONE_CLIENT
			if( null != Server.Instance )
			{
				return Server.Instance.GameInfo.GameType;
			}
#endif
		}
		else
		{
			if (null != Client.Instance)
			{
				return Client.Instance.GameState.GameType;
			}
		}

		return E_MPGameType.None;
	}
}
