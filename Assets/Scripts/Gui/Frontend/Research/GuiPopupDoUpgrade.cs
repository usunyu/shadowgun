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

//#define TEMPORARY_UPGRADE_HACK

using UnityEngine;
using System.Collections;

// =====================================================================================================================
// =====================================================================================================================
public class GuiPopupDoUpgrade : GuiPopupAnimatedBase
{
	GUIBase_Label m_Caption_Label;
	GUIBase_Label m_Value_Label;
	GUIBase_Button m_AcceptButton;
	GUIBase_Sprite m_BigThumbnail;
	GuiShopFunds m_Cost;

	IResearchItem m_ResearchItem = null;
#if !TEMPORARY_UPGRADE_HACK
	BaseCloudAction m_UpgradeCloudAction;
#endif
	UpgradeIcon m_UpgradeIcon;
	int m_UpgradeIndex = -1;

	// ------
	public override void SetCaption(string inCaption)
	{
	}

	// ------
	public override void SetText(string inText)
	{
	}

	// ------
	public void SetItem(IResearchItem item, int upgradeIndex)
	{
		m_ResearchItem = item;
		m_UpgradeIndex = upgradeIndex;

		m_Caption_Label.SetNewText(TextDatabase.instance[item.GetUpgradeName(upgradeIndex)]);
		m_Value_Label.SetNewText(item.GetUpgradeValueText(upgradeIndex));

		if (m_Cost != null)
		{
			m_Cost.Show(true);

			WeaponSettings.Upgrade upgrade = m_ResearchItem.GetUpgrade(upgradeIndex);

			int cost;
			bool hasEnoughFunds;

			if (upgrade.MoneyCost > 0)
			{
				cost = upgrade.MoneyCost;
				hasEnoughFunds = ResearchSupport.Instance.HasPlayerEnoughFunds(cost, false);
				m_Cost.SetValue(cost, false);
			}
			else
			{
				cost = upgrade.GoldCost;
				hasEnoughFunds = ResearchSupport.Instance.HasPlayerEnoughFunds(cost, true);
				m_Cost.SetValue(cost, true);
			}

			if (!hasEnoughFunds)
			{
				m_AcceptButton.SetDisabled(upgrade.GoldCost > 0 ? false : true);
				m_Cost.SetMissingFunds(true);
			}
			else
			{
				m_AcceptButton.SetDisabled(false);
				m_Cost.SetMissingFunds(false);
			}
		}

		m_UpgradeIcon.SetUpgradeType(item.GetUpgrade(upgradeIndex).ID);
		m_UpgradeIcon.SetStatus(UpgradeIcon.Status.Active);
		m_UpgradeIcon.Show();
	}

	// ------
	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_Caption_Label = GuiBaseUtils.PrepareLabel(Layout, "Caption_Label");
		m_Value_Label = GuiBaseUtils.PrepareLabel(Layout, "Value_Label");
		m_AcceptButton = GuiBaseUtils.GetControl<GUIBase_Button>(Layout, "Accept_Button");
		m_BigThumbnail = GuiBaseUtils.PrepareSprite(Layout, "BigThumbnail");
		GuiBaseUtils.RegisterButtonDelegate(Layout, "Back_Button", null, OnCloseButton);
		GuiBaseUtils.RegisterButtonDelegate(Layout, "Accept_Button", null, OnAcceptButton);
		m_Cost = new GuiShopFunds(GuiBaseUtils.PrepareSprite(Layout, "Cost_Sprite"));
		m_UpgradeIcon = ResearchSupport.Instance.GetNewUpgradeIcon();
		m_UpgradeIcon.Relink(m_BigThumbnail.Widget);
	}

	// ------
	protected override void OnViewShow()
	{
		base.OnViewShow();
	}

	// ------
	protected override void OnViewUpdate()
	{
		if (IsVisible)
		{
			//todo: update statusu buy operace?			
		}

		base.OnViewUpdate();
	}

	// ------
	void OnCloseButton(bool inside)
	{
		if (!inside)
			return;

		Owner.Back();
		SendResult(E_PopupResultCode.Cancel);
	}

	// ------
	void OnAcceptButton(bool inside)
	{
		if (!inside)
			return;

		WeaponSettings.Upgrade upgrade = m_ResearchItem.GetUpgrade(m_UpgradeIndex);
		if (upgrade.GoldCost > 0 && ResearchSupport.Instance.HasPlayerEnoughFunds(upgrade.GoldCost, true) == false)
		{
			//zobrazit not funds popup
			/*GuiShopNotFundsPopup.Instance.DesiredItem = new ShopItemId((int)upgrade.ParentID, GuiShop.E_ItemType.Weapon);
			GuiShopNotFundsPopup.Instance.UpgradeID   = m_UpgradeIndex;
			Owner.ShowPopup("NotFundsPopup", "", "");*/
			Owner.ShowPopup("ShopMessageBox", TextDatabase.instance[02030091], TextDatabase.instance[02030092], null);
		}
		else
		{
#if !TEMPORARY_UPGRADE_HACK
			int guid = upgrade.GetGUID();
			m_UpgradeCloudAction = new BuyAndFetchPPI(CloudUser.instance.authenticatedUserID, guid);
			GameCloudManager.AddAction(m_UpgradeCloudAction);
#else
				// TEMPORARY CODE
			ResearchSupport.Instance.GetPPI().InventoryList.TMP_CODE_AddWeaponUpgrade( (m_ResearchItem as ResearchItem).weaponID, m_ResearchItem.GetUpgrade(m_UpgradeIndex).ID );
#endif
			GuiPopupResearchWait popik =
							Owner.ShowPopup("ResearchWait", TextDatabase.instance[0113050], TextDatabase.instance[0113060], BuyWaitResultHandler) as
							GuiPopupResearchWait;
			popik.SetActionStatusDelegate(GetActionStatus);
		}
	}

	// ------
	GuiPopupResearchWait.E_AsyncOpStatus GetActionStatus()
	{
#if TEMPORARY_UPGRADE_HACK		
		return GuiPopupResearchWait.E_AsyncOpStatus.Finished;
#else
		return DeduceActionStatus(m_UpgradeCloudAction);
#endif
	}

	// ------
	GuiPopupResearchWait.E_AsyncOpStatus DeduceActionStatus(BaseCloudAction action)
	{
		if (action.isFailed == true)
			return GuiPopupResearchWait.E_AsyncOpStatus.Failed;
		if (action.isSucceeded == true)
			return GuiPopupResearchWait.E_AsyncOpStatus.Finished;
		return GuiPopupResearchWait.E_AsyncOpStatus.Waiting;
	}

	// ------
	void BuyWaitResultHandler(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		//Debug.Log("Waiting for buy finished. popup: " + inResult + " action status " + BuyActionStatus);
		if (inResult == E_PopupResultCode.Success)
		{
			Owner.Back();
			m_ResearchItem.StateChanged();
			SendResult(E_PopupResultCode.Ok);
		}
		else
		{
			Owner.Back();
			SendResult(E_PopupResultCode.Failed);
		}

		m_ResearchItem = null;
#if !TEMPORARY_UPGRADE_HACK
		m_UpgradeCloudAction = null;
#endif
	}
}
