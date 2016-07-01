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

// =====================================================================================================================
// =====================================================================================================================
public class GuiPopupDoResetTree : GuiPopupAnimatedBase
{
	GUIBase_Button m_AcceptButton;
	GuiShopFunds m_CostWidgets;
	int m_Cost;
	int m_RefundedMoney;

	int[] m_ResearchGUIDs = null;
	BaseCloudAction m_ResetCloudAction;
	BaseCloudAction m_GetPPICloudAction;
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
	public void SetItems(int[] items)
	{
		m_ResearchGUIDs = items;
		m_Cost = 0;
		m_RefundedMoney = 0;
		m_Cost += m_ResearchGUIDs.Length*GameCloudSettings.REFUND_COST_PER_RESEARCH_ITEM;

		foreach (int i  in m_ResearchGUIDs)
		{
			m_RefundedMoney += ResearchSupport.Instance.GetPriceBasedOnGuid(i);
		}
		//Debug.Log("Refunded: " + m_RefundedMoney);

		if (m_CostWidgets != null)
		{
			m_CostWidgets.Show(true);
			m_CostWidgets.SetValue(m_Cost, true);
		}
		bool disabled = (m_Cost <= 0) || !(ResearchSupport.Instance.HasPlayerEnoughFunds(m_Cost, true));
		m_AcceptButton.SetDisabled(disabled);
		m_CostWidgets.SetDisabled(disabled);
	}

	// ------
	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_AcceptButton = GuiBaseUtils.GetButton(Layout, "Accept_Button");
		GuiBaseUtils.RegisterButtonDelegate(Layout, "Back_Button", null, OnCloseButton);
		GuiBaseUtils.RegisterButtonDelegate(Layout, "Accept_Button", null, OnAcceptButton);
		m_CostWidgets = new GuiShopFunds(GuiBaseUtils.PrepareSprite(Layout, "Cost_Sprite"));
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

		m_GetPPICloudAction = null;
		m_ResetCloudAction = new RefundItems(CloudUser.instance.authenticatedUserID, m_ResearchGUIDs);
		GameCloudManager.AddAction(m_ResetCloudAction);

		//tohle se mi moc nelibi, vyvolavame wait box a result vlastne ani nepotrebujeme
		GuiPopupResearchWait popik =
						Owner.ShowPopup("ResearchWait", TextDatabase.instance[0112015], TextDatabase.instance[0113040], ResetWaitResultHandler) as
						GuiPopupResearchWait;
		popik.SetActionStatusDelegate(GetActionStatus);
	}

	// ------
	void NoFundsResultHandler(GuiPopup inPopup, E_PopupResultCode inResult)
	{
	}

	// ------
	GuiPopupResearchWait.E_AsyncOpStatus GetActionStatus()
	{
		if (m_GetPPICloudAction == null)
		{
			if (m_ResetCloudAction.isDone == true)
			{
				m_GetPPICloudAction = new FetchPlayerPersistantInfo(CloudUser.instance.authenticatedUserID);
				GameCloudManager.AddAction(m_GetPPICloudAction);

				return DeduceActionStatus(m_GetPPICloudAction);
			}
			else
				return DeduceActionStatus(m_ResetCloudAction);
		}
		else
		{
			if (m_CheckEquipCloudAction == null)
			{
				if (m_GetPPICloudAction.isDone == true)
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
					return DeduceActionStatus(m_GetPPICloudAction);
			}
			else
				return DeduceActionStatus(m_CheckEquipCloudAction);
		}
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
	void ResetWaitResultHandler(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		//Debug.Log("Waiting for buy finished. popup: " + inResult + " action status " + BuyActionStatus);
		if (inResult == E_PopupResultCode.Success)
		{
			if (m_ResetCloudAction != null && m_ResetCloudAction.isSucceeded == true)
			{
				//Debug.Log(Time.realtimeSinceStartup + " " + m_ResetCloudAction.status);
			}

			Owner.Back();
			ResearchSupport.Instance.AnyResearchItemChanged();
			SendResult(E_PopupResultCode.Success);
		}
		else
		{
			Owner.Back();
			SendResult(E_PopupResultCode.Failed);
		}

		m_Cost = 0;
		m_RefundedMoney = 0;
		m_ResearchGUIDs = null;
		m_ResetCloudAction = null;
		m_CheckEquipCloudAction = null;
	}
}
