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

// -----------
public class HudComponentGadgets : HudComponent
{
	class GuiGadget<T>
	{
		public GUIBase_Button m_Button;
		public GUIBase_Widget m_GadgetIcon;
		public GUIBase_Widget m_Progress;
		public GUIBase_Widget m_BarBck;
		public GUIBase_Widget m_Div2;
		public GUIBase_Widget m_Div3;
		public GUIBase_Widget m_Parent;
		public T m_Settings;
		public float m_PrevScale;
		public bool m_IsTransparent { get; private set; }

		// ------
		public void SetAllTransparent(bool transparent)
		{
			float alpha = transparent ? 0.1f : 1.0f;
			m_Button.Widget.FadeAlpha = alpha;
			m_GadgetIcon.FadeAlpha = alpha;
			m_Progress.FadeAlpha = alpha;
			m_BarBck.FadeAlpha = alpha;
			GUIBase_Widget[] widgets = m_Div2.GetComponentsInChildren<GUIBase_Widget>();
			foreach (GUIBase_Widget w in widgets)
				w.FadeAlpha = alpha;
			m_Div2.FadeAlpha = alpha;
			widgets = m_Div3.GetComponentsInChildren<GUIBase_Widget>();
			foreach (GUIBase_Widget w in widgets)
				w.FadeAlpha = alpha;
			m_Div3.FadeAlpha = alpha;
			m_Parent.FadeAlpha = alpha;
			m_Button.SetDisabled(transparent);
			m_IsTransparent = transparent;
		}
	}

	static string s_PivotMainName = "MainHUD";
	static string s_LayoutMainName = "HUD_Layout";
	//gadgets
	public List<E_ItemID> Gadgets { get; private set; }
	GuiGadget<ItemSettings>[] m_GuiGadgets = new GuiGadget<ItemSettings>[s_GadgetsBtnNames.Length];
	GuiGadget<PerkSettings> m_Perk;
	static string[] s_GadgetsBtnNames = new string[] {"GButton_1", "GButton_2", "GButton_3"};
	static GUIBase_Button.TouchDelegate[] s_GadgetsBtnDelegates = new GUIBase_Button.TouchDelegate[] {G_Touch1, G_Touch2, G_Touch3, G_Touch4};
	GUIBase_Pivot m_PivotMain;
	GUIBase_Layout m_LayoutMain;
	GUIBase_Widget m_SelectionWidget;
	int m_SelIndex;
	bool m_Initialised = false;

	// ---------
	protected override bool OnInit()
	{
		if (base.OnInit() == false)
			return false;

		Gadgets = new List<E_ItemID>();

		m_PivotMain = MFGuiManager.Instance.GetPivot(s_PivotMainName);
		if (!m_PivotMain)
		{
			Debug.LogError("'" + s_PivotMainName + "' not found!!! Assert should come now");
			return false;
		}
		m_LayoutMain = m_PivotMain.GetLayout(s_LayoutMainName);
		if (!m_LayoutMain)
		{
			Debug.LogError("'" + s_LayoutMainName + "' not found!!! Assert should come now");
			return false;
		}

		m_SelIndex = 0;
		return true;
	}

	void ResetGadgetsList()
	{
		UnregisterFloatingFireInteractions();

		E_ItemID[] ids = {E_ItemID.None, E_ItemID.None, E_ItemID.None, E_ItemID.None};
		Gadgets.Clear();
		Gadgets.AddRange(ids);
	}

