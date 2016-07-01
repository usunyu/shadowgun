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

public class GuiScrollItem : IScrollItem
{
	GUIBase_Widget m_Widget; //main widget
	ShopItemId m_Id;
	ShopItemInfo m_Inf;

	GUIBase_Sprite m_Equiped_Sprite;
	GUIBase_Label m_EquipedLabel;
	GUIBase_Sprite m_Owned_Sprite;
	GUIBase_Label m_Owned_Label;
	GUIBase_Sprite m_OwnedCheck_Sprite;
	GUIBase_Sprite m_New_Sprite;
	GUIBase_Sprite m_Rare_Sprite;
	GUIBase_Sprite m_Sale_Sprite;
	GUIBase_Label m_Sale_Label;
	GUIBase_Sprite m_Locked_Sprite;
	GUIBase_Label m_Locked_Label;

	GUIBase_Sprite m_Gold_Sprite;

	GUIBase_Sprite m_Name_Sprite;
	GUIBase_Label m_Name_Label;

	GUIBase_Sprite m_Thumbnail; //sprite predmetu
	GUIBase_Sprite m_WeaponType_Sprite;
	GUIBase_Label m_DiscountLabel;

	GUIBase_Label m_AddCountText;

	bool m_EquipMenu; //true if we are in equip menu scroller

	public GuiScrollItem(ShopItemId id, GUIBase_Widget w, bool equipMenu)
	{
		m_Widget = w;
		m_Id = id;
		m_EquipMenu = equipMenu;
		UpdateItemInfo();
		InitGui();
	}

	public override void UpdateItemInfo()
	{
		m_Inf = ShopDataBridge.Instance.GetItemInfo(m_Id);
	}

	public override void Show()
	{
		if (m_Inf.EmptySlot)
		{
			m_Widget.Show(false, true);
		}
		else
		{
			m_Widget.Show(true, false);

			bool isWaiting = false;
			if (m_EquipMenu)
				isWaiting = GuiEquipMenu.Instance.IsSlotItemWaiting(m_Id);

			m_Thumbnail.Widget.FadeAlpha = isWaiting ? GuiEquipMenu.WAITING_ITEM_ALPHA : 1.0f;
			m_Thumbnail.Widget.Show(true, false);

			if (m_Id.ItemType == GuiShop.E_ItemType.Weapon)
			{
				m_WeaponType_Sprite.Widget.FadeAlpha = isWaiting ? GuiEquipMenu.WAITING_ITEM_ALPHA : 1.0f;
				m_WeaponType_Sprite.Widget.Show(true, false);
			}
			else
				m_WeaponType_Sprite.Widget.Show(false, false);

			string nameText = m_Inf.NameText;

			if (nameText != null && nameText.Length > 0)
			{
				m_Name_Sprite.Widget.FadeAlpha = isWaiting ? GuiEquipMenu.WAITING_ITEM_ALPHA : 1.0f;
				m_Name_Sprite.Widget.Show(true, true);

				m_Name_Label.Widget.FadeAlpha = isWaiting ? GuiEquipMenu.WAITING_ITEM_ALPHA : 1.0f;
				m_Name_Label.SetNewText(nameText);
			}
			else
				m_Name_Sprite.Widget.Show(false, true);

			//currency
			m_Gold_Sprite.Widget.Show(m_Inf.GoldCurrency && !m_EquipMenu && !m_Inf.Owned, true); //gold pouze v shopu, a pokud jej jeste nemame

			//show spriteto reflect state
			bool showNew = false;
			bool showSale = false;
			bool showOwned = false;
			bool showEquiped = false;
			bool showRare = false;

			bool isEquiped = ShopDataBridge.Instance.IsEquiped(m_Id);
			bool showLocked = m_Inf.Locked;

			if (isEquiped)
				showEquiped = true;
			else if (m_Inf.Owned && !m_EquipMenu)
				showOwned = true;
			else if (m_Inf.PriceSale && !m_EquipMenu)
				showSale = true;
			else if (m_Inf.RareItem && !m_EquipMenu)
				showRare = true;
			else if (m_Inf.NewInShop && !m_EquipMenu)
				showNew = true;

			//Debug.Log(m_Id + ": showEquiped " + showEquiped + " showOwned " + showOwned + " owned " + m_Inf.Owned + " showSale " + showSale + " showNew " + showNew );

			m_Equiped_Sprite.Widget.Show(showEquiped, true);
			if (m_Id.ItemType == GuiShop.E_ItemType.Skin)
			{
				m_EquipedLabel.SetNewText(02030066); //string Selected
			}
			else
			{
				m_EquipedLabel.SetNewText(2030059); //string EQUIPPED
			}

			m_Owned_Sprite.Widget.Show(showOwned, true);
			if (m_Inf.PremiumOnly)
			{
				m_Owned_Label.SetNewText(02050048); //string PREMIUM
			}
			else
			{
				m_Owned_Label.SetNewText(2030061); //string OWNED
			}

			m_OwnedCheck_Sprite.Widget.Show((m_Inf.Owned || m_Inf.Equiped) && !m_EquipMenu, true);

			m_Sale_Sprite.Widget.Show(showSale, true);
			if (showSale)
			{
				m_Sale_Label.SetNewText(m_Inf.DiscountTag);
			}

			m_New_Sprite.Widget.Show(showNew, true);
			m_Rare_Sprite.Widget.Show(showRare, true);

			m_Locked_Sprite.Widget.Show(showLocked, true);
			if (showLocked)
			{
				if (m_Inf.RequiresRank > 0)
				{
					string strReqRank = TextDatabase.instance[2030080];
					strReqRank = strReqRank.Replace("%d1", m_Inf.RequiresRank.ToString());
					m_Locked_Label.SetNewText(strReqRank);
				}
				else if (m_Inf.PremiumOnly)
				{
					string strReqPremium = TextDatabase.instance[02050045];
					m_Locked_Label.SetNewText(strReqPremium);
				}
			}

			if (m_Inf.Consumable)
			{
				string strCount;
				if (m_EquipMenu)
					strCount = m_Inf.OwnedCount.ToString();
				else
					strCount = "+" + m_Inf.AddCount.ToString();

				m_AddCountText.Widget.Show(true, true);
				m_AddCountText.SetNewText(strCount);
			}
			else
				m_AddCountText.Widget.Show(false, true);
		}
	}

