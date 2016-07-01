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
//  - GuiResearchItem: zobrazeni polozky rersearche ve scrolleru
class GuiResearchScroller
{
	GameObject m_ScrollBarPrefab;
	GuiScroller<int> m_ScrollInventory = new GuiScroller<int>();
	const int maxScrollItems = 10;
	List<GUIBase_Widget> m_ScrollCache = new List<GUIBase_Widget>();
	GUIBase_Sprite[] m_Icons;
	int[] labelsText = {0110120, 0110110, 0110130, 0110140, 0110220, 0110210, 0110230, 0110310, 0110330, 0110320};

	public GuiResearchScroller(GameObject ScrollBarPrefab)
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

			w.transform.parent = m_ScrollBarPrefab.transform.parent;
			w.transform.localScale = m_ScrollBarPrefab.transform.localScale;
			w.transform.localRotation = m_ScrollBarPrefab.transform.localRotation;
			w.transform.localPosition = m_ScrollBarPrefab.transform.localPosition;

			w.SetModify(true);
			m_ScrollCache.Add(w);
		}
	}

	public void Update()
	{
		//Debug.Log("AA");
		m_ScrollInventory.Update();
	}

	public void InitGui()
	{
		GUIBase_Pivot Pivot = MFGuiManager.Instance.GetPivot("MainResearch");
		GUIBase_Layout Layout = Pivot.GetLayout("ResearchScroller_Layout");

		GUIBase_Pivot scrollerPivot = GuiBaseUtils.GetChild<GUIBase_Pivot>(Layout, "Scroller");
		m_ScrollInventory.InitGui(Layout, scrollerPivot);

		GUIBase_Widget iconsRoot = Layout.GetWidget("ResearchIcons");
		m_Icons = new GUIBase_Sprite[maxScrollItems];
		for (int i = 0; i < maxScrollItems; i++)
		{
			m_Icons[i] = GuiBaseUtils.GetChildSprite(iconsRoot, "Icon" + i);
		}

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

	public void RegisterOnSelectionDelegate(GuiScroller<int>.ChangeDelegate onSelectionChange)
	{
		m_ScrollInventory.m_OnSelectionChange = onSelectionChange;
	}

	public void Insert(List<int> items, bool hideOwnedHack)
	{
		m_ScrollInventory.HideItems();
		m_ScrollInventory.Clear();

		if (items.Count > m_ScrollCache.Count)
			Debug.LogError("Scroll cache too small: size " + m_ScrollCache.Count + ", required " + items.Count);

		items.Sort();

		for (int i = 0; i < items.Count; i++)
		{
			//Debug.Log(" " + items.Count + " " + m_ScrollCache.Count + " " + items[i] + m_ScrollCache[i]);
			int buttonId = items[i];
			int textId = labelsText[i];
			GUIBase_Widget rootSprite = m_ScrollCache[i];
			GUIBase_Sprite icon = m_Icons[i];

			m_ScrollInventory.AddItem(buttonId, rootSprite, new GuiResearchItem(rootSprite, icon, textId));
		}
		//m_ScrollInventory.Sort();
	}

	public void Hide()
	{
		m_ScrollInventory.Hide();
	}

	public void Show()
	{
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

	public void SetSelectedItem(int id)
	{
		m_ScrollInventory.SetSelectedItem(id);
	}

	public void ScrollToItem(int id)
	{
		m_ScrollInventory.ScrollToItem(id);
	}

	public int GetSelectedItem()
	{
		if (m_ScrollInventory.HasSelection())
			return m_ScrollInventory.GetSelectedItem();
		else
			return -1;
	}

	public bool HasSelection()
	{
		return m_ScrollInventory.HasSelection();
	}

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
}