	// -----------
	//create gadgets buttons
	public void CreateGadgetInventory(PlayerPersistantInfo ppi)
	{
		ResetGadgetsList();

		// ----------
		foreach (PPIItemData d in ppi.EquipList.Items)
		{
			if (d.ID == E_ItemID.None)
				continue;

			Gadgets[d.EquipSlotIdx] = d.ID;
		}

		for (int i = 0; i < m_GuiGadgets.Length; i++)
		{
			m_GuiGadgets[i] = new GuiGadget<ItemSettings>();
			GUIBase_Widget parent = PrepareWidget(m_LayoutMain, "Gadget_" + (i + 1));
			m_GuiGadgets[i].m_Parent = parent;
			m_GuiGadgets[i].m_Button = GuiBaseUtils.GetButton(m_LayoutMain, s_GadgetsBtnNames[i]);
			m_GuiGadgets[i].m_GadgetIcon = GuiBaseUtils.GetChildSprite(parent, "GadgetIcon").Widget;
			m_GuiGadgets[i].m_BarBck = GuiBaseUtils.GetChildSprite(parent, "Meter_Back").Widget;
			m_GuiGadgets[i].m_Progress = GuiBaseUtils.GetChildSprite(parent, "Meter_BAR").Widget;
			m_GuiGadgets[i].m_Div2 = GuiBaseUtils.GetChildSprite(parent, "Meter_Div2").Widget;
			m_GuiGadgets[i].m_Div3 = GuiBaseUtils.GetChildSprite(parent, "Meter_Div3").Widget;
			m_GuiGadgets[i].m_PrevScale = -1;

			if (i >= Gadgets.Count || Gadgets[i] == E_ItemID.None)
			{
				m_GuiGadgets[i].m_Parent.Show(false, true);
				continue;
			}

			m_GuiGadgets[i].m_Settings = ItemSettingsManager.Instance.Get(Gadgets[i]);
			GuiBaseUtils.RegisterButtonDelegate(m_LayoutMain, s_GadgetsBtnNames[i], s_GadgetsBtnDelegates[i], null);
			GUIBase_Sprite s = GuiBaseUtils.GetChildSprite(parent, "GadgetIcon");
			s.Widget.CopyMaterialSettings(m_GuiGadgets[i].m_Settings.HudWidget);
			m_GuiGadgets[i].m_Parent.Show(true, true);
			m_GuiGadgets[i].SetAllTransparent(false);

			Item gadget = LocalPlayer.Owner.GadgetsComponent.GetGadget(Gadgets[i]);
			UpdateGuiGadgetBar(m_GuiGadgets[i], gadget.Timer, m_GuiGadgets[i].m_Settings.Timer, gadget.Count, gadget.OrigCount, false);
		}

		// ----------
		m_Perk = new GuiGadget<PerkSettings>();
		m_Perk.m_Parent = PrepareWidget(m_LayoutMain, "Perk");
		m_Perk.m_Button = GuiBaseUtils.GetButton(m_LayoutMain, "GButton");
		m_Perk.m_GadgetIcon = GuiBaseUtils.GetChildSprite(m_Perk.m_Parent, "GadgetIcon").Widget;
		m_Perk.m_BarBck = GuiBaseUtils.GetChildSprite(m_Perk.m_Parent, "Meter_Back").Widget;
		m_Perk.m_Progress = GuiBaseUtils.GetChildSprite(m_Perk.m_Parent, "Meter_BAR").Widget;
		m_Perk.m_Div2 = GuiBaseUtils.GetChildSprite(m_Perk.m_Parent, "Meter_Div2").Widget;
		m_Perk.m_Div3 = GuiBaseUtils.GetChildSprite(m_Perk.m_Parent, "Meter_Div3").Widget;
		m_Perk.m_PrevScale = -1;

		PerkInfo perk = LocalPlayer.Owner.GadgetsComponent.Perk;
		m_Perk.m_Settings = PerkSettingsManager.Instance.Get(ppi.EquipList.Perk);
		if (!perk.IsEmpty())
		{
			GUIBase_Sprite s = GuiBaseUtils.GetChildSprite(m_Perk.m_Parent, "GadgetIcon");
			s.Widget.CopyMaterialSettings(m_Perk.m_Settings.HudWidget);
			m_Perk.m_Parent.Show(true, true);
			UpdateGuiGadgetBar(m_Perk, perk.Timer, m_Perk.m_Settings.Timer, 0, 0, perk.IsSprint());
		}
		else
			m_Perk.m_Parent.Show(false, true);
		//m_Perk.SetAllTransparent(false);

		m_SelIndex = SelectFirst();
		m_SelectionWidget = m_LayoutMain.GetWidget("GadgetSelection_Sprite", true);
		ShowSelection(true);

		m_Initialised = true;
		UpdateControlsPosition();

		RegisterFloatingFireInteractions();
	}

