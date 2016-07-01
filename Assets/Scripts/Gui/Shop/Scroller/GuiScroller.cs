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

//Pozadavky zjistene experimentovanim:
//- scroling po pustene ma mit setrvacnost (pokracuje v puvodnim pohybu a jeho rychlost se zmensuje)
//- snaping pri setrvacnosti: Kdyz klesne rychlost pod urcitou hodnotu, doscrolujeme na nejblizsi item. Vzdy scrolujeme na nejblizsi item ve smeru pohybu, vraceni zpet nepusobi prirozene.
//		Mozna bude potrebovat rozlisovat jakou mame aktualni setrvacnost vzhledem k pozici slideru; pokud je jiz mala a blizime se k nove bunce, zastavit ji predcasne.
// 		Mozna nechat scrollovat a zastavit presna na pozici itemu pokud je rychlost mensi nez urcity trashhold (ale hrozi ze momentum nebude stacit)
//- snaping pri scrolovani: 
//		TODO: vymyslet 
//- ohraniceni a pretahnuti pres ne:
//		 Kdy udelame velmi rychly swipe, muzeme prejet o vice jak povoleny presah.
//- obecne: tohle se chova jako prototyp, vsechny uzivatelske casti nepusobi dostatecne plynule. Mozna najit jinou datavou reprezentaci a jine algoritmy pro scrolling (vice podle fyziky?).

// Trida resi pouze scroling seznamem itemu po zarazky a zjisteni ktery item je focusnuty (ve stredu).
// K tomu potrebujeme:
//  Seznam predmetu (jejich id a widget; pod widgetem mohou byt nalinkovany dalsi slozky jako sprite, label, number, etc).
//  Rozestupy mezi predmety (tohle by slo pripadne i generovat ze sirky widgetu)
//  Background gui (to co se zobrazuje pri zobrazenis scrolleru)
//  

public abstract class IScrollItem
{
	public abstract void Show();
	public abstract void Hide();
	public abstract void UpdateItemInfo();
};

class GuiScroller<Key> where Key : System.IComparable<Key>, new()
{
	class Item<TKey> : System.IComparable<Item<TKey>> where TKey : System.IComparable<TKey>, new()
	{
		//public delegate void DrawDelegte(TKey key, GUIBase_Widget w);
		//public DrawDelegte m_DrawDelegate;

		public Item(TKey id, GUIBase_Widget w, IScrollItem itemGui)
		{
			m_UID = id;
			m_Widget = w;
			m_ItemGui = itemGui;
		}

		public int CompareTo(Item<TKey> other)
		{
			return m_UID.CompareTo(other.m_UID);
		}

		public TKey m_UID;
		public GUIBase_Widget m_Widget;
		public IScrollItem m_ItemGui;
	};

	public delegate void ChangeDelegate(Key selectedItem);
	public ChangeDelegate m_OnSelectionChange;

	public delegate void HoldDelegate(int itemIndex, Key itemId);
	public HoldDelegate m_OnHoldBegin = null;
	public delegate void HoldEndDelegate();
	public HoldEndDelegate m_OnHoldEnd = null;

	int m_LastItem = -1;
	bool m_ControlsEnabled = true;

	Rect m_ScrollRect = new Rect();

	class Transition
	{
		public Transition(float From, float To, float Duration, Tween.EasingFunc Easing)
		{
			m_From = From;
			m_To = To;
			m_BeginTime = Time.time;
			m_Duration = Duration;
			m_Easing = Easing ?? Tween.Easing.Linear.EaseNone;
		}

		float m_From;
		float m_To;
		float m_BeginTime;
		float m_Duration;
		Tween.EasingFunc m_Easing;

		public float GetTransition()
		{
			return IsDone() ? m_To : m_Easing(Time.time - m_BeginTime, m_From, m_To - m_From, m_Duration);
		}

		public bool IsDone()
		{
			return (Time.time > (m_BeginTime + m_Duration));
		}
	}

	enum E_ScrollMode
	{
		Drag,
		Momentum,
		Anim,
		Idle,
	};

	List<Item<Key>> m_Items = new List<Item<Key>>();
	GUIBase_Pivot m_ScrollPivot; //pivot skrze ktery scrolujeme se vsemi itemy
	GUIBase_Layout m_BackgroundLayout;
	GUIBase_Widget m_SelectionBackground;
	GUIBase_Widget m_SelectionArrow;

