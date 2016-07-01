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

[AddComponentMenu("GUI/Frontend/Screens/GuiScreenResearchMain")]
public class GuiScreenResearchMain : GuiScreenMultiPage, IGuiOverlayScreen
{
	struct popupItem
	{
		public Rect area;
		public ShopItemId item;
	}

	public GameObject m_ScrollItemTemplate;
	GuiResearchScroller m_ResearchScroller;

	GuiPopupViewItemDescription m_ItemDescription = new GuiPopupViewItemDescription();
	List<popupItem> m_PopupItems;

	// ------
	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_ScreenPivot = MFGuiManager.Instance.GetPivot("EquipMenu");
		m_ItemDescription.Init(m_ScreenPivot.GetLayout("ItemDescription_Layout"));

		m_PopupItems = new List<popupItem>();

		InitResearchScroller();
	}

	// ------
	protected override void OnViewShow()
	{
		InserScrollerItems();
		m_ResearchScroller.Show();
		m_ResearchScroller.RegisterOnSelectionDelegate(OnSelectionChange);

		m_ItemDescription.Key = "";

		// call super
		base.OnViewShow();
	}

	// ------
	protected override void OnViewHide()
	{
		m_PopupItems = null;
		m_ResearchScroller.Hide();
		m_ResearchScroller.RegisterOnSelectionDelegate(null);

		// call super
		base.OnViewHide();
	}

	// ------
	protected override void OnViewUpdate()
	{
		m_ResearchScroller.Update();

		base.OnViewUpdate();

		if (m_PopupItems == null || !IsEnabled)
		{
			m_ItemDescription.Show(false);
			return;
		}

		ShopItemId id = new ShopItemId();

		Vector3 input = Input.mousePosition;
		input.y = Screen.height - input.y;

		foreach (popupItem item in m_PopupItems)
		{
			if (item.area.Contains(input))
			{
				id = item.item;
				break;
			}
		}

		switch (id.ItemType)
		{
		case GuiShop.E_ItemType.Weapon:
			m_ItemDescription.SetItem(new PreviewItem((E_WeaponID)id.Id));
			m_ItemDescription.Show(true);
			break;
		case GuiShop.E_ItemType.Item:
			m_ItemDescription.SetItem(new PreviewItem((E_ItemID)id.Id));
			m_ItemDescription.Show(true);
			break;
		case GuiShop.E_ItemType.Perk:
			m_ItemDescription.SetItem(new PreviewItem((E_PerkID)id.Id));
			m_ItemDescription.Show(true);
			break;
		case GuiShop.E_ItemType.Upgrade:
			m_ItemDescription.SetItem(new PreviewItem((E_UpgradeID)id.Id));
			m_ItemDescription.Show(true);
			break;
		default:
			m_ItemDescription.Show(false);
			break;
		}
	}

	//find positions of all items on screen
	void CreateRects()
	{
		m_PopupItems = new List<popupItem>();
		for (int i = 0; i < CurrentPage.Layout.transform.childCount; i++)
		{
			ResearchItem itemOnScreen = ((ResearchItem)CurrentPage.Layout.transform.GetChild(i).GetComponent("ResearchItem"));
			if (itemOnScreen != null && itemOnScreen.Enabled())
			{
				popupItem item;
				item.area = CurrentPage.Layout.transform.GetChild(i).Find("Placeholder").GetComponent<GUIBase_Sprite>().Widget.GetRectInScreenCoords();
				if (itemOnScreen.weaponID != E_WeaponID.None)
				{
					item.item = new ShopItemId((int)itemOnScreen.weaponID, GuiShop.E_ItemType.Weapon);
					m_PopupItems.Add(item);
				}
				else if (itemOnScreen.upgradeID != E_UpgradeID.None)
				{
					item.item = new ShopItemId((int)itemOnScreen.upgradeID, GuiShop.E_ItemType.Upgrade);
					m_PopupItems.Add(item);
				}
				else if (itemOnScreen.perkID != E_PerkID.None)
				{
					item.item = new ShopItemId((int)itemOnScreen.perkID, GuiShop.E_ItemType.Perk);
					m_PopupItems.Add(item);
				}
				else if (itemOnScreen.itemID != E_ItemID.None)
				{
					item.item = new ShopItemId((int)itemOnScreen.itemID, GuiShop.E_ItemType.Item);
					m_PopupItems.Add(item);
				}
			}
		}
	}

	protected override void OnPageVisible(GuiScreen page)
	{
		CreateRects();
		if (m_ResearchScroller.GetSelectedItem() != CurrentPageIndex)
		{
			m_ResearchScroller.ScrollToItem(CurrentPageIndex);
		}
	}

	void OnSelectionChange(int selectedItem)
	{
		//Debug.Log("TODO: research sel change" + selectedItem);

		if (selectedItem == CurrentPageIndex)
			return;

		base.GotoPage(selectedItem);
	}

	void InitResearchScroller()
	{
		m_ResearchScroller = new GuiResearchScroller(m_ScrollItemTemplate);
	}

	void InserScrollerItems()
	{
		m_ResearchScroller.InitGui();
		List<int> items = new List<int>();
		for (int i = 0; i < 10; i++)
			items.Add(i);

		m_ResearchScroller.Insert(items, true);
	}

	protected override void OnViewEnable()
	{
		base.OnViewEnable();

		//Debug.Log("Enable Shop menu");
		m_ResearchScroller.EnableControls();
		m_ResearchScroller.FadeIn();
	}

	protected override void OnViewDisable()
	{
		//Debug.Log("Disable Shop menu");
		m_ResearchScroller.DisableControls();
		m_ResearchScroller.FadeOut();

		base.OnViewDisable();
	}

	// IGUIOVERLAYSCREEN INTERFACE

	string IGuiOverlayScreen.OverlayButtonTooltip
	{
		get { return null; }
	}

	bool IGuiOverlayScreen.HideOverlayButton
	{
		get { return Ftue.IsActionIdle<FtueAction.Research>(); }
	}

	bool IGuiOverlayScreen.HighlightOverlayButton
	{
		get { return Ftue.IsActionActive<FtueAction.Research>(); }
	}
}