	// -----------
	void UpdateGuiGadgetBar<T>(GuiGadget<T> guiGadet, float timer, float maxTimer, int count, int origCount, bool showTimer)
	{
		float scale = 1;
		Vector3 pos = guiGadet.m_BarBck.transform.localPosition;
		float width = guiGadet.m_BarBck.GetWidth();

		bool showDiv2 = false;
		bool showDiv3 = false;
		bool showProgress = false;

		if (guiGadet.m_Parent.IsVisible())
		{
			showProgress = origCount > 0;
			if (showTimer)
			{
				scale = timer/maxTimer;
				showProgress = true;
			}
			else if (origCount == 3)
			{
				showDiv3 = true;
				scale = count/3f;
			}
			else if (origCount == 2)
			{
				showDiv2 = true;
				scale = count/2f;
			}
			if (!Mathf.Approximately(guiGadet.m_PrevScale, scale))
			{
				pos.x -= width*(1 - scale)*0.5f;
				guiGadet.m_Progress.transform.localScale = new Vector3(scale, 1, 1);
				guiGadet.m_Progress.transform.localPosition = pos;
				guiGadet.m_Progress.SetModify();
				guiGadet.m_PrevScale = scale;
			}
		}

		if (guiGadet.m_Div2.IsVisible() != showDiv2)
			guiGadet.m_Div2.Show(showDiv2, true);
		if (guiGadet.m_Div3.IsVisible() != showDiv3)
			guiGadet.m_Div3.Show(showDiv3, true);
		if (guiGadet.m_Progress.IsVisible() != showProgress)
			guiGadet.m_Progress.Show(showProgress, true);
		if (guiGadet.m_BarBck.IsVisible() != showProgress)
			guiGadet.m_BarBck.Show(showProgress, true);
	}

	// -----------
	void UpdateGadgets()
	{
		if (!LocalPlayer)
			return;

		for (int i = 0; i < Gadgets.Count; i++)
		{
			if (i >= m_GuiGadgets.Length)
				break;

			if (Gadgets[i] == E_ItemID.None)
				continue;

			Item gadget = LocalPlayer.Owner.GadgetsComponent.GetGadget(Gadgets[i]);

			if (gadget.Settings.ItemBehaviour == E_ItemBehaviour.Booster)
			{
				if (gadget.IsAvailableForUse() || gadget.Active)
				{
					if (m_GuiGadgets[i].m_IsTransparent)
						m_GuiGadgets[i].SetAllTransparent(false);
				}
				else
				{
					if (!m_GuiGadgets[i].m_IsTransparent)
						m_GuiGadgets[i].SetAllTransparent(true);
				}

				UpdateGuiGadgetBar(m_GuiGadgets[i], gadget.Timer, m_GuiGadgets[i].m_Settings.BoostTimer, gadget.Count, gadget.OrigCount, true);
			}
			else
			{
				if (gadget.IsAvailableForUse())
				{
					if (m_GuiGadgets[i].m_IsTransparent)
						m_GuiGadgets[i].SetAllTransparent(false);
				}
				else
				{
					if (!m_GuiGadgets[i].m_IsTransparent)
						m_GuiGadgets[i].SetAllTransparent(true);
				}

				UpdateGuiGadgetBar(m_GuiGadgets[i], gadget.Timer, m_GuiGadgets[i].m_Settings.Timer, gadget.Count, gadget.OrigCount, false);
			}
		}

		PerkInfo perk = LocalPlayer.Owner.GadgetsComponent.Perk;
		if (!perk.IsEmpty())
		{
			UpdateGuiGadgetBar(m_Perk, perk.Timer, m_Perk.m_Settings.Timer, 0, 0, perk.IsSprint());
		}
	}

