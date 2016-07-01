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

// =====================================================================================================================
// =====================================================================================================================
public class MainScreen : GuiScreen
{
	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_ScreenPivot = GetPivot(s_PivotName);
		m_ScreenLayout = GetLayout(s_PivotName, s_ScreenLayoutName);

		//main
		PrepareButton(m_ScreenLayout, s_GameButtonName, null, OnGamePressed);
		PrepareButton(m_ScreenLayout, s_StatsButtonName, null, OnStatsPressed);
		PrepareButton(m_ScreenLayout, s_EquipButtonName, null, OnEquipPressed);
		PrepareButton(m_ScreenLayout, s_ShopButtonName, null, OnShopPressed);
		PrepareButton(m_ScreenLayout, s_FriendsButtonName, null, OnFriendsPressed);

		ButtonDisable(m_ScreenLayout, s_StatsButtonName, true);
		ButtonDisable(m_ScreenLayout, s_ShopButtonName, true);
		ButtonDisable(m_ScreenLayout, s_FriendsButtonName, true);

		m_PlaySpaceButton = GetSpaceButton(s_PlaySpaceButtonName);
		m_BankSpaceButton = GetSpaceButton(s_BankSpaceButtonName);
		m_PostSpaceButton = GetSpaceButton(s_PostSpaceButtonName);
		ActivateSpaceButtons(false);
	}

	protected override void OnViewShow()
	{
		MFGuiManager.Instance.ShowPivot(m_ScreenPivot, true);

		ActivateSpaceButtons(true);

		base.OnViewShow();
	}

	protected override void OnViewHide()
	{
		MFGuiManager.Instance.ShowPivot(m_ScreenPivot, false);

		ActivateSpaceButtons(false);

		base.OnViewHide();
	}

	protected override void OnViewUpdate()
	{
		ProcessSpaceInput();

		base.OnViewUpdate();
	}

	// #################################################################################################################
	// ###  Delegates  #################################################################################################
	void OnGamePressed(GUIBase_Widget inWidget)
	{
		Owner.ShowScreen("PlayServer");
	}

	void OnStatsPressed(GUIBase_Widget inWidget)
	{
		//Owner.ShowScreen("Options");
	}

	void OnEquipPressed(GUIBase_Widget inWidget)
	{
		Owner.ShowScreen("Equip");
	}

	void OnShopPressed(GUIBase_Widget inWidget)
	{
		Owner.ShowScreen("Shop");
	}

	void OnFriendsPressed(GUIBase_Widget inWidget)
	{
		Owner.ShowScreen("Friends");
	}

	void OnPlayPressed()
	{
		OnGamePressed(null);
	}

	void OnBankPressed()
	{
		Owner.ShowPopup("MessageBox", "Bank", "Trading is not available yet...", null);
	}

	void OnPostPressed()
	{
		Owner.ShowPopup("MessageBox", "Post", "Messaging is not available yet...", null);
	}

	// #################################################################################################################
	Collider GetSpaceButton(string name)
	{
		GameObject gameObject = GameObject.Find(name);
		return gameObject ? gameObject.GetComponent<Collider>() : null;
	}

	void ActivateSpaceButtons(bool state)
	{
		ActivateSpaceButton(m_PlaySpaceButton, state);
		ActivateSpaceButton(m_BankSpaceButton, state);
		ActivateSpaceButton(m_PostSpaceButton, state);
	}

	void ActivateSpaceButton(Collider button, bool state)
	{
		if (button != null)
		{
			button.gameObject.SetActive(state);
			button.enabled = state;
		}
	}

	void ProcessSpaceInput()
	{
		if (Input.touchCount > 0)
		{
			for (int idx = 0; idx < Input.touchCount; ++idx)
			{
				Touch touch = Input.GetTouch(idx);
				if (touch.phase == TouchPhase.Began)
				{
					SpaceHitTest(touch.position);
					break;
				}
			}
		}
		else if (Input.GetMouseButtonDown(0) == true)
		{
			SpaceHitTest(Input.mousePosition);
		}
	}

	void SpaceHitTest(Vector2 point)
	{
		Ray ray = Camera.main.ScreenPointToRay(point);
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit))
		{
			if (m_PlaySpaceButton != null && m_PlaySpaceButton == hit.collider)
			{
				OnPlayPressed();
			}
			else if (m_BankSpaceButton != null && m_BankSpaceButton == hit.collider)
			{
				OnBankPressed();
			}
			else if (m_PostSpaceButton != null && m_PostSpaceButton == hit.collider)
			{
				OnPostPressed();
			}
		}
	}

	// #################################################################################################################	
	static string s_PivotName = "Main";
	static string s_ScreenLayoutName = "01Buttons_Layout";

	static string s_GameButtonName = "Game_Button";
	static string s_StatsButtonName = "Stats_Button";
	static string s_EquipButtonName = "Equip_Button";
	static string s_ShopButtonName = "Shop_Button";
	static string s_FriendsButtonName = "Friends_Button";

	static string s_PlaySpaceButtonName = "MainMenubuttonPlay";
	static string s_BankSpaceButtonName = "MainMenubuttonBank";
	static string s_PostSpaceButtonName = "MainMenubuttonPost";

	Collider m_PlaySpaceButton;
	Collider m_BankSpaceButton;
	Collider m_PostSpaceButton;
}