	public override void Hide()
	{
		//Debug.Log("=====================hide");
		m_Widget.Show(false, true);
	}

	void InitGui()
	{
		//najdi vsechny prvky ze kterych je je polozka scrolleru slozene a nastav ji spravne hodnoty podle itemu

		//skiny a hats maji vetsi sprite
		m_Thumbnail = GuiBaseUtils.GetChildSprite(m_Widget, ThumbName(m_Id.ItemType));

		if (m_Inf.ScrollerWidget != null)
			m_Thumbnail.Widget.CopyMaterialSettings(m_Inf.ScrollerWidget);
		else if (m_Inf.SpriteWidget != null)
			m_Thumbnail.Widget.CopyMaterialSettings(m_Inf.SpriteWidget);

		m_WeaponType_Sprite = GuiBaseUtils.GetChildSprite(m_Widget, "WeaponType_Sprite");
		if (m_Inf.WeaponTypeWidget != null)
			m_WeaponType_Sprite.Widget.CopyMaterialSettings(m_Inf.WeaponTypeWidget);

		m_Equiped_Sprite = GuiBaseUtils.GetChildSprite(m_Widget, "Equiped_Sprite");
		m_EquipedLabel = GuiBaseUtils.GetChildLabel(m_Equiped_Sprite.Widget, "Label");
		m_Owned_Sprite = GuiBaseUtils.GetChildSprite(m_Widget, "Owned_Sprite");
		m_Owned_Label = GuiBaseUtils.GetChildLabel(m_Owned_Sprite.Widget, "Label");
		m_OwnedCheck_Sprite = GuiBaseUtils.GetChildSprite(m_Widget, "OwnedCheck_Sprite");
		m_New_Sprite = GuiBaseUtils.GetChildSprite(m_Widget, "New_Sprite");
		m_Rare_Sprite = GuiBaseUtils.GetChildSprite(m_Widget, "Rare_Sprite");
		m_Sale_Sprite = GuiBaseUtils.GetChildSprite(m_Widget, "Sale_Sprite");
		m_Sale_Label = GuiBaseUtils.GetChildLabel(m_Sale_Sprite.Widget, "Label");
		m_Locked_Sprite = GuiBaseUtils.GetChildSprite(m_Widget, "Locked_Sprite");
		m_Locked_Label = GuiBaseUtils.GetChildLabel(m_Locked_Sprite.Widget, "LockedText");

		m_Gold_Sprite = GuiBaseUtils.GetChildSprite(m_Widget, "Gold_Sprite");

		m_Name_Sprite = GuiBaseUtils.GetChildSprite(m_Widget, "Name_Sprite");
		m_Name_Label = GuiBaseUtils.GetChildLabel(m_Widget, "Name_Label");

		m_AddCountText = GuiBaseUtils.GetChildLabel(m_Widget, "AddCountText");
	}

	string ThumbName(GuiShop.E_ItemType t)
	{
		switch (t)
		{
		case GuiShop.E_ItemType.Skin:
			return "Thumbnail_Skin";
		case GuiShop.E_ItemType.Hat:
			return "Thumbnail_Hat";
		case GuiShop.E_ItemType.Item:
			return "Thumbnail_Item";
		default:
			return "Thumbnail_Default";
		}
	}
};