	public bool IsScrolling
	{
		get { return m_ScrollMode != E_ScrollMode.Idle ? true : false; }
	}

	public float ItemOffset { get; private set; }
	int m_CurrentItemIndex = -1;
	float m_ScrollLimitMin = 0;
	float m_ScrollLimitMax = 0;
	float m_ScrollLimitHardMin = 0;
	float m_ScrollLimitHardMax = 0;
	bool m_SelectionBackgroundVisible = false;

	GuiDragInput m_DragInput = new GuiDragInput();
	Transition m_Transition;
	E_ScrollMode m_ScrollMode = E_ScrollMode.Idle;
	Tween.Tweener m_Tweener = new Tween.Tweener();

	bool m_WasHolding;

	public void Show()
	{
		MFGuiManager.Instance.ShowPivot(m_ScrollPivot, true);
		MFGuiManager.Instance.ShowLayout(m_BackgroundLayout, true);

		UpdateItemsViews();
		/*foreach(Item<Key> itm in m_Items)
		{
			itm.m_ItemGui.UpdateItemInfo();
			itm.m_ItemGui.Show();
		}*/
	}

	public void Hide()
	{
		MFGuiManager.Instance.ShowPivot(m_ScrollPivot, false);
		MFGuiManager.Instance.ShowLayout(m_BackgroundLayout, false);
		m_Tweener.StopTweens(true);
	}

	public void FadeIn()
	{
		Show();
		//Debug.Log("Scroller FadeIn");
		m_BackgroundLayout.FadeAlpha = 0;
		m_Tweener.TweenTo(m_BackgroundLayout, "m_FadeAlpha", 1.0f, 0.1f, null, OnFadeInUpdate);
	}

	public void FadeOut()
	{
		//Debug.Log("Scroller FadeOut");
		m_Tweener.TweenTo(m_BackgroundLayout, "m_FadeAlpha", 0.0f, 0.05f, null, OnFadeOutUpdate);
	}

	public void EnableControls()
	{
		m_ControlsEnabled = true;
	}

	public void DisableControls()
	{
		m_ControlsEnabled = false;
	}

	public void UpdateItemsViews()
	{
		foreach (Item<Key> itm in m_Items)
		{
			itm.m_ItemGui.UpdateItemInfo();
			itm.m_ItemGui.Show(); //TODO: nemuseli bychom volat show ale pouze zmeny podle aktualniho stavu
		}
	}

	public void InitGui(GUIBase_Layout bgLayout, GUIBase_Pivot scrollPivot)
	{
		m_ScrollPivot = scrollPivot;
		m_BackgroundLayout = bgLayout;

		m_SelectionBackground = m_BackgroundLayout.GetWidget("Sprite_selection");
		m_SelectionArrow = m_BackgroundLayout.GetWidget("Sprite_sipka");

		/*
		//setup scroll area
		GUIBase_Sprite areaSprite = GuiBaseUtils.PrepareSprite(m_BackgroundLayout, "ActiveArea_Sprite");
		Vector2 pos = new Vector2(areaSprite.Widget.transform.localPosition.x - areaSprite.Widget.m_Width/2, areaSprite.Widget.transform.localPosition.y + areaSprite.Widget.m_Height/2);
		pos = m_BackgroundLayout.LayoutSpacePosToScreen(pos);
		Vector2 size = new Vector2(areaSprite.Widget.m_Width, areaSprite.Widget.m_Height);
		size = m_BackgroundLayout.LayoutSpaceDeltaToScreen(size);
		Rect scrollRect = new Rect(pos.x, pos.y, size.x, size.y);
		*/
		GUIBase_Sprite areaSprite = GuiBaseUtils.PrepareSprite(m_BackgroundLayout, "ActiveArea_Sprite");
		m_ScrollRect = areaSprite.Widget.GetRectInScreenCoords();
		m_ScrollRect.center = new Vector2(m_ScrollRect.center.x, Screen.height - m_ScrollRect.center.y);

		m_DragInput.SetActiveArea(m_ScrollRect);

		//TODO: find distance between items (podle sirky widgetu nebo podle vzdalenosti mezi dvema widgety)
		//ted to delame rucne podle sirky spritu ramecku v 'ShopCore\Scroller_Layout\Graphic_Pivot'
		ItemOffset = 312;

		m_DragInput.isHorizontal = true;
	}

