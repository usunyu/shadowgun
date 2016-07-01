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

#define USE_DEBUG_SERVER_LIST

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LitJson;

[AddComponentMenu("GUI/Frontend/Menus/GuiMenuMain")]
public class GuiMenuMain : GuiMenu
{
	const int TextID_Logout_Caption = 00106003;
	const int TextID_Logout_Text = 00106004;

	// GUIMENU INTERFACE

	protected override void OnMenuInit()
	{
#if USE_DEBUG_SERVER_LIST
		_CreateScreen<ServerListScreen>("PlayServer");
#endif
		//_CreateScreen<HelpScreen>("Help");

		_CreateScreen<GuiShopNotFundsPopup>("NotFundsPopup");
		_CreateScreen<GuiShopBuyPopup>("ShopBuyPopup");
		_CreateScreen<GuiShopMessageBox>("ShopMessageBox");
		_CreateScreen<GuiShopStatusBuy>("ShopStatusBuy");
		_CreateScreen<GuiShopStatusIAP>("ShopStatusIAP");

		_CreateScreen<NewFriendDialog>("NewFriend");
		_CreateScreen<ForgotPasswordDialog>("ForgotPassword");

		GuiUniverse.Instance.Init(this);
	}

	protected override void OnMenuUpdate()
	{
#if UNITY_ANDROID //&& !UNITY_EDITOR
		if (MogaGamepad.IsConnected() && (MogaGamepad.MenuKeyPressed() || MogaGamepad.MenuMovePressed()))
		{
			if (GuiMogaPopup.Instance != null && !(GuiMogaPopup.Instance.IsShown() || GuiMogaPopup.Instance.IsHelpShown()))
				GuiMogaPopup.Instance.Show(02900033, 3.5f);
		}
#endif
	}

	protected override void OnMenuShowMenu()
	{
		// menu background
		{
			GuiUniverse.Instance.Show();
			GuiUniverse.Instance.Enable();
		}

#if ( !UNITY_EDITOR ) && ( UNITY_ANDROID || UNITY_IPHONE )
				//	if (uLink.Network.isServer == false)
				//	{
				//		StartCoroutine(CheckForNewMajorRank());
				//	}
	#endif

		GameCloudManager.AddAction(new UpdateSwearWords(CloudUser.instance.authenticatedUserID));

		Game.AskForReview();
	}

	protected override void OnMenuHideMenu()
	{
		if (GuiUniverse.Instance != null)
		{
			GuiUniverse.Instance.Disable();
			GuiUniverse.Instance.Hide();
		}

		CancelInvoke("WaitForDownloadingAssetsAndShowLobby");
	}

	protected override void OnMenuRefreshMenu(bool anyPopupVisible)
	{
		if (string.IsNullOrEmpty(ActiveScreenName) == true)
		{
			GuiUniverse.Instance.Enable();
		}
		else
		{
			GuiUniverse.Instance.Disable();
		}
	}

#if USE_DEBUG_SERVER_LIST
	protected override string FixActiveScreenName(string screenName)
	{
		if (screenName == "PlayServer")
			return "Lobby";
		return base.FixActiveScreenName(screenName);
	}
#endif

	// ISCREENOWNER INTERFACE

	public override void ShowScreen(string screenName, bool clearStack = false)
	{
#if USE_DEBUG_SERVER_LIST
		// hack for development
		if (GetComponent<ServerListScreen>() != null)
		{
			if (screenName == "Lobby")
			{
				screenName = "PlayServer";
			}
			else if (screenName == "LobbyHack")
			{
				screenName = "Lobby";
			}
		}
#endif

#if UNITY_STANDALONE_WIN //on PC, gold button in the status bar shows paywall page immediatelly
		if (screenName == "ShopFunds:0")
		{
			if(ShopDataBridge.Instance.IAPServiceAvailable())
			{
				ShopItemId dummyItem = new ShopItemId((int)E_FundID.Gold10, GuiShop.E_ItemType.Fund);
				ShopDataBridge.Instance.IAPRequestPurchase(dummyItem);
				
				GuiShopStatusIAP iapPopup = ShowPopup("ShopStatusIAP", TextDatabase.instance[02900014], TextDatabase.instance[02030097]) as GuiShopStatusIAP;
				iapPopup.BuyIAPItem = dummyItem;
			}
			else
				ShowPopup("ShopMessageBox", TextDatabase.instance[02900016], TextDatabase.instance[02900017]);
			return;
		}		
#endif
		if (screenName == "PlayerProfile" && CloudUser.instance.userAccountKind == E_UserAcctKind.Guest &&
			PlayerPrefs.GetInt("MigrateGuestDialog", 0) == 0)
		{
			GuiPopupMigrateGuest migratePopup = (GuiPopupMigrateGuest)ShowPopup("MigrateGuest", null, null, MigrateGuestResult);
			migratePopup.Usage = GuiPopupMigrateGuest.E_Usage.MainMenu;
			migratePopup.PrimaryKey = CloudUser.instance.primaryKey;
			migratePopup.Password = CloudUser.instance.passwordHash;
			PlayerPrefs.SetInt("MigrateGuestDialog", 1);
			return;
		}
		base.ShowScreen(screenName, clearStack);
	}

