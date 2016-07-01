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

// -----------
[AddComponentMenu("GUI/Frontend/Screens/GuiScreenResearch")]
public class GuiScreenResearch : GuiScreen
{
	List<ResearchItem> m_Items = new List<ResearchItem>();

	// ------
	protected override void OnViewInit()
	{
		ResearchItem[] items = Layout.GetComponentsInChildren<ResearchItem>();

		foreach (ResearchItem item in items)
		{
			if (item.Enabled())
			{
				m_Items.Add(item);
				item.Init();
				item.m_GuiPageIndex = MultiPageIndex;
			}
		}
		if (m_Items.Count > 0)
		{
			GUIBase_Widget resetParent = GuiBaseUtils.GetChild<GUIBase_Widget>(Layout, "Reset Tree");
			GUIBase_Button resetTree = ResearchSupport.Instance.GetNewResetTreeButton();
			resetTree.Widget.Relink(resetParent);
			resetTree.RegisterTouchDelegate(ResetResearchTree);
		}
	}

	// ------
	protected override void OnViewShow()
	{
		foreach (ResearchItem item in m_Items)
			item.Show(Owner);
	}

	// ------
	protected override void OnViewHide()
	{
		foreach (ResearchItem item in m_Items)
			item.Hide();
	}

	// ------
	protected void ResetResearchTree()
	{
		List<int> guids = new List<int>();

		foreach (ResearchItem item in m_Items)
		{
			if (item.GetState() == ResearchState.Active)
			{
				ResearchSupport.Instance.AddAllConnectedItemGUIDs(item, guids);
			}
		}

		GuiPopupDoResetTree popik = Owner.ShowPopup("DoResetTree", "", "", ResetTreeResultHandler) as GuiPopupDoResetTree;
		popik.SetItems(guids.ToArray());
	}

	// ------
	void ResetTreeResultHandler(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		//if(inResult == E_PopupResultCode.Success)
		{
			foreach (ResearchItem item in m_Items)
				item.StateChanged();
		}
	}
}