	public void Clear()
	{
		m_Items.Clear();
		m_CurrentItemIndex = -1;
		//Debug.Log("Clear ");
	}

	public void HideItems()
	{
		foreach (Item<Key> itm in m_Items)
		{
			itm.m_ItemGui.Hide();
		}
	}

	public void AddItem(Key uid, GUIBase_Widget w, IScrollItem itemGui)
	{
		//Debug.Log("Adding item: " + uid);

		Item<Key> itm = new Item<Key>(uid, w, itemGui);
		int curIndex = m_Items.Count;
		w.transform.localPosition = new Vector2(ItemOffset*curIndex, w.transform.localPosition.y);
		w.SetModify(true);
		m_Items.Add(itm);
		ComputeScrollLimits();
	}

	void ComputeScrollLimits()
	{
		float border = ItemOffset*0.25f;
		m_ScrollLimitMax = 0 + border;
		m_ScrollLimitMin = -(ItemOffset*(m_Items.Count - 1) + border);

		float borderHard = ItemOffset*0.5f;
		m_ScrollLimitHardMax = borderHard;
		m_ScrollLimitHardMin = -(ItemOffset*(m_Items.Count - 1) + borderHard);
	}

	public bool HasSelection()
	{
		//Debug.Log("m_CurrentItemIndex: " + m_CurrentItemIndex + " m_Items.Count: " + m_Items.Count);
		return (m_CurrentItemIndex >= 0 && m_CurrentItemIndex < m_Items.Count);
	}

	//vraci uid vybrane zbrane
	public Key GetSelectedItem()
	{
		if (m_CurrentItemIndex < 0 || m_CurrentItemIndex >= m_Items.Count)
		{
			//Debug.LogWarning("Trying to get selection before items inserted, or selection set. (items: " + m_Items.Count + " , selection: " + m_CurrentItemIndex + " )");
			return new Key();
		}
		return m_Items[m_CurrentItemIndex].m_UID;
	}

	public Key GetItemOverMouse()
	{
		bool dontDisplayWhenNoTouchOnAndroid = false;
#if UNITY_ANDROID || UNITY_IPHONE
		dontDisplayWhenNoTouchOnAndroid = Input.touchCount < 1;
#endif

		if (!m_ScrollRect.Contains(Input.mousePosition) || dontDisplayWhenNoTouchOnAndroid)
		{
			//		Debug.Log("Outside of scroller");
			return new Key();
		}

		float currentScrollPos = -m_ScrollPivot.transform.localPosition.x;
		//prepocitej pozici na screenu na vzdalenost od stredu scrolleru
		float distToCenter = Input.mousePosition.x - Screen.width/2;
		Vector2 layoutTapPos = m_BackgroundLayout.ScreenPosToLayoutSpace(new Vector2(distToCenter, 0)); //todo: vertical scrollbar
		//
		int index = (int)((currentScrollPos + layoutTapPos.x + ItemOffset/2)/ItemOffset);
		//	Debug.Log("TAP: scroller pos " + currentScrollPos + " tap pos: " + pos + " distToCenter " + distToCenter + " layout dist: " + layoutTapPos.x + " index " + index);

		if (index < 0 || index >= m_Items.Count)
		{
			return new Key();
		}

		return m_Items[index].m_UID;
	}

	public void SetSelectedItem(Key uid)
	{
		int idx = GetItemByUid(uid);

		SetSelectedItem(idx);
	}

	public void ScrollToItem(Key uid)
	{
		int idx = GetItemByUid(uid);

		ScrollToItem(idx);
	}

	int GetItemByUid(Key uid)
	{
		//pokud je platne uid, pokus se jej najit v seznamu
		int idx = FindItemIndex(uid);

		//pokud jsme nic nenalezli, scrolujeme na prvni polozku
		if (idx == -1)
		{
			//Debug.Log("Item not found, scrolling to beginning. Id " + uid);
			idx = 0;
		}

		return idx;
	}

