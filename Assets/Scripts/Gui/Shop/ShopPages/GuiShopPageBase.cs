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

public class GuiShopPageBase : GuiScreen
{
	//public
	public ShopItemId LastId { get; private set; }

	//private
	GuiShopPageShared m_SharedGui = new GuiShopPageShared();

	// GUIVIEW INTERFACE
	protected override void OnViewInit()
	{
		//Debug.Log("OnViewInit");

		LastId = ShopItemId.EmptyId;
		m_SharedGui.InitGui(Layout);
	}

	protected override void OnViewReset()
	{
		//Debug.Log("OnViewReset");
	}

	protected override void OnViewShow()
	{
		m_SharedGui.Show(LastId);
	}

	protected override void OnViewHide()
	{
		//m_SharedGui.Hide();  //not necessary while it have its own layout
	}

	// SPOLECNY ITERFACE PRO VSECHNY PAGE
	public virtual void OnItemChange(ShopItemId itemId, bool forceUpdateView)
	{
		//Debug.Log("ItemChange " + itemId);

		if (!IsVisible)
			return;

		//caching
		if (itemId == LastId && !forceUpdateView)
			return;

		LastId = itemId;

		//update shared view
		m_SharedGui.Show(LastId);
	}

	public virtual List<ShopItemId> GetItems()
	{
		return null;
	}
}
