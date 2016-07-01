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

// =============================================================================================================================
// =============================================================================================================================
public class SMSideBar : GuiScreen
{
	const string SCREEN_SPAWN = "Spawn";
	const string SCREEN_SPAWN_DEATHMATCH = "DeadtMatch";
	const string SCREEN_SPAWN_DOMINANTION = "Domination";

	const string SCREEN_SHOP = "Shop";
	const string SCREEN_EQUIP = "Equip";
	const string SCREEN_FRIENDS = "Friends";
	const string SCREEN_STATS = "PlayerStats";

	GUIBase_Button m_EquipButton;
	GUIBase_Button m_FriendsButton;
	GUIBase_Button m_GameButton;
	GUIBase_Button m_ShopButton;
	GUIBase_Button m_StatsButton;

	// =========================================================================================================================
	// === public interface ====================================================================================================
	public void OnScreenShow(string inShowScreenName)
	{
		switch (inShowScreenName)
		{
		case SCREEN_SPAWN:
			UpdateButtonDownState(m_GameButton.Widget);
			return;
		case SCREEN_SPAWN_DEATHMATCH:
			UpdateButtonDownState(m_GameButton.Widget);
			return;
		case SCREEN_SPAWN_DOMINANTION:
			UpdateButtonDownState(m_GameButton.Widget);
			return;

		case SCREEN_SHOP:
			UpdateButtonDownState(m_ShopButton.Widget);
			return;
		case SCREEN_EQUIP:
			UpdateButtonDownState(m_EquipButton.Widget);
			return;
		case SCREEN_FRIENDS:
			UpdateButtonDownState(m_FriendsButton.Widget);
			return;
		case SCREEN_STATS:
			UpdateButtonDownState(m_StatsButton.Widget);
			return;
		default: //ignore.
			break;
		}
	}

	public void EnableButton(string inButtonName, bool inEnable)
	{
		switch (inButtonName)
		{
		case "Shop":
			m_ShopButton.SetDisabled(!inEnable);
			break;
		case "Equip":
			m_EquipButton.SetDisabled(!inEnable);
			break;
		default:
			Debug.LogError("Unknown button name: " + inButtonName);
			break;
		}
	}

	// =========================================================================================================================
	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_ScreenLayout = GetLayout("IngameMenu", "SMSideButtons_Layout");

		// prepare screen widgets...
		m_EquipButton = PrepareButton(m_ScreenLayout, "Equip_Button", ShowEquip, null);
		m_FriendsButton = PrepareButton(m_ScreenLayout, "Friends_Button", ShowFriend, null);
		m_GameButton = PrepareButton(m_ScreenLayout, "Game_Button", ShowGame, null);
		m_ShopButton = PrepareButton(m_ScreenLayout, "Shop_Button", ShowShop, null);
		m_StatsButton = PrepareButton(m_ScreenLayout, "Stats_Button", ShowStats, null);

		m_EquipButton.autoColorLabels = true;
		m_FriendsButton.autoColorLabels = true;
		m_GameButton.autoColorLabels = true;
		m_ShopButton.autoColorLabels = true;
		m_StatsButton.autoColorLabels = true;

		m_EquipButton.stayDown = true;
		m_FriendsButton.stayDown = true;
		m_GameButton.stayDown = true;
		m_ShopButton.stayDown = true;
		m_StatsButton.stayDown = true;
	}

	// #################################################################################################################
	// ###  Delegates  #################################################################################################
	void ShowEquip(GUIBase_Widget inWidget)
	{
		UpdateButtonDownState(inWidget);
		Owner.ShowScreen(SCREEN_EQUIP, true);
		Owner.DoCommand("HideSpawnButtons");
	}

	void ShowFriend(GUIBase_Widget inWidget)
	{
		UpdateButtonDownState(inWidget);
		Owner.ShowScreen(SCREEN_FRIENDS);
		Owner.DoCommand("HideSpawnButtons");
	}

	void ShowGame(GUIBase_Widget inWidget)
	{
		UpdateButtonDownState(inWidget);
		Owner.ShowScreen(SCREEN_SPAWN, true);
		Owner.DoCommand("ShowSpawnButtons");
	}

	void ShowShop(GUIBase_Widget inWidget)
	{
		UpdateButtonDownState(inWidget);
		Owner.ShowScreen(SCREEN_SHOP, true);
		Owner.DoCommand("HideSpawnButtons");
	}

	void ShowStats(GUIBase_Widget inWidget)
	{
		UpdateButtonDownState(inWidget);
		Owner.ShowScreen(SCREEN_STATS, true);
		Owner.DoCommand("HideSpawnButtons");
	}

	void UpdateButtonDownState(GUIBase_Widget inWidget)
	{
		m_EquipButton.ForceDownStatus(m_EquipButton.Widget == inWidget);
		m_FriendsButton.ForceDownStatus(m_FriendsButton.Widget == inWidget);
		m_GameButton.ForceDownStatus(m_GameButton.Widget == inWidget);
		m_ShopButton.ForceDownStatus(m_ShopButton.Widget == inWidget);
		m_StatsButton.ForceDownStatus(m_StatsButton.Widget == inWidget);
	}

	// #################################################################################################################		
}
