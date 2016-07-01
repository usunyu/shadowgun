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

public class HudComponentCommandMenu : HudComponent
{
	readonly static string LAYOUT = "HUD_Layout";
	readonly static string ROOT = "CommandMenu";
	readonly static string MENU = "Menu";
	readonly static string[] ITEMS = {"Affirmative", "Negative", "Attack", "Help", "CoverMe", "Back", "OutOfAmmo", "Medic"};
	readonly static string IDLE = "idle";
	readonly static string HOVER = "hover";

	enum E_State
	{
		Idle,
		Visible,
		Hidding
	}

	// PRIVATE MEMBERS

	E_State m_State = E_State.Idle;
	GUIBase_Widget m_Root;
	GUIBase_Button m_Menu;
	GUIBase_Widget[] m_Items = new GUIBase_Widget[(int)E_CommandID.Max];
	float m_MouseSelectionPos = 0;
	float m_MouseSelectionChange = 30;
	int m_MouseSelection = -1;

	// HUDCOMPONENT INTERFACE

	public bool IsShown
	{
		private set;
		get;
	}

	public bool IsMenuVisible
	{
		get { return m_State == E_State.Visible; }
	}

	protected override bool OnInit()
	{
		if (base.OnInit() == false)
			return false;

		GUIBase_Layout layout = MFGuiManager.Instance.GetLayout(LAYOUT);
		if (layout == null)
			return false;

		m_Root = layout.GetWidget(ROOT);
		if (m_Root == null)
			return false;

		GUIBase_Widget[] widgets = m_Root.GetComponentsInChildren<GUIBase_Widget>();
		for (int idx = 0; idx < (int)E_CommandID.Max; ++idx)
		{
			GUIBase_Widget widget = GetWidget(ITEMS[idx], ref widgets);
			widget.Show(false, true);
			m_Items[idx] = widget;
		}

		if (GuiHUD.ControledByTouchScreen())
		{
			m_Menu = GetWidget(MENU, ref widgets).GetComponent<GUIBase_Button>();
		}

		return true;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
	}

	protected override void OnShow()
	{
		base.OnShow();

		m_MouseSelectionPos = 0;
		m_MouseSelection = -1;

		IsShown = true;

		if (m_Menu != null)
		{
			m_Menu.Widget.ShowImmediate(true, false);
			GuiBaseUtils.RegisterButtonDelegate3(m_Menu, OnMenuPressed, OnMenuReleased, OnMenuCancelled);
		}
	}

	public void OpenMenu()
	{
		ShowItems();
	}

	protected override void OnHide()
	{
		if (m_Menu != null)
		{
			GuiBaseUtils.RegisterButtonDelegate3(m_Menu, null, null, null);
		}

		HideItems(E_CommandID.Max);
		IsShown = false;

		if (m_Menu != null)
		{
			m_Menu.Widget.Show(false, false);
		}

		base.OnHide();
	}

	// HANDLERS

	void OnMenuPressed(GUIBase_Widget widget, object evt)
	{
		ShowItems();
	}

	void OnMenuReleased(GUIBase_Widget widget, object evt)
	{
		if (m_State != E_State.Visible)
			return;

		TouchEvent touch = (TouchEvent)evt;
		Vector2 position = touch.Position;

		position.y = Screen.height - position.y;

		int idx = 0;
		for (; idx < (int)E_CommandID.Max; ++idx)
		{
			if (m_Items[idx].IsMouseOver(position) == true)
			{
				OnSendCommand((E_CommandID)idx);
				break;
			}
		}

		HideItems((E_CommandID)idx);
	}

	void OnMenuCancelled(GUIBase_Widget widget, object evt)
	{
		HideItems(E_CommandID.Max);
	}

	void OnSendCommand(E_CommandID id)
	{
		LocalPlayer.Controls.SendCommandDelegate(id);
	}

	// PRIVATE METHODS

	void ShowItems()
	{
		if (m_State != E_State.Idle)
			return;

		m_State = E_State.Visible;

		if (m_Menu != null)
		{
			m_Menu.ForceHighlight(true);
		}

		for (int idx = 0; idx < (int)E_CommandID.Max; ++idx)
		{
			m_Items[idx].Show(true, true);
		}
	}

	void HideItems(E_CommandID activeCommandId)
	{
		if (m_State != E_State.Visible)
			return;
		m_State = E_State.Hidding;

		Owner.StartCoroutine(HideItems_Coroutine(activeCommandId));
	}

	IEnumerator HideItems_Coroutine(E_CommandID activeCommandId)
	{
		if (m_Menu != null)
		{
			m_Menu.ForceHighlight(false);
		}

		for (int idx = 0; idx < (int)E_CommandID.Max; ++idx)
		{
			if (idx != (int)activeCommandId)
			{
				m_Items[idx].Show(false, true);
				SetState(m_Items[idx], IDLE);
			}
			else
			{
				SetState(m_Items[idx], HOVER);
			}
		}

		if (activeCommandId != E_CommandID.Max)
		{
			yield return new WaitForSeconds(1.0f);

			int idx = (int)activeCommandId;
			m_Items[idx].Show(false, true);
			SetState(m_Items[idx], IDLE);
		}

		m_State = E_State.Idle;
	}

	void SetState(GUIBase_Widget widget, string state)
	{
		GUIBase_MultiSprite sprite = widget ? widget.GetComponentInChildren<GUIBase_MultiSprite>() : null;
		if (sprite != null)
		{
			sprite.State = state;
		}
	}

	GUIBase_Widget GetWidget(string name, ref GUIBase_Widget[] widgets)
	{
		foreach (var widget in widgets)
		{
			if (widget.name == name)
				return widget;
		}
		return null;
	}

	//Selecting commands on PC using mouse
	public void SetMouseMove(float d)
	{
		m_MouseSelectionPos += d;
		if (m_MouseSelectionPos == 0)
			return;

		float maxLen = m_MouseSelectionChange*m_Items.Length/2;

		float sign = Mathf.Sign(m_MouseSelectionPos);
		float mouseSelectionPos = Mathf.Abs(m_MouseSelectionPos);
		if (mouseSelectionPos > maxLen)
			mouseSelectionPos = maxLen;

		m_MouseSelectionPos = sign*mouseSelectionPos;
		mouseSelectionPos = maxLen + sign*mouseSelectionPos;
		int mouseSelection = (int)(mouseSelectionPos/m_MouseSelectionChange);
		mouseSelection = mouseSelection < 0 ? 0 : (mouseSelection >= m_Items.Length ? m_Items.Length - 1 : mouseSelection);
		if (mouseSelection == m_MouseSelection)
			return;

		if (m_MouseSelection != -1)
			SetState(m_Items[m_MouseSelection], IDLE);
		m_MouseSelection = mouseSelection;
		SetState(m_Items[m_MouseSelection], HOVER);
	}

	public void ConfirmMouseSelection()
	{
		if (m_MouseSelection >= 0)
			OnSendCommand((E_CommandID)m_MouseSelection);
		Hide();
	}
}
