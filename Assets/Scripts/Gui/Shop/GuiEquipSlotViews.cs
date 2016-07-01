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

//interface pro zobrazeni informaci pro equip slot
abstract class ISlotView
{
	public abstract void Show(ShopItemId id, bool locked, bool waiting, bool premium);
	public abstract void Hide();
};

//zobrazovaci trida pro weapon slot
class SlotViewWeapon : ISlotView
{
	GUIBase_Widget m_RootWidget;
	GUIBase_Label m_NameLabel;
	GUIBase_Sprite m_WeaponSprite;
	GUIBase_Sprite m_WeaponTypeSprite;
	GUIBase_Sprite m_LockSprite;
	GUIBase_Label m_EmptyLabel;
	GUIBase_Sprite m_PremiumSprite;
	GUIBase_Sprite m_PremiumLockedSprite;

	public void InitGui(GUIBase_Layout layout, GUIBase_Widget widget, int slotId)
	{
		m_RootWidget = widget;
		m_NameLabel = GuiBaseUtils.GetChildLabel(widget, "Gun_Label");
		m_WeaponSprite = GuiBaseUtils.GetChildSprite(widget, "Gun_Sprite");
		m_WeaponTypeSprite = GuiBaseUtils.GetChildSprite(widget, "WeaponType_Sprite");
		m_LockSprite = GuiBaseUtils.GetChildSprite(widget, "Lock_Sprite");
		m_EmptyLabel = GuiBaseUtils.GetChildLabel(widget, "Empty_Label");
		if (slotId == 1 || slotId == 2)
		{
			m_PremiumSprite = GuiBaseUtils.GetChildSprite(widget, "PremiumOn_Sprite");
			m_PremiumLockedSprite = GuiBaseUtils.GetChildSprite(widget, "PremiumOnlyLocked_Sprite");
		}
	}

	public override void Show(ShopItemId id, bool locked, bool waiting, bool premium)
	{
		m_RootWidget.Show(true, false);

		//get info for weapon:
		if (id == ShopItemId.EmptyId) //tady by melo byt ve vsech typech slotu  if(id == ShopItemId.EmptyId || locked)
		{
			ShowEmpty(locked, waiting, premium);
		}
		else
		{
			ShopItemInfo inf = ShopDataBridge.Instance.GetItemInfo(id);
			m_NameLabel.SetNewText(inf.NameText);
			m_WeaponSprite.Widget.CopyMaterialSettings(inf.SpriteWidget);
			m_WeaponTypeSprite.Widget.CopyMaterialSettings(inf.WeaponTypeWidget);

			UpdateVisibility(false, locked, waiting, premium);
		}
	}

	void ShowEmpty(bool locked, bool waiting, bool premium)
	{
		//m_NameLabel.SetNewText(0204000 + m_SlotId);

		UpdateVisibility(true, locked, waiting, premium);
	}

	public override void Hide()
	{
		m_RootWidget.Show(false, true);
	}

	void UpdateVisibility(bool empty, bool locked, bool waiting, bool premium)
	{
		m_NameLabel.Widget.FadeAlpha = waiting ? GuiEquipMenu.WAITING_ITEM_ALPHA : 1.0f;
		m_NameLabel.Widget.Show(!empty, true);

		m_WeaponSprite.Widget.FadeAlpha = waiting ? GuiEquipMenu.WAITING_ITEM_ALPHA : 1.0f;
		m_WeaponSprite.Widget.Show(!empty, true);
		m_WeaponTypeSprite.Widget.FadeAlpha = waiting ? GuiEquipMenu.WAITING_ITEM_ALPHA : 1.0f;
		m_WeaponTypeSprite.Widget.Show(!empty, true);

		m_LockSprite.Widget.Show(locked && !premium, true);

		if (m_PremiumSprite != null)
			m_PremiumSprite.Widget.Show(premium && !locked, true);

		if (m_PremiumLockedSprite != null)
			m_PremiumLockedSprite.Widget.Show(premium && locked, true);

		m_EmptyLabel.Widget.Show(empty && !waiting ? !locked : false, true);
	}
};