	// ---------
	protected override void OnDestroy()
	{
		base.OnDestroy();

		UnregisterFloatingFireInteractions();
	}

	void RegisterFloatingFireInteractions()
	{
		// register all items cooperating with the FloatingFire feature
		for (int i = 0; i < m_GuiGadgets.Length; i++)
		{
			if (m_GuiGadgets[i] != null)
			{
				if (m_GuiGadgets[i].m_Button != null)
				{
					Owner.RegisterFloatingFireCooperatingItem(m_GuiGadgets[i].m_Button);
				}
			}
		}
		/*
		if (m_Perk != null)
		{
			if ( m_Perk.m_Button != null )
			{
				Owner.UnregisterFloatingFireCooperatingItem( m_Perk.m_Button );
			}
		}
		*/
	}

	void UnregisterFloatingFireInteractions()
	{
		// unregister all items cooperating with the FloatingFire feature
		for (int i = 0; i < m_GuiGadgets.Length; i++)
		{
			if (m_GuiGadgets[i] != null)
			{
				if (m_GuiGadgets[i].m_Button != null)
				{
					Owner.UnregisterFloatingFireCooperatingItem(m_GuiGadgets[i].m_Button);
				}
			}
		}
		/*
		if (m_Perk != null)
		{
			if ( m_Perk.m_Button != null )
			{
				Owner.UnregisterFloatingFireCooperatingItem( m_Perk.m_Button );
			}
		}
		*/
	}

	// ---------
	protected override void OnShow()
	{
		base.OnShow();

		for (int i = 0; i < m_GuiGadgets.Length; i++)
		{
			if (m_GuiGadgets[i] != null)
			{
				bool shouldBeVisible = i < Gadgets.Count && Gadgets[i] != E_ItemID.None ? true : false;
				m_GuiGadgets[i].m_Parent.Show(shouldBeVisible, true);
			}
		}
		PerkInfo perk = LocalPlayer.Owner.GadgetsComponent.Perk;
		if (m_Perk != null)
		{
			m_Perk.m_Parent.Show(!perk.IsEmpty(), true);
		}

		RegisterFloatingFireInteractions();
		ShowSelection(true);
	}

	// ---------
	protected override void OnHide()
	{
		for (int i = 0; i < m_GuiGadgets.Length; i++)
		{
			if (m_GuiGadgets[i] != null)
			{
				m_GuiGadgets[i].m_Parent.Show(false, true);
			}
		}
		if (m_Perk != null)
		{
			m_Perk.m_Parent.Show(false, true);
		}

		UnregisterFloatingFireInteractions();
		ShowSelection(false);

		base.OnHide();
	}

	// ------
	protected override void OnLateUpdate()
	{
		base.OnLateUpdate();

		UpdateGadgets();
	}

	// ------
	public void SelectGadget(int index)
	{
		if (index < 0 || index >= Gadgets.Count)
			return;

		LocalPlayer.Controls.UseGadgetDelegate(Gadgets[index]);
	}

	// ------
	public E_ItemID GetGadgetInInventoryIndex(int index)
	{
		if (Gadgets.Count == 0 || Gadgets.Count <= index)
			return E_ItemID.None;

		return Gadgets[index];
	}

	// ------
	void TouchGadget(E_ItemID g)
	{
		if (LocalPlayer)
			LocalPlayer.Controls.UseGadgetDelegate(g);
	}

	// ------
	static void G_Touch1()
	{
		//GuiHUD.Instance.ShowNewRank( PPIManager.Instance.GetPPI(LocalPlayer.networkView.owner).Rank );
		GuiHUD.Instance.SelectGadget(0);
	}