	//Zjisti index itemu ktery je nejbliz ke stredu okenka s vyberem.
	int GetNearestItem(float lastScrollDir)
	{
		float itemTrashold = ItemOffset*0.5f;

		if (lastScrollDir > 0) //scroluje vpravo, chce zustat na aktualnim
			itemTrashold = ItemOffset*0.1f;
		else if (lastScrollDir < 0) //scrolujeme vlevo, chcem najit nejblizsi vpravo
			itemTrashold = ItemOffset*0.9f;

		float currentScrollPos = -m_ScrollPivot.transform.localPosition.x;

		int index = (int)((currentScrollPos + itemTrashold)/ItemOffset);
		//Debug.Log("orig pos " + currentScrollPos + " trashold " + itemTrashold + " position: " + (currentScrollPos + itemTrashold) + " index " + index);
		return index;
	}

	int GetCurrentItem()
	{
		float itemTrashold = ItemOffset*0.5f;
		float currentScrollPos = -m_ScrollPivot.transform.localPosition.x;
		int curItem = (int)((currentScrollPos + itemTrashold)/ItemOffset);
		return Mathf.Clamp(curItem, 0, m_Items.Count - 1);
	}

	int GetItemOnPos(float pos)
	{
		float currentScrollPos = -m_ScrollPivot.transform.localPosition.x;
		//prepocitej pozici na screenu na vzdalenost od stredu scrolleru
		float distToCenter = pos - Screen.width/2;
		Vector2 layoutTapPos = m_BackgroundLayout.ScreenPosToLayoutSpace(new Vector2(distToCenter, 0)); //todo: vertical scrollbar
		//
		int index = (int)((currentScrollPos + layoutTapPos.x + ItemOffset/2)/ItemOffset);
		//	Debug.Log("TAP: scroller pos " + currentScrollPos + " tap pos: " + pos + " distToCenter " + distToCenter + " layout dist: " + layoutTapPos.x + " index " + index);
		return index;
	}

	//Doscroluje k itemu na pozici index.
	void SetSelectedItem(int index)
	{
		//reset scroll mode and remove transition
		m_Transition = null;
		m_ScrollMode = E_ScrollMode.Idle;

		//Debug.Log("SetSelectedItem: " + index);
		m_ScrollPivot.transform.localPosition = new Vector2(-index*ItemOffset, m_ScrollPivot.transform.localPosition.y);
						//changed y value because value 0 broke layout in ResearchScroller_Layout
		m_ScrollPivot.SetModify(true);

		//store index of selected item
		m_CurrentItemIndex = index;

		//notify user
		if (m_OnSelectionChange != null)
			m_OnSelectionChange(GetSelectedItem());
	}

	//Doscroluje k itemu na pozici index.
	void ScrollToItem(int index)
	{
		if (m_CurrentItemIndex == index)
			return;

		//Debug.Log("ScrollToItem: " + index);
		m_ScrollMode = E_ScrollMode.Anim;

		//float distToItem = Mathf.Abs(m_ScrollPivot.transform.localPosition.x + index*ItemOffset);
		int scrCnt = Mathf.Abs(m_CurrentItemIndex - index);
		float scrollTime = 0.5f + (scrCnt - 1)*0.15f;

		//create transition anim
		m_Transition = new Transition(m_ScrollPivot.transform.localPosition.x, -(index*ItemOffset), scrollTime, Tween.Easing.Sine.EaseInOut);

		//store index of selected item
		m_CurrentItemIndex = index;

		//notify user by nemel byt potreba, k notifikaci by melo dojit na konci transition.
		//if(m_OnSelectionChange != null)
		//	m_OnSelectionChange( GetSelectedItem() );
	}

	int FindItemIndex(Key uid)
	{
		//Debug.Log("compare item: " + uid.ToString());
		for (int index = 0; index < m_Items.Count; index++)
		{
			if (m_Items[index].m_UID.Equals(uid))
				return index;
		}
		return -1;
	}

	void MoveScrollerPivot(Vector2 delta)
	{
		Vector2 newDelta = m_BackgroundLayout.ScreenDeltaToLayoutSpace(delta);
		Vector3 curPos = m_ScrollPivot.transform.localPosition;

		//if(Mathf.Abs(delta.x) > 0)
		//	Debug.Log("delta: " + delta.x + " layoutDelta: " +  newDelta.x);

		curPos.x += newDelta.x;
		curPos.y += newDelta.y;

		curPos.x = Mathf.Clamp(curPos.x, m_ScrollLimitHardMin, m_ScrollLimitHardMax);

		m_ScrollPivot.transform.localPosition = curPos;
		m_ScrollPivot.SetModify(true);
	}

