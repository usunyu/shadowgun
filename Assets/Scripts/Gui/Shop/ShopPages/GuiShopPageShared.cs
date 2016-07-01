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
using System.Collections.Generic;

//updatuje gui sdilene mezi strankami shopu (informace o kupovanych predmetech jsou zatim z vetsi casti sdilene).
public class GuiShopPageShared
{
	//widget names
	readonly static string NAME_LABEL = "Name_Label";
	readonly static string COST_SPRITE = "Cost_Sprite";
	readonly static string DESCR_LABEL = "Desc_Label";
	readonly static string OWNED_SPRITE = "OwnedSprite";
	readonly static string WIDGET_SALE = "Sale";
	readonly static string SALE_LABEL = "Sale_Label";
	readonly static string SPEC_INFO_LABEL = "SpecInfo_Label";
	readonly static string WIDGET_IAP = "IAP";
	readonly static string IAP_COST = "IAP_Cost_Label";
	readonly static string COST_BEFORE_SALE = "CostBeforeSale_Label";

	//widgets
	GUIBase_Label m_NameLabel;
	GUIBase_Sprite m_PictureSprite;
	GuiShopFunds m_Cost;
	GUIBase_TextArea m_DescArea;
	GUIBase_Sprite m_OwnedSprite;
	GUIBase_Widget m_Sale;
	GUIBase_Label m_SaleLabel;
	GUIBase_Label m_SpecInfoLabel;
	GUIBase_Layout m_Layout;
	GUIBase_Widget m_IAP_Widget;
	GUIBase_Label m_IAP_Cost_Label;
	GUIBase_Label m_CostBeforeSale;

	public void InitGui(GUIBase_Layout Layout)
	{
		m_Layout = Layout;

		m_NameLabel = GuiBaseUtils.PrepareLabel(Layout, NAME_LABEL);
		//m_PictureSprite		= GuiBaseUtils.PrepareSprite(Layout, PICTURE_SPRITE);
		m_Cost = new GuiShopFunds(GuiBaseUtils.PrepareSprite(Layout, COST_SPRITE));
		m_CostBeforeSale = GuiBaseUtils.PrepareLabel(Layout, COST_BEFORE_SALE);

		m_DescArea = GuiBaseUtils.PrepareTextArea(Layout, DESCR_LABEL);
		m_OwnedSprite = GuiBaseUtils.PrepareSprite(Layout, OWNED_SPRITE);
		m_Sale = GuiBaseUtils.FindLayoutWidget<GUIBase_Widget>(Layout, WIDGET_SALE);
		m_SaleLabel = GuiBaseUtils.PrepareLabel(Layout, SALE_LABEL);
		m_SpecInfoLabel = GuiBaseUtils.PrepareLabel(Layout, SPEC_INFO_LABEL);
		m_IAP_Widget = GuiBaseUtils.FindLayoutWidget<GUIBase_Widget>(Layout, WIDGET_IAP);
		m_IAP_Cost_Label = GuiBaseUtils.PrepareLabel(Layout, IAP_COST);
	}

	public void Show(ShopItemId itemId)
	{
		m_PictureSprite = GuiBaseUtils.PrepareSprite(m_Layout, ThumbName(itemId.ItemType));

		if (itemId != ShopItemId.EmptyId)
		{
			ShopItemInfo inf = ShopDataBridge.Instance.GetItemInfo(itemId);

			m_NameLabel.Widget.Show(true, false);
			m_NameLabel.SetNewText(inf.NameText);

			m_PictureSprite.Widget.Show(true, false);
			m_PictureSprite.Widget.CopyMaterialSettings(inf.SpriteWidget);

			//string strDesc = TextDatabase.instance[ inf.Description ];
			if (itemId.ItemType == GuiShop.E_ItemType.Hat)
			{
				m_SpecInfoLabel.SetNewText(TextDatabase.instance[0208048]);
				m_SpecInfoLabel.Widget.Show(true, false);
			}
			else
			{
				m_SpecInfoLabel.Widget.Show(false, false);
			}
			m_DescArea.SetNewText(inf.Description);
			m_DescArea.Widget.SetModify(true);
			m_DescArea.Widget.Show(true, false);

			m_OwnedSprite.Widget.Show(inf.Owned, true);

			//show cost only for what costs money
			bool showCost = (inf.Cost > 0) && (!inf.Owned || inf.Consumable);
			m_Cost.SetValue(inf.Cost, inf.GoldCurrency, false);
			m_Cost.Show(showCost);

			m_Sale.Show(inf.PriceSale, true);
			bool showCostBeforeSale = false;
			if (inf.PriceSale)
			{
				m_SaleLabel.SetNewText(inf.DiscountTag);
				showCostBeforeSale = (inf.CostBeforeSale > 0);
				m_CostBeforeSale.SetNewText(inf.CostBeforeSale.ToString());
			}
			m_CostBeforeSale.Widget.Show(showCostBeforeSale, true);

			bool showIAP = ShopDataBridge.Instance.IsIAPFund(itemId) && inf.IAPCost != null;
			if (showIAP)
			{
				m_IAP_Widget.Show(true, true);
				m_IAP_Cost_Label.SetNewText(inf.IAPCost);
			}
			else
				m_IAP_Widget.Show(false, true);

#if IAP_USE_MFLIVE //when using MFLive, amount of gold is chosen at the paywall page, so we display just "Gold" without any sales
			if (inf.AddGold > 0)
			{
				m_NameLabel.SetNewText(TextDatabase.instance[2030004]);
				m_Sale.Show(false, true);
			}
#endif
		}
		else
		{
			//Debug.Log("Empty id");
			m_NameLabel.Widget.Show(false, false);
			m_PictureSprite.Widget.Show(false, false);
			m_Cost.Show(false);
			m_DescArea.Widget.Show(false, false);
			m_OwnedSprite.Widget.Show(false, false);
			m_SaleLabel.Widget.Show(false, true);
		}
	}

	string ThumbName(GuiShop.E_ItemType t)
	{
		switch (t)
		{
		case GuiShop.E_ItemType.Skin:
			return "Thumbnail_Skin";
		case GuiShop.E_ItemType.Hat:
			return "Thumbnail_Hat";
		default:
			return "Thumbnail_Default";
		}
	}
}