	// ------
	static void G_Touch2()
	{
		GuiHUD.Instance.SelectGadget(1);
	}

	// ------
	static void G_Touch3()
	{
		GuiHUD.Instance.SelectGadget(2);
	}

	// ------
	static void G_Touch4()
	{
		GuiHUD.Instance.SelectGadget(3);
	}

	// ------
	GUIBase_Widget PrepareWidget(GUIBase_Layout layout, string name)
	{
		GUIBase_Widget tmpWidget = layout.GetWidget(name);

		return tmpWidget;
	}

	public void UpdateControlsPosition()
	{
		//skip if we have no gadgets yet (we call it this method on create gadgets also)
		if (!m_Initialised)
			return;

		for (int i = 0; i < m_GuiGadgets.Length; i++)
		{
			//Debug.Log("updating Gadget " + i + " pos: " + GuiOptions.GadgetButtons[i].Positon + "orig: " + GuiOptions.GadgetButtons[i].OrigPos + " offset: " + GuiOptions.GadgetButtons[i].Offset);
			m_GuiGadgets[i].m_Parent.transform.position = GuiOptions.GadgetButtons[i].Positon;
			m_GuiGadgets[i].m_Parent.SetModify(true);
		}
		UpdateSelectionPos();
	}

	bool IsValidSelection()
	{
		if (Gadgets != null && m_SelIndex >= 0 && m_SelIndex < Gadgets.Count)
		{
			return (Gadgets[m_SelIndex] != E_ItemID.None);
		}
		else
			return false;
	}

	public void ShowSelection(bool show)
	{
		if (m_SelectionWidget != null)
		{
			UpdateSelectionPos();
			bool gamepadControl = MogaGamepad.IsConnected();
			m_SelectionWidget.Show(show && gamepadControl && IsValidSelection(), true);
		}
	}

	void UpdateSelectionPos()
	{
		if (m_SelectionWidget != null)
		{
			if (IsValidSelection())
			{
				//update position
				m_SelectionWidget.transform.position = m_GuiGadgets[m_SelIndex].m_Parent.transform.position;
				m_SelectionWidget.SetModify(true);
			}
		}
	}

	public int GetSelected()
	{
		return m_SelIndex;
	}

	// ---------
	int SelectFirst()
	{
		int gadgetsCount = Mathf.Min(Gadgets.Count, m_GuiGadgets.Length);
		int index = 0;
		for (int i = 0; i < gadgetsCount; i++)
		{
			if (Gadgets[i] != E_ItemID.None)
			{
				index = i;
				break;
			}
		}

		return index;
	}

	// ---------
	public void SelectNext()
	{
		int gadgetsCount = Mathf.Min(Gadgets.Count, m_GuiGadgets.Length);
		int index = m_SelIndex;
		do
		{
			index = (++index)%gadgetsCount;

			if (Gadgets[index] != E_ItemID.None)
				break;
		} while (index != m_SelIndex);

		//Debug.Log("gadgetsCount: " + gadgetsCount + ", m_SelIndex: " + m_SelIndex + ", new index: " + index);
		m_SelIndex = index;
		UpdateSelectionPos();
		//ShowSelection(IsVisible);
	}

	// ---------
	public void SelectPrev()
	{
		int gadgetsCount = Mathf.Min(Gadgets.Count, m_GuiGadgets.Length);
		int index = m_SelIndex;
		do
		{
			index = (index <= 0) ? gadgetsCount - 1 : index - 1;

			if (Gadgets[index] != E_ItemID.None)
				break;
		} while (index != m_SelIndex);

		//Debug.Log("gadgetsCount: " + gadgetsCount + ", m_SelIndex: " + m_SelIndex + ", new index: " + index);
		m_SelIndex = index;
		UpdateSelectionPos();
		//ShowSelection(IsVisible);
	}
}