	bool AdjustScrollToLimits()
	{
		Vector3 curPos = m_ScrollPivot.transform.localPosition;
		if (curPos.x > m_ScrollLimitMax)
		{
			return true;
		}
		else if (curPos.x < m_ScrollLimitMin)
		{
			return true;
		}
		return false;
	}

	public void Update()
	{
		if (m_ScrollPivot == null)
			return;

		if (m_ControlsEnabled)
		{
			UpdateMouseScroll();
			UpdateControls();
			UpdateState();
			NotifySelectionChange();
		}

		UpdateSelectionBackground(!IsScrolling);
		UpdateTweener();
	}

	void UpdateMouseScroll()
	{
		float scroll = 0;

		try
		{
			scroll = Input.GetAxis("Mouse ScrollWheel");
		}
		catch
		{
			return;
		}

		int item = GetCurrentItem();

		//scroll nahoru (pøedchozí item)
		if (scroll > 0 && item > 0)
		{
			SetSelectedItem(item - 1);
		}
		//scroll dolu (další item)
		else if (scroll < 0 && item < (m_Items.Count - 1))
		{
			SetSelectedItem(item + 1);
		}
	}

	void UpdateControls()
	{
		//	Debug.Log("m_ScrollMode " + m_ScrollMode);
		m_DragInput.Update();

		//check for tap on item
		DetectTap();

		//check for hold begin/end event
		DetectHold();
	}

	void UpdateState()
	{
		//mode changes
		//faze: 1) tahneme scroller 																-> touch begin
		//		2) pustime ho ale jeste ma setrvacnost 												-> touch end
		//		3) setrvacnost klesne pod urcitou hodnotu, doscroloujeme na nejblizsi item			-> momentum klesne pod limit
		//		4) ukonceni scrollu, dal jsme v klidu dokud znova nezacne opet 1)					-> ukonceni transition
		if (m_DragInput.IsDragging && Mathf.Abs(m_DragInput.ScrollDelta.x) > 0.0f /*&& !AdjustScrollToLimits()*/)
		{
			m_ScrollMode = E_ScrollMode.Drag;
			m_Transition = null;
			UpdateStateDrag();
		}
		else
		{
			switch (m_ScrollMode)
			{
			case E_ScrollMode.Idle:
				UpdateStateIdle();
				break;
			case E_ScrollMode.Drag:
				UpdateStateDrag();
				break;
			case E_ScrollMode.Momentum:
				UpdateStateMomentum();
				break;
			case E_ScrollMode.Anim:
				UpdateStateAnim();
				break;
			}
		}
	}

	void NotifySelectionChange()
	{
		//notify about selection change
		int curItem = GetCurrentItem();
		if (m_LastItem != curItem)
		{
			//Debug.Log("Cur item " + curItem + " last: " + m_LastItem + " callback: " + (m_OnSelectionChange!=null));
			m_LastItem = curItem;
			m_CurrentItemIndex = curItem;
							//TODO: m_CurrentItemIndex by sel pravdepodobne odstranit a misto nej by se mohlo pracovat jen s GetCurrentItem()

			if (m_OnSelectionChange != null)
				m_OnSelectionChange(GetSelectedItem());
		}
	}

	void DetectTap()
	{
		if (m_DragInput.tapEvent)
		{
			int tapIndex = GetItemOnPos(m_DragInput.tapEventPos);
			m_DragInput.ClearTapEvent();

			tapIndex = Mathf.Clamp(tapIndex, 0, m_Items.Count - 1);

			ScrollToItem(tapIndex);
		}
	}

	void DetectHold()
	{
		//check for hold begin/end event
		if (!m_WasHolding && m_DragInput.isHolding)
		{
			//zjistit na kterem itemu drzime:
			int holdIndex = GetItemOnPos(m_DragInput.holdingPos);
			//Debug.Log("Hold begin " + holdIndex);

			if (holdIndex < 0 || holdIndex >= m_Items.Count)
				return;

			Key holdID = m_Items[holdIndex].m_UID;

			if (m_OnHoldBegin != null)
				m_OnHoldBegin(holdIndex, holdID);

			m_WasHolding = true;
		}
		else if (m_WasHolding && !m_DragInput.isHolding)
		{
			//Debug.Log("Hold end");
			if (m_OnHoldEnd != null)
				m_OnHoldEnd();

			m_WasHolding = false;
		}
	}