//zobrazovaci trida pro item slot
class SlotViewItem : ISlotView
{
	GUIBase_Widget m_RootWidget;
	GUIBase_Label m_NameLabel;
	GUIBase_Sprite m_ItemSprite;
	GUIBase_Sprite m_LockSprite;
	GUIBase_Label m_EmptyLabel;
	GUIBase_Sprite m_PremiumSprite;
	GUIBase_Sprite m_PremiumLockedSprite;
	GUIBase_Label m_CountText;

	public void InitGui(GUIBase_Layout layout, GUIBase_Widget widget, int slotId)
	{
		m_RootWidget = widget;
		m_NameLabel = GuiBaseUtils.GetChildLabel(widget, "Item_Label");
		m_ItemSprite = GuiBaseUtils.GetChildSprite(widget, "Item_Sprite");
		m_LockSprite = GuiBaseUtils.GetChildSprite(widget, "Lock_Sprite");
		m_EmptyLabel = GuiBaseUtils.GetChildLabel(widget, "Empty_Label");
		m_CountText = GuiBaseUtils.GetChildLabel(widget, "CountText");

		if (slotId == 1 || slotId == 2)
		{
			m_PremiumSprite = GuiBaseUtils.GetChildSprite(widget, "PremiumOn_Sprite");
			m_PremiumLockedSprite = GuiBaseUtils.GetChildSprite(widget, "PremiumOnlyLocked_Sprite");
		}
	}

	public override void Show(ShopItemId id, bool locked, bool waiting, bool premium)
	{
		m_RootWidget.Show(true, false);

		//Debug.Log("Showing Item Slot: " + id + " lck: " + locked);

		//get info for weapon:
		if (id == ShopItemId.EmptyId)
		{
			ShowEmpty(locked, waiting, premium);
		}
		else
		{
			ShopItemInfo inf = ShopDataBridge.Instance.GetItemInfo(id);
			m_NameLabel.SetNewText(inf.NameText);
			m_ItemSprite.Widget.CopyMaterialSettings(inf.SpriteWidget);
			m_CountText.SetNewText(inf.OwnedCount.ToString());

			UpdateVisibility(false, locked, waiting, premium, inf.Consumable);
		}
	}

	void ShowEmpty(bool locked, bool waiting, bool premium)
	{
		//m_NameLabel.SetNewText(209001 + m_SlotId);

		UpdateVisibility(true, locked, waiting, premium, false);
	}

	public override void Hide()
	{
		m_RootWidget.Show(false, true);
	}

	void UpdateVisibility(bool empty, bool locked, bool waiting, bool premium, bool consumable)
	{
		m_NameLabel.Widget.FadeAlpha = waiting ? GuiEquipMenu.WAITING_ITEM_ALPHA : 1.0f;
		m_NameLabel.Widget.Show(!empty, true);

		m_ItemSprite.Widget.FadeAlpha = waiting ? GuiEquipMenu.WAITING_ITEM_ALPHA : 1.0f;
		m_ItemSprite.Widget.Show(!empty, true);

		m_CountText.Widget.FadeAlpha = waiting ? GuiEquipMenu.WAITING_ITEM_ALPHA : 1.0f;
		m_CountText.Widget.Show(!empty && consumable, true);

		m_LockSprite.Widget.Show(locked && !premium, true);

		if (m_PremiumSprite != null)
			m_PremiumSprite.Widget.Show(premium && !locked, true);

		if (m_PremiumLockedSprite != null)
			m_PremiumLockedSprite.Widget.Show(premium && locked, true);

		m_EmptyLabel.Widget.Show(empty && !waiting ? !locked : false, true);
	}
};

//zobrazovaci trida pro Hat, Perk a Skin
class SlotViewSimple : ISlotView
{
	GUIBase_Widget m_RootWidget;
	GUIBase_Label m_NameLabel;

	public void InitGui(GUIBase_Layout layout, GUIBase_Widget widget)
	{
		m_RootWidget = widget;
		m_NameLabel = GuiBaseUtils.GetChildLabel(widget, "Label");
	}

	public override void Show(ShopItemId id, bool locked, bool waiting, bool premium)
	{
		m_RootWidget.Show(true, false);

		//get info 
		if (id == ShopItemId.EmptyId)
		{
			ShowEmpty(locked, waiting);
		}
		else
		{
			ShopItemInfo inf = ShopDataBridge.Instance.GetItemInfo(id);
			m_NameLabel.SetNewText(inf.NameText);

			UpdateVisibility(locked, waiting);
		}
	}

