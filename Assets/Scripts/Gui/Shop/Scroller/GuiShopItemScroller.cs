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

//Implementace scrolleru pro shop, equip a funds.
//Pouziva tridy:
//  - GuiDragInput; omplementace ovladani touchem a mysi
//  - GuiScroller: implementuje mechanismus scrollovani, vkladani polozek podle klice a zobrazovani pres view interface IScrollItem
//  - GuiScrollItem: zobrazeni polozky shopu ve scrolleru
class GuiShopItemScroller
{
	GameObject m_ScrollBarPrefab;
	GuiScroller<ShopItemId> m_ScrollInventory = new GuiScroller<ShopItemId>();
	const int maxScrollItems = 40;
	List<GUIBase_Widget> m_ScrollCache = new List<GUIBase_Widget>();
	//GuiShopInfoPopup m_InfoPopup		= new GuiShopInfoPopup();  //TODO: tohle byl rychly pokus, melo by to pracovat pouze s abstraktnim interfacem, nebo jen notifikovat ze se ma zobrazit popup.

	public GuiShopItemScroller(GameObject ScrollBarPrefab)
	{
		m_ScrollBarPrefab = ScrollBarPrefab;
		CreateItemsCache();
	}

	public bool IsScrolling
	{
		get { return m_ScrollInventory.IsScrolling; }
	}

	//Pokud tuhle metodu zavolame tesne pred naslednym pouzitim vytvoreneho widgetu, dostameme crash, protoze gui manager nenajde zaregistrovane child widgety (na jednotlivych wedgetech se jeste nezavolal sprite)
	void CreateItemsCache()
	{
		m_ScrollCache.Clear();
		for (int i = 0; i < maxScrollItems; i++)
		{
			GameObject o = MonoBehaviour.Instantiate(m_ScrollBarPrefab) as GameObject;
			GUIBase_Widget w = o.GetComponent<GUIBase_Widget>();
			//if(w == null)
			//	Debug.Log("widget was no found!");

			w.transform.parent = m_ScrollBarPrefab.transform.parent;
			w.transform.localPosition = m_ScrollBarPrefab.transform.localPosition;
			w.transform.localRotation = m_ScrollBarPrefab.transform.localRotation;
			w.transform.localScale = m_ScrollBarPrefab.transform.localScale;
			w.SetModify(true);
			m_ScrollCache.Add(w);
		}
	}

	public void Update()
	{
		m_ScrollInventory.Update();
	}

	public void InitGui()
	{
		GUIBase_Pivot Pivot = MFGuiManager.Instance.GetPivot("ShopMenu");
		GUIBase_Layout Layout = Pivot.GetLayout("Scroller_Layout");

		GUIBase_Pivot scrollerPivot = GuiBaseUtils.GetChild<GUIBase_Pivot>(Layout, "Scroll_Pivot");
		m_ScrollInventory.InitGui(Layout, scrollerPivot);

		/*m_InfoPopup.GuiInit();
		m_ScrollInventory.m_OnHoldBegin = ShowInfoPopup;
		m_ScrollInventory.m_OnHoldEnd = HideInfoPopup;*/

		// anchor scroll bar to the bottom of the screen
		if (MFGuiManager.ForcePreserveAspectRatio)
		{
			Transform  trans = Layout.transform;
			Vector3 position = trans.position;

			GUIBase_Widget specialBottomCara = Layout.GetWidget("SpecialBottom_cara");
			Rect bbox = specialBottomCara.GetBBox();
			bbox.y -= position.y;
			position.y = Screen.height - bbox.yMax;
			trans.position = position;
		}
	}

	public void RegisterOnSelectionDelegate(GuiScroller<ShopItemId>.ChangeDelegate onSelectionChange)
	{
		m_ScrollInventory.m_OnSelectionChange = onSelectionChange;
	}

	public void Insert(List<ShopItemId> items, bool hideOwnedHack)
	{
		m_ScrollInventory.HideItems();
		m_ScrollInventory.Clear();

		if (items.Count > m_ScrollCache.Count)
			Debug.LogError("Scroll cache too small: size " + m_ScrollCache.Count + ", required " + items.Count);

		items.Sort();

		for (int i = 0; i < items.Count; i++)
		{
			//Debug.Log(" " + items.Count + " " + m_ScrollCache.Count + " " + items[i] + m_ScrollCache[i]);
			m_ScrollInventory.AddItem(items[i], m_ScrollCache[i], new GuiScrollItem(items[i], m_ScrollCache[i], hideOwnedHack));
		}
		//m_ScrollInventory.Sort();
	}

	public void Hide()
	{
		//Debug.Log("Scroller Hide");
		m_ScrollInventory.Hide();
	}

	public void Show()
	{
		//Debug.Log("Scroller Show");
		m_ScrollInventory.Show();
	}

	public void FadeIn()
	{
		m_ScrollInventory.FadeIn();
	}

	public void FadeOut()
	{
		m_ScrollInventory.FadeOut();
	}

	public void SetSelectedItem(ShopItemId id)
	{
		if (id == null)
			return;

		//Debug.Log("Set selected item: " + id);
		m_ScrollInventory.SetSelectedItem(id);
	}

	public void ScrollToItem(ShopItemId id)
	{
		if (id == null)
			return;

		m_ScrollInventory.ScrollToItem(id);
	}

	public ShopItemId GetSelectedItem()
	{
		//Debug.Log("... m_ScrollInventory.HasSelection(): " + m_ScrollInventory.HasSelection() + " cur index: " + m_ScrollInventory.m_CurrentItemIndex);
		//if(m_ScrollInventory.HasSelection())
		//	Debug.Log( " -------------" + m_ScrollInventory.GetSelectedItem().ItemType + " " + m_ScrollInventory.GetSelectedItem().Id );

		if (m_ScrollInventory.HasSelection())
			return m_ScrollInventory.GetSelectedItem();
		else
			return ShopItemId.EmptyId;
	}

	public ShopItemId GetItemOverMouse()
	{
		return m_ScrollInventory.GetItemOverMouse();
	}

	public bool HasSelection()
	{
		return m_ScrollInventory.HasSelection();
	}

	/*void ShowInfoPopup(int itemIndex, ShopItemId itemId)
	{
		//zjisti pozici na ktere zobrazit popup:
		//spocitej pozici scroll polozky
		//int centerOffsetCount = itemIndex - m_ScrollInventory.m_CurrentItemIndex;  //o kolik policek jsme vzdaleni od stredu scrollbaru 
		//float pos = centerOffsetCount*m_ScrollInventory.ItemOffset;
		//Debug.Log("pos: " + pos + " centerOffsetCount: " + centerOffsetCount);
		
		//m_InfoPopup.Show(itemId, pos); TODO: zprovoznit popup
	}
	
	void HideInfoPopup()
	{
		m_InfoPopup.Hide();
	}*/

	public void EnableControls()
	{
		m_ScrollInventory.EnableControls();
	}

	public void DisableControls()
	{
		m_ScrollInventory.DisableControls();
	}

	public void UpdateItemsViews()
	{
		m_ScrollInventory.UpdateItemsViews();
	}
};