	void UpdateStateIdle()
	{
		if (m_DragInput.IsDragging && Mathf.Abs(m_DragInput.ScrollDelta.x) > 0.0f)
		{
			m_ScrollMode = E_ScrollMode.Drag;
			MoveScrollerPivot(m_DragInput.ScrollDelta);
		}
	}

	void UpdateStateDrag()
	{
		if (m_DragInput.IsDragging && Mathf.Abs(m_DragInput.ScrollDelta.x) > 0.0f)
		{
			MoveScrollerPivot(m_DragInput.ScrollDelta);
			if (AdjustScrollToLimits())
			{
				m_ScrollMode = E_ScrollMode.Anim;

				int stopItemIndex = GetNearestItem(m_DragInput.ScrollDelta.x);
				m_CurrentItemIndex = Mathf.Clamp(stopItemIndex, 0, m_Items.Count - 1);
				m_Transition = new Transition(m_ScrollPivot.transform.localPosition.x,
											  -(m_CurrentItemIndex*ItemOffset),
											  0.35f,
											  Tween.Easing.Linear.EaseNone);
			}
		}
		else
		{
			m_ScrollMode = E_ScrollMode.Momentum;
		}
	}

	void UpdateStateMomentum()
	{
		MoveScrollerPivot(m_DragInput.ScrollDelta);
		bool switchToAnim = !m_DragInput.HasMomentum() || AdjustScrollToLimits();

		if (switchToAnim)
		{
			m_ScrollMode = E_ScrollMode.Anim;

			int stopItemIndex = GetNearestItem(m_DragInput.ScrollDelta.x);
			m_CurrentItemIndex = Mathf.Clamp(stopItemIndex, 0, m_Items.Count - 1);
			//Debug.Log("m_CurrentItemIndex is set to: " + m_CurrentItemIndex);

			//z aktualni rychlosti a vzdalenosti vypocitej cas za jaky tam dorazime
			float distToItem = Mathf.Abs(m_ScrollPivot.transform.localPosition.x + m_CurrentItemIndex*ItemOffset);
			float curSpeed = Mathf.Max(m_DragInput.MoveSpeed, m_DragInput.MinSpeed);
			float timeToScroll = distToItem/curSpeed;
			//Debug.Log("Finishing anim - timeToScroll: " + timeToScroll + " dist: " + distToItem + " move speed: " + curSpeed);

			//create transition anim
			m_Transition = new Transition(m_ScrollPivot.transform.localPosition.x,
										  -(m_CurrentItemIndex*ItemOffset),
										  timeToScroll,
										  Tween.Easing.Quad.EaseOut);
		}
	}

	void UpdateStateAnim()
	{
		Vector2 curPos = m_ScrollPivot.transform.localPosition;
		curPos.x = m_Transition.GetTransition();
		m_ScrollPivot.transform.localPosition = curPos;
		m_ScrollPivot.SetModify(true);
		if (m_Transition.IsDone())
		{
			m_Transition = null;
			m_ScrollMode = E_ScrollMode.Idle;

			if (m_OnSelectionChange != null)
				m_OnSelectionChange(GetSelectedItem());
		}
	}

	void UpdateSelectionBackground(bool state)
	{
		if (m_SelectionBackgroundVisible != state)
		{
			m_SelectionBackgroundVisible = state;

			m_Tweener.TweenTo(m_SelectionBackground, "m_FadeAlpha", state ? 1.0f : 0.0f, state ? 0.1f : 0.05f);
			m_Tweener.TweenTo(m_SelectionArrow, "m_FadeAlpha", state ? 1.0f : 0.0f, state ? 0.2f : 0.01f);
		}
	}

	void UpdateTweener()
	{
		if (m_Tweener.IsTweening == true)
		{
			m_Tweener.UpdateTweens();
		}
	}

	void OnFadeInUpdate(Tween.Tweener.Tween tween, bool finished)
	{
		WidgetsModify();
	}

	void OnFadeOutUpdate(Tween.Tweener.Tween tween, bool finished)
	{
		WidgetsModify();

		if (finished)
		{
			Hide();
		}
	}

	void WidgetsModify()
	{
		m_SelectionBackground.SetModify();
		m_SelectionArrow.SetModify();
		m_BackgroundLayout.SetModify(true);
	}
};
