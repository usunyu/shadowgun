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
using System.Collections.Generic;

[RequireComponent(typeof (AudioSource))]
public class Mission : MonoBehaviour
{
	// GUI	
	public enum E_GuiState
	{
		E_GS_NONE,
		E_GS_HUD,
		E_GS_INGAME_MENU,
		E_GS_SNIFF_MINIGAME,
	}

	public GameZoneMP GameZone;

	public ExplosionCache ExplosionCache = new ExplosionCache();

	public int LeaderBoardID;

	public AudioClip RoundStart;
	public AudioClip RoundEnd;

	public static Mission Instance;

	// Use this for initialization
	protected void Awake()
	{
		Instance = this;

		Random.seed = System.DateTime.Now.Second;
	}

	protected void Start()
	{
		//mute audio until we laod gui
		AudioListener.pause = true;
		AudioListener.volume = 0;

		if (Game.Instance)
		{
			Game.Instance.GameState = E_GameState.Game;
			Game.Instance.CurrentLevel = ApplicationDZ.loadedLevelName;
			Game.Instance.LeaderBoardID = LeaderBoardID;
		}

		ExplosionCache.Init();
		Shader.WarmupAllShaders();

		if (Game.Instance && Game.Instance.AppType == Game.E_AppType.DedicatedServer)
			return;

		LoadGui();

		Invoke("WaitForLogGui", 0.1f);
	}

	protected void OnDestroy()
	{
// destry data for mission..

		ExplosionCache.Clear();
		ExplosionCache = null;
	}

	public void Reset()
	{
		GameZone.Reset();
	}

	void WaitForLogGui()
	{
		// TODO - potrebuji robustni test, jestli uz byl level obsahujici GUI doloadovan a vse z nej bylo inicializovano
		// To co nasleduje rozhodne ROBUSTNI test neni
		// snazim se najit root platformu a pokud tam je, mela by byt zaregistrovana (ze sveho Startu), podivam se, jestli se uz updatla
		// To znamena, ze vse uz je loadle, zaregistrovane a uz se updatuje...

		// Debug.Log("mission waitfor gui");

		GUIBase_Platform p = MFGuiManager.Instance.FindPlatform("Gui_16_9");

		if (p && p.IsInitialized())
		{
			GuiHUD.StoreControlsPositions();
			StartCoroutine(PrepareForStart());
		}
		else
		{
			Invoke("WaitForLogGui", 0.1f);
		}
	}

	IEnumerator PrepareForStart()
	{
		//GuiManager.Instance.Reset(); //TODO: je treba volat reset do playercontrols na tlacitka?

		StartCoroutine(FadeInAudio_Corout(1, GuiOptions.soundVolume));

		yield return new WaitForSeconds(1.0f);

		//if (GuiHUD.Instance)
		//    GuiHUD.Instance.ShowMessageTimed(GuiHUD.E_HudMessageType.E_001_GameLoaded, GuiHUD.InfoMessageTime);
	}

	void LoadGui()
	{
		ApplicationDZ.LoadLevelAdditive("Gui_16_9");
	}

	protected IEnumerator FadeInAudio_Corout(float fadeTime, float targetVolume)
	{
		float beginFadeTime = Time.realtimeSinceStartup;

		AudioListener.volume = 0;
		AudioListener.pause = false;

		while ((Time.realtimeSinceStartup - beginFadeTime) <= fadeTime)
		{
			float volume = targetVolume*Mathf.Max((Time.realtimeSinceStartup - beginFadeTime)/fadeTime, 1.0f);

			AudioListener.volume = volume;

			yield return new WaitForEndOfFrame();
		}

		AudioListener.volume = targetVolume;
	}

	void OnApplicationFocus(bool focus)
	{
		// When the application is switched to the fullscreen mode (web player on Mac OS X)
		// this method is called with focus == false.
		// Focus must be set to true.
		if (Application.platform == RuntimePlatform.OSXWebPlayer)
		{
			if (Screen.fullScreen)
			{
				focus = true;
			}
		}

		if (uLink.Network.isClient == false)
			return;

		if (focus == false)
		{
			// loosing focus
			// we can't pause the mutiplayer game here, we will just mute the audio			

			if (uLink.Network.isClient == true)
			{
				AudioListener.volume = 0.0f;
			}

			// TODO: we should switch player's character into a thinking/bussy mode to notify other player that our player is not currently playing the game
		}
		else
		{
			AudioListener.volume = GuiOptions.soundVolume;
		}
		/*		 
        if (GuiHUD.Instance != null)
        {
            if (Game.Instance.GameState != E_GameState.IngameMenu)
            {
                //GuiFrontendIngame.Instance.DisableRestartButton(true);
                GuiHUD.Instance.SwitchToIngameMenu();
            }
        }
        AudioListener.pause = true;
        */
	}

	void OnApplicationPause(bool focus)
	{
		if (uLink.Network.isClient == false)
		{
			//server or cell server
			return;
		}

#if UNITY_EDITOR
		if (focus)
			return;
#endif
		//GuiFrontendIngame.Instance.DisableRestartButton(true);
		GuiFrontendIngame.ShowPauseMenu();

		AudioListener.pause = true;
	}
}
