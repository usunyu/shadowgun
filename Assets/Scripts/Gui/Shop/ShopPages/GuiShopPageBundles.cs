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

public class GuiShopPageBundles : GuiShopPageBase
{
	class BundleGui
	{
		public GUIBase_Widget Root;
		public GUIBase_Sprite Sprite;
		public GUIBase_Label Label;
		public GUIBase_Sprite Owned;
	}

	const int MAX_BUNDLE_ITEMS = 4;
	BundleGui[] m_BundleGui = new BundleGui[MAX_BUNDLE_ITEMS];

	// GUIVIEW INTERFACE
	protected override void OnViewInit()
	{
		base.OnViewInit();

		for (int i = 0; i < MAX_BUNDLE_ITEMS; i++)
		{
			BundleGui bg = new BundleGui();

			string bundleName = "Bundle" + i.ToString();
			bg.Root = m_ScreenLayout.GetWidget(bundleName);
			bg.Label = GuiBaseUtils.GetChildLabel(bg.Root, "Bundle_Name_Label");
			bg.Sprite = GuiBaseUtils.GetChildSprite(bg.Root, "Bundle_Thumbnail_Default");
			bg.Owned = GuiBaseUtils.GetChildSprite(bg.Root, "Owned_Sprite");
			m_BundleGui[i] = bg;
		}
	}

	protected override void OnViewReset()
	{
		base.OnViewReset();
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();
	}

	protected override void OnViewHide()
	{
		base.OnViewHide();
	}

	// SPOLECNY ITERFACE PRO VSECHNY PAGE
	public override void OnItemChange(ShopItemId bundleId, bool forceUpdateView)
	{
		base.OnItemChange(bundleId, forceUpdateView);

		if (bundleId.IsEmpty())
		{
			//hide icons
			foreach (BundleGui bg in m_BundleGui)
				bg.Root.Show(false, true);
		}

		//show all items in the bundle and hide the rest of the icons
		ShopItemInfo bundleInf = ShopDataBridge.Instance.GetItemInfo(bundleId);

		for (int i = 0; i < MAX_BUNDLE_ITEMS; i++)
		{
			BundleGui bg = m_BundleGui[i];
			if (bg.Root == null)
				continue;

			//do we have an item on this index?
			if (i < bundleInf.BundleItems.Count)
			{
				//get info about bundle item
				ShopItemId id = bundleInf.BundleItems[i];
				ShopItemInfo itemInfo = ShopDataBridge.Instance.GetItemInfo(id);
				bg.Root.Show(true, false);
				bg.Label.SetNewText(itemInfo.NameText);
				bg.Label.Widget.Show(true, true);
				bg.Owned.Widget.Show(itemInfo.Owned, true);

				//sprite
				GUIBase_Sprite newSprite = GuiBaseUtils.GetChildSprite(bg.Root, ThumbName(id.ItemType));
				if (bg.Sprite != newSprite)
				{
					bg.Sprite.Widget.Show(false, true);
					bg.Sprite = newSprite;
				}

				bg.Sprite.Widget.CopyMaterialSettings(itemInfo.ScrollerWidget ? itemInfo.ScrollerWidget : itemInfo.SpriteWidget);
				bg.Sprite.Widget.Show(true, true);
			}
			else
				bg.Root.Show(false, true);
		}
	}

	public override List<ShopItemId> GetItems()
	{
		return ShopDataBridge.Instance.GetBundles();
	}

	//--------------------------------------------------------------------------------	
	//--------------------------------------------------------------------------------	
	string ThumbName(GuiShop.E_ItemType t)
	{
		switch (t)
		{
		case GuiShop.E_ItemType.Skin:
			return "Bundle_Thumbnail_Skin";
		case GuiShop.E_ItemType.Hat:
			return "Bundle_Thumbnail_Hat";
		case GuiShop.E_ItemType.Account:
			return "Bundle_Thumbnail_Hat";
		case GuiShop.E_ItemType.Item:
			return "Bundle_Thumbnail_Item";
		default:
			return "Bundle_Thumbnail_Default";
		}
	}
}