	void ShowEmpty(bool locked, bool waiting)
	{
		m_NameLabel.SetNewText(02050036); //empty text

		UpdateVisibility(locked, waiting);
	}

	public override void Hide()
	{
		m_RootWidget.Show(false, true);
	}

	void UpdateVisibility(bool locked, bool waiting)
	{
		m_NameLabel.Widget.Show(!waiting ? true : false, true);
	}
};

//perk
class SlotViewPerk : ISlotView
{
	GUIBase_Widget m_RootWidget;
	GUIBase_Label m_NameLabel;
	GUIBase_Sprite m_PerkSprite;
	GUIBase_Sprite m_LockSprite;
	GUIBase_Label m_EmptyLabel;

	public void InitGui(GUIBase_Layout layout, GUIBase_Widget widget)
	{
		m_RootWidget = widget;
		m_NameLabel = GuiBaseUtils.GetChildLabel(widget, "Label");
		m_PerkSprite = GuiBaseUtils.GetChildSprite(widget, "Perk_Sprite");
		m_LockSprite = GuiBaseUtils.GetChildSprite(widget, "Lock_Sprite");
		m_EmptyLabel = GuiBaseUtils.GetChildLabel(widget, "Empty_Label");
	}

	public override void Show(ShopItemId id, bool locked, bool waiting, bool premium)
	{
		m_RootWidget.Show(true, false);

		//get info 
		if (id == ShopItemId.EmptyId)
		{
			ShowEmpty(locked, waiting);
		}
		else
		{
			ShopItemInfo inf = ShopDataBridge.Instance.GetItemInfo(id);
			m_NameLabel.SetNewText(inf.NameText);
			m_PerkSprite.Widget.CopyMaterialSettings(inf.SpriteWidget);

			UpdateVisibility(false, locked, waiting);
		}
	}

	void ShowEmpty(bool locked, bool waiting)
	{
		m_NameLabel.SetNewText(02050036); //empty text

		UpdateVisibility(true, locked, waiting);
	}

	public override void Hide()
	{
		m_RootWidget.Show(false, true);
	}

	void UpdateVisibility(bool empty, bool locked, bool waiting)
	{
		m_NameLabel.Widget.Show(!(waiting || empty), true);

		m_PerkSprite.Widget.FadeAlpha = waiting ? GuiEquipMenu.WAITING_ITEM_ALPHA : 1.0f;
		m_PerkSprite.Widget.Show(!empty, true);

		m_EmptyLabel.Widget.Show((empty && !waiting) ? !locked : false, true);
		m_LockSprite.Widget.Show(locked, true);
	}
};

//zobrazovaci trida pro skin (model hrace)
/*class SlotViewSkin: ISlotView
{
	GUIBase_Widget  m_RootWidget;
	GUIBase_Label 	m_NameLabel;
	//GUIBase_Sprite  m_Skin_Sprite;
	
	public void InitGui(GUIBase_Layout layout, GUIBase_Widget widget)
	{
		m_RootWidget = widget;
		m_NameLabel = GuiBaseUtils.GetChildLabel(widget, "Label");
		//m_Skin_Sprite = GuiBaseUtils.GetChildSprite(widget, "Skin_Sprite"); 
	}

	override public void Show(ShopItemId id, bool locked, bool waiting)
	{
		m_RootWidget.Show(true, false);
		
		//Debug.Log("Showing item: " + id.ItemType + " " + id.Id);
		
		if(id == ShopItemId.EmptyId)
		{
			//Debug.LogError("We expect allways some skin active");
		}
		else
		{
			ShopItemInfo inf = ShopDataBridge.Instance.GetItemInfo(id);
			m_NameLabel.SetNewText( inf.NameText );
			//m_Skin_Sprite.Widget.CopyMaterialSettings( inf.SpriteWidget );
			
			UpdateVisibility(locked, waiting);
		}
		
	}
	
	override public void Hide()
	{
		m_RootWidget.Show(false, true);
	}
	
	private void UpdateVisibility(bool locked, bool waiting)
	{
		m_NameLabel.Widget.Show(!waiting ? true : false, true);
		//m_ItemSprite.Widget.Show(false, true);
		//m_EmptyLabel.Widget.Show(true, true);
	}
};*/
