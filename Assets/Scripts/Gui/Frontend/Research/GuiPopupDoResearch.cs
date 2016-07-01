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

// =====================================================================================================================
// =====================================================================================================================
public class GuiPopupDoResearch : GuiPopupAnimatedBase
{
	GUIBase_Label m_Caption_Label;
	GUIBase_Sprite m_BigThumbnail;
	GuiShopFunds m_Cost;

	IResearchItem m_ResearchItem = null;
	BaseCloudAction m_BuyCloudAction;
	BaseCloudAction m_CheckEquipCloudAction;

	// ------
	public override void SetCaption(string inCaption)
	{
	}

	// ------

	public override void SetText(string inText)
	{
	}

	// ------
	public void SetItem(IResearchItem item)
	{
		m_ResearchItem = item;

		//different caption for differen confirmantion (buy, upgrade, add gold, convert gold)
		string caption = TextDatabase.instance[m_ResearchItem.GetName()];
		m_Caption_Label.SetNewText(caption);

		m_BigThumbnail.Widget.CopyMaterialSettings(m_ResearchItem.GetImage());
		/*if(inf.PriceSale)
		{
			m_Sale_Label.SetNewText(inf.DiscountTag);
		}
		/**/
		//m_Sale_Label.Widget.Show( false, true);

		if (m_Cost != null)
		{
			m_Cost.Show(true);
			bool isGold;
			int cost = m_ResearchItem.GetPrice(out isGold);
			m_Cost.SetValue(cost, isGold);
		}
	}

	// ------
	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_Caption_Label = GuiBaseUtils.PrepareLabel(Layout, "Caption_Label");
		m_BigThumbnail = GuiBaseUtils.PrepareSprite(Layout, "BigThumbnail");
		GuiBaseUtils.RegisterButtonDelegate(Layout, "Back_Button", null, OnCloseButton);
		GuiBaseUtils.RegisterButtonDelegate(Layout, "Accept_Button", null, OnAcceptButton);
		//m_Sale_Label	= GuiBaseUtils.PrepareLabel(Layout, "Sale_Label");
		m_Cost = new GuiShopFunds(GuiBaseUtils.PrepareSprite(Layout, "Cost_Sprite"));
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

		int guid = m_ResearchItem.GetGUID();

		m_BuyCloudAction = new BuyAndFetchPPI(CloudUser.instance.authenticatedUserID, guid);
		GameCloudManager.AddAction(m_BuyCloudAction);

		//tohle se mi moc nelibi, vyvolavame wait box a result vlastne ani nepotrebujeme
		GuiPopupResearchWait popik =
						Owner.ShowPopup("ResearchWait", TextDatabase.instance[0113050], TextDatabase.instance[0113060], BuyWaitResultHandler) as
						GuiPopupResearchWait;
		popik.SetActionStatusDelegate(GetActionStatus);

		//Debug.Log(" Starting buy request: time " + Time.time + " item " + m_BuyItemId);
		//pri lokalni koupi by stacilo poslat jen result success
		//SendResult(E_PopupResultCode.Success);
	}

	// ------
	void NoFundsResultHandler(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		//so far nothing to do. (all handled in NoFundsDialog)
		//Debug.Log("Not funds result: " + inResult);
	}

	// ------
	GuiPopupResearchWait.E_AsyncOpStatus GetActionStatus()
	{
		if (m_CheckEquipCloudAction == null)
		{
			if ( /*(m_BuyCloudAction.isFailed == true) ||*/  (m_BuyCloudAction.isSucceeded == true))
			{
				m_CheckEquipCloudAction = GuiShopUtils.ValidateEquip();

				if (m_CheckEquipCloudAction != null)
				{
					GameCloudManager.AddAction(m_CheckEquipCloudAction);
					return DeduceActionStatus(m_CheckEquipCloudAction);
				}
				else
					return GuiPopupResearchWait.E_AsyncOpStatus.Finished;
			}
			else
				return DeduceActionStatus(m_BuyCloudAction);
		}
		else
			return DeduceActionStatus(m_CheckEquipCloudAction);
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

	void BuyWaitResultHandler(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		//Debug.Log("Waiting for buy finished. popup: " + inResult + " action status " + BuyActionStatus);
		if (inResult == E_PopupResultCode.Success)
		{
			Owner.Back();
			ResearchSupport.Instance.AnyResearchItemChanged();
			m_ResearchItem.StateChanged();
			SendResult(E_PopupResultCode.Success);
		}
		else
		{
			Owner.Back();
			SendResult(E_PopupResultCode.Failed);
		}

		m_ResearchItem = null;
		m_BuyCloudAction = null;
		m_CheckEquipCloudAction = null;
	}
}
