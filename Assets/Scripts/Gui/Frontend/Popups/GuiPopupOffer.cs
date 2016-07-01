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

public class GuiPopupOffer : GuiPopup
{
	public enum E_Type
	{
		None,
		Item,
		PremiumAcct,
		MoreApps,
		FreeGold,
		Hat,
		Consumable
	}

	// PRIVATE MEMBERS

	[SerializeField] int m_DefaultHintTextId = 0502047;

	[SerializeField] int m_GetItemCaptionId = 0502046;
	[SerializeField] int m_UpgradeItemCaptionId = 0502049;

	[SerializeField] int m_PremiumAcctTextId = 0105022;
	[SerializeField] int m_GetPremiumAcctCaptionId = 0105001;
	[SerializeField] int m_UpgradePremiumAcctCaptionId = 0105006;

	[SerializeField] int m_MoreAppsTextId = 0100005;
	[SerializeField] int m_MoreAppsCaptionId = 0;

	[SerializeField] int m_FreeGoldTextId = 02030007;
	[SerializeField] int m_GetFreeGoldCaptiopnId = 0211031;
	[SerializeField] int m_FreeGoldHintTextId = 0211033;

	[SerializeField] int m_GetHatCaptionId = 0;

	[SerializeField] int m_GetConsumableCaptionId = 00502046;

	E_Type m_Type;
	object m_Data;

	// PUBLIC METHODS

	public void SetData(E_Type type, object data = null)
	{
		m_Type = type;
		m_Data = data;

		string caption = "";
		string text = "";
		string hint = TextDatabase.instance[m_DefaultHintTextId];
		bool showGratz = false;

		switch (m_Type)
		{
		case E_Type.None:
			break;
		case E_Type.Item:
			if (m_Data != null)
			{
				var item = (UserGuideAction_Offers.ItemDesc)m_Data;
				caption = TextDatabase.instance[item.Owned ? m_UpgradeItemCaptionId : m_GetItemCaptionId];
				text = TextDatabase.instance[item.Item.GetName()];
				showGratz = true;

				ShowImage("Item_Image", item.Item.GetImage());
			}
			break;
		case E_Type.PremiumAcct:
		{
			bool owned = CloudUser.instance.isPremiumAccountActive;
			caption = TextDatabase.instance[owned ? m_UpgradePremiumAcctCaptionId : m_GetPremiumAcctCaptionId];
			text = TextDatabase.instance[m_PremiumAcctTextId];

			ShowImage("Premium_Image");
		}
			break;
		case E_Type.MoreApps:
		{
			caption = TextDatabase.instance[m_MoreAppsCaptionId];
			text = TextDatabase.instance[m_MoreAppsTextId];

			ShowImage("MoreApps_Image");
		}
			break;
		case E_Type.FreeGold:
		{
			caption = TextDatabase.instance[m_GetFreeGoldCaptiopnId];
			text = TextDatabase.instance[m_FreeGoldTextId];
			hint = TextDatabase.instance[m_FreeGoldHintTextId];

			ShowImage("FreeGold_Image");
		}
			break;
		case E_Type.Hat:
		{
			var item = (UserGuideAction_Offers.HatDesc)m_Data;
			caption = TextDatabase.instance[m_GetHatCaptionId];
			text = TextDatabase.instance[item.Item.Name];
			showGratz = true;

			ShowImage("Hat_Image", item.Item.ShopWidget);
		}
			break;
		case E_Type.Consumable:
		{
			var item = (UserGuideAction_Offers.ConsumableDesc)m_Data;
			caption = TextDatabase.instance[m_GetConsumableCaptionId];
			text = TextDatabase.instance[item.Item.Name];
			showGratz = true;

			ShowImage("Item_Image", item.Item.ShopWidget);
		}
			break;
		default:
			throw new System.IndexOutOfRangeException();
		}

		SetCaption(caption);
		SetText(text);
		SetHint(hint);
		ShowGratz(showGratz);
	}

	// GUIPOPUP INTERFACE

	public override void SetCaption(string inCaption)
	{
		GUIBase_Label label = GuiBaseUtils.GetControl<GUIBase_Label>(Layout, "Caption_Label");
		label.SetNewText(inCaption);
	}

	public override void SetText(string inText)
	{
		GUIBase_Label label = GuiBaseUtils.GetControl<GUIBase_Label>(Layout, "Text_Label");
		label.SetNewText(inText);
	}

	public void SetHint(string inText)
	{
		GUIBase_Label label = GuiBaseUtils.GetControl<GUIBase_Label>(Layout, "Hint_Label");
		label.SetNewText(inText);
	}

	public void ShowGratz(bool state)
	{
		GUIBase_Widget widget = Layout.GetWidget("Gratz_Label");
		widget.Show(state, true);
	}

	// GUIVIEW INTERFACE

	protected override void OnViewShow()
	{
		base.OnViewShow();

		GuiBaseUtils.RegisterButtonDelegate(Layout,
											"Close_Button",
											() =>
											{
												Owner.Back();
												SendResult(E_PopupResultCode.Cancel);
											},
											null);

		GuiBaseUtils.RegisterButtonDelegate(Layout,
											"OK_Button",
											() =>
											{
												IViewOwner owner = Owner;

												owner.Back();

												switch (m_Type)
												{
												case E_Type.None:
													break;
												case E_Type.Item:
													if (m_Data != null)
													{
														var item = (UserGuideAction_Offers.ItemDesc)m_Data;

														owner.ShowScreen("ResearchMain:" + item.Item.m_GuiPageIndex);
														item.Item.ButtonPressed();
													}
													break;
												case E_Type.PremiumAcct:
													owner.ShowPopup("PremiumAccount", "", "", null);
													break;
												case E_Type.MoreApps:
													owner.DoCommand("MoreApps");
													break;
												case E_Type.FreeGold:
													// show tapjoy native plugin gui
													GuiShopUtils.EarnFreeGold(new ShopItemId((int)E_FundID.TapJoyInApp, GuiShop.E_ItemType.Fund));
													break;
												case E_Type.Hat:
													owner.ShowScreen("Shop:2");
													break;
												case E_Type.Consumable:
													owner.ShowScreen("Shop:0");
													break;
												default:
													throw new System.IndexOutOfRangeException();
												}

												SendResult(E_PopupResultCode.Ok);
											},
											null);
	}

	protected override void OnViewHide()
	{
		if (Layout != null)
		{
			if (Layout.GetComponent<AudioSource>() != null && Layout.GetComponent<AudioSource>().isPlaying == true)
			{
				Layout.GetComponent<AudioSource>().Stop();
			}

			GuiBaseUtils.RegisterButtonDelegate(Layout, "OK_Button", null, null);
			GuiBaseUtils.RegisterButtonDelegate(Layout, "Close_Button", null, null);
		}

		base.OnViewHide();
	}

	// PRIVATE METHODS

	void ShowImage(string name)
	{
		GUIBase_Widget widget = Layout.GetWidget(name);
		widget.Show(true, true);
	}

	void ShowImage(string name, GUIBase_Widget image)
	{
		GUIBase_Widget widget = Layout.GetWidget(name);
		widget.CopyMaterialSettings(image);
		widget.Show(true, true);
	}
}