	void MigrateGuestResult(GuiPopup popup, E_PopupResultCode result)
	{
		if (result == E_PopupResultCode.Cancel)
			base.ShowScreen("PlayerProfile", false);
	}

	public override void Exit()
	{
		//Debug.Log("Quiting application");
		Application.Quit();
	}

	public override void DoCommand(string inCommand)
	{
		switch (inCommand)
		{
		case "LogoutUser":
			LogoutUser();
			break;
		case "MoreApps":
			ShowMoreApps();
			break;
		case "ShowSocial":
			ShowPopup("Social", null, null);
			break;
		case "DailyRewards":
			ShowDailyRewards();
			break;
		case "FreeGold":
			ShowFreeGold();
			break;
		case "InviteFB":
			ShowInviteFB();
			break;
		default:
			base.DoCommand(inCommand);
			break;
		}
	}

	// GUIMENU INTERFACE

	protected override bool ProcessMenuInput(ref IInputEvent evt)
	{
		if (base.ProcessMenuInput(ref evt) == true)
			return true;

		if (evt.Kind == E_EventKind.Key)
		{
			KeyEvent key = (KeyEvent)evt;
			if (key.Code == KeyCode.Escape)
			{
				if (key.State == E_KeyState.Released)
				{
					ShowConfirmDialog("Exit", TextDatabase.instance[00106013], ExitHandler);
				}
				return true;
			}
		}

		return false;
	}

	// PRIVATE METHODS

	void LogoutUser()
	{
		// user is about to leave the game
		// ask him if we can do that
		ShowConfirmDialog(TextID_Logout_Caption, TextID_Logout_Text, OnLogoutConfirmation);
	}

	void ShowDailyRewards()
	{
		var ppi = PPIManager.Instance.GetLocalPPI();
		if (ppi == null)
			return;

		GuiPopupDailyRewards popup = ShowPopup("DailyRewards", null, null) as GuiPopupDailyRewards;
		if (popup != null)
		{
			popup.SetData(ppi.DailyRewards, false, false, null, CloudDateTime.UtcNow);
		}
	}

	void ShowFreeGold()
	{
#if !UNITY_STANDALONE //Dont show freegold on webplayer and standalone
		GuiShopUtils.EarnFreeGold(new ShopItemId((int)E_FundID.TapJoyInApp, GuiShop.E_ItemType.Fund));
#endif
	}

	void InviteSentCallback(FBResult response)
	{
		/*
		JsonData data = JsonMapper.ToObject(response.Text);
		JsonData list;
		string[] ids;

		try
		{
			list = data["to"];

			ids = new string[list.Count];
			for (int i = 0; i < list.Count; i++)
				ids[i] = (string)list[i];
		}
		catch
		{
			return;
		}
		*/
	}

	void ShowInviteFB()
	{
		//string[] id = {"100000267513808"};
		string message = "Invite your friends !!!";

		FB.AppRequest(message, null, null, null, default(int?), string.Empty, string.Empty, InviteSentCallback);
	}

	static float nextShowTime = 0;

	void ShowMoreApps()
	{
		if (nextShowTime > Time.timeSinceLevelLoad)
			return;

		nextShowTime = Time.timeSinceLevelLoad + 0.5f; //wait moment to avoid button-smashing crash 

#if UNITY_STANDALONE || UNITY_EDITOR
		Application.OpenURL("http://madfingergames.com/games.html");
#else
        ChartBoost.showMoreApps();
#endif

		RefreshMenu();
	}

	// HANDLERS

	void OnLogoutConfirmation(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		if (inResult != E_PopupResultCode.Ok)
			return;

		CloudUser.instance.LogoutLocalUser();

		GuiFrontendMain.ShowLoginMenu();
	}

	void ExitHandler(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		if (inResult == E_PopupResultCode.Ok)
		{
			QuitApplication();
		}
	}
}
